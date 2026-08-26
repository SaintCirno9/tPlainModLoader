using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TPML.Core.Pinyin
{
    /// <summary>
    /// 拼音转换与解析核心类
    /// 作者: SaintCirno9
    /// </summary>
    public class PinyinConvert
    {
        private static readonly object _lock = new object();
        private static volatile PinyinTreeNode _dict;

        private readonly string m_strText;
        private readonly string m_strPinyin;
        private readonly bool[] m_bIsPinyinStartIndex;
        private readonly string m_strPinyinShort;

        static PinyinConvert()
        {
            EnsureLoaded();
        }

        public static void EnsureLoaded()
        {
            if (_dict != null) return;
            lock (_lock)
            {
                if (_dict != null) return;
                Load();
            }
        }

        private static void Load()
        {
            var dict = new PinyinTreeNode();
            string pinyinRaw = LoadResourceString();

            if (!string.IsNullOrEmpty(pinyinRaw))
            {
                using (var reader = new StringReader(pinyinRaw))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                            continue;

                        int tabIndex = line.IndexOf('\t');
                        if (tabIndex <= 0 || tabIndex >= line.Length - 1)
                            continue;

                        string word = line.Substring(0, tabIndex);
                        string pinyin = line.Substring(tabIndex + 1);

                        PinyinTreeNode current = dict;
                        for (int i = 0; i < word.Length; i++)
                        {
                            char c = word[i];
                            if (!current.Nodes.TryGetValue(c, out var next))
                            {
                                next = new PinyinTreeNode();
                                current.Nodes[c] = next;
                            }
                            current = next;
                        }
                        current.Pinyin = pinyin;
                        current.PinyinWord = word;
                    }
                }
            }

            _dict = dict;
        }

        private static string LoadResourceString()
        {
            var assembly = typeof(PinyinConvert).Assembly;
            string resourceName = "TPML.Core.Pinyin.PinyinResource.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            // 备用：从同目录直接读取文件
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PinyinResource.txt");
            if (File.Exists(localPath))
            {
                return File.ReadAllText(localPath, Encoding.UTF8);
            }

            return string.Empty;
        }

        /// <summary>
        /// 将中文文本翻译为带竖线分隔的拼音（如 "钻石镐" -> "zuan|shi|gao"）
        /// </summary>
        public static string TranslateToPinyin(string strChinese)
        {
            if (string.IsNullOrEmpty(strChinese)) return string.Empty;
            EnsureLoaded();

            StringBuilder sb = new StringBuilder(strChinese.Length * 4);
            int num = 0;
            while (num < strChinese.Length)
            {
                int i = num;
                PinyinTreeNode matchedNode = null;
                for (PinyinTreeNode curr = _dict; i < strChinese.Length && curr.Nodes.TryGetValue(strChinese[i], out var next); i++)
                {
                    if (!string.IsNullOrEmpty(next.Pinyin))
                    {
                        matchedNode = next;
                    }
                    curr = next;
                }

                if (matchedNode != null)
                {
                    sb.Append(matchedNode.Pinyin);
                    num += matchedNode.PinyinWord.Length;
                }
                else
                {
                    sb.Append(strChinese[num]).Append('|');
                    num++;
                }
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == '|')
            {
                sb.Length--;
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取中文文本的拼音字符串（以竖线分割，如 "zuan|shi|gao"）
        /// </summary>
        public static string GetPinyin(string strChinese)
        {
            return TranslateToPinyin(strChinese);
        }

        /// <summary>
        /// 获取中文文本的拼音首字母缩写（如 "钻石镐" -> "zsg"）
        /// </summary>
        public static string GetPinyinFirstLetter(string strChinese)
        {
            if (string.IsNullOrEmpty(strChinese)) return string.Empty;
            string text = TranslateToPinyin(strChinese);
            string[] parts = text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    sb.Append(parts[i][0]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取用于模糊搜索与自动补全的组合串（包含首字母、全拼连写和原文）
        /// </summary>
        public static string GetPinyinForAutoComplete(string strChinese)
        {
            if (string.IsNullOrEmpty(strChinese)) return string.Empty;
            string text = TranslateToPinyin(strChinese);
            string[] parts = text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder firstLetters = new StringBuilder(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    firstLetters.Append(parts[i][0]);
                }
            }

            string fullPinyin = text.Replace("|", "");
            return $"{firstLetters} {fullPinyin} {strChinese}".ToLower();
        }

        public PinyinConvert(string strText)
        {
            if (strText == null) strText = string.Empty;
            string text = TranslateToPinyin(strText);
            string[] parts = text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder shortSb = new StringBuilder(parts.Length);
            StringBuilder fullSb = new StringBuilder(text.Length);
            m_bIsPinyinStartIndex = new bool[text.Length + 1];

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    shortSb.Append(parts[i][0]);
                    if (fullSb.Length < m_bIsPinyinStartIndex.Length)
                    {
                        m_bIsPinyinStartIndex[fullSb.Length] = true;
                    }
                    fullSb.Append(parts[i]);
                }
            }

            m_strText = strText.ToLower();
            m_strPinyin = fullSb.ToString().ToLower();
            m_strPinyinShort = shortSb.ToString().ToLower();
        }

        public int IndexOf(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            value = value.ToLower();

            int num = m_strText.IndexOf(value, StringComparison.Ordinal);
            if (num < 0)
            {
                num = m_strPinyinShort.IndexOf(value, StringComparison.Ordinal);
            }
            if (num < 0)
            {
                num = m_strPinyin.IndexOf(value, StringComparison.Ordinal);
                if (num >= 0 && num < m_bIsPinyinStartIndex.Length && !m_bIsPinyinStartIndex[num])
                {
                    num = -1;
                }
            }
            return num;
        }
    }
}
