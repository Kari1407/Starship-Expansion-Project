using System;
using System.Collections.Generic;
using KSPCommunityFixes.Modding;
using UnityEngine;

// Borrowed from KSP-RO at https://github.com/KSP-RO/ROUtils/blob/master/Source/ROUtils/DataTypes/DictionaryHelpers.cs
namespace StarshipExpansionProject.Utils.DataTypes
{
    public interface IReadOnlyCollectionDictionary<TKey, TValue, TCollection> : IReadOnlyDictionary<TKey, TCollection> where TCollection : ICollection<TValue>, new()
    {
        CollectionDictionaryAllValues<TKey, TValue, TCollection> AllValues { get; }
    }
    public abstract class DictionaryPersistence<TKey, TValue> : PersistenceHelper, IConfigNode
    {
        protected static readonly Type _KeyType = typeof(TKey);
        protected static readonly Type _ValueType = typeof(TValue);

        protected IDictionary<TKey, TValue> _dict;

        public DictionaryPersistence(IDictionary<TKey, TValue> dict) { _dict = dict; }

        public abstract void Load(ConfigNode node);

        public abstract void Save(ConfigNode node);
    }

    public abstract class DictionaryPersistenceValueNode<TKey, TValue> : DictionaryPersistence<TKey, TValue> where TValue : IConfigNode
    {
        protected static readonly string _ValueTypeName = typeof(TValue).Name;
        protected static readonly int _ValueTypeHash = _ValueTypeName.GetHashCode();
        protected static readonly int _DefaultValueNameHash = "VALUE".GetHashCode();

        public DictionaryPersistenceValueNode(IDictionary<TKey, TValue> dict) : base(dict) { }

        protected abstract TKey GetKey(int i, ConfigNode keyNode);
        protected abstract void AddKey(TKey key, ConfigNode keyNode);

        protected virtual TValue GetValue(int i, ConfigNode valueNode)
        {
            var n = valueNode.nodes[i];
            TValue value;
            int hash = n.name.GetHashCode();
            if (_version == 1 || hash == _DefaultValueNameHash || hash == _ValueTypeHash)
            {
                value = Activator.CreateInstance<TValue>();
            }
            else
            {
                value = (TValue)Activator.CreateInstance(GetTypeFromString(n.name, hash, _ValueType));
            }
            value.Load(n);
            return value;
        }

        protected virtual void AddValue(TValue value, ConfigNode valueNode)
        {
            var type = value.GetType();
            ConfigNode n = new ConfigNode(type == _ValueType ? _ValueTypeName : type.FullName);
            value.Save(n);
            valueNode.AddNode(n);
        }

        public override void Load(ConfigNode node)
        {
            _dict.Clear();
            ConfigNode keyNode = node.nodes[0];
            ConfigNode valueNode = node.nodes[1];
            _version = 1;
            node.TryGetValue("version", ref _version);

            int c = valueNode.nodes.Count;
            for (int i = 0; i < c; ++i)
            {
                TKey key = GetKey(i, keyNode);
                TValue value = GetValue(i, valueNode);
                _dict.Add(key, value);
            }
            _version = VERSION;
        }

        public override void Save(ConfigNode node)
        {
            node.AddValue("version", _version);
            ConfigNode keyNode = node.AddNode("Keys");
            ConfigNode valueNode = node.AddNode("Values");

            foreach (var kvp in _dict)
            {
                AddKey(kvp.Key, keyNode);
                AddValue(kvp.Value, valueNode);
            }
        }
    }

    public class DictionaryPersistenceBothObjects<TKey, TValue> : DictionaryPersistenceValueNode<TKey, TValue> where TKey : IConfigNode where TValue : IConfigNode
    {
        protected static readonly string _KeyTypeName = typeof(TKey).Name;
        protected static readonly int _KeyTypeHash = _KeyTypeName.GetHashCode();
        protected static readonly int _DefaultKeyNameHash = "KEY".GetHashCode();

        public DictionaryPersistenceBothObjects(IDictionary<TKey, TValue> dict) : base(dict) { }

        protected override TKey GetKey(int i, ConfigNode keyNode)
        {
            var n = keyNode.nodes[i];
            TKey key;
            int hash = n.name.GetHashCode();
            if (_version == 1 || hash == _DefaultKeyNameHash || hash == _KeyTypeHash)
            {
                key = Activator.CreateInstance<TKey>();
            }
            else
            {
                key = (TKey)Activator.CreateInstance(GetTypeFromString(n.name, hash, _KeyType));
            }
            key.Load(n);
            return key;
        }

        protected override void AddKey(TKey key, ConfigNode keyNode)
        {
            var type = key.GetType();
            ConfigNode n = new ConfigNode(type == _KeyType ? _KeyTypeName : type.FullName);
            key.Save(n);
            keyNode.AddNode(n);
        }
    }

    public class DictionaryPersistenceNodeValueKeyed<TKey, TValue> : DictionaryPersistenceValueNode<TKey, TValue> where TValue : IConfigNode
    {
        protected static readonly DataType _KeyDataType = FieldData.ValueDataType(_KeyType);
        protected string _keyName = "name";

