using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SolarApp.Data;
using SolarApp.Models;
using SolarApp.Services;

namespace SolarApp.Tests;

[TestClass]
public sealed class SolarDataServiceTests
{
    [TestMethod]
    public async Task ClearDatabaseRecordsAsync_SupprimeTousLesEnregistrements()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var site = new Site { SiteId = "SITE-1", Name = "Site Test" };
        dbContext.Sites.Add(site);
        await dbContext.SaveChangesAsync();

        var device = new Device { SiteId = site.Id, DeviceSerialNumber = "DEV-1", DeviceType = "gateway" };
        dbContext.Devices.Add(device);
        await dbContext.SaveChangesAsync();

        dbContext.SiteDataSnapshots.Add(new SiteDataSnapshot { SiteId = site.Id, SnapshotTime = DateTime.UtcNow });
        dbContext.DeviceDataSnapshots.Add(new DeviceDataSnapshot { DeviceId = device.Id, SnapshotTime = DateTime.UtcNow });
        dbContext.DeviceAlerts.Add(new DeviceAlert { SiteId = site.SiteId, DeviceSerialNumber = "DEV-1", AlarmId = "AL-1", AlarmOccurTime = DateTime.UtcNow });
        dbContext.DailyEnergyRecords.Add(new DailyEnergyRecord { RecordDate = DateOnly.FromDateTime(DateTime.UtcNow), ImportedAt = DateTime.UtcNow });
        dbContext.ApiTokens.Add(new ApiToken { AccessToken = "A", RefreshToken = "R", ExpiresAt = DateTime.UtcNow.AddHours(1), RefreshExpiresAt = DateTime.UtcNow.AddDays(1) });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var deletedCount = await service.ClearDatabaseRecordsAsync();

        Assert.AreEqual(7, deletedCount);
        Assert.AreEqual(0, await dbContext.Sites.CountAsync());
        Assert.AreEqual(0, await dbContext.Devices.CountAsync());
        Assert.AreEqual(0, await dbContext.SiteDataSnapshots.CountAsync());
        Assert.AreEqual(0, await dbContext.DeviceDataSnapshots.CountAsync());
        Assert.AreEqual(0, await dbContext.DeviceAlerts.CountAsync());
        Assert.AreEqual(0, await dbContext.DailyEnergyRecords.CountAsync());
        Assert.AreEqual(0, await dbContext.ApiTokens.CountAsync());
    }

    [TestMethod]
    public async Task MarkAlertAsResolvedAsync_MarqueLalerteCommeResolue()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var alert = new DeviceAlert
        {
            SiteId = "SITE-1",
            DeviceSerialNumber = "DEV-1",
            AlarmId = "AL-1",
            AlarmOccurTime = DateTime.UtcNow,
            Resolved = false
        };

        dbContext.DeviceAlerts.Add(alert);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.MarkAlertAsResolvedAsync(alert.Id);

        var updatedAlert = await dbContext.DeviceAlerts.SingleAsync(a => a.Id == alert.Id);
        Assert.IsTrue(updatedAlert.Resolved);
        Assert.IsNotNull(updatedAlert.ResolvedAt);
    }

    [TestMethod]
    public async Task GetSiteDataSnapshotsAsync_FiltreParPeriodeEtTrieParDateDesc()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        var site = new Site { SiteId = "SITE-1", Name = "Site Test" };
        dbContext.Sites.Add(site);
        await dbContext.SaveChangesAsync();

        var newer = DateTime.UtcNow.AddDays(-1);
        var older = DateTime.UtcNow.AddDays(-2);

        dbContext.SiteDataSnapshots.AddRange(
            new SiteDataSnapshot { SiteId = site.Id, SnapshotTime = newer },
            new SiteDataSnapshot { SiteId = site.Id, SnapshotTime = DateTime.UtcNow.AddDays(-10) },
            new SiteDataSnapshot { SiteId = site.Id, SnapshotTime = older });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var snapshots = await service.GetSiteDataSnapshotsAsync(site.Id, daysBack: 7);

        Assert.AreEqual(2, snapshots.Count);
        Assert.AreEqual(newer, snapshots[0].SnapshotTime);
        Assert.AreEqual(older, snapshots[1].SnapshotTime);
    }

    [TestMethod]
    public async Task GetActiveAlertsAsync_RetourneSeulementLesAlertesActivesDuSiteTriees()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.DeviceAlerts.AddRange(
            new DeviceAlert { SiteId = "SITE-1", DeviceSerialNumber = "DEV-1", AlarmId = "AL-1", AlarmOccurTime = DateTime.UtcNow.AddHours(-2), Resolved = false },
            new DeviceAlert { SiteId = "SITE-1", DeviceSerialNumber = "DEV-1", AlarmId = "AL-2", AlarmOccurTime = DateTime.UtcNow.AddHours(-1), Resolved = false },
            new DeviceAlert { SiteId = "SITE-1", DeviceSerialNumber = "DEV-1", AlarmId = "AL-3", AlarmOccurTime = DateTime.UtcNow, Resolved = true },
            new DeviceAlert { SiteId = "SITE-2", DeviceSerialNumber = "DEV-2", AlarmId = "AL-4", AlarmOccurTime = DateTime.UtcNow, Resolved = false });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var alerts = await service.GetActiveAlertsAsync("SITE-1");

        Assert.AreEqual(2, alerts.Count);
        Assert.AreEqual("AL-2", alerts[0].AlarmId);
        Assert.AreEqual("AL-1", alerts[1].AlarmId);
    }

    private static AtmocDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AtmocDbContext>()
            .UseSqlite(connection)
            .Options;

        return new AtmocDbContext(options);
    }

    private static SolarDataService CreateService(AtmocDbContext dbContext)
    {
        return new SolarDataService(
            apiService: null!,
            dbContext: dbContext,
            excelDailyReportService: null!,
            logger: NullLogger<SolarDataService>.Instance);
    }
}
