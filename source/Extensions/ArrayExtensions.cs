using System;
using Unmanaged;

namespace Collections
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Checks if the array contains <paramref name="value"/>.
        /// </summary>
        public static bool Contains<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().Contains(value);
        }

        /// <summary>
        /// Retrieves the index for the first occurrence of <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public static uint IndexOf<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().IndexOf(value);
        }

        /// <summary>
        /// Retrieves the index for the last occurrence of <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public static uint LastIndexOf<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().LastIndexOf(value);
        }

        /// <summary>
        /// Tries to retrieve the index for the first occurrence of <paramref name="value"/>.
        /// </summary>
        public static bool TryIndexOf<T>(this Array<T> array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().TryIndexOf(value, out index);
        }

        /// <summary>
        /// Tries to retrieve the index for the last occurrence of <paramref name="value"/>.
        /// </summary>
        public static bool TryLastIndexOf<T>(this Array<T> array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().TryLastIndexOf(value, out index);
        }
    }
}