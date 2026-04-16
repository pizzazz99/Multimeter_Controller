
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Application_Settings.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Single source of truth for all user-configurable application state.
//   Settings are serialized to JSON in the user's AppData folder and loaded
//   at startup.  The two Chart_Theme references (Panel_Theme, Chart_Theme)
//   are excluded from the main JSON file and loaded from their own separate
//   theme files instead.  All numeric properties are validated and clamped
//   on every save via Validate_And_Fix().
//
// PERSISTENCE
//   File path  : %APPDATA%\Multimeter_Controller\multi_poll_settings.json
//   Format     : indented UTF-8 JSON via System.Text.Json
//   Themes     : loaded separately from Chart_Theme.Panel_Theme_Path and
//                Chart_Theme.Chart_Theme_Path; marked [JsonIgnore] so they
//                are never embedded in the main settings file.
//
// PROPERTY GROUPS
//
//   Connection / Prologix
//     Default_IP_Address              Target IP, empty = auto-detect
//     Default_Prologic_Port           TCP port (default 1234)
//     Network_Scan_Subnet             Subnet prefix for discovery, empty = auto
//     Prologic_Scan_Timeout_MS        UDP/TCP scan timeout in ms
//     Prologic_MAC_Address            OUI/MAC prefix for Prologix device matching
//     Default_GPIB_Instrument_Address GPIB address (0–30, default 10)
//     Prologix_Auto_Read              Enable Prologix auto-read mode
//     Prologix_Read_Tmo_Ms            Read response timeout in ms
//     Prologix_Fetch_Ms               Delay between fetch cycles in ms
//
//   HP3458A
//     Send_Reset_On_Connect_3458      Send RESET command on connect
//     Reset_Settle_Delay_Ms           Post-reset settle wait in ms
//     Send_End_Always_3458            Assert END on every GPIB write
//     Instrument_Settle_Ms            General post-command settle time in ms
//     ERR_Read_Delay_Ms               Delay before reading ERR? response in ms
//     NPLC_Apply_Delay_Ms             Delay after applying NPLC setting in ms
//     Default_NPLC_3458               NPLC string for HP3458A (e.g. "1", "10")
//     Default_Trig_Mode_3458          Trigger mode string (e.g. "TRIG HOLD")
//
//   Display / Chart
//     Tooltip_Distance_Threshold      Max pixel distance for hover tooltip (10–200)
//     Show_Tooltips_On_Hover          Enable hover tooltips
//     Tooltip_Display_Duration_Ms     How long a tooltip stays visible (500–10000)
//     Chart_Refresh_Rate_Ms           Chart repaint interval (16–1000)
//     Show_Grid_Lines                 Draw chart grid lines
//     Show_Data_Dots                  Render a dot at each data point
//     Data_Dot_Size                   Dot diameter in pixels (1–10)
//     Line_Thickness                  Series line width in pixels (1–10)
//     Default_To_Combined_View        Start in combined (single-chart) view
//     Default_To_Normalized_View      Start with Y-axis normalized
//     Show_Legend_On_Startup          Show series legend when chart opens
//
//   Polling / Data Collection
//     Max_Display_Points              Max points rendered on chart (10–50 000 000)
//     Stop_Polling_At_Max_Display_Points  Stop vs. roll-and-warn when limit reached
//     Default_Poll_Delay_Ms           Inter-poll delay in ms (50–60 000)
//     Default_NPLC                    NPLC string applied to new instruments
//     Default_Measurement_Type        Measurement function for new instruments
//     Default_Continuous_Poll         Start continuous polling by default
//     Default_GPIB_Timeout_Ms         GPIB operation timeout in ms (1000–60 000)
//     Max_Retry_Attempts              Retry count per failed command (0–10)
//     Max_Consecutive_Errors_Before_Disable  Errors before instrument is disabled (1–100)
//     Auto_Retry_Failed_Instruments   Automatically re-enable disabled instruments
//     Retry_Delay_Seconds             Seconds between re-enable attempts (1–300)
//     Skew_Warning_Threshold_Seconds  Inter-instrument timestamp skew → orange (0.2–10)
//     Stale_Data_Threshold_Seconds    Age of last reading before red alert (0.2–60);
//                                     always enforced > Skew_Warning_Threshold_Seconds
//
//   Rendering
//     Use_GPU_Rendering               Use SKGLControl (SkiaSharp/OpenGL) if available
//     GPU_Rendering_Available         Set at runtime by Detect_Hardware(); persisted
//                                     so the UI can reflect last-known state on load
//     Rendering_Mode_Display          [JsonIgnore] Human-readable mode string
//     Discrete_GPU_Available          [JsonIgnore] Runtime-only; set by Detect_Hardware()
//
//   File / Data Management
//     Default_Save_Folder             Root folder for CSV exports ("Graph_Captures")
//     Resolved_Save_Folder            [JsonIgnore] Fully resolved absolute path
//     Filename_Pattern                Token pattern: {date}, {time}, {function}
//     Enable_Auto_Save                Periodic auto-save while polling
//     Auto_Save_Interval_Minutes      Auto-save cadence in minutes (1–1440)
//     Auto_Save_On_Stop               Save automatically when polling stops
//     Include_Statistics_In_Save      Append stats block to CSV output
//     Prompt_Before_Clear             Confirm dialog before clearing data
//     Auto_Load_Last_Session          Re-open last CSV on startup
//     Export_Format                   "CSV", "Excel", or "JSON"
//
//   Performance / Memory
//     Max_Data_Points_In_Memory       Hard cap on stored points (1 000–10 000 000)
//     Warning_Threshold_Percent       % of max before memory warning (50–99)
//     Warn_At_Threshold               Show warning when threshold is reached
//     Throttle_When_Many_Points       Reduce refresh rate above threshold
//     Throttle_Point_Threshold        Point count that triggers throttling (1 000–1 000 000)
//     Auto_Trim_Old_Data              Discard oldest points when limit is reached
//     Keep_Last_N_Points              Points to retain when trimming (100–1 000 000)
//     Reduce_Refresh_Rate_When_Large  Halve refresh rate for large datasets
//
//   Decimation
//     Enable_Decimation               Draw only every Nth point above threshold
//     Decimation_Threshold            Point count above which decimation activates
//                                     (10–1 000 000, default 10 000)
//     Decimation_Step                 Draw every Nth point when active (2–1000)
//     NOTE: Decimation is display-only. All stored points are always written to CSV.
//
//   Analysis
//     Auto_Analyze_After_Recording    Show analysis popup when recording stops
//     Analysis_Show_GPU_Comparison    Include GPU vs CPU timing in results
//     Analysis_Series_Count           1 = single-series summary, 2 = full comparison
//     Analysis_Show_Mean              Include mean/average stat
//     Analysis_Show_Std_Dev           Include standard deviation stat
//     Analysis_Show_Min_Max           Include min/max stat
//     Analysis_Show_RMS               Include RMS stat
//     Analysis_Show_Trend             Include trend direction stat
//     Analysis_Show_Sample_Rate       Include sample rate stat
//     Analysis_Show_Errors            Include error count stat
//
//   UI / UX
//     Default_Window_Title            Main window title bar text
//     Remember_Window_Size            Restore last width/height on startup
//     Remember_Window_Position        Restore last X/Y on startup
//     Last_Window_Width/Height/X/Y    Persisted window geometry
//     Enable_Keyboard_Pan             Arrow/Page keys pan the chart
//     Enable_Ctrl_Scroll_Zoom         Ctrl+scroll wheel zooms the chart
//     Show_Progress_Messages          Status-bar progress text while polling
//     Flash_On_Error                  Flash the main window title on error
//     Play_Sound_On_Complete          Play system sound when polling finishes
//     Analysis_Tab_Alignment          TabControl alignment (Left/Right/Top);
//                                     stored as string, exposed as TabAlignment
//
//   Zoom
//     Default_Zoom_Level              Startup zoom (1–100, 50 = 1.0×)
//     Zoom_Sensitivity                Mouse-wheel zoom step multiplier (0.1–5.0)
//     Remember_Zoom_Level             Persist last zoom between sessions
//     Last_Zoom_Level                 Zoom value saved on exit (1–100)
//
// KEY METHODS
//
//   Load()                   Deserializes from JSON; returns defaults if the
//                            file is missing or corrupt.  Always reloads both
//                            theme files after deserialization.
//   Save()                   Serializes to indented JSON.  Shows a MessageBox
//                            on write failure rather than throwing.
//   Validate_And_Fix()       Clamps every numeric property to its allowed range,
//                            enforces cross-property invariants (e.g. stale >
//                            skew), and fills in empty strings with safe defaults.
//                            Must be called before Save() — Settings_Form does
//                            this automatically.
//   Initialize_Default_Save_Folder()
//                            Sets Default_Save_Folder to "Graph_Captures" if
//                            currently null or whitespace.
//   Detect_Hardware()        Runs Detect_Discrete_GPU() via WMI Win32_VideoController
//                            and sets Discrete_GPU_Available and
//                            GPU_Rendering_Available accordingly.  Should be
//                            called once at startup and again after Reset.
//   Set_Theme()              Replaces Panel_Theme or Chart_Theme and fires
//                            Theme_Changed so all open chart forms repaint
//                            immediately without restarting.
//   Apply_Theme_To_Control() / Apply_Theme_To_Single_Control()
//                            Apply ForeColor/BackColor from a Chart_Theme to a
//                            specific control, dispatching on Control.Tag for
//                            Response_List, Instrument_List, Command_History,
//                            Command_List, and Detail_List.
//
// EVENTS
//   Theme_Changed            Raised by Set_Theme() after either theme property
//                            is updated.  Subscribers (chart forms, panels)
//                            should re-read the relevant theme and repaint.
//
// GPU DETECTION NOTES
//   Detect_Discrete_GPU() queries Win32_VideoController via WMI and excludes:
//     • Software / virtual renderers (Microsoft Basic Display, WARP, VMware,
//       VirtualBox, Hyper-V, Remote Desktop)
//     • Intel integrated graphics (HD/UHD Graphics, Iris, Iris Plus, Iris Xe)
//     • AMD APU integrated graphics (Radeon Vega, Radeon Graphics, Radeon NNNm)
//     • Any adapter reporting less than 512 MB of dedicated VRAM (fallback heuristic)
//   Intel Arc discrete cards and AMD Radeon RX discrete cards pass all filters
//   and correctly return true.
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════

