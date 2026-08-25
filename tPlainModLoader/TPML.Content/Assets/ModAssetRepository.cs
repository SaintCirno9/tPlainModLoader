using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace TPML.Content.Assets
{
    public class ModAssetRepository
    {
        public Mod Mod { get; }
        private readonly Dictionary<string, object> _cachedAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public ModAssetRepository(Mod mod)
        {
            Mod = mod;
        }

        public Asset<T> Request<T>(string assetPath, AssetRequestMode mode = AssetRequestMode.ImmediateLoad) where T : class
        {
            if (_cachedAssets.TryGetValue(assetPath, out object val) && val is Asset<T> typed)
                return typed;

            var empty = Asset<T>.Empty;
            _cachedAssets[assetPath] = empty;
            return empty;
        }
    }
}
