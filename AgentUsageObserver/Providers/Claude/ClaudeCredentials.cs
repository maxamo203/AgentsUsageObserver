using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentUsageObserver.Providers.Claude;

/// <summary>
/// Token OAuth de Claude Code leído desde ~/.claude/.credentials.json.
/// Estructura verificada en el sistema real:
/// { "claudeAiOauth": { "accessToken", "refreshToken", "expiresAt" (epoch ms), ... } }
/// </summary>
public sealed class ClaudeCredentials
{
    public required string AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? SubscriptionType { get; init; }

    public bool IsExpired => ExpiresAt is { } e && e <= DateTimeOffset.UtcNow.AddMinutes(1);

    /// <summary>Ruta del archivo de credenciales (respeta $CLAUDE_CONFIG_DIR).</summary>
    public static string CredentialsPath
    {
        get
        {
            var configDir = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
            if (string.IsNullOrWhiteSpace(configDir))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                configDir = Path.Combine(home, ".claude");
            }
            return Path.Combine(configDir, ".credentials.json");
        }
    }

    /// <summary>
    /// Carga las credenciales. Devuelve null si el archivo no existe o no es parseable
    /// (interpretado como "no autenticado").
    /// </summary>
    public static ClaudeCredentials? Load()
    {
        var path = CredentialsPath;
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            var root = JsonSerializer.Deserialize<CredentialsFile>(json);
            var oauth = root?.ClaudeAiOauth;
            if (oauth is null || string.IsNullOrWhiteSpace(oauth.AccessToken))
                return null;

            return new ClaudeCredentials
            {
                AccessToken = oauth.AccessToken!,
                RefreshToken = oauth.RefreshToken,
                ExpiresAt = oauth.ExpiresAt is > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(oauth.ExpiresAt.Value)
                    : null,
                SubscriptionType = oauth.SubscriptionType
            };
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // --- DTOs de deserialización ---

    private sealed class CredentialsFile
    {
        [JsonPropertyName("claudeAiOauth")]
        public OauthBlock? ClaudeAiOauth { get; set; }
    }

    private sealed class OauthBlock
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expiresAt")]
        public long? ExpiresAt { get; set; }

        [JsonPropertyName("subscriptionType")]
        public string? SubscriptionType { get; set; }
    }
}
