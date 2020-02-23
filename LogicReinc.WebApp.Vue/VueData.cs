using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LogicReinc.WebApp.Vue
{
    public class VueData
    {
        private object _parent;
        private PropertyInfo[] _dataProperties;

        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public VueData(object parent, PropertyInfo[] props)
        {
            _parent = parent;

            List<PropertyInfo> newProps = new List<PropertyInfo>();
            HashSet<string> ai = new HashSet<string>();
            foreach(var prop in props)
            {
                if (!ai.Contains(prop.Name))
                {
                    ai.Add(prop.Name);
                    newProps.Add(prop);
                }
            }
            _dataProperties = newProps.ToArray();
        }

        public void Initialize()
        {
            Data = GetObjectData();
        }

        public Dictionary<string, object> FindChangesAndUpdate()
        {
            Dictionary<string, object> newData = GetObjectData();
            Dictionary<string, object> changes = new Dictionary<string, object>();
            foreach(var pair in newData)
            {
                if (!Data.ContainsKey(pair.Key))
                    throw new InvalidOperationException("Either not initialized or data structure mismatch");
                if(pair.Value != Data[pair.Key])
                {
                    Data[pair.Key] = pair.Value;
                    changes.Add(pair.Key, pair.Value);
                }
            }
            return changes;
        }

        private Dictionary<string, object> GetObjectData()
        {
            Dictionary<string,object> newData = new Dictionary<string, object>();
            foreach (var prop in _dataProperties)
                newData.Add(prop.Name, prop.GetValue(_parent));
            return newData;
        }

        public static VueData Create(object parent, bool requiresExpose = false)
        {
            PropertyInfo[] props = parent.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (requiresExpose)
                props = props.Where(x => x.GetCustomAttribute<VueDataAttribute>() != null).ToArray();

            return new VueData(parent, props);
        }


        public string AsVueDataString()
        {
            return JsonConvert.SerializeObject(Data);
        }
    }
}
