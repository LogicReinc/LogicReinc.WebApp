using System;
using System.Collections.Generic;
using System.Text;

namespace LogicReinc.WebApp
{
    public interface IWindowManager
    {
        IWebWindowImplementation Create();

        void Invoke(Action action);

        void Exit();
    }
}
