using Collections.Generic;
using System;
using Unmanaged;

namespace Collections
{
    public static class StackExtensions
    {
        /// <summary>
        /// Checks if the stack contains <paramref name="value"/>.
        /// </summary>
        public static bool Contains<T>(this Stack<T> stack, T value) where T : unmanaged, IEquatable<T>
        {
            return stack.AsSpan().Contains(value);
        }

        /// <summary>
        /// Tries to push <paramref name="value"/> to the stack if it's not contained.
        /// </summary>
        /// <returns><see langword="true"/> if it was pushed.</returns>
        public static bool TryPush<T>(this Stack<T> stack, T value) where T : unmanaged, IEquatable<T>
        {
            bool contains = stack.Contains(value);
            if (!contains)
            {
                stack.Push(value);
            }

            return !contains;
        }
    }
}