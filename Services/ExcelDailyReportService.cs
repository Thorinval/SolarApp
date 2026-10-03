using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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

        progress?.Report("Mise à jour des données dans l’application...");
        var (addedDates, updatedDates) = await UpsertDailyRecordsFromCloudRowsAsync(tableResult.Headers, tableResult.Rows, cancellationToken);

        progress?.Report("Recalcul des données ensoleillement...");
        var updatedSunshineCells = await RefreshSunshineDataFromDailyRecordsAsync(cancellationToken);

        var dataPreview = BuildDataPreview(tableResult.Headers, tableResult.Rows);
        var importedRows = tableResult.Rows.Count;

        return new DailyExportMergeResult(
            true,
            addedDates,
            updatedDates,
            $"Import Atmoce Cloud terminé : {addedDates} nouvelle(s) date(s), {updatedDates} date(s) mise(s) à jour, {importedRows} ligne(s) analysée(s), {updatedSunshineCells} cellule(s) ensoleillement recalculée(s).",
            tableResult.CurrentUrl,
            dataPreview);
    }

    private async Task<(int AddedDates, int UpdatedDates)> UpsertDailyRecordsFromCloudRowsAsync(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, CancellationToken cancellationToken)
    {
        var addedDates = 0;
        var updatedDates = 0;
        var parsedRows = 0;

        var columnMap = ResolveCloudColumnMap(headers);
        _logger.LogInformation("Mapping colonnes Atmoce Cloud détecté : Date={Date}, Produit={Produced}, Consommé={Consumed}, DepuisRéseau={FromGrid}, VersRéseau={ToGrid}, Chargé={Charged}, Déchargé={Discharged}, SocMax={SocMax}, SocMin={SocMin}",
            columnMap.Date,
            columnMap.Produced,
            columnMap.Consumed,
            columnMap.FromGrid,
            columnMap.ToGrid,
            columnMap.Charged,
            columnMap.Discharged,
            columnMap.SocMax,
            columnMap.SocMin);

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryParseCloudDailyRow(row, columnMap, out var date, out var values))
            {
                continue;
            }

            parsedRows++;

            var producedKwh = values[0];
            var consumedKwh = values[1];
            var fromGridKwh = values[2];
            var toGridKwh = values[3];
            var chargedKwh = values[4];
            var dischargedKwh = values[5];
            var socMax = values[6];
            var columnIValue = values[7];

            var existingRecord = await _dbContext.DailyEnergyRecords.FirstOrDefaultAsync(r => r.RecordDate == date, cancellationToken);
            if (existingRecord is null)
            {
                _dbContext.DailyEnergyRecords.Add(new DailyEnergyRecord
                {
                    RecordDate = date,
                    ProducedKwh = double.IsNaN(producedKwh) ? 0 : producedKwh,
                    ConsumedKwh = double.IsNaN(consumedKwh) ? 0 : consumedKwh,
                    FromGridKwh = double.IsNaN(fromGridKwh) ? 0 : fromGridKwh,
                    ToGridKwh = double.IsNaN(toGridKwh) ? 0 : toGridKwh,
                    ChargedKwh = double.IsNaN(chargedKwh) ? 0 : chargedKwh,
                    DischargedKwh = double.IsNaN(dischargedKwh) ? 0 : dischargedKwh,
                    SocMax = double.IsNaN(socMax) ? 0 : socMax,
                    ColumnIValue = double.IsNaN(columnIValue) ? 0 : columnIValue,
                    ImportedAt = DateTime.UtcNow
                });

                addedDates++;
                continue;
            }

            var hasChanges = false;

            if (!double.IsNaN(producedKwh) && producedKwh != existingRecord.ProducedKwh)
            {
                existingRecord.ProducedKwh = producedKwh;
                hasChanges = true;
            }

            if (!double.IsNaN(consumedKwh) && consumedKwh != existingRecord.ConsumedKwh)
            {
                existingRecord.ConsumedKwh = consumedKwh;
                hasChanges = true;
            }

            if (!double.IsNaN(fromGridKwh) && fromGridKwh != existingRecord.FromGridKwh)
            {
                existingRecord.FromGridKwh = fromGridKwh;
                hasChanges = true;
            }

            if (!double.IsNaN(toGridKwh) && toGridKwh != existingRecord.ToGridKwh)
            {
                existingRecord.ToGridKwh = toGridKwh;
                hasChanges = true;
            }

            if (!double.IsNaN(chargedKwh) && chargedKwh != existingRecord.ChargedKwh)
            {
                existingRecord.ChargedKwh = chargedKwh;
                hasChanges = true;
            }

            if (!double.IsNaN(dischargedKwh) && dischargedKwh != existingRecord.DischargedKwh)
            {
                existingRecord.DischargedKwh = dischargedKwh;
                hasChanges = true;
            }

            if (!double.IsNaN(socMax) && socMax != existingRecord.SocMax)
            {
                existingRecord.SocMax = socMax;
                hasChanges = true;
            }

            if (!double.IsNaN(columnIValue) && columnIValue != existingRecord.ColumnIValue)
            {
                existingRecord.ColumnIValue = columnIValue;
                hasChanges = true;
            }

            if (hasChanges)
            {
                existingRecord.ImportedAt = DateTime.UtcNow;
                updatedDates++;
            }
        }

        if (addedDates > 0 || updatedDates > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (parsedRows == 0 && rows.Count > 0)
        {
            var sample = string.Join(" | ", rows[0]);
            _logger.LogWarning("Aucune ligne Atmoce Cloud exploitable. Exemple première ligne : {SampleRow}", sample);
        }

        _logger.LogInformation("Import Atmoce Cloud vers DB terminé : {AddedDates} ajout(s), {UpdatedDates} date(s) mise(s) à jour, {ParsedRows}/{TotalRows} ligne(s) exploitables", addedDates, updatedDates, parsedRows, rows.Count);
        return (addedDates, updatedDates);
    }

    private static bool TryParseCloudDailyRow(IReadOnlyList<string> row, DailyCloudColumnMap map, out DateOnly date, out double[] values)
    {
        date = default;
        values = [];

        if (row.Count < 2)
        {
            return false;
        }

        if (map.Date >= 0 && map.Date < row.Count && TryParseDate(row[map.Date], out var mappedDate))
        {
            date = mappedDate;
        }
        else
        {
            var parsed = false;
            for (var index = 0; index < row.Count; index++)
            {
                if (!TryParseDate(row[index], out var scannedDate))
                {
                    continue;
                }

                date = scannedDate;
                parsed = true;
                break;
            }

            if (!parsed)
            {
                return false;
            }
        }

        values =
        [
            GetCloudMetricValue(row, map.Produced),
            GetCloudMetricValue(row, map.Consumed),
            GetCloudMetricValue(row, map.FromGrid),
            GetCloudMetricValue(row, map.ToGrid),
            GetCloudMetricValue(row, map.Charged),
            GetCloudMetricValue(row, map.Discharged),
            GetCloudMetricValue(row, map.SocMax),
            GetCloudMetricValue(row, map.SocMin)
        ];

        return true;
    }

    private static DailyCloudColumnMap ResolveCloudColumnMap(IReadOnlyList<string> headers)
    {
        static bool Match(string normalized, params string[] keys)
            => keys.Any(key => normalized.Contains(key, StringComparison.Ordinal));

        var map = new DailyCloudColumnMap(-1, -1, -1, -1, -1, -1, -1, -1, -1);

        for (var index = 0; index < headers.Count; index++)
        {
            var normalized = NormalizeHeader(headers[index]);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (map.Date < 0 && Match(normalized, "date"))
            {
                map = map with { Date = index };
                continue;
            }

            if (map.Produced < 0 && Match(normalized, "produit", "production"))
            {
                map = map with { Produced = index };
                continue;
            }

            if (map.Consumed < 0 && Match(normalized, "consomme", "consommation"))
            {
                map = map with { Consumed = index };
                continue;
            }

            if (map.FromGrid < 0 && Match(normalized, "depuisreseau", "fromgrid", "importreseau", "consoreseau", "reseauconsomme"))
            {
                map = map with { FromGrid = index };
                continue;
            }

            if (map.ToGrid < 0 && Match(normalized, "versreseau", "togrid", "exportreseau"))
            {
                map = map with { ToGrid = index };
                continue;
            }

            if (map.Charged < 0 && Match(normalized, "charge") && !Match(normalized, "decharge"))
            {
                map = map with { Charged = index };
                continue;
            }

            if (map.Discharged < 0 && Match(normalized, "decharge"))
            {
                map = map with { Discharged = index };
                continue;
            }

            if (map.SocMax < 0 && Match(normalized, "socmax", "socmaximum", "socmax%"))
            {
                map = map with { SocMax = index };
                continue;
            }

            if (map.SocMin < 0 && Match(normalized, "socmin", "socminimum", "socmin%"))
            {
                map = map with { SocMin = index };
            }
        }

        var hasUsableHeaders = headers.Any(h => !string.IsNullOrWhiteSpace(h));
        var dateIndex = map.Date >= 0 ? map.Date : 0;
        var start = dateIndex + 1;

        if (!hasUsableHeaders)
        {
            return map with
            {
                Produced = map.Produced >= 0 ? map.Produced : start,
                Consumed = map.Consumed >= 0 ? map.Consumed : start + 1,
                FromGrid = map.FromGrid >= 0 ? map.FromGrid : start + 2,
                ToGrid = map.ToGrid >= 0 ? map.ToGrid : start + 3,
                Charged = map.Charged >= 0 ? map.Charged : start + 4,
                Discharged = map.Discharged >= 0 ? map.Discharged : start + 5,
                SocMax = map.SocMax >= 0 ? map.SocMax : start + 6,
                SocMin = map.SocMin >= 0 ? map.SocMin : start + 7
            };
        }

        return map with
        {
            Produced = map.Produced >= 0 ? map.Produced : start,
            Consumed = map.Consumed >= 0 ? map.Consumed : start + 1
        };
    }

    private static string NormalizeHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static double GetCloudMetricValue(IReadOnlyList<string> row, int index)
    {
        if (index < 0 || index >= row.Count)
        {
            return double.NaN;
        }

        return ParseDouble(row[index]);
    }

    private async Task<int> RefreshSunshineDataFromDailyRecordsAsync(CancellationToken cancellationToken)
    {
        const string sunshineSheetName = "Données ensoleillement";

        var filePath = _configuration["ExcelImport:FilePath"];
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            _logger.LogWarning("Fichier Excel introuvable pour recalcul ensoleillement : {FilePath}", filePath);
            return 0;
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == sunshineSheetName);
        if (worksheet is null)
        {
            _logger.LogWarning("Feuille Excel introuvable pour recalcul ensoleillement : {SheetName}", sunshineSheetName);
            return 0;
        }

        var headerRow = GetConfiguredHeaderRow(sunshineSheetName) ?? FindHeaderRow(worksheet);
        if (headerRow == 0)
        {
            headerRow = 1;
        }

        var monthRows = new Dictionary<int, int>();
        var lastDataRow = FindDataLastRowBeforeTotal(worksheet, headerRow);
        for (var row = headerRow + 1; row <= lastDataRow; row++)
        {
            var monthText = worksheet.Cell(row, 1).GetString().Trim();
            if (TryParseFrenchMonth(monthText, out var month))
            {
                monthRows[month] = row;
            }
        }

        if (monthRows.Count == 0)
        {
            _logger.LogWarning("Aucun libellé de mois exploitable trouvé dans la feuille {SheetName}", sunshineSheetName);
            return 0;
        }

        var monthlyStats = await _dbContext.DailyEnergyRecords
            .AsNoTracking()
            .GroupBy(r => new { r.RecordDate.Year, r.RecordDate.Month })
            .Select(g => new MonthlyAtmoceStat(
                g.Key.Year,
                g.Key.Month,
                g.Sum(x => x.ProducedKwh),
                g.Sum(x => x.ConsumedKwh),
                g.Sum(x => x.FromGridKwh)))
            .ToListAsync(cancellationToken);

        var updatedCells = 0;

        foreach (var yearGroup in monthlyStats.GroupBy(x => x.Year))
        {
            var columns = GetSunshineColumnsForYear(yearGroup.Key);
            if (columns is null)
            {
                continue;
            }

            var producedIndex = 0d;
            foreach (var monthStat in yearGroup.OrderBy(x => x.Month))
            {
                if (!monthRows.TryGetValue(monthStat.Month, out var targetRow))
                {
                    continue;
                }

                producedIndex += monthStat.ProducedKwh;

                updatedCells += SetCellNumericIfChanged(worksheet, targetRow, columns.ProducedKwh, monthStat.ProducedKwh, 2);
                updatedCells += SetCellNumericIfChanged(worksheet, targetRow, columns.ProducedIndex, producedIndex, 2);
                updatedCells += SetCellNumericIfChanged(worksheet, targetRow, columns.ConsumedKwh, monthStat.ConsumedKwh, 2);
                updatedCells += SetCellNumericIfChanged(worksheet, targetRow, columns.FromGridKwh, monthStat.FromGridKwh, 2);

                var coverageRatio = monthStat.ConsumedKwh == 0
                    ? 0
                    : 1 - (monthStat.FromGridKwh / monthStat.ConsumedKwh);

                var gridRatio = monthStat.ConsumedKwh == 0
                    ? 0
                    : monthStat.FromGridKwh / monthStat.ConsumedKwh;

                updatedCells += SetCellNumericIfChanged(worksheet, targetRow, columns.CoveragePercent, coverageRatio, 4);
                updatedCells += SetCellNumericIfChanged(worksheet, targetRow, columns.GridPercent, gridRatio, 4);
            }
        }

        if (updatedCells > 0)
        {
            CreateBackupFile(filePath);
            workbook.Save();
        }

        _logger.LogInformation("Recalcul ensoleillement terminé : {UpdatedCells} cellule(s) mise(s) à jour", updatedCells);
        return updatedCells;
    }

    private static SunshineColumns? GetSunshineColumnsForYear(int year)
    {
        const int firstYear = 2026;
        const int firstProducedCol = 8;
        const int yearBlockSize = 9;

        if (year < firstYear)
        {
            return null;
        }

        var blockOffset = (year - firstYear) * yearBlockSize;
        var producedCol = firstProducedCol + blockOffset;

        return new SunshineColumns(
            producedCol,
            producedCol + 1,
            producedCol + 2,
            producedCol + 3,
            producedCol + 4,
            producedCol + 5);
    }

    private static bool TryParseFrenchMonth(string input, out int month)
    {
        month = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var frCulture = CultureInfo.GetCultureInfo("fr-FR");
        if (DateTime.TryParseExact(input.Trim(), "MMMM", frCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDate)
            || DateTime.TryParse(input.Trim(), frCulture, DateTimeStyles.AllowWhiteSpaces, out parsedDate))
        {
            month = parsedDate.Month;
            return true;
        }

        return false;
    }

    private static int SetCellNumericIfChanged(IXLWorksheet worksheet, int row, int column, double value, int decimals)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        var cell = worksheet.Cell(row, column);
        var rounded = Math.Round(value, decimals);

        if (!TryReadCellNumeric(cell, out var current))
        {
            if (!string.IsNullOrWhiteSpace(cell.FormulaA1))
            {
                cell.FormulaA1 = string.Empty;
            }

            cell.Value = rounded;
            return 1;
        }

        if (Math.Abs(current - rounded) < 0.0001)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(cell.FormulaA1))
        {
            cell.FormulaA1 = string.Empty;
        }

        cell.Value = rounded;
        return 1;
    }

    private static bool TryReadCellNumeric(IXLCell cell, out double value)
    {
        value = 0;

        try
        {
            value = ParseDouble(cell);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
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

        try
        {
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
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Fichier Excel verrouillé ou inaccessible : {FilePath}", filePath);
            return 0;
        }
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

        try
        {
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
        catch (IOException ex)
        {
            if (logWarning)
            {
                _logger.LogWarning(ex, "Fichier Excel verrouillé ou inaccessible : {FilePath}", filePath);
            }
            return Task.FromResult((new List<string>(), new List<List<string>>()));
        }
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

        try
        {
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
                    var cell = worksheet.Cell(row, column);
                    string value;

                    if (!string.IsNullOrWhiteSpace(cell.FormulaA1))
                    {
                        value = cell.CachedValue.ToString(CultureInfo.GetCultureInfo("fr-FR"))?.Trim() ?? string.Empty;
                    }
                    else
                    {
                        try
                        {
                            value = cell.GetFormattedString().Trim();
                        }
                        catch (InvalidOperationException)
                        {
                            value = cell.CachedValue.ToString(CultureInfo.GetCultureInfo("fr-FR"))?.Trim() ?? string.Empty;
                        }
                    }

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
        catch (IOException ex)
        {
            if (logWarning)
            {
                _logger.LogWarning(ex, "Fichier Excel verrouillé ou inaccessible : {FilePath}", filePath);
            }
            return Task.FromResult((new List<string>(), new List<List<string>>()));
        }
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

    private sealed record DailyCloudColumnMap(
        int Date,
        int Produced,
        int Consumed,
        int FromGrid,
        int ToGrid,
        int Charged,
        int Discharged,
        int SocMax,
        int SocMin);

    private sealed record MonthlyAtmoceStat(int Year, int Month, double ProducedKwh, double ConsumedKwh, double FromGridKwh);

    private sealed record SunshineColumns(
        int ProducedKwh,
        int ProducedIndex,
        int ConsumedKwh,
        int FromGridKwh,
        int CoveragePercent,
        int GridPercent);

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

        var trimmed = input.Trim();

        if (DateOnly.TryParseExact(trimmed,
            ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy", "yyyy/MM/dd"],
            CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.AllowWhiteSpaces, out var frDateTime)
            || DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out frDateTime))
        {
            date = DateOnly.FromDateTime(frDateTime);
            return true;
        }

        var match = Regex.Match(trimmed, @"(\d{1,2}[\/\-.]\d{1,2}[\/\-.]\d{2,4}|\d{4}[\/\-.]\d{1,2}[\/\-.]\d{1,2})");
        if (match.Success)
        {
            var extracted = match.Value.Trim();
            if (!string.Equals(extracted, trimmed, StringComparison.Ordinal))
            {
                return TryParseDate(extracted, out date);
            }
        }

        return false;
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

        var normalized = value
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace("kWh", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("%", string.Empty)
            .Replace("SOC", string.Empty, StringComparison.OrdinalIgnoreCase);

        normalized = Regex.Replace(normalized, @"[^0-9,\.\-]", string.Empty);

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

