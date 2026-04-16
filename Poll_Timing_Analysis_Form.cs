
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Poll_Timing_Analysis_Form.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Standalone analysis dialog for poll-timing CSV files produced by the
//   polling thread.  Parses the CSV into Poll_Timing_Analysis records and
//   presents five GDI+ chart tabs giving a complete picture of cycle-time
//   behaviour, phase breakdown, distribution, and error rate.
//
// ── CLASS: Poll_Timing_Analysis ──────────────────────────────────────────────
//
//   Data record (one row per CSV line) with fields:
//     Timestamp      DateTime  — wall-clock time of the cycle.
//     Cycle          int       — monotonic cycle counter.
//     Total_Ms       double    — complete cycle wall time.
//     Comm_Ms        double    — time spent in serial/USB communication.
//     AddrSwitch_Ms  double    — time spent switching GPIB addresses.
//     UI_Ms          double    — time spent in Invoke / UI updates.
//     Record_Ms      double    — time spent writing to the data stream.
//     Had_Error      bool      — true if the field value is not "0".
//
//   Load_CSV(path)
//     Reads the file line-by-line, skips the header, and parses each
//     comma-separated row into a Poll_Timing_Analysis.  Malformed rows
//     are silently skipped.  Requires ≥ 8 fields per row.
//
// ── CLASS: Poll_Timing_Analysis_Form ─────────────────────────────────────────
//
//   Entry point
//     Show_From_File(owner, theme)   Static method that shows an OpenFileDialog,
//                                    loads the CSV via Poll_Timing_Analysis.Load_CSV(),
//                                    validates that at least 2 records were parsed,
//                                    and shows the form modally.
//
//   Constructor
//     Poll_Timing_Analysis_Form(records, theme)
//       Pre-extracts five parallel double lists from _Records (_Total_Ms,
//       _Comm_Ms, _AddrSwitch_Ms, _UI_Ms, _Record_Ms) and a DateTime list
//       (_Times).  Computes a TimingStat summary for each column.  Builds a
//       100-sample rolling σ of Total_Ms (Welford online, identical to
//       Analysis_Popup_Form).  Calls Build_UI().
//
//   TABS
//     0  Total Cycle Time   Line + fill of Total_Ms over time; gold dashed
//                           mean line; red dot markers on error cycles.
//                           Title shows mean, σ, max, count, error count.
//     1  Time Breakdown     Stacked filled-area chart of the four sub-phases
//                           (Comm / AddrSwitch / UI / Record) per cycle.
//                           Inline color legend in the top-left margin.
//     2  Summary Stats      GDI+ text table: one row per phase showing
//                           Mean / σ / Min / Max / % of Total; followed by
//                           effective sample rate, error count and rate.
//     3  Distribution       Sturges-rule histogram of Total_Ms (8–50 bins);
//                           overlaid fitted normal curve; mean and ±1σ/±2σ
//                           markers; 99th-percentile value in the title.
//     4  Errors             Vertical bar per cycle — red for Had_Error,
//                           transparent blue for normal; bar height = Total_Ms.
//                           Overlay text with error count, rate, and first
//                           error timestamp.
//
//   SEGMENT COLOURS  (static readonly)
//     C_Comm    RGB(100,160,240) — blue
//     C_Addr    RGB(255,180, 60) — gold
//     C_UI      RGB(100,220,130) — green
//     C_Record  RGB(200,110,200) — purple
//     C_Error   RGB(255, 80, 80) — red
//
//   LAYOUT CONSTANTS
//     ML=80  MR=30  MT=34  MB=44  (pixels; MB is taller to fit the stats strip)
//
//   HELP SYSTEM
//     _Tab_Help[]          One plain-text string per tab; same bullet/section
//                          parse format as Analysis_Popup_Form.
//     Show_Tab_Help()      Builds a scrollable FlowLayoutPanel dialog with
//                          section headers (blue accent bar) and bullet rows
//                          (colored dot); shown modally from the "?" button.
//
//   SHARED DRAWING HELPERS
//     Setup_Graphics()     AntiAlias + ClearTypeGridFit + background fill.
//     Draw_Grid()          Six horizontal grid lines with formatted Y labels.
//     Build_Points()       Maps List<double> → PointF[] by index fraction.
//     Draw_Title()         Centred bold Segoe UI 8.5pt title in top margin.
//     Draw_Time_Axis()     Up to 8 time labels and vertical grid lines on X
//                          axis indexed into _Times[] by fractional position.
//     Percentile()         Sorts a copy of the list and interpolates the
//                          requested percentile (used for 99th pct in title).
//
//   PRIVATE HELPER CLASS: TimingStat
//     Immutable value object computed from a List<double> in the constructor.
//     Exposes Mean, StdDev (population), Min, Max, Range.  One instance is
//     created per timing column during form construction.
//
// NOTES
//   • All five tab panels are Buffered_Panel instances; Paint handlers are
//     wired in Build_UI() and the active panel is invalidated on tab change.
//   • The form has no designer file; all controls are constructed inline.
//   • Draw_Breakdown() builds stacked cumulative arrays using Zip() and
//     renders each layer as a filled polygon between the top of the current
//     phase and the top of the previous phase.
//   • Error markers in Draw_Timeline() iterate the full record list so marker
//     X positions match the corresponding line-chart points exactly.
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════





using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Drawing.Drawing2D;

