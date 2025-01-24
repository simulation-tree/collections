using System;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Implementations
{
    /// <summary>
    /// Opaque pointer implementation of an array.
    /// </summary>
    public unsafe struct Array
    {
        private uint stride;
        private uint length;
        private Allocation items;

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(Array* array, uint index)
        {
            if (index >= array->length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for array of length {array->length}");
            }
        }

        public static void Free(ref Array* array)
        {
            Allocations.ThrowIfNull(array);

            array->items.Dispose();
            Allocations.Free(ref array);
        }

        public static uint GetLength(Array* array)
        {
            Allocations.ThrowIfNull(array);

            return array->length;
        }

        public static nint GetStartAddress(Array* array)
        {
            Allocations.ThrowIfNull(array);

            return array->items.Address;
        }

        public static Array* Allocate<T>(uint length) where T : unmanaged
        {
            return Allocate(length, (uint)sizeof(T));
        }

        public static Array* Allocate(uint length, uint stride)
        {
            Array* array = Allocations.Allocate<Array>();
            array->stride = stride;
            array->length = length;
            array->items = new(stride * length, true);
            return array;
        }

        public static Array* Allocate<T>(USpan<T> span) where T : unmanaged
        {
            Array* array = Allocations.Allocate<Array>();
            array->stride = (uint)sizeof(T);
            array->length = span.Length;
            array->items = Allocation.Create(span);
            return array;
        }

        public static ref T GetRef<T>(Array* array, uint index) where T : unmanaged
        {
            Allocations.ThrowIfNull(array);
            ThrowIfOutOfRange(array, index);

            T* ptr = (T*)GetStartAddress(array);
            return ref ptr[index];
        }

        public static USpan<T> AsSpan<T>(Array* array) where T : unmanaged
        {
            Allocations.ThrowIfNull(array);

            return array->items.AsSpan<T>(0, array->length);
        }

        public static bool TryIndexOf<T>(Array* array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(array);

            USpan<T> span = AsSpan<T>(array);
            return span.TryIndexOf(value, out index);
        }

        /// <summary>
        /// Checks if the array contains the given <paramref name="value"/>.
        /// </summary>
        public static bool Contains<T>(Array* array, T value) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(array);

            USpan<T> span = AsSpan<T>(array);
            return span.Contains(value);
        }

        /// <summary>
        /// Resizes the array and optionally initializes new elements.
        /// </summary>
        public static void Resize(Array* array, uint newLength, bool initialize = false)
        {
            Allocations.ThrowIfNull(array);

            if (array->length != newLength)
            {
                uint stride = array->stride;
                uint oldLength = array->length;
                Allocation.Resize(ref array->items, stride * newLength);
                array->length = newLength;

                if (initialize && newLength > oldLength)
                {
                    array->items.Clear(stride * oldLength, stride * (newLength - oldLength));
                }
            }
        }

        /// <summary>
        /// Clears the entire array to <c>default</c> state.
        /// </summary>
        public static void Clear(Array* array)
        {
            Allocations.ThrowIfNull(array);

            array->items.Clear(array->length * array->stride);
        }

        /// <summary>
        /// Clears a range of elements in the array to <c>default</c> state.
        /// </summary>
        public static void Clear(Array* array, uint start, uint length)
        {
            Allocations.ThrowIfNull(array);

            array->items.Clear(start * array->stride, length * array->stride);
        }
    }
}