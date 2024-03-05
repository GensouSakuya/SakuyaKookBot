using GensouSakuya.KookBot.App.Handlers.Base;
using GensouSakuya.KookBot.App.Interfaces;
using GensouSakuya.KookBot.App.Services.KookService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GensouSakuya.KookBot.App;

class Program
{
    static async Task Main(string[] args)
    {

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<IConfigService, ConfigService>();
        builder.Services.AddSingleton<IKookService, KookService>();
        builder.Services.AddSingleton<HandlerEngine>();
        builder.Services.AddSingleton<HandlerCenter>();
        builder.Services.AddLog();

        using IHost host = builder.Build();
        using IServiceScope serviceScope = host.Services.CreateScope();

        IServiceProvider provider = serviceScope.ServiceProvider;

        var handler = provider.GetRequiredService<HandlerCenter>();
        handler.ReloadHandlers();

        var kook = provider.GetRequiredService<IKookService>();
        await kook.LoginAsync();
        await kook.StartAsync();

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

static class ProgramExtensions
{
    public static IServiceCollection AddLog(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(Path.Combine("logs", "log-.txt"), rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                shared: true)
            .CreateLogger();

        return services.AddLogging(builder => builder.AddSerilog(dispose: true));
    }

}
