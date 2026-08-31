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
    /// 敌人钱币掉落倍率（对齐 ImproveGame 语义）：NPC 基础身价 value × 倍率，
    /// 掉落时（NPCLoot_DropMoney 内 num3 = value）自动按新值计算。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class Economy
    {
        public static GetSetReset<bool> EnableCoinDropRate = new GetSetReset<bool>(false, false);
        public static GetSetReset<float> CoinDropRate = new GetSetReset<float>(1f, 1f, v => v < 0.01f ? 0.01f : (v > 100f ? 100f : v));

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get1("npcCoinDropRate", EnableCoinDropRate, CoinDropRate)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get1(EnableCoinDropRate, CoinDropRate, float.Parse, "敌人钱币掉落倍率：1=原版，2=双倍钱币<float>", "Images/Item_73", "敌人钱币掉落倍率")
            };
        }
    }

    /// <summary>
    /// 钱币倍率实施：NPC 生成时（SetDefaultsPostfix）改写基础身价 value。
    /// </summary>
    internal class Patch_CoinDropRate : PatchNPC
    {
        public override void SetDefaultsPostfix(NPC This, int Type, NPCSpawnParams spawnparams)
        {
            if (!Economy.EnableCoinDropRate.val) return;
            if (This.value > 0f)
            {
                This.value = This.value * Economy.CoinDropRate.val;
            }
        }
    }
}
