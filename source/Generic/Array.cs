using Collections.Pointers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Generic
{
    /// <summary>
    /// Native array that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Array<T> : IDisposable, IReadOnlyList<T>, IEquatable<Array<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ArrayPointer* array;

        /// <summary>
        /// Checks if the array has been disposed.
        /// </summary>
        public readonly bool IsDisposed => array is null;

        /// <summary>
        /// Length of the array.
        /// <para>
        /// Resizing the array to be bigger will not clear the new elements.
        /// </para>
        /// </summary>
        public readonly int Length
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);

                return array->length;
            }
            set
            {
                MemoryAddress.ThrowIfDefault(array);

                if (array->length != value)
                {
                    MemoryAddress.Resize(ref array->items, sizeof(T) * value);
                    array->length = value;
                }
            }
        }

        /// <summary>
        /// The underlying allocation of the array containing all elements.
        /// </summary>
        public readonly MemoryAddress Items
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);

                return array->items;
            }
        }

        /// <summary>
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly ref T this[int index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);
                ThrowIfOutOfRange(index);

                return ref array->items.ReadElement<T>(index);
            }
        }

        /// <summary>
        /// Accesses the element at the specified index.
        /// </summary>
        public readonly ref T this[uint index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);
                ThrowIfOutOfRange(index);

                return ref array->items.ReadElement<T>(index);
            }
        }

        /// <summary>
        /// The native pointer to the array.
        /// </summary>
        public readonly ArrayPointer* Pointer => array;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly int IReadOnlyCollection<T>.Count => Length;

        readonly T IReadOnlyList<T>.this[int index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);
                ThrowIfOutOfRange(index);

                return array->items.ReadElement<T>(index);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Values => AsSpan().ToArray();

        /// <summary>
        /// Initializes an existing array from the given <paramref name="pointer"/>
        /// </summary>
        public Array(ArrayPointer* pointer)
        {
            array = pointer;
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="length"/>.
        /// </summary>
        public Array(int length)
        {
            array = MemoryAddress.AllocatePointer<ArrayPointer>();
            array->items = MemoryAddress.AllocateZeroed(sizeof(T) * length);
            array->length = length;
            array->stride = sizeof(T);
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="span"/>.
        /// </summary>
        public Array(ReadOnlySpan<T> span)
        {
            array = MemoryAddress.AllocatePointer<ArrayPointer>();
            array->items = MemoryAddress.Allocate(span);
            array->length = span.Length;
            array->stride = sizeof(T);
        }

#if NET
        /// <summary>
        /// Creates an empty array.
        /// </summary>
        public Array()
        {
            array = MemoryAddress.AllocatePointer<ArrayPointer>();
            array->items = MemoryAddress.AllocateEmpty();
            array->length = 0;
            array->stride = sizeof(T);
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
            MemoryAddress.ThrowIfDefault(array);

            array->items.Dispose();
            MemoryAddress.Free(ref array);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= array->length || index < 0)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for array of length {array->length}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= array->length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for array of length {array->length}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeMismatch<X>() where X : unmanaged
        {
            if (sizeof(T) != sizeof(X))
            {
                throw new InvalidOperationException($"Size of {typeof(T)} does not match size of {typeof(X)}");
            }
        }

        /// <summary>
        /// Resets all elements in the array to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(array);

            new Span<T>(array->items.Pointer, array->length).Clear();
        }

        /// <summary>
        /// Clears <paramref name="length"/> amount of elements from this array
        /// starting at <paramref name="start"/> index.
        /// </summary>
        public readonly void Clear(int start, int length)
        {
            MemoryAddress.ThrowIfDefault(array);

            array->items.Clear(start * sizeof(T), length * sizeof(T));
        }

        /// <summary>
        /// Fills the array with the given <paramref name="value"/>.
        /// </summary>
        public readonly void Fill(T value)
        {
            MemoryAddress.ThrowIfDefault(array);

            new Span<T>(array->items.Pointer, array->length).Fill(value);
        }

        /// <summary>
        /// Returns the array as a span.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            MemoryAddress.ThrowIfDefault(array);

            return new(array->items.Pointer, array->length);
        }

        /// <summary>
        /// Returns the array as a span of a different type <typeparamref name="X"/>.
        /// </summary>
        public readonly Span<X> AsSpan<X>() where X : unmanaged
        {
            MemoryAddress.ThrowIfDefault(array);
            ThrowIfSizeMismatch<T>();

            return new(array->items.Pointer, array->length);
        }

        /// <summary>
        /// Returns the remainder of the array from <paramref name="start"/>,
        /// as a span of a different type <typeparamref name="X"/>.
        /// </summary>
        public readonly Span<X> AsSpan<X>(int start) where X : unmanaged
        {
            MemoryAddress.ThrowIfDefault(array);
            ThrowIfSizeMismatch<T>();

            return array->items.AsSpan<X>(start, array->length - start);
        }

        /// <summary>
        /// Returns the array as a span with the given <paramref name="length"/>.
        /// </summary>
        public readonly Span<T> GetSpan(int length)
        {
            MemoryAddress.ThrowIfDefault(array);

            return new(array->items.Pointer, length);
        }

        /// <summary>
        /// Returns the remainder of the array from <paramref name="start"/> as a span.
        /// </summary>
        public readonly Span<T> AsSpan(int start)
        {
            MemoryAddress.ThrowIfDefault(array);

            return array->items.AsSpan<T>(start, array->length - start);
        }

        /// <summary>
        /// Returns the array as a span starting at <paramref name="start"/> index
        /// with the given <paramref name="length"/>.
        /// </summary>
        public readonly Span<T> AsSpan(int start, int length)
        {
            MemoryAddress.ThrowIfDefault(array);

            return array->items.AsSpan<T>(start, length);
        }

        /// <summary>
        /// Copies the array to the given <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Span<T> destination)
        {
            new Span<T>(array->items.Pointer, array->length).CopyTo(destination);
        }

        /// <summary>
        /// Copies the given <paramref name="source"/> to this array
        /// assuming its length is less or equal than the array.
        /// </summary>
        public readonly void CopyFrom(ReadOnlySpan<T> source)
        {
            source.CopyTo(new(array->items.Pointer, array->length));
        }

        /// <inheritdoc/>
        public readonly Span<T>.Enumerator GetEnumerator()
        {
            return new Span<T>(array->items.Pointer, array->length).GetEnumerator();
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
            private readonly ArrayPointer* array;
            private int index;

            public readonly T Current => array->items.ReadElement<T>(index);
            readonly object IEnumerator.Current => Current;

            public Enumerator(ArrayPointer* array)
            {
                this.array = array;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < array->length;
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

        public static implicit operator Array(Array<T> array)
        {
            return new(array.array);
        }
    }
}