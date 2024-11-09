using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using DStream.Net;
using DStream.Net.Config;

// Configure Serilog with JSON console output
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: null, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()  // Replace default .NET logger with Serilog
        .ConfigureServices((context, services) =>
        {
            // Load configuration using ConfigLoader
            var configLoader = new ConfigLoader("dstream.yaml");
            var appConfig = configLoader.LoadConfig();

            // Register AppConfig and TableMonitoringService as a hosted service
            services.AddSingleton(appConfig);
            services.AddHostedService<TableMonitoringService>();
        })
        .Build();

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
