using Collections.Unsafe;
using System;
using System.Collections;
using System.Collections.Generic;
using Unmanaged;

namespace Collections
{
    /// <summary>
    /// Native array that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Array<T> : IDisposable, IReadOnlyList<T>, IEquatable<Array<T>> where T : unmanaged
    {
        private UnsafeArray* value;

        /// <summary>
        /// Checks if the array has been disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// Length of the array.
        /// </summary>
        public readonly uint Length
        {
            get => UnsafeArray.GetLength(value);
            set => UnsafeArray.Resize(this.value, value);
        }

        /// <summary>
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly ref T this[uint index] => ref UnsafeArray.GetRef<T>(value, index);

        readonly int IReadOnlyCollection<T>.Count => (int)Length;
        readonly T IReadOnlyList<T>.this[int index] => UnsafeArray.GetRef<T>(value, (uint)index);

        /// <summary>
        /// Initializes an existing array from the given <paramref name="pointer"/>
        /// </summary>
        public Array(UnsafeArray* pointer)
        {
            value = pointer;
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="length"/>.
        /// </summary>
        public Array(uint length = 0)
        {
            value = UnsafeArray.Allocate<T>(length);
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="span"/>.
        /// </summary>
        public Array(USpan<T> span)
        {
            value = UnsafeArray.Allocate(span);
        }

        /// <summary>
        /// Creates a new array containing elements from the given <paramref name="list"/>.
        /// </summary>
        public Array(List<T> list)
        {
            value = UnsafeArray.Allocate(list.AsSpan());
        }

#if NET
        /// <summary>
        /// Creates an empty array.
        /// </summary>
        public Array()
        {
            value = UnsafeArray.Allocate<T>(0);
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
            UnsafeArray.Free(ref value);
        }

        /// <summary>
        /// Resets all elements in the array back to <c>default</c> state.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeArray.Clear(value);
        }

        /// <summary>
        /// Clears <paramref name="length"/> amount of elements from this array
        /// starting at <paramref name="start"/> index.
        /// </summary>
        public readonly void Clear(uint start, uint length)
        {
            UnsafeArray.Clear(value, start, length);
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
            return UnsafeArray.AsSpan<T>(value);
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
        /// Attempts to find the index of the given <paramref name="value"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryIndexOf<V>(V value, out uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.TryIndexOf(this.value, value, out index);
        }

        /// <summary>
        /// Retrieves the index of the given <paramref name="value"/>.
        /// <para>
        /// May throw <see cref="NullReferenceException"/> if <paramref name="value"/> was not found.
        /// </para>
        /// </summary>
        /// <returns>Index of the <paramref name="value"/>.</returns>
        public readonly uint IndexOf<V>(V value) where V : unmanaged, IEquatable<V>
        {
            if (!TryIndexOf(value, out uint index))
            {
                throw new NullReferenceException($"The value {value} was not found in the array.");
            }

            return index;
        }

        /// <summary>
        /// Checks if the array contains the given <paramref name="value"/>.
        /// </summary>
        public readonly bool Contains<V>(V value) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.Contains(this.value, value);
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
        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(value);
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

        /// <summary>
        /// Opaque pointer implementation of an array.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly UnsafeArray* array;
            private int index;

            /// <inheritdoc/>
            public readonly T Current => UnsafeArray.GetRef<T>(array, (uint)index);

            readonly object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public Enumerator(UnsafeArray* array)
            {
                this.array = array;
                index = -1;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                index++;
                return index < UnsafeArray.GetLength(array);
            }

            /// <inheritdoc/>
            public void Reset()
            {
                index = -1;
            }

            readonly void IDisposable.Dispose()
            {
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(Array<T> left, Array<T> right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Array<T> left, Array<T> right)
        {
            return !(left == right);
        }
    }
}
