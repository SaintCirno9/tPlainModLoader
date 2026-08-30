namespace TPML.Content
{
    public interface ILoadable
    {
        void Load(Mod mod);
        void Unload();
        bool IsLoadingEnabled(Mod mod);
    }

    public abstract class ModType : ILoadable
    {
        public Mod Mod { get; internal set; }
        public virtual string Name => GetType().Name;
        public virtual string FullName => (Mod != null ? Mod.Name + "/" : "") + Name;

        public virtual void Load(Mod mod)
        {
            Mod = mod;
            Load();
        }

        /// <summary>
        /// 无参加载生命周期，方便 tML 风格子类覆写
        /// </summary>
        public virtual void Load()
        {
        }

        /// <summary>
        /// 在所有模组内容加载并注册完成后触发
        /// </summary>
        public virtual void PostSetupContent()
        {
        }

        public virtual void Unload()
        {
        }

        public virtual bool IsLoadingEnabled(Mod mod)
        {
            return true;
        }
    }
}
