using System.Collections.Generic;

namespace AgentUsageObserver.Services.Localization;

/// <summary>
/// Claves de todos los textos visibles de la aplicación.
/// Cada clave debe tener entrada en las tres tablas de <see cref="Strings"/>.
/// Las que contienen "{0}" se usan con <see cref="Loc.T(Str, object[])"/>.
/// </summary>
public enum Str
{
    // App / general
    AppName,
    UnexpectedError,          // {0} = mensaje

    // Ventana de configuración (MainWindow)
    WindowConfigTitle,
    SettingsHeader,
    SectionRefresh,
    PollIntervalLabel,        // {0} = segundos
    PollIntervalHint,
    SectionIconColors,
    UseServerSeverity,
    UseServerSeverityHint,
    WarnFromLabel,            // {0} = porcentaje
    CritFromLabel,            // {0} = porcentaje
    SectionSystem,
    StartWithWindows,
    ButtonClose,
    ButtonSave,

    // Mini panel
    MiniPanelTitle,
    StatusNoSession,
    StatusNoConnection,
    StatusRateLimited,
    MsgSignIn,
    MsgCouldNotFetch,
    MsgRateLimitReached,
    Updated,                  // {0} = hora
    OpenSettingsLink,

    // Barras de uso
    LabelFiveHour,
    LabelWeek,
    ResetsSoon,
    ResetsInDays,             // {0}=días {1}=horas
    ResetsInHours,            // {0}=horas {1}=minutos
    ResetsInMinutes,          // {0}=minutos

    // Tray / tooltip
    TrayStartupTitle,
    TrayStartupMessage,
    TooltipLoading,
    TooltipNoSession,
    TooltipNoConnection,
    TooltipFiveHour,          // {0} = porcentaje
    TooltipFiveHourEmpty,
    TooltipWeek,              // {0} = porcentaje
    TooltipWeekEmpty,
    TooltipWaiting,
    MenuSettings,
    MenuRefreshNow,
    MenuExit,

    // Mensajes del proveedor (Claude)
    ProviderSignInClaudeCode,
    ProviderSessionExpired,
    ProviderRateLimitRetry,   // {0} = espera
    ProviderUsageQueryError,  // {0} = código http
    ProviderNoConnection,

    // Unidades de tiempo de espera
    WaitSeconds,              // {0}
    WaitMinutes,              // {0}
    WaitHours,                // {0}

    // Actualizaciones
    UpdateAvailableTitle,
    UpdateAvailableMessage,   // {0}=versión nueva {1}=versión actual
    UpdateButtonInstall,
    UpdateButtonLater,
    UpdateButtonSkip,
    UpdateDownloadError,      // {0} = mensaje
}

