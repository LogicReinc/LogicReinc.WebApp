using System;
using System.Collections.Generic;
using System.Text;

namespace LogicReinc.WebApp
{
    public static class WebAppLogger
    {
        public static WebAppLogLevel Level { get; set; } = WebAppLogLevel.Verbose;
        public static bool Print { get; set; } = true;

        public static event Action<WebAppLogLevel, string> OnLog;

        public static void Log(WebAppLogLevel level, string log)
        {
            if (level < Level)
                return;

            if (OnLog != null)
                OnLog(level, log);
            if (Print)
            {
                if (level == WebAppLogLevel.Error)
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[{level}]{log}");
                if (level == WebAppLogLevel.Error)
                    Console.ResetColor();
            }
        }
    }
    public enum WebAppLogLevel
    {
        Verbose = 0,
        Info = 1,
        Error = 2
    }
}
