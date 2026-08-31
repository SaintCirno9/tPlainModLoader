using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ObjectData;
using TPML.Content.IO;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义物块实体 (ModTileEntity) 基类
    /// 紧密绑定物块，承载自定义容器、复杂状态机、计时器与伴随存档持久化
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class ModTileEntity : TileEntity, ILoadable
    {
        public Mod Mod { get; internal set; }
        public virtual string Name => GetType().Name;
        public string FullName => (Mod != null ? Mod.Name + "/" : "") + Name;
        public int Type { get; internal set; }

        public PlacementHook Generic_HookPostPlaceMyPlayer => new PlacementHook(Generic_Hook_AfterPlacement, -1, 0, true);

        public static PlacementHook GetPlacementHook<T>() where T : ModTileEntity
        {
            return new PlacementHook((i, j, type, style, direction, alternate) =>
            {
                int entityType = ModContent.TileEntityType<T>();
                if (Main.netMode == 1)
                {
                    NetMessage.SendTileSquare(Main.myPlayer, i, j, 1, 1);
                    NetMessage.SendData(86, -1, -1, null, entityType, i, j);
                    return -1;
                }
                return ModContent.GetInstance<T>()?.Place(i, j) ?? -1;
            }, -1, 0, true);
        }

        public virtual void Load(Mod mod)
        {
            Mod = mod;
            TileEntityLoader.Register(this);
        }

        public virtual void Unload()
        {
        }

        public virtual bool IsLoadingEnabled(Mod mod) => true;

        public override void RegisterTileEntityID(int assignedID)
        {
            Type = (byte)assignedID;
            type = (byte)assignedID;
        }

        public override TileEntity GenerateInstance()
        {
            return (TileEntity)Activator.CreateInstance(GetType());
        }

        public virtual void SaveData(TagCompound tag)
        {
        }

        public virtual void LoadData(TagCompound tag)
        {
        }

        public virtual void OnKill()
        {
        }

        public virtual void OnNetPlace()
        {
        }

        public virtual int Generic_Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
        {
            if (Main.netMode == 1)
            {
                NetMessage.SendTileSquare(Main.myPlayer, i, j, 1, 1);
                NetMessage.SendData(86, -1, -1, null, Type, i, j);
                return -1;
            }
            return Place(i, j);
        }

        public virtual int Place(int i, int j)
        {
            ModTileEntity newEntity = (ModTileEntity)GenerateInstance();
            newEntity.Position = new Point16(i, j);
            int id = AssignNewID();
            newEntity.ID = id;
            newEntity.type = (byte)Type;
            newEntity.Type = Type;
            newEntity.RequiresUpdates = true;

            ByID[id] = newEntity;
            ByPosition[newEntity.Position] = newEntity;
            if (!UpdateEntities.Contains(newEntity))
            {
                UpdateEntities.Add(newEntity);
            }
            newEntity.OnPlaced();
            return id;
        }

        public virtual void Kill(int i, int j)
        {
            Point16 pos = new Point16(i, j);
            if (ByPosition.TryGetValue(pos, out TileEntity entity) && entity.type == (byte)Type)
            {
                if (entity is ModTileEntity mte)
                {
                    mte.OnKill();
                }
                ByID.Remove(entity.ID);
                ByPosition.Remove(pos);
                UpdateEntities.Remove(entity);
                entity.OnRemoved();
            }
        }

        public virtual int Find(int i, int j)
        {
            Point16 pos = new Point16(i, j);
            if (ByPosition.TryGetValue(pos, out TileEntity entity) && entity.type == (byte)Type)
            {
                return entity.ID;
            }
            return -1;
        }

        public override bool IsTileValidForEntity(int x, int y)
        {
            return true;
        }

        public override void NetPlaceEntityAttempt(int x, int y)
        {
            int number = Place(x, y);
            if (ByID.TryGetValue(number, out var ent) && ent is ModTileEntity mte)
            {
                mte.OnNetPlace();
            }
            NetMessage.SendData(86, -1, -1, null, number, x, y);
        }
    }
}
