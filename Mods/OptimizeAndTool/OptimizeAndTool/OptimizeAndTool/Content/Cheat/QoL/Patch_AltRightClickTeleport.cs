using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using OptimizeAndTool.Content.Cheat.Function1;
using System;
using TPML;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// Alt + 右键微距精准传送（智能吸附空位，防滑出小角落）
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_AltRightClickTeleport : TPML.Content.ModPlayer
    {
        private static bool _wasRightDown = false;

        public override void UpdatePrefix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer) return;

            bool isRightDown = Main.mouseRight;
            bool isAltDown = Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt);

            if (QoLValSet.altRightClickTeleport.val)
            {
                // 检查 UI 与模态界面状态：全屏大地图、游戏菜单、聊天输入、NPC对话、宝箱、标牌、模态菜单或悬停在 UI 控件上
                bool isBlocked = Main.gameMenu || Main.mapFullscreen || Main.drawingPlayerChat ||
                                 Main.editChest || Main.editSign || Main.ingameOptionsWindow ||
                                 This.mouseInterface;

                if (!isBlocked && isAltDown)
                {
                    // 当 Alt 处于按下状态且按住右键时，拦截普通物品使用与交互
                    if (isRightDown)
                    {
                        This.mouseInterface = true;
                        This.releaseUseItem = false;
                    }

                    // 仅在首次按下的单帧触发传送（通过 _wasRightDown 与 Main.mouseRightRelease 双重防重）
                    if (isRightDown && !_wasRightDown)
                    {
                        Main.mouseRightRelease = false;
                        ExecuteTeleport(This);
                    }
                }
            }

            _wasRightDown = isRightDown;
        }

        private static void ExecuteTeleport(Player player)
        {
            Vector2 mouseWorld = Main.MouseWorld;
            Vector2 desiredPos = mouseWorld - new Vector2(player.width / 2f, player.height / 2f);

            // 智能空位吸附查找
            Vector2 finalPos = FindSmartTeleportPosition(player, desiredPos);

            // 边界约束
            float minX = 0f;
            float maxX = Main.maxTilesX * 16f - player.width;
            float minY = 0f;
            float maxY = Main.maxTilesY * 16f - player.height;
            finalPos.X = MathHelper.Clamp(finalPos.X, minX, maxX);
            finalPos.Y = MathHelper.Clamp(finalPos.Y, minY, maxY);

            // 执行传送
            player.Teleport(finalPos, 0);
            player.position = finalPos;
            player.velocity = Vector2.Zero;
            player.fallStart = player.fallStart2 = (int)(player.position.Y / 16f);

            // 播放传送音效
            SoundEngine.PlaySound(SoundID.Item6, player.Center);

            // 多人模式网络同步
            if (Main.netMode == 1)
            {
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, (float)player.whoAmI, finalPos.X, finalPos.Y, 0, 0);
            }

            // 恢复飞行控制状态
            Function_fly2.fly2_resume = true;
        }

        /// <summary>
        /// 智能寻找最近的无碰撞空旷站立/停留空间
        /// </summary>
        private static Vector2 FindSmartTeleportPosition(Player player, Vector2 desiredPos)
        {
            // 1. 若原始目标点无实心碰撞，直接选用
            if (!Collision.SolidCollision(desiredPos, player.width, player.height))
            {
                return desiredPos;
            }

            // 2. 发生实心物块碰撞，在同心扩散网格内寻找最近可用空隙
            int step = 8; // 半格步长检测，保证精度
            int maxRadius = 160; // 最多向外扩展 10 个图格 (160 像素)
            Vector2 bestPos = desiredPos;
            float bestDistSq = float.MaxValue;
            bool found = false;

            for (int r = step; r <= maxRadius; r += step)
            {
                for (int dx = -r; dx <= r; dx += step)
                {
                    for (int dy = -r; dy <= r; dy += step)
                    {
                        // 仅扫描当前外层边界
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;

                        Vector2 candidate = desiredPos + new Vector2(dx, dy);
                        candidate.X = MathHelper.Clamp(candidate.X, 0f, Main.maxTilesX * 16f - player.width);
                        candidate.Y = MathHelper.Clamp(candidate.Y, 0f, Main.maxTilesY * 16f - player.height);

                        if (!Collision.SolidCollision(candidate, player.width, player.height))
                        {
                            float distSq = (candidate - desiredPos).LengthSquared();
                            if (distSq < bestDistSq)
                            {
                                bestDistSq = distSq;
                                bestPos = candidate;
                                found = true;
                            }
                        }
                    }
                }

                if (found)
                {
                    return bestPos;
                }
            }

            // 若周围全为实心墙，保底传送到鼠标目标点
            return desiredPos;
        }
    }
}
