using SolarApp.Components;
using SolarApp.Data;
using SolarApp.Logging;
using SolarApp.Services;
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
builder.Services.AddScoped<AtmoceCloudBrowserService>();
builder.Services.AddScoped<ExcelDailyReportService>();
builder.Services.AddScoped<SolarDataService>();
builder.Services.AddSingleton<AppRuntimeSettingsService>();

// Add Logging
builder.Services.AddLogging();
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(builder.Environment.ContentRootPath, "Logs")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup");

    var dbContext = scope.ServiceProvider.GetRequiredService<AtmocDbContext>();
    dbContext.Database.Migrate();

    try
    {
        var runtimeSettingsService = scope.ServiceProvider.GetRequiredService<AppRuntimeSettingsService>();
        var startupSettings = await runtimeSettingsService.GetSettingsAsync();

        if (!startupSettings.UpdateOnLaunch)
        {
            logger.LogInformation("Mise à jour au lancement désactivée (paramètre utilisateur).");
        }
        else if (startupSettings.InitializationSource == InitializationSource.Excel)
        {
            var solarDataService = scope.ServiceProvider.GetRequiredService<SolarDataService>();
            var importedCount = await solarDataService.ImportDailyRecordsFromExcelAsync();
            logger.LogInformation("Initialisation Excel au démarrage terminée : {Count} nouvelle(s) date(s)", importedCount);
        }
        else
        {
            logger.LogInformation("Initialisation au démarrage depuis la BDD (aucun import Excel exécuté).");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Initialisation au démarrage partiellement ignorée (application continue)");
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

