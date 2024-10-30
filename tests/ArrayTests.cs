using System;
using Unmanaged;

namespace Collections
{
    public class ArrayTests : UnmanagedTests
    {
        [Test]
        public void EmptyArray()
        {
            Array<int> array = new();
            Assert.That(array.Length, Is.EqualTo(0));
            array.Dispose();
            Assert.That(array.IsDisposed, Is.True);
        }

        [Test]
        public void ArrayLength()
        {
            using Array<Guid> array = new(4);
            Assert.That(array.Length, Is.EqualTo(4));
        }

        [Test]
        public void CreatingArrayFromSpan()
        {
            USpan<int> span = stackalloc int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using Array<int> array = new(span);
            Assert.That(array.Length, Is.EqualTo(8));
        }

        [Test]
        public void ResizeArray()
        {
            using Array<int> array = new(4);
            array.Length = 8;
            Assert.That(array.Length, Is.EqualTo(8));

            array[array.Length - 1] = 1;

            array.Length = 4;
            Assert.That(array.Length, Is.EqualTo(4));

            array.Length = 12;
            Assert.That(array.Length, Is.EqualTo(12));
        }

        [Test]
        public void ClearingArray()
        {
            using Array<int> array = new(4);
            array[0] = 1;
            array[1] = 2;
            array[2] = 3;
            array[3] = 4;
            array.Clear();
            Assert.That(array[0], Is.EqualTo(0));
            Assert.That(array[1], Is.EqualTo(0));
            Assert.That(array[2], Is.EqualTo(0));
            Assert.That(array[3], Is.EqualTo(0));
        }

        [Test]
        public void IndexingArray()
        {
            using Array<int> array = new(4);
            array[0] = 1;
            array[1] = 2;
            array[2] = 3;
            array[3] = 4;
            Assert.That(array[0], Is.EqualTo(1));
            Assert.That(array[1], Is.EqualTo(2));
            Assert.That(array[2], Is.EqualTo(3));
            Assert.That(array[3], Is.EqualTo(4));
        }
    }
}