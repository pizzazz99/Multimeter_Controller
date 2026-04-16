
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
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════

using System.Drawing.Drawing2D;
using System.Globalization;
using static Trace_Execution_Namespace.Trace_Execution;
using Font = System.Drawing.Font;

namespace Multimeter_Controller
{
  public class Analysis_Popup_Form : Form
  {
    // ── Data types ────────────────────────────────────────────────────
    public record Instrument_Series( string Name, List<( DateTime Time, double Value )> Points );

    private class Series_Pair
    {
      public readonly string Name_A;
      public readonly string Name_B;
      public readonly List<double> Deltas;
      public readonly double       Mean, StdDev, Min, Max;
      public readonly double       Mean_uV, StdDev_uV, Min_uV, Max_uV;
      public readonly List<double> Rolling_StdDev = new();

      public Series_Pair( string                                Name_A,
                          List<( DateTime Time, double Value )> Pts_A,
                          string                                Name_B,
                          List<( DateTime Time, double Value )> Pts_B )
      {
        this.Name_A = Name_A;
        this.Name_B = Name_B;

        int Count = Math.Min( Pts_A.Count, Pts_B.Count );
        Deltas    = new List<double>( Count );
        for ( int I = 0; I < Count; I++ )
          Deltas.Add( Pts_A[ I ].Value - Pts_B[ I ].Value );

        Mean       = Deltas.Average();
        double Var = Deltas.Average( D => ( D - Mean ) * ( D - Mean ) );
        StdDev     = Math.Sqrt( Var );
        Min        = Deltas.Min();
        Max        = Deltas.Max();
        Mean_uV    = Mean * 1e6;
        StdDev_uV  = StdDev * 1e6;
        Min_uV     = Min * 1e6;
        Max_uV     = Max * 1e6;

        const int Window = 100;
        var       Q      = new Queue<double>();
        double    R_Sum = 0, R_Sum_Sq = 0;
        foreach ( double D in Deltas )
        {
          Q.Enqueue( D );
          R_Sum    += D;
          R_Sum_Sq += D * D;
          if ( Q.Count > Window )
          {
            double Old  = Q.Dequeue();
            R_Sum      -= Old;
            R_Sum_Sq   -= Old * Old;
          }
          int    N     = Q.Count;
          double R_Mu  = R_Sum / N;
          double R_Var = ( R_Sum_Sq / N ) - ( R_Mu * R_Mu );
          Rolling_StdDev.Add( R_Var > 0 ? Math.Sqrt( R_Var ) : 0.0 );
        }
      }
    }

    // ── Fields ────────────────────────────────────────────────────────
    private readonly List<Instrument_Series> _Instruments;
    private readonly List<Series_Pair> _Pairs;
    private readonly List<DateTime> _Times;
    private readonly List<double> _Delta_Ms;
    private readonly int          _Total_Points;
    private readonly int          _Total_All_Points;
    private readonly double       _Mean_Delta_Ms;
    private readonly double       _StdDev_Delta_Ms;
    private readonly double       _Max_Delta_Ms;

    private double                _Baseline_Ms;
    private double                _Threshold_Ms;
    private double                _Pct_Slow;
    private int                   _Slow_Count;
    private int                   _Slowdown_Index;
    private double                _Slope_Total;
    private string                _Trend;
    private List<double>          _Intervals;
    private List<double>          _Rolling_Mean;

    private TabControl            _Tabs;
    private Panel                 _Stats_Panel;
    private Panel                 _Timing_Panel;
    private readonly List<Panel> _Delta_Panels        = new();
    private readonly List<Panel> _Rolling_Panels      = new();
    private readonly List<Panel> _Histo_Panels        = new();
    private readonly List<Panel>          _Raw_Panels = new();

    private readonly Chart_Theme          _Theme;
    private readonly Application_Settings _Settings;

    private const int                     ML = 90, MR = 30, MT = 30, MB = 40;

    private int                           _Active_Pair_Index = 0;

    private TabAlignment                  _Tab_Alignment;

    // ── Constructors ──────────────────────────────────────────────────
    public Analysis_Popup_Form( List<Instrument_Series> Instruments,
                                Chart_Theme             Theme,
                                Application_Settings    Settings    = null,
                                string                  Master_Name = null ) // ← add master name
    {
      _Settings    = Settings;
      _Theme       = Theme ?? Chart_Theme.Dark_Preset();
      _Instruments = Instruments ?? throw new ArgumentNullException( nameof( Instruments ) );

      if ( _Instruments.Count == 0 )
        throw new ArgumentException( "At least one instrument required." );

      _Pairs = new List<Series_Pair>();

      if ( _Instruments.Count >= 3 && ! string.IsNullOrEmpty( Master_Name ) )
      {
        // ── 3+ instruments: pair master against every other instrument ──
        var Master = _Instruments.FirstOrDefault( I => I.Name == Master_Name );
        if ( Master != null )
        {
          foreach ( var Other in _Instruments.Where( I => I.Name != Master_Name ) )
            _Pairs.Add( new Series_Pair( Master.Name, Master.Points, Other.Name, Other.Points ) );
        }
      }
      else
      {
        // ── 2 instruments: original sequential pairing ──────────────────
        for ( int I = 0; I < _Instruments.Count - 1; I++ )
          _Pairs.Add( new Series_Pair( _Instruments[ I ].Name,
                                       _Instruments[ I ].Points,
                                       _Instruments[ I + 1 ].Name,
                                       _Instruments[ I + 1 ].Points ) );
      }

      var Pts0  = _Instruments[ 0 ].Points;
      _Times    = Pts0.Select( P => P.Time ).ToList();
      _Delta_Ms = new List<double>( Pts0.Count );
      for ( int I = 0; I < Pts0.Count; I++ )
        _Delta_Ms.Add( I == 0 ? 0.0 : ( Pts0[ I ].Time - Pts0[ I - 1 ].Time ).TotalMilliseconds );

      _Total_Points     = _Instruments[ 0 ].Points.Count;          // sample count (what shows in UI)
      _Total_All_Points = _Instruments.Sum( S => S.Points.Count ); // raw total across all series

      var Valid_Ms = _Delta_Ms.Skip( 1 ).ToList();
      if ( Valid_Ms.Count > 0 )
      {
        _Mean_Delta_Ms   = Valid_Ms.Average();
        double T_Var     = Valid_Ms.Average( D => ( D - _Mean_Delta_Ms ) * ( D - _Mean_Delta_Ms ) );
        _StdDev_Delta_Ms = Math.Sqrt( T_Var );
        _Max_Delta_Ms    = Valid_Ms.Max();
      }

      Precompute_Timing_Stats();
      Build_UI();
    }

    // Backwards-compat two-instrument constructor
    public Analysis_Popup_Form( List<( DateTime Time, double Value )> Points_A,
                                List<( DateTime Time, double Value )> Points_B,
                                string                                Name_A,
                                string                                Name_B,
                                Chart_Theme                           Theme )
        : this( Build_Instrument_List( Points_A, Points_B, Name_A, Name_B ), Theme )
    {
    }

    private static List<Instrument_Series> Build_Instrument_List( List<( DateTime Time, double Value )> Pts_A,
                                                                  List<( DateTime Time, double Value )> Pts_B,
                                                                  string Name_A,
                                                                  string Name_B )
    {
      var Result = new List<Instrument_Series> { new( Name_A, Pts_A ) };
      if ( Pts_B != null && Pts_B.Count > 0 )
        Result.Add( new Instrument_Series( Name_B, Pts_B ) );
      return Result;
    }

    // ─────────────────────────────────────────────────────────────────
    // UI Construction
    // ─────────────────────────────────────────────────────────────────

