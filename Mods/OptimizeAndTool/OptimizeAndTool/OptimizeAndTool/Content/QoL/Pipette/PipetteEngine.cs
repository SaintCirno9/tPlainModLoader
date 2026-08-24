using CommandHelp;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using OptimizeAndTool.Content.UI;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;

namespace OptimizeAndTool.Content.QoL.Pipette
{
    /// <summary>
    /// 吸管工具调度引擎（负责吸管目标提取与物品栏选中状态调度）
    /// 作者: SaintCirno9
    /// </summary>
    public static class PipetteEngine
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> PickWall = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> PlaySound = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> ShowNotification = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("pipetteTool", Enable),
                CommandBuild.get2("pipettePickWall", PickWall),
                CommandBuild.get2("pipettePlaySound", PlaySound),
                CommandBuild.get2("pipetteShowNotification", ShowNotification)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "按下快捷键（默认 Q）直接从背包/虚空袋中拿出手持鼠标指向的物块/背景墙", "Images/Item_509", "吸管工具 (Pick Block)"),
                UIBuild.get2(PickWall, "指向背景墙时若未指向物块则自动吸取背景墙放置物", "Images/Item_130", "支持吸取背景墙"),
                UIBuild.get2(PlaySound, "吸取成功或缺少物块时播放提示音效", "Images/Item_1300", "播放吸取音效"),
                UIBuild.get2(ShowNotification, "在玩家头顶显示吸取到的物品名称与数量", "Images/Item_1344", "显示浮字通知")
            };
        }

        // 记录吸管操作前玩家原本的手持槽位（0~9）
        private static int lastNormalSlot = 0;

        // 当前吸管所选中的槽位（0~49），-1 表示未处于吸管状态
        private static int currentPipetteSlot = -1;

        // 当前吸管选中的物品 Type，-1 表示未处于吸管状态
        private static int currentPipetteItemType = -1;

        // 标记当前吸管选中的物块是否来自大背包
        private static bool isFromBigBag = false;

        // 记录从大背包调取物块时的源槽位索引
        private static int bigBagSourceSlot = -1;

        /// <summary>
        /// 执行吸管动作（Pick Block）
        /// </summary>
        public static void PerformPipette()
        {
            // 1. 基础状态校验
            if (Main.gameMenu || Main.myPlayer < 0 || Main.myPlayer >= Main.player.Length) return;
            Player player = Main.player[Main.myPlayer];
            if (player == null || !player.active || player.dead) return;
            if (!PipetteEngine.Enable.val) return;

            // 2. 获取鼠标当前指向的图格坐标
            int tileX = Player.tileTargetX;
            int tileY = Player.tileTargetY;

            // 边界保底
            if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
            {
                tileX = (int)(Main.MouseWorld.X / 16f);
                tileY = (int)(Main.MouseWorld.Y / 16f);
            }

            int targetItemId = -1;
            if (tileX >= 0 && tileX < Main.maxTilesX && tileY >= 0 && tileY < Main.maxTilesY)
            {
                Tile tile = Main.tile[tileX, tileY];
                if (tile != null)
                {
                    targetItemId = TileToItemResolver.ResolveTileOrWallToItemId(tile, player, PipetteEngine.PickWall.val);
                }
            }

            int curSelected = player.selectedItem;
            int curHeldType = player.HeldItem != null ? player.HeldItem.type : -1;

            // 3. 检查当前是否处于由吸管选出的状态（当前选中的槽位就是吸管选出的槽位）
            bool isCurrentlyPipetted = (currentPipetteSlot != -1 && curSelected == currentPipetteSlot);

            // 4. 指向空气 / 无效区域 -> 恢复原手持
            if (targetItemId <= 0)
            {
                if (isCurrentlyPipetted || curSelected >= 10)
                {
                    RestoreOriginalHotbar(player);
                }
                else if (PipetteEngine.ShowNotification.val)
                {
                    CombatText.NewText(player.getRect(), Color.Silver, "未指向有效物块");
                }
                return;
            }

            string itemName = Lang.GetItemNameValue(targetItemId);

            // 5. 检查是否指向相同物块（当前手持与目标物块一致） -> 再次按 Q 恢复原快捷栏手持
            if (curHeldType == targetItemId)
            {
                RestoreOriginalHotbar(player);
                return;
            }

            // 6. 准备吸取新物块：若当前未处于吸管状态，记录当前手持为原手持槽位
            if (!isCurrentlyPipetted)
            {
                if (curSelected >= 0 && curSelected < 10)
                {
                    lastNormalSlot = curSelected;
                }
                else if (player.selectedItemState.Hotbar >= 0 && player.selectedItemState.Hotbar < 10)
                {
                    lastNormalSlot = player.selectedItemState.Hotbar;
                }
            }

            // 7. 优先在玩家个人背包（0 ~ 49）中检索目标物品并选中拿出
            for (int i = 0; i < 50; i++)
            {
                Item invItem = player.inventory[i];
                if (invItem != null && invItem.type == targetItemId && invItem.stack > 0)
                {
                    // 若之前是从大背包调取的物块，先将大背包物块归还
                    if (isFromBigBag)
                    {
                        ReturnBigBagItemToStorage(player);
                    }

                    player.selectedItemState.Select(i);
                    currentPipetteSlot = i;
                    currentPipetteItemType = targetItemId;
                    isFromBigBag = false;
                    bigBagSourceSlot = -1;

                    if (PipetteEngine.PlaySound.val) SoundEngine.PlaySound(SoundID.MenuTick);
                    if (PipetteEngine.ShowNotification.val)
                    {
                        CombatText.NewText(player.getRect(), Color.LightGreen, $"拿出: {itemName} (x{invItem.stack})");
                    }
                    return;
                }
            }

            // 8. 个人背包未命中 -> 深入大背包检索并智能调取手持
            if (BigBag.BigBag.EnableBigBag.val && BigBag.BigBag.Slots != null)
            {
                int bIdx = BigBag.BigBag.FindItem(targetItemId);
                if (bIdx != -1)
                {
                    // 若之前已持有另一个大背包物块，先归还
                    if (isFromBigBag)
                    {
                        ReturnBigBagItemToStorage(player);
                    }
                    // 寻找主物品栏（10~49）中的承载槽位：优先找靠后的空格（从 49 往前找），若全满则使用第 49 格置换
                    int carrierSlot = 49;
                    for (int s = 49; s >= 10; s--)
                    {
                        if (player.inventory[s] == null || player.inventory[s].IsAir)
                        {
                            carrierSlot = s;
                            break;
                        }
                    }

                    Item bigBagItem = BigBag.BigBag.Slots[bIdx];
                    Item originalCarrierItem = player.inventory[carrierSlot] ?? new Item();

                    // 将大背包物块放入主物品栏承载槽位，原槽位物品（若有）暂存入大背包该格
                    player.inventory[carrierSlot] = bigBagItem;
                    BigBag.BigBag.Slots[bIdx] = originalCarrierItem;

                    BigBag.BigBagStorage.SaveNow();
                    BigBag.BigBag.NotifySlotsChanged();

                    // 选中主物品栏承载槽位（原版支持直接手持 10~49 格放置物品，快捷栏 0~9 布局 100% 保持不变！）
                    player.selectedItemState.Select(carrierSlot);
                    currentPipetteSlot = carrierSlot;
                    currentPipetteItemType = targetItemId;
                    isFromBigBag = true;
                    bigBagSourceSlot = bIdx;

                    if (PipetteEngine.PlaySound.val) SoundEngine.PlaySound(SoundID.MenuTick);
                    if (PipetteEngine.ShowNotification.val)
                    {
                        CombatText.NewText(player.getRect(), Color.LightGreen, $"[大背包] 调取: {itemName} (x{player.inventory[carrierSlot].stack})");
                    }
                    return;
                }
            }
            // 9. 背包与大背包均未找到对应物块 -> 弹出未持有警示提示
            if (PipetteEngine.PlaySound.val) SoundEngine.PlaySound(SoundID.MenuClose);
            if (PipetteEngine.ShowNotification.val)
            {
                CombatText.NewText(player.getRect(), Color.OrangeRed, $"背包与大背包中均未找到: {itemName}");
            }
        }

        /// <summary>
        /// 恢复到吸管前记录的原始快捷栏槽位（若来自大背包则将物块安全归还大背包）
        /// </summary>
        private static void RestoreOriginalHotbar(Player player)
        {
            if (isFromBigBag)
            {
                ReturnBigBagItemToStorage(player);
            }

            int targetSlot = (lastNormalSlot >= 0 && lastNormalSlot < 10) ? lastNormalSlot : 0;
            currentPipetteSlot = -1;
            currentPipetteItemType = -1;
            isFromBigBag = false;
            bigBagSourceSlot = -1;

            player.selectedItemState.Select(targetSlot);
            if (PipetteEngine.PlaySound.val) SoundEngine.PlaySound(SoundID.MenuTick);

            if (PipetteEngine.ShowNotification.val)
            {
                CombatText.NewText(player.getRect(), Color.LightBlue, "恢复原快捷栏手持");
            }
        }

        /// <summary>
        /// 将由吸管从大背包调取的物块安全送回大背包并恢复主物品栏原本的格子内容
        /// </summary>
        private static void ReturnBigBagItemToStorage(Player player)
        {
            if (!isFromBigBag || currentPipetteSlot < 10 || currentPipetteSlot > 49 || bigBagSourceSlot < 0) return;

            try
            {
                Item heldItem = player.inventory[currentPipetteSlot] ?? new Item();
                Item originalItem = (bigBagSourceSlot < BigBag.BigBag.Slots.Length) ? BigBag.BigBag.Slots[bigBagSourceSlot] : new Item();

                // 还原主背包承载槽位置换前原本存放的物品
                player.inventory[currentPipetteSlot] = originalItem;

                // 将剩余的手持物块放回大背包原槽位或存入空格/合并
                if (heldItem != null && !heldItem.IsAir && heldItem.stack > 0)
                {
                    BigBag.BigBag.Slots[bigBagSourceSlot] = new Item();
                    BigBag.BigBag.DepositItem(heldItem, bigBagSourceSlot);
                }
                else
                {
                    BigBag.BigBag.Slots[bigBagSourceSlot] = new Item();
                }

                BigBag.BigBagStorage.SaveNow();
                BigBag.BigBag.NotifySlotsChanged();
            }
            catch
            {
            }
            finally
            {
                isFromBigBag = false;
                bigBagSourceSlot = -1;
            }
        }
    }
}
