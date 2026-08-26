using System.Collections.Generic;

namespace TPML.Core.Pinyin
{
    /// <summary>
    /// 拼音 Trie 前缀树节点
    /// </summary>
    internal class PinyinTreeNode
    {
        public string Pinyin { get; set; } = string.Empty;
        public string PinyinWord { get; set; } = string.Empty;
        public Dictionary<char, PinyinTreeNode> Nodes { get; } = new Dictionary<char, PinyinTreeNode>();

        public override string ToString()
        {
            return string.IsNullOrEmpty(Pinyin)
                ? $"<无拼音节点>, 子节点{Nodes.Count}个"
                : $"{PinyinWord}({Pinyin}), 子节点{Nodes.Count}个";
        }
    }
}
