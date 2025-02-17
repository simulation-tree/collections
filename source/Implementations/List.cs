using System;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Implementations
{
    /// <summary>
    /// Opaque pointer implementation of a list.
    /// </summary>
    public unsafe struct List
    {
        private uint stride;
        private uint count;
        private uint capacity;
        private Allocation items;

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(List* list, uint index)
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} outside of list count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfPastRange(List* list, uint index)
        {
            if (index > list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} that is greater than list count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfLessThanCount(List* list, uint newCapacity)
        {
            if (newCapacity < list->count)
            {
                throw new InvalidOperationException($"New capacity {newCapacity} cannot be less than the current count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowifStrideSizeMismatch<T>(List* list) where T : unmanaged
        {
            if (list->stride != (uint)sizeof(T))
            {
                throw new InvalidOperationException($"Stride size {list->stride} does not match expected size of type {sizeof(T)}");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowifStrideSizeMismatch(List* list, uint length)
        {
            if (list->stride != length)
            {
                throw new InvalidOperationException($"Stride size {list->stride} does not match expected size of length {length}");
            }
        }

        public static void Free(ref List* list)
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
        public static List* Allocate<T>(uint initialCapacity) where T : unmanaged
        {
            return Allocate(initialCapacity, (uint)sizeof(T));
        }

        /// <summary>
        /// Allocates a new list with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public static List* Allocate(uint initialCapacity, uint stride)
        {
            ref List list = ref Allocations.Allocate<List>();
            list.stride = stride;
            list.count = 0;
            list.capacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            list.items = new(stride * list.capacity);
            fixed (List* pointer = &list)
            {
                return pointer;
            }
        }

        /// <summary>
        /// Allocates a new list containing the given <paramref name="span"/>.
        /// </summary>
        public static List* Allocate<T>(USpan<T> span) where T : unmanaged
        {
            uint stride = (uint)sizeof(T);
            ref List list = ref Allocations.Allocate<List>();
            list.count = span.Length;
            list.stride = stride;
            list.capacity = Allocations.GetNextPowerOf2(Math.Max(1, span.Length));
            list.items = new(stride * list.capacity);
            span.CopyTo(list.items.AsSpan<T>(0, span.Length));
            fixed (List* pointer = &list)
            {
                return pointer;
            }
        }

        public static ref T GetElement<T>(List* list, uint index) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            T* ptr = (T*)GetStartAddress(list);
            return ref ptr[index];
        }

        /// <summary>
        /// Returns the bytes for the element at the given index.
        /// </summary>
        public static USpan<byte> GetElementBytes(List* list, uint index)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            uint stride = list->stride;
            return list->items.AsSpan<byte>(index * stride, stride);
        }

        public static void Insert<T>(List* list, uint index, T item) where T : unmanaged
        {
            ThrowifStrideSizeMismatch<T>(list);

            uint stride = list->stride;
            if (list->count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            uint count = list->count - index;
            USpan<T> destination = list->items.AsSpan<T>(index + 1, count);
            USpan<T> source = list->items.AsSpan<T>(index, count);
            source.CopyTo(destination);

            list->items.Write(index * stride, item);
            list->count++;
        }

        public static void Insert(List* list, uint index, USpan<byte> elementBytes)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfPastRange(list, index);
            ThrowifStrideSizeMismatch(list, elementBytes.Length);

            uint stride = list->stride;
            if (list->count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            //copy all elements after index to the right
            void* destination = (void*)(list->items.Address + (index + 1) * stride);
            void* source = (void*)(list->items.Address + index * stride);
            uint count = list->count - index;
            Span<byte> destinationSpan = new(destination, (int)(count * stride));
            Span<byte> sourceSpan = new(source, (int)(count * stride));
            sourceSpan.CopyTo(destinationSpan);

            //copy the new element to the index
            destination = (void*)(list->items.Address + index * stride);
            source = (void*)elementBytes.Address;
            destinationSpan = new(destination, (int)stride);
            sourceSpan = new(source, (int)stride);
            sourceSpan.CopyTo(destinationSpan);
            list->count++;
        }

        public static void Add<T>(List* list, T item) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch<T>(list);

            uint stride = list->stride;
            if (list->count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            list->items.Write(list->count * stride, item);
            list->count++;
        }

        public static void Add(List* list, USpan<byte> elementBytes)
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch(list, elementBytes.Length);

            uint stride = list->stride;
            if (list->count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            void* destination = (void*)(list->items.Address + list->count * stride);
            void* source = (void*)elementBytes.Address;
            Span<byte> destinationSpan = new(destination, (int)elementBytes.Length);
            Span<byte> sourceSpan = new(source, (int)elementBytes.Length);
            sourceSpan.CopyTo(destinationSpan);
            list->count++;
        }

        public static void AddDefault(List* list)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            if (list->count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            void* destination = (void*)(list->items.Address + list->count * stride);
            USpan<byte> destinationSpan = new(destination, stride);
            destinationSpan.Clear();
            list->count++;
        }

        public static void AddDefault(List* list, uint count)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint newCount = list->count + count;
            if (newCount >= list->capacity)
            {
                list->capacity = Allocations.GetNextPowerOf2(newCount);
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            void* destination = (void*)(list->items.Address + list->count * stride);
            USpan<byte> destinationSpan = new(destination, count * stride);
            destinationSpan.Clear();
            list->count = newCount;
        }

        public static void AddRepeat<T>(List* list, T value, uint count) where T : unmanaged
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
            span.Fill(value);
            list->count = newCount;
        }

        public static void AddRange<T>(List* list, USpan<T> items) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch<T>(list);

            uint addLength = items.Length;
            uint newCount = list->count + addLength;
            if (newCount >= list->capacity)
            {
                list->capacity = Allocations.GetNextPowerOf2(newCount);
                Allocation.Resize(ref list->items, list->stride * list->capacity);
            }

            USpan<T> destination = list->items.AsSpan<T>(list->count, addLength);
            items.CopyTo(destination);
            list->count = newCount;
        }

        public static void AddRange(List* list, void* pointer, uint count)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint newCount = list->count + count;
            if (newCount >= list->capacity)
            {
                list->capacity = Allocations.GetNextPowerOf2(newCount);
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            USpan<byte> destination = list->items.AsSpan<byte>(list->count * stride, count * stride);
            USpan<byte> source = new(pointer, count * stride);
            source.CopyTo(destination);
            list->count = newCount;
        }

        public static void RemoveAt(List* list, uint index)
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

        public static void RemoveAtBySwapping(List* list, uint index)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            USpan<byte> lastElement = list->items.AsSpan<byte>((list->count - 1) * list->stride, list->stride);
            USpan<byte> indexElement = list->items.AsSpan<byte>(index * list->stride, list->stride);
            lastElement.CopyTo(indexElement);
            list->count--;
        }

        public static T RemoveAt<T>(List* list, uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);
            ThrowifStrideSizeMismatch<T>(list);

            USpan<T> span = list->items.AsSpan<T>(0, list->count);
            T removed = span[index];
            RemoveAt(list, index);
            return removed;
        }

        public static T RemoveAtBySwapping<T>(List* list, uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);
            ThrowifStrideSizeMismatch<T>(list);

            USpan<T> span = list->items.AsSpan<T>(0, list->count);
            T removed = span[index];
            RemoveAtBySwapping(list, index);
            return removed;
        }

        public static void Clear(List* list)
        {
            Allocations.ThrowIfNull(list);

            list->count = 0;
        }

        public static USpan<T> AsSpan<T>(List* list) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch<T>(list);

            return list->items.AsSpan<T>(0, list->count);
        }

        public static USpan<T> AsSpan<T>(List* list, uint start) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch<T>(list);
            ThrowIfOutOfRange(list, start);

            return list->items.AsSpan<T>(start, list->count - start);
        }

        public static USpan<T> AsSpan<T>(List* list, uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch<T>(list);
            ThrowIfPastRange(list, start + length);

            return list->items.AsSpan<T>(start, length);
        }

        public static uint GetCount(List* list)
        {
            Allocations.ThrowIfNull(list);

            return list->count;
        }

        public static uint GetCapacity(List* list)
        {
            Allocations.ThrowIfNull(list);

            return list->capacity;
        }

        public static uint SetCapacity(List* list, uint newCapacity)
        {
            Allocations.ThrowIfNull(list);

            newCapacity = Allocations.GetNextPowerOf2(newCapacity);
            ThrowIfLessThanCount(list, newCapacity);

            uint stride = list->stride;
            Allocation newItems = new(stride * newCapacity);
            list->items.CopyTo(newItems, stride * list->count);
            list->items.Dispose();
            list->items = newItems;
            list->capacity = newCapacity;
            return newCapacity;
        }

        /// <summary>
        /// Returns the address of the first element in the list.
        /// </summary>
        public static nint GetStartAddress(List* list)
        {
            Allocations.ThrowIfNull(list);

            return list->items.Address;
        }

        public static void CopyElementTo(List* source, uint sourceIndex, List* destination, uint destinationIndex)
        {
            ThrowIfOutOfRange(source, sourceIndex);
            ThrowIfOutOfRange(destination, destinationIndex);

            uint stride = source->stride;
            USpan<byte> sourceElement = source->items.AsSpan<byte>(sourceIndex * stride, stride);
            USpan<byte> destinationElement = destination->items.AsSpan<byte>(destinationIndex * stride, stride);
            sourceElement.CopyTo(destinationElement);
        }

        public static void CopyTo<T>(List* source, uint sourceIndex, USpan<T> destination, uint destinationIndex) where T : unmanaged
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
