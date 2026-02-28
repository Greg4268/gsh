#pragma warning disable IDE1006

namespace src
{
    public static class parse
    {
        public static readonly string[] operators = [">", "1>", "|", ">>"]; 
        /* 
            TODO: improve parser logic

            Current logic relies on finding the next white space and making assumptions about what the 
            user put in that place. This works because anything that is parsed into the execution plan that is 
            not valid will get handled downstream. 
        */
        public static Dictionary<int, CommandInfo> Run(string text) { 
            var executionPlan = new Dictionary<int, CommandInfo>(); 

            /* 
                at this point, we've trimmed the text if there's 
                a space, the command is not by itself if there 
                isn't space we can assume it's just a command
                so exit early 
            */
            if ( !text.Contains(' ') ) {
                return new Dictionary<int, CommandInfo> {
                    [0] = new CommandInfo {
                        Command = text, 
                        Args = [string.Empty], 
                        Operator = string.Empty,
                        RedirectFileName = string.Empty
                    }
                };
            }
            
            /* 
                loop through text and extract commands, args, and operators 
                each loop = one dicionary entry 
            */
            int cIdx = 0; // current index in text string 
            int dIdx = 0; // current index in execution plan dictionary 
            int summedLength = 0; 
            while (summedLength < text.Length) 
            {
                // 1) find the command 
                string cmd = text.Contains(' ') ? text[..text.IndexOf(' ', cIdx)] : text[..text.Length]; // if no space we take up to end (out of bounds error here?)
                // cIdx += !string.IsNullOrEmpty(cmd) ? cmd.Length : 0;  
                cIdx += cmd.Length; // should never be null here... 

                // 2) find the (optional) args
                // somehow check that the next thing is not an operator but an arg? 
                // assume there's always at least one arg to parse so start loop no matter 
                List<string> argSublist = [];
                bool hasOperator = false; 
                for(int i = cIdx; i < text.Length; i++) 
                {
                    if (operators.Contains(text[i].ToString())) 
                    {
                        // take arg up to operator then break 
                        argSublist.Add(text[cIdx..text[i-1]]); 
                        hasOperator = true; 
                        cIdx += text[cIdx..text[i-1]].Length; // may need to adjust 
                        break; 
                    } 
                    int spaceIdx = text.IndexOf(' ', cIdx);
                    if (spaceIdx == -1)
                    {
                        // No more spaces — take the rest of the string
                        string arg = text[cIdx..];
                        argSublist.Add(arg);
                        break;
                    }
                    else
                    {
                        string arg = text[cIdx..spaceIdx];
                        argSublist.Add(arg);
                        i = spaceIdx;
                        cIdx = spaceIdx + 1;
                    } 
                }

                // 3) get the operator and it's file 
                string op = string.Empty; 
                string file = string.Empty; 
                if(hasOperator) 
                {
                    int opIdx = text.IndexOf(' ', cIdx);
                    if (opIdx == -1) 
                    {
                        op = text[cIdx..];
                        break; // don't know if this'll behave as I intend. 
                    }
                    else
                    {
                        op = text[cIdx..opIdx]; 
                        cIdx = opIdx + 1; 
                    }
                    int fileIdx = text.IndexOf(' ', cIdx);
                    if (fileIdx == -1) 
                    {
                        file = text[cIdx..];
                    }   
                    else 
                    {
                        file = text[cIdx..fileIdx];
                        cIdx = fileIdx + 1; 
                    }
                }
                
                // 4) build dictionary 
                executionPlan[dIdx] = new CommandInfo {
                    Command = cmd.ToLower() ?? string.Empty, 
                    Args = argSublist.ToArray() ?? [string.Empty], 
                    Operator = op ?? string.Empty, 
                    RedirectFileName = file ?? string.Empty 
                }; 
                Logger.Log($"Command: {executionPlan[dIdx].Command}", LogLevel.Debug);
                Logger.Log($"Args:", LogLevel.Debug);
                foreach(string str in executionPlan[dIdx].Args) Logger.Log(str, LogLevel.Debug);
                Logger.Log($"Operator: {executionPlan[dIdx].Operator}", LogLevel.Debug);
                Logger.Log($"Redirect File Name: {executionPlan[dIdx].RedirectFileName}\n\n", LogLevel.Debug);

                dIdx++; 
                summedLength +=
                    (cmd?.Length ?? 0) +
                    (op?.Length ?? 0) +
                    (file?.Length ?? 0) +
                    argSublist.Sum(arg => arg?.Length ?? 0);
            }
            return executionPlan; 
        }
    }
}