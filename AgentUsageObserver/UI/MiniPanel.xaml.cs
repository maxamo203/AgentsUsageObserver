using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AgentUsageObserver.Models;
using AgentUsageObserver.Services.Localization;

namespace AgentUsageObserver.UI;

/// <summary>
/// Mini panel emergente (clic simple en el icono). Muestra barras de 5h y semanal
/// con su porcentaje y el tiempo hasta el reset.
/// </summary>
public partial class MiniPanel : Window
{
    private readonly Action _openSettings;
    private readonly Action<string> _refreshProvider;

    private static readonly Color Green = Color.FromRgb(46, 160, 67);
    private static readonly Color Yellow = Color.FromRgb(210, 153, 34);
    private static readonly Color Red = Color.FromRgb(218, 54, 51);
    private static readonly Color Gray = Color.FromRgb(110, 118, 129);

    // Proveedor (agente) que muestra este panel; lo usa el botón de refresh.
    private string? _providerId;

    public MiniPanel(Action openSettings, Action<string> refreshProvider)
    {
        _openSettings = openSettings;
        _refreshProvider = refreshProvider;
        InitializeComponent();
    }

    /// <summary>Refresca el contenido con un nuevo snapshot.</summary>
    public void Update(UsageSnapshot snapshot)
    {
        _providerId = snapshot.ProviderId;
        ProviderNameText.Text = snapshot.ProviderName;
        StopSpin();
        BarsHost.Children.Clear();

        switch (snapshot.Status)
        {
            case UsageStatus.NotAuthenticated:
                StatusText.Text = Loc.T(Str.StatusNoSession);
                ShowMessage(snapshot.Message ?? Loc.T(Str.MsgSignIn));
                break;

            case UsageStatus.Error when snapshot.Windows.Count == 0:
                StatusText.Text = Loc.T(Str.StatusNoConnection);
                ShowMessage(snapshot.Message ?? Loc.T(Str.MsgCouldNotFetch));
                break;

            case UsageStatus.RateLimited:
                // Mostramos el aviso y, si los tenemos, los últimos datos buenos.
                StatusText.Text = Loc.T(Str.StatusRateLimited);
                ShowMessage(snapshot.Message ?? Loc.T(Str.MsgRateLimitReached));
                foreach (var w in snapshot.Windows)
                    BarsHost.Children.Add(BuildBar(w));
                break;

            default:
                StatusText.Text = "";
                MessageText.Visibility = Visibility.Collapsed;
                foreach (var w in snapshot.Windows)
                    BarsHost.Children.Add(BuildBar(w));
                break;
        }

        UpdatedText.Text = Loc.T(Str.Updated, snapshot.RetrievedAt.ToLocalTime().ToString("HH:mm:ss"));
    }

    private void ShowMessage(string text)
    {
        MessageText.Text = text;
        MessageText.Visibility = Visibility.Visible;
    }

    private UIElement BuildBar(UsageWindow w)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

        // Fila superior: etiqueta + porcentaje.
        var header = new Grid();
        header.Children.Add(new TextBlock
        {
            Text = w.Label,
            Foreground = new SolidColorBrush(Color.FromRgb(201, 205, 212)),
            FontSize = 12
        });
        header.Children.Add(new TextBlock
        {
            Text = $"{w.Percent:0}%",
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = Brushes.White,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold
        });
        panel.Children.Add(header);

        // Barra de progreso.
        var track = new Border
        {
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.FromRgb(44, 46, 54)),
            Margin = new Thickness(0, 5, 0, 3),
            ClipToBounds = true
        };
        var fill = new Border
        {
            Height = 8,
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = new SolidColorBrush(ColorFor(w.Severity)),
            Width = 0,
            Tag = Math.Clamp(w.Percent, 0, 100)
        };
        // El ancho se calcula al renderizar el track (cuando ya tiene ActualWidth).
        track.Child = fill;
        track.Loaded += (_, _) => SetFillWidth(track, fill);
        track.SizeChanged += (_, _) => SetFillWidth(track, fill);
        panel.Children.Add(track);

        // Pie: tiempo hasta el reset.
        panel.Children.Add(new TextBlock
        {
            Text = FormatReset(w.TimeUntilReset),
            Foreground = new SolidColorBrush(Color.FromRgb(110, 118, 129)),
            FontSize = 10
        });

        return panel;
    }

    private static void SetFillWidth(Border track, Border fill)
    {
        if (fill.Tag is double pct && track.ActualWidth > 0)
            fill.Width = track.ActualWidth * (pct / 100.0);
    }

    private static Color ColorFor(UsageSeverity s) => s switch
    {
        UsageSeverity.Critical => Red,
        UsageSeverity.Warning => Yellow,
        UsageSeverity.Normal => Green,
        _ => Gray
    };

    private static string FormatReset(TimeSpan? t)
    {
        if (t is not { } span) return "";
        if (span < TimeSpan.Zero) return Loc.T(Str.ResetsSoon);
        if (span.TotalHours >= 24) return Loc.T(Str.ResetsInDays, (int)span.TotalDays, span.Hours);
        if (span.TotalHours >= 1) return Loc.T(Str.ResetsInHours, (int)span.TotalHours, span.Minutes);
        return Loc.T(Str.ResetsInMinutes, span.Minutes);
    }

    /// <summary>Posiciona el panel sobre el área de notificación y lo muestra.</summary>
    public void ShowNearTray()
    {
        var area = SystemParameters.WorkArea; // excluye la barra de tareas
        // Asegura medir el tamaño antes de posicionar.
        Show();
        UpdateLayout();

        Left = area.Right - ActualWidth - 8;
        Top = area.Bottom - ActualHeight - 8;

        Activate();
        Topmost = true;
    }

    public void HidePanel() => Hide();

    private void OnDeactivated(object? sender, EventArgs e) => Hide();

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        Hide();
        _openSettings();
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_providerId)) return;
        StartSpin();
        _refreshProvider(_providerId);
        // El giro se detiene en el próximo Update() con el snapshot fresco.
    }

    private void StartSpin()
    {
        var spin = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(0.8),
            RepeatBehavior = RepeatBehavior.Forever
        };
        RefreshRotate.BeginAnimation(RotateTransform.AngleProperty, spin);
    }

    private void StopSpin()
    {
        RefreshRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        RefreshRotate.Angle = 0;
    }
}
