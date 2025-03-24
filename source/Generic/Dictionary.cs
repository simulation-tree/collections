using Collections.Pointers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Generic
{
    /// <summary>
    /// Native dictionary that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Dictionary<K, V> : IDisposable, IReadOnlyDictionary<K, V>, IDictionary<K, V>, IEquatable<Dictionary<K, V>> where K : unmanaged, IEquatable<K> where V : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DictionaryPointer* dictionary;

        /// <summary>
        /// Number of key-value pairs in the dictionary.
        /// </summary>
        public readonly int Count
        {
            get
            {
                MemoryAddress.ThrowIfDefault(dictionary);

                return dictionary->count;
            }
        }

        /// <summary>
        /// Capacity of the dictionary.
        /// </summary>
        public readonly int Capacity
        {
            get
            {
                MemoryAddress.ThrowIfDefault(dictionary);

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
                MemoryAddress.ThrowIfDefault(dictionary);
                ThrowIfKeyIsMissing(key);

                int capacity = dictionary->capacity;
                int hashCode = SharedFunctions.GetHashCode(key);
                int index = hashCode % capacity;
                int startIndex = index;
                Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
                Span<K> keys = new(dictionary->keys.Pointer, capacity);
                Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, capacity);

                while (occupied[index])
                {
                    if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
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
                int capacity = Capacity;
                for (int i = 0; i < capacity; i++)
                {
                    if (TryGetKeyAtIndex(i, out K key))
                    {
                        yield return key;
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
                int capacity = Capacity;
                for (int i = 0; i < capacity; i++)
                {
                    if (TryGetValueAtIndex(i, out V value))
                    {
                        yield return value;
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
        readonly bool ICollection<KeyValuePair<K, V>>.IsReadOnly => false;

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
        public Dictionary(DictionaryPointer* pointer)
        {
            dictionary = pointer;
        }

        /// <summary>
        /// Creates a new dictionary with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Dictionary(int initialCapacity)
        {
            initialCapacity = Math.Max(1, initialCapacity).GetNextPowerOf2();
            dictionary = MemoryAddress.AllocatePointer<DictionaryPointer>();
            dictionary->keys = MemoryAddress.Allocate(initialCapacity * sizeof(K));
            dictionary->hashCodes = MemoryAddress.Allocate(initialCapacity * sizeof(int));
            dictionary->values = MemoryAddress.Allocate(initialCapacity * sizeof(V));
            dictionary->occupied = MemoryAddress.AllocateZeroed(initialCapacity);
            dictionary->capacity = initialCapacity;
            dictionary->count = 0;
            dictionary->keyStride = sizeof(K);
            dictionary->valueStride = sizeof(V);
        }

#if NET
        /// <summary>
        /// Creates a new dictionary.
        /// </summary>
        public Dictionary()
        {
            dictionary = MemoryAddress.AllocatePointer<DictionaryPointer>();
            dictionary->keys = MemoryAddress.Allocate(4 * sizeof(K));
            dictionary->hashCodes = MemoryAddress.Allocate(4 * sizeof(int));
            dictionary->values = MemoryAddress.Allocate(4 * sizeof(V));
            dictionary->occupied = MemoryAddress.AllocateZeroed(4);
            dictionary->capacity = 4;
            dictionary->count = 0;
            dictionary->keyStride = sizeof(K);
            dictionary->valueStride = sizeof(V);
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
            MemoryAddress.ThrowIfDefault(dictionary);

            dictionary->keys.Dispose();
            dictionary->hashCodes.Dispose();
            dictionary->values.Dispose();
            dictionary->occupied.Dispose();
            MemoryAddress.Free(ref dictionary);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
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

        private readonly void Resize(int newCapacity)
        {
            MemoryAddress.ThrowIfDefault(dictionary);

            int oldCapacity = dictionary->capacity;
            dictionary->capacity = newCapacity;
            int count = 0;
            MemoryAddress oldKeys = dictionary->keys;
            MemoryAddress oldValues = dictionary->values;
            MemoryAddress oldOccupied = dictionary->occupied;
            MemoryAddress oldKeyHashCodes = dictionary->hashCodes;
            dictionary->keys = MemoryAddress.Allocate(newCapacity * sizeof(K));
            dictionary->values = MemoryAddress.Allocate(newCapacity * sizeof(V));
            dictionary->hashCodes = MemoryAddress.Allocate(newCapacity * sizeof(int));
            dictionary->occupied = MemoryAddress.AllocateZeroed(newCapacity);
            Span<K> oldKeysSpan = new(oldKeys.Pointer, oldCapacity);
            Span<V> oldValuesSpan = new(oldValues.Pointer, oldCapacity);
            Span<bool> oldOccupiedSpan = new(oldOccupied.Pointer, oldCapacity);
            Span<bool> newOccupiedSpan = new(dictionary->occupied.Pointer, newCapacity);
            Span<int> newKeyHashCodesSpan = new(dictionary->hashCodes.Pointer, newCapacity);
            Span<K> newKeysSpan = new(dictionary->keys.Pointer, newCapacity);
            Span<V> newValuesSpan = new(dictionary->values.Pointer, newCapacity);

            for (int i = 0; i < oldCapacity; i++)
            {
                if (oldOccupiedSpan[i])
                {
                    K key = oldKeysSpan[i];
                    V value = oldValuesSpan[i];
                    int hashCode = SharedFunctions.GetHashCode(key);
                    int index = hashCode % newCapacity;
                    int startIndex = index;
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

        private readonly bool TryGetPairAtIndex(int index, out K key, out V value)
        {
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfOutOfRange(index);

            key = dictionary->keys.ReadElement<K>(index);
            value = dictionary->values.ReadElement<V>(index);
            return dictionary->occupied.ReadElement<bool>(index);
        }

        private readonly (K key, V value) GetPairAtIndex(int index)
        {
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfOutOfRange(index);

            return (dictionary->keys.ReadElement<K>(index), dictionary->values.ReadElement<V>(index));
        }

        private readonly bool ContainsAtIndex(int index)
        {
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfOutOfRange(index);

            return dictionary->occupied.ReadElement<bool>(index);
        }

        private readonly bool TryGetKeyAtIndex(int index, out K key)
        {
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfOutOfRange(index);

            key = dictionary->keys.ReadElement<K>(index);
            return dictionary->occupied.ReadElement<bool>(index);
        }

        private readonly bool TryGetValueAtIndex(int index, out V value)
        {
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfOutOfRange(index);

            value = dictionary->values.ReadElement<V>(index);
            return dictionary->occupied.ReadElement<bool>(index);
        }

        /// <summary>
        /// Checks if the dictionary contains the given <paramref name="key"/>.
        /// </summary>
        public readonly bool ContainsKey(K key)
        {
            MemoryAddress.ThrowIfDefault(dictionary);

            int capacity = dictionary->capacity;
            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
            Span<K> keys = new(dictionary->keys.Pointer, capacity);
            Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
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
            MemoryAddress.ThrowIfDefault(dictionary);

            int capacity = dictionary->capacity;
            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
            Span<K> keys = new(dictionary->keys.Pointer, capacity);
            Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
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
            MemoryAddress.ThrowIfDefault(dictionary);

            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % dictionary->capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, dictionary->capacity);
            Span<K> keys = new(dictionary->keys.Pointer, dictionary->capacity);
            Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
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
            MemoryAddress.ThrowIfDefault(dictionary);

            int capacity = dictionary->capacity;
            if (dictionary->count == capacity)
            {
                capacity *= 2;
                Resize(capacity);
            }

            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
            Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, capacity);
            Span<K> keys = new(dictionary->keys.Pointer, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
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
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfKeyAlreadyPresent(key);

            int capacity = dictionary->capacity;
            int newCount = dictionary->count + 1;
            if (newCount > capacity)
            {
                capacity *= 2;
                Resize(capacity);
            }

            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
            while (occupied[index])
            {
                index = (index + 1) % capacity;
            }

            occupied[index] = true;
            dictionary->keys.WriteElement(index, key);
            dictionary->values.WriteElement(index, value);
            dictionary->hashCodes.WriteElement(index, hashCode);
            dictionary->count = newCount;
        }

        /// <summary>
        /// Adds the given <paramref name="key"/> and <paramref name="value"/> pair to the dictionary.
        /// <para>
        /// In debug mode, throws <see cref="InvalidOperationException"/> if the key already exists.
        /// </para>
        /// </summary>
        public readonly void Add(K key, ref V value)
        {
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfKeyAlreadyPresent(key);

            int capacity = dictionary->capacity;
            int count = dictionary->count;
            if (count == capacity)
            {
                capacity *= 2;
                Resize(capacity);
            }

            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
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
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfKeyIsMissing(key);

            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % dictionary->capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, dictionary->capacity);
            Span<K> keys = new(dictionary->keys.Pointer, dictionary->capacity);
            Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
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
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfKeyAlreadyPresent(key);

            int capacity = dictionary->capacity;
            int count = dictionary->count;
            if (count == capacity)
            {
                capacity *= 2;
                Resize(capacity);
            }

            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
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
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfKeyIsMissing(key);

            int capacity = dictionary->capacity;
            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
            Span<K> keys = new(dictionary->keys.Pointer, capacity);
            Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
                {
                    occupied[index] = false;
                    keys[index] = default;
                    keyHashCodes[index] = 0;
                    dictionary->values.Clear(index * dictionary->valueStride, dictionary->valueStride);
                    dictionary->count--;
                    return;
                }

                index = (index + 1) % capacity;
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
            MemoryAddress.ThrowIfDefault(dictionary);
            ThrowIfKeyIsMissing(key);

            int capacity = dictionary->capacity;
            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
            Span<K> keys = new(dictionary->keys.Pointer, capacity);
            Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
                {
                    occupied[index] = false;
                    keys[index] = default;
                    removed = dictionary->values.ReadElement<V>(index);
                    dictionary->values.WriteElement<V>(index, default);
                    dictionary->count--;
                    return;
                }

                index = (index + 1) % capacity;
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
            MemoryAddress.ThrowIfDefault(dictionary);

            int capacity = dictionary->capacity;
            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
            Span<K> keys = new(dictionary->keys.Pointer, capacity);
            Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
                {
                    occupied[index] = false;
                    keys[index] = default;
                    keyHashCodes[index] = 0;
                    removed = dictionary->values.ReadElement<V>(index);
                    dictionary->values.WriteElement<V>(index, default);
                    dictionary->count--;
                    return true;
                }

                index = (index + 1) % capacity;
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
            MemoryAddress.ThrowIfDefault(dictionary);

            int capacity = dictionary->capacity;
            int hashCode = SharedFunctions.GetHashCode(key);
            int index = hashCode % capacity;
            int startIndex = index;
            Span<bool> occupied = new(dictionary->occupied.Pointer, capacity);
            Span<K> keys = new(dictionary->keys.Pointer, capacity);
            Span<int> keyHashCodes = new(dictionary->hashCodes.Pointer, capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && keys[index].Equals(key))
                {
                    occupied[index] = false;
                    keys[index] = default;
                    keyHashCodes[index] = 0;
                    dictionary->values.WriteElement<V>(index, default);
                    dictionary->count--;
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
        /// Clears the dictionary.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(dictionary);

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

        readonly void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item)
        {
            Add(item.Key, item.Value);
        }

        readonly bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item)
        {
            return TryGetValue(item.Key, out V value) && EqualityComparer<V>.Default.Equals(value, item.Value);
        }

        public readonly void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
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

            int count = Capacity;
            for (int i = 0; i < count; i++)
            {
                if (TryGetPairAtIndex(i, out K key, out V value))
                {
                    array[arrayIndex++] = new(key, value);
                }
            }
        }

        readonly bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
        {
            return TryRemove(item.Key);
        }

        readonly IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
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
            private readonly int capacity;
            private int index;

            public readonly (K key, V value) Current => map.GetPairAtIndex(index);

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
                    if (map.TryGetKeyAtIndex(index, out _))
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

        public struct SystemEnumerator : IEnumerator<KeyValuePair<K, V>>
        {
            private readonly Dictionary<K, V> map;
            private readonly int capacity;
            private int index;

            public readonly KeyValuePair<K, V> Current
            {
                get
                {
                    (K key, V value) pair = map.GetPairAtIndex(index);
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
                    if (map.ContainsAtIndex(index))
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