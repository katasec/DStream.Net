using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using DStream.Net.Config;

namespace DStream.Net;

public class Server
{
    private readonly string[] _args;

    public Server(string[] args)
    {
        _args = args;

        // Configure Serilog with JSON console output
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: null, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .Enrich.FromLogContext()
            .CreateLogger();
    }

    public async Task StartAsync()
    {
        try
        {
            var host = CreateHostBuilder().Build();

            // Run StartupValidator to check configuration
            using (var scope = host.Services.CreateScope())
            {
                var startupValidator = scope.ServiceProvider.GetRequiredService<StartupValidator>();
                if (!startupValidator.ValidateConfiguration())
                {
                    Log.Fatal("Configuration validation failed. Exiting application.");
                    return;  // Exit application if validation fails
                }
            }

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "The application failed to start.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private IHostBuilder CreateHostBuilder()
    {
        var host= Host.CreateDefaultBuilder(_args)
            .UseSerilog()  // Replace default .NET logger with Serilog
            .ConfigureServices((context, services) =>
            {
                // Load configuration using ConfigLoader
                var configLoader = new ConfigLoader("dstream.yaml");
                var appConfig = configLoader.LoadConfig();

                // Register AppConfig and validator services
                services.AddSingleton(appConfig);
                services.AddTransient<StartupValidator>();
                services.AddHostedService<TableMonitoringService>();
            });

        return host;
    }
}
