using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using tContentPatch.Utils;

namespace tContentPatch.ModLoad
{
    internal partial class LoadConfig : IModLoader
    {
        public readonly string ConfigDirectory = null;

        private bool isCancel = false;
        private LoadState state = LoadState.None;

        private string stateText = string.Empty;
        private int progressV = 0;
        private int progressMax = 0;


        public LoadConfig(string configDirectory) { ConfigDirectory = configDirectory; }


        private List<ModObject> LoadModConfigList(string modsRootPath)
        {
            if (Directory.Exists(modsRootPath) == false) return new List<ModObject>();

            DirectoryInfo[] dis = new DirectoryInfo(modsRootPath).GetDirectories();
            FileInfo[] tmodFiles = new DirectoryInfo(modsRootPath).GetFiles("*.tmod");

            progressV = 0;
            progressMax = dis.Length + tmodFiles.Length;
            stateText = "加载模组配置";

            List<ModObject> mos = new List<ModObject>();

            foreach (DirectoryInfo di in dis)
            {
                CheckLoadCancel();

                bool addProgress = true;

                try
                {
                    if (di == null) continue;

                    stateText = $"加载模组配置:{di.Name}";

                    string filePath = Path.Combine(di.FullName, InfoList.Files.ModLoadConfig);

                    ModConfig config = LoadModConfig(filePath);

                    if (config == null) continue;
                    if (config.key == null) continue;
                    //未启用的也存着

                    mos.Add(new ModObject(config) { modPath = di.FullName });
                }
                catch
                {
                    addProgress = false;
                }
                finally
                {
                    if (addProgress) ++progressV;
                }
            }

            // 扫描并解析 .tmod 单文件模组
            foreach (FileInfo tmodFile in tmodFiles)
            {
                CheckLoadCancel();
                bool addProgress = true;

                try
                {
                    if (tmodFile == null) continue;
                    stateText = $"解析 .tmod 模组:{tmodFile.Name}";

                    var container = Terraria.ModLoader.Container.TModContainerReader.Read(tmodFile.FullName);
                    if (container != null)
                    {
                        var config = new ModConfig
                        {
                            key = container.ModName,
                            dllPath = $"{container.ModName}.dll",
                            isEnable = true
                        };

                        var info = new ModInfo
                        {
                            name = container.ModName,
                            author = "tModLoader 模组",
                            description = $"[v{container.ModVersion}] 通过 tModLoader Shim 兼容层直接载入\nTML 版本: {container.TmlVersion}"
                        };

                        var mo = new ModObject(config)
                        {
                            modPath = Path.GetDirectoryName(tmodFile.FullName),
                            tmodContainer = container,
                            info = info
                        };

                        mos.Add(mo);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Add($"解析 .tmod [{tmodFile.Name}] 失败: {ex}");
                    addProgress = false;
                }
                finally
                {
                    if (addProgress) ++progressV;
                }
            }

            // 从用户文档目录 (Documents/My Games/Terraria/tPlainModLoader/enabled.json) 读取启用状态
            try
            {
                string enabledFilePath = Path.Combine(ContentPatch.UserSaveDirectory ?? string.Empty, InfoList.Files.EnabledJson);
                List<string> enabledKeys = null;
                if (File.Exists(enabledFilePath))
                {
                    try
                    {
                        enabledKeys = MyJson1.Get2<List<string>>(enabledFilePath);
                    }
                    catch { }
                }

                if (enabledKeys == null)
                {
                    // 首次运行或 enabled.json 不存在时，默认全量启用所有发现的模组并写入 enabled.json
                    enabledKeys = new List<string>();
                    foreach (ModObject mo in mos)
                    {
                        if (!string.IsNullOrEmpty(mo.config?.key))
                        {
                            enabledKeys.Add(mo.config.key);
                        }
                    }
                    if (!string.IsNullOrEmpty(ContentPatch.UserSaveDirectory))
                    {
                        MyJson1.Save(enabledKeys, enabledFilePath);
                    }
                }

                HashSet<string> enabledSet = new HashSet<string>(enabledKeys);
                foreach (ModObject mo in mos)
                {
                    if (mo.config != null)
                    {
                        mo.config.isEnable = enabledSet.Contains(mo.config.key);
                    }
                }
            }
            catch { }

            return mos;
        }

        private void LoadModInfo(List<ModObject> mos)
        {
            foreach (ModObject mo in mos)
            {
                try
                {
                    string filePath = Path.Combine(mo.modPath, InfoList.Files.ModInfo);
                    if (File.Exists(filePath) == false) continue;
                    ModInfo mi = MyJson1.Get2<ModInfo>(filePath);
                    mo.info = mi;
                }
                catch { }
            }
        }

        private ModConfig LoadModConfig(string filePath)
        {
            ConfigHelp<ModConfig> config = new ConfigHelp<ModConfig>(filePath);
            config.UpdateConfig();

            return config.config;
        }

        private void CheckLoadCancel()
        {
            if (isCancel) throw new TaskCanceledException();
        }
    }
}
