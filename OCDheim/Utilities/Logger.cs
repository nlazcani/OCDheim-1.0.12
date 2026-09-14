using System;
using BepInEx.Logging;

namespace OCDheim
{
    public static class Logger
    {
        // Debug used to be hardwired on. Every piece the placement ghost touches walks through these
        // calls, so the per-frame string interpolation showed up as stutter while building. Info by default,
        // flip it in BepInEx/config/dymek.dev.OCDheim.cfg when you need the chatter back.
        public static LogLevel logLevel { get; set; } = LogLevel.Info;

        public static void Debug(Func<string> func)
        {
            if (logLevel >= LogLevel.Debug)
            {
                Jotunn.Logger.LogDebug(func());
            }
        }
        
        public static void Info(Func<string> func)
        {
            if (logLevel >= LogLevel.Info)
            {
                Jotunn.Logger.LogInfo(func());
            }
        }
        
        public static void Warn(Func<string> func)
        {
            if (logLevel >= LogLevel.Warning)
            {
                Jotunn.Logger.LogWarning(func());
            }
        }
    }
}
