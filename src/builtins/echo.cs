using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src;
#pragma warning disable CS8981,IDE1006

namespace src.builtins
{
    public static class echo
    {
        public static CommandReturnStruct Run(string[] args) {
            if (args.Length < 1) args = [" "]; 
            return new CommandReturnStruct {
                Output = [string.Join(" ", args)], 
                ReturnCode = 0, 
                Error = string.Empty
            };
        }
    }
}