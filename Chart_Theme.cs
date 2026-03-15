using System.Text.Json;
using System.Xml.Linq;

namespace Multimeter_Controller
{
  public class Chart_Theme
  {

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
    public Color [ ] Line_Colors
    {
      get; set;
    }

    public string Name { get; set; } = "";

    public void Copy_From ( Chart_Theme Other )
    {
      Background = Other.Background;
      Foreground = Other.Foreground;  // Add this
      Grid = Other.Grid;
      Labels = Other.Labels;
      Separator = Other.Separator;
      Line_Colors = Other.Line_Colors;
    }



    private static readonly string _File_Path =
      Path.Combine ( AppContext.BaseDirectory,
        "chart_theme.json" );

    public static Chart_Theme Dark_Preset ( )
    {
      return new Chart_Theme
      {
        Name = "Dark",
        Background = Color.FromArgb ( 24, 27, 31 ),
        Foreground = Color.FromArgb ( 220, 220, 220 ),
        Grid = Color.FromArgb ( 44, 50, 58 ),
        Labels = Color.FromArgb ( 140, 155, 170 ),
        Separator = Color.FromArgb ( 70, 80, 90 ),
        Line_Colors = new [ ]
        {
      Color.FromArgb ( 115, 191, 105 ),
      Color.FromArgb ( 110, 159, 232 ),
      Color.FromArgb ( 242, 163, 68 ),
      Color.FromArgb ( 184, 119, 217 ),
    }
      };
    }

    public static Chart_Theme Light_Preset ( )
    {
      return new Chart_Theme
      {
        Name = "Light",
        Background = Color.FromArgb ( 245, 245, 248 ),
        Foreground = Color.FromArgb ( 30, 30, 30 ),
        Grid = Color.FromArgb ( 210, 215, 220 ),
        Labels = Color.FromArgb ( 60, 70, 80 ),
        Separator = Color.FromArgb ( 180, 185, 190 ),
        Line_Colors = new [ ]
        {
      Color.FromArgb ( 40, 140, 30 ),
      Color.FromArgb ( 30, 100, 200 ),
      Color.FromArgb ( 210, 120, 20 ),
      Color.FromArgb ( 140, 60, 180 ),
    }
      };
    }


    public static Chart_Theme Brown_Preset ( )
    {
      return new Chart_Theme
      {
        Background = Color.FromArgb ( 45, 35, 25 ),
        Foreground = Color.FromArgb ( 30, 30, 30 ),
        Grid = Color.FromArgb ( 90, 70, 50 ),
        Labels = Color.BurlyWood,
        Separator = Color.SaddleBrown,
        Line_Colors = new [ ]
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
        Line_Colors = new [ ]
        {
      Color.LightGray,
      Color.Silver,
      Color.DarkGray,
      Color.WhiteSmoke
    }
      };
    }

    public static Chart_Theme Golden_Preset ( )
    {
      return new Chart_Theme
      {
        Background = Color.FromArgb ( 30, 25, 10 ),
        Foreground = Color.FromArgb ( 30, 30, 30 ),
        Grid = Color.Goldenrod,
        Labels = Color.Khaki,
        Separator = Color.DarkGoldenrod,
        Line_Colors = new [ ]
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
        Line_Colors = new [ ]
        {
      Color.Orange,
      Color.Goldenrod,
      Color.OliveDrab,
      Color.DarkOrange
    }
      };
    }


    public static Chart_Theme Light_Blue_Preset ( )
    {
      return new Chart_Theme
      {
        Name = "Light Blue",
        Background = Color.FromArgb ( 230, 240, 255 ),
        Foreground = Color.FromArgb ( 20, 20, 60 ),
        Grid = Color.FromArgb ( 180, 200, 230 ),
        Labels = Color.FromArgb ( 40, 80, 140 ),
        Separator = Color.FromArgb ( 140, 170, 210 ),
        Line_Colors = new [ ]
          {
            Color.FromArgb(30, 100, 200),
            Color.FromArgb(0, 160, 180),
            Color.FromArgb(100, 60, 200),
            Color.FromArgb(20, 140, 100),
        }
      };
    }


    public void Save ( )
    {
      try
      {
        var Data = new Theme_Data
        {
          Name = Name,
          Background = To_Hex ( Background ),
          Foreground = To_Hex ( Foreground ),
          Grid = To_Hex ( Grid ),
          Labels = To_Hex ( Labels ),
          Separator = To_Hex ( Separator ),
          Line_1 = To_Hex ( Line_Colors [ 0 ] ),
          Line_2 = To_Hex ( Line_Colors [ 1 ] ),
          Line_3 = To_Hex ( Line_Colors [ 2 ] ),
          Line_4 = To_Hex ( Line_Colors [ 3 ] ),
        };

        var Options = new JsonSerializerOptions
        {
          WriteIndented = true
        };

        string Json = JsonSerializer.Serialize (
          Data, Options );
        File.WriteAllText ( _File_Path, Json );
      }
      catch
      {
        // Silently ignore save failures
      }
    }

    public static Chart_Theme Load ( )
    {
      try
      {
        if ( !File.Exists ( _File_Path ) )
        {
          return Dark_Preset ( );
        }

        string Json = File.ReadAllText ( _File_Path );
        var Data = JsonSerializer.Deserialize<Theme_Data> (
          Json );

        if ( Data == null )
        {
          return Dark_Preset ( );
        }

        return new Chart_Theme
        {
          Name = Data.Name,
          Background = From_Hex ( Data.Background ),
          Foreground = From_Hex ( Data.Foreground ),  // ← add
          Grid = From_Hex ( Data.Grid ),
          Labels = From_Hex ( Data.Labels ),
          Separator = From_Hex ( Data.Separator ),
          Line_Colors = new [ ]
     {
            From_Hex ( Data.Line_1 ),
            From_Hex ( Data.Line_2 ),
            From_Hex ( Data.Line_3 ),
            From_Hex ( Data.Line_4 ),
          }
        };
      }
      catch
      {
        return Dark_Preset ( );
      }
    }



    private static string To_Hex ( Color C )
    {
      return $"#{C.R:X2}{C.G:X2}{C.B:X2}";
    }

    private static Color From_Hex ( string Hex )
    {
      if ( string.IsNullOrEmpty ( Hex ) || Hex.Length < 7 )
      {
        return Color.Gray;
      }

      Hex = Hex.TrimStart ( '#' );
      int R = Convert.ToInt32 ( Hex.Substring ( 0, 2 ), 16 );
      int G = Convert.ToInt32 ( Hex.Substring ( 2, 2 ), 16 );
      int B = Convert.ToInt32 ( Hex.Substring ( 4, 2 ), 16 );
      return Color.FromArgb ( R, G, B );
    }

    private class Theme_Data
    {
      public string Name { get; set; } = "";
      public string Background { get; set; } = "";
      public string Foreground { get; set; } = "";  // ← add
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
