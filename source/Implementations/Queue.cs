using System;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Implementations
{
    public unsafe struct Queue
    {
        public readonly uint stride;

        internal uint capacity;
        internal uint top;
        internal uint rear;
        internal Allocation items;

        private Queue(uint stride, uint capacity)
        {
            this.stride = stride;
            this.capacity = capacity;
            
            top = 0;
            rear = 0;
            items = new(stride * capacity);
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfZero(uint top)
        {
            if (top == 0)
            {
                throw new InvalidOperationException("Queue is empty");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfSizeMismatch<T>(Queue* queue) where T : unmanaged
        {
            if (queue->stride != (uint)sizeof(T))
            {
                throw new InvalidOperationException($"Stride size {queue->stride} does not match expected size of type {sizeof(T)}");
            }
        }

        public static Queue* Allocate<T>(uint initialCapacity) where T : unmanaged
        {
            initialCapacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            ref Queue queue = ref Allocations.Allocate<Queue>();
            queue = new((uint)sizeof(T), initialCapacity);
            fixed (Queue* pointer = &queue)
            {
                return pointer;
            }
        }

        public static void Free(ref Queue* queue)
        {
            Allocations.ThrowIfNull(queue);

            queue->items.Dispose();
            Allocations.Free(ref queue);
        }

        public static void Enqueue<T>(Queue* queue, T item) where T : unmanaged
        {
            Allocations.ThrowIfNull(queue);
            ThrowIfSizeMismatch<T>(queue);

            uint top = queue->top;
            if (top == queue->capacity)
            {
                queue->capacity *= 2;
                Allocation.Resize(ref queue->items, queue->capacity * (uint)sizeof(T));
            }

            queue->items.WriteElement(top, item);
            queue->top = top + 1;
        }

        public static void EnqueueRange<T>(Queue* queue, USpan<T> items) where T : unmanaged
        {
            Allocations.ThrowIfNull(queue);
            ThrowIfSizeMismatch<T>(queue);

            uint newTop = queue->top + items.Length;
            if (newTop > queue->capacity)
            {
                queue->capacity = Allocations.GetNextPowerOf2(newTop);
                Allocation.Resize(ref queue->items, queue->capacity * (uint)sizeof(T));
            }

            queue->items.Write(queue->top * (uint)sizeof(T), items);
            queue->top = newTop;
        }

        public static T Dequeue<T>(Queue* queue) where T : unmanaged
        {
            Allocations.ThrowIfNull(queue);
            ThrowIfSizeMismatch<T>(queue);

            uint rear = queue->rear;
            T item = queue->items.Read<T>(rear * (uint)sizeof(T));
            queue->rear = rear + 1;
            return item;
        }

        public static void Clear(Queue* queue)
        {
            Allocations.ThrowIfNull(queue);

            queue->top = 0;
            queue->rear = 0;
        }

        public static void Clear(Queue* queue, uint minimumCapacity)
        {
            Allocations.ThrowIfNull(queue);

            if (queue->capacity < minimumCapacity)
            {
                queue->capacity = Allocations.GetNextPowerOf2(minimumCapacity);
                Allocation.Resize(ref queue->items, queue->capacity * queue->stride);
            }

            queue->top = 0;
            queue->rear = 0;
        }

        public static USpan<T> AsSpan<T>(Queue* queue) where T : unmanaged
        {
            Allocations.ThrowIfNull(queue);
            ThrowIfSizeMismatch<T>(queue);

            uint length = queue->top - queue->rear;
            return queue->items.AsSpan<T>(queue->rear, length);
        }
    }
}