// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace FormFinch.JsonSchemaValidation.Common
{
    /// <summary>
    /// A thread-safe Least Recently Used (LRU) cache with fixed capacity.
    /// When the cache reaches capacity, the least recently accessed entry is evicted.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    internal sealed class LruCache<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _dictionary;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> _accessOrder;
        private readonly Lock _lock = new();
        private readonly int _capacity;

        /// <summary>
        /// Initializes a new instance of the <see cref="LruCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="capacity">The maximum number of entries the cache can hold.</param>
        /// <param name="comparer">The equality comparer for keys, or null to use the default comparer.</param>
        public LruCache(int capacity, IEqualityComparer<TKey>? comparer = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0);

            _capacity = capacity;
            _dictionary = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(comparer);
            _accessOrder = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        /// <summary>
        /// Gets the number of entries in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                _lock.Enter();
                try
                {
                    return _dictionary.Count;
                }
                finally
                {
                    _lock.Exit();
                }
            }
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// If found, the entry is marked as most recently used.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">When this method returns, contains the value if found; otherwise, the default value.</param>
        /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            _lock.Enter();
            try
            {
                if (_dictionary.TryGetValue(key, out var node))
                {
                    // Move to front (most recently used)
                    _accessOrder.Remove(node);
                    _accessOrder.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }

                value = default;
                return false;
            }
            finally
            {
                _lock.Exit();
            }
        }

        /// <summary>
        /// Adds or updates an entry in the cache.
        /// If the cache is at capacity, the least recently used entry is evicted.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        public void Set(TKey key, TValue value)
        {
            _lock.Enter();
            try
            {
                if (_dictionary.TryGetValue(key, out var existingNode))
                {
                    // Update existing: remove and re-add at front
                    _accessOrder.Remove(existingNode);
                    var newNode = _accessOrder.AddFirst(new KeyValuePair<TKey, TValue>(key, value));
                    _dictionary[key] = newNode;
                }
                else
                {
                    // Evict LRU entry if at capacity
                    if (_dictionary.Count >= _capacity)
                    {
                        var oldest = _accessOrder.Last!;
                        _accessOrder.RemoveLast();
                        _dictionary.Remove(oldest.Value.Key);
                    }

                    // Add new entry at front (most recently used)
                    var node = _accessOrder.AddFirst(new KeyValuePair<TKey, TValue>(key, value));
                    _dictionary[key] = node;
                }
            }
            finally
            {
                _lock.Exit();
            }
        }
    }
}
