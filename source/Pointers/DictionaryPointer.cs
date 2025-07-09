using Unmanaged;

namespace Collections.Pointers
{
    public struct DictionaryPointer
    {
        public int keyStride;
        public int valueStride;
        public int count;
        public int capacity;
        public MemoryAddress keys;
        public MemoryAddress hashCodes;
        public MemoryAddress values;
        public MemoryAddress slotStates;
    }
}