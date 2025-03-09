using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using Pointer = Collections.Pointers.Stack;

namespace Collections.Generic
{
    /// <summary>
    /// Native stack that can be used in native code.
    /// </summary>
    public unsafe struct Stack<T> : IDisposable, IReadOnlyCollection<T>, ICollection<T>, IEquatable<Stack<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Pointer* stack;

        /// <summary>
        /// Checks if this stack has been disposed.
        /// </summary>
        public readonly bool IsDisposed => stack is null;

        /// <summary>
        /// Amount of items in the stack.
        /// </summary>
        public readonly int Count
        {
            get
            {
                MemoryAddress.ThrowIfDefault(stack);

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
                MemoryAddress.ThrowIfDefault(stack);

                return stack->top == 0;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly bool ICollection<T>.IsReadOnly => false;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Items => AsSpan().ToArray();
#if NET
        /// <summary>
        /// Creates an empty stack
        /// </summary>
        public Stack()
        {
            ref Pointer stack = ref MemoryAddress.Allocate<Pointer>();
            stack = new(sizeof(T), 4);
            fixed (Pointer* pointer = &stack)
            {
                this.stack = pointer;
            }
        }
#endif

        /// <summary>
        /// Creates a stack with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Stack(int initialCapacity = 4)
        {
            ref Pointer stack = ref MemoryAddress.Allocate<Pointer>();
            initialCapacity = Math.Max(1, initialCapacity).GetNextPowerOf2();
            stack = new(sizeof(T), initialCapacity);
            fixed (Pointer* pointer = &stack)
            {
                this.stack = pointer;
            }
        }

        /// <summary>
        /// Initializes a stack from an existing <paramref name="pointer"/>.
        /// </summary>
        public Stack(void* pointer)
        {
            stack = (Pointer*)pointer;
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(stack);

            stack->items.Dispose();
            MemoryAddress.Free(ref stack);
        }

        public readonly Span<T> AsSpan()
        {
            MemoryAddress.ThrowIfDefault(stack);

            return new(stack->items.Pointer, stack->top);
        }

        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(stack);

            stack->top = 0;
        }

        public readonly void Clear(int minimumCapacity)
        {
            MemoryAddress.ThrowIfDefault(stack);

            if (stack->capacity < minimumCapacity)
            {
                stack->capacity = minimumCapacity.GetNextPowerOf2();
                MemoryAddress.Resize(ref stack->items, stack->capacity * sizeof(T));
            }

            stack->top = 0;
        }

        public readonly void Push(T item)
        {
            MemoryAddress.ThrowIfDefault(stack);

            int top = stack->top;
            if (top == stack->capacity)
            {
                stack->capacity *= 2;
                MemoryAddress.Resize(ref stack->items, stack->capacity * sizeof(T));
            }

            stack->items.WriteElement(top, item);
            stack->top = top + 1;
        }

        public readonly void PushRange(ReadOnlySpan<T> items)
        {
            MemoryAddress.ThrowIfDefault(stack);

            if (stack->top + items.Length > stack->capacity)
            {
                stack->capacity = stack->top + items.Length.GetNextPowerOf2();
                MemoryAddress.Resize(ref stack->items, stack->capacity * sizeof(T));
            }

            stack->items.Write(stack->top * sizeof(T), items);
            stack->top += items.Length;
        }

        public readonly T Pop()
        {
            MemoryAddress.ThrowIfDefault(stack);
            ThrowIfZero(stack->top);

            int newTop = stack->top - 1;
            stack->top = newTop;
            return stack->items.ReadElement<T>(newTop);
        }

        public readonly bool TryPop(out T value)
        {
            MemoryAddress.ThrowIfDefault(stack);
            if (stack->top > 0)
            {
                int newTop = stack->top - 1;
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
            MemoryAddress.ThrowIfDefault(stack);
            ThrowIfZero(stack->top);

            return stack->items.ReadElement<T>(stack->top - 1);
        }

        public readonly bool TryPeek(out T value)
        {
            MemoryAddress.ThrowIfDefault(stack);

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
            Span<T> span = AsSpan();
            for (int i = 0; i < span.Length; i++)
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
            Span<T> span = AsSpan();
            for (int i = 0; i < span.Length; i++)
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

        [Conditional("DEBUG")]
        private static void ThrowIfZero(int value)
        {
            if (value == 0)
            {
                throw new InvalidOperationException("Stack is empty");
            }
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
            private readonly Pointer* stack;
            private int index;

            public readonly T Current
            {
                get
                {
                    MemoryAddress.ThrowIfDefault(stack);

                    return new Span<T>(stack->items.Pointer, stack->top)[index];
                }
            }

            readonly object IEnumerator.Current => Current;

            public Enumerator(Pointer* stack)
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