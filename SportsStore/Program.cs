using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using Microsoft.AspNetCore.Identity;
using Serilog;

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

    builder.Services.AddControllersWithViews();
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
    builder.Services.AddDbContext<AppIdentityDbContext>(options =>
        options.UseSqlite(
            builder.Configuration["ConnectionStrings:IdentityConnection"]));
    builder.Services.AddIdentity<IdentityUser, IdentityRole>()
        .AddEntityFrameworkStores<AppIdentityDbContext>();

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

    app.MapControllerRoute("catpage",
        "{category}/Page{productPage:int}",
        new { Controller = "Home", action = "Index" });
    app.MapControllerRoute("page", "Page{productPage:int}",
        new { Controller = "Home", action = "Index", productPage = 1 });
    app.MapControllerRoute("category", "{category}",
        new { Controller = "Home", action = "Index", productPage = 1 });
    app.MapControllerRoute("pagination",
        "Products/Page{productPage}",
        new { Controller = "Home", action = "Index", productPage = 1 });
    app.MapDefaultControllerRoute();
    app.MapRazorPages();
    app.MapBlazorHub();
    app.MapFallbackToPage("/admin/{*catchall}", "/Admin/Index");

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