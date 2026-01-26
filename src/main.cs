using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
class Program
{
    static Dictionary<string, bool> commandsDict= new Dictionary<string, bool> {
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

    static void Main()
    { 
        while (true) {
            Console.Write("$ ");
            string? input = Console.ReadLine();

            if (input == string.Empty) continue; 

            var (command, arguments) = ExtractCommandAndArguments(input);

            if (string.IsNullOrEmpty(command)) continue; 

            if (command == "exit") {
                Environment.Exit(0);
            }
            else if (commandsDict.ContainsKey(command)) {
                switch (command){
                    case "echo":
                        Echo(arguments);
                        break;
                    case "type": 
                        Type(arguments);
                        break;
                    case "pwd": 
                        PrintWorkingDirectory();
                        break;
                    case "cd":
                        ChangeDirectory(arguments);
                        break;
                    case "cat":
                        Cat(arguments);
                        break;
                    case "ls":
                        Ls(arguments);
                        break;
                    default: 
                        Console.Write("something went wrong here");
                        break;
                }
            }
            else {
                // check if command passed is an executable in PATH 
                var (isExecutable, path) = CheckPATH(command);
                if (isExecutable) { 
                    // execute the program at the returned path passing arguments supplied 
                    var process = new Process();                       
                    process.StartInfo.FileName = command;              
                    process.StartInfo.Arguments = arguments;           
                    process.StartInfo.UseShellExecute = false;         
                    process.StartInfo.RedirectStandardOutput = true;   
                    process.Start();                                   
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();                             
                    Console.Write(output);
                } 
                else {
                    Console.WriteLine($"{command}: command not found");
                }
            }
        }
    }

    // helper to get command and preceding string of potential arguments 
    static (string, string) ExtractCommandAndArguments(string? userInput) {
        string command = String.Empty; 
        string arguments = String.Empty;

        if (string.IsNullOrEmpty(userInput)) {
            return (command, arguments);
        }

        userInput = userInput.Trim();
        
        int firstSpaceIndex = userInput.IndexOf(' ');                                                                          
        if (firstSpaceIndex == -1) {                                    
            return (userInput, arguments);
        } else {                                                        
            command = userInput.Substring(0, firstSpaceIndex);            
            arguments = userInput.Substring(firstSpaceIndex + 1);         
        }
        return (command.ToLower(), arguments.Trim());
    }

    // helper for Type builtin to determine if executable has a path in PATH 
    static (bool, string) CheckPATH(string command) {
        string? paths = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(paths)){
            return (false, string.Empty);
        }

        foreach (var path in paths.Split(':')) {
            string fullPath = Path.Combine(path, command);
            if (File.Exists(fullPath)) {
                if(access(fullPath, X_OK) == 0) {
                    return (true, fullPath);
                }
            }
        }
        return (false, String.Empty);
    }

    static void Echo(string content) { 
        //bool isSingleQuotes = false; 
        //bool isDoubleQuotes = false; 

        if (content.StartsWith('"') && content.EndsWith('"')) {

        }
        if (content.StartsWith("'") && content.EndsWith("'")) {
            // find another end quote and keep count
            // print literal without the single quotes included on either end? 
            content = content.Substring(1, content.Length - 2);
            Console.WriteLine(content);
        }
        else {
            // is content ending with redirect to file? 
            if (content.Contains(" > ") || content.Contains(" 1> ")) {
                // get file to write to
                int idxOfRedirect = content.IndexOf('>');
                string contentForFile = content.Substring(0, idxOfRedirect - 1);
                string fileName = content.Substring(idxOfRedirect + 1);

                if (File.Exists(fileName)) {
                    using (StreamWriter writer = new StreamWriter(fileName)) {
                        writer.WriteLine(contentForFile);
                    }
                }
                else {
                    Console.Write($"echo: nonexistent: No such file or directory");
                }
            }
        }
    }

    static void Type(string command) {                                                  
        if (string.IsNullOrEmpty(command)) {                            
            Console.WriteLine($"{command}: not found");                   
            return;                                                       
        }                                                               
                                                                        
        // Check if command is a builtin                                
        if (commandsDict.ContainsKey(command)) {                        
            Console.WriteLine($"{command} is a shell builtin");           
            return;                                                       
        }                                                               
                                                                        
        // Search in PATH                                               
        var (inPath, path) = CheckPATH(command);                        
        if (inPath) {                                                   
            Console.WriteLine($"{command} is {path}");                    
        } else {                                                        
            Console.WriteLine($"{command}: not found");                   
        }                                                                                                                             
    }                                                                   

    static void PrintWorkingDirectory() {
        string workingDirectory = System.IO.Directory.GetCurrentDirectory();
        Console.WriteLine(workingDirectory);
    }

    static void ChangeDirectory(string path) {
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
                Console.WriteLine($"cd: {path}: No such file or directory");
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
                Console.WriteLine($"cd: {path}: No such file or directory");
            }
        }
        else {
            Console.WriteLine($"cd: {path}: No such file or directory");
        }
    }

    static void Cat(string args) {

    }

    static void Ls(string unparsedArgs) {
        // parse args to not miss content or redirect 
        
    }

}

