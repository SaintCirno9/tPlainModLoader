using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace TPML.Content.Assets
{
    public class ModAssetRepository
    {
        public Mod Mod { get; }
        private readonly Dictionary<string, object> _cachedAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private static readonly FieldInfo _assetNameField = typeof(Asset<Texture2D>).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo _assetValueField = typeof(Asset<Texture2D>).GetField("ownValue", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? typeof(Asset<Texture2D>).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo _assetStateField = typeof(Asset<Texture2D>).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? typeof(Asset<Texture2D>).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public ModAssetRepository(Mod mod)
        {
            Mod = mod;
        }

        public Asset<T> Request<T>(string assetPath, AssetRequestMode mode = AssetRequestMode.ImmediateLoad) where T : class
        {
            if (string.IsNullOrEmpty(assetPath)) return Asset<T>.Empty;

            if (_cachedAssets.TryGetValue(assetPath, out object val) && val is Asset<T> typed)
                return typed;

            if (typeof(T) == typeof(Texture2D))
            {
                Texture2D texture = LoadTexture(assetPath);
                if (texture != null)
                {
                    var asset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                    _assetNameField?.SetValue(asset, assetPath);
                    _assetValueField?.SetValue(asset, texture);
                    _assetStateField?.SetValue(asset, AssetState.Loaded);

                    var result = asset as Asset<T>;
                    _cachedAssets[assetPath] = result;
                    return result;
                }
            }

            var empty = Asset<T>.Empty;
            _cachedAssets[assetPath] = empty;
            return empty;
        }

        private Texture2D LoadTexture(string assetPath)
        {
            try
            {
                GraphicsDevice device = Main.graphics?.GraphicsDevice;
                if (device == null) return null;

                string cleanPath = assetPath.Replace('\\', '/');
                if (cleanPath.StartsWith(Mod.Name + "/", StringComparison.OrdinalIgnoreCase))
                {
                    cleanPath = cleanPath.Substring(Mod.Name.Length + 1);
                }

                // 1. 查嵌入资源
                if (Mod.Code != null)
                {
                    string resourceName = Mod.Name + "." + cleanPath.Replace('/', '.');
                    string pngResource = resourceName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? resourceName : resourceName + ".png";
                    string rawimgResource = resourceName.EndsWith(".rawimg", StringComparison.OrdinalIgnoreCase) ? resourceName : resourceName + ".rawimg";

                    using (Stream stream = Mod.Code.GetManifestResourceStream(pngResource))
                    {
                        if (stream != null)
                        {
                            return Texture2D.FromStream(device, stream);
                        }
                    }

                    using (Stream stream = Mod.Code.GetManifestResourceStream(rawimgResource))
                    {
                        if (stream != null)
                        {
                            byte[] bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, bytes.Length);
                            return DecodeRawImg(device, bytes);
                        }
                    }
                }

                // 2. 查 Mod._fileData
                string pngFile = cleanPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? cleanPath : cleanPath + ".png";
                if (Mod.HasAsset(pngFile))
                {
                    using (Stream s = Mod.GetFileStream(pngFile))
                    {
                        if (s != null) return Texture2D.FromStream(device, s);
                    }
                }

                string rawimgFile = cleanPath.EndsWith(".rawimg", StringComparison.OrdinalIgnoreCase) ? cleanPath : cleanPath + ".rawimg";
                if (Mod.HasAsset(rawimgFile))
                {
                    byte[] rawBytes = Mod.GetFileBytes(rawimgFile);
                    if (rawBytes != null) return DecodeRawImg(device, rawBytes);
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[ModAssetRepository] 加载材质 [{assetPath}] 异常: {ex.Message}");
            }
            return null;
        }

        private static Texture2D DecodeRawImg(GraphicsDevice device, byte[] rawBytes)
        {
            if (rawBytes == null || rawBytes.Length < 12) return null;
            int width = BitConverter.ToInt32(rawBytes, 4);
            int height = BitConverter.ToInt32(rawBytes, 8);
            int expectedPixelBytes = width * height * 4;
            if (width > 0 && height > 0 && rawBytes.Length >= 12 + expectedPixelBytes)
            {
                Texture2D texture = new Texture2D(device, width, height);
                byte[] pixelData = new byte[expectedPixelBytes];
                Buffer.BlockCopy(rawBytes, 12, pixelData, 0, expectedPixelBytes);
                texture.SetData(pixelData);
                return texture;
            }
            return null;
        }
    }
}
