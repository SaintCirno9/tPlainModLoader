using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.Localization;
using tContentPatch.ModPatch;
using TPML.Content.Engine;
using TPML.Content.IO;
using TPML.Core.Diagnostics;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义玩家 ModPlayer 与 Player 核心生命周期强类型门面调度中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class PlayerLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("PlayerLoader");
        private static bool _hooksInitialized = false;

        /// <summary>
        /// 集中注册所有 Player 相关的强类型 HookGen 钩子（单点入口，杜绝重复拦截）
        /// </summary>
        public static void InitializeHooks()
        {
            if (_hooksInitialized) return;

            On_Player.ResetEffects += Hook_ResetEffects;
            On_Player.Update += Hook_Update;
            On_Player.UpdateEquips += Hook_UpdateEquips;
            On_Player.UpdateArmorSets += Hook_UpdateArmorSets;
            On_Player.SavePlayer += Hook_SavePlayer;
            On_Player.LoadPlayer += Hook_LoadPlayer;
            On_PlayerFileData.SetAsActive += Hook_SetAsActive;
            On_Player.DropTombstone += Hook_DropTombstone;
            On_Player.AdjTiles += Hook_AdjTiles;
            On_Player.KillMe += Hook_KillMe;
            On_Player.GetItem += Hook_GetItem;
            On_Player.ItemCheck_Shoot += Hook_ItemCheck_Shoot;
            On_Player.ItemCheck_StartActualUse += Hook_ItemCheck_StartActualUse;
            On_Player.ItemCheck_CheckCanUse_Inner += Hook_ItemCheck_CheckCanUse_Inner;

            _hooksInitialized = true;
            Logger.Info("PlayerLoader 强类型生命周期钩子全部初始化完成");
        }

        #region Hook Handlers

        private static void Hook_ResetEffects(On_Player.orig_ResetEffects orig, Player self)
        {
            orig(self);
            ResetEffects(self);
        }

        private static void Hook_Update(On_Player.orig_Update orig, Player self, int i)
        {
            if (self == null) return;

            // 确保玩家 adjTile 数组容量满足模组物块需求，防止 UpdateRecipeList 判定配方时越界
            EnsureAdjTileCapacity(self);

            // 分发遗留 PatchPlayer.UpdatePrefix
            tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.UpdatePrefix(self, i));

            // 分发 ModPlayer.PreUpdate
            PreUpdate(self);

            orig(self, i);

            // 保持鼠标物品与 58 槽位无 stack <= 0 幽灵残留（对齐 tML）
            CleanGhostItems(self);

            if (self.HeldItem != null && !self.HeldItem.IsAir)
            {
                ItemLoader.HoldItem(self.HeldItem, self);
            }

            // 防御失焦/无物理按键时的 controlUseItem 残留，保护消耗品手持安全
            if (!Main.mouseLeft && !Main.mouseRight && self.itemAnimation == 0)
            {
                self.controlUseItem = false;
            }

            // 分发遗留 PatchPlayer.UpdatePostfix
            tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.UpdatePostfix(self, i));

            // 分发 ModPlayer.PostUpdate
            PostUpdate(self);
        }

        private static void Hook_UpdateEquips(On_Player.orig_UpdateEquips orig, Player self, int i)
        {
            tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.UpdateEquipsPrefix(self, i));
            orig(self, i);
            tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.UpdateEquipsPostfix(self, i));
            PostUpdateEquips(self);
        }

        private static void Hook_UpdateArmorSets(On_Player.orig_UpdateArmorSets orig, Player self, int i)
        {
            orig(self, i);
            tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.UpdateArmorSetsPostfix(self, i));
        }

        private static void Hook_SavePlayer(On_Player.orig_SavePlayer orig, PlayerFileData playerFile, bool skipMapSave, bool canBeSkipped)
        {
            if (Main.netMode == 0 || Main.netMode == 1)
            {
                if (playerFile?.Player != null)
                {
                    ModItemSidecarEngine.OnPlayerSavePrefix(playerFile.Player, playerFile);
                }
                tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.SavePlayerPrefix(playerFile, skipMapSave));
            }

            orig(playerFile, skipMapSave, canBeSkipped);

            if (Main.netMode == 0 || Main.netMode == 1)
            {
                if (playerFile?.Player != null)
                {
                    ModItemSidecarEngine.OnPlayerSavePostfix(playerFile.Player);
                }
                tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.SavePlayerPostfix(playerFile, skipMapSave));
            }
        }

        private static PlayerFileData Hook_LoadPlayer(On_Player.orig_LoadPlayer orig, string playerPath, bool cloudSave)
        {
            PlayerFileData result = orig(playerPath, cloudSave);
            if (result?.Player != null)
            {
                ModItemSidecarEngine.OnPlayerLoaded(result.Player);
                tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.LoadPlayerPostfix(result));
            }
            return result;
        }

        private static void Hook_SetAsActive(On_PlayerFileData.orig_SetAsActive orig, PlayerFileData self)
        {
            orig(self);
            if (self?.Player != null)
            {
                // 激活新角色前，先广播重置并清理上一个角色的扩展容器内存驻留状态
                ModItemSidecarEngine.ResetContainers();
                ModItemSidecarEngine.OnPlayerLoaded(self.Player);
                tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.SetAsActivePostfix(self));
            }
        }

        private static void Hook_DropTombstone(On_Player.orig_DropTombstone orig, Player self, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            bool canDrop = tContentPatch.ModPatch.Patch_Player.ModList.ForTry(item => item.CanDropTombstone(self, coinsOwned, deathText, hitDirection));
            if (!canDrop) return;
            orig(self, coinsOwned, deathText, hitDirection);
        }

        private static void Hook_AdjTiles(On_Player.orig_AdjTiles orig, Player self)
        {
            if (self == null) return;
            // 全量安全接管图格扫描，彻底杜绝天顶/颠倒世界空图格 NRE 与越界中断
            self.SafeScanAdjTiles();
        }

        private static void Hook_KillMe(On_Player.orig_KillMe orig, Player self, PlayerDeathReason damageSource, double dmg, int hitDirection, bool pvp)
        {
            bool playSound = true;
            bool genDust = true;
            bool continueKill = true;

            var activePlayers = ContentHookDispatcher.ActiveModPlayers;
            for (int idx = 0; idx < activePlayers.Count; idx++)
            {
                var mp = activePlayers[idx];
                mp.Player = self;
                try
                {
                    if (!mp.PreKill(dmg, hitDirection, pvp, ref playSound, ref genDust, ref damageSource))
                    {
                        continueKill = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModPlayer.PreKill 异常: {ex.Message}", ex);
                }
            }

            if (!continueKill) return;

            orig(self, damageSource, dmg, hitDirection, pvp);

            for (int idx = 0; idx < activePlayers.Count; idx++)
            {
                var mp = activePlayers[idx];
                mp.Player = self;
                try
                {
                    mp.Kill(dmg, hitDirection, pvp, damageSource);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModPlayer.Kill 异常: {ex.Message}", ex);
                }
            }
        }

        private static Item Hook_GetItem(On_Player.orig_GetItem orig, Player self, Item newItem, GetItemSettings settings)
        {
            if (self != Main.LocalPlayer || newItem == null || newItem.IsAir)
            {
                return orig(self, newItem, settings);
            }

            var activePlayers = ContentHookDispatcher.ActiveModPlayers;
            for (int idx = 0; idx < activePlayers.Count; idx++)
            {
                var mp = activePlayers[idx];
                mp.Player = self;
                try
                {
                    if (!mp.OnPickup(newItem))
                    {
                        return new Item();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModPlayer.OnPickup 异常: {ex.Message}", ex);
                }
            }

            return orig(self, newItem, settings);
        }

        private static void Hook_ItemCheck_Shoot(On_Player.orig_ItemCheck_Shoot orig, Player player, int i, Item item, int weaponDamage, bool withAudioVisualFeedback)
        {
            if (item != null && !item.IsAir && item.type >= ItemLoader.ModItemOffset)
            {
                return; // 模组物品统一由 StartActualUse 触发发射与扣料，拦截原版避免重复
            }
            orig(player, i, item, weaponDamage, withAudioVisualFeedback);
        }

        private static void Hook_ItemCheck_StartActualUse(On_Player.orig_ItemCheck_StartActualUse orig, Player player, Item item)
        {
            orig(player, item);
            if (item == null || item.IsAir) return;

            ItemLoader.UseItem(item, player);

            // 当物品配置了自定义弹幕发射且属于模组物品时，在开始实际使用时分发 ItemLoader.Shoot
            if (item.shoot > 0 && item.type >= ItemLoader.ModItemOffset && player.whoAmI == Main.myPlayer)
            {
                Vector2 position = player.RotatedRelativePoint(player.MountedCenter, true);
                Vector2 mouseDir = Main.MouseWorld - position;
                Vector2 velocity = mouseDir != Vector2.Zero ? Vector2.Normalize(mouseDir) * (item.shootSpeed > 0 ? item.shootSpeed : 5f) : Vector2.Zero;
                var source = new EntitySource_ItemUse_WithAmmo(player, item, 0);
                int weaponDamage = player.GetWeaponDamage(item);
                bool canShootVanilla = ItemLoader.Shoot(item, player, source, position, velocity, item.shoot, weaponDamage, item.knockBack);
                if (canShootVanilla)
                {
                    Projectile.NewProjectile(source, position, velocity, item.shoot, weaponDamage, item.knockBack, player.whoAmI);
                }
                if (item.consumable && item.useAmmo == 0 && ItemLoader.ConsumeItem(item, player))
                {
                    item.stack--;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                    }
                }
            }
        }

        private static bool Hook_ItemCheck_CheckCanUse_Inner(On_Player.orig_ItemCheck_CheckCanUse_Inner orig, Player player, Item item, bool ignoreCursed)
        {
            // 彻底对齐 tML 官方 CombinedHooks.CanUseItem 规则：空物品或堆叠 <= 0 的幽灵物品直接禁止使用
            if (item == null || item.IsAir || item.stack <= 0 || item.type <= 0)
            {
                return false;
            }

            bool result = orig(player, item, ignoreCursed);
            if (!result || player == null) return result;
            bool? canUse = ItemLoader.CanUseItem(item, player);
            return canUse != false;
        }

        #endregion

        #region Helper Methods

        private static void EnsureAdjTileCapacity(Player player)
        {
            if (player.adjTile == null || player.adjTile.Length <= TileLoader.TileCount)
            {
                int req = Math.Max(TileLoader.TileCount + 64, 800);
                int cur = player.adjTile?.Length ?? 0;
                bool[] newAdj = new bool[Math.Max(req, cur * 2)];
                if (player.adjTile != null)
                {
                    Array.Copy(player.adjTile, newAdj, player.adjTile.Length);
                }
                player.adjTile = newAdj;
            }
        }

        private static void CleanGhostItems(Player player)
        {
            if (Main.mouseItem != null && Main.mouseItem.type > 0 && Main.mouseItem.stack <= 0)
            {
                Main.mouseItem.TurnToAir();
            }
            if (player.inventory != null && player.inventory.Length > 58 && player.inventory[58] != null && player.inventory[58].type > 0 && player.inventory[58].stack <= 0)
            {
                player.inventory[58].TurnToAir();
            }
        }

        public static void ResetEffects(Player player)
        {
            if (player == null) return;
            if (ModPlayerExtensions.TryGetActiveModPlayers(player, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    try
                    {
                        list[i].ResetEffects();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"ModPlayer [{list[i].GetType().FullName}].ResetEffects 异常", ex);
                    }
                }
            }
        }

        public static void PreUpdate(Player player)
        {
            if (player == null) return;
            if (ModPlayerExtensions.TryGetActiveModPlayers(player, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var mp = list[i];
                    try
                    {
                        if (PerformanceProfiler.IsEnabled)
                        {
                            using (PerformanceProfiler.Measure(mp.Mod?.Name ?? mp.GetType().Assembly.GetName().Name, mp.GetType().Name + ".PreUpdate"))
                            {
                                mp.PreUpdate();
                            }
                        }
                        else
                        {
                            mp.PreUpdate();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"ModPlayer [{mp.GetType().FullName}].PreUpdate 异常", ex);
                    }
                }
            }
        }

        public static void PostUpdate(Player player)
        {
            if (player == null) return;
            if (ModPlayerExtensions.TryGetActiveModPlayers(player, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var mp = list[i];
                    try
                    {
                        if (PerformanceProfiler.IsEnabled)
                        {
                            using (PerformanceProfiler.Measure(mp.Mod?.Name ?? mp.GetType().Assembly.GetName().Name, mp.GetType().Name + ".PostUpdate"))
                            {
                                mp.PostUpdate();
                            }
                        }
                        else
                        {
                            mp.PostUpdate();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"ModPlayer [{mp.GetType().FullName}].PostUpdate 异常", ex);
                    }
                }
            }
        }

        public static void PostUpdateEquips(Player player)
        {
            if (player == null) return;
            if (ModPlayerExtensions.TryGetActiveModPlayers(player, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    try
                    {
                        list[i].PostUpdateEquips();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"ModPlayer [{list[i].GetType().FullName}].PostUpdateEquips 异常", ex);
                    }
                }
            }
        }

        #endregion
    }
}
