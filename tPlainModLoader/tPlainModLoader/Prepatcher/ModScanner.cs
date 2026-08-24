using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using tContentPatch;

namespace tPlainModLoader.Prepatcher
{
    internal class ModScanner
    {
        internal class MiniModConfig
        {
            public string key { get; set; }
            public string dllPath { get; set; }
            public bool isLoad { get; set; } = true;
        }

        public static List<string> ScanActiveModDlls(string gameDir, string hostDir)
        {
            List<string> result = new List<string>();
            HashSet<string> scannedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] possibleModsDirs = {
                Path.Combine(hostDir ?? string.Empty, "Mods"),
                Path.Combine(gameDir ?? string.Empty, "Mods"),
                Path.Combine(Directory.GetCurrentDirectory(), "Mods"),
                Path.GetFullPath("../Mods")
            };

            foreach (string modsDir in possibleModsDirs)
            {
                if (string.IsNullOrEmpty(modsDir) || !Directory.Exists(modsDir))
                    continue;

                foreach (string modSubDir in Directory.GetDirectories(modsDir))
                {
                    try
                    {
                        string loadConfigPath = Path.Combine(modSubDir, InfoList.Files.ModLoadConfig);
                        if (File.Exists(loadConfigPath))
                        {
                            string json = File.ReadAllText(loadConfigPath);
                            MiniModConfig cfg = JsonConvert.DeserializeObject<MiniModConfig>(json);
                            if (cfg != null)
                            {
                                if (!cfg.isLoad)
                                    continue; // 模组未启用

                                if (!string.IsNullOrEmpty(cfg.dllPath))
                                {
                                    string fullDllPath = Path.GetFullPath(Path.Combine(modSubDir, cfg.dllPath));
                                    if (File.Exists(fullDllPath) && scannedPaths.Add(fullDllPath))
                                    {
                                        result.Add(fullDllPath);
                                        continue;
                                    }
                                }
                            }
                        }

                        // 如果没有 loadConfig 或 dllPath 为空，检索子目录下的主 dll（与目录同名或第一个非依赖 dll）
                        string modDirName = Path.GetFileName(modSubDir);
                        string namedDll = Path.Combine(modSubDir, modDirName + ".dll");
                        if (File.Exists(namedDll) && scannedPaths.Add(namedDll))
                        {
                            result.Add(namedDll);
                            continue;
                        }

                        foreach (string dllFile in Directory.GetFiles(modSubDir, "*.dll"))
                        {
                            string fileName = Path.GetFileName(dllFile);
                            if (fileName.Equals("tContentPatch.dll", StringComparison.OrdinalIgnoreCase) ||
                                fileName.Equals("CommandHelp.dll", StringComparison.OrdinalIgnoreCase) ||
                                fileName.Equals("0Harmony.dll", StringComparison.OrdinalIgnoreCase) ||
                                fileName.Equals("Newtonsoft.Json.dll", StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (scannedPaths.Add(dllFile))
                            {
                                result.Add(dllFile);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Prepatcher] 扫描模组目录异常 {modSubDir}: {ex.Message}");
                    }
                }
            }

            return result;
        }
    }
}
