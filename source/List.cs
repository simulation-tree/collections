using System;
using System.Diagnostics;
using Unmanaged;
using Pointer = Collections.Pointers.List;

namespace Collections
{
    public unsafe struct List : IDisposable
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
        public readonly uint Count
        {
            get
            {
                MemoryAddress.ThrowIfDefault(list);

                return list->count;
            }
        }

        /// <summary>
        /// Size of each element in the list.
        /// </summary>
        public readonly uint Stride
        {
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
            get
            {
                MemoryAddress.ThrowIfDefault(list);

                return list->items;
            }
        }

        /// <summary>
        /// Capacity of the list.
        /// </summary>
        public readonly uint Capacity
        {
            get
            {
                MemoryAddress.ThrowIfDefault(list);

                return list->capacity;
            }
            set
            {
                MemoryAddress.ThrowIfDefault(list);

                uint newCapacity = value.GetNextPowerOf2();
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
        public readonly MemoryAddress this[uint index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(list);
                ThrowIfOutOfRange(index);

                return new((void*)(list->items.Address + list->stride * index));
            }
        }

        /// <summary>
        /// Initializes an existing list from the given <paramref name="pointer"/>
        /// </summary>
        public List(Pointer* pointer)
        {
            list = pointer;
        }

        /// <summary>
        /// Creates a new list with the given <paramref name="initialCapacity"/> and <paramref name="stride"/>
        /// for each element.
        /// </summary>
        public List(uint initialCapacity, uint stride)
        {
            initialCapacity = Math.Max(1, initialCapacity).GetNextPowerOf2();
            ref Pointer list = ref MemoryAddress.Allocate<Pointer>();
            list = new(stride, 0, initialCapacity, MemoryAddress.Allocate(stride * initialCapacity));
            fixed (Pointer* pointer = &list)
            {
                this.list = pointer;
            }
        }

#if NET
        [Obsolete("Default constructor not supported", true)]
        public List()
        {
            throw new NotSupportedException();
        }
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
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for array of length {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfPastRange(uint index)
        {
            if (index > list->count)
            {
                throw new IndexOutOfRangeException($"Index {index} is past the range for array of length {list->count}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfLessThanCount(uint newCapacity)
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
                throw new InvalidOperationException($"Size mismatch. Expected stride of {sizeof(T)} but got {list->stride}");
            }
        }

        /// <summary>
        /// Retrieves a span of the elements in the list.
        /// </summary>
        public readonly USpan<T> AsSpan<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();

            return new(list->items.Pointer, list->count);
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the end of the list.
        /// </summary>
        public readonly void Add<T>(T item) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();

            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                unchecked
                {
                    MemoryAddress.Resize(ref list->items, (uint)sizeof(T) * list->capacity);
                }
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

            uint count = list->count;
            uint stride = list->stride;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                unchecked
                {
                    MemoryAddress.Resize(ref list->items, stride * list->capacity);
                }
            }

            list->items.Clear(list->count * stride, stride);
            list->count = count + 1;
        }

        public readonly void Insert<T>(uint index, T item) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();
            ThrowIfPastRange(index);

            uint count = list->count;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                unchecked
                {
                    MemoryAddress.Resize(ref list->items, (uint)sizeof(T) * list->capacity);
                }
            }

            uint remaining = count - index;
            USpan<T> destination = list->items.AsSpan<T>(index + 1, remaining);
            USpan<T> source = list->items.AsSpan<T>(index, remaining);
            source.CopyTo(destination);

            list->items.WriteElement(index, item);
            list->count = count + 1;
        }

        public readonly void Insert(uint index, MemoryAddress item)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfPastRange(index);

            uint count = list->count;
            uint stride = list->stride;
            if (count == list->capacity)
            {
                list->capacity *= 2;
                unchecked
                {
                    MemoryAddress.Resize(ref list->items, stride * list->capacity);
                }
            }

            uint remaining = count - index;
            USpan<byte> destination = list->items.AsSpan((index + 1) * stride, remaining * stride);
            USpan<byte> source = list->items.AsSpan(index * stride, remaining * stride);
            source.CopyTo(destination);

            item.CopyTo((void*)(list->items.Address + index * stride), stride);
            list->count = count + 1;
        }

        public readonly void RemoveAt(uint index)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            uint newCount = list->count - 1;
            while (index < newCount)
            {
                list->items.CopyTo(list->items, list->stride * (index + 1), list->stride * index, list->stride);
                index++;
            }

            list->count = newCount;
        }

        public readonly void RemoveAtBySwapping(uint index)
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfOutOfRange(index);

            uint newCount = list->count - 1;
            list->items.CopyTo(list->items, list->stride * newCount, list->stride * index, list->stride);
            list->count = newCount;
        }

        public readonly ref T Get<T>(uint index) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();

            return ref list->items.ReadElement<T>(index);
        }

        public readonly void Set<T>(uint index, T value) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(list);
            ThrowIfSizeMismatch<T>();

            list->items.WriteElement(index, value);
        }
    }
}