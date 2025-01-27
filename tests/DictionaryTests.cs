using System;
using Unmanaged;
using Unmanaged.Tests;

namespace Collections.Tests
{
    public class DictionaryTests : UnmanagedTests
    {
        [Test]
        public void AddOneEntry()
        {
            using Dictionary<byte, int> map = new();
            map.Add(1, 1337);
            Assert.That(map.ContainsKey(1), Is.True);
            Assert.That(map.Count, Is.EqualTo(1));
            Assert.That(map.TryGetValue(1, out int value), Is.True);
            Assert.That(value, Is.EqualTo(1337));
        }

        [Test]
        public void Clearing()
        {
            using Dictionary<byte, int> map = new();
            map.Add(1, 1337);
            map.Clear();
            Assert.That(map.Count, Is.EqualTo(0));
            Assert.That(map.ContainsKey(1), Is.False);
            Assert.That(map.TryGetValue(1, out int value), Is.False);
        }

        [Test]
        public void AddOrSet()
        {
            using Dictionary<byte, int> map = new();
            map.Add(1, 1337);
            map.Add(2, 2007);
            map.Add(33, 8008135);
            map.AddOrSet(1, 123);
            map.AddOrSet(2, 123);
            map.AddOrSet(5, 232323);

            Assert.That(map.Count, Is.EqualTo(4));
            Assert.That(map.ContainsKey(1), Is.True);
            Assert.That(map.ContainsKey(2), Is.True);
            Assert.That(map.ContainsKey(33), Is.True);
            Assert.That(map.ContainsKey(5), Is.True);
            Assert.That(map[1], Is.EqualTo(123));
            Assert.That(map[2], Is.EqualTo(123));
            Assert.That(map[33], Is.EqualTo(8008135));
            Assert.That(map[5], Is.EqualTo(232323));
        }

        [Test]
        public void AddManyValues()
        {
            using Dictionary<byte, int> map = new();
            map.Add(1, 1337);
            map.Add(2, 2007);
            map.Add(33, 8008135);
            map.Add(100, 42);
            Assert.That(map.Count, Is.EqualTo(4));
            Assert.That(map.ContainsKey(1), Is.True);
            Assert.That(map.ContainsKey(2), Is.True);
            Assert.That(map.ContainsKey(33), Is.True);
            Assert.That(map.ContainsKey(100), Is.True);
            Assert.That(map.TryGetValue(1, out int value1), Is.True);
            Assert.That(value1, Is.EqualTo(1337));
            Assert.That(map.TryGetValue(2, out int value2), Is.True);
            Assert.That(value2, Is.EqualTo(2007));
            Assert.That(map.TryGetValue(33, out int value3), Is.True);
            Assert.That(value3, Is.EqualTo(8008135));
            Assert.That(map.TryGetValue(100, out int value4), Is.True);
            Assert.That(value4, Is.EqualTo(42));
        }

        [Test]
        public void ManuallyClearing()
        {
            using Dictionary<byte, int> map = new();
            map.Add(1, 1337);
            map.Add(2, 2007);
            map.Add(33, 8008135);
            map.Add(100, 42);
            Assert.That(map.Count, Is.EqualTo(4));
            map.TryRemove(100, out int value1);
            map.TryRemove(33, out int value2);
            map.TryRemove(2, out int value3);
            map.TryRemove(1, out int value4);
            Assert.That(map.Count, Is.EqualTo(0));
            Assert.That(value1, Is.EqualTo(42));
            Assert.That(value2, Is.EqualTo(8008135));
            Assert.That(value3, Is.EqualTo(2007));
            Assert.That(value4, Is.EqualTo(1337));
        }

