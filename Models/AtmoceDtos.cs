using System.Text.Json.Serialization;

namespace AtmoceSolarApp.Models;

/// <summary>
/// Response wrapper pour toutes les réponses API Atmoce
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("pageEnd")]
    public int? PageEnd { get; set; }
}

/// <summary>
/// Response pour l'authentification
/// </summary>
public class AuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}

/// <summary>
/// Informations sur un site
/// </summary>
public class SiteDto
{
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("buildState")]
    public string? BuildState { get; set; }

    [JsonPropertyName("buildStateName")]
    public string? BuildStateName { get; set; }

    [JsonPropertyName("countryGec")]
    public string? CountryGec { get; set; }

    [JsonPropertyName("countryStanag")]
    public string? CountryStanag { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("zipCode")]
    public string? ZipCode { get; set; }

    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("constructionMethod")]
    public string? ConstructionMethod { get; set; }

    [JsonPropertyName("solarCapacity")]
    public double SolarCapacity { get; set; }

    [JsonPropertyName("batteryCapacity")]
    public double BatteryCapacity { get; set; }

    [JsonPropertyName("MICapacity")]
    public double MICapacity { get; set; }

    [JsonPropertyName("ownerName")]
    public string? OwnerName { get; set; }

    [JsonPropertyName("ownerMail")]
    public string? OwnerMail { get; set; }

    [JsonPropertyName("gridTiedTime")]
    public string? GridTiedTime { get; set; }
}

/// <summary>
/// Données actuelles d'un site
/// </summary>
public class SiteLastPowerDto
{
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("lastReportedTime")]
    public long LastReportedTime { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("solarGenerationPower")]
    public int SolarGenerationPower { get; set; }

    [JsonPropertyName("solarReactivePower")]
    public int SolarReactivePower { get; set; }

    [JsonPropertyName("dailySoalrGeneration")]
    public double DailySolarGeneration { get; set; }

    [JsonPropertyName("monthlySoalrGeneration")]
    public double MonthlySolarGeneration { get; set; }

    [JsonPropertyName("yearlySoalrGeneration")]
    public double YearlySolarGeneration { get; set; }

    [JsonPropertyName("lifetimeSoalrGeneration")]
    public double LifetimeSolarGeneration { get; set; }

    [JsonPropertyName("gridPower")]
    public int GridPower { get; set; }

    [JsonPropertyName("dailyGridExport")]
    public double DailyGridExport { get; set; }

    [JsonPropertyName("dailyGridImport")]
    public double DailyGridImport { get; set; }

    [JsonPropertyName("monthlyGridExport")]
    public double MonthlyGridExport { get; set; }

    [JsonPropertyName("monthlyGridImport")]
    public double MonthlyGridImport { get; set; }

    [JsonPropertyName("yearlyConsumption")]
    public double YearlyConsumption { get; set; }

    [JsonPropertyName("yearlyGridExport")]
    public double YearlyGridExport { get; set; }

    [JsonPropertyName("lifetimeGridExport")]
    public double LifetimeGridExport { get; set; }

    [JsonPropertyName("lifetimeGridImport")]
    public double LifetimeGridImport { get; set; }

    [JsonPropertyName("consumptionPower")]
    public int ConsumptionPower { get; set; }

    [JsonPropertyName("dailyConsumption")]
    public double DailyConsumption { get; set; }

    [JsonPropertyName("monthlyConsumption")]
    public double MonthlyConsumption { get; set; }

    [JsonPropertyName("yearlyGridImport")]
    public double YearlyGridImportValue { get; set; }

    [JsonPropertyName("lifetimeConsumption")]
    public double LifetimeConsumption { get; set; }

    [JsonPropertyName("batteryPower")]
    public int BatteryPower { get; set; }

    [JsonPropertyName("batterySOC")]
    public double BatterySOC { get; set; }

    [JsonPropertyName("batteryStatus")]
    public string? BatteryStatus { get; set; }

    [JsonPropertyName("batteryMode")]
    public string? BatteryMode { get; set; }

    [JsonPropertyName("dailyBatteryCharging")]
    public double DailyBatteryCharging { get; set; }

    [JsonPropertyName("dailyBatteryDischarge")]
    public double DailyBatteryDischarge { get; set; }

    [JsonPropertyName("monthly_batteryCharging")]
    public double MonthlyBatteryCharging { get; set; }

    [JsonPropertyName("monthlyBatteryCharging")]
    public double MonthlyBatteryChargingValue { get; set; }

    [JsonPropertyName("yearlyBatteryCharging")]
    public double YearlyBatteryCharging { get; set; }

    [JsonPropertyName("yearlyBatteryDischarge")]
    public double YearlyBatteryDischarge { get; set; }

    [JsonPropertyName("lifetimeBatteryCharging")]
    public double LifetimeBatteryCharging { get; set; }

    [JsonPropertyName("lifetimeBatteryDischarge")]
    public double LifetimeBatteryDischarge { get; set; }

    [JsonPropertyName("totalCo2Reduced")]
    public double TotalCo2Reduced { get; set; }

    [JsonPropertyName("totalTreesPlanted")]
    public double TotalTreesPlanted { get; set; }
}

