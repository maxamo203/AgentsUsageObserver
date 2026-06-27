using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AgentUsageObserver.Providers.Claude;

/// <summary>
/// Renueva un access token vencido usando el refresh token, contra el endpoint OAuth
/// público de Claude Code. Es un best-effort: si falla, el provider cae en el camino
/// de "re-leer el archivo de credenciales" (que el propio CLI de Claude reescribe).
/// </summary>
public sealed class ClaudeTokenRefresher
{
    // client_id público de Claude Code CLI.
    private const string ClientId = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
    private const string TokenEndpoint = "https://platform.claude.com/v1/oauth/token";

    private readonly HttpClient _http;

    public ClaudeTokenRefresher(HttpClient http) => _http = http;

    /// <summary>
    /// Intenta obtener un nuevo access token. Devuelve null si no es posible
    /// (sin refresh token, error de red, refresh revocado, etc.).
    /// </summary>
    public async Task<RefreshResult?> TryRefreshAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        try
        {
            var body = new RefreshRequest
            {
                GrantType = "refresh_token",
                RefreshToken = refreshToken,
                ClientId = ClientId
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.TryAddWithoutValidation("User-Agent", "claude-cli/1.0");

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return null;

            var parsed = await resp.Content
                .ReadFromJsonAsync<RefreshResponse>(cancellationToken: ct)
                .ConfigureAwait(false);

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.AccessToken))
                return null;

            DateTimeOffset? expiresAt = parsed.ExpiresIn is > 0
                ? DateTimeOffset.UtcNow.AddSeconds(parsed.ExpiresIn.Value)
                : null;

            return new RefreshResult(parsed.AccessToken!, parsed.RefreshToken ?? refreshToken, expiresAt);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return null;
        }
    }

    public sealed record RefreshResult(string AccessToken, string RefreshToken, DateTimeOffset? ExpiresAt);

    private sealed class RefreshRequest
    {
        [JsonPropertyName("grant_type")]
        public required string GrantType { get; init; }

        [JsonPropertyName("refresh_token")]
        public required string RefreshToken { get; init; }

        [JsonPropertyName("client_id")]
        public required string ClientId { get; init; }
    }

    private sealed class RefreshResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }
}
