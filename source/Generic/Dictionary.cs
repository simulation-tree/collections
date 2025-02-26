using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;
using Pointer = Collections.Pointers.Dictionary;

namespace Collections.Generic
{
    /// <summary>
    /// Native dictionary that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Dictionary<K, V> : IDisposable, IReadOnlyDictionary<K, V>, IDictionary<K, V>, IEquatable<Dictionary<K, V>> where K : unmanaged, IEquatable<K> where V : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Pointer* dictionary;

        /// <summary>
        /// Number of key-value pairs in the dictionary.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                Allocations.ThrowIfNull(dictionary);

                return dictionary->count;
            }
        }

        /// <summary>
        /// Capacity of the dictionary.
        /// </summary>
        public readonly uint Capacity
        {
            get
            {
                Allocations.ThrowIfNull(dictionary);

                return dictionary->capacity;
            }
        }

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
                ThrowIfKeyIsMissing(key);

                uint capacity = dictionary->capacity;
                uint hashCode = GetHash(key);
                uint index = hashCode % capacity;
                uint startIndex = index;
                USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(capacity);
                USpan<K> keys = dictionary->keys.GetSpan<K>(capacity);
                USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(capacity);

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

        readonly V IReadOnlyDictionary<K, V>.this[K key] => this[key];

        readonly V IDictionary<K, V>.this[K key]
        {
            get => this[key];
            set => this[key] = value;
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
        public Dictionary(Pointer* pointer)
        {
            dictionary = pointer;
        }

        /// <summary>
        /// Creates a new dictionary with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Dictionary(uint initialCapacity = 4)
        {
            uint capacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            uint keyStride = (uint)sizeof(K);
            uint valueStride = (uint)sizeof(V);
            ref Pointer map = ref Allocations.Allocate<Pointer>();
            map = new(keyStride, valueStride, capacity);
            fixed (Pointer* pointer = &map)
            {
                dictionary = pointer;
            }
        }

#if NET
        /// <summary>
        /// Creates a new dictionary.
        /// </summary>
        public Dictionary()
        {
            uint keyStride = (uint)sizeof(K);
            uint valueStride = (uint)sizeof(V);
            ref Pointer map = ref Allocations.Allocate<Pointer>();
            map = new(keyStride, valueStride, 4);
            fixed (Pointer* pointer = &map)
            {
                dictionary = pointer;
            }
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
            Allocations.ThrowIfNull(dictionary);

            dictionary->keys.Dispose();
            dictionary->hashCodes.Dispose();
            dictionary->values.Dispose();
            dictionary->occupied.Dispose();
            Allocations.Free(ref dictionary);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= dictionary->capacity)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for dictionary of size {dictionary->capacity}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfKeyAlreadyPresent(K key)
        {
            if (ContainsKey(key))
            {
                throw new InvalidOperationException($"Key `{key}` already exists in dictionary");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfKeyIsMissing(K key)
        {
            if (!ContainsKey(key))
            {
                throw new KeyNotFoundException($"Key `{key}` not found in dictionary");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetHash<T>(T value) where T : unmanaged
        {
            unchecked
            {
                return (uint)value.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DoValuesEqual<T>(T left, T right) where T : unmanaged, IEquatable<T>
        {
            return left.Equals(right);
        }

        private readonly void Resize()
        {
            Allocations.ThrowIfNull(dictionary);

            uint oldCapacity = dictionary->capacity;
            uint newCapacity = oldCapacity * 2;
            dictionary->capacity = newCapacity;
            uint count = 0;
            Allocation oldKeys = dictionary->keys;
            Allocation oldValues = dictionary->values;
            Allocation oldOccupied = dictionary->occupied;
            Allocation oldKeyHashCodes = dictionary->hashCodes;
            dictionary->keys = Allocation.Create(newCapacity * (uint)sizeof(K));
            dictionary->values = Allocation.Create(newCapacity * (uint)sizeof(V));
            dictionary->hashCodes = Allocation.Create(newCapacity * sizeof(uint));
            dictionary->occupied = Allocation.CreateZeroed(newCapacity);
            USpan<K> oldKeysSpan = oldKeys.GetSpan<K>(oldCapacity);
            USpan<V> oldValuesSpan = oldValues.GetSpan<V>(oldCapacity);
            USpan<bool> oldOccupiedSpan = oldOccupied.GetSpan<bool>(oldCapacity);
            USpan<bool> newOccupiedSpan = dictionary->occupied.GetSpan<bool>(newCapacity);
            USpan<uint> newKeyHashCodesSpan = dictionary->hashCodes.GetSpan<uint>(newCapacity);
            USpan<K> newKeysSpan = dictionary->keys.GetSpan<K>(newCapacity);
            USpan<V> newValuesSpan = dictionary->values.GetSpan<V>(newCapacity);

            for (uint i = 0; i < oldCapacity; i++)
            {
                if (oldOccupiedSpan[i])
                {
                    K key = oldKeysSpan[i];
                    V value = oldValuesSpan[i];
                    uint hashCode = GetHash(key);
                    uint index = hashCode % newCapacity;
                    uint startIndex = index;
                    while (newOccupiedSpan[index])
                    {
                        index = (index + 1) % newCapacity;
                    }

                    newOccupiedSpan[index] = true;
                    newKeysSpan[index] = key;
                    newValuesSpan[index] = value;
                    newKeyHashCodesSpan[index] = hashCode;
                    count++;
                }
            }

            dictionary->count = count;
            oldKeys.Dispose();
            oldValues.Dispose();
            oldOccupied.Dispose();
            oldKeyHashCodes.Dispose();
        }

        private readonly bool TryGetPair(uint index, out KeyValuePair<K, V> pair)
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfOutOfRange(index);

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
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(capacity);
            USpan<K> keys = dictionary->keys.GetSpan<K>(capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(capacity);

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
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(capacity);
            USpan<K> keys = dictionary->keys.GetSpan<K>(capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(capacity);

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
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(dictionary->capacity);
            USpan<K> keys = dictionary->keys.GetSpan<K>(dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(dictionary->capacity);

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
                Resize();
                capacity = dictionary->capacity;
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(capacity);
            USpan<K> keys = dictionary->keys.GetSpan<K>(capacity);

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
            ThrowIfKeyAlreadyPresent(key);

            uint capacity = dictionary->capacity;
            uint count = dictionary->count;
            if (count == capacity)
            {
                Resize();
                capacity = dictionary->capacity;
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(capacity);
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
            ThrowIfKeyAlreadyPresent(key);

            uint capacity = dictionary->capacity;
            uint count = dictionary->count;
            if (count == capacity)
            {
                Resize();
                capacity = dictionary->capacity;
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(capacity);
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
            ThrowIfKeyIsMissing(key);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(dictionary->capacity);
            USpan<K> keys = dictionary->keys.GetSpan<K>(dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(dictionary->capacity);

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
            ref V existingValue = ref TryGetValue(key, out bool contains);
            if (!contains)
            {
                existingValue = ref Add(key);
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
            ThrowIfKeyAlreadyPresent(key);

            uint capacity = dictionary->capacity;
            uint count = dictionary->count;
            if (count == capacity)
            {
                Resize();
                capacity = dictionary->capacity;
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(capacity);
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
            ThrowIfKeyIsMissing(key);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(dictionary->capacity);
            USpan<K> keys = dictionary->keys.GetSpan<K>(dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(dictionary->capacity);

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
            ThrowIfKeyIsMissing(key);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(dictionary->capacity);
            USpan<K> keys = dictionary->keys.GetSpan<K>(dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(dictionary->capacity);

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
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(dictionary->capacity);
            USpan<K> keys = dictionary->keys.GetSpan<K>(dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(dictionary->capacity);

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
            USpan<bool> occupied = dictionary->occupied.GetSpan<bool>(dictionary->capacity);
            USpan<K> keys = dictionary->keys.GetSpan<K>(dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.GetSpan<uint>(dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    occupied[index] = false;
                    keys[index] = default;
                    keyHashCodes[index] = 0;
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
                if (TryGetPair(i, out KeyValuePair<K, V> pair))
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
            return new SystemEnumerator(this);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public readonly Enumerator GetEnumerator()
        {
            return new(this);
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
            private readonly Dictionary<K, V> map;
            private readonly uint capacity;
            private int index;

            public readonly (K key, V value) Current
            {
                get
                {
                    map.TryGetPair((uint)index, out KeyValuePair<K, V> pair);
                    return pair;
                }
            }

            readonly object IEnumerator.Current => Current;

            internal Enumerator(Dictionary<K, V> map)
            {
                this.map = map;
                index = -1;
                capacity = map.Capacity;
            }

            public bool MoveNext()
            {
                while (++index < capacity)
                {
                    if (map.TryGetPair((uint)index, out _))
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
            private readonly Dictionary<K, V> map;
            private readonly uint capacity;
            private int index;

            public readonly System.Collections.Generic.KeyValuePair<K, V> Current
            {
                get
                {
                    map.TryGetPair((uint)index, out KeyValuePair<K, V> pair);
                    return new(pair.key, pair.value);
                }
            }

            readonly object IEnumerator.Current => Current;

            internal SystemEnumerator(Dictionary<K, V> map)
            {
                this.map = map;
                index = -1;
                capacity = map.Capacity;
            }

            public bool MoveNext()
            {
                while (++index < capacity)
                {
                    if (map.TryGetPair((uint)index, out _))
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