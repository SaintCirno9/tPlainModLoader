namespace ChatAi.Apis
{
    internal class RequestUrl
    {
        /// <summary>
        /// https://api.yaohud.cn/doc/15
        /// </summary>
        public static readonly string apiUrl_yaohu = "https://api.yaohud.cn/api/v5/smartai";

        /// <summary>
        /// https://api.yaohud.cn/doc/65
        /// </summary>
        public static readonly string apiUrl_yaohu2 = "https://api.yaohud.cn/api/model/AI_yueyao";


        //https://api.yaohud.cn/api/v5/smartai?key=key&type=&userid=&msg=泰拉瑞亚介绍
        public static string Get_yaohu(string key, string msg)
        {
            string url = $"{apiUrl_yaohu}?key={key}&type=&userid=&msg={msg}";

            return url;
        }

        public static string Get_yaohu2(string key, string msg)
        {
            string url = $"{apiUrl_yaohu2}?key={key}&type=&text={msg}";

            return url;
        }
    }
}
