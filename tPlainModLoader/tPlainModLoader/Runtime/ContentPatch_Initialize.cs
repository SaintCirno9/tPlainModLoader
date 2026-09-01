using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using tContentPatch.Content;
using tContentPatch.Content.Menus.ModLoadException;
using tContentPatch.Content.Menus.ModManager;
using tContentPatch.ModLoad;
using tContentPatch.Patch;
using Terraria;
using Terraria.ID;
using TPML.Core.Logging;

namespace tContentPatch
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
            Initialize_AddPatch();
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
            ModLoader ml = new ModLoader(li, patchId_mod);

            LoaderControl.SetModLoader(ml, intercept);
            //加载时
            LoaderControl.OnModLoad_Start += (e) => Content.Menus.ModLoadingMenu.ModLoadingMenu.OpenLoadMenu(e, LoaderControl.CancelLoad);
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
            LoaderControl.OnModLoad_Cancel += (e) => Content.Menus.ModLoadingMenu.ModLoadingMenu.OpenLoadMenu(e, LoaderControl.CancelLoad);
            //取消完成
            LoaderControl.OnModLoad_Canceled += () => ModManager.OpenModManagerMenu(null);
            //加载异常时
            LoaderControl.OnModLoad_Exception += (e) =>
            {
                ModLoadException.OpenModLoadExceptionMenu(e);
                ModLoadException.WaitMenuClose();
                ModManager.OpenModManagerMenu(null);
            };
            //卸载异常时：记录错误并回到模组管理器，禁止杀进程
            LoaderControl.OnModUnload_Exception += (e) =>
            {
                Logger.Error($"卸载模组异常，已中止卸载并返回模组管理器: {e?.Message}", e);
                Content.Menus.ModLoadException.ModLoadException.OpenModLoadExceptionMenu(e);
                Content.Menus.ModManager.ModManager.OpenModManagerMenu(null);
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

        private void Initialize_AddPatch()
        {
            CheckNetplayConnect();

            // 初始化原生内容引擎与全量背包融合底层门控
            try
            {
                TPML.Content.ContentHost.Initialize();
            }
            catch (Exception ex)
            {
                Logger.Error($"[ContentPatch] ContentHost 初始化异常: {ex.Message}", ex);
            }

            // M2: 引擎补丁自 Harmony PatchAll 迁移为各补丁类显式 RegisterAll()（MonoMod）
            ModPatch.Patch_Main.RegisterAll();
            ModPatch.Patch_Player.RegisterAll();
            ModPatch.Patch_PlayerFileData.RegisterAll();
            ModPatch.Patch_NPC.RegisterAll();
            ModPatch.Patch_Item.RegisterAll();
            ModPatch.Patch_Projectile.RegisterAll();
            ModPatch.Patch_TileLightScanner.RegisterAll();
            ModPatch.Patch_RemadeChatMonitor.RegisterAll();
            ModPatch.Patch_WorldFile.RegisterAll();
            ModPatch.Patch_NetMessage.RegisterAll();
            ModPatch.Patch_MessageBuffer.RegisterAll();
            ModPatch.Patch_Chest.RegisterAll();
            ModPatch.Patch_RemoteClient.RegisterAll();
            ModPatch.Patch_WorldGen.RegisterAll();
            ModPatch.Patch_CreativeAndCraftingSearch.RegisterAll();
            ModPatch.Patch_ChatCommand.RegisterAll();

            Content.AutoLoadMod.RegisterAll();
            Content.DrawTip.RegisterAll();
            Content.DrawIME.RegisterAll();
            Content.DedServConsoleCommand.RegisterAll();
            Content.Network.RegisterNetModule.RegisterAll();
            Content.TitleInfo.RegisterAll();
            Content.Menus.Patch_MainMenu.Patch_MainMenu.RegisterAll();
            Content.Menus.Patch_UIManageControls.Patch_UIManageControls.RegisterAll();
            Content.Menus.Patch_UIManageControls.Patch_UIKeybindingListItem.RegisterAll();
            Content.Menus.ModSetSwitch.Patch.RegisterAll();

            typePatch = new ModPatch.TypePatch();
            typePatch.AddPatch(new ModPatch.Patch_Main());
            typePatch.AddPatch(new ModPatch.Patch_Player());
            typePatch.AddPatch(new ModPatch.Patch_NPC());
            typePatch.AddPatch(new ModPatch.Patch_Item());
            typePatch.AddPatch(new ModPatch.Patch_Projectile());
            typePatch.AddPatch(new ModPatch.Patch_TileLightScanner());
            typePatch.AddPatch(new ModPatch.Patch_RemadeChatMonitor());
            typePatch.AddPatch(new ModPatch.Patch_WorldFile());
            typePatch.AddPatch(new ModPatch.Patch_NetMessage());
            typePatch.AddPatch(new ModPatch.Patch_MessageBuffer());
            typePatch.AddPatch(new ModPatch.Patch_Chest());
            typePatch.AddPatch(new ModPatch.Patch_RemoteClient());
            typePatch.AddPatch(new ModPatch.Patch_WorldGen());
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
