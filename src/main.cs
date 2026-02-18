using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using src;
using src.builtins;
#pragma warning disable CS8981, CA2101, SYSLIB1054, IDE0305
partial class Program
{
    static readonly Dictionary<string, bool> commandsDict = new()
    {
        ["echo"] = true, 
        ["type"] = true, 
        ["pwd"] = true,
        ["cd"] = true,
        ["cat"] = true,
        ["ls"] = true, 
    }; 

    // use a dictionary to store parsed user input for execution 
    public static Dictionary<int, CommandInfo> executionPlan = [];

    static void Main()
    { 
        Console.Clear();
        while (true) {
            Console.Write("$ ");
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue; 

            executionPlan = parse.Run(input.Trim());
            if ( executionPlan.Count == 0 ) {
                continue; // dict is empty, just request new user input 
            }
             
            List<KeyValuePair<int, CommandInfo>> items = executionPlan
                                                            .OrderBy(c => c.Key)
                                                            .ToList();

            CommandReturnStruct resp; 
            resp.ReturnCode = -1; 
            int soloExecutionReturnCode = -1; 

            for ( int i = 0; i < items.Count; i++ ) {
                CommandInfo current = items[i].Value;
                bool hasNext = i + 1 < items.Count; 

                Logger.Log($"Current Command: {current.Command}", LogLevel.Debug);
                Logger.Log($"HasNext? => {hasNext}", LogLevel.Debug);

                if (hasNext) {
                    CommandInfo next = items[i + 1].Value;
                    Logger.Log($"Next Command: {next.Command}", LogLevel.Debug);
                    
                    if (current.Operator == ">" || current.Operator == "1>") {
                        // output stdout to file current.Command (echo) current.Operator (>) current.Args(['some text', 'file.txt'])? 
                        // so, I'd have to determine the type of input it is (valid, invalid, which it is too)?
                        resp = Execute(current, next, true, false, false);
                    }
                    else if (current.Operator == ">>") {
                        // append stdout to file 
                        resp = Execute(current, next, false, true, false);
                    }
                    else if (current.Operator == "|") {
                        // pipe current stdout to next stdin 
                        resp = Execute(current, next, false, false, true);
                    }
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
                    // no command after, execute normally 
                    soloExecutionReturnCode = ExecuteSolo(current);
                }
                if (resp.ReturnCode != 0 || soloExecutionReturnCode != 0) {
                    // there was an error. Cancel execution 
                    Logger.Log("Invalid command. Please try again.", LogLevel.Error);
                }
            }
        }
    }

    // execute normally, just return a status code (int) 
    static int ExecuteSolo(CommandInfo? ci) {
        CommandInfo item = ci ?? default;
        CommandReturnStruct resp;
        // 0 : success 
        // 1 : fail 
        // Logger.Log($"is {item.Command} a builtin: {commandsDict.ContainsKey(item.Command)}", LogLevel.Debug);
        if (commandsDict.ContainsKey(item.Command)) {
            // is a builtin, run through switch-case 
            // Logger.Log($"Executing builtin {item.Command}", LogLevel.Debug);
            resp = ExecuteBuiltin(item);
        }
        else if (utilities.IsCommandExecutableFromPATH(item.Command)) {
            Logger.Log($"Executing in PATH {item.Command}", LogLevel.Debug);
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

        bool isOperatorNullOrEmpty = string.IsNullOrEmpty(item.Operator);
        if (!isOperatorNullOrEmpty && item.Operator.Contains('>')) {
            return utilities.WriteOutputToFile(item, resp);
        }
        if (!isOperatorNullOrEmpty && item.Operator.Contains('|')) {
            //return PipeToCommand() // idk if this is how I want to do pipe yet ... instead maybe do it different or mark pipe-to command as next command and have it handled upstream in main loop 
        }

        return OutputResponse(resp); // don't output the response here if this method will get called by other Execute methods?... since output may need to be passed into redirect 
    }

    // overload method for Execute to handle redirect, append to file, pipe
    static CommandReturnStruct Execute(
        CommandInfo item, 
        CommandInfo? item2 = null, 
        bool? redirect = false, 
        bool? append = false, 
        bool? pipe = false
        ) {

        CommandReturnStruct response = new()
        {
            Output = [string.Empty], 
            ReturnCode = -1, 
            Error = string.Empty 
        };
        // 0 : success 
        // 1 : fail 
        if (commandsDict.ContainsKey(item.Command)) {
            // is a builtin, run through switch-case 
            response = ExecuteBuiltin(item);
        }
        else if (utilities.IsCommandExecutableFromPATH(item.Command)) {
            response = ExecuteInPATH(item);
        }
        else if (item.Command == "exit") {
            Environment.Exit(0);
        }
        else {
            Console.WriteLine($"{item.Command}: command not found");
            response.ReturnCode = 1;
            response.Error = $"command {item.Command}: not found";
        }

        if (redirect == true || append == true || pipe == true) {
            // not a console write, route output into next. 
            // CommandInfo ci = item2 ?? default;
            // int idx = -1; 
            // get index of next available spot 
            // for (int i = 0; i < ci.Args.Length; i++){
            //     if (ci.Args[i] == null) { // maybe do IsNullOrEmpty instead? 
            //         if (i + 2 < ci.Args.Length) idx = i; 
            //         break; 
            //     }
            // }
            // if (idx != -1){
            //     //ci.Args[idx] = [response.Output]; 
            //     ci.Args[idx] = "placeholder";
            //     ci.Args[idx + 1] = response.ReturnCode.ToString(); 
            //     ci.Args[idx + 2] = response.Error ?? string.Empty;
            // }
            // else {
            //     Console.WriteLine("idx was -1. Array is maybe full?");
            // }
            // if(item2 != null){
            //     ExecuteSolo(item2); // no need for return, it will handle it's own response output 
            // }
            utilities.WriteOutputToFile(item, response); 
        }
        return new CommandReturnStruct {
            Output = [string.Empty],
            ReturnCode = OutputResponse(response), // dont output the response here if this method will get called by other Execute methods?... since output may need to be passed into redirect 
            Error = string.Empty,
        };
    }
    static CommandReturnStruct ExecuteBuiltin(CommandInfo item) {
        switch(item.Command) {
            case "echo":
                return echo.Run(item.Args);
            case "type": 
                return type.Run(item.Args);
            case "pwd": 
                return pwd.Run();
            case "cd":
                return cd.Run(item.Args);
            case "cat":
                return cat.Run(item.Args);
            case "ls":
                return ls.Run(item.Args);
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
        process.StartInfo.UseShellExecute = false;     // enable / disable redirects,pipes,etc.     
        process.StartInfo.RedirectStandardOutput = true;  
        process.StartInfo.RedirectStandardError = true; 

        process.Start();                                   

        string[] output = [process.StandardOutput.ReadToEnd()];
        string error = process.StandardError.ReadToEnd();

        Logger.Log($"output: {output[0]}", LogLevel.Debug);

        process.WaitForExit();  

        // display errors 
        if (!string.IsNullOrEmpty(error)){
            Logger.Log("stderr output is not null or empty. something happened", LogLevel.Debug);
            return new CommandReturnStruct {Output = output, ReturnCode = 1, Error = error};
        }
        return new CommandReturnStruct {Output = output, ReturnCode = 0, Error = error};
    }

    static int OutputResponse(CommandReturnStruct response) {
        // now, handle the output of the command (stdout, stderr)
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

    [GeneratedRegex(@"(?<=\S)(?=\s)|(?<=\s)(?=\S)")]
    private static partial Regex MyRegex();
}