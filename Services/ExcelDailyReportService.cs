using System.Globalization;
using SolarApp.Data;
using SolarApp.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace SolarApp.Services;

public class ExcelDailyReportService
{
    private readonly AtmocDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExcelDailyReportService> _logger;

    public ExcelDailyReportService(
        AtmocDbContext dbContext,
        IConfiguration configuration,
        ILogger<ExcelDailyReportService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> ImportMissingDailyRecordsAsync()
    {
        var filePath = _configuration["ExcelImport:FilePath"];
        var sheetName = _configuration["ExcelImport:SheetName"] ?? "Relevé quotidien Atmoce";

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            _logger.LogWarning("Fichier Excel introuvable : {FilePath}", filePath);
            return 0;
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == sheetName);

        if (worksheet is null)
        {
            _logger.LogWarning("Feuille Excel introuvable : {SheetName}", sheetName);
            return 0;
        }

        var headerRow = FindHeaderRow(worksheet);
        if (headerRow == 0)
        {
            _logger.LogWarning("Ligne d'en-tête introuvable dans la feuille {SheetName}", sheetName);
            return 0;
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var importedCount = 0;
        var updatedCount = 0;

        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            var dateCell = worksheet.Cell(row, 1);
            if (!TryParseDate(dateCell, out var date))
            {
                continue;
            }

            var producedKwh = ParseDouble(worksheet.Cell(row, 2).GetString());
            var consumedKwh = ParseDouble(worksheet.Cell(row, 3).GetString());
            var fromGridKwh = ParseDouble(worksheet.Cell(row, 4).GetString());
            var toGridKwh = ParseDouble(worksheet.Cell(row, 5).GetString());
            var chargedKwh = ParseDouble(worksheet.Cell(row, 6).GetString());
            var dischargedKwh = ParseDouble(worksheet.Cell(row, 7).GetString());
            var socMax = ParseDouble(worksheet.Cell(row, 8).GetString());
            var columnIValue = ParseDouble(worksheet.Cell(row, 9).GetString());

            var existingRecord = await _dbContext.DailyEnergyRecords.FirstOrDefaultAsync(r => r.RecordDate == date);
            if (existingRecord is null)
            {
                var record = new DailyEnergyRecord
                {
                    RecordDate = date,
                    ProducedKwh = producedKwh,
                    ConsumedKwh = consumedKwh,
                    FromGridKwh = fromGridKwh,
                    ToGridKwh = toGridKwh,
                    ChargedKwh = chargedKwh,
                    DischargedKwh = dischargedKwh,
                    SocMax = socMax,
                    ColumnIValue = columnIValue,
                    ImportedAt = DateTime.UtcNow
                };

                _dbContext.DailyEnergyRecords.Add(record);
                importedCount++;
            }
            else
            {
                existingRecord.ProducedKwh = producedKwh;
                existingRecord.ConsumedKwh = consumedKwh;
                existingRecord.FromGridKwh = fromGridKwh;
                existingRecord.ToGridKwh = toGridKwh;
                existingRecord.ChargedKwh = chargedKwh;
                existingRecord.DischargedKwh = dischargedKwh;
                existingRecord.SocMax = socMax;
                existingRecord.ColumnIValue = columnIValue;
                updatedCount++;
            }
        }

        if (importedCount > 0 || updatedCount > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("Import Excel terminé : {ImportedCount} nouvelle(s) date(s), {UpdatedCount} mise(s) à jour", importedCount, updatedCount);
        return importedCount;
    }

    public Task<(List<string> Headers, List<List<string>> Rows)> ReadSheetContentAsync(string sheetName)
    {
        return ReadSheetContentAsync(sheetName, logWarning: true);
    }

    public Task<(List<string> Headers, List<List<string>> Rows)> ReadSheetContentSilentAsync(string sheetName)
    {
        return ReadSheetContentAsync(sheetName, logWarning: false);
    }

    public Task<(List<string> Headers, List<List<string>> Rows)> ReadSheetContentSilentAsync(string sheetName, int lastColumnIndex)
    {
        return ReadSheetContentAsync(sheetName, lastColumnIndex, logWarning: false);
    }

    private Task<(List<string> Headers, List<List<string>> Rows)> ReadSheetContentAsync(string sheetName, bool logWarning)    {
        var filePath = _configuration["ExcelImport:FilePath"];
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            if (logWarning)
            {
                _logger.LogWarning("Fichier Excel introuvable : {FilePath}", filePath);
            }
            return Task.FromResult((new List<string>(), new List<List<string>>()));
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == sheetName);
        if (worksheet is null)
        {
            if (logWarning)
            {
                _logger.LogWarning("Feuille Excel introuvable : {SheetName}", sheetName);
            }
            return Task.FromResult((new List<string>(), new List<List<string>>()));
        }

        var headerRow = GetConfiguredHeaderRow(sheetName) ?? FindHeaderRow(worksheet);
        if (headerRow == 0)
        {
            headerRow = 1;
        }

        var detectedLastColumnIndex = DetectLastUsefulColumnIndex(worksheet, headerRow);
        return ReadSheetContentAsync(sheetName, detectedLastColumnIndex, logWarning);
    }

