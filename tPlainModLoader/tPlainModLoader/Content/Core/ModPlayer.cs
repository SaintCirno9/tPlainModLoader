using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using TPML.Content.IO;

namespace TPML.Content
{
    /// <summary>
    /// TPML 玩家生命周期与数据绑定基类
    /// </summary>
    public abstract class ModPlayer : ModType
    {
        public Player Player { get; internal set; }

        public virtual void Initialize()
        {
        }

        public virtual void ResetEffects()
        {
        }

        public virtual void PreUpdate()
        {
        }

        public virtual void PostUpdate()
        {
        }

        public virtual void PostUpdateEquips()
        {
        }

        public virtual void PostUpdateMiscEffects()
        {
        }

        public virtual void PostUpdateRunSpeeds()
        {
        }

        public virtual void ProcessTriggers(TriggersSet triggersSet)
        {
        }

        public virtual void OnRespawn(Player player)
        {
        }

        public virtual bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            return true;
        }

        public virtual void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
        }

        public virtual bool OnPickup(Item item)
        {
            return true;
        }

        public virtual void SaveData(TagCompound tag)
        {
        }

        public virtual void LoadData(TagCompound tag)
        {
        }

        public virtual void PostUpdateBuffs()
        {
        }

        public virtual void UpdateDead()
        {
        }

        public virtual bool PreModifyLuck(ref float luck)
        {
            return true;
        }

        public virtual void ModifyLuck(ref float luck)
        {
        }

        public virtual void ModifyScreenPosition()
        {
        }

        public virtual void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
        }

        public virtual void CopyClientState(ModPlayer targetCopy)
        {
        }

        public virtual void SendClientChanges(ModPlayer clientPlayer)
        {
        }

        public virtual void ModifyStartingInventory(IReadOnlyDictionary<string, List<Item>> itemsByMod, bool mediumCoreDeath)
        {
        }

        public virtual IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
        {
            yield break;
        }

        public virtual void OnEnterWorld()
        {
        }

        public virtual void OnEnterWorld(Player player)
        {
            OnEnterWorld();
        }

        public virtual void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            health = StatModifier.Default;
            mana = StatModifier.Default;
        }
    }

    /// <summary>
    /// 玩家 ModPlayer 扩展查找方法
    /// </summary>
    public static class ModPlayerExtensions
    {
        private static readonly Dictionary<Player, Dictionary<Type, ModPlayer>> _playerMods = new Dictionary<Player, Dictionary<Type, ModPlayer>>();

        /// <summary>
        /// 对齐 tML <c>Player.TryGetModPlayer</c>：只查询已绑定实例，失败返回 false，不兜底实例化。
        /// </summary>
        public static bool TryGetModPlayer<T>(this Player player, out T result) where T : ModPlayer
        {
            result = null;
            if (player == null) return false;
            if (!_playerMods.TryGetValue(player, out var map)) return false;
            if (!map.TryGetValue(typeof(T), out var existing) || existing is not T typed) return false;
            result = typed;
            return true;
        }

        public static T GetModPlayer<T>(this Player player) where T : ModPlayer
        {
            if (player == null) return null;

            if (!_playerMods.TryGetValue(player, out var map))
            {
                map = new Dictionary<Type, ModPlayer>();
                _playerMods[player] = map;
            }

            if (map.TryGetValue(typeof(T), out var existing))
            {
                return (T)existing;
            }

            // 查找或根据已加载 Mod 内容实例化
            foreach (var mp in ModContent.GetContent<ModPlayer>())
            {
                if (mp is T match)
                {
                    var instance = (T)Activator.CreateInstance(typeof(T), true);
                    instance.Player = player;
                    instance.Mod = match.Mod;
                    instance.Initialize();
                    map[typeof(T)] = instance;
                    return instance;
                }
            }

            // 兜底直接实例化
            try
            {
                var instance = (T)Activator.CreateInstance(typeof(T), true);
                instance.Player = player;
                instance.Initialize();
                map[typeof(T)] = instance;
                return instance;
            }
            catch { }

            return null;
        }

        public static ModPlayer GetModPlayer(this Player player, Type type)
        {
            if (player == null || type == null) return null;

            if (!_playerMods.TryGetValue(player, out var map))
            {
                map = new Dictionary<Type, ModPlayer>();
                _playerMods[player] = map;
            }

            if (map.TryGetValue(type, out var existing))
            {
                return existing;
            }

            foreach (var mp in ModContent.GetContent<ModPlayer>())
            {
                if (type.IsAssignableFrom(mp.GetType()))
                {
                    var instance = (ModPlayer)Activator.CreateInstance(type, true);
                    instance.Player = player;
                    instance.Mod = mp.Mod;
                    instance.Initialize();
                    map[type] = instance;
                    return instance;
                }
            }

            try
            {
                var instance = (ModPlayer)Activator.CreateInstance(type, true);
                instance.Player = player;
                instance.Initialize();
                map[type] = instance;
                return instance;
            }
            catch { }

            return null;
        }

        internal static void ClearInstances()
        {
            _playerMods.Clear();
        }

        public static IEntitySource GetSource_Misc(this Entity entity, string context)
        {
            return new EntitySource_Misc(context);
        }
    }

    /// <summary>
    /// 兼容存根 EntitySource_Misc
    /// </summary>
    public class EntitySource_Misc : IEntitySource
    {
        public string Context { get; }
        public EntitySource_Misc(string context = null) => Context = context ?? "Misc";
    }
}
