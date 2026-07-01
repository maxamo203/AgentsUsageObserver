namespace AgentUsageObserver.Models;

/// <summary>
/// Configuración persistida de la aplicación.
/// Se guarda como JSON en %APPDATA%\AgentUsageObserver\settings.json.
/// </summary>
public sealed class Settings
{
    /// <summary>Intervalo de sondeo en segundos (default 300, rango 60..900).</summary>
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>Umbral (%) a partir del cual el icono pasa a amarillo.</summary>
    public int WarningThreshold { get; set; } = 70;

    /// <summary>Umbral (%) a partir del cual el icono pasa a rojo.</summary>
    public int CriticalThreshold { get; set; } = 90;

    /// <summary>Iniciar con Windows (clave HKCU\...\Run).</summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// Si true, el color del icono usa la severidad que entrega el servidor;
    /// si false, usa los umbrales locales (Warning/Critical).
    /// </summary>
    public bool UseServerSeverity { get; set; } = false;

    /// <summary>Buscar actualizaciones al iniciar (consulta GitHub Releases).</summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>
    /// Versión que el usuario pidió omitir (ej. "0.2.0"). Si la última release
    /// coincide con este valor, no se vuelve a avisar por ella.
    /// </summary>
    public string? IgnoredUpdateVersion { get; set; }

    public const int MinIntervalSeconds = 60;
    public const int MaxIntervalSeconds = 900;

    public Settings Clone() => (Settings)MemberwiseClone();

    public void Normalize()
    {
        if (PollingIntervalSeconds < MinIntervalSeconds) PollingIntervalSeconds = MinIntervalSeconds;
        if (PollingIntervalSeconds > MaxIntervalSeconds) PollingIntervalSeconds = MaxIntervalSeconds;
        if (WarningThreshold < 1) WarningThreshold = 1;
        if (CriticalThreshold > 100) CriticalThreshold = 100;
        if (CriticalThreshold <= WarningThreshold) CriticalThreshold = System.Math.Min(100, WarningThreshold + 1);
    }
}
