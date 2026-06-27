using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AgentUsageObserver.Models;
using AgentUsageObserver.Services.Localization;

namespace AgentUsageObserver.Providers.Claude;

/// <summary>
/// Proveedor de uso de Claude. Llama al endpoint OAuth de Anthropic (verificado):
///   GET https://api.anthropic.com/api/oauth/usage
/// y mapea five_hour / seven_day a <see cref="UsageSnapshot"/>.
/// </summary>
public sealed class ClaudeUsageProvider : IUsageProvider
{
    private const string UsageEndpoint = "https://api.anthropic.com/api/oauth/usage";
    private const string AnthropicBeta = "oauth-2025-04-20";

    // Tras un 429 sin Retry-After explícito, esperamos al menos esto antes de reintentar.
    private static readonly TimeSpan DefaultBackoff = TimeSpan.FromMinutes(5);

    private readonly HttpClient _http;
    private readonly ClaudeTokenRefresher _refresher;
    private readonly Func<Settings> _settings;

    // Estado para respetar el rate limit entre llamadas.
    private DateTimeOffset _cooldownUntil = DateTimeOffset.MinValue;
    private IReadOnlyList<UsageWindow> _lastWindows = Array.Empty<UsageWindow>();

    public string Id => "claude";
    public string Name => "Claude";

    public ClaudeUsageProvider(HttpClient http, ClaudeTokenRefresher refresher, Func<Settings> settings)
    {
        _http = http;
        _refresher = refresher;
        _settings = settings;
    }

    public async Task<UsageSnapshot> GetUsageAsync(CancellationToken ct)
    {
        // Si el servidor nos limitó, no consultamos hasta que pase el cooldown:
        // devolvemos los últimos datos buenos con aviso, sin gastar otra request.
        var remaining = _cooldownUntil - DateTimeOffset.UtcNow;
        if (remaining > TimeSpan.Zero)
            return UsageSnapshot.RateLimited(Id, Name,
                Loc.T(Str.ProviderRateLimitRetry, FormatWait(remaining)), _lastWindows);

        var creds = ClaudeCredentials.Load();
        if (creds is null)
            return UsageSnapshot.NotAuthenticated(Id, Name, Loc.T(Str.ProviderSignInClaudeCode));

        // Si está vencido, intentamos renovar; si no se puede, igual probamos el token
        // existente (puede que el CLI ya lo haya rotado) y dejamos que un 401 lo confirme.
        string accessToken = creds.AccessToken;
        if (creds.IsExpired)
        {
            var refreshed = await _refresher.TryRefreshAsync(creds.RefreshToken, ct).ConfigureAwait(false);
            if (refreshed is not null)
                accessToken = refreshed.AccessToken;
        }

        var result = await FetchAsync(accessToken, ct).ConfigureAwait(false);

        // 401 con token posiblemente viejo: un intento de refresh y reintento.
        if (result.Status == HttpStatusCode.Unauthorized && creds.RefreshToken is not null)
        {
            var refreshed = await _refresher.TryRefreshAsync(creds.RefreshToken, ct).ConfigureAwait(false);
            if (refreshed is not null)
                result = await FetchAsync(refreshed.AccessToken, ct).ConfigureAwait(false);
        }

        // 429: activamos cooldown y conservamos los últimos datos buenos.
        if (result.Status == HttpStatusCode.TooManyRequests)
        {
            // Retry-After 0 / ausente / muy corto → usamos el backoff por defecto (5 min)
            // para no seguir martillando al servidor.
            var wait = result.RetryAfter is { } ra && ra >= TimeSpan.FromSeconds(5)
                ? ra
                : DefaultBackoff;
            _cooldownUntil = DateTimeOffset.UtcNow + wait;
            return UsageSnapshot.RateLimited(Id, Name,
                Loc.T(Str.ProviderRateLimitRetry, FormatWait(wait)), _lastWindows);
        }

        if (result.Status == HttpStatusCode.Unauthorized)
            return UsageSnapshot.NotAuthenticated(Id, Name, Loc.T(Str.ProviderSessionExpired));

        if (result.Status != HttpStatusCode.OK || result.Payload is null)
            return UsageSnapshot.Error(Id, Name, result.Message ?? Loc.T(Str.ProviderUsageQueryError, (int)result.Status));

        var snapshot = Map(result.Payload);
        _lastWindows = snapshot.Windows; // guardamos para mostrar durante un futuro cooldown
        return snapshot;
    }

    private static string FormatWait(TimeSpan t)
    {
        if (t.TotalSeconds < 60) return Loc.T(Str.WaitSeconds, Math.Max(1, (int)t.TotalSeconds));
        if (t.TotalMinutes < 60) return Loc.T(Str.WaitMinutes, (int)t.TotalMinutes);
        return Loc.T(Str.WaitHours, (int)t.TotalHours);
    }