    private void Build_UI()
    {
      bool Has_Pairs  = _Pairs.Count > 0;
      bool Multi_Pair = _Pairs.Count > 1; // ← 3+ instruments with master

      Text = Has_Pairs
               ? $"Meter Comparison Analysis — {string.Join( ", ", _Instruments.Select( I => I.Name ) )}"
               : $"Analysis — {_Instruments[ 0 ].Name}";
      Size = new Size( 1100, 620 );
      MinimumSize   = new Size( 900, 500 );
      StartPosition = FormStartPosition.CenterParent;
      Padding       = new Padding( 0 );

      _Tabs = new TabControl {
        Dock      = DockStyle.Fill,
        Padding   = new Point( 10, 3 ),
        SizeMode  = TabSizeMode.Normal,
        Alignment = _Tab_Alignment,
        Multiline = true,
      };

      if ( Has_Pairs )
      {
        // ── Single set of chart panels, driven by _Active_Pair_Index ──
        var Delta_Panel = new Buffered_Panel();
        var Roll_Panel  = new Buffered_Panel();
        var Histo_Panel = new Buffered_Panel();
        var Raw_Panel   = new Buffered_Panel();

        Delta_Panel.Paint += ( s, e ) =>
          Draw_Delta_Chart( e.Graphics, _Pairs[ _Active_Pair_Index ], Delta_Panel );
        Roll_Panel.Paint += ( s, e ) =>
          Draw_Rolling_Chart( e.Graphics, _Pairs[ _Active_Pair_Index ], Roll_Panel );
        Histo_Panel.Paint += ( s, e ) =>
          Draw_Histogram_Panel( e.Graphics, _Pairs[ _Active_Pair_Index ], Histo_Panel );
        Raw_Panel.Paint += ( s, e ) => Draw_Raw_Chart( e.Graphics, _Pairs[ _Active_Pair_Index ], Raw_Panel );

        _Delta_Panels.Add( Delta_Panel );
        _Rolling_Panels.Add( Roll_Panel );
        _Histo_Panels.Add( Histo_Panel );
        _Raw_Panels.Add( Raw_Panel );

        Add_Tab( "Δ Delta", Delta_Panel );
        Add_Tab( "Rolling σ", Roll_Panel );
        Add_Tab( "Δ Distribution", Histo_Panel );
        Add_Tab( "Raw", Raw_Panel );
      }

      // ── Summary Stats — always all pairs, scrollable ───────────────
      _Stats_Panel         = new Buffered_Panel();
      _Stats_Panel.Dock    = DockStyle.None;
      _Stats_Panel.Size    = new Size( 860, 200 + _Pairs.Count * 260 + 380 );
      _Stats_Panel.Anchor  = AnchorStyles.Top | AnchorStyles.Left;
      _Stats_Panel.Paint += ( s, e ) => Draw_Stats_Panel( e.Graphics );

      var Stats_Scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
      Stats_Scroll.Controls.Add( _Stats_Panel );
      var Stats_Page = new TabPage( "Summary Stats" );
      Stats_Page.Controls.Add( Stats_Scroll );
      _Tabs.TabPages.Add( Stats_Page );

      // ── Poll Intervals ─────────────────────────────────────────────
      _Timing_Panel        = new Buffered_Panel();
      _Timing_Panel.Paint += ( s, e ) => Draw_Timing_Panel( e.Graphics );
      Add_Tab( "Poll Intervals", _Timing_Panel );

      // ── Bottom bar ─────────────────────────────────────────────────
      var Bottom_Panel = new Panel { Dock = DockStyle.Bottom, Height = 36 };

      var Help_Btn = new System.Windows.Forms.Button {
        Text     = "?  What am I looking at?",
        Size     = new Size( 180, 26 ),
        Location = new Point( 8, 5 ),
        Anchor   = AnchorStyles.Bottom | AnchorStyles.Left,
      };
      Help_Btn.Click += ( s, e ) => Show_Tab_Help();

      // ── Tab alignment combo ────────────────────────────────────────
      var Alignment_Label = new Label {
        Text     = "Tabs:",
        AutoSize = true,
        Location = new Point( 200, 10 ),
        Anchor   = AnchorStyles.Bottom | AnchorStyles.Left,
      };

      var Alignment_Combo = new System.Windows.Forms.ComboBox {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Size          = new Size( 80, 26 ),
        Location      = new Point( 240, 5 ),
        Anchor        = AnchorStyles.Bottom | AnchorStyles.Left,
      };
      Alignment_Combo.Items.AddRange( new object[ ] { "Top", "Left", "Right" } );
      Alignment_Combo.SelectedItem = _Tab_Alignment switch {
        TabAlignment.Left  => "Left",
        TabAlignment.Right => "Right",
        _                  => "Top",
      };
      Alignment_Combo.SelectedIndexChanged += ( s, e ) =>
      {
        _Tab_Alignment = Alignment_Combo.SelectedItem switch {
          "Left"  => TabAlignment.Left,
          "Right" => TabAlignment.Right,
          _       => TabAlignment.Top,
        };
        _Tabs.Alignment = _Tab_Alignment;
        _Tabs.Multiline = _Tab_Alignment != TabAlignment.Top;
        if ( _Settings != null )
          _Settings.Analysis_Tab_Alignment = _Tab_Alignment;
      };

      // ── Pair selector — only shown for 3+ instruments ──────────────
      if ( Multi_Pair )
      {
        var Pair_Label = new Label {
          Text     = "Pair:",
          AutoSize = true,
          Location = new Point( 334, 10 ),
          Anchor   = AnchorStyles.Bottom | AnchorStyles.Left,
        };

        var Pair_Combo = new System.Windows.Forms.ComboBox {
          DropDownStyle = ComboBoxStyle.DropDownList,
          Size          = new Size( 200, 26 ),
          Location      = new Point( 370, 5 ),
          Anchor        = AnchorStyles.Bottom | AnchorStyles.Left,
        };

        foreach ( var Pair in _Pairs )
          Pair_Combo.Items.Add( $"{Pair.Name_A}  ↔  {Pair.Name_B}" );

        Pair_Combo.SelectedIndex         = 0;
        Pair_Combo.SelectedIndexChanged += ( s, e ) =>
        {
          _Active_Pair_Index = Pair_Combo.SelectedIndex;
          Invalidate_Chart_Panels();
        };

        Bottom_Panel.Controls.Add( Pair_Label );
        Bottom_Panel.Controls.Add( Pair_Combo );
      }

      // ── Close button ───────────────────────────────────────────────
      var Close_Btn = new System.Windows.Forms.Button {
        Text   = "Close",
        Size   = new Size( 80, 26 ),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
      };
      Close_Btn.Click += ( s, e ) => Close();
      Bottom_Panel.Layout += ( s, e ) => Close_Btn.Location = new Point( Bottom_Panel.Width - 90, 5 );

      Bottom_Panel.Controls.Add( Help_Btn );
      Bottom_Panel.Controls.Add( Alignment_Label );
      Bottom_Panel.Controls.Add( Alignment_Combo );
      Bottom_Panel.Controls.Add( Close_Btn );

      Controls.Add( _Tabs );
      Controls.Add( Bottom_Panel );

      _Tabs.SelectedIndexChanged += ( s, e ) =>
      {
        var Tab = _Tabs.SelectedTab;
        if ( Tab == null || Tab.Controls.Count == 0 )
          return;
        var First = Tab.Controls[ 0 ];
        if ( First is Buffered_Panel BP )
          BP.Invalidate();
        else if ( First is Panel Scroll && Scroll.Controls.Count > 0 )
          ( Scroll.Controls[ 0 ] as Buffered_Panel )?.Invalidate();
      };
    }

    // ── Helper to redraw all chart panels when pair changes ────────────
    private void Invalidate_Chart_Panels()
    {
      foreach ( var P in _Delta_Panels )
        P.Invalidate();
      foreach ( var P in _Rolling_Panels )
        P.Invalidate();
      foreach ( var P in _Histo_Panels )
        P.Invalidate();
      foreach ( var P in _Raw_Panels )
        P.Invalidate();
    }

    private void Add_Tab( string Title, Panel Content )
    {
      var Page     = new TabPage( Title );
      Content.Dock = DockStyle.Fill;
      Page.Controls.Add( Content );
      _Tabs.TabPages.Add( Page );
    }

    // ─────────────────────────────────────────────────────────────────
    // Tab — Delta over time
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Delta_Chart( Graphics G, Series_Pair Pair, Panel Host )
    {
      int W = Host.ClientSize.Width;
      int H = Host.ClientSize.Height;

      Setup_Graphics( G, W, H );

      int Count   = Pair.Deltas.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;
      if ( Count < 2 || Chart_W < 20 || Chart_H < 20 )
        return;

      double Y_Min = Pair.Min - Math.Abs( Pair.Min ) * 0.2;
      double Y_Max = Pair.Max + Math.Abs( Pair.Max ) * 0.2;
      if ( Y_Max - Y_Min < 1e-12 )
      {
        Y_Min -= 1e-7;
        Y_Max += 1e-7;
      }
      double Y_Range = Y_Max - Y_Min;

      Draw_Grid( G, W, H, Chart_W, Chart_H, Y_Min, Y_Range, "µV", V => ( V * 1e6 ).ToString( "F2" ) );

      var       Pts = Build_Points_Sampled( Pair.Deltas, Count, W, H, Chart_W, Chart_H, Y_Min, Y_Range );
      int       Pts_Count = Pts.Length;

      using var Fill_Brush = new LinearGradientBrush( new PointF( 0, MT ),
                                                      new PointF( 0, H - MB ),
                                                      Color.FromArgb( 50, _Theme.Line_Colors[ 0 ] ),
                                                      Color.FromArgb( 5, _Theme.Line_Colors[ 0 ] ) );

      var       Fill_Pts = new PointF[ Pts_Count + 2 ];
      Array.Copy( Pts, Fill_Pts, Pts_Count );
      Fill_Pts[ Pts_Count ]     = new PointF( Pts[ Pts_Count - 1 ].X, H - MB );
      Fill_Pts[ Pts_Count + 1 ] = new PointF( Pts[ 0 ].X, H - MB );
      G.FillPolygon( Fill_Brush, Fill_Pts );

      using var Line_Pen = new Pen( _Theme.Line_Colors[ 0 ], 1.5f );
      G.DrawLines( Line_Pen, Pts );

      float     Zero_Y   = H - MB - (float) ( ( 0 - Y_Min ) / Y_Range * Chart_H );
      using var Zero_Pen = new Pen( Color.FromArgb( 180, Color.Red ), 1.5f ) { DashStyle = DashStyle.Dash };
      G.DrawLine( Zero_Pen, ML, Zero_Y, W - MR, Zero_Y );

      float     Mean_Y   = H - MB - (float) ( ( Pair.Mean - Y_Min ) / Y_Range * Chart_H );
      using var Mean_Pen = new Pen( _Theme.Accent, 1.5f ) { DashStyle = DashStyle.Dash };
      G.DrawLine( Mean_Pen, ML, Mean_Y, W - MR, Mean_Y );

      Draw_Title(
        G,
        W,
        $"Delta  ({Pair.Name_A} − {Pair.Name_B})  |  mean: {Pair.Mean_uV:F3} µV  σ: {Pair.StdDev_uV:F3} µV" );
      Draw_Time_Axis( G, W, H, Chart_W, Count );
    }

