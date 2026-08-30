using System;
using System.Collections.Generic;
using System.Reflection;

namespace tContentPatch.Patch
{
    /// <summary>补丁种类（自 Harmony 迁移，等价 HarmonyPatchType 语义）</summary>
    internal enum HarmonyPatchType
    {
        Prefix,
        Postfix
    }

    /// <summary>
    /// IAddPatch 补丁注册后端（M2 迁移：Harmony → MonoMod）。
    /// API 签名保持与迁移前一致，仓库 Mods 使用方无感。
    /// </summary>
    internal static class PatchUtil
    {
        private static readonly Dictionary<string, List<IDisposable>> patchMap = new Dictionary<string, List<IDisposable>>(StringComparer.Ordinal);

        internal static void AddPatch(string patchId, MethodBase original, MethodInfo method, HarmonyPatchType harmonyPatchType)
        {
            if (patchId == null) throw new ArgumentNullException(nameof(patchId));
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (method == null) throw new ArgumentNullException(nameof(method));

            IDisposable hook = harmonyPatchType == HarmonyPatchType.Prefix
                ? HookBinder.CreateHook(original, method, null)
                : HookBinder.CreateHook(original, null, method);

            if (!patchMap.TryGetValue(patchId, out var list))
            {
                list = new List<IDisposable>();
                patchMap[patchId] = list;
            }
            list.Add(hook);
        }

        internal static void AllPatch(string patchId)
        {
            // M2: PatchAll 属性扫描已废弃，引擎补丁改为各补丁类的显式 RegisterAll() 注册；
            // 保留空实现以兼容旧调用链。
            TPML.Core.Logging.LogManager.GetLogger("PatchUtil").Info("[PatchUtil] AllPatch 已由显式注册替代，忽略调用");
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
            return null; // 兼容旧调用：Harmony 实例已不存在
        }
    }
}
