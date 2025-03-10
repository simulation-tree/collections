using System;

namespace Collections.Generic
{
    public static class QueueExtensions
    {
        /// <summary>
        /// Checks if the queue contains <paramref name="value"/>.
        /// </summary>
        public static bool Contains<T>(this Queue<T> queue, T value) where T : unmanaged, IEquatable<T>
        {
            return queue.AsSpan().Contains(value);
        }

        /// <summary>
        /// Tries to enqueue <paramref name="value"/> to the queue if it's not contained.
        /// </summary>
        /// <returns><see langword="true"/> if it was enqueued.</returns>
        public static bool TryEnqueue<T>(this Queue<T> queue, T value) where T : unmanaged, IEquatable<T>
        {
            bool contains = queue.Contains(value);
            if (!contains)
            {
                queue.Enqueue(value);
            }

            return !contains;
        }
    }
}