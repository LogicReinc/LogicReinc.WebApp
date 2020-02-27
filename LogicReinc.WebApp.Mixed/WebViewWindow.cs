using LogicReinc.WebApp;
using Microsoft.Toolkit.Forms.UI.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogicReinc.WebApp.Mixed
{
    public class WebViewWindow : Form, IWebWindowImplementation
    {
        private bool _isReady = false;
        private bool _needCleanup = true;
        private WebView _browser = null;

        public event Func<string, Task<object>> OnIPC;

        public WebWindow Controller { get; set; }

        private static string _polyfill = WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Mixed.polyfill.js");

        
        public WebViewWindow()
        {
            DoubleBuffered = true;
            _browser = new WebView();
            _browser.IsJavaScriptEnabled = true;
            _browser.IsScriptNotifyAllowed = true;

            _browser.AddInitializeScript(_polyfill);
            _browser.ScriptNotify += async (sender, wsn) =>
            {
                string notify = wsn.Value;
                string id = notify.Substring(0, notify.IndexOf(":"));
                notify = notify.Substring(notify.IndexOf(":") + 1);


                Task<object> resultT = null;
                if (OnIPC != null)
                    resultT = OnIPC(notify);

                if (resultT == null)
                    return;

                //resultT.Wait();
                object result = await resultT;//.Result;


                if (result != null && result.GetType() == typeof(NoIPCResponse))
                    return;

                if(!string.IsNullOrEmpty(id))
                    Execute(WebAppTemplates.FormatIf(
                        $"_IPCResolves[{id}]",
                        $"_IPCResolves[{id}]({JsonConvert.SerializeObject(result)});"));
            };
            this.FormClosing += (a, b) =>
            {
                Invoke(() =>
                {
                    this.Controls.Remove(_browser);
                    if (_needCleanup)
                    {
                        _browser.Process.Terminate();
                        _needCleanup = false;
                    }
                    _browser.Dispose();
                });
            };
            AppDomain.CurrentDomain.ProcessExit += (a, b) =>
            {
                if (_needCleanup)
                    _browser.Process.Terminate();
            };

            _browser.Dock = DockStyle.Fill;
            Controls.Add(_browser);

            RegisterInitScript(string.Format(WebContext.LoadStringResource(Assembly.GetExecutingAssembly(), "LogicReinc.WebApp.Mixed.IPCSetup.WebView.js")));
            _isReady = true;
        }


        public void Startup()
        {

        }

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

        public void RegisterInitScript(string script)
        {
            _browser.AddInitializeScript(script);
        }
        public void LoadHtml(string html)
        {
            _browser.NavigateToString(html);
        }
        public void LoadUrl(string url)
        {
            _browser.Navigate(url);
        }

        public JToken Execute(string js)
        {
            try
            {
                string result = _browser.InvokeScript("evalJson", js);

                if (string.IsNullOrEmpty(result))
                    return null;
                else
                    return JToken.Parse(result);
            }
            catch(AggregateException ex)
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
                throw;
            }
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

            //Try to get Windows API to do this instead
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



















        //User32
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;


        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        static extern IntPtr SetCapture(IntPtr hWnd);
    }
}
