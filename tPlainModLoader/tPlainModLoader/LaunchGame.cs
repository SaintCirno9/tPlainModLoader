using Microsoft.Xna.Framework;
using Mono.Cecil;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using tContentPatch;
using tContentPatch.Utils;

namespace tPlainModLoader
{
    internal class LaunchGame
    {
        internal static Assembly GameAssembly { get; private set; } = null;

        internal static void Initialize(LauncherConfig launchConfig)
        {
            Initialize_CurrentDirectory(launchConfig);
            Initialize_AssemblyResolveEvent();
            Initialize_Microsoft_Xna_Framework_TitleLocation();
        }

        private static void Initialize_CurrentDirectory(LauncherConfig launchConfig)
        {
            string file = launchConfig?.LauncherFilePath;
            string dir = Path.GetDirectoryName(file);

            if (file != null)
            {
                if (File.Exists(file) == false) throw new Exception($"启动文件不存在[{file}]");
            }
            else
            {
                file = null;
                dir = Path.GetFullPath("../Terraria");

                if (Directory.Exists(dir) == false) throw new Exception($"目录不存在[{dir}]");

                string[] fileNames = { "Terraria.exe", "Terraria_v1.4.5.7.exe", "Terraria_v1.4.5.exe" };
                foreach (string i in fileNames)
                {
                    string s = Path.Combine(dir, i);
                    if (File.Exists(s) == false) continue;
                    file = s;
                }

                if (file == null) throw new Exception($"在目录中找不到启动文件[{dir}]");

                if (launchConfig != null) launchConfig.LauncherFilePath = file;
            }

            Directory.SetCurrentDirectory(dir);

            Console.WriteLine($"启动文件路径[{file}]");
            Console.WriteLine($"工作目录[{Directory.GetCurrentDirectory()}]");
        }

        private static void Initialize_AssemblyResolveEvent()
        {
            // 处理服务端/客户端名称及公有化后程序集重定向
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string reqName = new AssemblyName(e.Name).Name;
                if (reqName == "Terraria" || reqName == "TerrariaServer")
                    return GameAssembly;
                return null;
            };

