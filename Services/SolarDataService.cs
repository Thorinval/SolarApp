using SolarApp.Data;
using SolarApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SolarApp.Services;

/// <summary>
/// Service métier pour gérer la logique de synchronisation et récupération des données solaires
/// </summary>
public class SolarDataService
{
    private readonly AtmocApiService _apiService;
    private readonly AtmocDbContext _dbContext;
    private readonly ExcelDailyReportService _excelDailyReportService;
    private readonly ILogger<SolarDataService> _logger;

    public SolarDataService(
        AtmocApiService apiService,
        AtmocDbContext dbContext,
        ExcelDailyReportService excelDailyReportService,
        ILogger<SolarDataService> logger)
    {
        _apiService = apiService;
        _dbContext = dbContext;
        _excelDailyReportService = excelDailyReportService;
        _logger = logger;
    }

    /// <summary>
    /// Synchroniser tous les sites depuis l'API Atmoce
    /// </summary>
    public async Task<List<Site>> SyncSitesAsync()
    {
        try
        {
            _logger.LogInformation("Début de la synchronisation des sites...");

            var sites = new List<Site>();
            int page = 1;
            bool hasMore = true;

            while (hasMore)
            {
                var siteDtos = await _apiService.GetSitesAsync(page);
                if (siteDtos == null || siteDtos.Count == 0)
                    break;

                foreach (var siteDto in siteDtos)
                {
                    if (siteDto.SiteId == null)
                        continue;

                    var existingSite = await _dbContext.Sites
                        .FirstOrDefaultAsync(s => s.SiteId == siteDto.SiteId);

                    if (existingSite != null)
                    {
                        // Mettre à jour le site existant
                        existingSite.Name = siteDto.Name ?? existingSite.Name;
                        existingSite.BuildState = siteDto.BuildState;
                        existingSite.CountryGec = siteDto.CountryGec;
                        existingSite.CountryStanag = siteDto.CountryStanag;
                        existingSite.Address = siteDto.Address;
                        existingSite.ZipCode = siteDto.ZipCode;
                        existingSite.TimeZone = siteDto.TimeZone;
                        existingSite.ConstructionMethod = siteDto.ConstructionMethod;
                        existingSite.SolarCapacity = siteDto.SolarCapacity;
                        existingSite.BatteryCapacity = siteDto.BatteryCapacity;
                        existingSite.MICapacity = siteDto.MICapacity;
                        existingSite.OwnerName = siteDto.OwnerName;
                        existingSite.OwnerMail = siteDto.OwnerMail;
                        existingSite.GridTiedTime = siteDto.GridTiedTime;
                        existingSite.UpdatedAt = DateTime.UtcNow;

                        _dbContext.Sites.Update(existingSite);
                        sites.Add(existingSite);
                    }
                    else
                    {
                        // Créer un nouveau site
                        var newSite = new Site
                        {
                            SiteId = siteDto.SiteId,
                            Name = siteDto.Name ?? "Unknown",
                            BuildState = siteDto.BuildState,
                            CountryGec = siteDto.CountryGec,
                            CountryStanag = siteDto.CountryStanag,
                            Address = siteDto.Address,
                            ZipCode = siteDto.ZipCode,
                            TimeZone = siteDto.TimeZone,
                            ConstructionMethod = siteDto.ConstructionMethod,
                            SolarCapacity = siteDto.SolarCapacity,
                            BatteryCapacity = siteDto.BatteryCapacity,
                            MICapacity = siteDto.MICapacity,
                            OwnerName = siteDto.OwnerName,
                            OwnerMail = siteDto.OwnerMail,
                            GridTiedTime = siteDto.GridTiedTime,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _dbContext.Sites.Add(newSite);
                        sites.Add(newSite);
                    }
                }

                page++;
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Synchronisation complète : {sites.Count} sites");

            return sites;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la synchronisation des sites : {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Synchroniser les données actuelles d'un site et créer un snapshot
    /// </summary>
    public async Task<SiteDataSnapshot?> SyncSiteDataAsync(string siteId)
    {
        try
        {
            _logger.LogInformation($"Synchronisation des données du site {siteId}...");

            var siteLastPower = await _apiService.GetSiteLastPowerAsync(siteId);
            if (siteLastPower == null)
            {
                _logger.LogWarning($"Impossible de récupérer les données pour le site {siteId}");
                return null;
            }

            var site = await _dbContext.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId);
            if (site == null)
            {
                _logger.LogWarning($"Site {siteId} non trouvé en base de données");
                return null;
            }

            var snapshot = new SiteDataSnapshot
            {
                SiteId = site.Id,
                SolarGenerationPower = siteLastPower.SolarGenerationPower,
                SolarReactivePower = siteLastPower.SolarReactivePower,
                DailySolarGeneration = siteLastPower.DailySolarGeneration,
                MonthlySolarGeneration = siteLastPower.MonthlySolarGeneration,
                YearlySolarGeneration = siteLastPower.YearlySolarGeneration,
                LifetimeSolarGeneration = siteLastPower.LifetimeSolarGeneration,
                GridPower = siteLastPower.GridPower,
                DailyGridExport = siteLastPower.DailyGridExport,
                DailyGridImport = siteLastPower.DailyGridImport,
                MonthlyGridExport = siteLastPower.MonthlyGridExport,
                MonthlyGridImport = siteLastPower.MonthlyGridImport,
                YearlyGridExport = siteLastPower.YearlyGridExport,
                YearlyGridImport = siteLastPower.LifetimeGridImport,
                ConsumptionPower = siteLastPower.ConsumptionPower,
                DailyConsumption = siteLastPower.DailyConsumption,
                MonthlyConsumption = siteLastPower.MonthlyConsumption,
                LifetimeConsumption = siteLastPower.LifetimeConsumption,
                BatteryPower = siteLastPower.BatteryPower,
                BatterySOC = siteLastPower.BatterySOC,
                BatteryStatus = siteLastPower.BatteryStatus,
                BatteryMode = siteLastPower.BatteryMode,
                DailyBatteryCharging = siteLastPower.DailyBatteryCharging,
                DailyBatteryDischarge = siteLastPower.DailyBatteryDischarge,
                YearlyBatteryCharging = siteLastPower.YearlyBatteryCharging,
                YearlyBatteryDischarge = siteLastPower.YearlyBatteryDischarge,
                TotalCo2Reduced = siteLastPower.TotalCo2Reduced,
                TotalTreesPlanted = siteLastPower.TotalTreesPlanted,
                Status = siteLastPower.Status,
                SnapshotTime = DateTime.UtcNow
            };

            _dbContext.SiteDataSnapshots.Add(snapshot);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Snapshot du site {siteId} créé avec succès");
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la synchronisation des données du site {siteId} : {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Synchroniser les appareils d'un site
    /// </summary>
    public async Task<List<Device>> SyncDevicesAsync(string siteId)
    {
        try
        {
            _logger.LogInformation($"Synchronisation des appareils du site {siteId}...");

            var site = await _dbContext.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId);
            if (site == null)
            {
                _logger.LogWarning($"Site {siteId} non trouvé");
                return new List<Device>();
            }

            var deviceDtos = await _apiService.GetDevicesBySiteAsync(siteId);
            if (deviceDtos == null || deviceDtos.Count == 0)
            {
                _logger.LogInformation($"Aucun appareil pour le site {siteId}");
                return new List<Device>();
            }

            var devices = new List<Device>();

            foreach (var deviceDto in deviceDtos)
            {
                if (string.IsNullOrEmpty(deviceDto.DeviceSN))
                    continue;

                var existingDevice = await _dbContext.Devices
                    .FirstOrDefaultAsync(d => d.DeviceSerialNumber == deviceDto.DeviceSN);

                if (existingDevice != null)
                {
                    existingDevice.DeviceType = deviceDto.DeviceType ?? existingDevice.DeviceType;
                    existingDevice.DeviceMode = deviceDto.DeviceMode;
                    existingDevice.Capacity = deviceDto.MICapacity > 0 ? deviceDto.MICapacity : deviceDto.BatteryCapacity;
                    existingDevice.MaxChargepower = deviceDto.BatteryChargeMaxPower > 0 ? deviceDto.BatteryChargeMaxPower : null;
                    existingDevice.MaxDischargepower = deviceDto.BatteryDisChargeMaxPower > 0 ? deviceDto.BatteryDisChargeMaxPower : null;
                    existingDevice.UpdatedAt = DateTime.UtcNow;

                    _dbContext.Devices.Update(existingDevice);
                    devices.Add(existingDevice);
                }
                else
                {
                    var newDevice = new Device
                    {
                        SiteId = site.Id,
                        DeviceSerialNumber = deviceDto.DeviceSN,
                        DeviceType = deviceDto.DeviceType ?? "unknown",
                        DeviceMode = deviceDto.DeviceMode,
                        Capacity = deviceDto.MICapacity > 0 ? deviceDto.MICapacity : deviceDto.BatteryCapacity,
                        MaxChargepower = deviceDto.BatteryChargeMaxPower > 0 ? deviceDto.BatteryChargeMaxPower : null,
                        MaxDischargepower = deviceDto.BatteryDisChargeMaxPower > 0 ? deviceDto.BatteryDisChargeMaxPower : null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _dbContext.Devices.Add(newDevice);
                    devices.Add(newDevice);
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Synchronisation de {devices.Count} appareils complète pour le site {siteId}");

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la synchronisation des appareils du site {siteId} : {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Importer les relevés quotidiens depuis le fichier Excel
    /// </summary>
    public async Task<int> ImportDailyRecordsFromExcelAsync()
    {
        try
        {
            return await _excelDailyReportService.ImportMissingDailyRecordsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'import des relevés Excel");
            throw;
        }
    }

    /// <summary>
    /// Récupérer tous les sites
    /// </summary>
    public async Task<List<Site>> GetSitesAsync()
    {
        return await _dbContext.Sites.ToListAsync();
    }

    /// <summary>
    /// Récupérer un site par ID
    /// </summary>
    public async Task<Site?> GetSiteAsync(int id)
    {
        return await _dbContext.Sites
            .Include(s => s.DataSnapshots)
            .Include(s => s.Devices)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <summary>
    /// Récupérer les snapshots récents d'un site
    /// </summary>
    public async Task<List<SiteDataSnapshot>> GetSiteDataSnapshotsAsync(int siteId, int daysBack = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

        return await _dbContext.SiteDataSnapshots
            .Where(s => s.SiteId == siteId && s.SnapshotTime >= cutoffDate)
            .OrderByDescending(s => s.SnapshotTime)
            .ToListAsync();
    }

    /// <summary>
    /// Récupérer les alertes actives
    /// </summary>
    public async Task<List<DeviceAlert>> GetActiveAlertsAsync(string siteId)
    {
        return await _dbContext.DeviceAlerts
            .Where(a => a.SiteId == siteId && !a.Resolved)
            .OrderByDescending(a => a.AlarmOccurTime)
            .ToListAsync();
    }

    /// <summary>
    /// Synchroniser les alertes depuis l'API
    /// </summary>
    public async Task SyncAlertsAsync(string siteId)
    {
        try
        {
            _logger.LogInformation($"Synchronisation des alertes du site {siteId}...");

            var alertDtos = await _apiService.GetAlarmsAsync(siteId);
            if (alertDtos == null)
            {
                _logger.LogInformation($"Aucune alerte pour le site {siteId}");
                return;
            }

            foreach (var alertDto in alertDtos)
            {
                if (string.IsNullOrEmpty(alertDto.AlarmId))
                    continue;

                var existingAlert = await _dbContext.DeviceAlerts
                    .FirstOrDefaultAsync(a => a.AlarmId == alertDto.AlarmId);

                if (existingAlert == null)
                {
                    var newAlert = new DeviceAlert
                    {
                        SiteId = siteId,
                        DeviceSerialNumber = alertDto.DeviceSN ?? string.Empty,
                        AlarmId = alertDto.AlarmId,
                        AlarmName = alertDto.AlarmName,
                        AlarmLevel = alertDto.AlarmLevel,
                        AlarmCause = alertDto.AlarmCause,
                        AlarmRepairSuggestion = alertDto.AlarmRepairSuggestion,
                        AlarmOccurTime = string.IsNullOrEmpty(alertDto.AlarmOccurTime) 
                            ? DateTime.UtcNow 
                            : DateTime.Parse(alertDto.AlarmOccurTime),
                        RecordedAt = DateTime.UtcNow,
                        Resolved = false
                    };

                    _dbContext.DeviceAlerts.Add(newAlert);
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Synchronisation des alertes complète pour le site {siteId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la synchronisation des alertes du site {siteId} : {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Récupérer les derniers appareils d'un site
    /// </summary>
    public async Task<List<Device>> GetDevicesAsync(int siteId)
    {
        return await _dbContext.Devices
            .Where(d => d.SiteId == siteId)
            .ToListAsync();
    }

    /// <summary>
    /// Supprimer tous les enregistrements de la base de données
    /// </summary>
    public async Task<int> ClearDatabaseRecordsAsync()
    {
        try
        {
            _logger.LogInformation("Début du vidage de la base de données...");

            var deletedCount = 0;
            deletedCount += await _dbContext.DeviceDataSnapshots.ExecuteDeleteAsync();
            deletedCount += await _dbContext.SiteDataSnapshots.ExecuteDeleteAsync();
            deletedCount += await _dbContext.Devices.ExecuteDeleteAsync();
            deletedCount += await _dbContext.DeviceAlerts.ExecuteDeleteAsync();
            deletedCount += await _dbContext.DailyEnergyRecords.ExecuteDeleteAsync();
            deletedCount += await _dbContext.ApiTokens.ExecuteDeleteAsync();
            deletedCount += await _dbContext.Sites.ExecuteDeleteAsync();

            _logger.LogInformation("Vidage de la base de données terminé : {DeletedCount} enregistrement(s) supprimé(s)", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du vidage de la base de données");
            throw;
        }
    }

    /// <summary>
    /// Marquer une alerte comme résolue
    /// </summary>
    public async Task MarkAlertAsResolvedAsync(int alertId)
    {
        var alert = await _dbContext.DeviceAlerts.FindAsync(alertId);
        if (alert != null)
        {
            alert.Resolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            _dbContext.DeviceAlerts.Update(alert);
            await _dbContext.SaveChangesAsync();
        }
    }
}

