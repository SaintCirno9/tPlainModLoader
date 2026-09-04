using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace OptimizeAndTool.Content.QoL.GuaranteedDrop
{
    /// <summary>
    /// 全场景全物品 100% 必定全量大爆门控（基于 HookGen 强类型 On_ 门控）：
    /// 1. 拦截 ItemDropResolver.ResolveRule（怪物/Boss 掉落池）：只要满足条件 100% 必定掉落最大数量，多选一池全量大爆；
    /// 2. 拦截 Player.OpenBossBag（专家/大师 Boss 宝藏袋）：专属战利品永久全量大爆；
    /// 3. 拦截 Player.OpenFishingCrate 及各摸奖袋/锁盒：宝匣与专属战利品永久全量大爆。
    /// 作者: SaintCirno9
    /// </summary>
    public static class GuaranteedDropHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_ItemDropResolver.ResolveRule += Hook_ResolveRule;
            On_Player.OpenBossBag += Hook_OpenBossBag;
            On_Player.OpenFishingCrate += Hook_OpenFishingCrate;
            On_Player.OpenCanofWorms += Hook_OpenCanofWorms;
            On_Player.OpenOyster += Hook_OpenOyster;
            On_Player.OpenLockBox += Hook_OpenLockBox;
            On_Player.OpenShadowLockbox += Hook_OpenShadowLockbox;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_ItemDropResolver.ResolveRule -= Hook_ResolveRule;
            On_Player.OpenBossBag -= Hook_OpenBossBag;
            On_Player.OpenFishingCrate -= Hook_OpenFishingCrate;
            On_Player.OpenCanofWorms -= Hook_OpenCanofWorms;
            On_Player.OpenOyster -= Hook_OpenOyster;
            On_Player.OpenLockBox -= Hook_OpenLockBox;
            On_Player.OpenShadowLockbox -= Hook_OpenShadowLockbox;
            _registered = false;
        }

        #region 1. 怪物掉落规则底层拦截 (ItemDropResolver)

        private static readonly HashSet<IItemDropRule> _visitingRules = new HashSet<IItemDropRule>();
        private static int _recursionDepth = 0;
        private const int MaxRecursionDepth = 32;

        private static ItemDropAttemptResult Hook_ResolveRule(On_ItemDropResolver.orig_ResolveRule orig, ItemDropResolver self, IItemDropRule rule, DropAttemptInfo info)
        {
            if (!GuaranteedDropSystem.EnableGuaranteedDrop.val || rule == null || info.player == null || info.npc == null || info.IsInSimulation)
            {
                return orig(self, rule, info);
            }

            // 必须满足前置条件（如肉山后、夜晚、日食、特定事件等），不破坏游戏进程机制
            if (!rule.CanDrop(info))
            {
                return orig(self, rule, info);
            }

            // 防环与递归深度保护
            if (_recursionDepth >= MaxRecursionDepth || _visitingRules.Contains(rule))
            {
                return new ItemDropAttemptResult
                {
                    State = ItemDropAttemptResultState.DidNotRunCode
                };
            }

            _visitingRules.Add(rule);
            _recursionDepth++;

            try
            {
                // A. 单物品普通掉落规则 (CommonDrop 及其派生类)
                if (rule is CommonDrop commonDrop)
                {
                    int itemId = commonDrop.itemId;
                    if (itemId > 0)
                    {
                        int stack = commonDrop.amountDroppedMaximum;
                        if (stack <= 0) stack = 1;
                        CommonCode.DropItemFromNPC(info.npc, itemId, stack);

                        var result = new ItemDropAttemptResult
                        {
                            State = ItemDropAttemptResultState.Success
                        };
                        CustomResolveRuleChains(self, rule, info, result);
                        return result;
                    }
                }
                // B. 多选一掉落规则 (OneFromOptionsDropRule)
                else if (rule is OneFromOptionsDropRule optionsRule)
                {
                    int[] options = optionsRule.dropIds;
                    if (options != null && options.Length > 0)
                    {
                        if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                        {
                            // 全量大爆特爆：掉出该池中所有物品
                            for (int i = 0; i < options.Length; i++)
                            {
                                int id = options[i];
                                if (id > 0)
                                {
                                    CommonCode.DropItemFromNPC(info.npc, id, 1);
                                }
                            }
                        }
                        else
                        {
                            int chosen = options[info.rng.Next(options.Length)];
                            if (chosen > 0)
                            {
                                CommonCode.DropItemFromNPC(info.npc, chosen, 1);
                            }
                        }

                        var result = new ItemDropAttemptResult
                        {
                            State = ItemDropAttemptResultState.Success
                        };
                        CustomResolveRuleChains(self, rule, info, result);
                        return result;
                    }
                }
                // C. 多选一不随幸运缩放规则 (OneFromOptionsNotScaledWithLuckDropRule)
                else if (rule is OneFromOptionsNotScaledWithLuckDropRule optionsNotLuckRule)
                {
                    int[] options = optionsNotLuckRule.dropIds;
                    if (options != null && options.Length > 0)
                    {
                        if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                        {
                            for (int i = 0; i < options.Length; i++)
                            {
                                int id = options[i];
                                if (id > 0)
                                {
                                    CommonCode.DropItemFromNPC(info.npc, id, 1);
                                }
                            }
                        }
                        else
                        {
                            int chosen = options[info.rng.Next(options.Length)];
                            if (chosen > 0)
                            {
                                CommonCode.DropItemFromNPC(info.npc, chosen, 1);
                            }
                        }

                        var result = new ItemDropAttemptResult
                        {
                            State = ItemDropAttemptResultState.Success
                        };
                        CustomResolveRuleChains(self, rule, info, result);
                        return result;
                    }
                }
                // D. 不重复多选规则 (FromOptionsWithoutRepeatsDropRule)
                else if (rule is FromOptionsWithoutRepeatsDropRule withoutRepeatsRule)
                {
                    int[] options = withoutRepeatsRule.dropIds;
                    if (options != null && options.Length > 0)
                    {
                        if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                        {
                            for (int i = 0; i < options.Length; i++)
                            {
                                int id = options[i];
                                if (id > 0)
                                {
                                    CommonCode.DropItemFromNPC(info.npc, id, 1);
                                }
                            }
                        }
                        else
                        {
                            int chosen = options[info.rng.Next(options.Length)];
                            if (chosen > 0)
                            {
                                CommonCode.DropItemFromNPC(info.npc, chosen, 1);
                            }
                        }

                        var result = new ItemDropAttemptResult
                        {
                            State = ItemDropAttemptResultState.Success
                        };
                        CustomResolveRuleChains(self, rule, info, result);
                        return result;
                    }
                }
                // E. 规则池多选一规则 (OneFromRulesRule)
                else if (rule is OneFromRulesRule oneFromRules)
                {
                    IItemDropRule[] options = oneFromRules.options;
                    if (options != null && options.Length > 0)
                    {
                        if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                        {
                            // 全量大爆特爆：触发该规则池中的所有规则
                            for (int i = 0; i < options.Length; i++)
                            {
                                var subRule = options[i];
                                if (subRule != null)
                                {
                                    self.ResolveRule(subRule, info);
                                }
                            }
                        }
                        else
                        {
                            var chosen = options[info.rng.Next(options.Length)];
                            if (chosen != null)
                            {
                                self.ResolveRule(chosen, info);
                            }
                        }

                        var result = new ItemDropAttemptResult
                        {
                            State = ItemDropAttemptResultState.Success
                        };
                        CustomResolveRuleChains(self, rule, info, result);
                        return result;
                    }
                }
                // F. 批量多散落掉落规则 (DropOneByOne)
                else if (rule is DropOneByOne dropOneByOne)
                {
                    int itemId = dropOneByOne.itemId;
                    if (itemId > 0)
                    {
                        int count = dropOneByOne.parameters.MaximumItemDropsCount;
                        int activePlayersCount = Main.CurrentFrameFlags.ActivePlayersCount;
                        int maxStack = dropOneByOne.parameters.MaximumStackPerChunkBase + activePlayersCount * dropOneByOne.parameters.BonusMaxDropsPerChunkPerPlayer;
                        if (maxStack <= 0) maxStack = 1;
                        for (int i = 0; i < count; i++)
                        {
                            CommonCode.DropItemFromNPC(info.npc, itemId, maxStack, scattered: true);
                        }

                        var result = new ItemDropAttemptResult
                        {
                            State = ItemDropAttemptResultState.Success
                        };
                        CustomResolveRuleChains(self, rule, info, result);
                        return result;
                    }
                }
                // G. 机械三王召唤物掉落规则 (MechBossSpawnersDropRule)
                else if (rule is MechBossSpawnersDropRule)
                {
                    if (!NPC.downedMechBoss1)
                    {
                        CommonCode.DropItemFromNPC(info.npc, ItemID.MechanicalWorm, 1);
                    }
                    if (!NPC.downedMechBoss2)
                    {
                        CommonCode.DropItemFromNPC(info.npc, ItemID.MechanicalEye, 1);
                    }
                    if (!NPC.downedMechBoss3)
                    {
                        CommonCode.DropItemFromNPC(info.npc, ItemID.MechanicalSkull, 1);
                    }

                    var result = new ItemDropAttemptResult
                    {
                        State = ItemDropAttemptResultState.Success
                    };
                    CustomResolveRuleChains(self, rule, info, result);
                    return result;
                }

                // 其他嵌套规则继续由原版派发（会自动递归调用 ResolveRule）
                return orig(self, rule, info);
            }
            finally
            {
                _recursionDepth--;
                _visitingRules.Remove(rule);
            }
        }

        /// <summary>
        /// 自定义链条派发器：支持全量大爆穿透 OnFailedRoll（TryIfFailedRandomRoll）链条
        /// </summary>
        private static void CustomResolveRuleChains(ItemDropResolver self, IItemDropRule rule, DropAttemptInfo info, ItemDropAttemptResult parentResult)
        {
            var chains = rule.ChainedRules;
            if (chains == null || chains.Count == 0) return;

            for (int i = 0; i < chains.Count; i++)
            {
                var chain = chains[i];
                if (chain == null || chain.RuleToChain == null) continue;

                bool shouldExecute = false;

                // 1. 遇到随机失败链（如不死矿工的衣服/裤子/炸弹，鲨鱼鳍，食人鱼抓钩等）：
                //    全量大爆模式下强制穿透！
                if (chain is Chains.TryIfFailedRandomRoll)
                {
                    shouldExecute = true;
                }
                // 2. 遇到成功链：只要父级成功（在我们的 Hook 拦截中，大爆基本都是 Success）
                else if (chain is Chains.TryIfSucceeded)
                {
                    shouldExecute = parentResult.State == ItemDropAttemptResultState.Success;
                }
                // 3. 遇到条件不满足链：只有条件不满足时才链入
                else if (chain is Chains.TryIfDoesntFillConditions)
                {
                    shouldExecute = parentResult.State == ItemDropAttemptResultState.DoesntFillConditions;
                }
                // 4. 其他自定义链条类型（兜底判定）
                else
                {
                    shouldExecute = chain.CanChainIntoRule(parentResult);
                }

                if (shouldExecute)
                {
                    self.ResolveRule(chain.RuleToChain, info);
                }
            }
        }

        #endregion

        #region 2. 困难模式开发者套装与 Boss 宝藏袋永久全量大爆 (Player.OpenBossBag)

        /// <summary>
        /// 21 套完整的困难模式开发者时装套装定义（头饰/面具、上衣、裤子/裙子、翅膀、专属附带饰品/染料）
        /// </summary>
        private static readonly int[][] DeveloperSets = new int[][]
        {
            // 1. Red's set
            new int[] { ItemID.RedsHelmet, ItemID.RedsBreastplate, ItemID.RedsLeggings, ItemID.RedsWings, ItemID.RedsYoyo },
            // 2. Cenx's set
            new int[] { ItemID.CenxsTiara, ItemID.CenxsBreastplate, ItemID.CenxsLeggings, ItemID.CenxsWings },
            // 3. Cenx's dress set
            new int[] { ItemID.CenxsTiara, ItemID.CenxsDress, ItemID.CenxsDressPants, ItemID.CenxsWings },
            // 4. Crowno's set
            new int[] { ItemID.CrownosMask, ItemID.CrownosBreastplate, ItemID.CrownosLeggings, ItemID.CrownosWings },
            // 5. Will's set
            new int[] { ItemID.WillsHelmet, ItemID.WillsBreastplate, ItemID.WillsLeggings, ItemID.WillsWings },
            // 6. Jim's set
            new int[] { ItemID.JimsHelmet, ItemID.JimsBreastplate, ItemID.JimsLeggings, ItemID.JimsWings },
            // 7. Aaron's set
            new int[] { ItemID.AaronsHelmet, ItemID.AaronsBreastplate, ItemID.AaronsLeggings },
            // 8. D-Town's set
            new int[] { ItemID.DTownsHelmet, ItemID.DTownsBreastplate, ItemID.DTownsLeggings, ItemID.DTownsWings },
            // 9. Lazure's set
            new int[] { ItemID.BejeweledValkyrieHead, ItemID.BejeweledValkyrieBody, ItemID.BejeweledValkyrieWing, ItemID.ValkyrieYoyo },
            // 10. Yoraiz0r's set
            new int[] { ItemID.Yoraiz0rHead, ItemID.Yoraiz0rShirt, ItemID.Yoraiz0rPants, ItemID.Yoraiz0rWings, ItemID.Yoraiz0rDarkness },
            // 11. Skiphs' set
            new int[] { ItemID.SkiphsHelm, ItemID.SkiphsShirt, ItemID.SkiphsPants, ItemID.SkiphsWings, ItemID.DevDye },
            // 12. Loki's set
            new int[] { ItemID.LokisHelm, ItemID.LokisShirt, ItemID.LokisPants, ItemID.LokisWings, ItemID.LokisDye },
            // 13. Arkhalis's set
            new int[] { ItemID.Arkhalis, ItemID.ArkhalisHat, ItemID.ArkhalisShirt, ItemID.ArkhalisPants, ItemID.ArkhalisWings },
            // 14. Leinfors' set
            new int[] { ItemID.LeinforsHat, ItemID.LeinforsShirt, ItemID.LeinforsPants, ItemID.LeinforsWings, ItemID.LeinforsAccessory },
            // 15. Ghostar's set
            new int[] { ItemID.GhostarSkullPin, ItemID.GhostarShirt, ItemID.GhostarPants, ItemID.GhostarsWings },
            // 16. Safeman's set
            new int[] { ItemID.SafemanSunHair, ItemID.SafemanSunDress, ItemID.SafemanDressLeggings, ItemID.SafemanWings },
            // 17. FoodBarbarian's set
            new int[] { ItemID.FoodBarbarianHelm, ItemID.FoodBarbarianArmor, ItemID.FoodBarbarianGreaves, ItemID.FoodBarbarianWings },
            // 18. Grox The Great's set
            new int[] { ItemID.GroxTheGreatHelm, ItemID.GroxTheGreatArmor, ItemID.GroxTheGreatGreaves, ItemID.GroxTheGreatWings },
            // 19. ChickenBones' set
            new int[] { ItemID.ChickenBonesHead, ItemID.ChickenBonesBody, ItemID.ChickenBonesLegs, ItemID.ChickenBonesWings, ItemID.ChickenBonesRobe },
            // 20. Kazzymodus's set
            new int[] { ItemID.KazzymodusHood, ItemID.KazzymodusChestpiece, ItemID.KazzymodusLeggings, ItemID.KazzymodusWings },
            // 21. Luna's set
            new int[] { ItemID.LunasHead, ItemID.LunasBody, ItemID.LunasLegs, ItemID.LunasWings, ItemID.LunasCloak }
        };

        /// <summary>
        /// 困难模式（肉山后）Boss 宝藏袋集合
        /// </summary>
        private static readonly HashSet<int> HardmodeBossBags = new HashSet<int>
        {
            ItemID.TwinsBossBag,
            ItemID.DestroyerBossBag,
            ItemID.SkeletronPrimeBossBag,
            ItemID.PlanteraBossBag,
            ItemID.GolemBossBag,
            ItemID.FishronBossBag,
            ItemID.FairyQueenBossBag,
            ItemID.QueenSlimeBossBag,
            ItemID.MoonLordBossBag
        };

        /// <summary>
        /// 17 个原版 Boss 宝藏袋全部专属武器、专家饰品、坐骑、宠物、工具、面具与稀有战利品全集
        /// </summary>
        private static readonly Dictionary<int, int[]> BossBagLootTable = new Dictionary<int, int[]>
        {
            // 史莱姆王 (3318)
            {
                ItemID.KingSlimeBossBag,
                new int[]
                {
                    ItemID.RoyalGel,
                    ItemID.KingSlimeMask,
                    ItemID.SlimySaddle,
                    ItemID.NinjaHood,
                    ItemID.NinjaShirt,
                    ItemID.NinjaPants,
                    ItemID.SlimeHook,
                    ItemID.Solidifier,
                    ItemID.SlimeGun
                }
            },
            // 克苏鲁之眼 (3319)
            {
                ItemID.EyeOfCthulhuBossBag,
                new int[]
                {
                    ItemID.EoCShield,
                    ItemID.EyeMask,
                    ItemID.Binoculars,
                    ItemID.DemoniteOre,
                    ItemID.CrimtaneOre,
                    ItemID.UnholyArrow,
                    ItemID.CorruptSeeds,
                    ItemID.CrimsonSeeds
                }
            },
            // 世界吞噬怪 (3320)
            {
                ItemID.EaterOfWorldsBossBag,
                new int[]
                {
                    ItemID.WormScarf,
                    ItemID.EaterMask,
                    ItemID.EatersBone,
                    ItemID.DemoniteOre,
                    ItemID.ShadowScale
                }
            },
            // 克苏鲁之脑 (3321)
            {
                ItemID.BrainOfCthulhuBossBag,
                new int[]
                {
                    ItemID.BrainOfConfusion,
                    ItemID.BrainMask,
                    ItemID.BoneRattle,
                    ItemID.CrimtaneOre,
                    ItemID.TissueSample
                }
            },
            // 蜂王 (3322)
            {
                ItemID.QueenBeeBossBag,
                new int[]
                {
                    ItemID.HiveBackpack,
                    ItemID.BeeMask,
                    ItemID.HoneyedGoggles,
                    ItemID.HiveWand,
                    ItemID.BeeKeeper,
                    ItemID.BeesKnees,
                    ItemID.BeeGun,
                    ItemID.HoneyComb,
                    ItemID.BeeHat,
                    ItemID.BeeShirt,
                    ItemID.BeePants,
                    ItemID.Beenade,
                    ItemID.BeeWax,
                    ItemID.QueenOfBees
                }
            },
            // 独眼巨鹿 (5111)
            {
                ItemID.DeerclopsBossBag,
                new int[]
                {
                    ItemID.BoneHelm,
                    ItemID.DeerclopsMask,
                    ItemID.Eyebrella,
                    ItemID.ChesterPetItem,
                    ItemID.DontStarveShaderItem,
                    ItemID.HamBat,
                    ItemID.LucyTheAxe,
                    ItemID.PewMaticHorn,
                    ItemID.WeatherPain,
                    ItemID.HoundiusShootius,
                    ItemID.DizzyHat
                }
            },
            // 骷髅王 (3323)
            {
                ItemID.SkeletronBossBag,
                new int[]
                {
                    ItemID.BoneGlove,
                    ItemID.SkeletronMask,
                    ItemID.SkeletronHand,
                    ItemID.BookofSkulls,
                    ItemID.ChippysCouch
                }
            },
            // 血肉墙 (3324)
            {
                ItemID.WallOfFleshBossBag,
                new int[]
                {
                    ItemID.DemonHeart,
                    ItemID.FleshMask,
                    ItemID.Pwnhammer,
                    ItemID.WarriorEmblem,
                    ItemID.RangerEmblem,
                    ItemID.SorcererEmblem,
                    ItemID.SummonerEmblem,
                    ItemID.BreakerBlade,
                    ItemID.ClockworkAssaultRifle,
                    ItemID.LaserRifle,
                    ItemID.FireWhip,
                    ItemID.WallOfFleshGoatMountItem
                }
            },
            // 史莱姆皇后 (4957)
            {
                ItemID.QueenSlimeBossBag,
                new int[]
                {
                    ItemID.VolatileGelatin,
                    ItemID.QueenSlimeMask,
                    ItemID.QueenSlimeMountSaddle,
                    ItemID.Smolstar,
                    ItemID.QueenSlimeHook,
                    ItemID.CrystalNinjaHelmet,
                    ItemID.CrystalNinjaChestplate,
                    ItemID.CrystalNinjaLeggings,
                    ItemID.GelBalloon
                }
            },
            // 双子魔眼 (3326)
            {
                ItemID.TwinsBossBag,
                new int[]
                {
                    ItemID.MechanicalWheelPiece,
                    ItemID.TwinMask,
                    ItemID.SoulofSight,
                    ItemID.HallowedBar
                }
            },
            // 毁灭者 (3325)
            {
                ItemID.DestroyerBossBag,
                new int[]
                {
                    ItemID.MechanicalWagonPiece,
                    ItemID.DestroyerMask,
                    ItemID.SoulofMight,
                    ItemID.HallowedBar
                }
            },
            // 机械骷髅王 (3327)
            {
                ItemID.SkeletronPrimeBossBag,
                new int[]
                {
                    ItemID.MechanicalBatteryPiece,
                    ItemID.SkeletronPrimeMask,
                    ItemID.SoulofFright,
                    ItemID.HallowedBar
                }
            },
            // 世纪之花 (3328)
            {
                ItemID.PlanteraBossBag,
                new int[]
                {
                    ItemID.SporeSac,
                    ItemID.PlanteraMask,
                    ItemID.TempleKey,
                    ItemID.Seedling,
                    ItemID.TheAxe,
                    ItemID.PygmyStaff,
                    ItemID.ThornHook,
                    ItemID.GrenadeLauncher,
                    ItemID.RocketI,
                    ItemID.VenusMagnum,
                    ItemID.NettleBurst,
                    ItemID.LeafBlower,
                    ItemID.FlowerPow,
                    ItemID.WaspGun,
                    ItemID.Seedler,
                    ItemID.FlowerWhip
                }
            },
            // 石巨人 (3329)
            {
                ItemID.GolemBossBag,
                new int[]
                {
                    ItemID.ShinyStone,
                    ItemID.GolemMask,
                    ItemID.SunStone,
                    ItemID.EyeoftheGolem,
                    ItemID.Picksaw,
                    ItemID.PossessedHatchet,
                    ItemID.Stynger,
                    ItemID.StyngerBolt,
                    ItemID.HeatRay,
                    ItemID.StaffofEarth,
                    ItemID.GolemFist,
                    ItemID.BeetleHusk,
                    ItemID.MobiusStrip
                }
            },
            // 猪鲨公爵 (3330)
            {
                ItemID.FishronBossBag,
                new int[]
                {
                    ItemID.ShrimpyTruffle,
                    ItemID.DukeFishronMask,
                    ItemID.FishronWings,
                    ItemID.TempestStaff,
                    ItemID.RazorbladeTyphoon,
                    ItemID.BubbleGun,
                    ItemID.Tsunami,
                    ItemID.Flairon,
                    ItemID.FlaironFlail,
                    ItemID.EelWhip,
                    ItemID.Kraken
                }
            },
            // 光之女皇 (4782)
            {
                ItemID.FairyQueenBossBag,
                new int[]
                {
                    ItemID.EmpressFlightBooster,
                    ItemID.FairyQueenMask,
                    ItemID.RainbowWings,
                    ItemID.SparkleGuitar,
                    ItemID.HallowBossDye,
                    ItemID.RainbowCursor,
                    ItemID.PiercingStarlight,
                    ItemID.FairyQueenMagicItem,
                    ItemID.FairyQueenRangedItem,
                    ItemID.RainbowWhip
                }
            },
            // 月球领主 (3332)
            {
                ItemID.MoonLordBossBag,
                new int[]
                {
                    ItemID.GravityGlobe,
                    ItemID.SuspiciousLookingTentacle,
                    ItemID.PortalGun,
                    ItemID.BossMaskMoonlord,
                    ItemID.LunarOre,
                    ItemID.Meowmere,
                    ItemID.StarWrath,
                    ItemID.Terrarian,
                    ItemID.SDMG,
                    ItemID.Celeb2,
                    ItemID.LastPrism,
                    ItemID.LunarFlareBook,
                    ItemID.RainbowCrystalStaff,
                    ItemID.MoonlordTurretStaff,
                    ItemID.MoonLordWhip,
                    ItemID.MeowmereMinecart,
                    ItemID.LongRainbowTrailWings
                }
            }
        };

        private static void Hook_OpenBossBag(On_Player.orig_OpenBossBag orig, Player self, int type)
        {
            if (GuaranteedDropSystem.EnableGuaranteedDrop.val && self != null && self == Main.LocalPlayer)
            {
                IEntitySource source = self.GetItemSource_OpenItem(type);

                // 1. 永久全量掉落该 Boss 宝藏袋池中的所有可能物品
                if (BossBagLootTable.TryGetValue(type, out int[] potentialLoot) && potentialLoot != null)
                {
                    for (int i = 0; i < potentialLoot.Length; i++)
                    {
                        int itemId = potentialLoot[i];
                        if (itemId > 0 && itemId < ItemID.Count)
                        {
                            self.QuickSpawnItem(source, itemId, 1);
                        }
                    }
                }

                // 2. 若属于困难模式（肉后）Boss 宝藏袋，每次必定额外掉落 1 套随机完整的开发者套装
                if (HardmodeBossBags.Contains(type) && DeveloperSets.Length > 0)
                {
                    int setIndex = Main.rand.Next(DeveloperSets.Length);
                    int[] chosenSet = DeveloperSets[setIndex];
                    if (chosenSet != null)
                    {
                        for (int i = 0; i < chosenSet.Length; i++)
                        {
                            int devItemId = chosenSet[i];
                            if (devItemId > 0 && devItemId < ItemID.Count)
                            {
                                self.QuickSpawnItem(source, devItemId, 1);
                            }
                        }
                    }
                }
            }

            orig(self, type);
        }

        #endregion

        #region 3. 钓鱼宝匣、摸奖包与锁盒永久全量大爆 (Player.OpenFishingCrate / OpenCanofWorms / OpenLockBox 等)

        private static readonly Dictionary<int, int[]> CrateExclusiveLootTable = new Dictionary<int, int[]>
        {
            // 木匣 / 珍珠木匣 (2334, 3979)
            { ItemID.WoodenCrate, new int[] { ItemID.SailfishBoots, ItemID.TsunamiInABottle, ItemID.Aglet, ItemID.Radar, ItemID.ClimbingClaws, ItemID.CordageGuide, ItemID.Sundial, ItemID.Anchor } },
            { ItemID.WoodenCrateHard, new int[] { ItemID.SailfishBoots, ItemID.TsunamiInABottle, ItemID.Aglet, ItemID.Radar, ItemID.ClimbingClaws, ItemID.CordageGuide, ItemID.Sundial, ItemID.Anchor } },
            // 铁匣 / 秘银匣 (2335, 3980)
            { ItemID.IronCrate, new int[] { ItemID.FalconBlade, ItemID.TartarSauce, ItemID.SailfishBoots, ItemID.TsunamiInABottle, ItemID.Sundial } },
            { ItemID.IronCrateHard, new int[] { ItemID.FalconBlade, ItemID.TartarSauce, ItemID.SailfishBoots, ItemID.TsunamiInABottle, ItemID.Sundial } },
            // 金匣 / 钛金匣 (2336, 3981)
            { ItemID.GoldenCrate, new int[] { ItemID.LifeformAnalyzer, ItemID.Sundial, ItemID.HardySaddle } },
            { ItemID.GoldenCrateHard, new int[] { ItemID.LifeformAnalyzer, ItemID.Sundial, ItemID.HardySaddle } },
            // 地牢匣 / 围栏匣 (3203, 3982)
            { ItemID.DungeonFishingCrate, new int[] { ItemID.GoldenKey, ItemID.LockBox } },
            { ItemID.DungeonFishingCrateHard, new int[] { ItemID.GoldenKey, ItemID.LockBox } },
            // 天空匣 / 天蓝匣 (3206, 3985)
            { ItemID.FloatingIslandFishingCrate, new int[] { ItemID.Starfury, ItemID.LuckyHorseshoe, ItemID.ShinyRedBalloon, ItemID.CreativeWings, ItemID.CelestialMagnet } },
            { ItemID.FloatingIslandFishingCrateHard, new int[] { ItemID.Starfury, ItemID.LuckyHorseshoe, ItemID.ShinyRedBalloon, ItemID.CreativeWings, ItemID.CelestialMagnet } },
            // 丛林匣 / 荆棘匣 (3208, 3987)
            { ItemID.JungleFishingCrate, new int[] { ItemID.FeralClaws, ItemID.AnkletoftheWind, ItemID.StaffofRegrowth, ItemID.Boomstick, ItemID.Seaweed, ItemID.FlowerBoots, ItemID.FiberglassFishingPole } },
            { ItemID.JungleFishingCrateHard, new int[] { ItemID.FeralClaws, ItemID.AnkletoftheWind, ItemID.StaffofRegrowth, ItemID.Boomstick, ItemID.Seaweed, ItemID.FlowerBoots, ItemID.FiberglassFishingPole } },
            // 腐化匣 / 污损匣 (3204, 3983)
            { ItemID.CorruptFishingCrate, new int[] { ItemID.BallOHurt, ItemID.BandofStarpower, ItemID.Musket, ItemID.ShadowOrb, ItemID.Vilethorn } },
            { ItemID.CorruptFishingCrateHard, new int[] { ItemID.BallOHurt, ItemID.BandofStarpower, ItemID.Musket, ItemID.ShadowOrb, ItemID.Vilethorn } },
            // 猩红匣 / 血匣 (3207, 3986)
            { ItemID.CrimsonFishingCrate, new int[] { ItemID.TheUndertaker, ItemID.TheMeatball, ItemID.TheRottedFork, ItemID.PanicNecklace, ItemID.CrimsonRod } },
            { ItemID.CrimsonFishingCrateHard, new int[] { ItemID.TheUndertaker, ItemID.TheMeatball, ItemID.TheRottedFork, ItemID.PanicNecklace, ItemID.CrimsonRod } },
            // 神圣匣 / 圣灵匣 (3205, 3984)
            { ItemID.HallowedFishingCrate, new int[] { ItemID.SoulofLight, ItemID.CrystalShard, ItemID.BlessedApple, ItemID.Sundial } },
            { ItemID.HallowedFishingCrateHard, new int[] { ItemID.SoulofLight, ItemID.CrystalShard, ItemID.BlessedApple, ItemID.Sundial } },
            // 冰冻匣 / 极寒匣 (3209, 3988)
            { ItemID.FrozenCrate, new int[] { ItemID.IceBoomerang, ItemID.IceBlade, ItemID.IceSkates, ItemID.SnowballCannon, ItemID.BlizzardinaBottle, ItemID.FlurryBoots, ItemID.Fish } },
            { ItemID.FrozenCrateHard, new int[] { ItemID.IceBoomerang, ItemID.IceBlade, ItemID.IceSkates, ItemID.SnowballCannon, ItemID.BlizzardinaBottle, ItemID.FlurryBoots, ItemID.Fish } },
            // 绿洲匣 / 海市蜃楼匣 (4442, 4443)
            { ItemID.OasisCrate, new int[] { ItemID.SandstorminaBottle, ItemID.FlyingCarpet, ItemID.AncientChisel, ItemID.SandBoots, ItemID.CatBast, ItemID.EncumberingStone } },
            { ItemID.OasisCrateHard, new int[] { ItemID.SandstorminaBottle, ItemID.FlyingCarpet, ItemID.AncientChisel, ItemID.SandBoots, ItemID.CatBast, ItemID.EncumberingStone } },
            // 海洋匣 / 渊海匣 (4444, 4445)
            { ItemID.OceanCrate, new int[] { ItemID.BreathingReed, ItemID.Flipper, ItemID.WaterWalkingBoots, ItemID.Trident, ItemID.SharkToothNecklace, ItemID.FloatingTube } },
            { ItemID.OceanCrateHard, new int[] { ItemID.BreathingReed, ItemID.Flipper, ItemID.WaterWalkingBoots, ItemID.Trident, ItemID.SharkToothNecklace, ItemID.FloatingTube } }
        };

        private static void Hook_OpenFishingCrate(On_Player.orig_OpenFishingCrate orig, Player self, int crateItemID)
        {
            if (GuaranteedDropSystem.EnableGuaranteedDrop.val && self != null && self == Main.LocalPlayer)
            {
                if (CrateExclusiveLootTable.TryGetValue(crateItemID, out int[] potentialLoot) && potentialLoot != null)
                {
                    IEntitySource source = self.GetItemSource_OpenItem(crateItemID);
                    for (int i = 0; i < potentialLoot.Length; i++)
                    {
                        int itemId = potentialLoot[i];
                        if (itemId > 0 && itemId < ItemID.Count)
                        {
                            self.QuickSpawnItem(source, itemId, 1);
                        }
                    }
                }
            }

            orig(self, crateItemID);
        }

        private static void Hook_OpenCanofWorms(On_Player.orig_OpenCanofWorms orig, Player self, int sourceItemType)
        {
            if (GuaranteedDropSystem.EnableGuaranteedDrop.val && self != null && self == Main.LocalPlayer)
            {
                IEntitySource source = self.GetItemSource_OpenItem(sourceItemType);
                int[] worms = { ItemID.GoldWorm, ItemID.EnchantedNightcrawler, ItemID.Worm };
                for (int i = 0; i < worms.Length; i++)
                {
                    self.QuickSpawnItem(source, worms[i], 1);
                }
            }

            orig(self, sourceItemType);
        }

        private static void Hook_OpenOyster(On_Player.orig_OpenOyster orig, Player self, int sourceItemType)
        {
            if (GuaranteedDropSystem.EnableGuaranteedDrop.val && self != null && self == Main.LocalPlayer)
            {
                IEntitySource source = self.GetItemSource_OpenItem(sourceItemType);
                int[] pearls = { ItemID.WhitePearl, ItemID.BlackPearl, ItemID.PinkPearl };
                for (int i = 0; i < pearls.Length; i++)
                {
                    self.QuickSpawnItem(source, pearls[i], 1);
                }
            }

            orig(self, sourceItemType);
        }

        private static void Hook_OpenLockBox(On_Player.orig_OpenLockBox orig, Player self, int lockboxItemType)
        {
            if (GuaranteedDropSystem.EnableGuaranteedDrop.val && self != null && self == Main.LocalPlayer)
            {
                IEntitySource source = self.GetItemSource_OpenItem(lockboxItemType);
                int[] dungeonWeapons = { ItemID.Muramasa, ItemID.CobaltShield, ItemID.AquaScepter, ItemID.BlueMoon, ItemID.MagicMissile, ItemID.Handgun, ItemID.ShadowKey };
                for (int i = 0; i < dungeonWeapons.Length; i++)
                {
                    self.QuickSpawnItem(source, dungeonWeapons[i], 1);
                }
            }

            orig(self, lockboxItemType);
        }

        private static void Hook_OpenShadowLockbox(On_Player.orig_OpenShadowLockbox orig, Player self, int boxType)
        {
            if (GuaranteedDropSystem.EnableGuaranteedDrop.val && self != null && self == Main.LocalPlayer)
            {
                IEntitySource source = self.GetItemSource_OpenItem(boxType);
                int[] hellWeapons = { ItemID.DarkLance, ItemID.Sunfury, ItemID.FlowerofFire, ItemID.Flamelash, ItemID.HellwingBow };
                for (int i = 0; i < hellWeapons.Length; i++)
                {
                    self.QuickSpawnItem(source, hellWeapons[i], 1);
                }
            }

            orig(self, boxType);
        }

        #endregion
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    public static class Patch_GuaranteedDrop
    {
    }
}
