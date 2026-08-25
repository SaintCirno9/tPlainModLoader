using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using System.Reflection;
using Terraria;
using Terraria.IO;
using tContentPatch;
using TPMLBridge.GABP;
using TPMLBridge.GABP.Tools;

namespace TPMLBridge
{
    public class TPMLBridgeMod : Mod
    {
        public static bool WorldSaveProtectionEnabled = false;

        public override void Load()
        {
            // 默认关闭保护（玩家正常手动游玩时 100% 正常保存世界与角色）
            // 仅在通过 GABP 自动化测试接口（如 tpml/load_world）接管世界时按需激活保护
            WorldSaveProtectionEnabled = false;

            try
            {
                var harmony = new Harmony("saintcirno9.tpmlbridge.saveprotection");
                var targetMethods = typeof(WorldFile).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                var prefixMethod = new HarmonyMethod(typeof(TPMLBridgeMod).GetMethod(nameof(SaveWorldPrefix), BindingFlags.Public | BindingFlags.Static));

                foreach (var method in targetMethods)
                {
                    if (method.Name == "SaveWorld")
                    {
                        harmony.Patch(method, prefix: prefixMethod);
                    }
                }
            }
            catch (Exception ex)
            {
                Terraria.Main.NewText($"[TPMLBridge] 存档保护补丁挂载失败: {ex.Message}", 255, 100, 100);
            }

            // 启动 GABP TCP Server
            GABPServer.StartFromEnvironment();
        }

        public static bool SaveWorldPrefix(MethodBase __originalMethod)
        {
            if (WorldSaveProtectionEnabled)
            {
                // 彻底拦截写盘保存，保护玩家正常游玩的世界数据
                return false;
            }
            return true;
        }

        public override void Unload()
        {
            GABPServer.Instance?.Stop();
        }
    }

    public class TPMLBridgePatchMain : PatchMain
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
