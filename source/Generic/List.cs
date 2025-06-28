using Collections.Pointers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Generic
{
    /// <summary>
    /// Native list that can be used in unmanaged code.
    /// </summary>
    public unsafe struct List<T> : IDisposable, IReadOnlyList<T>, IList<T>, IEquatable<List<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ListPointer* list;

        /// <summary>
        /// Checks if the list has been disposed.
        /// </summary>
        public readonly bool IsDisposed => list is null;

        /// <summary>
        /// Amount of elements in the list.
        /// </summary>
        public readonly int Count
        {
            get
            {
                MemoryAddress.ThrowIfDefault(list);

                return list->count;
            }
        }

        /// <summary>
        /// Capacity of the list.
        /// </summary>
        public readonly int Capacity
        {
            get
            {
                MemoryAddress.ThrowIfDefault(list);

                return list->capacity;
            }
        }

        /// <summary>
        /// Native address of this list.
        /// </summary>
        public readonly nint Address => (nint)list;

        /// <summary>
        /// The underlying pointer for this list.
        /// </summary>
        public readonly ListPointer* Pointer => list;

        /// <summary>
        /// The underlying memory allocation for this list.
        /// </summary>
        public readonly MemoryAddress Items
        {
            get
            {
                MemoryAddress.ThrowIfDefault(list);

                return list->items;
            }
        }

        /// <summary>
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly ref T this[int index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(list);
                ThrowIfOutOfRange(index);

                return ref list->items.ReadElement<T>(index);
            }
        }

        /// <summary>
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly ref T this[uint index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(list);
                ThrowIfOutOfRange(index);

                return ref list->items.ReadElement<T>(index);
            }
        }

        readonly T IReadOnlyList<T>.this[int index] => list->items.ReadElement<T>(index);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly bool ICollection<T>.IsReadOnly => false;

        readonly T IList<T>.this[int index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(list);
                ThrowIfOutOfRange(index);

                return list->items.ReadElement<T>(index);
            }
            set
            {
                MemoryAddress.ThrowIfDefault(list);
                ThrowIfOutOfRange(index);

                list->items.WriteElement(index, value);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Values => AsSpan().ToArray();

        /// <summary>
        /// Initializes an existing list from the given <paramref name="pointer"/>.
        /// </summary>
        public List(void* pointer)
        {
            list = (ListPointer*)pointer;
        }

        /// <summary>
        /// Creates a new list with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public List(int initialCapacity)
        {
            initialCapacity = Math.Max(1, initialCapacity).GetNextPowerOf2();
            list = MemoryAddress.AllocatePointer<ListPointer>();
            list->stride = sizeof(T);
            list->count = 0;
            list->capacity = initialCapacity;
            list->items = MemoryAddress.AllocateZeroed(sizeof(T) * initialCapacity);
        }

        /// <summary>
        /// Creates a new list containing the given <paramref name="span"/>.
        /// </summary>
        public List(ReadOnlySpan<T> span)
        {
            int initialCapacity = Math.Max(1, span.Length).GetNextPowerOf2();
            list = MemoryAddress.AllocatePointer<ListPointer>();
            list->stride = sizeof(T);
            list->count = span.Length;
            list->capacity = initialCapacity;
            list->items = MemoryAddress.AllocateZeroed(sizeof(T) * initialCapacity);
            span.CopyTo(list->items.GetSpan<T>(span.Length));
        }

        /// <summary>
        /// Creates a new list containing elements from the given <paramref name="enumerable"/>.
        /// </summary>
        public List(IEnumerable<T> enumerable)
        {
            list = MemoryAddress.AllocatePointer<ListPointer>();
            list->stride = sizeof(T);
            list->count = 0;
            list->capacity = 4;
            list->items = MemoryAddress.AllocateZeroed(sizeof(T) * 4);
            foreach (T item in enumerable)
            {
                Add(item);
            }
        }

#if NET
        /// <summary>
        /// Creates a new empty list.
        /// </summary>
        public List()
        {
            list = MemoryAddress.AllocatePointer<ListPointer>();
            list->stride = sizeof(T);
            list->count = 0;
            list->capacity = 4;
            list->items = MemoryAddress.AllocateZeroed(sizeof(T) * 4);
        }
#endif

        /// <summary>
        /// Disposes the list and frees its memory.
        /// <para>Elements need to be disposed manually prior to
        /// calling this if they are allocations/disposable themselves.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(list);

            list->items.Dispose();
            MemoryAddress.Free(ref list);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= list->count || index < 0)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} outside of list count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} outside of list count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfLessThanCount(int newCapacity)
        {
            if (newCapacity < list->count)
            {
                throw new InvalidOperationException($"New capacity {newCapacity} cannot be less than the current count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfPastRange(int index)
        {
            if (index > list->count)
            {
                throw new IndexOutOfRangeException($"Index {index} is past the range for array of length {list->count}");
            }
        }

        /// <summary>
        /// Returns a span containing elements in the list.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            MemoryAddress.ThrowIfDefault(list);

            return new(list->items.Pointer, list->count);
        }

        /// <summary>
        /// Returns the remaining span starting from <paramref name="start"/>.
        /// </summary>
        public readonly Span<T> AsSpan(int start)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfPastRange(start);

            return list->items.AsSpan<T>(start * sizeof(T), list->count - start);
        }

        /// <summary>
        /// Returns a span of specified <paramref name="length"/> starting from <paramref name="start"/>.
        /// </summary>
        public readonly Span<T> AsSpan(int start, int length)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfPastRange(start + length);

            return list->items.AsSpan<T>(start * sizeof(T), length);
        }

        /// <summary>
        /// Returns a span of the specified <paramref name="range"/>.
        /// </summary>
        public readonly Span<T> AsSpan(Range range)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfPastRange(range.End.Value);

            return list->items.AsSpan<T>(range.Start.Value * sizeof(T), range.End.Value - range.Start.Value);
        }

        /// <summary>
        /// Inserts the given <paramref name="item"/> at the specified <paramref name="index"/>.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is greater than the count.
        /// </para>
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"/>
        public readonly void Insert(int index, T item)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfPastRange(index);

            int count = list->count;
            if (count == list->capacity)
            {
                MemoryAddress.ResizePowerOf2AndClear(ref list->items, list->capacity * sizeof(T));
                list->capacity *= 2;
            }

            int remaining = count - index;
            int bytePosition = index * sizeof(T);
            Span<T> destination = list->items.AsSpan<T>(bytePosition + sizeof(T), remaining);
            Span<T> source = list->items.AsSpan<T>(bytePosition, remaining);
            source.CopyTo(destination);

            list->items.Write(bytePosition, item);
            list->count = count + 1;
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the end of the list.
        /// </summary>
        public readonly void Add(T item)
        {
            MemoryAddress.ThrowIfDefault(list);

            int count = list->count;
            if (count == list->capacity)
            {
                MemoryAddress.ResizePowerOf2AndClear(ref list->items, list->capacity * sizeof(T));
                list->capacity *= 2;
            }

            list->items.WriteElement(count, item);
            list->count = count + 1;
        }

        /// <summary>
        /// Adds a <see langword="default"/> element to the end of the list,
        /// and returns it by reference.
        /// </summary>
        public readonly ref T Add()
        {
            MemoryAddress.ThrowIfDefault(list);

            int count = list->count;
            if (count == list->capacity)
            {
                MemoryAddress.ResizePowerOf2AndClear(ref list->items, list->capacity * sizeof(T));
                list->capacity *= 2;
            }

            ref T added = ref list->items.ReadElement<T>(count);
            list->count = count + 1;
            return ref added;
        }

        /// <summary>
        /// Adds a <see langword="default"/> value to the end of the list.
        /// </summary>
        public readonly void AddDefault()
        {
            MemoryAddress.ThrowIfDefault(list);

            int count = list->count;
            if (count == list->capacity)
            {
                MemoryAddress.ResizePowerOf2AndClear(ref list->items, list->capacity * sizeof(T));
                list->capacity *= 2;
            }

            list->count = count + 1;
        }

        /// <summary>
        /// Adds a <see langword="default"/> value <paramref name="count"/> amount of times.
        /// </summary>
        public readonly void AddDefault(int count)
        {
            MemoryAddress.ThrowIfDefault(list);

            int newCount = list->count + count;
            if (newCount >= list->capacity)
            {
                int newCapacity = newCount.GetNextPowerOf2();
                MemoryAddress.ResizeAndClear(ref list->items, list->capacity * sizeof(T), newCapacity * sizeof(T));
                list->capacity = newCapacity;
            }

            list->count = newCount;
        }

        /// <summary>
        /// Adds a range of the specified <paramref name="item"/> to the list.
        /// </summary>
        public readonly void AddRepeat(T item, int count)
        {
            MemoryAddress.ThrowIfDefault(list);

            int newCount = list->count + count;
            if (newCount >= list->capacity)
            {
                int newCapacity = newCount.GetNextPowerOf2();
                MemoryAddress.ResizeAndClear(ref list->items, list->capacity * sizeof(T), newCapacity * sizeof(T));
                list->capacity = newCapacity;
            }

            list->items.AsSpan<T>(list->count * sizeof(T), count).Fill(item);
            list->count = newCount;
        }

        /// <summary>
        /// Adds the memory from <paramref name="pointer"/> to the list.
        /// </summary>
        public readonly void AddRange(void* pointer, int count)
        {
            MemoryAddress.ThrowIfDefault(list);

            int newCount = list->count + count;
            if (newCount >= list->capacity)
            {
                int newCapacity = newCount.GetNextPowerOf2();
                MemoryAddress.ResizeAndClear(ref list->items, list->capacity * sizeof(T), newCapacity * sizeof(T));
                list->capacity = newCapacity;
            }

            Span<T> destination = list->items.AsSpan<T>(list->count * sizeof(T), count);
            Span<T> source = new(pointer, count);
            source.CopyTo(destination);
            list->count = newCount;
        }

        /// <summary>
        /// Inserts the given <paramref name="span"/> at the specified <paramref name="index"/>.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is greater than the count.
        /// </para>
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public readonly void InsertRange(int index, ReadOnlySpan<T> span)
        {
            int count = Count;
            if (index > count)
            {
                throw new IndexOutOfRangeException($"Index {index} is greater than the count {count}");
            }

            //todo: efficiency: this deserves its own logic
            if (index == count)
            {
                AddRange(span);
            }
            else
            {
                foreach (T item in span)
                {
                    Insert(index++, item);
                }
            }
        }

        /// <summary>
        /// Adds the given span to the list.
        /// </summary>
        public readonly void AddRange(ReadOnlySpan<T> span)
        {
            MemoryAddress.ThrowIfDefault(list);

            int newCount = list->count + span.Length;
            if (newCount >= list->capacity)
            {
                int newCapacity = newCount.GetNextPowerOf2();
                MemoryAddress.ResizeAndClear(ref list->items, list->capacity * sizeof(T), newCapacity * sizeof(T));
                list->capacity = newCapacity;
            }

            span.CopyTo(list->items.AsSpan<T>(list->count * sizeof(T), span.Length));
            list->count = newCount;
        }

        /// <summary>
        /// Removes the element at the given index and returns the removed value.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is outside the bounds.
        /// </para>
        /// </summary>
        /// <returns>The removed element.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>"
        public readonly void RemoveAt(int index)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            Span<T> values = list->items.GetSpan<T>(list->count);
            int newCount = list->count - 1;
            values.Slice(index + 1).CopyTo(values.Slice(index));
            list->count = newCount;
        }

        /// <summary>
        /// Removes the element at the given <paramref name="index"/>, and
        /// provides access to the <paramref name="removed"/> value.
        /// </summary>
        public readonly void RemoveAt(int index, out T removed)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            Span<T> values = list->items.GetSpan<T>(list->count);
            removed = values[index];
            int newCount = list->count - 1;
            values.Slice(index + 1).CopyTo(values.Slice(index));
            list->count = newCount;
        }

        /// <summary>
        /// Removes the element at the given index by swapping it with the last element,
        /// and returns the removed value.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is outside the bounds.
        /// </para>
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public readonly void RemoveAtBySwapping(int index)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            Span<T> items = list->items.GetSpan<T>(list->count);
            items[index] = items[--list->count];
        }

        /// <summary>
        /// Removes the element at the given <paramref name="index"/> by
        /// swapping it with the last element, and provides access to the 
        /// <paramref name="removed"/> value.
        /// </summary>
        public readonly void RemoveAtBySwapping(int index, out T removed)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            Span<T> items = list->items.GetSpan<T>(list->count);
            ref T reference = ref items[index];
            removed = reference;
            reference = items[--list->count];
        }

        /// <summary>
        /// Clears the list so that it's count becomes 0.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(list);

            list->count = 0;
        }

        /// <summary>
        /// Ensures that the list has the given <paramref name="minimumCapacity"/>
        /// and clears it.
        /// </summary>
        public readonly void Clear(int minimumCapacity)
        {
            int capacity = list->capacity;
            if (capacity < minimumCapacity)
            {
                int toAdd = minimumCapacity - capacity;
                int newCount = list->count + toAdd;
                if (newCount >= list->capacity)
                {
                    int newCapacity = newCount.GetNextPowerOf2();
                    MemoryAddress.ResizeAndClear(ref list->items, list->capacity * sizeof(T), newCapacity * sizeof(T));
                    list->capacity = newCapacity;
                }
            }

            list->count = 0;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)list).GetHashCode();
        }

        /// <summary>
        /// Copies the elements of the list to the given <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Span<T> destination)
        {
            AsSpan().CopyTo(destination);
        }

        /// <summary>
        /// Copies the elements from <paramref name="source"/> into this list,
        /// and updates count to match.
        /// </summary>
        public readonly void CopyFrom(ReadOnlySpan<T> source)
        {
            MemoryAddress.ThrowIfDefault(list);

            int newCount = source.Length;
            if (newCount >= list->capacity)
            {
                int newCapacity = newCount.GetNextPowerOf2();
                MemoryAddress.ResizeAndClear(ref list->items, list->capacity * sizeof(T), newCapacity * sizeof(T));
                list->capacity = newCapacity;
            }

            source.CopyTo(list->items.GetSpan<T>(newCount));
            list->count = newCount;
        }

        /// <inheritdoc/>
        public readonly Span<T>.Enumerator GetEnumerator()
        {
            return AsSpan().GetEnumerator();
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(list);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(list);
        }

        /// <inheritdoc/>
        public readonly bool Equals(List<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return list == other.list;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is List<T> list && Equals(list);
        }

        readonly int IList<T>.IndexOf(T item)
        {
            Span<T> values = AsSpan();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < values.Length; i++)
            {
                if (comparer.Equals(values[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        readonly bool ICollection<T>.Contains(T item)
        {
            Span<T> values = AsSpan();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < values.Length; i++)
            {
                if (comparer.Equals(values[i], item))
                {
                    return true;
                }
            }

            return false;
        }

        readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }

        readonly bool ICollection<T>.Remove(T item)
        {
            Span<T> values = AsSpan();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < values.Length; i++)
            {
                if (comparer.Equals(values[i], item))
                {
                    RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly ListPointer* list;
            private int index;

            public readonly T Current => list->items.ReadElement<T>(index);

            readonly object IEnumerator.Current => Current;

            public Enumerator(ListPointer* list)
            {
                this.list = list;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < list->count;
            }

            public void Reset()
            {
                index = -1;
            }

            readonly void IDisposable.Dispose()
            {
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(List<T> left, List<T> right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(List<T> left, List<T> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator List(List<T> list)
        {
            return new(list.list);
        }
    }
}