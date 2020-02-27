using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogicReinc.WebApp.Vue
{
    public class VueComponent
    {
        private Dictionary<string, Func<JToken[], object>> _ipcAvailable = null;

        private VueData _data = null;
        private string _compName = null;

        protected string ComponentID { get; set; }
        protected VueWindow Parent { get; set; }

        protected T ParentT<T>() where T:VueWindow => (T)Parent;

        public VueComponent(string id, VueWindow parent)
        {
            ComponentID = id;
            _compName = GetType().Name;
            Parent = parent;

            MapIPC(WebExposeType.All);

            _data = new VueData(this, GetVueProperties(GetType(), false));
            _data.Initialize();
        }


        //Events
        [VuePreventTick]
        public virtual void Mounted()
        {

        }

        [VuePreventTick]
        public void Destroyed()
        {
            Parent.RemoveComponent(ComponentID);
        }

        //Control
        public JToken Execute(string js)
        {
            return Parent.Execute($"_comp{_compName}Eval['{ComponentID}']('{js}')");
        }

        public void NextTick()
        {
            var changes = _data.FindChangesAndUpdate();
            if (changes.Count > 0)
                Task.Run(() => Execute($"this.PushChanges({JsonConvert.SerializeObject(changes)})"));
        }

        public Dictionary<string, object> GetData()
        {
            return _data.Data;
        }


        //Meta
        public static PropertyInfo[] GetVueProperties(Type compType, bool requiresExpose)
        {

            PropertyInfo[] props = compType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (requiresExpose)
                props = props.Where(x => x.GetCustomAttribute<VueDataAttribute>() != null).ToArray();
            return props;
        }
        public static MethodInfo[] GetVueMethods(Type compType, WebExposeType expose)
        {
            MethodInfo[] methods = compType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            switch (expose)
            {
                case WebExposeType.Attributed:
                    methods = methods.Where(x => x.GetCustomAttribute<WebExposeAttribute>() != null).ToArray();
                    break;
                case WebExposeType.None:
                    return null;
            }
            return methods;
        }


        //Private
        private object[] ConvertIPCParameters(ParameterInfo[] paras, JToken[] args)
        {
            if (paras.Length != args.Length)
                throw new ArgumentException("IPC parameters do not match count");

            object[] resultArgs = new object[paras.Length];
            for (int i = 0; i < paras.Length; i++)
            {
                ParameterInfo parameter = paras[i];
                JToken arg = args[i];
                resultArgs[i] = arg.ToObject(parameter.ParameterType);
            }
            return resultArgs;
        }
        private void MapIPC(WebExposeType expose)
        {
            _ipcAvailable = new Dictionary<string, Func<JToken[], object>>();

            MethodInfo[] methods = GetVueMethods(GetType(), expose);
            foreach (MethodInfo meth in methods)
            {
                if (_ipcAvailable.ContainsKey(meth.Name))
                    throw new InvalidOperationException("Not allowed duplicate method names in UI class");

                ParameterInfo[] methParas = meth.GetParameters();
                _ipcAvailable.Add(meth.Name, (paras) =>
                {
                    return meth.Invoke(this, ConvertIPCParameters(methParas, paras));
                });
            }
        }

        //Internal
        internal bool HandleCall(string function, JToken[] arguments, out object result)
        {
            if (_ipcAvailable.ContainsKey(function))
            {
                result = _ipcAvailable[function](arguments);
                return true;
            }
            result = null;
            return false;
        }
    }

    public class VueTemplateAttribute : Attribute
    {
        public string Resource { get; set; }
        public VueTemplateAttribute(string res)
        {
            Resource = res;
        }
    }

    internal class VuePreventTickAttribute : Attribute
    {

    }
}
