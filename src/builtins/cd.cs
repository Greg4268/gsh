using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable CS8981,IDE1006

namespace src.builtins
{
    public class cd
    {
        public static CommandReturnStruct Run(string[] args) {
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
    }
}