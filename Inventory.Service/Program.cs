using Serilog;
using Inventory.Service;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("ServiceName", "Inventory.Service")
    .CreateLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((ctx, services) =>
        {
            services.AddHostedService<InventoryWorker>();
        })
        .Build();

    Log.Information("Inventory.Service starting up");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Inventory.Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
