using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using src;
partial class Program
{
    static void Main(string[] args)
    { 
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddTransient<Shell>();
            })
            .Build();

        var shell = host.Services.GetRequiredService<Shell>();
        shell.Run();
    }
}