    // ─────────────────────────────────────────────────────────────────
    // Tab — Rolling σ over time
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Rolling_Chart( Graphics G, Series_Pair Pair, Panel Host )
    {
      int W = Host.ClientSize.Width;
      int H = Host.ClientSize.Height;

      Setup_Graphics( G, W, H );

      int Count   = Pair.Rolling_StdDev.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;
      if ( Count < 2 || Chart_W < 20 || Chart_H < 20 )
        return;

      double Y_Max   = Pair.Rolling_StdDev.Max() * 1.15;
      double Y_Min   = 0;
      double Y_Range = Y_Max > 0 ? Y_Max : 1e-9;

      Draw_Grid( G, W, H, Chart_W, Chart_H, Y_Min, Y_Range, "µV", V => ( V * 1e6 ).ToString( "F3" ) );

      var       Pts = Build_Points( Pair.Rolling_StdDev, Count, W, H, Chart_W, Chart_H, Y_Min, Y_Range );

      using var Fill_Brush = new LinearGradientBrush( new PointF( 0, MT ),
                                                      new PointF( 0, H - MB ),
                                                      Color.FromArgb( 60, _Theme.Line_Colors[ 1 ] ),
                                                      Color.FromArgb( 5, _Theme.Line_Colors[ 1 ] ) );

      var       Fill_Pts = new PointF[ Count + 2 ];
      Array.Copy( Pts, Fill_Pts, Count );
      Fill_Pts[ Count ]     = new PointF( Pts[ Count - 1 ].X, H - MB );
      Fill_Pts[ Count + 1 ] = new PointF( Pts[ 0 ].X, H - MB );
      G.FillPolygon( Fill_Brush, Fill_Pts );

      using var Line_Pen = new Pen( _Theme.Line_Colors[ 1 ], 1.5f );
      G.DrawLines( Line_Pen, Pts );

      Draw_Title( G, W, $"Rolling σ of Delta  ({Pair.Name_A} − {Pair.Name_B})  |  100-sample window" );
      Draw_Time_Axis( G, W, H, Chart_W, Count );
    }

    // ─────────────────────────────────────────────────────────────────
    // Tab — Summary stats
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Stats_Panel( Graphics G )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      int       W = _Stats_Panel.ClientSize.Width;
      int       H = _Stats_Panel.ClientSize.Height;
      G.Clear( SystemColors.Control );

      using var Title_Font  = new Font( "Segoe UI", 12f, FontStyle.Bold );
      using var Header_Font = new Font( "Segoe UI", 10f, FontStyle.Bold );
      using var Value_Font  = new Font( "Courier New", 11f );
      using var Lbl_Font    = new Font( "Segoe UI", 10f ); // ← add this
      using var Fg_Brush    = new SolidBrush( SystemColors.ControlText );
      using var Dim_Brush   = new SolidBrush( SystemColors.GrayText );
      using var Sep_Pen     = new Pen( _Theme.Grid, 1f );

      int       Col1 = 40, Col2 = 360, Col3 = 580;
      int       Y   = 30;
      int       Row = 30;

      string    Title = _Pairs.Count == 0
                          ? $"Analysis: {_Instruments[0].Name}"
                          : $"Comparison: {string.Join(" → ", _Instruments.Select(I => I.Name))}";
      G.DrawString( Title, Title_Font, Fg_Brush, Col1, Y );
      Y += Row + 8;

      // ── One delta block per pair ──────────────────────────────────────
      foreach ( var Pair in _Pairs )
      {
        G.DrawLine( Sep_Pen, Col1, Y, W - 40, Y );
        Y += 12;
        G.DrawString( $"Δ  ({Pair.Name_A} − {Pair.Name_B})", Header_Font, Fg_Brush, Col1, Y );
        G.DrawString( "Volts", Header_Font, Fg_Brush, Col2, Y );
        G.DrawString( "µV", Header_Font, Fg_Brush, Col3, Y );
        Y += Row;
        Draw_Stat_Row( G,
                       Value_Font,
                       Lbl_Font,
                       Fg_Brush,
                       Fg_Brush,
                       "Mean",
                       Pair.Mean,
                       Pair.Mean_uV,
                       Col1,
                       Col2,
                       Col3,
                       Y );
        Y += Row;
        Draw_Stat_Row( G,
                       Value_Font,
                       Lbl_Font,
                       Fg_Brush,
                       Fg_Brush,
                       "σ",
                       Pair.StdDev,
                       Pair.StdDev_uV,
                       Col1,
                       Col2,
                       Col3,
                       Y );
        Y += Row;
        Draw_Stat_Row( G,
                       Value_Font,
                       Lbl_Font,
                       Fg_Brush,
                       Fg_Brush,
                       "Min",
                       Pair.Min,
                       Pair.Min_uV,
                       Col1,
                       Col2,
                       Col3,
                       Y );
        Y += Row;
        Draw_Stat_Row( G,
                       Value_Font,
                       Lbl_Font,
                       Fg_Brush,
                       Fg_Brush,
                       "Max",
                       Pair.Max,
                       Pair.Max_uV,
                       Col1,
                       Col2,
                       Col3,
                       Y );
        Y += Row;
        Draw_Stat_Row( G,
                       Value_Font,
                       Lbl_Font,
                       Fg_Brush,
                       Fg_Brush,
                       "Range",
                       Pair.Max - Pair.Min,
                       Pair.Max_uV - Pair.Min_uV,
                       Col1,
                       Col2,
                       Col3,
                       Y );
        Y += Row + 16;
      }

      if ( _Pairs.Count == 0 )
      {
        G.DrawString( "Δ (A − B) — N/A (single instrument)", Header_Font, Dim_Brush, Col1, Y );
        Y += Row * 2 + 16;
      }

      // ── Sample Timing ─────────────────────────────────────────────────
      G.DrawLine( Sep_Pen, Col1, Y, W - 40, Y );
      Y += 12;
      G.DrawString( "Sample Timing", Header_Font, Fg_Brush, Col1, Y );
      Y += Row;

      Draw_Stat_Row_Single( G,
                            Value_Font,
                            Lbl_Font,
                            Fg_Brush,
                            Fg_Brush,
                            "Mean interval",
                            $"{_Mean_Delta_Ms:F2} ms  ({1000.0 / _Mean_Delta_Ms:F2} samples/sec)",
                            Col1,
                            Col2,
                            Y );
      Y += Row;
      Draw_Stat_Row_Single( G,
                            Value_Font,
                            Lbl_Font,
                            Fg_Brush,
                            Fg_Brush,
                            "Jitter (σ)",
                            $"{_StdDev_Delta_Ms:F2} ms",
                            Col1,
                            Col2,
                            Y );
      Y += Row;
      Draw_Stat_Row_Single( G,
                            Value_Font,
                            Lbl_Font,
                            Fg_Brush,
                            Fg_Brush,
                            "Max interval",
                            $"{_Max_Delta_Ms:F2} ms",
                            Col1,
                            Col2,
                            Y );
      Y += Row;
      Draw_Stat_Row_Single( G,
                            Value_Font,
                            Lbl_Font,
                            Fg_Brush,
                            Fg_Brush,
                            "Point count",
                            $"{_Total_Points:N0} per instrument  ({_Total_All_Points:N0} total)",
                            Col1,
                            Col2,
                            Y );

      Y += Row + 16;

