
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Application_Settings.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Single source of truth for all user-configurable behaviour in the
//   Multimeter Controller application.  Settings are persisted as indented
//   JSON in the user's AppData\Roaming folder and loaded/saved via the
//   static Load() / instance Save() methods.  The active Chart_Theme is
//   always loaded from its own separate file and is never serialised here.
//
// PERSISTENCE
//   File location : %AppData%\Multimeter_Controller\multi_poll_settings.json
//   Format        : UTF-8 JSON, WriteIndented = true
//   On load error : silently falls back to a default instance; no exception
//                   is propagated to the caller.
//   Theme         : [JsonIgnore] — loaded independently via Chart_Theme.Load()
//                   on every settings load, guaranteeing theme and settings
//                   files stay in sync.
//
// SETTINGS GROUPS
//
//   Prologix / GPIB Hardware
//     Default_Prologic_Port              TCP port for Prologix GPIB-ETHERNET
//     Prologic_Scan_Timeout_MS           UDP discovery timeout (ms)
//     Prologic_MAC_Address               MAC prefix/address for adapter scan
//     Default_GPIB_Instrument_Address    Default primary listen address (0–30)
//     Prologix_Auto_Read                 Enable auto-read mode on the adapter
//     Prologix_Read_Tmo_Ms               Read timeout when auto-read is on
//     Prologix_Fetch_Ms                  Polling interval for adapter fetch
//     Network_Scan_Subnet                Override subnet; empty = auto-detect
//     Default_IP_Address                 Pre-filled IP for manual connection
//
//   HP 3458A Instrument Behaviour
//     Send_Reset_On_Connect_3458         Issue RESET on first connect
//     Reset_Settle_Delay_Ms              Delay after RESET before commands
//     Send_End_Always_3458               Assert EOI on every write
//     Instrument_Settle_Ms               General post-command settle time
//     ERR_Read_Delay_Ms                  Delay before reading ERR register
//     NPLC_Apply_Delay_Ms                Settle time after NPLC change
//     Default_NPLC_3458                  NPLC string sent to the 3458A
//     Default_Trig_Mode_3458             Trigger mode string (e.g. TRIG HOLD)
//
//   Chart / Display
//     Chart_Refresh_Rate_Ms              Base repaint interval (16–1000 ms)
//     Show_Grid_Lines                    Horizontal and vertical grid lines
//     Show_Data_Dots                     Individual sample dots on line charts
//     Data_Dot_Size                      Dot diameter in pixels (1–10)
//     Line_Thickness                     Chart line width in pixels (1–10)
//     Default_To_Combined_View           Open chart in Combined view
//     Default_To_Normalized_View         Open chart in Normalized mode
//     Show_Legend_On_Startup             Show series legend on form open
//     Show_Tooltips_On_Hover             Enable mouse-hover value tooltips
//     Tooltip_Distance_Threshold         Max pixel distance for hit detection
//     Tooltip_Display_Duration_Ms        How long the tooltip stays visible
//
//   Polling / Data Collection
//     Max_Display_Points                 Rolling window hard cap (10–1 000 000)
//     Default_Poll_Delay_Ms              Inter-cycle delay (50–60 000 ms)
//     Stop_Polling_At_Max_Display_Points Halt polling vs. warn at cap
//     Default_NPLC                       NPLC preset for new sessions
//     Default_Measurement_Type           Measurement function for new sessions
//     Default_Continuous_Poll            Start in continuous-poll mode
//     Default_GPIB_Timeout_Ms            Per-read GPIB timeout (1–60 s)
//     Max_Retry_Attempts                 Read retries before error (0–10)
//     Max_Consecutive_Errors_Before_Disable  Errors before instrument disabled
//     Auto_Retry_Failed_Instruments      Re-enable disabled instruments
//     Retry_Delay_Seconds                Wait before retry attempt (1–300 s)
//     Skew_Warning_Threshold_Seconds     Max allowed timestamp skew (0.2–10 s)
//     Stale_Data_Threshold_Seconds       Age before reading flagged stale;
//                                        always enforced > Skew_Warning_Threshold
//
//   File / Data Management
//     Default_Save_Folder                Relative or absolute output path
//     Resolved_Save_Folder               [JsonIgnore] — absolute path resolved
//                                        by Multimeter_Common_Helpers_Class
//     Filename_Pattern                   Token pattern: {date} {time} {function}
//     Enable_Auto_Save                   Periodic CSV auto-save during recording
//     Auto_Save_Interval_Minutes         Auto-save period (1–1440 min)
//     Auto_Save_On_Stop                  Save immediately when polling stops
//     Include_Statistics_In_Save         Append stats footer to CSV
//     Prompt_Before_Clear                Confirm dialog before data clear
//     Auto_Load_Last_Session             Reload previous file on startup
//     Export_Format                      File format token (e.g. "CSV")
//
//   Performance / Memory
//     Max_Data_Points_In_Memory          Absolute in-memory cap (1 000–10 000 000)
//     Warning_Threshold_Percent          Memory warning level (50–99 %)
//     Warn_At_Threshold                  Show warning when threshold reached
//     Throttle_When_Many_Points          Reduce refresh rate at high point count
//     Throttle_Point_Threshold           Point count that triggers throttling
//     Auto_Trim_Old_Data                 Drop oldest points to stay under cap
//     Keep_Last_N_Points                 Points to retain when trimming
//     Reduce_Refresh_Rate_When_Large     Alias flag for throttle behaviour
//
//   Auto-Analysis
//     Auto_Analyze_After_Recording       Launch analysis form on polling stop
//     Analysis_Series_Count              Number of series to include
//     Analysis_Show_Mean / _Std_Dev / _Min_Max / _RMS / _Trend /
//       _Sample_Rate / _Errors           Toggle individual stats in the popup
//
//   UI / UX Preferences
//     Default_Window_Title               Main form caption
//     Remember_Window_Size/Position      Persist window geometry on exit
//     Last_Window_Width/Height/X/Y       Stored geometry from previous session
//     Enable_Keyboard_Pan                Arrow/Home/End/PgUp/PgDn chart pan
//     Enable_Ctrl_Scroll_Zoom            Mouse-wheel zoom on chart panel
//     Show_Progress_Messages             Status-strip progress text updates
//     Flash_On_Error                     Visual flash on instrument error
//     Play_Sound_On_Complete             Audio cue when recording completes
//
//   Zoom
//     Default_Zoom_Level                 TrackBar position on open (1–100)
//     Zoom_Sensitivity                   Scroll-wheel zoom speed (0.1–5.0)
//     Remember_Zoom_Level                Persist zoom between sessions
//     Last_Zoom_Level                    Zoom saved from previous session
//
// KEY METHODS
//   static Load()              Reads JSON from AppData; falls back to defaults
//                              on missing file or parse failure.  Always calls
//                              Chart_Theme.Load() to populate Current_Theme.
//   Save()                     Serialises this instance to JSON.  Shows a
//                              MessageBox on IO failure — never throws.
//   Validate_And_Fix()         Clamps every numeric field to its valid range
//                              and repairs null/empty strings.  Call after
//                              Load() before first use.
//   Initialize_Default_Save_Folder()   Sets Default_Save_Folder to the
//                              compile-time default if it is null or empty.
//   Set_Theme(Chart_Theme)     Replaces Current_Theme and raises Theme_Changed
//                              so subscribed forms can repaint immediately.
//   Theme_Changed (event)      Raised by Set_Theme(); consumers should
//                              invalidate their chart panels in the handler.
//
// NOTES
//   • Validate_And_Fix() enforces Stale_Data_Threshold > Skew_Warning_Threshold
//     by bumping Stale by +1 s when the invariant is violated.
//   • Default_NPLC is reset to "0.02" if it is blank or was previously "10",
//     preventing an accidental low-speed measurement mode on first run.
//   • Resolved_Save_Folder is [JsonIgnore]; it is computed on every access
//     by Multimeter_Common_Helpers_Class.Get_Graph_Captures_Folder() which
//     handles relative-to-executable vs. absolute path resolution.
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════




