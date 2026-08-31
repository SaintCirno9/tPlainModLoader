using System;

namespace TPML.Core.Pinyin
{
    /// <summary>
    /// 拼音文本匹配器（提供起始索引与命中长度计算）
    /// 作者: SaintCirno9
    /// </summary>
    public class PinyinMatch
    {
        private readonly string m_strText;
        private readonly string m_strPinyin;

        public string Text => m_strText;
        public string PinyinText => m_strPinyin;

        public PinyinMatch(string strText)
        {
            m_strText = (strText ?? string.Empty).ToLower();
            m_strPinyin = PinyinConvert.GetPinyin(m_strText).ToLower();
        }

        public int IndexOf(string strInput)
        {
            if (string.IsNullOrEmpty(strInput)) return 0;
            strInput = strInput.ToLower();

            int iStart = m_strText.IndexOf(strInput, StringComparison.Ordinal);
            if (iStart < 0)
            {
                int iLength = 0;
                if (!Match(strInput, ref iStart, ref iLength))
                {
                    iStart = -1;
                }
            }
            return iStart;
        }

        public int MatchLength(string strInput)
        {
            if (string.IsNullOrEmpty(strInput) || string.IsNullOrEmpty(m_strText)) return 0;
            strInput = strInput.ToLower();

            if (m_strText.Contains(strInput))
            {
                return strInput.Length;
            }

            int iStart = 0;
            int iLength = 0;
            if (!Match(strInput, ref iStart, ref iLength))
            {
                return 0;
            }
            return iLength;
        }

        private bool Match(string strInput, ref int iStart, ref int iLength)
        {
            int num = 0;
            int num2 = 0;
            bool flag = false;
            while (num < m_strPinyin.Length)
            {
                flag = false;
                int num3 = num;
                int num4 = num2;
                int num5 = num3;
                if (strInput[0] == m_strPinyin[num3])
                {
                    flag = true;
                    num3++;
                    for (int i = 1; i < strInput.Length; i++)
                    {
                        if (num3 >= m_strPinyin.Length)
                        {
                            flag = false;
                            break;
                        }
                        if (strInput[i] != m_strPinyin[num3])
                        {
                            if (num3 - num5 > 1 && m_strPinyin[num3] != '|')
                            {
                                flag = false;
                                break;
                            }
                            num5 = (num3 = m_strPinyin.IndexOf('|', num3) + 1);
                            num4++;
                            if (num3 <= 0 || num3 >= m_strPinyin.Length)
                            {
                                flag = false;
                                break;
                            }
                            if (strInput[i] != m_strPinyin[num3])
                            {
                                flag = false;
                                break;
                            }
                        }
                        num3++;
                    }
                }
                if (flag)
                {
                    iStart = num2;
                    iLength = num4 - num2 + 1;
                    break;
                }
                num = m_strPinyin.IndexOf('|', num) + 1;
                num2++;
                if (num <= 0)
                {
                    break;
                }
            }
            return flag;
        }

        public override string ToString()
        {
            return $"{Text} - {PinyinText}";
        }
    }
}