        public DictionaryPersistenceNodeValueKeyed(IDictionary<TKey, TValue> dict, string keyName = null) : base(dict)
        {
            if (keyName != null)
                _keyName = keyName;
        }

        protected override TKey GetKey(int i, ConfigNode keyNode)
        {
            return (TKey)FieldData.ReadValue(keyNode.nodes[i].GetValue(_keyName), _KeyDataType, _KeyType);
        }

        protected override void AddKey(TKey key, ConfigNode keyNode)
        {
            // AddValue (our Save runs that first) creates the node
            // and it will be the last node. So put the key there.
            keyNode.nodes[keyNode.nodes.Count - 1].SetValue(_keyName, FieldData.WriteValue(key, _KeyDataType), true);
        }

        public override void Load(ConfigNode node)
        {
            _dict.Clear();
            _version = 1;
            node.TryGetValue("version", ref _version);
            for (int i = 0; i < node.nodes.Count; ++i)
            {
                TKey key = GetKey(i, node);
                //if (string.IsNullOrEmpty(key))
                //{
                //    Debug.LogError("PersistentDictionaryNodeKeyed: null or empty key in node! Skipping. Node=\n" + node.nodes[i].ToString());
                //    continue;
                //}

                TValue value = GetValue(i, node);
                _dict.Add(key, value);
            }
            _version = VERSION;
        }

        public override void Save(ConfigNode node)
        {
            node.AddValue("version", _version);
            foreach (var kvp in _dict)
            {
                AddValue(kvp.Value, node);
                AddKey(kvp.Key, node);
            }
        }
    }

    public class DictionaryPersistenceNodeStringHashKeyed<TValue> : DictionaryPersistenceValueTypeKey<int, TValue> where TValue : IConfigNode
    {
        protected string _keyName = "name";

        public DictionaryPersistenceNodeStringHashKeyed(IDictionary<int, TValue> dict, string keyName = null) : base(dict)
        {
            if (keyName != null)
                _keyName = keyName;
        }

        public override void Load(ConfigNode node)
        {
            bool hasKeyNode = false;
            node.TryGetValue("hasKeyNode", ref hasKeyNode);
            if (hasKeyNode)
            {
                base.Load(node);
                return;
            }

            _dict.Clear();
            _version = 1;
            node.TryGetValue("version", ref _version);
            for (int i = 0; i < node.nodes.Count; ++i)
            {
                string key = node.nodes[i].GetValue(_keyName);
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogError("DictionaryPersistenceNodeStringHashKeyed: null or empty key in node! Skipping. Node=\n" + node.nodes[i].ToString());
                    continue;
                }

                TValue value = GetValue(i, node);
                _dict.Add(key.GetHashCode(), value);
            }
            _version = VERSION;
            return;
        }

