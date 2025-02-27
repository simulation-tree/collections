using Collections.Generic;
using System;
using Unmanaged;

namespace Collections
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Checks if the array contains <paramref name="value"/>.
        /// </summary>
        public unsafe static bool Contains<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                return new Span<T>(array.Items.Pointer, (int)array.Length).Contains(value);
            }
        }

        /// <summary>
        /// Retrieves the index for the first occurrence of <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public unsafe static uint IndexOf<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                return (uint)new Span<T>(array.Items.Pointer, (int)array.Length).IndexOf(value);
            }
        }

        /// <summary>
        /// Retrieves the index for the last occurrence of <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public unsafe static uint LastIndexOf<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                return (uint)new Span<T>(array.Items.Pointer, (int)array.Length).LastIndexOf(value);
            }
        }

        /// <summary>
        /// Tries to retrieve the index for the first occurrence of <paramref name="value"/>.
        /// </summary>
        public unsafe static bool TryIndexOf<T>(this Array<T> array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                index = (uint)new Span<T>(array.Items.Pointer, (int)array.Length).IndexOf(value);
                return index != uint.MaxValue;
            }
        }

        /// <summary>
        /// Tries to retrieve the index for the last occurrence of <paramref name="value"/>.
        /// </summary>
        public unsafe static bool TryLastIndexOf<T>(this Array<T> array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                index = (uint)new Span<T>(array.Items.Pointer, (int)array.Length).LastIndexOf(value);
                return index != uint.MaxValue;
            }
        }
    }
}