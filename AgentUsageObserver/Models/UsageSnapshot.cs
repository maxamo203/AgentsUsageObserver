using System;
using System.Collections.Generic;
using System.Linq;

namespace AgentUsageObserver.Models;

/// <summary>
/// Estado de un snapshot de uso devuelto por un proveedor.
/// </summary>
public enum UsageStatus
{
    Ok,              // datos frescos válidos
    NotAuthenticated,// no hay credenciales / sesión válida
    RateLimited,     // 429: el servidor nos pidió bajar el ritmo
    Error            // fallo de red u otro; puede traer datos antiguos en Windows
}

/// <summary>
/// Resultado de consultar a un <see cref="Providers.IUsageProvider"/>.
/// Modelo común a todos los agentes (Claude hoy, otros después).
/// </summary>
public sealed record UsageSnapshot(
    string ProviderId,           // "claude"
    string ProviderName,         // "Claude"
    UsageStatus Status,
    IReadOnlyList<UsageWindow> Windows,
    DateTimeOffset RetrievedAt,
    string? Message = null)      // texto de error / estado para la UI
{
    public static UsageSnapshot NotAuthenticated(string providerId, string providerName, string message) =>
        new(providerId, providerName, UsageStatus.NotAuthenticated,
            Array.Empty<UsageWindow>(), DateTimeOffset.UtcNow, message);

    public static UsageSnapshot Error(string providerId, string providerName, string message) =>
        new(providerId, providerName, UsageStatus.Error,
            Array.Empty<UsageWindow>(), DateTimeOffset.UtcNow, message);

    public static UsageSnapshot RateLimited(string providerId, string providerName,
        string message, IReadOnlyList<UsageWindow> keepWindows) =>
        new(providerId, providerName, UsageStatus.RateLimited,
            keepWindows, DateTimeOffset.UtcNow, message);

    /// <summary>Ventana cuya <paramref name="key"/> coincide, o null.</summary>
    public UsageWindow? Window(string key) =>
        Windows.FirstOrDefault(w => w.Key == key);

    /// <summary>La ventana de 5 horas (la que se muestra en el icono).</summary>
    public UsageWindow? FiveHour => Window("five_hour");

    /// <summary>La ventana semanal.</summary>
    public UsageWindow? SevenDay => Window("seven_day");
}
