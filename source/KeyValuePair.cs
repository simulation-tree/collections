namespace Collections
{
    public readonly struct KeyValuePair<K, V> where K : unmanaged where V : unmanaged
    {
        public readonly K key;
        public readonly V value;

        public KeyValuePair(K key, V value)
        {
            this.key = key;
            this.value = value;
        }

        public readonly void Deconstruct(out K key, out V value)
        {
            key = this.key;
            value = this.value;
        }
    }
}