using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using Pointer = Collections.Pointers.Array;

namespace Collections.Generic
{
    /// <summary>
    /// Native array that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Array<T> : IDisposable, IReadOnlyList<T>, IEquatable<Array<T>> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Pointer* array;

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

                if (array->length != value)
                {
                    uint oldLength = array->length;
                    Allocation.Resize(ref array->items, (uint)sizeof(T) * value);
                    array->length = value;
                }
            }
        }

        /// <summary>
        /// The underlying allocation of the array containing all elements.
        /// </summary>
        public readonly Allocation Items
        {
            get
            {
                Allocations.ThrowIfNull(array);

                return array->items;
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
                ThrowIfOutOfRange(index);

                return ref array->items.ReadElement<T>(index);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly int IReadOnlyCollection<T>.Count => (int)Length;

        readonly T IReadOnlyList<T>.this[int index]
        {
            get
            {
                Allocations.ThrowIfNull(array);
                ThrowIfOutOfRange((uint)index);

                return array->items.ReadElement<T>((uint)index);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly T[] Values => AsSpan().ToArray();

        /// <summary>
        /// Initializes an existing array from the given <paramref name="pointer"/>
        /// </summary>
        public Array(Pointer* pointer)
        {
            array = pointer;
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="length"/>.
        /// </summary>
        public Array(uint length)
        {
            ref Pointer array = ref Allocations.Allocate<Pointer>();
            array = new((uint)sizeof(T), length, Allocation.CreateZeroed((uint)sizeof(T) * length));
            fixed (Pointer* pointer = &array)
            {
                this.array = pointer;
            }
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="span"/>.
        /// </summary>
        public Array(USpan<T> span)
        {
            ref Pointer array = ref Allocations.Allocate<Pointer>();
            array = new((uint)sizeof(T), span.Length, Allocation.Create(span));
            fixed (Pointer* pointer = &array)
            {
                this.array = pointer;
            }
        }

#if NET
        /// <summary>
        /// Creates an empty array.
        /// </summary>
        public Array()
        {
            ref Pointer array = ref Allocations.Allocate<Pointer>();
            array = new((uint)sizeof(T), 0, Allocation.CreateEmpty());
            fixed (Pointer* pointer = &array)
            {
                this.array = pointer;
            }
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
            Allocations.ThrowIfNull(array);

            array->items.Dispose();
            Allocations.Free(ref array);
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
            Allocations.ThrowIfNull(array);

            unchecked
            {
                array->items.Clear(array->length * (uint)sizeof(T));
            }
        }

        /// <summary>
        /// Clears <paramref name="length"/> amount of elements from this array
        /// starting at <paramref name="start"/> index.
        /// </summary>
        public readonly void Clear(uint start, uint length)
        {
            Allocations.ThrowIfNull(array);

            unchecked
            {
                array->items.Clear(start * (uint)sizeof(T), length * (uint)sizeof(T));
            }
        }

        /// <summary>
        /// Fills the array with the given <paramref name="value"/>.
        /// </summary>
        public readonly void Fill(T value)
        {
            Allocations.ThrowIfNull(array);

            new USpan<T>(array->items.Pointer, array->length).Fill(value);
        }

        /// <summary>
        /// Returns the array as a span.
        /// </summary>
        public readonly USpan<T> AsSpan()
        {
            Allocations.ThrowIfNull(array);

            return new(array->items.Pointer, array->length);
        }

        /// <summary>
        /// Returns the array as a span of a different type <typeparamref name="X"/>.
        /// </summary>
        public readonly USpan<X> AsSpan<X>() where X : unmanaged
        {
            Allocations.ThrowIfNull(array);
            ThrowIfSizeMismatch<T>();

            return new(array->items.Pointer, array->length);
        }

        /// <summary>
        /// Returns the remainder of the array from <paramref name="start"/>,
        /// as a span of a different type <typeparamref name="X"/>.
        /// </summary>
        public readonly USpan<X> AsSpan<X>(uint start) where X : unmanaged
        {
            Allocations.ThrowIfNull(array);
            ThrowIfSizeMismatch<T>();

            return array->items.AsSpan<X>(start, array->length - start);
        }

        /// <summary>
        /// Returns the array as a span with the given <paramref name="length"/>.
        /// </summary>
        public readonly USpan<T> GetSpan(uint length)
        {
            Allocations.ThrowIfNull(array);

            return new(array->items.Pointer, length);
        }

        /// <summary>
        /// Returns the remainder of the array from <paramref name="start"/> as a span.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start)
        {
            Allocations.ThrowIfNull(array);

            return array->items.AsSpan<T>(start, array->length - start);
        }

        /// <summary>
        /// Returns the array as a span starting at <paramref name="start"/> index
        /// with the given <paramref name="length"/>.
        /// </summary>
        public readonly USpan<T> AsSpan(uint start, uint length)
        {
            Allocations.ThrowIfNull(array);

            return array->items.AsSpan<T>(start, length);
        }

        /// <summary>
        /// Copies the array to the given <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(USpan<T> destination)
        {
            AsSpan().CopyTo(destination);
        }

        /// <summary>
        /// Copies the given <paramref name="source"/> to this array.
        /// </summary>
        public readonly void CopyFrom(USpan<T> source)
        {
            source.CopyTo(AsSpan());
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
            private readonly Pointer* array;
            private int index;

            public readonly T Current
            {
                get
                {
                    unchecked
                    {
                        return array->items.Read<T>((uint)index * (uint)sizeof(T));
                    }
                }
            }

            readonly object IEnumerator.Current => Current;

            public Enumerator(Pointer* array)
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