using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using TPML.Content.Assets;
using TPML.Core.Logging;

namespace TPML.Content.Core
{
    /// <summary>
    /// 模组内容统一纹理加载与生命周期管理器
    /// 统一负责各 Loader (Item/Tile/NPC/Projectile/Buff) 的程序集内嵌资源及外部模组资源检索、解码与 Asset 装配
    /// Author: SaintCirno9
    /// </summary>
    public static class ContentTextureLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("ContentTextureLoader");

        /// <summary>
        /// 统一加载模组内容纹理并注册到对应 Asset 槽位
        /// </summary>
        /// <param name="mod">所属模组实例（可为 null）</param>
        /// <param name="asm">目标程序集</param>
        /// <param name="texPath">指定纹理相对路径</param>
        /// <param name="name">内容短名称</param>
        /// <param name="fullName">内容完整全名</param>
        /// <param name="typeId">分配的类型 ID</param>
        /// <param name="targetSetter">贴图装配回调</param>
        /// <param name="fallbackSupplier">兜底纹理提供函数</param>
        /// <param name="assetNameOverride">覆盖 Asset 标识名称</param>
        /// <param name="extraAliases">额外的资源别名检索项</param>
        public static void Load(
            Mod mod,
            Assembly asm,
            string texPath,
            string name,
            string fullName,
            int typeId,
            Action<Asset<Texture2D>> targetSetter,
            Func<Texture2D> fallbackSupplier,
            string assetNameOverride = null,
            string[] extraAliases = null)
        {
            try
            {
                GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                       Main.instance?.GraphicsDevice ??
                                       Main.graphics?.GraphicsDevice;

                if (device == null)
                {
                    return;
                }

                Texture2D texture = null;

                // 1. 程序集 ManifestResourceNames 扫描
                if (asm != null)
                {
                    string[] resNames = asm.GetManifestResourceNames();
                    string targetRes = null;
                    string normalizedTex = texPath?.Replace('/', '.')?.Replace('\\', '.');

                    foreach (var res in resNames)
                    {
                        // 1.1 匹配 normalizedTex 相对路径
                        if (!string.IsNullOrEmpty(normalizedTex) &&
                            (res.Equals(normalizedTex, StringComparison.OrdinalIgnoreCase) ||
                             res.Equals(normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) ||
                             res.EndsWith("." + normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) ||
                             res.EndsWith("." + normalizedTex, StringComparison.OrdinalIgnoreCase)))
                        {
                            targetRes = res;
                            break;
                        }

                        // 1.2 匹配 extraAliases 额外别名
                        if (extraAliases != null && extraAliases.Length > 0)
                        {
                            bool matched = false;
                            foreach (var alias in extraAliases)
                            {
                                if (string.IsNullOrEmpty(alias)) continue;
                                if (res.Equals(alias, StringComparison.OrdinalIgnoreCase) ||
                                    res.EndsWith("." + alias, StringComparison.OrdinalIgnoreCase) ||
                                    res.Equals($"{alias}.png", StringComparison.OrdinalIgnoreCase) ||
                                    res.EndsWith($".{alias}.png", StringComparison.OrdinalIgnoreCase) ||
                                    res.Equals($"{alias}.rawimg", StringComparison.OrdinalIgnoreCase) ||
                                    res.EndsWith($".{alias}.rawimg", StringComparison.OrdinalIgnoreCase) ||
                                    res.EndsWith(alias, StringComparison.OrdinalIgnoreCase))
                                {
                                    targetRes = res;
                                    matched = true;
                                    break;
                                }
                            }
                            if (matched) break;
                        }

                        // 1.3 匹配短名 name
                        if (!string.IsNullOrEmpty(name) &&
                            (res.Equals($"{name}.png", StringComparison.OrdinalIgnoreCase) ||
                             res.EndsWith($".{name}.png", StringComparison.OrdinalIgnoreCase) ||
                             res.Equals($"{name}.rawimg", StringComparison.OrdinalIgnoreCase) ||
                             res.EndsWith($".{name}.rawimg", StringComparison.OrdinalIgnoreCase)))
                        {
                            targetRes = res;
                            break;
                        }
                    }

                    if (targetRes != null)
                    {
                        using (Stream stream = asm.GetManifestResourceStream(targetRes))
                        {
                            if (stream != null)
                            {
                                if (targetRes.EndsWith(".rawimg", StringComparison.OrdinalIgnoreCase))
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        stream.CopyTo(ms);
                                        byte[] bytes = ms.ToArray();
                                        if (bytes.Length >= 12)
                                        {
                                            int version = BitConverter.ToInt32(bytes, 0);
                                            int width = BitConverter.ToInt32(bytes, 4);
                                            int height = BitConverter.ToInt32(bytes, 8);
                                            int expectedPixelBytes = width * height * 4;
                                            if (version == 1 && width > 0 && height > 0 && bytes.Length >= 12 + expectedPixelBytes)
                                            {
                                                texture = new Texture2D(device, width, height);
                                                byte[] pixelData = new byte[expectedPixelBytes];
                                                Buffer.BlockCopy(bytes, 12, pixelData, 0, expectedPixelBytes);
                                                texture.SetData(pixelData);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    texture = Texture2D.FromStream(device, stream);
                                }
                            }
                        }
                    }
                }

                // 2. 外部 Mod 文件回退 (HasAsset)
                if (texture == null && mod != null && !string.IsNullOrEmpty(texPath))
                {
                    string cleanPath = texPath.Replace('\\', '/');
                    if (mod.HasAsset(cleanPath + ".png"))
                    {
                        using (Stream s = mod.GetFileStream(cleanPath + ".png"))
                        {
                            if (s != null) texture = Texture2D.FromStream(device, s);
                        }
                    }
                    else if (mod.HasAsset(cleanPath + ".rawimg"))
                    {
                        byte[] rawBytes = mod.GetFileBytes(cleanPath + ".rawimg");
                        if (rawBytes != null && rawBytes.Length >= 12)
                        {
                            int width = BitConverter.ToInt32(rawBytes, 4);
                            int height = BitConverter.ToInt32(rawBytes, 8);
                            int expectedPixelBytes = width * height * 4;
                            if (width > 0 && height > 0 && rawBytes.Length >= 12 + expectedPixelBytes)
                            {
                                texture = new Texture2D(device, width, height);
                                byte[] pixelData = new byte[expectedPixelBytes];
                                Buffer.BlockCopy(rawBytes, 12, pixelData, 0, expectedPixelBytes);
                                texture.SetData(pixelData);
                            }
                        }
                    }
                }

                // 3. Fallback 兜底
                if (texture == null && fallbackSupplier != null)
                {
                    texture = fallbackSupplier();
                }

                // 4. 极端兜底 (防止 fallbackSupplier 也返回 null)
                if (texture == null)
                {
                    texture = new Texture2D(device, 16, 16);
                }

                // 5. 构造 Asset 并设置
                if (targetSetter != null)
                {
                    string assetName = !string.IsNullOrEmpty(assetNameOverride) ? assetNameOverride : fullName;
                    targetSetter(AssetFactory.CreateLoaded(texture, assetName));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"为 [{fullName}] 加载贴图异常: {ex.Message}");
                ModLoader.Log($"[ContentTextureLoader] 为 [{fullName}] 加载贴图异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 统一清理模组注册分配的 Texture2D 资产并重置槽位（遵循 INV-05，绝不释放原版 Fallback 纹理）
        /// </summary>
        /// <param name="assetArray">目标资产数组</param>
        /// <param name="modOffset">模组 ID 起始偏移</param>
        /// <param name="nextId">当前分配的最新 ID 上界</param>
        /// <param name="fallbackTexture">原版兜底共享纹理（绝不释放）</param>
        public static void ClearAssets(Asset<Texture2D>[] assetArray, int modOffset, int nextId, Texture2D fallbackTexture = null)
        {
            if (assetArray == null) return;
            int start = Math.Max(0, modOffset);
            int end = Math.Min(nextId, assetArray.Length);

            for (int i = start; i < end; i++)
            {
                var asset = assetArray[i];
                if (asset?.Value != null && asset.Value != fallbackTexture && !asset.Value.IsDisposed)
                {
                    try
                    {
                        asset.Value.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"释放纹理槽位 [{i}] 异常: {ex.Message}");
                    }
                }
                assetArray[i] = null;
            }
        }
    }
}
