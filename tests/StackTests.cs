using Unmanaged.Tests;

namespace Collections.Tests
{
    public class StackTests : UnmanagedTests
    {
        [Test]
        public void PushItemsThenPopAll()
        {
            using Stack<int> stack = new();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);
            stack.Push(5);

            Assert.That(stack.Count, Is.EqualTo(5));
            Assert.That(stack.Pop(), Is.EqualTo(5));
            Assert.That(stack.Pop(), Is.EqualTo(4));
            Assert.That(stack.Pop(), Is.EqualTo(3));
            Assert.That(stack.Pop(), Is.EqualTo(2));
            Assert.That(stack.Pop(), Is.EqualTo(1));
            Assert.That(stack.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClearAfterPushing()
        {
            using Stack<int> stack = new();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);
            stack.Push(5);
            Assert.That(stack.Count, Is.EqualTo(5));
            stack.Clear();
            Assert.That(stack.Count, Is.EqualTo(0));
            Assert.That(stack.Contains(1), Is.False);
            Assert.That(stack.Contains(2), Is.False);
            Assert.That(stack.Contains(3), Is.False);
            Assert.That(stack.Contains(4), Is.False);
            Assert.That(stack.Contains(5), Is.False);
        }

        [Test]
        public void PushRange()
        {
            using Stack<int> stack = new();
            stack.Push(1);
            Assert.That(stack.Count, Is.EqualTo(1));
            stack.Push(2);
            Assert.That(stack.Count, Is.EqualTo(2));
            stack.Push(3);
            Assert.That(stack.Count, Is.EqualTo(3));
            stack.Push(4);
            Assert.That(stack.Count, Is.EqualTo(4));
            stack.Push(5);
            Assert.That(stack.Count, Is.EqualTo(5));
            stack.PushRange([6, 7, 8, 9, 10]);
            Assert.That(stack.Count, Is.EqualTo(10));
            Assert.That(stack.Pop(), Is.EqualTo(10));
            Assert.That(stack.Pop(), Is.EqualTo(9));
            Assert.That(stack.Pop(), Is.EqualTo(8));
            Assert.That(stack.Pop(), Is.EqualTo(7));
            Assert.That(stack.Pop(), Is.EqualTo(6));
            Assert.That(stack.Pop(), Is.EqualTo(5));
            Assert.That(stack.Pop(), Is.EqualTo(4));
            Assert.That(stack.Pop(), Is.EqualTo(3));
            Assert.That(stack.Pop(), Is.EqualTo(2));
            Assert.That(stack.Pop(), Is.EqualTo(1));
            Assert.That(stack.Count, Is.EqualTo(0));
        }

        [Test]
        public void Contains()
        {
            using Stack<int> stack = new();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);
            stack.Push(5);
            Assert.That(stack.Contains(1), Is.True);
            Assert.That(stack.Contains(2), Is.True);
            Assert.That(stack.Contains(3), Is.True);
            Assert.That(stack.Contains(4), Is.True);
            Assert.That(stack.Contains(5), Is.True);
            Assert.That(stack.Contains(6), Is.False);
        }

        [Test]
        public void TryPeek()
        {
            using Stack<int> stack = new();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);
            stack.Push(5);
            Assert.That(stack.TryPeek(out int value), Is.True);
            Assert.That(value, Is.EqualTo(5));
            stack.Pop();
            Assert.That(stack.TryPeek(out value), Is.True);
            Assert.That(value, Is.EqualTo(4));
            stack.Pop();
            stack.Pop();
            stack.Pop();
            stack.Pop();
            Assert.That(stack.TryPeek(out value), Is.False);
        }
    }
}