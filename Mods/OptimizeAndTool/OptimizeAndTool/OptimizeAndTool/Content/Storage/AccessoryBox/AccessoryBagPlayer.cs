using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TPML;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;
using TPML.Core.Logging;
using OptimizeAndTool.Content.Storage.Core;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋玩家属性挂载、外观渲染与垃圾桶防误丢生命周期处理
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagPlayer : TPML.Content.ModPlayer
    {
        public override void UpdateEquipsPrefix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || Main.dedServ) return;
            if (!AccessoryBagConfig.EnablePassive.val) return;

            AccessoryBagDuplicateCleaner.CheckAndCleanDuplicates(This);

            var bags = CarriedBagCacheManager.GetAllAccessoryBags();
            if (bags == null || bags.Count == 0) return;

            var equippedTypes = new HashSet<int>();
            if (AccessoryBagConfig.PreventPlayerBagDuplicates.val && This.armor != null)
            {
                // 仅扫描 0..9 号有效穿戴槽位，绝不把 10..19 号时装饰品槽位误判为重复装备
                int maxEffectiveArmor = Math.Min(10, This.armor.Length);
                for (int i = 0; i < maxEffectiveArmor; i++)
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

                    if (AccessoryBagConfig.PreventPlayerBagDuplicates.val && equippedTypes.Contains(it.type))
                        continue;

                    if (AccessoryBagConfig.PreventBagDuplicates.val)
                    {
                        if (seenInBags.Contains(it.type)) continue;
                        seenInBags.Add(it.type);
                    }

                    // 1. 词缀属性生效 (+4 防御、暴击、伤害、移速等)
                    if (AccessoryBagConfig.AllowPrefixRoll.val && (it.accessory || it.prefix > 0))
                    {
                        This.GrantPrefixBenefits(it);
                    }

                    // 2. 基础防具/装备属性生效 (+防御、+生命恢复等)
                    if (AccessoryBagConfig.ApplyBaseStats.val)
                    {
                        This.GrantArmorBenefits(it);
                        ItemLoader.UpdateEquip(it, This);
                    }

                    // 3. 饰品功能生效 (激活 skyStoneEffects, manaFlower, chiselSpeed, dd2Accessory, buffImmune, noKnockback 等)
                    if (it.accessory && AccessoryBagConfig.EnableAccessoryEffects.val)
                    {
                        This.ApplyEquipFunctional(3, it);
                    }

                    // 4. 信息类与机械类饰品激活 (PDA, 手机, 表, 深度计, 罗盘, 雷达, 生命体分析仪, DPS 计, 秒表, 金属探测器等)
                    This.RefreshInfoAccsFromItemType(it.type);
                    This.RefreshMechanicalAccsFromItemType(it.type);

                    // 逻辑翅膀飞行能力不受外观隐藏影响，必须在原版后续派生计算前写入
                    if (it.wingSlot > 0)
                    {
                        This.wingsLogic = it.wingSlot;
                    }
                }
            }
        }

        public override void UpdateEquipsPostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || Main.dedServ) return;
            if (!AccessoryBagConfig.EnablePassive.val) return;

            var bags = CarriedBagCacheManager.GetAllAccessoryBags();
            if (bags == null || bags.Count == 0) return;

            var equippedTypes = new HashSet<int>();
            if (AccessoryBagConfig.PreventPlayerBagDuplicates.val && This.armor != null)
            {
                // 仅扫描 0..9 号有效穿戴槽位，绝不把 10..19 号时装饰品槽位误判为重复装备
                int maxEffectiveArmor = Math.Min(10, This.armor.Length);
                for (int i = 0; i < maxEffectiveArmor; i++)
                {
                    if (This.armor[i] != null && !This.armor[i].IsAir)
                    {
                        equippedTypes.Add(This.armor[i].type);
                    }
                }
            }

            var seenInBags = new HashSet<int>();

            bool hasTileSpeed = false;
            bool hasWallSpeed = false;
            bool hasTileRange = false;

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

                    // 仅重扫建筑类饰品标志与外观，避免把 Prefix 已应用的属性再次计算
                    if (it.accessory && AccessoryBagConfig.EnableAccessoryEffects.val)
                    {
                        if (it.type == ItemID.ArchitectGizmoPack || it.type == ItemID.HandOfCreation || it.type == ItemID.BrickLayer)
                        {
                            hasTileSpeed = true;
                        }
                        if (it.type == ItemID.ArchitectGizmoPack || it.type == ItemID.HandOfCreation || it.type == ItemID.PortableCementMixer)
                        {
                            hasWallSpeed = true;
                        }
                        if (it.type == ItemID.ArchitectGizmoPack || it.type == ItemID.HandOfCreation || it.type == ItemID.ExtendoGrip || it.type == ItemID.Toolbelt || it.type == ItemID.Toolbox)
                        {
                            hasTileRange = true;
                        }
                    }

                    // 外观渲染：受 hideVisuals 控制
                    bool hidden = bag.hideVisuals != null && i < bag.hideVisuals.Length && bag.hideVisuals[i];
                    if (!hidden)
                    {
                        if (it.wingSlot > 0)
                        {
                            if (This.velocity.Y != 0f && This.mount.CanUseWings)
                            {
                                This.wings = it.wingSlot;
                            }
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

            // 补全建筑放置速度与范围属性
            if (hasTileSpeed)
            {
                int createTile = This.inventory[This.selectedItem].createTile;
                if (createTile >= 0 && !TileID.Sets.Torches[createTile])
                {
                    This.tileSpeed += 0.5f;
                }
            }
            if (hasWallSpeed)
            {
                This.wallSpeed += 0.5f;
            }
            if (hasTileRange && This.whoAmI == Main.myPlayer)
            {
                Player.tileRangeX += 3;
                Player.tileRangeY += 2;
            }
        }

        public override void UpdateArmorSetsPostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || Main.dedServ) return;
            if (!AccessoryBagConfig.EnablePassive.val || !AccessoryBagConfig.EnableArmorSetBonuses.val) return;

            var bags = CarriedBagCacheManager.GetAllAccessoryBags();
            if (bags == null || bags.Count == 0) return;

            // 收集所有可用装备类型：包括身上穿的防具与饰品袋内的防具
            var availableArmorTypes = new HashSet<int>();
            if (This.armor != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (This.armor[i] != null && !This.armor[i].IsAir)
                    {
                        availableArmorTypes.Add(This.armor[i].type);
                    }
                }
            }

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
                    if (it != null && !it.IsAir && it.type > ItemID.None)
                    {
                        availableArmorTypes.Add(it.type);
                    }
                }
            }

            if (availableArmorTypes.Count == 0) return;

            // 遍历所有原版套装规则
            var allSets = Terraria.DataStructures.ArmorSetBonuses.All;
            if (allSets != null)
            {
                for (int s = 0; s < allSets.Count; s++)
                {
                    var set = allSets[s];
                    if (set == null) continue;

                    bool headOk = set.Head == 0 || availableArmorTypes.Contains(set.Head);
                    bool bodyOk = set.Body == 0 || availableArmorTypes.Contains(set.Body);
                    bool legsOk = set.Legs == 0 || availableArmorTypes.Contains(set.Legs);

                    if (headOk && bodyOk && legsOk)
                    {
                        // 若玩家身上穿戴的正好是该套装，原版 UpdateArmorSets 已经触发过，此处避免重复调用
                        bool wornOnBody = (set.Head == 0 || (This.armor[0] != null && This.armor[0].type == set.Head)) &&
                                          (set.Body == 0 || (This.armor[1] != null && This.armor[1].type == set.Body)) &&
                                          (set.Legs == 0 || (This.armor[2] != null && This.armor[2].type == set.Legs));

                        if (!wornOnBody)
                        {
                            set.Effect(This);
                        }
                    }
                }
            }

            // 针对有常驻计时器/状态的特殊套装进行后处理刷新
            This.UpdateArmorSets_Always_Beetle();
            This.UpdateArmorSets_Always_Solar();
            This.UpdateArmorSets_Always_Stardust();
            This.UpdateArmorSets_Always_Chlorophyte();
            This.UpdateArmorSets_Always_Vortex();
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
