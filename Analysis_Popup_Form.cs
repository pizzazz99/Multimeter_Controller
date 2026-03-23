
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Analysis_Popup_Form.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   A self-contained modal Form that performs a deep statistical comparison
//   between two simultaneously-recorded instrument series (A and B).  All
//   computation happens once in the constructor; the six tab panels paint
//   directly from pre-computed arrays on every resize/invalidate.
//
// TABS
//   0  Δ Delta          — downsampled (A − B) line + fill over time;
//                         zero reference line and mean marker.
//   1  Rolling σ        — 100-sample sliding-window standard deviation of the
//                         delta, revealing noise bursts and drift over time.
//   2  Summary Stats    — three sections rendered via GDI+ text:
//                           • Δ (A − B) — Mean / σ / Min / Max / Range
//                             in both Volts and µV columns.
//                           • Sample Timing — mean interval, jitter (σ),
//                             max interval, total point count.
//                           • Polling Health — baseline interval (median of
//                             first 20 samples), slowdown threshold (+20%),
//                             first sustained-slow timestamp (5 consecutive
//                             intervals above threshold), % slow cycles,
//                             peak interval as a multiple of baseline, trend.
//   3  Δ Distribution   — Sturges-rule histogram of the delta in µV; overlaid
//                         fitted normal curve, mean line, ±1σ/±2σ markers.
//   4  Raw              — every sample plotted with no downsampling; useful for
//                         spotting individual outliers or dropped readings.
//   5  Poll Intervals   — raw sample-to-sample gap in ms; rolling 50-sample
//                         mean overlay; +20% threshold line; orange vertical
//                         marker at the first sustained slowdown.
//
// CONSTRUCTOR INPUTS
//   Points_A / Points_B   Paired time-series from the two instruments.
//                         Only Math.Min(A.Count, B.Count) pairs are used.
//   Name_A  / Name_B      Display names shown in titles and the window caption.
//   Theme                 Chart_Theme supplying Background, Foreground, Grid,
//                         Labels, Accent, and Line_Colors[].
//
// DATA COMPUTED IN CONSTRUCTOR (computed once, read-only thereafter)
//   _Deltas               List<double>   — A[i].Value − B[i].Value (Volts)
//   _Delta_Ms             List<double>   — ms between consecutive A timestamps
//   _Times                List<DateTime> — timestamps from series A
//   _Mean / _StdDev / _Min / _Max               — delta statistics (Volts)
//   _Mean_uV / _StdDev_uV / _Min_uV / _Max_uV  — same values scaled to µV
//   _Mean_Delta_Ms / _StdDev_Delta_Ms / _Max_Delta_Ms — timing statistics
//   _Rolling_StdDev       100-sample Welford-style rolling σ of _Deltas
//
// LAYOUT CONSTANTS  (local to the class)
//   ML = 90   Left margin  — Y-axis labels
//   MR = 30   Right margin — overflow clearance
//   MT = 30   Top margin   — title area
//   MB = 40   Bottom margin — X-axis time labels + stats strip
//
// GDI RENDERING
//   Every tab panel is a Buffered_Panel whose Paint event calls a dedicated
//   Draw_*() method.  All drawing is done with locally scoped using-blocks —
//   no persistent GDI resources are held between paints.
//   Setup_Graphics() applies AntiAlias + ClearTypeGridFit and fills the
//   background from _Theme.Background before any chart drawing begins.
//
// SHARED DRAWING HELPERS
//   Draw_Grid()              Horizontal grid lines + formatted Y-axis labels.
//   Build_Points()           Maps a List<double> directly to a PointF[].
//   Build_Points_Sampled()   Same, but skips every N-th point to cap output
//                            at Max_Pts (default 800) for large datasets.
//   Draw_Title()             Centred bold title string in the top margin.
//   Draw_Time_Axis()         Time labels and vertical grid lines on X axis;
//                            indexes into _Times[] by fractional position.
//   Draw_Stat_Row()          Two-column row (Volts + µV) for the stats panel.
//   Draw_Stat_Row_Single()   Single-value row for the stats panel.
//   Color_Name()             Resolves a Color to its KnownColor name, or
//                            falls back to "#RRGGBB" hex notation.
//
// HELP SYSTEM
//   _Tab_Help[]              One plain-text help string per tab, built in the
//                            constructor using the resolved color names so
//                            references match the actual theme in use.
//   Show_Tab_Help()          Parses the active tab's help string line-by-line
//                            into a formatted FlowLayoutPanel dialog with
//                            section headers (accent bar), bullet rows
//                            (colored dot), and body text labels.
//   "?" button               Always visible in the bottom bar; calls
//                            Show_Tab_Help() for the currently selected tab.
//
// NOTES
//   • The Stats tab is wrapped in a scrollable Panel so that the fixed-size
//     860 × 860 Buffered_Panel is accessible on small screens.
//   • Slowdown detection (both in Summary Stats and Poll Intervals) uses the
//     same algorithm: median of the first 20 intervals as the baseline,
//     +20% as the threshold, 5 consecutive exceedances as "sustained".
//   • The form is shown modally (ShowDialog) from the parent chart window;
//     the help dialog (Show_Tab_Help) is also modal to this form.
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════









