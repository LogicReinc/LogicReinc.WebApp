using LogicReinc.WebApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogicReinc.WebApp.Mixed
{
    public class MixedWindowManager : IWindowManager
    {
        public PlatformID Platform { get; set; }
        public bool Is64Bit { get; set; }


        //Windows
        public Thread MainThread { get; private set; }
        private Form MainForm { get; set; }

        public MixedWindowManager(PlatformID platform, bool is64)
        {
            Platform = platform;
            Is64Bit = is64;
            Initialize();
        }
        
        public void Initialize()
        {
            switch (Platform)
            {
                case PlatformID.Win32NT:
                    InitializeWindows();
                    break;
                default:
                    throw new NotImplementedException("Not implemented");
            }
        }
        public void InitializeWindows()
        {
            AutoResetEvent ev = new AutoResetEvent(false);
            MainForm = new Form();
            MainForm.FormBorderStyle = FormBorderStyle.None;
            MainForm.Size = new System.Drawing.Size(0, 0);
            MainForm.Load += (a, b) =>
            {
                ev.Set();
            };

            MainThread = new Thread(() =>
            {
                Application.Run(MainForm);
            });
            MainThread.ApartmentState = ApartmentState.STA;
            MainThread.Start();
            ev.WaitOne();
            Invoke(() =>
            {
                MainForm.Hide();
            });
        }

        public void Invoke(Action action)
        {
            MainForm.Invoke((MethodInvoker)delegate
            {
                action();
            });
        }

        public void Exit()
        {
            Application.Exit();
        }

        public IWebWindowImplementation Create()
        {
            switch (Platform)
            {
                case PlatformID.Win32NT:
                    return CreateWindows();
                default:
                    throw new NotImplementedException("Not implemented");
            }
        }

        private IWebWindowImplementation CreateWindows()
        {
            AutoResetEvent ev = new AutoResetEvent(false);
            WebViewWindow window = null;
            Exception x = null;
            Invoke(() =>
            {
                try
                {
                    window = new WebViewWindow();
                    var obs = window.FormBorderStyle;
                    window.FormBorderStyle = FormBorderStyle.None;
                    window.Size = new System.Drawing.Size(0, 0);
                    window.HandleCreated += (a, b) =>
                    {
                        ev.Set();
                    };
                    window.Show();
                    window.Hide();
                    window.FormBorderStyle = obs;
                }
                catch(Exception ex)
                {
                    x = ex;
                    ev.Set();
                }
            });
            ev.WaitOne();

            if (x != null && window == null)
                throw x;
            return window;
        }
    }
}
