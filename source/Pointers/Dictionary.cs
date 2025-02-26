using Unmanaged;

namespace Collections.Pointers
{
    public struct Dictionary
    {
        public readonly uint keyStride;
        public readonly uint valueStride;

        internal uint count;
        internal uint capacity;
        internal Allocation keys;
        internal Allocation hashCodes;
        internal Allocation values;
        internal Allocation occupied;

        internal Dictionary(uint keyStride, uint valueStride, uint capacity)
        {
            this.keyStride = keyStride;
            this.valueStride = valueStride;
            this.capacity = capacity;
            keys = Allocation.Create(keyStride * capacity);
            hashCodes = Allocation.Create(capacity * sizeof(int));
            values = Allocation.Create(valueStride * capacity);
            occupied = Allocation.CreateZeroed(capacity);
            count = 0;
        }
    }
}