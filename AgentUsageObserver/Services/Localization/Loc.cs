using System;
using System.Collections.Generic;
using System.Globalization;

namespace AgentUsageObserver.Services.Localization;

/// <summary>
/// Idiomas soportados por la aplicación.
/// </summary>
public enum AppLanguage
{
    English,
    Spanish,
    Portuguese
}

/// <summary>
/// Localización ligera basada en código (sin .resx ni satellite assemblies).
///
/// El idioma se elige una sola vez, a partir de la cultura de UI del sistema
/// (<see cref="CultureInfo.CurrentUICulture"/>). Se usa el código ISO de dos
/// letras del idioma, de modo que todas las variantes regionales colapsan al
/// mismo idioma:
///   es, es-AR, es-CL, es-ES, es-MX, ... → Español
///   pt, pt-BR, pt-PT, ...               → Portugués
///   cualquier otro                      → Inglés (fallback)
///
/// Uso desde código:    Loc.T(Str.Save)
/// Uso desde XAML:       {loc:Tr Save}   (ver <see cref="TrExtension"/>)
/// </summary>
public static class Loc
{
    /// <summary>Idioma activo, resuelto desde la cultura del sistema.</summary>
    public static AppLanguage Language { get; } = Detect();

    private static AppLanguage Detect()
    {
        // TwoLetterISOLanguageName normaliza es-AR/es-CL/es-ES → "es", pt-BR/pt-PT → "pt", etc.
        string iso = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return iso switch
        {
            "es" => AppLanguage.Spanish,
            "pt" => AppLanguage.Portuguese,
            _ => AppLanguage.English,
        };
    }

    /// <summary>Devuelve el texto de <paramref name="key"/> en el idioma activo.</summary>
    public static string T(Str key)
    {
        var table = Language switch
        {
            AppLanguage.Spanish => Strings.Es,
            AppLanguage.Portuguese => Strings.Pt,
            _ => Strings.En,
        };

        // Fallback en cascada: idioma activo → inglés → el propio nombre de la clave.
        if (table.TryGetValue(key, out var value)) return value;
        if (Strings.En.TryGetValue(key, out var en)) return en;
        return key.ToString();
    }

    /// <summary>Como <see cref="T(Str)"/> pero formateando con <paramref name="args"/>.</summary>
    public static string T(Str key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);
}
