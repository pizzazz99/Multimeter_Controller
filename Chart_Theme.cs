using System.Text.Json;

namespace Multimeter_Controller
{
  public class Chart_Theme
  {
    public Color Background
    {
      get; set;
    }
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
    public Color [ ] Line_Colors { get; set; } = new Color [ 4 ];

    private static readonly string _File_Path =
      Path.Combine ( AppContext.BaseDirectory,
        "chart_theme.json" );

    public static Chart_Theme Dark_Preset ( )
    {
      return new Chart_Theme
      {
        Background = Color.FromArgb ( 24, 27, 31 ),
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
        Background = Color.FromArgb ( 245, 245, 248 ),
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

    public void Save ( )
    {
      try
      {
        var Data = new Theme_Data
        {
          Background = To_Hex ( Background ),
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
          Background = From_Hex ( Data.Background ),
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

    public void Copy_From ( Chart_Theme Other )
    {
      Background = Other.Background;
      Grid = Other.Grid;
      Labels = Other.Labels;
      Separator = Other.Separator;
      for ( int I = 0; I < 4; I++ )
      {
        Line_Colors [ I ] = Other.Line_Colors [ I ];
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
      public string Background { get; set; } = "";
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
