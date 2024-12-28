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
            return Allocate(initialCapacity, TypeInfo<T>.size);
        }

        /// <summary>
        /// Allocates a new list with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public static List* Allocate(uint initialCapacity, uint stride)
        {
            List* list = Allocations.Allocate<List>();
            list->stride = stride;
            list->count = 0;
            list->capacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            list->items = new(stride * list->capacity);
            return list;
        }

        /// <summary>
        /// Allocates a new list containing the given <paramref name="span"/>.
        /// </summary>
        public static List* Allocate<T>(USpan<T> span) where T : unmanaged
        {
            uint stride = TypeInfo<T>.size;
            List* list = Allocations.Allocate<List>();
            list->count = span.Length;
            list->stride = stride;
            list->capacity = Allocations.GetNextPowerOf2(Math.Max(1, span.Length));
            list->items = new(stride * list->capacity);
            span.CopyTo(list->items.AsSpan<T>(0, span.Length));
            return list;
        }

        public static ref T GetRef<T>(List* list, uint index) where T : unmanaged
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
            T* ptr = &item;
            USpan<byte> bytes = new(ptr, list->stride);
            Insert(list, index, bytes);
        }

        public static void Insert(List* list, uint index, USpan<byte> elementBytes)
        {
            Allocations.ThrowIfNull(list);
            ThrowIfPastRange(list, index);

            uint stride = list->stride;
            if (list->count == list->capacity)
            {
                uint newCapacity = list->capacity * 2;
                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                if (list->count > 0)
                {
                    list->items.CopyTo(newItems, list->count * stride);
                }

                list->items.Dispose();
                list->items = newItems;
            }

            //copy all elements after index to the right
            void* destination = (void*)(list->items.Address + (index + 1) * stride);
            void* source = (void*)(list->items.Address + index * stride);
            uint count = list->count - index;
            System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(destination, source, count * stride);
            //copy the new element to the index
            destination = (void*)(list->items.Address + index * stride);
            source = (void*)elementBytes.Address;
            System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(destination, source, stride);
            list->count++;
        }

        public static void Add<T>(List* list, T item) where T : unmanaged
        {
            T* ptr = &item;
            USpan<byte> bytes = new(ptr, list->stride);
            Add(list, bytes);
        }

        public static void Add(List* list, USpan<byte> elementBytes)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            if (list->count == list->capacity)
            {
                uint newCapacity = list->capacity * 2;
                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                if (list->count > 0)
                {
                    list->items.CopyTo(newItems, list->count * stride);
                }

                list->items.Dispose();
                list->items = newItems;
            }

            void* destination = (void*)(list->items.Address + list->count * stride);
            void* source = (void*)elementBytes.Address;
            System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(destination, source, elementBytes.Length);
            list->count++;
        }

        public static void AddDefault(List* list, uint count = 1)
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint newCount = list->count + count;
            if (newCount >= list->capacity)
            {
                uint newCapacity = list->capacity * 2;
                while (newCount > newCapacity)
                {
                    newCapacity *= 2;
                }

                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                if (list->count > 0)
                {
                    list->items.CopyTo(newItems, stride * list->count);
                }

                list->items.Dispose();
                list->items = newItems;
            }

            void* destination = (void*)(list->items.Address + list->count * stride);
            System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned(destination, 0, count * stride);
            list->count = newCount;
        }

        public static void AddRange<T>(List* list, USpan<T> items) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);

            uint addLength = items.Length;
            uint newCount = list->count + addLength;
            if (newCount >= list->capacity)
            {
                uint newCapacity = list->capacity * 2;
                while (newCount > newCapacity)
                {
                    newCapacity *= 2;
                }

                uint stride = list->stride;
                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                if (list->count > 0)
                {
                    list->items.CopyTo(newItems, stride * list->count);
                }

                list->items.Dispose();
                list->items = newItems;
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
                uint newCapacity = list->capacity * 2;
                while (newCount > newCapacity)
                {
                    newCapacity *= 2;
                }

                Allocation newItems = new(stride * newCapacity);
                list->capacity = newCapacity;
                if (list->count > 0)
                {
                    list->items.CopyTo(newItems, stride * list->count);
                }

                list->items.Dispose();
                list->items = newItems;
            }

            USpan<byte> destination = list->items.AsSpan<byte>(list->count * stride, count * stride);
            USpan<byte> source = new(pointer, count * stride);
            source.CopyTo(destination);
            list->count = newCount;
        }

        public static uint IndexOf<T>(List* list, T item) where T : unmanaged, IEquatable<T>
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

        public static bool TryIndexOf<T>(List* list, T item, out uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);

            USpan<T> span = AsSpan<T>(list);
            return span.TryIndexOf(item, out index);
        }

        public static bool Contains<T>(List* list, T item) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);

            USpan<T> span = AsSpan<T>(list);
            return span.Contains(item);
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

            uint count = list->count;
            uint lastIndex = count - 1;
            uint stride = list->stride;
            void* lastElement = (void*)(list->items.Address + lastIndex * stride);
            void* indexElement = (void*)(list->items.Address + index * stride);
            System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(indexElement, lastElement, stride);
            list->count = lastIndex;
        }

        public static T RemoveAt<T>(List* list, uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

            USpan<T> span = list->items.AsSpan<T>(0, list->count);
            T removed = span[index];
            RemoveAt(list, index);
            return removed;
        }

        public static T RemoveAtBySwapping<T>(List* list, uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, index);

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

            uint count = list->stride / TypeInfo<T>.size * list->count;
            return list->items.AsSpan<T>(0, count);
        }

        public static USpan<T> AsSpan<T>(List* list, uint start) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);
            ThrowIfOutOfRange(list, start);

            uint count = list->stride / TypeInfo<T>.size * list->count;
            return list->items.AsSpan<T>(start, count - start);
        }

        public static USpan<T> AsSpan<T>(List* list, uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(list);

            uint stride = list->stride;
            uint count = stride / TypeInfo<T>.size * list->count;

            ThrowIfPastRange(list, start + length);
            return list->items.AsSpan<T>(start, length);
        }

        public static ref uint GetCountRef(List* list)
        {
            Allocations.ThrowIfNull(list);

            return ref list->count;
        }

        public static uint GetCapacity(List* list)
        {
            Allocations.ThrowIfNull(list);

            return list->capacity;
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
