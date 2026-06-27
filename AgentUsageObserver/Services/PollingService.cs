using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentUsageObserver.Models;
using AgentUsageObserver.Providers;

namespace AgentUsageObserver.Services;

/// <summary>
/// Sondea periódicamente a los proveedores registrados y emite <see cref="Updated"/>.
/// Hoy hay un solo proveedor (Claude); está preparado para varios.
/// </summary>
public sealed class PollingService : IDisposable
{
    private readonly IReadOnlyList<IUsageProvider> _providers;
    private readonly Func<Settings> _settings;
    private readonly SynchronizationContext? _uiContext;

    // Mínimo entre sondeos manuales (PollNow) para no provocar 429 al abrir el panel seguido.
    private static readonly TimeSpan ManualPollThrottle = TimeSpan.FromSeconds(15);

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private DateTimeOffset _lastPoll = DateTimeOffset.MinValue;
    private readonly Dictionary<string, DateTimeOffset> _lastProviderPoll = new();
    private readonly object _gate = new();

    /// <summary>Se dispara (en el hilo de UI) con el snapshot de cada proveedor tras cada sondeo.</summary>
    public event Action<UsageSnapshot>? Updated;

    public PollingService(IReadOnlyList<IUsageProvider> providers, Func<Settings> settings)
    {
        _providers = providers;
        _settings = settings;
        _uiContext = SynchronizationContext.Current;
    }

    public void Start()
    {
        if (_loop is not null) return;
        _cts = new CancellationTokenSource();
        _loop = RunLoopAsync(_cts.Token);
    }

    /// <summary>
    /// Solicita un sondeo inmediato (p.ej. al abrir el panel o cambiar settings),
    /// pero lo ignora si hubo otro hace muy poco, para no provocar 429.
    /// </summary>
    public void PollNow()
    {
        lock (_gate)
        {
            if (DateTimeOffset.UtcNow - _lastPoll < ManualPollThrottle)
                return;
        }
        _ = Task.Run(() => PollOnceAsync(_cts?.Token ?? CancellationToken.None));
    }

    /// <summary>
    /// Refresca un único proveedor (p.ej. el botón de refresh junto a su nombre).
    /// El throttle es por proveedor para no provocar 429.
    /// </summary>
    public void PollProvider(string providerId)
    {
        lock (_gate)
        {
            if (_lastProviderPoll.TryGetValue(providerId, out var last) &&
                DateTimeOffset.UtcNow - last < ManualPollThrottle)
                return;
            _lastProviderPoll[providerId] = DateTimeOffset.UtcNow;
        }
        _ = Task.Run(() => PollProviderAsync(providerId, _cts?.Token ?? CancellationToken.None));
    }

    private async Task PollProviderAsync(string providerId, CancellationToken ct)
    {
        var provider = _providers.FirstOrDefault(p => p.Id == providerId);
        if (provider is null) return;

        UsageSnapshot snapshot;
        try
        {
            snapshot = await provider.GetUsageAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            snapshot = UsageSnapshot.Error(provider.Id, provider.Name, ex.Message);
        }

        Raise(snapshot);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        // Primer sondeo inmediato.
        await PollOnceAsync(ct).ConfigureAwait(false);

        while (!ct.IsCancellationRequested)
        {
            int seconds = Math.Clamp(
                _settings().PollingIntervalSeconds,
                Settings.MinIntervalSeconds,
                Settings.MaxIntervalSeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { break; }

            await PollOnceAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        lock (_gate) { _lastPoll = DateTimeOffset.UtcNow; }

        foreach (var provider in _providers)
        {
            if (ct.IsCancellationRequested) return;

            UsageSnapshot snapshot;
            try
            {
                snapshot = await provider.GetUsageAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                snapshot = UsageSnapshot.Error(provider.Id, provider.Name, ex.Message);
            }

            Raise(snapshot);
        }
    }

    private void Raise(UsageSnapshot snapshot)
    {
        if (_uiContext is not null)
            _uiContext.Post(_ => Updated?.Invoke(snapshot), null);
        else
            Updated?.Invoke(snapshot);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
