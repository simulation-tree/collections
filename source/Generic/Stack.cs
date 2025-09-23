using Collections.Pointers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Collections.Generic
{
    /// <summary>
    /// Native stack that can be used in native code.
    /// </summary>
    public unsafe struct Stack<T> : IDisposable, IReadOnlyCollection<T>, ICollection<T>, IEquatable<Stack<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private StackPointer* stack;

        /// <summary>
        /// Checks if this stack has been disposed.
        /// </summary>
        public readonly bool IsDisposed => stack is null;

        /// <summary>
        /// Amount of items in the stack.
        /// </summary>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            stack = MemoryAddress.AllocatePointer<StackPointer>();
            stack->items = MemoryAddress.Allocate(sizeof(T) * 4);
            stack->capacity = 4;
            stack->stride = sizeof(T);
            stack->top = 0;
        }
#endif

        /// <summary>
        /// Creates a stack with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Stack(int initialCapacity)
        {
            initialCapacity = Math.Max(4, initialCapacity).GetNextPowerOf2();
            stack = MemoryAddress.AllocatePointer<StackPointer>();
            stack->items = MemoryAddress.Allocate(sizeof(T) * initialCapacity);
            stack->capacity = initialCapacity;
            stack->stride = sizeof(T);
            stack->top = 0;
        }

        /// <summary>
        /// Initializes a stack from an existing <paramref name="pointer"/>.
        /// </summary>
        public Stack(void* pointer)
        {
            stack = (StackPointer*)pointer;
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(stack);

            stack->items.Dispose();
            MemoryAddress.Free(ref stack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan()
        {
            MemoryAddress.ThrowIfDefault(stack);

            return new(stack->items.pointer, stack->top);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            private readonly StackPointer* stack;
            private int index;

            public readonly T Current
            {
                get
                {
                    MemoryAddress.ThrowIfDefault(stack);

                    return new Span<T>(stack->items.pointer, stack->top)[index];
                }
            }

            readonly object IEnumerator.Current => Current;

            public Enumerator(StackPointer* stack)
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