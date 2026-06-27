using System;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using AgentUsageObserver.Models;
using AgentUsageObserver.Services;
using AgentUsageObserver.Services.Localization;
using AgentUsageObserver.UI;
using H.NotifyIcon;

namespace AgentUsageObserver.Tray;

/// <summary>
/// Dueño del icono del tray y de la interacción:
///  - Clic simple  → muestra/oculta el MiniPanel.
///  - Doble clic    → abre la ventana de configuración.
///  - Clic derecho  → menú contextual (Configuración / Salir).
/// Actualiza el icono dibujado y el tooltip con cada snapshot recibido.
/// </summary>
public sealed class TrayController : IDisposable
{
    private readonly TaskbarIcon _icon;
    private readonly SettingsService _settings;
    private readonly PollingService _polling;
    private readonly DispatcherTimer _clickTimer;

    private MiniPanel? _miniPanel;
    private MainWindow? _mainWindow;
    private Icon? _currentIcon;
    private UsageSnapshot? _lastSnapshot;
    private bool _doubleClickPending;

    public TrayController(SettingsService settings, PollingService polling)
    {
        _settings = settings;
        _polling = polling;

        _icon = new TaskbarIcon
        {
            // Id estable → Windows registra y persiste la posición/visibilidad del icono.
            Id = new Guid("8E0F7A12-BFB3-4FE8-B9A5-48FD50A15A9A"),
            ToolTipText = Loc.T(Str.AppName),
            Visibility = Visibility.Visible
        };
        _icon.TrayLeftMouseUp += OnLeftClick;
        _icon.TrayMouseDoubleClick += OnDoubleClick;
        _icon.ContextMenu = BuildContextMenu();

        // Distingue clic simple de doble clic: esperamos el doble-click time del sistema.
        _clickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(GetDoubleClickTime())
        };
        _clickTimer.Tick += OnSingleClickConfirmed;

        UpdateIcon(null); // estado inicial "cargando"

        // Garantiza que el icono se cree de inmediato (sin esperar al primer render WPF).
        try { _icon.ForceCreate(); } catch { /* algunos entornos no lo soportan */ }

        // Aviso de arranque para confirmar que está vivo (aparece aunque el icono esté en el overflow).
        try
        {
            _icon.ShowNotification(
                title: Loc.T(Str.TrayStartupTitle),
                message: Loc.T(Str.TrayStartupMessage));
        }
        catch { /* notificaciones deshabilitadas */ }

        _polling.Updated += OnUsageUpdated;
    }

    // ---- Eventos del tray ----

    private void OnLeftClick(object sender, RoutedEventArgs e)
    {
        // Arrancamos el timer; si llega un doble clic antes de que dispare, se cancela.
        _doubleClickPending = false;
        _clickTimer.Stop();
        _clickTimer.Start();
    }

    private void OnSingleClickConfirmed(object? sender, EventArgs e)
    {
        _clickTimer.Stop();
        if (_doubleClickPending) { _doubleClickPending = false; return; }
        ToggleMiniPanel();
    }

    private void OnDoubleClick(object sender, RoutedEventArgs e)
    {
        _doubleClickPending = true;
        _clickTimer.Stop();
        HideMiniPanel();
        ShowMainWindow();
    }

    // ---- Actualización de uso ----

    private void OnUsageUpdated(UsageSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        UpdateIcon(snapshot);
        _miniPanel?.Update(snapshot);
    }

    private void UpdateIcon(UsageSnapshot? snapshot)
    {
        var newIcon = TrayIconRenderer.Render(snapshot);
        _icon.Icon = newIcon;
        _currentIcon?.Dispose();
        _currentIcon = newIcon;
        _icon.ToolTipText = BuildTooltip(snapshot);
    }

    private static string BuildTooltip(UsageSnapshot? s)
    {
        if (s is null) return Loc.T(Str.TooltipLoading);
        if (s.Status == UsageStatus.NotAuthenticated) return Loc.T(Str.TooltipNoSession);
        if (s.Status == UsageStatus.Error && s.FiveHour is null) return Loc.T(Str.TooltipNoConnection);

        string fh = s.FiveHour is { } a ? Loc.T(Str.TooltipFiveHour, $"{a.Percent:0}") : Loc.T(Str.TooltipFiveHourEmpty);
        string wk = s.SevenDay is { } b ? Loc.T(Str.TooltipWeek, $"{b.Percent:0}") : Loc.T(Str.TooltipWeekEmpty);
        string suffix = s.Status == UsageStatus.RateLimited ? Loc.T(Str.TooltipWaiting) : "";
        return $"{s.ProviderName} · {fh} · {wk}{suffix}";
    }

    // ---- MiniPanel ----

    private void ToggleMiniPanel()
    {
        if (_miniPanel is { IsVisible: true })
            HideMiniPanel();
        else
            ShowMiniPanel();
    }

    private void ShowMiniPanel()
    {
        _miniPanel ??= new MiniPanel(() => ShowMainWindow(), id => _polling.PollProvider(id));
        if (_lastSnapshot is not null) _miniPanel.Update(_lastSnapshot);
        _miniPanel.ShowNearTray();

        // No pidas datos automáticamente al abrir si estamos limitados (429):
        // esperamos a que pase el cooldown. El usuario puede forzar con el botón de refresh.
        if (_lastSnapshot?.Status != UsageStatus.RateLimited)
            _polling.PollNow(); // datos frescos al abrir
    }

    private void HideMiniPanel() => _miniPanel?.HidePanel();

    // ---- MainWindow (configuración) ----

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            _mainWindow = new MainWindow(_settings);
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }
        _mainWindow.Show();
        _mainWindow.Activate();
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;
    }

    // ---- Menú contextual ----

    private System.Windows.Controls.ContextMenu BuildContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem { Header = Loc.T(Str.MenuSettings) };
        openItem.Click += (_, _) => ShowMainWindow();

        var refreshItem = new System.Windows.Controls.MenuItem { Header = Loc.T(Str.MenuRefreshNow) };
        refreshItem.Click += (_, _) => _polling.PollNow();

        var exitItem = new System.Windows.Controls.MenuItem { Header = Loc.T(Str.MenuExit) };
        exitItem.Click += (_, _) => Application.Current.Shutdown();

        menu.Items.Add(openItem);
        menu.Items.Add(refreshItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exitItem);
        return menu;
    }

    private static int GetDoubleClickTime()
    {
        try { return Math.Max(200, (int)NativeMethods.GetDoubleClickTime()); }
        catch { return 300; }
    }

    public void Dispose()
    {
        _clickTimer.Stop();
        _polling.Updated -= OnUsageUpdated;
        _icon.Dispose();
        _currentIcon?.Dispose();
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern uint GetDoubleClickTime();
    }
}
