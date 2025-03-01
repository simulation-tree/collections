using Unmanaged;

namespace Collections.Pointers
{
    /// <summary>
    /// Opaque pointer implementation of an array.
    /// </summary>
    public struct Array
    {
        public readonly uint stride;
        public uint length;
        public Allocation items;

        internal Array(uint stride, uint length, Allocation items)
        {
            this.stride = stride;
            this.length = length;
            this.items = items;
        }
    }
}