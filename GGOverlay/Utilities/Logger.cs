using System;

namespace GGOverlay.Utilities
{
    public static class Logger
    {
        public static event Action<string> OnLogMessage;

        public static void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }
    }
}