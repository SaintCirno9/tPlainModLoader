using System.Linq;
using Microsoft.Xna.Framework;
using SuspiciousPlayer.Content.VirtualPlayer;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;

namespace SuspiciousPlayer.Content.Event1
{
    /// <summary>
    /// 虚拟玩家生命周期处理（基于 TPML ModPlayer）
    /// 作者: SaintCirno9
    /// </summary>
    internal class PlayerNPC : TPML.Content.ModPlayer
    {
        public override void UpdatePrefix(Player This, int playerI)
        {
            if (VP.vps?.Contains(This) == false) return;

            if (This.buffType.Contains(137) == false) return;

            This.dead = true;
            if (Main.netMode == 2)
            {
                NetMessage.SendData(107, -1, playerI,
                    NetworkText.FromLiteral($"{This.name}被激怒了"),
                    255, 175, 75, 255,
                    460);

                NetMessage.PlayNetSound(new NetMessage.NetSoundInfo(This.position,
                    341, SoundID.NPCDeath59.Style));
            }
            else
            {
                Main.NewText($"{This.name}被激怒了", 175, 75, 255);
                SoundEngine.PlaySound(SoundID.NPCDeath59);
            }

            Event.Run(This.Center);
        }

        public override bool CanDropTombstone(Player player, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            return VP.vps?.Contains(player) != true;
        }
    }

    /// <summary>
    /// 弹幕命中虚拟玩家 Buff 施加（基于 TPML GlobalProjectile）
    /// </summary>
    public class PlayerNPC_addBuff : TPML.Content.GlobalProjectile
    {
        public override void PostAI(Projectile This)
        {
            if (This.type != 406) return;
            if (This.active == false) return;
            if (Main.netMode != 2) return;
            if (This.ai[0] > 3f == false) return;

            for (int pi = 0; pi < VP.vps?.Count; pi++)
            {
                Player player = VP.vps[pi];

                if (player.whoAmI == This.owner) continue;

                Rectangle rect1 = new Rectangle((int)(This.position.X + This.velocity.X), (int)(This.position.Y + This.velocity.Y),
                    This.width, This.height);
                Rectangle rect2 = new Rectangle((int)player.position.X - 10, (int)player.position.Y - 10,
                    player.width + 20, player.height + 20);

                if (rect1.Intersects(rect2))
                {
                    This.Kill();
                    player.AddBuff(137, 1500, false);
                    NetMessage.SendData(50, number: player.whoAmI);//buff
                }
            }
        }
    }

    /// <summary>
    /// 物品触发与系统初始化（基于 TPML ModSystem）
    /// </summary>
    public class Init : TPML.Content.ModSystem
    {
        public override void Load()
        {
            // 丢物品传送
            On_Item.NewItem_IEntitySource_Vector2_int_int_int_NewItemOwnership_Nullable1_NewItemModifier_bool += (orig, source, pos, type, stack, prefix, ownership, velocity, modifier, noBroadcast) =>
            {
                int res = orig(source, pos, type, stack, prefix, ownership, velocity, modifier, noBroadcast);
                if (type == ItemID.SlimeGun)
                {
                    for (int i = 0; i < VP.vps?.Count; i++)
                    {
                        Player player = VP.vps[i];
                        if (player == null) continue;

                        Vector2 p = new Vector2(pos.X - player.width / 2, pos.Y - player.height / 2);
                        p.X += i * 8;

                        if (p.X < 0) p.X = 0;
                        else if (p.X > Main.maxTilesX * 16) p.X = Main.maxTilesX * 16;
                        if (p.Y < 0) p.Y = 0;
                        else if (p.Y > Main.maxTilesY * 16) p.Y = Main.maxTilesY * 16;

                        player.Center = p;
                        
                        Rectangle location = new Rectangle((int)p.X, (int)p.Y, 0, 0);
                        Color color = Color.Green;
                        string text = $"传送到:{location.X},{location.Y}";

                        if (Main.netMode == 2)
                        {
                            NetMessage.SendData(13, number: player.whoAmI);//控制,属性,位置
                            NetMessage.SendData(MessageID.CombatTextString, text: NetworkText.FromLiteral(text),
                                number: (int)color.PackedValue, number2: location.X, number3: location.Y);
                        }
                        else
                        {
                            CombatText.NewText(location, color, text, false, false);
                        }

                        ContentPatch.PrintTry(text);
                    }
                }
                return res;
            };
        }
    }
}
