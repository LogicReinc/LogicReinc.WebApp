using LogicReinc.WebApp;
using LogicReinc.WebApp.Mixed.Chromium;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xilium.CefGlue;
using Xilium.CefGlue.Wrapper;

namespace LogicReinc.WebApp.Mixed
{
    public class MixedWindowManager : IWindowManager
    {
        public static bool ZipDependencies { get; set; } = true;

        public PlatformID Platform { get; set; }
        public bool Is64Bit { get; set; }

        //Windows
        public Thread MainThread { get; private set; }
        private Form MainForm { get; set; }

        public string[] Args { get; set; }


        public bool ForceChromium { get; set; } = false;


        public MixedWindowManager(PlatformID platform, bool is64, string[] args, bool forceChromium)
        {
            ForceChromium = forceChromium;
            Args = args;
            Platform = platform;
            Is64Bit = is64;
            Initialize();
        }
        
        public void Initialize()
        {
            if (ForceChromium)
            {
                PrepareCEF();
                StartChromiumThread(Args);
            }
            else
            {
                switch (Platform)
                {
                    case PlatformID.Win32NT:
                        StartMainThreadWindows();
                        break;
                    case PlatformID.Unix:
                        PrepareCEF();
                        StartChromiumThread(Args);
                        break;
                    default:
                        throw new NotImplementedException("Not implemented");
                }
            }
        }

