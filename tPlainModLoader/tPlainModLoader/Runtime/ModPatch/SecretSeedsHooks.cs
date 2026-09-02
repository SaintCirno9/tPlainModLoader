using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using TPML.Core.Logging;

namespace TPML.ModPatch
{
    /// <summary>
    /// 秘密彩蛋种子（Secret Seeds）全量自动解锁与 HookGen 门面钩子：
    /// 1. 自动注入 35 个已知明文短语作为按钮显示文本；
    /// 2. 遍历 AllSecretSeeds 实例池，确保 37 个底层彩蛋（含雷暴/无雷暴等）100% 真正全解锁；
    /// 3. Hook PrepareInterface 门控，让创建世界高级界面的专属彩蛋子菜单常驻显示并可用。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class SecretSeedsHooks
    {
        private static readonly ILogger Logger = LogManager.GetLogger("SecretSeeds");
        private static bool _registered = false;

        // 已知的 35 个标准明文短语
        private static readonly string[] SecretSeedPhrases = new string[]
        {
            "how did i get here",                      // 出生点随机
            "royale with cheese",                     // 出生点由队伍决定
            "mole people",                            // 出生地底，地表被填满
            "night of the living dead",               // 开局血月与墓地
            "too easy",                               // 开局困难模式
            "what a horrible night to have a curse",  // 吸血鬼地底出生
            "bring a towel",                          // 无休止下雨
            "hocus pocus",                            // 无尽万圣节
            "jingle all the way",                     // 无尽圣诞节
            "pumpkin season",                         // 草地长南瓜
            "arachnophobia",                          // 无蜘蛛洞
            "more traps please",                      // 无陷阱
            "beam me up",                             // 随机传送机
            "we don't even test for that",            // 宝箱概率出传送枪
            "abandoned manors",                       // 超大地下小屋
            "save the rainforest",                    // 生命树暴增
            "the care bears movie",                   // 空岛数量暴增
            "double daring dangers & dual dungeons",  // 双地牢与地下城
            "such great heights",                     // 世界极高地表近太空
            "sandy britches",                         // 地表沙漠化
            "toadstool",                              // 地表发光蘑菇化
            "does that sparkle",                      // 地表神圣化
            "fish mox",                               // 世界净化
            "purify this",                            // 世界污染
            "winter is coming",                       // 世界冻结
            "truck stop",                             // 世界到处是便便
            "rainbow road",                           // 彩虹物品泛滥
            "jagged rocks",                           // 世界布满深坑
            "waterpark",                              // 水上乐园
            "planetoids",                             // 行星岛屿
            "i am error",                             // 混乱错误世界
            "monochrome",                             // 全图灰色漆
            "negative infinity",                      // 全图反色漆
            "xray vision",                            // 全图夜明漆
            "invisible plane"                         // 全图回声漆
        };

        /// <summary>注册彩蛋种子全量解锁 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_registered) return;

            try
            {
                On_SecretSeedsTracker.PrepareInterface += Hook_PrepareInterface;
                _registered = true;
                Logger.Info("秘密彩蛋种子全量解锁钩子已挂载");
            }
            catch (Exception ex)
            {
                Logger.Error($"挂载秘密彩蛋种子钩子失败: {ex.Message}", ex);
            }
        }

        private static void Hook_PrepareInterface(On_SecretSeedsTracker.orig_PrepareInterface orig)
        {
            UnlockAll();
            orig();
        }

        /// <summary>
        /// 批量全量解锁全部 37 个彩蛋种子
        /// </summary>
        public static void UnlockAll()
        {
            try
            {
                bool changed = false;

                // 1. 先通过已知明文短语匹配，注入友好英文短语作为按钮显示文本
                foreach (string phrase in SecretSeedPhrases)
                {
                    if (WorldGen.SecretSeed.CheckInputForSecretSeed(phrase, out var seed))
                    {
                        if (!SecretSeedsTracker.SeedsForInterface.Contains(seed))
                        {
                            SecretSeedsTracker.SeedsForInterface.Add(seed);
                            changed = true;
                        }
                    }
                }

                // 2. 遍历底层的全部 37 个彩蛋实例，确保未公开暗号的彩蛋（如雷暴/无雷暴等）也能 100% 解锁
                if (WorldGen.SecretSeed.AllSecretSeeds != null)
                {
                    foreach (WorldGen.SecretSeed seed in WorldGen.SecretSeed.AllSecretSeeds)
                    {
                        if (seed == null) continue;

                        if (string.IsNullOrEmpty(seed.TextThatWasUsedToUnlock))
                        {
                            string locName = !string.IsNullOrEmpty(seed.Localization) ? Language.GetTextValue(seed.Localization) : null;
                            seed.TextThatWasUsedToUnlock = !string.IsNullOrEmpty(locName) ? locName : "SecretSeed";
                        }

                        if (!SecretSeedsTracker.SeedsForInterface.Contains(seed))
                        {
                            SecretSeedsTracker.SeedsForInterface.Add(seed);
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    Logger.Info($"已自动全量解锁 {SecretSeedsTracker.SeedsForInterface.Count} 个彩蛋种子");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"执行彩蛋种子全量解锁异常: {ex.Message}", ex);
            }
        }
    }
}