using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Multimeter_Controller
{
  public class Application_Settings
  {
    // In Application_Settings:



    [JsonIgnore]
    public Chart_Theme Current_Theme { get; set; } = Chart_Theme.Load ( );
   

    public int Default_Prologic_Port { get; set; } = 1234;




    [JsonPropertyName ( "prologic_scan_timeout_ms" )]
    public int Prologic_Scan_Timeout_MS { get; set; } = 50;

    [JsonPropertyName ( "prologic_mac_address" )]
    public string Prologic_MAC_Address { get; set; } = "00-21-69-01-3D-DD";

    [JsonPropertyName ( "default_gpib_instrument_address" )]
    public int Default_GPIB_Instrument_Address { get; set; } = 10;

    [JsonPropertyName ( "send_reset_on_connect_3458" )]
    public bool Send_Reset_On_Connect_3458 { get; set; } = true;

    [JsonPropertyName ( "reset_settle_delay_ms" )]
    public int Reset_Settle_Delay_Ms { get; set; } = 2000;

    [JsonPropertyName ( "send_end_always_3458" )]
    public bool Send_End_Always_3458 { get; set; } = true;

    [JsonPropertyName ( "instrument_settle_ms" )]
    public int Instrument_Settle_Ms { get; set; } = 500;

    [JsonPropertyName ( "prologix_fetch_ms" )]
    public int Prologix_Fetch_Ms { get; set; } = 100;

    [JsonPropertyName ( "err_read_delay_ms" )]
    public int ERR_Read_Delay_Ms { get; set; } = 500;

    [JsonPropertyName ( "nplc_apply_delay_ms" )]
    public int NPLC_Apply_Delay_Ms { get; set; } = 50;

    [JsonPropertyName ( "default_nplc_3458" )]
    public string Default_NPLC_3458 { get; set; } = "1";

    [JsonPropertyName ( "default_trig_mode_3458" )]
    public string Default_Trig_Mode_3458 { get; set; } = "TRIG HOLD";

    [JsonPropertyName ( "prologix_auto_read" )]
    public bool Prologix_Auto_Read { get; set; } = false;

    [JsonPropertyName ( "prologix_read_tmo_ms" )]
    public int Prologix_Read_Tmo_Ms { get; set; } = 5000;

    [JsonPropertyName ( "network_scan_subnet" )]
    public string Network_Scan_Subnet { get; set; } = "";

    [JsonPropertyName ( "default_ip_address" )]
    public string Default_IP_Address { get; set; } = "";

    // ===== CHART/DISPLAY SETTINGS =====



  

    [JsonPropertyName ( "tooltip_distance_threshold" )]
    public int Tooltip_Distance_Threshold { get; set; } = 50;

    [JsonPropertyName ( "show_tooltips_on_hover" )]
    public bool Show_Tooltips_On_Hover { get; set; } = true;

    [JsonPropertyName ( "tooltip_display_duration_ms" )]
    public int Tooltip_Display_Duration_Ms { get; set; } = 2000;

    [JsonPropertyName ( "chart_refresh_rate_ms" )]
    public int Chart_Refresh_Rate_Ms { get; set; } = 100;

    [JsonPropertyName ( "show_grid_lines" )]
    public bool Show_Grid_Lines { get; set; } = true;

    [JsonPropertyName ( "show_data_dots" )]
    public bool Show_Data_Dots { get; set; } = true;

    [JsonPropertyName ( "data_dot_size" )]
    public int Data_Dot_Size { get; set; } = 4;

    [JsonPropertyName ( "line_thickness" )]
    public int Line_Thickness { get; set; } = 2;

    [JsonPropertyName ( "default_to_combined_view" )]
    public bool Default_To_Combined_View { get; set; } = false;

    [JsonPropertyName ( "default_to_normalized_view" )]
    public bool Default_To_Normalized_View { get; set; } = false;

    [JsonPropertyName ( "show_legend_on_startup" )]
    public bool Show_Legend_On_Startup { get; set; } = false;

  


    // ===== POLLING/DATA COLLECTION =====

    [JsonPropertyName ( "max_display_points" )]
    public int Max_Display_Points { get; set; } = 50_000_000;


    [JsonPropertyName ( "default_poll_delay_ms" )]
    public int Default_Poll_Delay_Ms { get; set; } = 500;

    [JsonPropertyName ( "stop_polling_at_max_display_points" )]
    public bool Stop_Polling_At_Max_Display_Points { get; set; } = false;  // default = warn only



    [JsonPropertyName ( "default_nplc" )]
    public string Default_NPLC { get; set; } = "1";

    [JsonPropertyName ( "default_measurement_type" )]
    public string Default_Measurement_Type { get; set; } = "DC Voltage";

    [JsonPropertyName ( "default_continuous_poll" )]
    public bool Default_Continuous_Poll { get; set; } = true;

    [JsonPropertyName ( "default_gpib_timeout_ms" )]
    public int Default_GPIB_Timeout_Ms { get; set; } = 5000;

    [JsonPropertyName ( "max_retry_attempts" )]
    public int Max_Retry_Attempts { get; set; } = 3;

    [JsonPropertyName ( "max_consecutive_errors_before_disable" )]
    public int Max_Consecutive_Errors_Before_Disable { get; set; } = 10;

    [JsonPropertyName ( "auto_retry_failed_instruments" )]
    public bool Auto_Retry_Failed_Instruments { get; set; } = true;

    [JsonPropertyName ( "retry_delay_seconds" )]
    public int Retry_Delay_Seconds { get; set; } = 5;

    [JsonPropertyName ( "skew_warning_threshold_seconds" )]
    public double Skew_Warning_Threshold_Seconds { get; set; } = 1.0;

    [JsonPropertyName ( "stale_data_threshold_seconds" )]
    public double Stale_Data_Threshold_Seconds { get; set; } = 3.0;



    // ===== FILE/DATA MANAGEMENT =====

    [JsonIgnore]
    public string Resolved_Save_Folder =>
    Multimeter_Common_Helpers_Class.Get_Graph_Captures_Folder ( this );

    [JsonPropertyName ( "default_save_folder" )]
    public string Default_Save_Folder { get; set; } = "Graph_Captures";

    [JsonPropertyName ( "filename_pattern" )]
    public string Filename_Pattern { get; set; } = "{date}_{time}_{function}";

    [JsonPropertyName ( "enable_auto_save" )]
    public bool Enable_Auto_Save { get; set; } = false;

    [JsonPropertyName ( "auto_save_interval_minutes" )]
    public int Auto_Save_Interval_Minutes { get; set; } = 5;

    [JsonPropertyName ( "auto_save_on_stop" )]
    public bool Auto_Save_On_Stop { get; set; } = false;

    [JsonPropertyName ( "include_statistics_in_save" )]
    public bool Include_Statistics_In_Save { get; set; } = true;

    [JsonPropertyName ( "prompt_before_clear" )]
    public bool Prompt_Before_Clear { get; set; } = true;

    [JsonPropertyName ( "auto_load_last_session" )]
    public bool Auto_Load_Last_Session { get; set; } = false;

    [JsonPropertyName ( "export_format" )]
    public string Export_Format { get; set; } = "CSV";

    // ===== PERFORMANCE/MEMORY =====

    [JsonPropertyName( "max_data_points_in_memory" )]
    public int Max_Data_Points_In_Memory { get; set; } = 100000;

    [JsonPropertyName( "warning_threshold_percent" )]
    public int Warning_Threshold_Percent { get; set; } = 80;

    [JsonPropertyName( "warn_at_threshold" )]
    public bool Warn_At_Threshold { get; set; } = true;

    [JsonPropertyName( "throttle_when_many_points" )]
    public bool Throttle_When_Many_Points { get; set; } = true;

    [JsonPropertyName( "throttle_point_threshold" )]
    public int Throttle_Point_Threshold { get; set; } = 10000;

    [JsonPropertyName( "auto_trim_old_data" )]
    public bool Auto_Trim_Old_Data { get; set; } = false;

    [JsonPropertyName( "keep_last_n_points" )]
    public int Keep_Last_N_Points { get; set; } = 10000;

    [JsonPropertyName( "reduce_refresh_rate_when_large" )]
    public bool Reduce_Refresh_Rate_When_Large { get; set; } = true;

    // ── Decimation ────────────────────────────────────────────────────────────
    // When enabled, the chart draws at most Decimation_Max_Draw points
    // regardless of how many are stored in memory.  Decimation only kicks in
    // when the visible point count exceeds Decimation_Threshold.
    // All stored points are always written to CSV — decimation is display-only.
   
[JsonPropertyName( "enable_decimation" )]
    public bool Enable_Decimation { get; set; } = true;

    [JsonPropertyName( "decimation_threshold" )]
    public int Decimation_Threshold { get; set; } = 10_000;  // start decimating above this

    [JsonPropertyName( "decimation_step" )]
    public int Decimation_Step { get; set; } = 10;  // draw every Nth point


    // ===== Analysis ======
    [JsonPropertyName ( "auto_analyze_after_recording" )]
    public bool Auto_Analyze_After_Recording { get; set; } = false;

    [JsonPropertyName ( "analysis_series_count" )]
    public int Analysis_Series_Count { get; set; } = 2;

    [JsonPropertyName ( "analysis_show_mean" )]
    public bool Analysis_Show_Mean { get; set; } = true;

    [JsonPropertyName ( "analysis_show_std_dev" )]
    public bool Analysis_Show_Std_Dev { get; set; } = true;

    [JsonPropertyName ( "analysis_show_min_max" )]
    public bool Analysis_Show_Min_Max { get; set; } = true;

    [JsonPropertyName ( "analysis_show_rms" )]
    public bool Analysis_Show_RMS { get; set; } = true;

    [JsonPropertyName ( "analysis_show_trend" )]
    public bool Analysis_Show_Trend { get; set; } = true;

    [JsonPropertyName ( "analysis_show_sample_rate" )]
    public bool Analysis_Show_Sample_Rate { get; set; } = true;

    [JsonPropertyName ( "analysis_show_errors" )]
    public bool Analysis_Show_Errors { get; set; } = true;




    // ===== UI/UX PREFERENCES =====

    [JsonPropertyName ( "default_window_title" )]
    public string Default_Window_Title { get; set; } = "Multi-Instrument Poller";

    [JsonPropertyName ( "remember_window_size" )]
    public bool Remember_Window_Size { get; set; } = true;

    [JsonPropertyName ( "remember_window_position" )]
    public bool Remember_Window_Position { get; set; } = true;

    [JsonPropertyName ( "last_window_width" )]
    public int Last_Window_Width { get; set; } = 1340;

    [JsonPropertyName ( "last_window_height" )]
    public int Last_Window_Height { get; set; } = 730;

    [JsonPropertyName ( "last_window_x" )]
    public int Last_Window_X { get; set; } = -1;

    [JsonPropertyName ( "last_window_y" )]
    public int Last_Window_Y { get; set; } = -1;

    [JsonPropertyName ( "enable_keyboard_pan" )]
    public bool Enable_Keyboard_Pan { get; set; } = true;

    [JsonPropertyName ( "enable_ctrl_scroll_zoom" )]
    public bool Enable_Ctrl_Scroll_Zoom { get; set; } = true;

    [JsonPropertyName ( "show_progress_messages" )]
    public bool Show_Progress_Messages { get; set; } = true;

    [JsonPropertyName ( "flash_on_error" )]
    public bool Flash_On_Error { get; set; } = true;

    [JsonPropertyName ( "play_sound_on_complete" )]
    public bool Play_Sound_On_Complete { get; set; } = false;

    [JsonPropertyName("analysis_tab_alignment")]
    public string Analysis_Tab_Alignment_str { get; set; } = "Left";

    [JsonIgnore]
    public TabAlignment Analysis_Tab_Alignment
    {
      get => Analysis_Tab_Alignment_str switch
      {
        "Left" => TabAlignment.Left,
        "Right" => TabAlignment.Right,
        _ => TabAlignment.Top,
      };
      set => Analysis_Tab_Alignment_str = value switch
      {
        TabAlignment.Left => "Left",
        TabAlignment.Right => "Right",
        _ => "Top",
      };
    }

    // ===== ZOOM SETTINGS =====

    [JsonPropertyName ( "default_zoom_level" )]
    public int Default_Zoom_Level { get; set; } = 50;

    [JsonPropertyName ( "zoom_sensitivity" )]
    public double Zoom_Sensitivity { get; set; } = 1.0;

    [JsonPropertyName ( "remember_zoom_level" )]
    public bool Remember_Zoom_Level { get; set; } = false;

    [JsonPropertyName ( "last_zoom_level" )]
    public int Last_Zoom_Level { get; set; } = 50;

    // ===== LOAD/SAVE METHODS =====



    public event EventHandler? Theme_Changed;

    public void Set_Theme ( Chart_Theme New_Theme )
    {
      Current_Theme = New_Theme;
      Theme_Changed?.Invoke ( this, EventArgs.Empty );
    }



    private static string Get_Settings_File_Path ( )
    {
      string App_Data = Environment.GetFolderPath ( Environment.SpecialFolder.ApplicationData );
      string App_Folder = Path.Combine ( App_Data, "Multimeter_Controller" );
      Directory.CreateDirectory ( App_Folder );
      return Path.Combine ( App_Folder, "multi_poll_settings.json" );
    }

    public static Application_Settings Load ( )
    {
      string File_Path = Get_Settings_File_Path ( );

      if ( !File.Exists ( File_Path ) )
      {
        var Default_Settings = new Application_Settings ( );
        Default_Settings.Initialize_Default_Save_Folder ( );
        Default_Settings.Current_Theme = Chart_Theme.Load ( );
        return Default_Settings;
      }

      try
      {
        string Json = File.ReadAllText ( File_Path );
        var Settings = JsonSerializer.Deserialize<Application_Settings> ( Json );

        if ( Settings == null )
        {
          var Default_Settings = new Application_Settings ( );
          Default_Settings.Initialize_Default_Save_Folder ( );
          Default_Settings.Current_Theme = Chart_Theme.Load ( );
          return Default_Settings;
        }

        if ( string.IsNullOrEmpty ( Settings.Default_Save_Folder ) )
          Settings.Initialize_Default_Save_Folder ( );

        Settings.Current_Theme = Chart_Theme.Load ( );  // always load from theme file
        return Settings;
      }
      catch
      {
        var Default_Settings = new Application_Settings ( );
        Default_Settings.Initialize_Default_Save_Folder ( );
        Default_Settings.Current_Theme = Chart_Theme.Load ( );
        return Default_Settings;
      }
    }
    public void Save ( )
    {
      string File_Path = Get_Settings_File_Path ( );

      try
      {
        var Options = new JsonSerializerOptions
        {
          WriteIndented = true
        };

        string Json = JsonSerializer.Serialize ( this, Options );
        File.WriteAllText ( File_Path, Json );
      }
      catch ( Exception Ex )
      {
        System.Windows.Forms.MessageBox.Show (
          $"Failed to save settings: {Ex.Message}",
          "Settings Error",
          System.Windows.Forms.MessageBoxButtons.OK,
          System.Windows.Forms.MessageBoxIcon.Warning );
      }
    }

    public void Initialize_Default_Save_Folder ( )
    {
      if ( string.IsNullOrWhiteSpace ( Default_Save_Folder ) )
        Default_Save_Folder = "Graph_Captures";
    }
    // ===== VALIDATION METHODS =====


    public void Validate_And_Fix ( )
    {
      // Clamp numeric values to reasonable ranges
      Tooltip_Distance_Threshold = Math.Max ( 10, Math.Min ( 200, Tooltip_Distance_Threshold ) );
      Tooltip_Display_Duration_Ms = Math.Max ( 500, Math.Min ( 10000, Tooltip_Display_Duration_Ms ) );
      Chart_Refresh_Rate_Ms = Math.Max ( 16, Math.Min ( 1000, Chart_Refresh_Rate_Ms ) );
      Data_Dot_Size = Math.Max ( 1, Math.Min ( 10, Data_Dot_Size ) );
      Line_Thickness = Math.Max ( 1, Math.Min ( 10, Line_Thickness ) );
   //   Default_Max_Display_Points = Math.Max ( 10, Math.Min ( 100000, Default_Max_Display_Points ) );

      // Default subnet to empty so Get_Local_Subnet auto-detects
      if ( Network_Scan_Subnet == null )
        Network_Scan_Subnet = "";


      // Decimation
      Decimation_Threshold = Math.Max( 1_000, Math.Min( 1_000_000, Decimation_Threshold ) );
      Decimation_Step = Math.Max( 2, Math.Min( 100, Decimation_Step ) );

     

      Skew_Warning_Threshold_Seconds = Math.Max ( 0.2, Math.Min ( 10.0, Skew_Warning_Threshold_Seconds ) );
      Stale_Data_Threshold_Seconds = Math.Max ( 0.2, Math.Min ( 60.0, Stale_Data_Threshold_Seconds ) );

      // Stale must always be greater than skew
      if ( Stale_Data_Threshold_Seconds <= Skew_Warning_Threshold_Seconds )
        Stale_Data_Threshold_Seconds = Skew_Warning_Threshold_Seconds + 1.0;

    
      Max_Display_Points = Math.Clamp ( Max_Display_Points, 10, 1_000_000 );

      // Poll delay: 50ms to 60000ms (0.05s to 60s)
      Default_Poll_Delay_Ms = Math.Max ( 50, Math.Min ( 60000, Default_Poll_Delay_Ms ) );

      // GPIB timeout: 1s to 60s
      Default_GPIB_Timeout_Ms = Math.Max ( 1000, Math.Min ( 60000, Default_GPIB_Timeout_Ms ) );

      // Retry attempts: 0 to 10
      Max_Retry_Attempts = Math.Max ( 0, Math.Min ( 10, Max_Retry_Attempts ) );

      Max_Consecutive_Errors_Before_Disable = Math.Max ( 1, Math.Min ( 100, Max_Consecutive_Errors_Before_Disable ) );
      Retry_Delay_Seconds = Math.Max ( 1, Math.Min ( 300, Retry_Delay_Seconds ) );

      Auto_Save_Interval_Minutes = Math.Max ( 1, Math.Min ( 1440, Auto_Save_Interval_Minutes ) );

      Max_Data_Points_In_Memory = Math.Max ( 1000, Math.Min ( 10000000, Max_Data_Points_In_Memory ) );
      Warning_Threshold_Percent = Math.Max ( 50, Math.Min ( 99, Warning_Threshold_Percent ) );
      Throttle_Point_Threshold = Math.Max ( 1000, Math.Min ( 1000000, Throttle_Point_Threshold ) );
      Keep_Last_N_Points = Math.Max ( 100, Math.Min ( 1000000, Keep_Last_N_Points ) );

      Default_Zoom_Level = Math.Max ( 1, Math.Min ( 100, Default_Zoom_Level ) );
      Zoom_Sensitivity = Math.Max ( 0.1, Math.Min ( 5.0, Zoom_Sensitivity ) );
      Last_Zoom_Level = Math.Max ( 1, Math.Min ( 100, Last_Zoom_Level ) );


      // Prologix defaults
      if ( string.IsNullOrWhiteSpace ( Prologic_MAC_Address ) )
        Prologic_MAC_Address = "00-21-69";

      Default_GPIB_Instrument_Address = Math.Max ( 0, Math.Min ( 30, Default_GPIB_Instrument_Address ) );


      if ( string.IsNullOrWhiteSpace ( Default_NPLC ) || Default_NPLC == "10" )
        Default_NPLC = "0.02";

   

      if ( string.IsNullOrWhiteSpace ( Default_Measurement_Type ) )
        Default_Measurement_Type = "DC Voltage";

      if ( string.IsNullOrWhiteSpace ( Export_Format ) )
        Export_Format = "CSV";

      if ( string.IsNullOrWhiteSpace ( Default_Window_Title ) )
        Default_Window_Title = "Multi-Instrument Poller";

      if ( string.IsNullOrWhiteSpace ( Filename_Pattern ) )
        Filename_Pattern = "{date}_{time}_{function}";

      // Ensure save folder is valid
      if ( string.IsNullOrWhiteSpace ( Default_Save_Folder ) )
        Initialize_Default_Save_Folder ( );
    }
  }
}
