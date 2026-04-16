// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Multimeter_Common_Helpers_Class.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Static utility library shared by every polling form and helper class in the
//   application.  Centralises logic that would otherwise be duplicated between
//   Single_Instrument_Poll_Form and Multi_Instrument_Poll_Form, and provides
//   general-purpose services (formatting, file I/O, statistics, network
//   scanning) used across the broader codebase.
//
// METHOD GROUPS
//
// ── NPLC Command Builder ──────────────────────────────────────────────────────
//
//   Build_NPLC_Command( Measurement_Label, NPLC_Value ) → string?
//     Maps a human-readable measurement label to the correct SCPI NPLC
//     sub-command string, e.g. "DC Voltage" → "VOLT:DC:NPLC {value}".
//     Returns null for measurement types that have no NPLC setting
//     (Frequency, Period, Diode, Continuity).  Used by both polling forms
//     during instrument configuration.
//     Supported labels: DC Voltage, AC Voltage, DC Current, AC Current,
//     2-Wire Ohms (RES:NPLC), 4-Wire Ohms (FRES:NPLC).
//
// ── Time Formatting ───────────────────────────────────────────────────────────
//
//   Format_Time_Span( TimeSpan Span ) → string
//     Converts a TimeSpan to the most readable single-unit string:
//       < 1 s   → "{ms}ms"
//       < 1 min → "{s}s"  (1 decimal)
//       < 1 h   → "{m}m"  (1 decimal)
//       < 1 d   → "{h}h"  (1 decimal)
//       ≥ 1 d   → "{d}d"  (1 decimal)
//     Used in CSV headers, stats panels, and chart axis labels.
//
// ── File / Folder Helpers ─────────────────────────────────────────────────────
//
//   Get_Graph_Captures_Folder( Application_Settings ) → string
//     Resolves Default_Save_Folder to an absolute path:
//       1. If the value is already rooted (absolute), creates and returns it.
//       2. Otherwise walks up the directory tree from AppContext.BaseDirectory
//          looking for an existing folder with that relative name.
//       3. Falls back to creating the folder next to the executable.
//     This strategy finds the project-level captures folder during development
//     while still working correctly in installed deployments.
//
//   Get_Filename_From_Pattern( Pattern, Function_Name ) → string
//     Expands the three supported tokens in a filename pattern:
//       {date}     → "yyyy-MM-dd"
//       {time}     → "HH-mm-ss"
//       {function} → the supplied Function_Name string
//     Appends ".csv" if the result does not already end with that extension.
//
// ── Value Formatting ──────────────────────────────────────────────────────────
//
//   Format_Value( Value, Unit, Meter, Digits ) → string
//     Full-precision engineering-units formatter for display in both forms.
//     Unit-specific behaviour:
//       "Hz"  → MHz / kHz / Hz with 3 decimal places
//       "s"   → ns / us / ms / s with appropriate decimals
//       "Ohm" → MOhm / kOhm / Ohm with 3 decimal places
//       "°C"  → "{F2} °C"
//       "V"/"A" → Auto-ranges to m{Unit} / u{Unit} / n{Unit} / scientific
//                 notation, scaling decimal places with the Digits parameter
//                 (millirange loses 2 digits, microrange 3, nanorange 4).
//     Digits defaults to 6; pass Instrument.Display_Digits for per-instrument
//     precision.  Meter parameter accepted for future meter-specific overrides.
//
// ── Settings Helpers ──────────────────────────────────────────────────────────
//
//   Apply_Common_Settings( Settings, Comm, Auto_Save_Timer,
//                          Chart_Refresh_Timer, Current_Point_Count )
//     Applies the subset of Application_Settings that is identical for both
//     polling forms:
//       • Creates Default_Save_Folder if non-empty.
//       • Sets Comm.Read_Timeout_Ms from Default_GPIB_Timeout_Ms (if Comm
//         is non-null).
//       • Starts or stops Auto_Save_Timer based on Enable_Auto_Save; sets
//         interval to Auto_Save_Interval_Minutes × 60 000 ms.
//       • Sets Chart_Refresh_Timer.Interval; doubles it when throttling is
//         enabled and Current_Point_Count exceeds Throttle_Point_Threshold.
//
//   Calculate_Refresh_Rate( Settings, Total_Points ) → int
//     Returns the correct chart refresh interval in ms using a graduated
//     throttle scale when Throttle_When_Many_Points is enabled:
//       > Threshold × 10  → Base × 4
//       > Threshold × 5   → Base × 3
//       > Threshold × 2   → Base × 2
//       ≤ Threshold        → Base (no throttle)
//
// ── Memory Limit Check ────────────────────────────────────────────────────────
//
//   Check_Memory_Limit( Settings, Get_Point_Count, Stop_Recording,
//                       Show_Warning, ref Warning_Shown )
//     Checks whether the current point count has reached or approached the
//     Max_Data_Points_In_Memory ceiling:
//       • At or above Max → calls Stop_Recording() immediately.
//       • At or above Warning_Threshold_Percent of Max, and warning not yet
//         shown → calls Show_Warning(current, max) and sets Warning_Shown.
//     Accepts delegates so the same logic serves both single-series and
//     multi-series point-count scenarios.
//
// ── CSV Statistics Block Writer ───────────────────────────────────────────────
//
//   Write_Stats_Block( Writer, Series_Name, Point_Count, Min, Max, Avg,
//                      Std_Dev, Range, Duration?, Rate?, Avg_Interval_Ms? )
//     Writes a standardised "# [Series_Name]" comment block to an open
//     StreamWriter.  Optional fields (Duration, Rate, Avg_Interval_Ms) are
//     only written when their Nullable values are non-null.  Format matches
//     the preamble expected by Load_CSV_Preamble().
//
// ── Statistics Calculator ─────────────────────────────────────────────────────
//
//   Calculate_Stats( List<(DateTime Time, double Value)> Points )
//     → (Min, Max, Avg, Std_Dev, Range, Duration, Rate, Avg_Interval_Ms)
//     Single-pass computation of descriptive statistics over a timestamped
//     point list:
//       Min / Max / Avg / Range    Straightforward single-pass accumulation.
//       Std_Dev                    Population standard deviation
//                                  (√(Σ(xi − μ)² / N)).
//       Duration                   Last.Time − First.Time; Zero for < 2 points.
//       Rate                       (N − 1) / Duration.TotalSeconds; 0 if
//                                  Duration is zero.
//       Avg_Interval_Ms            Mean of consecutive time-deltas in ms;
//                                  0 for < 2 points.
//     Returns all-zero tuple for null or empty input.
//
// ── Scrollbar Range Management ────────────────────────────────────────────────
//
//   Update_Scrollbar_Range( Pan_Scrollbar, Total_Points, Max_Display_Points,
//                           Auto_Scroll, ref View_Offset )
//     Configures an HScrollBar for chart panning:
//       • When Total_Points ≤ Max_Display_Points: disables the scrollbar
//         (Maximum = 100, LargeChange = 101 effectively hides the thumb).
//       • Otherwise: sets Maximum = Max_Offset + LargeChange − 1 so the
//         thumb occupies the correct proportion of the track.
//         SmallChange = Max_Display_Points / 100 (fine step).
//         LargeChange = Max_Display_Points / 10  (page step).
//       • When Auto_Scroll is true, resets Value and View_Offset to 0
//         (newest data always visible).
//       • When Auto_Scroll is false, clamps the existing Value to the new
//         valid range without moving the viewport.
//     Detailed trace output is written for each scrollbar parameter.
//
// ── Visible Range Calculation ─────────────────────────────────────────────────
//
//   Get_Visible_Range( Total_Count, Enable_Rolling, Max_Display_Points,
//                      View_Offset ) → (Start_Index, Visible_Count)
//     Returns the slice of the data list that should be drawn:
//       • Total_Count ≤ Max_Display_Points → (0, Total_Count) — show all.
//       • Enable_Rolling false → (Total − Max, Max) — show tail, no panning.
//       • Enable_Rolling true  → honours View_Offset for scrolled panning;
//         End_Index = Total − View_Offset, clamped to [Max, Total].
//     Used by chart paint handlers in both polling forms.
//
// ── Performance Status ────────────────────────────────────────────────────────
//
//   Update_Performance_Status( Performance_Label, Memory_Label, Actual_FPS,
//                              Total_Points, Current_Points, Max_Points,
//                              Warning_Threshold_Percent, Is_Decimating )
//     Updates the two status-strip labels on every chart repaint:
//       Performance_Label  "Refresh: N.N FPS | Points: N (of N stored)"
//                          when Total_Points ≠ Current_Points (decimation
//                          active), otherwise omits the "(of N stored)" part.
//                          Color: Orange when FPS < 5 AND decimation is off;
//                          Green otherwise (low FPS during decimation is
//                          intentional and should not be flagged).
//       Memory_Label       "Memory: N / N (N%)"
//                          Color: Red ≥ 90%, Orange ≥ Warning_Threshold_Percent,
//                          Green otherwise.
//
// ── Recording State UI ────────────────────────────────────────────────────────
//
//   Start_Recording_UI( Record_Button )
//     Sets button Text = "Stop Rec", BackColor = IndianRed, ForeColor = White.
//
//   Stop_Recording_UI( Record_Button )
//     Restores button to system default colors and Text = "Record".
//
//   Stop_Recording( ref Is_Recording, Record_Button, Get_Point_Count,
//                   Save_Recorded_Data )
//     Sets Is_Recording = false, calls Stop_Recording_UI(), then either shows
//     a "Nothing to Save" MessageBox (if Get_Point_Count() returns 0) or
//     calls Save_Recorded_Data().
//
// ── CSV File Writing ──────────────────────────────────────────────────────────
//
//   Save_Single_Series_CSV( File_Path, Record_Function, Record_Unit,
//                           Points, Meter )
//     Writes a single-instrument CSV file:
//       Preamble  # comment block with Function, Unit, capture time,
//                 point count, and full statistics block.
//       Header    "Timestamp,Value"
//       Data      "yyyy-MM-dd HH:mm:ss.fff,{InvariantCulture value}"
//     Avg_Δt line is omitted for single-point recordings.
//
//   Save_Multi_Series_CSV( File_Path, Series )
//     Writes a multi-instrument CSV with one column per instrument:
//       Preamble  Per-series statistics blocks; single-point series get a
//                 condensed "Value:" line instead of full stats.
//       Header    "Timestamp,Name1,Name2,..."
//       Data      All unique timestamps merged into a SortedSet; missing
//                 values for a given timestamp are written as empty fields.
//
// ── CSV File Loading ──────────────────────────────────────────────────────────
//
//   Load_CSV_Preamble( File_Path ) → Task<CSV_Preamble_Result?>
//     Asynchronously reads all lines and parses the # comment preamble into:
//       Flat_Stats        Key-value pairs from top-level "#   Key: Value" lines
//                         and "# Unit:" / "# Measurement:" shorthand lines.
//       Sectioned_Stats   Per-series dictionaries keyed by "# [SectionName]"
//                         header lines; subsequent "#   Key: Value" lines are
//                         added to the active section.
//       Header_Index      Zero-based index of the first non-comment,
//                         non-empty line (the CSV column header row).
//       Lines             The full line array for subsequent parsing.
//     Returns null and shows a MessageBox if the file is missing or contains
//     no valid data rows.
//
//   CSV_Preamble_Result  (inner class)
//     Immutable result bag: Lines, Header_Index, Flat_Stats,
//     Sectioned_Stats.  Passed to the per-form data parsers.
//
//   Parse_Single_Column_Data( Lines, Header_Index )
//     → (List<double> Values, List<DateTime> Timestamps)
//     Parses data rows from a single-instrument CSV (Timestamp,Value).
//     Skips blank lines, comment lines, and rows with fewer than 2 fields.
//     Uses InvariantCulture for double parsing.
//
// ── FPS Tracking ──────────────────────────────────────────────────────────────
//
//   Track_FPS( ref Paint_Count, ref Actual_FPS, FPS_Stopwatch,
//              Update_Status )
//     Increments Paint_Count on every call.  When the stopwatch crosses 1
//     second, computes Actual_FPS = Paint_Count / elapsed, resets both
//     counter and stopwatch, and invokes Update_Status() so the caller can
//     refresh the status strip.  Fields live in the calling form; only the
//     computation logic lives here.
//
// ── Status Strip Initialization ───────────────────────────────────────────────
//
//   Initialize_Status_Strip( Owner, Settings, Instrument_Count )
//     → (Memory_Label, Performance_Label)
//     Removes any existing StatusStrip named "Status_Strip" from the owner,
//     creates a new one, and adds:
//       Memory_Label        "Memory: 0 / 0 (0%)"
//       Performance_Label   "Refresh: N FPS | Points: 0" (rate from Settings)
//       Instrument_Label    "Instruments: N" — added only when
//                           Instrument_Count > 0 (multi-instrument form only)
//       Spring label        Right-aligns subsequent items.
//     Returns the two labels so the owner can update them on repaint.
//
// ── Theme Cycling ─────────────────────────────────────────────────────────────
//
//   Get_Next_Theme( Current ) → Chart_Theme
//     Cycles through [Dark, Light] by name match.  Returns Dark if the
//     current theme name is not found in the list.
//
// ── Prologix Network Scanner ──────────────────────────────────────────────────
//
//   Scan_For_Prologix( Subnet, Timeout_Ms, Progress, Trace )
//     → Task<List<string>>
//     Scans all 254 host addresses on the given subnet (e.g. "192.168.1")
//     concurrently using a SemaphoreSlim(50) to cap parallelism.  For each
//     address, Is_Prologix() attempts a TCP connection on port 1234 and sends
//     "++ver\n"; a response containing "Prologix" (case-insensitive) confirms
//     the device.  ICMP ping is deliberately skipped — Prologix adapters may
//     not respond to ICMP.  Results are returned sorted by the last octet.
//     Progress is reported via IProgress<(Current, Total)>; detailed trace
//     output is sent to the optional Trace action.
//
//   Is_Prologix( IP, Port, Timeout_Ms, Trace ) → Task<bool>   [private]
//     Opens a TcpClient with CancellationToken timeout, flushes any pending
//     data, sends "++ver\n", and reads the response with a secondary
//     Task.WhenAny timeout guard.  Returns false on any exception or timeout.
//
// ── Instrument Type Resolution ────────────────────────────────────────────────
//
//   Get_Meter_Type( Name ) → Meter_Type
//     Maps a model-number substring in the instrument name to a Meter_Type
//     enum value.  Checked in order: 34401, 33120, 34420, 53132, 3458.
//     Returns Meter_Type.Generic_GPIB if no pattern matches.
//
// ── NPLC Settle Time Calculation ─────────────────────────────────────────────
//
//   Calculate_Settle_Ms( NPLC_Value, Safety_Factor ) → int
//     Computes the minimum settle delay for a given NPLC setting:
//       Measurement_Ms = NPLC × (1000 / 60)   (one 60 Hz power-line cycle)
//       Result = max(50, (int)(Measurement_Ms × Safety_Factor))
//     Safety_Factor defaults to 2.0.  Returns 50 ms minimum for very low
//     NPLC values or unparseable strings.
//
// NOTES
//   • All methods are static; the class is never instantiated.
//   • CSV values are always written and parsed with InvariantCulture to
//     prevent locale-dependent decimal separator issues.
//   • Scan_For_Prologix does not require administrator privileges; it uses
//     only outbound TCP connections.
//   • Write_Stats_Block and the two Save_*_CSV methods produce preambles
//     in the exact format consumed by Load_CSV_Preamble, so files written
//     by one method can be round-tripped through the other.
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════



