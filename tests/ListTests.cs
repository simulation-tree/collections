using Unmanaged;
using Unmanaged.Tests;

namespace Collections.Tests
{
    public class ListTests : UnmanagedTests
    {
        [Test]
        public void EmptyList()
        {
            using List list = new(8, 4);
            Assert.That(list.Capacity, Is.EqualTo(8));
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Stride, Is.EqualTo(4));
        }

        [Test]
        public void AddItems()
        {
            using List list = new(2, sizeof(int));
            Assert.That(list.Capacity, Is.EqualTo(2));
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Stride, Is.EqualTo(4));

            list.Add(1);
            list.Add(3);
            list.Add(3);
            list.Add(7);

            Assert.That(list.Capacity, Is.EqualTo(4));
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list[0].Read<int>(), Is.EqualTo(1));
            Assert.That(list[1].Read<int>(), Is.EqualTo(3));
            Assert.That(list[2].Read<int>(), Is.EqualTo(3));
            Assert.That(list[3].Read<int>(), Is.EqualTo(7));
        }

        [Test]
        public void RemoveAt()
        {
            using List list = new(2, sizeof(int));
            list.Add(1);
            list.Add(3);
            list.Add(7);
            list.Add(11);
            list.Add(18);

            Assert.That(list.Count, Is.EqualTo(5));

            list.RemoveAt(1);

            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list[0].Read<int>(), Is.EqualTo(1));
            Assert.That(list[1].Read<int>(), Is.EqualTo(7));
            Assert.That(list[2].Read<int>(), Is.EqualTo(11));
            Assert.That(list[3].Read<int>(), Is.EqualTo(18));

            list.RemoveAt(0);

            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].Read<int>(), Is.EqualTo(7));
            Assert.That(list[1].Read<int>(), Is.EqualTo(11));
            Assert.That(list[2].Read<int>(), Is.EqualTo(18));

            list.RemoveAt(2);

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].Read<int>(), Is.EqualTo(7));
            Assert.That(list[1].Read<int>(), Is.EqualTo(11));

            list.RemoveAt(1);

            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].Read<int>(), Is.EqualTo(7));

            list.RemoveAt(0);

            Assert.That(list.Count, Is.EqualTo(0));
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
    }
}