using Unmanaged;

namespace Collections.Pointers
{
    public struct HashSetPointer
    {
        public int stride;
        public int count;
        public int capacity;
        public MemoryAddress values;
        public MemoryAddress hashCodes;
        public MemoryAddress occupied;
    }
}