using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
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

    public static string Get_Graph_Captures_Folder ( Application_Settings Settings )
    {

      using var Block = Trace_Block.Start_If_Enabled();

      string Folder = Settings.Default_Save_Folder;

      // If user gave an absolute path, use it directly
      if (Path.IsPathRooted( Folder ))
      {
        Directory.CreateDirectory( Folder );
        Capture_Trace.Write( $"  → Using rooted path: [{Folder}]" );
        return Folder;
      }

      // Otherwise walk up from BaseDirectory to find project root
      string Base = AppContext.BaseDirectory;
      string? Dir = Base;

      while ( Dir != null )
      {
        string Candidate = Path.Combine ( Dir, Folder );
        if ( Directory.Exists ( Candidate ) )
          return Candidate;
        Dir = Directory.GetParent ( Dir )?.FullName;
      }

      // Fallback: create next to executable
      string Fallback = Path.Combine ( Base, Folder );
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
      Meter_Type Meter = Meter_Type.HP34401,
      int Digits = 6 )
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

      // V or A — use Digits to drive precision
      int D = Digits;
      int D2 = Math.Max ( 0, Digits - 2 );  // millirange loses 2 integer digits
      int D3 = Math.Max ( 0, Digits - 3 );  // microrange loses 3
      int D4 = Math.Max ( 0, Digits - 4 );  // nanorange loses 4

      if ( Abs >= 1.0 )
        return $"{Value.ToString ( $"F{D}" )} {Unit}";
      if ( Abs >= 0.001 )
        return $"{( Value * 1000 ).ToString ( $"F{D2}" )} m{Unit}";
      if ( Abs >= 0.000001 )
        return $"{( Value * 1e6 ).ToString ( $"F{D3}" )} u{Unit}";
      if ( Abs >= 0.000000001 )
        return $"{( Value * 1e9 ).ToString ( $"F{D4}" )} n{Unit}";

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
      using var Block = Trace_Block.Start_If_Enabled ( );

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

      int Large_Change = Math.Max ( 1, Max_Display_Points / 10 );  // thumb represents viewport size
      Pan_Scrollbar.Minimum = 0;
      Pan_Scrollbar.Maximum = Max_Offset + Large_Change - 1;
      Pan_Scrollbar.LargeChange = Large_Change;
      Pan_Scrollbar.SmallChange = Math.Max ( 1, Max_Display_Points / 100 );
      Pan_Scrollbar.Enabled = true;

      Capture_Trace.Write ( $"Scrollbar:" );
      Capture_Trace.Write ( $"  Total       = {Total_Points}" );
      Capture_Trace.Write ( $"  Max_Display = {Max_Display_Points}" );
      Capture_Trace.Write ( $"  Max_Offset  = {Max_Offset}" );
      Capture_Trace.Write ( $"  Min         = {Pan_Scrollbar.Minimum}" );
      Capture_Trace.Write ( $"  Max         = {Pan_Scrollbar.Maximum}" );
      Capture_Trace.Write ( $"  LargeChange = {Pan_Scrollbar.LargeChange}" );
      Capture_Trace.Write ( $"  Value       = {Pan_Scrollbar.Value}" );
      Capture_Trace.Write ( $"  Enabled     = {Pan_Scrollbar.Enabled}" );



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

    public static (int Start_Index, int Visible_Count) Get_Visible_Range(
    int Total_Count,
    bool Enable_Rolling,
    int Max_Display_Points,
    int View_Offset )
    {
      if (Total_Count == 0)
        return (0, 0);

      // Always respect Max_Display_Points — show all only if within limit
      if (Total_Count <= Max_Display_Points)
        return (0, Total_Count);

      // Rolling off — show the last Max_Display_Points with no offset
      if (!Enable_Rolling)
      {
        int Start = Math.Max( 0, Total_Count - Max_Display_Points );
        return (Start, Math.Min( Max_Display_Points, Total_Count - Start ));
      }

      // Rolling on — respect View_Offset for panning
      int End_Index = Total_Count - View_Offset;
      End_Index = Math.Max( Max_Display_Points,
                     Math.Min( Total_Count, End_Index ) );

      int Start_Index = Math.Max( 0, End_Index - Max_Display_Points );
      int Visible_Count = End_Index - Start_Index;

      return (Start_Index, Visible_Count);
    }
    public static void Update_Performance_Status(
     ToolStripStatusLabel? Performance_Label,
     ToolStripStatusLabel? Memory_Label,
     double Actual_FPS,
     int Total_Points,
     int Current_Points,
     int Max_Points,
     int Warning_Threshold_Percent,
     bool Is_Decimating = false )        // ← add this
    {
      if (Performance_Label != null)
      {
        Performance_Label.Text = Total_Points != Current_Points
          ? $"Refresh: {Actual_FPS:F1} FPS | Points: {Total_Points:N0} (of {Current_Points:N0} stored)"
          : $"Refresh: {Actual_FPS:F1} FPS | Points: {Total_Points:N0}";

        // Don't flag low FPS as a problem when decimation is intentionally reducing repaint rate
        Performance_Label.ForeColor = !Is_Decimating && Actual_FPS < 5.0 ? Color.Orange : Color.Green;
      }

      if (Memory_Label != null)
      {
        int Percent = Max_Points > 0 ? (Current_Points * 100) / Max_Points : 0;
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


    public class CSV_Preamble_Result
    {
      public string [ ] Lines
      {
        get; init;
      }
      public int Header_Index
      {
        get; init;
      }
      public Dictionary<string, string> Flat_Stats
      {
        get; init;
      }
      public Dictionary<string, Dictionary<string, string>> Sectioned_Stats
      {
        get; init;
      }
    }




    public static async Task<CSV_Preamble_Result?> Load_CSV_Preamble ( string File_Path )
    {
      if ( !File.Exists ( File_Path ) )
      {
        MessageBox.Show ( "File not found.", "Load Error",
          MessageBoxButtons.OK, MessageBoxIcon.Warning );
        return null;
      }

      string [ ] Lines = await File.ReadAllLinesAsync ( File_Path );

      int Header_Index = -1;
      var Flat_Stats = new Dictionary<string, string> ( );
      var Sectioned_Stats = new Dictionary<string, Dictionary<string, string>> ( );
      string? Current_Section = null;

      foreach ( string Line in Lines )
      {
        if ( Line.StartsWith ( "# [" ) && Line.EndsWith ( "]" ) )
        {
          Current_Section = Line.Substring ( 3, Line.Length - 4 );
          Sectioned_Stats [ Current_Section ] = new Dictionary<string, string> ( );
        }
        else if ( Line.StartsWith ( "# Unit" ) )
        {
          int Colon = Line.IndexOf ( ':' );
          if ( Colon > 0 )
            Flat_Stats [ "Unit" ] = Line.Substring ( Colon + 1 ).Trim ( );
        }
        else if ( Line.StartsWith ( "# Measurement" ) )
        {
          int Colon = Line.IndexOf ( ':' );
          if ( Colon > 0 )
            Flat_Stats [ "Measurement" ] = Line.Substring ( Colon + 1 ).Trim ( );
        }
        else if ( Line.StartsWith ( "#   " ) )
        {
          string Stat = Line.Substring ( 4 ).Trim ( );
          int Colon_Idx = Stat.IndexOf ( ':' );
          if ( Colon_Idx > 0 )
          {
            string Key = Stat.Substring ( 0, Colon_Idx ).Trim ( );
            string Value = Stat.Substring ( Colon_Idx + 1 ).Trim ( );
            if ( Current_Section != null )
              Sectioned_Stats [ Current_Section ] [ Key ] = Value;
            else
              Flat_Stats [ Key ] = Value;
          }
        }
      }

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
        return null;
      }

      return new CSV_Preamble_Result
      {
        Lines = Lines,
        Header_Index = Header_Index,
        Flat_Stats = Flat_Stats,
        Sectioned_Stats = Sectioned_Stats
      };
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
    int Timeout_Ms = 500,
    IProgress<(int Current, int Total)>? Progress = null,
    Action<string>? Trace = null )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Trace?.Invoke ( $"Scanning subnet {Subnet}.1-254 timeout={Timeout_Ms}ms" );

      var Results = new ConcurrentBag<string> ( );
      int Total = 254;
      int Completed = 0;
      var Semaphore = new SemaphoreSlim ( 50 );

      var Tasks = Enumerable.Range ( 1, 254 ).Select ( async I =>
      {
        await Semaphore.WaitAsync ( );
        try
        {
          string IP = $"{Subnet}.{I}";
          Trace?.Invoke ( $"  Trying {IP}" );

          // Skip ping - Prologix may not respond to ICMP
          // Go straight to TCP connect on port 1234
          if ( await Is_Prologix ( IP, 1234, Timeout_Ms, Trace ) )
          {
            Trace?.Invoke ( $"  ✓ Confirmed Prologix at {IP}" );
            Results.Add ( IP );
          }
        }
        catch { }
        finally
        {
          Semaphore.Release ( );
          Progress?.Report ( (Interlocked.Increment ( ref Completed ), Total) );
        }
      } );

      await Task.WhenAll ( Tasks );

      var Found = Results
        .OrderBy ( IP => int.Parse ( IP.Split ( '.' ).Last ( ) ) )
        .ToList ( );

      Trace?.Invoke ( $"Scan complete. Found {Found.Count} device(s): {string.Join ( ", ", Found )}" );
      return Found;
    }

    private static async Task<bool> Is_Prologix (
        string IP, int Port, int Timeout_Ms,
        Action<string>? Trace = null )
    {
      try
      {

        using var TCP = new TcpClient ( );
        using var CTS = new CancellationTokenSource ( Timeout_Ms );

        try
        {
          await TCP.ConnectAsync ( IP, Port, CTS.Token );
        }
        
        catch ( Exception Ex )
        {
          return false;
        }

        if ( !TCP.Connected )
          return false;

        using var Stream = TCP.GetStream ( );
        Stream.ReadTimeout = Timeout_Ms;
        Stream.WriteTimeout = Timeout_Ms;

        // Flush anything pending
        byte [ ] Flush_Buf = new byte [ 256 ];
        while ( Stream.DataAvailable )
          await Stream.ReadAsync ( Flush_Buf, 0, Flush_Buf.Length );

        // Send ++ver
        byte [ ] Cmd = Encoding.ASCII.GetBytes ( "++ver\n" );
        await Stream.WriteAsync ( Cmd, 0, Cmd.Length );

        // Read response safely
        byte [ ] Buf = new byte [ 256 ];
        var Read_Task = Stream.ReadAsync ( Buf, 0, Buf.Length );
        if ( await Task.WhenAny ( Read_Task, Task.Delay ( Timeout_Ms ) ) != Read_Task )
          return false;

        int Bytes = Read_Task.Result;
        string Response = Encoding.ASCII.GetString ( Buf, 0, Bytes ).Trim ( );
        Trace?.Invoke ( $"  {IP} ver response: '{Response}'" );

        if ( await Task.WhenAny ( Read_Task, Task.Delay ( Timeout_Ms ) ) != Read_Task )
        {
          Trace?.Invoke ( $"  {IP} read timed out" );
          return false;
        }

        return Response.Contains ( "Prologix", StringComparison.OrdinalIgnoreCase );
      }
      catch
      {
        return false;
      }
    }




    public static Meter_Type Get_Meter_Type ( string Name )
    {
      if ( Name.Contains ( "34401" ) )
        return Meter_Type.HP34401;
      if ( Name.Contains ( "33120" ) )
        return Meter_Type.HP33120;
      if ( Name.Contains ( "34420" ) )
        return Meter_Type.HP34420;
      if ( Name.Contains ( "53132" ) )
        return Meter_Type.HP53132;
      if ( Name.Contains ( "3458" ) )
        return Meter_Type.HP3458;
      return Meter_Type.Generic_GPIB;
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
