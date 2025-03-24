using Collections.Pointers;
using System;
using System.Collections;
using System.Diagnostics;
using Unmanaged;

namespace Collections
{
    public unsafe struct Array : IDisposable, IList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ArrayPointer* array;

        /// <summary>
        /// Checks if the array has been disposed.
        /// </summary>
        public readonly bool IsDisposed => array is null;

        /// <summary>
        /// Length of the array.
        /// <para>
        /// New elements will not be zeroed.
        /// </para>
        /// </summary>
        public readonly int Length
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);

                return array->length;
            }
            set
            {
                MemoryAddress.ThrowIfDefault(array);

                if (array->length != value)
                {
                    int oldLength = array->length;
                    MemoryAddress.Resize(ref array->items, array->stride * value);
                    array->length = value;
                }
            }
        }

        /// <summary>
        /// Size of each element in the array.
        /// </summary>
        public readonly int Stride
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);

                return array->stride;
            }
        }

        /// <summary>
        /// The underlying allocation of the array containing all elements.
        /// </summary>
        public readonly MemoryAddress Items
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);

                return array->items;
            }
        }

        /// <summary>
        /// The native pointer to the array.
        /// </summary>
        public readonly ArrayPointer* Pointer => array;

        public readonly MemoryAddress this[int index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);
                ThrowIfOutOfRange(index);

                return new(array->items.Pointer + array->stride * index);
            }
        }

        readonly bool IList.IsFixedSize => false;
        readonly bool IList.IsReadOnly => false;
        readonly int ICollection.Count => Length;
        readonly bool ICollection.IsSynchronized => false;
        readonly object ICollection.SyncRoot => false;

        readonly object? IList.this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Initializes an existing array from the given <paramref name="pointer"/>
        /// </summary>
        public Array(ArrayPointer* pointer)
        {
            array = pointer;
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="length"/> and <paramref name="stride"/>.
        /// </summary>
        public Array(int length, int stride)
        {
            array = MemoryAddress.AllocatePointer<ArrayPointer>();
            array->stride = stride;
            array->length = length;
            array->items = MemoryAddress.AllocateZeroed(stride * length);
        }

#if NET
        [Obsolete("Default constructor not supported", true)]
        public Array()
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
            MemoryAddress.ThrowIfDefault(array);

            array->items.Dispose();
            MemoryAddress.Free(ref array);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= array->length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for array of length {array->length}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeMismatch<T>() where T : unmanaged
        {
            if (array->stride != sizeof(T))
            {
                throw new InvalidOperationException($"Cannot get element of type {typeof(T)} from array with stride {array->stride}");
            }
        }

        public readonly Generic.Array<T> AsArray<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(array);
            ThrowIfSizeMismatch<T>();

            return new(array);
        }

        public readonly Span<T> AsSpan<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(array);
            ThrowIfSizeMismatch<T>();

            return new(array->items.Pointer, array->length);
        }

        /// <summary>
        /// Gets a span of all bytes containing the memory of the array.
        /// </summary>
        public readonly Span<byte> AsSpan()
        {
            MemoryAddress.ThrowIfDefault(array);

            return new(array->items.Pointer, array->length * array->stride);
        }

        /// <summary>
        /// Gets a span of all bytes from <paramref name="byteStart"/>.
        /// </summary>
        public readonly Span<byte> AsSpan(int byteStart)
        {
            MemoryAddress.ThrowIfDefault(array);

            return new(array->items.Pointer + byteStart, (array->length * array->stride) - byteStart);
        }

        /// <summary>
        /// Resets all elements in the array to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(array);

            array->items.Clear(array->length * array->stride);
        }

        /// <summary>
        /// Resets a range of elements in the array to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear(int startIndex, int length)
        {
            MemoryAddress.ThrowIfDefault(array);

            array->items.Clear(startIndex * array->stride, length * array->stride);
        }

        public readonly ref T Get<T>(int index) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(array);
            ThrowIfSizeMismatch<T>();

            return ref array->items.ReadElement<T>(index);
        }

        public readonly void Set<T>(int index, T value) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(array);
            ThrowIfSizeMismatch<T>();

            array->items.WriteElement(index, value);
        }

        readonly int IList.Add(object? value)
        {
            throw new NotSupportedException();
        }

        readonly bool IList.Contains(object? value)
        {
            throw new NotSupportedException();
        }

        readonly int IList.IndexOf(object? value)
        {
            throw new NotSupportedException();
        }

        readonly void IList.Insert(int index, object? value)
        {
            throw new NotSupportedException();
        }

        readonly void IList.Remove(object? value)
        {
            throw new NotSupportedException();
        }

        readonly void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        readonly void ICollection.CopyTo(System.Array array, int index)
        {
            throw new NotSupportedException();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}