using System;
using System.Collections.Generic;
using System.Reflection;
using TPML.Core.Logging;

namespace TPML.Patch
{
    /// <summary>补丁种类（自 Harmony 迁移，等价 HarmonyPatchType 语义）</summary>
    internal enum HarmonyPatchType
    {
        Prefix,
        Postfix
    }

    /// <summary>
    /// 旧 IAddPatch 补丁注册兼容后端门面（HookBinder 已彻底移除，补丁已全量改用显式注册与 MonoMod 门面）。
    /// </summary>
    internal static class PatchUtil
    {
        private static readonly ILogger Logger = LogManager.GetLogger("PatchUtil");
        private static readonly Dictionary<string, List<IDisposable>> patchMap = new Dictionary<string, List<IDisposable>>(StringComparer.Ordinal);

        internal static void AddPatch(string patchId, MethodBase original, MethodInfo method, HarmonyPatchType harmonyPatchType)
        {
            Logger.Warn($"[PatchUtil] AddPatch({patchId}, {original?.Name}) 被调用：IAddPatch 已废弃，请改用显式 MonoMod Hook 或 TerrariaHooks 门面");
        }

        internal static void AllPatch(string patchId)
        {
            Logger.Info("[PatchUtil] AllPatch 已由显式注册替代，忽略调用");
        }

        internal static void AddPatchPrefix(string patchId, MethodBase original, MethodInfo prefix)
        {
            AddPatch(patchId, original, prefix, HarmonyPatchType.Prefix);
        }

        internal static void AddPatchPostfix(string patchId, MethodBase original, MethodInfo postfix)
        {
            AddPatch(patchId, original, postfix, HarmonyPatchType.Postfix);
        }

        internal static void ClearPathc(string patchId)
        {
            if (patchMap.TryGetValue(patchId, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    try { list[i].Dispose(); } catch { }
                }
                list.Clear();
                patchMap.Remove(patchId);
            }
        }

        internal static object GetHarmony(string patchId)
        {
            return null;
        }
    }
}

