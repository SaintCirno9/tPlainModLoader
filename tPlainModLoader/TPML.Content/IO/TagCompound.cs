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
            if (TryGet<T>(key, out T val))
                return val;
            return default;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (TryGetValue(key, out object obj) && obj != null)
            {
                if (obj is T direct)
                {
                    value = direct;
                    return true;
                }
                if (obj is Newtonsoft.Json.Linq.JToken jToken)
                {
                    try
                    {
                        value = jToken.ToObject<T>();
                        return true;
                    }
                    catch { }
                }
                if (typeof(T) == typeof(int) && obj is IConvertible cInt)
                {
                    value = (T)(object)cInt.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                if (typeof(T) == typeof(long) && obj is IConvertible cLong)
                {
                    value = (T)(object)cLong.ToInt64(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                if (typeof(T) == typeof(bool) && obj is IConvertible cBool)
                {
                    value = (T)(object)cBool.ToBoolean(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                if (typeof(T) == typeof(float) && obj is IConvertible cFloat)
                {
                    value = (T)(object)cFloat.ToSingle(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                if (typeof(T) == typeof(double) && obj is IConvertible cDouble)
                {
                    value = (T)(object)cDouble.ToDouble(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                if (typeof(T) == typeof(string))
                {
                    value = (T)(object)obj.ToString();
                    return true;
                }
            }
            value = default;
            return false;
        }

        public string GetString(string key) => Get<string>(key) ?? string.Empty;
        public int GetInt(string key) => Get<int>(key);
        public bool GetBool(string key) => Get<bool>(key);
        public float GetFloat(string key) => Get<float>(key);
        public double GetDouble(string key) => Get<double>(key);
        public byte[] GetByteArray(string key) => Get<byte[]>(key);
        public TagCompound GetCompound(string key) => Get<TagCompound>(key);
        public List<T> GetList<T>(string key) => Get<List<T>>(key) ?? new List<T>();
    }
}
