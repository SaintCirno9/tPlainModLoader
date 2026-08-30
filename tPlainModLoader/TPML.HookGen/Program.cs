using System;
using System.Diagnostics;
using System.IO;
using Mono.Cecil;

namespace TPML.HookGen
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("  TPML.HookGen - MonoMod 强类型 Hook 生成器");
            Console.WriteLine("==================================================");

            string defaultGameDir = @"C:\Games\Steam\steamapps\common\Terraria";
            string inputTerrariaPath = args.Length > 0 ? args[0] : Path.Combine(defaultGameDir, "Terraria.exe");
            string outputDllPath = args.Length > 1 ? args[1] : "TerrariaHooks.dll";

            if (!File.Exists(inputTerrariaPath))
            {
                // 尝试当前目录
                string localTerraria = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Terraria.exe");
                if (File.Exists(localTerraria))
                {
                    inputTerrariaPath = localTerraria;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[错误] 未找到输入程序集: {inputTerrariaPath}");
                    Console.ResetColor();
                    return 1;
                }
            }

            Console.WriteLine($"[输入] Terraria 路径: {inputTerrariaPath}");
            Console.WriteLine($"[输出] 目标 DLL 路径: {outputDllPath}");

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                var resolver = new DefaultAssemblyResolver();
                string terrariaDir = Path.GetDirectoryName(inputTerrariaPath);
                if (!string.IsNullOrEmpty(terrariaDir) && Directory.Exists(terrariaDir))
                {
                    resolver.AddSearchDirectory(terrariaDir);
                }
                resolver.AddSearchDirectory(AppDomain.CurrentDomain.BaseDirectory);

                var readerParams = new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    ReadingMode = ReadingMode.Deferred,
                    ReadSymbols = false
                };

                Console.WriteLine("[1/3] 正在解析原版 Terraria 元数据...");
                using (var inputAssembly = AssemblyDefinition.ReadAssembly(inputTerrariaPath, readerParams))
                {
                    Console.WriteLine("[2/3] 正在生成全量 On_ / IL_ 强类型钩子与委托...");
                    var generator = new HookGenerator(inputAssembly.MainModule, "TerrariaHooks")
                    {
                        HookPrivate = true
                    };

                    generator.Generate();

                    Console.WriteLine($"[3/3] 正在写入目标程序集 {outputDllPath}...");
                    string outDir = Path.GetDirectoryName(outputDllPath);
                    if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    {
                        Directory.CreateDirectory(outDir);
                    }

                    generator.OutputModule.Write(outputDllPath);

                    sw.Stop();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine($"★ 生成成功！耗时: {sw.ElapsedMilliseconds}ms");
                    Console.WriteLine($"  - 生成类型数: {generator.GeneratedTypeCount}");
                    Console.WriteLine($"  - 生成方法数: {generator.GeneratedMethodCount}");
                    Console.WriteLine($"  - 产物路径: {Path.GetFullPath(outputDllPath)}");
                    Console.WriteLine("--------------------------------------------------");
                    Console.ResetColor();
                    return 0;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[失败] HookGen 异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                return 2;
            }
        }
    }
}
