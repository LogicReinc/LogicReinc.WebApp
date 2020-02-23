﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicReinc.WebApp
{
    public interface IWebWindowImplementation
    {
        event Func<string, object> OnIPC;

        int CaptionHeight { get; set; }
        int ResizeGrip { get; set; }

        void SetSize(int x, int y);
        void SetPosition(int x, int y);

        void StartDragMove();
        void StopDragMove();

        void ShowBorder();
        void RemoveBorder();

        void Maximize();
        void Minimize();
        void Show();
        void Hide();
        void Focus();
        void Close();

        void BringToFront();

        JToken Execute(string js);

        void RegisterInitScript(string script);

        void LoadHtml(string html);
        void LoadUrl(string url);

        void Invoke(Action act);
    }
}