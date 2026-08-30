using System;
using System.Linq;
using Mono.Cecil;
using tContentPatch.Prepatcher;
using TPML.Core.Logging;

namespace Instavator
{
    /// <summary>
    /// Item.SetDefaults 早期预修补：禁用 JIT 内联与优化，确保 Harmony 拦截 100% 生效
    /// </summary>
    public class InstavatorItemSetDefaultsPrepatcher : IPrepatcher
    {
        private static readonly ILogger Logger = LogManager.GetLogger("InstavatorPrepatcher");
        public void EarlyPatch(AssemblyDefinition terrariaAssembly)
        {
            try
            {
                var itemType = terrariaAssembly.MainModule.Types.FirstOrDefault(t => t.FullName == "Terraria.Item");
                if (itemType == null) return;

                foreach (var method in itemType.Methods.Where(m => m.Name == "SetDefaults" || m.Name == "netDefaults"))
                {
                    method.NoInlining = true;
                    method.NoOptimization = true;
                }

                Logger.Info("成功预修补 Item.SetDefaults 与 Item.netDefaults (NoInlining)");
            }
            catch (Exception ex)
            {
                Logger.Warn($"预修补异常: {ex.Message}");
            }
        }
    }
}
