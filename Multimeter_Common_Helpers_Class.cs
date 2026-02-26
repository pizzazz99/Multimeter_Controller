using System;
// ============================================================================
// MULTIMETER_COMMON.CS
// Shared utility methods extracted from Single_Instrument_Poll_Form
// and Multi_Instrument_Poll_Form
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trace_Execution_Namespace;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  public static class Multimeter_Common_Helpers_Class
  {

    // ========================================================================
    // NPLC COMMAND BUILDER
    // Used by both forms during instrument configuration
    // ========================================================================

    public static string? Build_NPLC_Command ( string Measurement_Label, string NPLC_Value )
    {
      return Measurement_Label switch
      {
        "DC Voltage" => $"VOLT:DC:NPLC {NPLC_Value}",
        "AC Voltage" => $"VOLT:AC:NPLC {NPLC_Value}",
        "DC Current" => $"CURR:DC:NPLC {NPLC_Value}",
        "AC Current" => $"CURR:AC:NPLC {NPLC_Value}",
        "2-Wire Ohms" => $"RES:NPLC {NPLC_Value}",
        "4-Wire Ohms" => $"FRES:NPLC {NPLC_Value}",
        _ => null  // Freq, Period, Diode, Continuity — no NPLC
      };
    }


    // ========================================================================
    // TIME FORMATTING
    // Used in stats panels, CSV headers, chart axis labels
    // ========================================================================

    public static string Format_Time_Span ( TimeSpan Span )
    {
      if ( Span.TotalSeconds < 1 )
        return $"{Span.TotalMilliseconds:F0}ms";
      if ( Span.TotalMinutes < 1 )
        return $"{Span.TotalSeconds:F1}s";
      if ( Span.TotalHours < 1 )
        return $"{Span.TotalMinutes:F1}m";
      if ( Span.TotalDays < 1 )
        return $"{Span.TotalHours:F1}h";
      return $"{Span.TotalDays:F1}d";
    }


    // ========================================================================
    // FILE / FOLDER HELPERS
    // ========================================================================

    public static string Get_Graph_Captures_Folder ( )
    {
      string Base = AppContext.BaseDirectory;
      string? Dir = Base;

      while ( Dir != null )
      {
        string Candidate = Path.Combine ( Dir, "Graph_Captures" );
        if ( Directory.Exists ( Candidate ) )
          return Candidate;
        Dir = Directory.GetParent ( Dir )?.FullName;
      }

      string Fallback = Path.Combine ( Base, "Graph_Captures" );
      Directory.CreateDirectory ( Fallback );
      return Fallback;
    }

    public static string Get_Filename_From_Pattern (
      string Pattern,
      string Function_Name )
    {
      string Filename = Pattern;
      Filename = Filename.Replace ( "{date}", DateTime.Now.ToString ( "yyyy-MM-dd" ) );
      Filename = Filename.Replace ( "{time}", DateTime.Now.ToString ( "HH-mm-ss" ) );
      Filename = Filename.Replace ( "{function}", Function_Name );

      if ( !Filename.EndsWith ( ".csv", StringComparison.OrdinalIgnoreCase ) )
        Filename += ".csv";

      return Filename;
    }


    // ========================================================================
    // VALUE FORMATTING
    // Full-precision formatter used for display in both forms
    // ========================================================================

    public static string Format_Value (
      double Value,
      string Unit,
      Meter_Type Meter = Meter_Type.HP34401 )
    {
      double Abs = Math.Abs ( Value );
      bool Is_HP = Meter == Meter_Type.HP3458;

      if ( Unit == "Hz" )
      {
        if ( Abs >= 1e6 )
          return $"{Value / 1e6:F3} MHz";
        if ( Abs >= 1e3 )
          return $"{Value / 1e3:F3} kHz";
        return $"{Value:F2} Hz";
      }

      if ( Unit == "s" )
      {
        if ( Abs < 1e-6 )
          return $"{Value * 1e9:F2} ns";
        if ( Abs < 1e-3 )
          return $"{Value * 1e6:F2} us";
        if ( Abs < 1.0 )
          return $"{Value * 1e3:F3} ms";
        return $"{Value:F4} s";
      }

      if ( Unit == "Ohm" )
      {
        if ( Abs >= 1e6 )
          return $"{Value / 1e6:F3} MOhm";
        if ( Abs >= 1e3 )
          return $"{Value / 1e3:F3} kOhm";
        return $"{Value:F2} Ohm";
      }

      if ( Unit == "\u00b0C" )
        return $"{Value:F2} \u00b0C";

      // V or A
      if ( Abs >= 1.0 )
        return Is_HP
            ? $"{Value:F10} {Unit}"
            : $"{Value:F8} {Unit}";
      if ( Abs >= 0.001 )
        return Is_HP
            ? $"{Value * 1000:F8} m{Unit}"
            : $"{Value * 1000:F6} m{Unit}";
      if ( Abs >= 0.000001 )
        return Is_HP
            ? $"{Value * 1e6:F6} u{Unit}"
            : $"{Value * 1e6:F5} u{Unit}";
      if ( Abs >= 0.000000001 )
        return Is_HP
            ? $"{Value * 1e9:F4} n{Unit}"
            : $"{Value * 1e9:F3} n{Unit}";

      return $"{Value:E2} {Unit}";
    }


    // ========================================================================
    // SETTINGS HELPERS
    // Common Apply_Settings logic (the parts that are identical)
    // ========================================================================

    public static void Apply_Common_Settings (
      Application_Settings Settings,
      Instrument_Comm Comm,
      System.Windows.Forms.Timer Auto_Save_Timer,
      System.Windows.Forms.Timer Chart_Refresh_Timer,
      int Current_Point_Count )
    {
      // Save folder
      if ( !string.IsNullOrEmpty ( Settings.Default_Save_Folder ) )
      {
        try
        {
          Directory.CreateDirectory ( Settings.Default_Save_Folder );
        }
        catch { }
      }

      // GPIB timeout
      if ( Comm != null )
        Comm.Read_Timeout_Ms = Settings.Default_GPIB_Timeout_Ms;

      // Auto-save timer
      if ( Settings.Enable_Auto_Save )
      {
        Auto_Save_Timer.Interval = Settings.Auto_Save_Interval_Minutes * 60 * 1000;
        Auto_Save_Timer.Start ( );
      }
      else
      {
        Auto_Save_Timer.Stop ( );
      }

      // Chart refresh rate with throttling
      bool Should_Throttle = Settings.Throttle_When_Many_Points
                          && Current_Point_Count > Settings.Throttle_Point_Threshold;

      Chart_Refresh_Timer.Interval = Should_Throttle
        ? Settings.Chart_Refresh_Rate_Ms * 2
        : Settings.Chart_Refresh_Rate_Ms;
    }


    // ========================================================================
    // CHART REFRESH RATE CALCULATION
    // Same throttle algorithm used by both forms
    // ========================================================================

    public static int Calculate_Refresh_Rate (
      Application_Settings Settings,
      int Total_Points )
    {
      int Base_Rate = Settings.Chart_Refresh_Rate_Ms;

      if ( !Settings.Throttle_When_Many_Points
          || Total_Points <= Settings.Throttle_Point_Threshold )
        return Base_Rate;

      int Multiplier = 1;

      if ( Total_Points > Settings.Throttle_Point_Threshold * 10 )
        Multiplier = 4;
      else if ( Total_Points > Settings.Throttle_Point_Threshold * 5 )
        Multiplier = 3;
      else if ( Total_Points > Settings.Throttle_Point_Threshold * 2 )
        Multiplier = 2;

      return Base_Rate * Multiplier;
    }


    // ========================================================================
    // MEMORY LIMIT CHECK
    // Accepts a delegate for the point count so it works for both
    // single-list and multi-series cases
    // ========================================================================

    public static void Check_Memory_Limit (
      Application_Settings Settings,
      Func<int> Get_Point_Count,
      Action Stop_Recording,
      Action<int, int> Show_Warning,
      ref bool Warning_Shown )
    {
      int Current = Get_Point_Count ( );
      int Max = Settings.Max_Data_Points_In_Memory;

      if ( Current >= Max )
      {
        Stop_Recording ( );
        return;
      }

      if ( Settings.Warn_At_Threshold && !Warning_Shown )
      {
        int Threshold = ( Max * Settings.Warning_Threshold_Percent ) / 100;

        if ( Current >= Threshold )
        {
          Show_Warning ( Current, Max );
          Warning_Shown = true;
        }
      }
    }


    // ========================================================================
    // CSV STATISTICS BLOCK WRITER
    // Shared header/stats format for both single and multi save files
    // ========================================================================

    public static void Write_Stats_Block (
      StreamWriter Writer,
      string Series_Name,
      int Point_Count,
      double Min,
      double Max,
      double Avg,
      double Std_Dev,
      double Range,
      TimeSpan? Duration,
      double? Rate,
      double? Avg_Interval_Ms )
    {
      Writer.WriteLine ( $"# [{Series_Name}]" );
      Writer.WriteLine ( $"#   Points: {Point_Count}" );
      Writer.WriteLine ( $"#   Average: {Avg:F7}" );
      Writer.WriteLine ( $"#   Std Dev: {Std_Dev:F7}" );
      Writer.WriteLine ( $"#   Min: {Min:F7}" );
      Writer.WriteLine ( $"#   Max: {Max:F7}" );
      Writer.WriteLine ( $"#   Range: {Range:F7}" );

      if ( Duration.HasValue )
        Writer.WriteLine ( $"#   Duration: {Format_Time_Span ( Duration.Value )}" );

      if ( Rate.HasValue )
        Writer.WriteLine ( $"#   Rate: {Rate.Value:F2} S/s" );

      if ( Avg_Interval_Ms.HasValue )
        Writer.WriteLine ( $"#   Avg \u0394t: {Avg_Interval_Ms.Value:F1} ms" );
    }


    // ========================================================================
    // STATISTICS CALCULATOR
    // Computes min/max/avg/stddev/rate from a list of timestamped points
    // Returns a named tuple so callers can pick what they need
    // ========================================================================

    public static (double Min, double Max, double Avg, double Std_Dev,
                   double Range, TimeSpan Duration, double Rate, double Avg_Interval_Ms)
      Calculate_Stats ( List<(DateTime Time, double Value)> Points )
    {
      if ( Points == null || Points.Count == 0 )
        return (0, 0, 0, 0, 0, TimeSpan.Zero, 0, 0);

      double Min = double.MaxValue;
      double Max = double.MinValue;
      double Sum = 0;

      foreach ( var P in Points )
      {
        if ( P.Value < Min )
          Min = P.Value;
        if ( P.Value > Max )
          Max = P.Value;
        Sum += P.Value;
      }

      double Avg = Sum / Points.Count;
      double Range = Max - Min;

      double Sum_Sq = 0;
      foreach ( var P in Points )
      {
        double D = P.Value - Avg;
        Sum_Sq += D * D;
      }
      double Std_Dev = Math.Sqrt ( Sum_Sq / Points.Count );

      TimeSpan Duration = Points.Count >= 2
        ? Points [ Points.Count - 1 ].Time - Points [ 0 ].Time
        : TimeSpan.Zero;

      double Rate = Duration.TotalSeconds > 0
        ? ( Points.Count - 1 ) / Duration.TotalSeconds
        : 0;

      double Avg_Interval_Ms = 0;
      if ( Points.Count >= 2 )
      {
        double Total_Ms = 0;
        for ( int I = 1; I < Points.Count; I++ )
          Total_Ms += ( Points [ I ].Time - Points [ I - 1 ].Time ).TotalMilliseconds;
        Avg_Interval_Ms = Total_Ms / ( Points.Count - 1 );
      }

      return (Min, Max, Avg, Std_Dev, Range, Duration, Rate, Avg_Interval_Ms);
    }


    // ========================================================================
    // SCROLLBAR RANGE MANAGEMENT
    // Call after data changes, after loading, and in Finish_Reading/Polling
    // ========================================================================

    public static void Update_Scrollbar_Range (
      HScrollBar Pan_Scrollbar,
      int Total_Points,
      int Max_Display_Points,
      bool Auto_Scroll,
      ref int View_Offset )
    {
      if ( Pan_Scrollbar == null )
        return;

      int Max_Offset = Math.Max ( 0, Total_Points - Max_Display_Points );

      if ( Max_Offset <= 0 || Total_Points <= Max_Display_Points )
      {
        Pan_Scrollbar.Minimum = 0;
        Pan_Scrollbar.Maximum = 100;
        Pan_Scrollbar.LargeChange = 101;
        Pan_Scrollbar.Value = 0;
        Pan_Scrollbar.Enabled = false;
        return;
      }

      int Large_Change = Math.Max ( 5, Max_Offset / 20 );
      Pan_Scrollbar.Minimum = 0;
      Pan_Scrollbar.Maximum = Max_Offset + Large_Change;
      Pan_Scrollbar.LargeChange = Large_Change;
      Pan_Scrollbar.SmallChange = Math.Max ( 1, Max_Offset / 100 );
      Pan_Scrollbar.Enabled = true;

      if ( Auto_Scroll )
      {
        View_Offset = 0;
        Pan_Scrollbar.Value = 0;
      }
      else
      {
        int Max_Value = Pan_Scrollbar.Maximum - Pan_Scrollbar.LargeChange + 1;
        Pan_Scrollbar.Value = Math.Max ( 0, Math.Min ( Pan_Scrollbar.Value, Max_Value ) );
      }
    }


    // ========================================================================
    // VISIBLE RANGE CALCULATION
    // Respects rolling mode and view offset for both forms
    // Single form:  pass _Readings.Count
    // Multi form:   pass individual S.Points.Count per series
    // ========================================================================

    public static (int Start_Index, int Visible_Count) Get_Visible_Range (
      int Total_Count,
      bool Enable_Rolling,
      int Max_Display_Points,
      int View_Offset )
    {
      if ( Total_Count == 0 )
        return (0, 0);

      if ( !Enable_Rolling || Total_Count <= Max_Display_Points )
        return (0, Total_Count);

      int End_Index = Total_Count - View_Offset;
      End_Index = Math.Max ( Max_Display_Points, Math.Min ( Total_Count, End_Index ) );

      int Start_Index = Math.Max ( 0, End_Index - Max_Display_Points );
      int Visible_Count = End_Index - Start_Index;

      return (Start_Index, Visible_Count);
    }

    public static void Update_Performance_Status (
      ToolStripStatusLabel? Performance_Label,
      ToolStripStatusLabel? Memory_Label,
      double Actual_FPS,
      int Total_Points,
      int Current_Points,
      int Max_Points,
      int Warning_Threshold_Percent )
    {
      if ( Performance_Label != null )
      {
        Performance_Label.Text = $"Refresh: {Actual_FPS:F1} FPS | Points: {Total_Points:N0}";
        Performance_Label.ForeColor = Actual_FPS < 5.0 ? Color.Orange : Color.Green;
      }

      if ( Memory_Label != null )
      {
        int Percent = Max_Points > 0 ? ( Current_Points * 100 ) / Max_Points : 0;
        Memory_Label.Text = $"Memory: {Current_Points:N0} / {Max_Points:N0} ({Percent}%)";
        Memory_Label.ForeColor = Percent >= 90
          ? Color.Red
          : Percent >= Warning_Threshold_Percent
            ? Color.Orange
            : Color.Green;
      }
    }

    // ========================================================================
    // RECORDING STATE - START
    // Sets button UI state, returns false if caller should abort
    // ========================================================================

    public static void Start_Recording_UI (
      Button Record_Button )
    {
      Record_Button.Text = "Stop Rec";
      Record_Button.BackColor = Color.IndianRed;
      Record_Button.ForeColor = Color.White;
    }

    public static void Stop_Recording_UI (
      Button Record_Button )
    {
      Record_Button.Text = "Record";
      Record_Button.BackColor = SystemColors.Control;
      Record_Button.ForeColor = SystemColors.ControlText;
    }


    // In Multimeter_Common:

    public static void Stop_Recording (
      ref bool Is_Recording,
      Button Record_Button,
      Func<int> Get_Point_Count,
      Action Save_Recorded_Data )
    {
      Is_Recording = false;
      Stop_Recording_UI ( Record_Button );

      if ( Get_Point_Count ( ) == 0 )
      {
        MessageBox.Show (
          "No data points were captured.",
          "Nothing to Save",
          MessageBoxButtons.OK,
          MessageBoxIcon.Information );
        return;
      }

      Save_Recorded_Data ( );
    }


    // ========================================================================
    // CSV FILE WRITING - SINGLE SERIES
    // Used by Single_Instrument_Poll_Form
    // ========================================================================

    public static void Save_Single_Series_CSV (
      string File_Path,
      string Record_Function,
      string Record_Unit,
      List<(DateTime Time, double Value)> Points,
      Meter_Type Meter )
    {
      var (Min, Max, Avg, Std_Dev, Range, Duration, Rate, Avg_Interval_Ms)
        = Calculate_Stats ( Points );

      using var Writer = new StreamWriter ( File_Path );

      Writer.WriteLine ( $"# Function: {Record_Function}" );
      Writer.WriteLine ( $"# Unit: {Record_Unit}" );
      Writer.WriteLine ( $"# Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" );
      Writer.WriteLine ( $"# Points: {Points.Count}" );
      Writer.WriteLine ( "#" );
      Writer.WriteLine ( "# Statistics:" );
      Writer.WriteLine ( $"#   Average: {Avg:F7} {Record_Unit}" );
      Writer.WriteLine ( $"#   Std Dev: {Std_Dev:F7} {Record_Unit}" );
      Writer.WriteLine ( $"#   Min: {Min:F7} {Record_Unit}" );
      Writer.WriteLine ( $"#   Max: {Max:F7} {Record_Unit}" );
      Writer.WriteLine ( $"#   Range: {Range:F7} {Record_Unit}" );
      Writer.WriteLine ( $"#   Duration: {Format_Time_Span ( Duration )}" );
      Writer.WriteLine ( $"#   Rate: {Rate:F2} S/s" );

      if ( Points.Count >= 2 )
        Writer.WriteLine ( $"#   Avg \u0394t: {Avg_Interval_Ms:F1} ms" );

      Writer.WriteLine ( "#" );
      Writer.WriteLine ( "Timestamp,Value" );

      foreach ( var P in Points )
      {
        Writer.WriteLine (
          $"{P.Time:yyyy-MM-dd HH:mm:ss.fff}," +
          $"{P.Value.ToString ( CultureInfo.InvariantCulture )}" );
      }
    }


    // ========================================================================
    // CSV FILE WRITING - MULTI SERIES
    // Used by Multi_Instrument_Poll_Form
    // ========================================================================

    public static void Save_Multi_Series_CSV (
      string File_Path,
      List<(string Name,
            List<(DateTime Time, double Value)> Points)> Series )
    {
      using var Writer = new StreamWriter ( File_Path );

      int Total_Points = Series.Sum ( s => s.Points.Count );

      Writer.WriteLine ( $"# Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" );
      Writer.WriteLine ( $"# Instruments: {Series.Count}" );
      Writer.WriteLine ( $"# Total Points: {Total_Points}" );
      Writer.WriteLine ( "#" );
      Writer.WriteLine ( "# Statistics:" );

      foreach ( var (Name, Points) in Series )
      {
        if ( Points.Count == 0 )
          continue;

        var (Min, Max, Avg, Std_Dev, Range, Duration, Rate, Avg_Interval_Ms)
          = Calculate_Stats ( Points );

        Writer.WriteLine ( $"# [{Name}]" );
        Writer.WriteLine ( $"#   Points: {Points.Count}" );

        if ( Points.Count >= 2 )
        {
          Writer.WriteLine ( $"#   Average: {Avg:F7}" );
          Writer.WriteLine ( $"#   Std Dev: {Std_Dev:F7}" );
          Writer.WriteLine ( $"#   Min: {Min:F7}" );
          Writer.WriteLine ( $"#   Max: {Max:F7}" );
          Writer.WriteLine ( $"#   Range: {Range:F7}" );
          Writer.WriteLine ( $"#   Duration: {Format_Time_Span ( Duration )}" );
          Writer.WriteLine ( $"#   Rate: {Rate:F2} S/s" );
          Writer.WriteLine ( $"#   Avg \u0394t: {Avg_Interval_Ms:F1} ms" );
        }
        else
        {
          Writer.WriteLine ( $"#   Value: {Points [ 0 ].Value:F7}" );
        }
      }

      Writer.WriteLine ( "#" );

      // Column headers
      Writer.Write ( "Timestamp" );
      foreach ( var (Name, _) in Series )
        Writer.Write ( $",{Name}" );
      Writer.WriteLine ( );

      // Merged timestamp rows
      var All_Timestamps = new SortedSet<DateTime> ( );
      foreach ( var (_, Points) in Series )
        foreach ( var P in Points )
          All_Timestamps.Add ( P.Time );

      foreach ( var Time in All_Timestamps )
      {
        Writer.Write ( $"{Time:yyyy-MM-dd HH:mm:ss.fff}" );

        foreach ( var (_, Points) in Series )
        {
          var Point = Points.FirstOrDefault ( p => p.Time == Time );
          Writer.Write ( Point != default
            ? $",{Point.Value.ToString ( CultureInfo.InvariantCulture )}"
            : "," );
        }
        Writer.WriteLine ( );
      }
    }

    // ========================================================================
    // CSV FILE LOADING - SHARED PARSING
    // Returns parsed header stats, header row index, and all lines
    // Both forms call this then handle the data differently
    // ========================================================================

    public static bool Load_CSV_Preamble (
      string File_Path,
      out string [ ] Lines,
      out int Header_Index,
      out Dictionary<string, string> Flat_Stats,
      out Dictionary<string, Dictionary<string, string>> Sectioned_Stats )
    {
      Lines = Array.Empty<string> ( );
      Header_Index = -1;
      Flat_Stats = new Dictionary<string, string> ( );
      Sectioned_Stats = new Dictionary<string, Dictionary<string, string>> ( );

      if ( !File.Exists ( File_Path ) )
      {
        MessageBox.Show ( "File not found.", "Load Error",
          MessageBoxButtons.OK, MessageBoxIcon.Warning );
        return false;
      }

      Lines = File.ReadAllLines ( File_Path );

      // Parse header comments
      string? Current_Section = null;

      foreach ( string Line in Lines )
      {
        if ( Line.StartsWith ( "# [" ) && Line.EndsWith ( "]" ) )
        {
          // Sectioned stats: "# [Instrument Name]"
          Current_Section = Line.Substring ( 3, Line.Length - 4 );
          Sectioned_Stats [ Current_Section ] = new Dictionary<string, string> ( );
        }
        else if ( Line.StartsWith ( "# Unit:" ) )
        {
          Flat_Stats [ "Unit" ] = Line.Substring ( "# Unit:".Length ).Trim ( );
        }
        else if ( Line.StartsWith ( "#   " ) )
        {
          string Stat = Line.Substring ( 4 ).Trim ( );
          int Colon_Idx = Stat.IndexOf ( ':' );
          if ( Colon_Idx > 0 )
          {
            string Key = Stat.Substring ( 0, Colon_Idx ).Trim ( );
            string Value = Stat.Substring ( Colon_Idx + 1 ).Trim ( );

            // Add to current section if in one, otherwise flat
            if ( Current_Section != null )
              Sectioned_Stats [ Current_Section ] [ Key ] = Value;
            else
              Flat_Stats [ Key ] = Value;
          }
        }
      }

      // Find first non-comment non-empty line (header row)
      for ( int I = 0; I < Lines.Length; I++ )
      {
        if ( !Lines [ I ].StartsWith ( "#" ) && !string.IsNullOrWhiteSpace ( Lines [ I ] ) )
        {
          Header_Index = I;
          break;
        }
      }

      if ( Header_Index < 0 )
      {
        MessageBox.Show ( "No valid data found in file.", "Load Error",
          MessageBoxButtons.OK, MessageBoxIcon.Warning );
        return false;
      }

      return true;
    }


    // ========================================================================
    // CSV DATA ROW PARSING - SINGLE COLUMN
    // For single instrument files: Timestamp,Value
    // ========================================================================

    public static (List<double> Values, List<DateTime> Timestamps)
      Parse_Single_Column_Data ( string [ ] Lines, int Header_Index )
    {
      var Values = new List<double> ( );
      var Timestamps = new List<DateTime> ( );

      for ( int I = Header_Index + 1; I < Lines.Length; I++ )
      {
        string Line = Lines [ I ].Trim ( );
        if ( string.IsNullOrEmpty ( Line ) || Line.StartsWith ( "#" ) )
          continue;

        string [ ] Parts = Line.Split ( ',' );
        if ( Parts.Length < 2 )
          continue;

        if ( DateTime.TryParse ( Parts [ 0 ], out DateTime Timestamp ) )
          Timestamps.Add ( Timestamp );

        if ( double.TryParse ( Parts [ 1 ],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double Value ) )
          Values.Add ( Value );
      }

      return (Values, Timestamps);
    }

    // ========================================================================
    // FPS TRACKING
    // Add one instance per form - fields live in the form, logic in helper
    // ========================================================================

    public static void Track_FPS (
      ref int Paint_Count,
      ref double Actual_FPS,
      Stopwatch FPS_Stopwatch,
      Action Update_Status )
    {
      Paint_Count++;

      if ( FPS_Stopwatch.Elapsed.TotalSeconds >= 1.0 )
      {
        Actual_FPS = Paint_Count / FPS_Stopwatch.Elapsed.TotalSeconds;
        Paint_Count = 0;
        FPS_Stopwatch.Restart ( );
        Update_Status ( );
      }
    }

    // ========================================================================
    // STATUS STRIP INITIALIZATION
    // ========================================================================

    public static (ToolStripStatusLabel Memory_Label,
                   ToolStripStatusLabel Performance_Label)
      Initialize_Status_Strip (
        Form Owner,
        Application_Settings Settings,
        int Instrument_Count = 0 )
    {
      // Remove existing strip if present
      var Existing = Owner.Controls.OfType<StatusStrip> ( ).FirstOrDefault ( );
      if ( Existing != null )
        Owner.Controls.Remove ( Existing );

      var Status_Strip = new StatusStrip { Name = "Status_Strip" };

      var Memory_Label = new ToolStripStatusLabel
      {
        Name = "Memory_Status_Label",
        Text = "Memory: 0 / 0 (0%)",
        BorderSides = ToolStripStatusLabelBorderSides.Right,
        AutoSize = true
      };
      Status_Strip.Items.Add ( Memory_Label );

      var Performance_Label = new ToolStripStatusLabel
      {
        Name = "Performance_Status_Label",
        Text = $"Refresh: {1000.0 / Settings.Chart_Refresh_Rate_Ms:F1} FPS | Points: 0",
        BorderSides = ToolStripStatusLabelBorderSides.Right,
        AutoSize = true
      };
      Status_Strip.Items.Add ( Performance_Label );

      // Only shown in multi form when Instrument_Count > 0
      if ( Instrument_Count > 0 )
      {
        var Instrument_Label = new ToolStripStatusLabel
        {
          Text = $"Instruments: {Instrument_Count}",
          BorderSides = ToolStripStatusLabelBorderSides.Right,
          AutoSize = true
        };
        Status_Strip.Items.Add ( Instrument_Label );
      }

      Status_Strip.Items.Add ( new ToolStripStatusLabel { Spring = true } );

      Owner.Controls.Add ( Status_Strip );

      return (Memory_Label, Performance_Label);
    }


   

    public static Chart_Theme Get_Next_Theme ( Chart_Theme Current )
    {
      var Themes = new [ ]
      {
    Chart_Theme.Dark_Preset  ( ),
    Chart_Theme.Light_Preset ( )
  };

      for ( int I = 0; I < Themes.Length; I++ )
      {
        if ( Themes [ I ].Name == Current.Name )
          return Themes [ ( I + 1 ) % Themes.Length ];
      }

      return Themes [ 0 ];
    }


    public static async Task<List<string>> Scan_For_Prologix (
     string Subnet,
     int Timeout_Ms = 200,
     IProgress<(int Current, int Total)>? Progress = null,
     Action<string>? Trace = null )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Trace?.Invoke ( $"Scanning subnet {Subnet}.1-254 timeout={Timeout_Ms}ms" );

      var Results = new System.Collections.Concurrent.ConcurrentBag<string> ( );
      int Total = 254;
      int Completed = 0;

      var Semaphore = new SemaphoreSlim ( 20 ); // limit concurrency
      var Tasks = Enumerable.Range ( 1, 254 ).Select ( async I =>
      {
        await Semaphore.WaitAsync ( );
        try
        {
          string IP = $"{Subnet}.{I}";
          using var Ping = new Ping ( );
          var Reply = await Ping.SendPingAsync ( IP, Timeout_Ms );

          if ( Reply.Status == IPStatus.Success )
          {
            bool Found_By_DNS = false;
            bool Found_By_TCP = false;

            try
            {
              var Host_Entry = await Dns.GetHostEntryAsync ( IP );
              string Host_Name = Host_Entry.HostName.ToLower ( );

              if ( Host_Name.Contains ( "prologix" ) )
                Found_By_DNS = true;
            }
            catch { }

            if ( !Found_By_DNS )
            {
              try
              {
                using var TCP = new TcpClient ( );
                var Connect_Task = TCP.ConnectAsync ( IP, 1234 );

                if ( await Task.WhenAny ( Connect_Task, Task.Delay ( Timeout_Ms ) ) == Connect_Task &&
                    TCP.Connected )
                {
                  Found_By_TCP = true;
                }
              }
              catch { }
            }

            if ( Found_By_DNS || Found_By_TCP )
              Results.Add ( IP );
          }
        }
        finally
        {
          Semaphore.Release ( );
          int Done = Interlocked.Increment ( ref Completed );
          Progress?.Report ( (Done, Total) );
        }
      } );

      await Task.WhenAll ( Tasks );

      var Found = Results.OrderBy ( IP =>
      {
        int Last_Octet = int.Parse ( IP.Split ( '.' ).Last ( ) );
        return Last_Octet;
      } ).ToList ( );

      Trace?.Invoke ( $"Scan complete. Found {Found.Count} Prologix device(s): {string.Join ( ", ", Found )}" );
      return Found;
    }







    public static int Calculate_Settle_Ms ( string NPLC_Value, double Safety_Factor = 2.0 )
    {
      if ( !double.TryParse ( NPLC_Value, out double NPLC ) || NPLC <= 0 )
        NPLC = 1.0;

      // One power line cycle = 16.67ms at 60Hz
      double Measurement_Ms = NPLC * ( 1000.0 / 60.0 );
      return Math.Max ( 50, (int) ( Measurement_Ms * Safety_Factor ) );
    }

  }
}
