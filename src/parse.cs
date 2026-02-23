using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable CS8981, IDE1006

namespace src
{
    public static class parse
    {
        // TODO: improve parser logic. It works well for very basic tasks but benchmarking needs to be done 
        public static Dictionary<int, CommandInfo> Run(string? text) { 
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
            List<string> redirectFileNames = []; 
            int summedLength = 0; 

            // after trimming, there is not a space (somewhere in middle) -> if there is a command it is by itself and does not have args or operator 
            if ( !text.Contains(' ') ) {
                //single work, command only probably 
                return new Dictionary<int, CommandInfo> {
                    [0] = new CommandInfo {
                        Command = text, 
                        Args = [string.Empty], 
                        Operator = string.Empty,
                        RedirectFileName = string.Empty
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
                        i = -1; 
                    }
                    else if (text[i] == '>' || text[i] == '|'){ // just do this for now not considering if it's a valid part of the commands args  (ex. echo "this | text")
                        if(!string.IsNullOrWhiteSpace(text[..i])) {
                            argList.Add(text[..i]);
                        }
                        text = text[i..];
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

                // 3) get the operator and it's file 
                if(hasOperator) {
                    // Logger.Log($"Has Operator? {hasOperator}", LogLevel.Debug);
                    // Logger.Log($"Text: {text}", LogLevel.Debug); 
                    for(int i = 0; i < text.Length; i++) {
                        summedLength++;
                        if(text[i] == ' ') {// passed operator, take from slice 
                            operators.Add(text[..i]);
                            text = text[i..].Trim();
                            // get the file name but nothing more - i know this is nested like crazy but it'll do for now 
                            for(int j = 0; j < text.Length; j++) {
                                if(text[j] == ' ') {
                                    redirectFileNames.Add(text[..i].Trim());
                                    break;
                                }
                                else if(j + 1 == text.Length) {
                                    int k = j + 1; 
                                    redirectFileNames.Add(text[..k]);
                                    text = string.Empty;
                                    break;
                                }
                            }
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
                    Operator = i < operators.Count ? operators[i] : string.Empty,
                    RedirectFileName = i < redirectFileNames.Count ? redirectFileNames[i] : string.Empty
                };

                Logger.Log($"Command: {executionPlan[i].Command}", LogLevel.Debug);
                Logger.Log($"Args:", LogLevel.Debug);
                foreach(string str in executionPlan[i].Args) Logger.Log(str, LogLevel.Debug);
                Logger.Log($"Operator: {executionPlan[i].Operator}", LogLevel.Debug);
                Logger.Log($"Redirect File Name: {executionPlan[i].RedirectFileName}\n\n", LogLevel.Debug);
            }

            return executionPlan; 
        }

    }
}