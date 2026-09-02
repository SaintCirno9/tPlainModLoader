using System;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace TPML.Content.Assets
{
    /// <summary>
    /// ReLogic Asset 实例安全创建工厂
    /// 解决 ReLogic.dll 外部程序集在运行时无法直接访问 internal 构造函数与 private setter (防止 MethodAccessException) 的底层封装
    /// 作者: SaintCirno9
    /// </summary>
    public static class AssetFactory
    {
        private static readonly FieldInfo _nameField =
            typeof(Asset<Texture2D>).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(Asset<Texture2D>).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly FieldInfo _valueField =
            typeof(Asset<Texture2D>).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(Asset<Texture2D>).GetField("ownValue", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly FieldInfo _stateField =
            typeof(Asset<Texture2D>).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(Asset<Texture2D>).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        /// <summary>
        /// 安全在内存中构造并装配处于 Loaded 状态的 Asset 实例
        /// </summary>
        /// <param name="texture">纹理内容对象</param>
        /// <param name="name">资产标识名</param>
        public static Asset<Texture2D> CreateLoaded(Texture2D texture, string name = "")
        {
            var asset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
            _nameField?.SetValue(asset, name ?? string.Empty);
            _valueField?.SetValue(asset, texture);
            _stateField?.SetValue(asset, AssetState.Loaded);
            return asset;
        }
    }
}