        public void StartMainThreadWindows()
        {
            Exception ex = null;
            AutoResetEvent ev = new AutoResetEvent(false);
            Thread thread = new Thread(() =>
            {
                try
                {
                    MainForm = new Form();
                    MainForm.Width = 0;
                    MainForm.Height = 0;
                    MainForm.FormBorderStyle = FormBorderStyle.None;
                    MainForm.Opacity = 0;
                    MainForm.Load += (s, e) =>
                    {
                        ev.Set();
                        MainForm.Hide();
                    };
                    Application.Run(MainForm);
                }
                catch (Exception x)
                {
                    ex = x;
                    ev.Set();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ev.WaitOne();
            if (ex != null)
                throw ex;
        }

        private ConcurrentQueue<Action> _invokeQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();
        public void StartChromiumThread(string[] args)
        {
            Exception ex = null;
            AutoResetEvent ev = new AutoResetEvent(false);
            Thread thread = new Thread(() =>
            {
                try
                {
                    CefRuntime.Load();

                    var argv = args;
                    if (CefRuntime.Platform != CefRuntimePlatform.Windows)
                    {
                        argv = new string[args.Length + 1];
                        Array.Copy(args, 0, argv, 1, args.Length);
                        argv[0] = "-";
                    }
                    var mainArgs = new CefMainArgs(argv);

                    ChromiumApp app = new ChromiumApp();

                    var exitCode = CefRuntime.ExecuteProcess(mainArgs, app, IntPtr.Zero);
                    if (exitCode != -1)
                        return;

                    // guard if something wrong
                    foreach (var arg in args) { if (arg.StartsWith("--type=")) { return; } }

                    DirectoryInfo baseDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory;


                    Console.WriteLine("Initializing");
                    CefRuntime.Initialize(mainArgs, new CefSettings()
                    {
                        MultiThreadedMessageLoop = CefRuntime.Platform == CefRuntimePlatform.Windows,
                        SingleProcess = true,
                        LogSeverity = CefLogSeverity.Error,
                        LogFile = "cef.log",
                        ResourcesDirPath = baseDir.FullName,
                        NoSandbox = true,
                        RemoteDebuggingPort = 52345
                    }, app, IntPtr.Zero);

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    if (CefRuntime.Platform != CefRuntimePlatform.Windows)
                        Application.Idle += (s, e) =>
                        {
                            if (_invokeQueue.Count > 0)
                            {
                                Action eq = null;
                                if (_invokeQueue.TryDequeue(out eq))
                                    try
                                    {
                                        eq();
                                    }
                                    catch (Exception x)
                                    {
                                        Console.WriteLine("Invoke exception: " + x.Message);
                                    }
                            }
                            try
                            {
                                CefRuntime.DoMessageLoopWork();
                            }
                            catch (Exception x)
                            {
                                Console.WriteLine("Exception in MSG loop:" + x.Message);
                            }
                        };

                    //ev.Set();

                    if (CefRuntime.Platform == CefRuntimePlatform.Windows)
                    {

                        MainForm = new Form();
                        MainForm.Width = 0;
                        MainForm.Height = 0;
                        MainForm.FormBorderStyle = FormBorderStyle.None;
                        MainForm.Opacity = 0;
                        MainForm.Load += (s, e) =>
                        {
                            ev.Set();
                            MainForm.Hide();
                        };
                        Application.Run(MainForm);
                    }
                    else
                    {
                        ev.Set();
                        Application.Run();
                    }
                    CefRuntime.Shutdown();
                }
                catch (Exception x)
                {
                    ex = x;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ev.WaitOne();
            if (ex != null)
                throw ex;
        }


        public void Invoke(Action action)
        {
            if (Platform == PlatformID.Unix)
                _invokeQueue.Enqueue(action);
            else
                MainForm.Invoke(action);
        }

        public void Exit()
        {
            if (Platform == PlatformID.Unix)
                ExitLinux();
            else
                ExitWindows();
        }

        public void ExitWindows()
        {
            Application.Exit();
        }
        public void ExitLinux()
        {
            CefRuntime.Shutdown();
            Application.Exit();
        }

        public IWebWindowImplementation Create()
        {
            switch (Platform)
            {
                case PlatformID.Win32NT:
                    if (!ForceChromium)
                        return CreateWindows();
                    else
                        return CreateChromium();
                case PlatformID.Unix:
                    return CreateChromium();
                default:
                    throw new NotImplementedException("Not implemented");
            }
        }

        private IWebWindowImplementation CreateChromium()
        {
            Console.WriteLine("Creating Linux Window..");
            AutoResetEvent ev = new AutoResetEvent(false);
            ChromiumWindow window = null;
            Exception x = null;

            Action startup = () =>
            {
                try
                {
                    window = new ChromiumWindow();
                    var obs = window.FormBorderStyle;
                    //window.FormBorderStyle = FormBorderStyle.None;
                    window.Size = new System.Drawing.Size(0, 0);
                    window.HandleCreated += (a, b) =>
                    {
                        ev.Set();
                    };
                    window.Opacity = 0;
                    window.Show();
                    window.Hide();
                    window.Opacity = 1;
                    window.FormBorderStyle = obs;
                }
                catch (Exception ex)
                {
                    x = ex;
                    ev.Set();
                }
            };

            Invoke(startup);
            ev.WaitOne();

            if (x != null && window == null)
                throw x;
            return window;
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
                    window.Opacity = 0;
                    window.Show();
                    window.Hide();
                    window.Opacity = 1;
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

        //CEF Prep
        private const string Linux64Path = "cef_Linux64";
        private const string Windows64Path = "cef_Windows64";
        public static void PrepareCEF()
        {
            DirectoryInfo baseDir = new FileInfo(Assembly.GetEntryAssembly().Location).Directory;

            if (File.Exists(Path.Combine(baseDir.FullName, "libcef")) || File.Exists(Path.Combine(baseDir.FullName, "libcef.dll")))
                return;

            string depPath = Path.Combine(baseDir.FullName, GetOSDepPath());

            Console.WriteLine("DependencyPath:" + depPath);


            if (ZipDependencies)
            {
                FileInfo depZip = new FileInfo(depPath + ".zip");
                if (!depZip.Exists)
                    throw new FileNotFoundException("Missing dependency zip " + depZip.Name);
                ZipFile.ExtractToDirectory(depZip.FullName, baseDir.FullName);
            }
            else
            {
                DirectoryInfo depDir = new DirectoryInfo(depPath);
                if (!depDir.Exists)
                    throw new DirectoryNotFoundException("Missing dependency directory");

                foreach (FileInfo info in depDir.GetFiles())
                    info.MoveTo(Path.Combine(baseDir.FullName, info.Name));
            }
        }

        private static string GetOSDepPath()
        {
            bool is64 = IntPtr.Size == 8;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    if (is64)
                        return Windows64Path;
                    else
                        throw new NotImplementedException("Windows 32bit not available");
                case PlatformID.MacOSX:
                    throw new NotImplementedException("Mac not implemented yet");
                case PlatformID.Unix:
                    if (is64)
                        return Linux64Path;
                    else
                        throw new NotImplementedException("Linux 32bit not available");
                default:
                    throw new NotImplementedException("Windows not yet available");
            }
        }
    }
}
