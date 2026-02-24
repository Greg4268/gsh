#pragma warning disable CS8981,IDE1006

namespace src.builtins
{
    public class echo : IBuiltinCommand
    {
        public string Name => "Echo";
        public string Description => "Prints string contents provided as args";
        public CommandReturnStruct Run(string[] args) {
            if (args.Length < 1) args = [" "]; 
            return new CommandReturnStruct {
                Output = [string.Join(" ", args)], 
                ReturnCode = 0, 
                Error = string.Empty
            };
        }
    }
}