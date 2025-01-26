using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Implementations
{
    public unsafe struct Stack
    {
        public uint capacity;
        public uint top;
        public uint stride;
        public Allocation items;

        [Conditional("DEBUG")]
        private static void ThrowIfZero(uint top)
        {
            if (top == 0)
            {
                throw new InvalidOperationException("Stack is empty");
            }
        }

        public static Stack* Allocate<T>(uint initialCapacity) where T : unmanaged
        {
            Stack* stack = Allocations.Allocate<Stack>();
            stack->capacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            stack->top = 0;
            stack->stride = (uint)sizeof(T);
            stack->items = new(stack->capacity * stack->stride);
            return stack;
        }

        public static void Free(ref Stack* stack)
        {
            Allocations.ThrowIfNull(stack);

            stack->items.Dispose();
            Allocations.Free(ref stack);
        }

        public static void Push<T>(Stack* stack, T item) where T : unmanaged
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

        public static void PushRange<T>(Stack* stack, USpan<T> items) where T : unmanaged
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

        public static T Pop<T>(Stack* stack) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);
            ThrowIfZero(stack->top);

            stack->top--;
            return stack->items.Read<T>(stack->top * stack->stride);
        }

        public static T Peek<T>(Stack* stack) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);
            ThrowIfZero(stack->top);

            return stack->items.Read<T>((stack->top - 1) * stack->stride);
        }

        public static void Clear(Stack* stack)
        {
            Allocations.ThrowIfNull(stack);

            stack->top = 0;
        }

        public static void Clear(Stack* stack, uint minimumCapacity)
        {
            Allocations.ThrowIfNull(stack);

            if (stack->capacity < minimumCapacity)
            {
                stack->capacity = Allocations.GetNextPowerOf2(minimumCapacity);
                Allocation.Resize(ref stack->items, stack->capacity * stack->stride);
            }

            stack->top = 0;
        }

        public static USpan<T> AsSpan<T>(Stack* stack) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);

            return stack->items.AsSpan<T>(0, stack->top);
        }
    }
}