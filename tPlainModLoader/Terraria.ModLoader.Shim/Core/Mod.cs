using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Terraria.ModLoader
{
    public enum ModSide
    {
        Both,
        Client,
        Server,
        NoSync
    }

    /// <summary>
    /// tModLoader 模组核心基类
    /// </summary>
    public abstract class Mod
    {
        public virtual string Name { get; internal set; }
        public virtual string DisplayName { get; internal set; }
        public virtual Version Version { get; internal set; } = new Version(1, 0, 0, 0);
        public virtual ModSide Side { get; internal set; } = ModSide.Both;
        public ModLogger Logger { get; internal set; }
        public Assembly Code { get; internal set; }
        public Assets.ModAssetRepository Assets { get; internal set; }

        internal readonly Dictionary<string, byte[]> _fileData = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        internal readonly List<ILoadable> _content = new List<ILoadable>();

        protected Mod()
        {
            Name = GetType().Name;
            DisplayName = Name;
            Logger = new ModLogger(Name);
            Code = GetType().Assembly;
            Assets = new Assets.ModAssetRepository(this);
        }

        public virtual void Load()
        {
        }

        public virtual void Unload()
        {
        }

        public virtual void PostSetupContent()
        {
        }

        public virtual object Call(params object[] args)
        {
            return null;
        }

        public virtual void HandlePacket(BinaryReader reader, int whoAmI)
        {
        }

        public bool HasAsset(string assetName)
        {
            return _fileData.ContainsKey(assetName);
        }

        public byte[] GetFileBytes(string name)
        {
            if (_fileData.TryGetValue(name, out byte[] bytes))
                return bytes;
            return null;
        }

        public Stream GetFileStream(string name, bool newInstance = false)
        {
            byte[] bytes = GetFileBytes(name);
            return bytes != null ? new MemoryStream(bytes) : null;
        }

        public void AddContent(ILoadable content)
        {
            _content.Add(content);
            ModContent.RegisterContent(content);
        }

        public IEnumerable<ILoadable> GetContent()
        {
            return _content;
        }
    }
}
