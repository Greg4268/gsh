using System.Diagnostics;
namespace src
{
    public class Shell
    {
        private static Dictionary<string, IBuiltinCommand> _builtins = new();
        private static Dictionary<int, CommandInfo> executionPlan = [];
        private readonly string[] operators = [">", "1>", "|", ">>"];
        public void Run()
        {
            _builtins = BuiltinRegistry.LoadBuiltins();
            Console.Clear();
            while (true) {
                Console.Write("$ ");
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(input.Trim())) continue; // skip whitespace and no input 

                executionPlan = parse.Run(input.Trim()); // execution plan will always have a value at this point. 
                List<KeyValuePair<int, CommandInfo>> items = executionPlan.OrderBy(c => c.Key).ToList(); 

                CommandReturnStruct resp; 
                resp.ReturnCode = -1;
                for ( int i = 0; i < items.Count; i++ ) {
                    CommandInfo current = items[i].Value;
                    bool hasNext = i + 1 < items.Count; 
                    // if there's an operator but no next item them we lose the operator? we wouldn't enter executesolo with current logic 

                    //Logger.Log($"has next, {hasNext}", LogLevel.Debug);
                    if (hasNext) {
                        CommandInfo next = items[i + 1].Value;
                        Logger.Log($"Next Command: {next.Command}", LogLevel.Debug);

                        if(operators.Contains(current.Operator)) resp = Execute(current, next);
                        else if (current.Operator == "||") {
                            // execute next only if current fails 
                            resp = Execute(current); 
                            if (resp.ReturnCode != 0) {
                                Execute(next);
                            }
                        }
                        else if (current.Operator == "&&") {
                            // execute next only if current succeeds 
                            resp = Execute(current); 
                            if (resp.ReturnCode == 0) {
                                Execute(next);
                            }
                        }
                    } 
                    else {
                        resp.ReturnCode = ExecuteSolo(current);
                    }
                    // do nothing with return code for now since bad commands are handled by execute functions. 
                    // 0 : success 
                    // 1 : fail 
                }
            }
        }
    
        static int ExecuteSolo(CommandInfo? ci) {
            CommandInfo item = ci ?? default;
            CommandReturnStruct resp;
            if (_builtins.ContainsKey(item.Command)) {
                resp = ExecuteBuiltin(item);
            }
            else if (utilities.IsCommandExecutableFromPATH(item.Command)) {
                resp = ExecuteInPATH(item);
            }
            else if (item.Command == "exit") {
                Environment.Exit(0);
                return default;
            }
            else {
                Console.WriteLine($"{item.Command}: command not found");
                return 1;
            }
            return OutputResponse(resp); 
        }

        static CommandReturnStruct Execute(CommandInfo item, CommandInfo? item2 = null) {
            CommandReturnStruct resp; 

            if (_builtins.ContainsKey(item.Command)) {
                resp = ExecuteBuiltin(item);
            }
            else if (utilities.IsCommandExecutableFromPATH(item.Command)) {
                resp = ExecuteInPATH(item);
            }
            else if (item.Command == "exit") {
                Environment.Exit(0);
                return default;
            }
            else {
                Console.WriteLine($"{item.Command}: command not found");
                return new CommandReturnStruct {
                    ReturnCode = 1,
                    Error = $"command {item.Command}: not found",
                    Output = [string.Empty]
                };
            }

            switch(item.Operator) {
                case "|": 
                    // TODO 
                    break; 
                case ">": 
                    utilities.WriteOutputToFile(item, resp);
                    break; 
                case ">>": 
                    // TODO 
                    break; 
                case "1>": 
                    // TODO 
                    break; 
                default: 
                    break; 
            }

            resp.ReturnCode = OutputResponse(resp); 
            return resp;
        }
        static CommandReturnStruct ExecuteBuiltin(CommandInfo item) {
            switch(item.Command) {
                case "echo":
                    return _builtins["echo"].Run(item.Args);
                case "type": 
                    return _builtins["type"].Run(item.Args);
                case "pwd": 
                    return _builtins["pwd"].Run(item.Args); 
                case "cd":
                    return _builtins["cd"].Run(item.Args);
                case "cat":
                    return _builtins["cat"].Run(item.Args);
                case "ls":
                    return _builtins["ls"].Run(item.Args);
                default:
                    break;
            }
            return new CommandReturnStruct {
                Output = [string.Empty], 
                ReturnCode = 1,
                Error = $"ExecuteBuiltin({item}) => Could not find a valid builtin matching {item.Command}."
            };
        }
        static CommandReturnStruct ExecuteInPATH(CommandInfo item) {
            var process = new Process();                       
            process.StartInfo.FileName = item.Command;              
            process.StartInfo.Arguments = string.Join(' ', item.Args);           
            process.StartInfo.UseShellExecute = false;     // enable/disable redirects,pipes,etc.     
            process.StartInfo.RedirectStandardOutput = true;  
            process.StartInfo.RedirectStandardError = true; 
            process.Start();                                   

            string[] output = [process.StandardOutput.ReadToEnd()];
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();  

            return new CommandReturnStruct {
                Output = output, 
                ReturnCode = !string.IsNullOrEmpty(error) ? 1 : 0, 
                Error = error 
            };
        }

        static int OutputResponse(CommandReturnStruct response) {
            if (response.Output != null && response.Output.Length > 0)
            {
                foreach (string item in response.Output)
                {
                    if (!string.IsNullOrEmpty(item))
                        Console.WriteLine(item);
                }

                return 0;
            }

            if (!string.IsNullOrEmpty(response.Error))
            {
                Console.WriteLine(response.Error);
            }

            return 1;
        }
    }
}