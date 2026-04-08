using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Microsoft.AspNetCore.Components.Authorization;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

try
{
    Log.Information("SportsStore application starting up");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    
    builder.Services.AddDbContext<StoreDbContext>(opts => {
        opts.UseSqlite(
            builder.Configuration["ConnectionStrings:SportsStoreConnection"]);
    });
    builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
    builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();
    builder.Services.AddRazorPages();
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession();
    builder.Services.AddScoped<Cart>(sp => SessionCart.GetCart(sp));
    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddCascadingAuthenticationState();
    
    // Add HttpClient to call OrderManagement.Api
    builder.Services.AddHttpClient("OrderApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["OrderApi:BaseUrl"] ?? "http://localhost:5001/");
    });

    builder.Services.AddDbContext<AppIdentityDbContext>(options =>
        options.UseSqlite(
            builder.Configuration["ConnectionStrings:IdentityConnection"]));
    builder.Services.AddIdentity<IdentityUser, IdentityRole>()
        .AddEntityFrameworkStores<AppIdentityDbContext>();
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
    });

    // Register IPaymentService
    builder.Services.AddScoped<SportsStore.Infrastructure.IPaymentService, SportsStore.Infrastructure.StripePaymentService>();

    var app = builder.Build();

    if (app.Environment.IsProduction()) {
        app.UseExceptionHandler("/error");
    }

    app.UseSerilogRequestLogging();

    app.UseRequestLocalization(opts => {
        opts.AddSupportedCultures("en-US")
        .AddSupportedUICultures("en-US")
        .SetDefaultCulture("en-US");
    });

    app.UseStaticFiles();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapRazorPages();
    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    SeedData.EnsurePopulated(app);
    IdentitySeedData.EnsurePopulated(app);

    Log.Information("SportsStore application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SportsStore application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
