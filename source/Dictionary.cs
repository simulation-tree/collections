using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Implementation = Collections.Implementations.Dictionary;

namespace Collections
{
    /// <summary>
    /// Native dictionary that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Dictionary<K, V> : IDisposable, IReadOnlyDictionary<K, V>, IDictionary<K, V>, IEquatable<Dictionary<K, V>> where K : unmanaged, IEquatable<K> where V : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Implementation* dictionary;

        /// <summary>
        /// Number of key-value pairs in the dictionary.
        /// </summary>
        public readonly uint Count => Implementation.GetCount(dictionary);

        /// <summary>
        /// Capacity of the dictionary.
        /// </summary>
        public readonly uint Capacity => Implementation.GetCapacity(dictionary);

        /// <summary>
        /// Checks if the dictionary has been disposed.
        /// </summary>
        public readonly bool IsDisposed => dictionary is null;

        /// <summary>
        /// Accesses the value associated with the specified key.
        /// <para>
        /// May throw <see cref="NullReferenceException"/> if the key is not found.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public readonly ref V this[K key] => ref Implementation.GetValue<K, V>(dictionary, key);

        /// <summary>
        /// All keys in this dictionary.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly IEnumerable<K> Keys
        {
            get
            {
                uint capacity = Capacity;
                for (uint i = 0; i < capacity; i++)
                {
                    bool contains;
                    KeyValuePair<K, V> pair;
                    unsafe
                    {
                        contains = Implementation.TryGetPair(dictionary, i, out pair);
                    }

                    if (contains)
                    {
                        yield return pair.key;
                    }
                }
            }
        }

