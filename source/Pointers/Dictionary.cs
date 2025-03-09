using Unmanaged;

namespace Collections.Pointers
{
    public struct Dictionary
    {
        public readonly int keyStride;
        public readonly int valueStride;

        internal int count;
        internal int capacity;
        internal MemoryAddress keys;
        internal MemoryAddress hashCodes;
        internal MemoryAddress values;
        internal MemoryAddress occupied;

        internal Dictionary(int keyStride, int valueStride, int capacity)
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