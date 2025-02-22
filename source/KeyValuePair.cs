using System;
using System.Collections.Generic;

namespace Collections
{
    public readonly struct KeyValuePair<K, V> : IEquatable<KeyValuePair<K, V>> where K : unmanaged where V : unmanaged
    {
        public readonly K key;
        public readonly V value;

        public KeyValuePair(K key, V value)
        {
            this.key = key;
            this.value = value;
        }

        public override string ToString()
        {
            return $"{key}={value}";
        }

        public readonly void Deconstruct(out K key, out V value)
        {
            key = this.key;
            value = this.value;
        }

        public override bool Equals(object? obj)
        {
            return obj is KeyValuePair<K, V> pair && Equals(pair);
        }

        public bool Equals(KeyValuePair<K, V> other)
        {
            return EqualityComparer<K>.Default.Equals(key, other.key) && EqualityComparer<V>.Default.Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(key, value);
        }

        public static bool operator ==(KeyValuePair<K, V> left, KeyValuePair<K, V> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(KeyValuePair<K, V> left, KeyValuePair<K, V> right)
        {
            return !(left == right);
        }

        public static implicit operator KeyValuePair<K, V>((K key, V value) pair)
        {
            return new(pair.key, pair.value);
        }

        public static implicit operator (K key, V value)(KeyValuePair<K, V> pair)
        {
            return (pair.key, pair.value);
        }
    }
}