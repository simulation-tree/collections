using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using static Collections.Implementations.Dictionary;
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
        public readonly uint Count => dictionary->count;

        /// <summary>
        /// Capacity of the dictionary.
        /// </summary>
        public readonly uint Capacity => dictionary->capacity;

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
        public readonly ref V this[K key]
        {
            get
            {
                Allocations.ThrowIfNull(dictionary);
                ThrowIfKeyIsMissing(dictionary, key);

                uint capacity = dictionary->capacity;
                uint hashCode = GetHash(key);
                uint index = hashCode % capacity;
                uint startIndex = index;
                USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, capacity);
                USpan<K> keys = dictionary->keys.AsSpan<K>(0, capacity);
                USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, capacity);

                while (occupied[index])
                {
                    if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                    {
                        return ref dictionary->values.ReadElement<V>(index);
                    }

                    index = (index + 1) % capacity;
                    if (index == startIndex)
                    {
                        break;
                    }
                }

                return ref *(V*)default(nint);
            }
        }

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
                    if (TryGetPair(i, out KeyValuePair<K, V> pair))
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
                    if (TryGetPair(i, out KeyValuePair<K, V> pair))
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

        readonly V IReadOnlyDictionary<K, V>.this[K key] => Get<K, V>(dictionary, key);

        readonly V IDictionary<K, V>.this[K key]
        {
            get => Get<K, V>(dictionary, key);
            set => Set<K, V>(dictionary, key, value);
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
            dictionary = Allocate<K, V>(initialCapacity);
        }

