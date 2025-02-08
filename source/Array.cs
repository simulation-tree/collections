using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using Implementation = Collections.Implementations.Array;

namespace Collections
{
    /// <summary>
    /// Native array that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Array<T> : IDisposable, IReadOnlyList<T>, IEquatable<Array<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Implementation* value;

        /// <summary>
        /// Checks if the array has been disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// Length of the array.
        /// </summary>
        public readonly uint Length
        {
            get => Implementation.GetLength(value);
            set => Implementation.Resize(this.value, value);
        }

        /// <summary>
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly ref T this[uint index] => ref Implementation.GetRef<T>(value, index);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly int IReadOnlyCollection<T>.Count => (int)Length;

        readonly T IReadOnlyList<T>.this[int index] => Implementation.GetRef<T>(value, (uint)index);

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Items => AsSpan().ToArray();

        /// <summary>
        /// Initializes an existing array from the given <paramref name="pointer"/>
        /// </summary>
        public Array(Implementation* pointer)
        {
            value = pointer;
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="length"/>.
        /// </summary>
        public Array(uint length = 0)
        {
            value = Implementation.Allocate<T>(length);
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="span"/>.
        /// </summary>
        public Array(USpan<T> span)
        {
            value = Implementation.Allocate(span);
        }

#if NET
        /// <summary>
        /// Creates an empty array.
        /// </summary>
        public Array()
        {
            value = Implementation.Allocate<T>(0);
        }
#endif

        /// <summary>
        /// Disposes the array and frees its memory.
        /// </summary>
        /// <para>Elements need to be disposed manually prior to
        /// calling this if they are allocations/disposable themselves.
        /// </para>
        public void Dispose()
        {
            Implementation.Free(ref value);
        }

        /// <summary>
        /// Resets all elements in the array back to <c>default</c> state.
        /// </summary>
        public readonly void Clear()
        {
            Implementation.Clear(value);
        }

        /// <summary>
        /// Clears <paramref name="length"/> amount of elements from this array
        /// starting at <paramref name="start"/> index.
        /// </summary>
        public readonly void Clear(uint start, uint length)
        {
            Implementation.Clear(value, start, length);
        }

        /// <summary>
        /// Fills the array with the given <paramref name="value"/>.
        /// </summary>
        public readonly void Fill(T value)
        {
            AsSpan().Fill(value);
        }

        /// <summary>
        /// Returns the array as a span.
        /// </summary>
        public readonly USpan<T> AsSpan()
        {
            return Implementation.AsSpan<T>(value);
        }

        /// <summary>
        /// Returns the array as a span starting at <paramref name="start"/> index.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start)
        {
            return AsSpan().Slice(start);
        }

        /// <summary>
        /// Returns the array as a span starting at <paramref name="start"/> index
        /// with the given <paramref name="length"/>.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start, uint length)
        {
            return AsSpan().Slice(start, length);
        }

        /// <summary>
        /// Copies the array to the given <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of elements copied.</returns>
        public readonly uint CopyTo(USpan<T> destination)
        {
            return AsSpan().CopyTo(destination);
        }

        /// <summary>
        /// Copies the given <paramref name="source"/> to this array.
        /// </summary>
        /// <returns>Amount of elements copied.</returns>
        public readonly uint CopyFrom(USpan<T> source)
        {
            return source.CopyTo(AsSpan());
        }

        /// <inheritdoc/>
        public readonly Span<T>.Enumerator GetEnumerator()
        {
            return AsSpan().GetEnumerator();
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(value);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(value);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj)
        {
            return obj is Array<T> array && Equals(array);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Array<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return value == other.value;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly Implementation* array;
            private int index;

            public readonly T Current => Implementation.GetRef<T>(array, (uint)index);

            readonly object IEnumerator.Current => Current;

            public Enumerator(Implementation* array)
            {
                this.array = array;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < Implementation.GetLength(array);
            }

            public void Reset()
            {
                index = -1;
            }

            readonly void IDisposable.Dispose()
            {
            }
        }

        public static bool operator ==(Array<T> left, Array<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Array<T> left, Array<T> right)
        {
            return !(left == right);
        }
    }
}