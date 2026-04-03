// ═══════════════════════════════════════════════════════════════════════════════
// POLL CYCLE TIMING CHART  —  Design, Purpose, and Implementation Notes
// ═══════════════════════════════════════════════════════════════════════════════
//
// OVERVIEW
// ────────
// This chart is a real-time and post-session diagnostic tool designed to answer
// one specific question: WHY does polling speed degrade over time?
//
// Traditional instrument polling loops give you no visibility into where time
// is being spent. You know the cycle took 300ms instead of 70ms, but you do not
// know if that is the instrument, the GPIB bus, the UI thread, or the file
// system. This chart makes that visible.
//
//
// WHAT IS BEING CAPTURED
// ──────────────────────
// Every polling cycle is timed using a Stopwatch that is started at the top of
// the while loop and stopped after all instruments have been read and all
// housekeeping is complete. The total wall-clock time is broken into five
// discrete phases:
//
//   Total_Ms        — Full cycle wall-clock time from first instrument read
//                     to end of UI update. This is the primary line on the
//                     chart and represents your actual polling rate.
//
//   Comm_Ms         — Time spent waiting for instrument responses across all
//                     instruments in the cycle. This includes the GPIB bus
//                     turnaround, the Prologix adapter overhead, and the
//                     instrument's own conversion time. For an HP3458A at
//                     NPLC=1 over RS-232/GPIB this is typically 40-60ms per
//                     instrument. If this band grows over time the instrument
//                     or the physical connection is the bottleneck.
//
//   Address_Switch_Ms — Time spent switching GPIB addresses between
//                     instruments (++addr, ++auto 0 commands). For a single
//                     instrument this is near zero. For multiple instruments
//                     on the same Prologix adapter this adds up. If this band
//                     grows it indicates Prologix adapter contention or serial
//                     port latency increasing.
//
//   UI_Ms           — Time spent on the UI thread via Invoke() to update the
//                     current values display, legend, cycle counter, and
//                     scrollbar. This is the most likely culprit for gradual
//                     slowdown at high point counts because the legend rebuild
//                     and panel repaints become more expensive as data
//                     accumulates. If this band grows while Comm_Ms stays
//                     flat the bottleneck is the UI, not the instrument.
//
//   Record_Ms       — Time spent writing the current cycle row to the CSV
//                     StreamWriter. Under normal conditions this is sub-
//                     millisecond since the writer is buffered. Periodic spikes
//                     every 100 cycles indicate the FlushAsync() call. If this
//                     band grows consistently it indicates disk pressure or
//                     the StringBuilder allocation cost is increasing.
//
// The remaining time (Total_Ms minus the sum of all four phases) represents
// miscellaneous overhead: Task.Delay() calls between instruments, await
// scheduling latency, and .NET thread pool overhead.
//
//
// HOW THE DATA IS STORED
// ──────────────────────
// Samples are stored in a fixed-size pre-allocated ring buffer:
//
//   private readonly Poll_Cycle_Sample[] _Cycle_Timing =
//       new Poll_Cycle_Sample[_Timing_Buffer_Size];  // default 10,000 entries
//
// Each entry is a value-type struct (no heap allocation). The ring buffer uses
// a head pointer (_Timing_Head) that wraps at _Timing_Buffer_Size. Writing a
// sample is a single array index assignment — there are no locks, no lists,
// no GC pressure on the hot polling path. This was a deliberate design choice
// to ensure the timing measurement itself does not distort the thing being
// measured.
//
// Disconnect events are stored separately in a List<Disconnect_Event> under a
// lock because they are rare (written only on first comm error per dropout)
// and need to survive ring buffer rollover for correlation purposes.
//
//
// WHAT THE CHART SHOWS
// ────────────────────
// The chart renders as a stacked area graph with five visual layers:
//
//   Gold band    — Address switching time (bottom of stack)
//   Blue band    — Communication / instrument read time
//   Green band   — UI update time
//   Red band     — Record write time
//   White line   — Total cycle time (drawn on top of all bands)
//
// The vertical gap between the top of the red band and the white total line
// represents unaccounted overhead — Task.Delay() calls, await scheduling,
// and any other time not explicitly measured.
//
// Orange dashed vertical lines mark the exact cycle where a communication
// disconnect was first detected on any instrument. The instrument name is
// labelled at the top of each marker. Because disconnects cause the polling
// loop to wait for the full GPIB timeout before Handle_Read_Error() fires,
// the white total line will almost always show a sharp upward spike at the
// same X position as an orange disconnect marker. If a spike occurs WITHOUT
// an orange marker the slowdown was caused by something other than a comm
// dropout — GC collection, Windows scheduler interference, or UI contention.
//
//
// HOW TO READ THE CHART IN PRACTICE
// ──────────────────────────────────
// Gradual upward creep in the green band
//     → UI thread is the bottleneck. Check legend rebuild frequency,
//       Current_Values_Display repaint cost, and whether the chart refresh
//       timer interval needs to be increased at high point counts.
//
// Sudden step increase in the blue band
//     → The instrument slowed down. Check NPLC setting, input signal
//       stability, and whether the instrument is in a degraded state.
//       On RS-232/GPIB also check for serial buffer overflow.
//
// Periodic spikes at regular time intervals
//     → Auto-save timer firing, FlushAsync() every 100 cycles, or Windows
//       power management interfering with the serial port.
//
// Spike exactly coincident with orange disconnect marker
//     → Physical connection issue. Cable, Prologix adapter, or instrument
//       power cycling. The height of the spike tells you how long the
//       timeout wait was before the error was detected.
//
// Baseline slowly drifting up with no single band growing
//     → .NET GC pressure from accumulated short-lived objects in the paint
//       loop (Pen, Brush, Font created and disposed on every repaint).
//       Consider pre-allocating chart drawing resources.
//
//
// CONTROLS
// ────────
// The existing Show Last 'N' Points numeric controls how many cycles are
// visible in the timing chart window — identical to how it controls the
// data chart window. The Pan scrollbar allows scrolling backward through
// the timing history. The Clear button resets the timing buffer and clears
// all disconnect events. These controls switch behavior automatically
// depending on whether the Data View or Poll Speed view is active.
//
//
// RECORDING
// ─────────
// When the Record button is pressed a companion timing CSV file is created
// alongside the instrument data CSV. The timing file uses the same timestamp
// prefix with a _Timing suffix so the two files sort together in the folder.
// Every cycle writes one row containing all five phase timings. Disconnect
// events are written as clearly marked rows and flushed to disk immediately
// so they are never lost even if the session ends abnormally. The timing file
// can be reloaded via the Load button — the chart will detect the _Timing.csv
// suffix and automatically switch to Poll Speed view with the full phase
// breakdown visible for post-session analysis.
//
// ═══════════════════════════════════════════════════════════════════════════════

