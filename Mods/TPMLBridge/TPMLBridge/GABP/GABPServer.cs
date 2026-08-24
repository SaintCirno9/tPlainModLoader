using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TPMLBridge.GABP
{
    public class GABPServer
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private int _port = 49153;
        private string _token;
        private string _gameId = "terraria";
        private bool _isRunning;

        public static GABPServer Instance { get; private set; }

        public static void StartFromEnvironment()
        {
            if (Instance != null) return;

            string portStr = Environment.GetEnvironmentVariable("GABP_SERVER_PORT");
            string token = Environment.GetEnvironmentVariable("GABP_TOKEN");
            string gameId = Environment.GetEnvironmentVariable("GABS_GAME_ID");

            int port = 49153;
            if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out int parsedPort))
            {
                port = parsedPort;
            }

            Instance = new GABPServer(port, token, gameId ?? "terraria");
            Instance.Start();
        }

        public GABPServer(int port, string token, string gameId)
        {
            _port = port;
            _token = token;
            _gameId = gameId;
        }

        public void Start()
        {
            if (_isRunning) return;
            _cts = new CancellationTokenSource();
            _isRunning = true;

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();
                Console.WriteLine($"[GABP] 服务器已成功启动，监听 127.0.0.1:{_port} (GameID: {_gameId})");

                Task.Run(() => AcceptLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GABP] 启动监听端口 {_port} 失败: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _cts?.Cancel();
            _listener?.Stop();
            Console.WriteLine("[GABP] 服务器已停止");
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _isRunning)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, ct));
                }
                catch (Exception) when (ct.IsCancellationRequested || !_isRunning)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GABP] 接受客户端连接异常: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
            {
                Console.WriteLine("[GABP] 收到来自 GABS 客户端的连接");

                while (!ct.IsCancellationRequested && client.Connected)
                {
                    string line = await reader.ReadLineAsync();
                    if (line == null) break;

                    line = line.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    try
                    {
                        var request = JsonConvert.DeserializeObject<GABPRequest>(line);
                        if (request == null) continue;

                        var response = await ProcessRequestAsync(request);
                        string responseJson = JsonConvert.SerializeObject(response, Formatting.None);
                        await writer.WriteLineAsync(responseJson);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GABP] 处理请求异常: {ex.Message}");
                        var errorResponse = new GABPResponse
                        {
                            Version = "gabp/1",
                            Id = Guid.NewGuid().ToString(),
                            Error = new GABPError { Code = -32603, Message = ex.Message }
                        };
                        await writer.WriteLineAsync(JsonConvert.SerializeObject(errorResponse, Formatting.None));
                    }
                }
            }
        }

        private async Task<GABPResponse> ProcessRequestAsync(GABPRequest req)
        {
            var resp = new GABPResponse
            {
                Version = "gabp/1",
                Id = req.Id,
                Type = "response"
            };

            switch (req.Method)
            {
                case "session/hello":
                    {
                        // 可选 Token 校验
                        if (!string.IsNullOrEmpty(_token))
                        {
                            string clientToken = req.Params?["token"]?.ToString();
                            if (clientToken != _token)
                            {
                                resp.Error = new GABPError { Code = -32000, Message = "Token 鉴权失败" };
                                return resp;
                            }
                        }

                        resp.Result = new
                        {
                            agentId = "terraria",
                            app = new
                            {
                                name = "TPMLBridge",
                                version = "1.0.0"
                            },
                            capabilities = new
                            {
                                methods = new[] { "tools/list", "tools/call", "ping" },
                                events = new string[0],
                                resources = new string[0]
                            },
                            schemaVersion = "1.0"
                        };
                        return resp;
                    }

                case "ping":
                    resp.Result = new { };
                    return resp;

                case "tools/list":
                    {
                        var tools = TerrariaTools.GetDescriptors();
                        resp.Result = new { tools };
                        return resp;
                    }

                case "tools/call":
                    {
                        string toolName = req.Params?["name"]?.ToString();
                        var args = req.Params?["arguments"] as JObject ?? new JObject();

                        if (string.IsNullOrEmpty(toolName))
                        {
                            resp.Error = new GABPError { Code = -32602, Message = "缺少 tool 名称" };
                            return resp;
                        }

                        try
                        {
                            var toolResult = await TerrariaTools.CallToolAsync(toolName, args);
                            resp.Result = toolResult;
                        }
                        catch (Exception ex)
                        {
                            resp.Error = new GABPError { Code = -32001, Message = ex.Message };
                        }
                        return resp;
                    }

                default:
                    resp.Error = new GABPError { Code = -32601, Message = $"未支持的方法: {req.Method}" };
                    return resp;
            }
        }
    }
}
