using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.WorldBuilding;
using TPML.Content;
using TPML.Content.Engine;
using TPML.Content.IO;
using TPML.Core.Logging;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// Player 生命周期补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_Player : ListCopy<PatchPlayer>
    {
        private static List<PatchPlayer> mod = new List<PatchPlayer>();
        internal static List<PatchPlayer> ModList => mod;

        public Patch_Player() : base(mod) { }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var player = typeof(Player);

            // Player.Update(int)
            HookRegistry.Add(GetInstance(player, "Update", typeof(int)),
                (Action<Action<Player, int>, Player, int>)((orig, self, i) =>
                {
                    UpdatePrefix(self, i);
                    orig(self, i);
                    UpdatePostfix(self, i);
                }));

            // Player.UpdateEquips(int)
            HookRegistry.Add(GetInstance(player, "UpdateEquips", typeof(int)),
                (Action<Action<Player, int>, Player, int>)((orig, self, i) =>
                {
                    UpdateEquipsPrefix(self, i);
                    orig(self, i);
                    UpdateEquipsPostfix(self, i);
                }));

            // Player.UpdateArmorSets(int)
            HookRegistry.Add(GetInstance(player, "UpdateArmorSets", typeof(int)),
                (Action<Action<Player, int>, Player, int>)((orig, self, i) =>
                {
                    orig(self, i);
                    UpdateArmorSetsPostfix(self, i);
                }));

            // Player.SavePlayer(PlayerFileData, bool, bool)（静态；原 patch 仅使用前两个参数）
            HookRegistry.Add(GetStatic(player, "SavePlayer", typeof(PlayerFileData), typeof(bool), typeof(bool)),
                (Action<Action<PlayerFileData, bool, bool>, PlayerFileData, bool, bool>)((orig, playerFile, skipMapSave, canBeSkipped) =>
                {
                    SavePlayerPrefix(playerFile, skipMapSave);
                    orig(playerFile, skipMapSave, canBeSkipped);
                    SavePlayerPostfix(playerFile, skipMapSave);
                }));

            // Player.LoadPlayer(string, bool)（静态，返回 PlayerFileData）
            HookRegistry.Add(GetStatic(player, "LoadPlayer", typeof(string), typeof(bool)),
                (Func<Func<string, bool, PlayerFileData>, string, bool, PlayerFileData>)((orig, playerPath, cloudSave) =>
                {
                    PlayerFileData result = orig(playerPath, cloudSave);
                    LoadPlayerPostfix(result);
                    return result;
                }));

            // Player.DropTombstone(long, NetworkText, int)（prefix 返回 bool 跳过原方法）
            HookRegistry.Add(GetInstance(player, "DropTombstone", typeof(long), typeof(NetworkText), typeof(int)),
                (Action<Action<Player, long, NetworkText, int>, Player, long, NetworkText, int>)((orig, self, coinsOwned, deathText, hitDirection) =>
                {
                    if (!CanDropTombstone(self, coinsOwned, deathText, hitDirection)) return;
                    orig(self, coinsOwned, deathText, hitDirection);
                }));

            // Player.AdjTiles()（全量安全接管图格扫描，彻底杜绝天顶/颠倒世界空图格 NRE 与越界中断）
            HookRegistry.Add(GetInstance(player, "AdjTiles"),
                (Action<Action<Player>, Player>)((orig, self) =>
                {
                    AdjTilesPrefix(self);
                }));
        }

        private static MethodInfo GetInstance(Type type, string name, params Type[] types)
        {
            return MethodLookup.Instance(type, name, types);
        }

        private static MethodInfo GetStatic(Type type, string name, params Type[] types)
        {
            return MethodLookup.Static(type, name, types);
        }

        public static void UpdatePrefix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdatePrefix(__instance, i));
        }

        public static void UpdatePostfix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdatePostfix(__instance, i));
        }

        public static void UpdateEquipsPrefix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdateEquipsPrefix(__instance, i));
        }

        public static void UpdateEquipsPostfix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdateEquipsPostfix(__instance, i));
        }

        public static void UpdateArmorSetsPostfix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdateArmorSetsPostfix(__instance, i));
        }

        public static void SavePlayerPrefix(PlayerFileData playerFile, bool skipMapSave)
        {
            if (Main.netMode != 0 && Main.netMode != 1) return;

            if (playerFile?.Player != null)
            {
                ModItemSidecarEngine.OnPlayerSavePrefix(playerFile.Player);
            }

            mod.ForTry(item => item.SavePlayerPrefix(playerFile, skipMapSave));
        }

        public static void SavePlayerPostfix(PlayerFileData playerFile, bool skipMapSave)
        {
            if (Main.netMode != 0 && Main.netMode != 1) return;

            if (playerFile?.Player != null)
            {
                ModItemSidecarEngine.OnPlayerSavePostfix(playerFile.Player);
            }

            mod.ForTry(item => item.SavePlayerPostfix(playerFile, skipMapSave));
        }

        public static void LoadPlayerPostfix(PlayerFileData __result)
        {
            if (__result?.Player != null)
            {
                ModItemSidecarEngine.OnPlayerLoaded(__result.Player);
                PlayerFileData res = __result;
                mod.ForTry(item => item.LoadPlayerPostfix(res));
            }
        }

        internal static bool CanDropTombstone(Player __instance, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            return mod.ForTry(item => item.CanDropTombstone(__instance, coinsOwned, deathText, hitDirection));
        }

        public static bool AdjTilesPrefix(Player __instance)
        {
            if (__instance == null) return false;
            __instance.SafeScanAdjTiles();
            return false; // 安全接管，阻止原版易崩代码执行
        }
    }

    /// <summary>
    /// PlayerFileData 激活生命周期补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_PlayerFileData
    {
        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var pfd = typeof(PlayerFileData);

            // PlayerFileData.SetAsActive()
            HookRegistry.Add(pfd.GetMethod("SetAsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                (Action<Action<PlayerFileData>, PlayerFileData>)((orig, self) =>
                {
                    orig(self);
                    SetAsActivePostfix(self);
                }));
        }

        public static void SetAsActivePostfix(PlayerFileData __instance)
        {
            if (__instance?.Player != null)
            {
                // 激活新角色前，先广播重置并清理上一个角色的扩展容器内存驻留状态
                ModItemSidecarEngine.ResetContainers();

                ModItemSidecarEngine.OnPlayerLoaded(__instance.Player);
                Patch_Player.ModList.ForTry(item => item.SetAsActivePostfix(__instance));
            }
        }
    }
}
