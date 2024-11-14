using Collections.Unsafe;
using System;
using Unmanaged;

namespace Collections
{
    /// <summary>
    /// Native dictionary that can be used in unmanaged code.
    /// </summary>
    public unsafe struct Dictionary<K, V> : IDisposable, IEquatable<Dictionary<K, V>> where K : unmanaged, IEquatable<K> where V : unmanaged
    {
        private UnsafeDictionary* value;

        /// <summary>
        /// Number of key-value pairs in the dictionary.
        /// </summary>
        public readonly uint Count => UnsafeDictionary.GetCount(value);

        /// <summary>
        /// Checks if the dictionary has been disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

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
                if (ContainsKey(key))
                {
                    return ref UnsafeDictionary.GetValueRef<K, V>(value, key);
                }
                else
                {
                    throw new NullReferenceException($"The key `{key}` was not found in the dictionary");
                }
            }
        }

        /// <summary>
        /// All keys in the dictionary.
        /// </summary>
        public readonly USpan<K> Keys => UnsafeDictionary.GetKeys<K>(value);

        /// <summary>
        /// Initializes an existing dictionary from the given <paramref name="pointer"/>.
        /// </summary>
        public Dictionary(UnsafeDictionary* pointer)
        {
            value = pointer;
        }

        /// <summary>
        /// Creates a new dictionary with the given <paramref name="initialCapacity"/>.
        /// </summary>
        public Dictionary(uint initialCapacity = 4)
        {
            value = UnsafeDictionary.Allocate<K, V>(initialCapacity);
        }

#if NET
        /// <summary>
        /// Creates a new dictionary.
        /// </summary>
        public Dictionary()
        {
            value = UnsafeDictionary.Allocate<K, V>(4);
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
            UnsafeDictionary.Free(ref value);
        }

        /// <summary>
        /// Checks if the dictionary contains the given <paramref name="key"/>.
        /// </summary>
        public readonly bool ContainsKey(K key)
        {
            return UnsafeDictionary.ContainsKey(value, key);
        }

        /// <summary>
        /// Attempts to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryGetValue(K key, out V value)
        {
            if (ContainsKey(key))
            {
                value = UnsafeDictionary.GetValueRef<K, V>(this.value, key);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Reference to the value if <paramref name="found"/> is true.</returns>
        public readonly ref V TryGetValueRef(K key, out bool found)
        {
            if (ContainsKey(key))
            {
                found = true;
                return ref UnsafeDictionary.GetValueRef<K, V>(value, key);
            }
            else
            {
                found = false;
                void* nullPointer = null;
                return ref *(V*)nullPointer;
            }
        }

        /// <summary>
        /// Adds the specified <paramref name="key"/> and <paramref name="value"/> pair to the dictionary.
        /// <para>
        /// May throw <see cref="ArgumentException"/> if the key already exists.
        /// </para>
        /// </summary>
        public readonly void Add(K key, V value)
        {
            UnsafeDictionary.Add(this.value, key, value);
        }

        /// <summary>
        /// Adds the specified <paramref name="key"/> and <paramref name="value"/> pair to the dictionary,
        /// or sets if already contained.
        /// </summary>
        /// <returns><c>true</c> if the key was added, <c>false</c> if set.</returns>
        public readonly bool AddOrSet(K key, V value)
        {
            if (ContainsKey(key))
            {
                ref V existingValue = ref UnsafeDictionary.GetValueRef<K, V>(this.value, key);
                existingValue = value;
                return false;
            }
            else
            {
                Add(key, value);
                return true;
            }
        }

        /// <summary>
        /// Adds an empty value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>Reference to the added value.</returns>
        public readonly ref V AddRef(K key)
        {
            UnsafeDictionary.Add<K, V>(value, key, default);
            return ref UnsafeDictionary.GetValueRef<K, V>(value, key);
        }

        /// <summary>
        /// Attempts to add the specified <paramref name="key"/> and <paramref name="value"/> pair to the dictionary.
        /// </summary>
        /// <returns><c>true</c> if successful.</returns>
        public readonly bool TryAdd(K key, V value)
        {
            if (ContainsKey(key))
            {
                return false;
            }
            else
            {
                Add(key, value);
                return true;
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
            V existingValue = UnsafeDictionary.GetValueRef<K, V>(value, key);
            UnsafeDictionary.Remove(value, key);
            return existingValue;
        }

        /// <summary>
        /// Attempts to remove the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if found and removed.</returns>
        public readonly bool TryRemove(K key, out V removed)
        {
            if (ContainsKey(key))
            {
                removed = Remove(key);
                return true;
            }
            else
            {
                removed = default;
                return false;
            }
        }

        /// <summary>
        /// Clears the dictionary.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeDictionary.Clear(value);
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

            return value == other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
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
