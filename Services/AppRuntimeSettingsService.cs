using System.Text.Json;

namespace SolarApp.Services;

public enum InitializationSource
{
    Database,
    Excel
}

public sealed class AppRuntimeSettings
{
    public bool UpdateOnLaunch { get; set; }
    public InitializationSource InitializationSource { get; set; }
}

public class AppRuntimeSettingsService
{
    private const string SettingsSection = "StartupOptions";
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppRuntimeSettingsService> _logger;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppRuntimeSettingsService(IConfiguration configuration, IWebHostEnvironment environment, ILogger<AppRuntimeSettingsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _settingsFilePath = Path.Combine(environment.ContentRootPath, "app-runtime-settings.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public async Task<AppRuntimeSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return GetDefaultSettings();
        }

        try
        {
            await using var stream = File.OpenRead(_settingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<AppRuntimeSettings>(stream, _jsonOptions, cancellationToken);
            return settings ?? GetDefaultSettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lecture des réglages runtime impossible, retour aux valeurs par défaut.");
            return GetDefaultSettings();
        }
    }

    public async Task SaveSettingsAsync(AppRuntimeSettings settings, CancellationToken cancellationToken = default)
    {
        var safeSettings = new AppRuntimeSettings
        {
            UpdateOnLaunch = settings.UpdateOnLaunch,
            InitializationSource = settings.InitializationSource
        };

        try
        {
            await using var stream = File.Create(_settingsFilePath);
            await JsonSerializer.SerializeAsync(stream, safeSettings, _jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec de sauvegarde des réglages runtime.");
            throw;
        }
    }

    private AppRuntimeSettings GetDefaultSettings()
    {
        var updateOnLaunch = _configuration.GetValue<bool>($"{SettingsSection}:UpdateOnLaunch", false);
        var configuredSource = _configuration[$"{SettingsSection}:InitializationSource"];

        if (!Enum.TryParse<InitializationSource>(configuredSource, true, out var initializationSource))
        {
            initializationSource = InitializationSource.Database;
        }

        return new AppRuntimeSettings
        {
            UpdateOnLaunch = updateOnLaunch,
            InitializationSource = initializationSource
        };
    }
}
