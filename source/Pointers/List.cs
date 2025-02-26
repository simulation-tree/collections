using Unmanaged;

namespace Collections.Pointers
{
    /// <summary>
    /// Opaque pointer implementation of a list.
    /// </summary>
    public struct List
    {
        public readonly uint stride;

        internal uint count;
        internal uint capacity;
        internal Allocation items;

        internal List(uint stride, uint count, uint capacity, Allocation items)
        {
            this.stride = stride;
            this.count = count;
            this.capacity = capacity;
            this.items = items;
        }
    }
}
