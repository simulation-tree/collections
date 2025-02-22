using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using static Collections.Implementations.Stack;
using Implementation = Collections.Implementations.Stack;

namespace Collections
{
    /// <summary>
    /// Native stack that can be used in native code.
    /// </summary>
    public unsafe struct Stack<T> : IDisposable, IReadOnlyCollection<T>, ICollection<T>, IEquatable<Stack<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Implementation* stack;

        /// <summary>
        /// Checks if this stack has been disposed.
        /// </summary>
        public readonly bool IsDisposed => stack is null;

        /// <summary>
        /// Amount of items in the stack.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                Allocations.ThrowIfNull(stack);

                return stack->top;
            }
        }

        /// <summary>
        /// Checks if the stack is empty.
        /// </summary>
        public readonly bool IsEmpty
        {
            get
            {
                Allocations.ThrowIfNull(stack);

                return stack->top == 0;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int ICollection<T>.Count => (int)stack->top;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly bool ICollection<T>.IsReadOnly => false;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int IReadOnlyCollection<T>.Count => (int)stack->top;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Items => AsSpan().ToArray();
#if NET
        /// <summary>
        /// Creates an empty stack
        /// </summary>
        public Stack()
        {
            stack = Allocate<T>(4);
        }
#endif

        /// <summary>
        /// Creates a stack with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Stack(uint initialCapacity = 4)
        {
            stack = Allocate<T>(initialCapacity);
        }

        /// <summary>
        /// Initializes a stack from an existing <paramref name="pointer"/>.
        /// </summary>
        public Stack(void* pointer)
        {
            stack = (Implementation*)pointer;
        }

        public void Dispose()
        {
            Free(ref stack);
        }

        public readonly USpan<T> AsSpan()
        {
            Allocations.ThrowIfNull(stack);

            return stack->items.AsSpan<T>(0, stack->top);
        }

        public readonly void Clear()
        {
            Allocations.ThrowIfNull(stack);

            stack->top = 0;
        }

        public readonly void Clear(uint minimumCapacity)
        {
            Allocations.ThrowIfNull(stack);

            if (stack->capacity < minimumCapacity)
            {
                stack->capacity = Allocations.GetNextPowerOf2(minimumCapacity);
                Allocation.Resize(ref stack->items, stack->capacity * (uint)sizeof(T));
            }

            stack->top = 0;
        }

        public readonly void Push(T item)
        {
            Allocations.ThrowIfNull(stack);

            uint top = stack->top;
            if (top == stack->capacity)
            {
                stack->capacity *= 2;
                Allocation.Resize(ref stack->items, stack->capacity * (uint)sizeof(T));
            }

            stack->items.WriteElement(top, item);
            stack->top = top + 1;
        }

        public readonly void PushRange(USpan<T> items)
        {
            Allocations.ThrowIfNull(stack);

            if (stack->top + items.Length > stack->capacity)
            {
                stack->capacity = Allocations.GetNextPowerOf2(stack->top + items.Length);
                Allocation.Resize(ref stack->items, stack->capacity * (uint)sizeof(T));
            }

            stack->items.Write(stack->top * (uint)sizeof(T), items);
            stack->top += items.Length;
        }

        public readonly T Pop()
        {
            Allocations.ThrowIfNull(stack);
            ThrowIfZero(stack->top);

            uint newTop = stack->top - 1;
            stack->top = newTop;
            return stack->items.ReadElement<T>(newTop);
        }

        public readonly bool TryPop(out T value)
        {
            Allocations.ThrowIfNull(stack);
            if (stack->top > 0)
            {
                ThrowIfZero(stack->top);

                uint newTop = stack->top - 1;
                stack->top = newTop;
                value = stack->items.ReadElement<T>(newTop);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public readonly T Peek()
        {
            Allocations.ThrowIfNull(stack);
            ThrowIfZero(stack->top);

            return stack->items.ReadElement<T>(stack->top - 1);
        }

        public readonly bool TryPeek(out T value)
        {
            Allocations.ThrowIfNull(stack);

            if (stack->top > 0)
            {
                value = stack->items.ReadElement<T>(stack->top - 1);
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
            Push(item);
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
            return new Enumerator(stack);
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public readonly bool Equals(Stack<T> other)
        {
            return stack == other.stack;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Stack<T> other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return (int)stack;
        }

        public static bool operator ==(Stack<T> left, Stack<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Stack<T> left, Stack<T> right)
        {
            return !left.Equals(right);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly Implementation* stack;
            private int index;

            public readonly T Current => Implementation.AsSpan<T>(stack)[(uint)index];
            readonly object IEnumerator.Current => Current;

            public Enumerator(Implementation* stack)
            {
                this.stack = stack;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < stack->top;
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