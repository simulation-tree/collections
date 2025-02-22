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
        public readonly uint stride;

        internal uint count;
        internal uint capacity;
        internal Allocation items;

        public readonly uint Count => count;
        public readonly uint Capacity => capacity;
        public readonly Allocation Items => items;

        private List(uint stride, uint count, uint capacity, Allocation items)
        {
            this.stride = stride;
            this.count = count;
            this.capacity = capacity;
            this.items = items;
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfOutOfRange(List* list, uint index)
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} outside of list count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfPastRange(List* list, uint index)
        {
            if (index > list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} that is greater than list count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfLessThanCount(List* list, uint newCapacity)
        {
            if (newCapacity < list->count)
            {
                throw new InvalidOperationException($"New capacity {newCapacity} cannot be less than the current count {list->count}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowifStrideSizeMismatch<T>(List* list) where T : unmanaged
        {
            if (list->stride != (uint)sizeof(T))
            {
                throw new InvalidOperationException($"Stride size {list->stride} does not match expected size of type {sizeof(T)}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowifStrideSizeMismatch(List* list, uint length)
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
            initialCapacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            ref List list = ref Allocations.Allocate<List>();
            list = new(stride, 0, initialCapacity, new(stride * initialCapacity));
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
            uint capacity = Allocations.GetNextPowerOf2(Math.Max(1, span.Length));
            ref List list = ref Allocations.Allocate<List>();
            list = new(stride, span.Length, capacity, new(stride * capacity));
            span.CopyTo(list.items.AsSpan<T>(0, span.Length));
            fixed (List* pointer = &list)
            {
                return pointer;
            }
        }

        public static void Insert<T>(List* list, uint index, T item) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch<T>(list);

            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, (uint)sizeof(T) * list->capacity);
            }

            uint remainingCount = count - index;
            USpan<T> destination = list->items.AsSpan<T>(index + 1, remainingCount);
            USpan<T> source = list->items.AsSpan<T>(index, remainingCount);
            source.CopyTo(destination);

            list->items.WriteElement(index, item);
            list->count = count + 1;
        }

        public static void Insert(List* list, uint index, USpan<byte> elementBytes)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfPastRange(list, index);
            ThrowifStrideSizeMismatch(list, elementBytes.Length);

            uint stride = list->stride;
            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            //copy all elements after index to the right
            void* destination = (void*)(list->items.Address + (index + 1) * stride);
            void* source = (void*)(list->items.Address + index * stride);
            uint remainingCount = count - index;
            Span<byte> destinationSpan = new(destination, (int)(remainingCount * stride));
            Span<byte> sourceSpan = new(source, (int)(remainingCount * stride));
            sourceSpan.CopyTo(destinationSpan);

            //copy the new element to the index
            destination = (void*)(list->items.Address + index * stride);
            source = (void*)elementBytes.Address;
            destinationSpan = new(destination, (int)stride);
            sourceSpan = new(source, (int)stride);
            sourceSpan.CopyTo(destinationSpan);
            list->count = count + 1;
        }

        public static void Insert(List* list, uint index, Allocation element)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfPastRange(list, index);

            uint stride = list->stride;
            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            //copy all elements after index to the right
            void* destination = (void*)(list->items.Address + (index + 1) * stride);
            void* source = (void*)(list->items.Address + index * stride);
            uint remainingCount = count - index;
            Span<byte> destinationSpan = new(destination, (int)(remainingCount * stride));
            Span<byte> sourceSpan = new(source, (int)(remainingCount * stride));
            sourceSpan.CopyTo(destinationSpan);

            //copy the new element to the index
            destination = (void*)(list->items.Address + index * stride);
            source = (void*)element.Address;
            destinationSpan = new(destination, (int)stride);
            sourceSpan = new(source, (int)stride);
            sourceSpan.CopyTo(destinationSpan);
            list->count = count + 1;
        }

        public static void Add<T>(List* list, T item) where T : unmanaged
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

        public static void Add(List* list, USpan<byte> elementBytes)
        {
            Allocations.ThrowIfNull(list);
            ThrowifStrideSizeMismatch(list, elementBytes.Length);

            uint stride = list->stride;
            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            void* destination = (void*)(list->items.Address + list->count * stride);
            void* source = (void*)elementBytes.Address;
            Span<byte> destinationSpan = new(destination, (int)elementBytes.Length);
            Span<byte> sourceSpan = new(source, (int)elementBytes.Length);
            sourceSpan.CopyTo(destinationSpan);
            list->count = count + 1;
        }

        public static void AddDefault(List* list)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                Allocation.Resize(ref list->items, stride * list->capacity);
            }

            list->items.Clear(count * stride, stride);
            list->count = count + 1;
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

            list->items.Clear(list->count * stride, count * stride);
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

        public static void AddRange<T>(List* list, USpan<T> span) where T : unmanaged
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

            uint newCount = list->count - 1;
            uint stride = list->stride;
            while (index < newCount)
            {
                USpan<byte> thisElement = list->items.AsSpan<byte>(index * stride, stride);
                USpan<byte> nextElement = list->items.AsSpan<byte>((index + 1) * stride, stride);
                nextElement.CopyTo(thisElement);
                index++;
            }

            list->count = newCount;
        }

        public static void RemoveAtBySwapping(List* list, uint index)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            uint newCount = list->count - 1;
            USpan<byte> lastElement = list->items.AsSpan<byte>(newCount * list->stride, list->stride);
            USpan<byte> indexElement = list->items.AsSpan<byte>(index * list->stride, list->stride);
            lastElement.CopyTo(indexElement);
            list->count = newCount;
        }

        public static void RemoveAt<T>(List* list, uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);
            ThrowifStrideSizeMismatch<T>(list);

            uint newCount = list->count - 1;
            uint stride = list->stride;
            while (index < newCount)
            {
                T nextElement = list->items.ReadElement<T>(index + 1);
                list->items.WriteElement(index, nextElement);
                index++;
            }

            list->count = newCount;
        }

        public static void RemoveAtBySwapping<T>(List* list, uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);
            ThrowifStrideSizeMismatch<T>(list);

            uint newCount = list->count - 1;
            T lastElement = list->items.ReadElement<T>(newCount);
            list->items.WriteElement(index, lastElement);
            list->count = newCount;
        }

        public static void Clear(List* list)
        {
            Allocations.ThrowIfNull(list);

            list->count = 0;
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
    }
}
