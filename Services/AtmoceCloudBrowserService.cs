using Microsoft.Playwright;

namespace SolarApp.Services;

public class AtmoceCloudBrowserService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AtmoceCloudBrowserService> _logger;

    public AtmoceCloudBrowserService(IConfiguration configuration, ILogger<AtmoceCloudBrowserService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public sealed record TableExtractionResult(
        bool Success,
        string Message,
        IReadOnlyList<string> Headers,
        IReadOnlyList<IReadOnlyList<string>> Rows,
        string? CurrentUrl);

    public async Task<TableExtractionResult> GetDailyProductionTableAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AtmoceCloud:BaseUrl"] ?? "https://www.atmocecloud.com";
        var loginUrl = _configuration["AtmoceCloud:LoginUrl"] ?? $"{baseUrl}/login";
        var overviewUrl = _configuration["AtmoceCloud:OverviewUrl"] ?? $"{baseUrl}/energy/child/energy/plant/overview";
        var tableUrl = _configuration["AtmoceCloud:TableUrl"] ?? $"{baseUrl}/energy/child/energy/plant/table";
        var username = _configuration["AtmoceCloud:Username"];
        var password = _configuration["AtmoceCloud:Password"];

        void Report(string message) => progress?.Report(message);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new TableExtractionResult(false, "Les identifiants Atmoce Cloud sont manquants.", [], [], null);
        }

        try
        {
            Report("Ouverture du navigateur Atmoce Cloud...");
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Channel = "msedge"
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "fr-FR",
                ViewportSize = new ViewportSize { Width = 1600, Height = 1200 }
            });

            var page = await context.NewPageAsync();

            Report("Chargement de la page de relevé quotidien...");
            await page.GotoAsync(tableUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 120_000
            });

            if (await IsLoginPageAsync(page, cancellationToken))
            {
                Report("Connexion à Atmoce Cloud...");
                await LoginAsync(page, loginUrl, overviewUrl, tableUrl, username, password, cancellationToken, progress);
                await EnsureOverviewPageAsync(page, overviewUrl, cancellationToken, progress);

                Report("Retour vers la page de relevé quotidien...");
                await page.GotoAsync(tableUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 120_000
                });
            }
            else
            {
                Report("Session Atmoce Cloud déjà active, accès direct au relevé quotidien.");
            }

            Report("Attente du tableau de données (Ant Design)...");
            ILocator table;
            try
            {
                table = await WaitForDataTableAsync(page, tableUrl, progress, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                Report("Session Atmoce Cloud expirée pendant l’attente du tableau, nouvelle tentative de connexion...");
                await LoginAsync(page, loginUrl, overviewUrl, tableUrl, username, password, cancellationToken, progress);
                await EnsureOverviewPageAsync(page, overviewUrl, cancellationToken, progress);

                Report("Retour vers la page de relevé quotidien après reconnexion...");
                await page.GotoAsync(tableUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 120_000
                });

                table = await WaitForDataTableAsync(page, tableUrl, progress, cancellationToken);
            }

            Report("Lecture des lignes du tableau...");
            var extraction = await ExtractTableAsync(page, progress, cancellationToken);
            Report($"Lecture terminée : {extraction.Rows.Count} ligne(s) récupérée(s).");
            return new TableExtractionResult(true, "Données récupérées depuis Atmoce Cloud.", extraction.Headers, extraction.Rows, page.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'extraction Atmoce Cloud.");
            return new TableExtractionResult(false, $"Erreur lors de la récupération Atmoce Cloud : {ex.Message}", [], [], null);
        }
    }

    private async Task LoginAsync(IPage page, string loginUrl, string overviewUrl, string tableUrl, string username, string password, CancellationToken cancellationToken, IProgress<string>? progress = null)
    {
        _logger.LogInformation("Démarrage de l'authentification Atmoce Cloud. Url courante : {Url}", page.Url);

        if (!page.Url.Contains("login", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync(loginUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 120_000
            });
        }

        _logger.LogDebug("Page de connexion chargée. Url : {Url}", page.Url);

        var userField = await FindFirstVisibleLocatorAsync(page, new[]
        {
            "input[type='email']",
            "input[name='username']",
            "input[name='account']",
            "input[name='userName']",
            "input[type='text']"
        }, cancellationToken);

        var passwordField = await FindFirstVisibleLocatorAsync(page, new[]
        {
            "input[type='password']",
            "input[name='password']"
        }, cancellationToken);

        _logger.LogDebug("Champs de connexion détectés. Utilisateur : {UserFieldFound}, Mot de passe : {PasswordFieldFound}", userField is not null, passwordField is not null);

        if (userField is null || passwordField is null)
        {
            throw new InvalidOperationException("Impossible de trouver les champs de connexion Atmoce Cloud.");
        }

        await userField.FillAsync(username);
        await passwordField.FillAsync(password);
        _logger.LogDebug("Identifiants saisis pour Atmoce Cloud.");

        var consentCheckbox = await FindFirstVisibleLocatorAsync(page, new[]
        {
            "input.ant-checkbox-input[type='checkbox']",
            "input[type='checkbox']",
            "label:has(input[type='checkbox'])",
            "div.ant-checkbox-wrapper input[type='checkbox']"
        }, cancellationToken);

        if (consentCheckbox is null)
        {
            throw new InvalidOperationException("Impossible de trouver la case de consentement Atmoce Cloud.");
        }

        var consentWasChecked = await consentCheckbox.IsCheckedAsync();
        _logger.LogDebug("Case de consentement détectée. Déjà cochée : {ConsentChecked}", consentWasChecked);

        if (!consentWasChecked)
        {
            await consentCheckbox.CheckAsync();
            _logger.LogDebug("Case de consentement cochée.");
        }

        var submitButton = await FindFirstVisibleLocatorAsync(page, new[]
        {
            "button[type='submit']",
            "input[type='submit']",
            "button:has-text('Login')",
            "button:has-text('Log in')",
            "button:has-text('Connexion')",
            "button:has-text('Se connecter')"
        }, cancellationToken);

        if (submitButton is null)
        {
            throw new InvalidOperationException("Impossible de trouver le bouton de connexion Atmoce Cloud.");
        }

        _logger.LogDebug("Bouton de connexion détecté. Soumission du formulaire...");
        progress?.Report("Soumission du formulaire de connexion...");
        await submitButton.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 120_000 });
        _logger.LogDebug("Retour après soumission. Url courante : {Url}", page.Url);

        var loginDeadline = DateTime.UtcNow.AddSeconds(30);
        var nextDiagnosticReport = DateTime.UtcNow;
        DateTime? nonLoginDetectedAt = null;
        while (DateTime.UtcNow < loginDeadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isLoginPage = await IsLoginPageAsync(page, cancellationToken);
            var currentUrl = page.Url;
            var loginError = await TryGetLoginErrorAsync(page, cancellationToken);

            if (DateTime.UtcNow >= nextDiagnosticReport)
            {
                _logger.LogDebug("Attente post-authentification Atmoce Cloud. Url : {Url}, LoginVisible : {IsLoginPage}, ErreurVisible : {LoginError}", currentUrl, isLoginPage, loginError ?? "aucune");
                nextDiagnosticReport = DateTime.UtcNow.AddSeconds(2);
            }

            var isExpectedAuthenticatedUrl = currentUrl.StartsWith(overviewUrl, StringComparison.OrdinalIgnoreCase)
                || currentUrl.StartsWith(tableUrl, StringComparison.OrdinalIgnoreCase);

            if (!isLoginPage && isExpectedAuthenticatedUrl)
            {
                nonLoginDetectedAt ??= DateTime.UtcNow;

                if (DateTime.UtcNow - nonLoginDetectedAt >= TimeSpan.FromSeconds(2))
                {
                    progress?.Report("Connexion Atmoce Cloud validée.");
                    _logger.LogInformation("Connexion Atmoce Cloud validée après stabilisation. Url finale : {Url}", currentUrl);
                    return;
                }
            }
            else
            {
                nonLoginDetectedAt = null;
            }

            await page.WaitForTimeoutAsync(500);
        }

        var finalLoginError = await TryGetLoginErrorAsync(page, cancellationToken);
        throw new InvalidOperationException($"L'authentification Atmoce Cloud n'a pas pu être confirmée après soumission. Url courante : {page.Url}. Message détecté : {finalLoginError ?? "aucun"}");
    }

    private static async Task<bool> IsLoginPageAsync(IPage page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (page.Url.Contains("login", StringComparison.OrdinalIgnoreCase)
            || page.Url.Contains("signin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var passwordInputs = page.Locator("input[type='password']");
        if (await passwordInputs.CountAsync() > 0)
        {
            return true;
        }

        var submitButtons = page.Locator("button[type='submit'], input[type='submit'], button:has-text('Se connecter'), button:has-text('Connexion'), button:has-text('Login'), button:has-text('Log in')");
        return await submitButtons.CountAsync() > 0;
    }

    private static async Task<string?> TryGetLoginErrorAsync(IPage page, CancellationToken cancellationToken)
    {
        var selectors = new[]
        {
            ".ant-form-item-explain-error",
            ".ant-alert-message",
            ".ant-alert-description",
            ".ant-message-notice-content",
            ".ant-notification-notice-message",
            ".ant-notification-notice-description",
            ".error",
            ".error-message"
        };

        foreach (var selector in selectors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var locator = page.Locator(selector).First;
            if (await locator.CountAsync() == 0 || !await locator.IsVisibleAsync())
            {
                continue;
            }

            var text = (await locator.InnerTextAsync()).Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private async Task EnsureOverviewPageAsync(IPage page, string overviewUrl, CancellationToken cancellationToken, IProgress<string>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (page.Url.Contains("overview", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (page.Url.Contains("login", StringComparison.OrdinalIgnoreCase) || page.Url.Contains("signin", StringComparison.OrdinalIgnoreCase))
        {
            progress?.Report("La page de login est encore affichée, ouverture de la page overview...");
        }
        else
        {
            progress?.Report("Navigation vers la première page de relevé quotidien...");
        }

        await page.GotoAsync(overviewUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 120_000
        });
    }

    private static async Task<ILocator?> FindFirstVisibleLocatorAsync(IPage page, IEnumerable<string> selectors, CancellationToken cancellationToken)
    {
        foreach (var selector in selectors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var locator = page.Locator(selector).First;
            if (await locator.CountAsync() == 0)
            {
                continue;
            }

            if (await locator.IsVisibleAsync())
            {
                return locator;
            }
        }

        return null;
    }

    private async Task<ILocator> WaitForDataTableAsync(IPage page, string tableUrl, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (!page.Url.StartsWith(tableUrl, StringComparison.OrdinalIgnoreCase))
        {
            progress?.Report("Navigation vers la page de relevé quotidien...");
            await page.GotoAsync(tableUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 120_000
            });
        }

        var candidates = new[]
        {
            ".ant-table-body tr.ant-table-row-level-0",
            ".ant-table-body tr.ant-table-row",
            "xpath=//*[@id='root']//tr[contains(@class,'ant-table-row')]",
            ".ant-table-container"
        };

        var deadline = DateTime.UtcNow.AddMinutes(2);
        var lastReport = DateTime.MinValue;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentUrl = page.Url;
            if (currentUrl.Contains("login", StringComparison.OrdinalIgnoreCase) || currentUrl.Contains("signin", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"La session Atmoce Cloud n'est pas authentifiée : redirection vers la page de connexion ({currentUrl}).");
            }

            if (!currentUrl.StartsWith(tableUrl, StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.UtcNow - lastReport > TimeSpan.FromSeconds(8))
                {
                    progress?.Report("En attente de la page de relevé quotidien...");
                    lastReport = DateTime.UtcNow;
                }

                await page.WaitForTimeoutAsync(1000);
                continue;
            }

            if (DateTime.UtcNow - lastReport > TimeSpan.FromSeconds(8))
            {
                progress?.Report("Recherche du tableau de données en cours...");
                lastReport = DateTime.UtcNow;
            }

            for (var index = 0; index < candidates.Length; index++)
            {
                var selector = candidates[index];
                var locator = page.Locator(selector);
                try
                {
                    var count = await locator.CountAsync();
                    if (count == 0)
                    {
                        continue;
                    }

                    return locator.First;
                }
                catch (PlaywrightException ex)
                {
                    _logger.LogDebug(ex, "Erreur Playwright sur le sélecteur {Selector}", selector);
                }
            }

            await page.WaitForTimeoutAsync(1000);
        }

        throw new TimeoutException($"Impossible de localiser les lignes du tableau Atmoce Cloud après attente. Sélecteurs testés : {string.Join(", ", candidates)}");
    }

    private static async Task<(List<string> Headers, List<List<string>> Rows)> ExtractTableAsync(IPage page, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var headers = new List<string>();
        var headerLocators = page.Locator(".ant-table-header thead th.ant-table-cell, .ant-table-container thead th.ant-table-cell, .ant-table thead th.ant-table-cell");
        var headerCount = await headerLocators.CountAsync();
        if (headerCount == 0)
        {
            headerLocators = page.Locator(".ant-table-header thead th, .ant-table-container thead th, .ant-table thead th");
            headerCount = await headerLocators.CountAsync();
        }

        if (headerCount > 0)
        {
            var headerTexts = await headerLocators.AllInnerTextsAsync();
            headers.AddRange(headerTexts.Select(text => text.Trim()).Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        var rows = new List<List<string>>();
        var collectedDates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pageIndex = 1;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Lecture de la page {pageIndex} du tableau...");

            var rowLocators = page.Locator(".ant-table-body tbody tr.ant-table-row-level-0, .ant-table-body tbody tr.ant-table-row, .ant-table-container tbody tr.ant-table-row-level-0, .ant-table-container tbody tr.ant-table-row, .ant-table-tbody tr.ant-table-row-level-0, .ant-table-tbody tr.ant-table-row");
            var rowCount = await rowLocators.CountAsync();
            if (rowCount == 0)
            {
                rowLocators = page.Locator("tr.ant-table-row-level-0, tr.ant-table-row, .ant-table-tbody tr");
                rowCount = await rowLocators.CountAsync();
            }

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = rowLocators.Nth(rowIndex);
                var cells = row.Locator("td.ant-table-cell, th.ant-table-cell, td, th");
                var cellTexts = await cells.AllInnerTextsAsync();
                var values = cellTexts
                    .Select(text => text.Trim())
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToList();

                if (values.Count == 0)
                {
                    continue;
                }

                var firstValue = values[0];
                if (!collectedDates.Add(firstValue))
                {
                    continue;
                }

                rows.Add(values);
            }

            var nextButton = page.Locator(".ant-pagination-next").First;
            if (await nextButton.CountAsync() == 0)
            {
                break;
            }

            var isDisabled = await nextButton.GetAttributeAsync("aria-disabled");
            if (string.Equals(isDisabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            await nextButton.ClickAsync();
            await page.WaitForTimeoutAsync(1000);
            pageIndex++;

            if (pageIndex > 20)
            {
                break;
            }
        }

        return (headers, rows);
    }
}
