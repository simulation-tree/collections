using System;
using System.Collections;
using System.Collections.Generic;
using Unmanaged;
using Implementation = Collections.Implementations.List;

namespace Collections
{
    /// <summary>
    /// Native list that can be used in unmanaged code.
    /// </summary>
    public unsafe struct List<T> : IDisposable, IReadOnlyList<T>, IList<T>, IEquatable<List<T>> where T : unmanaged
    {
        private Implementation* value;

        /// <summary>
        /// Checks if the list has been disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// Amount of elements in the list.
        /// </summary>
        public readonly uint Count => Implementation.GetCountRef(value);

        /// <summary>
        /// Capacity of the list.
        /// </summary>
        public readonly uint Capacity => Implementation.GetCapacity(value);

        /// <summary>
        /// Native address where the memory for elements begin.
        /// </summary>
        public readonly nint StartAddress => Implementation.GetStartAddress(value);

        /// <summary>
        /// Native address of this list.
        /// </summary>
        public readonly nint Address => (nint)value;

        /// <summary>
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly ref T this[uint index] => ref Implementation.GetRef<T>(value, index);

        readonly T IReadOnlyList<T>.this[int index] => Implementation.GetRef<T>(value, (uint)index);
        readonly int IReadOnlyCollection<T>.Count => (int)Count;
        readonly int ICollection<T>.Count => (int)Count;
        readonly bool ICollection<T>.IsReadOnly => false;
        readonly T IList<T>.this[int index]
        {
            get => Implementation.GetRef<T>(value, (uint)index);
            set => Implementation.GetRef<T>(this.value, (uint)index) = value;
        }

        /// <summary>
        /// Initializes an existing list from the given <paramref name="pointer"/>.
        /// </summary>
        public List(void* pointer)
        {
            value = (Implementation*)pointer;
        }

        /// <summary>
        /// Creates a new list with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public List(uint initialCapacity = 4)
        {
            value = Implementation.Allocate<T>(initialCapacity);
        }

        /// <summary>
        /// Creates a new list containing the given <paramref name="span"/>.
        /// </summary>
        public List(USpan<T> span)
        {
            value = Implementation.Allocate(span);
        }

        /// <summary>
        /// Creates a new list containing elements from the given <paramref name="list"/>.
        /// </summary>
        public List(IEnumerable<T> list)
        {
            value = Implementation.Allocate<T>(4);
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
            value = Implementation.Allocate<T>(4);
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
            Implementation.Free(ref value);
        }

        /// <summary>
        /// Returns a span containing elements in the list.
        /// </summary>
        public readonly USpan<T> AsSpan()
        {
            return Implementation.AsSpan<T>(value);
        }

        /// <summary>
        /// Returns the list as a span of a different type <typeparamref name="V"/>.
        /// </summary>
        public readonly USpan<V> AsSpan<V>() where V : unmanaged
        {
            return Implementation.AsSpan<V>(value);
        }

        /// <summary>
        /// Returns the remaining span starting from <paramref name="start"/>.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start)
        {
            return Implementation.AsSpan<T>(value, start);
        }

        /// <summary>
        /// Returns a span of specified <paramref name="length"/> starting from <paramref name="start"/>.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start, uint length)
        {
            return Implementation.AsSpan<T>(value, start, length);
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
            Implementation.Insert(value, index, item);
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the list.
        /// </summary>
        public readonly void Add(T item)
        {
            Implementation.Add(value, item);
        }

        /// <summary>
        /// Attempts to add the given item if its unique.
        /// </summary>
        /// <returns><c>true</c> if item was added.</returns>
        public readonly bool TryAdd<V>(V item) where V : unmanaged, IEquatable<V>
        {
            USpan<V> span = Implementation.AsSpan<V>(value);
            if (span.Contains(item))
            {
                return false;
            }

            Implementation.Add(value, item);
            return true;
        }

        /// <summary>
        /// Adds a range of default values to the list.
        /// </summary>
        public readonly void AddDefault(uint count = 1)
        {
            Implementation.AddDefault(value, count);
        }

        /// <summary>
        /// Adds a range of the specified <paramref name="item"/> to the list.
        /// </summary>
        public readonly void AddRepeat(T item, uint count)
        {
            uint start = Count;
            AddDefault(count);
            USpan<T> span = AsSpan(start);
            for (uint i = 0; i < count; i++)
            {
                span[i] = item;
            }
        }

        /// <summary>
        /// Adds the memory from <paramref name="pointer"/> to the list.
        /// </summary>
        public readonly void AddRange(void* pointer, uint count)
        {
            Implementation.AddRange(value, pointer, count);
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

            uint length = span.Length;
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
            Implementation.AddRange(value, span);
        }

        /// <summary>
        /// Adds the given <paramref name="span"/> of <typeparamref name="V"/> into
        /// the list, assuming its size equals to <typeparamref name="T"/>.
        /// </summary>
        public readonly void AddRange<V>(USpan<V> span) where V : unmanaged
        {
            Implementation.AddRange(value, span);
        }

        /// <summary>
        /// Adds the given <paramref name="list"/> to the list.
        /// </summary>
        public readonly void AddRange(List<T> list)
        {
            nint address = Implementation.GetStartAddress(list.value);
            Implementation.AddRange(value, (void*)address, list.Count);
        }

        /// <summary>
        /// Returns the index of the given <paramref name="item"/> in the list.
        /// <para>
        /// May throw <see cref="NullReferenceException"/> if the item is not found.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public readonly uint IndexOf<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return Implementation.IndexOf(value, item);
        }

