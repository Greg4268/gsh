using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable CS8981, IDE1006

namespace src.builtins
{
    public class pwd
    {
        public static CommandReturnStruct Run() {
            string workingDirectory = Directory.GetCurrentDirectory();
            return new CommandReturnStruct {
                Output = [workingDirectory], 
                ReturnCode = 0, 
                Error = string.Empty
            };
        }
    }
}