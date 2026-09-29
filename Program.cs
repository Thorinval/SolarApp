using AtmoceSolarApp.Components;
using AtmoceSolarApp.Data;
using AtmoceSolarApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add DbContext
builder.Services.AddDbContext<AtmocDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Add HTTP Client
builder.Services.AddHttpClient();

// Add custom services
builder.Services.AddScoped<AtmocApiService>();
builder.Services.AddScoped<ExcelDailyReportService>();
builder.Services.AddScoped<SolarDataService>();

// Add Logging
builder.Services.AddLogging();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup");

    var dbContext = scope.ServiceProvider.GetRequiredService<AtmocDbContext>();
    dbContext.Database.Migrate();

    try
    {
        var solarDataService = scope.ServiceProvider.GetRequiredService<SolarDataService>();
        var importedCount = await solarDataService.ImportDailyRecordsFromExcelAsync();
        logger.LogInformation("Import Excel au démarrage terminé : {Count} nouvelle(s) date(s)", importedCount);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Import Excel au démarrage ignoré (application continue)");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
