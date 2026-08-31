using System;
using System.IO;
using System.Reflection;
using System.Threading;
using tContentPatch;
using tContentPatch.Utils;
using TPML.Core.Logging;

namespace tPlainModLoader
{
    internal partial class Program
    {
        public static string LauncherFilePath => launchConfig?.config?.LauncherFilePath;
        private static ConfigHelp<LauncherConfig> launchConfig = null;
        private static string ProgramPath = null;
        private static readonly ILogger Logger = LogManager.GetLogger("Loader");

        public static void Main(string[] args)
        {
            try
            {
                ProgramPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (Directory.Exists(ProgramPath) == false)
                {
                    Console.WriteLine($"路径不存在:[{ProgramPath}]");
                    Console.ReadKey(true);
                    return;
                }

                LogManager.Initialize(ProgramPath, "tpml.log", LogLevel.Info);

                Logger.Info("=== tPlainModLoader 启动引导初始化 ===");

                Initialize_Config();
                Initialize_AssemblyResolveEvent();
                LaunchGame.Initialize(launchConfig?.config);

                Logger.Info("引导与 Cecil Prepatcher 初始化完成");
            }
            catch (Exception ex)
            {
                Logger.Fatal("初始化失败", ex);
                Console.ReadKey(true);
                return;
            }

            //

            Logger.Info("正在启动目标游戏进程...");

            if (LaunchTargetProgram())
            {
                Logger.Info("目标游戏进程已成功载入并启动");
            }
            else
            {
                Logger.Error("启动目标程序失败");
                return;
            }

            //

            Logger.Info("正在初始化内容补丁...");
            if (Initialize_tContentPatch())
            {
                Logger.Info("内容补丁初始化成功");
            }
            else
            {
                Logger.Error("初始化内容补丁失败");
                return;
            }

            string[] titles = new string[] { "tPlainModLoader", "固态泰拉瑞亚", "固态地面:)", "static tile", "this is 固态硬盘", "泰拉瑞亚!启动!",
                "简易模组加载器的意思是做的很简陋", "试试tModLoader", "你知道吗?传说有个叫tModLoader的比这个好一万倍!",
                "如果tPML崩溃了请冷静, 这是正常现象", "你让我怎么棱镜!", "按[Alt]和[F4]免费领取天顶剑", "Null", "1.4.5现已更新!",
                "修修补补又一年", "传奇BUG王[c/FF69B4:404]", "世纪之花灯泡",
                "哈!哈!你发现了彩蛋!"};
            Console.Title = titles[new Random().Next(0, titles.Length)];

            while (true) Thread.Sleep(1);
        }

        private static bool LaunchTargetProgram()
        {
            try
            {
                LaunchGame.Run(LauncherFilePath, OnProgramExit);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"启动目标程序失败: {ex.Message}", ex);
                return false;
            }
        }

        private static void OnProgramExit()
        {
            Logger.Info("目标游戏程序已退出，正在清理并结束宿主进程");
            LogManager.Shutdown();
            Environment.Exit(0);
        }

        private static bool Initialize_tContentPatch()
        {
            Type type = typeof(ContentPatch);
            ContentPatch cp = (ContentPatch)Activator.CreateInstance(type, true);

            while (cp.CanInitialize() == false) Thread.Sleep(1);

            try
            {
                cp.Initialize();
            }
            catch (Exception ex)
            {
                Logger.Error($"初始化内容补丁失败: {ex.Message}", ex);
                return false;
            }
            return true;
        }

        private static void Initialize_Config()
        {
            launchConfig = new ConfigHelp<LauncherConfig>(Path.Combine(ProgramPath, "launchConfig.json"));
            launchConfig.UpdateConfig(() => new LauncherConfig());

            Logger.Info($"启动文件位置: {LauncherFilePath}");
        }

        private static void Initialize_AssemblyResolveEvent()
        {
            //用来处理重复加载程序集的问题
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                for (int i = 0; i < assemblies.Length; ++i)
                {
                    if (e.Name == assemblies[i].FullName)
                        return assemblies[i];
                }

                return null;
            };
        }
    }
}
