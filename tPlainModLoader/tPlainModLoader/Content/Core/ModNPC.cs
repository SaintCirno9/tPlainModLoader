using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using TPML.Content.IO;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// TPML 场景音效/背景音优先级枚举（对齐 tML SceneEffectPriority）
    /// </summary>
    public enum SceneEffectPriority
    {
        None = 0,
        BiomeLow = 1,
        BiomeMedium = 2,
        BiomeHigh = 3,
        Event = 4,
        Environment = 5,
        BossLow = 6,
        BossMedium = 7,
        BossHigh = 8
    }

    /// <summary>
    /// TPML 头部贴图自动加载特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AutoloadHead : Attribute
    {
    }

    /// <summary>
    /// TPML Boss 头部贴图自动加载特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AutoloadBossHead : Attribute
    {
    }

    /// <summary>
    /// TPML 模组掉落物配置接口
    /// </summary>
    public interface ILoot
    {
        IItemDropRule Add(IItemDropRule entry);
        IItemDropRule Remove(IItemDropRule entry);
        void RemoveWhere(Predicate<IItemDropRule> predicate, bool includeGlobalDrops = true);
        List<IItemDropRule> Get(bool includeGlobalDrops = true);
    }

    /// <summary>
    /// TPML 模组 NPC 掉落物配置承载对象
    /// </summary>
    public class NPCLoot : ILoot
    {
        private static readonly ILogger Logger = LogManager.GetLogger("NPCLoot");

        public int NPCNetId { get; }
        private readonly List<IItemDropRule> _entries = new List<IItemDropRule>();

        public NPCLoot(int npcNetId)
        {
            NPCNetId = npcNetId;
        }

        public IItemDropRule Add(IItemDropRule entry)
        {
            _entries.Add(entry);
            try
            {
                Main.ItemDropsDB?.RegisterToNPCNetId(NPCNetId, entry);
            }
            catch (Exception ex)
            {
                Logger.Warn($"向 NPCNetId [{NPCNetId}] 注册掉落规则异常: {ex.Message}");
            }
            return entry;
        }

        public IItemDropRule Remove(IItemDropRule entry)
        {
            _entries.Remove(entry);
            try
            {
                Main.ItemDropsDB?.RemoveFromNPCNetId(NPCNetId, entry);
            }
            catch (Exception ex)
            {
                Logger.Warn($"从 NPCNetId [{NPCNetId}] 移除掉落规则异常: {ex.Message}");
            }
            return entry;
        }

        public void RemoveWhere(Predicate<IItemDropRule> predicate, bool includeGlobalDrops = true)
        {
            _entries.RemoveAll(predicate);
            try
            {
                var rules = Main.ItemDropsDB?.GetRulesForNPCID(NPCNetId, includeGlobalDrops);
                if (rules != null)
                {
                    foreach (var rule in rules)
                    {
                        if (predicate(rule))
                        {
                            Main.ItemDropsDB?.RemoveFromNPCNetId(NPCNetId, rule);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"条件移除 NPCNetId [{NPCNetId}] 掉落规则异常: {ex.Message}");
            }
        }

        public List<IItemDropRule> Get(bool includeGlobalDrops = true)
        {
            return Main.ItemDropsDB?.GetRulesForNPCID(NPCNetId, includeGlobalDrops) ?? new List<IItemDropRule>();
        }
    }

    /// <summary>
    /// TPML NPC 生成环境信息结构体
    /// </summary>
    public struct NPCSpawnInfo
    {
        public int SpawnTileX;
        public int SpawnTileY;
        public int SpawnTileType;
        public Player Player;
        public bool Sky;
        public bool Water;
        public bool Granit;
        public bool Marble;
        public bool SpiderCave;
        public bool DesertCave;
        public bool Lihzahrd;
        public bool PlayerInTown;
        public bool Invasion;
    }

    /// <summary>
    /// TPML 自定义 NPC / 生物 / Boss 基类
    /// 遵循 tModLoader 经典 API 范式与强类型生命周期分发
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class ModNPC : ModType
    {
        public NPC NPC { get; internal set; }
        private int _type;
        public int Type => NPC != null && NPC.type > 0 ? NPC.type : _type;
        internal void SetType(int type) => _type = type;

        public virtual string Texture => (GetType().Namespace + "." + Name).Replace('.', '/');
        public virtual string HeadTexture => Texture + "_Head";
        public virtual string BossHeadTexture => Texture + "_Head_Boss";

        public int AIType { get; set; }
        public int AnimationType { get; set; }
        public int Music { get; set; } = -1;
        public SceneEffectPriority SceneEffectPriority { get; set; } = SceneEffectPriority.BossLow;
        public float DrawOffsetY { get; set; }
        public int Banner { get; set; }
        public int BannerItem { get; set; }
        public int[] SpawnModBiomes { get; set; } = Array.Empty<int>();
        public bool TownNPCStayingHomeless { get; set; }
        public NPCHappiness Happiness => NPCHappiness.Get(Type);

        public string DisplayName => NPCLoader.GetDisplayName(Type);

        public override void Load(Mod mod)
        {
            Mod = mod;
            NPCLoader.Register(this);
            base.Load(mod);
        }

        public virtual void SetStaticDefaults()
        {
        }

        public virtual void SetDefaults()
        {
        }

        public virtual void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
        }

        public virtual ITownNPCProfile TownNPCProfile()
        {
            return null;
        }

        public virtual bool CanTownNPCSpawn(int numTownNPCs)
        {
            return false;
        }

        public virtual bool CanGoToStatue(bool toKingStatue)
        {
            return true;
        }

        public virtual List<string> SetNPCNameList()
        {
            return new List<string>();
        }

        public virtual string GetChat()
        {
            return null;
        }

        public virtual void SetChatButtons(ref string button, ref string button2)
        {
        }

        public virtual void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
        }

        public virtual void AddShops()
        {
        }

        public virtual void ModifyActiveShop(string shopName, Item[] items)
        {
        }

        public virtual void AddDebuffImmunities(List<int> debuffs)
        {
        }

        public virtual void BossHeadSlot(ref int index)
        {
        }

        public virtual void AI()
        {
        }

        public virtual bool PreAI()
        {
            return true;
        }

        public virtual void PostAI()
        {
        }

        public virtual void FindFrame(int frameHeight)
        {
        }

        public virtual bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return true;
        }

        public virtual void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
        }

        public virtual bool CheckDead()
        {
            return true;
        }

        public virtual void OnKill()
        {
        }

        public virtual void ModifyNPCLoot(NPCLoot npcLoot)
        {
        }

        public virtual bool CheckActive()
        {
            return true;
        }

        public virtual Color? GetAlpha(Color drawColor)
        {
            return null;
        }

        public virtual void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
        }

        public virtual void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
        }

        public virtual void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
        }

        public virtual void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
        }

        public virtual void TownNPCAttackMagic(ref float auraLightMultiplier)
        {
        }

        public virtual void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight)
        {
        }

        public virtual void DrawTownAttackGun(ref float scale, ref int item, ref int bHop)
        {
        }

        public virtual void DrawTownAttackSwing(ref Texture2D itemTexture, ref Rectangle rectangle, ref int itemSize, ref Vector2 scale, ref Vector2 offset)
        {
        }

        public virtual bool UsesPartyHat() => true;

        public virtual void ChatBubblePosition(ref Vector2 position, ref SpriteEffects spriteEffects)
        {
        }

        public virtual void OnSpawn(IEntitySource source)
        {
        }

        public virtual void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
        }

        public virtual bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => null;

        public virtual void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
        }

        public virtual void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
        }

        public virtual void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit)
        {
        }

        public virtual void HitEffect(int hitDirection, double damage)
        {
        }

        public virtual void SaveData(TagCompound tag)
        {
        }

        public virtual void LoadData(TagCompound tag)
        {
        }

        public virtual ModNPC Clone(NPC newEntity)
        {
            ModNPC clone = (ModNPC)Activator.CreateInstance(GetType());
            clone.Mod = Mod;
            clone.NPC = newEntity;
            clone.SetType(Type);
            clone.AIType = AIType;
            clone.AnimationType = AnimationType;
            clone.Music = Music;
            clone.SceneEffectPriority = SceneEffectPriority;
            clone.DrawOffsetY = DrawOffsetY;
            clone.Banner = Banner;
            clone.BannerItem = BannerItem;
            return clone;
        }
    }
}
