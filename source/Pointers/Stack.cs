using Unmanaged;

namespace Collections.Pointers
{
    public struct Stack
    {
        public readonly int stride;

        internal int capacity;
        internal int top;
        internal MemoryAddress items;

        internal Stack(int stride, int capacity)
        {
            this.stride = stride;
            this.capacity = capacity;
            this.top = 0;
            this.items = MemoryAddress.Allocate(stride * capacity);
        }
    }
}