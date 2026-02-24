namespace src
{
    public interface IBuiltinCommand
    {
        string Name {get;}
        string Description {get;}
        CommandReturnStruct Run(string[] args);
    }
}