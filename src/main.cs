using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
partial class Program
{
    static readonly Dictionary<string, bool> commandsDict = new()
    {
        ["echo"] = true, 
        ["type"] = true, 
        ["exit"] = true,
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

    public struct CommandReturnStruct {
        public string[] Output;
        public int ReturnCode; 
        public string Error; 
    }

    public struct CommandInfo { 
        public string Command; 
        public string[] Args;
        public string Operator;
    }

    // use a dictionary to store parsed user input for execution 
    public static Dictionary<int, CommandInfo> executionPlan = [];

    static void Main()
    { 
        Console.Clear();
        while (true) {
            Console.Write("$ ");
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue; 

            executionPlan = ExtractCommandsAndArgsFromUserInput(input.Trim());
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

    // TODO: build parser 
    static Dictionary<int, CommandInfo> ExtractCommandsAndArgsFromUserInput(string? text) { 
        // need a better way to parse args so that we don't have to handle redirect repetitively inside of each method. 
        // ex checking if args contains > or 1> and trying to parse the content before and after it 
        // we should take the users input, extract first command, look for valid arguments or another command then keep moving through string passed 
        // ex. echo 'this is ' 'some text > mytextfile.txt 
        // 1) parse echo, see its a command, store to table
        // 2) search for arguments 'this is ' and 'some text' are both valid arguments which get concatentated then added to table as the value for the command 
        // 3) continue forward and find '>' redirect, store to table
        // 4) search for arguments mytextfile.txt -> FindOrCreate, store to 'mytextfile.txt' to table 
        // 5) execution: run left to right, echo 'this is some text' redirecting standard output to mytextfile.txt which was made if didn't already exist prior to execution 

        // nothing in here so return everything as empty 
        if(string.IsNullOrEmpty(text)){
            return new Dictionary<int, CommandInfo> {
                [0] = new CommandInfo {
                    Command = string.Empty, 
                    Args = [string.Empty], 
                    Operator = string.Empty
                }
            };
        }
        
        List<string> commands = [];  
        List<List<string>> args = []; 
        List<string> operators = [];
        int summedLength = 0; 

        // after trimming, there is not a space (somewhere in middle) -> if there is a command it is by itself and does not have args or operator 
        if ( !text.Contains(' ') ) {
            //single work, command only probably 
            return new Dictionary<int, CommandInfo> {
                [0] = new CommandInfo {
                    Command = text, 
                    Args = [string.Empty], 
                    Operator = string.Empty
                }
            };
        }
        
        // continually parse out command, args, operators and build execution plan 
        while (text.Length > summedLength) 
        {
            // build out one dictionary entry at a time 

            // 1) find the command 
            for(int i = 0; i < text.Length; i++){
                summedLength++;
                if(text[i] == ' ') {
                    // we found a command, add it to the list and move onto args or operator 
                    commands.Add(text[..i]);
                    text = text[text[..i].Length..]; // slice off command which was extracted 
                    text = text.Trim(); // trim leading or trailing space 
                    break; // don't want to parse args or operators as more commands 
                }
            }

            // 2) find the (optional) args
            bool hasOperator = false; 
            int currLen = text.Length; 
            List<string> argList = []; // build local list to add onto master list 
            for(int i = 0; i < currLen; i++){
                summedLength++;
                if (text[i] == ' ') {
                    argList.Add(text[..i]); 
                    // must continue to parse args up to an operator being found 
                    text = text[text[..i].Length..]; // slice up to this point 
                    text = text.Trim();
                    currLen = text.Length; 
                }
                else if (text[i] == '>' || text[i] == '|'){ // just do this for now not considering if it's a valid part of the commands args  (ex. echo "this | text")
                    // operator found, stop parsing args after this point 
                    argList.Add(text[..i]);
                    text = text[text[..i].Length..]; // slice up to this point 
                    text = text.Trim();
                    hasOperator = true; 
                    break; 
                }
                else if(i + 1 == text.Length) {// we now reached the end and no operator so the inclusive of last index we have another arg to add
                    int j = i + 1;  
                    argList.Add(text[..j]);
                    text = string.Empty;
                    break; 
                }
            }
            args.Add(argList);

            // 3) get the operator 
            if(hasOperator) {
                for(int i = 0; i < text.Length; i++) {
                    summedLength++;
                    if(text[i] == ' ') {// passed operator, take from slice 
                        operators.Add(text[..i]);
                        break;
                    }
                }
            }
        }

        // 4) put execution plan together 
        var executionPlan = new Dictionary<int, CommandInfo>();
        for(int i = 0; i < commands.Count; i++) {
            executionPlan[i] = new CommandInfo {
                Command = commands[i].ToLower() ?? string.Empty, 
                Args = i < args.Count ? [.. args[i]] : [string.Empty], // ToArray equivalent 
                Operator = i < operators.Count ? operators[i] : string.Empty
            };
        }


        Logger.Log($"Command: {executionPlan[0].Command}", LogLevel.Debug);
        Logger.Log("Args: ", LogLevel.Debug);
        foreach ( string str in executionPlan[0].Args) {
            Logger.Log(str, LogLevel.Debug);
        }
        Logger.Log($"Operator: {executionPlan[0].Operator}", LogLevel.Debug);

        return executionPlan; 
    }

    // execute normally, just return a status code (int) 
    static int ExecuteSolo(CommandInfo? ci) {
        CommandInfo item = ci ?? default;
        CommandReturnStruct resp;
        // 0 : success 
        // 1 : fail 
        Logger.Log($"is {item.Command} a builtin: {commandsDict.ContainsKey(item.Command)}", LogLevel.Debug);
        if (commandsDict.ContainsKey(item.Command)) {
            // is a builtin, run through switch-case 
            Logger.Log($"Executing builtin {item.Command}", LogLevel.Debug);
            resp = ExecuteBuiltin(item);
        }
        else if (IsCommandExecutableFromPATH(item.Command)) {
            Logger.Log($"Executing in PATH {item.Command}", LogLevel.Debug);
            resp = ExecuteInPATH(item);
        }
        else {
            Console.WriteLine($"{item.Command}: command not found");
            return 1;
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
        CommandReturnStruct response;
        response.Output = [string.Empty]; 
        // 0 : success 
        // 1 : fail 
        if (commandsDict.ContainsKey(item.Command)) {
            // is a builtin, run through switch-case 
            response = ExecuteBuiltin(item);
        }
        if (IsCommandExecutableFromPATH(item.Command)) {
            response = ExecuteInPATH(item);
        }
        else {
            Console.WriteLine($"{item.Command}: command not found");
            response.ReturnCode = 1;
            response.Error = $"command {item.Command}: not found";
        }
        if (redirect == true || append == true || pipe == true) {
            // not a console write, route output into next. 
            CommandInfo ci = item2 ?? default;
            int idx = -1; 
            // get index of next available spot 
            for (int i = 0; i < ci.Args.Length; i++){
                if (ci.Args[i] == null) { // maybe do IsNullOrEmpty instead? 
                    if (i + 2 < ci.Args.Length) idx = i; 
                    break; 
                }
            }
            if (idx != -1){
                //ci.Args[idx] = [response.Output]; 
                ci.Args[idx] = "placeholder";
                ci.Args[idx + 1] = response.ReturnCode.ToString(); 
                ci.Args[idx + 2] = response.Error ?? string.Empty;
            }
            else {
                Console.WriteLine("idx was -1. Array is maybe full?");
            }

            if(item2 != null){
                ExecuteSolo(item2); // no need for return, it will handle it's own response output 
            }
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
                return Echo(item.Args);
            case "type": 
                return Type(item.Args);
            case "pwd": 
                return PrintWorkingDirectory();
            case "cd":
                return ChangeDirectory(item.Args);
            case "cat":
                return Cat(item.Args);
            case "ls":
                return Ls(item.Args);
            case "exit": 
                Environment.Exit(0);
                break;
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

#region builtins 
    static CommandReturnStruct Echo(string[] args) { 
        if (args.Length < 1) args = [" "]; 
        return new CommandReturnStruct {
            Output = [string.Join(" ", args)], 
            ReturnCode = 0, 
            Error = string.Empty
        };
    }

    static CommandReturnStruct Type(string[] args) {   
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

    static CommandReturnStruct PrintWorkingDirectory() {
        string workingDirectory = System.IO.Directory.GetCurrentDirectory();
        return new CommandReturnStruct {
            Output = [workingDirectory], 
            ReturnCode = 0, 
            Error = string.Empty
        };
    }

    static CommandReturnStruct ChangeDirectory(string[] args) {
        const int BASE_CAPACITY = 10;
        string[] output = new string[BASE_CAPACITY]; 
        int returnCode = -1;
        string error = string.Empty; 
        string path = string.Join(' ', args);
        // Absolute paths, like /usr/local/bin. (starts with / )
        // Relative paths, like ./, ../, ./dir. (starts with . )
        // The ~ character, which represents the user's home directory. (starts with ~ )
        if (path.StartsWith('/') || path.StartsWith('.')) {
            try
            {
                Directory.SetCurrentDirectory(path);
            }
            catch (Exception)
            {
                error = $"cd: {path}: No such file or directory";
                returnCode = 1;
            }
        }
        else if (path.StartsWith('~')) {
            try
            {
                string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                Directory.SetCurrentDirectory(homePath);
            }
            catch (Exception)
            {
                error = $"cd: {path}: No such file or directory";
                returnCode = 1;
            }
        }
        else {
            error = $"cd: {path}: No such file or directory";
            returnCode = 1;
        }
        return new CommandReturnStruct {
            Output = output, 
            ReturnCode = returnCode, 
            Error = error
        };
    }

    static CommandReturnStruct Cat(string[] args) {
        StringBuilder contents = new();
        string filePath = string.Join(" ", args);
        foreach(string line in File.ReadLines(filePath)) {
            contents.Append(line);
        }
        return new CommandReturnStruct {
            Output = [contents.ToString()], 
            ReturnCode = 0, 
            Error = string.Empty
        };
    }

    static CommandReturnStruct Ls(string[] args) {
        string error = string.Empty; 
        int returnCode = -1;
        // if no file path arg then its 'ls' and we just show current directory 
        string path = string.IsNullOrEmpty(args[0]) ? "." : args[0]; 
        List<string> output = [];
        bool exists = File.Exists(path) || Directory.Exists(path);

        if (exists) {
            foreach(var f in Directory.EnumerateFileSystemEntries(path)) {
                output.Add(f); 
            }
        }
        else {
            error = $"ls: {path} does not exist.";
            returnCode = 1; 
        }

        return new CommandReturnStruct {
            Output = output.ToArray() ?? [string.Empty], 
            ReturnCode = returnCode, 
            Error = error
        };
    }

#endregion 
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
    static bool IsCommandExecutableFromPATH(string command) {
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

    [GeneratedRegex(@"(?<=\S)(?=\s)|(?<=\s)(?=\S)")]
    private static partial Regex MyRegex();

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