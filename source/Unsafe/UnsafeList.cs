using System;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Unsafe
{
    /// <summary>
    /// Opaque pointer implementation of a list.
    /// </summary>
    public unsafe struct UnsafeList
    {
        private uint stride;
        private uint count;
        private uint capacity;
        private Allocation items;

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(UnsafeList* list, uint index)
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} outside of list count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfPastRange(UnsafeList* list, uint index)
        {
            if (index > list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} that is greater than list count {list->count}");
            }
        }

        /// <inheritdoc/>
        public static void Free(ref UnsafeList* list)
        {
            Allocations.ThrowIfNull(list);

            list->items.Dispose();
            Allocations.Free(ref list);
        }

        /// <summary>
        /// Allocates a new list with the given <paramref name="initialCapacity"/>.
        /// <para>
        /// May throw <see cref="InvalidOperationException"/> if the capacity is zero.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>"
        public static UnsafeList* Allocate<T>(uint initialCapacity) where T : unmanaged
        {
            return Allocate(initialCapacity, TypeInfo<T>.size);
        }

        /// <summary>
        /// Allocates a new list with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public static UnsafeList* Allocate(uint initialCapacity, uint stride)
        {
            UnsafeList* list = Allocations.Allocate<UnsafeList>();
            list->stride = stride;
            list->count = 0;
            list->capacity = initialCapacity;
            list->items = new(stride * initialCapacity);
            return list;
        }

        /// <summary>
        /// Allocates a new list containing the given <paramref name="span"/>.
        /// </summary>
        public static UnsafeList* Allocate<T>(USpan<T> span) where T : unmanaged
        {
            uint stride = TypeInfo<T>.size;
            UnsafeList* list = Allocations.Allocate<UnsafeList>();
            list->count = span.Length;
            list->stride = stride;
            list->capacity = span.Length;
            list->items = Allocation.Create(span);
            return list;
        }

        /// <inheritdoc/>
        public static ref T GetRef<T>(UnsafeList* list, uint index) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            T* ptr = (T*)GetStartAddress(list);
            return ref ptr[index];
        }

        /// <summary>
        /// Returns the bytes for the element at the given index.
        /// </summary>
        public static USpan<byte> GetElementBytes(UnsafeList* list, uint index)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            uint stride = list->stride;
            return list->items.AsSpan<byte>(index * stride, stride);
        }

        /// <inheritdoc/>
        public static void Insert<T>(UnsafeList* list, uint index, T item) where T : unmanaged
        {
            T* ptr = &item;
            USpan<byte> bytes = new(ptr, list->stride);
            Insert(list, index, bytes);
        }

        /// <inheritdoc/>
        public static void Insert(UnsafeList* list, uint index, USpan<byte> elementBytes)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfPastRange(list, index);

            uint stride = list->stride;
            if (list->count == GetCapacity(list))
            {
                uint newCapacity = Allocations.GetNextPowerOf2(list->count + 1);
                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                list->items.CopyTo(newItems, 0, 0, list->count * stride);
                list->items.Dispose();
                list->items = newItems;
            }

            USpan<byte> destination = list->items.AsSpan<byte>((index + 1) * stride, (list->count - index) * stride);
            USpan<byte> source = list->items.AsSpan<byte>(index * stride, (list->count - index) * stride);
            source.CopyTo(destination);
            elementBytes.CopyTo(list->items.AsSpan<byte>(index * stride, stride));
            list->count++;
        }

        /// <inheritdoc/>
        public static void Add<T>(UnsafeList* list, T item) where T : unmanaged
        {
            T* ptr = &item;
            USpan<byte> bytes = new(ptr, list->stride);
            Add(list, bytes);
        }

        /// <inheritdoc/>
        public static void Add(UnsafeList* list, USpan<byte> elementBytes)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint capacity = GetCapacity(list);
            if (list->count == capacity)
            {
                uint newCapacity = Allocations.GetNextPowerOf2(list->count + 1);
                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                list->items.CopyTo(newItems, 0, 0, list->count * stride);
                list->items.Dispose();
                list->items = newItems;
            }

            elementBytes.CopyTo(list->items.AsSpan<byte>(list->count * stride, stride));
            list->count++;
        }

        /// <inheritdoc/>
        public static void AddDefault(UnsafeList* list, uint count = 1)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint newCount = list->count + count;
            if (newCount >= GetCapacity(list))
            {
                uint newCapacity = Allocations.GetNextPowerOf2(newCount);
                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                list->items.CopyTo(newItems, 0, 0, stride * list->count);
                list->items.Dispose();
                list->items = newItems;
            }

            USpan<byte> bytes = list->items.AsSpan<byte>(list->count * stride, count * stride);
            bytes.Clear();
            list->count = newCount;
        }

        /// <inheritdoc/>
        public static void AddRange<T>(UnsafeList* list, USpan<T> items) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);

            uint addLength = items.Length;
            uint newCount = list->count + addLength;
            if (newCount >= GetCapacity(list))
            {
                uint stride = list->stride;
                uint newCapacity = Allocations.GetNextPowerOf2(newCount);
                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                list->items.CopyTo(newItems, 0, 0, stride * list->count);
                list->items.Dispose();
                list->items = newItems;
            }

            USpan<T> destination = list->items.AsSpan<T>(list->count, addLength);
            items.CopyTo(destination);
            list->count = newCount;
        }

        /// <inheritdoc/>
        public static void AddRange(UnsafeList* list, void* pointer, uint count)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint newCount = list->count + count;
            if (newCount >= GetCapacity(list))
            {
                uint newCapacity = Allocations.GetNextPowerOf2(newCount);
                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                list->items.CopyTo(newItems, 0, 0, stride * list->count);
                list->items.Dispose();
                list->items = newItems;
            }

            USpan<byte> destination = list->items.AsSpan<byte>(list->count * stride, count * stride);
            USpan<byte> source = new(pointer, count * stride);
            source.CopyTo(destination);
            list->count = newCount;
        }

        /// <inheritdoc/>
        public static uint IndexOf<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);

            USpan<T> span = AsSpan<T>(list);
            if (span.TryIndexOf(item, out uint index))
            {
                return index;
            }
            else
            {
                throw new NullReferenceException($"Item {item} not found in list");
            }
        }

        /// <inheritdoc/>
        public static bool TryIndexOf<T>(UnsafeList* list, T item, out uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);

            USpan<T> span = AsSpan<T>(list);
            return span.TryIndexOf(item, out index);
        }

        /// <inheritdoc/>
        public static bool Contains<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);

            USpan<T> span = AsSpan<T>(list);
            return span.Contains(item);
        }

        /// <inheritdoc/>
        public static void RemoveAt(UnsafeList* list, uint index)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            uint count = list->count;
            uint stride = list->stride;
            while (index < count - 1)
            {
                USpan<byte> thisElement = list->items.AsSpan<byte>(index * stride, stride);
                USpan<byte> nextElement = list->items.AsSpan<byte>((index + 1) * stride, stride);
                nextElement.CopyTo(thisElement);
                index++;
            }

            list->count--;
        }

        /// <inheritdoc/>
        public static void RemoveAtBySwapping(UnsafeList* list, uint index)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            uint count = list->count;
            uint lastIndex = count - 1;
            uint stride = list->stride;
            USpan<byte> lastElement = list->items.AsSpan<byte>(lastIndex * stride, stride);
            USpan<byte> indexElement = list->items.AsSpan<byte>(index * stride, stride);
            lastElement.CopyTo(indexElement);
            list->count = lastIndex;
        }

        /// <inheritdoc/>
        public static T RemoveAt<T>(UnsafeList* list, uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            USpan<T> span = list->items.AsSpan<T>(0, list->count);
            T removed = span[index];
            RemoveAt(list, index);
            return removed;
        }

        /// <inheritdoc/>
        public static T RemoveAtBySwapping<T>(UnsafeList* list, uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            USpan<T> span = list->items.AsSpan<T>(0, list->count);
            T removed = span[index];
            RemoveAtBySwapping(list, index);
            return removed;
        }

        /// <inheritdoc/>
        public static void Clear(UnsafeList* list)
        {
            Allocations.ThrowIfNull(list);

            list->count = 0;
        }

        /// <inheritdoc/>
        public static USpan<T> AsSpan<T>(UnsafeList* list) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);

            uint count = list->stride / TypeInfo<T>.size * list->count;
            return list->items.AsSpan<T>(0, count);
        }

        /// <inheritdoc/>
        public static USpan<T> AsSpan<T>(UnsafeList* list, uint start) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, start);

            uint count = list->stride / TypeInfo<T>.size * list->count;
            return list->items.AsSpan<T>(start, count - start);
        }

        /// <inheritdoc/>
        public static USpan<T> AsSpan<T>(UnsafeList* list, uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint count = stride / TypeInfo<T>.size * list->count;

            ThrowIfPastRange(list, start + length);
            return list->items.AsSpan<T>(start, length);
        }

        /// <inheritdoc/>
        public static ref uint GetCountRef(UnsafeList* list)
        {
            Allocations.ThrowIfNull(list);

            return ref list->count;
        }

        /// <inheritdoc/>
        public static uint GetCapacity(UnsafeList* list)
        {
            Allocations.ThrowIfNull(list);

            return list->capacity;
        }

        /// <inheritdoc/>
        public static void SetCapacity(UnsafeList* list, uint newCapacity)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            Allocation newItems = new(stride * newCapacity);
            list->capacity = newCapacity;
            list->items.CopyTo(newItems, 0, 0, list->count * stride);
            list->items.Dispose();
            list->items = newItems;
        }

        /// <summary>
        /// Returns the address of the first element in the list.
        /// </summary>
        public static nint GetStartAddress(UnsafeList* list)
        {
            Allocations.ThrowIfNull(list);

            return list->items.Address;
        }

        /// <inheritdoc/>
        public static void CopyElementTo(UnsafeList* source, uint sourceIndex, UnsafeList* destination, uint destinationIndex)
        {
            ThrowIfOutOfRange(source, sourceIndex);
            ThrowIfOutOfRange(destination, destinationIndex);

            uint stride = source->stride;
            USpan<byte> sourceElement = source->items.AsSpan<byte>(sourceIndex * stride, stride);
            USpan<byte> destinationElement = destination->items.AsSpan<byte>(destinationIndex * stride, stride);
            sourceElement.CopyTo(destinationElement);
        }

        /// <inheritdoc/>
        public static void CopyTo<T>(UnsafeList* source, uint sourceIndex, USpan<T> destination, uint destinationIndex) where T : unmanaged
        {
            ThrowIfOutOfRange(source, sourceIndex);

            if (destinationIndex + source->count - sourceIndex > destination.Length)
            {
                throw new ArgumentException("Destination span is too small to fit destination");
            }

            USpan<T> sourceSpan = AsSpan<T>(source, sourceIndex);
            sourceSpan.CopyTo(destination.Slice(destinationIndex));
        }
    }
}
