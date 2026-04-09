
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Color_Extensions.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Extension methods for System.Drawing.Color to support conversion
//   between GDI+ and SkiaSharp colour types.  Required when the GPU
//   rendering path (SkiaSharp/OpenGL) is active so that Chart_Theme
//   colours defined as System.Drawing.Color can be passed directly
//   to SKPaint and SKCanvas without manual conversion at every call site.
//
// USAGE
//   using Multimeter_Controller;
//
//   SKColor Sk = My_Color.To_SK_Color();
//
// ════════════════════════════════════════════════════════════════════════════════

using System.Drawing;
using SkiaSharp;

namespace Multimeter_Controller
{
  public static class Color_Extensions
  {
    /// <summary>
    /// Converts a System.Drawing.Color to a SkiaSharp SKColor,
    /// preserving all four ARGB channels.
    /// </summary>
    public static SKColor To_SK_Color( this Color C )
        => new SKColor( C.R, C.G, C.B, C.A );

    /// <summary>
    /// Converts a SkiaSharp SKColor back to a System.Drawing.Color.
    /// Useful when theme colours need to round-trip between renderers.
    /// </summary>
    public static Color To_GDI_Color( this SKColor C )
        => Color.FromArgb( C.Alpha, C.Red, C.Green, C.Blue );
  }
}

