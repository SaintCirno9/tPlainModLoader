using System;
using System.Collections.Generic;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 旧版补丁容器存根（保留用于向后兼容，内部不再挂载反射分发）。
    /// 作者: SaintCirno9
    /// </summary>
    public class TypePatch
    {
        private Dictionary<Type, IListPlain> list = new Dictionary<Type, IListPlain>();

        public ListCopy<T> Get<T>()
        {
            return (ListCopy<T>)list[typeof(T)];
        }

        public void AddPatch<T>(ListCopy<T> patch)
        {
            list.Add(typeof(T), patch);
        }

        public void ClearAllPatch()
        {
            foreach (KeyValuePair<Type, IListPlain> i in list)
            {
                i.Value.Clear();
            }
        }
    }
}
