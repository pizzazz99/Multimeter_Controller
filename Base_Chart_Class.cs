// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Base_Chart_Form.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Abstract base Form from which all chart/data-viewer windows in the
//   Multimeter Controller application inherit.  It centralises every piece of
//   chart logic that is shared between the live-polling form and the file-
//   playback form, preventing duplication and providing a single location for
//   maintenance.
//
// RESPONSIBILITIES
//   • GDI+ resource lifecycle   – Font, Pen, and Brush objects are allocated
//                                 once in Initialize_Chart_Resources() and
//                                 disposed in Dispose_Chart_Resources().
//   • Chart refresh timer       – A Windows.Forms.Timer drives all repaints.
//                                 Refresh rate is auto-throttled when the total
//                                 point count exceeds the configurable threshold
//                                 (Application_Settings.Throttle_Point_Threshold).
//   • Graph-style management    – Supports Line / Scatter / Step / Bar /
//                                 Histogram / Pie.  Multi-series restrictions
//                                 (Bar, Histogram, Pie are single-series only)
//                                 are enforced by Update_Graph_Style_Availability().
//   • View modes                – Combined (all series on one Y axis, absolute
//                                 or normalised 0–100%) and Split (one subplot
//                                 per visible series).
//   • Zoom                      – TrackBar-driven zoom factor (0.1 × … 10 ×)
//                                 applied symmetrically around the Y-axis centre.
//   • Rolling window & panning  – Optional rolling window of N most-recent
//                                 points.  HScrollBar + keyboard (←/→/Home/End/
//                                 PgUp/PgDn) pan through historical data.
//                                 Auto-scroll snaps to the latest sample when
//                                 View_Offset == 0.
//   • Tooltip hit-testing       – MouseMove finds the nearest plotted point
//                                 (Euclidean distance) across all visible series
//                                 and shows a ToolTip with time + formatted value.
//   • Legend / visibility       – FlowLayoutPanel with per-series CheckBoxes;
//                                 at least one series must remain visible.
//                                 "Stats" popup (Show_Stats_Popup) exposes per-
//                                 instrument statistics in a RichTextBox dialog.
//   • Histogram drawing         – Sturges-rule bin count (clamped 5–30), overlaid
//                                 normal-distribution curve, mean (μ) and ±1σ/±2σ
//                                 markers, both as a subplot (Draw_Subplot_Histogram)
//                                 and as a full-panel view (Draw_Histogram).
//   • Pie chart                 – Same Sturges-rule binning; right-side legend
//                                 with percentage labels.
//   • Poll-timing chart         – Reads from a ring buffer (_Cycle_Timing[10 000])
//                                 of Poll_Cycle_Sample structs, draws a filled
//                                 area chart of total cycle duration, and overlays
//                                 stacked phase bands (Addr / Comm / UI / Record).
//                                 Disconnect events are marked with vertical
//                                 dashed lines.
//   • Timing file loader        – Load_Timing_File() parses a _Timing.csv produced
//                                 by the polling thread; supports both normal sample
//                                 rows (≥ 8 columns) and DISCONNECT event rows.
//   • Analysis popup            – Show_Analysis_Results() / Build_Analysis_Rtb()
//                                 display Min/Max/Mean/σ/RMS/Trend/Rate for each
//                                 series in a non-modal dialog.
//   • Formatting helpers        – Format_Digits(), Format_Sig_Figs(),
//                                 Format_Axis_Value(), Format_Time_Label(),
//                                 Format_Value() (delegates to
//                                 Multimeter_Common_Helpers_Class).
//
// ABSTRACT / VIRTUAL MEMBERS — subclasses must override
//   protected virtual string Current_Unit               Unit string for Y-axis labels.
//   protected virtual bool   _Is_Running_State()        True while polling is active.
//   protected virtual void   Show_Progress(msg, color)  Update a status strip label.
//   protected virtual void   On_Chart_Refresh_Tick()    Called every timer tick
//                                                        while running (e.g. to
//                                                        refresh the legend).
//   protected virtual void   Update_Performance_Status() Refresh FPS / point-count
//                                                        display.
//
// CONTROL REFERENCES — must be assigned by the subclass after InitializeComponent()
//   Chart_Panel_Control         Panel whose Paint event renders the chart.
//   Pan_Scrollbar_Control       HScrollBar for panning through historical data.
//   Auto_Scroll_Check_Control   CheckBox — when checked, offset is locked to 0.
//   Rolling_Check_Control       CheckBox — enables the rolling-window mode.
//   Max_Points_Numeric_Control  NumericUpDown — window size (points).
//   Zoom_Slider_Control         TrackBar (0–100) mapped to 0.1–10 × zoom factor.
//   Graph_Style_Combo_Control   ComboBox — selects the active graph style.
//   View_Mode_Button_Control    Button — toggles Split ↔ Combined view.
//   Normalize_Button_Control    Button — toggles Absolute ↔ Normalised (combined
//                                        view only).
//   Legend_Toggle_Button_Control Button — opens the statistics popup.
//
// DATA FLOW
//   _Series (List<Instrument_Series>)
//     └─ Instrument_Series.Points  (List<(DateTime Time, double Value)>)
//          Populated by the subclass (polling thread via Invoke, or file loader).
//          All read access from Base_Chart_Form is UI-thread-safe because writes
//          are marshalled through Control.Invoke before the chart timer fires.
//
// TIMING RING BUFFER
//   _Cycle_Timing[10 000]  — fixed-size circular buffer of Poll_Cycle_Sample.
//   _Timing_Head           — next write index (modulo _Timing_Buffer_Size).
//   _Timing_Count          — number of valid samples (capped at buffer size).
//   _Disconnect_Events     — thread-safe list of Disconnect_Event; lock() guards
//                            every access from both the paint and the loader.
//
// CHART LAYOUT CONSTANTS
//   _Chart_Margin_Left   = 110 px   (Y-axis labels + padding)
//   _Chart_Margin_Right  = 140 px   (last-value labels + position indicator)
//   _Chart_Margin_Top    =  30 px
//   _Chart_Margin_Bottom =  30 px   (X-axis labels)
//   _Chart_Subplot_Gap   =   8 px   (vertical gap between subplots in Split view)
//
// DEPENDENCIES
//   Multimeter_Common_Helpers_Class  — Get_Visible_Range(), Format_Value()
//   Instrument_Series                — per-instrument data, statistics, styling
//   Application_Settings             — runtime-configurable behaviour flags
//   Chart_Theme                      — colour palette (Background, Foreground,
//                                      Grid, Separator, Labels, Line_Colors[])
//   Trace_Execution_Namespace        — optional block-level execution tracing
//                                      (Trace_Block.Start_If_Enabled())
//
// THREAD SAFETY
//   All chart state is read and written on the UI thread only.  Subclasses are
//   responsible for marshalling data from background threads via Invoke/BeginInvoke
//   before modifying _Series or _Cycle_Timing.
//
// KNOWN LIMITATIONS / NOTES
//   • Double-buffering is enabled on Chart_Panel_Control via reflection
//     (Enable_Double_Buffer) because the WinForms designer does not expose the
//     DoubleBuffered property for arbitrary Panel controls.
//   • The scrollbar arithmetic uses a "usable range" pattern
//     (Maximum − LargeChange) to avoid the WinForms scrollbar boundary quirk
//     where Value can never reach Maximum.
//   • Draw_Combined_Absolute() and Draw_Combined_Scatter() both recompute the
//     global Y range; this is intentional — scatter may later diverge in its
//     axis-padding strategy.
//   • Bar, Histogram, and Pie styles are deliberately removed from the combo
//     when _Series.Count > 1 because those renderers only use _Series[0].
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════






using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using static Trace_Execution_Namespace.Trace_Execution;


namespace Multimeter_Controller
{


  public struct Poll_Cycle_Sample
  {
    public DateTime Cycle_Time;
    public double Total_Ms; // full cycle wall time
    public double Comm_Ms; // time spent in actual GPIB reads
    public double Address_Switch_Ms; // time spent switching addresses
    public double UI_Ms; // time spent in Invoke/UI updates
    public double Record_Ms; // time spent writing to stream
    public int Instrument_Count;
    public bool Had_Error;
  }

  public struct Disconnect_Event
  {
    public DateTime Time;
    public string Instrument_Name;
    public int Cycle_Number;
  }

  public class Base_Chart_Form : Form
  {
    // ── Shared state ─────────────────────────────────────────────────────
    protected List<Instrument_Series> _Series = new();
    protected Application_Settings _Settings = new();
    protected Chart_Theme _Theme;
    protected string _Current_Graph_Style = "Line";
    protected bool _Combined_View = false;
    protected bool _Normalized_View = false;
    protected bool _Enable_Rolling = true;
    protected int _Max_Display_Points = 10;
    protected int _View_Offset = 0;
    protected bool _Auto_Scroll = true;
    protected double _Zoom_Factor = 1.0;
    protected bool _Show_Timing_View = false;
    protected int _Timing_View_Offset = 0;
    protected Meter_Type _Selected_Meter;
    protected Color _Foreground_Color = Color.Black;






    // Timing ring buffer
    protected const int _Timing_Buffer_Size = 100_000;
    protected readonly Poll_Cycle_Sample[] _Cycle_Timing = new Poll_Cycle_Sample[ 10_000 ];
    protected int _Timing_Head = 0;
    protected int _Timing_Count = 0;
    protected readonly List<Disconnect_Event> _Disconnect_Events = new();

    // Chart layout constants
    protected const int _Chart_Margin_Left = 110;
    protected const int _Chart_Margin_Right = 140;
    protected const int _Chart_Margin_Top = 30;
    protected const int _Chart_Margin_Bottom = 30;
    protected const int _Chart_Subplot_Gap = 8;

    // Pre-allocated GDI resources
    protected Font? _Chart_Label_Font;
    protected Font? _Chart_Name_Font;
    protected Font? _Chart_X_Label_Font;
    protected Pen? _Chart_Grid_Pen;
    protected Pen? _Chart_Sep_Pen;
    protected Brush? _Chart_Label_Brush;

    // Performance tracking
    protected int _Paint_Count = 0;
    protected double _Actual_FPS = 0;
    protected readonly Stopwatch _FPS_Stopwatch = Stopwatch.StartNew();

    // Legend
    protected Panel? _Legend_Panel_2;
    protected Dictionary<string, Label> _Stats_Labels = new();
    protected FlowLayoutPanel? _Legend_Panel;

    // Scrollbar guard
    protected bool _Updating_Scroll = false;

    // Combo guard
    protected bool _Updating_Combo = false;

    // Tooltip
    protected ToolTip _Chart_Tooltip;
    protected Point _Last_Mouse_Position = Point.Empty;
    protected DateTime _Last_Tooltip_Update = DateTime.MinValue;

    // Readings snapshot used by histogram/pie
    protected List<double> _Readings = new();

    // ── Abstract members each subclass must supply ────────────────────────

    protected virtual string Current_Unit => "";

    // ── Control references — assigned by each subclass after InitializeComponent ──
    protected Panel Chart_Panel_Control;
    protected HScrollBar Pan_Scrollbar_Control;
    protected CheckBox Auto_Scroll_Check_Control;
    protected CheckBox Rolling_Check_Control;
    protected NumericUpDown Max_Points_Numeric_Control;
    protected TrackBar Zoom_Slider_Control;
    protected ComboBox Graph_Style_Combo_Control;
    protected Button View_Mode_Button_Control;
    protected Button Normalize_Button_Control;
    protected Button Legend_Toggle_Button_Control;
    protected virtual void Show_Progress( string Message, Color Color )
    {
    }

    protected virtual void On_Chart_Refresh_Tick()
    {
      // empty virtual methods exist as default no-ops — let the base class
      // call them unconditionally without needing to null-check or know whether
      // a subclass cares.
    }

    protected System.Windows.Forms.Timer? _Chart_Refresh_Timer;

    // Subclasses supply whether polling is active
    protected virtual bool _Is_Running_State() => false;

    // ════════════════════════════════════════════════════════════════════
    // GDI RESOURCE MANAGEMENT
    // ════════════════════════════════════════════════════════════════════

    protected void Initialize_Chart_Resources()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Chart_Label_Font = new Font( "Consolas", 7.5F );
      _Chart_Name_Font = new Font( "Segoe UI", 8F, FontStyle.Bold );
      _Chart_X_Label_Font = new Font( "Segoe UI", 7.5F );
      _Chart_Grid_Pen = new Pen( _Theme.Grid, 1f );
      _Chart_Sep_Pen = new Pen( _Theme.Separator, 1f )
      {
        DashStyle = DashStyle.Dash
      };
      _Chart_Label_Brush = new SolidBrush( _Theme.Labels );
    }


    protected void Initialize_Chart_Refresh_Timer()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Chart_Refresh_Timer?.Stop();
      _Chart_Refresh_Timer?.Dispose();

      _Chart_Refresh_Timer = new System.Windows.Forms.Timer();
      _Chart_Refresh_Timer.Interval = Math.Max( 50, _Settings.Chart_Refresh_Rate_Ms );

