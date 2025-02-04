using System;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Implementations
{
    public unsafe struct Queue
    {
        public uint capacity;
        public uint top;
        public uint rear;
        public uint stride;
        public Allocation items;

        [Conditional("DEBUG")]
        private static void ThrowIfZero(uint top)
        {
            if (top == 0)
            {
                throw new InvalidOperationException("Queue is empty");
            }
        }

        public static Queue* Allocate<T>(uint initialCapacity) where T : unmanaged
        {
            ref Queue stack = ref Allocations.Allocate<Queue>();
            stack.capacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            stack.top = 0;
            stack.rear = 0;
            stack.stride = (uint)sizeof(T);
            stack.items = new(stack.capacity * stack.stride);
            fixed (Queue* pointer = &stack)
            {
                return pointer;
            }
        }

        public static void Free(ref Queue* stack)
        {
            Allocations.ThrowIfNull(stack);

            stack->items.Dispose();
            Allocations.Free(ref stack);
        }

        public static void Enqueue<T>(Queue* stack, T item) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);

            if (stack->top == stack->capacity)
            {
                stack->capacity *= 2;
                Allocation.Resize(ref stack->items, stack->capacity * stack->stride);
            }

            stack->items.Write(stack->top * stack->stride, item);
            stack->top++;
        }

        public static void EnqueueRange<T>(Queue* stack, USpan<T> items) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);

            if (stack->top + items.Length > stack->capacity)
            {
                stack->capacity = Allocations.GetNextPowerOf2(stack->top + items.Length);
                Allocation.Resize(ref stack->items, stack->capacity * stack->stride);
            }

            stack->items.Write(stack->top * stack->stride, items);
            stack->top += items.Length;
        }

        public static T Dequeue<T>(Queue* stack) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);

            T item = stack->items.Read<T>(stack->rear * stack->stride);
            stack->rear++;
            return item;
        }

        public static void Clear(Queue* stack)
        {
            Allocations.ThrowIfNull(stack);

            stack->top = 0;
            stack->rear = 0;
        }

        public static void Clear(Queue* stack, uint minimumCapacity)
        {
            Allocations.ThrowIfNull(stack);

            if (stack->capacity < minimumCapacity)
            {
                stack->capacity = Allocations.GetNextPowerOf2(minimumCapacity);
                Allocation.Resize(ref stack->items, stack->capacity * stack->stride);
            }

            stack->top = 0;
            stack->rear = 0;
        }

        public static USpan<T> AsSpan<T>(Queue* stack) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);

            uint length = stack->top - stack->rear;
            return stack->items.AsSpan<T>(stack->rear, length);
        }
    }
}