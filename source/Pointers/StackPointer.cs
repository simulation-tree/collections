using Unmanaged;

namespace Collections.Pointers
{
    public struct StackPointer
    {
        public int stride;
        public int capacity;
        public int top;
        public MemoryAddress items;
    }
}