/// <summary>
/// Tablas de traducción. Inglés (En) es el fallback y debe estar completo.
/// </summary>
public static class Strings
{
    public static readonly IReadOnlyDictionary<Str, string> En = new Dictionary<Str, string>
    {
        [Str.AppName] = "Agent Usage Observer",
        [Str.UnexpectedError] = "An unexpected error occurred:\n\n{0}",

        [Str.WindowConfigTitle] = "Agent Usage Observer",
        [Str.SettingsHeader] = "Settings",
        [Str.SectionRefresh] = "Refresh",
        [Str.PollIntervalLabel] = "Query interval: ",
        [Str.PollIntervalHint] = "How often usage is queried from Anthropic (60 to 900 s). Increasing this avoids the 429 error.",
        [Str.SectionIconColors] = "Icon colors",
        [Str.UseServerSeverity] = "Use the severity reported by the server",
        [Str.UseServerSeverityHint] = "When enabled, Anthropic decides the color; otherwise the thresholds below are used.",
        [Str.WarnFromLabel] = "Yellow from: ",
        [Str.CritFromLabel] = "Red from: ",
        [Str.SectionSystem] = "System",
        [Str.StartWithWindows] = "Start with Windows",
        [Str.ButtonClose] = "Close",
        [Str.ButtonSave] = "Save",

        [Str.MiniPanelTitle] = "Claude",
        [Str.StatusNoSession] = "no session",
        [Str.StatusNoConnection] = "no connection",
        [Str.StatusRateLimited] = "limit (429)",
        [Str.MsgSignIn] = "Sign in to Claude Code.",
        [Str.MsgCouldNotFetch] = "Could not fetch usage.",
        [Str.MsgRateLimitReached] = "Query limit reached.",
        [Str.Updated] = "Updated {0}",
        [Str.OpenSettingsLink] = "Open settings ⚙",

        [Str.LabelFiveHour] = "Last 5h",
        [Str.LabelWeek] = "Week",
        [Str.ResetsSoon] = "Resets soon",
        [Str.ResetsInDays] = "Resets in {0}d {1}h",
        [Str.ResetsInHours] = "Resets in {0}h {1}m",
        [Str.ResetsInMinutes] = "Resets in {0}m",

        [Str.TrayStartupTitle] = "Agent Usage Observer",
        [Str.TrayStartupMessage] = "Monitoring Claude. Look for the icon in the notification area (^ arrow).",
        [Str.TooltipLoading] = "Claude · loading…",
        [Str.TooltipNoSession] = "Claude · no session",
        [Str.TooltipNoConnection] = "Claude · no connection",
        [Str.TooltipFiveHour] = "5h {0}%",
        [Str.TooltipFiveHourEmpty] = "5h —",
        [Str.TooltipWeek] = "Week {0}%",
        [Str.TooltipWeekEmpty] = "Week —",
        [Str.TooltipWaiting] = " · waiting (429)",
        [Str.MenuSettings] = "Settings…",
        [Str.MenuRefreshNow] = "Refresh now",
        [Str.MenuExit] = "Exit",

        [Str.ProviderSignInClaudeCode] = "Sign in to Claude Code (claude login).",
        [Str.ProviderSessionExpired] = "Claude session expired. Reopen Claude Code.",
        [Str.ProviderRateLimitRetry] = "Query limit reached. Retry in {0}.",
        [Str.ProviderUsageQueryError] = "Error querying usage ({0}).",
        [Str.ProviderNoConnection] = "No connection to api.anthropic.com.",

        [Str.WaitSeconds] = "{0} s",
        [Str.WaitMinutes] = "{0} min",
        [Str.WaitHours] = "{0} h",

        [Str.UpdateAvailableTitle] = "Update available",
        [Str.UpdateAvailableMessage] = "A new version is available: {0} (you have {1}).\n\nDownload and install it now?",
        [Str.UpdateButtonInstall] = "Install now",
        [Str.UpdateButtonLater] = "Later",
        [Str.UpdateButtonSkip] = "Skip this version",
        [Str.UpdateDownloadError] = "Could not download the update:\n\n{0}",
    };

