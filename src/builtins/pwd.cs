#pragma warning disable CS8981, IDE1006
namespace src.builtins
{
    public class pwd : IBuiltinCommand
    {
        public string Name => "pwd"; 
        public string Description => "Prints the working directory";
        public CommandReturnStruct Run(string[] args) {
            string workingDirectory = Directory.GetCurrentDirectory();
            return new CommandReturnStruct {
                Output = [workingDirectory], 
                ReturnCode = 0, 
                Error = string.Empty
            };
        }
    }
}