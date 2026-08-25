using System;
using System.Collections.Generic;

namespace TPML.Content.IO
{
    public class TagCompound : Dictionary<string, object>
    {
        public TagCompound() : base(StringComparer.OrdinalIgnoreCase) { }

        public void Set(string key, object value) => this[key] = value;

        public T Get<T>(string key)
        {
            if (TryGetValue(key, out object obj) && obj is T val)
                return val;
            return default;
        }

        public string GetString(string key) => Get<string>(key) ?? string.Empty;
        public int GetInt(string key) => Get<int>(key);
        public bool GetBool(string key) => Get<bool>(key);
        public float GetFloat(string key) => Get<float>(key);
        public double GetDouble(string key) => Get<double>(key);
        public byte[] GetByteArray(string key) => Get<byte[]>(key);
        public TagCompound GetCompound(string key) => Get<TagCompound>(key);
        public List<T> GetList<T>(string key) => Get<List<T>>(key);
    }
}
