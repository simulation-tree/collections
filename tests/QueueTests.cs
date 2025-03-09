using Collections.Generic;
using System;
using Unmanaged.Tests;

namespace Collections.Tests
{
    public class QueueTests : UnmanagedTests
    {
        [Test]
        public void QueueThenEmptyManually()
        {
            using Queue<char> queue = new();
            queue.Enqueue('a');
            queue.Enqueue('b');
            queue.Enqueue('c');

            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.IsEmpty, Is.False);
            Assert.That(queue.Dequeue(), Is.EqualTo('a'));
            Assert.That(queue.Dequeue(), Is.EqualTo('b'));
            Assert.That(queue.Dequeue(), Is.EqualTo('c'));
            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.IsEmpty, Is.True);
        }

        [Test]
        public void SpanAfterDequeing()
        {
            using Queue<char> queue = new();
            queue.Enqueue('a');
            queue.Enqueue('b');
            queue.Enqueue('c');
            queue.Enqueue('d');
            queue.Enqueue('e');
            queue.Enqueue('f');

            Assert.That(queue.Count, Is.EqualTo(6));

            queue.Dequeue();
            queue.Dequeue();

            Assert.That(queue.Count, Is.EqualTo(4));

            Span<char> span = queue.AsSpan();

            Assert.That(span.Length, Is.EqualTo(4));
            Assert.That(span[0], Is.EqualTo('c'));
            Assert.That(span[1], Is.EqualTo('d'));
            Assert.That(span[2], Is.EqualTo('e'));
            Assert.That(span[3], Is.EqualTo('f'));
        }

        [Test]
        public void Clearing()
        {
            using Queue<char> queue = new();
            queue.Enqueue('a');
            queue.Enqueue('b');
            queue.Enqueue('c');
            queue.Enqueue('d');
            queue.Enqueue('e');
            queue.Enqueue('f');
            Assert.That(queue.Count, Is.EqualTo(6));
            queue.Clear();
            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.IsEmpty, Is.True);
        }

        [Test]
        public void EnqueueAndDequeue()
        {
            using Queue<char> queue = new();
            queue.Enqueue('a');
            queue.Enqueue('b');
            queue.Enqueue('c');
            queue.Enqueue('d');
            queue.Enqueue('e');
            queue.Enqueue('f');
            Assert.That(queue.Count, Is.EqualTo(6));
            Assert.That(queue.Dequeue(), Is.EqualTo('a'));
            queue.Enqueue('g');
            Assert.That(queue.Count, Is.EqualTo(6));
            Assert.That(queue.Dequeue(), Is.EqualTo('b'));
            Assert.That(queue.TryDequeue(out _), Is.True);
            queue.Enqueue('h');
            Assert.That(queue.Count, Is.EqualTo(5));
            Assert.That(queue.Dequeue(), Is.EqualTo('d'));
            Assert.That(queue.Dequeue(), Is.EqualTo('e'));
            Assert.That(queue.TryDequeue(out _), Is.True);
            queue.Enqueue('i');
            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.TryDequeue(out _), Is.True);
            Assert.That(queue.Dequeue(), Is.EqualTo('h'));
            Assert.That(queue.Dequeue(), Is.EqualTo('i'));
            Assert.That(queue.Count, Is.EqualTo(0));
            Assert.That(queue.TryDequeue(out _), Is.False);
        }
    }
}