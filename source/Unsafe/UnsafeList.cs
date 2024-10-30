using System;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Unsafe
{
    public unsafe struct UnsafeList
    {
        private RuntimeType type;
        private uint count;
        private uint capacity;
        private Allocation items;

        [Conditional("DEBUG")]
        public static void ThrowIfLengthIsZero(uint value)
        {
            if (value == 0)
            {
                throw new InvalidOperationException("List capacity cannot be zero");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfDisposed(UnsafeList* list)
        {
            if (IsDisposed(list))
            {
                throw new ObjectDisposedException(nameof(UnsafeList));
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfOutOfRange(UnsafeList* list, uint index)
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} that is out of range, count: {list->count}");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfPastRange(UnsafeList* list, uint index)
        {
            if (index > list->count)
            {
                throw new IndexOutOfRangeException($"Trying to insert at index {index} that is out of range, count: {list->count}");
            }
        }

        public static bool IsDisposed(UnsafeList* list)
        {
            return Allocations.IsNull(list);
        }

        public static void Free(ref UnsafeList* list)
        {
            ThrowIfDisposed(list);
            list->items.Dispose();
            Allocations.Free(ref list);
        }

        public static UnsafeList* Allocate<T>(uint initialCapacity = 1) where T : unmanaged
        {
            return Allocate(RuntimeType.Get<T>(), initialCapacity);
        }

        public static UnsafeList* Allocate(RuntimeType type, uint initialCapacity = 1)
        {
            ThrowIfLengthIsZero(initialCapacity);
            UnsafeList* list = Allocations.Allocate<UnsafeList>();
            list->type = type;
            list->count = 0;
            list->capacity = initialCapacity;
            list->items = new(type.Size * initialCapacity);
            return list;
        }

        public static UnsafeList* Allocate<T>(USpan<T> span) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            UnsafeList* list = Allocations.Allocate<UnsafeList>();
            list->count = span.Length;
            list->type = type;
            list->capacity = list->count;
            list->items = Allocation.Create(span);
            return list;
        }

        public static ref T GetRef<T>(UnsafeList* list, uint index) where T : unmanaged
        {
            ThrowIfDisposed(list);
            ThrowIfOutOfRange(list, index);
            T* ptr = (T*)GetStartAddress(list);
            return ref ptr[index];
        }

        public static T Get<T>(UnsafeList* list, uint index) where T : unmanaged
        {
            ThrowIfDisposed(list);
            ThrowIfOutOfRange(list, index);
            T* ptr = (T*)GetStartAddress(list);
            return ptr[index];
        }

        public static void Set<T>(UnsafeList* list, uint index, T value) where T : unmanaged
        {
            ThrowIfDisposed(list);
            ThrowIfOutOfRange(list, index);
            T* ptr = (T*)GetStartAddress(list);
            ptr[index] = value;
        }

        /// <summary>
        /// Returns the bytes for the element at the given index.
        /// </summary>
        public static USpan<byte> GetElementBytes(UnsafeList* list, uint index)
        {
            ThrowIfDisposed(list);
            ThrowIfOutOfRange(list, index);
            uint elementSize = list->type.Size;
            return list->items.AsSpan<byte>(index * elementSize, elementSize);
        }

        public static void Insert<T>(UnsafeList* list, uint index, T item) where T : unmanaged
        {
            T* ptr = &item;
            USpan<byte> bytes = new(ptr, list->type.Size);
            Insert(list, index, bytes);
        }

        public static void Insert(UnsafeList* list, uint index, USpan<byte> elementBytes)
        {
            ThrowIfDisposed(list);
            ThrowIfPastRange(list, index);
            uint elementSize = list->type.Size;
            uint capacity = GetCapacity(list);
            if (list->count == capacity)
            {
                uint newCapacity = capacity * 2;
                Allocation newItems = new(elementSize * newCapacity);
                list->capacity = newCapacity;
                list->items.CopyTo(newItems, 0, 0, list->count * elementSize);
                list->items.Dispose();
                list->items = newItems;
            }

            USpan<byte> destination = list->items.AsSpan<byte>((index + 1) * elementSize, (list->count - index) * elementSize);
            USpan<byte> source = list->items.AsSpan<byte>(index * elementSize, (list->count - index) * elementSize);
            source.CopyTo(destination);
            elementBytes.CopyTo(list->items.AsSpan<byte>(index * elementSize, elementSize));
            list->count++;
        }

        public static void Add<T>(UnsafeList* list, T item) where T : unmanaged
        {
            T* ptr = &item;
            USpan<byte> bytes = new(ptr, list->type.Size);
            Add(list, bytes);
        }

        public static void Add(UnsafeList* list, USpan<byte> elementBytes)
        {
            ThrowIfDisposed(list);
            uint elementSize = list->type.Size;
            uint capacity = GetCapacity(list);
            if (list->count == capacity)
            {
                uint newCapacity = capacity * 2;
                Allocation newItems = new(elementSize * newCapacity);
                list->capacity = newCapacity;
                list->items.CopyTo(newItems, 0, 0, list->count * elementSize);
                list->items.Dispose();
                list->items = newItems;
            }

            elementBytes.CopyTo(list->items.AsSpan<byte>(list->count * elementSize, elementSize));
            list->count++;
        }

        public static void AddDefault(UnsafeList* list, uint count = 1)
        {
            ThrowIfDisposed(list);
            uint elementSize = list->type.Size;
            uint newCount = list->count + count;
            if (newCount >= GetCapacity(list))
            {
                Allocation newItems = new(elementSize * newCount * 2);
                list->capacity = newCount;
                list->items.CopyTo(newItems, 0, 0, elementSize * list->count);
                list->items.Dispose();
                list->items = newItems;
            }

            USpan<byte> bytes = list->items.AsSpan<byte>(list->count * elementSize, count * elementSize);
            bytes.Clear();
            list->count = newCount;
        }

        public static void AddRange<T>(UnsafeList* list, USpan<T> items) where T : unmanaged
        {
            ThrowIfDisposed(list);
            uint capacity = GetCapacity(list);
            uint addLength = items.Length;
            uint newCount = list->count + addLength;
            if (newCount >= capacity)
            {
                uint elementSize = list->type.Size;
                Allocation newItems = new(elementSize * newCount * 2);
                list->capacity = newCount;
                list->items.CopyTo(newItems, 0, 0, elementSize * list->count);
                list->items.Dispose();
                list->items = newItems;
            }

            USpan<T> destination = list->items.AsSpan<T>(list->count, addLength);
            items.CopyTo(destination);
            list->count = newCount;
        }

        public static void AddRange(UnsafeList* list, void* pointer, uint count)
        {
            ThrowIfDisposed(list);
            uint capacity = GetCapacity(list);
            uint elementSize = list->type.Size;
            uint newCount = list->count + count;
            if (newCount >= capacity)
            {
                Allocation newItems = new(elementSize * newCount * 2);
                list->capacity = newCount;
                list->items.CopyTo(newItems, 0, 0, elementSize * list->count);
                list->items.Dispose();
                list->items = newItems;
            }

            USpan<byte> destination = list->items.AsSpan<byte>(list->count * elementSize, count * elementSize);
            USpan<byte> source = new(pointer, count * elementSize);
            source.CopyTo(destination);
            list->count = newCount;
        }

        public static uint IndexOf<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            ThrowIfDisposed(list);
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

        public static bool TryIndexOf<T>(UnsafeList* list, T item, out uint index) where T : unmanaged, IEquatable<T>
        {
            ThrowIfDisposed(list);
            USpan<T> span = AsSpan<T>(list);
            return span.TryIndexOf(item, out index);
        }

        public static bool Contains<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            ThrowIfDisposed(list);
            USpan<T> span = AsSpan<T>(list);
            return span.Contains(item);
        }

        public static void RemoveAt(UnsafeList* list, uint index)
        {
            ThrowIfDisposed(list);
            ThrowIfOutOfRange(list, index);
            uint count = list->count;
            uint size = list->type.Size;
            while (index < count - 1)
            {
                USpan<byte> thisElement = list->items.AsSpan<byte>(index * size, size);
                USpan<byte> nextElement = list->items.AsSpan<byte>((index + 1) * size, size);
                nextElement.CopyTo(thisElement);
                index++;
            }

            list->count--;
        }

        public static void RemoveAtBySwapping(UnsafeList* list, uint index)
        {
            ThrowIfDisposed(list);
            ThrowIfOutOfRange(list, index);
            uint count = list->count;
            uint lastIndex = count - 1;
            uint size = list->type.Size;
            USpan<byte> lastElement = list->items.AsSpan<byte>(lastIndex * size, size);
            USpan<byte> indexElement = list->items.AsSpan<byte>(index * size, size);
            lastElement.CopyTo(indexElement);
            list->count = lastIndex;
        }

        public static T RemoveAt<T>(UnsafeList* list, uint index) where T : unmanaged, IEquatable<T>
        {
            ThrowIfDisposed(list);
            ThrowIfOutOfRange(list, index);
            USpan<T> span = list->items.AsSpan<T>(0, list->count);
            T removed = span[index];
            RemoveAt(list, index);
            return removed;
        }

        public static T RemoveAtBySwapping<T>(UnsafeList* list, uint index) where T : unmanaged, IEquatable<T>
        {
            ThrowIfDisposed(list);
            ThrowIfOutOfRange(list, index);
            USpan<T> span = list->items.AsSpan<T>(0, list->count);
            T removed = span[index];
            RemoveAtBySwapping(list, index);
            return removed;
        }

        public static void Clear(UnsafeList* list)
        {
            ThrowIfDisposed(list);
            list->count = 0;
        }

        public static USpan<T> AsSpan<T>(UnsafeList* list) where T : unmanaged
        {
            ThrowIfDisposed(list);
            uint size = list->type.Size;
            uint count = size / USpan<T>.ElementSize * list->count;
            return list->items.AsSpan<T>(0, count);
        }

        public static USpan<T> AsSpan<T>(UnsafeList* list, uint start) where T : unmanaged
        {
            ThrowIfDisposed(list);
            ThrowIfOutOfRange(list, start);
            uint size = list->type.Size;
            uint count = size / USpan<T>.ElementSize * list->count;
            return list->items.AsSpan<T>(start, count - start);
        }

        public static USpan<T> AsSpan<T>(UnsafeList* list, uint start, uint length) where T : unmanaged
        {
            ThrowIfDisposed(list);
            uint size = list->type.Size;
            uint count = size / USpan<T>.ElementSize * list->count;
            if (start + length > count)
            {
                throw new IndexOutOfRangeException();
            }

            return list->items.AsSpan<T>(start, length);
        }

        public static ref uint GetCountRef(UnsafeList* list)
        {
            ThrowIfDisposed(list);
            return ref list->count;
        }

        public static uint GetCapacity(UnsafeList* list)
        {
            ThrowIfDisposed(list);
            return list->capacity;
        }

        public static void SetCapacity(UnsafeList* list, uint newCapacity)
        {
            ThrowIfDisposed(list);
            uint elementSize = list->type.Size;
            Allocation newItems = new(elementSize * newCapacity);
            list->capacity = newCapacity;
            list->items.CopyTo(newItems, 0, 0, list->count * elementSize);
            list->items.Dispose();
            list->items = newItems;
        }

        /// <summary>
        /// Returns the address of the first element in the list.
        /// </summary>
        public static nint GetStartAddress(UnsafeList* list)
        {
            ThrowIfDisposed(list);
            return list->items.Address;
        }

        public static void CopyElementTo(UnsafeList* source, uint sourceIndex, UnsafeList* destination, uint destinationIndex)
        {
            ThrowIfOutOfRange(source, sourceIndex);
            ThrowIfOutOfRange(destination, destinationIndex);
            uint elementSize = source->type.Size;
            USpan<byte> sourceElement = source->items.AsSpan<byte>(sourceIndex * elementSize, elementSize);
            USpan<byte> destinationElement = destination->items.AsSpan<byte>(destinationIndex * elementSize, elementSize);
            sourceElement.CopyTo(destinationElement);
        }

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
