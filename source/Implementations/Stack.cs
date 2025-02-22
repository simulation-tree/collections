using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using Unmanaged;

namespace Collections.Implementations
{
    public unsafe struct Stack
    {
        public readonly uint stride;

        internal uint capacity;
        internal uint top;
        internal Allocation items;

        public readonly uint Capacity => capacity;
        public readonly uint Top => top;
        public readonly Allocation Items => items;

        private Stack(uint stride, uint capacity)
        {
            this.stride = stride;
            this.capacity = capacity;
            this.top = 0;
            this.items = new(stride * capacity);
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfZero(uint top)
        {
            if (top == 0)
            {
                throw new InvalidOperationException("Stack is empty");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfSizeMismatch<T>(Stack* stack) where T : unmanaged
        {
            if (stack->stride != (uint)sizeof(T))
            {
                throw new InvalidOperationException($"Stride size {stack->stride} does not match expected size of type {sizeof(T)}");
            }
        }

        public static Stack* Allocate<T>(uint initialCapacity) where T : unmanaged
        {
            ref Stack stack = ref Allocations.Allocate<Stack>();
            initialCapacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            stack = new((uint)sizeof(T), initialCapacity);
            fixed (Stack* pointer = &stack)
            {
                return pointer;
            }
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
            ThrowIfSizeMismatch<T>(stack);

            uint top = stack->top;
            if (top == stack->capacity)
            {
                stack->capacity *= 2;
                Allocation.Resize(ref stack->items, stack->capacity * (uint)sizeof(T));
            }

            stack->items.WriteElement(top, item);
            stack->top = top + 1;
        }

        public static void PushRange<T>(Stack* stack, USpan<T> items) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);
            ThrowIfSizeMismatch<T>(stack);

            if (stack->top + items.Length > stack->capacity)
            {
                stack->capacity = Allocations.GetNextPowerOf2(stack->top + items.Length);
                Allocation.Resize(ref stack->items, stack->capacity * (uint)sizeof(T));
            }

            stack->items.Write(stack->top * (uint)sizeof(T), items);
            stack->top += items.Length;
        }

        public static T Pop<T>(Stack* stack) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);
            ThrowIfZero(stack->top);
            ThrowIfSizeMismatch<T>(stack);

            uint newTop = stack->top - 1;
            stack->top = newTop;
            return stack->items.ReadElement<T>(newTop);
        }

        public static T Peek<T>(Stack* stack) where T : unmanaged
        {
            Allocations.ThrowIfNull(stack);
            ThrowIfZero(stack->top);
            ThrowIfSizeMismatch<T>(stack);

            return stack->items.ReadElement<T>(stack->top - 1);
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
            ThrowIfSizeMismatch<T>(stack);

            return stack->items.AsSpan<T>(0, stack->top);
        }
    }
}