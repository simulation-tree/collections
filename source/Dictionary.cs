using Collections.Unsafe;
using System;
using System.Collections.Generic;

namespace Collections
{
    /// <summary>
    /// Native dictionary that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Dictionary<K, V> : IDisposable, IEquatable<Dictionary<K, V>> where K : unmanaged, IEquatable<K> where V : unmanaged
    {
        private UnsafeDictionary* dictionary;

        /// <summary>
        /// Number of key-value pairs in the dictionary.
        /// </summary>
        public readonly uint Count => UnsafeDictionary.GetCount(dictionary);

        /// <summary>
        /// Capacity of the dictionary.
        /// </summary>
        public readonly uint Capacity => UnsafeDictionary.GetCapacity(dictionary);

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
                ref V value = ref UnsafeDictionary.TryGetValue<K, V>(dictionary, key, out bool contains);
                if (!contains)
                {
                    throw new NullReferenceException($"The key `{key}` was not found in the dictionary");
                }

                return ref value;
            }
        }

        /// <summary>
        /// All keys in the dictionary.
        /// </summary>
        public unsafe readonly IEnumerable<K> Keys
        {
            get
            {
                uint count = Capacity;
                for (uint i = 0; i < count; i++)
                {
                    UnsafeDictionary.Entry<K, V> entry;
                    unsafe
                    {
                        entry = UnsafeDictionary.GetEntry<K, V>(dictionary, i);
                    }

                    if (entry.state == UnsafeDictionary.EntryState.Occupied)
                    {
                        yield return entry.key;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes an existing dictionary from the given <paramref name="pointer"/>.
        /// </summary>
        public Dictionary(UnsafeDictionary* pointer)
        {
            dictionary = pointer;
        }

        /// <summary>
        /// Creates a new dictionary with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Dictionary(uint initialCapacity = 4)
        {
            dictionary = UnsafeDictionary.Allocate<K, V>(initialCapacity);
        }

#if NET
        /// <summary>
        /// Creates a new dictionary.
        /// </summary>
        public Dictionary()
        {
            dictionary = UnsafeDictionary.Allocate<K, V>(4);
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
            UnsafeDictionary.Free(ref dictionary);
        }

        /// <summary>
        /// Checks if the dictionary contains the given <paramref name="key"/>.
        /// </summary>
        public readonly bool ContainsKey(K key)
        {
            return UnsafeDictionary.ContainsKey<K, V>(dictionary, key);
        }

        /// <summary>
        /// Attempts to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryGetValue(K key, out V value)
        {
            ref V found = ref UnsafeDictionary.TryGetValue<K, V>(dictionary, key, out bool contains);
            value = contains ? found : default;
            return contains;
        }

        /// <summary>
        /// Attempts to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Reference to the value if <paramref name="contains"/> is true.</returns>
        public readonly ref V TryGetValue(K key, out bool contains)
        {
            return ref UnsafeDictionary.TryGetValue<K, V>(dictionary, key, out contains);
        }

        /// <summary>
        /// Attempts to add the specified <paramref name="key"/> and <paramref name="value"/> pair to the dictionary.
        /// </summary>
        /// <returns><c>true</c> if successful.</returns>
        public readonly bool TryAdd(K key, V value)
        {
            return UnsafeDictionary.TryAdd(dictionary, key, value);
        }

        /// <summary>
        /// Assigns the specified <paramref name="value"/> to the <paramref name="key"/>.
        /// <para>
        /// May throw <see cref="NullReferenceException"/> if the key is not found.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public readonly void Set(K key, V value)
        {
            ref V existingValue = ref UnsafeDictionary.TryGetValue<K, V>(dictionary, key, out bool contains);
            if (contains)
            {
                existingValue = value;
            }
            else
            {
                throw new NullReferenceException($"The key `{key}` was not found in the dictionary");
            }
        }

        /// <summary>
        /// Adds the specified <paramref name="key"/> and <paramref name="value"/> pair to the dictionary,
        /// or sets if already contained.
        /// </summary>
        /// <returns><c>true</c> if the key was added, <c>false</c> if set.</returns>
        public readonly bool AddOrSet(K key, V value)
        {
            ref V existingValue = ref UnsafeDictionary.TryGetValue<K, V>(dictionary, key, out bool contains);
            if (contains)
            {
                existingValue = value;
                return false;
            }
            else
            {
                UnsafeDictionary.TryAdd(dictionary, key, value);
                return true;
            }
        }

        /// <summary>
        /// Adds an empty value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Reference to the added value.</returns>
        public readonly ref V AddRef(K key)
        {
            if (UnsafeDictionary.TryAdd<K, V>(dictionary, key, default))
            {
                return ref UnsafeDictionary.TryGetValue<K, V>(dictionary, key, out _);
            }
            else
            {
                throw new InvalidOperationException($"The key `{key}` already exists in the dictionary");
            }
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
            if (UnsafeDictionary.TryRemove(dictionary, key, out V value))
            {
                return value;
            }
            else
            {
                throw new NullReferenceException($"The key `{key}` was not found in the dictionary");
            }
        }

        /// <summary>
        /// Attempts to remove the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found and removed.</returns>
        public readonly bool TryRemove(K key, out V removed)
        {
            return UnsafeDictionary.TryRemove(dictionary, key, out removed);
        }

        /// <summary>
        /// Attempts to remove the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found and removed.</returns>
        public readonly bool TryRemove(K key)
        {
            return UnsafeDictionary.TryRemove<K, V>(dictionary, key, out _);
        }

        /// <summary>
        /// Clears the dictionary.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeDictionary.Clear(dictionary);
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

        /// <inheritdoc/>
        public static bool operator ==(Dictionary<K, V> left, Dictionary<K, V> right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Dictionary<K, V> left, Dictionary<K, V> right)
        {
            return !(left == right);
        }
    }
}
