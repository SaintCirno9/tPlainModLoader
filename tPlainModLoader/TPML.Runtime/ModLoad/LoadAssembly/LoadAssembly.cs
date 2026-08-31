using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace tContentPatch.ModLoad
{
    internal partial class LoadAssembly
    {
        private readonly IModLoader loadConfig = null;

        private bool isCancel = false;
        private LoadState state = LoadState.None;

        private string stateText = string.Empty;
        private int progressV = 0;
        private int progressMax = 0;


        public LoadAssembly(IModLoader loadConfig)
        {
            if (loadConfig == null) throw new ArgumentNullException(nameof(loadConfig));
            this.loadConfig = loadConfig;
        }

        private void CheckFrontMod(List<ModObject> mos)
        {
            progressV = 0;
            progressMax = mos.Count;
            stateText = "检查前置模组";

            foreach (ModObject mo in mos)
            {
                CheckLoadCancel();

                stateText = $"检查前置模组:{mo.info?.name ?? mo.config.key}";

                ContentPatch.PrintTry($"已加载模组配置:{mo.info?.name ?? mo.config.key}");

                if (mo.config.frontModKeys == null) continue;

                foreach (string key in mo.config.frontModKeys)
                {
                    if (mos.Exists(i => i.config.key == key)) continue;

                    throw new Exception($"模组[{mo.info?.name ?? mo.config.key}]的前置未加载:[{key}]");
                }

                ++progressV;
            }
        }

        private void LoadModAssemblyList(List<ModObject> mos)
        {
            progressV = 0;
            progressMax = mos.Count;
            stateText = "加载程序集";

            foreach (ModObject mo in mos)
            {
                CheckLoadCancel();

                stateText = $"加载程序集:{mo.info?.name ?? mo.config.key}";

                string dllPath = mo.config.dllPath;
                if (dllPath == null) continue;
                string filePath = Path.Combine(mo.modPath, dllPath);

                if (File.Exists(filePath) == false) throw new Exception($"dll文件缺失:[{filePath}]");
                if (mo.config.dllPath == null) mo.config.dllPath = dllPath;

                byte[] asmBytes;
                // 优先从 PrepatcherStorage 获取已被 Prepatcher 修补过的程序集字节流
                if (!Prepatcher.PrepatcherStorage.TryGetPatchedBytes(filePath, out asmBytes))
                {
                    asmBytes = File.ReadAllBytes(filePath);
                }

                mo.assembly = Assembly.Load(asmBytes);

                ++progressV;
                ContentPatch.PrintTry($"已加载程序集:{mo.info?.name ?? mo.config.key}");
            }
        }

        private void CheckLoadCancel()
        {
            if (isCancel) throw new TaskCanceledException();
        }
    }
}
