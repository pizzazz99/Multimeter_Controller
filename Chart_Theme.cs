
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Chart_Theme.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Defines the complete visual palette used by every chart panel in the
//   application.  A theme is a flat bag of Color properties that chart
//   renderers read directly — no CSS, no resource dictionaries.  Themes are
//   persisted as a small JSON file beside the source tree and loaded once at
//   startup via Application_Settings.
//
// PERSISTENCE
//   File location : <source directory>\chart_theme.json
//                   (resolved at compile time via [CallerFilePath])
//   Format        : UTF-8 JSON, WriteIndented = true
//   Serialisation : Colors are stored as KnownColor names where possible,
//                   falling back to "#RRGGBB" hex strings for non-named colors.
//   On load error : silently falls back to Dark_Preset(); never throws.
//   On save error : silently ignored; the in-memory theme remains intact.
//
// COLOR PROPERTIES
//   Background    Panel fill behind all chart content.
//   Foreground    Primary text and general annotation color.
//   Grid          Horizontal / vertical grid line color.
//   Labels        Y-axis and X-axis label text color.
//   Separator     Subplot divider and secondary border color.
//   Accent        Highlight color — mean lines, markers, interactive elements.
//   Line_Colors   Fixed array of exactly 4 colors cycled across data series.
//
// BUILT-IN PRESETS
//   Dark_Preset()         Near-black background; green/blue/orange/orchid lines.
//                         Default when no theme file exists.
//   Light_Preset()        WhiteSmoke background; forest-green/royal-blue lines.
//   Light_Blue_Preset()   AliceBlue background; blue/teal/slate palette.
//   Light_Yellow_Preset() LightYellow background; orange/goldenrod palette.
//   Golden_Preset()       Very dark warm background; gold/orange/yellow lines.
//   Brown_Preset()        Dark brown background; peru/chocolate/tan lines.
//   Grey_Preset()         Dark grey background; silver/gainsboro/white lines.
//
// KEY METHODS
//   static Load()          Reads chart_theme.json → deserialises Theme_Data →
//                          reconstructs Color properties via From_Color_String().
//                          Returns Dark_Preset() on any failure.
//   Save()                 Serialises all Color properties to Theme_Data via
//                          To_Color_String() and writes chart_theme.json.
//   Copy_From(other)       Shallow-copies all Color properties from another
//                          Chart_Theme instance in-place (used by the theme
//                          editor to apply a preset without replacing the
//                          reference held by Application_Settings).
//
// INTERNAL HELPERS
//   To_Color_String(Color)    Returns the KnownColor name if the color is named
//                             or matches a known color by RGB; otherwise "#RRGGBB".
//   From_Color_String(string) Parses "#RRGGBB" hex or a named color string back
//                             to a Color; returns Color.Gray on failure.
//
// PRIVATE SERIALISATION DTO
//   Theme_Data   Flat POCO with string properties for each color slot
//                (Background, Foreground, Grid, Labels, Separator, Accent,
//                Line_1 … Line_4) plus Name.  Never exposed outside the class.
//
// NOTES
//   • Line_Colors must always contain exactly 4 elements.  Presets enforce
//     this; Load() constructs the array from the four named slots in Theme_Data.
//   • _File_Path is resolved once at class-load time using [CallerFilePath]
//     so the theme file always sits next to the source file regardless of the
//     working directory at runtime.
//   • Background_Name, Foreground_Name, etc. are retained string properties
//     from an earlier serialisation approach; they are no longer written by
//     Save() but are left in place for backwards-compatible JSON loading.
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════




