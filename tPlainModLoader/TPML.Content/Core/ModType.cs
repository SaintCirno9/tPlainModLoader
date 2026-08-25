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