      // ── Decimation Info ───────────────────────────────────────────────
      if ( _Settings != null )
      {

        Capture_Trace.Write( $"Decimation:" );
        Capture_Trace.Write( $" Enable       = {_Settings.Enable_Decimation}" );
        Capture_Trace.Write( $" Total_Points = {_Total_Points}" );
        Capture_Trace.Write( $" Threshold    = {_Settings.Decimation_Threshold}" );
        Capture_Trace.Write( $" Kicked_In    = {_Total_Points > _Settings.Decimation_Threshold}" );
        Capture_Trace.Write( "" );

        G.DrawLine( Sep_Pen, Col1, Y, W - 40, Y );
        Y += 12;
        G.DrawString( "Chart Decimation", Header_Font, Fg_Brush, Col1, Y );
        Y += Row;

        if ( _Settings.Enable_Decimation )
        {
          bool Kicked_In = _Total_Points > _Settings.Decimation_Threshold;

          Draw_Stat_Row_Single( G,
                                Value_Font,
                                Lbl_Font,
                                Fg_Brush,
                                Dim_Brush,
                                "Status",
                                Kicked_In ? "Active"
                                          : "Enabled but not triggered — point count below threshold",
                                Col1,
                                Col2,
                                Y );
          Y += Row;

          if ( Kicked_In )
          {
            int Stored = _Total_Points;
            int Drawn = Stored > _Settings.Decimation_Threshold ? Stored / _Settings.Decimation_Step : Stored;
            double Fidelity = Stored > 0 ? ( Drawn * 100.0 ) / Stored : 100.0;

            Draw_Stat_Row_Single( G,
                                  Value_Font,
                                  Lbl_Font,
                                  Fg_Brush,
                                  Dim_Brush,
                                  "Stored points",
                                  $"{Stored:N0}",
                                  Col1,
                                  Col2,
                                  Y );
            Y += Row;
            Draw_Stat_Row_Single( G,
                                  Value_Font,
                                  Lbl_Font,
                                  Fg_Brush,
                                  Dim_Brush,
                                  "Drawn points",
                                  $"{Drawn:N0}  (every {_Settings.Decimation_Step}th point)",
                                  Col1,
                                  Col2,
                                  Y );
            Y += Row;
            Draw_Stat_Row_Single( G,
                                  Value_Font,
                                  Lbl_Font,
                                  Fg_Brush,
                                  Dim_Brush,
                                  "Visual fidelity",
                                  $"{Fidelity:F1}% of stored data visible on chart",
                                  Col1,
                                  Col2,
                                  Y );
            Y += Row;

            if ( _Settings.Decimation_Step > 20 )
            {
              using var Warn_Brush = new SolidBrush( Color.OrangeRed );
              G.DrawString( "  ⚠ High decimation step — peaks and troughs between drawn points " + "may " +
                              "not be " + "visible" + " on " + "the " + "chart. " + "Export " + "CSV " +
                              "for " + "full " + "data.",
                            Lbl_Font,
                            Warn_Brush,
                            Col1 + 16,
                            Y );
              Y += Row;
            }
          }
        }
        else
        {
          Draw_Stat_Row_Single( G,
                                Value_Font,
                                Lbl_Font,
                                Fg_Brush,
                                Dim_Brush,
                                "Status",
                                "Disabled — all stored points drawn",
                                Col1,
                                Col2,
                                Y );
          Y += Row;
        }

        Y += 8;
      }

      // ── Polling Health ────────────────────────────────────────────────
      G.DrawLine( Sep_Pen, Col1, Y, W - 40, Y );
      Y += 12;
      G.DrawString( "Polling Health", Header_Font, Fg_Brush, Col1, Y );
      Y += Row;

      var Valid_Ms       = _Delta_Ms.Skip( 1 ).ToList();
      int Interval_Count = _Intervals?.Count ?? 0;

      if ( Interval_Count > 2 )
      {
        double Baseline       = _Baseline_Ms;
        double Threshold      = _Threshold_Ms;
        double Slope_Total    = _Slope_Total;
        double Pct_Slow       = _Pct_Slow;
        int    Slowdown_Index = _Slowdown_Index;
        double Peak_Ms        = _Intervals.Max();

        // ── Declare missing locals ────────────────────────────────────────
        string Pct_Str = Pct_Slow < 0.1 && Pct_Slow > 0 ? $"{Pct_Slow:F3}%" : $"{Pct_Slow:F1}%";

        string Slow_Time_Str =
          Slowdown_Index >= 0
            ? $"{_Times[Math.Clamp(Slowdown_Index + 1, 0, _Times.Count - 1)]:HH:mm:ss}  (sample {Slowdown_Index:N0})"
            : "None detected";

        using var Slow_Brush = new SolidBrush( Pct_Slow > 10  ? Color.OrangeRed
                                               : Pct_Slow > 2 ? Color.Orange
                                                              : SystemColors.ControlText );

        Draw_Stat_Row_Single( G,
                              Value_Font,
                              Lbl_Font,
                              Fg_Brush,
                              Dim_Brush,
                              "Baseline interval",
                              $"{Baseline:F2} ms   ({1000.0 / Baseline:F1} S/s)",
                              Col1,
                              Col2,
                              Y );
        Y += Row;
        Draw_Stat_Row_Single( G,
                              Value_Font,
                              Lbl_Font,
                              Fg_Brush,
                              Dim_Brush,
                              "Slowdown threshold",
                              $"{Threshold:F2} ms   (+20%)",
                              Col1,
                              Col2,
                              Y );
        Y += Row;
        Draw_Stat_Row_Single( G,
                              Value_Font,
                              Lbl_Font,
                              Fg_Brush,
                              Dim_Brush,
                              "First sustained slow",
                              Slow_Time_Str,
                              Col1,
                              Col2,
                              Y );
        Y += Row;
        Draw_Stat_Row_Single(
          G,
          Value_Font,
          Lbl_Font,
          Slow_Brush,
          Dim_Brush,
          "Slow cycles",
          $"{Pct_Str}   ({_Intervals.Count(V => V > _Threshold_Ms):N0} of {Interval_Count:N0})",
          Col1,
          Col2,
          Y );
        Y += Row;
        Draw_Stat_Row_Single( G,
                              Value_Font,
                              Lbl_Font,
                              Fg_Brush,
                              Dim_Brush,
                              "Peak interval",
                              $"{Peak_Ms:F1} ms   ({Peak_Ms / Baseline:F1}x baseline)",
                              Col1,
                              Col2,
                              Y );
        Y += Row;
        Draw_Stat_Row_Single( G, Value_Font, Lbl_Font, Fg_Brush, Dim_Brush, "Trend", _Trend, Col1, Col2, Y );
        Y += Row;
      }
      // ── Interpretation ────────────────────────────────────────────────
      Y += 6;
      G.DrawLine( Sep_Pen, Col1, Y, W - 40, Y );
      Y += 12;
      G.DrawString( "Interpretation", Header_Font, Fg_Brush, Col1, Y );
      Y += Row;

      using var Interp_Font = new Font( "Segoe UI", 9f );
      using var Interp_Dim  = new SolidBrush( _Theme.Labels );

