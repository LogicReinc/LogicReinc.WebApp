using LogicReinc.WebApp.Javascript;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LogicReinc.WebApp
{
    public static class WebAppTemplates
    {
        public static string Template_SafeEvalJsonCall { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.SafeEvalJsonCall.js");
        public static string Template_SafeEvalJsonCall2 { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.SafeEvalJsonCall2.js");
        public static string Template_IPCSetup { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.IPCSetup.js");
        public static string Template_WebWindowBase { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.WebWindowBase.js");
        public static string Template_IPC { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.WebWindow.IPC.js");
        public static string Template_Function { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.Function.js");
        public static string Template_If { get; } = "if({0}){{ {1} }}";
        public static string Template_Call { get; } = "{0}({1})";

        public static string Template_Polyfill { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.polyfill.js");

        public static string FormatFunction(string name, string body)
        {
            return FormatFunction(name, new string[] { }, body);
        }
        public static string FormatFunction(string name, string[] paras, string body)
        {
            return string.Format(Template_Function, name, string.Join(",", paras), body);
        }
        public static string FormatIf(string condition, string body)
        {
            return string.Format(Template_If, condition, body);
        }
        public static string FormatCall(string call, params object[] paras)
        {
            return string.Format(Template_Call, call, string.Join(",", paras.Select(x => JsonConvert.SerializeObject(x)).ToArray()));
        }

        public static string Format_SafeEvalJsonCall(string js)
        {
            return string.Format(Template_SafeEvalJsonCall, js);
        }
        public static string Format_SafeEvalJsonCall2(int id, string js)
        {
            return string.Format(Template_SafeEvalJsonCall2, id, js);
        }

        public static string Format_WebWindowIPC()
        {
            return string.Format(Template_IPCSetup);
        }
        public static string Format_WebWindowBase(string onload)
        {
            return string.Format(Template_WebWindowBase, onload);
        }

        public static string FormatIPC<T>(T obj) where T : IPCObjectBase 
        {
            if(obj.GetType() == typeof(IPCObject2))
            {
                IPCObject2 obj2 = (IPCObject2)(IPCObjectBase)obj;
                if (obj2.Arguments == null)
                    obj2.Arguments = new object[0];
                else if (obj2.Arguments is JavascriptReference)
                {
                    if (((JavascriptReference)obj2.Arguments).Reference == "arguments")
                        ((JavascriptReference)obj2.Arguments).Reference = "[].slice.call(arguments)";
                }
                else if (!obj2.Arguments.GetType().IsArray)
                    obj2.Arguments = new object[] { obj2.Arguments };
            }
            return string.Format(Template_IPC, JsonConvert.SerializeObject(obj));
        }
        public static string FormatIPCFunction<T>(string name, T obj) where T : IPCObjectBase
        {
            return FormatFunction(name, "return " + FormatIPC(obj));
        }

        public static string GetPolyfill()
        {
            return Template_Polyfill;
        }
    }
}
