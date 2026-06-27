using System;
using System.Windows.Markup;

namespace AgentUsageObserver.Services.Localization;

/// <summary>
/// Extensión de marcado para localizar texto en XAML:
///   <TextBlock Text="{loc:Tr Save}"/>
///
/// El idioma se resuelve una sola vez al inicio (ver <see cref="Loc"/>), por lo que
/// el valor se evalúa al cargar la vista. No hay cambio de idioma en caliente.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TrExtension : MarkupExtension
{
    /// <summary>Clave de texto a traducir.</summary>
    [ConstructorArgument("key")]
    public Str Key { get; set; }

    public TrExtension() { }

    public TrExtension(Str key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);
}
