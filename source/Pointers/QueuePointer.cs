using Unmanaged;

namespace Collections.Pointers
{
    public struct QueuePointer
    {
        public int stride;
        public int capacity;
        public int top;
        public int rear;
        public MemoryAddress items;
    }
}