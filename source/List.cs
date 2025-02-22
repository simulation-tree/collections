using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using static Collections.Implementations.List;
using Implementation = Collections.Implementations.List;

namespace Collections
{
    /// <summary>
    /// Native list that can be used in unmanaged code.
    /// </summary>
    public unsafe struct List<T> : IDisposable, IReadOnlyList<T>, IList<T>, IEquatable<List<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Implementation* list;

        /// <summary>
        /// Checks if the list has been disposed.
        /// </summary>
        public readonly bool IsDisposed => list is null;

        /// <summary>
        /// Amount of elements in the list.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                Allocations.ThrowIfNull(list);

                return list->count;
            }
        }

        /// <summary>
        /// Capacity of the list.
        /// </summary>
        public readonly uint Capacity
        {
            get
            {
                Allocations.ThrowIfNull(list);

                return list->capacity;
            }
            set
            {
                Allocations.ThrowIfNull(list);

                SetCapacity(list, value);
            }
        }

        /// <summary>
        /// Native address of this list.
        /// </summary>
        public readonly nint Address => (nint)list;

        /// <summary>
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly ref T this[uint index]
        {
            get
            {
                Allocations.ThrowIfNull(list);
                ThrowIfOutOfRange(list, index);

                return ref list->Items.ReadElement<T>(index);
            }
        }

        readonly T IReadOnlyList<T>.this[int index] => list->Items.ReadElement<T>((uint)index);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly int IReadOnlyCollection<T>.Count => (int)Count;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly int ICollection<T>.Count => (int)Count;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly bool ICollection<T>.IsReadOnly => false;

        readonly T IList<T>.this[int index]
        {
            get
            {
                Allocations.ThrowIfNull(list);
                ThrowIfOutOfRange(list, (uint)index);

                return list->Items.ReadElement<T>((uint)index);
            }
            set
            {
                Allocations.ThrowIfNull(list);
                ThrowIfOutOfRange(list, (uint)index);

                list->Items.WriteElement((uint)index, value);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Items => AsSpan().ToArray();

        /// <summary>
        /// Initializes an existing list from the given <paramref name="pointer"/>.
        /// </summary>
        public List(void* pointer)
        {
            list = (Implementation*)pointer;
        }

        /// <summary>
        /// Creates a new list with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public List(uint initialCapacity = 4)
        {
            list = Allocate<T>(initialCapacity);
        }

        /// <summary>
        /// Creates a new list containing the given <paramref name="span"/>.
        /// </summary>
        public List(USpan<T> span)
        {
            list = Allocate(span);
        }

        /// <summary>
        /// Creates a new list containing elements from the given <paramref name="list"/>.
        /// </summary>
        public List(IEnumerable<T> list)
        {
            this.list = Allocate<T>(4);
            foreach (T item in list)
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
            list = Allocate<T>(4);
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
            Free(ref list);
        }

        /// <summary>
        /// Returns a span containing elements in the list.
        /// </summary>
        public readonly USpan<T> AsSpan()
        {
            Allocations.ThrowIfNull(list);

            return list->Items.AsSpan<T>(0, Count);
        }

        /// <summary>
        /// Returns the remaining span starting from <paramref name="start"/>.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, start);

            return list->Items.AsSpan<T>(start, Count - start);
        }

        /// <summary>
        /// Returns a span of specified <paramref name="length"/> starting from <paramref name="start"/>.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start, uint length)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, start + length);

            return list->Items.AsSpan<T>(start, length);
        }

        /// <summary>
        /// Inserts the given <paramref name="item"/> at the specified <paramref name="index"/>.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is greater than the count.
        /// </para>
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"/>
        public readonly void Insert(uint index, T item)
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch<T>(list);

            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, (uint)sizeof(T) * list->capacity);
            }

            uint remaining = count - index;
            USpan<T> destination = list->items.AsSpan<T>(index + 1, remaining);
            USpan<T> source = list->items.AsSpan<T>(index, remaining);
            source.CopyTo(destination);

            list->items.WriteElement(index, item);
            list->count = count + 1;
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the list.
        /// </summary>
        public readonly void Add(T item)
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch<T>(list);

            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, (uint)sizeof(T) * list->capacity);
            }

            list->items.WriteElement(count, item);
            list->count = count + 1;
        }

        /// <summary>
        /// Adds a <see langword="default"/> value.
        /// </summary>
        public readonly void AddDefault()
        {
            Allocations.ThrowIfNull(list);

            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, (uint)sizeof(T) * list->capacity);
            }

            list->items.WriteElement<T>(count, default);
            list->count = count + 1;
        }

        /// <summary>
        /// Adds a <see langword="default"/> value <paramref name="count"/> amount of times.
        /// </summary>
        public readonly void AddDefault(uint count)
        {
            Allocations.ThrowIfNull(list);

            uint newCount = list->count + count;
            if (newCount >= list->capacity)
            {
                list->capacity = Allocations.GetNextPowerOf2(newCount);
                Allocation.Resize(ref list->items, (uint)sizeof(T) * list->capacity);
            }

            list->items.Clear(list->count * (uint)sizeof(T), count * (uint)sizeof(T));
            list->count = newCount;
        }

        /// <summary>
        /// Adds a range of the specified <paramref name="item"/> to the list.
        /// </summary>
        public readonly void AddRepeat(T item, uint count)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint newCount = list->count + count;
            if (newCount >= list->capacity)
            {
                list->capacity = Allocations.GetNextPowerOf2(newCount);
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            USpan<T> span = list->items.AsSpan<T>(list->count, count);
            span.Fill(item);
            list->count = newCount;
        }

        /// <summary>
        /// Adds the memory from <paramref name="pointer"/> to the list.
        /// </summary>
        public readonly void AddRange(void* pointer, uint count)
        {
            Implementation.AddRange(list, pointer, count);
        }

        /// <summary>
        /// Inserts the given <paramref name="span"/> at the specified <paramref name="index"/>.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is greater than the count.
        /// </para>
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public readonly void InsertRange(uint index, USpan<T> span)
        {
            uint count = Count;
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
        public readonly void AddRange(USpan<T> span)
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch<T>(list);

            uint addLength = span.Length;
            uint newCount = list->count + addLength;
            if (newCount >= list->capacity)
            {
                list->capacity = Allocations.GetNextPowerOf2(newCount);
                Allocation.Resize(ref list->items, list->stride * list->capacity);
            }

            USpan<T> destination = list->items.AsSpan<T>(list->count, addLength);
            span.CopyTo(destination);
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
        public readonly void RemoveAt(uint index)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            uint newCount = list->count - 1;
            uint stride = list->stride;
            while (index < newCount)
            {
                T nextElement = list->Items.ReadElement<T>(index + 1);
                list->Items.WriteElement(index, nextElement);
                index++;
            }

            list->count = newCount;
        }

        /// <summary>
        /// Removes the element at the given <paramref name="index"/>, and
        /// provides access to the <paramref name="removed"/> value.
        /// </summary>
        public readonly void RemoveAt(uint index, out T removed)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            removed = this[index];
            uint newCount = list->count - 1;
            uint stride = list->stride;
            while (index < newCount)
            {
                T nextElement = list->Items.ReadElement<T>(index + 1);
                list->Items.WriteElement(index, nextElement);
                index++;
            }

            list->count = newCount;
        }

        /// <summary>
        /// Removes the element at the given index by swapping it with the last element,
        /// and returns the removed value.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is outside the bounds.
        /// </para>
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>"
        public readonly void RemoveAtBySwapping(uint index)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            uint newCount = list->count - 1;
            T lastElement = list->Items.ReadElement<T>(newCount);
            list->Items.WriteElement(index, lastElement);
            list->count = newCount;
        }

        /// <summary>
        /// Removes the element at the given <paramref name="index"/> by
        /// swapping it with the last element, and provides access to the 
        /// <paramref name="removed"/> value.
        /// </summary>
        public readonly void RemoveAtBySwapping(uint index, out T removed)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            removed = this[index];
            uint newCount = list->count - 1;
            T lastElement = list->Items.ReadElement<T>(newCount);
            list->Items.WriteElement(index, lastElement);
            list->count = newCount;
        }

        /// <summary>
        /// Clears the list so that it's count becomes 0.
        /// </summary>
        public readonly void Clear()
        {
            Allocations.ThrowIfNull(list);

            list->count = 0;
        }

        /// <summary>
        /// Ensures that the list has the given <paramref name="minimumCapacity"/>
        /// and clears it.
        /// </summary>
        public readonly void Clear(uint minimumCapacity)
        {
            uint capacity = Capacity;
            if (capacity < minimumCapacity)
            {
                Implementation.AddDefault(list, minimumCapacity - capacity);
            }

            Implementation.Clear(list);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)list).GetHashCode();
        }

        /// <summary>
        /// Copies the elements of the list to the given <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of elements copied.</returns>
        public readonly uint CopyTo(USpan<T> destination)
        {
            return AsSpan().CopyTo(destination);
        }

        /// <inheritdoc/>
        public readonly Span<T>.Enumerator GetEnumerator()
        {
            //return new(value);
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
            USpan<T> values = AsSpan();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (uint i = 0; i < values.Length; i++)
            {
                if (comparer.Equals(values[i], item))
                {
                    return (int)i;
                }
            }

            return -1;
        }

        readonly void IList<T>.Insert(int index, T item)
        {
            Insert((uint)index, item);
        }

        readonly void IList<T>.RemoveAt(int index)
        {
            RemoveAt((uint)index);
        }

        readonly bool ICollection<T>.Contains(T item)
        {
            USpan<T> values = AsSpan();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (uint i = 0; i < values.Length; i++)
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
            USpan<T> values = AsSpan();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (uint i = 0; i < values.Length; i++)
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
            private readonly Implementation* list;
            private int index;

            public readonly T Current => list->Items.ReadElement<T>((uint)index);
            readonly object IEnumerator.Current => Current;

            public Enumerator(Implementation* list)
            {
                this.list = list;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < list->Count;
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
    }
}