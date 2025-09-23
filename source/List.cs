using Collections.Pointers;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Collections
{
    public unsafe struct List : IDisposable
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(list);

                return list->count;
            }
        }

        /// <summary>
        /// Size of each element in the list.
        /// </summary>
        public readonly int Stride
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(list);

                return list->stride;
            }
        }

        /// <summary>
        /// Underlying allocation of the list containing all elements.
        /// </summary>
        public readonly MemoryAddress Items
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(list);

                return list->items;
            }
        }

        /// <summary>
        /// The underlying pointer of the list.
        /// </summary>
        public readonly ListPointer* Pointer => list;

        /// <summary>
        /// Capacity of the list.
        /// </summary>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                MemoryAddress newItems = MemoryAddress.Allocate(list->stride * newCapacity);
                list->items.CopyTo(newItems, list->stride * list->count);
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
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly MemoryAddress this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(list);
                ThrowIfOutOfRange(index);

                return new(list->items.pointer + list->stride * index);
            }
        }

        /// <summary>
        /// Initializes an existing list from the given <paramref name="pointer"/>
        /// </summary>
        public List(ListPointer* pointer)
        {
            list = pointer;
        }

        /// <summary>
        /// Creates a new list with the given <paramref name="initialCapacity"/> and <paramref name="stride"/>
        /// for each element.
        /// </summary>
        public List(int initialCapacity, int stride)
        {
            initialCapacity = Math.Max(1, initialCapacity).GetNextPowerOf2();
            list = MemoryAddress.AllocatePointer<ListPointer>();
            list->stride = stride;
            list->count = 0;
            list->capacity = initialCapacity;
            list->items = MemoryAddress.Allocate(stride * initialCapacity);
        }

#if NET
        [Obsolete("Default constructor not supported", true)]
        public List() { }
