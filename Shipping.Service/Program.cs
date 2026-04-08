using Serilog;
using Shipping.Service;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("ServiceName", "Shipping.Service")
    .CreateLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((ctx, services) =>
        {
            services.AddHostedService<ShippingWorker>();
        })
        .Build();

    Log.Information("Shipping.Service starting up");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Shipping.Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
