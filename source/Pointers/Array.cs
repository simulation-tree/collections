using Unmanaged;

namespace Collections.Pointers
{
    /// <summary>
    /// Opaque pointer implementation of an array.
    /// </summary>
    public struct Array
    {
        public readonly int stride;
        public int length;
        public MemoryAddress items;

        internal Array(int stride, int length, MemoryAddress items)
        {
            this.stride = stride;
            this.length = length;
            this.items = items;
        }
    }
}