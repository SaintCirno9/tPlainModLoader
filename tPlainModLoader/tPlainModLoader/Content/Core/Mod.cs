using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TPML.Content.Engine;
using TPML.Core.Logging;

namespace TPML.Content
{
    public enum ModSide
    {
        Both,
        Client,
        Server,
        NoSync
    }

    /// <summary>
    /// TPML 模组核心基类
    /// </summary>
    public abstract class Mod
    {
        public virtual string Name { get; internal set; }
        public virtual string DisplayName { get; internal set; }
        public virtual Version Version { get; internal set; } = new Version(1, 0, 0, 0);
        public virtual ModSide Side { get; internal set; } = ModSide.Both;
        public ILogger Logger { get; internal set; }
        public Assembly Code { get; internal set; }
        public Assets.ModAssetRepository Assets { get; internal set; }

        internal readonly Dictionary<string, byte[]> _fileData = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        internal readonly List<ILoadable> _content = new List<ILoadable>();

        protected Mod()
        {
            Name = GetType().Name;
            DisplayName = Name;
            Logger = LogManager.GetLogger(Name);
            Code = GetType().Assembly;
            Assets = new Assets.ModAssetRepository(this);
        }

        public virtual uint ExtraPlayerBuffSlots => 0;

        public virtual ModPacket GetPacket(int capacity = 256)
        {
            return new ModPacket(this, capacity);
        }

        public virtual void Load()
        {
        }

        public virtual void PostSetupContent()
        {
        }

        public virtual void Unload()
        {
        }

        public virtual void AddRecipes()
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
            if (content == null || !content.IsLoadingEnabled(this))
                return;

            string contentName = (content as ModType)?.Name ?? content.GetType().FullName;
            if (_content.Any(existing => existing.GetType() == content.GetType() && ((existing as ModType)?.Name ?? existing.GetType().FullName) == contentName))
                return;

            _content.Add(content);
            content.Load(this);
            ModContent.RegisterContent(content);
            ContentHookDispatcher.RegisterHookInstances(new[] { content });
        }

        public IEnumerable<ILoadable> GetContent()
        {
            return _content;
        }

        public int AddNPCHeadTexture(int npcType, string headTexture)
        {
            return NPCLoader.RegisterHeadSlot(headTexture);
        }

        public int AddNPCBossHeadTexture(int npcType, string bossHeadTexture)
        {
            return NPCLoader.RegisterHeadSlot(bossHeadTexture);
        }
    }
}