/// <summary>
/// Informations sur un microinverteur
/// </summary>
public class MicroinverterLastDataDto
{
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("SN")]
    public string? SerialNumber { get; set; }

    [JsonPropertyName("lastReportedTime")]
    public long LastReportedTime { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("generationPower")]
    public int GenerationPower { get; set; }

    [JsonPropertyName("dailyGeneration")]
    public double DailyGeneration { get; set; }

    [JsonPropertyName("monthlyGeneration")]
    public double MonthlyGeneration { get; set; }

    [JsonPropertyName("yearlyGeneration")]
    public double YearlyGeneration { get; set; }

    [JsonPropertyName("lifetimeGeneration")]
    public double LifetimeGeneration { get; set; }

    [JsonPropertyName("pvData")]
    public List<PvBranchDataDto>? PvData { get; set; }
}

/// <summary>
/// Données de branche PV (panneau solaire)
/// </summary>
public class PvBranchDataDto
{
    [JsonPropertyName("pvNumber")]
    public int PvNumber { get; set; }

    [JsonPropertyName("pvPower")]
    public int PvPower { get; set; }

    [JsonPropertyName("pvDailyGeneration")]
    public double PvDailyGeneration { get; set; }

    [JsonPropertyName("pvMonthlyGeneration")]
    public double PvMonthlyGeneration { get; set; }

    [JsonPropertyName("pvYearlyGeneration")]
    public double PvYearlyGeneration { get; set; }

    [JsonPropertyName("pvLifetimeGeneration")]
    public double PvLifetimeGeneration { get; set; }
}

/// <summary>
/// Informations sur une batterie
/// </summary>
public class BatteryLastDataDto
{
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("SN")]
    public string? SerialNumber { get; set; }

    [JsonPropertyName("lastReportedTime")]
    public long LastReportedTime { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("power")]
    public int Power { get; set; }

    [JsonPropertyName("batteryMode")]
    public string? BatteryMode { get; set; }

    [JsonPropertyName("SOC")]
    public int SOC { get; set; }

    [JsonPropertyName("dailyCharging")]
    public double DailyCharging { get; set; }

    [JsonPropertyName("dailyDischarge")]
    public double DailyDischarge { get; set; }

    [JsonPropertyName("monthlyCharging")]
    public double MonthlyCharging { get; set; }

    [JsonPropertyName("monthlyDischarge")]
    public double MonthlyDischarge { get; set; }

    [JsonPropertyName("yearlyCharging")]
    public double YearlyCharging { get; set; }

    [JsonPropertyName("yearlyDischarge")]
    public double YearlyDischarge { get; set; }

    [JsonPropertyName("lifetimeCharging")]
    public double LifetimeCharging { get; set; }

    [JsonPropertyName("lifetimeDischarge")]
    public double LifetimeDischarge { get; set; }
}

/// <summary>
/// Informations sur une passerelle (gateway)
/// </summary>
public class GatewayLastDataDto
{
    [JsonPropertyName("SN")]
    public string? SerialNumber { get; set; }

    [JsonPropertyName("lastReportedTime")]
    public long LastReportedTime { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("solarGenerationPower")]
    public int SolarGenerationPower { get; set; }

    [JsonPropertyName("solarGenerationPowerA")]
    public int SolarGenerationPowerA { get; set; }

    [JsonPropertyName("solarGenerationPowerB")]
    public int SolarGenerationPowerB { get; set; }

    [JsonPropertyName("solarGenerationPowerC")]
    public int SolarGenerationPowerC { get; set; }

    [JsonPropertyName("solarReactivePower")]
    public int SolarReactivePower { get; set; }

    [JsonPropertyName("solarReactivePowerA")]
    public int SolarReactivePowerA { get; set; }

    [JsonPropertyName("solarReactivePowerB")]
    public int SolarReactivePowerB { get; set; }

    [JsonPropertyName("solarReactivePowerC")]
    public int SolarReactivePowerC { get; set; }

    [JsonPropertyName("dailySoalrGeneration")]
    public double DailySolarGeneration { get; set; }

    [JsonPropertyName("monthlySoalrGeneration")]
    public double MonthlySolarGeneration { get; set; }

    [JsonPropertyName("yearlySoalrGeneration")]
    public double YearlySolarGeneration { get; set; }

    [JsonPropertyName("lifetimeSoalrGeneration")]
    public double LifetimeSolarGeneration { get; set; }

    [JsonPropertyName("gridPower")]
    public int GridPower { get; set; }

    [JsonPropertyName("gridPowerA")]
    public int GridPowerA { get; set; }

    [JsonPropertyName("gridPowerB")]
    public int GridPowerB { get; set; }

    [JsonPropertyName("gridPowerC")]
    public int GridPowerC { get; set; }

    [JsonPropertyName("gridVoltage")]
    public int GridVoltage { get; set; }

    [JsonPropertyName("gridVoltageA")]
    public int GridVoltageA { get; set; }

    [JsonPropertyName("gridVoltageB")]
    public int GridVoltageB { get; set; }

