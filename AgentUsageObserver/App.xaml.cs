using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using AgentUsageObserver.Models;
using AgentUsageObserver.Providers;
using AgentUsageObserver.Providers.Claude;
using AgentUsageObserver.Services;
using AgentUsageObserver.Services.Localization;
using AgentUsageObserver.Tray;

namespace AgentUsageObserver;

/// <summary>
/// Composición de la aplicación. No tiene ventana inicial: vive en el system tray.
/// </summary>
public partial class App : Application
{
    private HttpClient? _http;
    private SettingsService? _settingsService;
    private PollingService? _polling;
    private TrayController? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Red de seguridad: una excepción en una ventana no debe tumbar toda la app del tray.
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                Loc.T(Str.UnexpectedError, args.Exception.Message),
                Loc.T(Str.AppName),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };

        // Settings.
        _settingsService = new SettingsService();
        _settingsService.Load();

        // HTTP compartido.
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        // Proveedores (hoy solo Claude; extensible vía IUsageProvider).
        var refresher = new ClaudeTokenRefresher(_http);
        var providers = new List<IUsageProvider>
        {
            new ClaudeUsageProvider(_http, refresher, () => _settingsService.Current)
        };

        // Sondeo periódico.
        _polling = new PollingService(providers, () => _settingsService.Current);

        // Tray (crea el icono y maneja la interacción).
        _tray = new TrayController(_settingsService, _polling);

        // Si cambia el intervalo, forzamos un sondeo inmediato para reflejarlo.
        _settingsService.Changed += _ => _polling!.PollNow();

        _polling.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _polling?.Dispose();
        _http?.Dispose();
        base.OnExit(e);
    }
}