    public static readonly IReadOnlyDictionary<Str, string> Es = new Dictionary<Str, string>
    {
        [Str.AppName] = "Agent Usage Observer",
        [Str.UnexpectedError] = "Ocurrió un error inesperado:\n\n{0}",

        [Str.WindowConfigTitle] = "Agent Usage Observer",
        [Str.SettingsHeader] = "Configuración",
        [Str.SectionRefresh] = "Actualización",
        [Str.PollIntervalLabel] = "Intervalo de consulta: ",
        [Str.PollIntervalHint] = "Cada cuánto se consulta el uso a Anthropic (60 a 900 s). Subir esto evita el error 429.",
        [Str.SectionIconColors] = "Colores del icono",
        [Str.UseServerSeverity] = "Usar la severidad que reporta el servidor",
        [Str.UseServerSeverityHint] = "Si está activo, el color lo decide Anthropic; si no, usa los umbrales de abajo.",
        [Str.WarnFromLabel] = "Amarillo a partir de: ",
        [Str.CritFromLabel] = "Rojo a partir de: ",
        [Str.SectionSystem] = "Sistema",
        [Str.StartWithWindows] = "Iniciar con Windows",
        [Str.ButtonClose] = "Cerrar",
        [Str.ButtonSave] = "Guardar",

        [Str.MiniPanelTitle] = "Claude",
        [Str.StatusNoSession] = "sin sesión",
        [Str.StatusNoConnection] = "sin conexión",
        [Str.StatusRateLimited] = "límite (429)",
        [Str.MsgSignIn] = "Inicia sesión en Claude Code.",
        [Str.MsgCouldNotFetch] = "No se pudo obtener el uso.",
        [Str.MsgRateLimitReached] = "Límite de consultas alcanzado.",
        [Str.Updated] = "Actualizado {0}",
        [Str.OpenSettingsLink] = "Abrir configuración ⚙",

        [Str.LabelFiveHour] = "Últimas 5h",
        [Str.LabelWeek] = "Semana",
        [Str.ResetsSoon] = "Reinicia pronto",
        [Str.ResetsInDays] = "Reinicia en {0}d {1}h",
        [Str.ResetsInHours] = "Reinicia en {0}h {1}m",
        [Str.ResetsInMinutes] = "Reinicia en {0}m",

        [Str.TrayStartupTitle] = "Agent Usage Observer",
        [Str.TrayStartupMessage] = "Monitoreando Claude. Busca el icono en el área de notificaciones (flecha ^).",
        [Str.TooltipLoading] = "Claude · cargando…",
        [Str.TooltipNoSession] = "Claude · sin sesión",
        [Str.TooltipNoConnection] = "Claude · sin conexión",
        [Str.TooltipFiveHour] = "5h {0}%",
        [Str.TooltipFiveHourEmpty] = "5h —",
        [Str.TooltipWeek] = "Semana {0}%",
        [Str.TooltipWeekEmpty] = "Semana —",
        [Str.TooltipWaiting] = " · esperando (429)",
        [Str.MenuSettings] = "Configuración…",
        [Str.MenuRefreshNow] = "Actualizar ahora",
        [Str.MenuExit] = "Salir",

        [Str.ProviderSignInClaudeCode] = "Inicia sesión en Claude Code (claude login).",
        [Str.ProviderSessionExpired] = "Sesión de Claude expirada. Reabre Claude Code.",
        [Str.ProviderRateLimitRetry] = "Límite de consultas alcanzado. Reintenta en {0}.",
        [Str.ProviderUsageQueryError] = "Error consultando uso ({0}).",
        [Str.ProviderNoConnection] = "Sin conexión con api.anthropic.com.",

        [Str.WaitSeconds] = "{0} s",
        [Str.WaitMinutes] = "{0} min",
        [Str.WaitHours] = "{0} h",

        [Str.UpdateAvailableTitle] = "Actualización disponible",
        [Str.UpdateAvailableMessage] = "Hay una versión nueva: {0} (tenés la {1}).\n\n¿Descargarla e instalarla ahora?",
        [Str.UpdateButtonInstall] = "Instalar ahora",
        [Str.UpdateButtonLater] = "Después",
        [Str.UpdateButtonSkip] = "Omitir esta versión",
        [Str.UpdateDownloadError] = "No se pudo descargar la actualización:\n\n{0}",
    };