      _Chart_Refresh_Timer.Tick += ( s, e ) =>
      {
        if (Chart_Panel_Control == null)
          return;
        if (_Is_Running_State())
        {
          Chart_Panel_Control.Invalidate();
          On_Chart_Refresh_Tick();
        }
        else
        {
          _Chart_Refresh_Timer?.Stop();  // self-healing safety net
        }
      };
    }



    protected void Update_Chart_Refresh_Rate()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if (_Chart_Refresh_Timer == null)
        return;

      int Total_Points = _Series.Sum( s => s.Points.Count );
      int Base_Rate = _Settings.Chart_Refresh_Rate_Ms;

      if (_Settings.Throttle_When_Many_Points && Total_Points > _Settings.Throttle_Point_Threshold)
      {
        int Multiplier =
            Total_Points > _Settings.Throttle_Point_Threshold * 10 ? 4 :
            Total_Points > _Settings.Throttle_Point_Threshold * 5 ? 3 :
            Total_Points > _Settings.Throttle_Point_Threshold * 2 ? 2 : 1;
        _Chart_Refresh_Timer.Interval = Base_Rate * Multiplier;
      }
      else
      {
        _Chart_Refresh_Timer.Interval = Base_Rate;
      }

      Update_Performance_Status();
    }
    protected virtual void Update_Performance_Status()
    {
      // empty virtual methods exist as default no-ops — let the base class
      // call them unconditionally without needing to null-check or know whether
      // a subclass cares.
    }
    protected void Dispose_Chart_Resources()
    {

      using var Block = Trace_Block.Start_If_Enabled();

      _Chart_Label_Font?.Dispose();
      _Chart_Label_Font = null;
      _Chart_Name_Font?.Dispose();
      _Chart_Name_Font = null;
      _Chart_X_Label_Font?.Dispose();
      _Chart_X_Label_Font = null;
      _Chart_Grid_Pen?.Dispose();
      _Chart_Grid_Pen = null;
      _Chart_Sep_Pen?.Dispose();
      _Chart_Sep_Pen = null;
      _Chart_Label_Brush?.Dispose();
      _Chart_Label_Brush = null;
    }


    // ════════════════════════════════════════════════════════════════════
    // STATIC HELPERS
    // ════════════════════════════════════════════════════════════════════

    protected static void Enable_Double_Buffer( Control C )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      typeof( Panel ).InvokeMember(
          "DoubleBuffered",
          System.Reflection.BindingFlags.SetProperty
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic,
          null, C, new object[] { true } );
    }

    protected static double Parse_Double( string S ) =>
        double.TryParse( S,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double V ) ? V : 0;


    // ════════════════════════════════════════════════════════════════════
    // FORMATTING
    // ════════════════════════════════════════════════════════════════════

    public static string Format_Digits( double Value, int Digits )
    {

      using var Block = Trace_Block.Start_If_Enabled();

      if (Value == 0 || Math.Abs( Value ) < 1e-15)
        return "0." + new string( '0', Digits - 1 );

      double Abs = Math.Abs( Value );

      if (Abs < 0.001 || Abs >= 1e12)
        return Value.ToString( "G8" );

      int Integer_Digits = (int) Math.Floor( Math.Log10( Abs ) ) + 1;
      int Decimal_Places = Math.Max( 0, Digits - Integer_Digits );
      return Value.ToString( $"F{Decimal_Places}" );
    }

    protected static string Format_Sig_Figs( double Value, int Sig_Figs )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if (Value == 0)
        return "0";
      double Abs = Math.Abs( Value );
      int Integer_Digits = (int) Math.Floor( Math.Log10( Abs ) ) + 1;
      int Decimal_Places = Math.Max( 0, Sig_Figs - Integer_Digits );
      return Value.ToString( $"F{Decimal_Places}" );
    }

    protected string Format_Axis_Value( double Value )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      double Abs_Value = Math.Abs( Value );

      if (Abs_Value >= 1000)
        return Value.ToString( "F2" );
      if (Abs_Value >= 100)
        return Value.ToString( "F3" );
      if (Abs_Value >= 10)
        return Value.ToString( "F4" );
      if (Abs_Value >= 1)
        return Value.ToString( "F9" );
      if (Abs_Value >= 0.1)
        return Value.ToString( "F8" );
      if (Abs_Value >= 0.01)
        return Value.ToString( "F9" );
      return Value.ToString( "G6" );
    }

    protected string Format_Time_Label( double Seconds, double Time_Range_Sec )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if (Time_Range_Sec < 60)
        return $"{Seconds:F1}s";
      if (Time_Range_Sec < 3600)
        return $"{Seconds / 60:F1}m";
      return $"{Seconds / 3600:F1}h";
    }

    protected string Format_Value( double Value ) =>
        Multimeter_Common_Helpers_Class.Format_Value(
            Value, Current_Unit, _Selected_Meter );


    // ════════════════════════════════════════════════════════════════════
    // DATA HELPERS
    // ════════════════════════════════════════════════════════════════════





    protected List<double> Get_Single_Series_Readings()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if (_Series.Count == 0)
        return new List<double>();
      return _Series[ 0 ].Points.Select( p => p.Value ).ToList();
    }

    protected (double Min_V, double Max_V) Get_Y_Range()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      double Min_V = double.MaxValue;
      double Max_V = double.MinValue;

      foreach (var S in _Series)
        foreach (var P in S.Points)
        {
          if (P.Value < Min_V)
            Min_V = P.Value;
          if (P.Value > Max_V)
            Max_V = P.Value;
        }

      if (Min_V == double.MaxValue)
        return (0, 1);

      if (Math.Abs( Max_V - Min_V ) < 1e-12)
      {
        double Pad = Math.Abs( Max_V ) * 0.1;
        if (Pad < 1e-12)
          Pad = 1.0;
        Min_V -= Pad;
        Max_V += Pad;
      }
      return (Min_V, Max_V);
    }

    protected PointF[] Build_Points(
        List<(DateTime Time, double Value)> Points,
        int Start_Index, int Count,
        int Chart_W, int Chart_H,
        double Min_V, double Max_V )
    {

      using var Block = Trace_Block.Start_If_Enabled();
      var Result = new PointF[ Count ];
      double Range = Max_V - Min_V;

      for (int I = 0; I < Count; I++)
      {
        float X = _Chart_Margin_Left + (float) I / (Count - 1) * Chart_W;
        float Y = _Chart_Margin_Top +
            (float) ((Max_V - Points[ Start_Index + I ].Value) / Range * Chart_H);
        Result[ I ] = new PointF( X, Y );
      }
      return Result;
    }

    protected (DateTime Min, DateTime Max) Calculate_Time_Range()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      DateTime Time_Min = DateTime.MaxValue;
      DateTime Time_Max = DateTime.MinValue;

      if (_Enable_Rolling)
      {
        foreach (var S in _Series)
        {
          if (!S.Visible || S.Points.Count == 0)
            continue;

          var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
              S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
          if (Visible_Count == 0)
            continue;

          if (S.Points[ Start_Index ].Time < Time_Min)
            Time_Min = S.Points[ Start_Index ].Time;
          if (S.Points[ Start_Index + Visible_Count - 1 ].Time > Time_Max)
            Time_Max = S.Points[ Start_Index + Visible_Count - 1 ].Time;
        }
      }
      else
      {
        foreach (var S in _Series)
        {
          if (!S.Visible || S.Points.Count == 0)
            continue;
          if (S.Points[ 0 ].Time < Time_Min)
            Time_Min = S.Points[ 0 ].Time;
          if (S.Points[ S.Points.Count - 1 ].Time > Time_Max)
            Time_Max = S.Points[ S.Points.Count - 1 ].Time;
        }
      }
      return (Time_Min, Time_Max);
    }

    protected PointF[] Build_Point_Array(
        List<(DateTime Time, double Value)> Points,
        DateTime Time_Min, double Time_Range_Sec,
        double Padded_Min, double Padded_Range,
        int Chart_W, int Sub_Bottom, int Plot_H )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
          Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

      if (Visible_Count == 0)
        return Array.Empty<PointF>();

      PointF[] Result = new PointF[ Visible_Count ];

      DateTime Visible_Time_Min = Points[ Start_Index ].Time;
      DateTime Visible_Time_Max = Points[ Start_Index + Visible_Count - 1 ].Time;
      double Visible_Time_Range_Sec = (Visible_Time_Max - Visible_Time_Min).TotalSeconds;
      if (Visible_Time_Range_Sec < 0.001)
        Visible_Time_Range_Sec = 1.0;

      for (int I = 0; I < Visible_Count; I++)
      {
        int Data_Index = Start_Index + I;
        double Time_Sec = (Points[ Data_Index ].Time - Visible_Time_Min).TotalSeconds;
        double Time_Frac = Time_Sec / Visible_Time_Range_Sec;
        double V_Frac = (Points[ Data_Index ].Value - Padded_Min) / Padded_Range;
        float X = _Chart_Margin_Left + (float) (Time_Frac * Chart_W);
        float Y = Sub_Bottom - (float) (V_Frac * Plot_H);
        Result[ I ] = new PointF( X, Y );
      }
      return Result;
    }

    protected int Get_Bin_Count( int N )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      int Bins = (int) Math.Ceiling( 1.0 + 3.322 * Math.Log10( N ) );
      return Math.Clamp( Bins, 5, 30 );
    }

    protected Color Get_Bin_Color( int Index )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Color[] Base = _Theme.Line_Colors;
      int Cycle = Index / Base.Length;
      Color C = Base[ Index % Base.Length ];
      if (Cycle == 0)
        return C;
      int Shift = Cycle * 40;
      return Color.FromArgb(
          Math.Min( 255, C.R + Shift ),
          Math.Min( 255, C.G + Shift ),
          Math.Min( 255, C.B + Shift ) );
    }


    // ════════════════════════════════════════════════════════════════════
    // GRAPH STYLE COMBO
    // ════════════════════════════════════════════════════════════════════

    protected void Update_Graph_Style_Availability()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      _Updating_Combo = true;
      try
      {
        bool Multi = _Series.Count > 1;
        string Current_Selection = Graph_Style_Combo_Control.SelectedItem?.ToString() ?? "Line";
        Graph_Style_Combo_Control.Items.Clear();
        Graph_Style_Combo_Control.Items.Add( "Line" );
        Graph_Style_Combo_Control.Items.Add( "Scatter" );
        Graph_Style_Combo_Control.Items.Add( "Step" );
        if (!Multi)
        {
          Graph_Style_Combo_Control.Items.Add( "Bar" );
          Graph_Style_Combo_Control.Items.Add( "Histogram" );
          Graph_Style_Combo_Control.Items.Add( "Pie" );
        }
        if (Graph_Style_Combo_Control.Items.Contains( Current_Selection ))
          Graph_Style_Combo_Control.SelectedItem = Current_Selection;
        else
          Graph_Style_Combo_Control.SelectedIndex = 0;
      }
      finally
      {
        _Updating_Combo = false;
        _Current_Graph_Style = Graph_Style_Combo_Control.SelectedItem?.ToString() ?? "Line";
      }
    }

    protected void Graph_Style_Combo_SelectedIndexChanged( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if (_Updating_Combo)
        return;
      if (Graph_Style_Combo_Control.SelectedItem == null)
        return;
      _Current_Graph_Style = Graph_Style_Combo_Control.SelectedItem.ToString()!;
      Capture_Trace.Write( $"Graph style changed to: [{_Current_Graph_Style}]" );
      Chart_Panel_Control.Invalidate();
    }


    // ════════════════════════════════════════════════════════════════════
    // VIEW MODE AND NORMALIZE
    // ════════════════════════════════════════════════════════════════════

    protected void View_Mode_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      _Combined_View = !_Combined_View;
      View_Mode_Button_Control.Text = _Combined_View ? "Split View" : "Combined View";
      Normalize_Button_Control.Visible = _Combined_View;
      Zoom_Slider_Control.Enabled = !(_Combined_View && _Normalized_View);
      Chart_Panel_Control.Invalidate();
    }

    protected void Normalize_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      _Normalized_View = !_Normalized_View;
      Normalize_Button_Control.Text = _Normalized_View ? "Absolute" : "Normalize";
      Zoom_Slider_Control.Enabled = !_Normalized_View;
      Chart_Panel_Control.Invalidate();
    }


    // ════════════════════════════════════════════════════════════════════
    // ZOOM
    // ════════════════════════════════════════════════════════════════════

    protected void Zoom_Slider_Scroll( object sender, EventArgs e ) => Update_Zoom_From_Slider();
    protected void Zoom_Slider_ValueChanged( object sender, EventArgs e ) => Update_Zoom_From_Slider();

    protected void Update_Zoom_From_Slider()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Slider_Value = Zoom_Slider_Control.Value;

      if (Slider_Value < 50)
        _Zoom_Factor = 0.1 + (Slider_Value / 50.0) * 0.9;
      else
        _Zoom_Factor = 1.0 + ((Slider_Value - 50) / 50.0) * 9.0;

      Chart_Panel_Control.Invalidate();
    }


    // ════════════════════════════════════════════════════════════════════
    // ROLLING / MAX POINTS
    // ════════════════════════════════════════════════════════════════════

    protected void Rolling_Check_CheckedChanged( object? sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      _Enable_Rolling = Rolling_Check_Control.Checked;

      if (_Enable_Rolling && _Is_Running_State())
      {
        foreach (var S in _Series)
        {
          if (S.Points.Count > _Max_Display_Points)
            S.Points.RemoveRange( 0, S.Points.Count - _Max_Display_Points );
        }
      }
      Chart_Panel_Control.Invalidate();
    }



    protected void Max_Points_Numeric_ValueChanged( object? sender, EventArgs e )
    {
      _Max_Display_Points = (int) Max_Points_Numeric_Control.Value;

      if (_Enable_Rolling && _Is_Running_State())  // ← add the guard
      {
        foreach (var S in _Series)
        {
          if (S.Points.Count > _Max_Display_Points)
            S.Points.RemoveRange( 0, S.Points.Count - _Max_Display_Points );
        }
      }

      Chart_Panel_Control.Invalidate();
    }


    // ════════════════════════════════════════════════════════════════════
    // MOUSE / TOOLTIP
    // ════════════════════════════════════════════════════════════════════

    protected void Chart_Panel_Control_MouseMove( object? sender, MouseEventArgs e )
    {
      if (_Show_Timing_View || _Series.Count == 0)
      {
        _Chart_Tooltip.Hide( Chart_Panel_Control );
        return;
      }

      bool Has_Data = _Series.Any( s => s.Visible && s.Points.Count > 0 );
      if (!Has_Data)
      {
        _Chart_Tooltip.Hide( Chart_Panel_Control );
        return;
      }

      using var Block = Trace_Block.Start_If_Enabled();

      if (!_Settings.Show_Tooltips_On_Hover)
        return;
      if ((DateTime.Now - _Last_Tooltip_Update).TotalMilliseconds < 100)
        return;
      if (e.Location == _Last_Mouse_Position)
        return;

      _Last_Mouse_Position = e.Location;
      _Last_Tooltip_Update = DateTime.Now;

      var (Series, Point_Index, Distance) = Find_Closest_Point( e.Location );

      if (Series != null && Distance < _Settings.Tooltip_Distance_Threshold)
      {
        var Point_Data = Series.Points[ Point_Index ];
        string Tooltip_Text =
            $"{Series.Name}\n"
          + $"Time : {Point_Data.Time:HH:mm:ss.fff}\n"
          + $"Value: {Format_Digits( Point_Data.Value, Series.Display_Digits )} {Current_Unit}";
        _Chart_Tooltip.Show(
            Tooltip_Text, Chart_Panel_Control,
            e.Location.X + 15, e.Location.Y - 40,
            _Settings.Tooltip_Display_Duration_Ms );
      }
      else
      {
        _Chart_Tooltip.Hide( Chart_Panel_Control );
      }
    }

    protected (Instrument_Series? Series, int Index, double Distance) Find_Closest_Point( Point Mouse_Pos )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Instrument_Series? Closest_Series = null;
      int Closest_Index = -1;
      double Min_Distance = double.MaxValue;

      int W = Chart_Panel_Control.ClientSize.Width;
      int H = Chart_Panel_Control.ClientSize.Height;

      var (Time_Min, Time_Max) = Calculate_Time_Range();
      double Time_Range_Sec = (Time_Max - Time_Min).TotalSeconds;
      if (Time_Range_Sec < 0.001)
        Time_Range_Sec = 1.0;

      if (_Combined_View)
      {
        int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
        int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;

        double Global_Min = double.MaxValue, Global_Max = double.MinValue;

        foreach (var S in _Series.Where( s => s.Visible && s.Points.Count > 0 ))
        {
          var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
              S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
          if (Vc == 0)
            continue;
          for (int i = Si; i < Si + Vc; i++)
          {
            if (S.Points[ i ].Value < Global_Min)
              Global_Min = S.Points[ i ].Value;
            if (S.Points[ i ].Value > Global_Max)
              Global_Max = S.Points[ i ].Value;
          }
        }

        if (Global_Min == double.MaxValue)
          return (null, -1, double.MaxValue);

        double Range = Global_Max - Global_Min;
        double Padding = Math.Max( Range * 0.5, 0.0001 );
        double Pmin = Global_Min - Padding;
        double Pmax = Global_Max + Padding;
        double Pr = Pmax - Pmin;
        if (Pr == 0)
          Pr = 0.001;

        double Display_Min = Pmin;
        double Display_Range = Pr;

        if (_Zoom_Factor > 0 && _Zoom_Factor != 1.0)
        {
          double Center = (Pmax + Pmin) / 2.0;
          double Zoomed_Range = Pr / _Zoom_Factor;
          Display_Min = Center - Zoomed_Range / 2.0;
          Display_Range = Zoomed_Range;
        }

        foreach (var S in _Series.Where( s => s.Visible && s.Points.Count > 0 ))
        {
          var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
              S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
          if (Vc == 0)
            continue;

          DateTime Vtmin = S.Points[ Si ].Time;
          DateTime Vtmax = S.Points[ Si + Vc - 1 ].Time;
          double Vtr = Math.Max( (Vtmax - Vtmin).TotalSeconds, 0.001 );

          for (int i = 0; i < Vc; i++)
          {
            int Di = Si + i;
            var P = S.Points[ Di ];

            float X = _Chart_Margin_Left + (float) ((P.Time - Vtmin).TotalSeconds / Vtr * Chart_W);
            double Yn = (P.Value - Display_Min) / Display_Range;
            float Y = H - _Chart_Margin_Bottom - (float) (Yn * Chart_H);

            double Dist = Math.Sqrt( Math.Pow( Mouse_Pos.X - X, 2 ) + Math.Pow( Mouse_Pos.Y - Y, 2 ) );
            if (Dist < Min_Distance)
            {
              Min_Distance = Dist;
              Closest_Series = S;
              Closest_Index = Di;
            }
          }
        }
      }
      else
      {
        int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
        int Total_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;
        int Subplot_Count = _Series.Count( S => S.Visible );
        int Subplot_H = (Total_H - _Chart_Subplot_Gap * (Subplot_Count - 1)) / Subplot_Count;

        int SI = 0;
        foreach (var S in _Series)
        {
          if (!S.Visible || S.Points.Count == 0)
            continue;

          int Sub_Top = _Chart_Margin_Top + SI * (Subplot_H + _Chart_Subplot_Gap);
          int Sub_Bottom = Sub_Top + Subplot_H;
          int Label_Top = Sub_Top + 18;
          int Plot_H = Sub_Bottom - Label_Top;

          if (Mouse_Pos.Y < Label_Top || Mouse_Pos.Y > Sub_Bottom)
          {
            SI++;
            continue;
          }

          double Min_V = S.Get_Min(), Max_V = S.Get_Max();
          double Range = Max_V - Min_V;
          if (Range < 1e-12)
          {
            Range = Math.Abs( Max_V ) * 0.001;
            if (Range < 1e-12)
              Range = 1.0;
          }

          double Pmin = Min_V - Range * 0.5, Pmax = Max_V + Range * 0.5, Pr = Pmax - Pmin;
          double Display_Min = Pmin, Display_Range = Pr;

          if (_Zoom_Factor > 0 && _Zoom_Factor != 1.0)
          {
            double Center = (Pmax + Pmin) / 2.0;
            double Zr = Pr / _Zoom_Factor;
            Display_Min = Center - Zr / 2.0;
            Display_Range = Zr;
          }

          var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
              S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
          if (Vc == 0)
          {
            SI++;
            continue;
          }

          DateTime Vtmin = S.Points[ Si ].Time;
          DateTime Vtmax = S.Points[ Si + Vc - 1 ].Time;
          double Vtr = Math.Max( (Vtmax - Vtmin).TotalSeconds, 0.001 );

          for (int i = 0; i < Vc; i++)
          {
            int Di = Si + i;
            var P = S.Points[ Di ];

            float X = _Chart_Margin_Left + (float) ((P.Time - Vtmin).TotalSeconds / Vtr * Chart_W);
            double Yn = (P.Value - Display_Min) / Display_Range;
            float Y = Sub_Bottom - (float) (Yn * Plot_H);

            double Dist = Math.Sqrt( Math.Pow( Mouse_Pos.X - X, 2 ) + Math.Pow( Mouse_Pos.Y - Y, 2 ) );
            if (Dist < Min_Distance)
            {
              Min_Distance = Dist;
              Closest_Series = S;
              Closest_Index = Di;
            }
          }
          SI++;
        }
      }
      return (Closest_Series, Closest_Index, Min_Distance);
    }

    protected void Chart_Panel_Control_Mouse_Wheel( object? sender, MouseEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if (!_Enable_Rolling)
        return;

      int Delta = e.Delta > 0 ? 10 : -10;
      int New_Value = _Max_Display_Points + Delta;
      int Total = _Series.Count > 0 ? _Series.Max( s => s.Points.Count ) : 10;
      New_Value = Math.Max( 10, Math.Min( New_Value, Total ) );
      _Max_Display_Points = New_Value;
      Max_Points_Numeric_Control.Value = New_Value;
      Chart_Panel_Control.Invalidate();
    }


    // ════════════════════════════════════════════════════════════════════
    // KEYBOARD
    // ════════════════════════════════════════════════════════════════════

    protected override bool ProcessCmdKey( ref Message msg, Keys keyData )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      int Total_Points = _Series.Count > 0 ? _Series.Max( s => s?.Points?.Count ?? 0 ) : 0;
      int Max_Offset = Math.Max( 0, Total_Points - _Max_Display_Points );

      switch (keyData)
      {
        case Keys.Left:
          if (_Enable_Rolling)
          {
            _Auto_Scroll = false;
            _View_Offset = Math.Min( _View_Offset + 10, Max_Offset );
            Update_Scrollbar_Position();
            Chart_Panel_Control.Invalidate();
          }
          return true;

        case Keys.Right:
          if (_Enable_Rolling)
          {
            _View_Offset = Math.Max( 0, _View_Offset - 10 );
            if (_View_Offset == 0)
              _Auto_Scroll = true;
            Update_Scrollbar_Position();
            Chart_Panel_Control.Invalidate();
          }
          return true;

        case Keys.Home:
          if (_Enable_Rolling)
          {
            _Auto_Scroll = false;
            _View_Offset = Max_Offset;
            Update_Scrollbar_Position();
            Chart_Panel_Control.Invalidate();
          }
          return true;

        case Keys.End:
          _Auto_Scroll = true;
          _View_Offset = 0;
          Update_Scrollbar_Position();
          Chart_Panel_Control.Invalidate();
          return true;

        case Keys.PageUp:
          if (_Enable_Rolling)
          {
            _Auto_Scroll = false;
            _View_Offset = Math.Min( _View_Offset + _Max_Display_Points, Max_Offset );
            Update_Scrollbar_Position();
            Chart_Panel_Control.Invalidate();
          }
          return true;

        case Keys.PageDown:
          if (_Enable_Rolling)
          {
            _View_Offset = Math.Max( 0, _View_Offset - _Max_Display_Points );
            if (_View_Offset == 0)
              _Auto_Scroll = true;
            Update_Scrollbar_Position();
            Chart_Panel_Control.Invalidate();
          }
          return true;
      }
      return base.ProcessCmdKey( ref msg, keyData );
    }


    // ════════════════════════════════════════════════════════════════════
    // SCROLLBAR
    // ════════════════════════════════════════════════════════════════════

    protected void Update_Scrollbar_Position()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if (!Pan_Scrollbar_Control.Enabled || Pan_Scrollbar_Control.Maximum == 0)
        return;

      int Total_Points = _Series.Count > 0 ? _Series.Max( s => s?.Points?.Count ?? 0 ) : 0;
      int Max_Offset = Math.Max( 0, Total_Points - _Max_Display_Points );
      if (Max_Offset == 0)
      {
        Pan_Scrollbar_Control.Value = 0;
        return;
      }

      int Scrollbar_Max = Pan_Scrollbar_Control.Maximum - Pan_Scrollbar_Control.LargeChange + 1;
      int Scrollbar_Value = (int) ((double) _View_Offset / Max_Offset * Scrollbar_Max);
      Scrollbar_Value = Math.Max( 0, Math.Min( Scrollbar_Max, Scrollbar_Value ) );
      Pan_Scrollbar_Control.Value = Scrollbar_Value;
    }

    protected void Pan_Scrollbar_Control_Scroll( object sender, ScrollEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Updating_Scroll = true;
      Auto_Scroll_Check_Control.Checked = false;
      _Updating_Scroll = false;

      if (_Show_Timing_View)
      {
        int Total_Samples = Math.Min( _Timing_Count, _Timing_Buffer_Size );
        int Max_Offset = Math.Max( 0, Total_Samples - _Max_Display_Points );
        if (Max_Offset == 0)
          return;
        int Usable = Pan_Scrollbar_Control.Maximum - Pan_Scrollbar_Control.LargeChange;
        int Clamped = Math.Max( 0, Math.Min( Usable, e.NewValue ) );
        _Timing_View_Offset = Usable > 0 ? (int) ((double) Clamped / Usable * Max_Offset) : 0;
      }
      else
      {
        int Total_Points = _Series.Count > 0 ? _Series.Max( s => s?.Points?.Count ?? 0 ) : 0;
        int Max_Offset = Math.Max( 0, Total_Points - _Max_Display_Points );
        if (Max_Offset == 0)
          return;
        int Usable = Pan_Scrollbar_Control.Maximum - Pan_Scrollbar_Control.LargeChange;
        int Clamped = Math.Max( 0, Math.Min( Usable, e.NewValue ) );
        _View_Offset = Usable > 0 ? (int) ((double) Clamped / Usable * Max_Offset) : 0;

        if (_View_Offset == 0)
        {
          _Updating_Scroll = true;
          Auto_Scroll_Check_Control.Checked = true;
          _Updating_Scroll = false;
        }
      }
      Chart_Panel_Control.Invalidate();
    }

    protected void Pan_Scrollbar_Control_ValueChanged( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      _Updating_Scroll = true;
      Auto_Scroll_Check_Control.Checked = false;
      _Updating_Scroll = false;

      int Total_Points = _Show_Timing_View
          ? _Timing_Count
          : (_Series.Count > 0 ? _Series.Max( S => S?.Points?.Count ?? 0 ) : 0);

      if (Total_Points == 0)
        return;
      int Max_Offset = Math.Max( 0, Total_Points - _Max_Display_Points );
      if (Max_Offset == 0)
        return;
      int Scrollbar_Max = Pan_Scrollbar_Control.Maximum - Pan_Scrollbar_Control.LargeChange + 1;
      if (Scrollbar_Max <= 0)
        return;
      int Clamped_Value = Math.Max( 0, Math.Min( Scrollbar_Max, Pan_Scrollbar_Control.Value ) );
      _View_Offset = (int) ((double) Clamped_Value / Scrollbar_Max * Max_Offset);

      if (_View_Offset == 0)
      {
        _Updating_Scroll = true;
        Auto_Scroll_Check_Control.Checked = true;
        _Updating_Scroll = false;
      }

      Chart_Panel_Control.Invalidate();
    }

    protected void Auto_Scroll_Check_CheckedChanged( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if (_Updating_Scroll)
        return;
      _Auto_Scroll = Auto_Scroll_Check_Control.Checked;
      if (_Auto_Scroll)
      {
        _View_Offset = 0;
        if (Pan_Scrollbar_Control?.Enabled == true)
          Pan_Scrollbar_Control.Value = 0;
        Chart_Panel_Control.Invalidate();
      }
    }

    protected void Update_Data_Scrollbar( int Total_Points )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if (Total_Points <= _Max_Display_Points)
      {
        Pan_Scrollbar_Control.Enabled = false;
        Pan_Scrollbar_Control.Value = 0;
        return;
      }

      int Max_Offset = Total_Points - _Max_Display_Points;
      int Large_Change = Math.Max( 10, _Max_Display_Points );
      int Scroll_Max = Max_Offset + Large_Change;

      Pan_Scrollbar_Control.Enabled = true;
      Pan_Scrollbar_Control.Minimum = 0;
      Pan_Scrollbar_Control.LargeChange = Large_Change;
      Pan_Scrollbar_Control.SmallChange = Math.Max( 1, Max_Offset / 100 );
      Pan_Scrollbar_Control.Maximum = Scroll_Max;

      if (_Auto_Scroll)
      {
        _View_Offset = 0;
        Pan_Scrollbar_Control.Value = 0;
      }
      else
      {
        _View_Offset = Math.Min( _View_Offset, Max_Offset );
        int Usable = Scroll_Max - Large_Change;
        Pan_Scrollbar_Control.Value = Usable > 0 ? (int) ((double) _View_Offset / Max_Offset * Usable) : 0;
      }
    }

    protected void Update_Timing_Scrollbar()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Total_Samples = Math.Min( _Timing_Count, _Timing_Buffer_Size );

      if (Total_Samples <= _Max_Display_Points)
      {
        Pan_Scrollbar_Control.Enabled = false;
        Pan_Scrollbar_Control.Value = 0;
        _Timing_View_Offset = 0;
        return;
      }

      int Max_Offset = Total_Samples - _Max_Display_Points;
      int Large_Change = Math.Max( 10, _Max_Display_Points );
      int Scroll_Max = Max_Offset + Large_Change;

      Pan_Scrollbar_Control.Enabled = true;
      Pan_Scrollbar_Control.Minimum = 0;
      Pan_Scrollbar_Control.LargeChange = Large_Change;
      Pan_Scrollbar_Control.SmallChange = Math.Max( 1, Max_Offset / 100 );
      Pan_Scrollbar_Control.Maximum = Scroll_Max;
      _Timing_View_Offset = 0;
      Pan_Scrollbar_Control.Value = 0;
    }


    // ════════════════════════════════════════════════════════════════════
    // LEGEND
    // ════════════════════════════════════════════════════════════════════

    protected void Show_Stats_Popup()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      var Dlg = new Form
      {
        Text = "Instrument Statistics",
        Size = new Size( 560, 500 ),
        MinimumSize = new Size( 400, 300 ),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.Sizable,
        BackColor = SystemColors.Control,
      };

      // ── Header ────────────────────────────────────────────────────────
      var Header = new Panel
      {
        Dock = DockStyle.Top,
        Height = 48,
        BackColor = Color.FromArgb( 45, 45, 48 ),
      };
      Header.Controls.Add( new Label
      {
        Text = "Instrument Statistics",
        ForeColor = Color.White,
        Font = new Font( "Segoe UI", 13f, FontStyle.Bold ),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding( 16, 0, 0, 0 ),
      } );

      // ── Rich text area ────────────────────────────────────────────────
      var Rtb = new RichTextBox
      {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = SystemColors.Control,
        Font = new Font( "Consolas", 9.5f ),
        ScrollBars = RichTextBoxScrollBars.Vertical,
        Padding = new Padding( 12 ),
      };

      // ── Build content ─────────────────────────────────────────────────
      Build_Stats_Rtb( Rtb );

      // ── Footer ────────────────────────────────────────────────────────
      var Footer = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 44,
        BackColor = SystemColors.ControlLight,
      };

      var Refresh_Btn = new Button
      {
        Text = "Refresh",
        Size = new Size( 88, 28 ),
        Location = new Point( 12, 8 ),
      };
      Refresh_Btn.Click += ( s, e ) => Build_Stats_Rtb( Rtb );

      var Close_Btn = new Button
      {
        Text = "Close",
        Size = new Size( 88, 28 ),
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
      };
      Close_Btn.Click += ( s, e ) => Dlg.Close();

      Footer.Controls.Add( Refresh_Btn );
      Footer.Controls.Add( Close_Btn );

      Dlg.Controls.Add( Rtb );
      Dlg.Controls.Add( Footer );
      Dlg.Controls.Add( Header );

      // Position close button on the right after layout
      Dlg.Shown += ( s, e ) =>
        Close_Btn.Location = new Point( Footer.Width - 100, 8 );
      Dlg.Resize += ( s, e ) =>
        Close_Btn.Location = new Point( Footer.Width - 100, 8 );

      Dlg.ShowDialog( this );
    }

    private void Build_Stats_Rtb( RichTextBox Rtb )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Rtb.Clear();

      void Append_Row( string Label, string Value )
      {
        Rtb.SelectionFont = new Font( "Consolas", 9.5f );
        Rtb.SelectionColor = SystemColors.GrayText;
        Rtb.AppendText( $"  {Label,-18}" );
        Rtb.SelectionFont = new Font( "Consolas", 9.5f );
        Rtb.SelectionColor = SystemColors.ControlText;
        Rtb.AppendText( Value + "\n" );
      }

      void Append_Sep()
      {
        Rtb.SelectionFont = new Font( "Consolas", 9.5f );
        Rtb.SelectionColor = SystemColors.GrayText;
        Rtb.AppendText( new string( '─', 55 ) + "\n" );
      }

      for (int I = 0; I < _Series.Count; I++)
      {
        var S = _Series[ I ];

        // Coloured instrument name header
        Rtb.SelectionFont = new Font( "Segoe UI", 10f, FontStyle.Bold );
        Rtb.SelectionColor = S.Line_Color;
        Rtb.AppendText( $"● {S.Name}   GPIB: {S.Address}   NPLC: {S.NPLC}\n" );

        Append_Sep();

        if (S.Points.Count > 0)
        {
          Append_Row( "Last", Format_Digits( S.Get_Last(), S.Display_Digits ) );
          Append_Row( "Average", Format_Digits( S.Get_Average(), S.Display_Digits ) );
          Append_Row( "σ", Format_Digits( S.Get_StdDev(), S.Display_Digits ) );
          Append_Row( "Min", Format_Digits( S.Get_Min(), S.Display_Digits ) );
          Append_Row( "Max", Format_Digits( S.Get_Max(), S.Display_Digits ) );
          Append_Row( "Trend", S.Get_Trend() );
          Append_Row( "Points", S.Points.Count.ToString( "N0" ) );
        }
        else
        {
          Append_Row( "Data", "No data" );
        }

        Append_Row( "Disconnects", S.Disconnect_Count.ToString() );
        Append_Row( "Comm Errors", S.Comm_Error_Count.ToString() );

        // File stats from preamble if available
        if (S.File_Stats != null && S.File_Stats.Count > 0)
        {
          Rtb.AppendText( "\n" );
          Rtb.SelectionFont = new Font( "Segoe UI", 9f, FontStyle.Italic );
          Rtb.SelectionColor = SystemColors.GrayText;
          Rtb.AppendText( "  Recorded stats:\n" );
          foreach (var Kv in S.File_Stats)
            Append_Row( Kv.Key, Kv.Value );
        }

        Rtb.AppendText( "\n" );
      }
    }




    protected void Legend_Toggle_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      Show_Stats_Popup();
    }


    protected void Chart_Panel_Control_Resize( object? sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Chart_Panel_Control.Invalidate();
    }

    protected void Create_Legend_Panel()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if (_Legend_Panel != null)
      {
        _Legend_Panel.Controls.Clear();
        Controls.Remove( _Legend_Panel );
        _Legend_Panel.Dispose();
        _Legend_Panel = null;
      }

      _Legend_Panel = new FlowLayoutPanel
      {
        Height = 30,
        Dock = DockStyle.Top,
        AutoScroll = false,
        WrapContents = false,
        Padding = new Padding( 10, 5, 10, 5 )
      };

      _Legend_Panel.Controls.Add( new Label
      {
        Text = "Show:",
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding( 0, 5, 10, 0 )
      } );

      for (int I = 0; I < _Series.Count; I++)
      {
        var S = _Series[ I ];
        var Cb = new CheckBox
        {
          Text = $"{S.Name} Meter Roll: {S.Role}  GPIB: {S.Address}  NPLC: {S.NPLC}",
          Checked = S.Visible,
          AutoSize = true,
          Margin = new Padding( 5, 3, 15, 0 ),
          Tag = I
        };
        Cb.CheckedChanged += Legend_CheckBox_Changed;
        _Legend_Panel.Controls.Add( Cb );
      }
      Controls.Add( _Legend_Panel );
      _Legend_Panel.BringToFront();
    }

    protected void Legend_CheckBox_Changed( object? sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if (sender is not CheckBox Cb || Cb.Tag is not int Index)
        return;

      int Visible_Count = _Series.Count( S => S.Visible );
      if (Visible_Count == 1 && _Series[ Index ].Visible && !Cb.Checked)
      {
        Cb.Checked = true;
        MessageBox.Show( "At least one instrument must remain visible.",
            "Cannot Hide", MessageBoxButtons.OK, MessageBoxIcon.Information );
        return;
      }
      _Series[ Index ].Visible = Cb.Checked;
      Chart_Panel_Control.Invalidate();
    }








    // ════════════════════════════════════════════════════════════════════
    // PERFORMANCE
    // ════════════════════════════════════════════════════════════════════

    protected void Update_Memory_Status( int Current, int Max )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // subclass wires to its own status label
    }

    protected void Update_Chart_Refresh_Rate( System.Windows.Forms.Timer Chart_Refresh_Timer )
    {
      if (Chart_Refresh_Timer == null)
        return;

      using var Block = Trace_Block.Start_If_Enabled();
      int Total_Points = _Series.Sum( s => s.Points.Count );
      int Base_Rate = _Settings.Chart_Refresh_Rate_Ms;

      if (_Settings.Throttle_When_Many_Points && Total_Points > _Settings.Throttle_Point_Threshold)
      {
        int Multiplier =
            Total_Points > _Settings.Throttle_Point_Threshold * 10 ? 4 :
            Total_Points > _Settings.Throttle_Point_Threshold * 5 ? 3 :
            Total_Points > _Settings.Throttle_Point_Threshold * 2 ? 2 : 1;
        Chart_Refresh_Timer.Interval = Base_Rate * Multiplier;
      }
      else
      {
        Chart_Refresh_Timer.Interval = Base_Rate;
      }

      Update_Performance_Status();
    }

    protected void Chart_Refresh_Timer_Tick( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      Chart_Panel_Control.Invalidate();
    }

    protected int Calculate_Y_Axis_Width( Graphics G, double Min_V, double Max_V )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      using var Measure_Font = new Font( "Consolas", 7.5f );
      float Max_Label_Width = 0;
      var Ref_Series = _Series.FirstOrDefault( s => s.Visible && s.Points.Count > 0 );

      for (int I = 0; I <= 5; I++)
      {
        double Fraction = (double) I / 5;
        double Y_Val = Min_V + Fraction * (Max_V - Min_V);
        string Y_Label = Multimeter_Common_Helpers_Class.Format_Value(
            Y_Val, Current_Unit,
            Ref_Series?.Type ?? _Selected_Meter,
            Ref_Series?.Display_Digits ?? 6 );
        float Label_W = G.MeasureString( Y_Label, Measure_Font ).Width;
        if (Label_W > Max_Label_Width)
          Max_Label_Width = Label_W;
      }
      return (int) Max_Label_Width + 10;
    }


    // ════════════════════════════════════════════════════════════════════
    // DRAW HELPERS — PRIMITIVES
    // ════════════════════════════════════════════════════════════════════

    protected void Draw_Empty_State( Graphics G, int W, int H, string Message )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      using var Empty_Font = new Font( "Segoe UI", 10F );
      using var Empty_Brush = new SolidBrush( _Theme.Labels );
      G.DrawString( Message, Empty_Font, Empty_Brush, 20, H / 2 );
    }

    protected void Draw_Instrument_List( Graphics G, int H )
    {

      using var Block = Trace_Block.Start_If_Enabled();
      using var Empty_Font = new Font( "Segoe UI", 10F );
      using var Empty_Brush = new SolidBrush( _Theme.Labels );
      int Y_Pos = 30;
      G.DrawString( "Press Load to load a recorded file.", Empty_Font, Empty_Brush, 20, Y_Pos );
      Y_Pos += 25;

      for (int I = 0; I < _Series.Count; I++)
      {
        var S = _Series[ I ];
        using var Dot_Brush = new SolidBrush( S.Line_Color );
        G.FillEllipse( Dot_Brush, 30, Y_Pos + 3, 10, 10 );
        G.DrawString( $"{S.Name} GPIB: {S.Address}  NPLC: {S.NPLC}",
            Empty_Font, Empty_Brush, 48, Y_Pos );
        Y_Pos += 22;
      }
    }

    protected void Draw_Position_Indicator( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if (!_Enable_Rolling)
        return;

      int Total_Points = _Series.Count > 0 ? _Series.Max( s => s?.Points?.Count ?? 0 ) : 0;
      if (Total_Points <= _Max_Display_Points)
        return;

      int Indicator_Y = H - 45, Indicator_W = 200, Indicator_H = 20;
      int Indicator_X = W - Indicator_W - 20;

      using (var Bg_Brush = new SolidBrush( Color.FromArgb( 200, _Theme.Background ) ))
        G.FillRectangle( Bg_Brush, Indicator_X, Indicator_Y, Indicator_W, Indicator_H );
      using (var Border_Pen = new Pen( _Theme.Grid, 1f ))
        G.DrawRectangle( Border_Pen, Indicator_X, Indicator_Y, Indicator_W, Indicator_H );

      int Bar_W = (int) ((double) _Max_Display_Points / Total_Points * Indicator_W);
      int MaxOff = Total_Points - _Max_Display_Points;
      int Bar_X = Indicator_X + (int) ((1.0 - (double) _View_Offset / MaxOff) * (Indicator_W - Bar_W));

      using (var Bar_Brush = new SolidBrush( Color.FromArgb( 150, Color.LightBlue ) ))
        G.FillRectangle( Bar_Brush, Bar_X, Indicator_Y + 2, Bar_W, Indicator_H - 4 );

      string Position_Text = _Auto_Scroll ? "Live" : $"-{_View_Offset} pts";
      using var Text_Font = new Font( "Segoe UI", 8F );
      using var Text_Brush = new SolidBrush( _Theme.Foreground );
      var Text_Size = G.MeasureString( Position_Text, Text_Font );
      G.DrawString( Position_Text, Text_Font, Text_Brush,
          Indicator_X + (Indicator_W - Text_Size.Width) / 2,
          Indicator_Y + (Indicator_H - Text_Size.Height) / 2 );
    }

    protected void Draw_Time_Axis( Graphics G, int H, int Chart_W, double Time_Range_Sec,
        Pen Grid_Pen, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      using var X_Label_Font = new Font( "Segoe UI", 7.5F );

      int Num_X_Labels = Math.Max( 2, Math.Min( 8, (int) (Chart_W / 80.0) ) );

      for (int I = 0; I <= Num_X_Labels; I++)
      {
        double Fraction = (double) I / Num_X_Labels;
        int X_Pos = _Chart_Margin_Left + (int) (Fraction * Chart_W);
        string Time_Text = Format_Time_Label( Fraction * Time_Range_Sec, Time_Range_Sec );
        var Ts = G.MeasureString( Time_Text, X_Label_Font );
        G.DrawString( Time_Text, X_Label_Font, Label_Brush, X_Pos - Ts.Width / 2,
            H - _Chart_Margin_Bottom + 8 );
        G.DrawLine( Grid_Pen, X_Pos, _Chart_Margin_Top, X_Pos, H - _Chart_Margin_Bottom );
      }
    }

    protected void Draw_Line_And_Fill( Graphics G, PointF[] Points, Color Line_Color,
        int Sub_Bottom, int Label_Top )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Count = Points.Length;
      if (Count < 2)
        return;

      using var Line_Pen = new Pen( Line_Color, 2f );
      PointF[] Fill_Points = new PointF[ Count + 2 ];
      Array.Copy( Points, Fill_Points, Count );
      Fill_Points[ Count ] = new PointF( Points[ Count - 1 ].X, Sub_Bottom );
      Fill_Points[ Count + 1 ] = new PointF( Points[ 0 ].X, Sub_Bottom );

      using var Fill_Brush = new LinearGradientBrush(
          new PointF( 0, Label_Top ), new PointF( 0, Sub_Bottom ),
          Color.FromArgb( 60, Line_Color ), Color.FromArgb( 5, Line_Color ) );

      G.FillPolygon( Fill_Brush, Fill_Points );
      G.DrawLines( Line_Pen, Points );
    }

    protected void Draw_Data_Dots( Graphics G, PointF[] Points, Color Line_Color )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Count = Points.Length;
      if (Count > 150)
        return;

      using var Dot_Brush = new SolidBrush(
          Color.FromArgb( 200, Line_Color.R, Line_Color.G, Line_Color.B ) );
      float Dot_Size = Count > 100 ? 3f : Count > 50 ? 4f : 5f;

      foreach (PointF P in Points)
        G.FillEllipse( Dot_Brush, P.X - Dot_Size / 2, P.Y - Dot_Size / 2, Dot_Size, Dot_Size );
    }

    protected void Draw_Step_And_Fill( Graphics G, PointF[] Points, Color Line_Color,
        int Sub_Bottom, int Label_Top )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Count = Points.Length;
      if (Count < 2)
        return;

      var Step_Points = new List<PointF>( Count * 2 );
      for (int I = 0; I < Count - 1; I++)
      {
        Step_Points.Add( Points[ I ] );
        Step_Points.Add( new PointF( Points[ I + 1 ].X, Points[ I ].Y ) );
      }
      Step_Points.Add( Points[ Count - 1 ] );
      PointF[] SP = Step_Points.ToArray();

      PointF[] Fill_Points = new PointF[ SP.Length + 2 ];
      Array.Copy( SP, Fill_Points, SP.Length );
      Fill_Points[ SP.Length ] = new PointF( SP[ SP.Length - 1 ].X, Sub_Bottom );
      Fill_Points[ SP.Length + 1 ] = new PointF( SP[ 0 ].X, Sub_Bottom );

      using var Fill_Brush = new LinearGradientBrush(
          new PointF( 0, Label_Top ), new PointF( 0, Sub_Bottom ),
          Color.FromArgb( 60, Line_Color ), Color.FromArgb( 5, Line_Color ) );
      G.FillPolygon( Fill_Brush, Fill_Points );

      Color Total_Line_Color = _Theme.Background.GetBrightness() > 0.5f
          ? Color.DodgerBlue : Color.White;
      using var Line_Pen = new Pen( Total_Line_Color, 2f );
      G.DrawLines( Line_Pen, SP );
    }

    protected void Draw_Subplot_Bars( Graphics G, PointF[] Points, Color Line_Color,
        int Sub_Bottom, int Label_Top )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Count = Points.Length;
      if (Count == 0)
        return;

      float Bar_W = Count > 1 ? Math.Min( (Points[ 1 ].X - Points[ 0 ].X) * 0.7f, 20f ) : 10f;
      Color Fill_Top = Color.FromArgb( 180, Line_Color );
      Color Fill_Bot = Color.FromArgb( 60, Line_Color );
      using var Border_Pen = new Pen( Line_Color, 1f );

      foreach (PointF P in Points)
      {
        float Bar_H = Sub_Bottom - P.Y;
        if (Bar_H < 1f)
          continue;
        RectangleF Rect = new( P.X - Bar_W / 2, P.Y, Bar_W, Bar_H );
        using var Fill_Brush = new LinearGradientBrush(
            new PointF( 0, P.Y ), new PointF( 0, Sub_Bottom ), Fill_Top, Fill_Bot );
        G.FillRectangle( Fill_Brush, Rect );
        G.DrawRectangle( Border_Pen, Rect.X, Rect.Y, Rect.Width, Rect.Height );
      }
    }

    protected void Draw_Last_Point( Graphics G, PointF[] Points, Instrument_Series S,
        int W, Font Label_Font, Brush Name_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Count = Points.Length;
      if (Count == 0)
        return;

      PointF Last = Points[ Count - 1 ];
      using var Glow_Pen = new Pen( Color.FromArgb( 80, S.Line_Color ), 6f );
      G.DrawEllipse( Glow_Pen, Last.X - 5, Last.Y - 5, 10, 10 );
      using var Last_Brush = new SolidBrush( Color.White );
      G.FillEllipse( Last_Brush, Last.X - 3, Last.Y - 3, 6, 6 );

      string Val_Text = Multimeter_Common_Helpers_Class.Format_Value(
          S.Points[ Count - 1 ].Value, Current_Unit, S.Type, S.Display_Digits );
      var Val_Size = G.MeasureString( Val_Text, Label_Font );
      float Tx = Last.X + 8;
      if (Tx + Val_Size.Width > W - _Chart_Margin_Right)
        Tx = Last.X - Val_Size.Width - 8;
      G.DrawString( Val_Text, Label_Font, Name_Brush, Tx, Last.Y - Val_Size.Height / 2 );
    }


    // ════════════════════════════════════════════════════════════════════
    // DRAW HELPERS — Y AXIS
    // ════════════════════════════════════════════════════════════════════

    protected void Draw_Y_Axis( Graphics G, double Padded_Min, double Padded_Range,
      int Sub_Bottom, int Plot_H, int W,
      Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      for (int I = 0; I <= 4; I++)
      {
        double Fraction = (double) I / 4;
        double Value = Padded_Min + Fraction * Padded_Range;
        int Y = Sub_Bottom - (int) (Fraction * Plot_H);
        G.DrawLine( Grid_Pen, _Chart_Margin_Left, Y, W - _Chart_Margin_Right, Y );
        string Lbl = Format_Value( Value );
        var Sz = G.MeasureString( Lbl, Label_Font );
        G.DrawString( Lbl, Label_Font, Label_Brush,
            _Chart_Margin_Left - Sz.Width - 4, Y - Sz.Height / 2 );
      }
    }

    // Single convenience wrapper — pass Chart_H explicitly, or omit to derive from H
    protected void Draw_Y_Axis( Graphics G, double Padded_Min, double Padded_Range,
        int W, int H, Pen Grid_Pen, Font Label_Font, Brush Label_Brush,
        int Chart_H = -1 )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      int Actual_Chart_H = Chart_H >= 0
          ? Chart_H
          : H - _Chart_Margin_Top - _Chart_Margin_Bottom;

      Draw_Y_Axis( G, Padded_Min, Padded_Range,
          H - _Chart_Margin_Bottom, Actual_Chart_H,
          W, Grid_Pen, Label_Font, Label_Brush );
    }



    protected void Draw_Y_Axis_Subplot( Graphics G, double Min, double Range,
        int W, int Sub_Bottom, int Plot_H,
        Pen Grid_Pen, Font Label_Font, Brush Label_Brush,
        int Display_Digits = 6 )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      for (int I = 0; I <= 4; I++)
      {
        double Fraction = (double) I / 4;
        double Value = Min + Range * Fraction;
        int Y = Sub_Bottom - (int) (Fraction * Plot_H);
        G.DrawLine( Grid_Pen, _Chart_Margin_Left, Y, W - _Chart_Margin_Right, Y );
        string Lbl = Format_Sig_Figs( Value, Display_Digits );
        var Sz = G.MeasureString( Lbl, Label_Font );
        G.DrawString( Lbl, Label_Font, Label_Brush,
            _Chart_Margin_Left - Sz.Width - 4, Y - Sz.Height / 2 );
      }
    }

    protected void Draw_Y_Axis_Percentage( Graphics G, int W, int H,
        Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;
      for (int I = 0; I <= 4; I++)
      {
        float Y_Ratio = I / 4f;
        float Y = H - _Chart_Margin_Bottom - Y_Ratio * Chart_H;
        G.DrawLine( Grid_Pen, _Chart_Margin_Left, Y, W - _Chart_Margin_Right, Y );
        string Label = $"{I * 25}%";
        SizeF Sz = G.MeasureString( Label, Label_Font );
        G.DrawString( Label, Label_Font, Label_Brush,
            _Chart_Margin_Left - Sz.Width - 5, Y - Sz.Height / 2 );
      }
    }

    protected void Draw_Grid_And_Axes( Graphics G, int W, int H, double Min_V, double Max_V )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;
      using var Grid_Pen = new Pen( _Theme.Grid, 1f );
      using var Label_Brush = new SolidBrush( _Theme.Labels );
      using var Axis_Pen = new Pen( _Theme.Separator, 1f );
      using var Label_Font = new Font( "Consolas", 7.5f );
      float Baseline = H - _Chart_Margin_Bottom;

      var Ref_Series = _Series.FirstOrDefault( s => s.Visible && s.Points.Count > 0 );

      for (int I = 0; I <= 5; I++)
      {
        double Fraction = (double) I / 5;
        int Y_Pos = H - _Chart_Margin_Bottom - (int) (Fraction * Chart_H);
        double Y_Val = Min_V + Fraction * (Max_V - Min_V);
        G.DrawLine( Grid_Pen, _Chart_Margin_Left, Y_Pos, W - _Chart_Margin_Right, Y_Pos );

        string Y_Label = Multimeter_Common_Helpers_Class.Format_Value(
            Y_Val, Current_Unit,
            Ref_Series?.Type ?? _Selected_Meter,
            Ref_Series?.Display_Digits ?? 6 );
        var Sz = G.MeasureString( Y_Label, Label_Font );
        G.DrawString( Y_Label, Label_Font, Label_Brush,
            _Chart_Margin_Left - Sz.Width - 4, Y_Pos - Sz.Height / 2 );
      }

      for (int I = 0; I <= 6; I++)
      {
        int X_Pos = _Chart_Margin_Left + (int) ((double) I / 6 * Chart_W);
        G.DrawLine( Grid_Pen, X_Pos, _Chart_Margin_Top, X_Pos, Baseline );

        var Best = _Series.OrderByDescending( s => s.Points.Count ).FirstOrDefault();
        if (Best != null && Best.Points.Count >= 2)
        {
          var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
              Best.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
          int Ac = Math.Min( Vc, Best.Points.Count - Si );
          if (Ac >= 2)
          {
            int Pi = Math.Clamp(
                Si + (int) ((double) I / 6 * (Ac - 1)),
                0, Best.Points.Count - 1 );
            string X_Label = Best.Points[ Pi ].Time.ToString( "HH:mm:ss" );
            var Xsz = G.MeasureString( X_Label, Label_Font );
            G.DrawString( X_Label, Label_Font, Label_Brush,
                X_Pos - Xsz.Width / 2, Baseline + 4 );
          }
        }
      }
      G.DrawRectangle( Axis_Pen, _Chart_Margin_Left, _Chart_Margin_Top, Chart_W, Chart_H );
    }


    // ════════════════════════════════════════════════════════════════════
    // DRAW — SPLIT VIEW
    // ════════════════════════════════════════════════════════════════════

    protected void Draw_Split_View( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      var (Time_Min, Time_Max) = Calculate_Time_Range();
      double Time_Range_Sec = (Time_Max - Time_Min).TotalSeconds;
      if (Time_Range_Sec < 0.001)
        Time_Range_Sec = 1.0;

      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Total_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;
      int Subplot_Count = _Series.Count( S => S.Visible );
      int Subplot_H = (Total_H - _Chart_Subplot_Gap * (Subplot_Count - 1)) / Subplot_Count;

      if (Chart_W < 10 || Subplot_H < 30)
        return;
      if (_Chart_Grid_Pen == null)
        Initialize_Chart_Resources();

      var Grid_Pen = _Chart_Grid_Pen!;
      var Label_Font = _Chart_Label_Font!;
      var Name_Font = _Chart_Name_Font!;
      var Label_Brush = _Chart_Label_Brush!;
      var Sep_Pen = _Chart_Sep_Pen!;
      Sep_Pen.DashStyle = DashStyle.Dash;

      int SI = 0;
      for (int I = 0; I < _Series.Count; I++)
      {
        var S = _Series[ I ];
        if (!S.Visible)
          continue;
        int Sub_Top = _Chart_Margin_Top + SI * (Subplot_H + _Chart_Subplot_Gap);
        int Sub_Bottom = Sub_Top + Subplot_H;
        Draw_Subplot( G, S, SI, Sub_Top, Sub_Bottom, W,
            Time_Min, Time_Range_Sec, Chart_W,
            Grid_Pen, Sep_Pen, Label_Font, Name_Font, Label_Brush );
        SI++;
      }

      if (_Current_Graph_Style != "Histogram" && _Current_Graph_Style != "Pie")
        Draw_Time_Axis( G, H, Chart_W, Time_Range_Sec, Grid_Pen, Label_Brush );
    }

    protected void Draw_Subplot( Graphics G, Instrument_Series S, int Subplot_Index,
        int Sub_Top, int Sub_Bottom, int W,
        DateTime Time_Min, double Time_Range_Sec, int Chart_W,
        Pen Grid_Pen, Pen Sep_Pen, Font Label_Font, Font Name_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      using var Name_Brush = new SolidBrush( S.Line_Color );

      G.DrawString( $"{S.Name} GPIB {S.Address}  NPLC {S.NPLC}",
          Name_Font, Name_Brush, _Chart_Margin_Left + 4, Sub_Top + 2 );

      if (Subplot_Index > 0)
      {
        int Sep_Y = Sub_Top - 4;
        G.DrawLine( Sep_Pen, _Chart_Margin_Left, Sep_Y, W - _Chart_Margin_Right, Sep_Y );
      }

      if (S.Points.Count == 0)
      {
        G.DrawString( "No data", Label_Font, Label_Brush, _Chart_Margin_Left + 4, Sub_Top + 20 );
        return;
      }

      double Min_V = S.Get_Min(), Max_V = S.Get_Max();
      double Range = Max_V - Min_V;
      if (Range < 1e-12)
      {
        Range = Math.Abs( Max_V ) * 0.001;
        if (Range < 1e-12)
          Range = 1.0;
      }

      double Padded_Min = Min_V - Range * 0.5;
      double Padded_Max = Max_V + Range * 0.5;
      double Padded_Range = Padded_Max - Padded_Min;
      double Display_Min = Padded_Min, Display_Max = Padded_Max, Display_Range = Padded_Range;

      if (_Zoom_Factor > 0 && _Zoom_Factor != 1.0)
      {
        double Center = (Padded_Max + Padded_Min) / 2.0;
        double Zoomed_Range = Padded_Range / _Zoom_Factor;
        Display_Min = Center - Zoomed_Range / 2.0;
        Display_Max = Center + Zoomed_Range / 2.0;
        Display_Range = Display_Max - Display_Min;
      }

      int Label_Top = Sub_Top + 18, Plot_H = Sub_Bottom - Label_Top;

      Draw_Y_Axis_Subplot( G, Display_Min, Display_Range, W, Sub_Bottom, Plot_H,
          Grid_Pen, Label_Font, Label_Brush, S.Display_Digits );

      PointF[] Points = Build_Point_Array(
          S.Points, Time_Min, Time_Range_Sec,
          Display_Min, Display_Range, Chart_W, Sub_Bottom, Plot_H );

      switch (_Current_Graph_Style)
      {
        case "Scatter":
          Draw_Data_Dots( G, Points, S.Line_Color );
          break;
        case "Step":
          Draw_Step_And_Fill( G, Points, S.Line_Color, Sub_Bottom, Label_Top );
          Draw_Data_Dots( G, Points, S.Line_Color );
          break;
        case "Bar":
          Draw_Subplot_Bars( G, Points, S.Line_Color, Sub_Bottom, Label_Top );
          break;
        case "Histogram":
          Draw_Subplot_Histogram( G, S, W, Sub_Bottom, Label_Top, Display_Min, Display_Range );
          break;
        default:
          Draw_Line_And_Fill( G, Points, S.Line_Color, Sub_Bottom, Label_Top );
          Draw_Data_Dots( G, Points, S.Line_Color );
          break;
      }

      Draw_Last_Point( G, Points, S, W, Label_Font, Name_Brush );
    }


    // ════════════════════════════════════════════════════════════════════
    // DRAW — COMBINED VIEW
    // ════════════════════════════════════════════════════════════════════

    protected void Draw_Combined_View( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      var (Time_Min, Time_Max) = Calculate_Time_Range();
      double Time_Range_Sec = (Time_Max - Time_Min).TotalSeconds;
      if (Time_Range_Sec < 0.001)
        Time_Range_Sec = 1.0;

      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;
      if (Chart_W < 10 || Chart_H < 10)
        return;

      if (_Chart_Grid_Pen == null)
        Initialize_Chart_Resources();

      var Grid_Pen = _Chart_Grid_Pen!;
      var Label_Font = _Chart_Label_Font!;
      var Name_Font = _Chart_Name_Font!;
      var Label_Brush = _Chart_Label_Brush!;

      Draw_Time_Axis( G, H, Chart_W, Time_Range_Sec, Grid_Pen, Label_Brush );

      int Name_X = _Chart_Margin_Left + 5, Name_Y = 5, Color_Index = 0;
      foreach (var S in _Series.Where( s => s.Visible ))
      {
        Color LC = _Theme.Line_Colors[ Color_Index % _Theme.Line_Colors.Length ];
        using (var CB = new SolidBrush( LC ))
          G.FillRectangle( CB, Name_X, Name_Y + 2, 14, 14 );
        using (var BP = new Pen( _Theme.Foreground, 1f ))
          G.DrawRectangle( BP, Name_X, Name_Y + 2, 14, 14 );
        using (var NB = new SolidBrush( LC ))
          G.DrawString( S.Name, Name_Font, NB, Name_X + 20, Name_Y );
        Name_X += (int) G.MeasureString( S.Name, Name_Font ).Width + 40;
        Color_Index++;
      }

      switch (_Current_Graph_Style)
      {
        case "Scatter":
          Draw_Combined_Scatter( G, W, H, Time_Min, Time_Range_Sec, Grid_Pen, Label_Font, Label_Brush );
          break;
        case "Step":
          Draw_Combined_Step( G, W, H, Time_Min, Time_Range_Sec, Grid_Pen, Label_Font, Label_Brush );
          break;
        case "Histogram":
          Draw_Single_Histogram( G, W, H, Chart_W, Chart_H );
          break;
        case "Pie":
          Draw_Single_Pie( G, W, H, Chart_W, Chart_H );
          break;
        default:
          if (_Normalized_View)
            Draw_Combined_Normalized( G, W, H, Time_Min, Time_Range_Sec, Grid_Pen, Label_Font, Label_Brush );
          else
            Draw_Combined_Absolute( G, W, H, Chart_W, Chart_H, Time_Min, Time_Range_Sec, Grid_Pen, Label_Font, Label_Brush );
          break;
      }
    }

    protected void Draw_Combined_Absolute( Graphics G, int W, int H, int Chart_W, int Chart_H,
        DateTime Time_Min, double Time_Range_Sec,
        Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      double Global_Min = double.MaxValue, Global_Max = double.MinValue;
      foreach (var S in _Series.Where( s => s.Visible && s.Points.Count > 0 ))
      {
        var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
            S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if (Vc == 0)
          continue;
        for (int i = Si; i < Si + Vc; i++)
        {
          if (S.Points[ i ].Value < Global_Min)
            Global_Min = S.Points[ i ].Value;
          if (S.Points[ i ].Value > Global_Max)
            Global_Max = S.Points[ i ].Value;
        }
      }
      if (Global_Min == double.MaxValue)
        return;

      double Range = Global_Max - Global_Min;
      double Padding = Math.Max( Range * 0.5, 0.0001 );
      double Pmin = Global_Min - Padding, Pmax = Global_Max + Padding, Pr = Pmax - Pmin;
      if (Pr == 0)
        Pr = 0.001;

      double Display_Min = Pmin, Display_Max = Pmax, Display_Range = Pr;
      if (_Zoom_Factor > 0 && _Zoom_Factor != 1.0)
      {
        double Center = (Pmax + Pmin) / 2.0;
        double Zr = Pr / _Zoom_Factor;
        Display_Min = Center - Zr / 2.0;
        Display_Max = Center + Zr / 2.0;
        Display_Range = Zr;
      }

      Draw_Y_Axis( G, Display_Min, Display_Range, W, H, Chart_H, Grid_Pen, Label_Font, Label_Brush );

      int Color_Index = 0;
      foreach (var S in _Series)
      {
        if (!S.Visible || S.Points.Count == 0)
        {
          Color_Index++;
          continue;
        }
        Color LC = _Theme.Line_Colors[ Color_Index % _Theme.Line_Colors.Length ];

        var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
            S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if (Vc == 0)
        {
          Color_Index++;
          continue;
        }

        DateTime Vtmin = S.Points[ Si ].Time;
        DateTime Vtmax = S.Points[ Si + Vc - 1 ].Time;
        double Vtr = Math.Max( (Vtmax - Vtmin).TotalSeconds, 0.001 );

        var Pts = new List<PointF>();
        for (int i = 0; i < Vc; i++)
        {
          var P = S.Points[ Si + i ];
          float X = _Chart_Margin_Left + (float) ((P.Time - Vtmin).TotalSeconds / Vtr * Chart_W);
          float Y = H - _Chart_Margin_Bottom - (float) ((P.Value - Display_Min) / Display_Range * Chart_H);
          Pts.Add( new PointF( X, Y ) );
        }

        if (Pts.Count > 1)
        {
          using var Pen = new Pen( LC, 2f );
          G.DrawLines( Pen, Pts.ToArray() );
        }
        foreach (var Pt in Pts)
        {
          using var Br = new SolidBrush( LC );
          G.FillEllipse( Br, Pt.X - 2, Pt.Y - 2, 4, 4 );
        }
        if (Pts.Count > 0)
        {
          var Last = Pts[ Pts.Count - 1 ];
          using var Br = new SolidBrush( LC );
          G.FillEllipse( Br, Last.X - 4, Last.Y - 4, 8, 8 );
        }

        Color_Index++;
      }
    }

    protected void Draw_Combined_Normalized( Graphics G, int W, int H,
        DateTime Time_Min, double Time_Range_Sec,
        Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;

      Draw_Y_Axis_Percentage( G, W, H, Grid_Pen, Label_Font, Label_Brush );

      int Color_Index = 0;
      foreach (var S in _Series)
      {
        if (!S.Visible || S.Points.Count == 0)
        {
          Color_Index++;
          continue;
        }
        var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
            S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if (Vc == 0)
        {
          Color_Index++;
          continue;
        }

        double Series_Min = double.MaxValue, Series_Max = double.MinValue;
        for (int i = Si; i < Si + Vc; i++)
        {
          if (S.Points[ i ].Value < Series_Min)
            Series_Min = S.Points[ i ].Value;
          if (S.Points[ i ].Value > Series_Max)
            Series_Max = S.Points[ i ].Value;
        }
        double Series_Range = Series_Max - Series_Min;

        Color LC = _Theme.Line_Colors[ Color_Index % _Theme.Line_Colors.Length ];
        using var Line_Pen = new Pen( LC, 1.5f );
        var Pts = new List<PointF>( Vc );

        for (int I = Si; I < Si + Vc; I++)
        {
          var Pt = S.Points[ I ];
          float X = _Chart_Margin_Left + (float) ((Pt.Time - Time_Min).TotalSeconds / Time_Range_Sec * Chart_W);
          double Norm = Series_Range > 0 ? (Pt.Value - Series_Min) / Series_Range : 0.5;
          float Y = H - _Chart_Margin_Bottom - (float) (Norm * Chart_H);
          Pts.Add( new PointF( X, Y ) );
        }
        if (Pts.Count > 1)
          G.DrawLines( Line_Pen, Pts.ToArray() );

        using var Value_Brush = new SolidBrush( Color.FromArgb( 180, LC ) );

        string Top_Label = Format_Sig_Figs( Series_Max, S.Display_Digits );
        float Top_Y = H - _Chart_Margin_Bottom - Chart_H;
        G.DrawString( Top_Label, Label_Font, Value_Brush, _Chart_Margin_Left + 4, Top_Y + Color_Index * 12 );

        string Bot_Label = Format_Sig_Figs( Series_Min, S.Display_Digits );
        float Bot_Y = H - _Chart_Margin_Bottom - 12;
        G.DrawString( Bot_Label, Label_Font, Value_Brush, _Chart_Margin_Left + 4, Bot_Y - Color_Index * 12 );

        Color_Index++;
      }
    }

    protected void Draw_Combined_Scatter( Graphics G, int W, int H,
        DateTime Time_Min, double Time_Range_Sec,
        Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;

      double Global_Min = double.MaxValue, Global_Max = double.MinValue;
      foreach (var S in _Series.Where( s => s.Visible && s.Points.Count > 0 ))
      {
        var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
            S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if (Vc == 0)
          continue;
        for (int i = Si; i < Si + Vc; i++)
        {
          if (S.Points[ i ].Value < Global_Min)
            Global_Min = S.Points[ i ].Value;
          if (S.Points[ i ].Value > Global_Max)
            Global_Max = S.Points[ i ].Value;
        }
      }
      if (Global_Min == double.MaxValue)
        return;

      double Range = Global_Max - Global_Min, Padding = Math.Max( Range * 0.5, 0.0001 );
      double Dmin = Global_Min - Padding, Dmax = Global_Max + Padding, Dr = Dmax - Dmin;
      if (Dr == 0)
        Dr = 0.001;

      Draw_Y_Axis( G, Dmin, Dr, W, H, Chart_H, Grid_Pen, Label_Font, Label_Brush );

      int Color_Index = 0;
      foreach (var S in _Series)
      {
        if (!S.Visible || S.Points.Count == 0)
        {
          Color_Index++;
          continue;
        }
        Color LC = _Theme.Line_Colors[ Color_Index % _Theme.Line_Colors.Length ];
        var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
            S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if (Vc == 0)
        {
          Color_Index++;
          continue;
        }

        DateTime Vtmin = S.Points[ Si ].Time;
        DateTime Vtmax = S.Points[ Si + Vc - 1 ].Time;
        double Vtr = Math.Max( (Vtmax - Vtmin).TotalSeconds, 0.001 );
        float Dot_Size = Vc > 100 ? 5f : Vc > 50 ? 7f : 9f;

        using var Dot_Brush = new SolidBrush( LC );
        using var Ring_Pen = new Pen( LC, 1.5f );

        for (int I = 0; I < Vc; I++)
        {
          var P = S.Points[ Si + I ];
          float X = _Chart_Margin_Left + (float) ((P.Time - Vtmin).TotalSeconds / Vtr * Chart_W);
          float Y = H - _Chart_Margin_Bottom - (float) ((P.Value - Dmin) / Dr * Chart_H);
          G.FillEllipse( Dot_Brush, X - Dot_Size / 2, Y - Dot_Size / 2, Dot_Size, Dot_Size );
          G.DrawEllipse( Ring_Pen, X - Dot_Size / 2 - 1, Y - Dot_Size / 2 - 1, Dot_Size + 2, Dot_Size + 2 );
        }
        Color_Index++;
      }
    }

    protected void Draw_Combined_Step( Graphics G, int W, int H,
        DateTime Time_Min, double Time_Range_Sec,
        Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;

      double Global_Min = double.MaxValue, Global_Max = double.MinValue;
      foreach (var S in _Series.Where( s => s.Visible && s.Points.Count > 0 ))
      {
        var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
            S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if (Vc == 0)
          continue;
        for (int i = Si; i < Si + Vc; i++)
        {
          if (S.Points[ i ].Value < Global_Min)
            Global_Min = S.Points[ i ].Value;
          if (S.Points[ i ].Value > Global_Max)
            Global_Max = S.Points[ i ].Value;
        }
      }
      if (Global_Min == double.MaxValue)
        return;

      double Range = Global_Max - Global_Min, Padding = Math.Max( Range * 0.5, 0.0001 );
      double Dmin = Global_Min - Padding, Dmax = Global_Max + Padding, Dr = Dmax - Dmin;
      if (Dr == 0)
        Dr = 0.001;

      Draw_Y_Axis( G, Dmin, Dr, W, H, Chart_H, Grid_Pen, Label_Font, Label_Brush );

      int Color_Index = 0;
      foreach (var S in _Series)
      {
        if (!S.Visible || S.Points.Count == 0)
        {
          Color_Index++;
          continue;
        }
        Color LC = _Theme.Line_Colors[ Color_Index % _Theme.Line_Colors.Length ];
        var (Si, Vc) = Multimeter_Common_Helpers_Class.Get_Visible_Range(
            S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if (Vc == 0)
        {
          Color_Index++;
          continue;
        }

        DateTime Vtmin = S.Points[ Si ].Time;
        DateTime Vtmax = S.Points[ Si + Vc - 1 ].Time;
        double Vtr = Math.Max( (Vtmax - Vtmin).TotalSeconds, 0.001 );

        var Step_Points = new List<PointF>();
        for (int I = 0; I < Vc; I++)
        {
          var P = S.Points[ Si + I ];
          float X = _Chart_Margin_Left + (float) ((P.Time - Vtmin).TotalSeconds / Vtr * Chart_W);
          float Y = H - _Chart_Margin_Bottom - (float) ((P.Value - Dmin) / Dr * Chart_H);
          if (I > 0)
            Step_Points.Add( new PointF( X, Step_Points[ Step_Points.Count - 1 ].Y ) );
          Step_Points.Add( new PointF( X, Y ) );
        }
        if (Step_Points.Count > 1)
        {
          using var Pen = new Pen( LC, 1.5f );
          G.DrawLines( Pen, Step_Points.ToArray() );
        }

        Color_Index++;
      }
    }


    // ════════════════════════════════════════════════════════════════════
    // DRAW — HISTOGRAM AND PIE
    // ════════════════════════════════════════════════════════════════════

    protected void Draw_Subplot_Histogram( Graphics G, Instrument_Series S,
        int W, int Sub_Bottom, int Label_Top,
        double Display_Min, double Display_Range )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Count = S.Points.Count;
      if (Count == 0)
        return;

      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Plot_H = Sub_Bottom - Label_Top;

      List<double> Values = S.Points.Select( P => P.Value ).ToList();
      double Min_V = Values.Min(), Max_V = Values.Max(), Range = Max_V - Min_V;
      int Num_Bins = Get_Bin_Count( Count );

      if (Range < 1e-12)
      {
        Range = Math.Abs( Max_V ) * 0.1;
        if (Range < 1e-12)
          Range = 1.0;
        Min_V -= Range / 2;
        Max_V += Range / 2;
        Range = Max_V - Min_V;
      }

      double Bin_Width = Range / Num_Bins;
      int[] Bin_Counts = new int[ Num_Bins ];
      foreach (double V in Values)
        Bin_Counts[ Math.Clamp( (int) ((V - Min_V) / Bin_Width), 0, Num_Bins - 1 ) ]++;

      int Max_Count = Math.Max( 1, Bin_Counts.Max() );
      double Y_Max = Max_Count * 1.1;

      using var Grid_Pen = new Pen( _Theme.Grid, 1f );
      using var Label_Font = new Font( "Consolas", 7.5F );
      using var Label_Brush = new SolidBrush( _Theme.Labels );

      for (int I = 0; I <= 5; I++)
      {
        double Fraction = (double) I / 5;
        int Y_Pos = Sub_Bottom - (int) (Fraction * Plot_H);
        G.DrawLine( Grid_Pen, _Chart_Margin_Left, Y_Pos, _Chart_Margin_Left + Chart_W, Y_Pos );
        string Label = ((int) Math.Round( Fraction * Y_Max )).ToString();
        var Lsz = G.MeasureString( Label, Label_Font );
        G.DrawString( Label, Label_Font, Label_Brush,
            _Chart_Margin_Left - Lsz.Width - 6, Y_Pos - Lsz.Height / 2 );
      }

      float Bar_Spacing = Chart_W / (float) Num_Bins;
      float Bar_W = Bar_Spacing * 0.8f;
      float Gap = Bar_Spacing * 0.1f;

      using var Bar_Brush = new SolidBrush( S.Line_Color );
      using var Bar_Bord_Pen = new Pen( Color.FromArgb(
          (int) (S.Line_Color.R * 0.7), (int) (S.Line_Color.G * 0.7),
          (int) (S.Line_Color.B * 0.7) ), 1f );

      for (int I = 0; I < Num_Bins; I++)
      {
        float Bar_H = (float) ((Bin_Counts[ I ] / Y_Max) * Plot_H);
        if (Bar_H < 1f && Bin_Counts[ I ] > 0)
          Bar_H = 1f;
        if (Bar_H < 1f)
          continue;

        float X = _Chart_Margin_Left + I * Bar_Spacing + Gap;
        float Y = Sub_Bottom - Bar_H;
        RectangleF Rect = new( X, Y, Bar_W, Bar_H );
        G.FillRectangle( Bar_Brush, Rect );
        G.DrawRectangle( Bar_Bord_Pen, Rect.X, Rect.Y, Rect.Width, Rect.Height );

        if (Bin_Counts[ I ] > 0)
        {
          string Freq_Text = Bin_Counts[ I ].ToString();
          var Fsz = G.MeasureString( Freq_Text, Label_Font );
          G.DrawString( Freq_Text, Label_Font, Label_Brush,
              X + Bar_W / 2 - Fsz.Width / 2, Y - Fsz.Height - 2 );
        }
      }

      double Mean = Values.Average();
      double Sum_Sq = Values.Sum( V => (V - Mean) * (V - Mean) );
      double Std_Dev = Math.Sqrt( Sum_Sq / Count );

      if (Std_Dev > 1e-15)
      {
        using var Curve_Pen = new Pen( _Theme.Line_Colors[ 1 % _Theme.Line_Colors.Length ], 2.5f );
        PointF[] Curve_Pts = new PointF[ 100 ];
        double Scale = Bin_Width * Count;

        for (int I = 0; I < 100; I++)
        {
          double X_Val = Min_V + (I / 99.0) * Range;
          double Z = (X_Val - Mean) / Std_Dev;
          double PDF = (1.0 / (Std_Dev * Math.Sqrt( 2.0 * Math.PI ))) * Math.Exp( -0.5 * Z * Z );
          Curve_Pts[ I ] = new PointF(
              _Chart_Margin_Left + (float) ((X_Val - Min_V) / Range * Chart_W),
              Sub_Bottom - (float) ((PDF * Scale / Y_Max) * Plot_H) );
        }
        G.DrawLines( Curve_Pen, Curve_Pts );

        Color Mean_Color = _Theme.Line_Colors[ 2 % _Theme.Line_Colors.Length ];
        using var Mean_Pen = new Pen( Mean_Color, 2f ) { DashStyle = DashStyle.Dash };
        float Mean_X = _Chart_Margin_Left + (float) ((Mean - Min_V) / Range * Chart_W);
        G.DrawLine( Mean_Pen, Mean_X, Label_Top, Mean_X, Sub_Bottom );

        using var Sigma_Pen = new Pen( Color.FromArgb( 120, Mean_Color ), 1.5f ) { DashStyle = DashStyle.Dot };
        using var Sigma_Font = new Font( "Consolas", 7F );
        using var Sigma_Brush = new SolidBrush( Mean_Color );
        G.DrawString( "\u03bc", Sigma_Font, Sigma_Brush, Mean_X + 3, Label_Top + 2 );

        double[] Sigmas = { -2, -1, 1, 2 };
        string[] Sigma_Labels = { "-2\u03c3", "-1\u03c3", "+1\u03c3", "+2\u03c3" };
        for (int I = 0; I < Sigmas.Length; I++)
        {
          double Sv = Mean + Sigmas[ I ] * Std_Dev;
          if (Sv < Min_V || Sv > Max_V)
            continue;
          float Sx = _Chart_Margin_Left + (float) ((Sv - Min_V) / Range * Chart_W);
          G.DrawLine( Sigma_Pen, Sx, Label_Top, Sx, Sub_Bottom );
          G.DrawString( Sigma_Labels[ I ], Sigma_Font, Sigma_Brush, Sx + 3, Label_Top + 2 );
        }
      }

      using var X_Font = new Font( "Consolas", 6.5F );
      int Label_Step = Math.Max( 1, Num_Bins / 8 );
      for (int I = 0; I < Num_Bins; I += Label_Step)
      {
        double Bin_Center = Min_V + (I + 0.5) * Bin_Width;
        string X_Label = Multimeter_Common_Helpers_Class.Format_Value(
            Bin_Center, Current_Unit, S.Type, S.Display_Digits );
        var Xsz = G.MeasureString( X_Label, X_Font );
        float X_Pos = _Chart_Margin_Left + I * Bar_Spacing + Bar_Spacing / 2;
        G.DrawString( X_Label, X_Font, Label_Brush, X_Pos - Xsz.Width / 2, Sub_Bottom + 4 );
      }

      using var Title_Font = new Font( "Segoe UI", 7.5F );
      string Title = $"Distribution  ({Count} readings)";
      var Title_Size = G.MeasureString( Title, Title_Font );
      G.DrawString( Title, Title_Font, Label_Brush,
          _Chart_Margin_Left + Chart_W / 2 - Title_Size.Width / 2, Sub_Bottom + 18 );
    }

    protected void Draw_Single_Histogram( Graphics G, int W, int H, int Chart_W, int Chart_H )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      var Saved = _Readings;
      _Readings = Get_Single_Series_Readings();
      Draw_Histogram( G, W, H );
      _Readings = Saved;
    }

    protected void Draw_Single_Pie( Graphics G, int W, int H, int Chart_W, int Chart_H )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      var Saved = _Readings;
      _Readings = Get_Single_Series_Readings();
      Draw_Pie_Chart( G, W, H );
      _Readings = Saved;
    }

    // NOTE: Draw_Histogram and Draw_Pie_Chart use _Readings (the temporary
    // snapshot) and refer to _Series[0] for type/digit info.
    // Both methods are long but identical — included here in full.

    protected void Draw_Histogram( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;

      int Count = _Readings.Count;
      if (Count == 0)
        return;

      double Min_V = _Readings.Min(), Max_V = _Readings.Max(), Range = Max_V - Min_V;
      int Num_Bins = Get_Bin_Count( Count );

      if (Range < 1e-12)
      {
        Range = Math.Abs( Max_V ) * 0.1;
        if (Range < 1e-12)
          Range = 1.0;
        Min_V -= Range / 2;
        Max_V += Range / 2;
        Range = Max_V - Min_V;
      }

      double Bin_Width = Range / Num_Bins;
      int[] Bin_Counts = new int[ Num_Bins ];
      foreach (double V in _Readings)
        Bin_Counts[ Math.Clamp( (int) ((V - Min_V) / Bin_Width), 0, Num_Bins - 1 ) ]++;

      int Max_Count = Math.Max( 1, Bin_Counts.Max() );
      double Y_Max = Max_Count * 1.1;
      float Baseline = H - _Chart_Margin_Bottom;

      using var Grid_Pen = new Pen( _Theme.Grid, 1f );
      using var Label_Font = new Font( "Consolas", 7.5F );
      using var Label_Brush = new SolidBrush( _Theme.Labels );

      for (int I = 0; I <= 5; I++)
      {
        double Fraction = (double) I / 5;
        int Y_Pos = H - _Chart_Margin_Bottom - (int) (Fraction * Chart_H);
        G.DrawLine( Grid_Pen, _Chart_Margin_Left, Y_Pos, W - _Chart_Margin_Right, Y_Pos );
        string Label = ((int) Math.Round( Fraction * Y_Max )).ToString();
        var Lsz = G.MeasureString( Label, Label_Font );
        G.DrawString( Label, Label_Font, Label_Brush,
            _Chart_Margin_Left - Lsz.Width - 6, Y_Pos - Lsz.Height / 2 );
      }

      float Bar_Spacing = Chart_W / (float) Num_Bins;
      float Bar_W = Bar_Spacing * 0.8f;
      float Gap = Bar_Spacing * 0.1f;
      Color Bar_Color = _Theme.Line_Colors[ 0 ];

      using var Bar_Brush = new SolidBrush( Bar_Color );
      using var Bar_Bord_Pen = new Pen( Color.FromArgb(
          (int) (Bar_Color.R * 0.7), (int) (Bar_Color.G * 0.7),
          (int) (Bar_Color.B * 0.7) ), 1f );

      for (int I = 0; I < Num_Bins; I++)
      {
        float Bar_H = (float) ((Bin_Counts[ I ] / Y_Max) * Chart_H);
        if (Bar_H < 1 && Bin_Counts[ I ] > 0)
          Bar_H = 1;
        float X = _Chart_Margin_Left + I * Bar_Spacing + Gap;
        RectangleF Rect = new( X, Baseline - Bar_H, Bar_W, Bar_H );
        G.FillRectangle( Bar_Brush, Rect );
        G.DrawRectangle( Bar_Bord_Pen, Rect.X, Rect.Y, Rect.Width, Rect.Height );

        if (Bin_Counts[ I ] > 0)
        {
          string Freq_Text = Bin_Counts[ I ].ToString();
          var Fsz = G.MeasureString( Freq_Text, Label_Font );
          G.DrawString( Freq_Text, Label_Font, Label_Brush,
              X + Bar_W / 2 - Fsz.Width / 2, Baseline - Bar_H - Fsz.Height - 2 );
        }
      }

      double Mean = _Readings.Average();
      double Sum_Sq = 0;
      foreach (double V in _Readings)
      {
        double D = V - Mean;
        Sum_Sq += D * D;
      }
      double Std_Dev = Math.Sqrt( Sum_Sq / Count );

      if (Std_Dev > 1e-15)
      {
        using var Curve_Pen = new Pen( _Theme.Line_Colors[ 1 ], 2.5f );
        PointF[] Curve_Pts = new PointF[ 100 ];
        double Scale = Bin_Width * Count;

        for (int I = 0; I < 100; I++)
        {
          double X_Val = Min_V + (I / 99.0) * Range;
          double Z = (X_Val - Mean) / Std_Dev;
          double PDF = (1.0 / (Std_Dev * Math.Sqrt( 2.0 * Math.PI ))) * Math.Exp( -0.5 * Z * Z );
          Curve_Pts[ I ] = new PointF(
              _Chart_Margin_Left + (float) ((X_Val - Min_V) / Range * Chart_W),
              Baseline - (float) ((PDF * Scale / Y_Max) * Chart_H) );
        }
        G.DrawLines( Curve_Pen, Curve_Pts );

        Color Mean_Color = _Theme.Line_Colors[ 2 ];
        using var Mean_Pen = new Pen( Mean_Color, 2f ) { DashStyle = DashStyle.Dash };
        float Mean_X = _Chart_Margin_Left + (float) ((Mean - Min_V) / Range * Chart_W);
        G.DrawLine( Mean_Pen, Mean_X, _Chart_Margin_Top, Mean_X, Baseline );

        using var Sigma_Pen = new Pen( Color.FromArgb( 120, Mean_Color ), 1.5f ) { DashStyle = DashStyle.Dot };
        using var Sigma_Font = new Font( "Consolas", 7F );
        using var Sigma_Brush = new SolidBrush( Mean_Color );
        G.DrawString( "\u03bc", Sigma_Font, Sigma_Brush, Mean_X + 3, _Chart_Margin_Top + 2 );

        double[] Sigmas = { -2, -1, 1, 2 };
        string[] Sigma_Labels = { "-2\u03c3", "-1\u03c3", "+1\u03c3", "+2\u03c3" };
        for (int I = 0; I < Sigmas.Length; I++)
        {
          double Sv = Mean + Sigmas[ I ] * Std_Dev;
          if (Sv < Min_V || Sv > Max_V)
            continue;
          float Sx = _Chart_Margin_Left + (float) ((Sv - Min_V) / Range * Chart_W);
          G.DrawLine( Sigma_Pen, Sx, _Chart_Margin_Top, Sx, Baseline );
          G.DrawString( Sigma_Labels[ I ], Sigma_Font, Sigma_Brush, Sx + 3, _Chart_Margin_Top + 2 );
        }
      }

      var S0 = _Series.Count > 0 ? _Series[ 0 ] : null;
      using var X_Font = new Font( "Consolas", 6.5F );
      int Label_Step = Math.Max( 1, Num_Bins / 8 );
      for (int I = 0; I < Num_Bins; I += Label_Step)
      {
        double Bin_Center = Min_V + (I + 0.5) * Bin_Width;
        string X_Label = Multimeter_Common_Helpers_Class.Format_Value(
            Bin_Center, Current_Unit,
            S0?.Type ?? _Selected_Meter, S0?.Display_Digits ?? 6 );
        var Xsz = G.MeasureString( X_Label, X_Font );
        float X_Pos = _Chart_Margin_Left + I * Bar_Spacing + Bar_Spacing / 2;
        G.DrawString( X_Label, X_Font, Label_Brush, X_Pos - Xsz.Width / 2, H - _Chart_Margin_Bottom + 4 );
      }

      using var Title_Font = new Font( "Segoe UI", 7.5F );
      string Title = $"Distribution  ({Count} readings)";
      var Title_Size = G.MeasureString( Title, Title_Font );
      G.DrawString( Title, Title_Font, Label_Brush,
          _Chart_Margin_Left + Chart_W / 2 - Title_Size.Width / 2, H - _Chart_Margin_Bottom + 18 );
    }

    protected void Draw_Pie_Chart( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;

      int Count = _Readings.Count;
      if (Count == 0)
        return;

      double Min_V = _Readings.Min(), Max_V = _Readings.Max(), Range = Max_V - Min_V;
      int Num_Bins = Get_Bin_Count( Count );

      if (Range < 1e-12)
      {
        Range = Math.Abs( Max_V ) * 0.1;
        if (Range < 1e-12)
          Range = 1.0;
        Min_V -= Range / 2;
        Max_V += Range / 2;
        Range = Max_V - Min_V;
      }

      double Bin_Width = Range / Num_Bins;
      int[] Bin_Counts = new int[ Num_Bins ];
      foreach (double V in _Readings)
        Bin_Counts[ Math.Clamp( (int) ((V - Min_V) / Bin_Width), 0, Num_Bins - 1 ) ]++;

      var S0 = _Series.Count > 0 ? _Series[ 0 ] : null;

      int Legend_W = 0;
      using (var Temp_Font = new Font( "Consolas", 7.5F ))
      using (var Temp_G = Graphics.FromHwnd( IntPtr.Zero ))
      {
        for (int I = 0; I < Num_Bins; I++)
        {
          if (Bin_Counts[ I ] == 0)
            continue;
          double Bin_Low = Min_V + I * Bin_Width, Bin_High = Bin_Low + Bin_Width;
          double Pct = 100.0 * Bin_Counts[ I ] / Count;
          string Entry = $"{Multimeter_Common_Helpers_Class.Format_Value( Bin_Low, Current_Unit, S0?.Type ?? _Selected_Meter, S0?.Display_Digits ?? 6 )}"
                         + $" - {Multimeter_Common_Helpers_Class.Format_Value( Bin_High, Current_Unit, S0?.Type ?? _Selected_Meter, S0?.Display_Digits ?? 6 )}"
                         + $"  ({Pct:F1}%)";
          int Entry_W = (int) Temp_G.MeasureString( Entry, Temp_Font ).Width;
          if (Entry_W > Legend_W)
            Legend_W = Entry_W;
        }
      }
      Legend_W += 40;
      int Pie_Area_W = Chart_W - Legend_W;
      if (Pie_Area_W < 100)
      {
        Pie_Area_W = Chart_W;
        Legend_W = 0;
      }

      int Diameter = Math.Max( 40, Math.Min( Pie_Area_W, Chart_H ) - 20 );
      int Pie_X = _Chart_Margin_Left + (Pie_Area_W - Diameter) / 2;
      int Pie_Y = _Chart_Margin_Top + (Chart_H - Diameter) / 2;
      Rectangle Pie_Rect = new( Pie_X, Pie_Y, Diameter, Diameter );

      float Start_Angle = -90f;
      using var Outline_Pen = new Pen( _Theme.Background, 2f );

      for (int I = 0; I < Num_Bins; I++)
      {
        if (Bin_Counts[ I ] == 0)
          continue;
        float Sweep = 360f * Bin_Counts[ I ] / Count;
        using var Slice_Brush = new SolidBrush( Get_Bin_Color( I ) );
        G.FillPie( Slice_Brush, Pie_Rect, Start_Angle, Sweep );
        G.DrawPie( Outline_Pen, Pie_Rect, Start_Angle, Sweep );
        Start_Angle += Sweep;
      }

      if (Legend_W > 0)
      {
        using var Legend_Font = new Font( "Consolas", 7.5F );
        using var Label_Brush = new SolidBrush( _Theme.Labels );
        using var Title_Font = new Font( "Segoe UI", 8F, FontStyle.Bold );

        int Leg_X = _Chart_Margin_Left + Pie_Area_W + 10;
        int Leg_Y = _Chart_Margin_Top + 10, Row_H = 20;
        G.DrawString( "Distribution", Title_Font, Label_Brush, Leg_X, Leg_Y );
        Leg_Y += Row_H + 4;

        for (int I = 0; I < Num_Bins; I++)
        {
          if (Bin_Counts[ I ] == 0)
            continue;
          if (Leg_Y + Row_H > H - _Chart_Margin_Bottom)
            break;
          using var Swatch = new SolidBrush( Get_Bin_Color( I ) );
          G.FillRectangle( Swatch, Leg_X, Leg_Y + 2, 12, 12 );

          double Bin_Low = Min_V + I * Bin_Width, Bin_High = Bin_Low + Bin_Width;
          double Pct = 100.0 * Bin_Counts[ I ] / Count;
          string Entry = $"{Multimeter_Common_Helpers_Class.Format_Value( Bin_Low, Current_Unit, S0?.Type ?? _Selected_Meter, S0?.Display_Digits ?? 6 )}"
                         + $" - {Multimeter_Common_Helpers_Class.Format_Value( Bin_High, Current_Unit, S0?.Type ?? _Selected_Meter, S0?.Display_Digits ?? 6 )}"
                         + $"  ({Pct:F1}%)";
          G.DrawString( Entry, Legend_Font, Label_Brush, Leg_X + 18, Leg_Y );
          Leg_Y += Row_H;
        }
      }
    }


    // ════════════════════════════════════════════════════════════════════
    // DRAW — TIMING CHART
    // ════════════════════════════════════════════════════════════════════

    protected void Draw_Poll_Timing_Chart( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;

      G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

      if (_Timing_Count < 2 || Chart_W < 20 || Chart_H < 20)
      {
        Draw_Empty_State( G, W, H, "No timing data. Load a _Timing.csv file first." );
        return;
      }

      int Total_Samples = Math.Min( _Timing_Count, _Timing_Buffer_Size );
      int Sample_Count = Math.Min( Total_Samples, _Max_Display_Points );
      int Start = (_Timing_Head - Sample_Count - _Timing_View_Offset + _Timing_Buffer_Size * 2) % _Timing_Buffer_Size;

      double Max_Ms = 0, Min_Ms = double.MaxValue;
      for (int i = 0; i < Sample_Count; i++)
      {
        double D = _Cycle_Timing[ (Start + i) % _Timing_Buffer_Size ].Total_Ms;
        if (D > Max_Ms)
          Max_Ms = D;
        if (D < Min_Ms)
          Min_Ms = D;
      }
      double Ms_Range = Math.Max( Max_Ms - Min_Ms, 20.0 );
      double Pad = Ms_Range * 0.15;
      double Y_Min = Math.Max( 0, Min_Ms - Pad );
      double Y_Max = Max_Ms + Pad;
      double Y_Range = Y_Max - Y_Min;

      using var Grid_Pen = new Pen( _Theme.Grid, 1f );
      using var Label_Font = new Font( "Consolas", 7.5f );
      using var Label_Brush = new SolidBrush( _Theme.Labels );

      for (int i = 0; i <= 5; i++)
      {
        double Frac = (double) i / 5;
        int Y = H - _Chart_Margin_Bottom - (int) (Frac * Chart_H);
        double Val = Y_Min + Frac * Y_Range;
        G.DrawLine( Grid_Pen, _Chart_Margin_Left, Y, W - _Chart_Margin_Right, Y );
        string Lbl = $"{Val:F0} ms";
        var Sz = G.MeasureString( Lbl, Label_Font );
        G.DrawString( Lbl, Label_Font, Label_Brush,
            _Chart_Margin_Left - Sz.Width - 4, Y - Sz.Height / 2 );
      }

      bool Has_Phases = _Cycle_Timing[ Start ].Comm_Ms > 0;
      if (Has_Phases)
      {
        Draw_Phase_Band( G, Start, Sample_Count, Chart_W, H,
            s => s.Address_Switch_Ms,
            Y_Min, Y_Range, Color.FromArgb( 70, Color.Gold ), "Addr" );
        Draw_Phase_Band( G, Start, Sample_Count, Chart_W, H,
            s => s.Address_Switch_Ms + s.Comm_Ms,
            Y_Min, Y_Range, Color.FromArgb( 70, Color.DodgerBlue ), "Comm" );
        Draw_Phase_Band( G, Start, Sample_Count, Chart_W, H,
            s => s.Address_Switch_Ms + s.Comm_Ms + s.UI_Ms,
            Y_Min, Y_Range, Color.FromArgb( 70, Color.LimeGreen ), "UI" );
        Draw_Phase_Band( G, Start, Sample_Count, Chart_W, H,
            s => s.Address_Switch_Ms + s.Comm_Ms + s.UI_Ms + s.Record_Ms,
            Y_Min, Y_Range, Color.FromArgb( 70, Color.Tomato ), "Rec" );
      }

      var Line_Pts = new PointF[ Sample_Count ];
      for (int i = 0; i < Sample_Count; i++)
      {
        var Samp = _Cycle_Timing[ (Start + i) % _Timing_Buffer_Size ];
        float X = _Chart_Margin_Left + (float) i / (Sample_Count - 1) * Chart_W;
        float Y = H - _Chart_Margin_Bottom - (float) ((Samp.Total_Ms - Y_Min) / Y_Range * Chart_H);
        Line_Pts[ i ] = new PointF( X, Y );
      }

      var Fill_Pts = new PointF[ Sample_Count + 2 ];
      Array.Copy( Line_Pts, Fill_Pts, Sample_Count );
      Fill_Pts[ Sample_Count ] = new PointF( Line_Pts[ Sample_Count - 1 ].X, H - _Chart_Margin_Bottom );
      Fill_Pts[ Sample_Count + 1 ] = new PointF( Line_Pts[ 0 ].X, H - _Chart_Margin_Bottom );
      using var Fill_Brush = new LinearGradientBrush(
          new PointF( 0, _Chart_Margin_Top ), new PointF( 0, H - _Chart_Margin_Bottom ),
          Color.FromArgb( 30, Color.White ), Color.FromArgb( 5, Color.White ) );
      G.FillPolygon( Fill_Brush, Fill_Pts );

      using var Line_Pen = new Pen( Color.White, 2f );
      G.DrawLines( Line_Pen, Line_Pts );

      long First_Cycle = _Timing_Count - Sample_Count + 1;
      using var Disc_Pen = new Pen( Color.OrangeRed, 1.5f ) { DashStyle = DashStyle.Dash };
      using var Disc_Font = new Font( "Segoe UI", 7.5f );
      using var Disc_Brush = new SolidBrush( Color.OrangeRed );

      lock (_Disconnect_Events)
      {
        foreach (var Evt in _Disconnect_Events)
        {
          long Offset = Evt.Cycle_Number - First_Cycle;
          if (Offset < 0 || Offset >= Sample_Count)
            continue;
          float Disc_X = _Chart_Margin_Left + (float) Offset / (Sample_Count - 1) * Chart_W;
          G.DrawLine( Disc_Pen, Disc_X, _Chart_Margin_Top, Disc_X, H - _Chart_Margin_Bottom );
          string Tag = Evt.Instrument_Name.Length > 8 ? Evt.Instrument_Name[ ..8 ] : Evt.Instrument_Name;
          var Tag_Sz = G.MeasureString( Tag, Disc_Font );
          float Tag_X = (Disc_X + Tag_Sz.Width + 4 > W - _Chart_Margin_Right)
              ? Disc_X - Tag_Sz.Width - 2 : Disc_X + 2;
          G.DrawString( Tag, Disc_Font, Disc_Brush, Tag_X, _Chart_Margin_Top + 2 );
        }
      }

      int Num_X = Math.Min( 8, Chart_W / 80 );
      for (int i = 0; i <= Num_X; i++)
      {
        double Frac = (double) i / Num_X;
        int X_Pos = _Chart_Margin_Left + (int) (Frac * Chart_W);
        int S_Idx = (int) (Frac * (Sample_Count - 1));
        var Samp = _Cycle_Timing[ (Start + S_Idx) % _Timing_Buffer_Size ];
        string X_Lbl = Samp.Cycle_Time.ToString( "HH:mm:ss" );
        var X_Sz = G.MeasureString( X_Lbl, Label_Font );
        G.DrawString( X_Lbl, Label_Font, Label_Brush,
            X_Pos - X_Sz.Width / 2, H - _Chart_Margin_Bottom + 6 );
        G.DrawLine( Grid_Pen, X_Pos, _Chart_Margin_Top, X_Pos, H - _Chart_Margin_Bottom );
      }

      if (Has_Phases)
      {
        using var Leg_Font = new Font( "Segoe UI", 7.5f );
        (string Label, Color C)[] Legend_Items =
        {
                    ("Addr Switch", Color.FromArgb(70, Color.Gold)),
                    ("Comm",        Color.FromArgb(70, Color.FromArgb(0, 100, 200))),
                    ("UI Update",   Color.FromArgb(70, Color.LimeGreen)),
                    ("Record",      Color.FromArgb(70, Color.Tomato)),
                    ("Total",       Color.FromArgb(70, Color.DeepSkyBlue)),
                };

        float Max_Col1_W = 0;
        for (int I = 0; I < Legend_Items.Length; I += 2)
        {
          float Lw = G.MeasureString( Legend_Items[ I ].Label, Leg_Font ).Width;
          if (Lw > Max_Col1_W)
            Max_Col1_W = Lw;
        }

        int Col1_X = _Chart_Margin_Left + 10;
        int Col2_X = Col1_X + 12 + 4 + (int) Max_Col1_W + 12;
        int Leg_Y = _Chart_Margin_Top + 10, Row_H = 20;

        for (int I = 0; I < Legend_Items.Length; I++)
        {
          var (Label, C) = Legend_Items[ I ];
          int Leg_X = (I % 2 == 0) ? Col1_X : Col2_X;
          int Item_Y = Leg_Y + (I / 2) * Row_H;
          using var Swatch = new SolidBrush( C );
          using var Border = new Pen( Color.FromArgb( 180, C.R, C.G, C.B ), 1f );
          using var Text = new SolidBrush( _Theme.Labels );
          G.FillRectangle( Swatch, Leg_X, Item_Y + 2, 12, 12 );
          G.DrawRectangle( Border, Leg_X, Item_Y + 2, 12, 12 );
          G.DrawString( Label, Leg_Font, Text, Leg_X + 16, Item_Y );
        }
      }

      Color Title_Color = _Theme.Background.GetBrightness() > 0.5f
          ? Color.FromArgb( 40, 40, 40 ) : Color.WhiteSmoke;
      using var Title_Font = new Font( "Segoe UI", 9f, FontStyle.Bold );
      using var Title_Brush = new SolidBrush( Title_Color );

      double Avg_Ms = 0;
      for (int i = 0; i < Sample_Count; i++)
        Avg_Ms += _Cycle_Timing[ (Start + i) % _Timing_Buffer_Size ].Total_Ms;
      Avg_Ms /= Sample_Count;
      double Rate = Avg_Ms > 0 ? 1000.0 / Avg_Ms : 0;

      string Title =
          $"Poll Cycle Duration  |  Avg: {Avg_Ms:F0} ms  ({Rate:F2} cyc/s)  "
        + $"|  Last: {_Cycle_Timing[ (_Timing_Head - 1 + _Timing_Buffer_Size) % _Timing_Buffer_Size ].Total_Ms:F0} ms  "
        + $"|  Disc: {_Disconnect_Events.Count}";

      int Available_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      if (G.MeasureString( Title, Title_Font ).Width > Available_W)
        Title = $"Avg: {Avg_Ms:F0} ms  ({Rate:F2} cyc/s)  |  Last: "
              + $"{_Cycle_Timing[ (_Timing_Head - 1 + _Timing_Buffer_Size) % _Timing_Buffer_Size ].Total_Ms:F0} ms  |  Disc: {_Disconnect_Events.Count}";

      G.DrawString( Title, Title_Font, Title_Brush, _Chart_Margin_Left, _Chart_Margin_Top - 20 );
    }

    protected void Draw_Phase_Band( Graphics G, int Start, int Sample_Count, int Chart_W, int H,
        Func<Poll_Cycle_Sample, double> Value_Selector,
        double Y_Min, double Y_Range, Color Fill_Color, string Label )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if (Sample_Count < 2)
        return;

      int Bottom = H - _Chart_Margin_Bottom;
      int Plot_H = Bottom - _Chart_Margin_Top;

      var Pts = new PointF[ Sample_Count ];
      for (int I = 0; I < Sample_Count; I++)
      {
        var S = _Cycle_Timing[ (Start + I) % _Timing_Buffer_Size ];
        float X = _Chart_Margin_Left + (float) I / (Sample_Count - 1) * Chart_W;
        float Y = Bottom - (float) ((Value_Selector( S ) - Y_Min) / Y_Range * Plot_H);
        Pts[ I ] = new PointF( X, Y );
      }

      var Fill = new PointF[ Sample_Count + 2 ];
      Array.Copy( Pts, Fill, Sample_Count );
      Fill[ Sample_Count ] = new PointF( Pts[ Sample_Count - 1 ].X, Bottom );
      Fill[ Sample_Count + 1 ] = new PointF( Pts[ 0 ].X, Bottom );

      using var Brush = new SolidBrush( Fill_Color );
      G.FillPolygon( Brush, Fill );
    }


    // ════════════════════════════════════════════════════════════════════
    // TIMING FILE LOADER  (shared — both forms can load _Timing.csv)
    // ════════════════════════════════════════════════════════════════════

    protected void Load_Timing_File( string File_Path )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      var Lines = File.ReadAllLines( File_Path );
      if (Lines.Length < 2)
        return;

      _Timing_Head = 0;
      _Timing_Count = 0;
      _Disconnect_Events.Clear();

      foreach (var Line in Lines.Skip( 1 ))
      {
        if (string.IsNullOrWhiteSpace( Line ))
          continue;
        var Parts = Line.Split( ',' );

        if (Parts.Length >= 3 && Parts[ 2 ] == "DISCONNECT")
        {
          if (DateTime.TryParse( Parts[ 0 ], out DateTime Disc_Time )
            && int.TryParse( Parts[ 1 ], out int Disc_Cycle ))
          {
            lock (_Disconnect_Events)
              _Disconnect_Events.Add( new Disconnect_Event
              {
                Time = Disc_Time,
                Instrument_Name = Parts.Length >= 8 ? Parts[ 7 ] : "Unknown",
                Cycle_Number = Disc_Cycle,
              } );
          }
          continue;
        }

        if (Parts.Length < 8)
          continue;
        if (!DateTime.TryParse( Parts[ 0 ], out DateTime T ))
          continue;

        var Sample = new Poll_Cycle_Sample
        {
          Cycle_Time = T,
          Total_Ms = Parse_Double( Parts[ 2 ] ),
          Comm_Ms = Parse_Double( Parts[ 3 ] ),
          Address_Switch_Ms = Parse_Double( Parts[ 4 ] ),
          UI_Ms = Parse_Double( Parts[ 5 ] ),
          Record_Ms = Parse_Double( Parts[ 6 ] ),
          Had_Error = Parts[ 7 ] == "1",
        };

        int Idx = _Timing_Head % _Timing_Buffer_Size;
        _Cycle_Timing[ Idx ] = Sample;
        _Timing_Head++;
        if (_Timing_Count < _Timing_Buffer_Size)
          _Timing_Count++;
      }

      _Show_Timing_View = true;
      int Loaded = Math.Min( _Timing_Count, _Timing_Buffer_Size );
      Show_Progress( $"Loaded {Loaded} timing samples, {_Disconnect_Events.Count} disconnects",
          _Foreground_Color );

      Update_Timing_Scrollbar();
      Chart_Panel_Control.Invalidate();
    }




    protected void Show_Analysis_Results( List<Instrument_Series> Series )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      var Dlg = new Form
      {
        Text = "Auto Analysis Results",
        Size = new Size( 560, 600 ),
        MinimumSize = new Size( 400, 350 ),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.Sizable,
        BackColor = SystemColors.Control,
      };

      var Header = new Panel
      {
        Dock = DockStyle.Top,
        Height = 48,
        BackColor = Color.FromArgb( 45, 45, 48 ),
      };
      Header.Controls.Add( new Label
      {
        Text = "Auto Analysis Results",
        ForeColor = Color.White,
        Font = new Font( "Segoe UI", 13f, FontStyle.Bold ),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding( 16, 0, 0, 0 ),
      } );

      var Rtb = new RichTextBox
      {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = SystemColors.Control,
        Font = new Font( "Consolas", 9.5f ),
        ScrollBars = RichTextBoxScrollBars.Vertical,
        Padding = new Padding( 12 ),
      };
      Build_Analysis_Rtb( Rtb, Series );

      var Footer = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 44,
        BackColor = SystemColors.ControlLight,
      };

      var Close_Btn = new Button
      {
        Text = "Close",
        Size = new Size( 88, 28 ),
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
      };
      Close_Btn.Click += ( s, e ) => Dlg.Close();

      // ── Deep analysis button ──────────────────────────────────────────
      var Deep_Btn = new Button
      {
        Text = "Deep Analysis...",
        Size = new Size( 120, 28 ),
        Location = new Point( 8, 8 ),
        Anchor = AnchorStyles.Top | AnchorStyles.Left,
      };
      Deep_Btn.Click += ( s, e ) =>
      {
        var Points_A = Series[ 0 ].Points;
        var Points_B = Series.Count > 1 ? Series[ 1 ].Points : null;
        string Name_A = Series[ 0 ].Name;
        string Name_B = Series.Count > 1 ? Series[ 1 ].Name : "";

        var Popup = new Analysis_Popup_Form(
            Points_A,
            Points_B,
            Name_A,
            Name_B,
            _Theme
        );
        Popup.ShowDialog( Dlg );
      };

      Footer.Controls.Add( Close_Btn );
      Footer.Controls.Add( Deep_Btn );

      Dlg.Controls.Add( Rtb );
      Dlg.Controls.Add( Footer );
      Dlg.Controls.Add( Header );
      Dlg.Shown += ( s, e ) => Close_Btn.Location = new Point( Footer.Width - 100, 8 );
      Dlg.Resize += ( s, e ) => Close_Btn.Location = new Point( Footer.Width - 100, 8 );
      Dlg.Show( this );
    }



    private void Build_Analysis_Rtb( RichTextBox Rtb, List<Instrument_Series> Series )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Rtb.Clear();

      void Append_Header( string Text, Color C )
      {
        Rtb.SelectionFont = new Font( "Segoe UI", 10f, FontStyle.Bold );
        Rtb.SelectionColor = C;
        Rtb.AppendText( Text + "\n" );
      }

      void Append_Row( string Label, string Value )
      {
        Rtb.SelectionFont = new Font( "Consolas", 9.5f );
        Rtb.SelectionColor = SystemColors.GrayText;
        Rtb.AppendText( $"  {Label,-12}" );
        Rtb.SelectionFont = new Font( "Consolas", 9.5f );
        Rtb.SelectionColor = SystemColors.ControlText;
        Rtb.AppendText( Value + "\n" );
      }

      void Append_Sep()
      {
        Rtb.SelectionFont = new Font( "Consolas", 9f );
        Rtb.SelectionColor = SystemColors.GrayText;
        Rtb.AppendText( new string( '─', 55 ) + "\n" );
      }

      foreach (var S in Series)
      {
        // Coloured instrument name
        Append_Header( $"● {S.Name}   GPIB: {S.Address}   NPLC: {S.NPLC}", S.Line_Color );
        Append_Sep();

        string Duration_Str = "—";
        if (S.Points.Count >= 2)
        {
          TimeSpan Duration = S.Points[ S.Points.Count - 1 ].Time - S.Points[ 0 ].Time;
          Duration_Str = Duration.TotalSeconds < 60
              ? $"{Duration.TotalSeconds:F1} s"
              : $"{Duration.TotalMinutes:F1} min";
        }

        Append_Row( "Min", Format_Digits( S.Get_Min(), S.Display_Digits ) );
        Append_Row( "Max", Format_Digits( S.Get_Max(), S.Display_Digits ) );
        Append_Row( "Average", Format_Digits( S.Get_Average(), S.Display_Digits ) );
        Append_Row( "Std Dev", Format_Digits( S.Get_StdDev(), S.Display_Digits + 2 ) );
        Append_Row( "RMS", $"{S.Get_RMS():G4}" );
        Append_Row( "Range", $"{S.Get_Range():G4}" );
        Append_Row( "Trend", S.Get_Trend() );
        Append_Row( "Rate", $"{S.Get_Sample_Rate():F2} S/s" );
        Append_Row( "Samples", $"{S.Points.Count:N0}" );
        Append_Row( "Duration", Duration_Str );
        Append_Row( "Errors", $"{S.Total_Errors}" );

        Rtb.AppendText( "\n" );
      }
    }

  }
}