      if ( Interval_Count > 2 )
      {
        int    Slow_Count = _Intervals.Count( V => V > _Threshold_Ms );
        double Peak_Ms    = _Intervals.Max();
        double Peak_Ratio = Peak_Ms / _Baseline_Ms;
        double Jitter_Pct = ( _StdDev_Delta_Ms / _Baseline_Ms ) * 100.0;
        double Drift_Pct  = Math.Abs( _Slope_Total ) / _Baseline_Ms * 100.0;
        string Pct_Str    = _Pct_Slow < 0.1 && _Pct_Slow > 0 ? $"{_Pct_Slow:F3}%" : $"{_Pct_Slow:F1}%";

        var    Interp = new System.Text.StringBuilder();

        // ── Threshold explanation ──────────────────────────────────────
        Interp.AppendLine(
          $"Slowdown threshold: {_Threshold_Ms:F2} ms (+20%) means the baseline polling interval is " +
          $"{_Baseline_Ms:F2} ms, and any cycle taking longer than {_Threshold_Ms:F2} ms is flagged as slow." );
        Interp.AppendLine();

        // ── Slow cycle health ──────────────────────────────────────────
        string Health =
          _Pct_Slow == 0
            ? $"Polling consistency was excellent — no cycles exceeded the slowdown threshold ({Interval_Count:N0} cycles measured)."
          : _Pct_Slow < 1
            ? $"Polling was very consistent — only {Slow_Count} of {Interval_Count:N0} cycles ({Pct_Str}) exceeded the threshold."
          : _Pct_Slow < 10
            ? $"Polling was mostly consistent — {Slow_Count} of {Interval_Count:N0} cycles ({Pct_Str}) were slow."
            : $"Polling had significant slowdowns — {Slow_Count} of {Interval_Count:N0} cycles ({Pct_Str}) exceeded the threshold.";
        Interp.AppendLine( Health );
        Interp.AppendLine();

        // ── Jitter analysis ────────────────────────────────────────────
        string Jitter_Text =
          Jitter_Pct < 1.0
            ? $"Timing jitter was negligible at {_StdDev_Delta_Ms:F2} ms ({Jitter_Pct:F2}% of baseline) — " +
                $"the poll loop ran with very consistent spacing."
          : Jitter_Pct < 5.0
            ? $"Timing jitter was low at {_StdDev_Delta_Ms:F2} ms ({Jitter_Pct:F2}% of baseline) — " +
                $"minor system scheduling variation, normal for Windows."
            : $"Timing jitter was elevated at {_StdDev_Delta_Ms:F2} ms ({Jitter_Pct:F2}% of baseline) — " +
                $"system load may be affecting poll spacing.";
        Interp.AppendLine( Jitter_Text );
        Interp.AppendLine();

        // ── Peak interval ──────────────────────────────────────────────
        string Peak_Text =
          Peak_Ratio < 1.05  ? $"Peak interval was {Peak_Ms:F1} ms ({Peak_Ratio:F2}x baseline) — " +
                                 $"the worst single cycle was essentially identical to the baseline."
          : Peak_Ratio < 1.2 ? $"Peak interval was {Peak_Ms:F1} ms ({Peak_Ratio:F2}x baseline) — " +
                                 $"a minor outlier but within acceptable range."
                             : $"Peak interval was {Peak_Ms:F1} ms ({Peak_Ratio:F2}x baseline) — " +
                                 $"at least one cycle had a significant delay.";
        Interp.AppendLine( Peak_Text );
        Interp.AppendLine();

        // ── Drift / trend ──────────────────────────────────────────────
        string Drift_Text =
          Math.Abs( _Slope_Total ) < 0.5
            ? "Trend: Polling speed was stable throughout the session — no " + "drift detected."
          : _Slope_Total > 0
            ? Drift_Pct < 1.0
                ? $"Trend: A minor drift of +{_Slope_Total:F1} ms was detected over the session " +
                    $"({Drift_Pct:F2}% of baseline) — this is negligible."
              : Drift_Pct < 5.0
                ? $"Trend: Polling gradually slowed by +{_Slope_Total:F1} ms over the session " +
                    $"({Drift_Pct:F2}% of baseline) — mild system load increase is likely."
                : $"Trend: Significant slowdown of +{_Slope_Total:F1} ms detected over the session " +
                    $"({Drift_Pct:F2}% of baseline) — consider enabling the rolling window to reduce render cost."
            : $"Trend: Polling speed improved by {Math.Abs(_Slope_Total):F1} ms over the session.";
        Interp.AppendLine( Drift_Text );
        Interp.AppendLine();

        // ── Overall verdict ────────────────────────────────────────────
        string Verdict = _Pct_Slow == 0 && Jitter_Pct < 2.0 && Peak_Ratio < 1.1 && Drift_Pct < 1.0
                           ? "Overa" + "ll: " + "Sessi" + "on " + "quali" + "ty " + "was " + "excel" +
                               "lent." + " Data" + " coll" + "ectio" + "n " + "was " + "highl" + "y " +
                               "consi" + "stent" + " with" + " no " + "anoma" + "lies " + "detec" + "ted."
                         : _Pct_Slow < 2 && Jitter_Pct < 5.0 && Peak_Ratio < 1.5
                           ? "Overall: Session " + "quality was good. " + "Minor variations were " +
                               "within normal Windows " + "scheduling tolerances."
                         : _Pct_Slow < 10
                           ? "Overall: Session quality was acceptable. Some polling " +
                               "variation was detected — review the Poll Intervals tab for " + "details."
                           : "Overall: Session had notable polling issues. Long runs with " +
                               "all points displayed may cause increasing slowdown — consider " +
                               "enabling the rolling window.";
        Interp.AppendLine( Verdict );

        // ── Render all lines ───────────────────────────────────────────
        foreach ( string Line in Interp.ToString().Split( '\n' ) )
        {
          if ( string.IsNullOrWhiteSpace( Line ) )
            Y += 6;
          else
            Draw_Wrapped_Text( G, Interp_Font, Interp_Dim, Line.Trim(), Col1 + 16, ref Y, W - 80, Row );
        }
      }
      else
      {
        Draw_Wrapped_Text( G,
                           Interp_Font,
                           Interp_Dim,
                           "Not enough data for polling health analysis — need at least 3 intervals.",
                           Col1 + 16,
                           ref Y,
                           W - 80,
                           Row );
      }
    }

    // ─────────────────────────────────────────────────────────────────
    // Tab — Delta histogram + bell curve
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Histogram_Panel( Graphics G, Series_Pair Pair, Panel Host )
    {
      int W = Host.ClientSize.Width;
      int H = Host.ClientSize.Height;

      Setup_Graphics( G, W, H );

      int Count   = Pair.Deltas.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;
      if ( Count < 5 || Chart_W < 20 || Chart_H < 20 )
        return;

      var    UV_Deltas = Pair.Deltas.Select( D => D * 1e6 ).ToList();
      double UV_Min    = UV_Deltas.Min();
      double UV_Max    = UV_Deltas.Max();
      double Range     = UV_Max - UV_Min;
      if ( Range < 1e-9 )
      {
        Range   = 1.0;
        UV_Min -= 0.5;
        UV_Max += 0.5;
      }

      int    Num_Bins = Math.Clamp( (int) Math.Ceiling( 1.0 + 3.322 * Math.Log10( Count ) ), 8, 40 );
      double Bin_W    = Range / Num_Bins;
      var    Bins     = new int[ Num_Bins ];
      foreach ( double V in UV_Deltas )
        Bins[ Math.Clamp( (int) ( ( V - UV_Min ) / Bin_W ), 0, Num_Bins - 1 ) ]++;

      int       Max_Count = Math.Max( 1, Bins.Max() );
      double    Y_Max_Val = Max_Count * 1.15;

      using var Grid_Pen    = new Pen( _Theme.Grid, 1f );
      using var Label_Font  = new Font( "Consolas", 7.5f );
      using var Label_Brush = new SolidBrush( _Theme.Labels );

      for ( int I = 0; I <= 5; I++ )
      {
        double Frac = (double) I / 5;
        int    Y    = H - MB - (int) ( Frac * Chart_H );
        G.DrawLine( Grid_Pen, ML, Y, W - MR, Y );
        string Lbl = ( (int) Math.Round( Frac * Y_Max_Val ) ).ToString();
        var    Sz  = G.MeasureString( Lbl, Label_Font );
        G.DrawString( Lbl, Label_Font, Label_Brush, ML - Sz.Width - 4, Y - Sz.Height / 2 );
      }

      Color     Bar_Color   = _Theme.Line_Colors[ 0 ];
      using var Bar_Brush   = new SolidBrush( Color.FromArgb( 160, Bar_Color ) );
      using var Bar_Pen     = new Pen( Color.FromArgb( 220, Bar_Color ), 1f );
      float     Bar_Spacing = (float) Chart_W / Num_Bins;
      float     Bar_Draw_W  = Bar_Spacing * 0.85f;

      for ( int I = 0; I < Num_Bins; I++ )
      {
        if ( Bins[ I ] == 0 )
          continue;
        float Bar_H = (float) ( Bins[ I ] / Y_Max_Val * Chart_H );
        float X     = ML + I * Bar_Spacing;
        float Y     = H - MB - Bar_H;
        G.FillRectangle( Bar_Brush, X, Y, Bar_Draw_W, Bar_H );
        G.DrawRectangle( Bar_Pen, X, Y, Bar_Draw_W, Bar_H );
      }

      double UV_Mean   = Pair.Mean_uV;
      double UV_StdDev = Pair.StdDev_uV;

      if ( UV_StdDev > 1e-12 )
      {
        double Scale   = Bin_W * Count;
        int    Num_Pts = 200;
        var    Curve   = new PointF[ Num_Pts ];
        for ( int I = 0; I < Num_Pts; I++ )
        {
          double X_Val = UV_Min + ( I / (double) ( Num_Pts - 1 ) ) * Range;
          double Z     = ( X_Val - UV_Mean ) / UV_StdDev;
          double PDF   = ( 1.0 / ( UV_StdDev * Math.Sqrt( 2 * Math.PI ) ) ) * Math.Exp( -0.5 * Z * Z );
          float  Px    = ML + (float) ( ( X_Val - UV_Min ) / Range * Chart_W );
          float  Py    = H - MB - (float) ( ( PDF * Scale / Y_Max_Val ) * Chart_H );
          Curve[ I ]   = new PointF( Px, Py );
        }

        // ── Clip to chart area so curve and sigma lines can't escape ───
        G.SetClip( new Rectangle( ML, MT, Chart_W, Chart_H ) );

        using var Curve_Pen = new Pen( _Theme.Line_Colors[ 1 ], 2.5f );
        G.DrawLines( Curve_Pen, Curve );

        float     Mean_X   = ML + (float) ( ( UV_Mean - UV_Min ) / Range * Chart_W );
        using var Mean_Pen = new Pen( _Theme.Accent, 2f ) { DashStyle = DashStyle.Dash };
        G.DrawLine( Mean_Pen, Mean_X, MT, Mean_X, H - MB );

        using var Sigma_Pen =
          new Pen( Color.FromArgb( 120, Color.Gold ), 1.5f ) { DashStyle = DashStyle.Dot };
        using var Sigma_Font  = new Font( "Consolas", 7f );
        using var Sigma_Brush = new SolidBrush( Color.Gold );
        foreach ( var ( Mult, Lbl ) in new[ ] { ( -2, "-2σ" ), ( -1, "-1σ" ), ( 1, "+1σ" ), ( 2, "+2σ" ) } )
        {
          double Sx_Val = UV_Mean + Mult * UV_StdDev;
          if ( Sx_Val < UV_Min || Sx_Val > UV_Max )
            continue;
          float Sx = ML + (float) ( ( Sx_Val - UV_Min ) / Range * Chart_W );
          G.DrawLine( Sigma_Pen, Sx, MT, Sx, H - MB );
          G.DrawString( Lbl, Sigma_Font, Sigma_Brush, Sx + 2, MT + 2 );
        }

        // ── Restore before labels and title ────────────────────────────
        G.ResetClip();
      }

      using var X_Font     = new Font( "Consolas", 6.5f );
      int       Label_Step = Math.Max( 1, Num_Bins / 8 );
      for ( int I = 0; I < Num_Bins; I += Label_Step )
      {
        double Center = UV_Min + ( I + 0.5 ) * Bin_W;
        string Lbl    = $"{Center:F1}";
        var    Sz     = G.MeasureString( Lbl, X_Font );
        float  X      = ML + I * Bar_Spacing + Bar_Spacing / 2;
        G.DrawString( Lbl, X_Font, Label_Brush, X - Sz.Width / 2, H - MB + 4 );
      }

      Draw_Title( G,
                  W,
                  $"Δ Distribution (µV)  |  {Pair.Name_A} − {Pair.Name_B}  |  " +
                    $"mean: {UV_Mean:F3} µV  σ: {UV_StdDev:F3} µV  n: {Count:N0}" );
    }

