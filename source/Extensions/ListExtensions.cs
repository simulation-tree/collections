using System;
using Unmanaged;

namespace Collections
{
    public static class ListExtensions
    {
        public static bool Contains<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            return list.AsSpan().Contains(value);
        }

        public static uint IndexOf<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            return list.AsSpan().IndexOf(value);
        }

        public static uint LastIndexOf<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            return list.AsSpan().LastIndexOf(value);
        }

        public static bool TryIndexOf<T>(this List<T> list, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            return list.AsSpan().TryIndexOf(value, out index);
        }

        public static bool TryLastIndexOf<T>(this List<T> list, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            return list.AsSpan().TryLastIndexOf(value, out index);
        }

        public static bool TryAdd<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            bool contains = list.Contains(value);
            if (!contains)
            {
                list.Add(value);
            }

            return !contains;
        }

        public static bool TryRemove<T>(this List<T> list, T value) where T : unmanaged, IEquatable<T>
        {
            bool contains = list.TryIndexOf(value, out uint index);
            if (contains)
            {
                list.RemoveAt(index);
            }

            return contains;
        }

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