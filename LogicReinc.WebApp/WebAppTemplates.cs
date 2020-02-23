using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LogicReinc.WebApp
{
    public static class WebAppTemplates
    {

        public static string Template_WebWindowBase { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.WebWindowBase.js");
        public static string Template_IPC { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.WebWindow.IPC.js");
        public static string Template_Function { get; } = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Scripts.Function.js");
        public static string Template_If { get; } = "if({0}){{ {1} }}";


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

        public static string Format_WebWindowBase(string onload)
        {
            return string.Format(Template_WebWindowBase, onload);
        }

        public static string FormatIPC<T>(T obj) where T : IPCObject
        {
            return string.Format(Template_IPC, JsonConvert.SerializeObject(obj));
        }
        public static string FormatIPCFunction<T>(string name, T obj) where T : IPCObject
        {
            return FormatFunction(name, "return " + FormatIPC(obj));
        }
    }
}
