using Unmanaged.Tests;

namespace Collections.Tests
{
    public class TextTests : UnmanagedTests
    {
        [Test]
        public void CreateTextFromSpan()
        {
            using Text text = "Hello there";
            Assert.That(text.ToString(), Is.EqualTo("Hello there"));
        }

        [Test]
        public void ResizeText()
        {
            using Text text = "Apple";
            text.SetLength(10, 'x');

            Assert.That(text.Length, Is.EqualTo(10));
            Assert.That(text.ToString(), Is.EqualTo("Applexxxxx"));

            text.SetLength(3);
            Assert.That(text.Length, Is.EqualTo(3));
            Assert.That(text.ToString(), Is.EqualTo("App"));
        }

        [Test]
        public void CopyFrom()
        {
            using Text text = "";
            text.CopyFrom("Hello there");

            Assert.That(text.ToString(), Is.EqualTo("Hello there"));
        }

        [Test]
        public void ConcatenateText()
        {
            using Text a = "This";
            using Text b = " a test";
            using Text result = a + b;

            Assert.That(result.ToString(), Is.EqualTo("This a test"));
        }

        [Test]
        public void Enumerate()
        {
            using Text text = "Something in here";
            using List<char> list = new();
            foreach (char c in text)
            {
                list.Add(c);
            }

            Assert.That(list.AsSpan().ToString(), Is.EqualTo(text.ToString()));
        }
    }
}
