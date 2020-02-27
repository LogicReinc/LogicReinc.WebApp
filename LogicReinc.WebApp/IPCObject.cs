using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogicReinc.WebApp
{
    public class IPCObject
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("arguments")]
        public object Arguments { get; set; }

        [JsonProperty("nocallback")]
        public bool NoCallback { get; set; } = false;
    }
    public class NoIPCResponse
    {

    }
}
