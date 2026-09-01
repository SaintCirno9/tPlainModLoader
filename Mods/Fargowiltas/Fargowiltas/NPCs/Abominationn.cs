using System.Collections.Generic;
using Fargowiltas.Items.Summons.Deviantt;
using Fargowiltas.Items.Summons.Abom;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;
using static TPML.Content.ModContent;
using Terraria.Audio;
using Fargowiltas.Items.Vanity;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;
using Fargowiltas.Items.Tiles;
using Fargowiltas.Common.Configs;
using Fargowiltas.Content.Biomes;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;

namespace Fargowiltas.NPCs
{
    [AutoloadHead]
    public class Abominationn : ModNPC
    {
        private bool canSayDefeatQuote = true;
        private bool canSayMutantShimmerQuote = false;
        private int defeatQuoteTimer = 900;

        private static int ShimmerHeadIndex;
        private static Profiles.StackedNPCProfile AbomProfile;

        public override void Load()
        {
            ShimmerHeadIndex = Mod.AddNPCHeadTexture(Type, Texture + "_Shimmer_Head");
        }

        public override ITownNPCProfile TownNPCProfile()
        {
            return AbomProfile;
        }

        public override void SetStaticDefaults()
        {

            Main.npcFrameCount[Type] = 25;
            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 700;
            NPCID.Sets.AttackType[Type] = 0;
            NPCID.Sets.AttackTime[Type] = 90;
            NPCID.Sets.AttackAverageChance[Type] = 30;
            NPCID.Sets.HatOffsetY[Type] = 2;

            NPCID.Sets.ShimmerTownTransform[Type] = true; // This set says that the Town NPC has a Shimmered form. Otherwise, the Town NPC will become transparent when touching Shimmer like other enemies.

            NPCID.Sets.ShimmerTownTransform[Type] = true; // Allows for this NPC to have a different texture after touching the Shimmer liquid.

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Velocity = -1f,
                Direction = -1
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            Happiness.SetBiomeAffection<SkyBiome>(AffectionLevel.Love);
            Happiness.SetBiomeAffection<OceanBiome>(AffectionLevel.Like);
            Happiness.SetBiomeAffection<DungeonBiome>(AffectionLevel.Dislike);

            Happiness.SetNPCAffection<Mutant>(AffectionLevel.Love);
            Happiness.SetNPCAffection<Deviantt>(AffectionLevel.Like);
            Happiness.SetNPCAffection(NPCID.Nurse, AffectionLevel.Hate);

            FargoUtils.AddDebuffImmunities(Type, new List<int>()
            {
                 BuffID.Suffocation
            });

            // profile
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
                new FlavorTextBestiaryInfoElement("Mods.Fargowiltas.Bestiary.Abominationn")
            });
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 40;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 10;
            NPC.defense = NPC.downedMoonlord ? 50 : 15;
            NPC.lifeMax = NPC.downedMoonlord ? 5000 : Main.hardMode ? 1000 : 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.Guide;
            NPC.buffImmune[BuffID.Suffocation] = true;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)/* tModPorter Suggestion: Copy the implementation of NPC.SpawnAllowed_Merchant in vanilla if you to count money, and be sure to set a flag when unlocked, so you don't count every tick. */
        {
            /*
            if (Fargowiltas.ModLoaded["FargowiltasSouls"] && ((bool)0 || (bool)0))
            {
                return false;
            }
            */
            return FargoServerConfig.Instance.Abom && NPC.downedGoblins && !FargoGlobalNPC.AnyBossAlive();
        }

        public override bool CanGoToStatue(bool toKingStatue) => toKingStatue;

        public override void AI()
        {
            NPC.breath = 200;
            if (defeatQuoteTimer > 0)
                defeatQuoteTimer--;
            else
                canSayDefeatQuote = false;
            int mutant = NPC.FindFirstNPC(ModContent.NPCType<Mutant>());
            if (mutant != -1)
            {
                if (!Main.npc[mutant].IsShimmerVariant)
                {
                    canSayMutantShimmerQuote = true;
                }
            }
        }

        public override List<string> SetNPCNameList()
        {
            string[] names = ["Wilta", "Jack", "Harley", "Reaper", "Stevenn", "Doof", "Baroo", "Fergus", "Entev", "Catastrophe", "Bardo", "Betson"];

            return new List<string>(names);
        }

        public override string GetChat()
        {
            /*
            if (NPC.homeless && canSayDefeatQuote && Fargowiltas.ModLoaded["FargowiltasSouls"] && (bool)0)
            {
                canSayDefeatQuote = false;
                return AbomChat("Defeat");
            }
            */

            int mutant = NPC.FindFirstNPC(ModContent.NPCType<Mutant>());
            if (mutant != -1)
            {
                if (Main.npc[mutant].IsShimmerVariant)
                {
                    if (canSayMutantShimmerQuote)
                    {
                        canSayMutantShimmerQuote = false;
                        return AbomChat("MutantShimmer");
                    }

                }
            }

            /*
            if (Fargowiltas.ModLoaded["FargowiltasSouls"] && Main.rand.NextBool(3))
            {
                if ((bool)0)
                    return AbomChat("StyxArmor");
            }
            */

            List<string> dialogue = new List<string>();
            dialogue.Add(AbomChat("Formattable1", !Main.hardMode ? AbomChat("Formatter1PHM") : AbomChat("Formatter1HM")));

            if (Main.LocalPlayer.ZoneGraveyard)
            {
                dialogue.Add(AbomChat("Graveyard"));
            }

            int mechanic = NPC.FindFirstNPC(NPCID.Mechanic);
            if (mechanic != -1)
            {
                dialogue.Add(AbomChat("Mechanic", Main.npc[mechanic].GivenName));
            }

            return dialogue.Count > 0 ? dialogue[Main.rand.Next(dialogue.Count)] : "";
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
            button2 = Language.GetTextValue("Mods.Fargowiltas.NPCs.Abominationn.CancelEvent");
        }

        public const string ShopName = "Shop";

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = ShopName;
            }
            else
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    var netMessage = Mod.GetPacket();
                    netMessage.Write((byte)6);
                    netMessage.Send();
                }

                if (Fargowiltas.IsEventOccurring)
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        var netMessage = Mod.GetPacket();
                        netMessage.Write((byte)2);
                        netMessage.Send();
                    }

                    Main.npcChatText = Fargowiltas.TryClearEvents() ? AbomChat("Canceled") : AbomChat("CancelCD", FargoWorld.AbomClearCD / 60);
                }
                else
                {
                    Main.npcChatText = AbomChat("NoEvent");
                }
            }
        }

        public override void AddShops()
        {
            var npcShop = new NPCShop(Type, ShopName)
                .Add(new Item(ItemType<PartyInvite>(), 1) { shopCustomPrice = Item.buyPrice(copper: 10000) })
                .Add(new Item(ItemType<WeatherBalloon>(), 1) { shopCustomPrice = Item.buyPrice(copper: 20000) })
                .Add(new Item(ItemType<Anemometer>(), 1) { shopCustomPrice = Item.buyPrice(copper: 30000) })
                .Add(new Item(ItemType<ForbiddenScarab>(), 1) { shopCustomPrice = Item.buyPrice(copper: 30000) })
                .Add(new Item(ItemType<SlimyBarometer>(), 1) { shopCustomPrice = Item.buyPrice(copper: 40000) })
                .Add(new Item(ItemID.BloodMoonStarter, 1) { shopCustomPrice = Item.buyPrice(copper: 50000) })
                .Add(new Item(ItemID.GoblinBattleStandard, 1) { shopCustomPrice = Item.buyPrice(copper: 60000) })
                .Add(new Item(ItemType<MatsuriLantern>(), 1) { shopCustomPrice = Item.buyPrice(copper: 100000) }, new Condition("Mods.Fargowiltas.Conditions.BossDown", () => FargoWorld.DownedBools["boss"]))
                .Add(new Item(ItemID.SnowGlobe, 1) { shopCustomPrice = Item.buyPrice(copper: 150000) }, Condition.Hardmode)
                .Add(new Item(ItemID.PirateMap, 1) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedPirates)
                .Add(new Item(ItemType<PlunderedBooty>(), 1) { shopCustomPrice = Item.buyPrice(copper: 150000) }, new Condition("Mods.Fargowiltas.Conditions.DutchmanDown", () => NPC.downedPirates && FargoWorld.DownedBools["flyingDutchman"]))
                .Add(new Item(ItemID.SolarTablet, 1) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedMechBossAny)
                .Add(new Item(ItemType<ForbiddenTome>(), 1) { shopCustomPrice = Item.buyPrice(copper: 50000) }, new Condition("Mods.Fargowiltas.Conditions.MageDown", () => FargoWorld.DownedBools["darkMage"] || NPC.downedMechBossAny))
                .Add(new Item(ItemType<BatteredClub>(), 1) { shopCustomPrice = Item.buyPrice(copper: 150000) }, new Condition("Mods.Fargowiltas.Conditions.OgreDown", () => FargoWorld.DownedBools["ogre"] || NPC.downedGolemBoss))
                .Add(new Item(ItemType<BetsyEgg>(), 1) { shopCustomPrice = Item.buyPrice(copper: 400000) }, new Condition("Mods.Fargowiltas.Conditions.BetsyDown", () => FargoWorld.DownedBools["betsy"]))
                .Add(new Item(ItemID.PumpkinMoonMedallion, 1) { shopCustomPrice = Item.buyPrice(copper: 500000) }, Condition.DownedPumpking)
                 .Add(new Item(ItemType<HeadofMan>(), 1) { shopCustomPrice = Item.buyPrice(copper: 200000) }, new Condition("Mods.Fargowiltas.Conditions.HorsemanDown", () => FargoWorld.DownedBools["headlessHorseman"]))
                 .Add(new Item(ItemType<SpookyBranch>(), 1) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedMourningWood)
                 .Add(new Item(ItemType<SuspiciousLookingScythe>(), 1) { shopCustomPrice = Item.buyPrice(copper: 300000) }, Condition.DownedPumpking)
                 .Add(new Item(ItemID.NaughtyPresent, 1) { shopCustomPrice = Item.buyPrice(copper: 500000) }, Condition.DownedIceQueen)
                 .Add(new Item(ItemType<FestiveOrnament>(), 1) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedEverscream)
                 .Add(new Item(ItemType<NaughtyList>(), 1) { shopCustomPrice = Item.buyPrice(copper: 200000) }, Condition.DownedSantaNK1)
                 .Add(new Item(ItemType<IceKingsRemains>(), 1) { shopCustomPrice = Item.buyPrice(copper: 300000) }, Condition.DownedIceQueen)
                 .Add(new Item(ItemType<RunawayProbe>(), 1) { shopCustomPrice = Item.buyPrice(copper: 500000) }, Condition.DownedGolem)
                 .Add(new Item(ItemType<MartianMemoryStick>(), 1) { shopCustomPrice = Item.buyPrice(copper: 300000) }, Condition.DownedMartians)
                 .Add(new Item(ItemType<PillarSummon>(), 1) { shopCustomPrice = Item.buyPrice(copper: 750000) }, new Condition("Mods.Fargowiltas.Conditions.PillarsDown", () => NPC.downedTowers))
                 .Add(new Item(ItemType<AbominationnScythe>(), 1) { shopCustomPrice = Item.buyPrice(copper: 50000) }, new Condition("Mods.Fargowiltas.Conditions.PillarsDown", () => NPC.downedTowers))
                .Add(new Item(ItemType<SiblingPylon>(), 1), Condition.HappyEnoughToSellPylons, Condition.NpcIsPresent(NPCType<Mutant>()), Condition.NpcIsPresent(NPCType<Deviantt>()))

            ;

            npcShop.Register();
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = NPC.downedMoonlord ? 150 : 20;
            knockback = NPC.downedMoonlord ? 10f : 4f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = NPC.downedMoonlord ? 1 : 30;
            if (!NPC.downedMoonlord)
            {
                randExtraCooldown = 30;
            }
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = NPC.downedMoonlord ? ProjectileType<Projectiles.DeathScythe>() : ProjectileID.DeathSickle;
            attackDelay = 1;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 12f;
            randomOffset = 2f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemType<CrabSizedGlasses>(), 10));
        }
        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                for (int k = 0; k < 8; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2.5f * hitDirection, -2.5f, Scale: 0.8f);
                }

                if (!Main.dedServ)
                {
                    Vector2 pos = NPC.position + new Vector2(Main.rand.Next(NPC.width - 8), Main.rand.Next(NPC.height / 2));
                    Gore.NewGore(pos, NPC.velocity, ModContent.Find<ModGore>("Fargowiltas", "AbomGore3").Type);

                    pos = NPC.position + new Vector2(Main.rand.Next(NPC.width - 8), Main.rand.Next(NPC.height / 2));
                    Gore.NewGore(pos, NPC.velocity, ModContent.Find<ModGore>("Fargowiltas", "AbomGore2").Type);

                    pos = NPC.position + new Vector2(Main.rand.Next(NPC.width - 8), Main.rand.Next(NPC.height / 2));
                    Gore.NewGore(pos, NPC.velocity, ModContent.Find<ModGore>("Fargowiltas", "AbomGore1").Type);
                }
            }
            else
            {
                for (int k = 0; k < damage / NPC.lifeMax * 50.0; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, Scale: 0.6f);
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {   
            Texture2D texture = (Texture2D)TownNPCProfile().GetTextureNPCShouldUse(NPC);
            Rectangle rectangle = NPC.frame;
            Vector2 origin2 = rectangle.Size() / 2f;
            SpriteEffects effects = SpriteEffects.None;
            if (!NPC.IsShimmerVariant && !NPC.IsABestiaryIconDummy)
            {
                if (NPC.direction == -1)
                    texture = ModContent.Request<Texture2D>("Fargowiltas/NPCs/Abominationn").Value;
                else
                    texture = ModContent.Request<Texture2D>("Fargowiltas/NPCs/AbominationnRight").Value;
                Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY) + new Vector2(0, -5f), new Microsoft.Xna.Framework.Rectangle?(rectangle), NPC.GetAlpha(drawColor), NPC.rotation, origin2, NPC.scale, effects, 0);
                return false;
            }
            else
                return true;
        }

        private static string AbomChat(string key, params object[] args) => Language.GetTextValue($"Mods.Fargowiltas.NPCs.Abominationn.Chat.{key}", args);
    }

    public class AbomProfile : ITownNPCProfile
    {
        //public static int ShimmerHeadIndex = Mod.AddNPCHeadTexture(Main.npc[ModContent.NPCType<Abominationn>()].type, "Fargowiltas/NPCs/Abominationn_Shimmer_Head");

        public int RollVariation() => 0;
        public string GetNameForVariant(NPC npc) => npc.getNewNPCName();

        public Asset<Texture2D> GetTextureNPCShouldUse(NPC npc)
        {
            if (npc.IsABestiaryIconDummy)
                return ModContent.Request<Texture2D>("Fargowiltas/NPCs/Abominationn");

            if (npc.IsShimmerVariant)
            {
                return ModContent.Request<Texture2D>("Fargowiltas/NPCs/Abominationn_Shimmer");
            }

            if (npc.direction == -1)
                return ModContent.Request<Texture2D>("Fargowiltas/NPCs/Abominationn");
            else
                return ModContent.Request<Texture2D>("Fargowiltas/NPCs/AbominationnRight");
        }

        public int GetHeadTextureIndex(NPC npc) => npc.IsShimmerVariant ? ModContent.GetModHeadSlot("Fargowiltas/NPCs/Abominationn_Shimmer_Head") : ModContent.GetModHeadSlot("Fargowiltas/NPCs/Abominationn_Head");
    }
}
