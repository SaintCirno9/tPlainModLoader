using System;
using System.Linq;
using System.Reflection;
using TPML.ModLoad;
using TPML.Utils;
using Terraria.UI;

namespace TPML
{
    /// <summary>
    /// 模组设置
    /// </summary>
    public abstract class ModSetting
    {
        static ModSetting()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                SaveAllDirty();
            };
        }

        /// <summary>
        /// 全量保存所有被标记为已修改 (NeedSave == true) 的模组设置
        /// </summary>
        public static void SaveAllDirty()
        {
            var mos = LoaderControl.GetModObjects();
            if (mos == null) return;

            foreach (var mo in mos)
            {
                if (mo?.inheritance_setting == null) continue;
                foreach (var ms in mo.inheritance_setting)
                {
                    if (ms != null && ms.NeedSave)
                    {
                        try
                        {
                            ms.Save();
                        }
                        catch (Exception ex)
                        {
                            OutputDebug.OutputException(ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 需要保存
        /// </summary>
        public bool NeedSave { get; set; } = false;
        /// <summary>
        /// 设置项名称
        /// </summary>
        public virtual string Name => null;
        /// <summary>
        /// 标题
        /// </summary>
        public virtual string Title => null;
        /// <summary>
        /// 文件相对位置, 不设置则不保存
        /// </summary>
        public virtual string FilePath => null;
        /// <summary>
        /// 是否在模组设置UI中
        /// </summary>
        public virtual bool HasUI => true;
        /// <summary>
        /// 数据类型
        /// </summary>
        public virtual Type DataType => null;

        /// <summary>
        /// 在<see cref="Mod.Load"/>之后调用, 根据<see cref="FilePath"/>读取文件, 读取失败<paramref name="v"/>为<see langword="null"/>
        /// </summary>
        /// <param name="v"></param>
        public virtual void Load(object v) { }

        /// <summary>
        /// 获取设置界面
        /// </summary>
        /// <returns></returns>
        public virtual UIElement GetUI() => null;

        /// <summary>
        /// 设为默认
        /// </summary>
        public virtual void SetDefault() { }

        /// <summary>
        /// 获取需要保存的数据
        /// </summary>
        /// <returns>需要保存的数据</returns>
        public virtual object GetSaveData() => null;

        /// <summary>
        /// 保存
        /// </summary>
        public virtual void Save()
        {
            if (NeedSave == false) return;

            Assembly assembly = GetType().Assembly;
            ModObject mo = LoaderControl.GetModObjects()?.FirstOrDefault(i => i.assembly == assembly);

            bool saved = ModFile.SaveFileTry(FilePath, file =>
            {
                MyJson1.Save(GetSaveData(), file);
                return true;
            }, mo);

            if (saved)
            {
                NeedSave = false;
            }
        }

        /// <summary>
        /// 读取, 读取失败返回<see langword="null"/>
        /// </summary>
        /// <returns>读取到的数据</returns>
        public virtual object Read()
        {
            Assembly assembly = GetType().Assembly;
            ModObject mo = LoaderControl.GetModObjects()?.FirstOrDefault(i => i.assembly == assembly);

            object v = null;
            ModFile.ReadFileTry(FilePath, file =>
            {
                v = MyJson1.Get2(file, DataType);
                return true;
            }, mo);
            return v;
        }
    }
}
