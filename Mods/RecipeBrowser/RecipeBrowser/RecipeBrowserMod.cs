using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using RecipeBrowser.TagHandlers;
using RecipeBrowser.UIElements;
using tContentPatch.Input;
using Terraria;
using Terraria.UI.Chat;
using TPML.Content;
using TPML.Core.Logging;
using KeybindLoader = tContentPatch.Input.KeybindLoader;
using ModKeybind = tContentPatch.Input.ModKeybind;

namespace RecipeBrowser
{
    public class RecipeBrowserTPMLEntry : tContentPatch.Mod
    {
        public static RecipeBrowserTPMLEntry Instance { get; private set; }
        public static RecipeBrowserMod ModInstance { get; private set; }

        public override void Load()
        {
            try
            {
                Instance = this;
                // 内容模组由统一 ContentHost 自动注册并触发 Load，入口只保留旧引擎钩子职责
                ModInstance = ContentHost.Find<RecipeBrowserMod>();
            }
            catch (Exception ex)
            {
                LogManager.GetLogger("RecipeBrowser").Error("Load 异常", ex);
            }
        }
    }

    public class RecipeBrowserMod : TPML.Content.Mod
    {
        public static RecipeBrowserMod Instance { get; private set; }

        public static ModKeybind ToggleRecipeBrowserHotKey { get; private set; }
        public static ModKeybind QueryHoveredItemHotKey { get; private set; }
        public static ModKeybind ToggleFavoritedPanelHotKey { get; private set; }

        public RecipeBrowserTool recipeBrowserTool;
        private int lastSeenScreenWidth;
        private int lastSeenScreenHeight;

        private CancellationTokenSource concurrentTaskHandlerToken;
        private Task concurrentTaskHandler;
        public ConcurrentQueue<Task> concurrentTasks = new ConcurrentQueue<Task>();

        public List<ModCategory> modCategories = new List<ModCategory>();
        public List<ModCategory> modFilters = new List<ModCategory>();
        private HarmonyLib.Harmony harmony;

        public override void Load()
        {
            Instance = this;

            // 0. 初始化 Harmony 补丁
            try
            {
                harmony = new HarmonyLib.Harmony("saintcirno9.recipebrowser");
                harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Logger.Error("Harmony Patch 异常", ex);
            }

            // 1. 注册文本标签解析器
            ChatManager.Register<LinkTagHandler>(new string[] { "l", "link" });
            ChatManager.Register<ImageTagHandler>(new string[] { "image" });
            ChatManager.Register<NPCTagHandler>(new string[] { "npc" });
            ChatManager.Register<ItemHoverFixTagHandler>(new string[] { "itemhover" });

            // 2. 载入客户端 JSON 配置
            RecipeBrowserClientConfig.LoadConfig();

            // 3. 注册统一 ModKeybind
            ToggleRecipeBrowserHotKey = KeybindLoader.RegisterKeybind("RecipeBrowser", "ToggleRecipeBrowser", "O", "打开/关闭合成表 (Toggle Recipe Browser)");
            QueryHoveredItemHotKey = KeybindLoader.RegisterKeybind("RecipeBrowser", "QueryHoveredItem", "C", "查询悬停物品 (Query Hovered Item)");
            ToggleFavoritedPanelHotKey = KeybindLoader.RegisterKeybind("RecipeBrowser", "ToggleFavoritedRecipesWindow", "F5", "切换收藏夹面板 (Toggle Favorited Recipes Window)");

            // 4. 注册 ModPlayer (自动接入 Sidecar 持久化)
            AddContent(new RecipeBrowserPlayer());

            // 5. 初始化本地化注入
            RBLanguage.Initialize();

            // 6. 初始化缓存与算法
            LootCacheManager.Setup();
            RecipePath.PrepareGetCraftPaths();

            // 7. 启动并发计算任务调度器
            concurrentTaskHandlerToken = new CancellationTokenSource();
            concurrentTaskHandler = Task.Run(() => ConcurrentTaskHandler());

            if (!Main.dedServ)
            {
                recipeBrowserTool = new RecipeBrowserTool();
            }
        }

