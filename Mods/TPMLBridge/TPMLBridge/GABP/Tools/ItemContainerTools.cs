using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using OptimizeAndTool;
using OptimizeAndTool.Content.QoL;
using OptimizeAndTool.Content.Storage.ItemContainer;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TPML.Content;
using TPML.Content.UI;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// 药水袋 (PotionBag) 与旗帜盒 (BannerChest) 独立实体容器自动化诊断与测试工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class ItemContainerTools
    {
        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/test_item_containers",
                    Description = "诊断药水袋 (PotionBag) 与旗帜盒 (BannerChest) 的物品注册、材质、配方、容量、实体存储状态及无尽增益注入状态。",
                    Tags = new List<string> { "diagnostic", "read-only", "item_container" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/test_inventory_fusion",
                    Description = "全量诊断与验证 TPML.Content 框架级背包融合系统（HasItem/CountItem/ConsumeItem/魔杖使用与放置/油漆识别/大背包数据联动）。",
                    Tags = new List<string> { "diagnostic", "inventory_fusion" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/manage_item_container",
                    Description = "对药水袋与旗帜盒执行实机自动化交互（开闭窗口、一键收集、快速堆叠、一键退回、整理排序、自动收纳开关、存取测试等）。",
                    Tags = new List<string> { "interaction", "item_container" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "container", "action" },
                        properties = new
                        {
                            container = new { type = "string", description = "'potionBag' 或 'bannerChest'" },
                            action = new { type = "string", description = "'toggle' / 'open' / 'close' / 'collect' / 'quick_stack' / 'withdraw_all' / 'sort' / 'toggle_auto_storage' / 'deposit' / 'get_status' / 'clear'" },
                            itemId = new { type = "integer", description = "用于 deposit 动作的目标物品 ID" },
                            stack = new { type = "integer", description = "用于 deposit 动作的物品数量" }
                        }
                    }
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/test_inventory_fusion":
                case "tpml_test_inventory_fusion":
                    return await MainThreadQueue.EnqueueAsync(() => TestInventoryFusion());

                case "tpml/test_item_containers":
                case "tpml_test_item_containers":
                    return await MainThreadQueue.EnqueueAsync(() => TestItemContainers());

                case "tpml/manage_item_container":
                case "tpml_manage_item_container":
                    {
                        string container = args?["container"]?.ToString();
                        string action = args?["action"]?.ToString();
                        int? itemId = args?["itemId"]?.Value<int?>();
                        int? stack = args?["stack"]?.Value<int?>();
                        return await MainThreadQueue.EnqueueAsync(() => ManageItemContainer(container, action, itemId, stack));
                    }

                default:
                    return null;
            }
        }

        private static object TestInventoryFusion()
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
            {
                return new { success = false, message = "当前未进入有效世界或玩家未激活" };
            }

            var activeSources = TPML.Content.Fusion.InventoryFusionManager.GetActiveSources(player);
            var sourceIds = activeSources.Select(s => s.Id).ToList();

            // 1. 备份玩家原始背包 0~57 与大背包前 2 格
            Item[] originalInv = new Item[58];
            for (int i = 0; i < 58; i++)
            {
                originalInv[i] = player.inventory[i] != null ? player.inventory[i].Clone() : new Item();
            }

            Item originalBigBag0 = OptimizeAndTool.Content.BigBag.BigBag.Slots != null && OptimizeAndTool.Content.BigBag.BigBag.Slots.Length > 0
                ? OptimizeAndTool.Content.BigBag.BigBag.Slots[0].Clone()
                : new Item();
            Item originalBigBag1 = OptimizeAndTool.Content.BigBag.BigBag.Slots != null && OptimizeAndTool.Content.BigBag.BigBag.Slots.Length > 1
                ? OptimizeAndTool.Content.BigBag.BigBag.Slots[1].Clone()
                : new Item();

            try
            {
                // 2. 清空玩家主背包中的普通木材 (ItemID.Wood = 9)
                for (int i = 0; i < 58; i++)
                {
                    if (player.inventory[i] != null && player.inventory[i].type == ItemID.Wood)
                    {
                        player.inventory[i] = new Item();
                    }
                }

                // 3. 向大背包第 0 格放入 50 个普通木材
                Item woodInBigBag = new Item();
                woodInBigBag.SetDefaults(ItemID.Wood);
                woodInBigBag.stack = 50;
                OptimizeAndTool.Content.BigBag.BigBag.Slots[0] = woodInBigBag;
                OptimizeAndTool.Content.BigBag.BigBag.NotifySlotsChanged();

                // 4. 断言 HasItem & CountItem
                bool hasItemWood = player.HasItem(ItemID.Wood);
                int countItemWood = player.CountItem(ItemID.Wood);

                // 5. 构造生命木魔棒 (ItemID.LivingWoodWand = 832)
                Item livingWoodWand = new Item();
                livingWoodWand.SetDefaults(ItemID.LivingWoodWand);

                // 将生命木魔杖放入 0 号快捷栏并选中
                player.inventory[0] = livingWoodWand;
                player.selectedItemState.Select(0);
                player.selectedItemState.Update();

                // 测试挥动与放置可用性
                bool canUseWand = player.ItemCheck_CheckCanUse_Inner(livingWoodWand);
                bool canPlaceWandTile = player.PlaceThing_Tiles_CheckWandUsability(true);

                // 6. 测试消耗：调用 InventoryFusionManager.ConsumeItem
                bool consumeResult = TPML.Content.Fusion.InventoryFusionManager.ConsumeItem(player, ItemID.Wood);
                int countAfterConsume = player.CountItem(ItemID.Wood);
                int bigBagSlot0StackAfter = OptimizeAndTool.Content.BigBag.BigBag.Slots[0].stack;

                // 7. 测试油漆/涂料查找
                for (int i = 0; i < 58; i++)
                {
                    if (player.inventory[i] != null && player.inventory[i].PaintOrCoating)
                    {
                        player.inventory[i] = new Item();
                    }
                }
                Item paintInBigBag = new Item();
                paintInBigBag.SetDefaults(ItemID.RedPaint);
                paintInBigBag.stack = 20;
                OptimizeAndTool.Content.BigBag.BigBag.Slots[1] = paintInBigBag;

                Item foundPaint = player.FindPaintOrCoating();
                bool foundPaintSuccess = foundPaint != null && foundPaint.type == ItemID.RedPaint && foundPaint.stack == 20;

                // 8. 测试蓝图魔杖自动制造与大背包材料联动 (StructureCraftingEngine)
                var blueprintData = new WandsTool.Content.Structure.StructureData(2, 2, "TestBlueprint");
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        blueprintData.Tiles[x, y] = new WandsTool.Content.Structure.TileSnapshot
                        {
                            TileType = -1,
                            WallType = (short)WallID.Wood
                        };
                    }
                }
                // 清空主背包的木材与木墙
                for (int i = 0; i < 58; i++)
                {
                    if (player.inventory[i] != null && (player.inventory[i].type == ItemID.Wood || player.inventory[i].type == ItemID.WoodWall))
                    {
                        player.inventory[i] = new Item();
                    }
                }
                // 向大背包放入 1 个木材 (1 木材在工作台/免工作台可合成 4 木墙)
                Item woodForBlueprint = new Item();
                woodForBlueprint.SetDefaults(ItemID.Wood);
                woodForBlueprint.stack = 1;
                OptimizeAndTool.Content.BigBag.BigBag.Slots[0] = woodForBlueprint;
                OptimizeAndTool.Content.BigBag.BigBag.NotifySlotsChanged();

                var blueprintPlan = WandsTool.Content.Structure.StructureCraftingEngine.BuildPlan(blueprintData, player, allowAutoCraft: true);
                bool blueprintPlanPossible = blueprintPlan.IsPossible && blueprintPlan.IngredientConsumes.ContainsKey(ItemID.Wood) && blueprintPlan.IngredientConsumes[ItemID.Wood] == 1;

                if (blueprintPlanPossible)
                {
                    WandsTool.Content.Structure.StructureCraftingEngine.ExecutePlan(blueprintPlan, player);
                }
                bool blueprintExecuteSuccess = OptimizeAndTool.Content.BigBag.BigBag.Slots[0] == null || OptimizeAndTool.Content.BigBag.BigBag.Slots[0].IsAir;

                bool allPassed = hasItemWood &&
                                 countItemWood == 50 &&
                                 canUseWand &&
                                 canPlaceWandTile &&
                                 consumeResult &&
                                 countAfterConsume == 49 &&
                                 bigBagSlot0StackAfter == 49 &&
                                 foundPaintSuccess &&
                                 blueprintPlanPossible &&
                                 blueprintExecuteSuccess;

                return new
                {
                    success = allPassed,
                    activeSourceCount = activeSources.Count,
                    sourceIds = sourceIds,
                    hasItemWood = hasItemWood,
                    countItemWood = countItemWood,
                    canUseWand = canUseWand,
                    canPlaceWandTile = canPlaceWandTile,
                    consumeResult = consumeResult,
                    countAfterConsume = countAfterConsume,
                    bigBagSlot0StackAfter = bigBagSlot0StackAfter,
                    foundPaintSuccess = foundPaintSuccess,
                    blueprintPlanPossible = blueprintPlanPossible,
                    blueprintExecuteSuccess = blueprintExecuteSuccess,
                    message = allPassed ? "TPML 框架背包融合系统、生命木魔杖与蓝图自动制造全部断言通过！" : "部分断言未通过"
                };
            }
            finally
            {
                // 还原现场
                for (int i = 0; i < 58; i++)
                {
                    player.inventory[i] = originalInv[i];
                }
                if (OptimizeAndTool.Content.BigBag.BigBag.Slots != null && OptimizeAndTool.Content.BigBag.BigBag.Slots.Length > 1)
                {
                    OptimizeAndTool.Content.BigBag.BigBag.Slots[0] = originalBigBag0;
                    OptimizeAndTool.Content.BigBag.BigBag.Slots[1] = originalBigBag1;
                    OptimizeAndTool.Content.BigBag.BigBag.NotifySlotsChanged();
                }
            }
        }

        private static IItemContainer FindContainer(string containerName)
        {
            Player player = Main.LocalPlayer;
            if (player == null) return null;

            bool isPotion = string.Equals(containerName, "potionBag", StringComparison.OrdinalIgnoreCase);
            int targetType = isPotion ? ModContent.ItemType<PotionBagItem>() : ModContent.ItemType<BannerChestItem>();

            // 1. 如果窗口当前打开且正好是该容器，直接使用
            if (ItemContainerWindow.IsOpen && ItemContainerWindow.Instance.Container != null)
            {
                if (isPotion && ItemContainerWindow.Instance.Container is PotionBagItem) return ItemContainerWindow.Instance.Container;
                if (!isPotion && ItemContainerWindow.Instance.Container is BannerChestItem) return ItemContainerWindow.Instance.Container;
            }

            // 2. 从玩家背包查找
            if (player.inventory != null && targetType > 0)
            {
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item it = player.inventory[i];
                    if (it != null && !it.IsAir && it.type == targetType)
                    {
                        var c = ItemLoader.GetModItem(it) as IItemContainer;
                        if (c != null) return c;
                    }
                }
            }

            // 3. 从银行查找
            Item[][] banks = new[] { player.bank?.item, player.bank2?.item, player.bank3?.item, player.bank4?.item };
            foreach (var bank in banks)
            {
                if (bank == null) continue;
                for (int i = 0; i < bank.Length; i++)
                {
                    Item it = bank[i];
                    if (it != null && !it.IsAir && it.type == targetType)
                    {
                        var c = ItemLoader.GetModItem(it) as IItemContainer;
                        if (c != null) return c;
                    }
                }
            }

            return null;
        }

        public static object TestItemContainers()
        {
            var result = new Dictionary<string, object>();

            // 1. 物品注册与材质检测
            var itemsList = new List<object>();
            var targets = new (string name, Type type, Func<int> getId)[]
            {
                ("PotionBag", typeof(PotionBagItem), () => ModContent.ItemType<PotionBagItem>()),
                ("BannerChest", typeof(BannerChestItem), () => ModContent.ItemType<BannerChestItem>())
            };

            foreach (var (modItemName, itemType, getId) in targets)
            {
                int id = getId();
                bool registered = id > 0;
                bool texValid = false;
                int texW = 0, texH = 0;

                if (registered && id < TextureAssets.Item.Length && TextureAssets.Item[id] != null)
                {
                    var texAsset = TextureAssets.Item[id];
                    if (texAsset.IsLoaded && texAsset.Value != null)
                    {
                        texValid = true;
                        texW = texAsset.Value.Width;
                        texH = texAsset.Value.Height;
                    }
                }

                Item sampleItem = new Item();
                if (registered) sampleItem.SetDefaults(id);

                var tooltips = new List<string>();
                int yoyoLogo = 0;
                int numLines = 0;
                string[] lines = new string[30];
                Color[] colors = new Color[30];
                try
                {
                    Main.MouseText_DrawItemTooltip_GetLinesInfo(sampleItem, ref yoyoLogo, 0f, ref numLines, lines, colors);
                    for (int i = 0; i < numLines; i++)
                    {
                        if (!string.IsNullOrEmpty(lines[i])) tooltips.Add(lines[i]);
                    }
                }
                catch { }

                itemsList.Add(new
                {
                    name = modItemName,
                    id = id,
                    displayName = sampleItem.Name,
                    registered = registered,
                    textureValid = texValid,
                    textureWidth = texW,
                    textureHeight = texH,
                    tooltips = tooltips
                });
            }
            result["items"] = itemsList;

            // 2. 合成配方检测
            int pbId = ModContent.ItemType<PotionBagItem>();
            int bcId = ModContent.ItemType<BannerChestItem>();

            var recipesList = new List<object>();
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe r = Main.recipe[i];
                if (r == null || r.createItem == null) continue;

                int createId = r.createItem.type;
                if (createId == pbId || createId == bcId)
                {
                    var reqs = new List<object>();
                    for (int j = 0; j < r.requiredItem.Length; j++)
                    {
                        Item req = r.requiredItem[j];
                        if (req != null && req.type > 0 && req.stack > 0)
                        {
                            reqs.Add(new
                            {
                                id = req.type,
                                name = Lang.GetItemNameValue(req.type),
                                stack = req.stack
                            });
                        }
                    }

                    string tileName = "None";
                    if (r.requiredTile >= 0)
                    {
                        int tileId = r.requiredTile;
                        tileName = $"Tile_{tileId}";
                        if (tileId == TileID.Loom) tileName = "织布机 (Loom)";
                        else if (tileId == TileID.WorkBenches) tileName = "工作台 (WorkBench)";
                    }

                    recipesList.Add(new
                    {
                        recipeIndex = i,
                        outputItemId = createId,
                        outputItemName = r.createItem.Name,
                        outputStack = r.createItem.stack,
                        requiredTileName = tileName,
                        requirements = reqs
                    });
                }
            }
            result["recipes"] = recipesList;

            // 3. 实体容器状态
            IItemContainer activePb = FindContainer("potionBag");
            IItemContainer activeBc = FindContainer("bannerChest");

            result["potionBag"] = new
            {
                found = activePb != null,
                capacity = activePb?.Capacity ?? 200,
                storedCount = activePb?.GetStoredCount() ?? 0,
                autoStorage = activePb?.AutoStorage ?? false,
                autoSort = activePb?.AutoSortEnabled ?? false,
                isOpen = PotionBagWindow.IsOpen,
                storedItems = activePb != null ? activePb.GetStoredItems().Select(it => (object)new { id = it.type, name = Lang.GetItemNameValue(it.type), stack = it.stack }).ToList() : new List<object>()
            };

            result["bannerChest"] = new
            {
                found = activeBc != null,
                capacity = activeBc?.Capacity ?? 500,
                storedCount = activeBc?.GetStoredCount() ?? 0,
                autoStorage = activeBc?.AutoStorage ?? false,
                autoSort = activeBc?.AutoSortEnabled ?? false,
                isOpen = BannerChestWindow.IsOpen,
                storedItems = activeBc != null ? activeBc.GetStoredItems().Select(it => (object)new { id = it.type, name = Lang.GetItemNameValue(it.type), stack = it.stack }).ToList() : new List<object>()
            };

            // 4. 无尽增益与旗帜状态
            result["buffState"] = new
            {
                activeInfiniteBuffs = InfinitePotionAndBuff.ActiveInfiniteBuffs.ToList(),
                hasBanner = Main.SceneMetrics?.hasBanner ?? false,
                potionThreshold = InfinitePotionAndBuff.PotionThreshold.val,
                enableInfinitePotions = InfinitePotionAndBuff.EnableInfinitePotions.val,
                enableMonsterBanners = InfinitePotionAndBuff.EnableMonsterBanners.val
            };

            return result;
        }

        public static object ManageItemContainer(string containerName, string action, int? itemId, int? stack)
        {
            IItemContainer container = FindContainer(containerName);
            bool isPotion = string.Equals(containerName, "potionBag", StringComparison.OrdinalIgnoreCase);

            if (container == null)
            {
                // 如果身上没有，且动作为 deposit/open/get_status，尝试在身上找个空格放一个容器或者报错
                if (action == "get_status")
                {
                    return new
                    {
                        success = false,
                        container = containerName,
                        message = "玩家身上未找到该容器物品"
                    };
                }
            }

            Player player = Main.LocalPlayer;

            switch (action?.ToLowerInvariant())
            {
                case "toggle":
                    if (isPotion) PotionBagWindow.Toggle(container);
                    else BannerChestWindow.Toggle(container);
                    return new { success = true, isOpen = ItemContainerWindow.IsOpen, message = "已切换开闭状态" };

                case "open":
                    if (container != null && ModifyInterfaceLayers.ui_state != null)
                    {
                        ItemContainerWindow.Instance.Open(container, ModifyInterfaceLayers.ui_state);
                    }
                    return new { success = true, isOpen = ItemContainerWindow.IsOpen, message = "已打开容器窗口" };

                case "close":
                    if (ItemContainerWindow.IsOpen)
                    {
                        ItemContainerWindow.Instance.Close();
                    }
                    return new { success = true, isOpen = ItemContainerWindow.IsOpen, message = "已关闭容器窗口" };

                case "collect":
                    if (container != null) container.CollectFromAllInventories(player);
                    return new { success = container != null, storedCount = container?.GetStoredCount() ?? 0, message = "已执行一键收集" };

                case "quick_stack":
                    if (container != null) container.QuickStackFromPlayer(player);
                    return new { success = container != null, storedCount = container?.GetStoredCount() ?? 0, message = "已执行快速堆叠" };

                case "withdraw_all":
                    if (container != null) container.WithdrawAll(player);
                    return new { success = container != null, storedCount = container?.GetStoredCount() ?? 0, message = "已执行一键退回" };

                case "sort":
                    if (container != null) container.AutoSort();
                    return new { success = container != null, storedCount = container?.GetStoredCount() ?? 0, message = "已执行整理排序" };

                case "toggle_auto_storage":
                    if (container != null)
                    {
                        container.AutoStorage = !container.AutoStorage;
                        container.TriggerSlotsChanged();
                    }
                    return new { success = container != null, autoStorage = container?.AutoStorage ?? false, message = $"自动收纳已切换为: {container?.AutoStorage ?? false}" };

                case "deposit":
                    if (container == null) return new { success = false, message = "未找到容器实体" };
                    if (!itemId.HasValue || itemId.Value <= 0)
                    {
                        return new { success = false, message = "未指定有效的 itemId" };
                    }
                    Item depositItem = new Item();
                    depositItem.SetDefaults(itemId.Value);
                    depositItem.stack = Math.Max(1, stack ?? 1);
                    bool depResult = container.TryDeposit(depositItem);
                    return new
                    {
                        success = depResult,
                        leftoverStack = depositItem.stack,
                        storedCount = container.GetStoredCount(),
                        message = depResult ? "存入成功" : "存入失败（可能不符合准入条件或空间不足）"
                    };

                case "clear":
                    if (container != null)
                    {
                        for (int i = 0; i < container.Slots.Length; i++) container.Slots[i] = new Item();
                        container.TriggerSlotsChanged();
                    }
                    return new { success = container != null, storedCount = 0, message = "已清空容器" };

                case "get_status":
                default:
                    return new
                    {
                        success = container != null,
                        container = containerName,
                        capacity = container?.Capacity ?? (isPotion ? 200 : 500),
                        storedCount = container?.GetStoredCount() ?? 0,
                        autoStorage = container?.AutoStorage ?? false,
                        autoSort = container?.AutoSortEnabled ?? false,
                        isOpen = ItemContainerWindow.IsOpen,
                        items = container != null ? container.GetStoredItems().Select(it => (object)new { id = it.type, name = Lang.GetItemNameValue(it.type), stack = it.stack }).ToList() : new List<object>()
                    };
            }
        }
    }
}
