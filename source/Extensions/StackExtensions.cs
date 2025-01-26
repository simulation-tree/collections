using System;
using Unmanaged;

namespace Collections
{
    public static class StackExtensions
    {
        public static bool Contains<T>(this Stack<T> stack, T value) where T : unmanaged, IEquatable<T>
        {
            return stack.AsSpan().Contains(value);
        }
    }
}