namespace Multimeter_Controller
  {
    // ══════════════════════════════════════════════════════════════════════════
    //  Data record — one row of the CSV
    // ══════════════════════════════════════════════════════════════════════════
    public class Poll_Timing_Analysis
    {
      public DateTime Timestamp
      {
        get; set;
      }
      public int Cycle
      {
        get; set;
      }
      public double Total_Ms
      {
        get; set;
      }
      public double Comm_Ms
      {
        get; set;
      }
      public double AddrSwitch_Ms
      {
        get; set;
      }
      public double UI_Ms
      {
        get; set;
      }
      public double Record_Ms
      {
        get; set;
      }
      public bool Had_Error
      {
        get; set;
      }

      // ── CSV parser ────────────────────────────────────────────────
      // Expected header:
      //   Timestamp,Cycle,Total_Ms,Comm_Ms,AddrSwitch_Ms,UI_Ms,Record_Ms,Had_Error
      public static List<Poll_Timing_Analysis> Load_CSV ( string Path )
      {
        var Records = new List<Poll_Timing_Analysis> ( );
        bool First = true;

        foreach ( string Raw_Line in File.ReadLines ( Path ) )
        {
          if ( First )
          {
            First = false;
            continue;
          }           // skip header

          string Line = Raw_Line.Trim ( );
          if ( string.IsNullOrEmpty ( Line ) )
            continue;

          string [ ] F = Line.Split ( ',' );
          if ( F.Length < 8 )
            continue;

          try
          {
            Records.Add ( new Poll_Timing_Analysis
            {
              Timestamp = DateTime.Parse ( F [ 0 ].Trim ( ), CultureInfo.InvariantCulture ),
              Cycle = int.Parse ( F [ 1 ].Trim ( ) ),
              Total_Ms = double.Parse ( F [ 2 ].Trim ( ), CultureInfo.InvariantCulture ),
              Comm_Ms = double.Parse ( F [ 3 ].Trim ( ), CultureInfo.InvariantCulture ),
              AddrSwitch_Ms = double.Parse ( F [ 4 ].Trim ( ), CultureInfo.InvariantCulture ),
              UI_Ms = double.Parse ( F [ 5 ].Trim ( ), CultureInfo.InvariantCulture ),
              Record_Ms = double.Parse ( F [ 6 ].Trim ( ), CultureInfo.InvariantCulture ),
              Had_Error = F [ 7 ].Trim ( ) != "0",
            } );
          }
          catch { /* skip malformed rows */ }
        }

        return Records;
      }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Poll_Timing_Analysis_Form
    // ══════════════════════════════════════════════════════════════════════════
    public partial class Poll_Timing_Analysis_Form : Form
    {
      // ── Data ──────────────────────────────────────────────────────
      private readonly List<Poll_Timing_Analysis> _Records;
      private readonly Chart_Theme _Theme;

      // ── Pre-extracted columns ─────────────────────────────────────
      private readonly List<double> _Total_Ms;
      private readonly List<double> _Comm_Ms;
      private readonly List<double> _AddrSwitch_Ms;
      private readonly List<double> _UI_Ms;
      private readonly List<double> _Record_Ms;
      private readonly List<DateTime> _Times;
      private readonly int _Error_Count;

      // ── Summary stats ─────────────────────────────────────────────
      private readonly TimingStat _Total_Stat;
      private readonly TimingStat _Comm_Stat;
      private readonly TimingStat _Addr_Stat;
      private readonly TimingStat _UI_Stat;
      private readonly TimingStat _Rec_Stat;

      // ── Rolling σ of Total_Ms ─────────────────────────────────────
      private readonly List<double> _Rolling_StdDev = new ( );

      // ── UI ────────────────────────────────────────────────────────
      private TabControl _Tabs;
      private Panel _Timeline_Panel;
      private Panel _Breakdown_Panel;
      private Panel _Stats_Panel;
      private Panel _Histogram_Panel;
      private Panel _Errors_Panel;

      // ── Layout constants ──────────────────────────────────────────
      private const int ML = 80, MR = 30, MT = 34, MB = 44;

      // ── Segment colours (stacked bar) ─────────────────────────────
      private static readonly Color C_Comm = Color.FromArgb ( 100, 160, 240 );
      private static readonly Color C_Addr = Color.FromArgb ( 255, 180, 60 );
      private static readonly Color C_UI = Color.FromArgb ( 100, 220, 130 );
      private static readonly Color C_Record = Color.FromArgb ( 200, 110, 200 );
      private static readonly Color C_Error = Color.FromArgb ( 255, 80, 80 );

      // ══════════════════════════════════════════════════════════════
      //  Constructor
      // ══════════════════════════════════════════════════════════════
      public Poll_Timing_Analysis_Form (
          List<Poll_Timing_Analysis> Records,
          Chart_Theme Theme )
      {
        _Records = Records;
        _Theme = Theme;

        _Total_Ms = Records.Select ( R => R.Total_Ms ).ToList ( );
        _Comm_Ms = Records.Select ( R => R.Comm_Ms ).ToList ( );
        _AddrSwitch_Ms = Records.Select ( R => R.AddrSwitch_Ms ).ToList ( );
        _UI_Ms = Records.Select ( R => R.UI_Ms ).ToList ( );
        _Record_Ms = Records.Select ( R => R.Record_Ms ).ToList ( );
        _Times = Records.Select ( R => R.Timestamp ).ToList ( );
        _Error_Count = Records.Count ( R => R.Had_Error );

        _Total_Stat = new TimingStat ( _Total_Ms );
        _Comm_Stat = new TimingStat ( _Comm_Ms );
        _Addr_Stat = new TimingStat ( _AddrSwitch_Ms );
        _UI_Stat = new TimingStat ( _UI_Ms );
        _Rec_Stat = new TimingStat ( _Record_Ms );

        // ── Rolling σ (100-sample window) ─────────────────────────
        const int Window = 100;
        var Q = new Queue<double> ( );
        double R_Sum = 0, R_Sum_Sq = 0;
        foreach ( double D in _Total_Ms )
        {
          Q.Enqueue ( D );
          R_Sum += D;
          R_Sum_Sq += D * D;
          if ( Q.Count > Window )
          {
            double Old = Q.Dequeue ( );
            R_Sum -= Old;
            R_Sum_Sq -= Old * Old;
          }
          int N = Q.Count;
          double Mu = R_Sum / N;
          double Var = ( R_Sum_Sq / N ) - ( Mu * Mu );
          _Rolling_StdDev.Add ( Var > 0 ? Math.Sqrt ( Var ) : 0.0 );
        }

        Build_UI ( );
      }

      // ══════════════════════════════════════════════════════════════
      //  Static entry point: open file-picker then show form
      // ══════════════════════════════════════════════════════════════
      public static void Show_From_File ( IWin32Window Owner, Chart_Theme Theme )
      {
        using var Dlg = new OpenFileDialog
        {
          Title = "Open Poll Timing CSV",
          Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
          Multiselect = false,
        };
        if ( Dlg.ShowDialog ( Owner ) != DialogResult.OK )
          return;

        List<Poll_Timing_Analysis> Records;
        try
        {
          Records = Poll_Timing_Analysis.Load_CSV ( Dlg.FileName );
        }
        catch ( Exception Ex )
        {
          MessageBox.Show ( $"Failed to load CSV:\n{Ex.Message}",
              "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
          return;
        }

        if ( Records.Count < 2 )
        {
          MessageBox.Show ( "CSV contained fewer than 2 valid rows.",
              "Insufficient Data", MessageBoxButtons.OK, MessageBoxIcon.Warning );
          return;
        }

        using var Form = new Poll_Timing_Analysis_Form ( Records, Theme );
        Form.ShowDialog ( Owner );
      }

      // ══════════════════════════════════════════════════════════════
      //  Tab help text
      // ══════════════════════════════════════════════════════════════
      private static readonly string [ ] _Tab_Help =
      {
            // 0 — Total Cycle Time
            "Total Cycle Time — Over Time\n\n" +
            "Each point represents one complete poll cycle's total duration in milliseconds.\n\n" +
            "• Blue line — raw Total_Ms per cycle.\n" +
            "• Dashed gold — mean cycle time.\n" +
            "• Red markers — cycles that had an error.\n\n" +
            "What to look for:\n" +
            "  – A flat, stable line means consistent poll pacing.\n" +
            "  – Spikes indicate slow cycles (timeouts, retries, GC pauses).\n" +
            "  – Rising trend suggests growing latency over time.",

            // 1 — Stacked Breakdown
            "Time Breakdown — Stacked Area\n\n" +
            "Shows how each cycle's time is split between the four sub-phases, stacked on top of each other.\n\n" +
            "• Blue  — Comm_Ms      (serial/USB communication)\n" +
            "• Gold  — AddrSwitch_Ms (address switching overhead)\n" +
            "• Green — UI_Ms        (UI update time)\n" +
            "• Purple— Record_Ms    (data recording time)\n\n" +
            "What to look for:\n" +
            "  – Which phase dominates the total budget?\n" +
            "  – Do proportions shift over time (growing Comm cost, etc.)?\n" +
            "  – Spikes in one segment that don't appear in others.",

            // 2 — Summary Stats
            "Summary Statistics\n\n" +
            "Numeric summary of every timing column across the full run.\n\n" +
            "  • Mean — average duration in ms\n" +
            "  • σ    — standard deviation; lower = more consistent\n" +
            "  • Min / Max / Range — extremes\n" +
            "  • % of Total — how much of the cycle budget each phase consumes on average\n\n" +
            "Also shows error rate and total sample count.",

            // 3 — Histogram
            "Total_Ms Distribution — Histogram\n\n" +
            "Shows how often each cycle-time bucket occurred.\n\n" +
            "• Blue bars — histogram of Total_Ms.\n" +
            "• Smooth curve — fitted normal distribution.\n" +
            "• Dashed gold — mean; dotted lines — ±1σ / ±2σ.\n\n" +
            "What to look for:\n" +
            "  – A tight, symmetric bell is ideal.\n" +
            "  – A long right tail means occasional slow outliers.\n" +
            "  – Multiple humps suggest the meter switching between timing regimes.",

            // 4 — Errors
            "Error Timeline\n\n" +
            "Shows which cycles triggered Had_Error = 1.\n\n" +
            "• Red bars mark error cycles; height = Total_Ms of that cycle.\n" +
            "• Grey bars show normal cycles for context.\n" +
            "• Summary text shows total error count and rate.\n\n" +
            "What to look for:\n" +
            "  – Are errors clustered in time (bus contention, interference)?\n" +
            "  – Do error cycles take longer than normal cycles?\n" +
            "  – Is error rate trending upward?",
        };

      // ══════════════════════════════════════════════════════════════
      //  UI construction
      // ══════════════════════════════════════════════════════════════
      private void Build_UI ( )
      {
        Text = "Poll Timing Analysis";
        Size = new Size ( 960, 640 );
        MinimumSize = new Size ( 720, 520 );
        StartPosition = FormStartPosition.CenterParent;
        Padding = new Padding ( 0 );

        _Tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point ( 10, 3 ) };

        _Timeline_Panel = new Buffered_Panel ( );
        _Breakdown_Panel = new Buffered_Panel ( );
        _Stats_Panel = new Buffered_Panel ( );
        _Histogram_Panel = new Buffered_Panel ( );
        _Errors_Panel = new Buffered_Panel ( );

        _Timeline_Panel.Paint += ( s, e ) => Draw_Timeline ( e.Graphics );
        _Breakdown_Panel.Paint += ( s, e ) => Draw_Breakdown ( e.Graphics );
        _Stats_Panel.Paint += ( s, e ) => Draw_Stats ( e.Graphics );
        _Histogram_Panel.Paint += ( s, e ) => Draw_Histogram ( e.Graphics );
        _Errors_Panel.Paint += ( s, e ) => Draw_Errors ( e.Graphics );

        Add_Tab ( "Total Cycle Time", _Timeline_Panel );
        Add_Tab ( "Time Breakdown", _Breakdown_Panel );
        Add_Tab ( "Summary Stats", _Stats_Panel );
        Add_Tab ( "Distribution", _Histogram_Panel );
        Add_Tab ( "Errors", _Errors_Panel );

        // ── Bottom bar ────────────────────────────────────────────
        var Bottom = new Panel { Dock = DockStyle.Bottom, Height = 36 };

        var Help_Btn = new Button
        {
          Text = "?  What am I looking at?",
          Size = new Size ( 180, 26 ),
          Location = new Point ( 8, 5 ),
          Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
        };
      Help_Btn.Click += ( s, e ) =>
      {
        int Idx = _Tabs.SelectedIndex;
        if ( Idx < 0 || Idx >= _Tab_Help.Length )
          return;
        Show_Tab_Help ( _Tabs.TabPages [ Idx ].Text, _Tab_Help [ Idx ] );
      };

      var Close_Btn = new Button
        {
          Text = "Close",
          Size = new Size ( 80, 26 ),
          Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };
        Close_Btn.Click += ( s, e ) => Close ( );

        Bottom.Controls.Add ( Help_Btn );
        Bottom.Controls.Add ( Close_Btn );
        Bottom.Resize += ( s, e ) =>
            Close_Btn.Location = new Point ( Bottom.Width - 90, 5 );
        Close_Btn.Location = new Point ( Bottom.Width - 90, 5 );

        Controls.Add ( _Tabs );
        Controls.Add ( Bottom );

        _Tabs.SelectedIndexChanged += ( s, e ) =>
            ( _Tabs.SelectedTab?.Controls [ 0 ] as Panel )?.Invalidate ( );
      }

      private void Add_Tab ( string Title, Panel Content )
      {
        var Page = new TabPage ( Title );
        Content.Dock = DockStyle.Fill;
        Page.Controls.Add ( Content );
        _Tabs.TabPages.Add ( Page );
      }

      // ══════════════════════════════════════════════════════════════
      //  Tab 0 — Total cycle time over time
      // ══════════════════════════════════════════════════════════════
      private void Draw_Timeline ( Graphics G )
      {
        int W = _Timeline_Panel.ClientSize.Width;
        int H = _Timeline_Panel.ClientSize.Height;
        Setup_Graphics ( G, W, H );

        int Count = _Total_Ms.Count;
        int Chart_W = W - ML - MR;
        int Chart_H = H - MT - MB;
        if ( Count < 2 || Chart_W < 20 || Chart_H < 20 )
          return;

        double Y_Min = Math.Max ( 0, _Total_Stat.Min * 0.85 );
        double Y_Max = _Total_Stat.Max * 1.10;
        double Y_Range = Y_Max - Y_Min;
        if ( Y_Range < 1e-9 )
          Y_Range = 1.0;

        Draw_Grid ( G, W, H, Chart_W, Chart_H, Y_Min, Y_Range, "ms",
            V => V.ToString ( "F1" ) );

        // Mean line
        float Mean_Y = H - MB - (float) ( ( _Total_Stat.Mean - Y_Min ) / Y_Range * Chart_H );
        using var Mean_Pen = new Pen ( Color.Gold, 1.5f ) { DashStyle = DashStyle.Dash };
        G.DrawLine ( Mean_Pen, ML, Mean_Y, W - MR, Mean_Y );

        // Fill under curve
        var Pts = Build_Points ( _Total_Ms, Count, W, H, Chart_W, Chart_H, Y_Min, Y_Range );
        using var Fill = new LinearGradientBrush (
            new PointF ( 0, MT ), new PointF ( 0, H - MB ),
            Color.FromArgb ( 50, C_Comm ), Color.FromArgb ( 5, C_Comm ) );
        var Fill_Pts = new PointF [ Count + 2 ];
        Array.Copy ( Pts, Fill_Pts, Count );
        Fill_Pts [ Count ] = new PointF ( Pts [ Count - 1 ].X, H - MB );
        Fill_Pts [ Count + 1 ] = new PointF ( Pts [ 0 ].X, H - MB );
        G.FillPolygon ( Fill, Fill_Pts );

        using var Line_Pen = new Pen ( C_Comm, 1.5f );
        G.DrawLines ( Line_Pen, Pts );

        // Error markers
        if ( _Error_Count > 0 )
        {
          using var Err_Brush = new SolidBrush ( Color.FromArgb ( 200, C_Error ) );
          for ( int I = 0; I < Count; I++ )
          {
            if ( !_Records [ I ].Had_Error )
              continue;
            G.FillEllipse ( Err_Brush, Pts [ I ].X - 3, Pts [ I ].Y - 3, 6, 6 );
          }
        }

        Draw_Title ( G, W,
            $"Total Cycle Time  |  mean: {_Total_Stat.Mean:F2} ms  " +
            $"σ: {_Total_Stat.StdDev:F2} ms  " +
            $"max: {_Total_Stat.Max:F1} ms  " +
            $"n: {Count:N0}  errors: {_Error_Count}" );
        Draw_Time_Axis ( G, W, H, Chart_W, Count );
      }

      // ══════════════════════════════════════════════════════════════
      //  Tab 1 — Stacked area breakdown
      // ══════════════════════════════════════════════════════════════
      private void Draw_Breakdown ( Graphics G )
      {
        int W = _Breakdown_Panel.ClientSize.Width;
        int H = _Breakdown_Panel.ClientSize.Height;
        Setup_Graphics ( G, W, H );

        int Count = _Records.Count;
        int Chart_W = W - ML - MR;
        int Chart_H = H - MT - MB;
        if ( Count < 2 || Chart_W < 20 || Chart_H < 20 )
          return;

        // Use the max of stacked totals for Y scale
        double Y_Max = _Records
            .Select ( R => R.Comm_Ms + R.AddrSwitch_Ms + R.UI_Ms + R.Record_Ms )
            .Max ( ) * 1.10;
        double Y_Range = Y_Max > 0 ? Y_Max : 1.0;

        Draw_Grid ( G, W, H, Chart_W, Chart_H, 0, Y_Range, "ms",
            V => V.ToString ( "F1" ) );

        // Build stacked point arrays (bottom to top)
        PointF [ ] Build_Stack ( List<double> Layer, List<double> Base ) =>
            Enumerable.Range ( 0, Count )
                .Select ( I => new PointF (
                    ML + (float) I / ( Count - 1 ) * Chart_W,
                    H - MB - (float) ( ( Base [ I ] + Layer [ I ] - 0 ) / Y_Range * Chart_H ) ) )
                .ToArray ( );

        var Base_Zero = Enumerable.Repeat ( 0.0, Count ).ToList ( );
        var Stack_Comm = _Comm_Ms;
        var Stack_Addr = _Comm_Ms.Zip ( _AddrSwitch_Ms, ( A, B ) => A + B ).ToList ( );
        var Stack_UI = Stack_Addr.Zip ( _UI_Ms, ( A, B ) => A + B ).ToList ( );
        var Stack_Rec = Stack_UI.Zip ( _Record_Ms, ( A, B ) => A + B ).ToList ( );

        PointF [ ] Top_Comm = Build_Stack ( Stack_Comm, Base_Zero );
        PointF [ ] Top_Addr = Build_Stack ( _AddrSwitch_Ms, Stack_Comm );
        PointF [ ] Top_UI = Build_Stack ( _UI_Ms, Stack_Addr );
        PointF [ ] Top_Rec = Build_Stack ( _Record_Ms, Stack_UI );

        PointF [ ] Bot_Zero = Enumerable.Range ( 0, Count )
            .Select ( I => new PointF ( ML + (float) I / ( Count - 1 ) * Chart_W, H - MB ) )
            .ToArray ( );

        void Fill_Layer ( PointF [ ] Top, PointF [ ] Bot, Color C )
        {
          var Poly = new PointF [ Count * 2 ];
          Array.Copy ( Top, 0, Poly, 0, Count );
          for ( int I = 0; I < Count; I++ )
            Poly [ Count + I ] = Bot [ Count - 1 - I ];
          using var Brush = new SolidBrush ( Color.FromArgb ( 160, C ) );
          G.FillPolygon ( Brush, Poly );
          using var Pen = new Pen ( Color.FromArgb ( 220, C ), 1f );
          G.DrawLines ( Pen, Top );
        }

        Fill_Layer ( Top_Comm, Bot_Zero, C_Comm );
        Fill_Layer ( Top_Addr, Top_Comm, C_Addr );
        Fill_Layer ( Top_UI, Top_Addr, C_UI );
        Fill_Layer ( Top_Rec, Top_UI, C_Record );

        // Legend
        Draw_Stacked_Legend ( G, W, H );

        Draw_Title ( G, W, "Cycle Time Breakdown (stacked)  —  Comm / AddrSwitch / UI / Record" );
        Draw_Time_Axis ( G, W, H, Chart_W, Count );
      }

      private void Draw_Stacked_Legend ( Graphics G, int W, int H )
      {
        var Items = new (Color C, string Label) [ ]
        {
                ( C_Comm,   "Comm"       ),
                ( C_Addr,   "AddrSwitch" ),
                ( C_UI,     "UI"         ),
                ( C_Record, "Record"     ),
        };
        using var F = new Font ( "Consolas", 8f );
        int X = ML + 8;
        int Y = MT + 6;
        foreach ( var (C, Lbl) in Items )
        {
          using var B = new SolidBrush ( Color.FromArgb ( 200, C ) );
          G.FillRectangle ( B, X, Y, 12, 10 );
          using var TB = new SolidBrush ( _Theme.Labels );
          G.DrawString ( Lbl, F, TB, X + 16, Y - 1 );
          X += 90;
        }
      }

      // ══════════════════════════════════════════════════════════════
      //  Tab 2 — Summary stats
      // ══════════════════════════════════════════════════════════════
      private void Draw_Stats ( Graphics G )
      {
        int W = _Stats_Panel.ClientSize.Width;
        int H = _Stats_Panel.ClientSize.Height;
        G.Clear ( SystemColors.Control );

        using var Title_Font = new Font ( "Segoe UI", 12f, FontStyle.Bold );
        using var Header_Font = new Font ( "Segoe UI", 10f, FontStyle.Bold );
        using var Val_Font = new Font ( "Courier New", 10.5f );
        using var Lbl_Font = new Font ( "Segoe UI", 10f );
        using var Fg = new SolidBrush ( SystemColors.ControlText );
        using var Dim = new SolidBrush ( SystemColors.GrayText );
        using var Sep_Pen = new Pen ( _Theme.Grid, 1f );

        int C1 = 40, C2 = 200, C3 = 330, C4 = 460, C5 = 560, C6 = 680;
        int Y = 26;
        int Row = 28;

        string Run_Start = _Times.First ( ).ToString ( "yyyy-MM-dd  HH:mm:ss" );
        string Run_End = _Times.Last ( ).ToString ( "HH:mm:ss" );
        TimeSpan Duration = _Times.Last ( ) - _Times.First ( );

        G.DrawString ( $"Poll Timing Analysis  —  {Run_Start} → {Run_End}  " +
                       $"({Duration.TotalSeconds:F1} s)", Title_Font, Fg, C1, Y );
        Y += Row + 6;

        // Column headers
        G.DrawString ( "Phase", Header_Font, Fg, C1, Y );
        G.DrawString ( "Mean ms", Header_Font, Fg, C2, Y );
        G.DrawString ( "σ ms", Header_Font, Fg, C3, Y );
        G.DrawString ( "Min ms", Header_Font, Fg, C4, Y );
        G.DrawString ( "Max ms", Header_Font, Fg, C5, Y );
        G.DrawString ( "% Total", Header_Font, Fg, C6, Y );
        Y += Row - 4;
        G.DrawLine ( Sep_Pen, C1, Y, W - 40, Y );
        Y += 8;

        void Stat_Row ( string Name, TimingStat S, double Mean_Total )
        {
          double Pct = Mean_Total > 0 ? S.Mean / Mean_Total * 100 : 0;
          G.DrawString ( Name, Lbl_Font, Dim, C1, Y );
          G.DrawString ( S.Mean.ToString ( "F2" ), Val_Font, Fg, C2, Y );
          G.DrawString ( S.StdDev.ToString ( "F2" ), Val_Font, Fg, C3, Y );
          G.DrawString ( S.Min.ToString ( "F2" ), Val_Font, Fg, C4, Y );
          G.DrawString ( S.Max.ToString ( "F2" ), Val_Font, Fg, C5, Y );
          G.DrawString ( $"{Pct:F1} %", Val_Font, Fg, C6, Y );
          Y += Row;
        }

        double T_Mean = _Total_Stat.Mean;
        Stat_Row ( "Total", _Total_Stat, T_Mean );
        Stat_Row ( "Comm", _Comm_Stat, T_Mean );
        Stat_Row ( "AddrSwitch", _Addr_Stat, T_Mean );
        Stat_Row ( "UI", _UI_Stat, T_Mean );
        Stat_Row ( "Record", _Rec_Stat, T_Mean );

        Y += 6;
        G.DrawLine ( Sep_Pen, C1, Y, W - 40, Y );
        Y += 14;

        // Timing & errors
        double Effective_Rate_Hz = _Records.Count > 1
            ? ( _Records.Count - 1 ) /
              ( _Times.Last ( ) - _Times.First ( ) ).TotalSeconds
            : 0;

        void Info_Row ( string Label, string Value )
        {
          G.DrawString ( Label, Lbl_Font, Dim, C1, Y );
          G.DrawString ( Value, Val_Font, Fg, C2, Y );
          Y += Row;
        }

        Info_Row ( "Sample count", $"{_Records.Count:N0}" );
        Info_Row ( "Effective rate", $"{Effective_Rate_Hz:F3} cycles / sec" );
        Info_Row ( "Error cycles", $"{_Error_Count:N0}  ({(double) _Error_Count / _Records.Count * 100:F2} %)" );
        Info_Row ( "Cycles with no error", $"{_Records.Count - _Error_Count:N0}" );
      }

      // ══════════════════════════════════════════════════════════════
      //  Tab 3 — Histogram of Total_Ms
      // ══════════════════════════════════════════════════════════════
      private void Draw_Histogram ( Graphics G )
      {
        int W = _Histogram_Panel.ClientSize.Width;
        int H = _Histogram_Panel.ClientSize.Height;
        Setup_Graphics ( G, W, H );

        int Count = _Total_Ms.Count;
        int Chart_W = W - ML - MR;
        int Chart_H = H - MT - MB;
        if ( Count < 5 || Chart_W < 20 || Chart_H < 20 )
          return;

        double V_Min = _Total_Stat.Min;
        double V_Max = _Total_Stat.Max;
        double Range = V_Max - V_Min;
        if ( Range < 1e-9 )
        {
          Range = 1.0;
          V_Min -= 0.5;
          V_Max += 0.5;
        }

        int Num_Bins = Math.Clamp (
            (int) Math.Ceiling ( 1.0 + 3.322 * Math.Log10 ( Count ) ), 8, 50 );
        double Bin_W = Range / Num_Bins;
        var Bins = new int [ Num_Bins ];
        foreach ( double V in _Total_Ms )
          Bins [ Math.Clamp ( (int) ( ( V - V_Min ) / Bin_W ), 0, Num_Bins - 1 ) ]++;

        int Max_Count = Math.Max ( 1, Bins.Max ( ) );
        double Y_Max_Val = Max_Count * 1.15;

        using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
        using var Label_Font = new Font ( "Consolas", 7.5f );
        using var Label_Brush = new SolidBrush ( _Theme.Labels );

        for ( int I = 0; I <= 5; I++ )
        {
          double Frac = (double) I / 5;
          int Gy = H - MB - (int) ( Frac * Chart_H );
          G.DrawLine ( Grid_Pen, ML, Gy, W - MR, Gy );
          string Lbl = ( (int) Math.Round ( Frac * Y_Max_Val ) ).ToString ( );
          var Sz = G.MeasureString ( Lbl, Label_Font );
          G.DrawString ( Lbl, Label_Font, Label_Brush,
              ML - Sz.Width - 4, Gy - Sz.Height / 2 );
        }

        float Bar_Spacing = (float) Chart_W / Num_Bins;
        float Bar_Draw_W = Bar_Spacing * 0.85f;
        using var Bar_Brush = new SolidBrush ( Color.FromArgb ( 160, C_Comm ) );
        using var Bar_Pen = new Pen ( Color.FromArgb ( 220, C_Comm ), 1f );

        for ( int I = 0; I < Num_Bins; I++ )
        {
          if ( Bins [ I ] == 0 )
            continue;
          float Bar_H = (float) ( Bins [ I ] / Y_Max_Val * Chart_H );
          float Bx = ML + I * Bar_Spacing;
          float By = H - MB - Bar_H;
          G.FillRectangle ( Bar_Brush, Bx, By, Bar_Draw_W, Bar_H );
          G.DrawRectangle ( Bar_Pen, Bx, By, Bar_Draw_W, Bar_H );
        }

        // Bell curve
        double Mean = _Total_Stat.Mean;
        double StdDev = _Total_Stat.StdDev;
        if ( StdDev > 1e-9 )
        {
          double Scale = Bin_W * Count;
          int Np = 200;
          var Curve = new PointF [ Np ];
          for ( int I = 0; I < Np; I++ )
          {
            double XV = V_Min + (double) I / ( Np - 1 ) * Range;
            double Z = ( XV - Mean ) / StdDev;
            double PDF = ( 1.0 / ( StdDev * Math.Sqrt ( 2 * Math.PI ) ) )
                           * Math.Exp ( -0.5 * Z * Z );
            float Px = ML + (float) ( ( XV - V_Min ) / Range * Chart_W );
            float Py = H - MB - (float) ( PDF * Scale / Y_Max_Val * Chart_H );
            Curve [ I ] = new PointF ( Px, Py );
          }
          using var Curve_Pen = new Pen ( _Theme.Line_Colors.Length > 1
              ? _Theme.Line_Colors [ 1 ] : Color.Orange, 2.5f );
          G.DrawLines ( Curve_Pen, Curve );

          float Mx = ML + (float) ( ( Mean - V_Min ) / Range * Chart_W );
          using var Mean_Pen = new Pen ( Color.Gold, 2f ) { DashStyle = DashStyle.Dash };
          G.DrawLine ( Mean_Pen, Mx, MT, Mx, H - MB );

          using var Sig_Pen = new Pen ( Color.FromArgb ( 120, Color.Gold ), 1.5f )
          {
            DashStyle = DashStyle.Dot
          };
          using var Sig_Font = new Font ( "Consolas", 7f );
          using var Sig_Brush = new SolidBrush ( Color.Gold );
          foreach ( var (Mult, Lbl) in new [ ] {
                    (-2, "-2σ"), (-1, "-1σ"), (1, "+1σ"), (2, "+2σ") } )
          {
            double Sv = Mean + Mult * StdDev;
            if ( Sv < V_Min || Sv > V_Max )
              continue;
            float Sx = ML + (float) ( ( Sv - V_Min ) / Range * Chart_W );
            G.DrawLine ( Sig_Pen, Sx, MT, Sx, H - MB );
            G.DrawString ( Lbl, Sig_Font, Sig_Brush, Sx + 2, MT + 2 );
          }
        }

        // X-axis labels
        using var X_Font = new Font ( "Consolas", 6.5f );
        int Label_Step = Math.Max ( 1, Num_Bins / 8 );
        for ( int I = 0; I < Num_Bins; I += Label_Step )
        {
          double Center = V_Min + ( I + 0.5 ) * Bin_W;
          string Lbl = $"{Center:F1}";
          var Sz = G.MeasureString ( Lbl, X_Font );
          float X = ML + I * Bar_Spacing + Bar_Spacing / 2;
          G.DrawString ( Lbl, X_Font, Label_Brush, X - Sz.Width / 2, H - MB + 4 );
        }

        Draw_Title ( G, W,
            $"Total_Ms Distribution  |  mean: {Mean:F2} ms  σ: {StdDev:F2} ms  " +
            $"n: {Count:N0}  99th pct: {Percentile ( _Total_Ms, 99 ):F1} ms" );
      }

      // ══════════════════════════════════════════════════════════════
      //  Tab 4 — Error timeline
      // ══════════════════════════════════════════════════════════════
      private void Draw_Errors ( Graphics G )
      {
        int W = _Errors_Panel.ClientSize.Width;
        int H = _Errors_Panel.ClientSize.Height;
        Setup_Graphics ( G, W, H );

        int Count = _Records.Count;
        int Chart_W = W - ML - MR;
        int Chart_H = H - MT - MB;
        if ( Count < 2 || Chart_W < 20 || Chart_H < 20 )
          return;

        double Y_Max = _Total_Stat.Max * 1.10;
        double Y_Range = Y_Max > 0 ? Y_Max : 1.0;

        Draw_Grid ( G, W, H, Chart_W, Chart_H, 0, Y_Range, "ms",
            V => V.ToString ( "F1" ) );

        float Bar_W = Math.Max ( 1f, (float) Chart_W / Count );

        using var Normal_Brush = new SolidBrush ( Color.FromArgb ( 50, C_Comm ) );
        using var Error_Brush = new SolidBrush ( Color.FromArgb ( 200, C_Error ) );

        for ( int I = 0; I < Count; I++ )
        {
          float Bar_H = (float) ( _Records [ I ].Total_Ms / Y_Range * Chart_H );
          float X = ML + (float) I / ( Count - 1 ) * Chart_W - Bar_W / 2;
          float Y = H - MB - Bar_H;
          var B = _Records [ I ].Had_Error ? Error_Brush : Normal_Brush;
          G.FillRectangle ( B, X, Y, Bar_W, Bar_H );
        }

        // Summary text overlay
        double Error_Rate = (double) _Error_Count / Count * 100;
        string Summary =
            $"Error cycles: {_Error_Count:N0} / {Count:N0}  ({Error_Rate:F2} %)   " +
            $"First error: {( _Records.Any ( R => R.Had_Error )
                ? _Records.First ( R => R.Had_Error ).Timestamp.ToString ( "HH:mm:ss.fff" )
                : "none" )}";

        using var Sum_Font = new Font ( "Consolas", 8.5f );
        using var Sum_Brush = new SolidBrush ( _Error_Count > 0 ? C_Error : Color.LightGreen );
        G.DrawString ( Summary, Sum_Font, Sum_Brush, ML + 4, H - MB + 26 );

        Draw_Title ( G, W, "Error Timeline  —  red = Had_Error | grey = normal" );
        Draw_Time_Axis ( G, W, H, Chart_W, Count );
      }

    // ══════════════════════════════════════════════════════════════
    //  Shared helpers
    // ══════════════════════════════════════════════════════════════



    private void Show_Tab_Help ( string Tab_Title, string Help_Text )
    {
      var Dlg = new Form
      {
        Text = $"About: {Tab_Title}",
        Size = new Size ( 520, 480 ),
        MinimumSize = new Size ( 400, 300 ),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.Sizable,
        BackColor = SystemColors.Control,
      };

      var Header = new Panel
      {
        Dock = DockStyle.Top,
        Height = 48,
        BackColor = Color.FromArgb ( 45, 45, 48 ),
      };
      var Header_Label = new Label
      {
        Text = Tab_Title,
        ForeColor = Color.White,
        Font = new Font ( "Segoe UI", 13f, FontStyle.Bold ),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding ( 16, 0, 0, 0 ),
      };
      Header.Controls.Add ( Header_Label );

      var Scroll = new Panel
      {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        Padding = new Padding ( 16, 12, 16, 12 ),
      };

      var Flow = new FlowLayoutPanel
      {
        Dock = DockStyle.Top,
        AutoSize = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Width = 470,
        Padding = new Padding ( 0 ),
      };

      foreach ( string Raw_Line in Help_Text.Split ( '\n' ) )
      {
        string Line = Raw_Line.TrimEnd ( );

        if ( string.IsNullOrWhiteSpace ( Line ) )
        {
          Flow.Controls.Add ( new Panel { Height = 6, Width = 460 } );
          continue;
        }

        bool Is_Bullet = Line.StartsWith ( "  –" ) || Line.StartsWith ( "  •" );
        bool Is_Section = !Is_Bullet
                       && !Line.StartsWith ( " " )
                       && Line.EndsWith ( ":" )
                       && Line.Length < 60;

        if ( Is_Section )
        {
          var Row = new Panel { Width = 460, Height = 28, Margin = new Padding ( 0, 8, 0, 2 ) };
          Row.Controls.Add ( new Panel
          {
            Width = 4,
            Height = 28,
            Location = new Point ( 0, 0 ),
            BackColor = Color.FromArgb ( 0, 122, 204 ),
          } );
          Row.Controls.Add ( new Label
          {
            Text = Line.TrimEnd ( ':' ),
            Font = new Font ( "Segoe UI", 10f, FontStyle.Bold ),
            ForeColor = SystemColors.ControlText,
            Location = new Point ( 12, 4 ),
            AutoSize = true,
          } );
          Flow.Controls.Add ( Row );
        }
        else if ( Is_Bullet )
        {
          bool Is_Dash = Line.StartsWith ( "  –" );
          string Btn_Text = Line.TrimStart ( ).TrimStart ( '–', '•', ' ' ).Trim ( );
          var Row = new Panel { Width = 460, Height = 22, Margin = new Padding ( 0, 1, 0, 1 ) };
          Row.Controls.Add ( new Panel
          {
            Width = 6,
            Height = 6,
            Location = new Point ( 16, 8 ),
            BackColor = Is_Dash
                  ? Color.FromArgb ( 0, 180, 120 )
                  : Color.FromArgb ( 0, 122, 204 ),
          } );
          Row.Controls.Add ( new Label
          {
            Text = Btn_Text,
            Font = new Font ( "Segoe UI", 9f ),
            ForeColor = SystemColors.ControlText,
            Location = new Point ( 30, 3 ),
            Width = 420,
            AutoSize = false,
            Height = 20,
          } );
          Flow.Controls.Add ( Row );
        }
        else
        {
          Flow.Controls.Add ( new Label
          {
            Text = Line,
            Font = new Font ( "Segoe UI", 9f ),
            ForeColor = SystemColors.ControlText,
            Width = 460,
            AutoSize = false,
            Height = 20,
            Margin = new Padding ( 0, 1, 0, 1 ),
          } );
        }
      }

      Scroll.Controls.Add ( Flow );

      var Footer = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 44,
        BackColor = SystemColors.ControlLight,
      };
      var Close_Btn = new Button
      {
        Text = "Close",
        Size = new Size ( 88, 28 ),
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
      };
      Close_Btn.Click += ( s, e ) => Dlg.Close ( );
      Footer.Controls.Add ( Close_Btn );
      Footer.Resize += ( s, e ) =>
          Close_Btn.Location = new Point ( Footer.Width - 96, 8 );
      Close_Btn.Location = new Point ( 520 - 96, 8 );

      Dlg.Controls.Add ( Scroll );
      Dlg.Controls.Add ( Footer );
      Dlg.Controls.Add ( Header );
      Dlg.ShowDialog ( this );
    }


    private void Setup_Graphics ( Graphics G, int W, int H )
      {
        G.SmoothingMode = SmoothingMode.AntiAlias;
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using var Bg = new SolidBrush ( _Theme.Background );
        G.FillRectangle ( Bg, 0, 0, W, H );
      }

      private void Draw_Grid (
          Graphics G, int W, int H, int Chart_W, int Chart_H,
          double Y_Min, double Y_Range, string Unit,
          Func<double, string> Formatter )
      {
        using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
        using var Label_Font = new Font ( "Consolas", 7.5f );
        using var Label_Brush = new SolidBrush ( _Theme.Labels );
        for ( int I = 0; I <= 5; I++ )
        {
          double Frac = (double) I / 5;
          int Y = H - MB - (int) ( Frac * Chart_H );
          double Val = Y_Min + Frac * Y_Range;
          G.DrawLine ( Grid_Pen, ML, Y, W - MR, Y );
          string Lbl = Formatter ( Val );
          var Sz = G.MeasureString ( Lbl, Label_Font );
          G.DrawString ( Lbl, Label_Font, Label_Brush,
              ML - Sz.Width - 4, Y - Sz.Height / 2 );
        }
      }

      private PointF [ ] Build_Points (
          List<double> Values, int Count,
          int W, int H, int Chart_W, int Chart_H,
          double Y_Min, double Y_Range )
      {
        var Pts = new PointF [ Count ];
        for ( int I = 0; I < Count; I++ )
        {
          float X = ML + (float) I / ( Count - 1 ) * Chart_W;
          float Y = H - MB - (float) ( ( Values [ I ] - Y_Min ) / Y_Range * Chart_H );
          Pts [ I ] = new PointF ( X, Y );
        }
        return Pts;
      }

      private void Draw_Title ( Graphics G, int W, string Title )
      {
        using var Font = new Font ( "Segoe UI", 8.5f, FontStyle.Bold );
        using var Brush = new SolidBrush ( _Theme.Foreground );
        var Sz = G.MeasureString ( Title, Font );
        G.DrawString ( Title, Font, Brush, ( W - Sz.Width ) / 2, 6 );
      }

      private void Draw_Time_Axis ( Graphics G, int W, int H, int Chart_W, int Count )
      {
        if ( _Times.Count < 2 )
          return;
        using var Font = new Font ( "Consolas", 7.5f );
        using var Brush = new SolidBrush ( _Theme.Labels );
        using var Pen = new Pen ( _Theme.Grid, 1f );

        int Num_X = Math.Min ( 8, Chart_W / 80 );
        for ( int I = 0; I <= Num_X; I++ )
        {
          double Frac = (double) I / Num_X;
          int X_Pos = ML + (int) ( Frac * Chart_W );
          int T_Idx = Math.Clamp ( (int) ( Frac * ( Count - 1 ) ), 0, _Times.Count - 1 );
          string Lbl = _Times [ T_Idx ].ToString ( "HH:mm:ss" );
          var Sz = G.MeasureString ( Lbl, Font );
          G.DrawString ( Lbl, Font, Brush, X_Pos - Sz.Width / 2, H - MB + 6 );
          G.DrawLine ( Pen, X_Pos, MT, X_Pos, H - MB );
        }
      }

      private static double Percentile ( List<double> Sorted_Data, double Pct )
      {
        var S = Sorted_Data.OrderBy ( V => V ).ToList ( );
        double Idx = Pct / 100.0 * ( S.Count - 1 );
        int Lo = (int) Idx;
        int Hi = Math.Min ( Lo + 1, S.Count - 1 );
        return S [ Lo ] + ( Idx - Lo ) * ( S [ Hi ] - S [ Lo ] );
      }

      // ══════════════════════════════════════════════════════════════
      //  Helper: timing statistics for one column
      // ══════════════════════════════════════════════════════════════
      private sealed class TimingStat
      {
        public double Mean
        {
          get;
        }
        public double StdDev
        {
          get;
        }
        public double Min
        {
          get;
        }
        public double Max
        {
          get;
        }
        public double Range => Max - Min;

        public TimingStat ( List<double> Values )
        {
          Mean = Values.Average ( );
          double Var = Values.Average ( V => ( V - Mean ) * ( V - Mean ) );
          StdDev = Math.Sqrt ( Var );
          Min = Values.Min ( );
          Max = Values.Max ( );
        }
      }
    }
  }

