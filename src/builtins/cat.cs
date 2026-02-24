using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
#pragma warning disable CS8981, IDE1006

namespace src.builtins
{
    public class cat : IBuiltinCommand
    {
        public string Name => "Cat"; 
        public string Description => "Prints the contents of a given file";  
        public CommandReturnStruct Run(string[] args) {
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
    }
}