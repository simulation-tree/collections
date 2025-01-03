using System;
using System.Collections;
using System.Collections.Generic;
using Unmanaged;

namespace Collections
{
    public unsafe struct Text : IDisposable, IEquatable<Text>, IEnumerable<char>, IReadOnlyList<char>
    {
        private const uint Stride = 2;

        private UnsafeText* value;

        public readonly uint Length => value->length;
        public readonly ref char this[uint index] => ref value->buffer.Read<char>(index * Stride);

        readonly int IReadOnlyCollection<char>.Count => (int)Length;
        readonly char IReadOnlyList<char>.this[int index] => value->buffer.Read<char>((uint)index * Stride);

        public Text()
        {
            value = UnsafeText.Allocate(0);
        }

        public Text(uint length, char defaultCharacter = ' ')
        {
            value = UnsafeText.Allocate(length);
            value->buffer.AsSpan<char>(0, length).Fill(defaultCharacter);
        }

        public Text(USpan<char> text)
        {
            value = UnsafeText.Allocate(text.Length);
            text.CopyTo(value->buffer.AsSpan<char>(0, text.Length));
        }

        public Text(IEnumerable<char> text)
        {
            value = UnsafeText.Allocate(0);
            Append(text);
        }

        public void Dispose()
        {
            UnsafeText.Free(ref value);
            value = default;
        }

        public readonly USpan<char> AsSpan()
        {
            return value->buffer.AsSpan<char>(0, Length);
        }

        public readonly void Clear()
        {
            value->length = 0;
        }

        public readonly void CopyTo(USpan<char> text)
        {
            value->buffer.AsSpan<char>(0, Length).CopyTo(text);
        }

        public readonly void CopyFrom(USpan<char> text)
        {
            if (value->length != text.Length)
            {
                value->length = text.Length;
                Allocation.Resize(ref value->buffer, text.Length * Stride);
            }

            text.CopyTo(value->buffer.AsSpan<char>(0, text.Length));
        }

        public readonly void CopyFrom(IEnumerable<char> text)
        {
            uint length = 0;
            foreach (char character in text)
            {
                length++;
            }

            if (value->length != length)
            {
                value->length = length;
                Allocation.Resize(ref value->buffer, length * Stride);
            }

            USpan<char> buffer = AsSpan();
            length = 0;
            foreach (char character in text)
            {
                buffer[length++] = character;
            }
        }

        public readonly override string ToString()
        {
            uint length = Length;
            USpan<char> buffer = stackalloc char[(int)length];
            ToString(buffer);
            return buffer.ToString();
        }

        public readonly uint ToString(USpan<char> destination)
        {
            uint length = Length;
            USpan<char> source = AsSpan();
            uint copyLength = Math.Min(length, destination.Length);
            source.Slice(0, copyLength).CopyTo(destination);
            return copyLength;
        }

        public readonly void SetLength(uint newLength, char defaultCharacter = ' ')
        {
            uint oldLength = Length;
            if (newLength == oldLength)
            {
                return;
            }

            value->length = newLength;
            if (newLength > oldLength)
            {
                uint copyLength = newLength - oldLength;
                Allocation.Resize(ref value->buffer, newLength * Stride);
                value->buffer.AsSpan<char>(oldLength, copyLength).Fill(defaultCharacter);
            }
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Text text && Equals(text);
        }

        public readonly bool Equals(Text other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        public static void Append(USpan<char> originalText, char character, USpan<char> destination)
        {
            originalText.CopyTo(destination.Slice(0, originalText.Length));
            destination[originalText.Length] = character;
        }

        public static void Append(USpan<char> originalText, string newText, USpan<char> destination)
        {
            Append(originalText, newText.AsUSpan(), destination);
        }

        public static void Append(USpan<char> originalText, USpan<char> newText, USpan<char> destination)
        {
            uint originalLength = originalText.Length;
            uint newLength = newText.Length;
            uint destinationLength = destination.Length;
            uint copyLength = Math.Min(originalLength + newLength, destinationLength);
            originalText.CopyTo(destination.Slice(0, originalLength));
            newText.Slice(0, copyLength - originalLength).CopyTo(destination.Slice(originalLength));
        }

        public static uint Replace(USpan<char> originalText, string target, string replacement, USpan<char> destination)
        {
            return Replace(originalText, target.AsUSpan(), replacement.AsUSpan(), destination);
        }

        public static uint Replace(USpan<char> originalText, USpan<char> target, USpan<char> replacement, USpan<char> destination)
        {
            uint length = 0;
            uint originalStart = 0;
            uint destinationStart = 0;
            while (true)
            {
                if (originalText.Slice(originalStart).TryIndexOf(target, out uint index))
                {
                    originalText.Slice(originalStart).CopyTo(destination.Slice(destinationStart));
                    replacement.CopyTo(destination.Slice(destinationStart + index));
                    originalStart += index + target.Length;
                    destinationStart += index + replacement.Length;
                    length += index + replacement.Length;
                }
                else
                {
                    originalText.Slice(originalStart).CopyTo(destination.Slice(destinationStart));
                    length += originalText.Length - originalStart;
                    return length;
                }
            }
        }

        public readonly void Append(IEnumerable<char> text)
        {
            uint length = Length;
            foreach (char character in text)
            {
                SetLength(length + 1, character);
                length++;
            }
        }

        public readonly Enumerator GetEnumerator()
        {
            return new(value);
        }

        readonly IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct UnsafeText
        {
            public uint length;
            public Allocation buffer;

            public static UnsafeText* Allocate(uint length)
            {
                UnsafeText* text = Allocations.Allocate<UnsafeText>();
                text->length = length;
                text->buffer = new(length * Stride);
                return text;
            }

            public static void Free(ref UnsafeText* text)
            {
                Allocations.ThrowIfNull(text);

                text->buffer.Dispose();
                Allocations.Free(ref text);
            }
        }

        public struct Enumerator : IEnumerator<char>
        {
            private readonly UnsafeText* text;
            private int index;

            public readonly char Current => text->buffer.AsSpan<char>(0, text->length)[(uint)index];
            readonly object IEnumerator.Current => Current;

            public Enumerator(UnsafeText* text)
            {
                this.text = text;
                index = -1;
            }

            public bool MoveNext()
            {
                return ++index < text->length;
            }

            public void Reset()
            {
                index = -1;
            }

            public readonly void Dispose()
            {
            }
        }

        public static bool operator ==(Text left, Text right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Text left, Text right)
        {
            return !(left == right);
        }

        public static implicit operator Text(string text)
        {
            return new(text);
        }

        public static implicit operator Text(USpan<char> text)
        {
            return new(text);
        }

        public static Text operator +(Text left, Text right)
        {
            Text result = new(left);
            result.Append(right);
            return result;
        }
    }
}