        [Test]
        public void ModifyExistingKeys()
        {
            using Dictionary<byte, int> map = new();
            map.Add(1, 1337);
            map.Set(1, 123);
            Assert.That(map.Count, Is.EqualTo(1));
            Assert.That(map.TryGetValue(1, out int value), Is.True);
            Assert.That(value, Is.EqualTo(123));
        }

#if DEBUG
        [Test]
        public void AddSameKey()
        {
            using Dictionary<byte, int> map = new();
            map.Add(1, 1337);
            Assert.That(map.Count, Is.EqualTo(1));
            Assert.That(map.ContainsKey(1), Is.True);
            Assert.Throws<InvalidOperationException>(() => map.Add(1, 42));
        }

        [Test]
        public void ThrowIfModifyingNonExistentKeys()
        {
            using Dictionary<byte, int> map = new();
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => map.Set(1, 123));
        }

        [Test]
        public void ThrowWhenRemovingNonExistentKey()
        {
            using Dictionary<byte, uint> map = new();
            map.Add(0, 23);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => map.Remove(1));
        }
#endif

        [Test]
        public void CantRemoveNonExistentEntries()
        {
            using Dictionary<byte, int> map = new();
            bool removed = map.TryRemove(1);
            Assert.That(removed, Is.False);
        }

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
            Assert.That(map.TryAdd(0, 42), Is.False);
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

        [Test]
        public void IterateAllKeys()
        {
            using Dictionary<byte, uint> map = new();
            map.Add(0, 23);
            map.Add(1, 42);
            map.Add(23, 69);

            using List<byte> keys = new();
            foreach (byte key in map.Keys)
            {
                keys.Add(key);
            }

            Assert.That(keys.Count, Is.EqualTo(3));
            Assert.That(keys[0], Is.EqualTo(0));
            Assert.That(keys[1], Is.EqualTo(1));
            Assert.That(keys[2], Is.EqualTo(23));
        }

        [Test]
        public void RemoveKeysThenIterate()
        {
            using Dictionary<byte, uint> map = new();
            map.Add(0, 23);
            map.Add(1, 42);
            map.Add(23, 69);
            map.Remove(23);
            map.Remove(1);
            Assert.That(map.Count, Is.EqualTo(1));
            map.Add(2, 1337);
            Assert.That(map.Count, Is.EqualTo(2));
            map.Add(23, 2007);
            Assert.That(map.Count, Is.EqualTo(3));

            using List<byte> keys = new();
            foreach (byte key in map.Keys)
            {
                keys.Add(key);
                Assert.That(map.ContainsKey(key), Is.True);
                Assert.That(map.TryGetValue(key, out uint value), Is.True);
                if (key == 0)
                {
                    Assert.That(value, Is.EqualTo(23));
                }
                else if (key == 2)
                {
                    Assert.That(value, Is.EqualTo(1337));
                }
                else if (key == 23)
                {
                    Assert.That(value, Is.EqualTo(2007));
                }
            }

            Assert.That(keys.Count, Is.EqualTo(3));
            Assert.That(keys[0], Is.EqualTo(0));
        }

        [Test]
        public void EnumerateAllPairs()
        {
            using Dictionary<byte, uint> map = new();
            map.Add(0, 23);

            using List<KeyValuePair<byte, uint>> pairs = new();
            foreach ((byte key, uint value) pair in map)
            {
                pairs.Add(pair);
            }

            Assert.That(pairs.Count, Is.EqualTo(1));
            Assert.That(pairs[0].key, Is.EqualTo(0));
            Assert.That(pairs[0].value, Is.EqualTo(23));

            map.Add(1, 42);
            map.Add(50, 1337);
            map.Add(3, 8008135);
            map.Remove(0);
            map.Remove(1);
            map.Add(0, 1234);
            map.Add(4, map[50]);

            pairs.Clear();
            foreach ((byte key, uint value) pair in map)
            {
                pairs.Add(pair);
            }

            Assert.That(pairs.Count, Is.EqualTo(4));
            Assert.That(pairs.Contains(new(0, 1234)), Is.True);
            Assert.That(pairs.Contains(new(50, 1337)), Is.True);
            Assert.That(pairs.Contains(new(3, 8008135)), Is.True);
            Assert.That(pairs.Contains(new(4, 1337)), Is.True);
        }
    }
}
