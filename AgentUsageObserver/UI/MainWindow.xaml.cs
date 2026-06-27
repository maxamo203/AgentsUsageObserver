using System.Windows;
using AgentUsageObserver.Models;
using AgentUsageObserver.Services;

namespace AgentUsageObserver.UI;

/// <summary>
/// Ventana de configuración (doble clic en el icono). Edita una copia de los settings
/// y los persiste vía <see cref="SettingsService"/> al guardar.
/// </summary>
public partial class MainWindow : Window
{
    private readonly SettingsService _settings;
    private Settings _draft;
    // Arranca en true: WPF dispara ValueChanged de los sliders durante InitializeComponent(),
    // cuando aún no existen todos los controles. Lo apagamos al final de LoadFromDraft().
    private bool _loading = true;

    public MainWindow(SettingsService settings)
    {
        _settings = settings;
        _draft = settings.Current.Clone();
        InitializeComponent();
        LoadFromDraft();
    }

    private void LoadFromDraft()
    {
        _loading = true;

        IntervalSlider.Value = _draft.PollingIntervalSeconds;
        IntervalValueText.Text = $"{_draft.PollingIntervalSeconds} s";

        WarnSlider.Value = _draft.WarningThreshold;
        WarnValueText.Text = $"{_draft.WarningThreshold} %";

        CritSlider.Value = _draft.CriticalThreshold;
        CritValueText.Text = $"{_draft.CriticalThreshold} %";

        UseServerSeverityCheck.IsChecked = _draft.UseServerSeverity;
        StartupCheck.IsChecked = _draft.StartWithWindows;
        ThresholdsPanel.IsEnabled = !_draft.UseServerSeverity;

        _loading = false;
    }

    private void OnIntervalChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading || IntervalValueText is null) return;
        _draft.PollingIntervalSeconds = (int)e.NewValue;
        IntervalValueText.Text = $"{_draft.PollingIntervalSeconds} s";
    }

    private void OnWarnChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading || CritSlider is null || WarnValueText is null) return;
        _draft.WarningThreshold = (int)e.NewValue;
        WarnValueText.Text = $"{_draft.WarningThreshold} %";
        // Mantén crítico por encima de amarillo.
        if (CritSlider.Value <= e.NewValue)
            CritSlider.Value = System.Math.Min(100, e.NewValue + 1);
    }

    private void OnCritChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading || WarnSlider is null || CritValueText is null) return;
        _draft.CriticalThreshold = (int)e.NewValue;
        CritValueText.Text = $"{_draft.CriticalThreshold} %";
        if (WarnSlider.Value >= e.NewValue)
            WarnSlider.Value = System.Math.Max(1, e.NewValue - 1);
    }

    private void OnUseServerSeverityToggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _draft.UseServerSeverity = UseServerSeverityCheck.IsChecked == true;
        ThresholdsPanel.IsEnabled = !_draft.UseServerSeverity;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _draft.StartWithWindows = StartupCheck.IsChecked == true;
        _settings.Save(_draft);
        _draft = _settings.Current.Clone();
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
