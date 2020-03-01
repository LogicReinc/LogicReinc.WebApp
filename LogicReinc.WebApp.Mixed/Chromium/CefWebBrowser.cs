using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xilium.CefGlue;
using WebBrowser = LogicReinc.WebApp.Chromium.WebBrowser;

namespace LogicReinc.WebApp.Chromium
{
    public sealed class CefWebBrowser : Control
    {
        private bool _handleCreated;

        private WebBrowser _core;
        private IntPtr _browserWindowHandle;

        public CefBrowser Browser => _core?.CefBrowser;

        private int _initialWidth = 0;
        private int _initialHeight = 0;


        public event Action BrowserReady;

        public event Func<CefProcessId, CefProcessMessage, bool> OnProcessMessage;

        private IWebWindowImplementation _window = null;

        public CefWebBrowser(IWebWindowImplementation window, int initialWidth = 200, int initialHeight = 100)
        {
            _window = window;
            SetStyle(
                ControlStyles.ContainerControl
                | ControlStyles.ResizeRedraw
                //| ControlStyles.FixedWidth
                //| ControlStyles.FixedHeight
                | ControlStyles.StandardClick
                | ControlStyles.UserMouse
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.StandardDoubleClick
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.CacheText
                | ControlStyles.EnableNotifyMessage
                | ControlStyles.DoubleBuffer
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UseTextForAccessibility
                | ControlStyles.Opaque,
                false);

            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.Selectable,
                true);

            _initialWidth = initialWidth;
            _initialHeight = initialHeight;

            var settings = new CefBrowserSettings();
            
            _core = new WebBrowser(this, settings, "about:blank");
            _core.OnProcessMessage += (a, b) =>
            {
                if (OnProcessMessage != null)
                    return OnProcessMessage(a, b);
                return false;
            };
            _core.Created += new EventHandler(BrowserCreated);
           
        }

        public string StartUrl
        {
            get { return _core.StartUrl; }
            set { _core.StartUrl = value; }
        }

        public WebBrowser WebBrowser { get { return _core; } }

        private CefWindowInfo _windowInfo = null;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (DesignMode)
            {
                // if (!_handleCreated) Paint += PaintInDesignMode;
            }
            else
            {
                _windowInfo = CefWindowInfo.Create();
                _windowInfo.SetAsChild(Handle, new CefRectangle { X = 0, Y = 0, Width = _initialWidth, Height = _initialHeight });
                WebAppLogger.Log(WebAppLogLevel.Info, $"WindowInfo: ({_initialWidth},{_initialHeight})");
                _core.Create(_windowInfo);
            }

            _handleCreated = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (_core != null && disposing)
            {
                _core.Close();
            }

            _core = null;
            _browserWindowHandle = IntPtr.Zero;

            base.Dispose(disposing);
        }

        internal void BrowserCreated(object sender, EventArgs e)
        {
            // _browser = browser;
            _browserWindowHandle = _core.CefBrowser.GetHost().GetWindowHandle();

            WebAppLogger.Log(WebAppLogLevel.Info, $"Chromium Handle: {_browserWindowHandle}");

            if (CefRuntime.Platform == CefRuntimePlatform.Linux)
                _display = XOpenDisplay(IntPtr.Zero);

            //ResizeWindow(_browserWindowHandle, Width, Height);

            if (BrowserReady != null)
                BrowserReady();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            var form = TopLevelControl as Form;
            if (form != null && form.WindowState != FormWindowState.Minimized)
                ResizeWindow(_browserWindowHandle, Width, Height);
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);



        [DllImport("libX11")]
        public static extern int XMoveResizeWindow(IntPtr display, IntPtr w, int x, int y, int width, int height);
        [DllImport("libX11")]
        public static extern int XResizeWindow(IntPtr display, IntPtr w, int width, int height);
        [DllImport("libX11")]
        public static extern IntPtr XOpenDisplay(IntPtr display);


        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_widget_get_display(IntPtr window);
        [DllImport("libgdk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gdk_x11_window_get_xid(IntPtr raw);
        [DllImport("libgdk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdkDisplay);


        private IntPtr _display;

        private void ResizeWindow(IntPtr handle, int width, int height)
        {
            if (handle != IntPtr.Zero)
            {
                if (CefRuntime.Platform == CefRuntimePlatform.Windows)
                {
                    //Console.WriteLine($"Win32 Resize ({width},{height})");
                    SetWindowPos(handle, IntPtr.Zero, 0, 0, width, height, 0x0002 | 0x0004);
                }
                else if (CefRuntime.Platform == CefRuntimePlatform.Linux)
                {
                    Console.WriteLine($"X11 Resize ({width},{height}) Display: {_display}, Window: {handle}" );

                    //Move to init when working
                    IntPtr display = XOpenDisplay(IntPtr.Zero);
                    IntPtr window = _core.CefBrowser.GetHost().GetWindowHandle();


                    //Neither seem to work..
                    //XResizeWindow(display, window, width, height);
                    SetWindowPos(window, IntPtr.Zero, 0, 0, width, height, 0x0002 | 0x0004);

                    _core.CefBrowser?.GetHost().WasResized();
                }
                
            }
        }
    }
}
