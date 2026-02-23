using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using src; 
#pragma warning disable CS8981, IDE1006

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
            string path = utilities.GetExecutableFromPATH(command);                        
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
    }
}