// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Multimeter_Common_Helpers_Class.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Static utility library shared by every form and subsystem in the
//   application.  Centralises logic that would otherwise be duplicated between
//   Single_Instrument_Poll_Form and Multi_Instrument_Poll_Form, including
//   NPLC command building, value formatting, CSV I/O, statistics, chart
//   refresh throttling, scrollbar management, network scanning, and UI helpers.
//
// ── INSTRUMENT CONFIGURATION ─────────────────────────────────────────────────
//
//   Build_NPLC_Command(measurementLabel, nplcValue)
//     Maps a human-readable measurement label (e.g. "DC Voltage") to the
//     correct SCPI NPLC command string.  Returns null for functions that
//     have no NPLC parameter (Freq, Period, Diode, Continuity).
//
//   Calculate_Settle_Ms(nplcValue, safetyFactor)
//     Converts an NPLC string to a settle delay in milliseconds
//     (NPLC × 16.67 ms × safetyFactor), clamped to a minimum of 50 ms.
//     Default safety factor of 2.0 accounts for autozero.
//
//   Get_Meter_Type(name)
//     Substring-matches an instrument ID string to a Meter_Type enum value.
//     Falls back to Generic_GPIB for unrecognised strings.
//
// ── VALUE FORMATTING ─────────────────────────────────────────────────────────
//
//   Format_Value(value, unit, meter, digits)
//     Full-precision SI-prefix formatter for chart labels and stats panels.
//     Handles: Hz (MHz/kHz/Hz), s (ns/µs/ms/s), Ohm (MOhm/kOhm/Ohm),
//     °C, and V/A (with milli/micro/nano prefix scaling driven by Digits).
//     Falls back to G2 scientific notation for values below 1 nV/nA.
//
//   Format_Time_Span(span)
//     Human-readable duration string: ms / s / m / h / d, one decimal place.
//
// ── STATISTICS ───────────────────────────────────────────────────────────────
//
//   Calculate_Stats(points)
//     Single-pass O(n) computation of Min, Max, Avg, Std_Dev (population),
//     Range, Duration, sample Rate (S/s), and Avg_Interval_Ms from a
//     List<(DateTime, double)>.  Returns a named tuple; zero-fills on empty input.
//
// ── CSV FILE I/O ─────────────────────────────────────────────────────────────
//
//   Save_Single_Series_CSV(filePath, function, unit, points, meter)
//     Writes a commented preamble (function, unit, capture time, statistics)
//     followed by "Timestamp,Value" rows in InvariantCulture format.
//
//   Save_Multi_Series_CSV(filePath, series)
//     Writes a preamble with per-series statistics blocks, then a merged
//     wide-format table: one column per instrument, rows aligned to the
//     union of all timestamps.  Missing values are written as empty cells.
//
//   Write_Stats_Block(writer, …)
//     Shared helper that writes a "#  Key: Value" statistics block for one
//     series into an already-open StreamWriter.
//
//   Load_CSV_Preamble(filePath)   [async]
//     Parses the "#" comment preamble of any application CSV into a
//     CSV_Preamble_Result containing:
//       Lines[]          — all raw lines
//       Header_Index     — index of the first non-comment, non-blank line
//       Flat_Stats       — key/value pairs from un-sectioned "# Key: Value" lines
//       Sectioned_Stats  — per-instrument stats from "# [Name]" sections
//     Returns null and shows a MessageBox on file-not-found or no-data.
//
//   Parse_Single_Column_Data(lines, headerIndex)
//     Parses "Timestamp,Value" data rows below the preamble into parallel
//     List<double> and List<DateTime>.  Skips blank and comment lines.
//
// ── CHART / DISPLAY HELPERS ───────────────────────────────────────────────────
//
//   Calculate_Refresh_Rate(settings, totalPoints)
//     Returns the appropriate timer interval in ms, applying the step-wise
//     throttle multiplier (×2/×3/×4) when totalPoints exceeds the configured
//     threshold.
//
//   Get_Visible_Range(totalCount, enableRolling, maxDisplayPoints, viewOffset)
//     Computes (Start_Index, Visible_Count) for the currently visible slice
//     of a point list, respecting rolling-window mode and pan offset.
//     Used by both forms and by Base_Chart_Form draw helpers.
//
//   Track_FPS(ref paintCount, ref actualFps, stopwatch, updateStatus)
//     Increments paintCount and recalculates FPS once per second via the
//     supplied Stopwatch.  Calls updateStatus() after each recalculation.
//
//   Update_Performance_Status(perfLabel, memLabel, fps, …)
//     Updates ToolStripStatusLabel text and ForeColor for the FPS/points
//     display (orange below 5 FPS) and the memory usage display
//     (orange ≥ warning threshold, red ≥ 90%).
//
//   Get_Next_Theme(current)
//     Cycles through Dark → Light → Dark presets by name match.
//
// ── SCROLLBAR MANAGEMENT ──────────────────────────────────────────────────────
//
//   Update_Scrollbar_Range(scrollbar, totalPoints, maxDisplayPoints,
//                          autoScroll, ref viewOffset)
//     Configures HScrollBar Minimum/Maximum/LargeChange/SmallChange for the
//     current point count.  Disables the scrollbar when all points fit in the
//     window.  Resets Value to 0 when autoScroll is true.
//
// ── SETTINGS APPLICATION ──────────────────────────────────────────────────────
//
//   Apply_Common_Settings(settings, comm, autoSaveTimer, chartTimer, pointCount)
//     Applies settings shared by both forms: save-folder creation, GPIB
//     read timeout, auto-save timer start/stop, and chart refresh throttling.
//
// ── RECORDING UI HELPERS ──────────────────────────────────────────────────────
//
//   Start_Recording_UI(button)   Sets button to "Stop Rec" / red.
//   Stop_Recording_UI(button)    Resets button to "Record" / system colors.
//   Stop_Recording(…)            Stops recording, resets UI, shows "no data"
//                                dialog if point count is zero, otherwise
//                                calls the supplied Save_Recorded_Data action.
//
// ── MEMORY LIMIT CHECK ────────────────────────────────────────────────────────
//
//   Check_Memory_Limit(settings, getCount, stopRecording,
//                      showWarning, ref warningShown)
//     Calls stopRecording() if the point count reaches Max_Data_Points_In_Memory.
//     Calls showWarning() once when the configurable warning threshold is crossed.
//
// ── STATUS STRIP INITIALIZATION ───────────────────────────────────────────────
//
//   Initialize_Status_Strip(owner, settings, instrumentCount)
//     Removes any existing StatusStrip, builds Memory and Performance labels,
//     optionally adds an Instruments count label (multi-form only), and
//     docks the strip to the form.  Returns the two mutable labels.
//
// ── FILE / FOLDER HELPERS ─────────────────────────────────────────────────────
//
//   Get_Graph_Captures_Folder(settings)
//     Resolves Default_Save_Folder to an absolute path.  Absolute paths are
//     used directly; relative paths are searched upward from BaseDirectory
//     for an existing folder, falling back to creating one beside the executable.
//
//   Get_Filename_From_Pattern(pattern, functionName)
//     Expands {date}, {time}, {function} tokens in a filename pattern and
//     appends ".csv" if not already present.
//
// ── NETWORK SCANNING ─────────────────────────────────────────────────────────
//
//   Scan_For_Prologix(subnet, timeoutMs, progress, trace)   [async]
//     Scans subnet.1–254 in parallel (50 concurrent) via TCP port 1234.
//     Confirms each responding host by sending "++ver\n" and checking that
//     the response contains "Prologix".  Returns discovered IPs sorted by
//     last octet.  Reports progress via IProgress<(int, int)>.
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════


using System;
using System.Collections.Concurrent;
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