#endif

        /// <summary>
        /// Disposes the array and frees its memory.
        /// </summary>
        /// <para>Elements need to be disposed manually prior to
        /// calling this if they are allocations/disposable themselves.
        /// </para>
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
                throw new IndexOutOfRangeException($"Index {index} is out of range for array of length {list->count}");
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

        [Conditional("DEBUG")]
        private readonly void ThrowIfLessThanCount(int newCapacity)
        {
            if (newCapacity < list->count)
            {
                throw new InvalidOperationException($"New capacity {newCapacity} cannot be less than the current count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeMismatch<T>() where T : unmanaged
        {
            if (list->stride != sizeof(T))
            {
                throw new InvalidOperationException($"Size mismatch. Expected stride of {list->stride} but got {sizeof(T)}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeMismatch(int stride)
        {
            if (list->stride != stride)
            {
                throw new InvalidOperationException($"Size mismatch. Expected stride of {list->stride} but got {stride}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfGreaterThanStride(int length)
        {
            if (length > list->stride)
            {
                throw new InvalidOperationException($"Length {length} is greater than the stride {list->stride}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfLessThanStride(int length)
        {
            if (length < list->stride)
            {
                throw new InvalidOperationException($"Length {length} is less than the stride {list->stride}");
            }
        }

        /// <summary>
        /// Retrieves a span of the elements in the list.
        /// </summary>
        public readonly Span<T> AsSpan<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();

            return new(list->items.pointer, list->count);
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the end of the list.
        /// </summary>
        public readonly void Add<T>(T item) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();

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
        /// Adds the memory from the given <paramref name="item"/>.
        /// </summary>
        public readonly void Add(MemoryAddress item)
        {
            MemoryAddress.ThrowIfDefault(list);

            int count = list->count;
            int stride = list->stride;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, stride * list->capacity);
            }

            item.CopyTo(list->items.pointer + count * stride, stride);
            list->count = count + 1;
        }

        /// <summary>
        /// Adds a <see langword="default"/> value.
        /// </summary>
        public readonly void AddDefault()
        {
            MemoryAddress.ThrowIfDefault(list);

            int count = list->count;
            int stride = list->stride;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, stride * list->capacity);
            }

            list->items.Clear(list->count * stride, stride);
            list->count = count + 1;
        }

        /// <summary>
        /// Adds a <see langword="default"/> value and retrieves its <see cref="MemoryAddress"/>.
        /// </summary>
        public readonly void AddDefault(out MemoryAddress newElement)
        {
            MemoryAddress.ThrowIfDefault(list);

            int newCount = list->count + 1;
            int stride = list->stride;
            if (newCount > list->capacity)
            {
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, stride * list->capacity);
            }

            int bytePosition = list->count * stride;
            list->items.Clear(bytePosition, stride);
            list->count = newCount;
            newElement = new(list->items.pointer + bytePosition);
        }

        /// <summary>
        /// Adds a new uninitialized value and retrieves its <see cref="MemoryAddress"/>.
        /// </summary>
        public readonly void AddUninitialized(out MemoryAddress newElement)
        {
            MemoryAddress.ThrowIfDefault(list);

            int newCount = list->count + 1;
            int stride = list->stride;
            if (newCount > list->capacity)
            {
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, stride * list->capacity);
            }

            int bytePosition = list->count * stride;
            list->count = newCount;
            newElement = new(list->items.pointer + bytePosition);
        }

        /// <summary>
        /// Adds a range of <see langword="default"/> value.
        /// </summary>
        public readonly void AddDefault(int count)
        {
            MemoryAddress.ThrowIfDefault(list);

            int newCount = list->count + count;
            int stride = list->stride;
            if (newCount >= list->capacity)
            {
                list->capacity = newCount.GetNextPowerOf2();
                MemoryAddress.Resize(ref list->items, stride * list->capacity);
            }

            list->items.Clear(list->count * stride, stride * count);
            list->count = newCount;
        }

        /// <summary>
        /// Adds a new element from the given <paramref name="bytes"/>.
        /// </summary>
        public readonly void Add(ReadOnlySpan<byte> bytes)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfGreaterThanStride(bytes.Length);

            int count = list->count;
            int stride = list->stride;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, stride * list->capacity);
            }

            list->items.Write(count * stride, bytes);
            list->count = count + 1;
        }

        /// <summary>
        /// Inserts the given <paramref name="item"/> at the specified <paramref name="index"/>.
        /// </summary>
        public readonly void Insert<T>(int index, T item) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();
            ThrowIfPastRange(index);

            int count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, sizeof(T) * list->capacity);
            }

            int remaining = count - index;
            int bytePosition = index * sizeof(T);
            Span<T> destination = list->items.AsSpan<T>(bytePosition + sizeof(T), remaining);
            Span<T> source = list->items.AsSpan<T>(bytePosition, remaining);
            source.CopyTo(destination);

            list->items.Write(bytePosition, item);
            list->count = count + 1;
        }

        public readonly void Insert(int index, MemoryAddress item)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfPastRange(index);

            int count = list->count;
            int stride = list->stride;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                MemoryAddress.Resize(ref list->items, stride * list->capacity);
            }

            int remaining = count - index;
            Span<byte> destination = list->items.AsSpan((index + 1) * stride, remaining * stride);
            Span<byte> source = list->items.AsSpan(index * stride, remaining * stride);
            source.CopyTo(destination);
            item.CopyTo(list->items.pointer + index * stride, stride);
            list->count = count + 1;
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(list);

            list->count = 0;
        }

        /// <summary>
        /// Removes the element at the given <paramref name="index"/>.
        /// </summary>
        public readonly void RemoveAt(int index)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            int stride = list->stride;
            int elementsToMove = list->count - index - 1;
            if (elementsToMove > 0)
            {
                int sourceOffset = (index + 1) * stride;
                int destOffset = index * stride;
                int bytesToMove = elementsToMove * stride;
                Span<byte> source = list->items.AsSpan(sourceOffset, bytesToMove);
                Span<byte> destination = list->items.AsSpan(destOffset, bytesToMove);
                source.CopyTo(destination);
            }

            list->count--;
        }

        /// <summary>
        /// Removes the element at <paramref name="index"/> by swapping it with the last element.
        /// </summary>
        public readonly void RemoveAtBySwapping(int index)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            int stride = list->stride;
            int newCount = list->count - 1;
            new Span<byte>(list->items.pointer + stride * newCount, stride).CopyTo(new(list->items.pointer + stride * index, stride));
            list->count = newCount;
        }

        /// <summary>
        /// Removes and retrieves the bytes of the the removed element at <paramref name="index"/>.
        /// Done by swapping it with the last element.
        /// <para>
        /// Length of <paramref name="removed"/> is expected to be the same as the stride of the list.
        /// </para>
        /// </summary>
        public readonly void RemoveAtBySwapping(int index, Span<byte> removed)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);
            ThrowIfLessThanStride(removed.Length);

            int stride = list->stride;
            int newCount = list->count - 1;
            Span<byte> destination = new(list->items.pointer + stride * index, stride);
            destination.CopyTo(removed);
            new Span<byte>(list->items.pointer + stride * newCount, stride).CopyTo(destination);
            list->count = newCount;
        }

        /// <summary>
        /// Removes the element at <paramref name="index"/> by swapping with the last element,
        /// and adds it to the end of the <paramref name="destination"/> list.
        /// </summary>
        public readonly void RemoveAtBySwappingAndAdd(int index, List destination)
        {
            MemoryAddress.ThrowIfDefault(list);
            MemoryAddress.ThrowIfDefault(destination.list);
            ThrowIfOutOfRange(index);
            ThrowIfSizeMismatch(destination.list->stride);

            int stride = list->stride;
            int newSourceCount = list->count - 1;
            int destinationCount = destination.list->count;
            if (destinationCount == destination.list->capacity)
            {
                destination.list->capacity *= 2;
                MemoryAddress.Resize(ref destination.list->items, stride * destination.list->capacity);
            }

            Span<byte> removed = new(list->items.pointer + stride * index, stride);
            destination.list->items.Write(destinationCount * stride, removed);
            new Span<byte>(list->items.pointer + stride * newSourceCount, stride).CopyTo(removed);
            list->count = newSourceCount;
            destination.list->count = destinationCount + 1;
        }

        /// <summary>
        /// Removes the element at <paramref name="index"/> by swapping with the last element,
        /// and adds it to the end of the <paramref name="destination"/> list.
        /// </summary>
        public readonly void RemoveAtBySwappingAndAdd(int index, List destination, out MemoryAddress newItem, out bool capacityIncreased)
        {
            MemoryAddress.ThrowIfDefault(list);
            MemoryAddress.ThrowIfDefault(destination.list);
            ThrowIfOutOfRange(index);
            ThrowIfSizeMismatch(destination.list->stride);

            int stride = list->stride;
            int newSourceCount = list->count - 1;
            int destinationCount = destination.list->count;
            capacityIncreased = destinationCount == destination.list->capacity;
            if (capacityIncreased)
            {
                destination.list->capacity *= 2;
                MemoryAddress.Resize(ref destination.list->items, stride * destination.list->capacity);
            }

            Span<byte> removed = new(list->items.pointer + stride * index, stride);
            newItem = new(destination.list->items.pointer + destinationCount * stride);
            newItem.Write(removed);
            new Span<byte>(list->items.pointer + stride * newSourceCount, stride).CopyTo(removed);
            list->count = newSourceCount;
            destination.list->count = destinationCount + 1;
        }

        public readonly ref T Get<T>(int index) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();

            return ref list->items.ReadElement<T>(index);
        }

        public readonly void Set<T>(int index, T value) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();

            list->items.WriteElement(index, value);
        }

        public readonly void CopyFrom(List source)
        {
            MemoryAddress.ThrowIfDefault(list);
            MemoryAddress.ThrowIfDefault(source.list);
            ThrowIfSizeMismatch(source.list->stride);

            int count = source.list->count;
            if (list->capacity < count)
            {
                list->capacity = count.GetNextPowerOf2();
                MemoryAddress.Resize(ref list->items, list->stride * list->capacity);
            }

            list->items.CopyFrom(source.list->items, list->stride * count);
            list->count = count;
        }

        public readonly void CopyFrom(ReadOnlySpan<byte> bytes)
        {
            MemoryAddress.ThrowIfDefault(list);

            int count = bytes.Length / list->stride;
            if (list->capacity < count)
            {
                list->capacity = count.GetNextPowerOf2();
                MemoryAddress.Resize(ref list->items, list->stride * list->capacity);
            }

            list->items.Write(0, bytes);
            list->count = count;
        }
    }
}