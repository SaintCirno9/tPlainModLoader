using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TPMLBridge.GABP
{
    public class GABPRequest
    {
        [JsonProperty("v")]
        public string Version { get; set; } = "gabp/1";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "request";

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JObject Params { get; set; }
    }

    public class GABPResponse
    {
        [JsonProperty("v")]
        public string Version { get; set; } = "gabp/1";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "response";

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public GABPError Error { get; set; }
    }

    public class GABPError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }
    }

    public class GABPToolDescriptor
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }

        [JsonProperty("inputSchema")]
        public object InputSchema { get; set; }

        [JsonProperty("outputSchema", NullValueHandling = NullValueHandling.Ignore)]
        public object OutputSchema { get; set; }
    }
}
