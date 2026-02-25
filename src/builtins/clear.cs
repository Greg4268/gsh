using src; 
namespace builtins
{
    public class clear
    {
        public string Name => "clear"; 
        public string Description => "Clears the console";
        public CommandReturnStruct Run(string[] args)
        {
            Console.Clear(); 
            return new CommandReturnStruct {
                Output = [string.Empty], 
                Error = string.Empty, 
                ReturnCode = 0
            };
        }
    }
}