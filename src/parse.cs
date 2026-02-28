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
                string cmd = text[..text.IndexOf(' ')]; 
                cIdx += !string.IsNullOrEmpty(cmd) ? cmd.Length : 0;  

                // 2) find the (optional) args
                // somehow check that the next thing is not an operator but an arg? 
                // assume there's always at least one arg to parse so start loop no matter 
                List<string> argSublist = [];
                bool hasOperator = false; 
                for(int i = cIdx; i < text.Length; i++) 
                {
                    if (operators.Contains(text[i].ToString())) // idk if this will work but cool if it does 
                    {
                        // take arg up to operator then break 
                        argSublist.Add(text[cIdx..text[i-1]]); 
                        hasOperator = true; 
                        cIdx += text[..i].Length; 
                        System.Console.WriteLine("hello? ");
                        break; 
                    } 
                    int spaceIndex = text.IndexOf(' ', cIdx);
                    if (spaceIndex == -1)
                    {
                        // No more spaces — take the rest of the string
                        string arg = text[cIdx..];
                        argSublist.Add(arg);
                        break;
                    }
                    else
                    {
                        string arg = text[cIdx..spaceIndex];
                        argSublist.Add(arg);
                        i = spaceIndex;
                        cIdx = spaceIndex + 1;
                    } 
                }

                // 3) get the operator and it's file 
                string op = string.Empty; 
                string file = string.Empty; 
                if(hasOperator) 
                {
                    op = text[cIdx..text.IndexOf(' ')];
                    cIdx += !string.IsNullOrEmpty(op) ? op.Length : 0; 
                    file = text[cIdx..text.IndexOf(' ')]; 
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