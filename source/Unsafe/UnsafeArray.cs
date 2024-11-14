using System;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Unsafe
{
    /// <summary>
    /// Opaque pointer implementation of an array.
    /// </summary>
    public unsafe struct UnsafeArray
    {
        private uint stride;
        private uint length;
        private Allocation items;

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(UnsafeArray* array, uint index)
        {
            if (index >= array->length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for array of length {array->length}");
            }
        }

        /// <inheritdoc/>
        public static void Free(ref UnsafeArray* array)
        {
            Allocations.ThrowIfNull(array);

            array->items.Dispose();
            Allocations.Free(ref array);
            array = null;
        }

        /// <inheritdoc/>
        public static uint GetLength(UnsafeArray* array)
        {
            Allocations.ThrowIfNull(array);

            return array->length;
        }

        /// <inheritdoc/>
        public static nint GetStartAddress(UnsafeArray* array)
        {
            Allocations.ThrowIfNull(array);

            return array->items.Address;
        }

        /// <inheritdoc/>
        public static UnsafeArray* Allocate<T>(uint length) where T : unmanaged
        {
            return Allocate(length, TypeInfo<T>.size);
        }

        /// <inheritdoc/>
        public static UnsafeArray* Allocate(uint length, uint stride)
        {
            UnsafeArray* array = Allocations.Allocate<UnsafeArray>();
            array->stride = stride;
            array->length = length;
            array->items = new(stride * length, true);
            return array;
        }

        /// <inheritdoc/>
        public static UnsafeArray* Allocate<T>(USpan<T> span) where T : unmanaged
        {
            UnsafeArray* array = Allocations.Allocate<UnsafeArray>();
            array->stride = TypeInfo<T>.size;
            array->length = span.Length;
            array->items = Allocation.Create(span);
            return array;
        }

        /// <inheritdoc/>
        public static ref T GetRef<T>(UnsafeArray* array, uint index) where T : unmanaged
        {
            Allocations.ThrowIfNull(array);
            ThrowIfOutOfRange(array, index);

            T* ptr = (T*)GetStartAddress(array);
            return ref ptr[index];
        }

        /// <inheritdoc/>
        public static USpan<T> AsSpan<T>(UnsafeArray* array) where T : unmanaged
        {
            Allocations.ThrowIfNull(array);

            return array->items.AsSpan<T>(0, array->length);
        }

        /// <inheritdoc/>
        public static bool TryIndexOf<T>(UnsafeArray* array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(array);

            USpan<T> span = AsSpan<T>(array);
            return span.TryIndexOf(value, out index);
        }

        /// <summary>
        /// Checks if the array contains the given <paramref name="value"/>.
        /// </summary>
        public static bool Contains<T>(UnsafeArray* array, T value) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(array);

            USpan<T> span = AsSpan<T>(array);
            return span.Contains(value);
        }

        /// <summary>
        /// Resizes the array and optionally initializes new elements.
        /// </summary>
        public static void Resize(UnsafeArray* array, uint newLength, bool initialize = false)
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
        public static void Clear(UnsafeArray* array)
        {
            Allocations.ThrowIfNull(array);

            array->items.Clear(array->length * array->stride);
        }

        /// <summary>
        /// Clears a range of elements in the array to <c>default</c> state.
        /// </summary>
        public static void Clear(UnsafeArray* array, uint start, uint length)
        {
            Allocations.ThrowIfNull(array);

            array->items.Clear(start * array->stride, length * array->stride);
        }
    }
}
