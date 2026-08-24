using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Terraria.ModLoader.IO
{
    /// <summary>
    /// tModLoader 字典型 NBT 数据载体
    /// </summary>
    public class TagCompound : IDictionary<string, object>, ICloneable
    {
        private readonly Dictionary<string, object> _dict = new Dictionary<string, object>(StringComparer.Ordinal);

        public object this[string key]
        {
            get => _dict.TryGetValue(key, out var val) ? val : null;
            set => _dict[key] = value;
        }

        public ICollection<string> Keys => _dict.Keys;
        public ICollection<object> Values => _dict.Values;
        public int Count => _dict.Count;
        public bool IsReadOnly => false;

        public void Add(string key, object value) => _dict.Add(key, value);
        public void Add(KeyValuePair<string, object> item) => _dict.Add(item.Key, item.Value);
        public void Clear() => _dict.Clear();
        public bool Contains(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_dict).Contains(item);
        public bool ContainsKey(string key) => _dict.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((IDictionary<string, object>)_dict).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _dict.GetEnumerator();
        public bool Remove(string key) => _dict.Remove(key);
        public bool Remove(KeyValuePair<string, object> item) => ((IDictionary<string, object>)_dict).Remove(item);
        public bool TryGetValue(string key, out object value) => _dict.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();

        public void Set(string key, object value, bool replace = true)
        {
            if (replace || !_dict.ContainsKey(key))
                _dict[key] = value;
        }

        public T Get<T>(string key)
        {
            if (!_dict.TryGetValue(key, out var val) || val == null)
                return default;

            if (val is T exact)
                return exact;

            try
            {
                return (T)Convert.ChangeType(val, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        public int GetInt(string key) => Get<int>(key);
        public string GetString(string key) => Get<string>(key);
        public bool GetBool(string key) => Get<bool>(key);
        public float GetFloat(string key) => Get<float>(key);
        public double GetDouble(string key) => Get<double>(key);
        public byte GetByte(string key) => Get<byte>(key);
        public short GetShort(string key) => Get<short>(key);
        public long GetLong(string key) => Get<long>(key);

        public TagCompound GetCompound(string key)
        {
            if (_dict.TryGetValue(key, out var val) && val is TagCompound tag)
                return tag;
            return new TagCompound();
        }

        public List<T> GetList<T>(string key)
        {
            if (_dict.TryGetValue(key, out var val) && val is List<T> list)
                return list;
            return new List<T>();
        }

        public object Clone()
        {
            var clone = new TagCompound();
            foreach (var kvp in _dict)
            {
                clone[kvp.Key] = kvp.Value;
            }
            return clone;
        }
    }

    /// <summary>
    /// TagIO 序列化帮助类
    /// </summary>
    public static class TagIO
    {
        public static string ToJSON(TagCompound tag, bool pretty = false)
        {
            return JsonConvert.SerializeObject(tag, pretty ? Formatting.Indented : Formatting.None);
        }

        public static TagCompound FromJSON(string json)
        {
            if (string.IsNullOrEmpty(json)) return new TagCompound();
            return JsonConvert.DeserializeObject<TagCompound>(json) ?? new TagCompound();
        }

        public static void ToFile(TagCompound tag, string path)
        {
            File.WriteAllText(path, ToJSON(tag, true));
        }

        public static TagCompound FromFile(string path)
        {
            if (!File.Exists(path)) return new TagCompound();
            return FromJSON(File.ReadAllText(path));
        }
    }
}
