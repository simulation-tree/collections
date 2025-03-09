using Unmanaged;

namespace Collections.Pointers
{
    public struct Queue
    {
        public readonly int stride;

        internal int capacity;
        internal int top;
        internal int rear;
        internal MemoryAddress items;

        internal Queue(int stride, int capacity)
        {
            this.stride = stride;
            this.capacity = capacity;

            top = 0;
            rear = 0;
            items = MemoryAddress.Allocate(stride * capacity);
        }
    }
}