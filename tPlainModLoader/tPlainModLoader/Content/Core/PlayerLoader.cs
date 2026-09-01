using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using TPML.Content.Engine;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义玩家 ModPlayer 生命周期与钩子分发中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class PlayerLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("PlayerLoader");
        private static bool _hooksInitialized = false;

        public static void InitializeHooks()
        {
            if (_hooksInitialized) return;

            On_Player.ResetEffects += Hook_ResetEffects;
            On_Player.Update += Hook_Update;
            On_Player.UpdateEquips += Hook_UpdateEquips;

            _hooksInitialized = true;
            Logger.Info("PlayerLoader 强类型生命周期钩子初始化完成");
        }

        private static void Hook_ResetEffects(On_Player.orig_ResetEffects orig, Player self)
        {
            orig(self);
            ResetEffects(self);
        }

        private static void Hook_Update(On_Player.orig_Update orig, Player self, int i)
        {
            PreUpdate(self);
            orig(self, i);
            PostUpdate(self);
        }

        private static void Hook_UpdateEquips(On_Player.orig_UpdateEquips orig, Player self, int i)
        {
            orig(self, i);
            PostUpdateEquips(self);
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
                    try
                    {
                        list[i].PreUpdate();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"ModPlayer [{list[i].GetType().FullName}].PreUpdate 异常", ex);
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
                    try
                    {
                        list[i].PostUpdate();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"ModPlayer [{list[i].GetType().FullName}].PostUpdate 异常", ex);
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
    }
}
