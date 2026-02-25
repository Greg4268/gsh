using System.Reflection; 
namespace src
{
    public static class BuiltinRegistry
    {
        public static Dictionary<string, IBuiltinCommand> LoadBuiltins() {
            return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IBuiltinCommand).IsAssignableFrom(t)
                        && !t.IsInterface
                        && !t.IsAbstract)
            .Select(t => (IBuiltinCommand)Activator.CreateInstance(t)!)
            .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        }
    }
}