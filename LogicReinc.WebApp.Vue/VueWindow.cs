using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LogicReinc.WebApp.Vue
{
    public abstract class VueWindow : WebWindow
    {
        public abstract string VueElementID { get; }
        
        private VueData _vueData = null;

        private Dictionary<string, VueComponent> _components = new Dictionary<string, VueComponent>();
        private Dictionary<string, Type> _componentTypes = new Dictionary<string, Type>();

        public VueWindow()
        {
            bool requireExpose = false;
            
            VueSettingsAttribute settings = GetType().GetCustomAttribute<VueSettingsAttribute>();
            if (settings != null)
            {
                requireExpose = settings.RequireExpose;
            }

            _vueData = VueData.Create(this, requireExpose);
        }


        protected void RegisterComponent(Type componentType)
        {
            if (!_componentTypes.ContainsKey(componentType.Name))
                _componentTypes.Add(componentType.Name, componentType);
        }
        internal void RemoveComponent(string id)
        {
            if (_components.ContainsKey(id))
                _components.Remove(id);
        }

        public override void OnLoaded()
        {
            _vueData.Initialize();

            Task.Run(() =>
            {
                BeforeVue();

                foreach (var comp in _componentTypes)
                    Execute(CreateComponentTemplate(comp.Value));

                Execute(VueTemplates.Format_VueCore(VueElementID,
                    _vueData.AsVueDataString(), base.IPCAvailable
                    .Select(x => $"'{x}':{x}")
                    .ToArray()));
            });
        }

        public virtual void BeforeVue()
        {

        }

        [WebExpose]
        public virtual void Mounted()
        {

        }

        public void NextTick()
        {
            Dictionary<string, object> changes = _vueData.FindChangesAndUpdate();
            if (changes.Count > 0)
                Task.Run(() => JS._this.PushChanges(changes));
        }

        [WebExpose]
        public Dictionary<string,object> CreateComponentInstance(string name)
        {
            if (!_componentTypes.ContainsKey(name))
                throw new ArgumentException("Component [" + name + "] does not exist");

            string id = Guid.NewGuid().ToString();

            VueComponent comp = (VueComponent)Activator.CreateInstance(_componentTypes[name], id, this);
            _components.Add(id, comp);

            return new Dictionary<string, object>()
            {
                { "id", id },
                { "data", comp.GetData() }
            };
        }

        protected override bool HandleIPC(string type, JObject ipcPackage, out object result)
        {
            switch (type)
            {
                case "componentCall":
                    string compid = ipcPackage.GetValue("component").ToString();
                    if (_components.ContainsKey(compid))
                    {
                        string function = ipcPackage.GetValue("function").ToString();
                        JToken[] args = ipcPackage.GetValue("arguments").ToArray();

                        VueComponent comp = _components[compid];
                        object compResult = null;
                        if(comp.HandleCall(function, args, out compResult))
                        {
                            result = compResult;

                            if(!ipcPackage.ContainsKey("tick") || ipcPackage.GetValue("tick").ToObject<bool>())
                            comp.NextTick();
                        }
                    }
                    break;
            }
            return base.HandleIPC(type, ipcPackage, out result);
        }
        protected override object OnIPCCall(string function, JToken[] arguments)
        {
            object result = base.OnIPCCall(function, arguments);

            NextTick();
            return result;
        }

        private string CreateComponentTemplate(Type compType)
        {
            if (!_componentTypes.ContainsKey(compType.Name))
                throw new ArgumentException("Component [" + compType.Name + "] does not exist");

            VueTemplateAttribute templateAtt = compType.GetCustomAttribute<VueTemplateAttribute>();
            if (templateAtt == null)
                throw new InvalidOperationException("Missing VueComponentTemplateAttribute missing");

            return VueTemplates.Format_VueComponent(compType.Name,
                VueComponent.GetVueProperties(compType, false)
                    .Select(x => $"{x.Name}: undefined").ToArray(),
                VueComponent.GetVueMethods(compType, WebExposeType.All)
                    .Select(x => VueTemplates.Format_VueComponentCall(x.Name, x.GetCustomAttribute<VuePreventTickAttribute>() == null)).ToArray(),
                WebContext.LoadStringResource(Assembly.GetEntryAssembly(), templateAtt.Resource));

        }
    }
}
