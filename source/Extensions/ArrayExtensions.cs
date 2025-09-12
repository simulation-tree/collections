using System;

namespace Collections.Generic
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Checks if the array contains <paramref name="value"/>.
        /// </summary>
        public unsafe static bool Contains<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            return new Span<T>(array.Items.pointer, array.Length).Contains(value);
        }

        /// <summary>
        /// Retrieves the index for the first occurrence of <paramref name="value"/>.
        /// </summary>
        public unsafe static int IndexOf<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            return new Span<T>(array.Items.pointer, array.Length).IndexOf(value);
        }

        /// <summary>
        /// Retrieves the index for the last occurrence of <paramref name="value"/>.
        /// </summary>
        public unsafe static int LastIndexOf<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            return new Span<T>(array.Items.pointer, array.Length).LastIndexOf(value);
        }

        /// <summary>
        /// Tries to retrieve the index for the first occurrence of <paramref name="value"/>.
        /// </summary>
        public unsafe static bool TryIndexOf<T>(this Array<T> array, T value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = new Span<T>(array.Items.pointer, array.Length).IndexOf(value);
            return index != -1;
        }

        /// <summary>
        /// Tries to retrieve the index for the last occurrence of <paramref name="value"/>.
        /// </summary>
        public unsafe static bool TryLastIndexOf<T>(this Array<T> array, T value, out int index) where T : unmanaged, IEquatable<T>
        {
            index = new Span<T>(array.Items.pointer, array.Length).LastIndexOf(value);
            return index != -1;
        }
    }
}