            // 从工作目录找匹配的dll
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), new AssemblyName(e.Name).Name + ".dll");

                if (File.Exists(filePath) == false) return null;

                return Assembly.LoadFile(filePath);
            };
        }

        private static void Initialize_Microsoft_Xna_Framework_TitleLocation()
        {
            string assemblieName = typeof(Vector2).Assembly.GetName().Name;

            if (AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(i => i.GetName().Name == assemblieName) == null)
            {
                Assembly.Load("Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553");
            }

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().First(i => i.GetName().Name == assemblieName);
            Type type = assembly.GetType("Microsoft.Xna.Framework.TitleLocation");
            type.GetField("_titleLocation", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, Directory.GetCurrentDirectory());
        }

        private class CustomAssemblyResolver : BaseAssemblyResolver
        {
            private readonly DefaultAssemblyResolver _defaultResolver = new DefaultAssemblyResolver();
            private readonly string _gameDir;
            private readonly string _hostDir;

            public CustomAssemblyResolver(string launchFilePath)
            {
                _gameDir = Path.GetDirectoryName(launchFilePath);
                _hostDir = AppDomain.CurrentDomain.BaseDirectory;

                if (!string.IsNullOrEmpty(_gameDir) && Directory.Exists(_gameDir))
                {
                    _defaultResolver.AddSearchDirectory(_gameDir);
                }
                if (!string.IsNullOrEmpty(_hostDir) && Directory.Exists(_hostDir))
                {
                    _defaultResolver.AddSearchDirectory(_hostDir);
                }
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                try
                {
                    return _defaultResolver.Resolve(name, parameters);
                }
                catch { }

                string[] searchDirs = { _hostDir, _gameDir, Directory.GetCurrentDirectory() };
                foreach (string dir in searchDirs)
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                    string candidate = Path.Combine(dir, name.Name + ".dll");
                    if (File.Exists(candidate))
                    {
                        return AssemblyDefinition.ReadAssembly(candidate, parameters);
                    }
                }

                Assembly loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name.Name);
                if (loaded != null && !string.IsNullOrEmpty(loaded.Location) && File.Exists(loaded.Location))
                {
                    return AssemblyDefinition.ReadAssembly(loaded.Location, parameters);
                }

                throw new AssemblyResolutionException(name);
            }
        }

        private static Assembly LoadAndPublicizeAssembly(string filePath)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Console.WriteLine($"[Publicizer] 正在对目标程序集执行公有化预处理: {Path.GetFileName(filePath)}...");
            Log.Add($"[Publicizer] 开始公有化元数据: {filePath}");

            var resolver = new CustomAssemblyResolver(filePath);
            var readerParams = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = false
            };

            AssemblyDefinition assemblyDef = AssemblyDefinition.ReadAssembly(filePath, readerParams);

            int typeCount = 0;
            int methodCount = 0;
            int fieldCount = 0;

            foreach (ModuleDefinition module in assemblyDef.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    PublicizeType(type, ref typeCount, ref methodCount, ref fieldCount);
                }
            }

            sw.Stop();
            string msg = $"[Publicizer] 公有化完成 (类型: {typeCount}, 方法: {methodCount}, 字段: {fieldCount}, 耗时: {sw.ElapsedMilliseconds}ms)";
            Console.WriteLine(msg);
            Log.Add(msg);

            // 执行 Prepatcher 预修补引擎（自由字段注入与早期 Cecil 预补丁）
            try
            {
                string gameDir = Path.GetDirectoryName(filePath);
                string hostDir = AppDomain.CurrentDomain.BaseDirectory;
                Prepatcher.PrepatcherEngine.Process(assemblyDef, gameDir, hostDir);
            }
            catch (Exception ex)
            {
                string errMsg = $"[Prepatcher] 执行预修补异常: {ex}";
                Console.WriteLine(errMsg);
                Log.Add(errMsg);
            }

            byte[] assemblyBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                assemblyDef.Write(ms);
                assemblyBytes = ms.ToArray();
            }

            assemblyDef.Dispose();

            return Assembly.Load(assemblyBytes);
        }

        private static void PublicizeType(TypeDefinition type, ref int typeCount, ref int methodCount, ref int fieldCount)
        {
            typeCount++;
            if (type.IsNested)
            {
                type.IsNestedPublic = true;
            }
            else
            {
                type.IsPublic = true;
            }

            foreach (FieldDefinition field in type.Fields)
            {
                // 跳过编译器生成的后备字段（如事件后备字段），保持与 IncludeCompilerGeneratedMembers=false 一致
                if (field.Name.StartsWith("<") || field.IsCompilerControlled)
                    continue;

                fieldCount++;
                field.IsPublic = true;
            }

            foreach (MethodDefinition method in type.Methods)
            {
                // 保持虚方法修饰符（如 protected virtual），与 IncludeVirtualMembers=false 严格对齐，防止派生类 TypeLoadException
                if (method.IsVirtual)
                    continue;

                methodCount++;
                method.IsPublic = true;
            }

            foreach (PropertyDefinition prop in type.Properties)
            {
                if (prop.GetMethod != null && !prop.GetMethod.IsVirtual) prop.GetMethod.IsPublic = true;
                if (prop.SetMethod != null && !prop.SetMethod.IsVirtual) prop.SetMethod.IsPublic = true;
            }

            foreach (EventDefinition evt in type.Events)
            {
                if (evt.AddMethod != null && !evt.AddMethod.IsVirtual) evt.AddMethod.IsPublic = true;
                if (evt.RemoveMethod != null && !evt.RemoveMethod.IsVirtual) evt.RemoveMethod.IsPublic = true;
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                PublicizeType(nested, ref typeCount, ref methodCount, ref fieldCount);
            }
        }

        public static void Run(string launchFilePath, Action runExit)
        {
            GameAssembly = LoadAndPublicizeAssembly(launchFilePath);

            Type launchType = GameAssembly.GetType("Terraria.WindowsLaunch");
            MethodInfo launchMethodInfo = launchType.GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Task task = Task.Run(() =>
            {
                try
                {
                    launchMethodInfo.Invoke(null, new object[] { new string[0] });

                    runExit?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Add($"{nameof(LaunchGame)}:目标程序运行异常:{ex}");
                    Console.WriteLine("目标程序运行异常:");
                    Console.WriteLine($"{ex}");
                    runExit?.Invoke();
                }
            });
        }
    }
}
