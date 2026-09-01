using System.Collections.Generic;

namespace TPML.Content
{
    /// <summary>
    /// TPML 物品掉落物规则容器存根
    /// 作者: SaintCirno9
    /// </summary>
    public class ItemLoot
    {
        public List<object> Rules { get; } = new List<object>();

        public void Add(object rule)
        {
            if (rule != null)
            {
                Rules.Add(rule);
            }
        }
    }
}
