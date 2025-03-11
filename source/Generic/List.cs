using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using Pointer = Collections.Pointers.List;

namespace Collections.Generic
{
    /// <summary>
    /// Native list that can be used in unmanaged code.
    /// </summary>
    public unsafe struct List<T> : IDisposable, IReadOnlyList<T>, IList<T>, IEquatable<List<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Pointer* list;

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
            set
            {
                MemoryAddress.ThrowIfDefault(list);

                int newCapacity = value.GetNextPowerOf2();
                ThrowIfLessThanCount(newCapacity);

                MemoryAddress newItems = MemoryAddress.Allocate(sizeof(T) * newCapacity);
                list->items.CopyTo(newItems, sizeof(T) * list->count);
                list->items.Dispose();
                list->items = newItems;
                list->capacity = newCapacity;
            }
        }

        /// <summary>
        /// Native address of this list.
        /// </summary>
        public readonly nint Address => (nint)list;

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
            list = (Pointer*)pointer;
        }

        /// <summary>
        /// Creates a new list with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public List(int initialCapacity = 4)
        {
            initialCapacity = Math.Max(1, initialCapacity).GetNextPowerOf2();
            ref Pointer list = ref MemoryAddress.Allocate<Pointer>();
            list = new(sizeof(T), 0, initialCapacity, MemoryAddress.Allocate(sizeof(T) * initialCapacity));
            fixed (Pointer* pointer = &list)
            {
                this.list = pointer;
            }
        }

        /// <summary>
        /// Creates a new list containing the given <paramref name="span"/>.
        /// </summary>
        public List(ReadOnlySpan<T> span)
        {
            int initialCapacity = Math.Max(1, span.Length).GetNextPowerOf2();
            ref Pointer list = ref MemoryAddress.Allocate<Pointer>();
            list = new(sizeof(T), span.Length, initialCapacity, MemoryAddress.Allocate(sizeof(T) * initialCapacity));
            Span<T> destination = list.items.GetSpan<T>(span.Length);
            span.CopyTo(destination);
            fixed (Pointer* pointer = &list)
            {
                this.list = pointer;
            }
        }

        /// <summary>
        /// Creates a new list containing elements from the given <paramref name="enumerable"/>.
        /// </summary>
        public List(IEnumerable<T> enumerable)
        {
            ref Pointer list = ref MemoryAddress.Allocate<Pointer>();
            list = new(sizeof(T), 0, 4, MemoryAddress.Allocate(sizeof(T) * 4));
            fixed (Pointer* pointer = &list)
            {
                this.list = pointer;
            }

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
            ref Pointer list = ref MemoryAddress.Allocate<Pointer>();
            list = new(sizeof(T), 0, 4, MemoryAddress.Allocate(sizeof(T) * 4));
            fixed (Pointer* pointer = &list)
            {
                this.list = pointer;
            }
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
            ThrowIfOutOfRange(start);

            return list->items.AsSpan<T>(start, list->count - start);
        }

        /// <summary>
        /// Returns a span of specified <paramref name="length"/> starting from <paramref name="start"/>.
        /// </summary>
        public readonly Span<T> AsSpan(int start, int length)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(start + length);

            return list->items.AsSpan<T>(start, length);
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
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, sizeof(T) * list->capacity);
            }

            int remaining = count - index;
            Span<T> destination = list->items.AsSpan<T>(index + 1, remaining);
            Span<T> source = list->items.AsSpan<T>(index, remaining);
            source.CopyTo(destination);

            list->items.WriteElement(index, item);
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
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, sizeof(T) * list->capacity);
            }

            list->items.WriteElement(count, item);
            list->count = count + 1;
        }

        /// <summary>
        /// Adds a <see langword="default"/> value.
        /// </summary>
        public readonly void AddDefault()
        {
            MemoryAddress.ThrowIfDefault(list);

            int count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, sizeof(T) * list->capacity);
            }

            list->items.WriteElement<T>(count, default);
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
                list->capacity = newCount.GetNextPowerOf2();
                MemoryAddress.Resize(ref list->items, sizeof(T) * list->capacity);
            }

            list->items.Clear(list->count * sizeof(T), count * sizeof(T));
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
                list->capacity = newCount.GetNextPowerOf2();
                MemoryAddress.Resize(ref list->items, sizeof(T) * list->capacity);
            }

            Span<T> span = list->items.AsSpan<T>(list->count, count);
            span.Fill(item);
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
                list->capacity = newCount.GetNextPowerOf2();
                MemoryAddress.Resize(ref list->items, sizeof(T) * list->capacity);
            }

            Span<T> destination = list->items.AsSpan<T>(list->count, count);
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

            int addLength = span.Length;
            int newCount = list->count + addLength;
            if (newCount >= list->capacity)
            {
                list->capacity = newCount.GetNextPowerOf2();
                MemoryAddress.Resize(ref list->items, sizeof(T) * list->capacity);
            }

            Span<T> destination = list->items.AsSpan<T>(list->count, addLength);
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
        public readonly void RemoveAt(int index)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            int newCount = list->count - 1;
            while (index < newCount)
            {
                T nextElement = list->items.ReadElement<T>(index + 1);
                list->items.WriteElement(index, nextElement);
                index++;
            }

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

            removed = list->items.ReadElement<T>(index);
            int newCount = list->count - 1;
            while (index < newCount)
            {
                T nextElement = list->items.ReadElement<T>(index + 1);
                list->items.WriteElement(index, nextElement);
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
        public readonly void RemoveAtBySwapping(int index)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            T lastElement = list->items.ReadElement<T>(--list->count);
            list->items.WriteElement(index, lastElement);
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

            removed = list->items.ReadElement<T>(index);
            T lastElement = list->items.ReadElement<T>(--list->count);
            list->items.WriteElement(index, lastElement);
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
                    list->capacity = newCount.GetNextPowerOf2();
                    MemoryAddress.Resize(ref list->items, sizeof(T) * list->capacity);
                }

                list->items.Clear(list->count * sizeof(T), toAdd * sizeof(T));
                list->count = newCount;
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
            private readonly Pointer* list;
            private int index;

            public readonly T Current => list->items.ReadElement<T>(index);

            readonly object IEnumerator.Current => Current;

            public Enumerator(Pointer* list)
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