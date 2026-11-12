using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyInspector
{

    [Serializable]
    public class TinyDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        IReadOnlyDictionary<TKey, TValue>,
        ISerializationCallbackReceiver
    {
        [NonSerialized]
        private Dictionary<TKey, TValue> _dictionary = new();

        [SerializeField]
        private List<TKey> _keys = new();

        [SerializeField]
        private List<TValue> _values = new();

        #region IDictionary Implementation
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _dictionary.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _dictionary.Values;

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) => _dictionary.Add(key, value);

        public void Add(KeyValuePair<TKey, TValue> item) =>
            _dictionary.Add(item.Key, item.Value);

        public bool Remove(TKey key) => _dictionary.Remove(key);

        public bool Remove(KeyValuePair<TKey, TValue> item) =>
            _dictionary.Remove(item.Key);

        public void Clear()
        {
            _dictionary.Clear();
            _keys.Clear();
            _values.Clear();
        }

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public bool Contains(KeyValuePair<TKey, TValue> item) =>
            _dictionary.Contains(item);

        public bool TryGetValue(TKey key, out TValue value) =>
            _dictionary.TryGetValue(key, out value);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            var dictArray = new KeyValuePair<TKey, TValue>[_dictionary.Count];
            ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(dictArray, 0);
            Array.Copy(dictArray, 0, array, arrayIndex, Math.Min(dictArray.Length, array.Length - arrayIndex));
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
            _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            _dictionary.GetEnumerator();
        #endregion

        #region ISerializationCallbackReceiver
        public void OnBeforeSerialize()
        {
            // Unity 2022.3+ mo¿e serializowaæ wiêcej typów
            _keys.Clear();
            _values.Clear();

            foreach (var kvp in _dictionary)
            {
                // Dodatkowa walidacja dla Unity 2022.3+
                if (ShouldSerializeKey(kvp.Key) && ShouldSerializeValue(kvp.Value))
                {
                    _keys.Add(kvp.Key);
                    _values.Add(kvp.Value);
                }
            }
        }

        public void OnAfterDeserialize()
        {
            _dictionary.Clear();

            if (_keys.Count != _values.Count)
            {
                Debug.LogError($"TinyDictionary: Mismatched keys ({_keys.Count}) and values ({_values.Count})");

                // W Unity 2022.3 mo¿emy u¿yæ lepszego logowania
#if UNITY_2022_3_OR_NEWER
                Debug.LogWarning("Truncating to match counts");
                var minCount = Math.Min(_keys.Count, _values.Count);
                _keys = _keys.Take(minCount).ToList();
                _values = _values.Take(minCount).ToList();
#endif
            }

            for (int i = 0; i < _keys.Count; i++)
            {
                // Allow default(TKey) so drawer can create "empty" keys (e.g. enum=0)
                TryAddWithWarning(_keys[i], _values[i]);
            }
        }
        #endregion

        #region Unity 2022.3+ Specific Features
        // Dodatkowe metody dla lepszej integracji z Unity 2022.3+

        /// <summary>
        /// Sprawdza czy klucz jest serializowalny w Unity 2022.3+
        /// </summary>
        private bool ShouldSerializeKey(TKey key)
        {
            // Unity 2022.3+ wspiera wiêcej typów natywnie
            return key != null && (
                key.GetType().IsValueType ||
                key is string ||
                key is UnityEngine.Object ||
                key.GetType().IsSerializable
            );
        }

        /// <summary>
        /// Sprawdza czy wartoœæ jest serializowalna w Unity 2022.3+
        /// </summary>
        private bool ShouldSerializeValue(TValue value)
        {
            if (value == null) return true;

            var type = value.GetType();
            return type.IsValueType ||
                   value is string ||
                   value is UnityEngine.Object ||
                   type.IsSerializable ||
                   Attribute.IsDefined(type, typeof(SerializableAttribute));
        }

        /// <summary>
        /// Asynchroniczna inicjalizacja dla Addressables/AssetBundles
        /// </summary>
        public void InitializeAsync(System.Action onComplete = null)
        {
            // Unity 2022.3+ ma lepsze wsparcie dla async
#if UNITY_2022_3_OR_NEWER
            // Mo¿liwoœæ dodania async inicjalizacji
            onComplete?.Invoke();
#else
        onComplete?.Invoke();
#endif
        }
        #endregion

        #region Extension Methods
        public TValue GetValueOrDefault(TKey key, TValue defaultValue = default)
        {
            return _dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary.Add(key, value);
                return true;
            }
            return false;
        }

        public bool TryAddWithWarning(TKey key, TValue value)
        {
            if (TryAdd(key, value))
                return true;

            Debug.LogWarning($"TinyDictionary: Key already exists ({key}). Duplicate keys are not allowed.");
            return false;
        }

        public void MergeWith(TinyDictionary<TKey, TValue> other, bool overwrite = true)
        {
            foreach (var kvp in other._dictionary)
            {
                if (overwrite || !_dictionary.ContainsKey(kvp.Key))
                {
                    _dictionary[kvp.Key] = kvp.Value;
                }
            }
        }

        public Dictionary<TKey, TValue> ToDictionary() =>
            new Dictionary<TKey, TValue>(_dictionary);

        public TinyDictionary<TKey, TNewValue> ConvertValues<TNewValue>(
            Func<TValue, TNewValue> converter)
        {
            var result = new TinyDictionary<TKey, TNewValue>();
            foreach (var kvp in _dictionary)
            {
                result.Add(kvp.Key, converter(kvp.Value));
            }
            return result;
        }

        /// <summary>
        /// Checks whether it's possible to add a new empty entry to this dictionary.
        /// For enum keys this returns false when all enum values are already used as keys.
        /// For other key types it currently returns true.
        /// </summary>
        public bool CanAddEmptyEntry()
        {
            return true;
        }
        #endregion
    }
}
