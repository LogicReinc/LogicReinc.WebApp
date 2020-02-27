using LogicReinc.WebApp.Mixed;
using LogicReinc.WebApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    [AppSize(480,260)]
    [ContextResources("TestApp.Web", "")]
    [ResourcePage("TestApp.Web.TestApp.html")]
    [Window(HasBorder = false)]
    [ExposeSecurity(WebExposeType.Attributed)]
    public class TestApp : WebWindow
    {
        Stopwatch watch = new Stopwatch();

        public TestApp()
        {
            watch.Start();

            Loaded += () =>
            {
                watch.Stop();
                Console.WriteLine("TimeToBoot:" + watch.ElapsedMilliseconds);
            };
        }
        /*
        [WebExpose]
        public void Print(string str)
        {
            Console.WriteLine(str);
        }*/
        [WebExpose]
        public void DoSomething()
        {
            /*
            JS.testFunction("TestCall");
            JS.testStructure.subFunction();

            JS.SomeValue = "Testing?";
            JS.SomeObj = new Object();
            JS.SomeObj.Str = "Whatever";


            Console.WriteLine((string)JS.SomeValue);
            Console.WriteLine((string)JS.SomeObj.Str);
            */
            Console.WriteLine((int)JS.window.outerWidth);

            Stopwatch watch = new Stopwatch();
            int count = 10000;
            watch.Start();
            for (int i = 0; i < count; i++)
            {
                int val = (int)JS.window.outerWidth;
            }
            watch.Stop();
            Console.WriteLine($"Browser communication {count} times in {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average {((double)watch.ElapsedMilliseconds / count).ToString("0.##")}ms per call");
            
        }
    }
}
