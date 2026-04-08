using Microsoft.EntityFrameworkCore;
using Serilog;
using OrderManagement.Api.Data;
using OrderManagement.Api.Messaging;
using OrderManagement.Api.Mapping;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("ServiceName", "OrderManagement.Api")
    .CreateLogger();

try
{
    Log.Information("OrderManagement.Api starting up");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Database
    builder.Services.AddDbContext<OrderDbContext>(opts =>
        opts.UseSqlite(builder.Configuration.GetConnectionString("OrderDb") ?? "Data Source=orders.db"));

    // MediatR — CQRS
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssemblyContaining<Program>());

    // AutoMapper
    builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

    // RabbitMQ
    builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
    builder.Services.AddHostedService<OrderEventConsumer>();

    // API + CORS (for React dashboard and Blazor portal)
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    var app = builder.Build();

    // Migrate and seed database on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        db.Database.Migrate();
        Log.Information("Database migrated successfully");
    }

    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("ServiceName", "OrderManagement.Api");
        };
    });

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.UseCors("AllowAll");
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("OrderManagement.Api started on {Urls}", builder.Configuration["ASPNETCORE_URLS"] ?? "http://+:5001");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderManagement.Api failed to start");
}
finally
{
    Log.CloseAndFlush();
}
