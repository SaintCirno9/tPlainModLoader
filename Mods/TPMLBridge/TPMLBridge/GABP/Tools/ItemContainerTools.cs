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
    /// 药水袋 (PotionBag) 与旗帜盒 (BannerChest) 容器自动化诊断与测试工具
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
                    Description = "诊断药水袋 (PotionBag) 与旗帜盒 (BannerChest) 的物品注册、材质、配方、容量、存储状态及无尽增益注入状态。",
                    Tags = new List<string> { "diagnostic", "read-only", "item_container" },
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

            // 3. 存储状态
            PotionBagStorage.Instance.EnsurePlayerLoaded();
            BannerChestStorage.Instance.EnsurePlayerLoaded();

            result["potionBag"] = new
            {
                capacity = PotionBagStorage.Instance.Capacity,
                storedCount = PotionBagStorage.Instance.GetStoredCount(),
                autoStorage = PotionBagStorage.Instance.AutoStorage,
                autoSort = PotionBagStorage.Instance.AutoSortEnabled,
                isOpen = PotionBagWindow.IsOpen,
                storedItems = PotionBagStorage.Instance.GetStoredItems().Select(it => new { id = it.type, name = Lang.GetItemNameValue(it.type), stack = it.stack }).ToList()
            };

            result["bannerChest"] = new
            {
                capacity = BannerChestStorage.Instance.Capacity,
                storedCount = BannerChestStorage.Instance.GetStoredCount(),
                autoStorage = BannerChestStorage.Instance.AutoStorage,
                autoSort = BannerChestStorage.Instance.AutoSortEnabled,
                isOpen = BannerChestWindow.IsOpen,
                storedItems = BannerChestStorage.Instance.GetStoredItems().Select(it => new { id = it.type, name = Lang.GetItemNameValue(it.type), stack = it.stack }).ToList()
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
            ItemContainerStorage storage = null;
            ItemContainerWindow window = null;

            if (string.Equals(containerName, "potionBag", StringComparison.OrdinalIgnoreCase))
            {
                storage = PotionBagStorage.Instance;
                window = PotionBagWindow.Instance;
            }
            else if (string.Equals(containerName, "bannerChest", StringComparison.OrdinalIgnoreCase))
            {
                storage = BannerChestStorage.Instance;
                window = BannerChestWindow.Instance;
            }
            else
            {
                return new { success = false, message = $"未知容器: {containerName}" };
            }

            storage.EnsurePlayerLoaded();
            Player player = Main.LocalPlayer;

            switch (action?.ToLowerInvariant())
            {
                case "toggle":
                    if (storage is PotionBagStorage) PotionBagWindow.Toggle();
                    else BannerChestWindow.Toggle();
                    return new { success = true, isOpen = window.Parent != null, message = "已切换开闭状态" };

                case "open":
                    if (ModifyInterfaceLayers.ui_state != null && window.Parent == null)
                    {
                        window.Open(ModifyInterfaceLayers.ui_state);
                    }
                    return new { success = true, isOpen = window.Parent != null, message = "已打开容器窗口" };

                case "close":
                    if (window.Parent != null)
                    {
                        window.Close();
                    }
                    return new { success = true, isOpen = window.Parent != null, message = "已关闭容器窗口" };

                case "collect":
                    storage.CollectFromAllInventories(player);
                    return new { success = true, storedCount = storage.GetStoredCount(), message = "已执行一键收集" };

                case "quick_stack":
                    storage.QuickStackFromPlayer(player);
                    return new { success = true, storedCount = storage.GetStoredCount(), message = "已执行快速堆叠" };

                case "withdraw_all":
                    storage.WithdrawAll(player);
                    return new { success = true, storedCount = storage.GetStoredCount(), message = "已执行一键退回" };

                case "sort":
                    storage.AutoSort();
                    return new { success = true, storedCount = storage.GetStoredCount(), message = "已执行整理排序" };

                case "toggle_auto_storage":
                    storage.AutoStorage = !storage.AutoStorage;
                    storage.SaveNow();
                    return new { success = true, autoStorage = storage.AutoStorage, message = $"自动收纳已切换为: {storage.AutoStorage}" };

                case "deposit":
                    if (!itemId.HasValue || itemId.Value <= 0)
                    {
                        return new { success = false, message = "未指定有效的 itemId" };
                    }
                    Item depositItem = new Item();
                    depositItem.SetDefaults(itemId.Value);
                    depositItem.stack = Math.Max(1, stack ?? 1);
                    bool depResult = storage.TryDeposit(depositItem);
                    return new
                    {
                        success = depResult,
                        leftoverStack = depositItem.stack,
                        storedCount = storage.GetStoredCount(),
                        message = depResult ? "存入成功" : "存入失败（可能不符合准入条件或空间不足）"
                    };

                case "clear":
                    for (int i = 0; i < storage.Slots.Length; i++) storage.Slots[i] = new Item();
                    storage.SaveNow();
                    return new { success = true, storedCount = 0, message = "已清空容器" };

                case "get_status":
                default:
                    return new
                    {
                        success = true,
                        container = containerName,
                        capacity = storage.Capacity,
                        storedCount = storage.GetStoredCount(),
                        autoStorage = storage.AutoStorage,
                        autoSort = storage.AutoSortEnabled,
                        isOpen = window.Parent != null,
                        items = storage.GetStoredItems().Select(it => new { id = it.type, name = Lang.GetItemNameValue(it.type), stack = it.stack }).ToList()
                    };
            }
        }
    }
}
