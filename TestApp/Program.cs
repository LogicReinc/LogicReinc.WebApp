using LogicReinc.WebApp.Mixed;
using LogicReinc.WebApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    public class Program
    {
        static bool _launched = false;

        static bool forceChromium = true;

        [STAThread]
        static void Main(string[] args)
        {
            if (_launched)
            {
                Console.WriteLine("Already launched...exiting");
                return;
            }
            else
                _launched = true;

            PlatformID platform = Environment.OSVersion.Platform;
            bool Is64 = Environment.Is64BitProcess;//IntPtr.Size == 8;

            Console.WriteLine($"Platform:{platform} is64:{Is64}");

            //Console.ReadLine();

            try
            {
                WebWindows.SetManager(new MixedWindowManager(platform, Is64, args, forceChromium));

                for (int i = 0; i < 1; i++)
                {
                    Console.WriteLine("Starting application");
                    //TestApp app = new TestApp();
                    VueTestApp app = new VueTestApp();
                    app.Show();
                }
                Console.WriteLine("Launching done");
                string cmd = null;
                while((cmd = Console.ReadLine()) != "exit")
                {
                    if (cmd.StartsWith("echo "))
                        Console.WriteLine(cmd.Substring(5));
                }
                Console.WriteLine("Attempting exiting..");
                WebWindows.Exit();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception:" + ex.GetType().Name);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }

            Console.WriteLine("End");
        }
    }
}
