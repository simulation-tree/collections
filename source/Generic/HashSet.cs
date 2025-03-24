using Collections.Pointers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Generic
{
    public unsafe struct HashSet<T> : IDisposable, ICollection<T>, IReadOnlyCollection<T>, IEquatable<HashSet<T>> where T : unmanaged, IEquatable<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private HashSetPointer* hashSet;

        /// <summary>
        /// Number of values in the hash set.
        /// </summary>
        public readonly int Count
        {
            get
            {
                MemoryAddress.ThrowIfDefault(hashSet);

                return hashSet->count;
            }
        }

        /// <summary>
        /// Capacity of the hash set.
        /// </summary>
        public readonly int Capacity
        {
            get
            {
                MemoryAddress.ThrowIfDefault(hashSet);

                return hashSet->capacity;
            }
        }

        /// <summary>
        /// Checks if the hash set has been disposed.
        /// </summary>
        public readonly bool IsDisposed => hashSet is null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly bool ICollection<T>.IsReadOnly => false;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Values
        {
            get
            {
                T[] values = new T[Count];
                uint index = 0;
                foreach (T value in this)
                {
                    values[index++] = value;
                }

                return values;
            }
        }

        /// <summary>
        /// Initializes an existing hash set from the given <paramref name="pointer"/>.
        /// </summary>
        public HashSet(HashSetPointer* pointer)
        {
            hashSet = pointer;
        }

        /// <summary>
        /// Creates a new hash set with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public HashSet(int initialCapacity)
        {
            initialCapacity = Math.Max(1, initialCapacity).GetNextPowerOf2();
            hashSet = MemoryAddress.AllocatePointer<HashSetPointer>();
            hashSet->values = MemoryAddress.Allocate(initialCapacity * sizeof(T));
            hashSet->hashCodes = MemoryAddress.Allocate(initialCapacity * sizeof(int));
            hashSet->occupied = MemoryAddress.AllocateZeroed(initialCapacity);
            hashSet->capacity = initialCapacity;
            hashSet->count = 0;
            hashSet->stride = sizeof(T);
        }

#if NET
        /// <summary>
        /// Creates a new hash set.
        /// </summary>
        public HashSet()
        {
            hashSet = MemoryAddress.AllocatePointer<HashSetPointer>();
            hashSet->values = MemoryAddress.Allocate(4 * sizeof(T));
            hashSet->hashCodes = MemoryAddress.Allocate(4 * sizeof(int));
            hashSet->occupied = MemoryAddress.AllocateZeroed(4);
            hashSet->capacity = 4;
            hashSet->count = 0;
            hashSet->stride = sizeof(T);
        }
#endif

        /// <summary>
        /// Disposes the hash set.
        /// <para>Contained values need to be disposed manually prior to
        /// calling this if they are allocations/disposable themselves.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(hashSet);

            hashSet->hashCodes.Dispose();
            hashSet->values.Dispose();
            hashSet->occupied.Dispose();
            MemoryAddress.Free(ref hashSet);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= hashSet->capacity)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for hash set of size {hashSet->capacity}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfAlreadyPresent(T value)
        {
            if (Contains(value))
            {
                throw new InvalidOperationException($"Value `{value}` already exists in hash set");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfMissing(T value)
        {
            if (!Contains(value))
            {
                throw new KeyNotFoundException($"Value `{value}` not found in hash set");
            }
        }

        private readonly bool TryGetValueAtIndex(int index, out T value)
        {
            MemoryAddress.ThrowIfDefault(hashSet);
            ThrowIfOutOfRange(index);

            value = hashSet->values.ReadElement<T>(index);
            return hashSet->occupied.ReadElement<bool>(index);
        }

        private readonly T GetValueAtIndex(int index)
        {
            MemoryAddress.ThrowIfDefault(hashSet);
            ThrowIfOutOfRange(index);

            return hashSet->values.ReadElement<T>(index);
        }

        private readonly bool ContainsAtIndex(int index)
        {
            MemoryAddress.ThrowIfDefault(hashSet);
            ThrowIfOutOfRange(index);

            return hashSet->occupied.ReadElement<bool>(index);
        }

        private readonly void Resize()
        {
            MemoryAddress.ThrowIfDefault(hashSet);

            int oldCapacity = hashSet->capacity;
            int newCapacity = oldCapacity * 2;
            hashSet->capacity = newCapacity;
            int count = 0;

            MemoryAddress oldValues = hashSet->values;
            MemoryAddress oldOccupied = hashSet->occupied;
            MemoryAddress oldKeyHashCodes = hashSet->hashCodes;

            hashSet->values = MemoryAddress.Allocate(newCapacity * sizeof(T));
            hashSet->hashCodes = MemoryAddress.Allocate(newCapacity * sizeof(int));
            hashSet->occupied = MemoryAddress.AllocateZeroed(newCapacity);
            Span<T> oldValuesSpan = new(oldValues.Pointer, oldCapacity);
            Span<bool> oldOccupiedSpan = new(oldOccupied.Pointer, oldCapacity);
            Span<bool> newOccupiedSpan = new(hashSet->occupied.Pointer, newCapacity);
            Span<int> newHashCodes = new(hashSet->hashCodes.Pointer, newCapacity);
            Span<T> newValuesSpan = new(hashSet->values.Pointer, newCapacity);

            for (int i = 0; i < oldCapacity; i++)
            {
                if (oldOccupiedSpan[i])
                {
                    T value = oldValuesSpan[i];
                    int hashCode = SharedFunctions.GetHashCode(value);
                    int index = hashCode % newCapacity;
                    int startIndex = index;
                    while (newOccupiedSpan[index])
                    {
                        index = (index + 1) % newCapacity;
                    }

                    newOccupiedSpan[index] = true;
                    newValuesSpan[index] = value;
                    newHashCodes[index] = hashCode;
                    count++;
                }
            }

            hashSet->count = count;
            oldValues.Dispose();
            oldOccupied.Dispose();
            oldKeyHashCodes.Dispose();
        }

        /// <summary>
        /// Checks if the hash set contains the given <paramref name="value"/>.
        /// </summary>
        public readonly bool Contains(T value)
        {
            MemoryAddress.ThrowIfDefault(hashSet);

            int capacity = hashSet->capacity;
            int hashCode = SharedFunctions.GetHashCode(value);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(hashSet->occupied.Pointer, capacity);
            Span<T> values = new(hashSet->values.Pointer, capacity);
            Span<int> hashCodes = new(hashSet->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (hashCodes[index] == hashCode && values[index].Equals(value))
                {
                    return true;
                }

                index = (index + 1) % capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds the given <paramref name="value"/>.
        /// </summary>
        public readonly void Add(T value)
        {
            MemoryAddress.ThrowIfDefault(hashSet);
            ThrowIfAlreadyPresent(value);

            int capacity = hashSet->capacity;
            int newCount = hashSet->count + 1;
            if (newCount > capacity)
            {
                Resize();
                capacity = hashSet->capacity;
            }

            int hashCode = SharedFunctions.GetHashCode(value);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(hashSet->occupied.Pointer, capacity);
            Span<T> values = new(hashSet->values.Pointer, capacity);
            Span<int> hashCodes = new(hashSet->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (hashCodes[index] == hashCode && values[index].Equals(value))
                {
                    return; //already present
                }

                index = (index + 1) % capacity;
                if (index == startIndex)
                {
                    throw new InvalidOperationException("Hash set is full");
                }
            }

            occupied[index] = true;
            hashCodes[index] = hashCode;
            values[index] = value;
            hashSet->count = newCount;
        }

        /// <summary>
        /// Tries to add the given <paramref name="value"/> to the hash set and returns true if it was added successfully.
        /// </summary>
        public readonly bool TryAdd(T value)
        {
            MemoryAddress.ThrowIfDefault(hashSet);

            int capacity = hashSet->capacity;
            int newCount = hashSet->count + 1;
            if (newCount > capacity)
            {
                Resize();
                capacity = hashSet->capacity;
            }

            int hashCode = SharedFunctions.GetHashCode(value);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(hashSet->occupied.Pointer, capacity);
            Span<T> values = new(hashSet->values.Pointer, capacity);
            Span<int> hashCodes = new(hashSet->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (hashCodes[index] == hashCode && values[index].Equals(value))
                {
                    return false; //already present
                }

                index = (index + 1) % capacity;
                if (index == startIndex)
                {
                    throw new InvalidOperationException("Hash set is full");
                }
            }

            occupied[index] = true;
            hashCodes[index] = hashCode;
            values[index] = value;
            hashSet->count = newCount;
            return true;
        }

        /// <summary>
        /// Removes the given <paramref name="value"/> from the hash set.
        /// </summary>
        public readonly void Remove(T value)
        {
            MemoryAddress.ThrowIfDefault(hashSet);
            ThrowIfMissing(value);

            int hashCode = SharedFunctions.GetHashCode(value);
            int index = hashCode % hashSet->capacity;
            int startIndex = index;
            Span<bool> occupied = new(hashSet->occupied.Pointer, hashSet->capacity);
            Span<T> values = new(hashSet->values.Pointer, hashSet->capacity);
            Span<int> hashCodes = new(hashSet->hashCodes.Pointer, hashSet->capacity);

            while (occupied[index])
            {
                if (hashCodes[index] == hashCode && values[index].Equals(value))
                {
                    occupied[index] = false;
                    values[index] = default;
                    hashCodes[index] = 0;
                    hashSet->values.Clear(index * hashSet->stride, hashSet->stride);
                    hashSet->count--;
                    return;
                }

                index = (index + 1) % hashSet->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Tries to remove the given <paramref name="value"/> from the hash set.
        /// </summary>
        public readonly bool TryRemove(T value)
        {
            MemoryAddress.ThrowIfDefault(hashSet);

            int hashCode = SharedFunctions.GetHashCode(value);
            int index = hashCode % hashSet->capacity;
            int startIndex = index;
            Span<bool> occupied = new(hashSet->occupied.Pointer, hashSet->capacity);
            Span<T> values = new(hashSet->values.Pointer, hashSet->capacity);
            Span<int> hashCodes = new(hashSet->hashCodes.Pointer, hashSet->capacity);

            while (occupied[index])
            {
                if (hashCodes[index] == hashCode && values[index].Equals(value))
                {
                    occupied[index] = false;
                    values[index] = default;
                    hashCodes[index] = 0;
                    hashSet->values.Clear(index * hashSet->stride, hashSet->stride);
                    hashSet->count--;
                    return true;
                }

                index = (index + 1) % hashSet->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears the hash set.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(hashSet);

            hashSet->occupied.Clear(hashSet->capacity);
            hashSet->count = 0;
        }

        /// <summary>
        /// Tries to retrieve an existing value from the hash code of
        /// the given <paramref name="value"/>.
        /// </summary>
        public readonly bool TryGetValue(T value, out T existingValue)
        {
            MemoryAddress.ThrowIfDefault(hashSet);

            int capacity = hashSet->capacity;
            int hashCode = SharedFunctions.GetHashCode(value);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(hashSet->occupied.Pointer, capacity);
            Span<T> values = new(hashSet->values.Pointer, capacity);
            Span<int> hashCodes = new(hashSet->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (hashCodes[index] == hashCode && values[index].Equals(value))
                {
                    existingValue = values[index];
                    return true;
                }

                index = (index + 1) % capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            existingValue = default;
            return false;
        }

        public readonly void CopyTo(T[] array, int arrayIndex)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if ((uint)arrayIndex >= (uint)array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("The number of elements in the hash set is greater than the available space from the index to the end of the destination array");
            }

            int count = Capacity;
            for (int i = 0; i < count; i++)
            {
                if (TryGetValueAtIndex(i, out T value))
                {
                    array[arrayIndex++] = value;
                }
            }
        }

        readonly bool ICollection<T>.Remove(T item)
        {
            MemoryAddress.ThrowIfDefault(hashSet);
            ThrowIfMissing(item);

            return TryRemove(item);
        }

        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is HashSet<T> hashSet && Equals(hashSet);
        }

        /// <inheritdoc/>
        public readonly bool Equals(HashSet<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return hashSet == other.hashSet;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)hashSet).GetHashCode();
        }

        public static bool operator ==(HashSet<T> left, HashSet<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HashSet<T> left, HashSet<T> right)
        {
            return !(left == right);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly HashSet<T> hashSet;
            private readonly int capacity;
            private int index;

            public readonly T Current
            {
                get
                {
                    return hashSet.GetValueAtIndex(index);
                }
            }

            readonly object IEnumerator.Current => Current;

            internal Enumerator(HashSet<T> hashSet)
            {
                this.hashSet = hashSet;
                index = -1;
                capacity = hashSet.Capacity;
            }

            public bool MoveNext()
            {
                while (++index < capacity)
                {
                    if (hashSet.ContainsAtIndex(index))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                index = -1;
            }

            readonly void IDisposable.Dispose()
            {
            }
        }
    }
}