using System;
using System.Collections.Generic;

namespace TPML.Content.IO
{
    public class TagCompound : Dictionary<string, object>
    {
        public TagCompound() : base(StringComparer.OrdinalIgnoreCase) { }

        public new object this[string key]
        {
            get => base.TryGetValue(key, out var val) ? val : null;
            set
            {
                if (value is Terraria.Item item)
                {
                    base[key] = ItemIO.Save(item);
                }
                else
                {
                    base[key] = value;
                }
            }
        }

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
                if (typeof(T) == typeof(Terraria.Item))
                {
                    if (obj is TagCompound tagComp)
                    {
                        value = (T)(object)ItemIO.Load(tagComp);
                        return true;
                    }
                    if (obj is Newtonsoft.Json.Linq.JObject jObj)
                    {
                        var tc = new TagCompound();
                        foreach (var prop in jObj.Properties())
                        {
                            tc[prop.Name] = prop.Value;
                        }
                        value = (T)(object)ItemIO.Load(tc);
                        return true;
                    }
                }
                if (typeof(T) == typeof(TagCompound) && obj is Newtonsoft.Json.Linq.JObject jCompound)
                {
                    var tc = new TagCompound();
                    foreach (var prop in jCompound.Properties())
                    {
                        tc[prop.Name] = prop.Value;
                    }
                    value = (T)(object)tc;
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
        public Terraria.Item GetItem(string key) => Get<Terraria.Item>(key) ?? new Terraria.Item();
    }

    /// <summary>
    /// tML Item 序列化与反序列化工具类
    /// </summary>
    public static class ItemIO
    {
        public static TagCompound Save(Terraria.Item item)
        {
            var tag = new TagCompound();
            if (item == null || item.IsAir || item.type <= 0)
            {
                tag["id"] = 0;
                return tag;
            }

            var modItem = TPML.Content.ItemLoader.GetModItem(item);
            if (modItem != null)
            {
                tag["mod"] = modItem.Mod?.Name ?? string.Empty;
                tag["name"] = modItem.Name;
            }
            else
            {
                tag["mod"] = "Terraria";
                tag["id"] = item.type;
            }

            tag["stack"] = item.stack;
            if (item.prefix > 0) tag["prefix"] = item.prefix;
            if (item.favorited) tag["favorited"] = true;

            // 自定义 TagCompound 数据
            if (modItem != null)
            {
                var customTag = new TagCompound();
                try
                {
                    modItem.SaveData(customTag);
                    if (customTag.Count > 0) tag["data"] = customTag;
                }
                catch (Exception ex)
                {
                    TPML.Content.ModLoader.Log($"[ItemIO] 保存 ModItem [{modItem.FullName}] 数据异常: {ex.Message}");
                }
            }

            return tag;
        }

        public static Terraria.Item Load(TagCompound tag)
        {
            if (tag == null) return new Terraria.Item();

            string mod = tag.GetString("mod");
            string name = tag.GetString("name");
            int id = tag.GetInt("id");
            if (id <= 0 && tag.ContainsKey("type"))
            {
                id = tag.GetInt("type");
            }

            int type = 0;
            if (!string.IsNullOrEmpty(mod) && mod != "Terraria" && !string.IsNullOrEmpty(name))
            {
                type = TPML.Content.ItemLoader.ItemType(mod, name);
            }
            else if (id > 0)
            {
                type = id;
            }

            if (type <= 0) return new Terraria.Item();

            var item = new Terraria.Item();
            item.SetDefaults(type);
            int stack = tag.GetInt("stack");
            if (stack > 0) item.stack = stack;
            int prefix = tag.GetInt("prefix");
            if (prefix > 0) item.Prefix(prefix);
            item.favorited = tag.GetBool("favorited");

            if (tag.ContainsKey("data"))
            {
                var modItem = TPML.Content.ItemLoader.GetModItem(item);
                if (modItem != null)
                {
                    var dataTag = tag.GetCompound("data");
                    if (dataTag != null)
                    {
                        try
                        {
                            modItem.LoadData(dataTag);
                        }
                        catch (Exception ex)
                        {
                            TPML.Content.ModLoader.Log($"[ItemIO] 载入 ModItem [{modItem.FullName}] 数据异常: {ex.Message}");
                        }
                    }
                }
            }

            return item;
        }
    }
}

namespace Terraria.ModLoader.IO
{
    public class TagCompound : TPML.Content.IO.TagCompound
    {
    }

    public static class ItemIO
    {
        public static TPML.Content.IO.TagCompound Save(Terraria.Item item) => TPML.Content.IO.ItemIO.Save(item);
        public static Terraria.Item Load(TPML.Content.IO.TagCompound tag) => TPML.Content.IO.ItemIO.Load(tag);
    }
}
