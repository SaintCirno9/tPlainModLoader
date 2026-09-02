using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TPML.Content;
using TPML.UI.Menus.ModLoadException;
using TPML.UI.Menus.ModManager;
using TPML.ModLoad;
using TPML.Patch;
using Terraria;
using Terraria.ID;
using TPML.Core.Logging;

namespace TPML
{
    /// <summary/>
    public partial class ContentPatch
    {
        private static readonly ILogger Logger = LogManager.GetLogger("ContentPatch");

        /// <summary>
        /// 在修补前启动的线程使用的方法还是修补前的, 最好在有线程启动前修补
        /// </summary>
        public void Initialize()
        {
            Logger.Info("=== 初始化 ContentPatch ===");
            string p = $"版本: {VersionTPlainModLoader}";
            Logger.Info(p);
            PrintTry(p);

            if (Instance == null) Instance = this;
            else throw new Exception("不可重复初始化");

            Initialized = false;
            Logger.Info($"服务端运行状态: {Main.dedServ}");

            Initialize_CommandMsg();
            Initialize_ModDirectory();
            Initialize_Core_HookGen_And_Patches();
            Initialize_ModLoader();
            Initialize_CMD();

            Initialized = true;

            Logger.Info("ContentPatch 初始化完成");

            if (Main.dedServ)
            {
                LoaderControl.Load();
            }
        }

        private void Initialize_CommandMsg()
        {
            Command.MsgCommand.Initialize();
        }

        private void Initialize_ModLoader()
        {
            LoadConfig lc = new LoadConfig(ModDirectory);
            LoadAssembly la = new LoadAssembly(lc);
            Intercept intercept = new Intercept(la);
            LoadInstance li = new LoadInstance(intercept);//在实例化模组对象前拦截
            TPML.ModLoad.ModLoader ml = new TPML.ModLoad.ModLoader(li, patchId_mod);

            LoaderControl.SetModLoader(ml, intercept);
            //加载时
            LoaderControl.OnModLoad_Start += (e) => UI.Menus.ModLoadingMenu.ModLoadingMenu.OpenLoadMenu(e, LoaderControl.CancelLoad);
            //加载完成时
            LoaderControl.OnModLoad_Ok += () =>
            {
                TPML.Content.KeybindLoader.SyncWithPlayerInput();

                try
                {
                    TPML.Content.ModContent.PostSetupContent();
                }
                catch (Exception ex)
                {
                    TPML.Core.Logging.LogManager.CoreLogger.Error($"PostSetupContent 派发异常: {ex.Message}", ex);
                }

                if (Main.netMode != 0 && Main.netMode != 1) return;
                Threading.MainThreadDispatcher.Enqueue(() => Main.menuMode = MenuID.Title);
            };
            //取消时
            LoaderControl.OnModLoad_Cancel += (e) => UI.Menus.ModLoadingMenu.ModLoadingMenu.OpenLoadMenu(e, LoaderControl.CancelLoad);
            //取消完成
            LoaderControl.OnModLoad_Canceled += () => UI.Menus.ModManager.ModManager.OpenModManagerMenu(null);
            //加载异常时
            LoaderControl.OnModLoad_Exception += (e) =>
            {
                UI.Menus.ModLoadException.ModLoadException.OpenModLoadExceptionMenu(e);
                UI.Menus.ModLoadException.ModLoadException.WaitMenuClose();
                UI.Menus.ModManager.ModManager.OpenModManagerMenu(null);
            };
            //卸载异常时：记录错误并回到模组管理器，禁止杀进程
            LoaderControl.OnModUnload_Exception += (e) =>
            {
                Logger.Error($"卸载模组异常，已中止卸载并返回模组管理器: {e?.Message}", e);
                UI.Menus.ModLoadException.ModLoadException.OpenModLoadExceptionMenu(e);
                UI.Menus.ModManager.ModManager.OpenModManagerMenu(null);
            };
        }

        private void CheckNetplayConnect()
        {
            bool has = false;
            if (Netplay.TcpListener != null) has = true;
            else if (Main.netMode == 1) has = true;

            if (has == false) return;

            string s = "在修补前已有线程启动(在多人游戏中或服务端已启动),一些功能会失效";
            Logger.Warn(s);
            PrintTry(s);
        }

        private void Initialize_Core_HookGen_And_Patches()
        {
            CheckNetplayConnect();

            // 1. 初始化原生内容引擎与全量背包融合底层门控 (包含 PlayerLoader / ItemLoader / NPCLoader / ProjectileLoader)
            try
            {
                TPML.Content.ContentHost.Initialize();
            }
            catch (Exception ex)
            {
                Logger.Error($"[ContentPatch] ContentHost 初始化异常: {ex.Message}", ex);
            }

            // 2. 注册核心系统与功能 HookGen 强类型门面钩子
            ModPatch.Patch_Main.RegisterAll();
            ModPatch.Patch_CreativeAndCraftingSearch.RegisterAll();
            ModPatch.Patch_ChatCommand.RegisterAll();

            // 3. 注册 UI 与控制台 HookGen 强类型门面钩子
            Content.TitleInfo.RegisterAll();
            Content.DedServConsoleCommand.RegisterAll();
            UI.Menus.Patch_MainMenu.Patch_MainMenu.RegisterAll();
            UI.Menus.Patch_UIManageControls.Patch_UIManageControls.RegisterAll();
            UI.Menus.Patch_UIManageControls.Patch_UIKeybindingListItem.RegisterAll();
            UI.Menus.ModSetSwitch.Patch.RegisterAll();
        }

        private void Initialize_ModDirectory()
        {
            Logger.Info("初始化模组与用户数据目录");

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (Directory.Exists(path) == false) throw new Exception($"目录不存在[{path}]");

            ModDirectory = Path.Combine(path, InfoList.Directorys.Mods);

            if (Directory.Exists(ModDirectory) == false)
            {
                Directory.CreateDirectory(ModDirectory);
            }

            if (Directory.Exists(ModDirectory) == false) throw new Exception($"目录不存在[{ModDirectory}]");

            Logger.Info($"模组目录: {ModDirectory}");

            // 初始化 Windows 文档用户数据目录 (Documents/My Games/Terraria/tPlainModLoader)
            try
            {
                string baseSavePath = Main.SavePath;
                if (string.IsNullOrEmpty(baseSavePath))
                {
                    baseSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
                }
                UserSaveDirectory = Path.Combine(baseSavePath, InfoList.Directorys.UserDataRoot);
                if (!Directory.Exists(UserSaveDirectory)) Directory.CreateDirectory(UserSaveDirectory);

                ConfigDirectory = Path.Combine(UserSaveDirectory, InfoList.Directorys.Config);
                if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);

                Logger.Info($"用户数据目录: {UserSaveDirectory}");
                Logger.Info($"模组配置目录: {ConfigDirectory}");
            }
            catch (Exception ex)
            {
                Logger.Error($"用户数据目录初始化异常: {ex.Message}", ex);
            }
        }

        private void Initialize_CMD()
        {
            if (Main.dedServ)
            {
                DedServConsoleCommand.Enable = true;
                return;
            }

            _ = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        System.Threading.Thread.Sleep(1);
                        string s = Console.ReadLine();//注入的没有控制台时这玩意会直接返回null
                        if (s == null) continue;
                        RunCommand(s);
                    }
                    catch (Exception ex)
                    {
                        PrintTry($"指令运行失败:{ex.Message}");
                    }
                }
            });
        }
    }
}