    public static readonly IReadOnlyDictionary<Str, string> Pt = new Dictionary<Str, string>
    {
        [Str.AppName] = "Agent Usage Observer",
        [Str.UnexpectedError] = "Ocorreu um erro inesperado:\n\n{0}",

        [Str.WindowConfigTitle] = "Agent Usage Observer",
        [Str.SettingsHeader] = "Configurações",
        [Str.SectionRefresh] = "Atualização",
        [Str.PollIntervalLabel] = "Intervalo de consulta: ",
        [Str.PollIntervalHint] = "Com que frequência o uso é consultado na Anthropic (60 a 900 s). Aumentar isto evita o erro 429.",
        [Str.SectionIconColors] = "Cores do ícone",
        [Str.UseServerSeverity] = "Usar a severidade informada pelo servidor",
        [Str.UseServerSeverityHint] = "Se ativo, a cor é definida pela Anthropic; caso contrário, usa os limites abaixo.",
        [Str.WarnFromLabel] = "Amarelo a partir de: ",
        [Str.CritFromLabel] = "Vermelho a partir de: ",
        [Str.SectionSystem] = "Sistema",
        [Str.StartWithWindows] = "Iniciar com o Windows",
        [Str.ButtonClose] = "Fechar",
        [Str.ButtonSave] = "Salvar",

        [Str.MiniPanelTitle] = "Claude",
        [Str.StatusNoSession] = "sem sessão",
        [Str.StatusNoConnection] = "sem conexão",
        [Str.StatusRateLimited] = "limite (429)",
        [Str.MsgSignIn] = "Faça login no Claude Code.",
        [Str.MsgCouldNotFetch] = "Não foi possível obter o uso.",
        [Str.MsgRateLimitReached] = "Limite de consultas atingido.",
        [Str.Updated] = "Atualizado {0}",
        [Str.OpenSettingsLink] = "Abrir configurações ⚙",

        [Str.LabelFiveHour] = "Últimas 5h",
        [Str.LabelWeek] = "Semana",
        [Str.ResetsSoon] = "Reinicia em breve",
        [Str.ResetsInDays] = "Reinicia em {0}d {1}h",
        [Str.ResetsInHours] = "Reinicia em {0}h {1}m",
        [Str.ResetsInMinutes] = "Reinicia em {0}m",

        [Str.TrayStartupTitle] = "Agent Usage Observer",
        [Str.TrayStartupMessage] = "Monitorando o Claude. Procure o ícone na área de notificação (seta ^).",
        [Str.TooltipLoading] = "Claude · carregando…",
        [Str.TooltipNoSession] = "Claude · sem sessão",
        [Str.TooltipNoConnection] = "Claude · sem conexão",
        [Str.TooltipFiveHour] = "5h {0}%",
        [Str.TooltipFiveHourEmpty] = "5h —",
        [Str.TooltipWeek] = "Semana {0}%",
        [Str.TooltipWeekEmpty] = "Semana —",
        [Str.TooltipWaiting] = " · aguardando (429)",
        [Str.MenuSettings] = "Configurações…",
        [Str.MenuRefreshNow] = "Atualizar agora",
        [Str.MenuExit] = "Sair",

        [Str.ProviderSignInClaudeCode] = "Faça login no Claude Code (claude login).",
        [Str.ProviderSessionExpired] = "Sessão do Claude expirada. Reabra o Claude Code.",
        [Str.ProviderRateLimitRetry] = "Limite de consultas atingido. Tente novamente em {0}.",
        [Str.ProviderUsageQueryError] = "Erro ao consultar o uso ({0}).",
        [Str.ProviderNoConnection] = "Sem conexão com api.anthropic.com.",

        [Str.WaitSeconds] = "{0} s",
        [Str.WaitMinutes] = "{0} min",
        [Str.WaitHours] = "{0} h",

        [Str.UpdateAvailableTitle] = "Atualização disponível",
        [Str.UpdateAvailableMessage] = "Há uma versão nova: {0} (você tem a {1}).\n\nBaixar e instalar agora?",
        [Str.UpdateButtonInstall] = "Instalar agora",
        [Str.UpdateButtonLater] = "Depois",
        [Str.UpdateButtonSkip] = "Ignorar esta versão",
        [Str.UpdateDownloadError] = "Não foi possível baixar a atualização:\n\n{0}",
    };
}
