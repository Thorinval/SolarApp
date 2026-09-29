using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SolarApp.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SolarApp.Services;

/// <summary>
/// Service pour gérer l'authentification et les appels à l'API Atmoce
/// </summary>
public class AtmocApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AtmocApiService> _logger;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;

    public AtmocApiService(HttpClient httpClient, IConfiguration configuration, ILogger<AtmocApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Authentifier et récupérer les tokens
    /// </summary>
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var apiKey = _configuration["Atmoce:ApiKey"];
            var apiSecret = _configuration["Atmoce:ApiSecret"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                _logger.LogError("Configuration Atmoce manquante (BaseUrl, ApiKey ou ApiSecret)");
                return false;
            }

            var url = $"{baseUrl}/auth/auth_token";
            var payload = new
            {
                app_key = apiKey,
                app_secret = apiSecret,
                grant_type = "system"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            _logger.LogInformation("Tentative d'authentification Atmoce...");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<AuthTokenResponse>>(responseContent);

            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                _accessToken = apiResponse.Data.AccessToken;
                _refreshToken = apiResponse.Data.RefreshToken;
                _tokenExpiresAt = DateTime.UtcNow.AddDays(30);

                _logger.LogInformation("Authentification Atmoce réussie");
                return true;
            }

            _logger.LogError($"Erreur d'authentification : {apiResponse?.Reason}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception lors de l'authentification : {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Récupérer la liste des sites
    /// </summary>
    public async Task<List<SiteDto>?> GetSitesAsync(int page = 1)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
                return null;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/sites/getSites?page={page}";

            var response = await GetAsync<List<SiteDto>>(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la récupération des sites : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Récupérer les informations détaillées d'un site
    /// </summary>
    public async Task<SiteDto?> GetSiteAsync(string siteId)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
                return null;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/sites/getSite?siteIds={siteId}";

            var response = await GetAsync<List<SiteDto>>(url);
            return response?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la récupération du site {siteId} : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Récupérer les données actuelles d'un site
    /// </summary>
    public async Task<SiteLastPowerDto?> GetSiteLastPowerAsync(string siteId)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
                return null;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/sites/getSitesLastPower?siteIds={siteId}";

            var response = await GetAsync<List<SiteLastPowerDto>>(url);
            return response?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la récupération des données du site {siteId} : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Récupérer les données énergétiques d'un site
    /// </summary>
    public async Task<List<SiteEnergyDto>?> GetSiteEnergyAsync(string siteId, string dateType = "day", string? beginDate = null, string? endDate = null)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
                return null;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/sites/getSitesEnergy?siteIds={siteId}&dateType={dateType}";

            if (!string.IsNullOrEmpty(beginDate))
                url += $"&beginDate={beginDate}";
            if (!string.IsNullOrEmpty(endDate))
                url += $"&endDate={endDate}";

            var response = await GetAsync<List<SiteEnergyDto>>(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la récupération des données énergétiques du site {siteId} : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Récupérer les appareils d'un site
    /// </summary>
    public async Task<List<DeviceDto>?> GetDevicesBySiteAsync(string siteId)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
                return null;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/device/getDevicesBySite?siteIds={siteId}";

            var response = await GetAsync<List<DeviceDto>>(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la récupération des appareils du site {siteId} : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Récupérer les données actuelles des microinverteurs
    /// </summary>
    public async Task<List<MicroinverterLastDataDto>?> GetMicroinvertersLastDataAsync(string siteId)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
                return null;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/microInverter/getMIsLastData?siteId={siteId}";

            var response = await GetAsync<List<MicroinverterLastDataDto>>(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la récupération des microinverteurs du site {siteId} : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Récupérer les données actuelles des batteries
    /// </summary>
    public async Task<List<BatteryLastDataDto>?> GetBatteriesLastDataAsync(string siteId)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
                return null;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/battery/getBatterysLastData?siteId={siteId}";

            var response = await GetAsync<List<BatteryLastDataDto>>(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la récupération des batteries du site {siteId} : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Récupérer les données actuelles de la passerelle
    /// </summary>
    public async Task<List<GatewayLastDataDto>?> GetGatewaysLastDataAsync(string siteId)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
                return null;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/gateway/getGatewaysLastData?siteId={siteId}";

            var response = await GetAsync<List<GatewayLastDataDto>>(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la récupération de la passerelle du site {siteId} : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Récupérer les alertes actives
    /// </summary>
    public async Task<List<DeviceAlertDto>?> GetAlarmsAsync(string siteId, int page = 1)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync())
                return null;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/device/getAlarmsBySite?siteId={siteId}&page={page}";

            var response = await GetAsync<List<DeviceAlertDto>>(url);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors de la récupération des alertes du site {siteId} : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Récupération générique avec gestion des tokens
    /// </summary>
    private async Task<T?> GetAsync<T>(string url)
    {
        if (string.IsNullOrEmpty(_accessToken))
        {
            _logger.LogError("Token d'accès non disponible");
            return default;
        }

        using (var request = new HttpRequestMessage(HttpMethod.Get, url))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Token expiré, tentative de rafraîchissement...");
                if (await RefreshTokenAsync())
                {
                    return await GetAsync<T>(url);
                }
                return default;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(content);

            if (apiResponse?.Success == true)
            {
                return apiResponse.Data;
            }

            _logger.LogError($"Erreur API : {apiResponse?.Reason}");
            return default;
        }
    }

    /// <summary>
    /// Rafraîchir le token d'accès
    /// </summary>
    private async Task<bool> RefreshTokenAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_refreshToken))
                return false;

            var baseUrl = _configuration["Atmoce:BaseUrl"];
            var url = $"{baseUrl}/auth/auth_token";
            var payload = new
            {
                grant_type = "refresh",
                refresh_token = _refreshToken
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<AuthTokenResponse>>(responseContent);

            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                _accessToken = apiResponse.Data.AccessToken;
                _refreshToken = apiResponse.Data.RefreshToken;
                _tokenExpiresAt = DateTime.UtcNow.AddDays(30);

                _logger.LogInformation("Token rafraîchi avec succès");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erreur lors du rafraîchissement du token : {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Vérifier si authentifié, sinon authentifier
    /// </summary>
    private async Task<bool> EnsureAuthenticatedAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(_refreshToken) && await RefreshTokenAsync())
        {
            return true;
        }

        return await AuthenticateAsync();
    }
}

