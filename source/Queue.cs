using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using Implementation = Collections.Implementations.Queue;

namespace Collections
{
    public unsafe struct Queue<T> : IDisposable, IReadOnlyCollection<T>, ICollection<T>, IEquatable<Queue<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Implementation* implementation;

        /// <summary>
        /// Checks if this queue has been disposed.
        /// </summary>
        public readonly bool IsDisposed => implementation is null;

        /// <summary>
        /// Amount of items in the queue.
        /// </summary>
        public readonly uint Count => implementation->top - implementation->rear;

        /// <summary>
        /// Checks if the queue is empty.
        /// </summary>
        public readonly bool IsEmpty => implementation->top == implementation->rear;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int ICollection<T>.Count => (int)implementation->top;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly bool ICollection<T>.IsReadOnly => false;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int IReadOnlyCollection<T>.Count => (int)implementation->top;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Items => AsSpan().ToArray();
#if NET
        /// <summary>
        /// Creates an empty queue
        /// </summary>
        public Queue()
        {
            implementation = Implementation.Allocate<T>(4);
        }
#endif

        /// <summary>
        /// Creates a queue with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Queue(uint initialCapacity = 4)
        {
            implementation = Implementation.Allocate<T>(initialCapacity);
        }

        /// <summary>
        /// Initializes a queue from an existing <paramref name="pointer"/>.
        /// </summary>
        public Queue(void* pointer)
        {
            implementation = (Implementation*)pointer;
        }

        public void Dispose()
        {
            Implementation.Free(ref implementation);
        }

        public readonly USpan<T> AsSpan()
        {
            return Implementation.AsSpan<T>(implementation);
        }

        public readonly void Clear()
        {
            Implementation.Clear(implementation);
        }

        public readonly void Clear(uint minimumCapacity)
        {
            Implementation.Clear(implementation, minimumCapacity);
        }

        public readonly void Enqueue(T item)
        {
            Implementation.Enqueue(implementation, item);
        }

        public readonly void EnqueueRange(USpan<T> items)
        {
            Implementation.EnqueueRange(implementation, items);
        }

        public readonly T Dequeue()
        {
            return Implementation.Dequeue<T>(implementation);
        }

        public readonly bool TryDequeue(out T value)
        {
            if (!IsEmpty)
            {
                value = Implementation.Dequeue<T>(implementation);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        readonly void ICollection<T>.Add(T item)
        {
            Enqueue(item);
        }

        readonly bool ICollection<T>.Contains(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            USpan<T> span = AsSpan();
            for (uint i = 0; i < span.Length; i++)
            {
                if (comparer.Equals(span[i], item))
                {
                    return true;
                }
            }

            return false;
        }

        readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            USpan<T> span = AsSpan();
            for (uint i = 0; i < span.Length; i++)
            {
                array[arrayIndex + i] = span[i];
            }
        }

        readonly bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(implementation);
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public readonly bool Equals(Queue<T> other)
        {
            return implementation == other.implementation;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Queue<T> other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return (int)implementation;
        }

        public static bool operator ==(Queue<T> left, Queue<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Queue<T> left, Queue<T> right)
        {
            return !left.Equals(right);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly Implementation* queue;
            private int index;

            public readonly T Current => Implementation.AsSpan<T>(queue)[(uint)index];
            readonly object IEnumerator.Current => Current;

            public Enumerator(Implementation* queue)
            {
                this.queue = queue;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < queue->top;
            }

            public void Reset()
            {
                index = -1;
            }

            readonly void System.IDisposable.Dispose()
            {
            }
        }
    }
}