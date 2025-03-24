using Unmanaged;

namespace Collections.Pointers
{
    /// <summary>
    /// Opaque pointer implementation of a list.
    /// </summary>
    public struct ListPointer
    {
        public int stride;
        public int count;
        public int capacity;
        public MemoryAddress items;
    }
}
