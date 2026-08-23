using CommandHelp;
using HarmonyLib;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 城镇 NPC 机制优化补丁
    /// 包含：NPC 快速/无视邪恶/夜晚自动入住、NPC 快乐度锁定最优(75%买入/133%卖出)、
    /// 城镇 NPC 一键全员回家、旅商不离开与手动刷新商品、快捷护士一键满血。
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal class TownNPCOptimization
    {
        public static GetSetReset<bool> EnableAutoHouse = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableOptimalHappiness = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableTravellingMerchantStay = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableQuickNurse = new GetSetReset<bool>(true, true);

        private static int autoHousingTimer = 0;

        public static List<CommandObject> GetCO()
        {
            CommandMethod cmdHome = new CommandMethod("townNPCHome", 0);
            cmdHome.Runing += _ => TeleportAllTownNPCsHome();

            CommandMethod cmdRefreshShop = new CommandMethod("refreshTravelShop", 0);
            cmdRefreshShop.Runing += _ => RefreshTravellingMerchantShop();

            return new List<CommandObject>
            {
                CommandBuild.get2("npcAutoHouse", EnableAutoHouse),
                CommandBuild.get2("npcOptimalHappiness", EnableOptimalHappiness),
                CommandBuild.get2("travellingMerchantStay", EnableTravellingMerchantStay),
                CommandBuild.get2("quickNurse", EnableQuickNurse),
                cmdHome,
                cmdRefreshShop
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableAutoHouse, "城镇 NPC 满足条件后直接入住，允许在夜晚和邪恶环境中入住房屋", "Images/Item_267", "NPC 自动与无限制入住"),
                UIBuild.get2(EnableOptimalHappiness, "锁定所有城镇 NPC 快乐度为最优（75%最低买入价，133%最高卖出价）", "Images/Item_73", "NPC 快乐度锁定最优"),
                UIBuild.get2(EnableTravellingMerchantStay, "阻止旅商在傍晚离开或消失，常驻留在城镇中", "Images/Item_2274", "旅商常驻不离开"),
                UIBuild.get2(EnableQuickNurse, "右键护士直接扣费回满血并清除负面效果，跳过繁琐对话", "Images/Item_499", "快捷护士一键治疗")
            };
        }

        #region 1. NPC 快速与无视邪恶/夜晚入住 (Transpiler 绕过邪恶计算)

        /// <summary>
        /// 当开启 NPC 无视邪恶入住时，将房屋评分中的邪恶图格扫描结果旁路清零
        /// </summary>
        public static int BypassEvilTileCount(int count)
        {
            if (EnableAutoHouse.val) return 0;
            return count;
        }

        /// <summary>
        /// 使用 Transpiler 在 WorldGen.ScoreRoom 内部将 GetTileTypeCountByCategory 结果重定向至 BypassEvilTileCount
        /// </summary>
        [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.ScoreRoom))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ScoreRoomTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo targetMethod = typeof(WorldGen).GetMethod("GetTileTypeCountByCategory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo bypassMethod = typeof(TownNPCOptimization).GetMethod(nameof(BypassEvilTileCount), BindingFlags.Public | BindingFlags.Static);

            foreach (CodeInstruction instr in instructions)
            {
                yield return instr;
                if (instr.opcode == OpCodes.Call && instr.operand is MethodInfo mi && mi == targetMethod)
                {
                    yield return new CodeInstruction(OpCodes.Call, bypassMethod);
                }
            }
        }

        /// <summary>
        /// 玩家主循环中定期检测无家可归的 NPC 并自动分配空余房屋，加速生成
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        [HarmonyPostfix]
        public static void PlayerUpdatePostfix(Player __instance, int i)
        {
            if (__instance == null || !__instance.active || __instance.whoAmI != Main.myPlayer) return;
            if (!EnableAutoHouse.val) return;

            autoHousingTimer++;
            if (autoHousingTimer % 60 == 0) // 每秒检测一次
            {
                // 1. 加速城镇 NPC 生成进度
                if (Main.checkForSpawns < 7000)
                {
                    Main.checkForSpawns += 25;
                }

                // 2. 为无家可归的存活城镇 NPC 自动寻找房间
                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    NPC npc = Main.npc[n];
                    if (npc != null && npc.active && npc.townNPC && npc.homeless)
                    {
                        WorldGen.QuickFindHome(npc.whoAmI);
                    }
                }

                // 3. 满足前置条件后自动营救待发现 NPC
                if (NPC.downedBoss3) NPC.savedMech = true;
                if (NPC.downedGoblins) NPC.savedGoblin = true;
                if (Main.hardMode)
                {
                    NPC.savedWizard = true;
                    NPC.savedTaxCollector = true; // 税收官仅在肉后营救
                }
                if (NPC.downedBoss2) NPC.savedBartender = true;
                NPC.savedStylist = true;
                NPC.savedGolfer = true;
                NPC.savedAngler = true;
            }
        }

        #endregion

        #region 2. NPC 快乐度锁定最优

        /// <summary>
        /// 锁定普通 NPC 商店折算率为 0.75 (25% 折扣，最优快乐度)
        /// </summary>
        [HarmonyPatch(typeof(ShopHelper), nameof(ShopHelper.GetShoppingSettings))]
        [HarmonyPostfix]
        public static void GetShoppingSettingsPostfix(Player player, NPC npc, ref ShoppingSettings __result)
        {
            if (EnableOptimalHappiness.val)
            {
                __result.PriceAdjustment = 0.75f;
                __result.HappinessReport = "我在这里过得非常舒心！所有商品都享有最大优惠折扣！";
            }
        }

        /// <summary>
        /// 锁定旅商商店价格折算率为 0.75
        /// </summary>
        [HarmonyPatch(typeof(ShopHelper), "GetTravelingMerchantPrices")]
        [HarmonyPostfix]
        public static void GetTravelingMerchantPricesPostfix(ref float __result)
        {
            if (EnableOptimalHappiness.val)
            {
                __result = 0.75f;
            }
        }

        #endregion

        #region 3. 城镇 NPC 一键全员回家

        /// <summary>
        /// 将所有存活且已分配住房的城镇 NPC 瞬移回其房间
        /// </summary>
        public static void TeleportAllTownNPCsHome()
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || !npc.townNPC || npc.homeless) continue;
                if (npc.homeTileX <= 0 || npc.homeTileY <= 0) continue;

                Vector2 targetPos = new Vector2(npc.homeTileX * 16, (npc.homeTileY - 1) * 16 - npc.height + 16);
                npc.position = targetPos;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
                count++;
            }

            if (count > 0)
            {
                SoundEngine.PlaySound(SoundID.Item6);
                Main.NewText($"[c/32FF82:已将 {count} 位城镇 NPC 瞬移送回各自的房间！]");
            }
            else
            {
                Main.NewText("[c/FFA500:当前没有分配了住房且存活的城镇 NPC]");
            }
        }

        /// <summary>
        /// 在鼠标悬浮于房屋管理按钮时，右键瞬移全员回家并消费右键事件
        /// </summary>
        [HarmonyPatch(typeof(Main), "DrawInventory")]
        [HarmonyPostfix]
        public static void DrawInventoryPostfix()
        {
            if (Main.playerInventory && (Main.equipPageMouseOver == 1 || (Main.EquipPage == 1 && Main.mouseX >= Main.screenWidth - 70 && Main.mouseY >= 80 && Main.mouseY <= 140)))
            {
                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    Main.mouseRightRelease = false; // 消费右键，避免与 NPC 旗帜交互冲突
                    TeleportAllTownNPCsHome();
                }
            }
        }

        #endregion

        #region 4. 旅商不离开与手动刷新商品

        /// <summary>
        /// 阻止旅商在傍晚离开或超时消失
        /// </summary>
        [HarmonyPatch(typeof(NPC), nameof(NPC.UpdateNPC))]
        [HarmonyPrefix]
        public static void UpdateNPCPrefix(NPC __instance)
        {
            if (__instance == null || !__instance.active) return;
            if (EnableTravellingMerchantStay.val && __instance.type == NPCID.TravellingMerchant)
            {
                __instance.timeLeft = 7200;
                if (__instance.ai[0] == 1f)
                {
                    __instance.ai[0] = 0f;
                }
            }
        }

        /// <summary>
        /// 手动重新进货并刷新旅商商品列表
        /// </summary>
        public static void RefreshTravellingMerchantShop()
        {
            Chest.SetupTravelShop();
            SoundEngine.PlaySound(SoundID.MenuTick);
            Main.NewText("[c/00FFDD:旅商商品已重新进货刷新！]");
        }

        /// <summary>
        /// 在旅商对话界面中按中键直接刷新旅商商品
        /// </summary>
        [HarmonyPatch(typeof(Main), "GUIChatDrawInner")]
        [HarmonyPostfix]
        public static void GUIChatDrawInnerPostfix()
        {
            if (Main.LocalPlayer.talkNPC >= 0 && Main.npc[Main.LocalPlayer.talkNPC].type == NPCID.TravellingMerchant)
            {
                if (PlayerInput.MouseInfo.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                    PlayerInput.MouseInfoOld.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Released)
                {
                    RefreshTravellingMerchantShop();
                }
            }
        }

        #endregion

        #region 5. 快捷护士

        /// <summary>
        /// 右键护士时直接计算扣费并治愈满血（无双倍扣费，满血正常放行打开对话）
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.SetTalkNPC))]
        [HarmonyPrefix]
        public static bool SetTalkNPCPrefix(Player __instance, int npcIndex)
        {
            if (!EnableQuickNurse.val) return true;
            if (__instance == null || __instance.whoAmI != Main.myPlayer) return true;
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs) return true;

            NPC npc = Main.npc[npcIndex];
            if (npc == null || !npc.active || npc.type != NPCID.Nurse) return true;

            // 计算治疗费用
            int healCost = Main.GetNurseHealCost();

            // 若满血且无异常状态，放行打开正常护士对话界面
            if (__instance.statLife >= __instance.statLifeMax2 && healCost <= 0)
            {
                return true;
            }

            // 检查玩家金钱是否足够（不预扣费，由 NPCChatText_DoNurseHeal 内部扣费）
            long totalMoney = Terraria.Utils.CoinsCount(out _, __instance.inventory);
            if (totalMoney >= healCost)
            {
                Main.NPCChatText_DoNurseHeal(healCost);
                Main.NewText($"[c/32FF82:护士已直接为你治愈伤势并清除负面状态！(花费 {Main.ValueToCoins(healCost)})]");
                return false; // 拦截打开护士对话
            }
            else
            {
                Main.NewText($"[c/FF4500:治疗需要 {Main.ValueToCoins(healCost)}，你身上的钱不够！]");
                return true; // 钱不够时放行常规对话
            }
        }

        #endregion
    }
}
