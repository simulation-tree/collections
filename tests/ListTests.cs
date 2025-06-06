﻿using Collections.Generic;
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
            using List<ASCIIText256> list = new(3);
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
            list.RemoveAt(1, out int removed);
            Assert.That(removed, Is.EqualTo(3));
            Assert.That(list.Count, Is.EqualTo(2));
            list.RemoveAt(1, out removed);
            Assert.That(removed, Is.EqualTo(4));
            Assert.That(list.Count, Is.EqualTo(1));
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
        public void RemoveAndRetrieveValue()
        {
            using List<int> list = new();
            list.Add(1);
            list.Add(2); //removed
            list.Add(3);
            list.Add(4);
            list.RemoveAtBySwapping(1, out int removed);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(4));
            Assert.That(list[2], Is.EqualTo(3));
            list.RemoveAtBySwapping(1, out removed);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(removed, Is.EqualTo(4));
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(3));
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
            using List<byte> list = new(4);
            list.AddDefault(5);

            Assert.That(list.Count, Is.EqualTo(5));
            Assert.That(list.Capacity, Is.EqualTo(8));
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

            Span<int> span = stackalloc int[4];
            list.CopyTo(span);
            Assert.That(span[0], Is.EqualTo(1));
            Assert.That(span[1], Is.EqualTo(2));
            Assert.That(span[2], Is.EqualTo(3));
            Assert.That(span[3], Is.EqualTo(4));
        }

        [Test]
        public unsafe void ReadBytesFromList()
        {
            List<int> data = new(4);
            data.Add(1);
            data.Add(2);
            data.Add(3);
            data.Add(4);

            Span<byte> span = data.AsSpan().Reinterpret<int, byte>();
            Assert.That(span.Length, Is.EqualTo(sizeof(int) * 4));
            int value1 = BitConverter.ToInt32(span.Slice(0, 4));
            int value2 = BitConverter.ToInt32(span.Slice(4, 4));
            int value3 = BitConverter.ToInt32(span.Slice(8, 4));
            int value4 = BitConverter.ToInt32(span.Slice(12, 4));
            Assert.That(value1, Is.EqualTo(1));
            Assert.That(value2, Is.EqualTo(2));
            Assert.That(value3, Is.EqualTo(3));
            Assert.That(value4, Is.EqualTo(4));

            data.Dispose();
            Assert.That(data.IsDisposed, Is.True);
        }

        [Test]
        public void ListFromSpan()
        {
            Span<char> word = ['H', 'e', 'l', 'l', 'o'];
            List<char> list = new(word);
            Assert.That(list.Count, Is.EqualTo(5));
            Span<char> otherSpan = list.AsSpan();
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
            for (int i = 0; i < 8; i++)
            {
                ref List<byte> list = ref nestedData[i];
                list = new();
                list.Add((byte)i);
            }

            for (int i = 0; i < 8; i++)
            {
                List<byte> list = nestedData[i];
                Assert.That(list[0], Is.EqualTo((byte)i));

                list.Dispose();
            }

            nestedData.Dispose();
        }

        [Test]
        public unsafe void AddAnotherUnsafeList()
        {
            List<int> a = new(4);
            List<int> b = new(4);
            a.AddRange([1, 3]);
            b.AddRange([3, 7, 7]);
            Assert.That(a.AsSpan().ToArray(), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(b.AsSpan().ToArray(), Is.EqualTo(new[] { 3, 7, 7 }));
            a.AddRange(b.AsSpan());
            Assert.That(a.AsSpan().ToArray(), Is.EqualTo(new[] { 1, 3, 3, 7, 7 }));
            b.Dispose();
            a.Dispose();
        }

        [Test]
        public void CreateListFromEmptySpan()
        {
            Span<int> empty = default;
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

        [Test]
        public void InsertAllocation()
        {
            List a = new List<int>(4);
            a.Add(8);
            a.Add(9);
            a.Add(10);
            a.Add(11);

            List b = new List<int>(3);
            b.Add(5);
            b.Add(6);
            b.Add(7);

            MemoryAddress element = a[3]; //11
            b.Insert(1, element);

            Assert.That(b.Count, Is.EqualTo(4));
            Span<int> span = b.AsSpan<int>();
            Assert.That(span[0], Is.EqualTo(5));
            Assert.That(span[1], Is.EqualTo(11));
            Assert.That(span[2], Is.EqualTo(6));
            Assert.That(span[3], Is.EqualTo(7));

            b.Dispose();
            a.Dispose();
        }

        [Test]
        public void InsertAtEndIntoEmptyList()
        {
            using List<int> a = new(0);
            a.Insert(0, 32);
            Assert.That(a.Count, Is.EqualTo(1));
            Assert.That(a[0], Is.EqualTo(32));

            using List b = new List<int>(0);
            b.Insert(0, 32);
            Assert.That(b.Count, Is.EqualTo(1));
            Assert.That(b.Get<int>(0), Is.EqualTo(32));
        }

        [Test]
        public void CopyListToAnotherList()
        {
            using List a = new(0, sizeof(int));
            using List b = new(0, sizeof(int));
            a.Add(1);
            a.Add(2);
            a.Add(3);
            a.Add(4);
            a.Add(5);

            Assert.That(a.Count, Is.EqualTo(5));
            Assert.That(b.Count, Is.EqualTo(0));

            b.CopyFrom(a);

            Assert.That(b.Count, Is.EqualTo(a.Count));
            Assert.That(b.Capacity, Is.EqualTo(a.Capacity));
            Assert.That(b.Get<int>(0), Is.EqualTo(1));
            Assert.That(b.Get<int>(1), Is.EqualTo(2));
            Assert.That(b.Get<int>(2), Is.EqualTo(3));
            Assert.That(b.Get<int>(3), Is.EqualTo(4));
            Assert.That(b.Get<int>(4), Is.EqualTo(5));

            a.Clear();
            a.Add(6);

            Assert.That(a.Count, Is.EqualTo(1));
            Assert.That(b.Count, Is.EqualTo(5));

            b.CopyFrom(a);

            Assert.That(b.Count, Is.EqualTo(1));
            Assert.That(b.Get<int>(0), Is.EqualTo(6));
        }

        [Test]
        public void MoveFromOneListToAnother()
        {
            using List a = new(0, sizeof(int));
            using List b = new(0, sizeof(int));
            a.Add(1);
            a.Add(2);
            a.Add(3);
            a.Add(4);
            a.Add(5);
            Assert.That(a.Count, Is.EqualTo(5));
            Assert.That(b.Count, Is.EqualTo(0));
            a.RemoveAtBySwappingAndAdd(0, b, out MemoryAddress newItem, out _);
            Assert.That(newItem.Read<int>(), Is.EqualTo(1));
            Assert.That(a.Count, Is.EqualTo(4));
            Assert.That(b.Count, Is.EqualTo(1));
            a.RemoveAtBySwappingAndAdd(0, b, out newItem, out _);
            Assert.That(newItem.Read<int>(), Is.EqualTo(5));
            Assert.That(a.Count, Is.EqualTo(3));
            Assert.That(b.Count, Is.EqualTo(2));
            Assert.That(a.AsSpan<int>().ToArray(), Is.EqualTo(new int[] { 4, 2, 3 }));
            Assert.That(b.AsSpan<int>().ToArray(), Is.EqualTo(new int[] { 1, 5 }));
        }
    }
}