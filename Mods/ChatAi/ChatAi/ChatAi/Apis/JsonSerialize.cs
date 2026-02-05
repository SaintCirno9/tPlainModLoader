using Newtonsoft.Json.Linq;
using System;

namespace ChatAi.Apis
{
    internal class JsonSerialize
    {
        //{"code":200,"msg":"请求成功","data":{"to":"测试","fromtext":"你想测试什么","heuristic":["null"]},"exec_time":0.545729,"tips":"妖狐API提供","ip":""}
        public static string Serialize_yaohu(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            JObject jObj = JObject.Parse(text);
            if (jObj == null || jObj.HasValues == false) throw new NullReferenceException("解析失败");

            JToken msg = jObj["msg"];

            JToken code = jObj["code"];
            if (code?.ToString() != "200") throw new Exception($"状态码错误[{code}], 消息[{msg}]");

            JToken data = jObj["data"];
            if (data == null || data.HasValues == false) throw new NullReferenceException("属性[data]为null");

            JToken fromtext = data["fromtext"];

            return fromtext?.ToString();
        }

        //{"code":200,"msg":"请求成功（复用会话+AI调用，配置状态：已配置）",
        //"data":{"userText":"测试","aiText":"三连测试，你是想把我逼疯吗？再这样我可要挠你痒痒了！","sessionId":179977,"created":"2025-11-06 10:52:14","updated":"2025-11-06 10:52:15","model_think_time":"模型思考时间：1.006685秒","promptTokens":244,"completionTokens":36,"useTokens":280,"Tokentext":"提示: 244 Tokens + 结果: 36 Tokens 其中提示含预设+上下文 236 Tokens 总 280 Tokens","remind":"长时间未使用将自动删除对话","total耗时":"AI耗时：1.006685秒","当前使用模型":"本地模型训练(更新时间2025/10/18)","PUT配置响应摘要":{"id":179977,"created":"2025-11-06 10:45:02","updated":"2025-11-06 10:45:02","model":"qwen-plus-2025-07-14"},"配置状态":"已配置","最后配置时间":"2025-11-06 10:45:02"},"debug":"[初始化] 脚本启动;模型=本地模型训练(更新时间2025/10/18)\n[存储操作] 已加载历史数据（共1条记录）；检测参数已初始化\n[key状态] 已存在；sessionId=179977；最后使用时间=2025-11-06 10:46:26\n[配置检测] 当前状态：已配置；最后配置时间=2025-11-06 10:45:02\n[key状态] 已更新最后使用时间为：2025-11-06 10:52:14\n[主逻辑] 启动；isFirstCreate=否；配置状态=已配置\n[主逻辑] 存储更新成功\n[AI调用] 开始；sessionId=179977\n[AI调用] 完成；HTTP状态码=200\n[AI调用] 成功；收到回复\n[AI调用] 上下文携带条目数:3\n[主逻辑] 完成","exec_time":1.017863,"tips":"妖狐API提供(交流群:1101215018)","ip":""}
        public static string Serialize_yaohu2(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            JObject jObj = JObject.Parse(text);
            if (jObj == null || jObj.HasValues == false) throw new NullReferenceException("解析失败");

            JToken msg = jObj["msg"];

            JToken code = jObj["code"];
            if (code?.ToString() != "200") throw new Exception($"状态码错误[{code}], 消息[{msg}]");

            JToken data = jObj["data"];
            if (data == null || data.HasValues == false) throw new NullReferenceException("属性[data]为null");

            JToken fromtext = data["aiText"];

            return fromtext?.ToString();
        }
    }
}
