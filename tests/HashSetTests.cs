using Collections.Generic;
using Unmanaged.Tests;

namespace Collections.Tests
{
    public class HashSetTests : UnmanagedTests
    {
        [Test]
        public void AddMultipleValues()
        {
            using HashSet<int> hashSet = new(4);
            hashSet.Add(1);
            hashSet.Add(2);
            hashSet.Add(3);
            Assert.That(hashSet.Count, Is.EqualTo(3));
        }

        [Test]
        public void ClearThenAddAgain()
        {
            using HashSet<int> hashSet = new(4);
            hashSet.Add(1);
            hashSet.Add(2);
            hashSet.Add(3);
            Assert.That(hashSet.Count, Is.EqualTo(3));
            hashSet.Clear();
            Assert.That(hashSet.Count, Is.EqualTo(0));
            hashSet.Add(1);
            hashSet.Add(2);
            hashSet.Add(3);
            hashSet.Add(4);
            hashSet.Add(5);
            Assert.That(hashSet.Count, Is.EqualTo(5));
            Assert.That(hashSet.TryAdd(1), Is.False);
            Assert.That(hashSet.Count, Is.EqualTo(5));
            hashSet.Clear();
            Assert.That(hashSet.Count, Is.EqualTo(0));
            Assert.That(hashSet.TryAdd(1), Is.True);
            Assert.That(hashSet.TryAdd(5), Is.True);
            Assert.That(hashSet.Count, Is.EqualTo(2));
        }

        [Test]
        public void CantAddDuplicates()
        {
            using HashSet<int> hashSet = new(4);
            hashSet.TryAdd(1);
            hashSet.TryAdd(2);
            hashSet.TryAdd(3);
            Assert.That(hashSet.Count, Is.EqualTo(3));
            Assert.That(hashSet.TryAdd(1), Is.False);
            Assert.That(hashSet.Count, Is.EqualTo(3));
        }

        [Test]
        public void RemoveValues()
        {
            using HashSet<int> hashSet = new(4);
            hashSet.Add(1);
            hashSet.Add(2);
            hashSet.Add(3);
            Assert.That(hashSet.Count, Is.EqualTo(3));
            Assert.That(hashSet.TryRemove(1), Is.True);
            Assert.That(hashSet.Contains(1), Is.False);
            Assert.That(hashSet.Contains(2), Is.True);
            Assert.That(hashSet.Contains(3), Is.True);
            Assert.That(hashSet.Count, Is.EqualTo(2));
            Assert.That(hashSet.TryRemove(1), Is.False);
            Assert.That(hashSet.Count, Is.EqualTo(2));
            Assert.That(hashSet.TryRemove(2), Is.True);
            Assert.That(hashSet.Contains(1), Is.False);
            Assert.That(hashSet.Contains(2), Is.False);
            Assert.That(hashSet.Contains(3), Is.True);
            Assert.That(hashSet.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveUntilEmpty()
        {
            using HashSet<int> hashSet = new(4);
            hashSet.Add(1);
            hashSet.Add(2);
            hashSet.Add(3);
            Assert.That(hashSet.Count, Is.EqualTo(3));
            hashSet.Remove(3);
            Assert.That(hashSet.Count, Is.EqualTo(2));
            hashSet.Remove(2);
            Assert.That(hashSet.Count, Is.EqualTo(1));
            hashSet.Remove(1);
            Assert.That(hashSet.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetExistingValue()
        {
            using HashSet<int> hashSet = new(4);
            hashSet.Add(1);
            hashSet.Add(2);
            hashSet.Add(123123);
            Assert.That(hashSet.Count, Is.EqualTo(3));
            Assert.That(hashSet.TryGetValue(123123, out int value), Is.True);
            Assert.That(value, Is.EqualTo(123123));
        }

        [Test]
        public void IterateThroughValues()
        {
            using HashSet<int> hashSet = new(4);
            hashSet.Add(1);
            hashSet.Add(2);
            hashSet.Add(3);

            Assert.That(hashSet.Count, Is.EqualTo(3));
            int[] values = new int[hashSet.Count];
            int count = 0;
            foreach (int value in hashSet)
            {
                values[count] = value;
                count++;
            }

            Assert.That(count, Is.EqualTo(3));
            Assert.That(values[0], Is.EqualTo(1));
            Assert.That(values[1], Is.EqualTo(2));
            Assert.That(values[2], Is.EqualTo(3));
        }
    }
}