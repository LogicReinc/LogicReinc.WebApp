using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Java.Interop;
using Java.Lang;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogicReinc.WebApp.Android
{
    [Activity]
    public class WebAppActivity<T> : Activity, IWebWindowImplementation, IWebAppActivity where T : WebWindow
    {
        private static string _polyfill = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Android.polyfill.js");

        private FieldInfo _lastHtmlField = typeof(WebWindow).GetField("_lastLoadedHtml", BindingFlags.Instance | BindingFlags.NonPublic);


        public WebWindow Controller { get; set; }

        public event Func<string, Task<object>> OnIPC;

        WebView _view = null;
        
        private List<string> _initScripts = new List<string>();

        public WebAppActivity()
        {
            if (WebWindow.EntryAssembly == null) WebWindow.EntryAssembly = typeof(T).Assembly;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //_criticalScripts.Add(_polyfill);
            _initScripts.Add(string.Format(WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Android.Scripts.IPCSetup.Android.js")));

            _view = new WebView(this);
            _view.Settings.JavaScriptEnabled = true;
            _view.AddJavascriptInterface(new JSInterface(this), "_host");
            _view.SetWebViewClient(new WebviewCallbacks(this));
            this.SetContentView(_view);
            try
            {
                Controller = (WebWindow)Activator.CreateInstance(typeof(T), new object[] { this });
            }
            catch(TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        public void Startup()
        {

        }


        public object HandleIPC(string msg)
        {
            string notify = msg;
            string id = notify.Substring(0, notify.IndexOf(":"));
            notify = notify.Substring(notify.IndexOf(":") + 1);


            Task<object> resultT = null;
            if (OnIPC != null)
                resultT = OnIPC(notify);

            if (resultT == null)
                return null;

            resultT.Wait();
            object result = resultT.Result;//.Result;


            if (result != null && result.GetType() == typeof(NoIPCResponse))
                return null;

            if (!string.IsNullOrEmpty(id))
                Execute(WebAppTemplates.FormatIf(
                    $"_IPCResolves[{id}]",
                    $"_IPCResolves[{id}]({JsonConvert.SerializeObject(result)});"));
            return result ;
        }



        public JToken Execute(string js)
        {
            AutoResetEvent ev = new AutoResetEvent(false);
            System.Exception ex = null;
            Java.Lang.Object result = null;
            string call = js;//WebAppTemplates.Format_SafeEvalJsonCall(js);
            RunOnUiThread(() => {
                try
                {
                    _view.EvaluateJavascript(call, new JSCallback(this, (obj) =>
                      {
                          result = obj;
                          ev.Set();
                      }));
                  }
                catch (System.Exception x) {
                    ex = x;
                }
            });
            ev.WaitOne();

            if (ex != null)
                throw new System.Exception("Exception:" + ex.Message);
            return JToken.Parse(result.ToString());
        }

        public void LoadHtml(string html)
        {
            WebPage page = new WebPage(html);
            for (int i = _initScripts.Count - 1; i >= 0; i--)
                page.AddToHeaderStart($"\n<script>\n{_initScripts[i]}\n</script>\n");
            html = page.ToString();
            _lastHtmlField.SetValue(Controller, html);
            _view.LoadData(html, "text/html", "UTF-8");
        }

        public void LoadUrl(string url)
        {
            _view.LoadUrl(url);
        }

        public void RegisterInitScript(string script)
        {
            _initScripts.Add(script);
        }

        public void Invoke(Action act) { RunOnUiThread(act); }


        //Activity open close?
        public void Hide() { }
        public void Show() { }
        
        //Android not relevant:
        public void StartDragMove() { }
        public void StopDragMove() { }
        public void ShowBorder() { }
        public void RemoveBorder() { }
        public void SetSize(int x, int y) { }
        public void SetPosition(int x, int y) { }
        public void Maximize() { }
        public void Minimize() { }
        public void Close() { }
        public void Focus() { }
        public void BringToFront() { }



        private class WebviewCallbacks : WebViewClient
        {

            private WebAppActivity<T> _act = null;
            public WebviewCallbacks(WebAppActivity<T> act)
            {
                _act = act;
            }

            public override void OnPageFinished(WebView view, string url)
            {
                base.OnPageFinished(view, url);

                //Doing in webpage for now.
                //foreach (string str in _act._initScripts)
                //    _act._view.EvaluateJavascript(str, null);
            }
        }
        private class JSCallback : Java.Lang.Object, IValueCallback
        {
            private WebAppActivity<T> _activity = null;
            private Action<Java.Lang.Object> _act = null;

            //public IntPtr Handle => _activity.Handle;

            public JSCallback(WebAppActivity<T> activity, Action<Java.Lang.Object> act) : base()
            {
                _activity = activity;
                _act = act;
            }

            public void OnReceiveValue(Java.Lang.Object value)
            {
                _act(value);
            }
            

            public void Dispose() { }

        }
    }

    internal interface IWebAppActivity
    {
        object HandleIPC(string msg);
    }
    internal class JSInterface : Java.Lang.Object
    {
        private IWebAppActivity _act = null;

        public JSInterface(IWebAppActivity act)
        {
            _act = act;
        }

        [Export]
        [JavascriptInterface]
        public string onIPC(string msg)
        {
            return JsonConvert.SerializeObject(_act.HandleIPC(msg));
        }
    }
}