using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogicReinc.WebApp.Javascript
{


    [JsonConverter(typeof(JavascriptReferenceConverter))]
    public class JavascriptReference
    {
        public string Reference { get; set; }

        public JavascriptReference() { }
        public JavascriptReference(string r)
        {
            Reference = r;
        }

        public override string ToString()
        {
            return Reference;
        }

        public static implicit operator string(JavascriptReference r)
        {
            return r?.Reference;
        }
    }
    public class JSRef : JavascriptReference
    {
        public JSRef(string r) : base(r) { }
    }

    public class JavascriptReferenceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(value.ToString());
        }
    }
}
