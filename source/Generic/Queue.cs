using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using Pointer = Collections.Pointers.Queue;

namespace Collections.Generic
{
    public unsafe struct Queue<T> : IDisposable, IReadOnlyCollection<T>, ICollection<T>, IEquatable<Queue<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Pointer* queue;

        /// <summary>
        /// Checks if this queue has been disposed.
        /// </summary>
        public readonly bool IsDisposed => queue is null;

        /// <summary>
        /// Amount of items in the queue.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                MemoryAddress.ThrowIfDefault(queue);

                return queue->top - queue->rear;
            }
        }

        /// <summary>
        /// Checks if the queue is empty.
        /// </summary>
        public readonly bool IsEmpty
        {
            get
            {
                MemoryAddress.ThrowIfDefault(queue);

                return queue->top == queue->rear;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int ICollection<T>.Count => (int)queue->top;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly bool ICollection<T>.IsReadOnly => false;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int IReadOnlyCollection<T>.Count => (int)queue->top;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Items => AsSpan().ToArray();
#if NET
        /// <summary>
        /// Creates an empty queue
        /// </summary>
        public Queue()
        {
            ref Pointer queue = ref MemoryAddress.Allocate<Pointer>();
            queue = new((uint)sizeof(T), 4);
            fixed (Pointer* pointer = &queue)
            {
                this.queue = pointer;
            }
        }
#endif

        /// <summary>
        /// Creates a queue with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Queue(uint initialCapacity = 4)
        {
            initialCapacity = Math.Max(1, initialCapacity).GetNextPowerOf2();
            ref Pointer queue = ref MemoryAddress.Allocate<Pointer>();
            queue = new((uint)sizeof(T), initialCapacity);
            fixed (Pointer* pointer = &queue)
            {
                this.queue = pointer;
            }
        }

        /// <summary>
        /// Initializes a queue from an existing <paramref name="pointer"/>.
        /// </summary>
        public Queue(void* pointer)
        {
            queue = (Pointer*)pointer;
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(queue);

            queue->items.Dispose();
            MemoryAddress.Free(ref queue);
        }

        public readonly USpan<T> AsSpan()
        {
            MemoryAddress.ThrowIfDefault(queue);

            uint length = queue->top - queue->rear;
            return queue->items.AsSpan<T>(queue->rear, length);
        }

        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(queue);

            queue->top = 0;
            queue->rear = 0;
        }

        public readonly void Clear(uint minimumCapacity)
        {
            MemoryAddress.ThrowIfDefault(queue);

            if (queue->capacity < minimumCapacity)
            {
                queue->capacity = minimumCapacity.GetNextPowerOf2();
                unchecked
                {
                    MemoryAddress.Resize(ref queue->items, queue->capacity * (uint)sizeof(T));
                }
            }

            queue->top = 0;
            queue->rear = 0;
        }

        public readonly void Enqueue(T item)
        {
            MemoryAddress.ThrowIfDefault(queue);

            uint top = queue->top;
            if (top == queue->capacity)
            {
                queue->capacity *= 2;
                unchecked
                {
                    MemoryAddress.Resize(ref queue->items, queue->capacity * (uint)sizeof(T));
                }
            }

            queue->items.WriteElement(top, item);
            queue->top = top + 1;
        }

        public readonly void EnqueueRange(USpan<T> items)
        {
            MemoryAddress.ThrowIfDefault(queue);

            unchecked
            {
                uint newTop = queue->top + items.Length;
                if (newTop > queue->capacity)
                {
                    queue->capacity = newTop.GetNextPowerOf2();
                    MemoryAddress.Resize(ref queue->items, queue->capacity * (uint)sizeof(T));
                }

                queue->items.Write(queue->top * (uint)sizeof(T), items);
                queue->top = newTop;
            }
        }

        public readonly T Dequeue()
        {
            MemoryAddress.ThrowIfDefault(queue);

            unchecked
            {
                uint rear = queue->rear;
                T item = queue->items.Read<T>(rear * (uint)sizeof(T));
                queue->rear = rear + 1;
                return item;
            }
        }

        public readonly bool TryDequeue(out T value)
        {
            MemoryAddress.ThrowIfDefault(queue);
            if (queue->top != queue->rear)
            {
                unchecked
                {
                    uint rear = queue->rear;
                    value = queue->items.Read<T>(rear * (uint)sizeof(T));
                    queue->rear = rear + 1;
                    return true;
                }
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
            return new Enumerator(queue);
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
            return queue == other.queue;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Queue<T> other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return (int)queue;
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
            private readonly Pointer* queue;
            private int index;

            public readonly T Current
            {
                get
                {
                    unchecked
                    {
                        MemoryAddress.ThrowIfDefault(queue);

                        uint length = queue->top - queue->rear;
                        return queue->items.AsSpan<T>(queue->rear, length)[(uint)index];
                    }
                }
            }

            readonly object IEnumerator.Current => Current;

            public Enumerator(Pointer* queue)
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

            readonly void IDisposable.Dispose()
            {
            }
        }
    }
}