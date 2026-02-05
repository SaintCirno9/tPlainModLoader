using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChatAi.Apis
{
    public class Chat
    {
        public static string apiKey { get; protected set; } = null;


        public static void SetApiKey(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length < 1) throw new ArgumentException("空文本", nameof(text));

            apiKey = text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="TaskCanceledException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<string> InputAsync(string text, int type = 0)
        {
            if (api_yaohu.CanRequest() == false) throw new Exception("当前无法请求");

            api_yaohu.TextCheck(ref text);

            if (apiKey == null) throw new NullReferenceException("key为null");

            string requestText = await api_yaohu.RequestAsync(apiKey, text, type);

            if (type == 0) return JsonSerialize.Serialize_yaohu(requestText);
            if (type == 1) return JsonSerialize.Serialize_yaohu2(requestText);

            return null;
        }
    }
}
