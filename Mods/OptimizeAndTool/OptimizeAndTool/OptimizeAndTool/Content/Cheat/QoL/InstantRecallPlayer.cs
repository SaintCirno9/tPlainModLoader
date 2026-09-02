using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// 魔镜 / 回程药水 / 海螺 / 手机瞬传（消除施法前摇延迟）
    /// 作者: SaintCirno9
    /// </summary>
    internal class InstantRecallPlayer : TPML.Content.ModPlayer
    {
        public override void UpdatePrefix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || !QoLValSet.instantRecall.val) return;
            if (This.itemAnimation <= 0 || This.HeldItem == null) return;

            // 仅在开始使用的第一帧触发
            if (This.itemAnimation == This.itemAnimationMax)
            {
                TryExecuteInstantRecall(This, This.HeldItem);
            }
        }

        public static bool TryExecuteInstantRecall(Player player, Item item)
        {
            if (player == null || item == null || !QoLValSet.instantRecall.val || player.itemAnimation <= 0) return false;

            int type = item.type;
            bool handled = false;

            // 魔镜 / 冰雪镜 / 手机 / 贝壳电话(出生点) / 回程药水
            if (type == ItemID.MagicMirror ||
                type == ItemID.IceMirror ||
                type == ItemID.CellPhone ||
                type == ItemID.Shellphone ||
                type == ItemID.ShellphoneSpawn ||
                type == ItemID.RecallPotion)
            {
                player.Spawn(PlayerSpawnContext.RecallFromItem);
                handled = true;
            }
            // 魔法海螺 / 贝壳电话(海洋)
            else if (type == ItemID.MagicConch || type == ItemID.ShellphoneOcean)
            {
                player.MagicConch();
                handled = true;
            }
            // 恶魔海螺 / 贝壳电话(地狱)
            else if (type == ItemID.DemonConch || type == ItemID.ShellphoneHell)
            {
                player.DemonConch();
                handled = true;
            }
            // 返回药水
            else if (type == ItemID.PotionOfReturn)
            {
                player.DoPotionOfReturnTeleportationAndSetTheComebackPoint();
                handled = true;
            }

            if (handled)
            {
                // 如果是消耗性药水则扣减堆叠
                if (item.consumable)
                {
                    item.stack--;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                    }
                }

                // 立即结束前摇动画与使用时间
                player.itemAnimation = 0;
                player.itemTime = 0;

                // 播放传送音效
                SoundEngine.PlaySound(SoundID.Item6, player.Center);
                return true;
            }

            return false;
        }
    }

    // 说明：tModLoader 环境下需 Prefix 拦截 ItemCheck_UseTeleportGateways / ItemCheck_UsePotionOfReturn
    // 防止二次传送；原版无这两个拆分方法，传送分支条件为 itemAnimation > 0 且 itemTime 过半，
    // UpdatePrefix 瞬传后已将 itemAnimation/itemTime 清零，原版分支不会再触发，无需补丁。
}
