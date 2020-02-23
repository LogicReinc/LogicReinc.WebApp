using LogicReinc.WebApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Logicreinc.WebApp.Mixed
{
    public class WindowsWebWindow : Form, IWebWindowImplementation
    {
        WebBrowser _browser = null;

        public event Func<string, object[], object> OnIPC;
        public event Action<int, int, string, string> OnScriptError;

        public WindowsWebWindow()
        {
            _browser = new WebBrowser();
            _browser.ScriptErrorsSuppressed = true;
            _browser.ObjectForScripting = new ScriptObject(this);

            _browser.Dock = DockStyle.Fill;
            Controls.Add(_browser);
        }

        public void Invoke(Action act)
        {
            base.Invoke((MethodInvoker)delegate { act(); });
        }

        public bool Initialize(PlatformID platform, bool is64)
        {
            return true;
        }
        public bool IsReady()
        {
            return true;
        }


        public void Maximize()
        {
            base.WindowState = FormWindowState.Maximized;
        }

        public void Minimize()
        {
            base.WindowState = FormWindowState.Minimized;
        }

        public void RegisterFunction(string function, Func<object[], object> cb)
        {
            if (function.Any(x => !char.IsLetter(x)))
                throw new ArgumentException("Only letters allowed in function name");
            string id = RegisterCallback(cb, false);

            HtmlElement el = _browser.Document.CreateElement("script");
            el.InnerText = $@"function {function}()
 {{window.external.Callback(\'" + id + "\', arguments);}}";
            HtmlElement head = _browser.Document.GetElementsByTagName("head")[0];
            head.AppendChild(el);
        }

        public void LoadHtml(string html)
        {
            _browser.DocumentText = html;
        }

        public void LoadUrl(string url)
        {
            _browser.Navigate(url);
        }


        public void Execute(string js)
        {
            _browser.Document.InvokeScript(js);
        }


        public void SetPosition(int x, int y)
        {
            base.Location = new System.Drawing.Point(x, y);
        }
        public void SetSize(int x, int y)
        {
            base.Width = x;
            base.Height = y;
        }


        //Scripting Communication
        private Dictionary<string, ScriptCallback> _scriptCallbacks = new Dictionary<string, ScriptCallback>();
        private object TriggerIPC(string command, object[] args)
        {
            if (OnIPC != null)
                return OnIPC(command, args);
            return null;
        }
        private string RegisterCallback(Func<object[], object> callback, bool removeOnCall = false)
        {
            ScriptCallback cb = new ScriptCallback()
            {
                ID = Guid.NewGuid().ToString(),
                Callback = callback,
                RemoveOnCall = removeOnCall
            };
            if (_scriptCallbacks.ContainsKey(cb.ID))
                _scriptCallbacks[cb.ID] = cb;
            else
                _scriptCallbacks.Add(cb.ID, cb);
            return cb.ID;
        }
        private object TriggerCallback(string id, object[] args)
        {
            if (_scriptCallbacks.ContainsKey(id))
            {
                ScriptCallback callback = _scriptCallbacks[id];
                if (callback.RemoveOnCall)
                    _scriptCallbacks.Remove(id);
                return callback.Callback(args);
            }
            return null;
        }

        [ComVisible(true)]
        public class ScriptObject
        {
            WindowsWebWindow _parent = null;
            public ScriptObject(WindowsWebWindow parent)
            {
                _parent = parent;
            }

            public void IPC(string command, object[] data)
            {
                _parent.TriggerIPC(command, data);
            }
            public void Callback(string id, object[] data)
            {
                _parent.TriggerCallback(id, data);
            }
        }

        private class ScriptCallback
        {
            public string ID { get; set; }
            public Func<object[], object> Callback { get; set; }
            public bool RemoveOnCall { get; set; }
        }
    }
}
