using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace RightClickManager.Helpers
{
    public static class Logger
    {
        private static readonly string LogPath =
            Path.Combine(Path.GetTempPath(), "RightClickManager.log");

        private static readonly object _lock = new();

        static Logger()
        {
            try { File.Delete(LogPath); } catch { }
        }

        public static void Info(string msg, [CallerMemberName] string? caller = null)
        {
            Write("INFO", msg, caller);
        }

        public static void Error(string msg, string? exMsg = null, [CallerMemberName] string? caller = null)
        {
            Write("ERROR", $"{msg}{(exMsg != null ? " | " + exMsg : "")}", caller);
        }

        private static void Write(string level, string msg, string? caller)
        {
            try
            {
                var line = $"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId,2}] {level} {caller}: {msg}";
                lock (_lock)
                {
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
            }
            catch { }
        }

        public static string GetLogPath() => LogPath;
    }
}
