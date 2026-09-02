using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TPML;
using TPML.Core.Logging;

namespace tPlainModLoaderInjector
{
    internal static class InjectorGame
    {
        private static readonly ILogger Logger = LogManager.GetLogger("InjectorGame");

        public static int Injector(int pid, string dll)
        {
            if (File.Exists(dll) == false) throw new FileNotFoundException($"找不到注入用文件[{dll}]");

            if (pid == -1) throw new Exception("找不到目标程序");

            return tPlainModLoaderInjector.Injector.InjectManaged((uint)pid, dll, typeof(InjectorGame), InjectorAction, Program.ProgramPath);
        }

        private static int InjectorAction(string args)
        {
            Program.ProgramPath = args;
            LogManager.Initialize(Program.ProgramPath, "tpml.log", LogLevel.Info);

            try
            {
                Logger.Info("已附加到目标游戏程序");

                if (ContentPatch.Initialized) return GetMsgCommandPort(-3);//如果已注入

                Initialize_AssemblyResolveEvent();

                Type type = typeof(ContentPatch);
                ContentPatch cp = (ContentPatch)Activator.CreateInstance(type, true);

                //var a = AppDomain.CurrentDomain.GetAssemblies();

                while (cp.CanInitialize() == false) Thread.Sleep(1);//等待可以初始化

                try
                {
                    Logger.Info("正在初始化内容补丁...");
                    cp.Initialize();
                    Logger.Info("内容补丁初始化成功");

                    return GetMsgCommandPort(-3);
                }
                catch (Exception ex)
                {
                    Logger.Error($"初始化内容补丁失败: {ex.Message}", ex);
                    return -2;
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal("未知异常", ex);
                return -1;
            }
        }

        private static int GetMsgCommandPort(int def)
        {
            return TPML.Command.MsgCommand.Prot > 1 ? TPML.Command.MsgCommand.Prot : def;
        }

        public static void Initialize_AssemblyResolveEvent()
        {
            Initialize_AssemblyResolveEvent_Repeat();
            Initialize_AssemblyResolveEvent_AutoAdd();
            Initialize_AssemblyResolveEvent_RepeatName();
        }

        public static void Initialize_AssemblyResolveEvent_Repeat()//用来处理重复加载程序集的问题
        {
            FieldInfo field = typeof(AppDomain).GetField("_AssemblyResolve", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (field == null) return;

            MulticastDelegate eventDelegate = field.GetValue(AppDomain.CurrentDomain) as MulticastDelegate;
            if (eventDelegate == null) return;

            Delegate[] eventDelegates = eventDelegate.GetInvocationList();
            List<ResolveEventHandler> events = new List<ResolveEventHandler>();

            for (int i = 0; i < eventDelegates?.Length; ++i)
            {
                ResolveEventHandler resolveEventHandler = eventDelegates[i] as ResolveEventHandler;
                if (resolveEventHandler == null) continue;

                events.Add(resolveEventHandler);
            }

            for (int i = 0; i < events.Count; ++i)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= events[i];
            }

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                if (assemblies == null) return null;

                for (int i = 0; i < assemblies.Length; ++i)
                {
                    if (e.Name != assemblies[i].FullName) continue;

                    return assemblies[i];
                }

                return null;
            };

            for (int i = 0; i < events.Count; ++i)
            {
                AppDomain.CurrentDomain.AssemblyResolve += events[i];
            }
        }

        private static void Initialize_AssemblyResolveEvent_AutoAdd()//从目录找匹配的dll
        {
            string path = Program.ProgramPath;

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string filePath = Path.Combine(path, new AssemblyName(e.Name).Name + ".dll");

                if (File.Exists(filePath) == false) return null;

                return Assembly.LoadFile(filePath);
            };
        }

        private static void Initialize_AssemblyResolveEvent_RepeatName()//处理服务端名称不同问题
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string[] ns = new string[] { "TerrariaServer", "Terraria" };

                if (new AssemblyName(e.Name).Name == "Terraria")
                    return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(i => ns.Contains(i.GetName().Name));
                else return null;
            };
        }
    }
}