using Rich_Text_Popup_Namespace;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Windows.Forms;
using System.Xml.Linq;
using Trace_Execution_Namespace;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  public partial class Multi_Instrument_Poll_Form : Base_Chart_Form
  {
    private DateTime _Last_UI_Update = DateTime.MinValue;
    private StreamWriter? _Timing_Writer;
    private string? _Timing_File_Path;
    private bool                         _Is_Shutting_Down     = false;
    private readonly Stopwatch           _Cycle_Stopwatch      = new Stopwatch();
    private bool                         _Updating_UI          = false;
    private bool                         _File_Loading         = false;
    private DateTime                     _Last_Successful_Read = DateTime.Now;
    private Label                        _Zoom_Label;
    private Instrument_Comm              _Comm;
    private GPIB_Manager                 _GPIB_Manager;
    private Dictionary<string, int>      _Error_Counts         = new Dictionary<string, int>();
    private Dictionary<string, DateTime> _Last_Success         = new Dictionary<string, DateTime>();
    private bool                         _Memory_Warning_Shown = false;
    private System.Windows.Forms.Timer   _Auto_Save_Timer;
    private ToolStripStatusLabel         _Memory_Status_Label;
    private ToolStripStatusLabel         _Performance_Status_Label;
    private Meter_Type                   _Selected_Meter;
    private bool                         _Is_Running         = false;
    private DateTime                     _Last_Legend_Update = DateTime.MinValue;
    private CancellationTokenSource? _Cts;
    private int      _Cycle_Count;
    private bool     _Poll_Error_Shown = false;
    private bool     _Is_Recording;
    private bool     _Data_Was_Recorded = false;
    private DateTime _Record_Start;
    private string   _Record_Query      = "";
    private string   _Last_Tooltip_Text = "";
    private StreamWriter? _Recording_Writer;
    private string? _Recording_File_Path;
    private readonly List<DateTime> _Reading_Timestamps           = new List<DateTime>();
    private bool                    _Capture_Timing               = false;
    private int                     _Last_Legend_Series_Count     = -1;
    private int                     _Display_Update_Counter       = 0;
    private const int               _Legend_Update_Every_N_Cycles = 10; // only rebuild legend every 10 cycles
    private Panel                   _Analysis_Results_Panel;
  //  private NPLC_Summary_Form? _NPLC_Summary_Form;
    private List<int>               _Filtered_Indices = new List<int>();
    private bool                    _Data_From_File   = false;
    private DateTime _Start_Time;
    private DateTime _Stop_Time;

    // for indicating slow down in polling
    private double                  _Baseline_FPS          = 0;
    private int                     _Baseline_Samples      = 0;  // how many cycles before we lock in baseline
    private const int               Baseline_Warmup_Cycles = 10; // ignore first N cycles
    private const double            Slowdown_Threshold     = 0.80; // 80% of baseline = red

    private readonly Memory_Monitor _Memory_Monitor     = new Memory_Monitor();
    private int                     _Max_Display_Points = 0;

    private readonly List<Instrument> _Instruments;

    private Rich_Text_Popup? _NPLC_Summary_Popup;

    private static readonly( string Label,
                             string Cmd_3458,
                             string Cmd_34401,
                             string Cmd_3456,
                             string Cmd_Generic_GPIB,
                             string Unit )[ ] _Measurements = {
      ( "DC Voltage", "DCV", "MEAS:VOLT:DC", "F1T3", "MEAS:VOLT:DC", "V" ),
      ( "AC Voltage", "ACV", "MEAS:VOLT:AC", "F2T3", "MEAS:VOLT:AC", "V" ),
      ( "DC Current", "DCI", "MEAS:CURR:DC", "F5T3", "MEAS:CURR:DC", "A" ),
      ( "AC Current", "ACI", "MEAS:CURR:AC", "F6T3", "MEAS:CURR:AC", "A" ),
      ( "2-Wire Ohms", "OHM", "MEAS:RES", "F3T3", "MEAS:RES", "Ohm" ),
      ( "4-Wire Ohms", "OHMF", "MEAS:FRES", "F4T3", "MEAS:FRES", "Ohm" ),
      ( "Frequency", "FREQ", "MEAS:FREQ", "", "MEAS:FREQ", "Hz" ),
      ( "Period", "PER", "MEAS:PER", "", "MEAS:PER", "s" ),
      ( "Continuity", "", "MEAS:CONT", "", "MEAS:CONT", "Ohm" ),
      ( "Diode", "", "MEAS:DIOD", "", "MEAS:DIOD", "V" ),
      ( "Temperature", "TEMP", "", "", "", "\u00b0C" ),
    };

    public Multi_Instrument_Poll_Form( Instrument_Comm Comm,
                                    List<Instrument> Instruments,
                                    Application_Settings Settings,
                                    Meter_Type Selected_Meter )
    {
      
      using var Block = Trace_Block.Start_If_Enabled();

      InitializeComponent();

      _Instruments = Instruments;   // ← store it here
      _Comm = Comm;
      _Settings = Settings;
      _Selected_Meter = Selected_Meter;


      _Theme              = _Settings.Current_Theme;
      _Max_Display_Points = _Settings.Max_Display_Points;




      int Nplc_Min_Ms = _Instruments.Sum( s => s.Poll_Delay_Ms ) + 50;

      Delay_Numeric.Value = Math.Max( Delay_Numeric.Minimum,
                            Math.Min( Delay_Numeric.Maximum, Nplc_Min_Ms ) );


      Chart_Panel_Control          = Chart_Panel;
      Pan_Scrollbar_Control        = Pan_Scrollbar;
      Auto_Scroll_Check_Control    = Auto_Scroll_Check;
      Rolling_Check_Control        = Rolling_Check;
      Max_Points_Numeric_Control   = Max_Points_Numeric;
      Zoom_Slider_Control          = Zoom_Slider;
      Graph_Style_Combo_Control    = Graph_Style_Combo;
      View_Mode_Button_Control     = View_Mode_Button;
      Normalize_Button_Control     = Normalize_Button;
      Legend_Toggle_Button_Control = Legend_Toggle_Button;

      Chart_Panel.BackColor = _Theme.Background;
      // ── Dispose first, then rebuild with new theme colors ─────────────
      Dispose_Chart_Resources(); // ← old resources released
      Initialize_Chart_Resources();
      Initialize_Chart_Refresh_Timer();

      _Settings.Theme_Changed += ( s, e ) =>
      {
        _Theme                = _Settings.Current_Theme;
        Chart_Panel.BackColor = _Theme.Background;
        Apply_Theme_To_Current_Values_Panel();

        Chart_Panel.Invalidate();
      };

      Initialize_Current_Values_Display();

      _Selected_Meter = Selected_Meter;

      Enable_Double_Buffer( Chart_Panel );



      // Create series
      for (int I = 0; I < _Instruments.Count; I++)
      {
        var Inst = _Instruments[ I ];
        _Series.Add( new Instrument_Series
        {
          Instrument = Inst,
          Points = new List<(DateTime Time, double Value)>(),
          Line_Color = _Theme.Line_Colors[ I % _Theme.Line_Colors.Length ],
        } );
        _Error_Counts[ Inst.Name ] = 0;
        _Last_Success[ Inst.Name ] = DateTime.Now;
      }

      // Initialize tooltip
      _Chart_Tooltip              = new ToolTip();
      _Chart_Tooltip.AutoPopDelay = 5000;
      _Chart_Tooltip.InitialDelay = 100;
      _Chart_Tooltip.ReshowDelay  = 100;
      _Chart_Tooltip.ShowAlways   = true;

      // Wire up mouse move event
      Chart_Panel.MouseMove += Chart_Panel_MouseMove;

      Create_Legend_Panel();

      // Chart_Panel.BackColor = _Theme.Background;
      Text = $"Multi-Instrument Poller ({Instruments.Count} instruments)";

      Populate_Measurement_Combo();

      _Auto_Save_Timer       = new System.Windows.Forms.Timer();
      _Auto_Save_Timer.Tick += Auto_Save_Timer_Tick;

      // Initialize GPIB manager with settings AND comm
      _GPIB_Manager = new GPIB_Manager( _Settings, _Comm );

      Normalize_Button.Visible = _Combined_View;

      Initialize_Chart_Refresh_Timer();

      Update_Graph_Style_Availability();

      Capture_Trace.Write( "Constructor: about to call Query_Instrument_Name" );
      Query_Instrument_Name();
      Capture_Trace.Write( "Constructor: returned from Query_Instrument_Name" );

      Capture_Trace.Write( "Constructor: about to call Initialize_Stats_Panel" );
      Initialize_Status_Panel();

      Capture_Trace.Write( "Constructor: about to call Apply_Settings" );
      Apply_Settings();
      Capture_Trace.Write( "Constructor: Apply_Settings complete" );

      Legend_Toggle_Button.Text = "Stats";

      Capture_Trace.Write( $"_Chart_Tooltip initialized: {_Chart_Tooltip != null}" );
      Capture_Trace.Write( $"Show_Tooltips_On_Hover = {_Settings.Show_Tooltips_On_Hover}" );
      Capture_Trace.Write( $"Tooltip_Distance_Threshold = {_Settings.Tooltip_Distance_Threshold}" );

      Max_Points_Numeric.Enabled = true;

      Set_Button_State();
    }

    private void Pan_Scrollbar_Scroll( object          sender,
                                       ScrollEventArgs e ) => Pan_Scrollbar_Control_Scroll( sender, e );

    private void Pan_Scrollbar_ValueChanged( object    sender,
                                             EventArgs e ) => Pan_Scrollbar_Control_ValueChanged( sender, e );

    protected override bool _Is_Running_State() => _Is_Running;
    protected override void Show_Progress( string Message, Color Color )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Progress_Text_Box.Text      = Message;
      Progress_Text_Box.ForeColor = Color;
    }

    private void       Update_Chart_Refresh_Rate() => Update_Chart_Refresh_Rate( _Chart_Refresh_Timer );
    protected override string Current_Unit
    {
      get {
        string Measurement = Measurement_Combo.Text.Trim();
        if ( Measurement.Contains( "Voltage" ) )
          return "V";
        if ( Measurement.Contains( "Current" ) )
          return "A";
        if ( Measurement.Contains( "Resistance" ) )
          return "Ω";
        if ( Measurement.Contains( "Frequency" ) )
          return "Hz";
        if ( Measurement.Contains( "Temperature" ) )
          return "°C";
        if ( Measurement.Contains( "Capacitance" ) )
          return "F";
        return "";
      }
    }

    private void Chart_Panel_MouseLeave( object? sender, EventArgs e )
    {
      _Last_Mouse_Position = Point.Empty;
      _Last_Tooltip_Text   = "";
      _Chart_Tooltip.Hide( Chart_Panel_Control );
      Chart_Panel_Control.Invalidate(); // clear the crosshair
    }

    private void Chart_Panel_MouseMove( object sender, MouseEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Mirror the same no-data guards as Paint
      if ( _Show_Timing_View )
      {
        _Chart_Tooltip.Hide( Chart_Panel );
        return;
      }
      if ( _Series.Count == 0 )
      {
        _Chart_Tooltip.Hide( Chart_Panel );
        return;
      }
      bool Has_Data = _Series.Any( s => s.Visible && s.Points.Count > 0 );
      if ( ! Has_Data )
      {
        _Chart_Tooltip.Hide( Chart_Panel );
        return;
      }

      // Only start tracing once we know there's real work to do

      Capture_Trace.Write( $"MouseMove at ({e.X}, {e.Y})" );

      // Check if tooltips are enabled
      if ( ! _Settings.Show_Tooltips_On_Hover )
      {
        Capture_Trace.Write( "Tooltips disabled in settings" );
        return;
      }

      // Throttle updates to every 100ms
      if ( ( DateTime.Now - _Last_Tooltip_Update ).TotalMilliseconds < 100 )
        return;
      if ( e.Location == _Last_Mouse_Position )
        return;
      _Last_Mouse_Position = e.Location;
      _Last_Tooltip_Update = DateTime.Now;

      // Find the closest point
      var ( Series, Point_Index, Distance ) = Find_Closest_Point( e.Location );

      // Invalidate to draw position indicator whether running or not
      Chart_Panel_Control.Invalidate();

      // Use settings for distance threshold
      if ( Series != null && Distance < _Settings.Tooltip_Distance_Threshold )
      {
        var    Point_Data   = Series.Points[ Point_Index ];
        string Tooltip_Text = $"{Series.Name}\n" + $"Time: {Point_Data.Time:HH:mm:ss.fff}\n" +
                              $"Value: {Format_Digits(Point_Data.Value, Series.Display_Digits)}";

        _Chart_Tooltip.Show( Tooltip_Text,
                             Chart_Panel,
                             e.Location.X + 15,
                             e.Location.Y - 40,
                             _Settings.Tooltip_Display_Duration_Ms );
      }
      else
      {
        if ( _Last_Tooltip_Text != "" )
        {
          _Last_Tooltip_Text = "";
          _Chart_Tooltip.Hide( Chart_Panel );
        }
      }
    }

    private PointF[ ] Build_Point_Array_Combined( Instrument_Series Series,
                                                  DateTime          Start_Time,
                                                  TimeSpan          Duration,
                                                  double            Padded_Min,
                                                  double            Padded_Range,
                                                  int               W,
                                                  int               H )
    {
      if ( Series.Points.Count == 0 )
        return Array.Empty<PointF>();

      int Chart_W = W - _Chart_Margin_Left - _Chart_Margin_Right;
      int Chart_H = H - _Chart_Margin_Top - _Chart_Margin_Bottom;

      var Points = new PointF[ Series.Points.Count ];
      for ( int I = 0; I < Series.Points.Count; I++ )
      {
        var    P = Series.Points[ I ];

        double Time_Offset_Seconds = ( P.Time - Start_Time ).TotalSeconds;
        float  X_Ratio             = (float) ( Time_Offset_Seconds / Duration.TotalSeconds );
        float  X                   = _Chart_Margin_Left + ( X_Ratio * Chart_W );

        double Normalized = ( P.Value - Padded_Min ) / Padded_Range;
        float  Y          = H - _Chart_Margin_Bottom - (float) ( Normalized * Chart_H );

        Points[ I ] = new PointF( X, Y );
      }
      return Points;
    }

    private void Auto_Save_Timer_Tick( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Is_Running && _Series.Any( s => s.Points.Count > 0 ) )
      {
        // Auto-save in background
        try
        {
          string Folder = _Settings.Default_Save_Folder;
          Directory.CreateDirectory( Folder );

          string Timestamp = DateTime.Now.ToString( "yyyy-MM-dd_HH-mm-ss" );
          string File_Name = $"{Timestamp}_Multi_AutoSave.csv";
          string File_Path = Path.Combine( Folder, File_Name );

          Save_To_File( File_Path );

          Show_Progress( $"Auto-saved: {File_Name}", _Foreground_Color );
        }
        catch ( Exception ex )
        {
          Show_Progress( $"Auto-save failed: {ex.Message}", _Foreground_Color );
        }
      }
    }

    protected override void OnFormClosing( FormClosingEventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Only do cleanup here if Close_Button didn't already handle it
      if ( _Is_Running || _Is_Recording )
      {
        Capture_Trace.Write( "Cleanup needed (Close_Button was not used)" );
        // _Poll_Timer?.Stop ( );
        _Auto_Save_Timer?.Stop();
        _Chart_Refresh_Timer?.Stop();
        _Chart_Tooltip?.Dispose(); // ADD THIS
        Stop_Polling();
        // Set_Local_Mode ( );
        base.OnFormClosing( E );
      }
      else
      {
        Capture_Trace.Write( "Already cleaned up, skipping" );
      }
     // _NPLC_Summary_Form?.Close();
      Dispose_Chart_Resources();

      base.OnFormClosing( E );
    }

    private void Show_Memory_Warning( int Current, int Max )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      int       Percent = ( Current * 100 ) / Max;

      if ( ! _Is_Running )
      {
        // Just informational during load
        MessageBox.Show( $"Memory usage is at {Percent}% of the limit.\n\n" +
                           $"Current: {Current:N0} points across {_Series.Count} instruments\n" +
                           $"Limit: {Max:N0} points\n\n" +
                           "Consider reducing the data or increasing the limit.",
                         "Memory Warning",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        return;
      }

      // Do not stop recording immediately - give user the choice to continue or stop and save
      Capture_Trace.Write( $"Memory warning: {Percent}% used ({Current} / {Max} points)" );

      var Result = MessageBox.Show( $"Memory usage is at {Percent}% of the limit.\n\n" +
                                      $"Current: {Current:N0} points across {_Series.Count} instruments\n" +
                                      $"Limit: {Max:N0} points\n\n" + "Continue recording?",
                                    "Memory Warning",
                                    MessageBoxButtons.YesNoCancel,
                                    MessageBoxIcon.Warning );

      if ( Result == DialogResult.Yes )
      {
        // Continue - do nothing
        return;
      }
      else if ( Result == DialogResult.No )
      {
        // Stop and save
        _Is_Running            = false;
        Start_Stop_Button.Text = "Start";
        // _Poll_Timer.Stop ( );
        Save_Recorded_Data();
      }
      else
      {
        // Cancel - just stop
        _Is_Running            = false;
        Start_Stop_Button.Text = "Start";
        // _Poll_Timer.Stop ( );
      }
    }

    private void Update_Memory_Status( int Current, int Max )
    {
      using var Block   = Trace_Block.Start_If_Enabled();
      int       Percent = ( Current * 100 ) / Max;

      if ( _Memory_Status_Label != null )
      {
        _Memory_Status_Label.Text = $"Memory: {Current:N0} / {Max:N0} ({Percent}%)";
      }
    }

    protected override void Update_Performance_Status()
    {
      Multimeter_Common_Helpers_Class.Update_Performance_Status( _Performance_Status_Label,
                                                                 _Memory_Status_Label,
                                                                 _Actual_FPS,
                                                                 _Series.Sum( s => s.Points.Count ),
                                                                 _Series.Sum( s => s.Points.Count ),
                                                                 _Settings.Max_Data_Points_In_Memory,
                                                                 _Settings.Warning_Threshold_Percent );
    }

    private void Chart_Panel_Resize( object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Chart_Panel.Invalidate();
    }

    private void Continuous_Check_Changed( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Set_Button_State();
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PER-INSTRUMENT NPLC FIXES
    // ═══════════════════════════════════════════════════════════════════════════════
    //
    // SUMMARY OF CHANGES
    // ──────────────────
    // Previously a single NPLC_Textbox.Text string was parsed once and applied to
    // all instruments. Now each Instrument_Series has its own S.NPLC (double).
    //
    // Changes made:
    //   1. Start_Polling  — removed global NPLC_Value / NPLC_Value_Double.
    //                       Comm timeout is now set from the MAX NPLC across all
    //                       instruments. Each instrument's configure block reads
    //                       S.NPLC directly.
    //   2. Update_Settle_Display — now shows the max NPLC settle time across all
    //                       instruments (falls back to NPLC_Textbox if no series).
    //   3. Apply_Settings — the NPLC_Textbox.Text line is replaced with a call
    //                       to Update_Settle_Display().
    //   4. Per-instrument NPLC command strings now use S.NPLC.ToString().
    //
    // INSTRUCTIONS
    // ────────────
    // Replace the three methods below in Multi_Instrument_Poll_Form.cs.
    // The rest of the file is unchanged.
    // ═══════════════════════════════════════════════════════════════════════════════

    // ─── 1. Replace Update_Settle_Display ────────────────────────────────────────

    /*
    private void Update_Settle_Display ( )
    {
      // When instruments exist, show the worst-case (max NPLC) settle time.
      // If no instruments are loaded yet, fall back to the NPLC_Textbox value
      // so the UI still shows something useful before a session starts.
      string NPLC_Source = _Series.Count > 0
          ? _Series.Max ( s => s.NPLC ).ToString ( CultureInfo.InvariantCulture )
          : NPLC_Textbox.Text.Trim ( );

      int Settle_Ms = Multimeter_Common_Helpers_Class.Calculate_Settle_Ms ( NPLC_Source );
      NPLC_Delay_Textbox.Text = $"{Settle_Ms} ms";
    }
    */

    // ─── 2. Replace Apply_Settings (only the NPLC section changes) ───────────────
    //
    //  Find this line in Apply_Settings:
    //      NPLC_Textbox.Text = _Settings.Default_NPLC.ToString ( CultureInfo.InvariantCulture );
    //      Update_Settle_Display ( );
    //
    //  Replace with:
    //      NPLC_Textbox.Text = _Settings.Default_NPLC.ToString ( CultureInfo.InvariantCulture );
    //      Update_Settle_Display ( );   // ← already calls max-NPLC logic above; no other change needed
    //
    //  (No change required in Apply_Settings itself — Update_Settle_Display now
    //   reads from _Series automatically.)

    // ─── 3. Replace Start_Polling ────────────────────────────────────────────────

    private async void Start_Polling()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( ! _Comm.Is_Connected )
      {
        MessageBox.Show( "Not connected. Please connect first.",
                         "Connection Required",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        return;
      }

      if ( _Series.Count == 0 )
      {
        MessageBox.Show( "No instruments in the list.\n" + "Add instruments on the main form first.",
                         "No Instruments",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        return;
      }

      int Combo_Index = Measurement_Combo.SelectedIndex;
      if ( Combo_Index < 0 || Combo_Index >= _Filtered_Indices.Count )
        return;

      string Command = Measurement_Combo.Text.Trim();
      if ( string.IsNullOrEmpty( Command ) )
        return;

      _Cts?.Cancel();
      _Cts?.Dispose();
      _Cts              = new CancellationTokenSource();
      _Poll_Error_Shown = false;

      _Is_Running            = true;
      _Cycle_Count           = 0;
      Start_Stop_Button.Text = "Stop";
      Show_Progress( "Polling...", _Foreground_Color );

      int Func_Index = _Filtered_Indices[ Combo_Index ];
      var Selected   = _Measurements[ Func_Index ];

      Capture_Trace.Write( $"Starting poll with command: '{Command}'" );

      string Configure_Cmd =
        _Series.Count > 0 && _Series[ 0 ].Type == Meter_Type.HP34401 ? Selected.Cmd_34401 : Selected.Cmd_3458;

      string Unit          = Selected.Unit;
      bool   Is_Query_Mode = Configure_Cmd.EndsWith( "?" );

      Capture_Trace.Write( $"Configure command: {Configure_Cmd}" );
      Capture_Trace.Write( $"Unit             : {Unit}" );
      Capture_Trace.Write( $"Query mode       : {Is_Query_Mode}" );

      Delay_Numeric.Enabled     = true;
      Measurement_Combo.Enabled = false;
      Continuous_Check.Enabled  = false;
      Cycles_Numeric.Enabled    = false;
      Clear_Button.Enabled      = false;
      Load_Button.Enabled       = false;

      bool              Continuous       = Continuous_Check.Checked;
      int               Total_Cycles     = (int) Cycles_Numeric.Value;
      int               Original_Address = _Comm.GPIB_Address;
      CancellationToken Token            = _Cts.Token;

      // ── Per-instrument NPLC: derive max for timeout purposes ──────────────
      // Each instrument will use its own S.NPLC in the configure block below.
      // The comm timeout must cover the slowest instrument in the session.
      double            Max_NPLC = _Series.Count > 0 ? (double) _Series.Max( s => s.NPLC ) : 1.0;

      // Update settle display to show worst-case before polling starts
      _Comm.Instrument_Settle_Ms = Multimeter_Common_Helpers_Class.Calculate_Settle_Ms(
        Max_NPLC.ToString( CultureInfo.InvariantCulture ) );

      int    Integration_Ms = (int) ( Max_NPLC * ( 1000.0 / 60.0 ) );
      int    Rate_Per_Min   = (int) ( 60_000.0 / _Comm.Instrument_Settle_Ms );
      string Warning        = _Comm.Instrument_Settle_Ms >= 1000
                                ? $"⚠  One reading every {_Comm.Instrument_Settle_Ms / 1000.0:F1}s"
                              : _Comm.Instrument_Settle_Ms >= 200 ? "ℹ  Moderate settle time"
                                                                  : "✓  Fast polling";

      // Set read timeout from the slowest instrument (+50% safety margin + 2s base)
      int    Required_Timeout_Ms = (int) ( ( Max_NPLC / 60.0 ) * 1000 * 1.5 ) + 2000;
      _Comm.Read_Timeout_Ms      = Math.Max( _Settings.Prologix_Read_Tmo_Ms, Required_Timeout_Ms );

      Capture_Trace.Write( $"Max NPLC across instruments = {Max_NPLC}" );
      Capture_Trace.Write( $"Comm timeout set to {_Comm.Read_Timeout_Ms} ms" );

      bool[ ] Configured = new bool[ _Series.Count ];

      Capture_Trace.Write( $"Points: {_Settings.Max_Display_Points}" );

      // Setup cts for finish poling
      var My_Cts = _Cts;

      try
      {
        _Comm.Error_Occurred -= On_Poll_Error;
        _Comm.Error_Occurred += On_Poll_Error;

        // ========================================
        // PHASE 1: CONFIGURE ALL INSTRUMENTS
        // ========================================
        Show_Progress( "Setting up measurements", _Foreground_Color );

        for ( int I = 0; I < _Series.Count; I++ )
        {
          Token.ThrowIfCancellationRequested();
          var    S = _Series[ I ];

          // Each instrument uses its own NPLC
          double S_NPLC     = (double) S.NPLC;
          string S_NPLC_Str = S_NPLC.ToString( CultureInfo.InvariantCulture );

          Capture_Trace.Write( $"" );
          Capture_Trace.Write( $"=== Configuring {S.Name} GPIB {S.Address}, NPLC {S_NPLC}) ===" );

          await Task.Run( () => _Comm.Change_GPIB_Address( S.Address ), Token );
          await Task.Delay( 50, Token );

          await Task.Run( () => _Comm.Send_Prologix_Command( "++savecfg 0" ), Token );
          await Task.Delay( 50, Token );

          Capture_Trace.Write( $"Setting ++auto 0 for {S.Type}" );
          await Task.Run( () => _Comm.Send_Prologix_Command( "++auto 0" ), Token );
          await Task.Delay( 50, Token );

          await Task.Run( () => _Comm.Send_Prologix_Command( "++clr" ), Token );
          await Task.Delay( 100, Token );

          // Configure NPLC using this instrument's own value
          if (S.Type == Meter_Type.HP3458)
          {
            S.Total_Errors = 0;
            S.Consecutive_Errors = 0;

            // Send measurement function (e.g. DCV)
            string Config_Command = Get_Command_For_Series( S, Selected );
            Capture_Trace.Write( $"Sending config command: {Config_Command} to {S.Type}" );
            await Task.Run( () => _Comm.Send_Instrument_Command( Config_Command, S.Type ), Token );
            await Task.Delay( 200, Token );

            // Send NPLC
            Capture_Trace.Write( $"Sending NPLC {S_NPLC_Str} to HP3458" );
            await Task.Run( () => _Comm.Send_Instrument_Command( $"NPLC {S_NPLC_Str}", S.Type ), Token );
            await Task.Delay( 200, Token );

            if (S_NPLC >= 10)
            {
              Capture_Trace.Write( $"║  3458 configuring TRIG HOLD for NPLC {S_NPLC}" );
              await Task.Run( () => _Comm.Send_Instrument_Command( "TRIG HOLD", S.Type ), Token );
              await Task.Delay( 200, Token );
            }
            else
            {
              await Task.Run( () => _Comm.Send_Instrument_Command( "TRIG AUTO", S.Type ), Token );
              await Task.Delay( 1000, Token );

              Capture_Trace.Write( $"Priming 3458..." );
              await Task.Run( () => _Comm.Raw_Write_Prologix( "++read eoi" ), Token );
              string Prime = await Task.Run( () => _Comm.Read_Instrument( Token ), Token ) ?? "";
              Capture_Trace.Write( $"Prime response: {Prime.Split( '\n' )[ 0 ]}" );
              await Task.Delay( 50, Token );
            }
          }
          else if (S.Type == Meter_Type.HP34401)
          {
            S.Total_Errors = 0;
            S.Consecutive_Errors = 0;

            string Measurement_Label = Measurement_Combo.Text.Trim();
            string Conf_Cmd = Get_Command_For_Series( S, Selected );
            if (Conf_Cmd.StartsWith( "MEAS:" ))
              Conf_Cmd = "CONF:" + Conf_Cmd.Substring( 5 );

            Capture_Trace.Write( $"Sending CONF command: {Conf_Cmd} to HP34401" );
            await Task.Run( () => _Comm.Send_Instrument_Command( Conf_Cmd, S.Type ), Token );
            await Task.Delay( 200, Token );

            string? NPLC_Cmd = Multimeter_Common_Helpers_Class.Build_NPLC_Command( Measurement_Label, S_NPLC_Str );
            if (NPLC_Cmd != null)
            {
              Capture_Trace.Write( $"Sending NPLC command: {NPLC_Cmd} to HP34401" );
              await Task.Run( () => _Comm.Send_Instrument_Command( NPLC_Cmd, S.Type ), Token );
              await Task.Delay( 200, Token );
            }
            else
            {
              Capture_Trace.Write( $"NPLC not applicable for {Measurement_Label} on HP34401, skipping" );
            }
          }
          else
          {
            S.Total_Errors = 0;
            S.Consecutive_Errors = 0;

            string Config_Command = Get_Command_For_Series( S, Selected );
            Capture_Trace.Write( $"Sending config command: {Config_Command} to {S.Type}" );
            await Task.Run( () => _Comm.Send_Instrument_Command( Config_Command, S.Type ), Token );
            await Task.Delay( 200, Token );
          }

          Configured[ I ] = true;
          Capture_Trace.Write( $"{S.Name} configured successfully" );
        }

        Show_Progress( "Polling...", _Foreground_Color );
        await Task.Delay( 300, Token );

        // ========================================
        // PHASE 2: POLLING LOOP
        // ========================================

        _Baseline_FPS     = 0;
        _Baseline_Samples = 0;

        this.Invoke( () =>
                     {
                       Initialize_Current_Values_Display();
                       // _Last_Legend_Series_Count = _Series.Count;   // sync the rebuild guard too
                     } );

        _Cycle_Stopwatch.Restart();
        var      Sw               = Stopwatch.StartNew();
        int      Previous_Address = -1;

        DateTime Cycle_Start;

        int Nplc_Min_Ms = Calculate_Nplc_Min_Ms();

        this.Invoke( () =>
        {
          decimal Clamped = Math.Max( Delay_Numeric.Minimum,
                            Math.Min( Delay_Numeric.Maximum, Nplc_Min_Ms ) );

          if (Delay_Numeric.Value != Clamped)
          {
            Delay_Numeric.Value = Clamped;
            Show_Progress( $"Delay auto-set to {Nplc_Min_Ms}ms to match NPLC settings", Color.CornflowerBlue );
          }

          Delay_Numeric.Enabled = false;  // ← lock it
        } );





        while ( ! Token.IsCancellationRequested && ( Continuous || _Cycle_Count < Total_Cycles ) )
        {

          Cycle_Start = DateTime.UtcNow;

          _Cycle_Count++;
          _Cycle_Stopwatch.Restart();

          double   Addr_Ms = 0, Comm_Ms = 0, UI_Ms = 0, Record_Ms = 0;
          bool     Cycle_Had_Error = false;

          DateTime Cycle_Time = DateTime.Now;
          Log_Cycle_Header( Cycle_Count: _Cycle_Count );

          for ( int I = 0; I < _Series.Count; I++ )
          {
            Token.ThrowIfCancellationRequested();
            var    S = _Series[ I ];

            // Use this instrument's own NPLC for all read-time decisions
            double S_NPLC = (double) S.NPLC;

            // ── Address switch ────────────────────────────────────────────
            if ( S.Address != Previous_Address )
            {
              Sw.Restart();
              await Task.Run( () => _Comm.Change_GPIB_Address( S.Address ), Token );
              await Task.Delay( 50, Token );
              await Task.Run( () => _Comm.Send_Prologix_Command( "++auto 0" ), Token );
              await Task.Delay( 50, Token );
              Addr_Ms          += Sw.Elapsed.TotalMilliseconds;
              Previous_Address  = S.Address;
            }

            // ── Actual read ───────────────────────────────────────────────
            string Response;
            try
            {
              Sw.Restart();
              if ( S.Type == Meter_Type.HP3458 )
              {
                if ( S_NPLC >= 10 )
                {
                  Capture_Trace.Write( $"║  3458 TRIG SGL + read (NPLC {S_NPLC})" );
                  await Task.Run( () => _Comm.Send_Instrument_Command( "TRIG SGL", S.Type ), Token );

                  // Wait based on THIS instrument's NPLC
                  int              Wait_Ms = (int) ( ( S_NPLC / 60.0 ) * 1000 ) + 2000;
                  await            Task.Delay( Wait_Ms, Token );

                  await            Task.Run( () => _Comm.Raw_Write_Prologix( "++read eoi" ), Token );
                  Response = await Task.Run( () => _Comm.Read_Instrument( Token ), Token ) ?? "";
                }
                else
                {
                  await            Task.Run( () => _Comm.Raw_Write_Prologix( "++read eoi" ), Token );
                  await            Task.Delay( 50, Token );
                  Response = await Task.Run( () => _Comm.Read_Instrument( Token ), Token ) ?? "";
                }
              }
              else if ( S.Type == Meter_Type.HP34401 )
              {
                Response = await Task.Run( () => _Comm.Query_Instrument( "READ?", Token ), Token );
              }
              else
              {
                string           Instrument_Command = Get_Command_For_Series( S, Selected );
                Response = await Task.Run( () => _Comm.Query_Instrument( Instrument_Command, Token ), Token );
              }
              Comm_Ms += Sw.Elapsed.TotalMilliseconds;
            }
            catch ( TimeoutException Ex )
            {
              Comm_Ms         += Sw.Elapsed.TotalMilliseconds;
              Cycle_Had_Error  = true;
              Handle_Read_Error( S, $"TIMEOUT: {Ex.Message}" );
              continue;
            }
            catch ( InvalidOperationException Ex )
            {
              Comm_Ms         += Sw.Elapsed.TotalMilliseconds;
              Cycle_Had_Error  = true;
              Handle_Read_Error( S, $"PORT CLOSED: {Ex.Message}" );
              continue;
            }
            catch ( OperationCanceledException )
            {
              Capture_Trace.Write( $"║  Read cancelled for {S.Name} — exiting cycle" );
              break;  // ← break out of the instruments loop, falls through to finally naturally
            }
            catch ( Exception Ex )
            {
              Comm_Ms         += Sw.Elapsed.TotalMilliseconds;
              Cycle_Had_Error  = true;
              Handle_Read_Error( S, $"COMM ERROR: {Ex.GetType().Name} - {Ex.Message}" );
              continue;
            }

            if ( double.TryParse( Response,
                                  System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.InvariantCulture,
                                  out double Value ) )
            {
              var Point = ( DateTime.Now, Value );
              S.Points.Add( Point );
              S.Add_Point_Value( Value );

              if ( S.Points.Count > _Settings.Max_Display_Points )
              {
                if ( _Settings.Stop_Polling_At_Max_Display_Points )
                {
                  Capture_Trace.Write( "Max display points reached - stopping poll" );
                  this.Invoke( () =>
                  {
                    Show_Progress( "Max display points reached", Color.Orange );
                    Stop_Polling();
                  } );
                  break;  // ← let the cancellation propagate naturally to finally
                }
                else
                {
                  // Only trim when 10% over limit — reduces trim frequency dramatically
                  int Trim_Threshold = (int) ( _Settings.Max_Display_Points * 1.1 );
                  if ( S.Points.Count >= Trim_Threshold )
                  {
                    // Trim back to 90% of limit so next trim won't happen for a while
                    int Target = (int) ( _Settings.Max_Display_Points * 0.9 );
                    int Excess = S.Points.Count - Target;
                    S.Points.RemoveRange( 0, Excess );
                  }
                }
              }

              S.Consecutive_Errors  = 0;
              _Last_Successful_Read = DateTime.Now;
              Capture_Trace.Write( $"║  Parsed value: {Value}" );
            }
            else
            {
              S.Consecutive_Errors++;
              S.Total_Errors++;
              Capture_Trace.Write( $"║  ERROR: consecutive={S.Consecutive_Errors} total={S.Total_Errors}" );
            }
          }

          Log_Cycle_Footer();

          // ── Record write ──────────────────────────────────────────────────
          Sw.Restart();
          Write_Recording_Row();
          Record_Ms = Sw.Elapsed.TotalMilliseconds;

          // ── UI update ─────────────────────────────────────────────────────
          // Only redraw chart every N cycles or every ~500ms
          if ( _Cycle_Count % 5 == 0 || ( DateTime.Now - _Last_UI_Update ).TotalMilliseconds > 500 )
          {
            Sw.Restart();
            Update_Cycle_Display( Continuous, Total_Cycles );
            UI_Ms           = Sw.Elapsed.TotalMilliseconds;
            _Last_UI_Update = DateTime.Now;
          }

          // ── Capture full cycle ────────────────────────────────────────────
          _Cycle_Stopwatch.Stop();

          Record_Cycle_Timing( DateTime.Now,
                               _Cycle_Stopwatch.Elapsed.TotalMilliseconds,
                               Comm_Ms,
                               Addr_Ms,
                               UI_Ms,
                               Record_Ms,
                               Cycle_Had_Error );

          // This won't fix GPIB reads that are inherently slow,
          // but it eliminates the scheduling drift entirely —
          // the interval becomes wall-clock anchored.

          bool Has_More = Continuous || _Cycle_Count < Total_Cycles;
          if (Has_More)
          {
            // Calculate the minimum delay required by the slowest instrument's NPLC
            int Min_Required_Ms = _Series
    .Sum( s => (int) (((double) s.NPLC / 60.0) * 1000) + 500 );

            // Use whichever is larger: user-set delay or NPLC-derived minimum
            int Delay_Ms = Math.Max( (int) Delay_Numeric.Value, Min_Required_Ms );

            // How long did the cycle actually take?
            int Elapsed = (int) (DateTime.UtcNow - Cycle_Start).TotalMilliseconds;
            int Remaining = Delay_Ms - Elapsed;
            if (Remaining > 0)
              await Task.Delay( Remaining, Token );
            // If elapsed > Delay_Ms, fire immediately (no negative delay)
          }
        }
      }
      catch ( OperationCanceledException )
      {
        Capture_Trace.Write( "Polling cancelled by user" );
      }
      catch ( TimeoutException Ex )
      {
        Capture_Trace.Write( $"║  Unhandled timeout: {Ex.Message} - continuing" );
        _Cts?.Cancel();
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write( $"Polling error: {Ex.GetType().Name} - {Ex.Message}" );
        if ( ! _Poll_Error_Shown )
        {
          _Poll_Error_Shown = true;
          _Cts?.Cancel();
          MessageBox.Show( $"Polling error: {Ex.Message}",
                           "Error",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Error );
        }
      }
      finally
      {
        _Comm.Error_Occurred -= On_Poll_Error;
        try
        {
          _Comm.Change_GPIB_Address( Original_Address );
        }
        catch
        {
        }

        this.Invoke( () => Delay_Numeric.Enabled = true );  // ← unlock it

        Finish_Polling( My_Cts );
      }
    }

    private void Handle_Read_Error( Instrument_Series S, string Message )
    {
      Capture_Trace.Write( $"║  {Message} on {S.Name} - continuing" );
      S.Consecutive_Errors++;
      S.Total_Errors++;
      S.Comm_Error_Count++;

      if ( S.Consecutive_Errors == 1 && ! _Is_Shutting_Down ) // ← add check
      {
        S.Disconnect_Count++;
        Record_Disconnect( S.Name, _Cycle_Count );
        Capture_Trace.Write( $"║  DISCONNECT #{S.Disconnect_Count} on {S.Name}" );
      }

      if ( S.Consecutive_Errors == 3 && ! _Is_Shutting_Down ) // ← add check
      {
        Capture_Trace.Write( $"║  3 consecutive errors - attempting port recovery" );
        Reopen_Serial_Port( S.Address );
      }
    }

    private void old_Update_Cycle_Display( bool Continuous, int Total_Cycles )
    {
      // ── Compute everything OFF the UI thread ──────────────────────────
      int Total_Display = _Series.Sum( S => S.Points.Count );
      int Max_Points = _Show_Timing_View ? 0 : ( _Series.Count > 0 ? _Series.Max( s => s.Points.Count ) : 0 );
      bool Rebuild_Legend = _Series.Count != _Last_Legend_Series_Count;
      bool Update_Stats   = ( _Display_Update_Counter % _Legend_Update_Every_N_Cycles ) == 0;

      _Display_Update_Counter = ( _Display_Update_Counter + 1 ) % ( _Legend_Update_Every_N_Cycles * 1000 );

      string Cycle_Text = Continuous ? $"Cycle {_Cycle_Count}  (Continuous)  [{_Actual_FPS} S/s]"
                                     : $"Cycle {_Cycle_Count} of {Total_Cycles}";

      // ── Single Invoke with minimal UI work ────────────────────────────
      this.BeginInvoke( () => // ← BeginInvoke: fire and forget, don't block poll loop
                        {
                          // Always update current values display (fast — just sets label text)
                          Update_Current_Values_Display();

                          // Only update memory status every N cycles
                          if ( Update_Stats )
                            Update_Memory_Status( Total_Display, _Settings.Max_Display_Points );

                          // Only rebuild legend controls when series count changes
                          // Otherwise just update the stats text
                          if ( Rebuild_Legend )
                          {
                            // Build_Legend_Controls ( );
                            _Last_Legend_Series_Count = _Series.Count;
                          }

                          Cycle_Text_Box.Text = Cycle_Text;

                          // Scrollbar update every N cycles is fine — scrolling at 10Hz is smooth enough
                          if ( Update_Stats )
                          {
                            if ( _Show_Timing_View )
                              Update_Timing_Scrollbar();
                            else
                              Update_Data_Scrollbar( Max_Points );
                          }
                        } );
    }

    private void Update_Cycle_Display( bool Continuous, int Total_Cycles )
    {
      // ── Compute everything OFF the UI thread ──────────────────────────
      int Total_Display = _Series.Sum( S => S.Points.Count );
      int Max_Points = _Show_Timing_View ? 0 : ( _Series.Count > 0 ? _Series.Max( s => s.Points.Count ) : 0 );
      bool Rebuild_Legend     = _Series.Count != _Last_Legend_Series_Count;
      bool Update_Stats       = ( _Display_Update_Counter % _Legend_Update_Every_N_Cycles ) == 0;
      _Display_Update_Counter = ( _Display_Update_Counter + 1 ) % ( _Legend_Update_Every_N_Cycles * 1000 );

      Color FPS_Color      = SystemColors.Window;
      Color FPS_Text_Color = SystemColors.WindowText; // default when not Continuous

      // ── Establish baseline after warmup ───────────────────────────────
      if ( _Baseline_Samples < Baseline_Warmup_Cycles )
      {
        _Baseline_Samples++;
      }
      else if ( _Baseline_FPS == 0 && _Actual_FPS > 0 )
      {
        _Baseline_FPS = _Actual_FPS;
      }

      // ── Determine FPS color off UI thread ─────────────────────────────

      if ( Continuous && _Baseline_FPS > 0 && _Actual_FPS > 0 )
      {
        double Ratio = _Actual_FPS / _Baseline_FPS;

        if ( Ratio >= 0.90 )
        {
          FPS_Color      = Color.DarkGreen;
          FPS_Text_Color = Color.White;
        }
        else if ( Ratio >= 0.80 )
        {
          FPS_Color      = Color.Goldenrod; // more readable than pure Yellow
          FPS_Text_Color = Color.Black;
        }
        else
        {
          FPS_Color      = Color.DarkRed;
          FPS_Text_Color = Color.White;
        }
      }

      // string Cycle_Text = Continuous ? $"Cycle {_Cycle_Count}  (Continuous)  [{_Actual_FPS} S/s]"
      //                                : $"Cycle {_Cycle_Count} of {Total_Cycles}";

      string Cycle_Text = Continuous ? $"Cycle {_Cycle_Count}  (Continuous)  [{_Actual_FPS:F1} S/s]"
                                     : $"Cycle {_Cycle_Count} of {Total_Cycles}";

      // ── Single Invoke with minimal UI work ────────────────────────────
      this.BeginInvoke( () =>
                        {
                          // Always update current values display (fast — just sets label text)
                          Update_Current_Values_Display();

                          // Only update memory status every N cycles
                          if ( Update_Stats )
                            Update_Memory_Status( Total_Display, _Settings.Max_Display_Points );

                          // Only rebuild legend controls when series count changes
                          if ( Rebuild_Legend )
                          {
                            // Build_Legend_Controls();
                            _Last_Legend_Series_Count = _Series.Count;
                          }

                          Cycle_Text_Box.Text      = Cycle_Text;
                          Cycle_Text_Box.BackColor = FPS_Color;
                          Cycle_Text_Box.ForeColor = FPS_Text_Color;

                          // Scrollbar update every N cycles is fine
                          if ( Update_Stats )
                          {
                            if ( _Show_Timing_View )
                              Update_Timing_Scrollbar();
                            else
                              Update_Data_Scrollbar( Max_Points );
                          }
                        } );
    }

    private string Get_Command_For_Series(
      Instrument_Series S,
      (string Label, string Cmd_3458, string Cmd_34401, string Cmd_3456, string Cmd_Generic_GPIB, string Unit)
        Entry )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      switch ( S.Type )
      {
        case Meter_Type.HP3458 :
          return Entry.Cmd_3458;
        case Meter_Type.HP34401 :
          return Entry.Cmd_34401;
        case Meter_Type.HP34420 :
          return Entry.Cmd_34401; // same as 34401
        case Meter_Type.HP3456 :
          return Entry.Cmd_3456;
        default :
          return Entry.Cmd_Generic_GPIB;
      }
    }

    private void Stop_Polling()
    {
      using var Block   = Trace_Block.Start_If_Enabled();
      _Is_Shutting_Down = true;
      _Cts?.Cancel();
    }

    // Swallow errors during polling so that Raise_Error
    // does not deadlock by calling MessageBox via Invoke
    // while the UI thread is awaiting Task.Run.
    private void On_Poll_Error( object? Sender, string Message )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      // Log to Progress_Label instead of blocking with a dialog
      if ( InvokeRequired )
      {
        BeginInvoke( () => Show_Progress( $"Error: {Message}", _Foreground_Color ) );
      }
      else
      {
        Show_Progress( $"Error: {Message}", _Foreground_Color );
      }
    }

    private void Finish_Polling( CancellationTokenSource Session_Cts )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( ! ReferenceEquals( Session_Cts, _Cts ) )
      {
        Capture_Trace.Write( "Finish_Polling: new session already running, skipping teardown" );
        return;
      }

      bool Was_Cancelled = _Is_Shutting_Down; // ← capture before reset

      _Chart_Refresh_Timer?.Stop();

      _Is_Running       = false;
      _Is_Shutting_Down = false;

      _Is_Shutting_Down = false;
      _Poll_Error_Shown = false;

      _Cts?.Dispose();
      _Cts                   = null;
      Start_Stop_Button.Text = "Start";
      Show_Progress( "Idle", _Foreground_Color );

      Measurement_Combo.Enabled = true;
      Delay_Numeric.Enabled     = true;
      Continuous_Check.Enabled  = true;
      Cycles_Numeric.Enabled    = ! Continuous_Check.Checked;
      Clear_Button.Enabled      = true;
      Load_Button.Enabled       = true;
      Rolling_Check.Enabled     = true;

      _Chart_Refresh_Timer?.Stop();
      Set_Button_State();

      Run_Auto_Analysis_If_Enabled();
    }

    private void Clear_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // ── Timing view clear (works even while running) ──────────────────
      if ( _Show_Timing_View || ( _Is_Running && _Timing_Count > 0 ) )
      {
        _Timing_Head        = 0;
        _Timing_Count       = 0;
        _Timing_View_Offset = 0;
        _Disconnect_Events.Clear();

        if ( ! _Is_Running )
          _Show_Timing_View = false;

        Chart_Panel.Invalidate();
        return;
      }

      // ── Guard: don't clear instrument data while live polling ─────────
      if ( _Is_Running )
        return;

      // Check if we should prompt
      if ( _Settings.Prompt_Before_Clear && _Series.Any( s => s.Points.Count > 0 ) )
      {
        var Result = MessageBox.Show( "Clear all data?\n\nThis cannot be undone.",
                                      "Clear Data",
                                      MessageBoxButtons.YesNo,
                                      MessageBoxIcon.Question );

        if ( Result != DialogResult.Yes )
          return;
      }

      // ── Reset file-load state so Start works without re-opening the form ──
      _File_Loading = false;
      _Auto_Scroll  = true;
      _View_Offset  = 0;

      foreach ( var S in _Series )
        S.Reset_Stats();

      // Don't block the UI — let GC run on its own schedule
      Task.Run( () =>
                {
                  GC.Collect();
                  GC.WaitForPendingFinalizers();
                } );

      _Cycle_Count        = 0;

      Cycle_Text_Box.Text = "";
      Cycle_Text_Box.BackColor = SystemColors.Window;
      

      Show_Progress( "", _Foreground_Color );

      Update_Graph_Style_Availability();

     

      Set_Button_State();

      Delay_Numeric.Value = Math.Max( Delay_Numeric.Minimum,
                      Math.Min( Delay_Numeric.Maximum, Calculate_Nplc_Min_Ms() ) );

      Clear_Time_Indicators();

      Chart_Panel.Invalidate();
    }



    public void Clear_Time_Indicators()
    {

      Start_Time_TextBox.Clear();
      Stop_Time_TextBox.Clear();
      Total_Time_TextBox.Clear();
    }



    public void Set_Button_State()
    {
      bool Live = _Is_Running;
      bool Rec = _Is_Recording;
      bool Idle = !Live && !Rec;
      bool Has_Data = _Series != null && _Series.Any( s => s.Points.Count > 0 );
      bool Has_Timing = _Timing_Count > 0;
      bool Has_Any = Has_Data || Has_Timing;
      bool Loaded = _File_Loading == false && Has_Data && Idle;
      bool Continuous = Continuous_Check.Checked;

      // ── Always available ──────────────────────────────────────────────────
      Theme_Button.Enabled = true;
      Start_Stop_Button.Enabled = !Rec;
      Load_Button.Enabled = Idle;
      Close_Button.Enabled = !Rec;
      Reset_Errors_Button.Enabled = !Rec;

      // ── Only meaningful once there is something to clear ──────────────────
      Clear_Button.Enabled = Has_Any;

      // ── Configuration: locked once polling starts ─────────────────────────
      Measurement_Combo.Enabled = Idle;
      Capture_Timing_Checkbox.Enabled = Idle;

      // ── Poll Timing: only useful if we have timing data, and not mid-run ──
      Poll_Speed_Button.Enabled = Idle && Has_Timing;

      // ── Rolling / Max Points ──────────────────────────────────────────────
      Rolling_Check.Enabled = true;
      Max_Points_Numeric.Enabled = Rolling_Check.Checked;

      // ── Record ────────────────────────────────────────────────────────────
      Record_Button.Enabled = Live || Rec || Idle;

      // ── Polling controls: configure before starting ───────────────────────
      Continuous_Check.Enabled = Idle;
      Delay_Numeric.Enabled = Idle;

      // ── Cycle controls ────────────────────────────────────────────────────
      Cycles_Label.Enabled = Idle && !Continuous;
      Cycles_Numeric.Enabled = Idle && !Continuous;
      Cycle_Text_Box.Enabled = Has_Any || Live;

      // ── Graph / display ───────────────────────────────────────────────────
      Graph_Style_Combo.Enabled = Has_Any || Live;
      Zoom_Slider.Enabled = Has_Any || Live;

      // ── Combined / Analysis ───────────────────────────────────────────────
      View_Mode_Button.Enabled = Has_Data;
      Analyze_Data_Button.Enabled = Has_Data && Idle;
      Legend_Toggle_Button.Enabled = Has_Data && Idle;

      Close_Button.Enabled = !Live && !Rec;


    }

    // ===== Recording / Loading =====

    private void Record_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Is_Recording )
      {
        _Data_Was_Recorded = true;
        Stop_Recording();
      }
      else
      {
        Start_Recording();
      }
    }

    private void Start_Recording()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Set_Button_State();
      _Record_Query         = Measurement_Combo.Text.Trim();
      _Record_Start         = DateTime.Now;
      _Memory_Warning_Shown = false;

      // ── Session folder ────────────────────────────────────────────────
      string Session_Name = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_Multi";
      string Session_Folder =
        Path.Combine( Multimeter_Common_Helpers_Class.Get_Graph_Captures_Folder( _Settings ), Session_Name );

      Directory.CreateDirectory( Session_Folder );

      // ── Data file ─────────────────────────────────────────────────────
      _Recording_File_Path = Path.Combine( Session_Folder, $"{Session_Name}.csv" );
      _Recording_Writer    = new StreamWriter( _Recording_File_Path, false, System.Text.Encoding.UTF8 ) {
        AutoFlush = false,
      };

      _Recording_Writer.WriteLine( $"# Measurement : {Measurement_Combo.Text.Trim()}" );
      _Recording_Writer.WriteLine( $"# Unit        : {Current_Unit}" );
      string Header = "Timestamp," + string.Join( ",", _Series.Select( s => s.Name ) );
      _Recording_Writer.WriteLine( Header );

      // ── Timing file ───────────────────────────────────────────────────
      _Timing_File_Path = Path.Combine( Session_Folder, $"{Session_Name}_Timing.csv" );
      _Timing_Writer    = new StreamWriter( _Timing_File_Path, false, System.Text.Encoding.UTF8 ) {
        AutoFlush = false,
      };
      _Timing_Writer.WriteLine( "Timestamp,Cycle,Total_Ms,Comm_Ms,AddrSwitch_Ms,UI_Ms,Record_Ms,Had_Error" );

      foreach ( var S in _Series )
        S.Is_Recording = true;

      _Is_Recording = true;
      Update_Performance_Status();
      Multimeter_Common_Helpers_Class.Start_Recording_UI( Record_Button );
      Capture_Trace.Write( $"Session folder    -> {Session_Folder}" );
      Capture_Trace.Write( $"Recording started -> {_Recording_File_Path}" );
      Capture_Trace.Write( $"Timing recording  -> {_Timing_File_Path}" );
    }

    private async void Stop_Recording()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      foreach ( var S in _Series )
        S.Is_Recording = false;
      _Is_Recording = false;

      if ( _Recording_Writer != null )
      {
        await _Recording_Writer.FlushAsync();
        _Recording_Writer.Dispose();
        _Recording_Writer = null;
      }

      if ( _Timing_Writer != null )
      {
        await _Timing_Writer.FlushAsync();
        _Timing_Writer.Dispose();
        _Timing_Writer = null;
      }

      Multimeter_Common_Helpers_Class
        .Stop_Recording( ref _Is_Recording,
                         Record_Button,
                         () =>
                         {
                           if ( _Recording_File_Path == null || ! File.Exists( _Recording_File_Path ) )
                             return 0;
                           try
                           {
                             return File.ReadLines( _Recording_File_Path ).Count() - 1;
                           }
                           catch
                           {
                             return 0;
                           }
                         },
                         Save_Recorded_Data );

      Set_Button_State();

     // Run_Auto_Analysis_If_Enabled(); // ← add this
    }

    private async void old_Stop_Recording()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      foreach ( var S in _Series )
        S.Is_Recording = false;
      _Is_Recording = false;
      if ( _Recording_Writer != null )
      {
        await _Recording_Writer.FlushAsync();
        _Recording_Writer.Dispose();
        _Recording_Writer = null;
      }
      // ── Flush timing file ─────────────────────────────────────────────
      if ( _Timing_Writer != null )
      {
        await _Timing_Writer.FlushAsync();
        _Timing_Writer.Dispose();
        _Timing_Writer = null;
      }
      Multimeter_Common_Helpers_Class
        .Stop_Recording( ref _Is_Recording,
                         Record_Button,
                         () =>
                         {
                           if ( _Recording_File_Path == null || ! File.Exists( _Recording_File_Path ) )
                             return 0;
                           try
                           {
                             return File.ReadLines( _Recording_File_Path ).Count() - 1;
                           }
                           catch
                           {
                             return 0;
                           }
                         },
                         Save_Recorded_Data );

      Set_Button_State();
    }

    private void Save_Recorded_Data()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Recording_File_Path == null || ! File.Exists( _Recording_File_Path ) )
      {
        MessageBox.Show( "No recording file found.",
                         "Recording",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        return;
      }

      // Count lines in file (minus header) for point count
      int Line_Count = 0;
      try
      {
        Line_Count = File.ReadLines( _Recording_File_Path ).Count() - 1;
      }
      catch
      {
      }

      string Summary = $"Recording saved to:\n{_Recording_File_Path}\n\n" +
                       $"Instruments : {_Series.Count}\n" + $"Total cycles: {Line_Count:N0}\n" +
                       $"Duration    : {(DateTime.Now - _Record_Start):hh\\:mm\\:ss}";

      MessageBox.Show( Summary, "Recording Saved", MessageBoxButtons.OK, MessageBoxIcon.Information );

      _Recording_File_Path = null;
    }

  
    private void Analyze_Instrument_Recording_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Series == null || ! _Series.Any() )
      {
        Capture_Trace.Write( "No series data loaded" );
        return;
      }

      Capture_Trace.Write( $"Series count: {_Series.Count}" );
      foreach ( var S in _Series )
        Capture_Trace.Write( $"  Series: '{S.Name}'  Points: {S.Points?.Count ?? -1}" );

      var Instruments =
        _Series.Where( S => S.Points != null && S.Points.Count >= 2 )
          .Select( ( S, Index ) =>
                     new Analysis_Popup_Form.Instrument_Series( _Series.Count( X => X.Name == S.Name ) > 1
                                                                  ? $"{S.Name} #{Index + 1}"
                                                                  : S.Name,
                                                                S.Points.ToList() ) )
          .ToList();

      Capture_Trace.Write( $"Instruments built: {Instruments.Count}" );

      if ( ! Instruments.Any() )
      {
        MessageBox.Show( "No instruments with enough data to analyse.",
                         "Analysis",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Information );
        return;
      }

      var Popup = new Analysis_Popup_Form( Instruments, _Theme ?? Chart_Theme.Dark_Preset(), _Settings );
      Popup.Show( this );
      Popup.Begin_Async_Load();
    }
    private void old_Run_Auto_Analysis_If_Enabled( bool Force = false )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Capture_Trace.Write( $"Run_Auto_Analysis_If_Enabled called — Force={Force}" );

      if ( ! Force && ! _Settings.Auto_Analyze_After_Recording )
      {
        Capture_Trace.Write( "Returning — Auto_Analyze_After_Recording is false" );
        return;
      }

      if ( _Series == null || ! _Series.Any() )
      {
        Capture_Trace.Write( "Returning — _Series is null or empty" );
        return;
      }

      Capture_Trace.Write( $"_Series count: {_Series.Count}" );
      foreach ( var S in _Series )
        Capture_Trace.Write( $"  Series: '{S.Name}'  Points: {S.Points?.Count ?? -1}" );

      var Instruments =
        _Series.Where( S => S.Points != null && S.Points.Count >= 2 )
          .Select( ( S, Index ) =>
                     new Analysis_Popup_Form.Instrument_Series( _Series.Count( X => X.Name == S.Name ) > 1
                                                                  ? $"{S.Name} #{Index + 1}"
                                                                  : S.Name,
                                                                S.Points.ToList() ) )
          .ToList();

      Capture_Trace.Write( $"Instruments built: {Instruments.Count}" );
      foreach ( var Inst in Instruments )
        Capture_Trace.Write( $"  Instrument: '{Inst.Name}'  Points: {Inst.Points.Count}" );

      if ( ! Instruments.Any() )
      {
        Capture_Trace.Write( "Returning — no instruments with enough data" );
        return;
      }

      var Popup = new Analysis_Popup_Form( Instruments, _Theme ?? Chart_Theme.Dark_Preset(), _Settings );
      Popup.Show( this );
      Popup.Begin_Async_Load();
    }


    private void Run_Auto_Analysis_If_Enabled( bool Force = false )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      Capture_Trace.Write( $"Run_Auto_Analysis_If_Enabled called — Force={Force}" );
      if (!Force && !_Settings.Auto_Analyze_After_Recording)
      {
        Capture_Trace.Write( "Returning — Auto_Analyze_After_Recording is false" );
        return;
      }
      if (_Series == null || !_Series.Any())
      {
        Capture_Trace.Write( "Returning — _Series is null or empty" );
        return;
      }
      Capture_Trace.Write( $"_Series count: {_Series.Count}" );
      foreach (var S in _Series)
        Capture_Trace.Write( $"  Series: '{S.Name}'  Points: {S.Points?.Count ?? -1}" );

      var Instruments =
          _Series.Where( S => S.Points != null && S.Points.Count >= 2 )
                 .Select( ( S, Index ) =>
                              new Analysis_Popup_Form.Instrument_Series(
                                  _Series.Count( X => X.Name == S.Name ) > 1
                                      ? $"{S.Name} #{Index + 1}"
                                      : S.Name,
                                  S.Points.ToList() ) )
                 .ToList();

      Capture_Trace.Write( $"Instruments built: {Instruments.Count}" );
      foreach (var Inst in Instruments)
        Capture_Trace.Write( $"  Instrument: '{Inst.Name}'  Points: {Inst.Points.Count}" );

      if (!Instruments.Any())
      {
        Capture_Trace.Write( "Returning — no instruments with enough data" );
        return;
      }

      // ── Resolve master name from cloned instruments ────────────────────
      var Master = _Instruments?.FirstOrDefault( I => I.Is_Master );
      string Master_Raw_Name = Master?.Name;
      Capture_Trace.Write( $"Master raw name: '{Master_Raw_Name ?? "none"}'" );

      // ── Match master to the decorated Instrument_Series name ───────────
      string Master_Name = null;
      if (Master_Raw_Name != null)
      {
        var Master_Series = Instruments.FirstOrDefault( I =>
            I.Name == Master_Raw_Name ||                          // exact match
            I.Name.StartsWith( Master_Raw_Name + " #" ) );       // decorated match e.g. "HP34401A #1"
        Master_Name = Master_Series?.Name;
      }
      Capture_Trace.Write( $"Master resolved to series name: '{Master_Name ?? "none"}'" );

      var Popup = new Analysis_Popup_Form( Instruments, _Theme ?? Chart_Theme.Dark_Preset(), _Settings, Master_Name );
      Popup.Show( this );
      Popup.Begin_Async_Load();
    }









    private void Load_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Is_Running )
      {
        MessageBox.Show( "Stop the current reading before loading.",
                         "Reading in Progress",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        return;
      }

      _Memory_Warning_Shown = false;

      string    Folder = Multimeter_Common_Helpers_Class.Get_Graph_Captures_Folder( _Settings );

      using var Dlg = new OpenFileDialog();
      Dlg.Title     = "Load Recorded Data";
      Dlg.Filter    = "CSV files (*.csv)|*.csv";
      if ( Directory.Exists( Folder ) )
      {
        Dlg.InitialDirectory = Folder;
      }

      if ( Dlg.ShowDialog() != DialogResult.OK )
      {
        return;
      }

      Start_Time_TextBox.Text = string.Empty;
      Stop_Time_TextBox.Text = string.Empty;
      Total_Time_TextBox.Text = string.Empty;


      Load_Recorded_File( Dlg.FileName );
    }

    private async void Load_Recorded_File( string File_Path )
    {
      try
      {
        using var Block = Trace_Block.Start_If_Enabled();

        _Max_Display_Points = (int) Max_Points_Numeric.Value;
        _View_Offset        = 0;
        _Auto_Scroll        = false;

        if ( File_Path.EndsWith( "_Timing.csv", StringComparison.OrdinalIgnoreCase ) )
        {
          _File_Loading = false;
          Load_Timing_File( File_Path );
          return;
        }

        foreach ( var S in _Series )
          S.Reset_Stats();

        _Show_Timing_View = false;
        _File_Loading     = true;

        var Preamble = await Multimeter_Common_Helpers_Class.Load_CSV_Preamble( File_Path );
        if ( Preamble == null )
        {
          _File_Loading = false;
          return;
        }

        // ── Restore measurement type and unit from preamble ───────────
        if (Preamble.Flat_Stats.TryGetValue( "Measurement", out string? Saved_Measurement ))
        {
          Capture_Trace.Write( $"Saved_Measurement: [{Saved_Measurement}]" );
          Capture_Trace.Write( $"Combo current text: [{Measurement_Combo.Text}]" );
          Capture_Trace.Write( $"Items contains: {Measurement_Combo.Items.Contains( Saved_Measurement )}" );
          if (Measurement_Combo.Items.Contains( Saved_Measurement ))
          {
            Measurement_Combo.SelectedItem = Saved_Measurement;
            Capture_Trace.Write( $"Combo set to: [{Measurement_Combo.Text}]" );
          }
          else
          {
            Capture_Trace.Write(
              $"NO MATCH - combo items: [{string.Join( ", ", Measurement_Combo.Items.Cast<string>() )}]" );
          }
        }

        // ── Restore poll start/stop/run times ─────────────────────────
        if (Preamble.Flat_Stats.TryGetValue( "Poll_Start", out string? Poll_Start_Str ) &&
             DateTime.TryParse( Poll_Start_Str, out DateTime Poll_Start ))
        {
          _Start_Time = Poll_Start;
          Start_Time_TextBox.Text = Poll_Start.ToString( "hh:mm:ss tt" );
        }

        if (Preamble.Flat_Stats.TryGetValue( "Poll_Stop", out string? Poll_Stop_Str ) &&
             DateTime.TryParse( Poll_Stop_Str, out DateTime Poll_Stop ))
        {
          _Stop_Time = Poll_Stop;
          Stop_Time_TextBox.Text = Poll_Stop.ToString( "hh:mm:ss tt" );

          if (Start_Time_TextBox.Text != string.Empty)
          {
            TimeSpan Elapsed = _Stop_Time - _Start_Time;
            long Rounded_Ticks = (long) Math.Round( Elapsed.TotalSeconds ) * TimeSpan.TicksPerSecond;
            Total_Time_TextBox.Text = TimeSpan.FromTicks( Rounded_Ticks ).ToString( @"hh\:mm\:ss" );
          }
        }

        string[ ] Lines     = Preamble.Lines;
        int Header_Index    = Preamble.Header_Index;
        var Sectioned_Stats = Preamble.Sectioned_Stats;

        string[ ] Headers = Lines[ Header_Index ].Split( ',' );
        int Col_Count     = Headers.Length - 1;

        if ( Col_Count <= 0 )
        {
          MessageBox.Show( "No instrument columns found.",
                           "Load Error",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Warning );
          _File_Loading = false;
          return;
        }

        int  Data_Line_Count   = Lines.Length - Header_Index - 1;
        bool Show_Progress_Bar = Data_Line_Count > 10_000;
        int  Progress_Interval = Math.Max( 1, Data_Line_Count / 100 );

        // ── Build series from CSV headers ─────────────────────────────
        _Series.Clear();
        var Seen_Names = new Dictionary<string, int>();

        for ( int I = 0; I < Col_Count; I++ )
        {
          string Header_Name = Headers[ I + 1 ].Trim();

          // ── Disambiguate duplicate names ──────────────────────────
          if ( Seen_Names.TryGetValue( Header_Name, out int Name_Count ) )
          {
            Seen_Names[ Header_Name ] = Name_Count + 1;
            Header_Name               = $"{Header_Name} ({Name_Count + 1})";
          }
          else
          {
            Seen_Names[ Header_Name ] = 1;
          }

          var Instr = new Instrument {
            Name       = Header_Name,
            Address    = I,
            Meter_Roll = "Playback",
            Type       = Meter_Type.HP34401,
            Visible    = true,
            NPLC       = 1m,
          };

          var S = new Instrument_Series {
            Instrument = Instr,

            Line_Color = _Theme.Line_Colors[ I % _Theme.Line_Colors.Length ],
            Points     = new List<( DateTime Time, double Value )>( Data_Line_Count ),
            File_Stats = Sectioned_Stats != null && Sectioned_Stats.ContainsKey( Header_Name )
                           ? Sectioned_Stats[ Header_Name ]
                           : null,
          };

          _Series.Add( S );
        }

        // ── Parse data rows ───────────────────────────────────────────
        await Task.Run( () =>
                        {
                          for ( int I = Header_Index + 1; I < Lines.Length; I++ )
                          {
                            string Line = Lines[ I ].Trim();
                            if ( string.IsNullOrEmpty( Line ) || Line.StartsWith( "#" ) )
                              continue;
                            string[ ] Parts = Line.Split( ',' );
                            if ( Parts.Length < 2 || ! DateTime.TryParse( Parts[ 0 ], out DateTime T ) )
                              continue;
                            for ( int J = 1; J < Parts.Length && J - 1 < _Series.Count; J++ )
                            {
                              if ( double.TryParse( Parts[ J ],
                                                    NumberStyles.Float,
                                                    CultureInfo.InvariantCulture,
                                                    out double Val ) )
                              {
                                _Series[ J - 1 ].Points.Add( ( T, Val ) );
                                _Series[ J - 1 ].Add_Point_Value( Val );
                              }
                            }
                            if ( Show_Progress_Bar && ( I % Progress_Interval == 0 ) )
                            {
                              int Percent = ( ( I - Header_Index ) * 100 ) / Data_Line_Count;
                              this.Invoke( () =>
                                             Show_Progress( $"Loading... {Percent}%", _Foreground_Color ) );
                            }
                          }
                        } );

        // ── Post-load UI updates ──────────────────────────────────────
        int   Total = _Series.Sum( s => s.Points.Count );
        Show_Progress( $"Loaded {_Series.Count} instruments, {Total} points", _Foreground_Color );


        // ── Derive start/stop/run from data ──────────────────────────
        var All_Points = _Series.SelectMany( s => s.Points ).ToList();
        if (All_Points.Count > 0)
        {
          DateTime First = All_Points.Min( p => p.Time );
          DateTime Last = All_Points.Max( p => p.Time );
          TimeSpan Elapsed = Last - First;
          long Rounded_Ticks = (long) Math.Round( Elapsed.TotalSeconds ) * TimeSpan.TicksPerSecond;

          Start_Time_TextBox.Text = First.ToString( "hh:mm:ss tt" );
          Stop_Time_TextBox.Text = Last.ToString( "hh:mm:ss tt" );
          Total_Time_TextBox.Text = TimeSpan.FromTicks( Rounded_Ticks ).ToString( @"hh\:mm\:ss" );
        }


        // Create_Legend_Panel ( );   // force checkbox rebuild for new series
        // Update_Legend ( );
        Update_Performance_Status();
        Update_Graph_Style_Availability();

        Multimeter_Common_Helpers_Class.Update_Scrollbar_Range( Pan_Scrollbar,
                                                                _Series.Max( s => s.Points.Count ),
                                                                _Max_Display_Points,
                                                                _Auto_Scroll,
                                                                ref _View_Offset );

        _File_Loading   = false;
        _Data_From_File = true;
        Chart_Panel.Invalidate();
      }
      catch ( Exception Ex )
      {
        _File_Loading = false;
        MessageBox.Show( $"Load failed: {Ex.Message}\n\n{Ex.StackTrace}",
                         "Load Error",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error );
      }
      finally
      {
        Delay_Numeric.Enabled = false;
        Delay_Numeric.Value = Delay_Numeric.Minimum;
   
        Set_Button_State();
      }
    }
    private void Reset_Load_State()
    {
      _File_Loading         = false;
      _Show_Timing_View     = false;
      _Auto_Scroll          = true; // restore default for live reading
      _View_Offset          = 0;
      _Memory_Warning_Shown = false;
    }
    private void Chart_Panel_Paint( object? sender, PaintEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _File_Loading )
        return; // ← don't draw anything while loading

      Multimeter_Common_Helpers_Class.Track_FPS( ref _Paint_Count,
                                                 ref _Actual_FPS,
                                                 _FPS_Stopwatch,
                                                 Update_Performance_Status );

      Graphics G          = e.Graphics;
      G.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

      int       W = Chart_Panel.ClientSize.Width;
      int       H = Chart_Panel.ClientSize.Height;

      using var Bg_Brush = new SolidBrush( _Theme.Background );
      G.FillRectangle( Bg_Brush, 0, 0, W, H );

      // ── Timing view overrides everything else ─────────────────────────
      if ( _Show_Timing_View )
      {
        Draw_Poll_Timing_Chart( G, W, H );
        return;
      }
      // ─────────────────────────────────────────────────────────────────

      if ( _Series.Count == 0 )
      {
        Draw_Empty_State( G, W, H, "No instruments. Add instruments and press Start." );
        return;
      }

      bool Has_Data = _Series.Any( s => s.Visible && s.Points.Count > 0 );
      if ( ! Has_Data )
      {
        Draw_Instrument_List( G, H );
        return;
      }

      if ( _Combined_View || _Current_Graph_Style == "Pie" )
        Draw_Combined_View( G, W, H );
      else
        Draw_Split_View( G, W, H );

      Draw_Position_Indicator( e.Graphics, W, H );
      Capture_Trace.Write( "Paint complete" );
    }

    private List<double> Get_Single_Series_Readings()
    {
      if ( _Series.Count == 0 )
        return new List<double>();
      return _Series[ 0 ].Points.Select( p => p.Value ).ToList();
    }

    private void Theme_Button_Click( object Sender, EventArgs E )
    {
      using var Dlg = new Theme_Settings_Form( _Theme );
      if ( Dlg.ShowDialog( this ) == DialogResult.OK )
      {
        _Theme.Copy_From( Dlg.Result );
        _Theme.Save();
        Chart_Panel.BackColor = _Theme.Background;

        for ( int I = 0; I < _Series.Count; I++ )
          _Series[ I ].Line_Color = _Theme.Line_Colors[ I % _Theme.Line_Colors.Length ];

        _Settings.Set_Theme( _Theme );

        // ── Rebuild pre-allocated GDI resources with new theme colors ─
        Dispose_Chart_Resources();
        Initialize_Chart_Resources();

        Chart_Panel.Invalidate();
      }
    }

    private void Initialize_Status_Panel()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      ( _Memory_Status_Label, _Performance_Status_Label ) =
        Multimeter_Common_Helpers_Class.Initialize_Status_Strip( this, _Settings, _Series.Count );

      Update_Memory_Status( 0, _Settings.Max_Data_Points_In_Memory );
    }

    private void Update_Error_Status( string instrument, string error )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Truncate long error messages
      string    Short_Error = error.Length > 50 ? error.Substring( 0, 47 ) + "..." : error;

      int       Error_Count = _Error_Counts.ContainsKey( instrument ) ? _Error_Counts[ instrument ] : 0;

      Show_Progress( $"Error on {instrument} ({Error_Count} errors): {Short_Error}", _Foreground_Color );
    }

    // Add a manual reset errors button
    private void Reset_Errors_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Error_Counts.Clear();
      foreach ( var Series in _Series )
      {
        _Error_Counts[ Series.Name ] = 0;
        Series.Visible               = true;
      }

      Show_Progress( "Error counts reset - all instruments enabled", _Foreground_Color );

      // Update_Legend ( );
    }

    private void Save_To_File( string File_Path )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Series.Count == 0 || _Series.All( s => s.Points.Count == 0 ) )
        return;

      using var Writer = new StreamWriter( File_Path );

      // Header
      Writer.WriteLine( $"# Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" );
      Writer.WriteLine( $"# Instruments: {_Series.Count}" );

      int Total_Points = _Series.Sum( s => s.Points.Count );
      Writer.WriteLine( $"# Total Points: {Total_Points}" );
      Writer.WriteLine( "#" );

      // Statistics for each instrument
      Writer.WriteLine( "# Statistics:" );
      foreach ( var S in _Series )
      {
        if ( S.Points.Count == 0 )
          continue;

        // Calculate stats
        double min    = S.Get_Min();
        double max    = S.Get_Max();
        double avg    = S.Get_Average();
        double stdDev = S.Get_StdDev();
        double range  = S.Get_Range();
        double last   = S.Get_Last();

        Writer.WriteLine( $"# [{S.Name}]" );
        Writer.WriteLine( $"#   Points: {S.Points.Count}" );
        Writer.WriteLine( $"#   Last: {last:F6}" );
        Writer.WriteLine( $"#   Average: {avg:F6}" );
        Writer.WriteLine( $"#   Std Dev: {stdDev:F6}" );
        Writer.WriteLine( $"#   Min: {min:F6}" );
        Writer.WriteLine( $"#   Max: {max:F6}" );

        // Duration and rate
        if ( S.Points.Count >= 2 )
        {
          TimeSpan duration = S.Points[ S.Points.Count - 1 ].Time - S.Points[ 0 ].Time;
          double   rate     = S.Get_Sample_Rate();

          Writer.WriteLine( $"#   Duration: {Multimeter_Common_Helpers_Class.Format_Time_Span(duration)}" );
          Writer.WriteLine( $"#   Rate: {rate:F2} S/s" );

          // Average interval
          double totalMs = 0;
          for ( int i = 1; i < S.Points.Count; i++ )
          {
            totalMs += ( S.Points[ i ].Time - S.Points[ i - 1 ].Time ).TotalMilliseconds;
          }
          double avgInterval = totalMs / ( S.Points.Count - 1 );
          Writer.WriteLine( $"#   Avg Δt: {avgInterval:F1} ms" );
        }
      }

      Writer.WriteLine( "#" );

      // Build column headers
      Writer.Write( "Timestamp" );
      foreach ( var S in _Series )
      {
        Writer.Write( $",{S.Name}" );
      }
      Writer.WriteLine();

      // Find all unique timestamps across all series
      var All_Timestamps = new SortedSet<DateTime>();
      foreach ( var S in _Series )
      {
        foreach ( var P in S.Points )
        {
          All_Timestamps.Add( P.Time );
        }
      }

      // Write data rows
      foreach ( var Time in All_Timestamps )
      {
        Writer.Write( $"{Time:yyyy-MM-dd HH:mm:ss.fff}" );

        foreach ( var S in _Series )
        {
          // Find value at this timestamp
          var Point = S.Points.FirstOrDefault( p => p.Time == Time );
          if ( Point != default )
          {
            Writer.Write( $",{Point.Value.ToString(CultureInfo.InvariantCulture)}" );
          }
          else
          {
            Writer.Write( "," ); // Empty cell if no data
          }
        }
        Writer.WriteLine();
      }
    }

    // Update your existing Save button click handler to use this
    private void Save_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Series.Count == 0 || _Series.All( s => s.Points.Count == 0 ) )
      {
        MessageBox.Show( "No data to save.", "Save Data", MessageBoxButtons.OK, MessageBoxIcon.Information );
        return;
      }

      string    Folder = _Settings.Default_Save_Folder;

      using var Dlg        = new SaveFileDialog();
      Dlg.Title            = "Save Recorded Data";
      Dlg.Filter           = "CSV files (*.csv)|*.csv";
      Dlg.InitialDirectory = Folder;

      // Generate default filename from pattern
      string Default_Name = _Settings.Filename_Pattern;
      Default_Name        = Default_Name.Replace( "{date}", DateTime.Now.ToString( "yyyy-MM-dd" ) );
      Default_Name        = Default_Name.Replace( "{time}", DateTime.Now.ToString( "HH-mm-ss" ) );
      Default_Name        = Default_Name.Replace( "{function}", "Multi" );

      if ( ! Default_Name.EndsWith( ".csv", StringComparison.OrdinalIgnoreCase ) )
        Default_Name += ".csv";

      Dlg.FileName = Default_Name;

      if ( Dlg.ShowDialog() != DialogResult.OK )
        return;

      try
      {
        Save_To_File( Dlg.FileName );

        // Summary message
        int    Total_Points = _Series.Sum( s => s.Points.Count );
        string Summary =
          $"Saved {_Series.Count} instruments, {Total_Points} total points to:\n{Dlg.FileName}\n\n";

        foreach ( var S in _Series )
        {
          if ( S.Points.Count > 0 )
          {
            double avg  = S.Get_Average();
            Summary    += $"{S.Name}: {S.Points.Count} pts, Avg: {avg:F6}\n";
          }
        }

        MessageBox.Show( Summary, "Recording Saved", MessageBoxButtons.OK, MessageBoxIcon.Information );
      }
      catch ( Exception ex )
      {
        MessageBox.Show( $"Failed to save file:\n{ex.Message}",
                         "Save Error",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error );
      }
    }

    private void Apply_Settings()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // ===== DISPLAY SETTINGS =====

      // Tooltip settings
      if ( _Chart_Tooltip != null )
      {
        _Chart_Tooltip.AutoPopDelay = _Settings.Tooltip_Display_Duration_Ms;
        // Tooltip distance is checked in Chart_Panel_MouseMove
      }

      // Chart refresh rate
      _Chart_Refresh_Timer.Interval = _Settings.Chart_Refresh_Rate_Ms;
      Update_Chart_Refresh_Rate();

      // Default max display points
      _Max_Display_Points      = _Settings.Max_Display_Points;
      Max_Points_Numeric.Value = _Settings.Max_Display_Points;

      // View mode defaults (only apply if no data yet)
      if ( _Series.All( s => s.Points.Count == 0 ) )
      {
        _Combined_View   = _Settings.Default_To_Combined_View;
        _Normalized_View = _Settings.Default_To_Normalized_View;

        View_Mode_Button.Text    = _Combined_View ? "Split View" : "Combined View";
        Normalize_Button.Text    = _Normalized_View ? "Absolute" : "Normalize";
        Normalize_Button.Visible = _Combined_View;
      }

      // ===== POLLING SETTINGS =====
      if (!_Is_Running)
      {
        // Recalculate the NPLC minimum fresh — don't trust the saved value
        int Nplc_Min_Ms = _Series
      .Sum( s => (int) ((double) (s.NPLC) / 60.0 * 1000) + 20 )
      + 50;

        // Use whichever is larger: the NPLC floor or the user's saved preference
        decimal Delay_Ms = Math.Max( Nplc_Min_Ms,
                           Math.Max( Delay_Numeric.Minimum,
                           Math.Min( Delay_Numeric.Maximum, _Settings.Default_Poll_Delay_Ms ) ) );

        Delay_Numeric.Value = Delay_Ms;
      }
      // NPLC is now per-instrument via S.NPLC — no global textbox to update.

      // Default measurement type
      if ( Measurement_Combo.Items.Contains( _Settings.Default_Measurement_Type ) )
      {
        Measurement_Combo.SelectedItem = _Settings.Default_Measurement_Type;
      }

      // Default continuous polling
      Continuous_Check.Checked = _Settings.Default_Continuous_Poll;

      // ===== FILE SETTINGS =====

      // Save folder
      if ( ! string.IsNullOrEmpty( _Settings.Default_Save_Folder ) )
      {
        try
        {
          Directory.CreateDirectory( _Settings.Default_Save_Folder );
        }
        catch
        {
        }
      }

      // Auto-save timer
      if ( _Settings.Enable_Auto_Save )
      {
        _Auto_Save_Timer.Interval = _Settings.Auto_Save_Interval_Minutes * 60 * 1000;
        if ( _Is_Running )
          _Auto_Save_Timer.Start();
      }
      else
      {
        _Auto_Save_Timer.Stop();
      }

      // ===== UI SETTINGS =====

      // Window title
      if ( ! string.IsNullOrWhiteSpace( _Settings.Default_Window_Title ) )
      {
        this.Text = $"{_Settings.Default_Window_Title} - Multi-Poll ({_Series.Count} instruments)";
      }

      // ===== ZOOM SETTINGS =====

      // Default zoom level
      if ( Zoom_Slider != null )
      {
        Zoom_Slider.Value = _Settings.Default_Zoom_Level;
        Update_Zoom_From_Slider();
      }

      // ===== TRIGGER UPDATES =====

      Multimeter_Common_Helpers_Class.Update_Scrollbar_Range( Pan_Scrollbar,
                                                              _Series.Max( s => s.Points.Count ),
                                                              _Max_Display_Points,
                                                              _Auto_Scroll,
                                                              ref _View_Offset );
      // Update_Legend ( );
      Chart_Panel.Invalidate();

      Capture_Trace.Write( "Settings applied successfully" );
    }

    private void Start_Stop_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Reset_Load_State(); // ← ensure clean state regardless of what happened before

      Capture_Trace.Write( $"_Is_Running = {_Is_Running}" );

     

      if ( ! _Is_Running )
      {
        _Is_Running     = true; // ← set immediately, before async starts
        _Data_From_File = false;
       
        Set_Button_State();
        Capture_Trace.Write( "Starting polling..." );

        // Start chart refresh timer
        _Chart_Refresh_Timer.Start();
        Capture_Trace.Write(
          $"Timer enabled: {_Chart_Refresh_Timer.Enabled}, Interval: {_Chart_Refresh_Timer.Interval}" );

        // Start auto-save if enabled
        if ( _Settings.Enable_Auto_Save )
          _Auto_Save_Timer.Start();

        On_Start();

        // Start async polling loop directly
        Start_Polling(); // ← this is all you need
      }
      else
      {
        // Set_Button_State ( );
        Capture_Trace.Write( "Stopping polling..." );
        Stop_Polling(); // ← cancels the CTS
       
        _Chart_Refresh_Timer.Stop();
        _Auto_Save_Timer.Stop();

        if ( _Settings.Auto_Save_On_Stop && _Series.Any( s => s.Points.Count > 0 ) )
          Auto_Save_Timer_Tick( null, null );

        Chart_Panel.Invalidate();
        Multimeter_Common_Helpers_Class.Check_Memory_Limit( _Settings,
                                                            () => _Series.Sum( s => s.Points.Count ),
                                                            () =>
                                                            {
                                                              Stop_Recording();
                                                              if ( InvokeRequired )
                                                                this.Invoke(
                                                                  () => Update_Graph_Style_Availability() );
                                                              else
                                                                Update_Graph_Style_Availability();
                                                            },
                                                            Show_Memory_Warning,
                                                            ref _Memory_Warning_Shown );
        On_Stop();

        Set_Button_State();
        Update_Performance_Status();
      }
    }

    private void Chart_Panel_Mouse_Wheel( object sender, MouseEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( ! _Enable_Rolling )
        return;

      // Zoom in/out by 10 points per scroll
      int Delta     = e.Delta > 0 ? 10 : -10;
      int New_Value = _Max_Display_Points + Delta;

      // Clamp between 10 and total points
      int Total_Points = _Series.Max( s => s.Points.Count );
      New_Value        = Math.Max( 10, Math.Min( New_Value, Total_Points ) );

      _Max_Display_Points      = New_Value;
      Max_Points_Numeric.Value = New_Value;

      Capture_Trace.Write( $"Zoomed to {_Max_Display_Points} points" );
      Chart_Panel.Invalidate();
    }

    // Add keyboard support for panning:

    private void Draw_Position_Indicator( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( ! _Enable_Rolling )
        return;

      int Total_Points = _Series.Max( s => s?.Points?.Count ?? 0 );
      if ( Total_Points <= _Max_Display_Points )
        return;

      // Draw a small indicator showing current position
      int Indicator_Y = H - 45;
      int Indicator_W = 200;
      int Indicator_H = 20;
      int Indicator_X = W - Indicator_W - 20;

      // Background
      using ( var Bg_Brush = new SolidBrush( Color.FromArgb( 200, _Theme.Background ) ) )
      {
        G.FillRectangle( Bg_Brush, Indicator_X, Indicator_Y, Indicator_W, Indicator_H );
      }

      // Border
      using ( var Border_Pen = new Pen( _Theme.Grid, 1f ) )
      {
        G.DrawRectangle( Border_Pen, Indicator_X, Indicator_Y, Indicator_W, Indicator_H );
      }

      // Position bar
      int Bar_W      = (int) ( (double) _Max_Display_Points / Total_Points * Indicator_W );
      int Max_Offset = Total_Points - _Max_Display_Points;
      int Bar_X =
        Indicator_X + (int) ( ( 1.0 - (double) _View_Offset / Max_Offset ) * ( Indicator_W - Bar_W ) );

      using ( var Bar_Brush = new SolidBrush( Color.FromArgb( 150, Color.LightBlue ) ) )
      {
        G.FillRectangle( Bar_Brush, Bar_X, Indicator_Y + 2, Bar_W, Indicator_H - 4 );
      }

      // Text
      string Position_Text = _Auto_Scroll ? "Live" : $"-{_View_Offset} pts";
      using ( var Text_Font = new Font( "Segoe UI", 8F ) ) using ( var Text_Brush =
                                                                     new SolidBrush( _Theme.Foreground ) )
      {
        var Text_Size = G.MeasureString( Position_Text, Text_Font );
        G.DrawString( Position_Text,
                      Text_Font,
                      Text_Brush,
                      Indicator_X + ( Indicator_W - Text_Size.Width ) / 2,
                      Indicator_Y + ( Indicator_H - Text_Size.Height ) / 2 );
      }
    }

    private Task Set_Local_Mode( int Address, Meter_Type Type )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if ( ! _Comm.Is_Connected )
      {
        Capture_Trace.Write( "Not connected, skipping" );
        return Task.CompletedTask;
      }
      try
      {
        if ( _Comm.Mode == Connection_Mode.Prologix_GPIB )
        {
          Capture_Trace.Write( $"Selecting GPIB address {Address}" );
          _Comm.Send_Prologix_Command( $"++addr {Address}" );
        }
        switch ( Type )
        {
          case Meter_Type.HP34401 :
          case Meter_Type.HP33120 :
            Capture_Trace.Write( "Sending CLS?" );
            _Comm.Send_Instrument_Command( "CLS?" );
            break;
          case Meter_Type.HP3458 :
            Capture_Trace.Write( "3458 - GTL handled by ++loc only" );
            Capture_Trace.Write( "skipping instrument command" );
            break;
        }
        if ( _Comm.Mode == Connection_Mode.Prologix_GPIB )
        {
          Capture_Trace.Write( $"Sending ++loc to Prologix for address {Address}" );
          _Comm.Send_Prologix_Command( "++loc" );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write( $"Exception: [{Address}] : {Ex.Message}" );
      }
      return Task.CompletedTask;
    }

    private async Task Perform_Close_Operations( string Context )
    {
      Capture_Trace.Write( "Beginning graceful shutdown" );

      // 1. Stop active polling gracefully
      if ( _Is_Running )
      {
        Capture_Trace.Write( "Cancelling active polling" );
        _Cts?.Cancel();

        // Wait for the in-flight read to complete/timeout
        // rather than yanking the rug out
        int Wait_Ms = 0;
        while ( _Is_Running && Wait_Ms < 5000 )
        {
          await Task.Delay( 100 );
          Wait_Ms += 100;
        }

        if ( _Is_Running )
        {
          Capture_Trace.Write( "WARNING - polling did not stop within 5s, forcing" );
        }
        else
        {
          Capture_Trace.Write( "Polling stopped cleanly" );
        }
      }

      // 2. Stop recording and save if active
      if ( _Is_Recording )
      {
        Capture_Trace.Write( "Stopping active recording" );
        Stop_Recording();
      }

      // 3. Stop timers
      Capture_Trace.Write( "Stopping timers" );
      _Chart_Refresh_Timer?.Stop();
      _Auto_Save_Timer?.Stop();

      // 4. Flush serial buffer (discard any in-flight response bytes)
      try
      {
        if ( _Comm.Is_Connected )
        {
          Capture_Trace.Write( "Flushing serial buffers" );
          _Comm.Flush_Buffers();
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write( $"Flush error (non-fatal): {Ex.Message}" );
      }

      // 5. Return all instruments to local control
      Capture_Trace.Write( "Setting local mode" );
      Capture_Trace.Write( "Setting local mode for all instruments" );

      foreach ( var Series in _Series )
      {
        Capture_Trace.Write( $"Releasing {Series.Name} at GPIB {Series.Address}" );
        await Set_Local_Mode( Series.Address, Series.Type );
      }

      // 6. Dispose CTS
      _Cts?.Dispose();
      _Cts = null;
    }

    private async void Close_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Disable the button immediately to prevent double-clicks
      if ( Sender is Button Btn )
        Btn.Enabled = false;

      await Perform_Close_Operations( "Closing" );

      Capture_Trace.Write( "Shutdown complete, closing form" );
      this.Close();
    }

    private void Query_Instrument_Name()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Capture_Trace.Write( $"Is_Connected = {_Comm.Is_Connected}" );
      Capture_Trace.Write( $"Mode         = {_Comm.Mode}" );
      Capture_Trace.Write( $"Meter        = {_Selected_Meter}" );

      if ( ! _Comm.Is_Connected || _Comm.Mode != Connection_Mode.Prologix_GPIB )
      {
        Capture_Trace.Write( "Skipping - not connected or not GPIB" );
        return;
      }
      try
      {
        string IDN;
        if ( _Selected_Meter == Meter_Type.HP3458 )
        {
          Capture_Trace.Write( "Sending 'ID' to 3458" );
          _Comm.Send_Instrument_Command( "ID?" );
          Thread.Sleep( _Settings.Instrument_Settle_Ms );

          using var Cts = new CancellationTokenSource( TimeSpan.FromSeconds( 3 ) );
          Capture_Trace.Write( "Reading response..." );
          IDN = _Comm.Read_Instrument( Cts.Token );
          Capture_Trace.Write( $"Got response: [{IDN}]" );
        }
        else
        {
          Capture_Trace.Write( "Sending '*IDN?' to non-3458" );
          IDN = _Comm.Query_Instrument( "*IDN?" );
          Capture_Trace.Write( $"Got response: [{IDN}]" );
        }
        if ( ! string.IsNullOrWhiteSpace( IDN ) )
        {
          // string [ ] Parts = IDN.Split ( ',' );
          // _Instrument_Name = Parts.Length >= 2 ? Parts [ 1 ].Trim ( ) : IDN.Trim ( );
          // Capture_Trace.Write ( $"Parsed instrument name: [{_Instrument_Name}]" );
          Capture_Trace.Write( $"Name: [{IDN}]" );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write( $"EXCEPTION: {Ex.Message}" );
      }
    }

    private void Populate_Measurement_Combo()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Unsubscribe to prevent commands firing during population
      Measurement_Combo.SelectedIndexChanged -= Measurement_Combo_Changed;

      Measurement_Combo.Items.Clear();
      _Filtered_Indices.Clear();

      for ( int I = 0; I < _Measurements.Length; I++ )
      {
        string Cmd = _Series.Count > 0 && _Series[ 0 ].Type == Meter_Type.HP34401
                       ? _Measurements[ I ].Cmd_34401
                       : _Measurements[ I ].Cmd_3458;
        if ( ! string.IsNullOrEmpty( Cmd ) )
        {
          _Filtered_Indices.Add( I );
          Measurement_Combo.Items.Add( _Measurements[ I ].Label );
        }
      }

      // Default to DC Voltage if available, otherwise first item
      int Dc_Index = Measurement_Combo.Items.IndexOf( "DC Voltage" );
      if ( Dc_Index >= 0 )
        Measurement_Combo.SelectedIndex = Dc_Index;
      else if ( Measurement_Combo.Items.Count > 0 )
        Measurement_Combo.SelectedIndex = 0;

      // Re-subscribe now that population is complete
      Measurement_Combo.SelectedIndexChanged += Measurement_Combo_Changed;
    }

    private void Measurement_Combo_Changed( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _File_Loading )
        return;

      if ( Measurement_Combo.SelectedItem == null )
        return;

      string Selected = Measurement_Combo.SelectedItem.ToString();
      Capture_Trace.Write( $"Measurement changed to: {Selected}" );

      Show_Progress( $"Measurement changed to: {Selected}", _Foreground_Color );

      if ( ! _Comm.Is_Connected )
      {
        Capture_Trace.Write( "Not connected, skipping configuration" );
        return;
      }

      foreach ( var S in _Series )
      {
        try
        {
          int    Filtered_Index = _Filtered_Indices[ Measurement_Combo.SelectedIndex ];
          var    Entry          = _Measurements[ Filtered_Index ];

          string Command = Get_Command_For_Series( S, Entry ); // ← replaces the two-type switch

          if ( string.IsNullOrEmpty( Command ) )
          {
            Capture_Trace.Write( $"  {S.Name} has no command for {Selected}, skipping" );
            continue;
          }

          Capture_Trace.Write( $"Configuring {S.Type} {S.Name} @ {S.Address} with [{Command}]" );
          _Comm.Change_GPIB_Address( S.Address );
          _Comm.Send_Instrument_Command( Command );
          Thread.Sleep( 50 );
          Capture_Trace.Write( $"  {S.Name} configured successfully" );
        }
        catch ( Exception Ex )
        {
          Capture_Trace.Write( $"  ERROR configuring {S.Name}: {Ex.Message}" );
          MessageBox.Show( $"Error configuring {S.Name} for {Selected}:\n{Ex.Message}",
                           "Configuration Error",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Warning );
        }
      }
      Capture_Trace.Write( "All instruments configured" );
    }

    private void Update_Current_Values_Display()
    {

      using var Block = Trace_Block.Start_If_Enabled();

    
      if ( Current_Values_Panel.Controls.Count == 0 )
        return;

      foreach ( var S in _Series )
      {
        int Consecutive = S.Consecutive_Errors;
        int Total       = S.Total_Errors;

        var Value_Label = Current_Values_Panel.Controls[ $"Value_{S.Address}" ] as Label;
        var Time_Label  = Current_Values_Panel.Controls[ $"Time_{S.Address}" ] as Label;
        var Error_Label = Current_Values_Panel.Controls[ $"Error_{S.Address}" ] as Label;

        // --- Error label (always update, regardless of point count) ---
        if ( Error_Label != null )
        {
         
          string Error_Text  = $"Err:{Total:D3}";
          Color  Error_Color = Consecutive > 0 ? Color.Red : Color.Green;

          if ( Error_Label.Text != Error_Text )
            Error_Label.Text = Error_Text;
          if ( Error_Label.ForeColor != Error_Color )
            Error_Label.ForeColor = Error_Color;
        }
        else
        {
          Capture_Trace.Write( $"Error_Label not found for address {S.Address}" );
        }

        if ( S.Points.Count == 0 )
          continue;

        var      Last_Point = S.Points[ S.Points.Count - 1 ];
        double   Latest     = Last_Point.Value;
        DateTime Timestamp  = Last_Point.Time;
        TimeSpan Age        = DateTime.Now - Timestamp;

        string   Display =
          Multimeter_Common_Helpers_Class.Format_Value( Latest, Current_Unit, S.Type, S.Display_Digits );

        if ( Value_Label != null )
        {
          Capture_Trace.Write( $"Value_Label found - Display: '{Display}' Current_Unit: '{Current_Unit}'" );
          if ( Value_Label.Text != Display )
            Value_Label.Text = Display;
          if ( Value_Label.ForeColor != Color.Green )
            Value_Label.ForeColor = Color.Green;
        }

        if ( Time_Label != null )
        {
          bool   Is_Stale = Age.TotalSeconds > _Settings.Stale_Data_Threshold_Seconds;
          bool   Has_Skew = Age.TotalSeconds > _Settings.Skew_Warning_Threshold_Seconds;

          Color  Target_Color = Is_Stale ? Color.Red : Has_Skew ? Color.Orange : Color.Green;

          string Target_Text = $"[{Timestamp:HH:mm:ss.fff}]";

          if ( Time_Label.Text != Target_Text )
            Time_Label.Text = Target_Text;
          if ( Time_Label.ForeColor != Target_Color )
            Time_Label.ForeColor = Target_Color;
        }
      }
    }

    private void Initialize_Current_Values_Display()
    {
      Current_Values_Panel.Controls.Clear();
      Current_Values_Panel.BackColor  = _Theme.Background;
      Current_Values_Panel.AutoScroll = true;
      Current_Values_Panel.SuspendLayout();

      int Y = 5;
      foreach ( var S in _Series )
      {
        var Dot = new Label {
          Name      = $"Dot_{S.Address}",
          Text      = "●",
          Location  = new Point( 5, Y ),
          Size      = new Size( 12, 18 ),
          Font      = new Font( "Consolas", 8f ),
          ForeColor = S.Line_Color,
          AutoSize  = false,
        };
        var Name_Label = new Label {
          Name      = $"Name_{S.Address}",
          Text      = $"{S.Name} @{S.Address}", // combine name + address
          Location  = new Point( 18, Y ),
          Size      = new Size( 110, 18 ),
          Font      = new Font( "Consolas", 7.5f ),
          ForeColor = _Theme.Labels,
          AutoSize  = false,
        };
        var Time_Label = new Label {
          Name      = $"Time_{S.Address}",
          Text      = "",
          Location  = new Point( 130, Y ),
          Size      = new Size( 110, 18 ),
          Font      = new Font( "Consolas", 7.5f ),
          ForeColor = Color.Green,
          AutoSize  = false,
        };
        var Error_Label = new Label {
          Name      = $"Error_{S.Address}",
          Text      = "Err:000",
          Location  = new Point( 242, Y ),
          Size      = new Size( 65, 18 ),
          Font      = new Font( "Consolas", 7.5f ),
          ForeColor = Color.Green,
          AutoSize  = false,
        };

        var Value_Label = new Label {
          Name      = $"Value_{S.Address}",
          Text      = "---",
          Location  = new Point( 18, Y + 20 ),
          Size      = new Size( 290, 20 ),
          Font      = new Font( "Consolas", 9f, FontStyle.Bold ),
          ForeColor = _Theme.Foreground,
          AutoSize  = false,
        };

        Current_Values_Panel.Controls.Add( Dot );
        Current_Values_Panel.Controls.Add( Name_Label );
        Current_Values_Panel.Controls.Add( Time_Label );
        Current_Values_Panel.Controls.Add( Error_Label );
        Current_Values_Panel.Controls.Add( Value_Label );

        Y += 42; // two rows: header row + value row
      }

      Current_Values_Panel.ResumeLayout();
    }

    private void Apply_Theme_To_Current_Values_Panel()
    {
      Current_Values_Panel.BackColor = _Theme.Background;

      foreach ( Control C in Current_Values_Panel.Controls )
      {
        if ( C is Label L )
        {
          // Dot labels keep their series color
          if ( L.Name.StartsWith( "Dot_" ) )
            continue;

          // Name labels use Labels color
          if ( L.Name.StartsWith( "Name_" ) )
            L.ForeColor = _Theme.Labels;

          // Value labels use Foreground color
          if ( L.Name.StartsWith( "Value_" ) )
            L.ForeColor = _Theme.Foreground;
        }
      }
    }

    private void Delay_Numeric_ValueChanged( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Recalculate floor based on current series
      int Nplc_Min_Ms = _Series
          .Sum( s => (int) ((double) (s.NPLC) / 60.0 * 1000) + 20 )
          + 50;

      // Clamp upward if below minimum
      if ((int) Delay_Numeric.Value < Nplc_Min_Ms)
      {
        Delay_Numeric.Value = Nplc_Min_Ms; // will re-trigger this event, now clamped
        return;
      }

      _Settings.Default_Poll_Delay_Ms = (int) Delay_Numeric.Value;
      _Settings.Save();
    }

    private void Flush_Comm()
    {
      try
      {
        _Comm.Send_Prologix_Command( "++clr" );
        Thread.Sleep( 100 );
        _Comm.Send_Prologix_Command( "++auto 0" );
        Thread.Sleep( 100 );
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write( $"Flush error: {Ex.Message}" );
      }
    }

    private void Flush_Serial_Port()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      try
      {
        if ( _Comm.Is_Connected )
        {
          _Comm.Discard_IO_Buffers();

          Thread.Sleep( 200 );
          // Send a device clear to unstick the instrument
          _Comm.Send_Prologix_Command( "++clr" );
          Thread.Sleep( 100 );
        }
      }
      catch
      { /* ignore errors during flush */
      }
    }

    private void Reopen_Serial_Port( int GPIB_Address )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      try
      {
        if ( _Comm.Is_Connected )
        {
          _Comm.Disconnect_Async();
          Thread.Sleep( 500 );
        }
        _Comm.Connect();
        Thread.Sleep( 200 );
        // Re-init Prologix after reopen
        _Comm.Send_Prologix_Command( "++mode 1" );
        _Comm.Send_Prologix_Command( "++savecfg 0" ); // ← add here too
        _Comm.Send_Prologix_Command( "++auto 0" );
        _Comm.Send_Prologix_Command( $"++addr {GPIB_Address}" );
        Thread.Sleep( 100 );
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write( $"Port reopen failed: {Ex.Message}" );
      }
    }

    private void Write_Recording_Row()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( ! _Is_Recording )
        return;

      string Timestamp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.ffffff" );

      // ── Instrument data (existing) ────────────────────────────────────
      if ( _Recording_Writer != null )
      {
        var Values = _Series.Select(
          S => S.Points.Count > 0 ? S.Points[ S.Points.Count - 1 ].Value.ToString( "G10" ) : "" );
        _Recording_Writer.WriteLine( $"{Timestamp},{string.Join(",", Values)}" );
      }

      // ── Timing data (new) ─────────────────────────────────────────────
      if ( _Capture_Timing && _Timing_Writer != null && _Timing_Count > 0 )
      {
        int Idx = ( _Timing_Head - 1 + _Timing_Buffer_Size ) % _Timing_Buffer_Size;
        var S   = _Cycle_Timing[ Idx ];
        _Timing_Writer.WriteLine( $"{Timestamp}," + $"{_Cycle_Count}," + $"{S.Total_Ms:F1}," +
                                  $"{S.Comm_Ms:F1}," + $"{S.Address_Switch_Ms:F1}," + $"{S.UI_Ms:F1}," +
                                  $"{S.Record_Ms:F1}," + $"{(S.Had_Error ? "1" : "0")}" );
      }

      // ── Periodic flush (both files) ───────────────────────────────────
      if ( _Cycle_Count % 100 == 0 )
      {
        _ = Task.Run( async () =>
                      {
                        await _Recording_Writer?.FlushAsync();
                        await _Timing_Writer?.FlushAsync();
                      } );
      }

      Multimeter_Common_Helpers_Class.Check_Memory_Limit( _Settings,
                                                          () => _Series.Sum( S => S.Points.Count ),
                                                          () => this.Invoke( Stop_Recording ),
                                                          ( Current, Max ) => this.Invoke(
                                                            () => Show_Memory_Warning( Current, Max ) ),
                                                          ref _Memory_Warning_Shown );
    }

    // ─── Logging Helpers ──────────────────────────────────────────────────────────

    private void Log_Cycle_Header( int Cycle_Count )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Capture_Trace.Write( $"" );
      Capture_Trace.Write( $"╔═══════════════════════════════════════╗" );
      Capture_Trace.Write( $"║  CYCLE {Cycle_Count}" );
      Capture_Trace.Write( $"╠═══════════════════════════════════════╣" );
    }

    private void Log_Cycle_Footer()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Capture_Trace.Write( $"╚═══════════════════════════════════════╝" );
    }

    private void Restore_GPIB_Address( int Address )
    {
      try
      {
        _Comm.Change_GPIB_Address( Address );
      }
      catch
      {
      }
    }

    private void Record_Cycle_Timing( DateTime Time, double Duration_Ms, bool Had_Error )
    {
      if ( ! _Capture_Timing )
        return;

      Record_Cycle_Timing( Time, Duration_Ms, 0, 0, 0, 0, Had_Error );
    }

    private void Record_Cycle_Timing( DateTime Time,
                                      double   Duration_Ms,
                                      double   Comm_Ms,
                                      double   Addr_Ms,
                                      double   UI_Ms,
                                      double   Record_Ms,
                                      bool     Had_Error )
    {
      if ( ! _Capture_Timing )
        return;

      int Idx              = _Timing_Head % _Timing_Buffer_Size;
      _Cycle_Timing[ Idx ] = new Poll_Cycle_Sample {
        Cycle_Time        = Time,
        Total_Ms          = Duration_Ms,
        Comm_Ms           = Comm_Ms,
        Address_Switch_Ms = Addr_Ms,
        UI_Ms             = UI_Ms,
        Record_Ms         = Record_Ms,
        Instrument_Count  = _Series.Count,
        Had_Error         = Had_Error,
      };
      _Timing_Head++;
      if ( _Timing_Count < _Timing_Buffer_Size )
        _Timing_Count++;
    }

    private void Record_Disconnect( string Instrument_Name, int Cycle_Number )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Called from Handle_Read_Error when Consecutive_Errors == 1
      lock ( _Disconnect_Events )
      {
        _Disconnect_Events.Add( new Disconnect_Event {
          Time            = DateTime.Now,
          Instrument_Name = Instrument_Name,
          Cycle_Number    = Cycle_Number,
        } );
      }

      // ── Write to timing file immediately ─────────────────────────────
      if ( _Is_Recording && _Timing_Writer != null )
      {
        // Write a clearly marked disconnect row
        _Timing_Writer.WriteLine( $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}," + $"{Cycle_Number}," +
                                  $"DISCONNECT,,,,," + $"{Instrument_Name}" );
        _Timing_Writer.Flush(); // flush immediately so disconnect is never lost
      }
    }

    private void Poll_Speed_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Show_Timing_View      = ! _Show_Timing_View;
      Poll_Speed_Button.Text = _Show_Timing_View ? "Data View" : "Poll Speed";
      Chart_Panel.Invalidate();
    }

    private void Capture_Timing_Checkbox_CheckedChanged( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Capture_Timing = Capture_Timing_Checkbox.Checked;

      // Only warn — don't force it off
      // Timing buffer works independently of file recording
      if ( _Capture_Timing && ! _Is_Running && ! _Is_Recording )
      {
        Show_Progress( "Timing will be captured when polling starts.", _Foreground_Color );
      }
    }

    private void Show_Analysis_Popup()
    {

      using var Block = Trace_Block.Start_If_Enabled();

      Run_Auto_Analysis_If_Enabled( Force: true );
    }

    // ── Call from a button (wire in designer) ────────────────────────────────
    private void Analyze_Data_Button_Click( object Sender, EventArgs E ) => Show_Analysis_Popup();

 

    private void NPLC_Summary_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if (_NPLC_Summary_Popup == null || _NPLC_Summary_Popup.Form.IsDisposed)
      {
        _NPLC_Summary_Popup = new Rich_Text_Popup( "NPLC Summary", 520, 600, Resizable: true );

        foreach (var S in _Series)
        {
          string Digit_Text = S.Type == Meter_Type.HP3458 && S.Display_Digits == 10
              ? "10"
              : $"{S.Display_Digits}.5";

          _NPLC_Summary_Popup
              .Add_Heading_Mono( $"{S.Name}  GPIB: {S.Address}" )
              .Add_Row( "Max NPLC:", $"{S.NPLC}" )
              .Add_Row( "Digits:", Digit_Text )
              .Add_Row( "Integration:", $"{S.Integration_Ms:F0} ms" )
              .Add_Row( "Settle Time:", $"{S.Settle_Ms:F0} ms" )
              .Add_Row( "Rate:", $"~{S.Get_Readings_Per_Min( _Series.Count )}/min" )
              .Add_Row( "Warning/Info:", S.NPLC_Warning_Text, S.NPLC_Warning_Color )
              .Add_Blank();
        }

        if (_Series.Count > 1)
        {
          var Slowest = _Series.OrderByDescending( S => S.Settle_Ms ).First();
          var Fastest = _Series.OrderBy( S => S.Settle_Ms ).First();
          int Session_Rate = Slowest.Get_Readings_Per_Min( _Series.Count );
          bool All_Same = _Series.All( S => S.NPLC == Slowest.NPLC );

          _NPLC_Summary_Popup
              .Add_Heading_Mono( "Session Summary" )
              .Add_Row( "Session Rate:", $"~{Session_Rate}/min", Slowest.NPLC_Warning_Color );

          if (!All_Same)
          {
            _NPLC_Summary_Popup
                .Add_Row( "Slowest:", $"{Slowest.Name} (NPLC {Slowest.NPLC})" )
                .Add_Row( "Fastest:", $"{Fastest.Name} (NPLC {Fastest.NPLC})" )
                .Add_Row( "Note:", "Rate limited by slowest instrument", Color.Gray );
          }
        }

        _NPLC_Summary_Popup.Form.FormClosed += ( S, Args ) =>
        {
          _NPLC_Summary_Popup?.Dispose();
          _NPLC_Summary_Popup = null;
        };

        _NPLC_Summary_Popup.Show_Non_Modal( this );
      }
      else
      {
        _NPLC_Summary_Popup.Form.Close();
      }
    }



    private void Polling_Info_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      using var Popup = new Rich_Text_Popup( "Polling Performance — readme", 640, 520, Resizable: true );

      Popup.Add_Title( "Polling Performance Guide" )
           .Add_Blank()
           .Add_Body( "This application polls one or more instruments in a continuous loop and plots" )
           .Add_Body( "the results in real time. The S/s indicator in the cycle display shows how" )
           .Add_Body( "fast the poll loop is running. A green background means performance is" )
           .Add_Body( "healthy. Yellow means a mild slowdown has been detected. Red means the poll" )
           .Add_Body( "rate has dropped significantly from its baseline." )

           .Add_Heading( "What S/s Means" )
           .Add_Body( "S/s stands for Samples per second — the number of complete poll cycles the" )
           .Add_Body( "application completes every second. A higher number means faster polling." )
           .Add_Body( "A lower number means slower polling. If you see the number gradually" )
           .Add_Body( "decreasing over time, the system is slowing down." )

           .Add_Heading( "Why Does It Slow Down?" )
           .Add_Body( "The most common cause of a gradual, continuous slowdown is point" )
           .Add_Body( "accumulation. Every data point added to the chart must be iterated during" )
           .Add_Body( "every render pass. As the number of points grows, each render takes longer," )
           .Add_Body( "which steals time from the poll loop. Unlike most problems that stabilize," )
           .Add_Body( "this one keeps getting worse as long as data is being added." )

           .Add_Heading( "All Points on the Graph" )
           .Add_Body( "Displaying all points for the entire run is the single largest contributor" )
           .Add_Body( "to slowdown in long runs. A 10-hour run at even a modest poll rate can" )
           .Add_Body( "accumulate millions of points. Most charting libraries are not designed to" )
           .Add_Body( "render millions of points in real time — render time grows linearly with" )
           .Add_Body( "point count, so the slowdown never stops." )

           .Add_Heading( "Recommended: Rolling Window View" )
           .Add_Body( "The recommended approach is to enable the Rolling Window option and set a" )
           .Add_Body( "Maximum Points limit. This keeps the live chart displaying only the most" )
           .Add_Body( "recent N points, which keeps render time constant regardless of how long" )
           .Add_Body( "the run has been going. No data is lost — all samples are recorded in" )
           .Add_Body( "memory. When the run finishes, you can zoom out and review the entire" )
           .Add_Body( "dataset." )

           .Add_Heading( "Other Contributing Factors" )
           .Add_Body( "\u2022  Memory pressure and garbage collection: as point lists grow, the" )
           .Add_Body( "   .NET garbage collector runs more frequently and for longer, causing" )
           .Add_Body( "   periodic pauses in the poll loop." )
           .Add_Blank()
           .Add_Body( "\u2022  BeginInvoke queue buildup: the poll loop uses fire-and-forget UI" )
           .Add_Body( "   updates. If the UI thread cannot keep up with the rate of updates," )
           .Add_Body( "   pending invokes accumulate, adding latency." )
           .Add_Blank()
           .Add_Body( "\u2022  Memory fragmentation: large numbers of small allocations over time" )
           .Add_Body( "   lead to heap fragmentation, making each GC collection slower." )
           .Add_Blank()
           .Add_Body( "\u2022  Event handler leaks: if handlers are attached to chart series or" )
           .Add_Body( "   controls without being detached, the GC cannot collect old objects," )
           .Add_Body( "   causing memory to grow unboundedly." )

           .Add_Heading( "How to Confirm the Cause" )
           .Add_Body( "Watch Task Manager while the application runs. If memory climbs steadily" )
           .Add_Body( "and never comes back down, you have a memory leak. If memory climbs and" )
           .Add_Body( "then drops sharply in a repeating sawtooth pattern, garbage collection" )
           .Add_Body( "pressure is the bottleneck. If S/s drops steadily as points accumulate" )
           .Add_Body( "but stabilizes once the Max Points cap is reached, point accumulation and" )
           .Add_Body( "render cost are the cause." )

           .Add_Heading( "Summary" )
           .Add_Body( "For long runs, always use the Rolling Window with a reasonable Max Points" )
           .Add_Body( "value (2,000\u201310,000 is typical). This keeps the system responsive, the" )
           .Add_Body( "poll rate stable, and ensures your data is still fully preserved for" )
           .Add_Body( "post-run analysis." );

      Popup.Show_Popup( this );
    }

  

    private void Memory_Monitor_Button_Click( object sender, EventArgs e )
    {
      if ( _Memory_Monitor.Is_Open )
      {

        _Memory_Monitor.Close();
        Memory_Monitor_Button.Text = "Open Monitor";
      }
      else
      {
        _Memory_Monitor.Show( this );
        Memory_Monitor_Button.Text = "Close Monitor";
      }
    }

    private void Rolling_Check_CheckedChanged( object sender, EventArgs e )
    {
      if ( ! Rolling_Check.Checked )
        Max_Points_Numeric.Value = _Max_Display_Points;

      Set_Button_State();
    }

    private void On_Start()
    {
      _Start_Time = DateTime.Now;
      _Is_Running = true;

      Start_Time_TextBox.Text = _Start_Time.ToString( "hh:mm:ss tt" );  // 12-hour with AM/PM
      Stop_Time_TextBox.Text = string.Empty;
      Total_Time_TextBox.Text = string.Empty;

    
    Set_Button_State();
    }

    private void On_Stop()
    {
      _Stop_Time = DateTime.Now;
      _Is_Running = false;

      TimeSpan elapsed = _Stop_Time - _Start_Time;

      Stop_Time_TextBox.Text = _Stop_Time.ToString( "hh:mm:ss tt" );   // 12-hour with AM/PM
      long rounded_Ticks = (long) Math.Round( elapsed.TotalSeconds ) * TimeSpan.TicksPerSecond;
      Total_Time_TextBox.Text = TimeSpan.FromTicks( rounded_Ticks ).ToString( @"hh\:mm\:ss" );

    

      Set_Button_State();
    }
    public int Get_Comms_Overhead_Ms( Instrument Inst )
    {
      return Inst.Type switch
      {
        Meter_Type.HP3458 => 333,  // ~333ms comms overhead on top of NPLC time
        Meter_Type.HP34401 => 20,
        Meter_Type.Generic_GPIB => 50,
        _ => 20
      };
    }


    private int Calculate_Nplc_Min_Ms()
    {
      return _Instruments.Sum( s => s.Poll_Delay_Ms ) + 50;
    }



  }
}
