using System;
using System.Collections.Generic;
using System.Text;

namespace LogicReinc.WebApp
{
    public static class WebWindows
    {
        public static IWindowManager Manager { get; set; }

        public static void SetManager(IWindowManager manager)
        {
            Manager = manager;
        }

        public static IWebWindowImplementation GetWindow()
        {
            if (Manager == null)
                throw new InvalidOperationException("Use WebWindows.SetManager() first");
            return Manager.Create();
        }

        public static void Exit()
        {
            if (Manager == null)
                throw new InvalidOperationException("Use WebWindows.SetManager() first");
            Manager.Exit();
        }
    }
}
