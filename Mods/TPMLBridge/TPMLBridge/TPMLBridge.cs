using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.IO;
using tContentPatch;
using TPML.Core.Logging;
using TPMLBridge.GABP;
using TPMLBridge.GABP.Tools;

namespace TPMLBridge
{
    public class TPMLBridgeMod : Mod
    {
        private static readonly ILogger Logger = LogManager.GetLogger("TPMLBridge");
        public static volatile bool WorldSaveProtectionEnabled = false;
        public static volatile bool PlayerSaveProtectionEnabled = false;
        private static bool _hooksRegistered = false;

        public override void Load()
        {
            // 默认关闭保护（玩家正常手动游玩时 100% 正常保存世界与角色）
            // 仅在通过 GABP 自动化测试接口（如 tpml/load_world）接管世界时按需激活保护
            WorldSaveProtectionEnabled = false;
            PlayerSaveProtectionEnabled = false;

            RegisterHooks();

            // 启动 GABP TCP Server
            GABPServer.StartFromEnvironment();
        }

        public static void RegisterHooks()
        {
            if (_hooksRegistered) return;
            On_WorldFile.SaveWorld += Hook_SaveWorld;
            On_WorldFile._SaveWorld += Hook__SaveWorld;
            On_Player.SavePlayer += Hook_SavePlayer;
            _hooksRegistered = true;
            Logger.Info("★ TPMLBridge 存档保护 MonoMod On_ 门控已就绪");
        }

        public static void ResetSaveProtection()
        {
            WorldSaveProtectionEnabled = false;
            PlayerSaveProtectionEnabled = false;
        }

        public static void UnregisterHooks()
        {
            if (!_hooksRegistered) return;
            On_WorldFile.SaveWorld -= Hook_SaveWorld;
            On_WorldFile._SaveWorld -= Hook__SaveWorld;
            On_Player.SavePlayer -= Hook_SavePlayer;
            _hooksRegistered = false;
        }

        private static void Hook_SavePlayer(On_Player.orig_SavePlayer orig, PlayerFileData playerFile, bool skipMapSave, bool canBeSkipped)
        {
            if (PlayerSaveProtectionEnabled || WorldSaveProtectionEnabled)
            {
                return;
            }
            orig(playerFile, skipMapSave, canBeSkipped);
        }

        private static void Hook_SaveWorld(On_WorldFile.orig_SaveWorld orig, bool resetTime, bool useTemps, bool canBeSkipped)
        {
            if (WorldSaveProtectionEnabled)
            {
                // 彻底拦截写盘保存，保护玩家正常游玩的世界数据
                return;
            }
            orig(resetTime, useTemps, canBeSkipped);
        }

        private static void Hook__SaveWorld(On_WorldFile.orig__SaveWorld orig, bool useCloudSaving, bool resetTime, bool useTemps, bool canBeSkipped)
        {
            if (WorldSaveProtectionEnabled)
            {
                // 彻底拦截写盘保存，保护玩家正常游玩的世界数据
                return;
            }
            orig(useCloudSaving, resetTime, useTemps, canBeSkipped);
        }

        public override void Unload()
        {
            UnregisterHooks();
            GABPServer.Instance?.Stop();
        }
    }

    public class TPMLBridgeMain : PatchMain
    {
        public override void UpdatePrefix(GameTime gameTime)
        {
            // 如果存在长按/通道武器持续按住的任务
            if (TerrariaTools.PendingHoldUseFrames > 0 && !Main.gameMenu && Main.LocalPlayer != null)
            {
                TerrariaTools.PendingHoldUseFrames--;
                Main.LocalPlayer.controlUseItem = true;
                if (TerrariaTools.PendingHoldAlt)
                {
                    Main.LocalPlayer.altFunctionUse = 1;
                }
            }
            else if (TerrariaTools.PendingHoldUseFrames == 0 && !Main.gameMenu && Main.LocalPlayer != null)
            {
                // 无长按任务且物理未按下时，主动回收 controlUseItem 防止泄漏误用消耗品
                if (!Main.mouseLeft && !Main.mouseRight)
                {
                    Main.LocalPlayer.controlUseItem = false;
                }
            }

            // 每帧分发执行主线程队列中的任务
            MainThreadQueue.Update();
        }
    }
}
