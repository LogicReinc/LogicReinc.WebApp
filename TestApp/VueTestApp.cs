using LogicReinc.WebApp;
using LogicReinc.WebApp.Javascript;
using LogicReinc.WebApp.Vue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    [AppSize(400, 300)]
    [ContextResources("TestApp.Web", "")]
    [ResourcePage("TestApp.Web.VueTest.html")]
    [ExposeSecurity(WebExposeType.Attributed)]
    [Window(HasBorder = true)]
    [VueSettings(RequireExpose = false)]
    public class VueTestApp : VueWindow
    {
        public override string VueElementID => "#root";

        public int MyCounter { get; set; }

        public string TestString { get; set; } = "Test";

        public VueTestApp()
        {
            RegisterComponent(typeof(CounterComp));
        }


        public override void BeforeVue()
        {
            JS.Vue.use(new JSRef("VueMaterial.default"));
        }


        [WebExpose]
        public void Increment()
        {
            MyCounter++;
        }
        [VueTemplate("TestApp.Web.VueTestComp.html")]
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
