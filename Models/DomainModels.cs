namespace SolarApp.Models;

/// <summary>
/// Entité Site pour la base de données
/// </summary>
public class Site
{
    public int Id { get; set; }
    public string SiteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BuildState { get; set; }
    public string? CountryGec { get; set; }
    public string? CountryStanag { get; set; }
    public string? Address { get; set; }
    public string? ZipCode { get; set; }
    public string? TimeZone { get; set; }
    public string? ConstructionMethod { get; set; }
    public double SolarCapacity { get; set; }
    public double BatteryCapacity { get; set; }
    public double MICapacity { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerMail { get; set; }
    public string? GridTiedTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relations
    public ICollection<SiteDataSnapshot> DataSnapshots { get; set; } = [];
    public ICollection<Device> Devices { get; set; } = [];
}

/// <summary>
/// Snapshot des données du site à un moment donné
/// </summary>
public class SiteDataSnapshot
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public Site? Site { get; set; }

    // Données solaires
    public int SolarGenerationPower { get; set; }
    public int SolarReactivePower { get; set; }
    public double DailySolarGeneration { get; set; }
    public double MonthlySolarGeneration { get; set; }
    public double YearlySolarGeneration { get; set; }
    public double LifetimeSolarGeneration { get; set; }

    // Données réseau
    public int GridPower { get; set; }
    public double DailyGridExport { get; set; }
    public double DailyGridImport { get; set; }
    public double MonthlyGridExport { get; set; }
    public double MonthlyGridImport { get; set; }
    public double YearlyGridExport { get; set; }
    public double YearlyGridImport { get; set; }
    public double LifetimeGridExport { get; set; }
    public double LifetimeGridImport { get; set; }

    // Données consommation
    public int ConsumptionPower { get; set; }
    public double DailyConsumption { get; set; }
    public double MonthlyConsumption { get; set; }
    public double LifetimeConsumption { get; set; }

    // Données batterie
    public int BatteryPower { get; set; }
    public double BatterySOC { get; set; }
    public string? BatteryStatus { get; set; }
    public string? BatteryMode { get; set; }
    public double DailyBatteryCharging { get; set; }
    public double DailyBatteryDischarge { get; set; }
    public double YearlyBatteryCharging { get; set; }
    public double YearlyBatteryDischarge { get; set; }

    // Environnement
    public double TotalCo2Reduced { get; set; }
    public double TotalTreesPlanted { get; set; }

    public string? Status { get; set; }
    public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entité Device (Microinverteur, Batterie, Gateway)
/// </summary>
public class Device
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public Site? Site { get; set; }

    public string DeviceSerialNumber { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // gateway, micro_inverter, storage
    public string? DeviceMode { get; set; }
    public double Capacity { get; set; }
    public double? MaxChargepower { get; set; }
    public double? MaxDischargepower { get; set; }

    public string? Status { get; set; }
    public DateTime LastReportedTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relations
    public ICollection<DeviceDataSnapshot> DataSnapshots { get; set; } = [];
}

/// <summary>
/// Snapshot des données de l'appareil à un moment donné
/// </summary>
public class DeviceDataSnapshot
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public Device? Device { get; set; }

    // Données génériques
    public int CurrentPower { get; set; }
    public double DailyGeneration { get; set; }
    public double MonthlyGeneration { get; set; }
    public double YearlyGeneration { get; set; }
    public double LifetimeGeneration { get; set; }

    // Données spécifiques batterie
    public int? SOC { get; set; }
    public double? DailyCharging { get; set; }
    public double? DailyDischarge { get; set; }
    public double? MonthlyCharging { get; set; }
    public double? MonthlyDischarge { get; set; }

    // Données spécifiques gateway
    public int? GridVoltage { get; set; }
    public int? GridVoltageA { get; set; }
    public int? GridVoltageB { get; set; }
    public int? GridVoltageC { get; set; }

    public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Enregistrement d'alerte/anomalie
/// </summary>
public class DeviceAlert
{
    public int Id { get; set; }
    public string SiteId { get; set; } = string.Empty;
    public string DeviceSerialNumber { get; set; } = string.Empty;

    public string AlarmId { get; set; } = string.Empty;
    public string? AlarmName { get; set; }
    public string? AlarmLevel { get; set; } // critical, major, minor, warning
    public string? AlarmCause { get; set; }
    public string? AlarmRepairSuggestion { get; set; }

    public DateTime AlarmOccurTime { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public bool Resolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// Relevé énergétique quotidien importé depuis Excel
/// </summary>
public class DailyEnergyRecord
{
    public int Id { get; set; }
    public DateOnly RecordDate { get; set; }
    public double ProducedKwh { get; set; }
    public double ConsumedKwh { get; set; }
    public double FromGridKwh { get; set; }
    public double ToGridKwh { get; set; }
    public double ChargedKwh { get; set; }
    public double DischargedKwh { get; set; }
    public double SocMax { get; set; }
    public double ColumnIValue { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Token d'authentification API Atmoce
/// </summary>
public class ApiToken
{
    public int Id { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

