// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Recording_Playback_Form.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   WinForms chart form for loading, viewing, and analysing previously recorded
//   instrument data and poll-timing CSV files.  Derives from Base_Chart_Form,
//   which supplies all shared chart rendering, zoom, pan, scrollbar, and
//   tooltip infrastructure.  This form adds no live polling of its own — all
//   data originates from disk files selected via OpenFileDialog.
//
// INHERITANCE
//   Recording_Playback_Form → Base_Chart_Form → Form
//   Base_Chart_Form provides: _Series, _Theme, _Settings, _Combined_View,
//   _Normalized_View, _View_Offset, _Auto_Scroll, _Max_Display_Points,
//   _Enable_Rolling, _Chart_Tooltip, _FPS_Stopwatch, _Paint_Count,
//   _Actual_FPS, and all Draw_*() methods.
//
// FILE TYPES SUPPORTED
//   *_Multi.csv     Multi-instrument data file (Timestamp, Col1, Col2, …).
//                   Loaded by Load_Instrument_Recorded_File().
//   *_Timing.csv    Poll-cycle timing file.
//                   Loaded by Load_Timing_Recorded_File() / Load_Timing_File().
//   *_Settings.txt  Optional session settings snapshot in the same folder as
//                   the loaded CSV.  Auto-displayed as a non-modal Rich_Text_Popup
//                   after a successful file load.
//
// DUAL VIEW MODES
//   Data View     Renders _Data_Series via the inherited chart engine.
//   Poll Speed    Renders _Timing_Series / Draw_Poll_Timing_Chart().
//   Poll_Speed_Button toggles between the two; _Show_Timing_View tracks state.
//   _Series always points to whichever list is currently active so that all
//   Base_Chart_Form rendering paths use the right data without modification.
//
// FIELDS
//
//   State flags
//     _Polling_File_Loading      True while Load_Timing_Recorded_File() is active;
//                                suppresses chart repaints to avoid partial renders.
//     _Instrument_File_Loading   True while Load_Instrument_Recorded_File() runs.
//     _Is_Running                Set to false after close operations complete;
//                                used by Base_Chart_Form and OnFormClosing().
//     _Is_Recording              Tracks whether a recording session was open
//                                (relevant when closing mid-operation).
//     _Memory_Warning_Shown      Guards the one-time memory threshold warning.
//     _Is_Shutting_Down          Prevents duplicate cleanup in OnFormClosing.
//     _Poll_Error_Shown          Guards the one-time poll error dialog.
//
//   File tracking
//     _Loaded_File_Path          Full path of the most recently loaded CSV.
//     _Recording_File_Path       Path used during any active recording session.
//     _Record_Query              GPIB query string for the active recording.
//     _Record_Start              DateTime when recording began.
//     _Last_Successful_Read      DateTime of the last clean instrument read.
//     _Recording_Writer          Open StreamWriter during active recording.
//
//   Data
//     _Data_Series               List<Instrument_Series> for instrument data.
//     _Timing_Series             List<Instrument_Series> for poll-timing data.
//     _Reading_Timestamps        Per-read timestamps for interval analysis.
//     _Filtered_Indices          Maps Measurement_Combo indices to _Measurements[].
//     _Error_Counts              Per-instrument consecutive error counter.
//     _Last_Success              Per-instrument DateTime of last clean read.
//     _Cycle_Count               Number of completed poll cycles.
//     _Cycle_Stopwatch           Wall-clock timer for FPS and cycle-rate tracking.
//
//   Infrastructure
//     _Chart_Refresh_Timer       WinForms Timer driving Chart_Panel repaints.
//     _Auto_Save_Timer           WinForms Timer driving periodic CSV auto-save.
//     _Memory_Status_Label       Status-strip label for memory usage display.
//     _Performance_Status_Label  Status-strip label for FPS / point count display.
//     _Cts                       CancellationTokenSource for any async poll loop.
//     _Comm                      Instrument_Comm for GPIB communication.
//     _GPIB_Manager              Manages multi-instrument GPIB addressing.
//     _Analysis_Results_Panel    Overlay Panel showing auto-analysis results.
//     _Zoom_Label                Optional label reflecting current zoom level.
//
//   Static measurement table  _Measurements[]
//     Array of (Label, Cmd_3458, Cmd_34401, Cmd_Generic_GPIB, Unit) tuples
//     covering all supported measurement types.  Shared by Populate_Measurement_Combo()
//     and Get_Command_For_Series() to keep command strings in one place.
//
// CONSTRUCTOR
//   Accepts Application_Settings; throws ArgumentNullException for null Settings
//   or null Settings.Chart_Theme (the latter guards against bare
//   new Application_Settings() bypassing Load()).
//   Wires all Base_Chart_Form control references, applies theme, initialises the
//   chart refresh timer, attaches Paint / MouseMove / MouseWheel handlers, calls
//   Apply_Settings(), and wires UI events last to prevent premature callbacks.
//
// KEY METHODS
//
//   Load_Button_Click()
//     Opens an OpenFileDialog filtered to *.csv, routes to
//     Load_Timing_Recorded_File() or Load_Instrument_Recorded_File() based on
//     filename suffix (_Timing.csv vs _Multi.csv), then auto-shows any
//     *_Settings.txt file found in the same folder.
//
//   Load_Instrument_Recorded_File( File_Path ) → Task
//     Async multi-stage loader:
//       1. Calls Load_CSV_Preamble() to parse header comments into
//          Flat_Stats and Sectioned_Stats.
//       2. Restores Measurement_Combo selection from the "Measurement" preamble
//          key if present.
//       3. Builds one Instrument_Series per CSV column beyond the timestamp,
//          assigning theme line colors and attaching any per-series File_Stats
//          from Sectioned_Stats.  Handles the Add_File_Checkbox duplicate-name
//          suffix logic.
//       4. Parses data rows on a background Task.Run thread, reporting progress
//          to the UI thread every 1% when row count exceeds 10 000.
//       5. Derives session start/stop/duration from the actual data timestamps.
//       6. Updates scrollbar, performance status, and chart.
//       7. Auto-triggers Compute_Post_Processing() when exactly 2 series are
//          loaded (two-instrument comparison mode).
//     All exceptions are caught and shown via MessageBox; finally block always
//     clears _Instrument_File_Loading and calls Set_Button_State().
//
//   Load_Timing_Recorded_File( File_Path ) → Task
//     Thin async wrapper around the synchronous Load_Timing_File() (defined in
//     Base_Chart_Form).  Sets _Polling_File_Loading guard and calls
//     Set_Button_State() in finally.
//
//   Compute_Post_Processing( Output_Path? ) → Task
//     Two-series delta analysis written to a *_Analyzed.csv alongside the
//     source file.  For each aligned point pair (shorter series determines
//     count):
//       • Value_A, Value_B, Delta, Delta_uV, Delta_Ms
//       • Rolling_Mean_Delta and Rolling_StdDev_Delta over a 100-point window
//         (online Welford-style accumulation using sum and sum-of-squares).
//     After the Task.Run loop, computes global delta summary statistics and
//     displays them in the progress bar: mean, σ, min, max — all in µV.
//
//   Apply_Settings()
//     Applies Application_Settings to all form controls: tooltip duration,
//     chart refresh rate, max display points, default view mode (only when no
//     data is loaded), default measurement type, save folder creation, window
//     title, and default zoom level.
//
//   Set_Button_State()
//     Recomputes enabled/disabled state for every toolbar button based on the
//     combination of Has_Data (any series with points), Has_Timing (timing
//     buffer non-empty), and Has_Multi (more than one series with points).
//     No-ops while a file is loading.
//
//   Show_Analysis_Results( List<Instrument_Series> )
//     Builds a floating overlay Panel anchored to the bottom-right of the form,
//     displaying per-series color swatch, name, and stat rows controlled by the
//     Analysis_Show_* settings flags.  A close (✕) button disposes the panel.
//     Replaces any previously shown analysis panel.
//
//   Run_Auto_Analysis_If_Enabled()
//     Called after recording stops.  If Auto_Analyze_After_Recording is set:
//       • Two loaded series → launches Analysis_Popup_Form in two-series mode.
//       • One or more series → calls Show_Analysis_Results() for the lightweight
//         inline summary.
//
//   Analyze_Instrument_Data_Button_Click()
//     Launches Analysis_Popup_Form in multi-series mode with all series that
//     have ≥ 2 points.  Deduplicates series names by appending "#N" suffixes.
//     Calls Popup.Begin_Async_Load() immediately after Show().
//
//   Analyze_Poll_Timing_Button_Click() / Show_Timing_Analysis_Popup()
//     Snapshots the circular timing buffer in chronological order, constructs
//     a List<Poll_Timing_Analysis>, and launches Poll_Timing_Analysis_Form
//     as a non-modal window.
//
//   Perform_Close_Operations( Context ) → Task
//     Graceful shutdown: cancels any active CTS, polls _Is_Running up to 5 s,
//     stops _Chart_Refresh_Timer, disposes CTS.
//
//   Close_Button_Click()
//     Disables itself to prevent double-clicks, awaits Perform_Close_Operations,
//     then calls this.Close().
//
// CHART PAINT HANDLER  (Chart_Panel_Paint)
//   Guards against loading-in-progress, tracks FPS, fills background, then
//   dispatches to:
//     Draw_Poll_Timing_Chart()   when _Show_Timing_View is true.
//     Draw_Empty_State()         when _Series is empty.
//     Draw_Instrument_List()     when no visible series has data.
//     Draw_Combined_View() or Draw_Split_View() otherwise.
//   Draw_Position_Indicator() is always called last when data is present.
//
// MAX POINTS NUMERIC HANDLING
//   Three event handlers collaborate to ensure the NumericUpDown always
//   commits its typed text:
//     ValueChanged    Standard path — updates _Max_Display_Points and scrollbar.
//     KeyDown         Enter key forces parse + clamp + ValueChanged.
//     Leave           Focus loss forces parse + clamp.
//   Commit_Numeric_Value() centralises the parse/clamp logic called by
//   Rolling_Check.CheckedChanged and Max_Points_Numeric_KeyDown.
//
// MEASUREMENT COMBO
//   Populated by Populate_Measurement_Combo(), which filters _Measurements[]
//   to entries with a non-empty command for the first series' meter type.
//   SelectedIndexChanged is unsubscribed during population and re-subscribed
//   after to prevent spurious command sends.  On change, Measurement_Combo_Changed
//   maps the combo index through _Filtered_Indices to the correct _Measurements[]
//   entry and calls Get_Command_For_Series() for each series.
//   The combo is disabled in playback mode — command dispatch is present for
//   potential future live-instrument reuse but currently no-ops for playback.
//
// THEME
//   Theme_Button_Click opens Chart_Theme_Settings dialog.  On OK, copies the
//   result into _Theme, saves it, updates all series line colors from the theme
//   palette in index order, and calls _Settings.Set_Theme() to propagate
//   Theme_Changed to other open forms.
//
// STATUS STRIP
//   Initialize_Status_Panel() delegates to
//   Multimeter_Common_Helpers_Class.Initialize_Status_Strip().
//   Update_Performance_Status() delegates to
//   Multimeter_Common_Helpers_Class.Update_Performance_Status() with both
//   Total and Current equal to the sum of all series point counts (no
//   decimation distinction needed in playback mode).
//
// FORM CLOSING
//   OnFormClosing() performs a second-pass cleanup if _Is_Running or
//   _Is_Recording is still true (i.e. Close_Button was not used).  Stops
//   both timers and disposes _Chart_Tooltip.  Note: base.OnFormClosing() is
//   currently called twice — once conditionally and once unconditionally;
//   this is a known minor issue and is harmless for the tool window lifetime.
//
// NOTES
//   • _Series is a reference alias — assigning it to _Data_Series or
//     _Timing_Series switches which list all Base_Chart_Form code operates on
//     without any other changes.
//   • Add_File_Checkbox allows accumulating data from multiple files into a
//     single chart session; duplicate series names receive an integer suffix.
//   • Compute_Post_Processing() uses aligned indices (not timestamp matching),
//     which assumes both series were recorded synchronously at the same rate.
//   • File_Stats on each Instrument_Series carries the pre-parsed preamble
//     statistics so the chart legend can display recorded stats without
//     recomputing them from the loaded points.
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════






