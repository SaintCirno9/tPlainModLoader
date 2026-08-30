using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TPML.Content.IO;

namespace PotionSlots.Core
{
    internal class PotionStoragePlayer : ModPlayer
    {
        public Item lifeSlot = new Item();
        public Item manaSlot = new Item();
        public Item wormholeSlot = new Item();

        private bool wasMapOpen;
        private int temporaryInventorySlot = -1;

        public override void Initialize()
        {
            lifeSlot = new Item();
            manaSlot = new Item();
            wormholeSlot = new Item();
        }

        public override void Load()
        {
            On_Player.QuickHeal_GetItemToUse += PickLifeSlot;
            On_Player.QuickMana_GetItemToUse += PickManaSlot;
        }

        private Item PickLifeSlot(On_Player.orig_QuickHeal_GetItemToUse orig, Player self)
        {
            Item val = self.GetModPlayer<PotionStoragePlayer>()?.lifeSlot;
            if (val != null && !val.IsAir)
            {
                return val;
            }
            return orig.Invoke(self);
        }

        private Item PickManaSlot(On_Player.orig_QuickMana_GetItemToUse orig, Player self)
        {
            Item val = self.GetModPlayer<PotionStoragePlayer>()?.manaSlot;
            if (val != null && !val.IsAir)
            {
                return val;
            }
            return orig.Invoke(self);
        }

        public override void SaveData(TagCompound tag)
        {
            tag.Set("life", lifeSlot);
            tag.Set("mana", manaSlot);
            tag.Set("wormhole", wormholeSlot);
        }

        public override void LoadData(TagCompound tag)
        {
            lifeSlot = tag.Get<Item>("life") ?? new Item();
            manaSlot = tag.Get<Item>("mana") ?? new Item();
            wormholeSlot = tag.Get<Item>("wormhole") ?? new Item();
        }

        public override bool OnPickup(Item item)
        {
            bool flag = false;
            if (item.healLife > 0)
            {
                if (lifeSlot.IsAir || (lifeSlot.type == item.type && lifeSlot.stack < lifeSlot.maxStack))
                {
                    if (lifeSlot.IsAir)
                    {
                        lifeSlot = item.Clone();
                        lifeSlot.stack = 0;
                    }
                    int num = Math.Min(item.stack, lifeSlot.maxStack - lifeSlot.stack);
                    lifeSlot.stack += num;
                    item.stack -= num;
                    flag = true;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                        SoundEngine.PlaySound(SoundID.Grab);
                        return false;
                    }
                }
            }
            else if (item.healMana > 0)
            {
                if (manaSlot.IsAir || (manaSlot.type == item.type && manaSlot.stack < manaSlot.maxStack))
                {
                    if (manaSlot.IsAir)
                    {
                        manaSlot = item.Clone();
                        manaSlot.stack = 0;
                    }
                    int num2 = Math.Min(item.stack, manaSlot.maxStack - manaSlot.stack);
                    manaSlot.stack += num2;
                    item.stack -= num2;
                    flag = true;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                        SoundEngine.PlaySound(SoundID.Grab);
                        return false;
                    }
                }
            }
            else if (item.type == 2997 && (wormholeSlot.IsAir || (wormholeSlot.type == item.type && wormholeSlot.stack < wormholeSlot.maxStack)))
            {
                if (wormholeSlot.IsAir)
                {
                    wormholeSlot = item.Clone();
                    wormholeSlot.stack = 0;
                }
                int num3 = Math.Min(item.stack, wormholeSlot.maxStack - wormholeSlot.stack);
                wormholeSlot.stack += num3;
                item.stack -= num3;
                flag = true;
                if (item.stack <= 0)
                {
                    item.TurnToAir();
                    SoundEngine.PlaySound(SoundID.Grab);
                    return false;
                }
            }

            if (flag)
            {
                SoundEngine.PlaySound(SoundID.Grab);
            }
            return true;
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            if (Player.difficulty == 1)
            {
                DropItem(ref lifeSlot);
                DropItem(ref manaSlot);
                DropItem(ref wormholeSlot);
            }
        }

        private void DropItem(ref Item item)
        {
            if (!item.IsAir)
            {
                Item.NewItem(Player.GetSource_Misc("PlayerDeath"), Player.position, item.type, item.stack);
                item.TurnToAir();
            }
        }

        public override void PostUpdate()
        {
            bool mapFullscreen = Main.mapFullscreen;
            if (mapFullscreen == wasMapOpen)
            {
                return;
            }
            if (mapFullscreen)
            {
                if (!wormholeSlot.IsAir)
                {
                    AddWormholeToInventory();
                }
            }
            else if (temporaryInventorySlot >= 0)
            {
                RemoveWormholeFromInventory();
            }
            wasMapOpen = mapFullscreen;
        }

        private void AddWormholeToInventory()
        {
            for (int i = 0; i < Player.inventory.Length; i++)
            {
                if (Player.inventory[i].IsAir)
                {
                    Player.inventory[i] = wormholeSlot.Clone();
                    wormholeSlot.TurnToAir();
                    temporaryInventorySlot = i;
                    break;
                }
            }
        }

        private void RemoveWormholeFromInventory()
        {
            if (temporaryInventorySlot >= 0 && temporaryInventorySlot < Player.inventory.Length && Player.inventory[temporaryInventorySlot].type == 2997)
            {
                wormholeSlot = Player.inventory[temporaryInventorySlot].Clone();
                Player.inventory[temporaryInventorySlot].TurnToAir();
            }
            temporaryInventorySlot = -1;
        }
    }
}
