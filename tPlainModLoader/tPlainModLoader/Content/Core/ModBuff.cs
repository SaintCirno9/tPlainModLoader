using System;
using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// TPML 自定义增益/减益 (ModBuff) 基类
    /// 遵循 tModLoader 经典 API 范式与强类型生命周期分发
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class ModBuff : ModType
    {
        private int _type;
        public int Type => _type;
        internal void SetType(int type) => _type = type;

        public virtual string Texture => (GetType().Namespace + "." + Name).Replace('.', '/');

        public string DisplayName => BuffLoader.GetDisplayName(Type);
        public string Description => BuffLoader.GetDescription(Type);

        public override void Load(Mod mod)
        {
            Mod = mod;
            BuffLoader.Register(this);
            base.Load(mod);
        }

        public virtual void SetStaticDefaults()
        {
        }

        public virtual void Update(Player player, ref int buffIndex)
        {
        }

        public virtual void Update(NPC npc, ref int buffIndex)
        {
        }

        public virtual bool ReApply(Player player, int time, int buffIndex)
        {
            return false;
        }

        public virtual bool ReApply(NPC npc, int time, int buffIndex)
        {
            return false;
        }

        public virtual void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
        }

        public virtual bool RightClick(int buffIndex)
        {
            return true;
        }
    }
}
