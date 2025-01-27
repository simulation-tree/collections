using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Collections.Implementations
{
    public unsafe struct Dictionary
    {
        private uint entryStride;
        private uint count;
        private uint capacity;
        private Allocation entries;

        public static Dictionary* Allocate<K, V>(uint initialCapacity) where K : unmanaged where V : unmanaged
        {
            Dictionary* map = Allocations.Allocate<Dictionary>();
            map->entryStride = (uint)sizeof(Entry<K, V>);
            map->capacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            map->count = 0;
            map->entries = new(map->capacity * map->entryStride, true);
            return map;
        }

        public static void Free(ref Dictionary* map)
        {
            Allocations.ThrowIfNull(map);

            map->entries.Dispose();
            Allocations.Free(ref map);
        }

        public static uint GetCount(Dictionary* map)
        {
            Allocations.ThrowIfNull(map);

            return map->count;
        }

        public static uint GetCapacity(Dictionary* map)
        {
            Allocations.ThrowIfNull(map);

            return map->capacity;
        }

        public static ref Entry<K, V> GetEntry<K, V>(Dictionary* map, uint index) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            return ref map->entries.Read<Entry<K, V>>(index * map->entryStride);
        }

        public static bool TryGetPair<K, V>(Dictionary* map, uint index, out KeyValuePair<K, V> pair) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            ref Entry<K, V> entry = ref map->entries.Read<Entry<K, V>>(index * map->entryStride);
            bool occupied = entry.state == EntryState.Occupied;
            if (occupied)
            {
                pair = new KeyValuePair<K, V>(entry.key, entry.value);
            }
            else
            {
                pair = default;
            }

            return occupied;
        }

        private static uint GetHash<K>(Dictionary* map, K key) where K : unmanaged, IEquatable<K>
        {
            unchecked
            {
                EqualityComparer<K> comparer = EqualityComparer<K>.Default;
                int hash = comparer.GetHashCode(key);
                return (uint)hash % map->capacity;
            }
        }

        private static uint Probe(Dictionary* map, uint hash, uint index)
        {
            return (hash + index) % map->capacity;
        }

        private static uint FindIndex<K, V>(Dictionary* map, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            uint hash = GetHash(map, key);
            EqualityComparer<K> comparer = EqualityComparer<K>.Default;
            for (uint a = 0; a < map->capacity; a++)
            {
                uint index = Probe(map, hash, a);
                ref Entry<K, V> entry = ref GetEntry<K, V>(map, index);
                if (entry.state == EntryState.Empty)
                {
                    return uint.MaxValue;
                }
                else if (entry.state == EntryState.Occupied && comparer.Equals(entry.key, key))
                {
                    return index;
                }
            }

            return uint.MaxValue;
        }

        public static bool TryAdd<K, V>(Dictionary* map, K key, V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            if (map->count == map->capacity)
            {
                Resize<K, V>(map);
            }

            uint hash = GetHash(map, key);
            for (uint a = 0; a < map->capacity; a++)
            {
                uint index = Probe(map, hash, a);
                ref Entry<K, V> entry = ref GetEntry<K, V>(map, index);
                if (entry.state != EntryState.Occupied)
                {
                    entry.state = EntryState.Occupied;
                    entry.key = key;
                    entry.value = value;
                    map->count++;
                    return true;
                }

                if (entry.state == EntryState.Occupied && entry.key.Equals(key))
                {
                    return false;
                }
            }

            return false;
        }

        public static ref V Add<K, V>(Dictionary* map, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);
            ThrowIfKeyAlreadyPresent<K, V>(map, key);

            if (map->count == map->capacity)
            {
                Resize<K, V>(map);
            }

            uint hash = GetHash(map, key);
            for (uint a = 0; a < map->capacity; a++)
            {
                uint index = Probe(map, hash, a);
                ref Entry<K, V> entry = ref GetEntry<K, V>(map, index);
                if (entry.state != EntryState.Occupied)
                {
                    entry.state = EntryState.Occupied;
                    entry.key = key;
                    entry.value = default;
                    map->count++;
                    return ref entry.value;
                }
            }

            return ref *(V*)default(nint);
        }

        public static void Clear(Dictionary* map)
        {
            Allocations.ThrowIfNull(map);

            map->entries.Clear(map->capacity * map->entryStride);
            map->count = 0;
        }

        public static bool ContainsKey<K, V>(Dictionary* map, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            return FindIndex<K, V>(map, key) != uint.MaxValue;
        }

        private static void Resize<K, V>(Dictionary* map) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            Allocation oldEntries = map->entries;
            uint oldCapacity = map->capacity;
            map->capacity *= 2;
            map->count = 0;
            map->entries = new(map->capacity * map->entryStride, true);

            for (uint i = 0; i < oldCapacity; i++)
            {
                ref Entry<K, V> oldEntry = ref oldEntries.Read<Entry<K, V>>(i * map->entryStride);
                if (oldEntry.state == EntryState.Occupied)
                {
                    uint hash = GetHash(map, oldEntry.key);
                    for (uint a = 0; a < map->capacity; a++)
                    {
                        uint index = Probe(map, hash, a);
                        ref Entry<K, V> newEntry = ref GetEntry<K, V>(map, index);
                        if (newEntry.state != EntryState.Occupied)
                        {
                            newEntry.state = EntryState.Occupied;
                            newEntry.key = oldEntry.key;
                            newEntry.value = oldEntry.value;
                            map->count++;
                            break;
                        }
                    }
                }
            }

            oldEntries.Dispose();
        }

        public static ref V TryGetValue<K, V>(Dictionary* map, K key, out bool contains) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            uint index = FindIndex<K, V>(map, key);
            if (index == uint.MaxValue)
            {
                contains = false;
                nint nullValue = 0;
                return ref *(V*)nullValue;
            }

            contains = true;
            ref Entry<K, V> entry = ref GetEntry<K, V>(map, index);
            return ref entry.value;
        }

        public static bool TryGetValue<K, V>(Dictionary* map, K key, out V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            uint index = FindIndex<K, V>(map, key);
            bool found = index != uint.MaxValue;
            if (found)
            {
                value = GetEntry<K, V>(map, index).value;
            }
            else
            {
                value = default;
            }

            return found;
        }

        public static ref V GetValue<K, V>(Dictionary* map, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            uint index = FindIndex<K, V>(map, key);
            ThrowIfNotFound(key, index);

            return ref GetEntry<K, V>(map, index).value;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNotFound<K>(K key, uint index) where K : unmanaged
        {
            if (index == uint.MaxValue)
            {
                throw new KeyNotFoundException($"Key `{key}` not found in dictionary");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfKeyAlreadyPresent<K, V>(Dictionary* map, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            if (ContainsKey<K, V>(map, key))
            {
                throw new InvalidOperationException($"Key `{key}` already exists in dictionary");
            }
        }

        public static bool TryRemove<K, V>(Dictionary* map, K key, out V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            uint index = FindIndex<K, V>(map, key);
            if (index == uint.MaxValue)
            {
                value = default;
                return false;
            }

            ref Entry<K, V> entry = ref GetEntry<K, V>(map, index);
            entry.state = EntryState.Deleted;
            value = entry.value;
            entry.value = default;
            entry.key = default;
            map->count--;
            return true;
        }

        public static V Remove<K, V>(Dictionary* map, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            uint index = FindIndex<K, V>(map, key);
            ThrowIfNotFound(key, index);

            ref Entry<K, V> entry = ref GetEntry<K, V>(map, index);
            entry.state = EntryState.Deleted;
            V value = entry.value;
            entry.value = default;
            entry.key = default;
            map->count--;
            return value;
        }

        public enum EntryState : byte
        {
            Empty,
            Occupied,
            Deleted
        }

        public struct Entry<K, V>
        {
            public EntryState state;
            public K key;
            public V value;
        }
    }
}