        /// <summary>
        /// Attempts to find the index of the given <paramref name="item"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryIndexOf<V>(V item, out uint index) where V : unmanaged, IEquatable<V>
        {
            return Implementation.TryIndexOf(value, item, out index);
        }

        /// <summary>
        /// Checks whether the list contains the given <paramref name="item"/>.
        /// </summary>
        public readonly bool Contains<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return Implementation.Contains(value, item);
        }

        /// <summary>
        /// Attempts to remove the given <paramref name="item"/> from the list
        /// by swapping it with the removed element.
        /// </summary>
        /// <returns><c>true</c> if the item was removed.</returns>
        public readonly bool TryRemoveBySwapping<V>(V item) where V : unmanaged, IEquatable<V>
        {
            if (TryIndexOf(item, out uint index))
            {
                RemoveAtBySwapping(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Removes the element at the given index.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is outside the bounds.
        /// </para>
        /// </summary>
        /// <returns>The removed element.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>"
        public readonly T RemoveAt(uint index)
        {
            T removed = this[index];
            Implementation.RemoveAt(value, index);
            return removed;
        }

        /// <summary>
        /// Removes the element at the given index by swapping it with the last element.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is outside the bounds.
        /// </para>
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>"
        public readonly void RemoveAtBySwapping(uint index)
        {
            Implementation.RemoveAtBySwapping(value, index);
        }

        /// <summary>
        /// Removes the element at the given index.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is outside the bounds.
        /// </para>
        /// </summary>
        /// <returns>The removed element.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>"
        public readonly V RemoveAt<V>(uint index) where V : unmanaged, IEquatable<V>
        {
            return Implementation.RemoveAt<V>(value, index);
        }

        /// <summary>
        /// Removes the element at the given index by swapping it with the last element.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/> if the index is outside the bounds.
        /// </para>
        /// </summary>
        /// <returns>The removed element.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>"
        public readonly V RemoveAtBySwapping<V>(uint index) where V : unmanaged, IEquatable<V>
        {
            return Implementation.RemoveAtBySwapping<V>(value, index);
        }

        /// <summary>
        /// Clears the list so that it's count becomes 0.
        /// </summary>
        public readonly void Clear()
        {
            Implementation.Clear(value);
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
                Implementation.AddDefault(value, minimumCapacity - capacity);
            }

            Implementation.Clear(value);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
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
        public readonly Enumerator GetEnumerator()
        {
            return new(value);
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public readonly bool Equals(List<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return value == other.value;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is List<T> list && Equals(list);
        }

        readonly int IList<T>.IndexOf(T item)
        {
            USpan<T> values = AsSpan();
            if (values.TryIndexOfSlow(item, out uint index))
            {
                return (int)index;
            }
            else
            {
                return -1;
            }
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
            return values.ContainsSlow(item);
        }

        readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }

        readonly bool ICollection<T>.Remove(T item)
        {
            USpan<T> values = AsSpan();
            if (values.TryIndexOfSlow(item, out uint index))
            {
                RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly Implementation* list;
            private int index;

            public readonly T Current => Implementation.GetRef<T>(list, (uint)index);
            readonly object IEnumerator.Current => Current;

            public Enumerator(Implementation* list)
            {
                this.list = list;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < Implementation.GetCountRef(list);
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