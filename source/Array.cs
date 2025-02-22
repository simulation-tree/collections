using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using static Collections.Implementations.Array;
using Implementation = Collections.Implementations.Array;

namespace Collections
{
    /// <summary>
    /// Native array that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Array<T> : IDisposable, IReadOnlyList<T>, IEquatable<Array<T>> where T : unmanaged
    {
        private static readonly uint Stride = (uint)sizeof(T);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Implementation* array;

        /// <summary>
        /// Checks if the array has been disposed.
        /// </summary>
        public readonly bool IsDisposed => array is null;

        /// <summary>
        /// Length of the array.
        /// </summary>
        public readonly uint Length
        {
            get
            {
                Allocations.ThrowIfNull(array);

                return array->length;
            }
            set
            {
                Allocations.ThrowIfNull(array);

                Resize(array, value);
            }
        }

        /// <summary>
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly ref T this[uint index]
        {
            get
            {
                Allocations.ThrowIfNull(array);
                ThrowIfOutOfRange(array, index);

                return ref array->Items.ReadElement<T>(index);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly int IReadOnlyCollection<T>.Count => (int)Length;

        readonly T IReadOnlyList<T>.this[int index]
        {
            get
            {
                Allocations.ThrowIfNull(array);
                ThrowIfOutOfRange(array, (uint)index);

                return array->Items.ReadElement<T>((uint)index);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Items => AsSpan().ToArray();

        /// <summary>
        /// Initializes an existing array from the given <paramref name="pointer"/>
        /// </summary>
        public Array(Implementation* pointer)
        {
            array = pointer;
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="length"/>.
        /// </summary>
        public Array(uint length = 0, bool clear = true)
        {
            array = Allocate<T>(length, clear);
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="span"/>.
        /// </summary>
        public Array(USpan<T> span)
        {
            array = Allocate(span);
        }

#if NET
        /// <summary>
        /// Creates an empty array.
        /// </summary>
        public Array()
        {
            array = Allocate<T>(0, false);
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
            Free(ref array);
        }

        /// <summary>
        /// Resets all elements in the array back to <c>default</c> state.
        /// </summary>
        public readonly void Clear()
        {
            Allocations.ThrowIfNull(array);

            array->Items.Clear(array->Length * Stride);
        }

        /// <summary>
        /// Clears <paramref name="length"/> amount of elements from this array
        /// starting at <paramref name="start"/> index.
        /// </summary>
        public readonly void Clear(uint start, uint length)
        {
            Allocations.ThrowIfNull(array);

            array->Items.Clear(start * Stride, length * Stride);
        }

        /// <summary>
        /// Fills the array with the given <paramref name="value"/>.
        /// </summary>
        public readonly void Fill(T value)
        {
            Allocations.ThrowIfNull(array);

            array->Items.AsSpan<T>(0, array->Length).Fill(value);
        }

        /// <summary>
        /// Returns the array as a span.
        /// </summary>
        public readonly USpan<T> AsSpan()
        {
            Allocations.ThrowIfNull(array);

            return array->Items.AsSpan<T>(0, array->Length);
        }

        /// <summary>
        /// Returns the array as a span starting at <paramref name="start"/> index.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start)
        {
            Allocations.ThrowIfNull(array);

            return array->Items.AsSpan<T>(start, array->Length - start);
        }

        /// <summary>
        /// Returns the array as a span starting at <paramref name="start"/> index
        /// with the given <paramref name="length"/>.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start, uint length)
        {
            Allocations.ThrowIfNull(array);

            return array->Items.AsSpan<T>(start, length);
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
            return new Enumerator(array);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(array);
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

            return array == other.array;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return ((nint)array).GetHashCode();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly Implementation* array;
            private int index;

            public readonly T Current => array->Items.Read<T>((uint)index * Stride);

            readonly object IEnumerator.Current => Current;

            public Enumerator(Implementation* array)
            {
                this.array = array;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < array->Length;
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