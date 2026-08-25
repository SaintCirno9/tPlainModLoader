using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using TPML.Content;
using TPML.Content.UI;
using Terraria.DataStructures;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// 玩家实体、背包操作、物品交互、快捷栏与输入模拟工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class PlayerInventoryTools
    {
        public static bool IsManualUsingItem = false;

        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/get_inventory",
                    Description = "获取当前玩家背包物品列表（58格物品槽位包含ID、名称、数量、前缀与收藏状态）。",
                    Tags = new List<string> { "read-only" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/toggle_inventory",
                    Description = "打开或关闭玩家物品栏背包界面。",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            open = new { type = "boolean", description = "true 为打开，false 为关闭；不传则切换开/关状态" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/give_item",
                    Description = "向当前玩家背包直接发放指定物品（自动寻找空位或合并堆叠）。",
                    Tags = new List<string> { "write", "inventory" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "itemId" },
                        properties = new
                        {
                            itemId = new { type = "integer", description = "物品 ID (原版 ID 或模组自定义 ID，如 6200~6202)" },
                            stack = new { type = "integer", description = "发放数量（默认 1）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/spawn_item_in_world",
                    Description = "在世界中玩家当前脚下生成一个掉落物实体 (Item Entity)。",
                    Tags = new List<string> { "write", "world" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "itemId" },
                        properties = new
                        {
                            itemId = new { type = "integer", description = "物品 ID" },
                            stack = new { type = "integer", description = "掉落数量（默认 1）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/select_inventory_slot",
                    Description = "选择玩家当前手持的背包槽位 (0~49，其中 0~9 为快捷栏)。",
                    Tags = new List<string> { "write", "inventory" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "slot" },
                        properties = new
                        {
                            slot = new { type = "integer", description = "背包槽位索引 (0~49)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/get_item_info",
                    Description = "获取指定背包槽位中物品的完整属性、材质贴图信息与游戏内渲染的全部 Tooltip 行。",
                    Tags = new List<string> { "read-only", "inventory", "diagnostic" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "slot" },
                        properties = new
                        {
                            slot = new { type = "integer", description = "背包槽位索引 (0~58)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/use_held_item",
                    Description = "实机触发玩家使用当前手持物品（支持设置目标图格、朝向与长按帧数）。",
                    Tags = new List<string> { "write", "action" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tileX = new { type = "integer", description = "目标图格 X 坐标（可选，默认玩家当前所在图格）" },
                            tileY = new { type = "integer", description = "目标图格 Y 坐标（可选，默认玩家脚下）" },
                            worldX = new { type = "number", description = "目标世界像素坐标 X（可选）" },
                            worldY = new { type = "number", description = "目标世界像素坐标 Y（可选）" },
                            direction = new { type = "integer", description = "玩家面朝方向（1 为右，-1 为左，可选）" },
                            altUse = new { type = "boolean", description = "是否为右键/次要功能使用（默认 false）" },
                            holdFrames = new { type = "integer", description = "对于通道/蓄力/持续施法武器，保持按住的物理帧数（默认 0 为单击）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/swap_inventory_slots",
                    Description = "精确交换玩家背包中任意两个槽位的物品（无破坏性、不复制也不删除数据）。",
                    Tags = new List<string> { "write", "inventory" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "fromSlot", "toSlot" },
                        properties = new
                        {
                            fromSlot = new { type = "integer", description = "源槽位索引 (0~57)" },
                            toSlot = new { type = "integer", description = "目标槽位索引 (0~57)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/teleport",
                    Description = "瞬间将玩家传送至指定世界图格坐标。",
                    Tags = new List<string> { "write", "movement" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "tileX", "tileY" },
                        properties = new
                        {
                            tileX = new { type = "integer", description = "目标世界图格 X 坐标" },
                            tileY = new { type = "integer", description = "目标世界图格 Y 坐标" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/heal_player",
                    Description = "完全恢复当前玩家的生命值与法力值至上限。",
                    Tags = new List<string> { "write", "health" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/simulate_scroll_wheel",
                    Description = "模拟鼠标滚轮滚动（设置 ScrollWheelDelta / ScrollWheelDeltaForUI），测试快捷栏切换与 UI 滚动条。",
                    Tags = new List<string> { "write", "input" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "delta" },
                        properties = new
                        {
                            delta = new { type = "integer", description = "滚轮滚动量（如 120 为向上滚 1 格，-120 为向下滚 1 格）" },
                            forUI = new { type = "boolean", description = "是否同时提供给 UI 状态机（默认 true）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/get_scroll_state",
                    Description = "获取当前鼠标滚轮、XNA 状态与快捷栏状态的实时诊断数据。",
                    Tags = new List<string> { "read", "input" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/get_inventory":
                case "tpml_get_inventory":
                    return await MainThreadQueue.EnqueueAsync(() => GetInventory());

                case "tpml/toggle_inventory":
                case "tpml_toggle_inventory":
                    {
                        bool? open = args?["open"]?.Value<bool>();
                        return await MainThreadQueue.EnqueueAsync(() => ToggleInventory(open));
                    }

                case "tpml/give_item":
                case "tpml_give_item":
                    {
                        int itemId = args?["itemId"]?.Value<int>() ?? 0;
                        int stack = Math.Max(1, args?["stack"]?.Value<int>() ?? 1);
                        return await MainThreadQueue.EnqueueAsync(() => GiveItem(itemId, stack));
                    }

                case "tpml/spawn_item_in_world":
                case "tpml_spawn_item_in_world":
                    {
                        int itemId = args?["itemId"]?.Value<int>() ?? 0;
                        int stack = Math.Max(1, args?["stack"]?.Value<int>() ?? 1);
                        float offsetX = args?["offsetX"]?.Value<float>() ?? 200f;
                        float offsetY = args?["offsetY"]?.Value<float>() ?? 0f;
                        return await MainThreadQueue.EnqueueAsync(() => SpawnItemInWorld(itemId, stack, offsetX, offsetY));
                    }

                case "tpml/select_inventory_slot":
                case "tpml_select_inventory_slot":
                    {
                        int slot = args?["slot"]?.Value<int>() ?? -1;
                        return await MainThreadQueue.EnqueueAsync(() => SelectInventorySlot(slot));
                    }

                case "tpml/get_item_info":
                case "tpml_get_item_info":
                case "tpml/get_slot_info":
                case "tpml_get_slot_info":
                case "tpml/get_item_detail":
                case "tpml_get_item_detail":
                    {
                        int slot = args?["slot"]?.Value<int>() ?? -1;
                        return await MainThreadQueue.EnqueueAsync(() => GetInventorySlotInfo(slot));
                    }

                case "tpml/use_held_item":
                case "tpml_use_held_item":
                case "tpml/use_item":
                case "tpml_use_item":
                    {
                        int? tileX = args?["tileX"]?.Value<int?>();
                        int? tileY = args?["tileY"]?.Value<int?>();
                        float? worldX = args?["worldX"]?.Value<float?>();
                        float? worldY = args?["worldY"]?.Value<float?>();
                        int? direction = args?["direction"]?.Value<int?>();
                        bool? altUse = args?["altUse"]?.Value<bool?>();
                        int? holdFrames = args?["holdFrames"]?.Value<int?>();
                        return await MainThreadQueue.EnqueueAsync(() => UseHeldItem(tileX, tileY, worldX, worldY, direction, altUse, holdFrames));
                    }

                case "tpml/swap_inventory_slots":
                case "tpml_swap_inventory_slots":
                case "tpml/swap_slots":
                    {
                        int fromSlot = args?["fromSlot"]?.Value<int>() ?? -1;
                        int toSlot = args?["toSlot"]?.Value<int>() ?? -1;
                        return await MainThreadQueue.EnqueueAsync(() => SwapInventorySlots(fromSlot, toSlot));
                    }

                case "tpml/teleport":
                case "tpml_teleport":
                    {
                        int tx = args?["tileX"]?.Value<int>() ?? 0;
                        int ty = args?["tileY"]?.Value<int>() ?? 0;
                        return await MainThreadQueue.EnqueueAsync(() => TeleportPlayer(tx, ty));
                    }

                case "tpml/heal_player":
                case "tpml_heal_player":
                    return await MainThreadQueue.EnqueueAsync(() => HealPlayer());

                case "tpml/simulate_scroll_wheel":
                case "tpml_simulate_scroll_wheel":
                    {
                        int delta = args?["delta"]?.Value<int>() ?? 0;
                        bool forUI = args?["forUI"]?.Value<bool?>() ?? true;
                        return await MainThreadQueue.EnqueueAsync(() => SimulateScrollWheel(delta, forUI));
                    }

                case "tpml/get_scroll_state":
                case "tpml_get_scroll_state":
                    return await MainThreadQueue.EnqueueAsync(() => GetScrollState());

                default:
                    return null;
            }
        }

        public static object GetInventory()
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                return new { inWorld = false, message = "当前未进入世界" };
            }

            var p = Main.LocalPlayer;
            var items = new List<object>();

            for (int i = 0; i < p.inventory.Length; i++)
            {
                var item = p.inventory[i];
                if (item != null && item.type > ItemID.None && item.stack > 0)
                {
                    items.Add(new
                    {
                        slot = i,
                        id = item.type,
                        name = item.Name,
                        stack = item.stack,
                        maxStack = item.maxStack,
                        prefix = item.prefix,
                        favorited = item.favorited
                    });
                }
            }

            return new
            {
                inWorld = true,
                playerName = p.name,
                totalSlots = p.inventory.Length,
                itemCount = items.Count,
                inventoryOpen = Main.playerInventory,
                items
            };
        }

        public static object ToggleInventory(bool? open)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                return new { inWorld = false, message = "当前未进入世界，无法切换背包" };
            }

            if (open.HasValue)
            {
                Main.playerInventory = open.Value;
            }
            else
            {
                Main.playerInventory = !Main.playerInventory;
            }

            return new
            {
                success = true,
                inventoryOpen = Main.playerInventory,
                message = Main.playerInventory ? "背包已打开" : "背包已关闭"
            };
        }

        public static object GiveItem(int itemId, int stack)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            if (itemId <= 0)
                return new { success = false, message = "物品 ID 无效" };

            try
            {
                Item item = new Item();
                item.SetDefaults(itemId);
                item.stack = stack;

                Item overflow = Main.LocalPlayer.GetItem(item, GetItemSettings.QuickTransferFromSlot);
                int slot = ToolHelpers.FindInventorySlot(itemId);
                return new
                {
                    success = slot >= 0 && (overflow == null || overflow.IsAir),
                    slot,
                    itemId,
                    stack,
                    message = slot >= 0 ? $"已发放物品 ID={itemId} x{stack}" : "物品未能进入背包",
                    overflow = overflow?.stack ?? 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GiveItem] 异常: {ex}");
                TPML.Content.ModLoader.Log($"[GiveItem] 异常: {ex}");
                throw new Exception($"GiveItem 内部异常: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}", ex);
            }
        }

        public static object SpawnItemInWorld(int itemId, int stack, float offsetX = 200f, float offsetY = 0f)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            if (itemId <= 0)
                return new { success = false, message = "物品 ID 无效" };

            Player player = Main.LocalPlayer;
            Vector2 spawnPos = player.Center + new Vector2(player.direction * offsetX, offsetY);
            int itemIndex = Item.NewItem(new EntitySource_DebugCommand(), spawnPos, itemId, stack);
            return new
            {
                success = itemIndex >= 0 && itemIndex < Main.maxItems,
                itemIndex,
                itemId,
                stack,
                spawnX = spawnPos.X,
                spawnY = spawnPos.Y,
                message = itemIndex >= 0 ? $"已生成远端地面物品 ID={itemId} x{stack} (实体索引: {itemIndex})" : "地面物品生成失败"
            };
        }

        public static object SelectInventorySlot(int slot)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            Player player = Main.LocalPlayer;
            if (slot < 0 || slot >= player.inventory.Length)
                return new { success = false, message = $"背包槽位无效: {slot}" };

            Item selectedItem = player.inventory[slot];
            if (selectedItem == null || selectedItem.IsAir)
                return new { success = false, slot, itemId = 0, message = $"背包槽位为空: {slot}" };

            // 清空任何潜在的鼠标指针残留与丢弃标志
            Main.mouseItem = new Item();
            player.inventory[58] = new Item();
            player.controlUseItem = false;
            player.releaseUseItem = false;
            player.controlUseTile = false;
            player.releaseUseTile = false;
            player.controlThrow = false;
            player.releaseThrow = false;
            player.noThrow = 10;

            // 彻底复位使用计时器与动画
            player.itemAnimation = 0;
            player.itemAnimationMax = 0;
            player.itemTime = 0;
            player.itemTimeMax = 0;
            player.reuseDelay = 0;
            player.pendingItemReuse = false;
            player.channel = false;

            // 确保不残留 blockMouse 与 mouseInterface 污染玩家正常游玩
            Main.blockMouse = false;
            player.mouseInterface = false;

            // 原版标准快捷栏状态转移
            player.selectedItemState.Select(slot);
            player.selectedItemState.Update();

            Item held = player.HeldItem;
            return new
            {
                success = true,
                slot,
                selectedSlot = player.selectedItem,
                itemId = held?.type ?? 0,
                currentItemId = held?.type ?? 0,
                message = $"已将槽位 {slot} 物品 [{held?.Name}] 置为当前手持物品"
            };
        }

        public static object GetInventorySlotInfo(int slot)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { inWorld = false, message = "当前未进入世界" };

            Player player = Main.LocalPlayer;
            if (slot < 0 || slot >= player.inventory.Length)
                return new { inWorld = true, success = false, message = $"背包槽位无效: {slot} (有效范围 0~{player.inventory.Length - 1})" };

            Item item = player.inventory[slot];
            if (item == null || item.IsAir || item.type <= 0)
            {
                return new
                {
                    inWorld = true,
                    slot,
                    isAir = true,
                    itemId = 0,
                    name = string.Empty,
                    displayName = string.Empty,
                    stack = 0,
                    maxStack = 0,
                    message = $"槽位 {slot} 为空"
                };
            }

            // 计算 Tooltip 提示文本与颜色
            int yoyoLogo = -1;
            int numLines = 1;
            string[] toolTipLine = new string[60];
            Color[] lineColors = new Color[60];
            for (int i = 0; i < toolTipLine.Length; i++)
            {
                toolTipLine[i] = string.Empty;
                lineColors[i] = Color.White;
            }

            int rare = item.rare;
            if (item.expert) rare = -12;
            bool isMaster = item.rare == -13 || item.rare == 13;
            if (isMaster) rare = -13;
            lineColors[0] = Main.MouseText_DrawItemTooltip_GetItemNameColor(rare, item.expert ? (byte)1 : (byte)0);

            float knockBack = item.knockBack;
            float kbMult = 1f;
            if (item.melee && player.kbGlove) kbMult += 1f;
            if (player.kbBuff) kbMult += 0.5f;
            knockBack *= kbMult;

            try
            {
                Main.MouseText_DrawItemTooltip_GetLinesInfo(item, ref yoyoLogo, knockBack, ref numLines, toolTipLine, lineColors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TPMLBridge] 计算物品 Tooltip 异常: {ex.Message}");
            }

            var tooltipLines = new List<string>();
            var tooltipDetails = new List<object>();
            for (int i = 0; i < numLines && i < toolTipLine.Length; i++)
            {
                if (!string.IsNullOrEmpty(toolTipLine[i]))
                {
                    tooltipLines.Add(toolTipLine[i]);
                    Color c = lineColors[i];
                    tooltipDetails.Add(new
                    {
                        line = i,
                        text = toolTipLine[i],
                        color = $"#{c.R:X2}{c.G:X2}{c.B:X2}"
                    });
                }
            }

            // 基础属性与战斗属性
            string prefixName = string.Empty;
            if (item.prefix > 0 && item.prefix < Lang.prefix.Length && Lang.prefix[item.prefix] != null)
            {
                prefixName = Lang.prefix[item.prefix].Value;
            }

            string damageType = "none";
            if (item.melee) damageType = "melee";
            else if (item.ranged) damageType = "ranged";
            else if (item.magic) damageType = "magic";
            else if (item.summon) damageType = "summon";
            else if (item.sentry) damageType = "sentry";

            int actualDamage = item.damage > 0 ? player.GetWeaponDamage(item) : 0;
            int actualCrit = item.crit + player.GetWeaponCrit(item);
            float actualKnockback = player.GetWeaponKnockback(item, item.knockBack);

            long val = item.value;
            long platinum = val / 1000000;
            long gold = (val % 1000000) / 10000;
            long silver = (val % 10000) / 100;
            long copper = val % 100;

            return new
            {
                inWorld = true,
                slot,
                isAir = false,
                id = item.type,
                name = item.Name,
                displayName = ToolHelpers.GetItemDisplayName(item.type),
                stack = item.stack,
                maxStack = item.maxStack,
                favorited = item.favorited,
                prefix = item.prefix,
                prefixName,
                damage = actualDamage,
                baseDamage = item.damage,
                damageType,
                crit = actualCrit,
                knockBack = actualKnockback,
                baseKnockBack = item.knockBack,
                useTime = item.useTime,
                useAnimation = item.useAnimation,
                useStyle = item.useStyle,
                autoReuse = item.autoReuse,
                channel = item.channel,
                mana = item.mana,
                shoot = item.shoot,
                shootSpeed = item.shootSpeed,
                ammo = item.ammo,
                useAmmo = item.useAmmo,
                pick = item.pick,
                axe = item.axe * 5,
                hammer = item.hammer,
                fishingPole = item.fishingPole,
                bait = item.bait,
                tileBoost = item.tileBoost,
                createTile = item.createTile,
                createWall = item.createWall,
                defense = item.defense,
                accessory = item.accessory,
                headSlot = item.headSlot,
                bodySlot = item.bodySlot,
                legSlot = item.legSlot,
                vanity = item.vanity,
                lifeRegen = item.lifeRegen,
                healLife = item.healLife,
                healMana = item.healMana,
                buffType = item.buffType,
                buffTime = item.buffTime,
                rare = item.rare,
                expert = item.expert,
                master = isMaster,
                material = item.material,
                consumable = item.consumable,
                value = item.value,
                price = new { platinum, gold, silver, copper },
                tooltipLines,
                tooltipDetails
            };
        }

        public static object UseHeldItem(int? targetTileX, int? targetTileY, float? targetWorldX, float? targetWorldY, int? direction, bool? altUse, int? holdFrames)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            Player p = Main.LocalPlayer;
            if (p.dead || p.CCed)
                return new { success = false, message = "玩家当前处于死亡或被控制状态，无法使用物品" };

            Item item = p.HeldItem;
            if (item == null || item.IsAir || item.useStyle == 0)
                return new { success = false, message = "当前手持物品为空或不可使用" };

            // 计算瞄准的世界坐标与屏幕坐标
            Vector2 targetPos = p.Center + new Vector2(p.direction * 32f, 0f);
            if (targetTileX.HasValue && targetTileY.HasValue)
            {
                targetPos = new Vector2(targetTileX.Value * 16f + 8f, targetTileY.Value * 16f + 8f);
            }
            else if (targetWorldX.HasValue && targetWorldY.HasValue)
            {
                targetPos = new Vector2(targetWorldX.Value, targetWorldY.Value);
            }

            Main.mouseX = (int)(targetPos.X - Main.screenPosition.X);
            Main.mouseY = (int)(targetPos.Y - Main.screenPosition.Y);

            if (direction.HasValue && (direction.Value == -1 || direction.Value == 1))
            {
                p.direction = direction.Value;
            }
            else
            {
                p.direction = (targetPos.X >= p.Center.X) ? 1 : -1;
            }

            bool isAlt = altUse.HasValue && altUse.Value;
            if (isAlt)
            {
                p.altFunctionUse = 1;
            }

            p.controlUseItem = true;
            p.releaseUseItem = true;

            // 立即触发 ItemCheck
            p.ItemCheck();

            int frames = holdFrames.HasValue && holdFrames.Value > 1 ? holdFrames.Value : 1;
            if (frames > 1)
            {
                TerrariaTools.PendingHoldUseFrames = frames - 1;
                TerrariaTools.PendingHoldAlt = isAlt;
            }
            else
            {
                p.controlUseItem = false;
                p.releaseUseItem = false;
                Main.mouseX = Main.screenWidth / 2;
                Main.mouseY = Main.screenHeight / 2;
            }

            return new
            {
                success = true,
                itemId = item.type,
                itemName = item.Name,
                slot = p.selectedItem,
                itemAnimation = p.itemAnimation,
                itemTime = p.itemTime,
                channel = p.channel,
                direction = p.direction,
                altUse = isAlt,
                targetPos = new { x = targetPos.X, y = targetPos.Y },
                message = $"已触发使用手持物品 [{item.Name}] (ID: {item.type})"
            };
        }

        public static object SwapInventorySlots(int fromSlot, int toSlot)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            Player p = Main.LocalPlayer;
            if (fromSlot < 0 || fromSlot >= p.inventory.Length || toSlot < 0 || toSlot >= p.inventory.Length)
                return new { success = false, message = $"槽位索引超出有效范围 (0~{p.inventory.Length - 1})" };

            Utils.Swap(ref p.inventory[fromSlot], ref p.inventory[toSlot]);

            // 彻底清空鼠标残留与投掷标记，复位状态机与游玩状态
            Main.mouseItem = new Item();
            p.inventory[58] = new Item();
            p.controlThrow = false;
            p.releaseThrow = false;
            p.noThrow = 10;
            Main.blockMouse = false;
            p.mouseInterface = false;
            p.selectedItemState.Update();

            return new
            {
                success = true,
                fromSlot,
                fromItemId = p.inventory[fromSlot]?.type ?? 0,
                fromItemName = p.inventory[fromSlot]?.Name ?? string.Empty,
                toSlot,
                toItemId = p.inventory[toSlot]?.type ?? 0,
                toItemName = p.inventory[toSlot]?.Name ?? string.Empty,
                message = $"已交换槽位 {fromSlot} 与 {toSlot}"
            };
        }

        public static object TeleportPlayer(int tileX, int tileY)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                return new { success = false, message = "当前未在世界中" };
            }

            var p = Main.LocalPlayer;
            p.Teleport(new Vector2(tileX * 16f, tileY * 16f), 1);
            return new
            {
                success = true,
                message = $"已传送玩家至图格 ({tileX}, {tileY})",
                tileX,
                tileY
            };
        }

        public static object HealPlayer()
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                return new { success = false, message = "当前未在世界中" };
            }

            var p = Main.LocalPlayer;
            p.statLife = p.statLifeMax2;
            p.statMana = p.statManaMax2;
            p.ClearBuff(BuffID.Bleeding);
            p.ClearBuff(BuffID.Poisoned);
            p.ClearBuff(BuffID.OnFire);

            return new
            {
                success = true,
                life = p.statLife,
                mana = p.statMana,
                message = "玩家已恢复满状态"
            };
        }

        public static object SimulateScrollWheel(int delta, bool forUI)
        {
            int oldSelected = Main.LocalPlayer?.selectedItem ?? -1;

            Terraria.GameInput.PlayerInput.ScrollWheelDelta = delta;
            if (forUI)
            {
                Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI = delta;
            }

            if (!Main.gameMenu && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.HandleHotbarControls();
                Main.LocalPlayer.selectedItemState.Update();
            }

            int newSelected = Main.LocalPlayer?.selectedItem ?? -1;

            return new
            {
                success = true,
                delta,
                forUI,
                oldSelectedItem = oldSelected,
                newSelectedItem = newSelected,
                slotChanged = oldSelected != newSelected,
                hotbar = Main.LocalPlayer?.selectedItemState.Hotbar ?? -1,
                scrollWheelDelta = Terraria.GameInput.PlayerInput.ScrollWheelDelta,
                scrollWheelDeltaForUI = Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI,
                message = $"已模拟滚轮滚动 {delta} (快捷栏槽位: {oldSelected} -> {newSelected})"
            };
        }

        public static object GetScrollState()
        {
            var p = Main.LocalPlayer;
            var mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();

            return new
            {
                success = true,
                xnaMouseWheel = mouseState.ScrollWheelValue,
                playerInputMouseWheel = Terraria.GameInput.PlayerInput.MouseInfo.ScrollWheelValue,
                scrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValue,
                scrollWheelValueOld = Terraria.GameInput.PlayerInput.ScrollWheelValueOld,
                scrollWheelDelta = Terraria.GameInput.PlayerInput.ScrollWheelDelta,
                scrollWheelDeltaForUI = Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI,
                allowInputProcessing = FocusHelper.AllowInputProcessing,
                isAppActive = Main.instance?.IsActive ?? false,
                playerInventory = Main.playerInventory,
                mouseInterface = p?.mouseInterface ?? false,
                selectedItem = p?.selectedItem ?? -1,
                hotbarSelection = p?.selectedItemState.Hotbar ?? -1,
                stateTypeFields = typeof(Player.SelectedItemState).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Select(f => f.Name).ToList(),
                stateTypeMethods = typeof(Player.SelectedItemState).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Select(m => m.Name).Distinct().ToList(),
                hasBufferedSelection = p?.selectedItemState.HasBufferedChange ?? false,
                hasActiveOverride = p?.selectedItemState.HasActiveOverride ?? false,
                canChangeImmediately = p?.selectedItemState.CanChangeSelectedItemImmediately ?? false,
                itemAnimation = p?.itemAnimation ?? 0,
                itemTime = p?.itemTime ?? 0,
                usingOrReusing = p?.UsingOrReusingItem ?? false,
                focusRecipe = Main.focusRecipe
            };
        }
    }
}
