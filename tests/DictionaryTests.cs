using System;
using Unmanaged;
using Unmanaged.Tests;

namespace Collections.Tests
{
    public class DictionaryTests : UnmanagedTests
    {
        [Test]
        public void CreatingAndDisposingDictionary()
        {
            Dictionary<byte, uint> map = new();
            map.Add(0, 23);
            map.Add(1, 42);
            map.Add(2, 69);
            Assert.That(map.ContainsKey(0), Is.True);
            Assert.That(map.ContainsKey(1), Is.True);
            Assert.That(map.ContainsKey(2), Is.True);
            Assert.That(map.ContainsKey(3), Is.False);
            Assert.That(map[0], Is.EqualTo(23));
            Assert.That(map[1], Is.EqualTo(42));
            Assert.That(map[2], Is.EqualTo(69));
            map.Dispose();

            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void CantAddDuplicateKeys()
        {
            using Dictionary<byte, uint> map = new();
            map.Add(0, 23);
            Assert.Throws<ArgumentException>(() => map.Add(0, 42));
        }

        [Test]
        public void TryGetValueFromDictionary()
        {
            using Dictionary<byte, uint> map = new();
            map.Add(0, 23);
            map.Add(1, 42);
            map.Add(2, 69);

            Assert.That(map.TryGetValue(0, out uint value1), Is.True);
            Assert.That(map.TryGetValue(1, out uint value2), Is.True);
            Assert.That(map.TryGetValue(2, out uint value3), Is.True);
            Assert.That(map.TryGetValue(3, out uint value4), Is.False);
            Assert.That(value1, Is.EqualTo(23));
            Assert.That(value2, Is.EqualTo(42));
            Assert.That(value3, Is.EqualTo(69));

            map.Remove(0);

            Assert.That(map.TryGetValue(0, out uint value5), Is.False);
        }
    }
}
