namespace LogicReinc.WebApp.Chromium
{
    using LogicReinc.WebApp.Chromium.Handlers;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xilium.CefGlue;

    internal sealed class WebClient : CefClient
    {
        internal static bool DumpProcessMessages { get; set; }

        private readonly WebBrowser _core;
        private readonly WebLifeSpanHandler _lifeSpanHandler;
        private readonly WebDisplayHandler _displayHandler;
        private readonly WebLoadHandler _loadHandler;

        public event Func<CefProcessId, CefProcessMessage, bool> OnProcessMessage;

        public WebClient(WebBrowser core)
        {
            _core = core;
            _lifeSpanHandler = new WebLifeSpanHandler(_core);
            _displayHandler = new WebDisplayHandler(_core);
            _loadHandler = new WebLoadHandler(_core);
        }

        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return _lifeSpanHandler;
        }

        protected override CefDisplayHandler GetDisplayHandler()
        {
            return _displayHandler;
        }

        protected override CefLoadHandler GetLoadHandler()
        {
            return _loadHandler;
        }

        protected override bool OnProcessMessageReceived(CefBrowser browser, CefProcessId sourceProcess, CefProcessMessage message)
        {

            // var handled = DemoApp.BrowserMessageRouter.OnProcessMessageReceived(browser, sourceProcess, message);
            // if (handled) return true;

            if (OnProcessMessage != null)
                return OnProcessMessage(sourceProcess, message);

            return false;
        }
    }
}