    // ─────────────────────────────────────────────────────────────────
    // Tab — Raw delta (every point)
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Raw_Chart( Graphics G, Series_Pair Pair, Panel Host )
    {
      int W = Host.ClientSize.Width;
      int H = Host.ClientSize.Height;

      Setup_Graphics( G, W, H );

      int Count   = Pair.Deltas.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;
      if ( Count < 2 || Chart_W < 20 || Chart_H < 20 )
        return;

      double Y_Min = Pair.Min - Math.Abs( Pair.Min ) * 0.2;
      double Y_Max = Pair.Max + Math.Abs( Pair.Max ) * 0.2;
      if ( Y_Max - Y_Min < 1e-12 )
      {
        Y_Min -= 1e-7;
        Y_Max += 1e-7;
      }
      double Y_Range = Y_Max - Y_Min;

      Draw_Grid( G, W, H, Chart_W, Chart_H, Y_Min, Y_Range, "µV", V => ( V * 1e6 ).ToString( "F2" ) );

      var       Pts      = Build_Points( Pair.Deltas, Count, W, H, Chart_W, Chart_H, Y_Min, Y_Range );
      using var Line_Pen = new Pen( Color.FromArgb( 180, _Theme.Line_Colors[ 0 ] ), 1f );
      G.DrawLines( Line_Pen, Pts );

      float     Zero_Y   = H - MB - (float) ( ( 0 - Y_Min ) / Y_Range * Chart_H );
      using var Zero_Pen = new Pen( Color.FromArgb( 180, Color.Red ), 1.5f ) { DashStyle = DashStyle.Dash };
      G.DrawLine( Zero_Pen, ML, Zero_Y, W - MR, Zero_Y );

      float     Mean_Y   = H - MB - (float) ( ( Pair.Mean - Y_Min ) / Y_Range * Chart_H );
      using var Mean_Pen = new Pen( _Theme.Accent, 1.5f ) { DashStyle = DashStyle.Dash };
      G.DrawLine( Mean_Pen, ML, Mean_Y, W - MR, Mean_Y );

      Draw_Title( G,
                  W,
                  $"Raw Delta  ({Pair.Name_A} − {Pair.Name_B})  |  {Count:N0} points  |  " +
                    $"mean: {Pair.Mean_uV:F3} µV  σ: {Pair.StdDev_uV:F3} µV" );
      Draw_Time_Axis( G, W, H, Chart_W, Count );
    }

    // ─────────────────────────────────────────────────────────────────
    // Tab — Poll Intervals
    // ─────────────────────────────────────────────────────────────────

    private void Draw_Timing_Panel( Graphics G )
    {
      int W = _Timing_Panel.ClientSize.Width;
      int H = _Timing_Panel.ClientSize.Height;

      Setup_Graphics( G, W, H );

      int Count   = _Times.Count;
      int Chart_W = W - ML - MR;
      int Chart_H = H - MT - MB;
      if ( Count < 3 || Chart_W < 20 || Chart_H < 20 )
        return;

      int       N            = _Intervals.Count;
      double    Mean_Ms      = _Intervals.Average();
      double    Threshold_Ms = _Threshold_Ms;
      double    Y_Max        = Math.Max( _Intervals.Max() * 1.15, Mean_Ms * 2.0 );
      double    Y_Min        = 0;
      double    Y_Range      = Y_Max - Y_Min;

      using var Grid_Pen    = new Pen( _Theme.Grid, 1f );
      using var Label_Font  = new Font( "Consolas", 7.5f );
      using var Label_Brush = new SolidBrush( _Theme.Labels );

      for ( int I = 0; I <= 5; I++ )
      {
        double Frac  = (double) I / 5;
        int    Y_Pos = H - MB - (int) ( Frac * Chart_H );
        double Val   = Y_Min + Frac * Y_Range;
        G.DrawLine( Grid_Pen, ML, Y_Pos, W - MR, Y_Pos );
        string Lbl = $"{Val:F0} ms";
        var    Sz  = G.MeasureString( Lbl, Label_Font );
        G.DrawString( Lbl, Label_Font, Label_Brush, ML - Sz.Width - 4, Y_Pos - Sz.Height / 2 );
      }

      var Raw_Pts = new PointF[ N ];
      for ( int I = 0; I < N; I++ )
      {
        float X      = ML + (float) I / ( N - 1 ) * Chart_W;
        float Y      = H - MB - (float) ( ( _Intervals[ I ] - Y_Min ) / Y_Range * Chart_H );
        Raw_Pts[ I ] = new PointF( X, Math.Max( MT, Math.Min( H - MB, Y ) ) );
      }
      using var Raw_Pen = new Pen( Color.FromArgb( 160, _Theme.Line_Colors[ 0 ] ), 1f );
      G.DrawLines( Raw_Pen, Raw_Pts );

      var Roll_Pts = new PointF[ N ];
      for ( int I = 0; I < N; I++ )
      {
        float X       = ML + (float) I / ( N - 1 ) * Chart_W;
        float Y       = H - MB - (float) ( ( _Rolling_Mean[ I ] - Y_Min ) / Y_Range * Chart_H );
        Roll_Pts[ I ] = new PointF( X, Math.Max( MT, Math.Min( H - MB, Y ) ) );
      }
      using var Roll_Pen = new Pen( Color.FromArgb( 220, Color.MediumSeaGreen ), 2f );
      G.DrawLines( Roll_Pen, Roll_Pts );

      float     Mean_Y   = H - MB - (float) ( ( Mean_Ms - Y_Min ) / Y_Range * Chart_H );
      using var Mean_Pen = new Pen( _Theme.Accent, 1.5f ) { DashStyle = DashStyle.Dash };
      G.DrawLine( Mean_Pen, ML, Mean_Y, W - MR, Mean_Y );
      using var Mean_Lbl_Font = new Font( "Consolas", 7f );
      G.DrawString( $"mean {Mean_Ms:F1} ms", Mean_Lbl_Font, Label_Brush, W - MR + 3, Mean_Y - 8 );

      float     Thresh_Y = H - MB - (float) ( ( Threshold_Ms - Y_Min ) / Y_Range * Chart_H );
      using var Thresh_Pen =
        new Pen( Color.FromArgb( 160, Color.OrangeRed ), 1f ) { DashStyle = DashStyle.Dash };
      G.DrawLine( Thresh_Pen, ML, Thresh_Y, W - MR, Thresh_Y );
      G.DrawString( "+20%", Mean_Lbl_Font, new SolidBrush( Color.OrangeRed ), W - MR + 3, Thresh_Y - 8 );

      if ( _Slowdown_Index >= 0 )
      {
        float     Slow_X   = ML + (float) _Slowdown_Index / ( N - 1 ) * Chart_W;
        using var Slow_Pen = new Pen( Color.Orange, 2f );
        G.DrawLine( Slow_Pen, Slow_X, MT, Slow_X, H - MB );
        string    Slow_Time  = _Times[ _Slowdown_Index + 1 ].ToString( "HH:mm:ss" );
        using var Slow_Font  = new Font( "Consolas", 7.5f, FontStyle.Bold );
        using var Slow_Brush = new SolidBrush( Color.Orange );
        G.DrawString( $"slowdown\n{Slow_Time}", Slow_Font, Slow_Brush, Slow_X + 4, MT + 4 );
      }

      using var Leg_Font = new Font( "Consolas", 7.5f );
      int       Lx = ML + 8, Ly = MT + 6, Leg_Sp = 16;
      void      Draw_Leg( Color C, string Txt )
      {
        using var B = new SolidBrush( C );
        using var P = new Pen( C, 2f );
        G.DrawLine( P, Lx, Ly + 5, Lx + 18, Ly + 5 );
        G.DrawString( Txt, Leg_Font, B, Lx + 22, Ly );
        Ly += Leg_Sp;
      }
      Draw_Leg( _Theme.Line_Colors[ 0 ], "raw interval" );
      Draw_Leg( Color.MediumSeaGreen, "rolling mean (50-sample)" );

      double    Pct_Slow = _Intervals.Count( V => V > Threshold_Ms ) * 100.0 / N;
      double    Max_Ms   = _Intervals.Max();

      using var Stats_Font  = new Font( "Consolas", 8f );
      using var Stats_Brush = new SolidBrush( _Theme.Foreground );
      string    Stats       = $"mean: {Mean_Ms:F1} ms   max: {Max_Ms:F1} ms   " +
                              $"jitter σ: {_StdDev_Delta_Ms:F1} ms   " +
                              $"slow cycles (>{Threshold_Ms:F0} ms): {Pct_Slow:F1}%";
      if ( _Slowdown_Index >= 0 )
        Stats += $"   sustained slowdown: {_Times[_Slowdown_Index + 1]:HH:mm:ss}";

      var Stats_Sz = G.MeasureString( Stats, Stats_Font );
      G.DrawString( Stats, Stats_Font, Stats_Brush, ( W - Stats_Sz.Width ) / 2, H - MB + 20 );

      Draw_Title( G, W, "Poll Intervals — sample-to-sample gap (ms)" );
      Draw_Time_Axis( G, W, H, Chart_W, N );
    }

