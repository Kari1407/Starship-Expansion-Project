using System;
using System.Collections.Generic;
using KSPCommunityFixes.Modding;

// Borrowed from KSP-RO at https://github.com/KSP-RO/ROUtils/blob/master/Source/ROUtils/DataTypes/CollectionHelpers.cs
namespace StarshipExpansionProject.Utils.DataTypes
{
    public abstract class PersistenceHelper
    {
        protected static readonly Dictionary<int, Type> _TypeCache = new Dictionary<int, Type>();

        protected const int VERSION = 3;
        protected int _version; // will be set on load

        /// <summary>
        /// Returns a type from a type name and its hash (fuly qualified name preferred).
        /// If the type can't be found or the baseType isn't assignable from the found type,
        /// the base type is used. Successfully found types are cached.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="hash"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static Type GetTypeFromString(string typeName, int hash, Type baseType)
        {
            if (!_TypeCache.TryGetValue(hash, out var cachedType))
            {
                cachedType = HarmonyLib.AccessTools.TypeByName(typeName);
                _TypeCache[hash] = cachedType;
            }

            if (cachedType != null && baseType.IsAssignableFrom(cachedType))
                return cachedType;

            return baseType;
        }
    }

    public abstract class CollectionPersistence<T> : PersistenceHelper, IConfigNode
    {
        protected static readonly Type _Type = typeof(T);

        protected ICollection<T> _coll;

        public CollectionPersistence(ICollection<T> coll) { _coll = coll; }

        public abstract void Load(ConfigNode node);

        public abstract void Save(ConfigNode node);
    }

    public class CollectionPersistenceNode<T> : CollectionPersistence<T> where T : IConfigNode
    {
        protected static readonly string _TypeName = typeof(T).Name;
        protected static readonly int _TypeHash = _TypeName.GetHashCode();
        protected static readonly int _DefaultNodeNameHash = "ITEM".GetHashCode();

        public CollectionPersistenceNode(ICollection<T> coll) : base(coll) { }

        public override void Load(ConfigNode node)
        {
            _coll.Clear();
            int version = 1;
            node.TryGetValue("version", ref version);

            foreach (ConfigNode n in node.nodes)
            {
                T item;
                int hash = n.name.GetHashCode();
                if (version == 1 || hash == _DefaultNodeNameHash || hash == _TypeHash)
                {
                    item = Activator.CreateInstance<T>();
                }
                else
                {
                    item = (T)Activator.CreateInstance(GetTypeFromString(n.name, hash, _Type));
                }
                item.Load(n);
                _coll.Add(item);
            }
            version = VERSION;
        }

        public override void Save(ConfigNode node)
        {
            node.AddValue("version", _version);
            foreach (var item in _coll)
            {
                var type = item.GetType();
                ConfigNode n = new ConfigNode(type == _Type ? _TypeName : type.FullName);
                item.Save(n);
                node.AddNode(n);
            }
        }
    }

    public class CollectionPersistenceParseable<T> : CollectionPersistence<T> where T : class
    {
        private enum ParseableType
        {
            INVALID,
            ProtoCrewMember,
        }

        private static ParseableType GetParseableType()
        {
            if (_Type == typeof(ProtoCrewMember))
                return ParseableType.ProtoCrewMember;

            return ParseableType.INVALID;
        }

        private static readonly ParseableType _ParseType = GetParseableType();

        protected static readonly string _TypeName = typeof(T).Name;
        protected static readonly int _TypeHash = _TypeName.GetHashCode();
        protected static readonly int _DefaultNodeNameHash = "ITEM".GetHashCode();

        public CollectionPersistenceParseable(ICollection<T> coll) : base(coll) { }

        private T FromValue(ConfigNode.Value v)
        {
            switch (_ParseType)
            {
                case ParseableType.ProtoCrewMember:
                    return HighLogic.CurrentGame.CrewRoster[v.value] as T;
            }

            return null;
        }

        private ConfigNode.Value ToValue(T item)
        {
            switch (_ParseType)
            {
                case ParseableType.ProtoCrewMember:
                    return new ConfigNode.Value("item", item.ToString());
            }

            return null;
        }

        public override void Load(ConfigNode node)
        {
            _coll.Clear();
            foreach (ConfigNode.Value v in node.values)
            {
                T item = FromValue(v);
                if (item != null)
                    _coll.Add(item);
            }
        }

        public override void Save(ConfigNode node)
        {
            foreach (var item in _coll)
            {
                var v = ToValue(item);
                if (v != null)
                    node.values.Add(v);
            }
        }
    }

    /// <summary>
    /// NOTE: This does not have constraints because string is supported
    /// but string is not a valuetype
    /// </summary>
    public class CollectionPersistenceValueType<T> : CollectionPersistence<T>
    {
        protected static readonly DataType _DataType = FieldData.ValueDataType(_Type);

        public CollectionPersistenceValueType(ICollection<T> coll) : base(coll) { }

        public override void Load(ConfigNode node)
        {
            _coll.Clear();
            foreach (ConfigNode.Value v in node.values)
            {
                T item = (T)FieldData.ReadValue(v.value, _DataType, _Type);
                _coll.Add(item);
            }
        }

        public override void Save(ConfigNode node)
        {
            foreach (var item in _coll)
            {
                string value = FieldData.WriteValue(item, _DataType);
                node.AddValue("item", value);
            }
        }
    }
}