using System.Text.Json;
using System.Xml.Linq;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  public class Chart_Theme
  {


    // ── File paths ────────────────────────────────────────────────────────
    private static string Get_Theme_Folder()
    {
      string App_Data = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
      string App_Folder = Path.Combine( App_Data, "Multimeter_Controller" );
      Directory.CreateDirectory( App_Folder );
      return App_Folder;
    }

    private static readonly string _Panel_Theme_Path =
        Path.Combine( Get_Theme_Folder(), "panel_theme.json" );

    private static readonly string _Chart_Theme_Path =
        Path.Combine( Get_Theme_Folder(), "chart_theme.json" );


    public void Set_File_Path( string File_Path )
    {
      _My_File_Path = File_Path;
    }


    public static string Panel_Theme_Path => _Panel_Theme_Path;
    public static string Chart_Theme_Path => _Chart_Theme_Path;

    private string _My_File_Path = _Chart_Theme_Path;


    public string Background_Name { get; set; } = "";
    public string Foreground_Name { get; set; } = "";
    public string Grid_Name { get; set; } = "";
    public string Labels_Name { get; set; } = "";
    public string Separator_Name { get; set; } = "";
    public string [ ] Line_Color_Names { get; set; } = Array.Empty<string> ( );



    public Color [ ] Line_Colors
    {
      get; set;
    } = new Color [ ]
{
    Color.MediumSeaGreen,
    Color.CornflowerBlue,
    Color.SandyBrown,
    Color.MediumOrchid,
};

    public Color Accent
    {
      get; set;
    }
    public Color Background
    {
      get; set;
    }
    public Color Foreground
    {
      get; set;
    }  // Add this
    public Color Grid
    {
      get; set;
    }
    public Color Labels
    {
      get; set;
    }
    public Color Separator
    {
      get; set;
    }


    public string Name { get; set; } = "Dark";



    public void Copy_From ( Chart_Theme Other )
    {
      Background = Other.Background;
      Foreground = Other.Foreground;  // Add this
      Grid = Other.Grid;
      Labels = Other.Labels;
      Separator = Other.Separator;
      Line_Colors = Other.Line_Colors;
      Accent = Other.Accent;
    }



    private static readonly string _File_Path =
      Path.Combine (
          Path.GetDirectoryName ( Get_Source_Path ( ) )!,
          "chart_theme.json" );

    private static string Get_Source_Path (
        [System.Runtime.CompilerServices.CallerFilePath] string Path = "" ) => Path;





    public static Chart_Theme Brown_Preset()
    {
      return new Chart_Theme
      {
        Background = Color.FromArgb( 80, 60, 45 ),       // was (45, 35, 25) — lighter brown
        Foreground = Color.FromArgb( 240, 220, 200 ),    // was (30, 30, 30) — warm cream
        Grid = Color.FromArgb( 130, 100, 75 ),     // was (90, 70, 50) — lighter
        Labels = Color.BurlyWood,
        Separator = Color.SaddleBrown,
        Accent = Color.Gold,
        Name = "Brown",
        Line_Colors = new[]
          {
            Color.Peru,
            Color.Chocolate,
            Color.Tan,
            Color.SandyBrown
        }
      };
    }

    public static Chart_Theme Grey_Preset ( )
    {
      return new Chart_Theme
      {
        Background = Color.FromArgb ( 40, 40, 40 ),
        Foreground = Color.FromArgb ( 30, 30, 30 ),
        Grid = Color.DimGray,
        Labels = Color.Gainsboro,
        Separator = Color.Gray,
        Accent = Color.Gold,
        Name = "Grey",
        Line_Colors = new [ ]
        {
      Color.LightGray,
      Color.Silver,
      Color.DarkGray,
      Color.WhiteSmoke
    }
      };
    }

    public static Chart_Theme Golden_Preset()
    {
      return new Chart_Theme
      {
        Background = Color.FromArgb( 65, 55, 25 ),       // was (30, 25, 10) — lighter warm gold
        Foreground = Color.FromArgb( 255, 245, 200 ),    // was (30, 30, 30) — pale gold/cream
        Grid = Color.Goldenrod,
        Labels = Color.Khaki,
        Separator = Color.DarkGoldenrod,
        Accent = Color.Gold,
        Name = "Golden",
        Line_Colors = new[]
          {
            Color.Gold,
            Color.Orange,
            Color.Yellow,
            Color.Khaki
        }
      };
    }

    public static Chart_Theme Light_Yellow_Preset ( )
    {
      return new Chart_Theme
      {
        Background = Color.LightYellow,
        Foreground = Color.FromArgb ( 30, 30, 30 ),
        Grid = Color.Goldenrod,
        Labels = Color.Black,
        Separator = Color.DarkKhaki,
        Accent = Color.DarkGoldenrod,
        Name = "Light Yellow",
        Line_Colors = new [ ]
        {
      Color.Orange,
      Color.Goldenrod,
      Color.OliveDrab,
      Color.DarkOrange
    }
      };
    }


    public static Chart_Theme Dark_Preset ( )
    {
      return new Chart_Theme
      {
        Name = "Dark",
        Background = Color.FromArgb ( 24, 27, 31 ),    // truly no named match
        Foreground = Color.WhiteSmoke,                  // was (220, 220, 220)
        Grid = Color.FromArgb ( 44, 50, 58 ),    // truly no named match
        Labels = Color.LightSlateGray,              // was (140, 155, 170)
        Separator = Color.SlateGray,                   // was (70, 80, 90)
        Accent = Color.Gold,
        Line_Colors = new [ ]
          {
            Color.MediumSeaGreen,
            Color.CornflowerBlue,
            Color.SandyBrown,
            Color.MediumOrchid,
        }
      };
    }

    public static Chart_Theme Light_Preset ( )
    {
      return new Chart_Theme
      {
        Name = "Light",
        Background = Color.WhiteSmoke,                  // was (245, 245, 248)
        Foreground = Color.FromArgb ( 30, 30, 30 ),    // truly no named match
        Grid = Color.FromArgb ( 210, 215, 220 ), // truly no named match
        Labels = Color.FromArgb ( 60, 70, 80 ),    // truly no named match
        Separator = Color.Silver,                      // was (180, 185, 190)
        Accent = Color.DarkGoldenrod,
        Line_Colors = new [ ]
          {
            Color.ForestGreen,
            Color.RoyalBlue,
            Color.DarkOrange,
            Color.MediumPurple,
        }
      };
    }

    public static Chart_Theme Light_Blue_Preset ( )
    {
      return new Chart_Theme
      {
        Name = "Light Blue",
        Background = Color.AliceBlue,                   // was (230, 240, 255)
        Foreground = Color.MidnightBlue,                // was (20, 20, 60)
        Grid = Color.LightSteelBlue,              // was (180, 200, 230)
        Labels = Color.SteelBlue,                   // was (40, 80, 140)
        Separator = Color.CornflowerBlue,              // was (140, 170, 210)
        Accent = Color.Gold,
        Line_Colors = new [ ]
          {
            Color.RoyalBlue,
            Color.DarkTurquoise,
            Color.SlateBlue,
            Color.MediumSeaGreen,
        }
      };
    }


    public void Save()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      Capture_Trace.Write( $"File path -> {_My_File_Path}" );   // ✅
      try
      {
        var Data = new Theme_Data
        {
          Name = Name,
          Background = To_Color_String( Background ),
          Foreground = To_Color_String( Foreground ),
          Grid = To_Color_String( Grid ),
          Labels = To_Color_String( Labels ),
          Separator = To_Color_String( Separator ),
          Accent = To_Color_String( Accent ),
          Line_1 = To_Color_String( Line_Colors[ 0 ] ),
          Line_2 = To_Color_String( Line_Colors[ 1 ] ),
          Line_3 = To_Color_String( Line_Colors[ 2 ] ),
          Line_4 = To_Color_String( Line_Colors[ 3 ] ),
        };

        var Options = new JsonSerializerOptions { WriteIndented = true };
        string Json = JsonSerializer.Serialize( Data, Options );
        File.WriteAllText( _My_File_Path, Json );              // ✅
      }
      catch
      {
        // Silently ignore save failures
      }
    }
    public static Chart_Theme Load( string File_Path )
    {
      try
      {
        if (!File.Exists( File_Path ))
        {
          return Dark_Preset();
        }

        string Json = File.ReadAllText( File_Path );
        var Data = JsonSerializer.Deserialize<Theme_Data>( Json );

        if (Data == null)
        {
          return Dark_Preset();
        }

        return new Chart_Theme
        {
          _My_File_Path = File_Path,          // ← remember which file this came from
          Name = Data.Name,
          Background = From_Color_String( Data.Background ),
          Foreground = From_Color_String( Data.Foreground ),
          Grid = From_Color_String( Data.Grid ),
          Labels = From_Color_String( Data.Labels ),
          Separator = From_Color_String( Data.Separator ),
          Accent = From_Color_String( Data.Accent ),
          Line_Colors = new[]
            {
                From_Color_String( Data.Line_1 ),
                From_Color_String( Data.Line_2 ),
                From_Color_String( Data.Line_3 ),
                From_Color_String( Data.Line_4 ),
            }
        };
      }
      catch
      {
        return Dark_Preset();
      }
    }


    private static string To_Color_String ( Color C )
    {
      if ( C.IsNamedColor )
        return C.Name;
      // Check if it matches any known color by RGB
      foreach ( KnownColor KC in (KnownColor [ ]) Enum.GetValues ( typeof ( KnownColor ) ) )
      {
        Color Known = Color.FromKnownColor ( KC );
        if ( Known.IsSystemColor )
          continue;
        if ( Known.R == C.R && Known.G == C.G && Known.B == C.B )
          return KC.ToString ( );
      }
      return $"#{C.R:X2}{C.G:X2}{C.B:X2}";
    }

    private static Color From_Color_String ( string Value )
    {
      if ( string.IsNullOrEmpty ( Value ) )
        return Color.Gray;

      if ( Value.StartsWith ( "#" ) )
      {
        string Hex = Value.TrimStart ( '#' );
        if ( Hex.Length < 6 )
          return Color.Gray;
        int R = Convert.ToInt32 ( Hex.Substring ( 0, 2 ), 16 );
        int G = Convert.ToInt32 ( Hex.Substring ( 2, 2 ), 16 );
        int B = Convert.ToInt32 ( Hex.Substring ( 4, 2 ), 16 );
        return Color.FromArgb ( R, G, B );
      }

      // Try named color
      Color Named = Color.FromName ( Value );
      if ( Named.IsKnownColor )
        return Named;

      return Color.Gray;
    }

    private class Theme_Data
    {
      public string Name { get; set; } = "";
      public string Background { get; set; } = "";
      public string Foreground { get; set; } = "";
      public string Accent { get; set; } = "";   // ← add this
      public string Grid { get; set; } = "";
      public string Labels { get; set; } = "";
      public string Separator { get; set; } = "";
      public string Line_1 { get; set; } = "";
      public string Line_2 { get; set; } = "";
      public string Line_3 { get; set; } = "";
      public string Line_4 { get; set; } = "";
    }
  }
}
