using Unmanaged;

namespace Collections.Pointers
{
    /// <summary>
    /// Opaque pointer implementation of a list.
    /// </summary>
    public struct List
    {
        public readonly int stride;
        public int count;
        public int capacity;
        public MemoryAddress items;

        internal List(int stride, int count, int capacity, MemoryAddress items)
        {
            this.stride = stride;
            this.count = count;
            this.capacity = capacity;
            this.items = items;
        }
    }
}
