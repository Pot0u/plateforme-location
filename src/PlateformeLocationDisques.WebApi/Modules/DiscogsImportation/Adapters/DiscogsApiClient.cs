using System.Net.Http.Headers;
using System.Text.Json;

namespace PlateformeLocationDisques.WebApi.Modules.DiscogsImportation.Adapters;

/// <summary>
/// Real implementation of the Discogs API client.
/// Requires API token for authentication.
/// </summary>
public class DiscogsApiClient : IDiscogsClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiToken;
    private readonly JsonSerializerOptions _jsonOptions;

    public DiscogsApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiToken = configuration["Discogs:ApiToken"]
            ?? throw new InvalidOperationException("Discogs API token is not configured. Please set 'Discogs:ApiToken' in configuration.");

        _httpClient.BaseAddress = new Uri("https://api.discogs.com/");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PlateformeLocationDisques/1.0");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Discogs", $"token={_apiToken}");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public async Task<DiscogsMasterReleaseDto?> GetMasterReleaseAsync(int masterId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"masters/{masterId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Log error or handle specific status codes
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<DiscogsMasterReleaseDto>(content, _jsonOptions);
        }
        catch (HttpRequestException)
        {
            // Log exception
            return null;
        }
    }

    public async Task<DiscogsReleaseDto?> GetReleaseAsync(int releaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"releases/{releaseId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Log error or handle specific status codes
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<DiscogsReleaseDto>(content, _jsonOptions);
        }
        catch (HttpRequestException)
        {
            // Log exception
            return null;
        }
    }
}
