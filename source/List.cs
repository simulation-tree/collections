using Collections.Unsafe;
using System;
using System.Collections;
using System.Collections.Generic;
using Unmanaged;

namespace Collections
{
    public unsafe struct List<T> : IDisposable, IReadOnlyList<T>, IList<T>, IEquatable<List<T>> where T : unmanaged
    {
        private UnsafeList* value;

        public readonly bool IsDisposed => UnsafeList.IsDisposed(value);

        /// <summary>
        /// Amount of elements in the list.
        /// </summary>
        public readonly uint Count => UnsafeList.GetCountRef(value);

        public readonly uint Capacity
        {
            get => UnsafeList.GetCapacity(value);
            set => UnsafeList.SetCapacity(this.value, value);
        }

        public readonly nint StartAddress => UnsafeList.GetStartAddress(value);
        public readonly ref T this[uint index] => ref UnsafeList.GetRef<T>(value, index);

        readonly T IReadOnlyList<T>.this[int index] => UnsafeList.Get<T>(value, (uint)index);
        readonly int IReadOnlyCollection<T>.Count => (int)Count;
        readonly int ICollection<T>.Count => (int)Count;
        readonly bool ICollection<T>.IsReadOnly => false;
        readonly T IList<T>.this[int index]
        {
            get => UnsafeList.Get<T>(value, (uint)index);
            set => UnsafeList.Set(this.value, (uint)index, value);
        }

        public List(UnsafeList* list)
        {
            value = list;
        }

        /// <summary>
        /// Creates a new list with the given initial capacity.
        /// </summary>
        public List(uint initialCapacity = 1)
        {
            value = UnsafeList.Allocate<T>(initialCapacity);
        }

        public List(USpan<T> span)
        {
            value = UnsafeList.Allocate(span);
        }

        public List(IEnumerable<T> list)
        {
            value = UnsafeList.Allocate<T>();
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
            this = Create();
        }
#endif

        public void Dispose()
        {
            UnsafeList.Free(ref value);
        }

        /// <summary>
        /// Returns a span containing elements in the list.
        /// </summary>
        public readonly USpan<T> AsSpan()
        {
            return UnsafeList.AsSpan<T>(value);
        }

        /// <summary>
        /// Returns the list as a span of a different type <typeparamref name="V"/>.
        /// </summary>
        public readonly USpan<V> AsSpan<V>() where V : unmanaged
        {
            return UnsafeList.AsSpan<V>(value);
        }

        /// <summary>
        /// Returns the remaining span starting from the given index.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start)
        {
            return UnsafeList.AsSpan<T>(value, start);
        }

        public readonly USpan<T> AsSpan(uint start, uint length)
        {
            return UnsafeList.AsSpan<T>(value, start, length);
        }

        /// <summary>
        /// Inserts the given item at the specified index by shifting 
        /// succeeding elements over.
        /// </summary>
        public readonly void Insert(uint index, T item)
        {
            UnsafeList.Insert(value, index, item);
        }

        public readonly void Add(T item)
        {
            UnsafeList.Add(value, item);
        }

        /// <summary>
        /// Attempts to add the given item if its unique.
        /// </summary>
        /// <returns><c>true</c> if item was added.</returns>
        public readonly bool TryAdd<V>(V item) where V : unmanaged, IEquatable<V>
        {
            USpan<V> span = UnsafeList.AsSpan<V>(value);
            if (span.Contains(item))
            {
                return false;
            }

            UnsafeList.Add(value, item);
            return true;
        }

        /// <summary>
        /// Adds a range of default values to the list.
        /// </summary>
        public readonly void AddDefault(uint count = 1)
        {
            UnsafeList.AddDefault(value, count);
        }

        /// <summary>
        /// Adds a range of the specified default value to the list.
        /// </summary>
        public readonly void AddRepeat(T defaultValue, uint count)
        {
            uint start = Count;
            AddDefault(count);
            USpan<T> span = AsSpan(start);
            for (uint i = 0; i < count; i++)
            {
                span[i] = defaultValue;
            }
        }