        public override void Save(ConfigNode node)
        {
            base.Save(node);
            node.AddValue("hasKeyNode", true);
        }
    }

    public class DictionaryPersistenceValueTypeKey<TKey, TValue> : DictionaryPersistenceValueNode<TKey, TValue> where TValue : IConfigNode
    {
        protected static readonly DataType _KeyDataType = FieldData.ValueDataType(_KeyType);

        public DictionaryPersistenceValueTypeKey(IDictionary<TKey, TValue> dict) : base(dict) { }

        protected override TKey GetKey(int i, ConfigNode keyNode)
        {
            return (TKey)FieldData.ReadValue(keyNode.values[i].value, _KeyDataType, _KeyType);
        }

        protected override void AddKey(TKey key, ConfigNode keyNode)
        {
            keyNode.AddValue("key", FieldData.WriteValue(key, _KeyDataType));
        }
    }

    public class DictionaryPersistenceValueTypes<TKey, TValue> : DictionaryPersistence<TKey, TValue>
    {
        protected static readonly DataType _KeyDataType = FieldData.ValueDataType(_KeyType);
        protected static readonly DataType _ValueDataType = FieldData.ValueDataType(_ValueType);

        public DictionaryPersistenceValueTypes(IDictionary<TKey, TValue> dict) : base(dict) { }

        public override void Load(ConfigNode node)
        {
            _dict.Clear();
            foreach (ConfigNode.Value v in node.values)
            {
                TKey key = (TKey)FieldData.ReadValue(v.name, _KeyDataType, _KeyType);
                TValue value = (TValue)FieldData.ReadValue(v.value, _ValueDataType, _ValueType);
                if (_dict.ContainsKey(key))
                {
                    Debug.LogError($"PersistentDictionary: Contains key {key}");
                    _dict.Remove(key);
                }
                _dict.Add(key, value);
            }
        }

        public override void Save(ConfigNode node)
        {
            foreach (var kvp in _dict)
            {
                string key = FieldData.WriteValue(kvp.Key, _KeyDataType);
                string value = FieldData.WriteValue(kvp.Value, _ValueDataType);
                node.AddValue(key, value);
            }
        }
    }

    public sealed class CollectionDictionaryAllValues<TKey, TValue, TCollection> : ICollection<TValue>, IEnumerable<TValue>, System.Collections.IEnumerable, System.Collections.ICollection, IReadOnlyCollection<TValue> where TCollection : ICollection<TValue>
    {
        public struct CollectionDictionaryEnumerator : IEnumerator<TValue>, IDisposable, System.Collections.IEnumerator
        {
            private ICollection<TCollection> _dictValues;
            private IEnumerator<TCollection> _dictEnum;
            private IEnumerator<TValue> _collEnum;
            private bool _isValid;

            private TValue currentValue;

            public TValue Current => currentValue;

            object System.Collections.IEnumerator.Current => currentValue;

            public CollectionDictionaryEnumerator(IDictionary<TKey, TCollection> dictionary)
            {
                _dictValues = dictionary.Values;
                _dictEnum = _dictValues.GetEnumerator();
                _isValid = _dictEnum.MoveNext();
                if (_isValid)
                    _collEnum = _dictEnum.Current.GetEnumerator();
                else
                    _collEnum = default;
                currentValue = default(TValue);
            }

            /// <summary>Releases all resources used by the <see cref="T:System.Collections.Generic.Dictionary`2.ValueCollection.Enumerator" />.</summary>
            public void Dispose()
            {
                if (_isValid)
                    _collEnum.Dispose();
                _dictEnum.Dispose();
            }

            public bool MoveNext()
            {
                // If dict was empty, do nothing
                if (!_isValid)
                {
                    currentValue = default(TValue);
                    return false;
                }

                // we start with a valid collection enumerator if dict
                // was non-empty. So start there.
                if (_collEnum.MoveNext())
                {
                    currentValue = _collEnum.Current;
                    return true;
                }

                // If we run off the edge of the collection, try to advance
                // to the next collection
                if (!_dictEnum.MoveNext())
                {
                    currentValue = default(TValue);
                    return false;
                }

                // We have a new collection, so dispose the old enumerator
                // and grab the new one. Then try again.
                _collEnum.Dispose();
                _collEnum = _dictEnum.Current.GetEnumerator();
                return MoveNext();
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (_isValid)
                    _collEnum.Dispose();
                _dictEnum = _dictValues.GetEnumerator();
                _dictEnum.MoveNext();
                if (_isValid)
                    _collEnum = _dictEnum.Current.GetEnumerator();

                currentValue = default(TValue);
            }
        }

        private IDictionary<TKey, TCollection> _dict;

        public CollectionDictionaryAllValues(IDictionary<TKey, TCollection> dict) { _dict = dict; }

        public int Count
        {
            get
            {
                int c = 0;
                foreach (var coll in _dict.Values)
                    c += coll.Count;

                return c;
            }
        }

        bool ICollection<TValue>.IsReadOnly => true;

        bool System.Collections.ICollection.IsSynchronized => false;

        object System.Collections.ICollection.SyncRoot => ((System.Collections.ICollection)_dict).SyncRoot;

        public CollectionDictionaryEnumerator GetEnumerator() => new CollectionDictionaryEnumerator(_dict);
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => new CollectionDictionaryEnumerator(_dict);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new CollectionDictionaryEnumerator(_dict);

        public void CopyTo(TValue[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException("index", index, "Index was out of range. Must be non-negative and less than the size of the collection.");
            }
            if (array.Length - index < Count)
            {
                throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
            }

            foreach (var coll in _dict.Values)
            {
                coll.CopyTo(array, index);
                index += coll.Count;
            }
        }

        void System.Collections.ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (array.Rank != 1)
            {
                throw new ArgumentException("Only single dimensional arrays are supported for the requested action.", "array");
            }
            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException("The lower bound of target array must be zero.", "array");
            }
            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException("index", index, "Index was out of range. Must be non-negative and less than the size of the collection.");
            }
            if (array.Length - index < Count)
            {
                throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
            }
            TValue[] array2 = array as TValue[];
            if (array2 != null)
            {
                CopyTo(array2, index);
                return;
            }
            object[] array3 = array as object[];
            if (array3 == null)
            {
                throw new ArgumentException("Target array type is not compatible with the type of items in the collection.", "array");
            }

            try
            {
                foreach (var coll in _dict.Values)
                {
                    ((System.Collections.ICollection)coll).CopyTo(array3, index);
                    index += coll.Count;
                }
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Target array type is not compatible with the type of items in the collection.", "array");
            }
        }

        void ICollection<TValue>.Add(TValue item)
        {
            throw new NotSupportedException("Mutating a value collection derived from a dictionary is not allowed.");
        }

        bool ICollection<TValue>.Remove(TValue item)
        {
            throw new NotSupportedException("Mutating a value collection derived from a dictionary is not allowed.");
        }

        void ICollection<TValue>.Clear()
        {
            throw new NotSupportedException("Mutating a value collection derived from a dictionary is not allowed.");
        }

        bool ICollection<TValue>.Contains(TValue item)
        {
            foreach (var coll in _dict.Values)
            {
                if (coll.Contains(item))
                    return true;
            }

            return false;
        }
    }
}