    // ─────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────

    private void Setup_Graphics( Graphics G, int W, int H )
    {
      G.SmoothingMode     = SmoothingMode.AntiAlias;
      G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
      using var Bg        = new SolidBrush( _Theme.Background );
      G.FillRectangle( Bg, 0, 0, W, H );
    }

    private void Draw_Grid( Graphics             G,
                            int                  W,
                            int                  H,
                            int                  Chart_W,
                            int                  Chart_H,
                            double               Y_Min,
                            double               Y_Range,
                            string               Unit,
                            Func<double, string> Formatter )
    {
      using var Grid_Pen    = new Pen( _Theme.Grid, 1f );
      using var Label_Font  = new Font( "Consolas", 7.5f );
      using var Label_Brush = new SolidBrush( _Theme.Labels );
      for ( int I = 0; I <= 5; I++ )
      {
        double Frac = (double) I / 5;
        int    Y    = H - MB - (int) ( Frac * Chart_H );
        double Val  = Y_Min + Frac * Y_Range;
        G.DrawLine( Grid_Pen, ML, Y, W - MR, Y );
        string Lbl = Formatter( Val );
        var    Sz  = G.MeasureString( Lbl, Label_Font );
        G.DrawString( Lbl, Label_Font, Label_Brush, ML - Sz.Width - 4, Y - Sz.Height / 2 );
      }
    }

    private PointF[ ] Build_Points(
      List<double> Values, int Count, int W, int H, int Chart_W, int Chart_H, double Y_Min, double Y_Range )
    {
      var Pts = new PointF[ Count ];
      for ( int I = 0; I < Count; I++ )
      {
        float X  = ML + (float) I / ( Count - 1 ) * Chart_W;
        float Y  = H - MB - (float) ( ( Values[ I ] - Y_Min ) / Y_Range * Chart_H );
        Pts[ I ] = new PointF( X, Y );
      }
      return Pts;
    }

    private PointF[ ] Build_Points_Sampled( List<double> Values,
                                            int          Count,
                                            int          W,
                                            int          H,
                                            int          Chart_W,
                                            int          Chart_H,
                                            double       Y_Min,
                                            double       Y_Range,
                                            int          Max_Pts = 800 )
    {
      int Step = Math.Max( 1, Count / Max_Pts );
      var Pts  = new List<PointF>();
      for ( int I = 0; I < Count; I += Step )
      {
        float X = ML + (float) I / ( Count - 1 ) * Chart_W;
        float Y = H - MB - (float) ( ( Values[ I ] - Y_Min ) / Y_Range * Chart_H );
        Pts.Add( new PointF( X, Y ) );
      }
      return Pts.ToArray();
    }

    private void Draw_Title( Graphics G, int W, string Title )
    {
      using var Font  = new Font( "Segoe UI", 8.5f, FontStyle.Bold );
      using var Brush = new SolidBrush( _Theme.Foreground );
      var       Sz    = G.MeasureString( Title, Font );
      G.DrawString( Title, Font, Brush, ( W - Sz.Width ) / 2, 6 );
    }

    private void Draw_Time_Axis( Graphics G, int W, int H, int Chart_W, int Count )
    {
      if ( _Times.Count < 2 )
        return;
      using var Font  = new Font( "Consolas", 7.5f );
      using var Brush = new SolidBrush( _Theme.Labels );
      using var Pen   = new Pen( _Theme.Grid, 1f );
      int       Num_X = Math.Min( 8, Chart_W / 80 );
      for ( int I = 0; I <= Num_X; I++ )
      {
        double Frac  = (double) I / Num_X;
        int    X_Pos = ML + (int) ( Frac * Chart_W );
        int    T_Idx = Math.Clamp( (int) ( Frac * ( Count - 1 ) ), 0, _Times.Count - 1 );
        string Lbl   = _Times[ T_Idx ].ToString( "HH:mm:ss" );
        var    Sz    = G.MeasureString( Lbl, Font );
        G.DrawString( Lbl, Font, Brush, X_Pos - Sz.Width / 2, H - MB + 6 );
        G.DrawLine( Pen, X_Pos, MT, X_Pos, H - MB );
      }
    }

    private void Draw_Wrapped_Text(
      Graphics G, Font F, Brush B, string Text, int X, ref int Y, int Max_Width, int Line_Height )
    {
      if ( string.IsNullOrEmpty( Text ) )
        return;
      var Words = Text.Split( ' ' );
      var Line  = new System.Text.StringBuilder();
      foreach ( string Word in Words )
      {
        string Test = Line.Length == 0 ? Word : Line + " " + Word;
        if ( G.MeasureString( Test, F ).Width > Max_Width && Line.Length > 0 )
        {
          G.DrawString( Line.ToString(), F, B, X, Y );
          Y += Line_Height - 10;
          Line.Clear();
        }
        if ( Line.Length > 0 )
          Line.Append( ' ' );
        Line.Append( Word );
      }
      if ( Line.Length > 0 )
      {
        G.DrawString( Line.ToString(), F, B, X, Y );
        Y += Line_Height - 10;
      }
    }

    private void Draw_Stat_Row( Graphics            G,
                                System.Drawing.Font Val_Font,
                                System.Drawing.Font Lbl_Font,
                                Brush               Fg,
                                Brush               Dim,
                                string              Label,
                                double              V,
                                double              UV,
                                int                 C1,
                                int                 C2,
                                int                 C3,
                                int                 Y )
    {
      G.DrawString( Label, Lbl_Font, Dim, C1 + 16, Y );
      G.DrawString( V.ToString( "G10", CultureInfo.InvariantCulture ), Val_Font, Fg, C2, Y );
      G.DrawString( UV.ToString( "F4", CultureInfo.InvariantCulture ), Val_Font, Fg, C3, Y );
    }

    private void Draw_Stat_Row_Single( Graphics            G,
                                       System.Drawing.Font Val_Font,
                                       System.Drawing.Font Lbl_Font,
                                       Brush               Fg,
                                       Brush               Dim,
                                       string              Label,
                                       string              Value,
                                       int                 C1,
                                       int                 C2,
                                       int                 Y )
    {
      G.DrawString( Label, Lbl_Font, Dim, C1 + 16, Y );
      G.DrawString( Value, Val_Font, Fg, C2, Y );
    }

    private static string Color_Name( Color C )
    {
      foreach ( KnownColor KC in (KnownColor[ ]) Enum.GetValues( typeof( KnownColor ) ) )
      {
        Color K = Color.FromKnownColor( KC );
        if ( K.R == C.R && K.G == C.G && K.B == C.B )
          return K.Name;
      }
      return $"#{C.R:X2}{C.G:X2}{C.B:X2}";
    }

    // ─────────────────────────────────────────────────────────────────
    // Help dialog
    // ─────────────────────────────────────────────────────────────────

