using Collections.Implementations;
using System;
using Unmanaged;
using Unmanaged.Tests;

namespace Collections.Tests
{
    public class ListTests : UnmanagedTests
    {
        [Test]
        public unsafe void EmptyList()
        {
            using List<byte> list = new(8);
            Assert.That(list.Capacity, Is.EqualTo(8));
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddingIntoList()
        {
            using List<byte> list = new(1);
            list.Add(32);
            Assert.That(list[0], Is.EqualTo(32));
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void ListOfStrings()
        {
            using List<FixedString> list = new(3);
            list.Add("Hello");
            list.Add(" ");
            list.Add("there...");
            Assert.That(list[0].ToString(), Is.EqualTo("Hello"));
            Assert.That(list[1].ToString(), Is.EqualTo(" "));
            Assert.That(list[2].ToString(), Is.EqualTo("there..."));
        }

        [Test]
        public void ExpandingList()
        {
            using List<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list[3], Is.EqualTo(4));
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void ChangingCapacity()
        {
            using List<int> list = new(1);
            list.Add(1);
            list.Add(2);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.Capacity, Is.EqualTo(2));

            list.Capacity = 9;

            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.Capacity, Is.EqualTo(16));

            list.Add(3);

            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(16));
        }

        [Test]
        public void RemoveAtIndex()
        {
            using List<int> list = new();
            list.Add(1);
            list.Add(2); //removed
            list.Add(3);
            list.Add(4);
            list.RemoveAt(1);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(3));
            Assert.That(list[2], Is.EqualTo(4));
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void RemoveAtIndexWithSwapback()
        {
            using List<int> list = new();
            list.Add(1);
            list.Add(2); //removed
            list.Add(3);
            list.Add(4);
            list.RemoveAtBySwapping(1);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(4));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void InsertIntoList()
        {
            using List<int> list = new();
            list.Insert(0, 1);
            Assert.That(list[0], Is.EqualTo(1));
            list.Add(2);
            list.Add(4);
            list.Insert(2, 3);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list[3], Is.EqualTo(4));

            list.Insert(4, 2323);
            Assert.That(list[4], Is.EqualTo(2323));

            list.Add(1000);
            list.Insert(0, 2000);