#if NET
        /// <summary>
        /// Creates a new dictionary.
        /// </summary>
        public Dictionary()
        {
            dictionary = Allocate<K, V>(4);
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
            Free(ref dictionary);
        }

        private readonly bool TryGetPair(uint index, out KeyValuePair<K, V> pair)
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfOutOfRange(dictionary, index);

            pair = new KeyValuePair<K, V>(dictionary->keys.ReadElement<K>(index), dictionary->values.ReadElement<V>(index));
            return dictionary->occupied.ReadElement<bool>(index);
        }

        /// <summary>
        /// Checks if the dictionary contains the given <paramref name="key"/>.
        /// </summary>
        public readonly bool ContainsKey(K key)
        {
            Allocations.ThrowIfNull(dictionary);

            uint capacity = dictionary->capacity;
            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    return true;
                }

                index = (index + 1) % capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryGetValue(K key, out V value)
        {
            Allocations.ThrowIfNull(dictionary);

            uint capacity = dictionary->capacity;
            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    value = dictionary->values.ReadElement<V>(index);
                    return true;
                }

                index = (index + 1) % capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Attempts to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Reference to the value if <paramref name="contains"/> is true.</returns>
        public readonly ref V TryGetValue(K key, out bool contains)
        {
            Allocations.ThrowIfNull(dictionary);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    contains = true;
                    return ref dictionary->values.ReadElement<V>(index);
                }

                index = (index + 1) % dictionary->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            contains = false;
            return ref *(V*)default(nint);
        }

        /// <summary>
        /// Attempts to add the specified <paramref name="key"/> and <paramref name="value"/> pair to the dictionary.
        /// </summary>
        /// <returns><c>true</c> if successful.</returns>
        public readonly bool TryAdd(K key, V value)
        {
            Allocations.ThrowIfNull(dictionary);

            uint capacity = dictionary->capacity;
            if (dictionary->count == capacity)
            {
                Resize<K, V>(dictionary);
                capacity = dictionary->capacity;
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    return false;
                }

                index = (index + 1) % capacity;
                if (index == startIndex)
                {
                    return false;
                }
            }

            occupied[index] = true;
            keys[index] = key;
            keyHashCodes[index] = hashCode;
            dictionary->values.WriteElement(index, value);
            dictionary->count++;
            return true;
        }

        /// <summary>
        /// Adds the given <paramref name="key"/> and <paramref name="value"/> pair to the dictionary.
        /// <para>
        /// In debug mode, throws <see cref="InvalidOperationException"/> if the key already exists.
        /// </para>
        /// </summary>
        public readonly void Add(K key, V value)
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyAlreadyPresent(dictionary, key);

            uint capacity = dictionary->capacity;
            uint count = dictionary->count;
            if (count == capacity)
            {
                Resize<K, V>(dictionary);
                capacity = dictionary->capacity;
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, capacity);
            while (occupied[index])
            {
                index = (index + 1) % capacity;
            }

            occupied[index] = true;
            dictionary->keys.WriteElement(index, key);
            dictionary->values.WriteElement(index, value);
            dictionary->hashCodes.WriteElement(index, hashCode);
            dictionary->count = count + 1;
        }

        /// <summary>
        /// Adds the given <paramref name="key"/> and <paramref name="value"/> pair to the dictionary.
        /// <para>
        /// In debug mode, throws <see cref="InvalidOperationException"/> if the key already exists.
        /// </para>
        /// </summary>
        public readonly void Add(K key, ref V value)
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyAlreadyPresent(dictionary, key);

            uint capacity = dictionary->capacity;
            uint count = dictionary->count;
            if (count == capacity)
            {
                Resize<K, V>(dictionary);
                capacity = dictionary->capacity;
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, capacity);
            while (occupied[index])
            {
                index = (index + 1) % capacity;
            }

            occupied[index] = true;
            dictionary->keys.WriteElement(index, key);
            dictionary->values.WriteElement(index, value);
            dictionary->hashCodes.WriteElement(index, hashCode);
            dictionary->count = count + 1;

            value = ref dictionary->values.ReadElement<V>(index);
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
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyIsMissing(dictionary, key);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    dictionary->values.WriteElement(index, value);
                    return;
                }

                index = (index + 1) % dictionary->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Adds the specified <paramref name="key"/> and <paramref name="value"/> pair to the dictionary,
        /// or sets if already contained.
        /// </summary>
        /// <returns><c>true</c> if the key was added, <c>false</c> if set.</returns>
        public readonly bool AddOrSet(K key, V value)
        {
            ref V existingValue = ref TryGetValue<K, V>(dictionary, key, out bool contains);
            if (!contains)
            {
                existingValue = ref Add<K, V>(dictionary, key);
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
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyAlreadyPresent(dictionary, key);

            uint capacity = dictionary->capacity;
            uint count = dictionary->count;
            if (count == capacity)
            {
                Resize<K, V>(dictionary);
                capacity = dictionary->capacity;
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, capacity);
            while (occupied[index])
            {
                index = (index + 1) % capacity;
            }

            occupied[index] = true;
            dictionary->keys.WriteElement(index, key);
            dictionary->values.WriteElement<V>(index, default);
            dictionary->hashCodes.WriteElement(index, hashCode);
            dictionary->count = count + 1;

            return ref dictionary->values.ReadElement<V>(index);
        }

        /// <summary>
        /// Removes the value associated with the specified <paramref name="key"/>.
        /// <para>
        /// In debug mode, may throw <see cref="NullReferenceException"/> if the key is not found.
        /// </para>
        /// </summary>
        public readonly void Remove(K key)
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyIsMissing(dictionary, key);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    occupied[index] = false;
                    keys[index] = default;
                    keyHashCodes[index] = 0;
                    dictionary->values.Clear(index * dictionary->valueStride, dictionary->valueStride);
                    dictionary->count--;
                    return;
                }

                index = (index + 1) % dictionary->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Removes the value associated with the specified <paramref name="key"/>,
        /// and populates the <paramref name="removed"/> value.
        /// <para>
        /// In debug mode, may throw <see cref="NullReferenceException"/> if the key is not found.
        /// </para>
        /// </summary>
        public readonly void Remove(K key, out V removed)
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyIsMissing(dictionary, key);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    occupied[index] = false;
                    keys[index] = default;
                    removed = dictionary->values.ReadElement<V>(index);
                    dictionary->values.WriteElement<V>(index, default);
                    dictionary->count--;
                    return;
                }

                index = (index + 1) % dictionary->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            removed = default;
        }

        /// <summary>
        /// Attempts to remove the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found and removed.</returns>
        public readonly bool TryRemove(K key, out V removed)
        {
            Allocations.ThrowIfNull(dictionary);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    occupied[index] = false;
                    keys[index] = default;
                    keyHashCodes[index] = 0;
                    removed = dictionary->values.ReadElement<V>(index);
                    dictionary->values.WriteElement<V>(index, default);
                    dictionary->count--;
                    return true;
                }

                index = (index + 1) % dictionary->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            removed = default;
            return false;
        }

        /// <summary>
        /// Attempts to remove the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found and removed.</returns>
        public readonly bool TryRemove(K key)
        {
            Allocations.ThrowIfNull(dictionary);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    occupied[index] = false;
                    keys[index] = default;
                    keyHashCodes[index] = 0;
                    dictionary->values.Clear(index * dictionary->valueStride, dictionary->valueStride);
                    dictionary->count--;
                    return true;
                }

                index = (index + 1) % dictionary->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears the dictionary.
        /// </summary>
        public readonly void Clear()
        {
            Allocations.ThrowIfNull(dictionary);

            dictionary->occupied.Clear(dictionary->capacity);
            dictionary->count = 0;
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
                    Implementation.TryGetPair(map, (uint)index, out KeyValuePair<K, V> pair);
                    return pair;
                }
            }

            readonly object IEnumerator.Current => Current;

            internal Enumerator(Implementation* map)
            {
                this.map = map;
                index = -1;
                capacity = map->capacity;
            }

            public bool MoveNext()
            {
                while (++index < capacity)
                {
                    if (TryGetPair<K, V>(map, (uint)index, out _))
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
                    Implementation.TryGetPair(map, (uint)index, out KeyValuePair<K, V> pair);
                    return new(pair.key, pair.value);
                }
            }

            readonly object IEnumerator.Current => Current;

            internal SystemEnumerator(Implementation* map)
            {
                this.map = map;
                index = -1;
                capacity = map->capacity;
            }

            public bool MoveNext()
            {
                while (++index < capacity)
                {
                    if (TryGetPair<K, V>(map, (uint)index, out _))
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