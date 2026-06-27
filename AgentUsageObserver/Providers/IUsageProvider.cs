using System.Threading;
using System.Threading.Tasks;
using AgentUsageObserver.Models;

namespace AgentUsageObserver.Providers;

/// <summary>
/// Abstracción de un proveedor de uso de un agente de IA.
/// Hoy solo existe <see cref="Claude.ClaudeUsageProvider"/>; para añadir otro agente
/// basta implementar esta interfaz y registrarla en el PollingService.
/// </summary>
public interface IUsageProvider
{
    /// <summary>Id estable, p.ej. "claude".</summary>
    string Id { get; }

    /// <summary>Nombre legible, p.ej. "Claude".</summary>
    string Name { get; }

    /// <summary>
    /// Obtiene un snapshot del uso actual. Nunca lanza por errores esperables
    /// (red, auth): los devuelve como <see cref="UsageStatus"/> en el snapshot.
    /// </summary>
    Task<UsageSnapshot> GetUsageAsync(CancellationToken ct);
}
