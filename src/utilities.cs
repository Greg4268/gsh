using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

#pragma warning disable CS8981

namespace src
{
    public class utilities
    {
        // Unix file permission constants
        private const int S_IXUSR = 0x40; // owner execute
        private const int S_IXGRP = 0x08; // group execute
        private const int S_IXOTH = 0x01; // others execute
        // Import chmod function for checking execute permissions
        [DllImport("libc", SetLastError = true)]
        private static extern int access(string pathname, int mode);
        private const int F_OK = 0; // check for file existence
        private const int X_OK = 1; // check for execute permission

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

        public static int WriteOutputToFile(CommandInfo item, CommandReturnStruct response) {
            if(!string.IsNullOrEmpty(item.RedirectFileName)){
                string content = string.Join(" ", response.Output);
                try {
                    File.WriteAllText(item.RedirectFileName, content);
                    return 0;
                }
                catch (Exception e) {
                    Console.WriteLine($"Exception: {e}");
                    return 1; 
                }
            }
            else {
                Console.WriteLine($"error: cannot write to file '{item.RedirectFileName}'");
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