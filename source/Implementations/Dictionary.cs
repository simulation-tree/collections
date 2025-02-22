using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Collections.Implementations
{
    public unsafe struct Dictionary
    {
        public readonly uint keyStride;
        public readonly uint valueStride;

        internal uint count;
        internal uint capacity;
        internal Allocation keys;
        internal Allocation hashCodes;
        internal Allocation values;
        internal Allocation occupied;

        public readonly uint Count => count;
        public readonly uint Capacity => capacity;
        public readonly Allocation Keys => keys;
        public readonly Allocation Values => values;
        public readonly Allocation Occupied => occupied;
        public readonly Allocation HashCodes => hashCodes;

        private Dictionary(uint keyStride, uint valueStride, uint capacity)
        {
            this.keyStride = keyStride;
            this.valueStride = valueStride;
            this.capacity = capacity;
            keys = new(keyStride * capacity);
            hashCodes = new(capacity * sizeof(int));
            values = new(valueStride * capacity);
            occupied = new(capacity, true);
            count = 0;
        }

        public static Dictionary* Allocate<K, V>(uint initialCapacity = 4) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            uint capacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            uint keyStride = (uint)sizeof(K);
            uint valueStride = (uint)sizeof(V);
            ref Dictionary map = ref Allocations.Allocate<Dictionary>();
            map = new(keyStride, valueStride, capacity);
            fixed (Dictionary* pointer = &map)
            {
                return pointer;
            }
        }

        public static void Free(ref Dictionary* map)
        {
            Allocations.ThrowIfNull(map);

            map->keys.Dispose();
            map->hashCodes.Dispose();
            map->values.Dispose();
            map->occupied.Dispose();
            Allocations.Free(ref map);
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfKeyStrideMismatch<K>(Dictionary* dictionary) where K : unmanaged, IEquatable<K>
        {
            if (dictionary->keyStride != (uint)sizeof(K))
            {
                throw new InvalidOperationException($"Key stride size {dictionary->keyStride} does not match expected size of type {sizeof(K)}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfValueStrideMismatch<V>(Dictionary* dictionary) where V : unmanaged
        {
            if (dictionary->valueStride != (uint)sizeof(V))
            {
                throw new InvalidOperationException($"Value stride size {dictionary->valueStride} does not match expected size of type {sizeof(V)}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfOutOfRange(Dictionary* dictionary, uint index)
        {
            if (index >= dictionary->capacity)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for dictionary of size {dictionary->capacity}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfKeyAlreadyPresent<K>(Dictionary* dictionary, K key) where K : unmanaged, IEquatable<K>
        {
            if (ContainsKey(dictionary, key))
            {
                throw new InvalidOperationException($"Key `{key}` already exists in dictionary");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfKeyIsMissing<K>(Dictionary* dictionary, K key) where K : unmanaged, IEquatable<K>
        {
            if (!ContainsKey(dictionary, key))
            {
                throw new KeyNotFoundException($"Key `{key}` not found in dictionary");
            }
        }

        internal static uint GetHash<T>(T value) where T : unmanaged
        {
            unchecked
            {
                return (uint)value.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool DoValuesEqual<T>(T left, T right) where T : unmanaged, IEquatable<T>
        {
            return left.Equals(right);
        }

        internal static void Resize<K, V>(Dictionary* dictionary) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);

            uint oldCapacity = dictionary->capacity;
            uint newCapacity = oldCapacity * 2;
            dictionary->capacity = newCapacity;
            uint count = 0;
            Allocation oldKeys = dictionary->keys;
            Allocation oldValues = dictionary->values;
            Allocation oldOccupied = dictionary->occupied;
            Allocation oldKeyHashCodes = dictionary->hashCodes;
            dictionary->keys = new(newCapacity * (uint)sizeof(K));
            dictionary->values = new(newCapacity * (uint)sizeof(V));
            dictionary->hashCodes = new(newCapacity * sizeof(uint));
            dictionary->occupied = new(newCapacity, true);
            USpan<K> oldKeysSpan = oldKeys.AsSpan<K>(0, oldCapacity);
            USpan<V> oldValuesSpan = oldValues.AsSpan<V>(0, oldCapacity);
            USpan<bool> oldOccupiedSpan = oldOccupied.AsSpan<bool>(0, oldCapacity);
            USpan<bool> newOccupiedSpan = dictionary->occupied.AsSpan<bool>(0, newCapacity);
            USpan<uint> newKeyHashCodesSpan = dictionary->hashCodes.AsSpan<uint>(0, newCapacity);
            USpan<K> newKeysSpan = dictionary->keys.AsSpan<K>(0, newCapacity);
            USpan<V> newValuesSpan = dictionary->values.AsSpan<V>(0, newCapacity);

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

        public static void Add<K, V>(Dictionary* dictionary, K key, V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);
            ThrowIfKeyAlreadyPresent(dictionary, key);

            if (dictionary->count == dictionary->capacity)
            {
                Resize<K, V>(dictionary);
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            while (occupied[index])
            {
                index = (index + 1) % dictionary->capacity;
            }

            occupied[index] = true;
            dictionary->keys.WriteElement(index, key);
            dictionary->values.WriteElement(index, value);
            dictionary->hashCodes.WriteElement(index, hashCode);
            dictionary->count++;
        }

        public static void Add<K, V>(Dictionary* dictionary, K key, ref V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);
            ThrowIfKeyAlreadyPresent(dictionary, key);

            if (dictionary->count == dictionary->capacity)
            {
                Resize<K, V>(dictionary);
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            while (occupied[index])
            {
                index = (index + 1) % dictionary->capacity;
            }

            occupied[index] = true;
            dictionary->keys.WriteElement(index, key);
            dictionary->values.WriteElement(index, value);
            dictionary->hashCodes.WriteElement(index, hashCode);
            dictionary->count++;

            value = ref dictionary->values.ReadElement<V>(index);
        }

        public static ref V Add<K, V>(Dictionary* dictionary, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);
            ThrowIfKeyAlreadyPresent(dictionary, key);

            if (dictionary->count == dictionary->capacity)
            {
                Resize<K, V>(dictionary);
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            while (occupied[index])
            {
                index = (index + 1) % dictionary->capacity;
            }

            occupied[index] = true;
            dictionary->keys.WriteElement(index, key);
            dictionary->values.WriteElement<V>(index, default);
            dictionary->hashCodes.WriteElement(index, hashCode);
            dictionary->count++;

            return ref dictionary->values.ReadElement<V>(index);
        }

        public static bool TryAdd<K, V>(Dictionary* dictionary, K key, V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);

            if (dictionary->count == dictionary->capacity)
            {
                Resize<K, V>(dictionary);
            }

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, dictionary->capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
                    return false;
                }

                index = (index + 1) % dictionary->capacity;
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

        public static bool ContainsKey<K>(Dictionary* dictionary, K key) where K : unmanaged, IEquatable<K>
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);

            uint hashCode = GetHash(key);
            uint index = hashCode % dictionary->capacity;
            uint startIndex = index;
            USpan<bool> occupied = dictionary->occupied.AsSpan<bool>(0, dictionary->capacity);
            USpan<uint> keyHashCodes = dictionary->hashCodes.AsSpan<uint>(0, dictionary->capacity);
            USpan<K> keys = dictionary->keys.AsSpan<K>(0, dictionary->capacity);

            while (occupied[index])
            {
                if (keyHashCodes[index] == hashCode && DoValuesEqual(keys[index], key))
                {
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

        public static void Clear(Dictionary* dictionary)
        {
            Allocations.ThrowIfNull(dictionary);

            dictionary->occupied.Clear(dictionary->capacity);
            dictionary->count = 0;
        }

        public static bool TryGetValue<K, V>(Dictionary* dictionary, K key, out V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);

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
                    value = dictionary->values.ReadElement<V>(index);
                    return true;
                }

                index = (index + 1) % dictionary->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            value = default;
            return false;
        }

        public static ref V TryGetValue<K, V>(Dictionary* dictionary, K key, out bool contains) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);

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

        public static bool TryGetPair<K, V>(Dictionary* dictionary, uint index, out KeyValuePair<K, V> pair) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);
            ThrowIfOutOfRange(dictionary, index);

            pair = new KeyValuePair<K, V>(dictionary->keys.ReadElement<K>(index), dictionary->values.ReadElement<V>(index));
            return dictionary->occupied.ReadElement<bool>(index);
        }

        public static ref V Get<K, V>(Dictionary* dictionary, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);
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
                    return ref dictionary->values.ReadElement<V>(index);
                }

                index = (index + 1) % dictionary->capacity;
                if (index == startIndex)
                {
                    break;
                }
            }

            return ref *(V*)default(nint);
        }

        public static void Set<K, V>(Dictionary* dictionary, K key, V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);
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

        public static bool TryRemove<K>(Dictionary* dictionary, K key) where K : unmanaged, IEquatable<K>
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);

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

        public static bool TryRemove<K, V>(Dictionary* dictionary, K key, out V removed) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);

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

        public static void Remove<K>(Dictionary* dictionary, K key) where K : unmanaged, IEquatable<K>
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
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

        public static void Remove<K, V>(Dictionary* dictionary, K key, out V removed) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeyStrideMismatch<K>(dictionary);
            ThrowIfValueStrideMismatch<V>(dictionary);
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
    }
}