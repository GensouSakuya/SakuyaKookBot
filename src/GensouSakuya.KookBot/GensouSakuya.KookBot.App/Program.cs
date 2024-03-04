using GensouSakuya.KookBot.App.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GensouSakuya.KookBot.App;

class Program
{
    static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<IConfigService, ConfigService>();
        builder.Services.AddSingleton<KookClient>();

        using IHost host = builder.Build();
        using IServiceScope serviceScope = host.Services.CreateScope();

        IServiceProvider provider = serviceScope.ServiceProvider;

        var kook = provider.GetRequiredService<KookClient>();
        await kook.LoginAsync();
        await kook.StartAsync();
        await kook.TestAsync();

        var exitLoop = false;
        Console.CancelKeyPress += delegate
        {
            exitLoop = true;
        };

        _ = Task.Run(() =>
        {
            var command = Console.ReadLine();
            while (!exitLoop)
            {
                //do something

                command = Console.ReadLine();
            }
        });
        await host.RunAsync();
    }
}
