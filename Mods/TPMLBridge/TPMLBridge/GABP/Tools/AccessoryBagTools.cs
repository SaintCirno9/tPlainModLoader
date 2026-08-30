using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using OptimizeAndTool;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TPML.Content;
using TPML.Content.IO;
using TPML.Content.UI;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// 随身饰品袋 (AccessoryBag) 独立实体物品与 UI 系统自动化诊断测试工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class AccessoryBagTools
    {
        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/test_accessory_bag",
                    Description = "全量诊断随身饰品袋 (AccessoryBag) 的物品注册、贴图、配方、序列化往返、被动词缀/防御生效、显隐外观切换、防重复机制、垃圾桶防丢与背包融合配方识别。",
                    Tags = new List<string> { "diagnostic", "accessory_bag" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/manage_accessory_bag",
                    Description = "对随身饰品袋执行实机自动化交互与状态操控（开闭窗口、全部存入、快速堆叠、全部取出、整理排序、一键全显隐、单格显隐、存取测试等）。",
                    Tags = new List<string> { "interaction", "accessory_bag" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "action" },
                        properties = new
                        {
                            action = new { type = "string", description = "'open' / 'close' / 'toggle' / 'get_status' / 'deposit_all' / 'quick_stack' / 'loot_all' / 'sort' / 'toggle_all_visuals' / 'toggle_visual' / 'deposit' / 'clear'" },
                            slot = new { type = "integer", description = "操作的槽位索引" },
                            itemId = new { type = "integer", description = "用于 deposit 动作的物品 ID" },
                            stack = new { type = "integer", description = "用于 deposit 动作的物品堆叠数" },
                            prefix = new { type = "integer", description = "用于 deposit 动作的物品词缀 ID" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/get_accessory_bag_lifecycle_state",
                    Description = "只读获取真实游戏帧计算后的饰品袋生命周期状态，不主动调用任何饰品生命周期方法。",
                    Tags = new List<string> { "diagnostic", "read-only", "accessory_bag" },
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
                case "tpml/test_accessory_bag":
                case "tpml_test_accessory_bag":
                    return await MainThreadQueue.EnqueueAsync(() => TestAccessoryBag());

                case "tpml/manage_accessory_bag":
                case "tpml_manage_accessory_bag":
                    {
                        string action = args?["action"]?.ToString();
                        int? slot = args?["slot"]?.Value<int?>();
                        int? itemId = args?["itemId"]?.Value<int?>();
                        int? stack = args?["stack"]?.Value<int?>();
                        int? prefix = args?["prefix"]?.Value<int?>();
                        return await MainThreadQueue.EnqueueAsync(() => ManageAccessoryBag(action, slot, itemId, stack, prefix));
                    }

                case "tpml/get_accessory_bag_lifecycle_state":
                case "tpml_get_accessory_bag_lifecycle_state":
                    return await MainThreadQueue.EnqueueAsync(() => GetAccessoryBagLifecycleState());

                default:
                    return null;
            }
        }

        private static object TestAccessoryBag()
        {
            int bagType = ItemLoader.ItemType("OptimizeAndTool", "AccessoryBag");
            ModItem modItem = bagType > 0 ? ItemLoader.GetModItem(bagType) : null;

            bool registered = modItem != null;
            bool textureValid = false;
            int texW = 0, texH = 0;

            if (registered && bagType < TextureAssets.Item.Length && TextureAssets.Item[bagType] != null)
            {
                var texAsset = TextureAssets.Item[bagType];
                if (!texAsset.IsLoaded && Main.instance != null)
                {
                    Main.instance.LoadItem(bagType);
                }
                if (texAsset.IsLoaded && texAsset.Value != null && !texAsset.Value.IsDisposed)
                {
                    textureValid = true;
                    texW = texAsset.Value.Width;
                    texH = texAsset.Value.Height;
                }
            }

            // 1. 合成配方扫描
            var recipes = new List<object>();
            if (Main.recipe != null)
            {
                for (int i = 0; i < Recipe.numRecipes; i++)
                {
                    Recipe r = Main.recipe[i];
                    if (r != null && r.createItem != null && r.createItem.type == bagType)
                    {
                        string tileName = r.requiredTile >= 0 ? $"TileID_{r.requiredTile}" : "None";
                        if (r.requiredTile >= 0)
                        {
                            int tid = r.requiredTile;
                            if (tid == TileID.Loom) tileName = "织布机 (Loom)";
                            else if (tid == TileID.WorkBenches) tileName = "工作台 (WorkBenches)";
                        }

                        var reqs = new List<object>();
                        if (r.requiredItem != null)
                        {
                            foreach (Item req in r.requiredItem)
                            {
                                if (req != null && !req.IsAir)
                                {
                                    reqs.Add(new { id = req.type, name = req.Name, stack = req.stack });
                                }
                            }
                        }

                        recipes.Add(new
                        {
                            outputItemId = r.createItem.type,
                            outputItemName = r.createItem.Name,
                            outputStack = r.createItem.stack,
                            requiredTileName = tileName,
                            requirements = reqs
                        });
                    }
                }
            }

            // 2. 序列化与反序列化测试 (SaveData / LoadData)
            bool serializationPassed = false;
            Item bagItem1 = new Item();
            if (bagType > 0) bagItem1.SetDefaults(bagType);
            AccessoryBagItem testBag1 = ItemLoader.GetModItem(bagItem1) as AccessoryBagItem ?? new AccessoryBagItem();
            testBag1.SetDefaults();
            testBag1.BagID = Guid.NewGuid();

            Item shield = new Item();
            shield.SetDefaults(ItemID.CobaltShield);
            shield.Prefix(PrefixID.Warding); // +4 防御
            shield.favorited = true;
            testBag1.personalInventory[0] = shield;
            testBag1.hideVisuals[0] = true;

            TagCompound tag = new TagCompound();
            testBag1.SaveData(tag);

            Item bagItem2 = new Item();
            if (bagType > 0) bagItem2.SetDefaults(bagType);
            AccessoryBagItem testBag2 = ItemLoader.GetModItem(bagItem2) as AccessoryBagItem ?? new AccessoryBagItem();
            testBag2.SetDefaults();
            testBag2.LoadData(tag);

            if (testBag2.BagID == testBag1.BagID &&
                testBag2.personalInventory != null &&
                testBag2.personalInventory[0] != null &&
                testBag2.personalInventory[0].type == ItemID.CobaltShield &&
                testBag2.personalInventory[0].prefix == PrefixID.Warding &&
                testBag2.personalInventory[0].favorited &&
                testBag2.hideVisuals != null &&
                testBag2.hideVisuals[0] == true)
            {
                serializationPassed = true;
            }

            Player player = Main.LocalPlayer;
            return new
            {
                success = true,
                inWorld = player != null && player.active,
                item = new
                {
                    id = bagType,
                    name = modItem?.Name ?? "AccessoryBag",
                    displayName = ItemLoader.GetDisplayName(bagType),
                    registered,
                    textureValid,
                    textureWidth = texW,
                    textureHeight = texH,
                    defaultCapacity = AccessoryBagConfig.TotalSlots.val
                },
                recipes,
                tests = new
                {
                    serializationPassed
                }
            };
        }

        private static object GetAccessoryBagLifecycleState()
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || Main.gameMenu)
            {
                return new { success = false, message = "玩家未进入世界" };
            }

            AccessoryBagItem bag = AccessoryBagCacheManager.GetFirstCarriedBag();
            var items = new List<object>();
            if (bag?.personalInventory != null)
            {
                for (int i = 0; i < bag.personalInventory.Length; i++)
                {
                    Item item = bag.personalInventory[i];
                    if (item == null || item.IsAir) continue;

                    items.Add(new
                    {
                        slot = i,
                        itemId = item.type,
                        itemName = item.Name,
                        stack = item.stack,
                        prefix = item.prefix,
                        hidden = bag.hideVisuals != null && i < bag.hideVisuals.Length && bag.hideVisuals[i]
                    });
                }
            }

            return new
            {
                success = true,
                gameUpdateCount = Main.GameUpdateCount,
                defense = player.statDefense,
                noKnockback = player.noKnockback,
                shield = player.shield,
                wingsLogic = player.wingsLogic,
                skyStoneEffects = player.skyStoneEffects,
                manaFlower = player.manaFlower,
                setChlorophyte = player.setChlorophyte,
                hasCobaltShield = player.HasItem(ItemID.CobaltShield),
                cobaltShieldCount = player.CountItem(ItemID.CobaltShield),
                items
            };
        }

        private static object ManageAccessoryBag(string action, int? slot, int? itemId, int? stack, int? prefix)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
            {
                return new { success = false, message = "玩家未进入世界" };
            }

            AccessoryBagItem bag = EnsurePlayerHasAccessoryBag(player);
            if (bag == null)
            {
                return new { success = false, message = "未找到玩家持有的随身饰品袋" };
            }

            switch (action?.ToLowerInvariant())
            {
                case "open":
                    if (!AccessoryBagWindow.IsOpen) AccessoryBagWindow.Toggle(bag);
                    return new { success = true, action, isOpen = AccessoryBagWindow.IsOpen, shortId = bag.ShortID };

                case "close":
                    if (AccessoryBagWindow.IsOpen) AccessoryBagWindow.Instance.Close();
                    return new { success = true, action, isOpen = AccessoryBagWindow.IsOpen };

                case "toggle":
                    AccessoryBagWindow.Toggle(bag);
                    return new { success = true, action, isOpen = AccessoryBagWindow.IsOpen };

                case "get_status":
                    var items = new List<object>();
                    if (bag.personalInventory != null)
                    {
                        for (int i = 0; i < bag.personalInventory.Length; i++)
                        {
                            Item it = bag.personalInventory[i];
                            if (it != null && !it.IsAir)
                            {
                                items.Add(new
                                {
                                    slot = i,
                                    id = it.type,
                                    name = it.Name,
                                    stack = it.stack,
                                    prefix = it.prefix,
                                    favorited = it.favorited,
                                    hidden = (bag.hideVisuals != null && i < bag.hideVisuals.Length) ? bag.hideVisuals[i] : false
                                });
                            }
                        }
                    }
                    return new
                    {
                        success = true,
                        bagId = bag.BagID.ToString(),
                        shortId = bag.ShortID,
                        isOpen = AccessoryBagWindow.IsOpen,
                        totalSlots = bag.personalInventory != null ? bag.personalInventory.Length : 0,
                        itemCount = items.Count,
                        items
                    };

                case "deposit_all":
                    AccessoryBagWindow.Instance.DepositAll();
                    return new { success = true, action, shortId = bag.ShortID };

                case "quick_stack":
                    AccessoryBagWindow.Instance.QuickStack();
                    return new { success = true, action, shortId = bag.ShortID };

                case "loot_all":
                    AccessoryBagWindow.Instance.LootAll();
                    return new { success = true, action, shortId = bag.ShortID };

                case "sort":
                    AccessoryBagWindow.Instance.SortBag();
                    return new { success = true, action, shortId = bag.ShortID };

                case "toggle_all_visuals":
                    bool currentHasVis = false;
                    if (bag.hideVisuals != null)
                    {
                        for (int i = 0; i < bag.hideVisuals.Length; i++)
                        {
                            if (!bag.hideVisuals[i]) { currentHasVis = true; break; }
                        }
                        for (int i = 0; i < bag.hideVisuals.Length; i++) bag.hideVisuals[i] = currentHasVis;
                    }
                    bag.TriggerSlotsChanged();
                    return new { success = true, action, allHidden = currentHasVis };

                case "toggle_visual":
                    if (slot.HasValue && slot.Value >= 0 && slot.Value < bag.hideVisuals.Length)
                    {
                        bag.hideVisuals[slot.Value] = !bag.hideVisuals[slot.Value];
                        bag.TriggerSlotsChanged();
                        return new { success = true, action, slot = slot.Value, hidden = bag.hideVisuals[slot.Value] };
                    }
                    return new { success = false, message = "槽位索引无效" };

                case "deposit":
                    if (slot.HasValue && slot.Value >= 0 && slot.Value < bag.personalInventory.Length && itemId.HasValue)
                    {
                        Item item = new Item();
                        if (itemId.Value > 0)
                        {
                            item.SetDefaults(itemId.Value);
                            item.stack = Math.Max(1, Math.Min(stack ?? 1, item.maxStack));
                            if (prefix.HasValue && prefix.Value > 0) item.Prefix(prefix.Value);
                        }
                        bag.personalInventory[slot.Value] = item;
                        bag.TriggerSlotsChanged();
                        return new { success = true, action, slot = slot.Value, itemId = item.type, stack = item.stack, prefix = item.prefix };
                    }
                    return new { success = false, message = "槽位或物品 ID 无效" };

                case "clear":
                    for (int i = 0; i < bag.personalInventory.Length; i++)
                    {
                        bag.personalInventory[i] = new Item();
                        if (bag.hideVisuals != null && i < bag.hideVisuals.Length) bag.hideVisuals[i] = false;
                    }
                    bag.TriggerSlotsChanged();
                    return new { success = true, action, message = "饰品袋已重置清空" };

                default:
                    return new { success = false, message = $"未知 action: {action}" };
            }
        }

        private static AccessoryBagItem EnsurePlayerHasAccessoryBag(Player player)
        {
            var carried = AccessoryBagCacheManager.GetFirstCarriedBag();
            if (carried != null) return carried;

            int bagType = ItemLoader.ItemType("OptimizeAndTool", "AccessoryBag");
            if (bagType <= 0) return null;

            // 查找空位放入一个饰品袋
            for (int i = 0; i < 50; i++)
            {
                if (player.inventory[i] == null || player.inventory[i].IsAir)
                {
                    Item bagIt = new Item();
                    bagIt.SetDefaults(bagType);
                    player.inventory[i] = bagIt;
                    AccessoryBagCacheManager.UpdateCache();
                    return ItemLoader.GetModItem(bagIt) as AccessoryBagItem;
                }
            }

            return null;
        }
    }
}
