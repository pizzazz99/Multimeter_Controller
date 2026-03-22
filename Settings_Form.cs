using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Multimeter_Controller
{
  public partial class Settings_Form : Form
  {
    private Application_Settings _Settings;

    // Display tab controls
    private NumericUpDown _Tooltip_Distance_Numeric;
    private CheckBox _Show_Tooltips_Check;
    private NumericUpDown _Tooltip_Duration_Numeric;
    private NumericUpDown _Chart_Refresh_Numeric;
    private CheckBox _Show_Grid_Check;
    private CheckBox _Show_Dots_Check;
    private NumericUpDown _Dot_Size_Numeric;
    private NumericUpDown _Line_Thickness_Numeric;
    private CheckBox _Default_Combined_Check;
    private CheckBox _Default_Normalized_Check;
    private CheckBox _Show_Legend_Check;
    private NumericUpDown _Display_Digits_Numeric;




    // Polling tab controls
    private NumericUpDown _Poll_Delay_Numeric;
    private ComboBox _NPLC_Combo;
    private ComboBox _Measurement_Type_Combo;
    private CheckBox _Continuous_Poll_Check;
    private NumericUpDown _GPIB_Timeout_Numeric;
    private NumericUpDown _Max_Retry_Attempts_Numeric;
    private NumericUpDown _Max_Errors_Numeric;
    private CheckBox _Auto_Retry_Check;
    private NumericUpDown _Retry_Delay_Numeric;
    private NumericUpDown _Skew_Warning_Numeric;
    private NumericUpDown _Stale_Data_Numeric;
    private NumericUpDown _Max_Display_Points_Numeric;
    private CheckBox _Stop_At_Max_Check;

    // ===== In Build_Prologix_Tab ( ) =====

    private TextBox _Prologix_IP_Textbox;
    private NumericUpDown _Prologix_Port_Numeric;
    private TextBox _Prologix_MAC_Textbox;
    private TextBox _Prologic_Scan_Timeout_MS_Textbox;


    private NumericUpDown _Prologix_GPIB_Address_Numeric;
    private CheckBox _Prologix_Auto_Read_Check;
    private NumericUpDown _Prologix_Read_Tmo_Numeric;
    private NumericUpDown _Prologix_Fetch_Numeric;
    private TextBox _Prologix_Subnet_Textbox;


    // Files tab controls
    private TextBox _Save_Folder_Text;
    private Button _Browse_Folder_Button;
    private TextBox _Filename_Pattern_Text;
    private CheckBox _Enable_Auto_Save_Check;
    private NumericUpDown _Auto_Save_Interval_Numeric;
    private CheckBox _Auto_Save_On_Stop_Check;
    private CheckBox _Include_Stats_Check;
    private CheckBox _Prompt_Before_Clear_Check;
    private CheckBox _Auto_Load_Last_Check;
    private ComboBox _Export_Format_Combo;

    // Performance tab controls
    private NumericUpDown _Max_Points_Memory_Numeric;
    private NumericUpDown _Warning_Threshold_Numeric;
    private CheckBox _Warn_At_Threshold_Check;
    private CheckBox _Throttle_When_Many_Check;
    private NumericUpDown _Throttle_Threshold_Numeric;
    private CheckBox _Auto_Trim_Check;
    private NumericUpDown _Keep_Last_N_Numeric;
    private CheckBox _Reduce_Refresh_Check;

    // Analysis tab controls
   
    private RadioButton _Analysis_One_Series_Radio;
    private RadioButton _Analysis_Two_Series_Radio;
    private CheckBox _Analysis_Mean_Check;
    private CheckBox _Analysis_StdDev_Check;
    private CheckBox _Analysis_MinMax_Check;
    private CheckBox _Analysis_RMS_Check;
    private CheckBox _Analysis_Trend_Check;
    private CheckBox _Analysis_Sample_Rate_Check;
    private CheckBox _Analysis_Errors_Check;

    // UI tab controls
    private TextBox _Window_Title_Text;
    private CheckBox _Remember_Size_Check;
    private CheckBox _Remember_Position_Check;
    private CheckBox _Enable_Keyboard_Pan_Check;
    private CheckBox _Enable_Ctrl_Zoom_Check;
    private CheckBox _Show_Progress_Check;
    private CheckBox _Flash_On_Error_Check;
    private CheckBox _Play_Sound_Check;
    private ComboBox _Theme_Combo;

    // Zoom tab controls
    private NumericUpDown _Default_Zoom_Numeric;
    private TrackBar _Zoom_Sensitivity_Slider;
    private Label _Zoom_Sensitivity_Label;
    private CheckBox _Remember_Zoom_Check;

    // HP tab controls

    private TextBox _Default_IP_Text;
    private NumericUpDown _Default_Port_Numeric;
    private NumericUpDown _Reset_Delay_Numeric;
    private NumericUpDown _Instrument_Settle_Numeric;

    private NumericUpDown _ERR_Read_Delay_Numeric;
    private ComboBox _Default_NPLC_3458_Combo;
    private ComboBox _Default_Trig_Mode_Combo;
    private CheckBox _Send_Reset_On_Connect_Check;
    private CheckBox _Send_End_Always_Check;
    private NumericUpDown _NPLC_Apply_Delay_Numeric;



    public Settings_Form ( Application_Settings Settings )
    {
      InitializeComponent ( );
      _Settings = Settings;

      Initialize_Display_Tab ( );
      Initialize_Polling_Tab ( );
      Initialize_Files_Tab ( );
      Initialize_Performance_Tab ( );
      Initialize_UI_Tab ( );
      Initialize_Zoom_Tab ( );
      Initialize_HP_Tab ( );
      Build_Prologix_Tab ( );
      Initialize_Analysis_Tab ( );
      Load_Settings ( );
    }
    public Application_Settings Get_Settings ( )
    {
      return _Settings;
    }

    private void Build_Prologix_Tab ( )
    {
      int Left_Col = 15;
      int Right_Col = 180;
      int Y = 15;
      int Row_H = 30;

      // IP Address
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "IP Address:",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      } );
      _Prologix_IP_Textbox = new TextBox
      {
        Location = new Point ( Right_Col, Y - 2 ),
        Size = new Size ( 150, 23 )
      };
      Prologix_Tab.Controls.Add ( _Prologix_IP_Textbox );
      Y += Row_H;

      // Scan Subnet
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "Scan Subnet:",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      } );
      _Prologix_Subnet_Textbox = new TextBox
      {
        Location = new Point ( Right_Col, Y - 2 ),
        Size = new Size ( 150, 23 )
      };
      Prologix_Tab.Controls.Add ( _Prologix_Subnet_Textbox );
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "(e.g. 192.168.1 - leave blank to auto-detect)",
        Location = new Point ( Right_Col + 160, Y ),
        AutoSize = true,
        ForeColor = SystemColors.GrayText
      } );
      Y += Row_H;


      // Port
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "Port:",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      } );
      _Prologix_Port_Numeric = new NumericUpDown
      {
        Location = new Point ( Right_Col, Y - 2 ),
        Size = new Size ( 80, 23 ),
        Minimum = 1,
        Maximum = 65535,
        Value = 1234
      };
      Prologix_Tab.Controls.Add ( _Prologix_Port_Numeric );
      Y += Row_H;

      // MAC / OUI
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "MAC / OUI:",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      } );
      _Prologix_MAC_Textbox = new TextBox
      {
        Location = new Point ( Right_Col, Y - 2 ),
        Size = new Size ( 150, 23 )
      };
      Prologix_Tab.Controls.Add ( _Prologix_MAC_Textbox );
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "(e.g. 00-1C-4A)",
        Location = new Point ( Right_Col + 160, Y ),
        AutoSize = true,
        ForeColor = SystemColors.GrayText
      } );
      Y += Row_H;

      // Default GPIB Address
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "Default GPIB Address:",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      } );
      _Prologix_GPIB_Address_Numeric = new NumericUpDown
      {
        Location = new Point ( Right_Col, Y - 2 ),
        Size = new Size ( 80, 23 ),
        Minimum = 0,
        Maximum = 30,
        Value = 22
      };
      Prologix_Tab.Controls.Add ( _Prologix_GPIB_Address_Numeric );
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "(0 - 30)",
        Location = new Point ( Right_Col + 90, Y ),
        AutoSize = true,
        ForeColor = SystemColors.GrayText
      } );
      Y += Row_H;

      // Prologix Auto Read
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "Prologix Auto Read:",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      } );
      _Prologix_Auto_Read_Check = new CheckBox
      {
        Location = new Point ( Right_Col, Y - 2 ),
        Checked = _Settings.Prologix_Auto_Read
      };
      Prologix_Tab.Controls.Add ( _Prologix_Auto_Read_Check );
      Y += Row_H;

      // Prologix Read Timeout
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "Prologix Read Tmo (ms):",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      } );
      _Prologix_Read_Tmo_Numeric = new NumericUpDown
      {
        Location = new Point ( Right_Col, Y - 2 ),
        Size = new Size ( 80, 23 ),
        Minimum = 1000,
        Maximum = 30000,
        Increment = 500,
        Value = _Settings.Prologix_Read_Tmo_Ms
      };
      Prologix_Tab.Controls.Add ( _Prologix_Read_Tmo_Numeric );

      Y += Row_H;

      // Prologix Scan Timeout
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "Prologic Scan Timeout MS:",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      } );
      _Prologic_Scan_Timeout_MS_Textbox = new TextBox
      {
        Location = new Point ( Right_Col, Y - 2 ),
        Size = new Size ( 150, 23 )
      };
      Prologix_Tab.Controls.Add ( _Prologic_Scan_Timeout_MS_Textbox );
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "(Value is milliseconds 1000 = 1 second)",
        Location = new Point ( Right_Col + 160, Y ),
        AutoSize = true,
        ForeColor = SystemColors.GrayText
      } );

      Y += Row_H;

      // Prologix Fetch Delay
      Prologix_Tab.Controls.Add ( new Label
      {
        Text = "Prologix Fetch Delay (ms):",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      } );
      _Prologix_Fetch_Numeric = new NumericUpDown
      {
        Location = new Point ( Right_Col, Y - 2 ),
        Size = new Size ( 80, 23 ),
        Minimum = 10,
        Maximum = 500,
        Increment = 10,
        Value = 100
      };
      Prologix_Tab.Controls.Add ( _Prologix_Fetch_Numeric );
      Y += Row_H;
    }


    private void Initialize_Display_Tab ( )
    {
      int Y = 15;

      // Tooltip Distance
      var Tooltip_Distance_Label = new Label
      {
        Text = "Tooltip Distance (pixels):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( Tooltip_Distance_Label );

      _Tooltip_Distance_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 10,
        Maximum = 200,
        Value = 50
      };
      Display_Tab.Controls.Add ( _Tooltip_Distance_Numeric );
      Y += 30;

      // Show Tooltips
      _Show_Tooltips_Check = new CheckBox
      {
        Text = "Show tooltips on hover",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( _Show_Tooltips_Check );
      Y += 30;

      // Tooltip Duration
      var Tooltip_Duration_Label = new Label
      {
        Text = "Tooltip Duration (ms):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( Tooltip_Duration_Label );

      _Tooltip_Duration_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 500,
        Maximum = 10000,
        Increment = 500,
        Value = 2000
      };
      Display_Tab.Controls.Add ( _Tooltip_Duration_Numeric );
      Y += 30;

      // Chart Refresh Rate
      var Chart_Refresh_Label = new Label
      {
        Text = "Chart Refresh Rate (ms):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( Chart_Refresh_Label );

      _Chart_Refresh_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 16,
        Maximum = 1000,
        Value = 100
      };
      Display_Tab.Controls.Add ( _Chart_Refresh_Numeric );
      Y += 30;

      // Show Grid Lines
      _Show_Grid_Check = new CheckBox
      {
        Text = "Show grid lines",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( _Show_Grid_Check );
      Y += 30;

      // Show Data Dots
      _Show_Dots_Check = new CheckBox
      {
        Text = "Show data point dots",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( _Show_Dots_Check );
      Y += 30;

      // Dot Size
      var Dot_Size_Label = new Label
      {
        Text = "Data Dot Size (pixels):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( Dot_Size_Label );

      _Dot_Size_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 1,
        Maximum = 10,
        Value = 4
      };
      Display_Tab.Controls.Add ( _Dot_Size_Numeric );
      Y += 30;

      // Line Thickness
      var Line_Thickness_Label = new Label
      {
        Text = "Line Thickness (pixels):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( Line_Thickness_Label );

      _Line_Thickness_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 1,
        Maximum = 10,
        Value = 2
      };
      Display_Tab.Controls.Add ( _Line_Thickness_Numeric );
      Y += 30;

      // Default to Combined View
      _Default_Combined_Check = new CheckBox
      {
        Text = "Default to combined view",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( _Default_Combined_Check );
      Y += 30;

      // Default to Normalized View
      _Default_Normalized_Check = new CheckBox
      {
        Text = "Default to normalized view",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( _Default_Normalized_Check );
      Y += 30;

      // Show Legend on Startup
      _Show_Legend_Check = new CheckBox
      {
        Text = "Show legend on startup",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Display_Tab.Controls.Add ( _Show_Legend_Check );
      Y += 30;


    }

    private void Initialize_Polling_Tab ( )
    {
      int Y = 15;


      var Max_Points_Label = new Label
      {
        Text = "Max Display Points:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( Max_Points_Label );   // ← was missing

      _Max_Display_Points_Numeric = new NumericUpDown  // ← assign to field, not local
      {
        Minimum = 5,
        Maximum = _Settings.Max_Display_Points,
        Increment = 1_000,
        Value = 5,
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 90, 22 )
      };
      Polling_Tab.Controls.Add ( _Max_Display_Points_Numeric );  // ← was missing
      Y += 30;

      _Stop_At_Max_Check = new CheckBox
      {
        Text = "Stop polling when max display points reached (default: roll/warn)",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( _Stop_At_Max_Check );
      Y += 30;

      // Poll Delay
      var Poll_Delay_Label = new Label
      {
        Text = "Default Poll Delay (ms):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( Poll_Delay_Label );

      _Poll_Delay_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 50,
        Maximum = 60000,
        Increment = 50,
        Value = 1000
      };
      Polling_Tab.Controls.Add ( _Poll_Delay_Numeric );
      Y += 30;



      // NPLC
      var NPLC_Label = new Label
      {
        Text = "Default NPLC:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( NPLC_Label );

      _NPLC_Combo = new ComboBox
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 100, 23 ),
        DropDownStyle = ComboBoxStyle.DropDownList
      };
      _NPLC_Combo.Items.AddRange ( new object [ ] { "0.02", "0.2", "1", "10", "100" } );
      Polling_Tab.Controls.Add ( _NPLC_Combo );
      Y += 30;

      // Measurement Type
      var Measurement_Label = new Label
      {
        Text = "Default Measurement:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( Measurement_Label );

      _Measurement_Type_Combo = new ComboBox
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 150, 23 ),
        DropDownStyle = ComboBoxStyle.DropDownList
      };
      _Measurement_Type_Combo.Items.AddRange ( new object [ ]
      {
        "DC Voltage", "AC Voltage", "DC Current", "AC Current",
        "2-Wire Ohms", "4-Wire Ohms", "Frequency", "Period"
      } );
      Polling_Tab.Controls.Add ( _Measurement_Type_Combo );
      Y += 30;

      // Continuous Poll
      _Continuous_Poll_Check = new CheckBox
      {
        Text = "Default to continuous polling",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( _Continuous_Poll_Check );
      Y += 30;

      // GPIB Timeout
      var Timeout_Label = new Label
      {
        Text = "GPIB Timeout (ms):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( Timeout_Label );

      _GPIB_Timeout_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 1000,
        Maximum = 60000,
        Increment = 1000,
        Value = 5000
      };
      Polling_Tab.Controls.Add ( _GPIB_Timeout_Numeric );
      Y += 30;

      // Max Retry Attempts
      var Retry_Attempts_Label = new Label
      {
        Text = "Max Retry Attempts:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( Retry_Attempts_Label );

      _Max_Retry_Attempts_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 0,
        Maximum = 10,
        Value = 3
      };
      Polling_Tab.Controls.Add ( _Max_Retry_Attempts_Numeric );
      Y += 35;

      // Error Handling separator
      var Separator1 = new Label
      {
        Text = "Error Handling:",
        Location = new Point ( 15, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( Separator1 );
      Y += 25;

      // Max Errors
      var Max_Errors_Label = new Label
      {
        Text = "Max Errors Before Disable:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( Max_Errors_Label );

      _Max_Errors_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 1,
        Maximum = 100,
        Value = 10
      };
      Polling_Tab.Controls.Add ( _Max_Errors_Numeric );
      Y += 30;

      // Auto Retry
      _Auto_Retry_Check = new CheckBox
      {
        Text = "Auto-retry failed instruments",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( _Auto_Retry_Check );
      Y += 30;

      // Retry Delay
      var Retry_Delay_Label = new Label
      {
        Text = "Retry Delay (seconds):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Polling_Tab.Controls.Add ( Retry_Delay_Label );

      _Retry_Delay_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 1,
        Maximum = 300,
        Value = 5
      };
      Polling_Tab.Controls.Add ( _Retry_Delay_Numeric );

      Y += 35;

      Polling_Tab.Controls.Add ( new Label
      {
        Text = "Data Freshness:",
        Location = new Point ( 15, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      } );
      Y += 25;

      Polling_Tab.Controls.Add ( new Label
      {
        Text = "Skew Warning (seconds):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      } );
      _Skew_Warning_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 0.2M,
        Maximum = 10M,
        Increment = 0.1M,
        DecimalPlaces = 1,
        Value = 1.0M
      };
      Polling_Tab.Controls.Add ( _Skew_Warning_Numeric );
      Polling_Tab.Controls.Add ( new Label
      {
        Text = "(orange)",
        Location = new Point ( 340, Y ),
        AutoSize = true,
        ForeColor = Color.Orange
      } );
      Y += 30;

      Polling_Tab.Controls.Add ( new Label
      {
        Text = "Stale Data (seconds):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      } );
      _Stale_Data_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 0.2M,
        Maximum = 60M,
        Increment = 0.5M,
        DecimalPlaces = 1,
        Value = 3.0M
      };
      Polling_Tab.Controls.Add ( _Stale_Data_Numeric );
      Polling_Tab.Controls.Add ( new Label
      {
        Text = "(red)",
        Location = new Point ( 340, Y ),
        AutoSize = true,
        ForeColor = Color.Red
      } );
      Y += 30;


    }



    private void Initialize_Analysis_Tab ( )
    {
      int Left_Col = 15;
      int Y = 15;
      int Row_H = 28;

      // ── Auto-run ─────────────────────────────────────────────────────
      Analysis_Tab.Controls.Add ( new Label
      {
        Text = "Trigger:",
        Location = new Point ( Left_Col, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      } );
      Y += 22;

      _Auto_Analyze_Check = new CheckBox
      {
        Text = "Automatically show analysis popup when recording stops",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      };
      Analysis_Tab.Controls.Add ( _Auto_Analyze_Check );
      Y += Row_H + 8;

      // ── Series count ─────────────────────────────────────────────────
      Analysis_Tab.Controls.Add ( new Label
      {
        Text = "Compare:",
        Location = new Point ( Left_Col, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      } );
      Y += 22;

      _Analysis_One_Series_Radio = new RadioButton
      {
        Text = "Single series  (summary stats only)",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      };
      Analysis_Tab.Controls.Add ( _Analysis_One_Series_Radio );
      Y += Row_H;

      _Analysis_Two_Series_Radio = new RadioButton
      {
        Text = "Two series  (full delta / σ / histogram comparison)",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true
      };
      Analysis_Tab.Controls.Add ( _Analysis_Two_Series_Radio );
      Y += Row_H + 8;

      // ── Which stats ──────────────────────────────────────────────────
      Analysis_Tab.Controls.Add ( new Label
      {
        Text = "Include in analysis:",
        Location = new Point ( Left_Col, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      } );
      Y += 22;

      var Stat_Checks = new (string Text, Action<CheckBox> Assign) [ ]
      {
        ( "Mean / Average",  C => _Analysis_Mean_Check        = C ),
        ( "Std Dev (σ)",     C => _Analysis_StdDev_Check      = C ),
        ( "Min / Max",       C => _Analysis_MinMax_Check      = C ),
        ( "RMS",             C => _Analysis_RMS_Check         = C ),
        ( "Trend direction", C => _Analysis_Trend_Check       = C ),
        ( "Sample rate",     C => _Analysis_Sample_Rate_Check = C ),
        ( "Error count",     C => _Analysis_Errors_Check      = C ),
      };

      foreach ( var (Text, Assign) in Stat_Checks )
      {
        var CB = new CheckBox
        {
          Text = Text,
          Location = new Point ( Left_Col, Y ),
          AutoSize = true
        };
        Assign ( CB );
        Analysis_Tab.Controls.Add ( CB );
        Y += Row_H;
      }

      Y += 8;
      Analysis_Tab.Controls.Add ( new Label
      {
        Text = "Note: two-series comparison always shows delta, rolling σ, and histogram tabs.",
        Location = new Point ( Left_Col, Y ),
        AutoSize = true,
        ForeColor = SystemColors.GrayText
      } );
    }

    private void Initialize_Files_Tab ( )
    {
      int Y = 15;

      // Save Folder
      var Save_Folder_Label = new Label
      {
        Text = "Default Save Folder:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Files_Tab.Controls.Add ( Save_Folder_Label );
      Y += 20;

      _Save_Folder_Text = new TextBox
      {
        Location = new Point ( 15, Y ),
        Size = new Size ( 420, 23 ),
        ReadOnly = true
      };
      Files_Tab.Controls.Add ( _Save_Folder_Text );

      _Browse_Folder_Button = new Button
      {
        Location = new Point ( 440, Y ),
        Size = new Size ( 80, 23 ),
        Text = "Browse..."
      };
      _Browse_Folder_Button.Click += Browse_Folder_Button_Click;
      Files_Tab.Controls.Add ( _Browse_Folder_Button );
      Y += 35;

      // Filename Pattern
      var Pattern_Label = new Label
      {
        Text = "Filename Pattern:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Files_Tab.Controls.Add ( Pattern_Label );
      Y += 20;

      _Filename_Pattern_Text = new TextBox
      {
        Location = new Point ( 15, Y ),
        Size = new Size ( 300, 23 )
      };
      Files_Tab.Controls.Add ( _Filename_Pattern_Text );

      var Pattern_Help = new Label
      {
        Text = "Use: {date}, {time}, {function}",
        Location = new Point ( 320, Y + 3 ),
        AutoSize = true,
        ForeColor = SystemColors.GrayText
      };
      Files_Tab.Controls.Add ( Pattern_Help );
      Y += 35;

      // Auto Save
      _Enable_Auto_Save_Check = new CheckBox
      {
        Text = "Enable auto-save",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Files_Tab.Controls.Add ( _Enable_Auto_Save_Check );
      Y += 30;

      // Auto Save Interval
      var Auto_Save_Label = new Label
      {
        Text = "Auto-save Interval (min):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Files_Tab.Controls.Add ( Auto_Save_Label );

      _Auto_Save_Interval_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 1,
        Maximum = 1440,
        Value = 5
      };
      Files_Tab.Controls.Add ( _Auto_Save_Interval_Numeric );
      Y += 30;

      // Auto Save on Stop
      _Auto_Save_On_Stop_Check = new CheckBox
      {
        Text = "Auto-save when stopping",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Files_Tab.Controls.Add ( _Auto_Save_On_Stop_Check );
      Y += 30;

      // Include Statistics
      _Include_Stats_Check = new CheckBox
      {
        Text = "Include statistics in saved files",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Files_Tab.Controls.Add ( _Include_Stats_Check );
      Y += 30;

      // Prompt Before Clear
      _Prompt_Before_Clear_Check = new CheckBox
      {
        Text = "Prompt before clearing data",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Files_Tab.Controls.Add ( _Prompt_Before_Clear_Check );
      Y += 30;

      // Auto Load Last
      _Auto_Load_Last_Check = new CheckBox
      {
        Text = "Auto-load last session on startup",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Files_Tab.Controls.Add ( _Auto_Load_Last_Check );
      Y += 30;

      // Export Format
      var Export_Label = new Label
      {
        Text = "Export Format:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Files_Tab.Controls.Add ( Export_Label );

      _Export_Format_Combo = new ComboBox
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 100, 23 ),
        DropDownStyle = ComboBoxStyle.DropDownList
      };
      _Export_Format_Combo.Items.AddRange ( new object [ ] { "CSV", "Excel", "JSON" } );
      Files_Tab.Controls.Add ( _Export_Format_Combo );
    }

    private void Initialize_Performance_Tab ( )
    {
      int Y = 15;

      // Max Points in Memory
      var Max_Points_Label = new Label
      {
        Text = "Max Data Points in Memory:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Performance_Tab.Controls.Add ( Max_Points_Label );

      _Max_Points_Memory_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 100, 23 ),
        Minimum = 1000,
        Maximum = 10000000,
        Increment = 10000,
        Value = 100000
      };
      Performance_Tab.Controls.Add ( _Max_Points_Memory_Numeric );
      Y += 30;

      // Warning Threshold
      var Warning_Label = new Label
      {
        Text = "Warning Threshold (%):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Performance_Tab.Controls.Add ( Warning_Label );

      _Warning_Threshold_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 50,
        Maximum = 99,
        Value = 80
      };
      Performance_Tab.Controls.Add ( _Warning_Threshold_Numeric );
      Y += 30;

      // Warn at Threshold
      _Warn_At_Threshold_Check = new CheckBox
      {
        Text = "Warn when reaching threshold",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Performance_Tab.Controls.Add ( _Warn_At_Threshold_Check );
      Y += 35;

      // Separator
      var Separator = new Label
      {
        Text = "Performance Optimization:",
        Location = new Point ( 15, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      };
      Performance_Tab.Controls.Add ( Separator );
      Y += 25;

      // Throttle When Many Points
      _Throttle_When_Many_Check = new CheckBox
      {
        Text = "Throttle refresh when many points",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Performance_Tab.Controls.Add ( _Throttle_When_Many_Check );
      Y += 30;

      // Throttle Threshold
      var Throttle_Label = new Label
      {
        Text = "Throttle Point Threshold:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Performance_Tab.Controls.Add ( Throttle_Label );

      _Throttle_Threshold_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 100, 23 ),
        Minimum = 1000,
        Maximum = 1000000,
        Increment = 1000,
        Value = 10000
      };
      Performance_Tab.Controls.Add ( _Throttle_Threshold_Numeric );
      Y += 30;

      // Auto Trim Old Data
      _Auto_Trim_Check = new CheckBox
      {
        Text = "Auto-trim old data",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Performance_Tab.Controls.Add ( _Auto_Trim_Check );
      Y += 30;

      // Keep Last N Points
      var Keep_Last_Label = new Label
      {
        Text = "Keep Last N Points:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Performance_Tab.Controls.Add ( Keep_Last_Label );

      _Keep_Last_N_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 100, 23 ),
        Minimum = 100,
        Maximum = 1000000,
        Increment = 1000,
        Value = 10000
      };
      Performance_Tab.Controls.Add ( _Keep_Last_N_Numeric );
      Y += 30;

      // Reduce Refresh Rate
      _Reduce_Refresh_Check = new CheckBox
      {
        Text = "Reduce refresh rate when large dataset",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Performance_Tab.Controls.Add ( _Reduce_Refresh_Check );
    }

    private void Initialize_UI_Tab ( )
    {
      int Y = 15;

      // Window Title
      var Title_Label = new Label
      {
        Text = "Default Window Title:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( Title_Label );
      Y += 20;

      _Window_Title_Text = new TextBox
      {
        Location = new Point ( 15, Y ),
        Size = new Size ( 400, 23 )
      };
      UI_Tab.Controls.Add ( _Window_Title_Text );
      Y += 35;

      // Remember Window Size
      _Remember_Size_Check = new CheckBox
      {
        Text = "Remember window size",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( _Remember_Size_Check );
      Y += 30;

      // Remember Window Position
      _Remember_Position_Check = new CheckBox
      {
        Text = "Remember window position",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( _Remember_Position_Check );
      Y += 35;

      // Separator
      var Separator = new Label
      {
        Text = "Input Controls:",
        Location = new Point ( 15, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( Separator );
      Y += 25;

      // Enable Keyboard Pan
      _Enable_Keyboard_Pan_Check = new CheckBox
      {
        Text = "Enable keyboard panning (Arrow keys, Page Up/Down)",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( _Enable_Keyboard_Pan_Check );
      Y += 30;

      // Enable Ctrl+Scroll Zoom
      _Enable_Ctrl_Zoom_Check = new CheckBox
      {
        Text = "Enable Ctrl+Scroll wheel zooming",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( _Enable_Ctrl_Zoom_Check );
      Y += 35;

      // Separator
      var Separator2 = new Label
      {
        Text = "Feedback:",
        Location = new Point ( 15, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( Separator2 );
      Y += 25;

      // Show Progress Messages
      _Show_Progress_Check = new CheckBox
      {
        Text = "Show progress messages",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( _Show_Progress_Check );
      Y += 30;

      // Flash on Error
      _Flash_On_Error_Check = new CheckBox
      {
        Text = "Flash window on error",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( _Flash_On_Error_Check );
      Y += 30;

      // Play Sound on Complete
      _Play_Sound_Check = new CheckBox
      {
        Text = "Play sound when polling complete",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( _Play_Sound_Check );

      Y += 35;
      // Separator
      var Theme_Separator = new Label
      {
        Text = "Appearance:",
        Location = new Point ( 15, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( Theme_Separator );
      Y += 25;

      // Theme selector
      var Theme_Label = new Label
      {
        Text = "Chart Theme:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      UI_Tab.Controls.Add ( Theme_Label );

      _Theme_Combo = new ComboBox
      {
        Location = new Point ( 120, Y - 3 ),
        Size = new Size ( 150, 23 ),
        DropDownStyle = ComboBoxStyle.DropDownList
      };
      _Theme_Combo.Items.Add ( "Dark" );
      _Theme_Combo.Items.Add ( "Light" );
      _Theme_Combo.Items.Add ( "Brown" );
      _Theme_Combo.Items.Add ( "Grey" );
      _Theme_Combo.Items.Add ( "Golden" );
      _Theme_Combo.Items.Add ( "Light Yellow" );
      _Theme_Combo.SelectedItem = _Settings.Current_Theme.Name;
      _Theme_Combo.SelectedIndexChanged += ( s, e ) =>
      {
        string Selected = _Theme_Combo.SelectedItem?.ToString ( ) ?? "Dark";
        Chart_Theme New_Theme = Selected switch
        {
          "Light" => Chart_Theme.Light_Preset ( ),
          "Brown" => Chart_Theme.Brown_Preset ( ),
          "Grey" => Chart_Theme.Grey_Preset ( ),
          "Golden" => Chart_Theme.Golden_Preset ( ),
          "Light Yellow" => Chart_Theme.Light_Yellow_Preset ( ),
          _ => Chart_Theme.Dark_Preset ( )
        };
        _Settings.Set_Theme ( New_Theme );
      };
      UI_Tab.Controls.Add ( _Theme_Combo );

    }

    private void Initialize_Zoom_Tab ( )
    {
      int Y = 15;

      // Default Zoom Level
      var Zoom_Label = new Label
      {
        Text = "Default Zoom Level (1-100):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Zoom_Tab.Controls.Add ( Zoom_Label );

      _Default_Zoom_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 1,
        Maximum = 100,
        Value = 50
      };
      Zoom_Tab.Controls.Add ( _Default_Zoom_Numeric );

      var Zoom_Help = new Label
      {
        Text = "(50 = 1.0x zoom, <50 = zoom out, >50 = zoom in)",
        Location = new Point ( 15, Y + 25 ),
        AutoSize = true,
        ForeColor = SystemColors.GrayText
      };
      Zoom_Tab.Controls.Add ( Zoom_Help );
      Y += 60;

      // Zoom Sensitivity
      var Sensitivity_Label = new Label
      {
        Text = "Zoom Sensitivity:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Zoom_Tab.Controls.Add ( Sensitivity_Label );

      _Zoom_Sensitivity_Slider = new TrackBar
      {
        Location = new Point ( 15, Y + 25 ),
        Size = new Size ( 300, 45 ),
        Minimum = 1,
        Maximum = 50,
        TickFrequency = 5,
        Value = 10
      };
      _Zoom_Sensitivity_Slider.ValueChanged += Zoom_Sensitivity_Slider_ValueChanged;
      Zoom_Tab.Controls.Add ( _Zoom_Sensitivity_Slider );

      _Zoom_Sensitivity_Label = new Label
      {
        Text = "1.0x",
        Location = new Point ( 320, Y + 35 ),
        AutoSize = true
      };
      Zoom_Tab.Controls.Add ( _Zoom_Sensitivity_Label );
      Y += 80;

      // Remember Zoom Level
      _Remember_Zoom_Check = new CheckBox
      {
        Text = "Remember zoom level between sessions",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Zoom_Tab.Controls.Add ( _Remember_Zoom_Check );
    }









    private void Initialize_HP_Tab ( )
    {
      var Scroll_Panel = new Panel
      {
        AutoScroll = true,
        Dock = DockStyle.Fill
      };
      HP3458_Tab.Controls.Add ( Scroll_Panel );  // ← missing

      int Y = 15;



      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "Connection & Reset:",
        Location = new Point ( 15, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      } );
      Y += 25;

      _Send_Reset_On_Connect_Check = new CheckBox
      {
        Text = "Send RESET on connect",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Scroll_Panel.Controls.Add ( _Send_Reset_On_Connect_Check );
      Y += 30;

      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "Reset Settle Delay (ms):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      } );
      _Reset_Delay_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 500,
        Maximum = 10000,
        Increment = 500,
        Value = 2000
      };
      Scroll_Panel.Controls.Add ( _Reset_Delay_Numeric );
      Y += 30;

      _Send_End_Always_Check = new CheckBox
      {
        Text = "Send END ALWAYS on connect",
        Location = new Point ( 15, Y ),
        AutoSize = true
      };
      Scroll_Panel.Controls.Add ( _Send_End_Always_Check );
      Y += 35;

      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "Timing:",
        Location = new Point ( 15, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      } );
      Y += 25;

      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "Instrument Settle Time (ms):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      } );
      _Instrument_Settle_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 50,
        Maximum = 2000,
        Increment = 50,
        Value = 200
      };
      Scroll_Panel.Controls.Add ( _Instrument_Settle_Numeric );
      Y += 30;



      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "ERR? Read Delay (ms):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      } );
      _ERR_Read_Delay_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 100,
        Maximum = 2000,
        Increment = 100,
        Value = 500
      };
      Scroll_Panel.Controls.Add ( _ERR_Read_Delay_Numeric );
      Y += 30;

      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "NPLC Apply Delay (ms):",
        Location = new Point ( 15, Y ),
        AutoSize = true
      } );
      _NPLC_Apply_Delay_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 80, 23 ),
        Minimum = 0,
        Maximum = 1000,
        Increment = 50,
        Value = 50
      };
      Scroll_Panel.Controls.Add ( _NPLC_Apply_Delay_Numeric );
      Y += 35;

      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "Measurement Defaults:",
        Location = new Point ( 15, Y ),
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        AutoSize = true
      } );
      Y += 25;

      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "Default NPLC:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      } );
      _Default_NPLC_3458_Combo = new ComboBox
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 100, 23 ),
        DropDownStyle = ComboBoxStyle.DropDownList
      };
      _Default_NPLC_3458_Combo.Items.AddRange (
          new object [ ] { "0.001", "0.01", "0.1", "1", "2", "10", "100" } );
      Scroll_Panel.Controls.Add ( _Default_NPLC_3458_Combo );
      Y += 30;

      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "Default Trigger Mode:",
        Location = new Point ( 15, Y ),
        AutoSize = true
      } );
      _Default_Trig_Mode_Combo = new ComboBox
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 120, 23 ),
        DropDownStyle = ComboBoxStyle.DropDownList
      };
      _Default_Trig_Mode_Combo.Items.AddRange (
          new object [ ] { "TRIG AUTO", "TRIG HOLD", "TRIG EXT" } );
      Scroll_Panel.Controls.Add ( _Default_Trig_Mode_Combo );

      Y += 35;

      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "HP3458 Digits (4-10):",
        Location = new Point ( 15, Y ),       // ← left column
        AutoSize = true
      } );
      _Display_Digits_Numeric = new NumericUpDown
      {
        Location = new Point ( 250, Y - 3 ),
        Size = new Size ( 60, 23 ),
        Minimum = 4,
        Maximum = 10
      };
      Scroll_Panel.Controls.Add ( _Display_Digits_Numeric );
      Scroll_Panel.Controls.Add ( new Label
      {
        Text = "(also sets display precision)",
        Location = new Point ( 320, Y ),     // ← right of numeric
        AutoSize = true,
        ForeColor = SystemColors.GrayText
      } );
      Y += 30;





    }




    private void Load_Settings ( )
    {
      // HP tab
      _Send_Reset_On_Connect_Check.Checked = _Settings.Send_Reset_On_Connect_3458;
      _Reset_Delay_Numeric.Value = _Settings.Reset_Settle_Delay_Ms;
      _Send_End_Always_Check.Checked = _Settings.Send_End_Always_3458;
      _Instrument_Settle_Numeric.Value = _Settings.Instrument_Settle_Ms;

      _ERR_Read_Delay_Numeric.Value = _Settings.ERR_Read_Delay_Ms;
      _NPLC_Apply_Delay_Numeric.Value = _Settings.NPLC_Apply_Delay_Ms;
      _Default_NPLC_3458_Combo.SelectedItem = _Settings.Default_NPLC_3458;
      _Default_Trig_Mode_Combo.SelectedItem = _Settings.Default_Trig_Mode_3458;




      _Prologix_IP_Textbox.Text = _Settings.Default_IP_Address;
      _Prologix_Port_Numeric.Value = _Settings.Default_Prologic_Port;
      _Prologix_MAC_Textbox.Text = _Settings.Prologic_MAC_Address;
      _Prologix_GPIB_Address_Numeric.Value = _Settings.Default_GPIB_Instrument_Address;
      _Prologix_Auto_Read_Check.Checked = _Settings.Prologix_Auto_Read;
      _Prologix_Read_Tmo_Numeric.Value = _Settings.Prologix_Read_Tmo_Ms;
      _Prologix_Fetch_Numeric.Value = _Settings.Prologix_Fetch_Ms;
      _Prologix_Subnet_Textbox.Text = _Settings.Network_Scan_Subnet;
      _Prologic_Scan_Timeout_MS_Textbox.Text = _Settings.Prologic_Scan_Timeout_MS.ToString();


      // Analysis tab
      _Auto_Analyze_Check.Checked = _Settings.Auto_Analyze_After_Recording;
      _Analysis_Mean_Check.Checked = _Settings.Analysis_Show_Mean;
      _Analysis_StdDev_Check.Checked = _Settings.Analysis_Show_Std_Dev;
      _Analysis_MinMax_Check.Checked = _Settings.Analysis_Show_Min_Max;
      _Analysis_RMS_Check.Checked = _Settings.Analysis_Show_RMS;
      _Analysis_Trend_Check.Checked = _Settings.Analysis_Show_Trend;
      _Analysis_Sample_Rate_Check.Checked = _Settings.Analysis_Show_Sample_Rate;
      _Analysis_Errors_Check.Checked = _Settings.Analysis_Show_Errors;
      if ( _Settings.Analysis_Series_Count == 1 )
        _Analysis_One_Series_Radio.Checked = true;
      else
        _Analysis_Two_Series_Radio.Checked = true;

      // Display tab
      _Tooltip_Distance_Numeric.Value = _Settings.Tooltip_Distance_Threshold;
      _Show_Tooltips_Check.Checked = _Settings.Show_Tooltips_On_Hover;
      _Tooltip_Duration_Numeric.Value = _Settings.Tooltip_Display_Duration_Ms;
      _Chart_Refresh_Numeric.Value = _Settings.Chart_Refresh_Rate_Ms;
      _Show_Grid_Check.Checked = _Settings.Show_Grid_Lines;
      _Show_Dots_Check.Checked = _Settings.Show_Data_Dots;
      _Dot_Size_Numeric.Value = _Settings.Data_Dot_Size;
      _Line_Thickness_Numeric.Value = _Settings.Line_Thickness;
      _Default_Combined_Check.Checked = _Settings.Default_To_Combined_View;
      _Default_Normalized_Check.Checked = _Settings.Default_To_Normalized_View;
      _Show_Legend_Check.Checked = _Settings.Show_Legend_On_Startup;
     


      // Polling tab
      _Poll_Delay_Numeric.Value = _Settings.Default_Poll_Delay_Ms;
      _NPLC_Combo.SelectedItem = _Settings.Default_NPLC;
      _Measurement_Type_Combo.SelectedItem = _Settings.Default_Measurement_Type;
      _Continuous_Poll_Check.Checked = _Settings.Default_Continuous_Poll;
      _GPIB_Timeout_Numeric.Value = _Settings.Default_GPIB_Timeout_Ms;
      _Max_Retry_Attempts_Numeric.Value = _Settings.Max_Retry_Attempts;
      _Max_Errors_Numeric.Value = _Settings.Max_Consecutive_Errors_Before_Disable;
      _Auto_Retry_Check.Checked = _Settings.Auto_Retry_Failed_Instruments;
      _Retry_Delay_Numeric.Value = _Settings.Retry_Delay_Seconds;
      _Skew_Warning_Numeric.Value = (decimal) _Settings.Skew_Warning_Threshold_Seconds;
      _Stale_Data_Numeric.Value = (decimal) _Settings.Stale_Data_Threshold_Seconds;
      _Max_Display_Points_Numeric.Value = _Settings.Max_Display_Points;
      _Stop_At_Max_Check.Checked = _Settings.Stop_Polling_At_Max_Display_Points;

      // Files tab
      _Save_Folder_Text.Text = _Settings.Default_Save_Folder;
      _Filename_Pattern_Text.Text = _Settings.Filename_Pattern;
      _Enable_Auto_Save_Check.Checked = _Settings.Enable_Auto_Save;
      _Auto_Save_Interval_Numeric.Value = _Settings.Auto_Save_Interval_Minutes;
      _Auto_Save_On_Stop_Check.Checked = _Settings.Auto_Save_On_Stop;
      _Include_Stats_Check.Checked = _Settings.Include_Statistics_In_Save;
      _Prompt_Before_Clear_Check.Checked = _Settings.Prompt_Before_Clear;
      _Auto_Load_Last_Check.Checked = _Settings.Auto_Load_Last_Session;
      _Export_Format_Combo.SelectedItem = _Settings.Export_Format;

      // Performance tab
      _Max_Points_Memory_Numeric.Value = _Settings.Max_Data_Points_In_Memory;
      _Warning_Threshold_Numeric.Value = _Settings.Warning_Threshold_Percent;
      _Warn_At_Threshold_Check.Checked = _Settings.Warn_At_Threshold;
      _Throttle_When_Many_Check.Checked = _Settings.Throttle_When_Many_Points;
      _Throttle_Threshold_Numeric.Value = _Settings.Throttle_Point_Threshold;
      _Auto_Trim_Check.Checked = _Settings.Auto_Trim_Old_Data;
      _Keep_Last_N_Numeric.Value = _Settings.Keep_Last_N_Points;
      _Reduce_Refresh_Check.Checked = _Settings.Reduce_Refresh_Rate_When_Large;

      // UI tab
      _Window_Title_Text.Text = _Settings.Default_Window_Title;
      _Remember_Size_Check.Checked = _Settings.Remember_Window_Size;
      _Remember_Position_Check.Checked = _Settings.Remember_Window_Position;
      _Enable_Keyboard_Pan_Check.Checked = _Settings.Enable_Keyboard_Pan;
      _Enable_Ctrl_Zoom_Check.Checked = _Settings.Enable_Ctrl_Scroll_Zoom;
      _Show_Progress_Check.Checked = _Settings.Show_Progress_Messages;
      _Flash_On_Error_Check.Checked = _Settings.Flash_On_Error;
      _Play_Sound_Check.Checked = _Settings.Play_Sound_On_Complete;

      // Zoom tab
      _Default_Zoom_Numeric.Value = _Settings.Default_Zoom_Level;
      _Zoom_Sensitivity_Slider.Value = (int) ( _Settings.Zoom_Sensitivity * 10 );
      _Zoom_Sensitivity_Label.Text = $"{_Settings.Zoom_Sensitivity:F1}x";
      _Remember_Zoom_Check.Checked = _Settings.Remember_Zoom_Level;
    }

    private void Save_Settings ( )
    {

      // HP tab
      _Settings.Send_Reset_On_Connect_3458 = _Send_Reset_On_Connect_Check.Checked;
      _Settings.Reset_Settle_Delay_Ms = (int) _Reset_Delay_Numeric.Value;
      _Settings.Send_End_Always_3458 = _Send_End_Always_Check.Checked;
      _Settings.Instrument_Settle_Ms = (int) _Instrument_Settle_Numeric.Value;

      _Settings.ERR_Read_Delay_Ms = (int) _ERR_Read_Delay_Numeric.Value;
      _Settings.NPLC_Apply_Delay_Ms = (int) _NPLC_Apply_Delay_Numeric.Value;
      _Settings.Default_NPLC_3458 = _Default_NPLC_3458_Combo.SelectedItem?.ToString ( ) ?? "1";
      _Settings.Default_Trig_Mode_3458 = _Default_Trig_Mode_Combo.SelectedItem?.ToString ( ) ?? "TRIG AUTO";





      // Prologic Tab

      _Settings.Default_IP_Address = _Prologix_IP_Textbox.Text.Trim ( );
      _Settings.Default_Prologic_Port = (int) _Prologix_Port_Numeric.Value;
      _Settings.Prologic_MAC_Address = _Prologix_MAC_Textbox.Text.Trim ( );
      _Settings.Default_GPIB_Instrument_Address = (int) _Prologix_GPIB_Address_Numeric.Value;
      _Settings.Prologix_Auto_Read = _Prologix_Auto_Read_Check.Checked;
      _Settings.Prologix_Read_Tmo_Ms = (int) _Prologix_Read_Tmo_Numeric.Value;
      _Settings.Prologix_Fetch_Ms = (int) _Prologix_Fetch_Numeric.Value;
      _Settings.Network_Scan_Subnet = _Prologix_Subnet_Textbox.Text.Trim ( ).TrimEnd ( '.' );
      _Settings.Prologic_Scan_Timeout_MS = int.Parse ( _Prologic_Scan_Timeout_MS_Textbox.Text );

      // Analysis tab
      _Settings.Auto_Analyze_After_Recording = _Auto_Analyze_Check.Checked;
      _Settings.Analysis_Series_Count = _Analysis_Two_Series_Radio.Checked ? 2 : 1;
      _Settings.Analysis_Show_Mean = _Analysis_Mean_Check.Checked;
      _Settings.Analysis_Show_Std_Dev = _Analysis_StdDev_Check.Checked;
      _Settings.Analysis_Show_Min_Max = _Analysis_MinMax_Check.Checked;
      _Settings.Analysis_Show_RMS = _Analysis_RMS_Check.Checked;
      _Settings.Analysis_Show_Trend = _Analysis_Trend_Check.Checked;
      _Settings.Analysis_Show_Sample_Rate = _Analysis_Sample_Rate_Check.Checked;
      _Settings.Analysis_Show_Errors = _Analysis_Errors_Check.Checked;

      // Display tab
      _Settings.Tooltip_Distance_Threshold = (int) _Tooltip_Distance_Numeric.Value;
      _Settings.Show_Tooltips_On_Hover = _Show_Tooltips_Check.Checked;
      _Settings.Tooltip_Display_Duration_Ms = (int) _Tooltip_Duration_Numeric.Value;
      _Settings.Chart_Refresh_Rate_Ms = (int) _Chart_Refresh_Numeric.Value;
      _Settings.Show_Grid_Lines = _Show_Grid_Check.Checked;
      _Settings.Show_Data_Dots = _Show_Dots_Check.Checked;
      _Settings.Data_Dot_Size = (int) _Dot_Size_Numeric.Value;
      _Settings.Line_Thickness = (int) _Line_Thickness_Numeric.Value;
      _Settings.Default_To_Combined_View = _Default_Combined_Check.Checked;
      _Settings.Default_To_Normalized_View = _Default_Normalized_Check.Checked;
      _Settings.Show_Legend_On_Startup = _Show_Legend_Check.Checked;
    




      // Polling tab
      _Settings.Default_Poll_Delay_Ms = (int) _Poll_Delay_Numeric.Value;
      _Settings.Default_NPLC = _NPLC_Combo.SelectedItem?.ToString ( ) ?? "10";
      _Settings.Default_Measurement_Type = _Measurement_Type_Combo.SelectedItem?.ToString ( ) ?? "DC Voltage";
      _Settings.Default_Continuous_Poll = _Continuous_Poll_Check.Checked;
      _Settings.Default_GPIB_Timeout_Ms = (int) _GPIB_Timeout_Numeric.Value;
      _Settings.Max_Retry_Attempts = (int) _Max_Retry_Attempts_Numeric.Value;
      _Settings.Max_Consecutive_Errors_Before_Disable = (int) _Max_Errors_Numeric.Value;
      _Settings.Auto_Retry_Failed_Instruments = _Auto_Retry_Check.Checked;
      _Settings.Retry_Delay_Seconds = (int) _Retry_Delay_Numeric.Value;
      _Settings.Skew_Warning_Threshold_Seconds = (double) _Skew_Warning_Numeric.Value;
      _Settings.Stale_Data_Threshold_Seconds = (double) _Stale_Data_Numeric.Value;
      _Settings.Max_Display_Points = (int) _Max_Display_Points_Numeric.Value;
      _Settings.Stop_Polling_At_Max_Display_Points = _Stop_At_Max_Check.Checked;

      // Files tab
      _Settings.Default_Save_Folder = _Save_Folder_Text.Text;
      _Settings.Filename_Pattern = _Filename_Pattern_Text.Text;
      _Settings.Enable_Auto_Save = _Enable_Auto_Save_Check.Checked;
      _Settings.Auto_Save_Interval_Minutes = (int) _Auto_Save_Interval_Numeric.Value;
      _Settings.Auto_Save_On_Stop = _Auto_Save_On_Stop_Check.Checked;
      _Settings.Include_Statistics_In_Save = _Include_Stats_Check.Checked;
      _Settings.Prompt_Before_Clear = _Prompt_Before_Clear_Check.Checked;
      _Settings.Auto_Load_Last_Session = _Auto_Load_Last_Check.Checked;
      _Settings.Export_Format = _Export_Format_Combo.SelectedItem?.ToString ( ) ?? "CSV";

      // Performance tab
      _Settings.Max_Data_Points_In_Memory = (int) _Max_Points_Memory_Numeric.Value;
      _Settings.Warning_Threshold_Percent = (int) _Warning_Threshold_Numeric.Value;
      _Settings.Warn_At_Threshold = _Warn_At_Threshold_Check.Checked;
      _Settings.Throttle_When_Many_Points = _Throttle_When_Many_Check.Checked;
      _Settings.Throttle_Point_Threshold = (int) _Throttle_Threshold_Numeric.Value;
      _Settings.Auto_Trim_Old_Data = _Auto_Trim_Check.Checked;
      _Settings.Keep_Last_N_Points = (int) _Keep_Last_N_Numeric.Value;
      _Settings.Reduce_Refresh_Rate_When_Large = _Reduce_Refresh_Check.Checked;

      // UI tab
      _Settings.Default_Window_Title = _Window_Title_Text.Text;
      _Settings.Remember_Window_Size = _Remember_Size_Check.Checked;
      _Settings.Remember_Window_Position = _Remember_Position_Check.Checked;
      _Settings.Enable_Keyboard_Pan = _Enable_Keyboard_Pan_Check.Checked;
      _Settings.Enable_Ctrl_Scroll_Zoom = _Enable_Ctrl_Zoom_Check.Checked;
      _Settings.Show_Progress_Messages = _Show_Progress_Check.Checked;
      _Settings.Flash_On_Error = _Flash_On_Error_Check.Checked;
      _Settings.Play_Sound_On_Complete = _Play_Sound_Check.Checked;

      // Zoom tab
      _Settings.Default_Zoom_Level = (int) _Default_Zoom_Numeric.Value;
      _Settings.Zoom_Sensitivity = _Zoom_Sensitivity_Slider.Value / 10.0;
      _Settings.Remember_Zoom_Level = _Remember_Zoom_Check.Checked;

      // Validate and save
      _Settings.Validate_And_Fix ( );
      _Settings.Save ( );
    }

    private void Browse_Folder_Button_Click ( object Sender, EventArgs E )
    {
      using var Dialog = new FolderBrowserDialog ( );
      Dialog.Description = "Select default save folder";
      Dialog.SelectedPath = _Save_Folder_Text.Text;

      if ( Dialog.ShowDialog ( ) == DialogResult.OK )
      {
        _Save_Folder_Text.Text = Dialog.SelectedPath;
      }
    }

    private void Zoom_Sensitivity_Slider_ValueChanged ( object Sender, EventArgs E )
    {
      double Value = _Zoom_Sensitivity_Slider.Value / 10.0;
      _Zoom_Sensitivity_Label.Text = $"{Value:F1}x";
    }

    private void OK_Button_Click ( object Sender, EventArgs E )
    {
      Save_Settings ( );
      DialogResult = DialogResult.OK;
      Close ( );
    }

    private void Apply_Button_Click ( object Sender, EventArgs E )
    {
      Save_Settings ( );
    }

    private void Cancel_Button_Click ( object Sender, EventArgs E )
    {
      DialogResult = DialogResult.Cancel;
      Close ( );
    }

    private void Reset_Button_Click ( object Sender, EventArgs E )
    {
      var Result = MessageBox.Show (
        "Reset all settings to default values?\n\nThis cannot be undone.",
        "Reset Defaults",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question );

      if ( Result == DialogResult.Yes )
      {
        _Settings = new Application_Settings ( );
        _Settings.Initialize_Default_Save_Folder ( );
        Load_Settings ( );
      }
    }
  }
}
