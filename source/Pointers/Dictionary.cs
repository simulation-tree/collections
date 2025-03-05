using Unmanaged;

namespace Collections.Pointers
{
    public struct Dictionary
    {
        public readonly uint keyStride;
        public readonly uint valueStride;

        internal uint count;
        internal uint capacity;
        internal MemoryAddress keys;
        internal MemoryAddress hashCodes;
        internal MemoryAddress values;
        internal MemoryAddress occupied;

        internal Dictionary(uint keyStride, uint valueStride, uint capacity)
        {
            this.keyStride = keyStride;
            this.valueStride = valueStride;
            this.capacity = capacity;
            keys = MemoryAddress.Allocate(keyStride * capacity);
            hashCodes = MemoryAddress.Allocate(capacity * sizeof(int));
            values = MemoryAddress.Allocate(valueStride * capacity);
            occupied = MemoryAddress.AllocateZeroed(capacity);
            count = 0;
        }
    }
}