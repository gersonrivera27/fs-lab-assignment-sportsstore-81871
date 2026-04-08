using Serilog;
using Payment.Service;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("ServiceName", "Payment.Service")
    .CreateLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((ctx, services) =>
        {
            services.AddHostedService<PaymentWorker>();
        })
        .Build();

    Log.Information("Payment.Service starting up");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Payment.Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