    private async Task<FetchResult> FetchAsync(string accessToken, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, UsageEndpoint);
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
            req.Headers.TryAddWithoutValidation("anthropic-beta", AnthropicBeta);
            req.Headers.TryAddWithoutValidation("User-Agent", "claude-cli/1.0");
            req.Headers.TryAddWithoutValidation("Accept", "application/json");

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);

            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
                return new FetchResult(resp.StatusCode, null, "Demasiadas consultas (429).", ReadRetryAfter(resp));

            if (resp.StatusCode != HttpStatusCode.OK)
                return new FetchResult(resp.StatusCode, null, $"HTTP {(int)resp.StatusCode}", null);

            var payload = await resp.Content
                .ReadFromJsonAsync<UsageResponse>(cancellationToken: ct)
                .ConfigureAwait(false);
            return new FetchResult(HttpStatusCode.OK, payload, null, null);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new FetchResult(HttpStatusCode.ServiceUnavailable, null, Loc.T(Str.ProviderNoConnection), null);
        }
    }

    /// <summary>Lee el header Retry-After (segundos o fecha HTTP) que acompaña a un 429.</summary>
    private static TimeSpan? ReadRetryAfter(HttpResponseMessage resp)
    {
        var ra = resp.Headers.RetryAfter;
        if (ra is null) return null;
        if (ra.Delta is { } delta) return delta;
        if (ra.Date is { } date)
        {
            var diff = date - DateTimeOffset.UtcNow;
            return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
        }
        return null;
    }

    private readonly record struct FetchResult(
        HttpStatusCode Status, UsageResponse? Payload, string? Message, TimeSpan? RetryAfter);

    private UsageSnapshot Map(UsageResponse r)
    {
        var s = _settings();
        var windows = new List<UsageWindow>();

        if (r.FiveHour is { } fh)
            windows.Add(BuildWindow("five_hour", Loc.T(Str.LabelFiveHour), fh, r, s));
        if (r.SevenDay is { } sd)
            windows.Add(BuildWindow("seven_day", Loc.T(Str.LabelWeek), sd, r, s));

        return new UsageSnapshot(Id, Name, UsageStatus.Ok, windows, DateTimeOffset.UtcNow);
    }

    private UsageWindow BuildWindow(string key, string label, UsageBucket bucket, UsageResponse r, Settings s)
    {
        double percent = bucket.Utilization ?? 0;
        DateTimeOffset? resetsAt = ParseDate(bucket.ResetsAt);

        UsageSeverity severity = s.UseServerSeverity
            ? MapServerSeverity(r, key)
            : SeverityFromThresholds(percent, s);

        return new UsageWindow(key, label, percent, severity, resetsAt);
    }

    /// <summary>Severidad derivada de los umbrales locales del usuario.</summary>
    private static UsageSeverity SeverityFromThresholds(double percent, Settings s)
    {
        if (percent >= s.CriticalThreshold) return UsageSeverity.Critical;
        if (percent >= s.WarningThreshold) return UsageSeverity.Warning;
        return UsageSeverity.Normal;
    }

    /// <summary>Severidad que entrega el servidor en limits[].severity, si está.</summary>
    private static UsageSeverity MapServerSeverity(UsageResponse r, string key)
    {
        string? group = key == "five_hour" ? "session" : "weekly";
        if (r.Limits is not null)
        {
            foreach (var l in r.Limits)
            {
                if (string.Equals(l.Group, group, StringComparison.OrdinalIgnoreCase))
                    return ParseSeverity(l.Severity);
            }
        }
        return UsageSeverity.Unknown;
    }

    private static UsageSeverity ParseSeverity(string? s) => s?.ToLowerInvariant() switch
    {
        "normal" => UsageSeverity.Normal,
        "warning" or "warn" => UsageSeverity.Warning,
        "critical" or "exceeded" or "blocked" => UsageSeverity.Critical,
        _ => UsageSeverity.Unknown
    };

    private static DateTimeOffset? ParseDate(string? iso) =>
        DateTimeOffset.TryParse(iso, out var d) ? d.ToUniversalTime() : null;

    // --- DTOs del endpoint (campos verificados contra la respuesta real) ---

    private sealed class UsageResponse
    {
        [JsonPropertyName("five_hour")]
        public UsageBucket? FiveHour { get; set; }

        [JsonPropertyName("seven_day")]
        public UsageBucket? SevenDay { get; set; }

        [JsonPropertyName("limits")]
        public List<LimitEntry>? Limits { get; set; }
    }

    private sealed class UsageBucket
    {
        [JsonPropertyName("utilization")]
        public double? Utilization { get; set; }

        [JsonPropertyName("resets_at")]
        public string? ResetsAt { get; set; }
    }

    private sealed class LimitEntry
    {
        [JsonPropertyName("group")]
        public string? Group { get; set; }

        [JsonPropertyName("percent")]
        public double? Percent { get; set; }

        [JsonPropertyName("severity")]
        public string? Severity { get; set; }
    }
}
