using System;
using System.Collections;
using System.Collections.Generic;
using Unmanaged;
using Implementation = Collections.Implementations.Stack;

namespace Collections
{
    /// <summary>
    /// Native stack that can be used in native code.
    /// </summary>
    public unsafe struct Stack<T> : IDisposable, IReadOnlyCollection<T>, ICollection<T>, IEquatable<Stack<T>> where T : unmanaged
    {
        private Implementation* implementation;

        /// <summary>
        /// Checks if this stack has been disposed.
        /// </summary>
        public readonly bool IsDisposed => implementation is null;

        /// <summary>
        /// Amount of items in the stack.
        /// </summary>
        public readonly uint Count => implementation->top;

        /// <summary>
        /// Checks if the stack is empty.
        /// </summary>
        public readonly bool IsEmpty => implementation->top == 0;

        int ICollection<T>.Count => (int)implementation->top;
        readonly bool ICollection<T>.IsReadOnly => false;
        int IReadOnlyCollection<T>.Count => (int)implementation->top;

#if NET
        /// <summary>
        /// Creates an empty stack
        /// </summary>
        public Stack()
        {
            implementation = Implementation.Allocate<T>(4);
        }
#endif

        /// <summary>
        /// Creates a stack with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Stack(uint initialCapacity = 4)
        {
            implementation = Implementation.Allocate<T>(initialCapacity);
        }

        /// <summary>
        /// Initializes a stack from an existing <paramref name="pointer"/>.
        /// </summary>
        public Stack(void* pointer)
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

        public readonly void Push(T item)
        {
            Implementation.Push(implementation, item);
        }

        public readonly void PushRange(USpan<T> items)
        {
            Implementation.PushRange(implementation, items);
        }

        public readonly T Pop()
        {
            return Implementation.Pop<T>(implementation);
        }

        public readonly bool TryPop(out T value)
        {
            if (implementation->top > 0)
            {
                value = Implementation.Pop<T>(implementation);
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
            return Implementation.Peek<T>(implementation);
        }

        public readonly bool TryPeek(out T value)
        {
            if (implementation->top > 0)
            {
                value = Implementation.Peek<T>(implementation);
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

        public readonly bool Equals(Stack<T> other)
        {
            return implementation == other.implementation;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Stack<T> other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return (int)implementation;
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