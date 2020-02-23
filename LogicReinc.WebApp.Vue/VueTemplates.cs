using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LogicReinc.WebApp.Vue
{
    internal static class VueTemplates
    {
        public static string Template_VueCore { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Vue.Scripts.VueCore.js");
        public static string Template_VueComponent { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Vue.Scripts.VueCoreComponent.js");
        public static string Template_VueComponentCall { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Vue.Scripts.VueCoreComponent.Call.js");



        public static string Format_VueCore(string elementId, string dataObject, string[] methods)
        {
            return string.Format(Template_VueCore,
                elementId,
                dataObject,
                string.Join(",\n", methods));
        }

        public static string Format_VueComponent(string componentName, string[] data, string[] methods, string template)
        {
            return string.Format(Template_VueComponent, 
                componentName, 
                string.Join(",\n", data),
                string.Join(",\n", methods), 
                template);
        }

        public static string Format_VueComponentCall(string functionName, bool tick)
        {
            return string.Format(Template_VueComponentCall,
                functionName,
                tick.ToString().ToLower());
        }

    }
}