        public readonly void AddRange(void* pointer, uint count)
        {
            UnsafeList.AddRange(value, pointer, count);
        }

        public readonly void InsertRange(uint index, USpan<T> items)
        {
            uint count = Count;
            if (index > count)
            {
                throw new IndexOutOfRangeException();
            }

            uint length = items.Length;
            if (index == count)
            {
                AddRange(items);
            }
            else
            {
                Capacity = count + length;
                foreach (T item in items)
                {
                    Insert(index++, item);
                }
            }
        }

        /// <summary>
        /// Adds the given span to the list.
        /// </summary>
        public readonly void AddRange(USpan<T> items)
        {
            UnsafeList.AddRange(value, items);
        }

        /// <summary>
        /// Adds the given span of <typeparamref name="V"/> into
        /// the list, assuming its size equals to <typeparamref name="T"/>.
        /// </summary>
        public readonly void AddRange<V>(USpan<V> items) where V : unmanaged
        {
            UnsafeList.AddRange(value, items);
        }

        public readonly void AddRange(List<T> list)
        {
            nint address = UnsafeList.GetStartAddress(list.value);
            UnsafeList.AddRange(value, (void*)address, list.Count);
        }

        /// <summary>
        /// Returns the index of the given item in the list, otherwise
        /// throws an <see cref="Exception"/> if none found.
        /// </summary>
        public readonly uint IndexOf<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.IndexOf(value, item);
        }

        public readonly bool TryIndexOf<V>(V item, out uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.TryIndexOf(value, item, out index);
        }

        /// <summary>
        /// Checks whether the list contains the given item.
        /// </summary>
        public readonly bool Contains<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.Contains(value, item);
        }

        /// <summary>
        /// Removes the given item from the list by swapping it with the removed element.
        /// </summary>
        public readonly bool TryRemove<V>(V item) where V : unmanaged, IEquatable<V>
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
        /// </summary>
        /// <returns>The removed element.</returns>
        public readonly T RemoveAt(uint index)
        {
            T removed = this[index];
            UnsafeList.RemoveAt(value, index);
            return removed;
        }

        public readonly void RemoveAtBySwapping(uint index)
        {
            UnsafeList.RemoveAtBySwapping(value, index);
        }

        public readonly V RemoveAt<V>(uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.RemoveAt<V>(value, index);
        }

        public readonly V RemoveAtBySwapping<V>(uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.RemoveAtBySwapping<V>(value, index);
        }

        /// <summary>
        /// Clears the list so that it's count becomes 0.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeList.Clear(value);
        }

        /// <summary>
        /// Clears the list all elements and ensures that the capacity at least
        /// the given amount.
        /// </summary>
        public readonly void Clear(uint minimumCapacity)
        {
            uint capacity = Capacity;
            if (capacity < minimumCapacity)
            {
                UnsafeList.AddDefault(value, minimumCapacity - capacity);
            }

            UnsafeList.Clear(value);
        }

        public readonly override int GetHashCode()
        {
            nint ptr = (nint)value;
            return HashCode.Combine(ptr, 7);
        }

        public readonly void CopyTo(USpan<T> destination)
        {
            AsSpan().CopyTo(destination);
        }

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

        public readonly bool Equals(List<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return value == other.value;
        }

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

        public static List<T> Create(uint initialCapacity = 1)
        {
            return new(initialCapacity);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly UnsafeList* list;
            private int index;

            public readonly T Current => UnsafeList.Get<T>(list, (uint)index);
            readonly object IEnumerator.Current => Current;

            public Enumerator(UnsafeList* list)
            {
                this.list = list;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < UnsafeList.GetCountRef(list);
            }

            public void Reset()
            {
                index = -1;
            }

            readonly void IDisposable.Dispose()
            {
            }
        }

        public static bool operator ==(List<T> left, List<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(List<T> left, List<T> right)
        {
            return !left.Equals(right);
        }
    }
}
