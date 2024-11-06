using Collections.Unsafe;
using System;
using System.Collections;
using System.Collections.Generic;
using Unmanaged;

namespace Collections
{
    public unsafe struct Array<T> : IDisposable, IReadOnlyList<T>, IEquatable<Array<T>> where T : unmanaged
    {
        private UnsafeArray* value;

        public readonly bool IsDisposed => UnsafeArray.IsDisposed(value);

        public readonly uint Length
        {
            get => UnsafeArray.GetLength(value);
            set => UnsafeArray.Resize(this.value, value);
        }

        public readonly ref T this[uint index] => ref UnsafeArray.GetRef<T>(value, index);

        readonly int IReadOnlyCollection<T>.Count => (int)Length;
        readonly T IReadOnlyList<T>.this[int index] => UnsafeArray.GetRef<T>(value, (uint)index);

        public Array(UnsafeArray* array)
        {
            value = array;
        }

        /// <summary>
        /// Creates a new blank array with the specified length.
        /// </summary>
        public Array(uint length)
        {
            value = UnsafeArray.Allocate<T>(length);
        }

        /// <summary>
        /// Creates a new array containing the given span.
        /// </summary>
        public Array(USpan<T> span)
        {
            value = UnsafeArray.Allocate(span);
        }

        /// <summary>
        /// Creates a new array containing elements from the given list.
        /// </summary>
        public Array(List<T> items)
        {
            value = UnsafeArray.Allocate(items.AsSpan());
        }

#if NET
        /// <summary>
        /// Creates an empty array.
        /// </summary>
        public Array()
        {
            this = Create();
        }
#endif

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
        /// Clears the array from the specified start index to the end.
        /// </summary>
        public readonly void Clear(uint start, uint length)
        {
            UnsafeArray.Clear(value, start, length);
        }

        public readonly void Fill(T defaultValue)
        {
            AsSpan().Fill(defaultValue);
        }

        /// <summary>
        /// Returns the array as a span.
        /// </summary>
        public readonly USpan<T> AsSpan()
        {
            return UnsafeArray.AsSpan<T>(value);
        }

        public readonly USpan<T> AsSpan(uint start, uint length)
        {
            return AsSpan().Slice(start, length);
        }

        public readonly bool TryIndexOf<V>(V value, out uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.TryIndexOf(this.value, value, out index);
        }

        public readonly uint IndexOf<V>(V value) where V : unmanaged, IEquatable<V>
        {
            if (!TryIndexOf(value, out uint index))
            {
                throw new NullReferenceException($"The value {value} was not found in the array.");
            }

            return index;
        }

        public readonly bool Contains<V>(V value) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.Contains(this.value, value);
        }

        public readonly void CopyTo(USpan<T> span)
        {
            AsSpan().CopyTo(span);
        }

        public readonly void CopyFrom(USpan<T> span)
        {
            span.CopyTo(AsSpan());
        }

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

        public override readonly bool Equals(object? obj)
        {
            return obj is Array<T> array && Equals(array);
        }

        public readonly bool Equals(Array<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return value == other.value;
        }

        public override readonly int GetHashCode()
        {
            nint ptr = (nint)value;
            return HashCode.Combine(ptr, 7);
        }

        public static Array<T> Create(uint length = 0)
        {
            return new Array<T>(length);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly UnsafeArray* array;
            private int index;

            public readonly T Current => UnsafeArray.GetRef<T>(array, (uint)index);

            readonly object IEnumerator.Current => Current;

            public Enumerator(UnsafeArray* array)
            {
                this.array = array;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < UnsafeArray.GetLength(array);
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose()
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
