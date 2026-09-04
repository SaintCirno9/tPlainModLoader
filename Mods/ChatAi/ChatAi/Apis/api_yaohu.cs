using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatAi.Apis
{
    /// <summary>
    /// https://api.yaohud.cn/
    /// </summary>
    internal class api_yaohu
    {
        private static int requestCount = 0;


        public static void TextCheck(ref string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            if (text.Length > 50) throw new ArgumentException("文本过长", nameof(text));

            text = text.Trim();
            if (text.Length < 1) throw new ArgumentException("空文本", nameof(text));

            if (Regex.Replace(text, "\\s", "").Length < 1) throw new ArgumentException("空文本", nameof(text));
        }

        public static bool CanRequest()
        {
            return requestCount < 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="text"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="TaskCanceledException"></exception>
        /// <exception cref="HttpRequestException"></exception>
        public static async Task<string> RequestAsync(string key, string text, int type = 0)
        {
            if (CanRequest() == false) throw new Exception("当前无法请求");
            ++requestCount;

            string url = null;

            switch (type)
            {
                case 0: url = RequestUrl.Get_yaohu(key, text); break;
                case 1: url = RequestUrl.Get_yaohu2(key, text); break;
                default: throw new Exception($"类型错误[{type}]");
            }

            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(10000);
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content?.ReadAsStringAsync();
                    return responseBody;
                }
                else
                {
                    throw new HttpRequestException($"http响应失败, 状态码[{response.StatusCode}]");
                }
            }
            finally
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    if (requestCount > 0) --requestCount;
                });
            }
        }
    }
}
