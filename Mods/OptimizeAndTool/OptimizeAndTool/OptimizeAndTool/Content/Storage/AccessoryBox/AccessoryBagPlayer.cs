using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋玩家属性挂载、外观渲染与垃圾桶防误丢生命周期处理
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagPlayer : PatchPlayer
    {
        public override void UpdateEquipsPostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || Main.dedServ) return;
            if (!AccessoryBagConfig.EnablePassive.val) return;

            AccessoryBagDuplicateCleaner.CheckAndCleanDuplicates(This);

            var bags = AccessoryBagCacheManager.GetAllBags();
            if (bags == null || bags.Count == 0) return;

            var equippedTypes = new HashSet<int>();
            if (AccessoryBagConfig.PreventPlayerBagDuplicates.val && This.armor != null)
            {
                for (int i = 3; i < This.armor.Length; i++)
                {
                    if (This.armor[i] != null && !This.armor[i].IsAir)
                    {
                        equippedTypes.Add(This.armor[i].type);
                    }
                }
            }

            var seenInBags = new HashSet<int>();

            foreach (var bag in bags)
            {
                if (bag?.personalInventory == null) continue;

                int limit = bag.personalInventory.Length;
                if (AccessoryBagConfig.EnableEffectiveSlotsLimit.val)
                {
                    limit = Math.Min(limit, AccessoryBagConfig.EffectiveSlots.val);
                }

                for (int i = 0; i < limit; i++)
                {
                    Item it = bag.personalInventory[i];
                    if (it == null || it.IsAir || it.type <= ItemID.None) continue;

                    if (AccessoryBagConfig.PreventPlayerBagDuplicates.val && equippedTypes.Contains(it.type))
                        continue;

                    if (AccessoryBagConfig.PreventBagDuplicates.val)
                    {
                        if (seenInBags.Contains(it.type)) continue;
                        seenInBags.Add(it.type);
                    }

                    if (AccessoryBagConfig.AllowPrefixRoll.val && (it.accessory || it.prefix > 0))
                    {
                        This.GrantPrefixBenefits(it);
                    }

                    if (AccessoryBagConfig.ApplyBaseStats.val)
                    {
                        This.GrantArmorBenefits(it);
                    }

                    if (AccessoryBagConfig.EnableAccessoryEffects.val)
                    {
                        This.ApplyEquipFunctional(3, it);
                    }

                    // 外观渲染
                    bool hidden = bag.hideVisuals != null && i < bag.hideVisuals.Length && bag.hideVisuals[i];
                    if (!hidden)
                    {
                        if (it.wingSlot > 0)
                        {
                            if (This.velocity.Y != 0f && This.mount.CanUseWings)
                            {
                                This.wings = it.wingSlot;
                            }
                            This.wingsLogic = it.wingSlot;
                        }
                        if (it.backSlot > 0) This.back = (sbyte)it.backSlot;
                        if (it.shieldSlot > 0) This.shield = (sbyte)it.shieldSlot;
                        if (it.shoeSlot > 0) This.shoe = (sbyte)it.shoeSlot;
                        if (it.waistSlot > 0) This.waist = (sbyte)it.waistSlot;
                        if (it.handOnSlot > 0) This.handon = (sbyte)it.handOnSlot;
                        if (it.handOffSlot > 0) This.handoff = (sbyte)it.handOffSlot;
                        if (it.neckSlot > 0) This.neck = (sbyte)it.neckSlot;
                        if (it.faceSlot > 0) This.face = (sbyte)it.faceSlot;
                        if (it.balloonSlot > 0) This.balloon = (sbyte)it.balloonSlot;
                    }
                }
            }
        }

        public override void UpdatePostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || Main.dedServ || Main.gameMenu) return;

            // 垃圾桶误丢保护：若拖入垃圾桶，自动弹出饰品并重置空包
            Item trash = This.trashItem;
            if (trash != null && !trash.IsAir)
            {
                AccessoryBagItem bag = ItemLoader.GetModItem(trash) as AccessoryBagItem;
                if (bag != null && !bag.IsEmpty())
                {
                    bag.DropAllItems(This);
                    bag.ResetBagData();
                    Main.NewText("[饰品袋] 饰品袋已被清空，内部饰品已安全掉落至脚下。", Color.Orange);
                }
            }
        }
    }
}