using LibreHardwareMonitor.Interop.PowerMonitor;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using Trace_Execution_Namespace;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{

  public class Application_Settings
  {
    // In Application_Settings:

    [JsonIgnore]
    public Chart_Theme Panel_Theme { get; set; } = Chart_Theme.Load( Chart_Theme.Panel_Theme_Path );

    [JsonIgnore]
    public Chart_Theme Chart_Theme { get; set; } = Chart_Theme.Load( Chart_Theme.Chart_Theme_Path );

    public int         Default_Prologic_Port { get; set; } = 1234;

    [ JsonPropertyName( "prologic_scan_timeout_ms" ) ]
    public int Prologic_Scan_Timeout_MS { get; set; } = 50;

    [ JsonPropertyName( "prologic_mac_address" ) ]
    public string Prologic_MAC_Address { get; set; } = "00-21-69-01-3D-DD";

    [ JsonPropertyName( "default_gpib_instrument_address" ) ]
    public int Default_GPIB_Instrument_Address { get; set; } = 10;

    [ JsonPropertyName( "send_reset_on_connect_3458" ) ]
    public bool Send_Reset_On_Connect_3458 { get; set; } = true;

    [ JsonPropertyName( "reset_settle_delay_ms" ) ]
    public int Reset_Settle_Delay_Ms { get; set; } = 2000;

    [ JsonPropertyName( "send_end_always_3458" ) ]
    public bool Send_End_Always_3458 { get; set; } = true;

    [ JsonPropertyName( "instrument_settle_ms" ) ]
    public int Instrument_Settle_Ms { get; set; } = 500;

    [ JsonPropertyName( "prologix_fetch_ms" ) ]
    public int Prologix_Fetch_Ms { get; set; } = 100;

    [ JsonPropertyName( "err_read_delay_ms" ) ]
    public int ERR_Read_Delay_Ms { get; set; } = 500;

    [ JsonPropertyName( "nplc_apply_delay_ms" ) ]
    public int NPLC_Apply_Delay_Ms { get; set; } = 50;

    [ JsonPropertyName( "default_nplc_3458" ) ]
    public string Default_NPLC_3458 { get; set; } = "1";

    [ JsonPropertyName( "default_trig_mode_3458" ) ]
    public string Default_Trig_Mode_3458 { get; set; } = "TRIG HOLD";

    [ JsonPropertyName( "prologix_auto_read" ) ]
    public bool Prologix_Auto_Read { get; set; } = false;

    [ JsonPropertyName( "prologix_read_tmo_ms" ) ]
    public int Prologix_Read_Tmo_Ms { get; set; } = 5000;

    [ JsonPropertyName( "network_scan_subnet" ) ]
    public string Network_Scan_Subnet { get; set; } = "";

    [ JsonPropertyName( "default_ip_address" ) ]
    public string Default_IP_Address { get; set; } = "";

    // ===== CHART/DISPLAY SETTINGS =====

    [ JsonPropertyName( "tooltip_distance_threshold" ) ]
    public int Tooltip_Distance_Threshold { get; set; } = 50;

    [ JsonPropertyName( "show_tooltips_on_hover" ) ]
    public bool Show_Tooltips_On_Hover { get; set; } = true;

    [ JsonPropertyName( "tooltip_display_duration_ms" ) ]
    public int Tooltip_Display_Duration_Ms { get; set; } = 2000;

    [ JsonPropertyName( "chart_refresh_rate_ms" ) ]
    public int Chart_Refresh_Rate_Ms { get; set; } = 100;

    [ JsonPropertyName( "show_grid_lines" ) ]
    public bool Show_Grid_Lines { get; set; } = true;

    [ JsonPropertyName( "show_data_dots" ) ]
    public bool Show_Data_Dots { get; set; } = true;

    [ JsonPropertyName( "data_dot_size" ) ]
    public int Data_Dot_Size { get; set; } = 4;

    [ JsonPropertyName( "line_thickness" ) ]
    public int Line_Thickness { get; set; } = 2;

    [ JsonPropertyName( "default_to_combined_view" ) ]
    public bool Default_To_Combined_View { get; set; } = false;

    [ JsonPropertyName( "default_to_normalized_view" ) ]
    public bool Default_To_Normalized_View { get; set; } = false;

    [ JsonPropertyName( "show_legend_on_startup" ) ]
    public bool Show_Legend_On_Startup { get; set; } = false;

    // ===== POLLING/DATA COLLECTION =====

    [ JsonPropertyName( "max_display_points" ) ]
    public int Max_Display_Points { get; set; } = 50_000_000;

    [ JsonPropertyName( "default_poll_delay_ms" ) ]
    public int Default_Poll_Delay_Ms { get; set; } = 500;

    [ JsonPropertyName( "stop_polling_at_max_display_points" ) ]
    public bool Stop_Polling_At_Max_Display_Points { get; set; } = false; // default = warn only

    [ JsonPropertyName( "default_nplc" ) ]
    public string Default_NPLC { get; set; } = "1";

    [ JsonPropertyName( "default_measurement_type" ) ]
    public string Default_Measurement_Type { get; set; } = "DC Voltage";

    [ JsonPropertyName( "default_continuous_poll" ) ]
    public bool Default_Continuous_Poll { get; set; } = true;

    [ JsonPropertyName( "default_gpib_timeout_ms" ) ]
    public int Default_GPIB_Timeout_Ms { get; set; } = 5000;

    [ JsonPropertyName( "max_retry_attempts" ) ]
    public int Max_Retry_Attempts { get; set; } = 3;

    [ JsonPropertyName( "max_consecutive_errors_before_disable" ) ]
    public int Max_Consecutive_Errors_Before_Disable { get; set; } = 10;

    [ JsonPropertyName( "auto_retry_failed_instruments" ) ]
    public bool Auto_Retry_Failed_Instruments { get; set; } = true;

    [ JsonPropertyName( "retry_delay_seconds" ) ]
    public int Retry_Delay_Seconds { get; set; } = 5;

    [ JsonPropertyName( "skew_warning_threshold_seconds" ) ]
    public double Skew_Warning_Threshold_Seconds { get; set; } = 1.0;

    [ JsonPropertyName( "stale_data_threshold_seconds" ) ]
    public double Stale_Data_Threshold_Seconds { get; set; } = 3.0;

    // ── Rendering ─────────────────────────────────────────────────────────────
    // Controls whether the chart panel uses GPU-accelerated SkiaSharp (SKGLControl)
    // or falls back to CPU-based GDI+. GPU mode requires OpenGL 3.0 or later.
    // If GPU mode is selected but OpenGL is not available, the app automatically
    // falls back to CPU rendering and updates this setting accordingly.
    [ JsonPropertyName( "use_gpu_rendering" ) ]
    public bool Use_GPU_Rendering { get; set; } = false;

    [ JsonPropertyName( "gpu_rendering_available" ) ]
    public bool GPU_Rendering_Available { get; set; } = false; // detected at runtime

    [ JsonIgnore ]
    public string Rendering_Mode_Display =>
      Use_GPU_Rendering && GPU_Rendering_Available     ? "GPU " + "(SkiaSharp/" + "OpenGL)"
      : Use_GPU_Rendering && ! GPU_Rendering_Available ? "CPU (GPU " + "requested " + "but " + "unavailable)"
                                                       : "CPU (GDI+)";

    [ JsonIgnore ] // runtime-detected, no need to persist
    public bool Discrete_GPU_Available { get; set; } = false;

    // ===== FILE/DATA MANAGEMENT =====

    [ JsonIgnore ]
    public string Resolved_Save_Folder => Multimeter_Common_Helpers_Class.Get_Graph_Captures_Folder( this );

    [ JsonPropertyName( "default_save_folder" ) ]
    public string Default_Save_Folder { get; set; } = "Graph_Captures";

    [ JsonPropertyName( "filename_pattern" ) ]
    public string Filename_Pattern { get; set; } = "{date}_{time}_{function}";

    [ JsonPropertyName( "enable_auto_save" ) ]
    public bool Enable_Auto_Save { get; set; } = false;

    [ JsonPropertyName( "auto_save_interval_minutes" ) ]
    public int Auto_Save_Interval_Minutes { get; set; } = 5;

    [ JsonPropertyName( "auto_save_on_stop" ) ]
    public bool Auto_Save_On_Stop { get; set; } = false;

    [ JsonPropertyName( "include_statistics_in_save" ) ]
    public bool Include_Statistics_In_Save { get; set; } = true;

    [ JsonPropertyName( "prompt_before_clear" ) ]
    public bool Prompt_Before_Clear { get; set; } = true;

    [ JsonPropertyName( "auto_load_last_session" ) ]
    public bool Auto_Load_Last_Session { get; set; } = false;

    [ JsonPropertyName( "export_format" ) ]
    public string Export_Format { get; set; } = "CSV";

    // ===== PERFORMANCE/MEMORY =====

    [ JsonPropertyName( "max_data_points_in_memory" ) ]
    public int Max_Data_Points_In_Memory { get; set; } = 100000;

    [ JsonPropertyName( "warning_threshold_percent" ) ]
    public int Warning_Threshold_Percent { get; set; } = 80;

    [ JsonPropertyName( "warn_at_threshold" ) ]
    public bool Warn_At_Threshold { get; set; } = true;

    [ JsonPropertyName( "throttle_when_many_points" ) ]
    public bool Throttle_When_Many_Points { get; set; } = true;

    [ JsonPropertyName( "throttle_point_threshold" ) ]
    public int Throttle_Point_Threshold { get; set; } = 10000;

    [ JsonPropertyName( "auto_trim_old_data" ) ]
    public bool Auto_Trim_Old_Data { get; set; } = false;

    [ JsonPropertyName( "keep_last_n_points" ) ]
    public int Keep_Last_N_Points { get; set; } = 10000;

    [ JsonPropertyName( "reduce_refresh_rate_when_large" ) ]
    public bool Reduce_Refresh_Rate_When_Large { get; set; } = true;

    // ── Decimation ────────────────────────────────────────────────────────────
    // When enabled, the chart draws at most Decimation_Max_Draw points
    // regardless of how many are stored in memory.  Decimation only kicks in
    // when the visible point count exceeds Decimation_Threshold.
    // All stored points are always written to CSV — decimation is display-only.

    [ JsonPropertyName( "enable_decimation" ) ]
    public bool Enable_Decimation { get; set; } = true;

    [ JsonPropertyName( "decimation_threshold" ) ]
    public int Decimation_Threshold { get; set; } = 10_000; // start decimating above this

    [ JsonPropertyName( "decimation_step" ) ]
    public int Decimation_Step { get; set; } = 10; // draw every Nth point

    // ===== Analysis ======
    [ JsonPropertyName( "auto_analyze_after_recording" ) ]
    public bool Auto_Analyze_After_Recording { get; set; } = false;

    [ JsonPropertyName( "analysis_show_gpu_comparison" ) ]
    public bool Analysis_Show_GPU_Comparison { get; set; } = true;

    [ JsonPropertyName( "analysis_series_count" ) ]
    public int Analysis_Series_Count { get; set; } = 2;

    [ JsonPropertyName( "analysis_show_mean" ) ]
    public bool Analysis_Show_Mean { get; set; } = true;

    [ JsonPropertyName( "analysis_show_std_dev" ) ]
    public bool Analysis_Show_Std_Dev { get; set; } = true;

    [ JsonPropertyName( "analysis_show_min_max" ) ]
    public bool Analysis_Show_Min_Max { get; set; } = true;

    [ JsonPropertyName( "analysis_show_rms" ) ]
    public bool Analysis_Show_RMS { get; set; } = true;

    [ JsonPropertyName( "analysis_show_trend" ) ]
    public bool Analysis_Show_Trend { get; set; } = true;

    [ JsonPropertyName( "analysis_show_sample_rate" ) ]
    public bool Analysis_Show_Sample_Rate { get; set; } = true;

    [ JsonPropertyName( "analysis_show_errors" ) ]
    public bool Analysis_Show_Errors { get; set; } = true;

    // ===== UI/UX PREFERENCES =====

    [ JsonPropertyName( "default_window_title" ) ]
    public string Default_Window_Title { get; set; } = "Multi-Instrument Poller";

    [ JsonPropertyName( "remember_window_size" ) ]
    public bool Remember_Window_Size { get; set; } = true;

    [ JsonPropertyName( "remember_window_position" ) ]
    public bool Remember_Window_Position { get; set; } = true;

    [ JsonPropertyName( "last_window_width" ) ]
    public int Last_Window_Width { get; set; } = 1340;

    [ JsonPropertyName( "last_window_height" ) ]
    public int Last_Window_Height { get; set; } = 730;

    [ JsonPropertyName( "last_window_x" ) ]
    public int Last_Window_X { get; set; } = -1;

    [ JsonPropertyName( "last_window_y" ) ]
    public int Last_Window_Y { get; set; } = -1;

    [ JsonPropertyName( "enable_keyboard_pan" ) ]
    public bool Enable_Keyboard_Pan { get; set; } = true;

    [ JsonPropertyName( "enable_ctrl_scroll_zoom" ) ]
    public bool Enable_Ctrl_Scroll_Zoom { get; set; } = true;

    [ JsonPropertyName( "show_progress_messages" ) ]
    public bool Show_Progress_Messages { get; set; } = true;

    [ JsonPropertyName( "flash_on_error" ) ]
    public bool Flash_On_Error { get; set; } = true;

    [ JsonPropertyName( "play_sound_on_complete" ) ]
    public bool Play_Sound_On_Complete { get; set; } = false;

    [ JsonPropertyName( "analysis_tab_alignment" ) ]
    public string Analysis_Tab_Alignment_str { get; set; } = "Left";

    [ JsonIgnore ]
    public TabAlignment Analysis_Tab_Alignment
    {
      get => Analysis_Tab_Alignment_str switch {
        "Left"  => TabAlignment.Left,
        "Right" => TabAlignment.Right,
        _       => TabAlignment.Top,
      };
      set => Analysis_Tab_Alignment_str = value switch {
        TabAlignment.Left  => "Left",
        TabAlignment.Right => "Right",
        _                  => "Top",
      };
    }

    // ===== ZOOM SETTINGS =====

    [ JsonPropertyName( "default_zoom_level" ) ]
    public int Default_Zoom_Level { get; set; } = 50;

    [ JsonPropertyName( "zoom_sensitivity" ) ]
    public double Zoom_Sensitivity { get; set; } = 1.0;

    [ JsonPropertyName( "remember_zoom_level" ) ]
    public bool Remember_Zoom_Level { get; set; } = false;

    [ JsonPropertyName( "last_zoom_level" ) ]
    public int   Last_Zoom_Level { get; set; } = 50;

    // ===== LOAD/SAVE METHODS =====

    public event EventHandler? Theme_Changed;

    public void Set_Theme( Chart_Theme New_Theme, string Theme_Name )
    {
      if (Theme_Name == "Panel_Theme")
      {
        Panel_Theme = New_Theme;
      }
      else if (Theme_Name == "Chart_Theme")
      {
        Chart_Theme = New_Theme;
      }
      else
      {
        return;
      }

      Theme_Changed?.Invoke( this, EventArgs.Empty );
    }

    public void Apply_Theme_To_Single_Control( Control Control, Chart_Theme Theme )
    {
      using var block = Trace_Block.Start_If_Enabled();

      switch ( Control.Tag?.ToString() )
      {
        case "Response_List" :
          Capture_Trace.Write( $"Applying Theme to: Response_Text_Box" );
          Control.ForeColor = Theme.Foreground;
          Control.BackColor = Theme.Background;

          break;

        case "Instrument_List" :
          Capture_Trace.Write( $"Applying Theme to: Instruments_List" );
          Control.ForeColor = Theme.Foreground;
          Control.BackColor = Theme.Background;

          break;
        case "Command_History" :
          Capture_Trace.Write( $"Applying Theme to: Command_History" );
          Control.ForeColor = Theme.Foreground;
          Control.BackColor = Theme.Background;

          break;
        case "Command_List" :
          Capture_Trace.Write( $"Applying Theme to: Command_List" );
          Control.ForeColor = Theme.Foreground;
          Control.BackColor = Theme.Background;

          break;
        case "Detail_List" :
          Capture_Trace.Write( $"Applying Theme to: Detail_List" );

          Control.ForeColor = Theme.Foreground;
          Control.BackColor = Theme.Background;
          break;
      }
    }

    public void Apply_Theme_To_Control( Control Control, Chart_Theme Theme, string Tag )
    {
      using var block = Trace_Block.Start_If_Enabled();

      Capture_Trace.Write( $"Applying Theme to: {Tag}" );

      Control.ForeColor = Theme.Foreground;
      Control.BackColor = Theme.Background;
    }

    [ System.Runtime.InteropServices.DllImport( "dxgi.dll" ) ]
    private static extern int CreateDXGIFactory( [ System.Runtime.InteropServices.In ] ref Guid riid,
                                                 out nint                                       pp_factory );

    public void               Detect_Hardware()
    {
      Discrete_GPU_Available  = Detect_Discrete_GPU();
      GPU_Rendering_Available = Discrete_GPU_Available && Use_GPU_Rendering;
    }

    private static bool Detect_Discrete_GPU()
    {
      try
      {
        using var Searcher = new System.Management.ManagementObjectSearcher( "SELECT * FROM " + "Win32_" +
                                                                             "VideoContr" + "oller" );
        foreach ( System.Management.ManagementObject Obj in Searcher.Get() )
        {
          string Name   = Obj[ "Name" ]?.ToString() ?? "";
          string Compat = Obj[ "AdapterCompatibility" ]?.ToString() ?? "";

          ulong  VRAM = 0;
          try
          {
            VRAM = Convert.ToUInt64( Obj[ "AdapterRAM" ] );
          }
          catch
          {
          }

          // ── Software / virtual renderers ─────────────────────────────────────
          bool Is_Software = Name.IndexOf( "Microsoft Basic", StringComparison.OrdinalIgnoreCase ) >= 0 ||
                             Name.IndexOf( "Remote Desktop", StringComparison.OrdinalIgnoreCase ) >= 0 ||
                             Name.IndexOf( "VirtualBox", StringComparison.OrdinalIgnoreCase ) >= 0 ||
                             Name.IndexOf( "VMware", StringComparison.OrdinalIgnoreCase ) >= 0 ||
                             Name.IndexOf( "Hyper-V", StringComparison.OrdinalIgnoreCase ) >= 0 ||
                             Name.IndexOf( "WARP", StringComparison.OrdinalIgnoreCase ) >= 0;

          // ── Integrated / onboard GPU name patterns ────────────────────────────
          // Intel: HD Graphics, UHD Graphics, Iris, Iris Plus, Iris Xe, Arc (integrated Axxx)
          bool Is_Intel_Integrated =
            ( Name.IndexOf( "Intel", StringComparison.OrdinalIgnoreCase ) >= 0 ||
              Compat.IndexOf( "Intel", StringComparison.OrdinalIgnoreCase ) >= 0 ) &&
            ( Name.IndexOf( "HD Graphics", StringComparison.OrdinalIgnoreCase ) >= 0 ||
              Name.IndexOf( "UHD Graphics", StringComparison.OrdinalIgnoreCase ) >= 0 ||
              Name.IndexOf( "Iris", StringComparison.OrdinalIgnoreCase ) >= 0 );
          // Note: discrete Intel Arc cards contain "Arc" not "HD/UHD/Iris",
          // so they fall through correctly as discrete.

          // AMD: Radeon integrated lines (Vega, 680M, 890M, etc. live inside Ryzen APUs)
          // Discrete AMD cards say "Radeon RX" — we match only the APU-style names.
          bool Is_AMD_Integrated =
            ( Name.IndexOf( "AMD", StringComparison.OrdinalIgnoreCase ) >= 0 ||
              Name.IndexOf( "Radeon", StringComparison.OrdinalIgnoreCase ) >= 0 ||
              Compat.IndexOf( "Advanced Micro Devices", StringComparison.OrdinalIgnoreCase ) >= 0 ) &&
            ( Name.IndexOf( "Radeon Vega", StringComparison.OrdinalIgnoreCase ) >= 0 ||
              Name.IndexOf( "Radeon Graphics", StringComparison.OrdinalIgnoreCase ) >= 0 ||
              // Ryzen integrated: "Radeon 610M / 680M / 780M / 890M …"
              System.Text.RegularExpressions.Regex
                .IsMatch( Name,
                          @"Radeon\s+\d{3}M",
                          System.Text.RegularExpressions.RegexOptions.IgnoreCase ) );

          // ── VRAM sanity fallback (catches anything else mis-labelled) ─────────
          // Only apply when vendor is ambiguous; 512 MB is still a safe floor for
          // modern discrete cards while most integrated share system RAM and report
          // small values (or 0) here.
          bool Is_Low_VRAM_Fallback = VRAM > 0 && VRAM < 512UL * 1024 * 1024;

          bool Is_Integrated = Is_Intel_Integrated || Is_AMD_Integrated || Is_Low_VRAM_Fallback;

          if ( ! Is_Software && ! Is_Integrated )
            return true;
        }
      }
      catch
      {
      }

      return false;
    }

    private static string Get_Settings_File_Path()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      string    App_Data   = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
      string    App_Folder = Path.Combine( App_Data, "Multimeter_Controller" );
      Directory.CreateDirectory( App_Folder );
      return Path.Combine( App_Folder, "multi_poll_settings.json" );
    }

    public static Application_Settings Load()
    {
      using var            Block     = Trace_Block.Start_If_Enabled();
      string               File_Path = Get_Settings_File_Path();

      Application_Settings Make_Defaults()
      {
        var S = new Application_Settings();
        S.Initialize_Default_Save_Folder();
        S.Panel_Theme = Chart_Theme.Load( Chart_Theme.Panel_Theme_Path );
        S.Chart_Theme = Chart_Theme.Load( Chart_Theme.Chart_Theme_Path  );
        S.Save();
        return S;
      }

      if ( ! File.Exists( File_Path ) )
        return Make_Defaults();

      try
      {
        string Json     = File.ReadAllText( File_Path );
        var    Settings = JsonSerializer.Deserialize<Application_Settings>( Json );

        if ( Settings == null )
          return Make_Defaults();

        if ( string.IsNullOrEmpty( Settings.Default_Save_Folder ) )
          Settings.Initialize_Default_Save_Folder();

        Settings.Panel_Theme = Chart_Theme.Load( Chart_Theme.Panel_Theme_Path );
        Settings.Chart_Theme = Chart_Theme.Load( Chart_Theme.Chart_Theme_Path );
        return Settings;
      }
      catch
      {
        return Make_Defaults();
      }
    }

    public void Save()
    {

      using var Block     = Trace_Block.Start_If_Enabled();
      string    File_Path = Get_Settings_File_Path();

      try
      {
        var    Options = new JsonSerializerOptions { WriteIndented = true };

        string Json = JsonSerializer.Serialize( this, Options );
        File.WriteAllText( File_Path, Json );
      }
      catch ( Exception Ex )
      {
        System.Windows.Forms.MessageBox.Show( $"Failed to save settings: {Ex.Message}",
                                              "Settings Error",
                                              System.Windows.Forms.MessageBoxButtons.OK,
                                              System.Windows.Forms.MessageBoxIcon.Warning );
      }
    }

    public void Initialize_Default_Save_Folder()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( string.IsNullOrWhiteSpace( Default_Save_Folder ) )
        Default_Save_Folder = "Graph_Captures";
    }
    // ===== VALIDATION METHODS =====

    public void Validate_And_Fix()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Clamp numeric values to reasonable ranges
      Tooltip_Distance_Threshold  = Math.Max( 10, Math.Min( 200, Tooltip_Distance_Threshold ) );
      Tooltip_Display_Duration_Ms = Math.Max( 500, Math.Min( 10000, Tooltip_Display_Duration_Ms ) );
      Chart_Refresh_Rate_Ms       = Math.Max( 16, Math.Min( 1000, Chart_Refresh_Rate_Ms ) );
      Data_Dot_Size               = Math.Max( 1, Math.Min( 10, Data_Dot_Size ) );
      Line_Thickness              = Math.Max( 1, Math.Min( 10, Line_Thickness ) );

      // Default subnet to empty so Get_Local_Subnet auto-detects
      if ( Network_Scan_Subnet == null )
        Network_Scan_Subnet = "";

      // Decimation
      Decimation_Threshold = Math.Max( 10, Math.Min( 1_000_000, Decimation_Threshold ) );
      Decimation_Step      = Math.Max( 2, Math.Min( 1000, Decimation_Step ) );

      Skew_Warning_Threshold_Seconds = Math.Max( 0.2, Math.Min( 10.0, Skew_Warning_Threshold_Seconds ) );
      Stale_Data_Threshold_Seconds   = Math.Max( 0.2, Math.Min( 60.0, Stale_Data_Threshold_Seconds ) );

      // Stale must always be greater than skew
      if ( Stale_Data_Threshold_Seconds <= Skew_Warning_Threshold_Seconds )
        Stale_Data_Threshold_Seconds = Skew_Warning_Threshold_Seconds + 1.0;

      Max_Display_Points = Math.Clamp( Max_Display_Points, 10, 50_000_000 );

      // Poll delay: 50ms to 60000ms (0.05s to 60s)
      Default_Poll_Delay_Ms = Math.Max( 50, Math.Min( 60000, Default_Poll_Delay_Ms ) );

      // GPIB timeout: 1s to 60s
      Default_GPIB_Timeout_Ms = Math.Max( 1000, Math.Min( 60000, Default_GPIB_Timeout_Ms ) );

      // Retry attempts: 0 to 10
      Max_Retry_Attempts = Math.Max( 0, Math.Min( 10, Max_Retry_Attempts ) );

      Max_Consecutive_Errors_Before_Disable =
        Math.Max( 1, Math.Min( 100, Max_Consecutive_Errors_Before_Disable ) );
      Retry_Delay_Seconds = Math.Max( 1, Math.Min( 300, Retry_Delay_Seconds ) );

      Auto_Save_Interval_Minutes = Math.Max( 1, Math.Min( 1440, Auto_Save_Interval_Minutes ) );

      Max_Data_Points_In_Memory = Math.Max( 1000, Math.Min( 10000000, Max_Data_Points_In_Memory ) );
      Warning_Threshold_Percent = Math.Max( 50, Math.Min( 99, Warning_Threshold_Percent ) );
      Throttle_Point_Threshold  = Math.Max( 1000, Math.Min( 1000000, Throttle_Point_Threshold ) );
      Keep_Last_N_Points        = Math.Max( 100, Math.Min( 1000000, Keep_Last_N_Points ) );

      Default_Zoom_Level = Math.Max( 1, Math.Min( 100, Default_Zoom_Level ) );
      Zoom_Sensitivity   = Math.Max( 0.1, Math.Min( 5.0, Zoom_Sensitivity ) );
      Last_Zoom_Level    = Math.Max( 1, Math.Min( 100, Last_Zoom_Level ) );

      // Prologix defaults
      if ( string.IsNullOrWhiteSpace( Prologic_MAC_Address ) )
        Prologic_MAC_Address = "00-21-69";

      Default_GPIB_Instrument_Address = Math.Max( 0, Math.Min( 30, Default_GPIB_Instrument_Address ) );

      if ( string.IsNullOrWhiteSpace( Default_NPLC ) )
        Default_NPLC = "1";

      if ( string.IsNullOrWhiteSpace( Default_Measurement_Type ) )
        Default_Measurement_Type = "DC Voltage";

      if ( string.IsNullOrWhiteSpace( Export_Format ) )
        Export_Format = "CSV";

      if ( string.IsNullOrWhiteSpace( Default_Window_Title ) )
        Default_Window_Title = "Multi-Instrument Poller";

      if ( string.IsNullOrWhiteSpace( Filename_Pattern ) )
        Filename_Pattern = "{date}_{time}_{function}";

      // Ensure save folder is valid
      if ( string.IsNullOrWhiteSpace( Default_Save_Folder ) )
        Initialize_Default_Save_Folder();
    }
  }
}
