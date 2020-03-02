using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogicReinc.WebApp
{
    public class IPCObjectBase
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("debugName")]
        public string DebugName { get; set; }


        [JsonProperty("nocallback")]
        public bool NoCallback { get; set; } = false;

        //Optional
        [JsonProperty("function")]
        public string Function { get; set; }

        [JsonProperty("cb")]
        public int Callback { get; set; }
    }

    public class IPCObject : IPCObjectBase
    {
        [JsonProperty("arguments")]
        public JToken[] Arguments { get; set; }

        public JToken[] GetArguments() => Arguments;
    }
    //If object args is required
    public class IPCObject2 : IPCObjectBase
    {
        [JsonProperty("arguments")]
        public object Arguments { get; set; }
    }
    public class NoIPCResponse
    {

    }
}
