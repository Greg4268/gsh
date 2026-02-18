using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using src; 
#pragma warning disable CS8981

namespace src.builtins
{
    public class type
    {
        static readonly Dictionary<string, bool> commandsDict = new() // preferably not have this split between multiple files 
        {
            ["echo"] = true, 
            ["type"] = true, 
            ["pwd"] = true,
            ["cd"] = true,
            ["cat"] = true,
            ["ls"] = true, 
        }; 

        // Unix file permission constants
        private const int S_IXUSR = 0x40; // owner execute
        private const int S_IXGRP = 0x08; // group execute
        private const int S_IXOTH = 0x01; // others execute
        // Import chmod function for checking execute permissions
        [DllImport("libc", SetLastError = true)]
        private static extern int access(string pathname, int mode);
        private const int F_OK = 0; // check for file existence
        private const int X_OK = 1; // check for execute permission

        public static CommandReturnStruct Run(string[] args) {
            string[] output = [string.Empty]; 
            string error = string.Empty; 
            string command = string.Join(" ", args);                                               

            if (string.IsNullOrEmpty(command)) {     
                return new CommandReturnStruct {
                    Output = output,  
                    ReturnCode = 1, 
                    Error = $"{command}: not found"  
                };                                             
            }                                                               
                                                                            
            // Check if command is a builtin                                
            if (commandsDict.ContainsKey(command)) {              
                return new CommandReturnStruct {
                    Output = [$"{command} is a shell builtin"], 
                    ReturnCode = 0, 
                    Error = error
                };         
            }                                                               
                                                                            
            // Search in PATH                                               
            string path = GetExecutableFromPATH(command);                        
            if (!string.IsNullOrEmpty(path)) {      
                return new CommandReturnStruct {
                    Output = [$"{command} is {path}"], 
                    ReturnCode = 0, 
                    Error = error
                };
            } else {                                                        
                return new CommandReturnStruct {
                    Output = output, 
                    ReturnCode = 1, 
                    Error = $"{command}: not found"  // Fixed: no semicolon
                };  // Added closing semicolon here
            }                      
        }

        static string GetExecutableFromPATH(string command) {
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
    }
}