using LogicReinc.WebApp.Chromium;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
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
        public event Action<string> OnIPC;

        public WebWindow Controller { get; set; }
        
        public ChromiumWindow()
        {
            RegisterInitScript(WebAppTemplates.GetPolyfill());
            RegisterInitScript(string.Format(WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Mixed.IPCSetup.Chromium.js")));

        }

        public void Startup()
        {
            WebAppLogger.Log(WebAppLogLevel.Info, "Chromium Startup");
            _browserContainer.Dock = DockStyle.Fill;
            _browser = new CefWebBrowser(this, this.Width, this.Height);
            _browser.OnProcessMessage += ProcessMessage;
            _browser.BrowserReady += BrowserReady;
            _browser.BackColor = Color.DarkRed;

            _browser.StartUrl = "https://google.com";
            _browser.Dock = DockStyle.Fill;

            _browserContainer.Controls.Add(_browser);

            Controls.Add(_browserContainer);
            //Opacity = 0;
            //Show();
            //Hide();
            //Opacity = 1;
        }

        private bool ProcessMessage(CefProcessId pid, CefProcessMessage msg)
        {
            if (OnIPC != null)
                OnIPC(msg.Arguments.GetString(4));
            return true;
        }

        private void BrowserReady()
        {
            _isReady = true;
            if (OnReady != null)
                OnReady();
        }
        private class DevToolsWebClient : CefClient { }

        public void Invoke(Action act)
        {
            //Ensure not blocking wait on browser thread.
            Task.Run(() =>
            {
                if (InvokeRequired)
                {
                    base.Invoke((MethodInvoker)delegate { act(); });
                }
                else
                    act();
            });
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
        }
        public void LoadUrl(string url)
        {
            RunOnReady(() =>
            {
                WebAppLogger.Log(WebAppLogLevel.Info, "Loading Url:");
                _browser.Browser.GetMainFrame().LoadUrl(url);
            });
        }
        
        public void Execute(string js)
        {
            if (_browser.Browser == null)
                throw new InvalidOperationException("Browser not ready");
            _browser.Browser.GetMainFrame().ExecuteJavaScript(js, _browser.Browser.GetMainFrame().Url, 0);
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
