using System;

namespace Terraria.ModLoader
{
    /// <summary>
    /// 可由模组加载的内容接口
    /// </summary>
    public interface ILoadable
    {
        void Load(Mod mod);
        bool IsLoadingEnabled(Mod mod);
        void Unload();
    }

    /// <summary>
    /// tModLoader 泛型 ModType 基类
    /// </summary>
    public abstract class ModType : ILoadable
    {
        public Mod Mod { get; internal set; }
        public string Name => GetType().Name;
        public virtual string FullName => $"{Mod?.Name ?? "Terraria"}/{Name}";

        public virtual void Load(Mod mod)
        {
            Mod = mod;
            InitTemplateInstance();
            Load();
            SetStaticDefaults();
        }

        public virtual void Load()
        {
        }

        public virtual bool IsLoadingEnabled(Mod mod) => true;

        public virtual void Unload()
        {
        }

        public virtual void SetStaticDefaults()
        {
        }

        protected virtual void InitTemplateInstance()
        {
        }
    }
}