    public Task<(List<string> Headers, List<List<string>> Rows)> ReadSheetContentAsync(string sheetName, int lastColumnIndex)
    {
        return ReadSheetContentAsync(sheetName, lastColumnIndex, logWarning: true);
    }

    private Task<(List<string> Headers, List<List<string>> Rows)> ReadSheetContentAsync(string sheetName, int lastColumnIndex, bool logWarning)
    {
        var filePath = _configuration["ExcelImport:FilePath"];
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            if (logWarning)
            {
                _logger.LogWarning("Fichier Excel introuvable : {FilePath}", filePath);
            }
            return Task.FromResult((new List<string>(), new List<List<string>>()));
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == sheetName);
        if (worksheet is null)
        {
            if (logWarning)
            {
                _logger.LogWarning("Feuille Excel introuvable : {SheetName}", sheetName);
            }
            return Task.FromResult((new List<string>(), new List<List<string>>()));
        }

        var headerRow = GetConfiguredHeaderRow(sheetName) ?? FindHeaderRow(worksheet);

        if (headerRow == 0)
        {
            headerRow = 1;
        }

        var headers = new List<string>();
        for (var column = 1; column <= lastColumnIndex; column++)
        {
            var header = worksheet.Cell(headerRow, column).GetString().Trim();
            headers.Add(string.IsNullOrWhiteSpace(header) ? $"Colonne {column}" : header);
        }

        var rows = new List<List<string>>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            var values = new List<string>();
            var hasValue = false;

            for (var column = 1; column <= lastColumnIndex; column++)
            {
                var value = worksheet.Cell(row, column).GetFormattedString().Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    hasValue = true;
                }

                values.Add(value);
            }

            if (hasValue)
            {
                rows.Add(values);
            }
        }

        return Task.FromResult((headers, rows));
    }

    private static int FindHeaderRow(IXLWorksheet worksheet)
    {
        var maxScanRow = Math.Min(30, worksheet.LastRowUsed()?.RowNumber() ?? 30);

        for (var row = 1; row <= maxScanRow; row++)
        {
            var firstCell = string.Empty;
            var secondCell = string.Empty;
            var ninthCell = string.Empty;

            try
            {
                firstCell = worksheet.Cell(row, 1).GetString().Trim();
            }
            catch (ArgumentException)
            {
                // Ignore formula/function cells not supported by ClosedXML.
            }

            try
            {
                secondCell = worksheet.Cell(row, 2).GetString().Trim();
            }
            catch (ArgumentException)
            {
                // Ignore formula/function cells not supported by ClosedXML.
            }

            try
            {
                ninthCell = worksheet.Cell(row, 9).GetString().Trim();
            }
            catch (ArgumentException)
            {
                // Ignore formula/function cells not supported by ClosedXML.
            }

            if (string.Equals(firstCell, "Date", StringComparison.OrdinalIgnoreCase)
                && (
                    secondCell.Contains("Produit", StringComparison.OrdinalIgnoreCase)
                    || ninthCell.Contains("SOC", StringComparison.OrdinalIgnoreCase)
                ))
            {
                return row;
            }
        }

        return 0;
    }

    private static int DetectLastUsefulColumnIndex(IXLWorksheet worksheet, int headerRow)
    {
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;

        for (var column = lastColumn; column >= 1; column--)
        {
            try
            {
                var text = worksheet.Cell(headerRow, column).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return column;
                }
            }
            catch (ArgumentException)
            {
                // Ignore formula/function cells not supported by ClosedXML.
            }
        }

        return 1;
    }

    private static int? GetConfiguredHeaderRow(string sheetName)
    {
        if (sheetName.Equals("Relevé quotidien Atmoce", StringComparison.OrdinalIgnoreCase)
            || sheetName.Equals("Relevé mensuel Atmoce", StringComparison.OrdinalIgnoreCase)
            || sheetName.Equals("Données ensoleillement", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (sheetName.Equals("Relevé linky", StringComparison.OrdinalIgnoreCase)
            || sheetName.Equals("Bilan énergétique annuel", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return null;
    }

    private static bool TryParseDate(IXLCell cell, out DateOnly date)
    {
        date = default;

        if (cell.IsEmpty())
        {
            return false;
        }

        if (cell.TryGetValue<DateTime>(out var dateTimeValue))
        {
            date = DateOnly.FromDateTime(dateTimeValue);
            return true;
        }

        if (cell.TryGetValue<double>(out var oaDateValue))
        {
            try
            {
                date = DateOnly.FromDateTime(DateTime.FromOADate(oaDateValue));
                return true;
            }
            catch
            {
            }
        }

        var input = cell.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (DateOnly.TryParseExact(input, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
            CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(input, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out var parsedDateTime))
        {
            date = DateOnly.FromDateTime(parsedDateTime);
            return true;
        }

        return false;
    }

    private static double ParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var normalized = value.Trim().Replace(" ", string.Empty);

        if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.GetCultureInfo("fr-FR"), out var frenchValue))
        {
            return frenchValue;
        }

        if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariantValue))
        {
            return invariantValue;
        }

        return 0;
    }
}

