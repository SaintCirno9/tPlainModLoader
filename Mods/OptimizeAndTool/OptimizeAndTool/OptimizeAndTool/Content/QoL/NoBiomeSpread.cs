using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.UI;

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
    /// PatchWorldGen.CanConvert 即原版 WorldGen.Convert 的前置判定（conversionType 对应 BiomeConversionID）。
    /// </summary>
    internal class Patch_NoBiomeSpread : PatchWorldGen
    {
        public override bool CanConvert(int i2, int j2, int conversionType, bool tiles, bool walls)
        {
            if (!NoBiomeSpread.Enable.val) return true;
            // 1=腐化 2=神圣 4=猩红：自然蔓延转换，禁止
            if (conversionType == 1 || conversionType == 2 || conversionType == 4) return false;
            return true;
        }
    }
}
