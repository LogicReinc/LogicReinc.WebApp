using LogicReinc.WebApp.Chromium.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;
using Xilium.CefGlue.Wrapper;

namespace LogicReinc.WebApp.Mixed.Chromium
{
    public class ChromiumApp : CefApp
    {
        CefBrowserProcessHandler handler = new BrowserProcessHandler();
        CefRenderProcessHandler renderHandler = new RenderProcessHandler();

        protected override CefBrowserProcessHandler GetBrowserProcessHandler()
        {
            return handler;
        }
        protected override CefRenderProcessHandler GetRenderProcessHandler()
        {
            return renderHandler;
        }
        protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
        {
            if (CefRuntime.Platform == CefRuntimePlatform.Linux)
            {
                var path = new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath;
                path = Path.GetDirectoryName(path);

                commandLine.AppendSwitch("resources-dir-path", path);
                commandLine.AppendSwitch("locales-dir-path", Path.Combine(path, "locales"));
            }

        }
    }
}
