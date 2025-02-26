using Unmanaged;

namespace Collections.Pointers
{
    public struct Queue
    {
        public readonly uint stride;

        internal uint capacity;
        internal uint top;
        internal uint rear;
        internal Allocation items;

        internal Queue(uint stride, uint capacity)
        {
            this.stride = stride;
            this.capacity = capacity;

            top = 0;
            rear = 0;
            items = Allocation.Create(stride * capacity);
        }
    }
}