using System.Text;
using System.Threading.Tasks;


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Multimeter_Controller
{
  public class Analysis_Popup_Form : Form
  {

  

    // ── Data ──────────────────────────────────────────────────────────
    private readonly List<(DateTime Time, double Value)> _Points_A;
    private readonly List<(DateTime Time, double Value)> _Points_B;
    private readonly string _Name_A;
    private readonly string _Name_B;
    private readonly List<double> _Deltas;
    private readonly List<double> _Delta_Ms;
    private readonly List<DateTime> _Times;
    private readonly Chart_Theme _Theme;

    // ── Summary stats (computed once) ─────────────────────────────────
    private readonly double _Mean;
    private readonly double _StdDev;
    private readonly double _Min;
    private readonly double _Max;
    private readonly double _Mean_uV;
    private readonly double _StdDev_uV;
    private readonly double _Min_uV;
    private readonly double _Max_uV;
    private readonly double _Mean_Delta_Ms;
    private readonly double _StdDev_Delta_Ms;
    private readonly double _Max_Delta_Ms;

    // ── Rolling σ (100-sample window) ─────────────────────────────────
    private readonly List<double> _Rolling_StdDev = new ( );

    // ── UI ────────────────────────────────────────────────────────────
    private TabControl _Tabs;
    private Panel _Delta_Panel;
    private Panel _Rolling_Panel;
    private Panel _Stats_Panel;
    private Panel _Histogram_Panel;
    private Panel _Raw_Panel;
    private Panel _Timing_Panel;
    // ── Layout constants ──────────────────────────────────────────────
    private const int ML = 90, MR = 30, MT = 30, MB = 40;


    private readonly string [ ] _Tab_Help;



    public Analysis_Popup_Form (
        List<(DateTime Time, double Value)> Points_A,
        List<(DateTime Time, double Value)> Points_B,
        string Name_A,
        string Name_B,
        Chart_Theme Theme
    )
    {
      _Points_A = Points_A;
      _Points_B = Points_B;
      _Name_A = Name_A;
      _Name_B = Name_B;
      _Theme = Theme;

      int Count = Math.Min ( Points_A.Count, Points_B.Count );

      // ── Compute deltas ────────────────────────────────────────────
      _Deltas = new List<double> ( Count );
      _Delta_Ms = new List<double> ( Count );
      _Times = new List<DateTime> ( Count );

      for ( int I = 0; I < Count; I++ )
      {
        _Deltas.Add ( Points_A [ I ].Value - Points_B [ I ].Value );
        _Times.Add ( Points_A [ I ].Time );
        _Delta_Ms.Add ( I == 0 ? 0.0
            : ( Points_A [ I ].Time - Points_A [ I - 1 ].Time ).TotalMilliseconds );
      }

      // ── Summary stats ─────────────────────────────────────────────
      _Mean = _Deltas.Average ( );
      double Var = _Deltas.Average ( D => ( D - _Mean ) * ( D - _Mean ) );
      _StdDev = Math.Sqrt ( Var );
      _Min = _Deltas.Min ( );
      _Max = _Deltas.Max ( );

      _Mean_uV = _Mean * 1e6;
      _StdDev_uV = _StdDev * 1e6;
      _Min_uV = _Min * 1e6;
      _Max_uV = _Max * 1e6;

      // ── Timing stats ──────────────────────────────────────────────
      var Valid_Ms = _Delta_Ms.Skip ( 1 ).ToList ( );
      _Mean_Delta_Ms = Valid_Ms.Average ( );
      double T_Var = Valid_Ms.Average ( D => ( D - _Mean_Delta_Ms ) * ( D - _Mean_Delta_Ms ) );
      _StdDev_Delta_Ms = Math.Sqrt ( T_Var );
      _Max_Delta_Ms = Valid_Ms.Max ( );

      // ── Rolling σ ─────────────────────────────────────────────────
      const int Window = 100;
      var Q = new Queue<double> ( );
      double R_Sum = 0, R_Sum_Sq = 0;

      foreach ( double D in _Deltas )
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
        double R_Mu = R_Sum / N;
        double R_Var = ( R_Sum_Sq / N ) - ( R_Mu * R_Mu );
        _Rolling_StdDev.Add ( R_Var > 0 ? Math.Sqrt ( R_Var ) : 0.0 );
      }


      // ── Build help text ───────────────────────────────────────────
      string C0 = Color_Name ( _Theme.Line_Colors [ 0 ] );
      string C1 = Color_Name ( _Theme.Line_Colors [ 1 ] );
      string Acc = Color_Name ( _Theme.Accent );
      string Red = "Red";

      _Tab_Help = new [ ]
{
    // Tab 0 — Δ Delta
    "Δ Delta — Over Time\n\n" +
    "This chart shows the difference (A − B) between the two meters at each sample point, plotted over time.\n\n" +
    $"• The {C0} line is the downsampled delta value in µV.\n" +
    $"• The dashed {Acc} line is the mean (average) delta.\n" +
    $"• The dashed {Red} line marks zero — perfect agreement between meters.\n\n" +
    "What to look for:\n" +
    "  – A flat line near zero means the meters agree well.\n" +
    "  – A consistent offset means one meter reads higher than the other.\n" +
    "  – Spikes or drift suggest noise or instability in one or both meters.",

    // Tab 1 — Rolling σ
    "Rolling σ — Noise Over Time\n\n" +
    "This chart shows the rolling standard deviation (σ) of the delta, computed over a sliding 100-sample window.\n\n" +
    $"• The {C1} line shows variability (noise) between the two meters at each moment.\n" +
    "• Higher values mean more variability. Lower values mean consistent agreement.\n\n" +
    "What to look for:\n" +
    "  – A flat, low line means stable, low-noise measurements.\n" +
    "  – Bumps or spikes indicate periods of increased disagreement or noise.\n" +
    "  – Rising trend at the end may indicate a meter drifting.",

 // Tab 2 — Summary Stats
"Summary Statistics\n\n" +
"This panel summarises the entire comparison run in numbers.\n\n" +
"Δ (A − B) section:\n" +
"  • Mean — average difference between meters (ideally near zero)\n" +
"  • σ (sigma) — standard deviation; lower = more consistent agreement\n" +
"  • Min / Max / Range — extremes of the delta\n\n" +
"Sample Timing section:\n" +
"  • Mean interval — average time between samples (and equivalent sample rate)\n" +
"  • Jitter (σ) — variability in sample timing; lower = more even pacing\n" +
"  • Max interval — longest gap between any two samples\n" +
"  • Point count — total number of paired samples analysed\n\n" +
"Polling Health section:\n" +
"  • Baseline interval — the median of the first 20 intervals; represents the expected polling speed under normal conditions\n" +
"  • Slowdown threshold — 20% above the baseline; intervals exceeding this are flagged as anomalous\n" +
"  • First sustained slow — timestamp and sample number where 5 or more consecutive intervals all exceeded the threshold\n" +
"  • Slow cycles — percentage of all intervals above the threshold; values above 10% indicate a significant polling problem\n" +
"  • Peak interval — the single longest gap recorded, expressed as a multiple of the baseline\n" +
"  – Trend — whether polling speed was stable, gradually drifting slower, or improving over the session",

    // Tab 3 — Δ Distribution
    "Δ Distribution — Histogram\n\n" +
    "This chart shows how often each delta value occurred across the entire run.\n\n" +
    $"• The {C0} bars count how many samples fell in each µV range.\n" +
    $"• The {C1} curve is the ideal normal (bell) distribution fitted to the data.\n" +
    $"• The dashed {Acc} line marks the mean.\n" +
    $"• The dotted {Acc} lines mark ±1σ and ±2σ from the mean.\n\n" +
    "What to look for:\n" +
    "  – A narrow, symmetric bell curve centred near zero is ideal.\n" +
    "  – A wide or skewed distribution suggests noise or systematic offset.\n" +
    "  – Multiple humps may indicate the meters switching measurement ranges.",

    // Tab 4 — Raw
    "Raw Delta — Every Sample\n\n" +
    "This chart plots every single recorded delta value with no downsampling.\n\n" +
    $"• The {C0} line shows every raw sample — with large datasets this appears as a dense band.\n" +
    $"• The dashed {Acc} line is the mean.\n" +
    $"• The dashed {Red} line marks zero — perfect agreement between meters.\n\n" +
    "What to look for:\n" +
    "  – Sudden changes in band width indicate noise bursts.\n" +
    "  – A consistent offset from zero means one meter reads higher.\n" +
    "  – Gaps in the band may indicate dropped samples.",

  // Tab 5 — Poll Intervals
"Poll Intervals — Sample Timing\n\n" +
"This chart shows the gap in milliseconds between consecutive timestamp pairs, " +
"plotted over time.\n\n" +
"• The blue line is the raw interval between each pair of readings.\n" +
"• The dashed green line is the mean interval.\n" +
"• The dashed red line marks the ±20% threshold above the mean — " +
"intervals above this line are considered anomalous.\n\n" +
"What to look for:\n" +
"  – A flat line at the baseline means consistent, even polling.\n" +
"  – Spikes indicate individual cycles that took longer than normal.\n" +
"  – A gradual upward drift means the polling loop is slowing down over time.\n" +
"  – The slowdown start time is marked with a vertical orange line.",
};



      Build_UI ( );
    }

    // ─────────────────────────────────────────────────────────────────
    // UI Construction
    // ─────────────────────────────────────────────────────────────────

    private void Build_UI ( )
    {
      Text = $"Meter Comparison Analysis — {_Name_A}  vs  {_Name_B}";
      Size = new Size ( 900, 620 );
      MinimumSize = new Size ( 700, 500 );
     
      StartPosition = FormStartPosition.CenterParent;
      this.Padding = new Padding ( 0 );

      _Tabs = new TabControl
      {
        Dock = DockStyle.Fill,
      
        Padding = new Point ( 10, 3 ),   // optional: gives tab labels a little breathing room
      };


      // Push tabs flush to the top edge — eliminates the thin black gap
      _Tabs.SizeMode = TabSizeMode.Normal;
      Padding = new Padding ( 0, 0, 0, 0 );  // form padding

      _Delta_Panel = new Buffered_Panel ( );
      _Rolling_Panel = new Buffered_Panel ( );
      _Stats_Panel = new Buffered_Panel ( );
      _Histogram_Panel = new Buffered_Panel ( );
      _Raw_Panel = new Buffered_Panel ( );
      _Timing_Panel = new Buffered_Panel ( );

      _Delta_Panel.Paint += ( s, e ) => Draw_Delta_Chart ( e.Graphics );
      _Rolling_Panel.Paint += ( s, e ) => Draw_Rolling_Chart ( e.Graphics );
      _Stats_Panel.Paint += ( s, e ) => Draw_Stats_Panel ( e.Graphics );
      _Histogram_Panel.Paint += ( s, e ) => Draw_Histogram_Panel ( e.Graphics );
      _Raw_Panel.Paint += ( s, e ) => Draw_Raw_Chart ( e.Graphics );
      _Timing_Panel.Paint += ( s, e ) => Draw_Timing_Panel ( e.Graphics );

      Add_Tab ( "Δ Delta", _Delta_Panel );
      Add_Tab ( "Rolling σ", _Rolling_Panel );

      // ── Stats tab — wrapped in scrollable panel ───────────────────────
      _Stats_Panel.Dock = DockStyle.None;
      _Stats_Panel.Size = new Size ( 860, 860 );
      _Stats_Panel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

      var Stats_Scroll = new Panel
      {
        Dock = DockStyle.Fill,
        AutoScroll = true,
      };
      Stats_Scroll.Controls.Add ( _Stats_Panel );

      var Stats_Page = new TabPage ( "Summary Stats" );
      Stats_Page.Controls.Add ( Stats_Scroll );
      _Tabs.TabPages.Add ( Stats_Page );
      // ─────────────────────────────────────────────────────────────────

      Add_Tab ( "Δ Distribution", _Histogram_Panel );
      Add_Tab ( "Raw", _Raw_Panel );
      Add_Tab ( "Poll Intervals", _Timing_Panel );

      // ── Bottom panel ────────────────────────────────────────────────
      var Bottom_Panel = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 36,
      };

      // "?" help button — explains the current tab
      var Help_Btn = new Button
      {
        Text = "?  What am I looking at?",
        Size = new Size ( 180, 26 ),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
      };
      Help_Btn.Click += ( s, e ) => Show_Tab_Help ( );
      Help_Btn.Location = new Point ( 8, 5 );

      // Close button
      var Close_Btn = new Button
      {
        Text = "Close",
        Size = new Size ( 80, 26 ),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
      };
      Close_Btn.Click += ( s, e ) => Close ( );

      Bottom_Panel.Controls.Add ( Help_Btn );
      Bottom_Panel.Controls.Add ( Close_Btn );
      Close_Btn.Location = new Point ( Bottom_Panel.Width - 90, 5 );
      Close_Btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

      Controls.Add ( _Tabs );
      Controls.Add ( Bottom_Panel );

      _Tabs.SelectedIndexChanged += ( s, e ) =>
      {
        var First = _Tabs.SelectedTab?.Controls [ 0 ];
        if ( First is Buffered_Panel BP )
          BP.Invalidate ( );
        else if ( First is Panel Scroll )
          ( Scroll.Controls.Count > 0 ? Scroll.Controls [ 0 ] as Buffered_Panel : null )?.Invalidate ( );
      };
    }
  
    private void Add_Tab ( string Title, Panel Content )
    {
      var Page = new TabPage ( Title )
      {
      
      };
      Content.Dock = DockStyle.Fill;
      Page.Controls.Add ( Content );
      _Tabs.TabPages.Add ( Page );
    }



    // ─────────────────────────────────────────────────────────────────
    // Tab 1 — Delta over time
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Delta_Chart ( Graphics G )
    {
      int W = _Delta_Panel.ClientSize.Width;
      int H = _Delta_Panel.ClientSize.Height;
      Setup_Graphics ( G, W, H );
      int Count = _Deltas.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;
      if ( Count < 2 || Chart_W < 20 || Chart_H < 20 )
        return;

      double Y_Min = _Min - Math.Abs ( _Min ) * 0.2;
      double Y_Max = _Max + Math.Abs ( _Max ) * 0.2;
      if ( Y_Max - Y_Min < 1e-12 )
      {
        Y_Min -= 1e-7;
        Y_Max += 1e-7;
      }
      double Y_Range = Y_Max - Y_Min;

      Draw_Grid ( G, W, H, Chart_W, Chart_H, Y_Min, Y_Range, "µV",
          V => ( V * 1e6 ).ToString ( "F2" ) );

      // Delta line + fill
      var Pts = Build_Points_Sampled ( _Deltas, Count, W, H, Chart_W, Chart_H, Y_Min, Y_Range );
      int Pts_Count = Pts.Length;

      using var Fill_Brush = new LinearGradientBrush (
          new PointF ( 0, MT ), new PointF ( 0, H - MB ),
          Color.FromArgb ( 50, _Theme.Line_Colors [ 0 ] ),
          Color.FromArgb ( 5, _Theme.Line_Colors [ 0 ] ) );

      var Fill_Pts = new PointF [ Pts_Count + 2 ];
      Array.Copy ( Pts, Fill_Pts, Pts_Count );
      Fill_Pts [ Pts_Count ] = new PointF ( Pts [ Pts_Count - 1 ].X, H - MB );
      Fill_Pts [ Pts_Count + 1 ] = new PointF ( Pts [ 0 ].X, H - MB );
      G.FillPolygon ( Fill_Brush, Fill_Pts );

      using var Line_Pen = new Pen ( _Theme.Line_Colors [ 0 ], 1.5f );
      G.DrawLines ( Line_Pen, Pts );

      // Zero line — drawn after fill so it is never buried
      float Zero_Y = H - MB - (float) ( ( 0 - Y_Min ) / Y_Range * Chart_H );
      using var Zero_Pen = new Pen ( Color.FromArgb ( 180, Color.Red ), 1.5f )
      {
        DashStyle = DashStyle.Dash
      };
      G.DrawLine ( Zero_Pen, ML, Zero_Y, W - MR, Zero_Y );

      // Mean line
      float Mean_Y = H - MB - (float) ( ( _Mean - Y_Min ) / Y_Range * Chart_H );
      using var Mean_Pen = new Pen ( _Theme.Accent, 1.5f ) { DashStyle = DashStyle.Dash };
      G.DrawLine ( Mean_Pen, ML, Mean_Y, W - MR, Mean_Y );

      Draw_Title ( G, W, $"Delta  ({_Name_A} − {_Name_B})  |  mean: {_Mean_uV:F3} µV  σ: {_StdDev_uV:F3} µV" );
      Draw_Time_Axis ( G, W, H, Chart_W, Count );
    }

    // ─────────────────────────────────────────────────────────────────
    // Tab 2 — Rolling σ over time
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Rolling_Chart ( Graphics G )
    {
      int W = _Rolling_Panel.ClientSize.Width;
      int H = _Rolling_Panel.ClientSize.Height;

      Setup_Graphics ( G, W, H );

      int Count = _Rolling_StdDev.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;

      if ( Count < 2 || Chart_W < 20 || Chart_H < 20 )
        return;

      double Y_Max = _Rolling_StdDev.Max ( ) * 1.15;
      double Y_Min = 0;
      double Y_Range = Y_Max > 0 ? Y_Max : 1e-9;

      Draw_Grid ( G, W, H, Chart_W, Chart_H, Y_Min, Y_Range, "µV",
          V => ( V * 1e6 ).ToString ( "F3" ) );

      var Pts = Build_Points ( _Rolling_StdDev, Count, W, H, Chart_W, Chart_H, Y_Min, Y_Range );

      using var Fill_Brush = new LinearGradientBrush (
          new PointF ( 0, MT ), new PointF ( 0, H - MB ),
          Color.FromArgb ( 60, _Theme.Line_Colors [ 1 ] ),
          Color.FromArgb ( 5, _Theme.Line_Colors [ 1 ] ) );

      var Fill_Pts = new PointF [ Count + 2 ];
      Array.Copy ( Pts, Fill_Pts, Count );
      Fill_Pts [ Count ] = new PointF ( Pts [ Count - 1 ].X, H - MB );
      Fill_Pts [ Count + 1 ] = new PointF ( Pts [ 0 ].X, H - MB );
      G.FillPolygon ( Fill_Brush, Fill_Pts );

      using var Line_Pen = new Pen ( _Theme.Line_Colors [ 1 ], 1.5f );
      G.DrawLines ( Line_Pen, Pts );

      Draw_Title ( G, W, "Rolling σ of Delta  (100-sample window)" );
      Draw_Time_Axis ( G, W, H, Chart_W, Count );
    }

    // ─────────────────────────────────────────────────────────────────
    // Tab 3 — Summary stats
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Stats_Panel ( Graphics G )
    {
      int W = _Stats_Panel.ClientSize.Width;
      int H = _Stats_Panel.ClientSize.Height;
      G.Clear ( SystemColors.Control );

      using var Title_Font = new Font ( "Segoe UI", 12f, FontStyle.Bold );
      using var Header_Font = new Font ( "Segoe UI", 10f, FontStyle.Bold );
      using var Value_Font = new Font ( "Courier New", 11f );          // was Consolas 10f
      using var Label_Font = new Font ( "Courier New", 11f );          // was Consolas 9f
      using var Small_Font = new Font ( "Courier New", 9f );           // was Consolas 8f

      using var Fg_Brush = new SolidBrush ( SystemColors.ControlText );
      using var Dim_Brush = new SolidBrush ( SystemColors.GrayText );
      using var Header_Brush = new SolidBrush ( SystemColors.ControlText );

      int Col1 = 40, Col2 = 260, Col3 = 500;
      int Y = 30;
      int Row = 30;   // bumped from 28 to give Segoe UI a little more breathing room

    

      G.DrawString ( $"Comparison: {_Name_A}  vs  {_Name_B}", Title_Font, Fg_Brush, Col1, Y );
      Y += Row + 8;

      // Section: Delta stats
      G.DrawString ( "Δ  (A − B)", Header_Font, Fg_Brush, Col1, Y );
      G.DrawString ( "Volts", Header_Font, Fg_Brush, Col2, Y );
      G.DrawString ( "µV", Header_Font, Fg_Brush, Col3, Y );
      Y += Row;

      Draw_Stat_Row ( G, Value_Font, Fg_Brush, Fg_Brush,
          "Mean", _Mean, _Mean_uV, Col1, Col2, Col3, Y );
      Y += Row;
      Draw_Stat_Row ( G, Value_Font, Fg_Brush, Fg_Brush,
          "σ", _StdDev, _StdDev_uV, Col1, Col2, Col3, Y );
      Y += Row;
      Draw_Stat_Row ( G, Value_Font, Fg_Brush, Fg_Brush,
          "Min", _Min, _Min_uV, Col1, Col2, Col3, Y );
      Y += Row;
      Draw_Stat_Row ( G, Value_Font, Fg_Brush, Fg_Brush,
          "Max", _Max, _Max_uV, Col1, Col2, Col3, Y );
      Y += Row;
      Draw_Stat_Row ( G, Value_Font, Fg_Brush, Fg_Brush,
          "Range", _Max - _Min, _Max_uV - _Min_uV, Col1, Col2, Col3, Y );
      Y += Row + 16;

      // Separator
      using var Sep_Pen = new Pen ( _Theme.Grid, 1f );
      G.DrawLine ( Sep_Pen, Col1, Y, W - 40, Y );
      Y += 12;

      // Section: Timing stats
      G.DrawString ( "Sample Timing", Header_Font, Fg_Brush, Col1, Y );
      Y += Row;

      Draw_Stat_Row_Single ( G, Value_Font, Fg_Brush, Fg_Brush,
          "Mean interval", $"{_Mean_Delta_Ms:F2} ms  ({1000.0 / _Mean_Delta_Ms:F2} samples/sec)",
          Col1, Col2, Y );
      Y += Row;
      Draw_Stat_Row_Single ( G, Value_Font, Fg_Brush, Fg_Brush,
          "Jitter (σ)", $"{_StdDev_Delta_Ms:F2} ms",
          Col1, Col2, Y );
      Y += Row;
      Draw_Stat_Row_Single ( G, Value_Font, Fg_Brush, Fg_Brush,
          "Max interval", $"{_Max_Delta_Ms:F2} ms",
          Col1, Col2, Y );
      Y += Row;
      Draw_Stat_Row_Single ( G, Value_Font, Fg_Brush, Fg_Brush,
          "Point count", $"{_Deltas.Count:N0}",
          Col1, Col2, Y );


      Y += Row + 16;

      // ── Separator ─────────────────────────────────────────────────────
      G.DrawLine ( Sep_Pen, Col1, Y, W - 40, Y );
      Y += 12;

      G.DrawString ( "Polling Health", Header_Font, Fg_Brush, Col1, Y );
      Y += Row;

      var Valid_Ms = _Delta_Ms.Skip ( 1 ).ToList ( );
      int N = Valid_Ms.Count;

      if ( N > 2 )
      {
        var Early = Valid_Ms.Take ( 20 ).OrderBy ( V => V ).ToList ( );
        double Baseline = Early [ Early.Count / 2 ];
        double Threshold = Baseline * 1.2;

        int Slowdown_Index = -1;
        const int Sustain = 5;
        for ( int I = 0; I <= N - Sustain; I++ )
        {
          bool All_Slow = true;
          for ( int J = 0; J < Sustain; J++ )
            if ( Valid_Ms [ I + J ] <= Threshold )
            {
              All_Slow = false;
              break;
            }
          if ( All_Slow )
          {
            Slowdown_Index = I;
            break;
          }
        }

        double Pct_Slow = Valid_Ms.Count ( V => V > Threshold ) * 100.0 / N;
        double Peak_Ms = Valid_Ms.Max ( );

        double X_Mean = ( N - 1 ) / 2.0;
        double Y_Mean = Valid_Ms.Average ( );
        double Num = 0, Den = 0;
        for ( int I = 0; I < N; I++ )
        {
          Num += ( I - X_Mean ) * ( Valid_Ms [ I ] - Y_Mean );
          Den += ( I - X_Mean ) * ( I - X_Mean );
        }
        double Slope_Total = Den > 0 ? ( Num / Den ) * N : 0;
        string Trend =
            Math.Abs ( Slope_Total ) < 0.5 ? "Flat — stable polling" :
            Slope_Total > 0
                ? $"Gradual drift  (+{Slope_Total:F1} ms over session)"
                : $"Improving  ({Slope_Total:F1} ms over session)";

        Draw_Stat_Row_Single ( G, Value_Font, Fg_Brush, Dim_Brush,
            "Baseline interval",
            $"{Baseline:F2} ms   ({1000.0 / Baseline:F1} S/s)",
            Col1, Col2, Y );
        Y += Row;

        Draw_Stat_Row_Single ( G, Value_Font, Fg_Brush, Dim_Brush,
            "Slowdown threshold",
            $"{Threshold:F2} ms   (+20%)",
            Col1, Col2, Y );
        Y += Row;

        string Slow_Time_Str = Slowdown_Index >= 0
            ? $"{_Times [ Math.Clamp ( Slowdown_Index + 1, 0, _Times.Count - 1 ) ]:HH:mm:ss}  (sample {Slowdown_Index:N0})"
            : "None detected";
        Draw_Stat_Row_Single ( G, Value_Font, Fg_Brush, Dim_Brush,
            "First sustained slow", Slow_Time_Str,
            Col1, Col2, Y );
        Y += Row;

        using var Slow_Brush = new SolidBrush (
            Pct_Slow > 10 ? Color.OrangeRed :
            Pct_Slow > 2 ? Color.Orange :
                            SystemColors.ControlText );
        Draw_Stat_Row_Single ( G, Value_Font, Slow_Brush, Dim_Brush,
            "Slow cycles",
            $"{Pct_Slow:F1}%   ({Valid_Ms.Count ( V => V > Threshold ):N0} of {N:N0})",
            Col1, Col2, Y );
        Y += Row;

        Draw_Stat_Row_Single ( G, Value_Font, Fg_Brush, Dim_Brush,
            "Peak interval",
            $"{Peak_Ms:F1} ms   ({Peak_Ms / Baseline:F1}x baseline)",
            Col1, Col2, Y );
        Y += Row;

        Draw_Stat_Row_Single ( G, Value_Font, Fg_Brush, Dim_Brush,
            "Trend", Trend,
            Col1, Col2, Y );
      }
    }


    private void Draw_Stat_Row (
     Graphics G, Font Val_Font, Brush Fg, Brush Dim,
     string Label, double V, double UV,
     int C1, int C2, int C3, int Y )
    {
      using var Lbl_Font = new Font ( "Segoe UI", 10f );   // was Consolas 9f
      G.DrawString ( Label, Lbl_Font, Dim, C1 + 16, Y );
      G.DrawString ( V.ToString ( "G10", CultureInfo.InvariantCulture ), Val_Font, Fg, C2, Y );
      G.DrawString ( UV.ToString ( "F4", CultureInfo.InvariantCulture ), Val_Font, Fg, C3, Y );
    }

    private void Draw_Stat_Row_Single (
        Graphics G, Font Val_Font, Brush Fg, Brush Dim,
        string Label, string Value,
        int C1, int C2, int Y )
    {
      using var Lbl_Font = new Font ( "Segoe UI", 10f );   // was Consolas 9f
      G.DrawString ( Label, Lbl_Font, Dim, C1 + 16, Y );
      G.DrawString ( Value, Val_Font, Fg, C2, Y );
    }

    // ─────────────────────────────────────────────────────────────────
    // Tab 4 — Delta histogram + bell curve
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Histogram_Panel ( Graphics G )
    {
      int W = _Histogram_Panel.ClientSize.Width;
      int H = _Histogram_Panel.ClientSize.Height;

      Setup_Graphics ( G, W, H );

      int Count = _Deltas.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;

      if ( Count < 5 || Chart_W < 20 || Chart_H < 20 )
        return;

      // Work in µV for readability
      var UV_Deltas = _Deltas.Select ( D => D * 1e6 ).ToList ( );
      double UV_Min = UV_Deltas.Min ( );
      double UV_Max = UV_Deltas.Max ( );
      double Range = UV_Max - UV_Min;

      if ( Range < 1e-9 )
      {
        Range = 1.0;
        UV_Min -= 0.5;
        UV_Max += 0.5;
      }

      int Num_Bins = Math.Clamp ( (int) Math.Ceiling ( 1.0 + 3.322 * Math.Log10 ( Count ) ), 8, 40 );
      double Bin_W = Range / Num_Bins;
      var Bins = new int [ Num_Bins ];

      foreach ( double V in UV_Deltas )
        Bins [ Math.Clamp ( (int) ( ( V - UV_Min ) / Bin_W ), 0, Num_Bins - 1 ) ]++;

      int Max_Count = Math.Max ( 1, Bins.Max ( ) );
      double Y_Max_Val = Max_Count * 1.15;

      // Y grid
      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Label_Font = new Font ( "Consolas", 7.5f );
      using var Label_Brush = new SolidBrush ( _Theme.Labels );

      for ( int I = 0; I <= 5; I++ )
      {
        double Frac = (double) I / 5;
        int Y = H - MB - (int) ( Frac * Chart_H );
        G.DrawLine ( Grid_Pen, ML, Y, W - MR, Y );
        string Lbl = ( (int) Math.Round ( Frac * Y_Max_Val ) ).ToString ( );
        var Sz = G.MeasureString ( Lbl, Label_Font );
        G.DrawString ( Lbl, Label_Font, Label_Brush, ML - Sz.Width - 4, Y - Sz.Height / 2 );
      }

      // Bars
      Color Bar_Color = _Theme.Line_Colors [ 0 ];
      using var Bar_Brush = new SolidBrush ( Color.FromArgb ( 160, Bar_Color ) );
      using var Bar_Pen = new Pen ( Color.FromArgb ( 220, Bar_Color ), 1f );
      float Bar_Spacing = (float) Chart_W / Num_Bins;
      float Bar_Draw_W = Bar_Spacing * 0.85f;

      for ( int I = 0; I < Num_Bins; I++ )
      {
        if ( Bins [ I ] == 0 )
          continue;
        float Bar_H = (float) ( Bins [ I ] / Y_Max_Val * Chart_H );
        float X = ML + I * Bar_Spacing;
        float Y = H - MB - Bar_H;
        G.FillRectangle ( Bar_Brush, X, Y, Bar_Draw_W, Bar_H );
        G.DrawRectangle ( Bar_Pen, X, Y, Bar_Draw_W, Bar_H );
      }

      // Bell curve overlay
      double UV_Mean = _Mean_uV;
      double UV_StdDev = _StdDev_uV;

      if ( UV_StdDev > 1e-12 )
      {
        double Scale = Bin_W * Count;
        int Num_Pts = 200;
        var Curve = new PointF [ Num_Pts ];

        for ( int I = 0; I < Num_Pts; I++ )
        {
          double X_Val = UV_Min + ( I / (double) ( Num_Pts - 1 ) ) * Range;
          double Z = ( X_Val - UV_Mean ) / UV_StdDev;
          double PDF = ( 1.0 / ( UV_StdDev * Math.Sqrt ( 2 * Math.PI ) ) )
                         * Math.Exp ( -0.5 * Z * Z );
          float Px = ML + (float) ( ( X_Val - UV_Min ) / Range * Chart_W );
          float Py = H - MB - (float) ( ( PDF * Scale / Y_Max_Val ) * Chart_H );
          Curve [ I ] = new PointF ( Px, Py );
        }

        using var Curve_Pen = new Pen ( _Theme.Line_Colors [ 1 ], 2.5f );
        G.DrawLines ( Curve_Pen, Curve );

        // Mean line
        float Mean_X = ML + (float) ( ( UV_Mean - UV_Min ) / Range * Chart_W );
        using var Mean_Pen = new Pen ( _Theme.Accent, 2f ) { DashStyle = DashStyle.Dash };
        G.DrawLine ( Mean_Pen, Mean_X, MT, Mean_X, H - MB );

        // ±1σ and ±2σ lines
        using var Sigma_Pen = new Pen ( Color.FromArgb ( 120, Color.Gold ), 1.5f )
        {
          DashStyle = DashStyle.Dot
        };
        using var Sigma_Font = new Font ( "Consolas", 7f );
        using var Sigma_Brush = new SolidBrush ( Color.Gold );

        foreach ( var (Mult, Lbl) in new [ ] {
                    (-2, "-2σ"), (-1, "-1σ"), (1, "+1σ"), (2, "+2σ") } )
        {
          double Sx_Val = UV_Mean + Mult * UV_StdDev;
          if ( Sx_Val < UV_Min || Sx_Val > UV_Max )
            continue;
          float Sx = ML + (float) ( ( Sx_Val - UV_Min ) / Range * Chart_W );
          G.DrawLine ( Sigma_Pen, Sx, MT, Sx, H - MB );
          G.DrawString ( Lbl, Sigma_Font, Sigma_Brush, Sx + 2, MT + 2 );
        }
      }

      // X-axis labels (µV)
      using var X_Font = new Font ( "Consolas", 6.5f );
      int Label_Step = Math.Max ( 1, Num_Bins / 8 );
      for ( int I = 0; I < Num_Bins; I += Label_Step )
      {
        double Center = UV_Min + ( I + 0.5 ) * Bin_W;
        string Lbl = $"{Center:F1}";
        var Sz = G.MeasureString ( Lbl, X_Font );
        float X = ML + I * Bar_Spacing + Bar_Spacing / 2;
        G.DrawString ( Lbl, X_Font, Label_Brush, X - Sz.Width / 2, H - MB + 4 );
      }

      Draw_Title ( G, W,
          $"Δ Distribution (µV)  |  mean: {UV_Mean:F3} µV  σ: {UV_StdDev:F3} µV  "
        + $"n: {Count:N0}" );
    }


    // ─────────────────────────────────────────────────────────────────
    // Tab 5 — Raw delta (every point, no fill)
    // ─────────────────────────────────────────────────────────────────
    private void Draw_Raw_Chart ( Graphics G )
    {
      int W = _Raw_Panel.ClientSize.Width;
      int H = _Raw_Panel.ClientSize.Height;

      Setup_Graphics ( G, W, H );

      int Count = _Deltas.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;

      if ( Count < 2 || Chart_W < 20 || Chart_H < 20 )
        return;

      double Y_Min = _Min - Math.Abs ( _Min ) * 0.2;
      double Y_Max = _Max + Math.Abs ( _Max ) * 0.2;
      if ( Y_Max - Y_Min < 1e-12 )
      {
        Y_Min -= 1e-7;
        Y_Max += 1e-7;
      }
      double Y_Range = Y_Max - Y_Min;

      Draw_Grid ( G, W, H, Chart_W, Chart_H, Y_Min, Y_Range, "µV",
          V => ( V * 1e6 ).ToString ( "F2" ) );

      // Raw line — no fill, thin, slightly transparent
      var Pts = Build_Points ( _Deltas, Count, W, H, Chart_W, Chart_H, Y_Min, Y_Range );
      using var Line_Pen = new Pen ( Color.FromArgb ( 180, _Theme.Line_Colors [ 0 ] ), 1f );
      G.DrawLines ( Line_Pen, Pts );

      // Zero line — after data so it is never buried
      float Zero_Y = H - MB - (float) ( ( 0 - Y_Min ) / Y_Range * Chart_H );
      using var Zero_Pen = new Pen ( Color.FromArgb ( 180, Color.Red ), 1.5f ) { DashStyle = DashStyle.Dash };
      G.DrawLine ( Zero_Pen, ML, Zero_Y, W - MR, Zero_Y );

      // Mean line
      float Mean_Y = H - MB - (float) ( ( _Mean - Y_Min ) / Y_Range * Chart_H );
      using var Mean_Pen = new Pen ( _Theme.Accent, 1.5f ) { DashStyle = DashStyle.Dash };
      G.DrawLine ( Mean_Pen, ML, Mean_Y, W - MR, Mean_Y );

      Draw_Title ( G, W,
          $"Raw Delta  ({_Name_A} − {_Name_B})  |  {Count:N0} points  |  " +
          $"mean: {_Mean_uV:F3} µV  σ: {_StdDev_uV:F3} µV" );
      Draw_Time_Axis ( G, W, H, Chart_W, Count );
    }


    // ─────────────────────────────────────────────────────────────────
    // Tab 5 — Raw Timing
    // ─────────────────────────────────────────────────────────────────
    private void Draw_Timing_Panel ( Graphics G )
    {
      int W = _Timing_Panel.ClientSize.Width;
      int H = _Timing_Panel.ClientSize.Height;

      Setup_Graphics ( G, W, H );

      int Count = _Times.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;

      if ( Count < 3 || Chart_W < 20 || Chart_H < 20 )
        return;

      // ── Compute intervals ─────────────────────────────────────────────
      var Intervals = new List<double> ( Count - 1 );
      for ( int I = 1; I < Count; I++ )
        Intervals.Add ( ( _Times [ I ] - _Times [ I - 1 ] ).TotalMilliseconds );

      int N = Intervals.Count;
      double Mean_Ms = Intervals.Average ( );
      double Threshold_Ms = Mean_Ms * 1.2;   // 20% above mean = anomalous
      double Y_Max = Math.Max ( Intervals.Max ( ) * 1.15, Mean_Ms * 2.0 );
      double Y_Min = 0;
      double Y_Range = Y_Max - Y_Min;

      // ── Find first sustained slowdown ─────────────────────────────────
      // Sustained = 5 consecutive intervals all above threshold
      int Slowdown_Index = -1;
      const int Sustain = 5;
      for ( int I = 0; I <= N - Sustain; I++ )
      {
        bool All_Slow = true;
        for ( int J = 0; J < Sustain; J++ )
        {
          if ( Intervals [ I + J ] <= Threshold_Ms )
          {
            All_Slow = false;
            break;
          }
        }
        if ( All_Slow )
        {
          Slowdown_Index = I;
          break;
        }
      }

      // ── Compute rolling mean (50-sample window) ───────────────────────
      const int Roll_Win = 50;
      var Rolling = new List<double> ( N );
      double R_Sum = 0;
      var R_Q = new Queue<double> ( );
      foreach ( double V in Intervals )
      {
        R_Q.Enqueue ( V );
        R_Sum += V;
        if ( R_Q.Count > Roll_Win )
          R_Sum -= R_Q.Dequeue ( );
        Rolling.Add ( R_Sum / R_Q.Count );
      }

      // ── Grid ─────────────────────────────────────────────────────────
      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Label_Font = new Font ( "Consolas", 7.5f );
      using var Label_Brush = new SolidBrush ( _Theme.Labels );

      for ( int I = 0; I <= 5; I++ )
      {
        double Frac = (double) I / 5;
        int Y_Pos = H - MB - (int) ( Frac * Chart_H );
        double Val = Y_Min + Frac * Y_Range;
        G.DrawLine ( Grid_Pen, ML, Y_Pos, W - MR, Y_Pos );
        string Lbl = $"{Val:F0} ms";
        var Sz = G.MeasureString ( Lbl, Label_Font );
        G.DrawString ( Lbl, Label_Font, Label_Brush,
            ML - Sz.Width - 4, Y_Pos - Sz.Height / 2 );
      }

      // ── Raw interval line ─────────────────────────────────────────────
      var Raw_Pts = new PointF [ N ];
      for ( int I = 0; I < N; I++ )
      {
        float X = ML + (float) I / ( N - 1 ) * Chart_W;
        float Y = H - MB - (float) ( ( Intervals [ I ] - Y_Min ) / Y_Range * Chart_H );
        Raw_Pts [ I ] = new PointF ( X, Math.Max ( MT, Math.Min ( H - MB, Y ) ) );
      }
      using var Raw_Pen = new Pen ( Color.FromArgb ( 160, _Theme.Line_Colors [ 0 ] ), 1f );
      G.DrawLines ( Raw_Pen, Raw_Pts );

      // ── Rolling mean line ─────────────────────────────────────────────
      var Roll_Pts = new PointF [ N ];
      for ( int I = 0; I < N; I++ )
      {
        float X = ML + (float) I / ( N - 1 ) * Chart_W;
        float Y = H - MB - (float) ( ( Rolling [ I ] - Y_Min ) / Y_Range * Chart_H );
        Roll_Pts [ I ] = new PointF ( X, Math.Max ( MT, Math.Min ( H - MB, Y ) ) );
      }
      using var Roll_Pen = new Pen ( Color.FromArgb ( 220, Color.MediumSeaGreen ), 2f );
      G.DrawLines ( Roll_Pen, Roll_Pts );

      // ── Mean line ─────────────────────────────────────────────────────
      float Mean_Y = H - MB - (float) ( ( Mean_Ms - Y_Min ) / Y_Range * Chart_H );
      using var Mean_Pen = new Pen ( _Theme.Accent, 1.5f ) { DashStyle = DashStyle.Dash };
      G.DrawLine ( Mean_Pen, ML, Mean_Y, W - MR, Mean_Y );
      using var Mean_Lbl_Font = new Font ( "Consolas", 7f );
      G.DrawString ( $"mean {Mean_Ms:F1} ms", Mean_Lbl_Font, Label_Brush, W - MR + 3, Mean_Y - 8 );

      // ── Threshold line ────────────────────────────────────────────────
      float Thresh_Y = H - MB - (float) ( ( Threshold_Ms - Y_Min ) / Y_Range * Chart_H );
      using var Thresh_Pen = new Pen ( Color.FromArgb ( 160, Color.OrangeRed ), 1f )
      {
        DashStyle = DashStyle.Dash
      };
      G.DrawLine ( Thresh_Pen, ML, Thresh_Y, W - MR, Thresh_Y );
      G.DrawString ( "+20%", Mean_Lbl_Font, new SolidBrush ( Color.OrangeRed ), W - MR + 3, Thresh_Y - 8 );

      // ── Slowdown marker ───────────────────────────────────────────────
      if ( Slowdown_Index >= 0 )
      {
        float Slow_X = ML + (float) Slowdown_Index / ( N - 1 ) * Chart_W;
        using var Slow_Pen = new Pen ( Color.Orange, 2f );
        G.DrawLine ( Slow_Pen, Slow_X, MT, Slow_X, H - MB );

        string Slow_Time = _Times [ Slowdown_Index + 1 ].ToString ( "HH:mm:ss" );
        using var Slow_Font = new Font ( "Consolas", 7.5f, FontStyle.Bold );
        using var Slow_Brush = new SolidBrush ( Color.Orange );
        G.DrawString ( $"slowdown\n{Slow_Time}", Slow_Font, Slow_Brush,
            Slow_X + 4, MT + 4 );
      }

      // ── Legend ────────────────────────────────────────────────────────
      using var Leg_Font = new Font ( "Consolas", 7.5f );
      int Lx = ML + 8, Ly = MT + 6, Leg_Sp = 16;
      void Draw_Leg ( Color C, string Txt )
      {
        using var B = new SolidBrush ( C );
        using var P = new Pen ( C, 2f );
        G.DrawLine ( P, Lx, Ly + 5, Lx + 18, Ly + 5 );
        G.DrawString ( Txt, Leg_Font, B, Lx + 22, Ly );
        Ly += Leg_Sp;
      }
      Draw_Leg ( _Theme.Line_Colors [ 0 ], "raw interval" );
      Draw_Leg ( Color.MediumSeaGreen, $"rolling mean ({Roll_Win}-sample)" );

      // ── Stats summary ─────────────────────────────────────────────────
      double Pct_Slow = Intervals.Count ( V => V > Threshold_Ms ) * 100.0 / N;
      double Max_Ms = Intervals.Max ( );

      using var Stats_Font = new Font ( "Consolas", 8f );
      using var Stats_Brush = new SolidBrush ( _Theme.Foreground );
      string Stats =
          $"mean: {Mean_Ms:F1} ms   " +
          $"max: {Max_Ms:F1} ms   " +
          $"jitter σ: {_StdDev_Delta_Ms:F1} ms   " +
          $"slow cycles (>{Threshold_Ms:F0} ms): {Pct_Slow:F1}%";
      if ( Slowdown_Index >= 0 )
        Stats += $"   sustained slowdown: {_Times [ Slowdown_Index + 1 ]:HH:mm:ss}";

      var Stats_Sz = G.MeasureString ( Stats, Stats_Font );
      G.DrawString ( Stats, Stats_Font, Stats_Brush,
          ( W - Stats_Sz.Width ) / 2, H - MB + 20 );

      Draw_Title ( G, W, "Poll Intervals — sample-to-sample gap (ms)" );
      Draw_Time_Axis ( G, W, H, Chart_W, N );
    }


    // ─────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────

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



    private PointF [ ] Build_Points_Sampled (
    List<double> Values, int Count,
    int W, int H, int Chart_W, int Chart_H,
    double Y_Min, double Y_Range, int Max_Pts = 800 )
    {
      int Step = Math.Max ( 1, Count / Max_Pts );
      var Pts = new List<PointF> ( );
      for ( int I = 0; I < Count; I += Step )
      {
        float X = ML + (float) I / ( Count - 1 ) * Chart_W;
        float Y = H - MB - (float) ( ( Values [ I ] - Y_Min ) / Y_Range * Chart_H );
        Pts.Add ( new PointF ( X, Y ) );
      }
      return Pts.ToArray ( );
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

    private static string Color_Name ( Color C )
    {
      // Check known named colors by comparing RGB values
      KnownColor [ ] Known_Colors = (KnownColor [ ]) Enum.GetValues ( typeof ( KnownColor ) );
      foreach ( KnownColor KC in Known_Colors )
      {
        Color K = Color.FromKnownColor ( KC );
        if ( K.R == C.R && K.G == C.G && K.B == C.B )
          return K.Name;
      }

      // Fall back to hex
      return $"#{C.R:X2}{C.G:X2}{C.B:X2}";
    }

    private void Show_Tab_Help ( )
    {
      int Idx = _Tabs.SelectedIndex;
      if ( Idx < 0 || Idx >= _Tab_Help.Length )
        return;

      string Tab_Title = _Tabs.TabPages [ Idx ].Text;
      string Help_Text = _Tab_Help [ Idx ];

      var Dlg = new Form
      {
        Text = $"About: {Tab_Title}",
        Size = new Size ( 520, 480 ),
        MinimumSize = new Size ( 400, 300 ),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.Sizable,
        BackColor = SystemColors.Control,
      };

      // ── Header bar ────────────────────────────────────────────────────
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

      // ── Parse and render help sections ───────────────────────────────
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

      // Parse the help text into sections:
      // Lines starting with a word followed by newline = section header
      // Lines starting with "  –" or "  •" = bullet
      // Everything else = body text
      string [ ] Lines = Help_Text.Split ( '\n' );

      foreach ( string Raw_Line in Lines )
      {
        string Line = Raw_Line.TrimEnd ( );

        if ( string.IsNullOrWhiteSpace ( Line ) )
        {
          // Spacer
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
          // Section header with left accent bar
          var Row = new Panel
          {
            Width = 460,
            Height = 28,
            Margin = new Padding ( 0, 8, 0, 2 ),
          };

          var Accent = new Panel
          {
            Width = 4,
            Height = 28,
            Location = new Point ( 0, 0 ),
            BackColor = Color.FromArgb ( 0, 122, 204 ),
          };

          var Lbl = new Label
          {
            Text = Line.TrimEnd ( ':' ),
            Font = new Font ( "Segoe UI", 10f, FontStyle.Bold ),
            ForeColor = SystemColors.ControlText,
            Location = new Point ( 12, 4 ),
            AutoSize = true,
          };

          Row.Controls.Add ( Accent );
          Row.Controls.Add ( Lbl );
          Flow.Controls.Add ( Row );
        }
        else if ( Is_Bullet )
        {
          // Bullet row with colored dot
          var Row = new Panel
          {
            Width = 460,
            Height = 22,
            Margin = new Padding ( 0, 1, 0, 1 ),
          };

          bool Is_Dash = Line.StartsWith ( "  –" );
          string Bullet_Text = Line.TrimStart ( ).TrimStart ( '–', '•', ' ' ).Trim ( );

          var Dot = new Panel
          {
            Width = 6,
            Height = 6,
            Location = new Point ( 16, 8 ),
            BackColor = Is_Dash
                  ? Color.FromArgb ( 0, 180, 120 )
                  : Color.FromArgb ( 0, 122, 204 ),
          };

          var Lbl = new Label
          {
            Text = Bullet_Text,
            Font = new Font ( "Segoe UI", 9f ),
            ForeColor = SystemColors.ControlText,
            Location = new Point ( 30, 3 ),
            Width = 420,
            AutoSize = false,
            Height = 20,
          };

          Row.Controls.Add ( Dot );
          Row.Controls.Add ( Lbl );
          Flow.Controls.Add ( Row );
        }
        else
        {
          // Body text
          var Lbl = new Label
          {
            Text = Line,
            Font = new Font ( "Segoe UI", 9f ),
            ForeColor = SystemColors.ControlText,
            Width = 460,
            AutoSize = false,
            Height = 20,
            Margin = new Padding ( 0, 1, 0, 1 ),
          };
          Flow.Controls.Add ( Lbl );
        }
      }

      Scroll.Controls.Add ( Flow );

      // ── Close button ─────────────────────────────────────────────────
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
        Location = new Point ( 0, 8 ),
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
      };
      Close_Btn.Location = new Point ( 520 - 88 - 16, 8 );
      Close_Btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      Close_Btn.Click += ( s, e ) => Dlg.Close ( );

      Footer.Controls.Add ( Close_Btn );

      Dlg.Controls.Add ( Scroll );
      Dlg.Controls.Add ( Footer );
      Dlg.Controls.Add ( Header );

      Dlg.ShowDialog ( this );
    }
  }
}