        public void Loaded()
        {
        }

        /// <summary>
        /// 跨模组扩展 API（对齐原版 Call("AddItemCategory"/"AddItemFilter")）：
        /// 其他 TPML 模组可向合成表/物品图鉴注册自定义分类与过滤器
        /// 参数：name(string), parent(string, 可空), icon(Texture2D, 可空), belongs(Predicate&lt;Item&gt;)
        /// </summary>
        public static object Call(string message, params object[] args)
        {
            try
            {
                if (Main.dedServ || args == null || args.Length < 4) return "Failure";
                string name = args[0] as string;
                string parent = args[1] as string;
                Texture2D icon = args[2] as Texture2D;
                Predicate<Item> belongs = args[3] as Predicate<Item>;
                if (string.IsNullOrEmpty(name) || belongs == null) return "Failure";

                if (message == "AddItemCategory")
                {
                    Instance?.modCategories.Add(new ModCategory(name, parent, icon, belongs));
                    SharedUI.instance?.SetupAgain();
                    return "Success";
                }
                if (message == "AddItemFilter")
                {
                    Instance?.modFilters.Add(new ModCategory(name, parent, icon, belongs));
                    SharedUI.instance?.SetupAgain();
                    return "Success";
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger("RecipeBrowser").Error("Call 异常", ex);
            }
            return "Failure";
        }

        public override void Unload()
        {
            concurrentTaskHandlerToken?.Cancel();
            try { concurrentTaskHandler?.Wait(500); } catch { }
            try { harmony?.UnpatchAll("saintcirno9.recipebrowser"); } catch { }

            Instance = null;
            ToggleRecipeBrowserHotKey = null;
            QueryHoveredItemHotKey = null;
            ToggleFavoritedPanelHotKey = null;

            RecipeBrowserUI.instance = null;
            RecipeCatalogueUI.instance = null;
            ItemCatalogueUI.instance = null;
            BestiaryUI.instance = null;
            CraftUI.instance = null;
            SharedUI.instance = null;
            LootCache.instance = null;

            // 清理静态纹理/缓存（对齐原版 Unload 的逐项置空）
            try { RecipeBrowser.Common.RBTextures.Clear(); } catch { }
            try { RecipeBrowser.UIElements.UIItemSlot.hoveredItem = null; } catch { }
            try { RecipeBrowser.UIElements.UIRecipeSlot.availableRecipesCache = null; } catch { }
            try { RecipeBrowser.Utilities.tileTextures?.Clear(); } catch { }
            try { LootCacheManager.itemDrops = null; } catch { }
            try { RecipeBrowser.UIElements.ArmorSetFeatureHelper.Unload(); } catch { }

            RecipePath.Refresh(true);
            RecipeBrowserPlayer.seenTiles = null;
        }

        public async Task ConcurrentTaskHandler()
        {
            List<Task> runningTasks = new List<Task>();
            try
            {
                while (concurrentTaskHandlerToken != null && !concurrentTaskHandlerToken.IsCancellationRequested)
                {
                    if (runningTasks.Count >= 4)
                    {
                        Task task = await Task.WhenAny(runningTasks);
                        runningTasks.Remove(task);
                        try { await task; } catch { }
                    }

                    if (concurrentTasks.TryDequeue(out var result))
                    {
                        if (result != null && !result.IsCanceled)
                        {
                            result.Start();
                            runningTasks.Add(result);
                        }
                    }
                    else
                    {
                        await Task.Delay(100, concurrentTaskHandlerToken.Token);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        public void UpdateUI(GameTime gameTime)
        {
            recipeBrowserTool?.UIUpdate(gameTime);
        }

        public void DrawUI()
        {
            if (lastSeenScreenWidth != Main.screenWidth || lastSeenScreenHeight != Main.screenHeight)
            {
                recipeBrowserTool?.ScreenResolutionChanged();
                lastSeenScreenWidth = Main.screenWidth;
                lastSeenScreenHeight = Main.screenHeight;
            }
            recipeBrowserTool?.UIDraw();
        }
    }
}
