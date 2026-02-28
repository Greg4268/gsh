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
            int summedLength = 0; 

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
                        break; 
                    } 
                    string arg = text[cIdx..text.IndexOf(' ')];
                    argSublist.Add(arg);
                    i += arg.Length;
                    cIdx += !string.IsNullOrEmpty(arg) ? arg.Length : 0; 
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
                int argsLen = 0; 
                int opLen = !string.IsNullOrEmpty(op) ? op.Length : 0; 
                int fileLen = !string.IsNullOrEmpty(file) ? file.Length : 0; 
                foreach(string arg in argSublist) argsLen += arg.Length;
                summedLength += cmd.Length + argsLen + opLen + fileLen;
            }
            return executionPlan; 
        }
    }
}