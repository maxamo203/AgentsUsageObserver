using System;

namespace AgentUsageObserver.Models;

/// <summary>
/// Severidad de una ventana de uso. El servidor de Anthropic ya entrega "normal", etc.;
/// también la derivamos localmente desde umbrales configurables cuando hace falta.
/// </summary>
public enum UsageSeverity
{
    Normal,
    Warning,
    Critical,
    Unknown
}

/// <summary>
/// Una ventana de consumo concreta (p.ej. la de 5 horas o la semanal).
/// </summary>
public sealed record UsageWindow(
    string Key,            // identificador estable, p.ej. "five_hour", "seven_day"
    string Label,          // texto para UI, p.ej. "Últimas 5h", "Semana"
    double Percent,        // 0..100
    UsageSeverity Severity,
    DateTimeOffset? ResetsAt)
{
    /// <summary>Tiempo restante hasta el reset, o null si no se conoce.</summary>
    public TimeSpan? TimeUntilReset =>
        ResetsAt is { } r ? r - DateTimeOffset.UtcNow : null;
}
