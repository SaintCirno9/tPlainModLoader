using System.Collections.Generic;
using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using Terraria;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 禁止邪恶生物群落蔓延（对齐 ImproveGame 语义）：
    /// 拦截 WorldGen.Convert 的腐化(1)/神圣(2)/猩红(4) 三种自然蔓延转换，
    /// 保留氯金(8)/微光(9)/净化(11) 等玩家主动转换。
    /// 注：已有 Cheat.Function_stopTileConvert 作弊实现，本项为独立可配置开关。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class NoBiomeSpread
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("noBiomeSpread", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "禁止腐化/猩红/神圣三种邪恶生物群落自然蔓延（不影响净化枪/氯金/微光）", "Images/Item_27", "禁止邪恶群落蔓延")
            };
        }
    }

    /// <summary>
    /// 禁止邪恶生物群落蔓延系统（基于 TPML ModSystem 与 HookGen 强类型门面）
    /// </summary>
    internal class Patch_NoBiomeSpread : TPML.Content.ModSystem
    {
        public override void Load()
        {
            On_WorldGen.Convert_int_int_int_bool_bool += (orig, i2, j2, conversionType, tiles, walls) =>
            {
                if (NoBiomeSpread.Enable.val && (conversionType == 1 || conversionType == 2 || conversionType == 4))
                {
                    return;
                }
                orig(i2, j2, conversionType, tiles, walls);
            };
        }
    }
}