    [JsonPropertyName("gridVoltageC")]
    public int GridVoltageC { get; set; }

    [JsonPropertyName("dailyGridExport")]
    public double DailyGridExport { get; set; }

    [JsonPropertyName("dailyGridImport")]
    public double DailyGridImport { get; set; }

    [JsonPropertyName("monthlyGridExport")]
    public double MonthlyGridExport { get; set; }

    [JsonPropertyName("monthlyGridImport")]
    public double MonthlyGridImport { get; set; }

    [JsonPropertyName("yearlyGridExport")]
    public double YearlyGridExport { get; set; }

    [JsonPropertyName("yearlyGridImport")]
    public double YearlyGridImport { get; set; }

    [JsonPropertyName("lifetimeGridExport")]
    public double LifetimeGridExport { get; set; }

    [JsonPropertyName("lifetimeGridImport")]
    public double LifetimeGridImport { get; set; }

    [JsonPropertyName("dailyConsumption")]
    public double DailyConsumption { get; set; }

    [JsonPropertyName("monthlyConsumption")]
    public double MonthlyConsumption { get; set; }

    [JsonPropertyName("yearlyConsumption")]
    public double YearlyConsumption { get; set; }

    [JsonPropertyName("lifetimeConsumption")]
    public double LifetimeConsumption { get; set; }

    [JsonPropertyName("batteryPower")]
    public int BatteryPower { get; set; }

    [JsonPropertyName("batteryPowerA")]
    public int BatteryPowerA { get; set; }

    [JsonPropertyName("batteryPowerB")]
    public int BatteryPowerB { get; set; }

    [JsonPropertyName("batteryPowerC")]
    public int BatteryPowerC { get; set; }

    [JsonPropertyName("batterySOC")]
    public double BatterySOC { get; set; }

    [JsonPropertyName("batteryStatus")]
    public string? BatteryStatus { get; set; }

    [JsonPropertyName("batteryMode")]
    public string? BatteryMode { get; set; }

    [JsonPropertyName("dailyBatteryCharging")]
    public double DailyBatteryCharging { get; set; }

    [JsonPropertyName("dailyBatteryDischarge")]
    public double DailyBatteryDischarge { get; set; }

    [JsonPropertyName("monthly_batteryCharging")]
    public double MonthlyBatteryCharging { get; set; }

    [JsonPropertyName("monthlyBatteryCharging")]
    public double MonthlyBatteryChargingValue { get; set; }

    [JsonPropertyName("yearlyBatteryCharging")]
    public double YearlyBatteryCharging { get; set; }

    [JsonPropertyName("yearlyBatteryDischarge")]
    public double YearlyBatteryDischarge { get; set; }

    [JsonPropertyName("lifetimeBatteryCharging")]
    public double LifetimeBatteryCharging { get; set; }

    [JsonPropertyName("lifetimeBatteryDischarge")]
    public double LifetimeBatteryDischarge { get; set; }
}

/// <summary>
/// Données d'appareil de site
/// </summary>
public class DeviceDto
{
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("deivceSN")]
    public string? DeviceSN { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("deviceMode")]
    public string? DeviceMode { get; set; }

    [JsonPropertyName("MICapacity")]
    public double MICapacity { get; set; }

    [JsonPropertyName("batteryCapacity")]
    public double BatteryCapacity { get; set; }

    [JsonPropertyName("batteryChargeMaxPower")]
    public double BatteryChargeMaxPower { get; set; }

    [JsonPropertyName("batteryDisChargeMaxPower")]
    public double BatteryDisChargeMaxPower { get; set; }
}

/// <summary>
/// Données d'alerte d'appareil
/// </summary>
public class DeviceAlertDto
{
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("deviceSN")]
    public string? DeviceSN { get; set; }

    [JsonPropertyName("alarmId")]
    public string? AlarmId { get; set; }

    [JsonPropertyName("alarmName")]
    public string? AlarmName { get; set; }

    [JsonPropertyName("alarmLevel")]
    public string? AlarmLevel { get; set; }

    [JsonPropertyName("alarmOccurTime")]
    public string? AlarmOccurTime { get; set; }

    [JsonPropertyName("alarmCauseID")]
    public string? AlarmCauseID { get; set; }

    [JsonPropertyName("alarmCause")]
    public string? AlarmCause { get; set; }

    [JsonPropertyName("alarmRepairSuggestion")]
    public string? AlarmRepairSuggestion { get; set; }
}

/// <summary>
/// Données de puissance quotidienne du site
/// </summary>
public class SiteEnergyDto
{
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("dateType")]
    public string? DateType { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("soalrGeneration")]
    public double SolarGeneration { get; set; }

    [JsonPropertyName("consumption")]
    public double Consumption { get; set; }

    [JsonPropertyName("gridExport")]
    public double GridExport { get; set; }

    [JsonPropertyName("gridImport")]
    public double GridImport { get; set; }

    [JsonPropertyName("batteryCharging")]
    public double BatteryCharging { get; set; }

    [JsonPropertyName("batteryDischarge")]
    public double BatteryDischarge { get; set; }
}
