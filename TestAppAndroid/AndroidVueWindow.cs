using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using LogicReinc.WebApp;
using LogicReinc.WebApp.Android;
using LogicReinc.WebApp.Javascript;
using LogicReinc.WebApp.Vue;

namespace TestAppAndroid
{
    [ContextResources("TestAppAndroid.Web", "")]
    [ResourcePage("TestAppAndroid.Web.VueTest.html")]
    public class AndroidVueWindow : VueWindow
    {
        public override string VueElementID => "#root";

        //Required due to Androids structure
        public AndroidVueWindow(IWebWindowImplementation window) : base(window) { }

        public int MyCounter { get; set; }

        public string TestString { get; set; } = "Test";
        

        public override void BeforeVue()
        {
            Console.WriteLine("Registering components");
            RegisterComponent(typeof(CounterComp));

            Console.WriteLine("Set VueMAterial");
            JS.Vue.use(new JSRef("VueMaterial.default"));

            //int test = JS.window.innerWidth;

        }


        [WebExpose]
        public void Increment()
        {
            MyCounter++;
        }
        [VueTemplate("TestAppAndroid.Web.VueTestComp.html")]
        public class CounterComp : VueComponent
        {
            public int Count { get; set; }

            public CounterComp(string id, VueWindow parent) : base(id, parent) { }


            public void Increment()
            {
                Count++;
            }


            public override void Mounted()
            {
                Console.WriteLine("Component Mounted");
            }
        }
    }
}