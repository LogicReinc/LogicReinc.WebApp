using LogicReinc.WebApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestAppAndroid
{
    [ContextResources("TestAppAndroid.Web", "")]
    [ResourcePage("TestAppAndroid.Web.TestApp.html")]
    [ExposeSecurity(WebExposeType.Attributed)]
    public class AndroidWindow : WebWindow
    {
        Stopwatch watch = new Stopwatch();

        public AndroidWindow(IWebWindowImplementation window) : base(window)
        {
            watch.Start();

            Loaded += () =>
            {
                watch.Stop();
                Console.WriteLine("TimeToBoot:" + watch.ElapsedMilliseconds);
            };
        }

        [WebExpose]
        public void DoSomething()
        {
        }
    }
}
