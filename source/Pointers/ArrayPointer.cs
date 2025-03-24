using Unmanaged;

namespace Collections.Pointers
{
    /// <summary>
    /// Opaque pointer implementation of an array.
    /// </summary>
    public struct ArrayPointer
    {
        public int stride;
        public int length;
        public MemoryAddress items;
    }
}