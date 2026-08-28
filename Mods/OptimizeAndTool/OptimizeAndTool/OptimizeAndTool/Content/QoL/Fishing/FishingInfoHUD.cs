using CommandHelp;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using ReLogic.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace OptimizeAndTool.Content.QoL.Fishing
{
    /// <summary>
    /// 钓鱼环境与任务鱼信息 HUD
    /// 在玩家手持钓竿时于屏幕左侧实时显示水体大小、液体类别、打窝数、今日任务鱼、宝匣设置与挂机战利品收益统计
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal class FishingInfoHUD
    {
        public static GetSetReset<bool> EnableFishingInfoHUD = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("fishingInfoHUD", EnableFishingInfoHUD)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableFishingInfoHUD, "手持钓竿时在屏幕左侧实时显示水体大小、液体种类、打窝数、今日任务鱼与挂机收益", "Images/Item_3095", "钓鱼环境 HUD")
            };
        }

        [HarmonyPatch(typeof(Main), nameof(Main.DrawInterface_36_Cursor))]
        [HarmonyPostfix]
        public static void DrawInterface_CursorPostfix()
        {
            if (!EnableFishingInfoHUD.val || Main.gameMenu || Main.playerInventory)
                return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead)
                return;

            Item held = player.inventory[player.selectedItem];
            if (held == null || held.fishingPole <= 0)
                return;

            Projectile bobber = null;
            int activeBobberCount = 0;
            // 第一遍：优先选取已入水的浮标（此时才能读取水体信息）
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.bobber)
                {
                    activeBobberCount++;
                    if (bobber == null && p.wet)
                    {
                        bobber = p;
                    }
                }
            }
            // 第二遍：无入水浮标时退而取第一个活跃浮标
            if (bobber == null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.owner == player.whoAmI && p.bobber)
                    {
                        bobber = p;
                        break;
                    }
                }
            }

            int fishingPower = player.GetFishingConditions().FinalFishingLevel;
            string questFishName = "无任务";
            bool questFinished = false;
            if (Main.anglerQuest >= 0 && Main.anglerQuest < Main.anglerQuestItemNetIDs.Length)
            {
                int questItemID = Main.anglerQuestItemNetIDs[Main.anglerQuest];
                questFishName = Lang.GetItemNameValue(questItemID);
                questFinished = Main.anglerQuestFinished;
            }

            string waterInfo = "无浮标";
            string liquidInfo = "";
            string chumInfo = "";
            if (bobber != null)
            {
                int x = (int)(bobber.Center.X / 16f);
                int y = (int)(bobber.Center.Y / 16f);
                Projectile.GetFishingPondState(x, y, out bool lava, out bool honey, out int numWaters, out int chumCount);
                liquidInfo = lava ? "[c/FF7733:岩浆]" : honey ? "[c/FFD666:蜂蜜]" : "[c/66CCFF:水]";
                waterInfo = $"{liquidInfo} {numWaters} 格";
                chumInfo = $"打窝 {chumCount}/3";
            }

            string crateInfo = FishingCrateModifier.EnableGuaranteedCrate.val ? "[c/00FF00:100% 必出]" :
                               (FishingCrateModifier.EnableCrateMultiplier.val ? $"[c/77FFAA:{FishingCrateModifier.CrateChanceMultiplier.val}x 倍率]" : "[c/AAAAAA:原版]");

            string autoFishMode = AutoFishingSystem.EnableAutoFish.val ?
                (FishingCatchProcessor.EnableAutoSellAllCatches.val ? "[c/FFFF55:连钓+自动变现]" : "[c/00FF00:开启 (挂机连钓)]") :
                "[c/FF7777:手动收竿]";

            int totalBait = AutoFishingSupplies.CountAllBait(player);
            string baitInfo = AutoFishingSupplies.EnableInfiniteBait.val ? "[c/00FFDD:无限鱼饵]" : $"[c/77FFAA:{totalBait} 份]";

            List<KeyValuePair<string, Color>> lines = new List<KeyValuePair<string, Color>>
            {
                new KeyValuePair<string, Color>("[c/00FFDD:【钓鱼环境 HUD】]", Color.White),
                new KeyValuePair<string, Color>($"[c/FFFFFF:总渔力:] [c/77FFAA:{fishingPower}%] | [c/FFFFFF:鱼饵:] {baitInfo}", Color.White),
                new KeyValuePair<string, Color>($"[c/FFFFFF:水体:] {waterInfo} | [c/FFFFFF:打窝:] [c/FFFF99:{chumInfo}]", Color.White),
                new KeyValuePair<string, Color>($"[c/FFFFFF:浮标线数:] [c/66DDFF:{activeBobberCount} 条] | [c/FFFFFF:宝匣:] {crateInfo}", Color.White),
                new KeyValuePair<string, Color>($"[c/FFFFFF:今日任务鱼:] [c/FFFF77:{questFishName}] ({(questFinished ? "[c/00FF88:已交付]" : "[c/FFAA77:未交付]")})", Color.White),
                new KeyValuePair<string, Color>($"[c/FFFFFF:挂机模式:] {autoFishMode}", Color.White)
            };

            if (FishingCatchProcessor.TotalCatchesCount > 0)
            {
                string coinStr = FormatCoins(FishingCatchProcessor.TotalCoinsEarned);
                lines.Add(new KeyValuePair<string, Color>($"[c/FFFFFF:本次垂钓:] [c/77FFAA:{FishingCatchProcessor.TotalCatchesCount} 次] | [c/FFFFFF:变现收益:] {coinStr}", Color.White));
            }

            SpriteBatch sb = Main.spriteBatch;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 scale = new Vector2(0.85f);
            float xPos = 20f;
            float yPos = 110f;
            float lineSpacing = font.LineSpacing * scale.Y;
            float maxWidth = 0f;
            foreach (KeyValuePair<string, Color> line in lines)
            {
                Vector2 size = ChatManager.GetStringSize(font, line.Key, scale);
                if (size.X > maxWidth)
                    maxWidth = size.X;
            }

            float width = maxWidth + 16f;
            float height = lines.Count * lineSpacing + 12f;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)xPos - 8, (int)yPos - 6, (int)width, (int)height), new Color(15, 20, 30, 200));

            foreach (KeyValuePair<string, Color> line in lines)
            {
                ChatManager.DrawColorCodedStringWithShadow(sb, font, line.Key, new Vector2(xPos, yPos), line.Value, 0f, Vector2.Zero, scale);
                yPos += lineSpacing;
            }
        }

        private static string FormatCoins(long value)
        {
            if (value <= 0)
                return "[c/AAAAAA:0]";

            long plat = value / 1000000;
            long gold = (value % 1000000) / 10000;
            long silver = (value % 10000) / 100;
            long copper = value % 100;

            string res = "";
            if (plat > 0) res += $"[c/E5E8EA:{plat}白金] ";
            if (gold > 0) res += $"[c/E5C158:{gold}金] ";
            if (silver > 0) res += $"[c/A2B4BA:{silver}银] ";
            if (copper > 0 || res == "") res += $"[c/C7794D:{copper}铜]";
            return res.Trim();
        }
    }
}