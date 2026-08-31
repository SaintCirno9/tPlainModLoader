using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.DataStructures;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生物块实体 (ModTileEntity) 注册与生命周期分发中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class TileEntityLoader
    {
        private static readonly Dictionary<int, ModTileEntity> _entitiesByType = new Dictionary<int, ModTileEntity>();
        private static readonly Dictionary<string, int> _entitiesByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<ModTileEntity> Entities => _entitiesByType.Values;

        public static int Register(ModTileEntity entity)
        {
            if (entity == null) return 0;

            if (TileEntity.manager == null)
            {
                TileEntity.manager = new TileEntitiesManager();
            }

            TileEntity.manager.Register(entity);
            int type = entity.Type;

            _entitiesByType[type] = entity;
            _entitiesByName[entity.FullName] = type;
            _entitiesByName[entity.Name] = type;

            ModContent.RegisterTileEntityType(entity.GetType(), type);
            ModLoader.Log($"[TileEntityLoader] 成功注册 TileEntity: [{entity.FullName}] -> TypeID={type}");
            return type;
        }

        public static ModTileEntity GetEntity(int type)
        {
            _entitiesByType.TryGetValue(type, out ModTileEntity entity);
            return entity;
        }

        public static T GetEntity<T>() where T : ModTileEntity
        {
            return ModContent.GetInstance<T>();
        }

        public static int TileEntityType<T>() where T : ModTileEntity
        {
            return ModContent.TileEntityType<T>();
        }

        public static int TileEntityType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;
            if (_entitiesByName.TryGetValue(fullName, out int type)) return type;
            int idx = fullName.IndexOf('/');
            if (idx >= 0 && idx < fullName.Length - 1)
            {
                string shortName = fullName.Substring(idx + 1);
                if (_entitiesByName.TryGetValue(shortName, out int shortType)) return shortType;
            }
            return 0;
        }

        public static void Clear()
        {
            _entitiesByType.Clear();
            _entitiesByName.Clear();
        }
    }
}
