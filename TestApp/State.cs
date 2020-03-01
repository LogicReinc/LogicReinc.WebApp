using LogicReinc.WebApp;
using LogicReinc.WebApp.Android;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    public static class State
    {
        static AndroidWindowManager _windowManager;
        public static void Init()
        {
            _windowManager = new AndroidWindowManager();
            WebWindows.SetManager(_windowManager);
        }

    }
}
