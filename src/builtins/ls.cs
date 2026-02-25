#pragma warning disable CS8981,IDE1006

namespace src.builtins
{
    public class ls : IBuiltinCommand
    {
        public string Name => "ls";
        public string Description => "List directories and files";
        public CommandReturnStruct Run(string[] args) {
            string error = string.Empty; 
            int returnCode;
            // if no file path arg then its 'ls' and we just show current directory 
            Dictionary<string, bool> LsArgs = new() {
                ["-1"] = true, 
                ["l"] = true 
            };
            // find the file path (if it exists) 
            string path = string.Empty; 
            string argsCombined = string.Join(" ", args); 
            if(!LsArgs.ContainsKey(argsCombined)){
                // add as path  
                path = argsCombined; 
            }
            // else, argsCombined must be just be path, now check if path is valid  
            if(string.IsNullOrEmpty(path)) path = "."; // is argsCombined was args, then set path to current directory 
            bool exists = File.Exists(path) || Directory.Exists(path); 

            List<string> output = [];

            if (exists) { // path would always exist here so long as argsCombined isn't an invalid path 
                if(args.Length > 0) { 
                    string arg = args[0]; // just assume one arg for ls before path 
                    switch(arg) {
                        case "-1": 
                            System.Console.WriteLine("case -1 hit");
                            foreach(var f in Directory.EnumerateFileSystemEntries(path)) {
                                output.Add(f);
                            }
                            returnCode = 0; 
                            break;
                        case "":
                            // no arg besides path?
                            string content = string.Join(" ", Directory.GetFiles(path));
                            output.Add(content); // just all space seperated in the first element 
                            returnCode = 0;
                            break;
                        case ".":
                            // no arg besides path?
                            output.Add(string.Join(" ", Directory.GetFiles(path))); // just all space seperated in the first element 
                            returnCode = 0;
                            break;
                        default: 
                            Console.WriteLine($"arg '{arg}' not recognized for ls");
                            returnCode = 1; 
                            break;
                    }
                }
                else{
                    Console.WriteLine("else hit."); // always getting hit right now 
                    string content = string.Join(" ", Directory.GetFiles(path));
                    output.Add(content); // just all space seperated in the first element 
                    returnCode = 0;
                }
            }
            else {
                error = $"ls: '{path}' does not exist.";
                returnCode = 1; 
            }

            return new CommandReturnStruct {
                Output = output.ToArray() ?? [string.Empty], 
                ReturnCode = returnCode, 
                Error = error
            };
        }
    }
}