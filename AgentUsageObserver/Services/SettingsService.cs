using System;
using System.IO;
using System.Text.Json;
using AgentUsageObserver.Models;
using Microsoft.Win32;

namespace AgentUsageObserver.Services;

/// <summary>
/// Carga/guarda la configuración en %APPDATA%\AgentUsageObserver\settings.json y
/// gestiona el arranque con Windows (HKCU\...\Run).
/// </summary>
public sealed class SettingsService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "AgentUsageObserver";

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly string _path;

    public Settings Current { get; private set; } = new();

    /// <summary>Se dispara tras guardar, con la nueva configuración.</summary>
    public event Action<Settings>? Changed;

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AgentUsageObserver");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var loaded = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path));
                if (loaded is not null)
                {
                    loaded.Normalize();
                    Current = loaded;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            // settings corruptos → defaults
            Current = new Settings();
        }
    }

    public void Save(Settings settings)
    {
        settings.Normalize();
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOpts));
        Current = settings;
        ApplyAutostart(settings.StartWithWindows);
        Changed?.Invoke(settings);
    }

    /// <summary>Escribe/borra la clave de arranque con Windows.</summary>
    private static void ApplyAutostart(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key is null) return;

            if (enabled)
            {
                string exe = Environment.ProcessPath ?? "";
                if (!string.IsNullOrEmpty(exe))
                    key.SetValue(RunValueName, $"\"{exe}\"");
            }
            else if (key.GetValue(RunValueName) is not null)
            {
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            // sin permisos para escribir el registro: se ignora silenciosamente
        }
    }
}
