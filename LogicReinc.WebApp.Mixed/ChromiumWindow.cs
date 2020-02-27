using LogicReinc.WebApp.Chromium;
using LogicReinc.WebApp.Javascript;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xilium.CefGlue;

namespace LogicReinc.WebApp.Mixed
{
    public class ChromiumWindow : Form, IWebWindowImplementation
    {
        private CefWebBrowser _browser = null;

        private Panel _browserContainer = new Panel();

        private bool _isReady = false;

        public event Action OnReady;
        public event Func<string, Task<object>> OnIPC;

        public WebWindow Controller { get; set; }


        private static string _polyfill = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Mixed.polyfill.js");


        private int _chromeRespCounter = 0;
        private Dictionary<int, Action<JToken, JToken>> _chromeResps = new Dictionary<int, Action<JToken, JToken>>();


        public ChromiumWindow()
        {
            BackColor = Color.Black;
            RegisterInitScript(_polyfill);
            RegisterInitScript(string.Format(WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Mixed.IPCSetup.Chromium.js")));

        }

        public void Startup()
        {
            Thread.Sleep(2);
            _browserContainer.Dock = DockStyle.Fill;
            _browserContainer.BackColor = Color.Black;
            _browser = new CefWebBrowser(this.Width, this.Height);
            _browser.BackColor = Color.Black;
            _browser.OnProcessMessage += ProcessMessage;
            _browser.BrowserReady += BrowserReady;
            _browser.BackColor = Color.DarkRed;

            _browser.StartUrl = "https://google.com";
            _browser.Dock = DockStyle.Fill;

            _browserContainer.Controls.Add(_browser);

            Controls.Add(_browserContainer);
            Opacity = 0;
            Show();
            Hide();
            Opacity = 1;
        }

        private bool ProcessMessage(CefProcessId pid, CefProcessMessage msg)
        {
            string notify = msg.Arguments.GetString(4);

            if (notify.StartsWith("chrom:"))
            {
                JObject obj = JObject.Parse(notify.Substring("chrom:".Length));

                if (obj.ContainsKey("id"))
                {
                    int id = obj.GetValue("id").ToObject<int>();
                    if (_chromeResps.ContainsKey(id))
                    {
                        Action<JToken, JToken> cb = _chromeResps[id];
                        _chromeResps.Remove(id);
                        cb(obj.GetValue("result"), obj.GetValue("excp"));
                    }
                }

                return true;
            }
            else
            {
                string id = notify.Substring(0, notify.IndexOf(":"));
                notify = notify.Substring(notify.IndexOf(":") + 1);

                Task<object> resultT = null;
                if (OnIPC != null)
                    resultT = OnIPC(notify);

                if (resultT == null)
                    return true;


                IPCObject obj = JsonConvert.DeserializeObject<IPCObject>(notify);
                if(!obj.NoCallback)
                    resultT.Wait();
                object result = obj.NoCallback ? new NoIPCResponse() : resultT.Result;

                if (result != null && result.GetType() == typeof(NoIPCResponse))
                    return true;

                if(!string.IsNullOrEmpty(id))
                    Task.Run(() =>
                    {
                        Execute(WebAppTemplates.FormatIf(
                            $"_IPCResolves[{id}]",
                            $"_IPCResolves[{id}]({JsonConvert.SerializeObject(result)});"));
                    });

                return true;
            }
        }

        private void BrowserReady()
        {
            _isReady = true;
            if (OnReady != null)
                OnReady();
            Invoke(() => { Visible = true; });
        }
        private class DevToolsWebClient : CefClient { }

        public void Invoke(Action act)
        {
            base.Invoke((MethodInvoker)delegate { act(); });
        }

        public void Move(int x, int y)
        {
            Location = new Point(Location.X + x, Location.Y + y);
        }

        public void Maximize()
        {
            base.WindowState = FormWindowState.Maximized;
        }
        public void Minimize()
        {
            base.WindowState = FormWindowState.Minimized;
        }


        public void CanResize(bool resizable)
        {
        }

        public void ShowBorder()
        {
            base.FormBorderStyle = FormBorderStyle.Sizable;
        }
        public void RemoveBorder()
        {
            base.FormBorderStyle = FormBorderStyle.None;
        }

        private List<string> initScripts = new List<string>();
        public void RegisterInitScript(string script)
        {
            initScripts.Add(script);
            //_browser.AddInitializeScript(script);
        }
        public void LoadHtml(string html)
        {
            WebPage page = new WebPage(html);
            for (int i = initScripts.Count - 1; i >= 0; i--)
                page.AddToHeaderStart($"\n<script>\n{initScripts[i]}\n</script>\n");
            File.WriteAllText(Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "test.html"), page.ToString());
            RunOnReady(() =>
            {
                CefFrame frame = _browser.Browser.GetMainFrame();
                _browser.Browser.GetMainFrame().LoadString(page.ToString(), "http://localhost/");
            });
            //Console.WriteLine("Loaded");
        }
        public void LoadUrl(string url)
        {
            RunOnReady(() =>
            {
                Console.WriteLine("Loading Url:");
                _browser.Browser.GetMainFrame().LoadUrl(url);
            });
        }


        static string chromCall = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Mixed.ChromiumCall.js");
        public JToken Execute(string js)
        {
            
            try
            {
                if (_browser.Browser == null)
                    return null;
                //string result = _jscontext.EvaluateScript("evalJson", js);

                string ex = null;
                JToken result = null;


                int cbID = _chromeRespCounter++;
                AutoResetEvent ev = new AutoResetEvent(false);
                _chromeResps.Add(cbID, (res, excp) =>
                {
                    string calljs = js;
                    if (excp != null)
                        ex = excp.ToString();
                    else
                        result = res;
                    ev.Set();
                    
                });
                string call = String.Format(chromCall, js, cbID);//WebAppTemplates.FormatCall("evalJsonChrome", cbID, js);

                _browser.Browser.GetMainFrame().ExecuteJavaScript(call, _browser.Browser.GetMainFrame().Url, 0);
                ev.WaitOne();

                if (ex != null)
                    throw new Exception($"Javascript exception: {ex} \n@:" + js);

                if (result == null)
                    return null;
                else
                    return result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count == 1)
                {
                    Exception ex1 = ex.InnerExceptions[0];
                    if (ex1.Message.Contains("0x80020101"))
                        throw new Exception("Exception in javascript..:\n" + js);
                    else
                        throw;
                }
                else
                    throw;
            }
            catch
            {
                string calljs = js;
                throw;
            }
        }



        protected override void OnClosed(EventArgs e)
        {
            _browser.Dispose();
            base.OnClosed(e);
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

        public void Focus()
        {
            base.Focus();
        }

        private int _dragX = 0;
        private int _dragY = 0;
        private int _dragID = 0;

        public void StartDragMove()
        {
            _dragX = Cursor.Position.X - Location.X;
            _dragY = Cursor.Position.Y - Location.Y;
            int did = ++_dragID;

            Task.Run(() =>
            {
                while (_dragID == did)
                {
                    Invoke(() => Location = new Point(
                        Cursor.Position.X - _dragX,
                        Cursor.Position.Y - _dragY
                    ));
                    Thread.Sleep(1);
                }
            });

            //ReleaseCapture();
            //SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }
        public void StopDragMove()
        {
            _dragID++;
        }


        private void RunOnReady(Action act)
        {
            if (_isReady)
                act();
            else
                OnReady += act;
        }

    }
}
