using System;
using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader.IO;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 玩家生命周期与数据绑定基类
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

        public virtual void OnEnterWorld(Player player)
        {
        }
    }

    /// <summary>
    /// 玩家 ModPlayer 扩展查找方法
    /// </summary>
    public static class ModPlayerExtensions
    {
        private static readonly Dictionary<Player, Dictionary<Type, ModPlayer>> _playerMods = new Dictionary<Player, Dictionary<Type, ModPlayer>>();

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
        public EntitySource_Misc(string context) => Context = context;
    }
}
