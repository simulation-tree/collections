using Unmanaged;

namespace Collections.Pointers
{
    public struct HashSet
    {
        public readonly int valueStride;

        internal int count;
        internal int capacity;
        internal MemoryAddress values;
        internal MemoryAddress hashCodes;
        internal MemoryAddress occupied;

        internal HashSet(int valueStride, int capacity)
        {
            this.valueStride = valueStride;
            this.capacity = capacity;
            values = MemoryAddress.Allocate(valueStride * capacity);
            hashCodes = MemoryAddress.Allocate(capacity * sizeof(int));
            occupied = MemoryAddress.AllocateZeroed(capacity);
            count = 0;
        }
    }
}