    private void Show_Tab_Help()
    {
      var Tab = _Tabs.SelectedTab;
      if (Tab == null)
        return;

      string Tab_Title = Tab.Text;
      string Base = System.Text.RegularExpressions.Regex
                           .Replace( Tab_Title, @"\s*\(.*\)$", "" ).Trim();

      string C0 = Color_Name( _Theme.Line_Colors[ 0 ] );
      string C1 = Color_Name( _Theme.Line_Colors[ 1 ] );
      string Acc = Color_Name( _Theme.Accent );

      using var Popup = new Rich_Text_Popup_Namespace.Rich_Text_Popup(
          $"About: {Tab_Title}", 560, 520, Resizable: true );

      switch (Base)
      {
        case "Δ Delta":
          Popup.Add_Title( "Δ Delta — Over Time" )
               .Add_Blank()
               .Add_Body( "This chart shows the difference (A − B) between the two meters at each " +
                          "sample point, plotted over time." )
               .Add_Blank()
               .Add_Heading( "Chart Lines" )
               .Add_Body( $"  • The {C0} line is the downsampled delta value in µV." )
               .Add_Body( $"  • The dashed {Acc} line is the mean (average) delta." )
               .Add_Body( "  • The dashed Red line marks zero — perfect agreement between meters." )
               .Add_Blank()
               .Add_Heading( "What to look for" )
               .Add_Body( "  – A flat line near zero means the meters agree well." )
               .Add_Body( "  – A consistent offset means one meter reads higher than the other." )
               .Add_Body( "  – Spikes or drift suggest noise or instability in one or both meters." );
          break;

        case "Rolling σ":
          Popup.Add_Title( "Rolling σ — Noise Over Time" )
               .Add_Blank()
               .Add_Body( "This chart shows the rolling standard deviation (σ) of the delta, " +
                          "computed over a sliding 100-sample window." )
               .Add_Blank()
               .Add_Heading( "Chart Lines" )
               .Add_Body( $"  • The {C1} line shows variability (noise) between the two meters at each moment." )
               .Add_Body( "  • Higher values mean more variability. Lower values mean consistent agreement." )
               .Add_Blank()
               .Add_Heading( "What to look for" )
               .Add_Body( "  – A flat, low line means stable, low-noise measurements." )
               .Add_Body( "  – Bumps or spikes indicate periods of increased disagreement or noise." )
               .Add_Body( "  – A rising trend at the end may indicate a meter drifting." );
          break;

        case "Summary Stats":
          Popup.Add_Title( "Summary Statistics" )
               .Add_Blank()
               .Add_Body( "This panel summarises the entire comparison run in numbers." )
               .Add_Blank()
               .Add_Heading( "Δ (A − B) section" )
               .Add_Body( "  • Mean — average difference between meters (ideally near zero)" )
               .Add_Body( "  • σ (sigma) — standard deviation; lower = more consistent agreement" )
               .Add_Body( "  • Min / Max / Range — extremes of the delta" )
               .Add_Blank()
               .Add_Heading( "Sample Timing section" )
               .Add_Body( "  • Mean interval — average time between samples (and equivalent sample rate)" )
               .Add_Body( "  • Jitter (σ) — variability in sample timing; lower = more even pacing" )
               .Add_Body( "  • Max interval — longest gap between any two samples" )
               .Add_Body( "  • Point count — total number of paired samples analysed" )
               .Add_Blank()
               .Add_Heading( "Polling Health section" )
               .Add_Body( "  • Baseline interval — the median of the first 20 intervals" )
               .Add_Body( "  • Slowdown threshold — 20% above the baseline; intervals exceeding this are flagged" )
               .Add_Body( "  • First sustained slow — timestamp where 5+ consecutive intervals exceeded the threshold" )
               .Add_Body( "  • Slow cycles — percentage of all intervals above the threshold" )
               .Add_Body( "  • Peak interval — the single longest gap recorded, as a multiple of the baseline" )
               .Add_Body( "  • Trend — whether polling speed was stable, drifting, or improving" )
               .Add_Blank()
               .Add_Heading( "Chart Decimation section" )
               .Add_Body( "  • Shows whether decimation was active during the session" )
               .Add_Body( "  • Stored points — total samples held in memory" )
               .Add_Body( "  • Drawn points — how many were rendered on the chart" )
               .Add_Body( "  • Visual fidelity — percentage of stored data visible on the chart" )
               .Add_Warning( "  ⚠ If fidelity is low, export CSV to see all stored data." );
          break;

        case "Δ Distribution":
          Popup.Add_Title( "Δ Distribution — Histogram" )
               .Add_Blank()
               .Add_Body( "This chart shows how often each delta value occurred across the entire run." )
               .Add_Blank()
               .Add_Heading( "Chart Elements" )
               .Add_Body( $"  • The {C0} bars count how many samples fell in each µV range." )
               .Add_Body( $"  • The {C1} curve is the ideal normal (bell) distribution fitted to the data." )
               .Add_Body( $"  • The dashed {Acc} line marks the mean." )
               .Add_Body( $"  • The dotted lines mark ±1σ and ±2σ from the mean." )
               .Add_Blank()
               .Add_Heading( "What to look for" )
               .Add_Body( "  – A narrow, symmetric bell curve centred near zero is ideal." )
               .Add_Body( "  – A wide or skewed distribution suggests noise or systematic offset." )
               .Add_Body( "  – Multiple humps may indicate the meters switching measurement ranges." );
          break;

        case "Raw":
          Popup.Add_Title( "Raw Delta — Every Sample" )
               .Add_Blank()
               .Add_Body( "This chart plots every single recorded delta value with no downsampling." )
               .Add_Blank()
               .Add_Heading( "Chart Lines" )
               .Add_Body( $"  • The {C0} line shows every raw sample — with large datasets this appears as a dense band." )
               .Add_Body( $"  • The dashed {Acc} line is the mean." )
               .Add_Body( "  • The dashed Red line marks zero — perfect agreement between meters." )
               .Add_Blank()
               .Add_Heading( "What to look for" )
               .Add_Body( "  – Sudden changes in band width indicate noise bursts." )
               .Add_Body( "  – A consistent offset from zero means one meter reads higher." )
               .Add_Body( "  – Gaps in the band may indicate dropped samples." );
          break;

        case "Poll Intervals":
          Popup.Add_Title( "Poll Intervals — Sample Timing" )
               .Add_Blank()
               .Add_Body( "This chart shows the gap in milliseconds between consecutive timestamps, " +
                          "plotted over time." )
               .Add_Blank()
               .Add_Heading( "Chart Lines" )
               .Add_Body( $"  • The {C0} line is the raw interval between each pair of readings." )
               .Add_Body( "  • The solid green line is the 50-sample rolling mean — smooths out spikes " +
                           "to show the underlying trend." )
               .Add_Body( "  • The dashed orange/red line marks the +20% threshold — intervals above " +
                           "this are anomalous." )
               .Add_Body( "  • The dashed gold line marks the session mean interval." )
               .Add_Blank()
               .Add_Heading( "What to look for" )
               .Add_Body( "  – A flat rolling mean means consistent, even polling." )
               .Add_Body( "  – Regular alternating spikes suggest the instrument alternates between two read times." )
               .Add_Body( "  – A gradual upward drift in the rolling mean means the polling loop is slowing down." )
               .Add_Body( "  – The slowdown start time is marked with a vertical orange line." );
          break;

        default:
          Popup.Add_Title( Tab_Title )
               .Add_Blank()
               .Add_Body( $"No help available for '{Tab_Title}'." );
          break;
      }

      Popup.Show_Popup( this );
    }

 
    // ─────────────────────────────────────────────────────────────────
    // Async load + timing precompute
    // ─────────────────────────────────────────────────────────────────

    public async void Begin_Async_Load()
    {
      await Task.Run( () => Precompute_Timing_Stats() );
      if ( IsDisposed || ! IsHandleCreated )
        return;
      this.Invoke( () =>
                   {
                     _Timing_Panel.Invalidate();
                     _Stats_Panel.Invalidate();
                     foreach ( var P in _Delta_Panels )
                       P.Invalidate();
                     foreach ( var P in _Rolling_Panels )
                       P.Invalidate();
                     foreach ( var P in _Histo_Panels )
                       P.Invalidate();
                     foreach ( var P in _Raw_Panels )
                       P.Invalidate();
                   } );
    }

    private void Precompute_Timing_Stats()
    {
      _Intervals = new List<double>( _Times.Count - 1 );
      for ( int I = 1; I < _Times.Count; I++ )
        _Intervals.Add( ( _Times[ I ] - _Times[ I - 1 ] ).TotalMilliseconds );

      int N = _Intervals.Count;
      if ( N < 3 )
        return;

      var Early     = _Intervals.Take( 20 ).OrderBy( V => V ).ToList();
      _Baseline_Ms  = Early[ Early.Count / 2 ];
      _Threshold_Ms = _Baseline_Ms * 1.2;
      _Slow_Count   = _Intervals.Count( V => V > _Threshold_Ms );
      _Pct_Slow     = _Slow_Count * 100.0 / N;

      _Slowdown_Index   = -1;
      const int Sustain = 5;
      for ( int I = 0; I <= N - Sustain; I++ )
      {
        bool All_Slow = true;
        for ( int J = 0; J < Sustain; J++ )
          if ( _Intervals[ I + J ] <= _Threshold_Ms )
          {
            All_Slow = false;
            break;
          }
        if ( All_Slow )
        {
          _Slowdown_Index = I;
          break;
        }
      }

      double X_Mean = ( N - 1 ) / 2.0;
      double Y_Mean = _Intervals.Average();
      double Num = 0, Den = 0;
      for ( int I = 0; I < N; I++ )
      {
        Num += ( I - X_Mean ) * ( _Intervals[ I ] - Y_Mean );
        Den += ( I - X_Mean ) * ( I - X_Mean );
      }
      _Slope_Total = Den > 0 ? ( Num / Den ) * N : 0;
      _Trend       = Math.Abs( _Slope_Total ) < 0.5 ? "Flat — stable polling"
                     : _Slope_Total > 0             ? $"Gradual drift  (+{_Slope_Total:F1} ms over session)"
                                                    : $"Improving  ({_Slope_Total:F1} ms over session)";

      const int Roll_Win = 50;
      _Rolling_Mean      = new List<double>( N );
      double R_Sum       = 0;
      var    R_Q         = new Queue<double>();
      foreach ( double V in _Intervals )
      {
        R_Q.Enqueue( V );
        R_Sum += V;
        if ( R_Q.Count > Roll_Win )
          R_Sum -= R_Q.Dequeue();
        _Rolling_Mean.Add( R_Sum / R_Q.Count );
      }
    }
  }
}
