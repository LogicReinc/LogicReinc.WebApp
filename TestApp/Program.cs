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
        static void Main(string[] args)
        {
            //Console.ReadLine();
            WebWindows.SetManager(new MixedWindowManager(PlatformID.Win32NT, true));

            for (int i = 0; i < 1; i++)
            {
                TestApp app = new TestApp();
                //VueTestApp app = new VueTestApp();
                app.Show();
            }
            Console.ReadLine();
            WebWindows.Exit();
        }
    }
}
