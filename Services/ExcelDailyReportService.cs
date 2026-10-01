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
    private readonly AtmoceCloudBrowserService _atmoceCloudBrowserService;

    public ExcelDailyReportService(
        AtmocDbContext dbContext,
        IConfiguration configuration,
        ILogger<ExcelDailyReportService> logger,
        AtmoceCloudBrowserService atmoceCloudBrowserService)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _atmoceCloudBrowserService = atmoceCloudBrowserService;
    }

    public sealed record DailyExportMergeResult(
        bool Success,
        int AddedDates,
        int UpdatedValues,
        string Message,
        string? SourceFilePath,
        string? DataPreview = null);

    public async Task<DailyExportMergeResult> MergeLatestDailyExportFromDownloadsAsync()
    {
        const string downloadsPath = @"D:\Downloads";
        const string filePrefix = "Données de production quotidienne de la centrale Esteves Pascal";

        if (!Directory.Exists(downloadsPath))
        {
            return new DailyExportMergeResult(false, 0, 0, $"Le dossier {downloadsPath} est introuvable.", null);
        }

        var sourceFilePath = Directory
            .EnumerateFiles(downloadsPath, "*.xlsx", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileName(path).StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            return new DailyExportMergeResult(false, 0, 0, "Aucun export journalier Atmoce trouvé dans D:\\Downloads.", null);
        }

        var targetFilePath = _configuration["ExcelImport:FilePath"];
        var sheetName = _configuration["ExcelImport:SheetName"] ?? "Relevé quotidien Atmoce";

        if (string.IsNullOrWhiteSpace(targetFilePath) || !File.Exists(targetFilePath))
        {
            return new DailyExportMergeResult(false, 0, 0, "Le fichier Excel d'origine est introuvable.", sourceFilePath);
        }

        string backupPath;
        try
        {
            backupPath = CreateBackupFile(targetFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impossible de créer la sauvegarde du fichier Excel avant mise à jour.");
            return new DailyExportMergeResult(false, 0, 0, "Sauvegarde impossible. Mise à jour annulée pour sécurité.", sourceFilePath);
        }

        using var sourceWorkbook = new XLWorkbook(sourceFilePath);
        using var targetWorkbook = new XLWorkbook(targetFilePath);

        var sourceWorksheet = sourceWorkbook.Worksheets.FirstOrDefault(w => w.Name == sheetName) ?? sourceWorkbook.Worksheets.First();
        var targetWorksheet = targetWorkbook.Worksheets.FirstOrDefault(w => w.Name == sheetName);

        if (targetWorksheet is null)
        {
            return new DailyExportMergeResult(false, 0, 0, $"La feuille {sheetName} est introuvable dans le fichier d'origine.", sourceFilePath);
        }

        var sourceHeaderRow = GetConfiguredHeaderRow(sourceWorksheet.Name) ?? FindHeaderRow(sourceWorksheet);
        if (sourceHeaderRow == 0)
        {
            sourceHeaderRow = 1;
        }

        var targetHeaderRow = GetConfiguredHeaderRow(sheetName) ?? FindHeaderRow(targetWorksheet);
        if (targetHeaderRow == 0)
        {
            targetHeaderRow = 1;
        }

        var targetRowsByDate = new Dictionary<DateOnly, int>();
        var targetLastRow = FindDataLastRowBeforeTotal(targetWorksheet, targetHeaderRow);
        for (var row = targetHeaderRow + 1; row <= targetLastRow; row++)
        {
            if (TryParseDate(targetWorksheet.Cell(row, 1), out var existingDate))
            {
                targetRowsByDate[existingDate] = row;
            }
        }

        var sourceLastRow = FindDataLastRowBeforeTotal(sourceWorksheet, sourceHeaderRow);
        var addedDates = 0;
        var updatedValues = 0;
        var ignoredDates = 0;

        for (var row = sourceHeaderRow + 1; row <= sourceLastRow; row++)
        {
            if (!TryParseDate(sourceWorksheet.Cell(row, 1), out var recordDate))
            {
                continue;
            }

            var values = ReadDailyValues(sourceWorksheet, row);

            if (targetRowsByDate.TryGetValue(recordDate, out var targetRow))
            {
                updatedValues += UpdateRowValuesIfHigher(targetWorksheet, targetRow, values);
            }
            else
            {
                ignoredDates++;
            }
        }

        if (updatedValues == 0)
        {
            var noUpdateMessage = ignoredDates > 0
                ? $"Aucune valeur supérieure à appliquer ({ignoredDates} date(s) absente(s) du fichier d'origine ignorée(s))."
                : "Aucune donnée supérieure à la valeur existante à appliquer.";

            return new DailyExportMergeResult(true, 0, 0, noUpdateMessage, sourceFilePath);
        }

        targetWorkbook.Save();
        await ImportMissingDailyRecordsAsync();

        return new DailyExportMergeResult(
            true,
            addedDates,
            updatedValues,
            $"Mise à jour terminée : {updatedValues} valeur(s) mise(s) à jour. {ignoredDates} date(s) absente(s) ignorée(s). Sauvegarde : {Path.GetFileName(backupPath)}",
            sourceFilePath);
    }

    public async Task<DailyExportMergeResult> ImportFromAtmoceCloudAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("Récupération des données depuis Atmoce Cloud...");
        var tableResult = await _atmoceCloudBrowserService.GetDailyProductionTableAsync(progress, cancellationToken);
        if (!tableResult.Success)
        {
            return new DailyExportMergeResult(false, 0, 0, tableResult.Message, null);
        }

        var dataPreview = BuildDataPreview(tableResult.Headers, tableResult.Rows);
        var importedRows = tableResult.Rows.Count;

        return new DailyExportMergeResult(
            true,
            0,
            importedRows,
            $"Données Atmoce Cloud récupérées : {importedRows} ligne(s) prêtes à l’affichage.",
            tableResult.CurrentUrl,
            dataPreview);
    }

    private static string BuildDataPreview(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, int maxRows = 5)
    {
        var lines = new List<string>();

        if (headers.Count > 0)
        {
            lines.Add(string.Join(" | ", headers));
        }

        foreach (var row in rows.Take(maxRows))
        {
            lines.Add(string.Join(" | ", row));
        }

        return string.Join(Environment.NewLine, lines);
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

    private static double[] ReadDailyValues(IXLWorksheet worksheet, int row)
    {
        return
        [
            ParseDouble(worksheet.Cell(row, 2)),
            ParseDouble(worksheet.Cell(row, 3)),
            ParseDouble(worksheet.Cell(row, 4)),
            ParseDouble(worksheet.Cell(row, 5)),
            ParseDouble(worksheet.Cell(row, 6)),
            ParseDouble(worksheet.Cell(row, 7)),
            ParseDouble(worksheet.Cell(row, 8)),
            ParseDouble(worksheet.Cell(row, 9))
        ];
    }

    private static int UpdateRowValuesIfHigher(IXLWorksheet worksheet, int row, IReadOnlyList<double> sourceValues)
    {
        var updatedValues = 0;

        for (var index = 0; index < sourceValues.Count; index++)
        {
            var column = index + 2;
            var targetCell = worksheet.Cell(row, column);

            if (!string.IsNullOrWhiteSpace(targetCell.FormulaA1))
            {
                continue;
            }

            var currentValue = ParseDouble(targetCell);
            var incomingValue = sourceValues[index];

            if (incomingValue > currentValue)
            {
                targetCell.Value = incomingValue;
                updatedValues++;
            }
        }

        return updatedValues;
    }

    private static int UpdateRowValuesIfHigher(IXLWorksheet worksheet, int row, IReadOnlyList<string> sourceValues)
    {
        var parsedValues = new List<double>();
        for (var index = 1; index < sourceValues.Count; index++)
        {
            parsedValues.Add(ParseDouble(sourceValues[index]));
        }

        return UpdateRowValuesIfHigher(worksheet, row, parsedValues);
    }

    private static int FindDataLastRowBeforeTotal(IXLWorksheet worksheet, int headerRow)
    {
        var lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var row = headerRow + 1; row <= lastUsedRow; row++)
        {
            var firstColumnText = worksheet.Cell(row, 1).GetString().Trim();
            if (firstColumnText.StartsWith("Total", StringComparison.OrdinalIgnoreCase))
            {
                return row - 1;
            }
        }

        return lastUsedRow;
    }

    private static string CreateBackupFile(string targetFilePath)
    {
        var directory = Path.GetDirectoryName(targetFilePath) ?? Environment.CurrentDirectory;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(targetFilePath);
        var extension = Path.GetExtension(targetFilePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var backupPath = Path.Combine(directory, $"{fileNameWithoutExtension}.backup-{timestamp}{extension}");

        File.Copy(targetFilePath, backupPath, overwrite: false);
        return backupPath;
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
        return TryParseDate(input, out date);
    }

    private static bool TryParseDate(string? input, out DateOnly date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return DateOnly.TryParseExact(input, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
            CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out date);
    }

    private static double ParseDouble(IXLCell cell)
    {
        if (cell.TryGetValue<double>(out var numericValue))
        {
            return numericValue;
        }

        return ParseDouble(cell.GetString());
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

