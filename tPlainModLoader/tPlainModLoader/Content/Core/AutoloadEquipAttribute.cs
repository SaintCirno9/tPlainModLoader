using System;

namespace TPML.Content
{
    /// <summary>
    /// 装备材质自动加载标记特性（对齐 tModLoader AutoloadEquip）
    /// 作者: SaintCirno9
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AutoloadEquipAttribute : Attribute
    {
        public EquipType[] EquipTypes { get; }

        public AutoloadEquipAttribute(params EquipType[] equipTypes)
        {
            EquipTypes = equipTypes ?? Array.Empty<EquipType>();
        }
    }
}
