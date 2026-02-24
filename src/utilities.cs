using System.Runtime.InteropServices;

#pragma warning disable CS8981, CA2101, SYSLIB1054, IDE1006

namespace src
{
    public class utilities
    {
        // Import chmod function for checking execute permissions
        [DllImport("libc", SetLastError = true)]
        private static extern int access(string pathname, int mode);
        private const int X_OK = 1; 

        public static bool IsCommandExecutableFromPATH(string command) {
            string? paths = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(paths)){
                return false;
            }

            foreach (var path in paths.Split(':')) {
                string fullPath = Path.Combine(path, command);
                if (File.Exists(fullPath)) {
                    if(access(fullPath, X_OK) == 0) { 
                        Logger.Log("Command is executable from PATH", LogLevel.Debug);
                        return true;
                    }
                }
            }
            return false;
        }

        public static string GetExecutableFromPATH(string command) {
            string? paths = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(paths)) {
                return string.Empty; 
            }
            foreach (var path in paths.Split(':')) {
                string fullPath = Path.Combine(path, command);
                if (File.Exists(fullPath)) {
                    if(access(fullPath, X_OK) == 0) {
                        return fullPath; 
                    }
                }
            }
            return string.Empty;
        }

        public static int WriteOutputToFile(CommandInfo item, CommandReturnStruct response) {
            if(!string.IsNullOrEmpty(item.RedirectFileName)){
                string content = string.Join(" ", response.Output);
                try {
                    File.WriteAllText(item.RedirectFileName, content);
                    return 0;
                }
                catch (Exception e) {
                    Logger.Log($"Exception: {e}", LogLevel.Error);
                    return 1; 
                }
            }
            else {
                Logger.Log($"error: cannot write to file '{item.RedirectFileName}'", LogLevel.Error);
                return 1;
            }
        }
    }

    public enum LogLevel {
        Error, 
        Debug, 
        None
    }

    public static class Logger
    {
        public static LogLevel CurrentLevel = LogLevel.None;

        public static void Log(string message, LogLevel level)
        {
            if (level >= CurrentLevel)
            {
                Console.WriteLine($"[{level}] {message}");
            }
        }
    }
}