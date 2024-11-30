using System;
using System.Collections.Generic;
using Unmanaged;

namespace Collections.Unsafe
{
    public unsafe struct UnsafeDictionary
    {
        private uint entryStride;
        private uint count;
        private uint capacity;
        private Allocation entries;

        public static UnsafeDictionary* Allocate<K, V>(uint initialCapacity) where K : unmanaged where V : unmanaged
        {
            UnsafeDictionary* map = Allocations.Allocate<UnsafeDictionary>();
            map->entryStride = TypeInfo<Entry<K, V>>.size;
            map->capacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
            map->count = 0;
            map->entries = new(map->capacity * map->entryStride, true);
            return map;
        }

        public static void Free(ref UnsafeDictionary* map)
        {
            Allocations.ThrowIfNull(map);

            map->entries.Dispose();
            Allocations.Free(ref map);
        }

        public static uint GetCount(UnsafeDictionary* map)
        {
            Allocations.ThrowIfNull(map);

            return map->count;
        }

        public static uint GetCapacity(UnsafeDictionary* map)
        {
            Allocations.ThrowIfNull(map);

            return map->capacity;
        }

        public static uint GetHash<K>(K key) where K : unmanaged, IEquatable<K>
        {
            unchecked
            {
                return (uint)key.GetHashCode();
            }
        }

        public static Entry<K, V> GetEntry<K, V>(UnsafeDictionary* map, uint index) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);
            
            return map->entries.Read<Entry<K, V>>(index * map->entryStride);
        }

        private static uint FindSlotIndex<K, V>(UnsafeDictionary* map, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            uint hash = GetHash(key);
            uint startIndex = hash % map->capacity;
            uint index = startIndex;
            EqualityComparer<K> comparer = EqualityComparer<K>.Default;
            do
            {
                Entry<K, V> entry = map->entries.Read<Entry<K, V>>(index * map->entryStride);
                if (entry.state == EntryState.Empty)
                {
                    return index;
                }

                if (entry.state == EntryState.Occupied && comparer.Equals(entry.key, key))
                {
                    return index;
                }

                index = (index + 1) % map->capacity;
            }
            while (index != startIndex);
            return uint.MaxValue;
        }

        public static bool TryAdd<K, V>(UnsafeDictionary* map, K key, V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            if (ContainsKey<K, V>(map, key))
            {
                return false;
            }

            if (map->count == map->capacity)
            {
                Resize(map);
            }

            uint index = FindSlotIndex<K, V>(map, key);
            if (index == uint.MaxValue)
            {
                return false;
            }

            ref Entry<K, V> entry = ref map->entries.Read<Entry<K, V>>(index * map->entryStride);
            if (entry.state == EntryState.Occupied)
            {
                throw new InvalidOperationException($"Key `{key}` already exists");
            }

            map->count++;
            entry.state = EntryState.Occupied;
            entry.key = key;
            entry.value = value;
            return true;
        }

        public static void Clear(UnsafeDictionary* map)
        {
            Allocations.ThrowIfNull(map);

            for (uint i = 0; i < map->capacity; i++)
            {
                nint entry = (nint)(map->entries.Address + (i * map->entryStride));
                EntryState* entryState = (EntryState*)entry;
                *entryState = EntryState.Empty;
                System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned((void*)(entry + sizeof(byte)), 0, map->entryStride - sizeof(byte));
            }

            map->count = 0;
        }

        public static bool ContainsKey<K, V>(UnsafeDictionary* map, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);
            uint hash = GetHash(key);
            uint index = hash % map->capacity;
            uint startIndex = index;
            EqualityComparer<K> comparer = EqualityComparer<K>.Default;
            do
            {
                Entry<K, V> entry = map->entries.Read<Entry<K, V>>(index * map->entryStride);
                if (entry.state == EntryState.Empty)
                {
                    return false;
                }

                if (entry.state == EntryState.Occupied && comparer.Equals(entry.key, key))
                {
                    return true;
                }

                index = (index + 1) % map->capacity;
            }
            while (index != startIndex);
            return false;
        }

        private static void Resize(UnsafeDictionary* map)
        {
            Allocations.ThrowIfNull(map);

            uint newCapacity = map->capacity * 2;
            Allocation newEntries = new(newCapacity * map->entryStride, true);
            map->capacity = newCapacity;
            map->entries.CopyTo(newEntries, 0, 0, map->count * map->entryStride);
            map->entries.Dispose();
            map->entries = newEntries;
        }

        public static ref V TryGetValue<K, V>(UnsafeDictionary* map, K key, out bool contains) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            uint hash = GetHash(key);
            uint index = hash % map->capacity;
            uint startIndex = index;
            EqualityComparer<K> comparer = EqualityComparer<K>.Default;
            do
            {
                ref Entry<K, V> entry = ref map->entries.Read<Entry<K, V>>(index * map->entryStride);
                if (entry.state == EntryState.Empty)
                {
                    contains = false;
                    return ref *(V*)default(nint);
                }

                if (entry.state == EntryState.Occupied && comparer.Equals(entry.key, key))
                {
                    contains = true;
                    return ref entry.value;
                }

                index = (index + 1) % map->capacity;
            }
            while (index != startIndex);

            contains = false;
            return ref *(V*)default(nint);
        }

        public static bool TryRemove<K, V>(UnsafeDictionary* map, K key, out V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(map);

            uint hash = GetHash(key);
            uint startIndex = hash % map->capacity;
            uint index = startIndex;
            uint originalIndex = index;
            EqualityComparer<K> comparer = EqualityComparer<K>.Default;
            do
            {
                ref Entry<K, V> entry = ref map->entries.Read<Entry<K, V>>(index * map->entryStride);
                if (entry.state == EntryState.Occupied && comparer.Equals(entry.key, key))
                {
                    entry.state = EntryState.Deleted;
                    value = entry.value;
                    entry.key = default;
                    entry.value = default;
                    map->count--;
                    return true;
                }

                if (entry.state == EntryState.Empty)
                {
                    value = default;
                    return false;
                }

                index = (index + 1) % map->capacity;
            }
            while (index != startIndex);

            value = default;
            return false;
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