using Rich_Text_Popup_Namespace;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{

  public partial class Recording_Playback_Form : Base_Chart_Form
  {
    // ── Playback-only fields ──────────────────────────────────────────
    private bool _Polling_File_Loading    = false;
    private bool _Instrument_File_Loading = false;
    private bool _Is_Running              = false;
    private bool _Is_Recording            = false;
    private bool _Memory_Warning_Shown    = false;
    private bool _Is_Shutting_Down        = false;
    private bool _Poll_Error_Shown        = false;

    private string? _Loaded_File_Path    = null;
    private string? _Recording_File_Path = null;
    private string? _Record_Query        = "";
    private DateTime _Record_Start;
    private DateTime _Last_Successful_Read = DateTime.Now;

    private StreamWriter? _Recording_Writer;

    private List<Instrument_Series> _Data_Series   = new();
    private List<Instrument_Series> _Timing_Series = new();

    private Panel? _Analysis_Results_Panel;
    private Label? _Zoom_Label;

    private System.Windows.Forms.Timer? _Chart_Refresh_Timer;
    private System.Windows.Forms.Timer? _Auto_Save_Timer;
    private ToolStripStatusLabel? _Memory_Status_Label;
    private ToolStripStatusLabel? _Performance_Status_Label;

    private CancellationTokenSource? _Cts;
    private readonly List<DateTime>      _Reading_Timestamps = new();
    private List<int>                    _Filtered_Indices   = new();
    private Dictionary<string, int>      _Error_Counts       = new();
    private Dictionary<string, DateTime> _Last_Success       = new();
    private Instrument_Comm? _Comm;
    private GPIB_Manager? _GPIB_Manager;
    private int                _Cycle_Count;
    private readonly Stopwatch _Cycle_Stopwatch = new();

    private static readonly( string Label,
                             string Cmd_3458,
                             string Cmd_34401,
                             string Cmd_Generic_GPIB,
                             string Unit )[ ] _Measurements = {
      ( "DC Voltage", "DCV", "MEAS:VOLT:DC", "MEAS:VOLT:DC", "V" ),
      ( "AC Voltage", "ACV", "MEAS:VOLT:AC", "MEAS:VOLT:AC", "V" ),
      ( "DC Current", "DCI", "MEAS:CURR:DC", "MEAS:CURR:DC", "A" ),
      ( "AC Current", "ACI", "MEAS:CURR:AC", "MEAS:CURR:AC", "A" ),
      ( "2-Wire Ohms", "OHM", "MEAS:RES", "MEAS:RES", "Ohm" ),
      ( "4-Wire Ohms", "OHMF", "MEAS:FRES", "MEAS:FRES", "Ohm" ),
      ( "Frequency", "FREQ", "MEAS:FREQ", "MEAS:FREQ", "Hz" ),
      ( "Period", "PER", "MEAS:PER", "MEAS:PER", "s" ),
      ( "Continuity", "", "MEAS:CONT", "MEAS:CONT", "Ohm" ),
      ( "Diode", "", "MEAS:DIOD", "MEAS:DIOD", "V" ),
      ( "Temperature", "TEMP", "", "", "\u00b0C" ),
    };

    public Recording_Playback_Form( Application_Settings Settings )
    {
      InitializeComponent();

      using var Block = Trace_Block.Start_If_Enabled();

      helpToolStripMenuItem.Click += ( s, e ) => App_Help.Show_Recording_Playback_Form_Help( this );

      // ── Null guards ───────────────────────────────────────────────────
      if ( Settings == null )
        throw new ArgumentNullException( nameof( Settings ), "Settings must not be null" );

      if ( Settings.Chart_Theme == null )
        throw new ArgumentNullException( "Settings.Chart_Theme",
                                         "Theme must not be null — use Application_Settings.Load() not new " +
                                         "Application_Settings()" );

      // ── Base class control references ─────────────────────────────────
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

      if ( LicenseManager.UsageMode == LicenseUsageMode.Designtime )
        return;

      // ── Settings and theme ────────────────────────────────────────────
      _Settings             = Settings;
      _Theme                = _Settings.Chart_Theme ?? Chart_Theme.Dark_Preset();
      Chart_Panel.BackColor = _Theme.Background;

      _Settings.Theme_Changed += ( s, e ) =>
      {
        _Theme                = _Settings.Chart_Theme;
        Chart_Panel.BackColor = _Theme.Background;
        Chart_Panel.Invalidate();
      };

      // ── Chart setup ───────────────────────────────────────────────────
      Initialize_Chart_Refresh_Timer();
      Enable_Double_Buffer( Chart_Panel );
      Chart_Panel.Paint += Chart_Panel_Paint;

      _Chart_Tooltip = new ToolTip {
        AutoPopDelay = 5000,
        InitialDelay = 100,
        ReshowDelay  = 100,
        ShowAlways   = true,
      };
      Chart_Panel.MouseMove  += Chart_Panel_Control_MouseMove;
      Chart_Panel.MouseWheel += Chart_Panel_Control_Mouse_Wheel;

      Populate_Measurement_Combo();
      Normalize_Button.Visible = _Combined_View;
      Update_Graph_Style_Availability();
      Initialize_Status_Panel();
      Apply_Settings();

      Legend_Toggle_Button.Text  = "Show Stats";
      Max_Points_Numeric.Enabled = true;
      Measurement_Combo.Enabled  = false;
      Set_Button_State();

      // ── Event wiring — must be LAST so nothing fires during setup ─────
      Max_Points_Numeric.ValueChanged += Max_Points_Numeric_ValueChanged;

      // Add alongside your existing ValueChanged wiring
      Max_Points_Numeric.KeyDown -= Max_Points_Numeric_KeyDown;
      Max_Points_Numeric.KeyDown += Max_Points_Numeric_KeyDown;

      Max_Points_Numeric.Leave -= Max_Points_Numeric_Leave;
      Max_Points_Numeric.Leave += Max_Points_Numeric_Leave;

      Rolling_Check.CheckedChanged += ( s, e ) =>
      {
        _Enable_Rolling = Rolling_Check.Checked;

        if ( _Series == null || _Series.Count == 0 )
          return;

        int Total_Points = _Series.Count > 0 ? _Series.Max( s => s.Points.Count ) : 0;

        Multimeter_Common_Helpers_Class.Update_Scrollbar_Range( Pan_Scrollbar,
                                                                Total_Points,
                                                                _Max_Display_Points,
                                                                _Auto_Scroll,
                                                                ref _View_Offset );

        Chart_Panel.Invalidate();
      };
    }

    private void Chart_Panel_Mouse_Wheel( object         sender,
                                          MouseEventArgs e ) => Chart_Panel_Control_Mouse_Wheel( sender, e );

    private void Chart_Panel_Resize( object? sender, EventArgs e ) => Chart_Panel_Control_Resize( sender, e );

    private void Pan_Scrollbar_Scroll( object          sender,
                                       ScrollEventArgs e ) => Pan_Scrollbar_Control_Scroll( sender, e );

    private void Pan_Scrollbar_ValueChanged( object    sender,
                                             EventArgs e ) => Pan_Scrollbar_Control_ValueChanged( sender, e );

    protected override bool _Is_Running_State() => _Is_Running;

    protected override      string Current_Unit
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

    private void Poll_Speed_Button_Click( object Sender, EventArgs E )
    {
      _Show_Timing_View      = ! _Show_Timing_View;
      Poll_Speed_Button.Text = _Show_Timing_View ? "Data View" : "Poll Speed";
      _Series                = _Show_Timing_View ? _Timing_Series : _Data_Series;
      Chart_Panel.Invalidate();
    }
    private void Chart_Panel_Paint( object? sender, PaintEventArgs e )
    {
      if ( _Instrument_File_Loading || _Polling_File_Loading )
        return;

      Multimeter_Common_Helpers_Class.Track_FPS( ref _Paint_Count,
                                                 ref _Actual_FPS,
                                                 _FPS_Stopwatch,
                                                 Update_Performance_Status );

      Graphics G          = e.Graphics;
      G.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

      // ← Use Chart_Panel_Control not Chart_Panel
      int       W = Chart_Panel_Control.ClientSize.Width;
      int       H = Chart_Panel_Control.ClientSize.Height;

      using var Bg_Brush = new SolidBrush( _Theme.Background );
      G.FillRectangle( Bg_Brush, 0, 0, W, H );

      if ( _Show_Timing_View )
      {
        Draw_Poll_Timing_Chart( G, W, H );
        return;
      }

      if ( _Series.Count == 0 )
      {
        Draw_Empty_State( G, W, H, "Press Load to load a recorded file." );
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

        // Set_Local_Mode ( );
        base.OnFormClosing( E );
      }
      else
      {
        Capture_Trace.Write( "Already cleaned up, skipping" );
      }

      base.OnFormClosing( E );
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

    private string Get_Command_For_Series(
      Instrument_Series S,
      (string Label, string Cmd_3458, string Cmd_34401, string Cmd_Generic_GPIB, string Unit) Entry )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      return S.Type switch {
        Meter_Type.HP3458  => Entry.Cmd_3458,
        Meter_Type.HP34401 => Entry.Cmd_34401,
        _                  => Entry.Cmd_Generic_GPIB,
      };
    }

    private void Clear_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Show_Timing_View )
      {
        _Timing_Head        = 0;
        _Timing_Count       = 0;
        _Timing_View_Offset = 0;
        _Disconnect_Events.Clear();
        _Show_Timing_View = false;
      }

      // Clear data
      _Series.Clear();
      _Is_Running = false;

      Set_Button_State();
      SuspendLayout();
      ResumeLayout( true );
      Invalidate( true );
      Update();
    }

    public void Set_Button_State()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Polling_File_Loading || _Instrument_File_Loading )
        return;

      bool Has_Data   = _Series != null && _Series.Any( s => s.Points.Count > 0 );
      bool Has_Timing = _Timing_Count > 0;
      bool Has_Any    = Has_Data || Has_Timing;
      bool Has_Multi  = _Series != null && _Series.Count( s => s.Points.Count > 0 ) > 1;

      // always enabled when not loading
      Load_Button.Enabled  = true;
      Close_Button.Enabled = true;
      Theme_Button.Enabled = true;

      // Needs data or timing
      Clear_Button.Enabled      = Has_Any;
      Add_File_Checkbox.Enabled = Has_Any;
      Zoom_Slider.Enabled       = Has_Any;
      Graph_Style_Combo.Enabled = Has_Any;

      Legend_Toggle_Button.Enabled = Has_Any;
      Max_Points_Numeric.Enabled   = Has_Any;
      Rolling_Check.Enabled        = Has_Any;
      View_Mode_Button.Enabled     = Has_Any;

      // Needs instrument data specifically
      Analyze_Instrument_Data_Button.Enabled = Has_Data;
      Measurement_Combo.Enabled              = Has_Data;

      // Needs timing specifically
      Analyze_Poll_Timing_Button.Enabled = Has_Timing;

      // Needs both
      Poll_Speed_Button.Enabled = Has_Data && Has_Timing;
      Poll_Speed_Button.Text    = _Show_Timing_View ? "Data View" : "Poll Speed";
    }

    private async void Load_Button_Click( object Sender, EventArgs E )
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
      string    Folder      = Multimeter_Common_Helpers_Class.Get_Graph_Captures_Folder( _Settings );

      using var Dlg = new OpenFileDialog();
      Dlg.Title     = "Load Recorded Data";
      Dlg.Filter    = "CSV files (*.csv)|*.csv";
      if ( Directory.Exists( Folder ) )
        Dlg.InitialDirectory = Folder;

      if ( Dlg.ShowDialog() != DialogResult.OK )
        return;

      Start_Time_TextBox.Text = string.Empty;
      Stop_Time_TextBox.Text  = string.Empty;
      Total_Time_TextBox.Text = string.Empty;

      string File_Path = Dlg.FileName;

      if ( File_Path.EndsWith( "_Timing.csv", StringComparison.OrdinalIgnoreCase ) )
      {
        await Load_Timing_Recorded_File( File_Path );
        Refresh_Panel();
      }
      else if ( File_Path.EndsWith( "_Multi.csv", StringComparison.OrdinalIgnoreCase ) )
      {
        await Load_Instrument_Recorded_File( File_Path );
        Refresh_Panel();
      }
      else
      {
        MessageBox.Show( "Unrecognized file type. Please select a Timing or Multi CSV file.",
                         "Load Error",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        return;
      }

      // ── Auto-show settings snapshot if present ────────────────
      string Session_Folder = System.IO.Path.GetDirectoryName( File_Path )!;
      string Settings_File  = Directory.GetFiles( Session_Folder, "*_Settings.txt" ).FirstOrDefault() ?? "";

      if ( ! string.IsNullOrEmpty( Settings_File ) )
      {
        var Popup = new Rich_Text_Popup( "Session Settings", 740, 700, Resizable: true );
        Popup.Add_Title( "Session Settings" ).Add_Blank();

        foreach ( var Line in await File.ReadAllLinesAsync( Settings_File ) )
          Popup.Add_Mono( Line );

        Popup.Form.FormClosed += ( s, e ) => Popup.Dispose();
        Popup.Show_Non_Modal( this );
      }
    }

    public void Refresh_Panel()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      Chart_Panel.Invalidate();
      Chart_Panel.Update();
    }

    private async Task Load_Timing_Recorded_File( string File_Path )
    {
      try
      {
        using var Block       = Trace_Block.Start_If_Enabled();
        _Polling_File_Loading = true;
        _Loaded_File_Path     = File_Path;
        _View_Offset          = 0;
        _Auto_Scroll          = false;

        Load_Timing_File( File_Path );
      }
      catch ( Exception Ex )
      {
        MessageBox.Show( $"Load failed: {Ex.Message}\n\n{Ex.StackTrace}",
                         "Load Error",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error );
      }
      finally
      {
        _Polling_File_Loading = false;
        Set_Button_State();
      }
    }

    private void Max_Points_Numeric_ValueChanged( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Max_Points_Numeric.Value = Max_Points_Numeric.Value; // force parse of typed text
      _Max_Display_Points      = (int) Max_Points_Numeric.Value;

      if ( _Series == null || _Series.Count == 0 )
        return;

      int Total_Points = _Series.Count > 0 ? _Series.Max( s => s.Points.Count ) : 0;

      Multimeter_Common_Helpers_Class.Update_Scrollbar_Range( Pan_Scrollbar,
                                                              Total_Points,
                                                              _Max_Display_Points,
                                                              _Auto_Scroll,
                                                              ref _View_Offset );

      Chart_Panel.Invalidate();
    }

    private void Max_Points_Numeric_KeyDown( object? sender, KeyEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( e.KeyCode == Keys.Enter )
      {
        if ( decimal.TryParse( Max_Points_Numeric.Text, out decimal parsed ) )
        {
          decimal clamped = Math.Clamp( parsed, Max_Points_Numeric.Minimum, Max_Points_Numeric.Maximum );
          Max_Points_Numeric.Value = clamped; // force commit
                                              // ValueChanged will fire automatically if value changed
                                              // but if same value, call directly:
          if ( clamped == _Max_Display_Points )
            Max_Points_Numeric_ValueChanged( sender, EventArgs.Empty );
        }
        e.Handled          = true;
        e.SuppressKeyPress = true;
      }
    }
    private void Rolling_Check_CheckedChanged( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Capture_Trace.Write( $" Checked         = {Rolling_Check.Checked}" );
      Capture_Trace.Write( $" Numeric.Text    = {Max_Points_Numeric.Text}" );
      Capture_Trace.Write( $" Numeric.Value   = {Max_Points_Numeric.Value}" );
      Capture_Trace.Write( $" _Max_Display_Points = {_Max_Display_Points}" );

      if ( Rolling_Check.Checked )
      {
        Commit_Numeric_Value(); // parse whatever is typed right now
      }
      else
      {
        Max_Points_Numeric.Value = _Max_Display_Points;
      }

      Set_Button_State();
    }

    private void Commit_Numeric_Value()
    {

      using var Block = Trace_Block.Start_If_Enabled();

      if ( decimal.TryParse( Max_Points_Numeric.Text, out decimal parsed ) )
      {
        decimal Clamped = Math.Clamp( parsed, Max_Points_Numeric.Minimum, Max_Points_Numeric.Maximum );
        if ( Clamped != Max_Points_Numeric.Value )
          Max_Points_Numeric.Value = Clamped;                       // this WILL fire ValueChanged
        else
          Max_Points_Numeric_ValueChanged( null, EventArgs.Empty ); // same value, force update
      }
    }

    private void Max_Points_Numeric_Leave( object? sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( decimal.TryParse( Max_Points_Numeric.Text, out decimal parsed ) )
      {
        decimal clamped = Math.Clamp( parsed, Max_Points_Numeric.Minimum, Max_Points_Numeric.Maximum );
        if ( clamped != Max_Points_Numeric.Value )
          Max_Points_Numeric.Value = clamped; // fires ValueChanged automatically
      }
    }

    private async Task Load_Instrument_Recorded_File( string File_Path )
    {
      try
      {
        using var Block = Trace_Block.Start_If_Enabled();

        _Instrument_File_Loading = true;
        _Loaded_File_Path        = File_Path;
        _Max_Display_Points      = Math.Max( 10,
                                             Math.Min( (int) Max_Points_Numeric.Value,
                                                       _Series.Count > 0 ? _Series.Max( s => s.Points.Count )
                                                                         : (int) Max_Points_Numeric.Value ) );
        _View_Offset             = 0;
        _Auto_Scroll             = false;

        foreach ( var S in _Series )
          S.Reset_Stats();

        _Show_Timing_View = false;

        var Preamble = await Multimeter_Common_Helpers_Class.Load_CSV_Preamble( File_Path );
        if ( Preamble == null )
          return;

        // ── Restore measurement type from preamble ────────────────────
        if (Preamble.Flat_Stats.TryGetValue( "Measurement", out string? Saved_Measurement ))
        {
          Capture_Trace.Write( $"Saved_Measurement: [{Saved_Measurement}]" );
          if ( Measurement_Combo.Items.Contains( Saved_Measurement ) )
            Measurement_Combo.SelectedItem = Saved_Measurement;
          else
            Capture_Trace.Write(
              $"NO MATCH - combo items: [{string.Join( ", ", Measurement_Combo.Items.Cast<string>() )}]" );
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
          return;
        }

        int  Data_Line_Count   = Lines.Length - Header_Index - 1;
        bool Show_Progress_Bar = Data_Line_Count > 10_000;
        int  Progress_Interval = Math.Max( 1, Data_Line_Count / 100 );

        // ── Build series from CSV headers ─────────────────────────────
        if ( ! Add_File_Checkbox.Checked )
          _Data_Series.Clear();

        for ( int I = 0; I < Col_Count; I++ )
        {
          string Header_Name = Headers[ I + 1 ].Trim();

          if ( Add_File_Checkbox.Checked )
          {
            int    Suffix = 2;
            string Unique = Header_Name;
            while ( _Data_Series.Any( S => S.Name == Unique ) )
              Unique = $"{Header_Name} ({Suffix++})";
            Header_Name = Unique;
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
            Line_Color = _Theme.Line_Colors[ ( I + _Data_Series.Count ) % _Theme.Line_Colors.Length ],
            Points     = new List<( DateTime Time, double Value )>( Data_Line_Count ),
            File_Stats = Sectioned_Stats != null && Sectioned_Stats.ContainsKey( Header_Name )
                           ? Sectioned_Stats[ Header_Name ]
                           : null,
          };

          _Data_Series.Add( S );
        }
        _Series = _Data_Series;

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
        if ( All_Points.Count > 0 )
        {
          DateTime First         = All_Points.Min( p => p.Time );
          DateTime Last          = All_Points.Max( p => p.Time );
          TimeSpan Elapsed       = Last - First;
          long     Rounded_Ticks = (long) Math.Round( Elapsed.TotalSeconds ) * TimeSpan.TicksPerSecond;

          Start_Time_TextBox.Text = First.ToString( "hh:mm:ss tt" );
          Stop_Time_TextBox.Text  = Last.ToString( "hh:mm:ss tt" );
          Total_Time_TextBox.Text = TimeSpan.FromTicks( Rounded_Ticks ).ToString( @"hh\:mm\:ss" );
        }

        Update_Performance_Status();
        Update_Graph_Style_Availability();

        Multimeter_Common_Helpers_Class.Update_Scrollbar_Range( Pan_Scrollbar,
                                                                _Series.Count > 0
                                                                  ? _Series.Max( s => s.Points.Count )
                                                                  : 0,
                                                                _Max_Display_Points,
                                                                _Auto_Scroll,
                                                                ref _View_Offset );

        Chart_Panel.Invalidate();

        if ( _Series.Count == 2 )
          await Compute_Post_Processing();
      }
      catch ( Exception Ex )
      {
        MessageBox.Show( $"Load failed: {Ex.Message}\n\n{Ex.StackTrace}",
                         "Load Error",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error );
      }
      finally
      {
        _Instrument_File_Loading = false;
        bool Has_Data            = _Series != null && _Series.Any( s => s.Points.Count > 0 );
        Capture_Trace.Write(
          $"After load — Has_Data: {Has_Data}, Series count: {_Series?.Count}, Points: {_Series?.Sum( s => s.Points.Count )}" );
        Set_Button_State();
      }
    }

    private void Theme_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      using var Dlg = new Chart_Theme_Settings( _Theme );
      if ( Dlg.ShowDialog( this ) == DialogResult.OK )
      {
        _Theme.Copy_From( Dlg.Result );
        _Theme.Save();
        Chart_Panel.BackColor = _Theme.Background;

        // Update series line colors from theme palette
        for ( int I = 0; I < _Series.Count; I++ )
        {
          _Series[ I ].Line_Color = _Theme.Line_Colors[ I % _Theme.Line_Colors.Length ];
        }

        _Settings.Set_Theme( _Theme, "Chart_Theme" );
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

    private void Update_Chart_Refresh_Rate() => Update_Chart_Refresh_Rate( _Chart_Refresh_Timer );

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

      Update_Chart_Refresh_Rate();

      // Default max display points
      _Max_Display_Points = _Settings.Max_Display_Points;
      Max_Points_Numeric.Value =
        Math.Max( Max_Points_Numeric.Minimum,
                  Math.Min( Max_Points_Numeric.Maximum, _Settings.Max_Display_Points ) );

      // View mode defaults (only apply if no data yet)
      if ( _Series.All( s => s.Points.Count == 0 ) )
      {
        _Combined_View   = _Settings.Default_To_Combined_View;
        _Normalized_View = _Settings.Default_To_Normalized_View;

        View_Mode_Button.Text    = _Combined_View ? "Split View" : "Combined View";
        Normalize_Button.Text    = _Normalized_View ? "Absolute" : "Normalize";
        Normalize_Button.Visible = _Combined_View;
      }

      // Default measurement type
      if ( Measurement_Combo.Items.Contains( _Settings.Default_Measurement_Type ) )
      {
        Measurement_Combo.SelectedItem = _Settings.Default_Measurement_Type;
      }

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

      // ===== UI SETTINGS =====

      this.Text = $"Recorded Data Viewer";

      // ===== ZOOM SETTINGS =====

      // Default zoom level
      if ( Zoom_Slider != null )
      {
        Zoom_Slider.Value = _Settings.Default_Zoom_Level;
        Update_Zoom_From_Slider();
      }

      Chart_Panel.Invalidate();

      Capture_Trace.Write( "Settings applied successfully" );
    }

    protected override void Show_Progress( string Message, Color Color )
    {
      Progress_Text_Box.Text      = Message;
      Progress_Text_Box.ForeColor = Color;
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

      // 3. Stop timers
      Capture_Trace.Write( "Stopping timers" );
      _Chart_Refresh_Timer?.Stop();

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

    private void Populate_Measurement_Combo()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Measurement_Combo.Enabled = false;

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
      if ( Measurement_Combo.SelectedItem == null )
        return;

      string Selected = Measurement_Combo.SelectedItem.ToString();
      Capture_Trace.Write( $"Measurement changed to: {Selected}" );

      Show_Progress( $"Measurement changed to: {Selected}", _Foreground_Color );

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
        }

        catch ( Exception Ex )
        {
        }
        finally
        {
          Measurement_Combo.Enabled = false;
        }
      }
    }

    private async Task Compute_Post_Processing( string Output_Path = null )
    {
      if ( _Series.Count < 2 )
      {
        Show_Progress( "Post-processing requires exactly 2 series.", Color.Orange );
        return;
      }

      var S1 = _Series[ 0 ];
      var S2 = _Series[ 1 ];

      // Align by point count — use the shorter series
      int Count = Math.Min( S1.Points.Count, S2.Points.Count );
      if ( Count < 2 )
      {
        Show_Progress( "Not enough points for post-processing.", Color.Orange );
        return;
      }

      // ── Build output path ─────────────────────────────────────────────
      if ( string.IsNullOrEmpty( Output_Path ) )
      {
        // Analyzed file goes into the same folder as the loaded file
        string Session_Folder = Path.GetDirectoryName( _Loaded_File_Path );
        string Session_Name   = Path.GetFileNameWithoutExtension( _Loaded_File_Path );
        Output_Path           = Path.Combine( Session_Folder, $"{Session_Name}_Analyzed.csv" );
      }

      Show_Progress( "Computing post-processing analysis...", _Foreground_Color );

      await Task.Run(
        () =>
        {
          using var Writer = new StreamWriter( Output_Path );

          Writer.WriteLine( $"# Post-processed analysis" );
          Writer.WriteLine( $"# Series A : {S1.Name}  GPIB {S1.Address}  NPLC {S1.NPLC}" );
          Writer.WriteLine( $"# Series B : {S2.Name}  GPIB {S2.Address}  NPLC {S2.NPLC}" );
          Writer.WriteLine( $"# Generated: {Path.GetFileNameWithoutExtension( Output_Path )}" );
          Writer.WriteLine( $"# Points   : {Count}" );
          Writer.WriteLine( "Timestamp,Value_A,Value_B,Delta,Delta_uV,Delta_Ms,Rolling_Mean_Delta,Rolling_" +
                            "StdDev_Delta" );

          // ── Rolling stats accumulators ────────────────────────────────
          const int Rolling_Window = 100;
          var       Delta_Window   = new Queue<double>( Rolling_Window + 1 );
          double    Rolling_Sum    = 0.0;
          double    Rolling_Sum_Sq = 0.0;

          for ( int I = 0; I < Count; I++ )
          {
            var    P1 = S1.Points[ I ];
            var    P2 = S2.Points[ I ];

            double Delta    = P1.Value - P2.Value;
            double Delta_uV = Delta * 1_000_000.0;

            // Inter-sample interval (ms) — use S1 timestamp
            double Delta_Ms =
              I == 0 ? 0.0 : ( S1.Points[ I ].Time - S1.Points[ I - 1 ].Time ).TotalMilliseconds;

            // ── Rolling window ────────────────────────────────────────
            Delta_Window.Enqueue( Delta );
            Rolling_Sum    += Delta;
            Rolling_Sum_Sq += Delta * Delta;

            if ( Delta_Window.Count > Rolling_Window )
            {
              double Old      = Delta_Window.Dequeue();
              Rolling_Sum    -= Old;
              Rolling_Sum_Sq -= Old * Old;
            }

            int    N              = Delta_Window.Count;
            double Rolling_Mean   = Rolling_Sum / N;
            double Variance       = ( Rolling_Sum_Sq / N ) - ( Rolling_Mean * Rolling_Mean );
            double Rolling_StdDev = Variance > 0 ? Math.Sqrt( Variance ) : 0.0;

            Writer.WriteLine( $"{P1.Time:yyyy-MM-dd HH:mm:ss.fff}," +
                              $"{P1.Value.ToString( "G10", CultureInfo.InvariantCulture )}," +
                              $"{P2.Value.ToString( "G10", CultureInfo.InvariantCulture )}," +
                              $"{Delta.ToString( "G10", CultureInfo.InvariantCulture )}," +
                              $"{Delta_uV.ToString( "F3", CultureInfo.InvariantCulture )}," +
                              $"{Delta_Ms.ToString( "F1", CultureInfo.InvariantCulture )}," +
                              $"{Rolling_Mean.ToString( "G8", CultureInfo.InvariantCulture )}," +
                              $"{Rolling_StdDev.ToString( "G8", CultureInfo.InvariantCulture )}" );
          }
        } );

      // ── Summary stats on the delta series ────────────────────────────
      double Mean_Delta = 0, StdDev_Delta = 0, Min_Delta = double.MaxValue, Max_Delta = double.MinValue;
      double Sum = 0, Sum_Sq = 0;

      for ( int I = 0; I < Count; I++ )
      {
        double D  = S1.Points[ I ].Value - S2.Points[ I ].Value;
        Sum      += D;
        Sum_Sq   += D * D;
        if ( D < Min_Delta )
          Min_Delta = D;
        if ( D > Max_Delta )
          Max_Delta = D;
      }
      Mean_Delta   = Sum / Count;
      double Var   = ( Sum_Sq / Count ) - ( Mean_Delta * Mean_Delta );
      StdDev_Delta = Var > 0 ? Math.Sqrt( Var ) : 0.0;

      Show_Progress( $"Analysis complete → {Path.GetFileName( Output_Path )}  " +
                       $"|  Δ mean: {Mean_Delta * 1e6:F3} µV  " + $"|  Δ σ: {StdDev_Delta * 1e6:F3} µV  " +
                       $"|  Δ min/max: {Min_Delta * 1e6:F3} / {Max_Delta * 1e6:F3} µV",
                     _Foreground_Color );
    }

    private void Run_Auto_Analysis_If_Enabled()
    {
      if ( ! _Settings.Auto_Analyze_After_Recording )
        return;
      if ( _Series == null || _Series.Count == 0 )
        return;

      if ( _Settings.Analysis_Series_Count == 2 && _Series.Count >= 2 && _Series[ 0 ].Points.Count > 0 &&
           _Series[ 1 ].Points.Count > 0 )
      {
        // Full two-series comparison popup
        var Popup = new Analysis_Popup_Form( _Series[ 0 ].Points,
                                             _Series[ 1 ].Points,
                                             _Series[ 0 ].Name,
                                             _Series[ 1 ].Name,
                                             _Theme );
        Popup.Show( this );
      }
      else if ( _Series[ 0 ].Points.Count > 0 )
      {
        // Single series — show the lightweight summary panel
        Show_Analysis_Results( _Series.Where( S => S.Points.Count >= 2 ).ToList() );
      }
    }
    private void Show_Analysis_Results( List<Instrument_Series> Series )
    {
      // Remove old panel if present
      if ( _Analysis_Results_Panel != null )
      {
        this.Controls.Remove( _Analysis_Results_Panel );
        _Analysis_Results_Panel.Dispose();
      }

      _Analysis_Results_Panel = new Panel { BackColor   = _Theme.Background,
                                            ForeColor   = _Theme.Foreground,
                                            BorderStyle = BorderStyle.FixedSingle,
                                            Padding     = new Padding( 10 ),
                                            AutoScroll  = true,
                                            Size        = new Size( 360, 280 ),
                                            Anchor      = AnchorStyles.Bottom | AnchorStyles.Right };

      _Analysis_Results_Panel.Location =
        new Point( this.ClientSize.Width - _Analysis_Results_Panel.Width - 12,
                   this.ClientSize.Height - _Analysis_Results_Panel.Height - 32 );

      // ── Title ────────────────────────────────────────────────────────
      var Title_Label = new Label { Text      = "📊 Auto Analysis",
                                    Font      = new Font( this.Font, FontStyle.Bold ),
                                    ForeColor = _Theme.Foreground,
                                    AutoSize  = true,
                                    Location  = new Point( 8, 8 ) };

      var Close_Button = new Button { Text      = "✕",
                                      Size      = new Size( 24, 24 ),
                                      FlatStyle = FlatStyle.Flat,
                                      ForeColor = _Theme.Foreground,
                                      BackColor = _Theme.Background,
                                      Location  = new Point( _Analysis_Results_Panel.Width - 36, 4 ),
                                      Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                                      Cursor    = Cursors.Hand };
      Close_Button.FlatAppearance.BorderSize  = 0;
      Close_Button.Click                     += ( s, e ) =>
      {
        this.Controls.Remove( _Analysis_Results_Panel );
        _Analysis_Results_Panel.Dispose();
        _Analysis_Results_Panel = null;
      };

      // _Analysis_Results_Panel.Controls.Add ( Title_Label );
      _Analysis_Results_Panel.Controls.Add( Close_Button );

      int Y = 38;

      foreach ( var S in Series )
      {
        // Color swatch + series name
        var Color_Swatch =
          new Panel { BackColor = S.Line_Color, Size = new Size( 12, 12 ), Location = new Point( 8, Y + 3 ) };

        var Series_Label = new Label { Text      = S.Name,
                                       Font      = new Font( this.Font, FontStyle.Bold ),
                                       ForeColor = _Theme.Foreground,
                                       AutoSize  = true,
                                       Location  = new Point( 26, Y ) };

        _Analysis_Results_Panel.Controls.Add( Color_Swatch );
        _Analysis_Results_Panel.Controls.Add( Series_Label );
        Y += 20;

        // Duration
        string Duration_Str = "—";
        if ( S.Points.Count >= 2 )
        {
          TimeSpan Duration = S.Points[ S.Points.Count - 1 ].Time - S.Points[ 0 ].Time;
          Duration_Str =
            Duration.TotalSeconds < 60 ? $"{Duration.TotalSeconds:F1} s" : $"{Duration.TotalMinutes:F1} min";
        }

        // Build stat rows — respect settings toggles
        var Rows = new List<( string Key, string Value )>();

        if ( _Settings.Analysis_Show_Min_Max )
        {
          Rows.Add( ( "Min", $"{S.Get_Min():G6}" ) );
          Rows.Add( ( "Max", $"{S.Get_Max():G6}" ) );
        }
        if ( _Settings.Analysis_Show_Mean )
          Rows.Add( ( "Avg", $"{S.Get_Average():G6}" ) );
        if ( _Settings.Analysis_Show_Std_Dev )
          Rows.Add( ( "Std Dev", $"{S.Get_StdDev():G4}" ) );
        if ( _Settings.Analysis_Show_RMS )
          Rows.Add( ( "RMS", $"{S.Get_RMS():G4}" ) );
        if ( _Settings.Analysis_Show_Min_Max )
          Rows.Add( ( "Range", $"{S.Get_Range():G4}" ) );
        if ( _Settings.Analysis_Show_Trend )
          Rows.Add( ( "Trend", S.Get_Trend() ) );
        if ( _Settings.Analysis_Show_Sample_Rate )
          Rows.Add( ( "Rate", $"{S.Get_Sample_Rate():F2} S/s" ) );

        Rows.Add( ( "Samples", $"{S.Points.Count:N0}" ) );
        Rows.Add( ( "Duration", Duration_Str ) );

        if ( _Settings.Analysis_Show_Errors )
          Rows.Add( ( "Errors", $"{S.Total_Errors}" ) );

        foreach ( var ( Key, Value ) in Rows )
        {
          var Row = new Label { Text      = $"  {Key,-10} {Value}",
                                ForeColor = _Theme.Foreground,
                                AutoSize  = true,
                                Location  = new Point( 8, Y ),
                                Font      = new Font( "Consolas", 8.5f ) };
          _Analysis_Results_Panel.Controls.Add( Row );
          Y += 17;
        }

        Y += 10; // gap between series
      }

      this.Controls.Add( _Analysis_Results_Panel );
      _Analysis_Results_Panel.BringToFront();
    }

    private void Analyze_Poll_Timing_Button_Click( object    Sender,
                                                   EventArgs E ) => Show_Timing_Analysis_Popup();

    private void Show_Timing_Analysis_Popup()
    {
      if ( _Timing_Count < 2 )
      {
        Show_Progress( "Timing analysis requires at least 2 loaded samples.", Color.Orange );
        return;
      }

      // ── Snapshot circular buffer in chronological order ───────────
      int Count   = Math.Min( _Timing_Count, _Timing_Buffer_Size );
      var Records = new List<Poll_Timing_Analysis>( Count );

      for ( int I = 0; I < Count; I++ )
      {
        int Idx = ( _Timing_Head - Count + I + _Timing_Buffer_Size ) % _Timing_Buffer_Size;
        var S   = _Cycle_Timing[ Idx ];

        Records.Add( new Poll_Timing_Analysis {
          Timestamp     = S.Cycle_Time,
          Cycle         = I,
          Total_Ms      = S.Total_Ms,
          Comm_Ms       = S.Comm_Ms,
          AddrSwitch_Ms = S.Address_Switch_Ms,
          UI_Ms         = S.UI_Ms,
          Record_Ms     = S.Record_Ms,
          Had_Error     = S.Had_Error,
        } );
      }

      var Popup = new Poll_Timing_Analysis_Form( Records, _Theme );
      Popup.Show( this ); // non-modal, matches Show_Analysis_Popup behaviour
    }

    private void Analyze_Instrument_Data_Button_Click( object sender, EventArgs e )
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
  }
}
