using System;
using System.Collections;
using System.Diagnostics;
using Unmanaged;
using Pointer = Collections.Pointers.Array;

namespace Collections
{
    public unsafe struct Array : IDisposable, IList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Pointer* array;

        /// <summary>
        /// Checks if the array has been disposed.
        /// </summary>
        public readonly bool IsDisposed => array is null;

        /// <summary>
        /// Length of the array.
        /// <para>
        /// Resizing the array to be bigger will not clear the new elements.
        /// </para>
        /// </summary>
        public readonly uint Length
        {
            get
            {
                Allocations.ThrowIfNull(array);

                return array->length;
            }
            set
            {
                Allocations.ThrowIfNull(array);

                if (array->length != value)
                {
                    uint oldLength = array->length;
                    Allocation.Resize(ref array->items, array->stride * value);
                    array->length = value;
                }
            }
        }

        /// <summary>
        /// Size of each element in the array.
        /// </summary>
        public readonly uint Stride
        {
            get
            {
                Allocations.ThrowIfNull(array);

                return array->stride;
            }
        }

        /// <summary>
        /// The underlying allocation of the array containing all elements.
        /// </summary>
        public readonly Allocation Items
        {
            get
            {
                Allocations.ThrowIfNull(array);

                return array->items;
            }
        }

        public readonly Allocation this[uint index]
        {
            get
            {
                Allocations.ThrowIfNull(array);
                ThrowIfOutOfRange(index);

                return new((void*)((nint)array->items + array->stride * index));
            }
        }

        readonly bool IList.IsFixedSize => false;
        readonly bool IList.IsReadOnly => false;
        readonly int ICollection.Count => (int)Length;
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
        public Array(Pointer* pointer)
        {
            array = pointer;
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="length"/> and <paramref name="stride"/>.
        /// </summary>
        public Array(uint length, uint stride)
        {
            ref Pointer array = ref Allocations.Allocate<Pointer>();
            array = new(stride, length, Allocation.CreateZeroed(stride * length));
            fixed (Pointer* pointer = &array)
            {
                this.array = pointer;
            }
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
            Allocations.ThrowIfNull(array);

            array->items.Dispose();
            Allocations.Free(ref array);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
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

        public readonly USpan<T> AsSpan<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(array);
            ThrowIfSizeMismatch<T>();

            return array->items.GetSpan<T>(array->length);
        }

        /// <summary>
        /// Resets all elements in the array to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear()
        {
            Allocations.ThrowIfNull(array);

            unchecked
            {
                array->items.Clear(array->length * array->stride);
            }
        }

        /// <summary>
        /// Resets a range of elements in the array to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear(uint startIndex, uint length)
        {
            Allocations.ThrowIfNull(array);

            unchecked
            {
                array->items.Clear(startIndex * array->stride, length * array->stride);
            }
        }

        public readonly ref T Get<T>(uint index) where T : unmanaged
        {
            Allocations.ThrowIfNull(array);
            ThrowIfSizeMismatch<T>();

            return ref array->items.ReadElement<T>(index);
        }

        public readonly void Set<T>(uint index, T value) where T : unmanaged
        {
            Allocations.ThrowIfNull(array);
            ThrowIfSizeMismatch<T>();

            array->items.WriteElement(index, value);
        }

        readonly int IList.Add(object? value)
        {
            throw new NotSupportedException();
        }

        readonly void IList.Clear()
        {
            Clear();
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