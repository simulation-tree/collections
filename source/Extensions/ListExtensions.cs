using Collections.Generic;
using System;
using Unmanaged;

namespace Collections
{
    public static class ListExtensions
    {
        /// <summary>
        /// Checks if the list contains <paramref name="value"/>.
        /// </summary>
        public unsafe static bool Contains<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                return new Span<T>(list.Items.Pointer, (int)list.Count).Contains(value);
            }
        }

        /// <summary>
        /// Retrieves the index for the first occurrence of <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public unsafe static uint IndexOf<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                return (uint)new Span<T>(list.Items.Pointer, (int)list.Count).IndexOf(value);
            }
        }

        /// <summary>
        /// Retrieves the index for the last occurrence of <paramref name="value"/>.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public unsafe static uint LastIndexOf<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                return (uint)new Span<T>(list.Items.Pointer, (int)list.Count).LastIndexOf(value);
            }
        }

        /// <summary>
        /// Tries to retrieve the index for the first occurrence of <paramref name="value"/>.
        /// </summary>
        public unsafe static bool TryIndexOf<T>(this List<T> list, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                index = (uint)new Span<T>(list.Items.Pointer, (int)list.Count).IndexOf(value);
                return index != uint.MaxValue;
            }
        }

        /// <summary>
        /// Tries to retrieve the index for the last occurrence of <paramref name="value"/>.
        /// </summary>
        public unsafe static bool TryLastIndexOf<T>(this List<T> list, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            unchecked
            {
                index = (uint)new Span<T>(list.Items.Pointer, (int)list.Count).LastIndexOf(value);
                return index != uint.MaxValue;
            }
        }

        /// <summary>
        /// Tries to add <paramref name="value"/> to the list if it's not contained.
        /// </summary>
        /// <returns><c>true</c> if it was added.</returns>
        public static bool TryAdd<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            bool contains = list.Contains(value);
            if (!contains)
            {
                list.Add(value);
            }

            return !contains;
        }

        /// <summary>
        /// Tries to remove <paramref name="value"/> from the list if it's present.
        /// </summary>
        /// <returns><c>true</c> if it was removed.</returns>
        public static bool TryRemove<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            bool contains = list.TryIndexOf(value, out uint index);
            if (contains)
            {
                list.RemoveAt(index);
            }

            return contains;
        }

        /// <summary>
        /// Tries to remove <paramref name="value"/> from the list,
        /// by swapping it with the last element if it's present.
        /// </summary>
        /// <returns><c>true</c> if it was removed.</returns>
        public static bool TryRemoveBySwapping<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            bool contains = list.TryIndexOf(value, out uint index);
            if (contains)
            {
                list.RemoveAtBySwapping(index);
            }

            return contains;
        }
    }
}