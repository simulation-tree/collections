using Unmanaged;

namespace Collections.Pointers
{
    public struct Stack
    {
        public readonly uint stride;

        internal uint capacity;
        internal uint top;
        internal MemoryAddress items;

        internal Stack(uint stride, uint capacity)
        {
            this.stride = stride;
            this.capacity = capacity;
            this.top = 0;
            this.items = MemoryAddress.Allocate(stride * capacity);
        }
    }
}