            Assert.That(list[0], Is.EqualTo(2000));
            Assert.That(list[1], Is.EqualTo(1));
            Assert.That(list[2], Is.EqualTo(2));
            Assert.That(list[3], Is.EqualTo(3));
            Assert.That(list[4], Is.EqualTo(4));
            Assert.That(list[5], Is.EqualTo(2323));
        }

        [Test]
        public void AddRange()
        {
            using List<uint> list = new();
            list.AddRange(new[] { 1u, 2u, 3u, 4u });
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list[0], Is.EqualTo(1u));
            Assert.That(list[1], Is.EqualTo(2u));
            Assert.That(list[2], Is.EqualTo(3u));
            Assert.That(list[3], Is.EqualTo(4u));
            list.AddRange(new[] { 5u, 6u, 7u });
            Assert.That(list.Count, Is.EqualTo(7));
            Assert.That(list[4], Is.EqualTo(5u));
            Assert.That(list[5], Is.EqualTo(6u));
            Assert.That(list[6], Is.EqualTo(7u));
            list.AddRange(new[] { 8u, 9u, 10u });
            Assert.That(list.Count, Is.EqualTo(10));
            Assert.That(list[7], Is.EqualTo(8u));
            Assert.That(list[8], Is.EqualTo(9u));
            Assert.That(list[9], Is.EqualTo(10u));
        }

        [Test]
        public void AddRepeat()
        {
            using List<byte> list = new();
            list.AddRepeat(5, 33);
            Assert.That(list.Count, Is.EqualTo(33));
            Assert.That(list[0], Is.EqualTo(5));

            list.AddRepeat(9, 44);
            Assert.That(list[32], Is.EqualTo(5));
            Assert.That(list[33], Is.EqualTo(9));
            Assert.That(list.Count, Is.EqualTo(77));
        }

        [Test]
        public void AddDefaults()
        {
            using List<byte> list = new();
            list.AddDefault(5);

            Assert.That(list.Count, Is.EqualTo(5));
            Assert.That(list[0], Is.EqualTo(default(byte)));
            Assert.That(list[1], Is.EqualTo(default(byte)));
            Assert.That(list[2], Is.EqualTo(default(byte)));
            Assert.That(list[3], Is.EqualTo(default(byte)));
            Assert.That(list[4], Is.EqualTo(default(byte)));
        }

        [Test]
        public void InsertRange()
        {
            using List<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(6);
            list.InsertRange(2, new[] { 3, 4, 5 });
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list[3], Is.EqualTo(4));
            Assert.That(list[4], Is.EqualTo(5));
            Assert.That(list[5], Is.EqualTo(6));
        }

        [Test]
        public void ListContains()
        {
            using List<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            Assert.That(list.Contains(3), Is.True);
            Assert.That(list.Contains(5), Is.False);
        }

        [Test]
        public void ClearWithMinimumCapacity()
        {
            using List<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Clear(32);
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(32));
            Assert.That(list.IsDisposed, Is.False);
        }

        [Test]
        public void ClearListThenAdd()
        {
            using List<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Clear();
            Assert.That(list.Count, Is.EqualTo(0));
            list.Add(5);
            Assert.That(list[0], Is.EqualTo(5));
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void BuildListThenCopyToSpan()
        {
            using List<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            USpan<int> span = stackalloc int[4];
            list.CopyTo(span);
            Assert.That(span[0], Is.EqualTo(1));
            Assert.That(span[1], Is.EqualTo(2));
            Assert.That(span[2], Is.EqualTo(3));
            Assert.That(span[3], Is.EqualTo(4));
        }

        [Test]
        public unsafe void ReadBytesFromList()
        {
            List* data = List.Allocate<int>(4);
            List.Add(data, 1);
            List.Add(data, 2);
            List.Add(data, 3);
            List.Add(data, 4);

            USpan<byte> span = List.AsSpan<int>(data).Reinterpret<byte>();
            Assert.That(span.Length, Is.EqualTo(sizeof(int) * 4));
            int value1 = BitConverter.ToInt32(span.Slice(0, 4));
            int value2 = BitConverter.ToInt32(span.Slice(4, 4));
            int value3 = BitConverter.ToInt32(span.Slice(8, 4));
            int value4 = BitConverter.ToInt32(span.Slice(12, 4));
            Assert.That(value1, Is.EqualTo(1));
            Assert.That(value2, Is.EqualTo(2));
            Assert.That(value3, Is.EqualTo(3));
            Assert.That(value4, Is.EqualTo(4));
            List.Free(ref data);
            Assert.That(data is null, Is.True);
        }

        [Test]
        public void ListFromSpan()
        {
            USpan<char> word = ['H', 'e', 'l', 'l', 'o'];
            List<char> list = new(word);
            Assert.That(list.Count, Is.EqualTo(5));
            USpan<char> otherSpan = list.AsSpan();
            Assert.That(otherSpan[0], Is.EqualTo('H'));
            Assert.That(otherSpan[1], Is.EqualTo('e'));
            Assert.That(otherSpan[2], Is.EqualTo('l'));
            Assert.That(otherSpan[3], Is.EqualTo('l'));
            Assert.That(otherSpan[4], Is.EqualTo('o'));
            list.Dispose();
        }

        [Test]
        public void ListInsideArray()
        {
            Array<List<byte>> nestedData = new(8);
            for (uint i = 0; i < 8; i++)
            {
                ref List<byte> list = ref nestedData[i];
                list = new();
                list.Add((byte)i);
            }

            for (uint i = 0; i < 8; i++)
            {
                List<byte> list = nestedData[i];
                Assert.That(list[0], Is.EqualTo((byte)i));

                list.Dispose();
            }

            nestedData.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public unsafe void AddAnotherUnsafeList()
        {
            List* a = List.Allocate<int>(4);
            List* b = List.Allocate<int>(4);
            List.AddRange(a, [1, 3]);
            List.AddRange(b, [3, 7, 7]);
            Assert.That(List.AsSpan<int>(a).ToArray(), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(List.AsSpan<int>(b).ToArray(), Is.EqualTo(new[] { 3, 7, 7 }));
            List.AddRange(a, (void*)List.GetStartAddress(b), List.GetCount(b));
            Assert.That(List.AsSpan<int>(a).ToArray(), Is.EqualTo(new[] { 1, 3, 3, 7, 7 }));
            List.Free(ref a);
            List.Free(ref b);
        }

        [Test]
        public void CreateListFromEmptySpan()
        {
            USpan<int> empty = default;
            List<int> list = new(empty);
            Assert.That(list.Count, Is.EqualTo(0));
            list.Add(32);
            Assert.That(list.Count, Is.EqualTo(1));
            list.Dispose();
        }

        [Test]
        public void CreateEmptyList()
        {
            List<int> list = new(0);
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(1));
            list.AddRepeat(5, 32);
            Assert.That(list.Count, Is.EqualTo(32));
            list.Dispose();
        }
    }
}
