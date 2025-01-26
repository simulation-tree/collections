using System;
using Unmanaged;

namespace Collections
{
    public static class ArrayExtensions
    {
        public static bool Contains<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().Contains(value);
        }

        public static uint IndexOf<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().IndexOf(value);
        }

        public static uint LastIndexOf<T>(this Array<T> array, T value) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().LastIndexOf(value);
        }

        public static bool TryIndexOf<T>(this Array<T> array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().TryIndexOf(value, out index);
        }

        public static bool TryLastIndexOf<T>(this Array<T> array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            return array.AsSpan().TryLastIndexOf(value, out index);
        }
    }
}