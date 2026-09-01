using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义物块 (ModTile) 基类
    /// 遵循 tModLoader 经典 API 范式，支持多格建筑 (MultiTile)、交互、绘制与生命周期钩子
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class ModTile : ModType
    {
        public int Type { get; internal set; }
        public virtual string Texture => (GetType().Namespace + "." + Name).Replace('.', '/');

        public int ItemDrop { get; set; } = 0;
        public int DustType { get; set; } = 0;
        public int[] AdjTiles { get; set; } = Array.Empty<int>();
        public int AnimationFrameHeight { get; set; } = 0;

        public LocalizedText CreateMapEntryName() => Language.GetText($"Mods.{Mod?.Name ?? "TPML"}.Tiles.{Name}.MapEntry");
        public void AddMapEntry(Color color, LocalizedText text = null) { }

        public void AddToArray(ref int[] array)
        {
            if (array == null)
            {
                array = new int[] { Type };
                return;
            }
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = Type;
        }

        public void AddToArray(ref bool[] array)
        {
            if (array == null)
            {
                array = new bool[Type + 1];
                array[Type] = true;
                return;
            }
            if (Type >= array.Length)
            {
                Array.Resize(ref array, Type + 64);
            }
            array[Type] = true;
        }

        public override void Load(Mod mod)
        {
            Mod = mod;
            TileLoader.Register(this);
            base.Load(mod);
        }

        public virtual void SetStaticDefaults()
        {
        }

        public virtual void SetDefaults()
        {
        }

        /// <summary>
        /// 当玩家右键点击该物块时触发
        /// </summary>
        /// <returns>若返回 true 则表示交互已被消费，阻止原版进一步处理</returns>
        public virtual bool RightClick(int i, int j)
        {
            return false;
        }

        /// <summary>
        /// 当鼠标近距离悬停在该物块上时触发
        /// </summary>
        public virtual void MouseOver(int i, int j)
        {
        }

        /// <summary>
        /// 当鼠标远距离悬停在该物块上时触发
        /// </summary>
        public virtual void MouseOverFar(int i, int j)
        {
        }

        /// <summary>
        /// 当该多方块结构被挖掘破坏时触发（i, j 为被破坏方块左上角基准点坐标）
        /// </summary>
        public virtual void KillMultiTile(int i, int j, int frameX, int frameY)
        {
        }

        /// <summary>
        /// 当该方块被破坏时触发
        /// </summary>
        public virtual void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
        }

        /// <summary>
        /// 控制是否允许掉落物品
        /// </summary>
        public virtual bool Drop(int i, int j)
        {
            return true;
        }

        /// <summary>
        /// 获取物块被破坏时掉落的物品 ID（默认优先返回 ItemDrop，若未设置则自动反查 createTile 绑定的 ModItem）
        /// </summary>
        public virtual int GetItemDrop(int tileType, int frameX, int frameY)
        {
            if (ItemDrop > 0) return ItemDrop;
            foreach (var item in ItemLoader.Items)
            {
                Item sample = ContentSamples.ItemsByType.TryGetValue(item.Type, out var s) ? s : item.Item;
                if (sample != null && sample.createTile == tileType)
                {
                    return item.Type;
                }
            }
            return 0;
        }

        /// <summary>
        /// 物块被放置到世界中时触发
        /// </summary>
        public virtual void PlaceInWorld(int i, int j, Item item)
        {
        }

        /// <summary>
        /// 物块绘制前钩子。返回 false 可阻止默认物块绘制
        /// </summary>
        public virtual bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            return true;
        }

        /// <summary>
        /// 物块绘制后额外渲染钩子
        /// </summary>
        public virtual void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
        }

        /// <summary>
        /// 物块逐帧动画更新
        /// </summary>
        public virtual void AnimateTile(ref int frame, ref int frameCounter)
        {
        }

        /// <summary>
        /// 自定义 TileFrame 逻辑。返回 false 可阻止原版 TileFrame 处理
        /// </summary>
        public virtual bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return true;
        }

        /// <summary>
        /// 玩家靠近物块时的环境影响钩子
        /// </summary>
        public virtual void NearbyEffects(int i, int j, bool closer)
        {
        }

        /// <summary>
        /// 自定义物块发光
        /// </summary>
        public virtual void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
        }

        /// <summary>
        /// 物块被挖掘时生成的粒子数量
        /// </summary>
        public virtual void NumDust(int i, int j, bool fail, ref int num)
        {
        }

        /// <summary>
        /// 电路触发钩子
        /// </summary>
        public virtual void HitWire(int i, int j)
        {
        }

        /// <summary>
        /// 是否允许破坏物块
        /// </summary>
        public virtual bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            return true;
        }

        /// <summary>
        /// 是否允许被炸药破坏
        /// </summary>
        public virtual bool CanExplode(int i, int j)
        {
            return true;
        }
    }
}
