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
        public readonly uint stride;

        internal uint length;
        internal Allocation items;

        public readonly uint Length => length;
        public readonly Allocation Items => items;

        private Array(uint stride, uint length, Allocation items)
        {
            this.stride = stride;
            this.length = length;
            this.items = items;
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfOutOfRange(Array* array, uint index)
        {
            if (index >= array->length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for array of length {array->length}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfStrideSizeMismatch<T>(Array* array) where T : unmanaged
        {
            if (array->stride != (uint)sizeof(T))
            {
                throw new InvalidOperationException($"Stride size {array->stride} does not match expected size of type {sizeof(T)}");
            }
        }

        public static void Free(ref Array* array)
        {
            Allocations.ThrowIfNull(array);

            array->items.Dispose();
            Allocations.Free(ref array);
        }

        public static Array* Allocate<T>(uint length, bool clear) where T : unmanaged
        {
            return Allocate(length, (uint)sizeof(T), clear);
        }

        public static Array* Allocate(uint length, uint stride, bool clear)
        {
            ref Array array = ref Allocations.Allocate<Array>();
            array = new(stride, length, new(stride * length, clear));
            fixed (Array* pointer = &array)
            {
                return pointer;
            }
        }

        public static Array* Allocate<T>(USpan<T> span) where T : unmanaged
        {
            ref Array array = ref Allocations.Allocate<Array>();
            array = new((uint)sizeof(T), span.Length, Allocation.Create(span));
            fixed (Array* pointer = &array)
            {
                return pointer;
            }
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
    }
}