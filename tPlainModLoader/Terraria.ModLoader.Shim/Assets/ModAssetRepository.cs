using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader.Engine;

namespace Terraria.ModLoader.Assets
{
    /// <summary>
    /// 模组虚拟资产仓库
    /// </summary>
    public class ModAssetRepository
    {
        private readonly Mod _mod;
        private readonly Dictionary<string, object> _loadedAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private static Texture2D _fallbackTexture;

        public ModAssetRepository(Mod mod)
        {
            _mod = mod ?? throw new ArgumentNullException(nameof(mod));
        }

        public Asset<T> Request<T>(string assetPath, AssetRequestMode mode = AssetRequestMode.AsyncLoad) where T : class
        {
            string key = assetPath.Replace("\\", "/");
            if (key.StartsWith(_mod.Name + "/", StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring(_mod.Name.Length + 1);
            }

            if (_loadedAssets.TryGetValue(key, out object existing) && existing is Asset<T> cached)
            {
                return cached;
            }

            if (typeof(T) == typeof(Texture2D))
            {
                Texture2D texture = LoadTexture(key);
                if (texture == null)
                {
                    texture = GetFallbackTexture();
                }

                if (texture != null)
                {
                    var asset = CreateAsset<T>(texture as T, key);
                    _loadedAssets[key] = asset;
                    return asset;
                }
            }

            return null;
        }

        private Texture2D LoadTexture(string key)
        {
            byte[] bytes = _mod.GetFileBytes(key) ??
                           _mod.GetFileBytes(key + ".rawimg") ??
                           _mod.GetFileBytes(key + ".png");

            if (bytes == null)
            {
                TModShimEngine.Log($"[ModAssetRepository] 未找到资产文件: {key}");
                return null;
            }

            GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                   Main.instance?.GraphicsDevice ??
                                   Main.graphics?.GraphicsDevice;

            if (device == null)
            {
                TModShimEngine.Log($"[ModAssetRepository] GraphicsDevice 尚未就绪: {key}");
                return null;
            }

            // 1. 检查是否为 rawimg (12 字节头: int32 version, int32 width, int32 height)
            if (bytes.Length >= 12 && !IsPngHeader(bytes))
            {
                try
                {
                    int version = BitConverter.ToInt32(bytes, 0);
                    int width = BitConverter.ToInt32(bytes, 4);
                    int height = BitConverter.ToInt32(bytes, 8);

                    if (version == 1 && width > 0 && height > 0)
                    {
                        int expectedPixelBytes = width * height * 4;
                        if (bytes.Length >= 12 + expectedPixelBytes)
                        {
                            var tex = new Texture2D(device, width, height);
                            byte[] pixelData = new byte[expectedPixelBytes];
                            Buffer.BlockCopy(bytes, 12, pixelData, 0, expectedPixelBytes);
                            tex.SetData(pixelData);
                            TModShimEngine.Log($"[ModAssetRepository] 成功从 rawimg 载入纹理 [{key}] ({width}x{height})");
                            return tex;
                        }
                    }
                }
                catch (Exception ex)
                {
                    TModShimEngine.Log($"[ModAssetRepository] 解析 rawimg [{key}] 异常: {ex.Message}");
                }
            }

            // 2. PNG 格式加载
            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    var tex = Texture2D.FromStream(device, ms);
                    TModShimEngine.Log($"[ModAssetRepository] 成功从 PNG 载入纹理 [{key}] ({tex.Width}x{tex.Height})");
                    return tex;
                }
            }
            catch (Exception ex)
            {
                TModShimEngine.Log($"[ModAssetRepository] 解析 PNG [{key}] 异常: {ex.Message}");
            }

            return null;
        }

        private static Texture2D GetFallbackTexture()
        {
            try
            {
                GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                       Main.instance?.GraphicsDevice ??
                                       Main.graphics?.GraphicsDevice;
                if (device == null) return null;

                if (_fallbackTexture == null || _fallbackTexture.IsDisposed)
                {
                    _fallbackTexture = new Texture2D(device, 32, 32);
                    Color[] pixels = new Color[32 * 32];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(50, 50, 80, 180);
                    _fallbackTexture.SetData(pixels);
                }
                return _fallbackTexture;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsPngHeader(byte[] bytes)
        {
            return bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
        }

        private static Asset<T> CreateAsset<T>(T instance, string name) where T : class
        {
            if (instance == null) return null;
            try
            {
                var asset = new Asset<T>(name);
                asset.Value = instance;
                return asset;
            }
            catch
            {
                try
                {
                    var asset = (Asset<T>)Activator.CreateInstance(typeof(Asset<T>), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { name }, null);
                    var valField = typeof(Asset<T>).GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public) ??
                                   typeof(Asset<T>).GetField("Value", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    valField?.SetValue(asset, instance);
                    return asset;
                }
                catch { }
            }
            return null;
        }
    }
}
