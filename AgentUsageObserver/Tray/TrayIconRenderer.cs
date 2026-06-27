using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using AgentUsageObserver.Models;

namespace AgentUsageObserver.Tray;

/// <summary>
/// Genera dinámicamente el icono del tray:
///  - de fondo, un glifo propio estilo Claude (ráfaga radial) en escala de grises;
///  - encima, el porcentaje de la ventana de 5h, cuyo color cambia según la severidad.
/// El glifo es un símbolo propio (no el logo oficial) para evitar temas de licencia.
/// </summary>
public static class TrayIconRenderer
{
    private const int Size = 32;

    // Paleta del número por severidad.
    private static readonly Color Green = Color.FromArgb(63, 185, 80);
    private static readonly Color Yellow = Color.FromArgb(227, 179, 65);
    private static readonly Color Red = Color.FromArgb(240, 90, 84);
    private static readonly Color Gray = Color.FromArgb(150, 156, 163);

    // Tono de gris del glifo de fondo (más opaco para que se note detrás del número).
    private static readonly Color GlyphGray = Color.FromArgb(220, 150, 154, 161);

    public static Icon Render(UsageSnapshot? snapshot)
    {
        using var bmp = new Bitmap(Size, Size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            (Color numberColor, string text) = Describe(snapshot);

            // 1) Glifo de Claude en gris como ráfaga grande que llena el icono,
            //    bien visible detrás del número.
            DrawClaudeCorona(g, Size / 2f, Size / 2f, Size * 0.62f, GlyphGray);

            // 2) Número de % grande en el centro, coloreado por severidad, con halo.
            DrawNumber(g, text, numberColor);
        }

        IntPtr hIcon = bmp.GetHicon();
        using var temp = Icon.FromHandle(hIcon);
        var managed = (Icon)temp.Clone();
        DestroyIcon(hIcon);
        return managed;
    }

    /// <summary>Color del número + texto a mostrar según el snapshot.</summary>
    private static (Color numberColor, string text) Describe(UsageSnapshot? snapshot)
    {
        if (snapshot is null)
            return (Gray, "…");

        if (snapshot.Status == UsageStatus.NotAuthenticated)
            return (Gray, "?");

        var fh = snapshot.FiveHour;
        if (fh is null)
            return (Gray, snapshot.Status == UsageStatus.Error ? "!" : "?");

        int pct = (int)Math.Round(fh.Percent);
        Color color = fh.Severity switch
        {
            UsageSeverity.Critical => Red,
            UsageSeverity.Warning => Yellow,
            UsageSeverity.Normal => Green,
            _ => Gray
        };
        return (color, pct.ToString());
    }

    /// <summary>
    /// Dibuja una corona de rayos cortos estilo Claude alrededor del borde, con el centro
    /// despejado para que el número sea legible incluso a 16-32px.
    /// </summary>
    private static void DrawClaudeCorona(Graphics g, float cx, float cy, float radius, Color color)
    {
        const int rays = 12;                 // ráfaga de Claude
        float innerR = radius * 0.20f;       // rayos largos: arrancan cerca del centro
        using var brush = new SolidBrush(color);

        for (int i = 0; i < rays; i++)
        {
            float angle = (float)(i * 2 * Math.PI / rays) - (float)Math.PI / 2f;
            // Longitudes alternas (largo/corto) → aspecto orgánico característico.
            float len = radius * (i % 2 == 0 ? 1.05f : 0.80f);
            float halfWidth = radius * 0.15f;

            using var path = RayPetal(cx, cy, angle, innerR, len, halfWidth);
            g.FillPath(brush, path);
        }
    }

    /// <summary>Un pétalo/rayo: triángulo redondeado desde el radio interno hasta la punta.</summary>
    private static GraphicsPath RayPetal(float cx, float cy, float angle, float innerR, float len, float halfWidth)
    {
        // Vector dirección y perpendicular.
        float dx = (float)Math.Cos(angle), dy = (float)Math.Sin(angle);
        float px = -dy, py = dx;

        // Base (a innerR del centro, ancho = 2*halfWidth) y punta (a len del centro).
        PointF baseL = new(cx + dx * innerR + px * halfWidth, cy + dy * innerR + py * halfWidth);
        PointF baseR = new(cx + dx * innerR - px * halfWidth, cy + dy * innerR - py * halfWidth);
        PointF tip = new(cx + dx * len, cy + dy * len);

        var path = new GraphicsPath();
        path.AddPolygon(new[] { baseL, tip, baseR });
        return path;
    }

    /// <summary>Dibuja el número centrado, con un sutil contorno oscuro para destacarlo sobre el glifo.</summary>
    private static void DrawNumber(Graphics g, string text, Color color)
    {
        float fontSize = text.Length >= 3 ? 14f : 18f;
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        var rect = new RectangleF(0, 0, Size, Size);

        // Halo/contorno oscuro grueso (8 desplazamientos) para que el número resalte
        // con claridad sobre la ráfaga gris de fondo.
        using (var halo = new SolidBrush(Color.FromArgb(230, 18, 20, 24)))
        {
            var offsets = new[]
            {
                (-1.4f, 0f), (1.4f, 0f), (0f, -1.4f), (0f, 1.4f),
                (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f)
            };
            foreach (var (ox, oy) in offsets)
            {
                var r = new RectangleF(ox, oy, Size, Size);
                g.DrawString(text, font, halo, r, sf);
            }
        }

        using var brush = new SolidBrush(color);
        g.DrawString(text, font, brush, rect, sf);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
