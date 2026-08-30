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

            // Player.AdjTiles()（优先原版与下游补丁链路，异常时安全兜底接管）
            HookRegistry.Add(GetInstance(player, "AdjTiles"),
                (Action<Action<Player>, Player>)((orig, self) =>
                {
                    try
                    {
                        orig(self);
                    }
                    catch (Exception ex)
                    {
                        LogManager.GetLogger("PlayerPatch").Warn($"[AdjTiles] 原版图格扫描异常，启用框架安全兜底: {ex.Message}");
                        AdjTilesPrefix(self);
                    }
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

            try
            {
                if (__instance.adjTile == null || __instance.adjTile.Length < 693)
                {
                    LogManager.GetLogger("PlayerPatch").Warn($"[AdjTiles 诊断] 玩家 {__instance.name}(#{__instance.whoAmI}) 的 adjTile 为 null 或长度不足，正在自动补充初始化...");
                    __instance.adjTile = new bool[693];
                }

                Array.Clear(__instance.adjTile, 0, __instance.adjTile.Length);
                __instance.oldAdjWaterSource = __instance.adjWaterSource;
                __instance.adjWaterSource = false;
                __instance.oldAdjHoney = __instance.adjHoney;
                __instance.adjHoney = false;
                __instance.oldAdjLava = __instance.adjLava;
                __instance.adjLava = false;
                __instance.alchemyTable = false;

                if (Main.tile == null)
                {
                    LogManager.GetLogger("PlayerPatch").Warn($"[AdjTiles 诊断] Main.tile 当前为 null（处于世界加载/过渡态），已安全跳过图格扫描。");
                    return false;
                }

                Rectangle tileRegion = TileReachCheckSettings.Simple.GetTileRegion(__instance, __instance.ateArtisanBread ? 4 : 0);
                tileRegion = WorldUtils.ClampToWorld(tileRegion);

                int nullTileCount = 0;
                for (int x = tileRegion.Left; x <= tileRegion.Right; x++)
                {
                    for (int y = tileRegion.Top; y <= tileRegion.Bottom; y++)
                    {
                        if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) continue;
                        Tile tile = Main.tile[x, y];
                        if (tile == null)
                        {
                            nullTileCount++;
                            continue;
                        }

                        if (tile.active())
                        {
                            __instance.SafeSetAdjTileWithEquivalents(tile.type);
                            if (TileID.Sets.CountsAsWaterForCrafting != null &&
                                tile.type < TileID.Sets.CountsAsWaterForCrafting.Length &&
                                TileID.Sets.CountsAsWaterForCrafting[tile.type])
                            {
                                __instance.adjWaterSource = true;
                            }
                        }
                        if (tile.liquid > 200 && tile.liquidType() == 0)
                        {
                            __instance.adjWaterSource = true;
                        }
                        if (tile.liquid > 200 && tile.liquidType() == 2)
                        {
                            __instance.adjHoney = true;
                        }
                        if (tile.liquid > 200 && tile.liquidType() == 1)
                        {
                            __instance.adjLava = true;
                        }
                    }
                }

                if (nullTileCount > 0)
                {
                    LogManager.GetLogger("PlayerPatch").Warn($"[AdjTiles 诊断] 在玩家周围扫描区域 ({tileRegion.Left},{tileRegion.Top})-({tileRegion.Right},{tileRegion.Bottom}) 发现 {nullTileCount} 个 null 空图格对象，已安全忽略防止闪退。");
                }

                return false; // 安全接管，阻止原版易崩代码执行
            }
            catch (System.Exception ex)
            {
                LogManager.GetLogger("PlayerPatch").Error($"[AdjTiles 诊断] AdjTilesPrefix 发生异常，已拦截防止崩溃", ex);
                return false;
            }
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