        /// <summary>
        /// All values in this dictionary.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly IEnumerable<V> Values
        {
            get
            {
                uint capacity = Capacity;
                for (uint i = 0; i < capacity; i++)
                {
                    bool contains;
                    KeyValuePair<K, V> pair;
                    unsafe
                    {
                        contains = Implementation.TryGetPair(dictionary, i, out pair);
                    }

                    if (contains)
                    {
                        yield return pair.value;
                    }
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly ICollection<K> IDictionary<K, V>.Keys
        {
            get
            {
                K[] keys = new K[Count];
                uint index = 0;
                foreach (K key in Keys)
                {
                    keys[index++] = key;
                }

                return keys;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly ICollection<V> IDictionary<K, V>.Values
        {
            get
            {
                V[] values = new V[Count];
                uint index = 0;
                foreach (V value in Values)
                {
                    values[index++] = value;
                }

                return values;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly int ICollection<System.Collections.Generic.KeyValuePair<K, V>>.Count => (int)Count;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly bool ICollection<System.Collections.Generic.KeyValuePair<K, V>>.IsReadOnly => false;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly int IReadOnlyCollection<System.Collections.Generic.KeyValuePair<K, V>>.Count => (int)Count;

        readonly V IReadOnlyDictionary<K, V>.this[K key] => Implementation.GetValue<K, V>(dictionary, key);

        readonly V IDictionary<K, V>.this[K key]
        {
            get => Implementation.GetValue<K, V>(dictionary, key);
            set => Implementation.GetValue<K, V>(dictionary, key) = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly KeyValuePair<K, V>[] Pairs
        {
            get
            {
                KeyValuePair<K, V>[] pairs = new KeyValuePair<K, V>[Count];
                uint index = 0;
                foreach ((K key, V value) in this)
                {
                    pairs[index++] = new(key, value);
                }

                return pairs;
            }
        }

        /// <summary>
        /// Initializes an existing dictionary from the given <paramref name="pointer"/>.
        /// </summary>
        public Dictionary(Implementation* pointer)
        {
            dictionary = pointer;
        }

        /// <summary>
        /// Creates a new dictionary with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Dictionary(uint initialCapacity = 4)
        {
            dictionary = Implementation.Allocate<K, V>(initialCapacity);
        }

#if NET
        /// <summary>
        /// Creates a new dictionary.
        /// </summary>
        public Dictionary()
        {
            dictionary = Implementation.Allocate<K, V>(4);
        }
#endif

        /// <summary>
        /// Disposes the dictionary.
        /// <para>Keys and values need to be disposed manually prior to
        /// calling this if they are allocations/disposable themselves.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            Implementation.Free(ref dictionary);
        }

        /// <summary>
        /// Checks if the dictionary contains the given <paramref name="key"/>.
        /// </summary>
        public readonly bool ContainsKey(K key)
        {
            return Implementation.ContainsKey<K, V>(dictionary, key);
        }

        /// <summary>
        /// Attempts to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryGetValue(K key, out V value)
        {
            return Implementation.TryGetValue(dictionary, key, out value);
        }

        /// <summary>
        /// Attempts to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Reference to the value if <paramref name="contains"/> is true.</returns>
        public readonly ref V TryGetValue(K key, out bool contains)
        {
            return ref Implementation.TryGetValue<K, V>(dictionary, key, out contains);
        }

        /// <summary>
        /// Attempts to add the specified <paramref name="key"/> and <paramref name="value"/> pair to the dictionary.
        /// </summary>
        /// <returns><c>true</c> if successful.</returns>
        public readonly bool TryAdd(K key, V value)
        {
            return Implementation.TryAdd(dictionary, key, value);
        }

        /// <summary>
        /// Adds the given <paramref name="key"/> and <paramref name="value"/> pairs to the dictionary.
        /// <para>
        /// Throws <see cref="InvalidOperationException"/> if the key already exists.
        /// </para>
        /// </summary>
        public readonly ref V Add(K key, V value)
        {
            ref V existingValue = ref Implementation.Add<K, V>(dictionary, key);
            existingValue = value;
            return ref existingValue;
        }

        /// <summary>
        /// Assigns the specified <paramref name="value"/> to the <paramref name="key"/>.
        /// <para>
        /// May throw <see cref="KeyNotFoundException"/> if the key is not found.
        /// </para>
        /// </summary>
        /// <exception cref="KeyNotFoundException"></exception>
        public readonly void Set(K key, V value)
        {
            ref V existingValue = ref Implementation.GetValue<K, V>(dictionary, key);
            existingValue = value;
        }

        /// <summary>
        /// Adds the specified <paramref name="key"/> and <paramref name="value"/> pair to the dictionary,
        /// or sets if already contained.
        /// </summary>
        /// <returns><c>true</c> if the key was added, <c>false</c> if set.</returns>
        public readonly bool AddOrSet(K key, V value)
        {
            ref V existingValue = ref Implementation.TryGetValue<K, V>(dictionary, key, out bool contains);
            if (!contains)
            {
                existingValue = ref Implementation.Add<K, V>(dictionary, key);
            }

            existingValue = value;
            return !contains;
        }

        /// <summary>
        /// Adds an empty value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Reference to the added value.</returns>
        public readonly ref V Add(K key)
        {
            return ref Implementation.Add<K, V>(dictionary, key);
        }

        /// <summary>
        /// Removes the value associated with the specified <paramref name="key"/>.
        /// <para>
        /// May throw <see cref="NullReferenceException"/> if the key is not found.
        /// </para>
        /// </summary>
        /// <returns>The removed value.</returns>
        public readonly V Remove(K key)
        {
            return Implementation.Remove<K, V>(dictionary, key);
        }

        /// <summary>
        /// Attempts to remove the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found and removed.</returns>
        public readonly bool TryRemove(K key, out V removed)
        {
            return Implementation.TryRemove(dictionary, key, out removed);
        }

        /// <summary>
        /// Attempts to remove the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found and removed.</returns>
        public readonly bool TryRemove(K key)
        {
            return Implementation.TryRemove<K, V>(dictionary, key, out _);
        }

        /// <summary>
        /// Clears the dictionary.
        /// </summary>
        public readonly void Clear()
        {
            Implementation.Clear(dictionary);
        }

        readonly void IDictionary<K, V>.Add(K key, V value)
        {
            Add(key, value);
        }

        readonly bool IDictionary<K, V>.Remove(K key)
        {
            return TryRemove(key);
        }

        readonly void ICollection<System.Collections.Generic.KeyValuePair<K, V>>.Add(System.Collections.Generic.KeyValuePair<K, V> item)
        {
            Add(item.Key, item.Value);
        }

        readonly bool ICollection<System.Collections.Generic.KeyValuePair<K, V>>.Contains(System.Collections.Generic.KeyValuePair<K, V> item)
        {
            return TryGetValue(item.Key, out V value) && EqualityComparer<V>.Default.Equals(value, item.Value);
        }

        readonly void ICollection<System.Collections.Generic.KeyValuePair<K, V>>.CopyTo(System.Collections.Generic.KeyValuePair<K, V>[] array, int arrayIndex)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if ((uint)arrayIndex >= (uint)array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("The number of elements in the dictionary is greater than the available space from the index to the end of the destination array");
            }

            uint count = Capacity;
            for (uint i = 0; i < count; i++)
            {
                if (Implementation.TryGetPair(dictionary, i, out KeyValuePair<K, V> pair))
                {
                    array[arrayIndex++] = new(pair.key, pair.value);
                }
            }
        }

        readonly bool ICollection<System.Collections.Generic.KeyValuePair<K, V>>.Remove(System.Collections.Generic.KeyValuePair<K, V> item)
        {
            return TryRemove(item.Key);
        }

        readonly IEnumerator<System.Collections.Generic.KeyValuePair<K, V>> IEnumerable<System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()
        {
            return new SystemEnumerator(dictionary);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public readonly Enumerator GetEnumerator()
        {
            return new(dictionary);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Dictionary<K, V> dictionary && Equals(dictionary);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Dictionary<K, V> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return dictionary == other.dictionary;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)dictionary).GetHashCode();
        }

        public static bool operator ==(Dictionary<K, V> left, Dictionary<K, V> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Dictionary<K, V> left, Dictionary<K, V> right)
        {
            return !(left == right);
        }

        public struct Enumerator : IEnumerator<(K key, V value)>
        {
            private readonly Implementation* map;
            private readonly uint capacity;
            private int index;

            public readonly (K key, V value) Current
            {
                get
                {
                    ref Implementation.Entry<K, V> entry = ref Implementation.GetEntry<K, V>(map, (uint)index);
                    return (entry.key, entry.value);
                }
            }

            readonly object IEnumerator.Current => Current;

            internal Enumerator(Implementation* map)
            {
                this.map = map;
                index = -1;
                capacity = Implementation.GetCapacity(map);
            }

            public bool MoveNext()
            {
                while (++index < capacity)
                {
                    if (Implementation.TryGetPair<K, V>(map, (uint)index, out _))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                index = -1;
            }

            readonly void IDisposable.Dispose()
            {
            }
        }

        public struct SystemEnumerator : IEnumerator<System.Collections.Generic.KeyValuePair<K, V>>
        {
            private readonly Implementation* map;
            private readonly uint capacity;
            private int index;
            public readonly System.Collections.Generic.KeyValuePair<K, V> Current
            {
                get
                {
                    ref Implementation.Entry<K, V> entry = ref Implementation.GetEntry<K, V>(map, (uint)index);
                    return new(entry.key, entry.value);
                }
            }

            readonly object IEnumerator.Current => Current;

            internal SystemEnumerator(Implementation* map)
            {
                this.map = map;
                index = -1;
                capacity = Implementation.GetCapacity(map);
            }

            public bool MoveNext()
            {
                while (++index < capacity)
                {
                    ref Implementation.Entry<K, V> entry = ref Implementation.GetEntry<K, V>(map, (uint)index);
                    if (entry.state == Implementation.EntryState.Occupied)
                    {
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                index = -1;
            }

            readonly void IDisposable.Dispose()
            {
            }
        }
    }
}
