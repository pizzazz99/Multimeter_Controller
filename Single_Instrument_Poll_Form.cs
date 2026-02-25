// ============================================================================
// VOLTAGE READER FORM - WITH ROLLING GRAPH FEATURE
// ============================================================================
//
// Key additions for rolling graph:
// - Rolling mode checkbox
// - Max points numeric control
// - Get_Visible_Range() helper method
// - Modified Chart_Panel_Paint to show only visible range
// - Stats calculated on visible data only
//
// ============================================================================

using Multimeter_Controller.Properties;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;
using Trace_Execution_Namespace;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  public partial class Single_Instrument_Poll_Form : Form
  {

    private Application_Settings _Settings;
    private Instrument_Comm _Comm;

    private System.Windows.Forms.Timer _Auto_Save_Timer;  // ← Must be declared
    private System.Windows.Forms.Timer _Chart_Refresh_Timer;  // ← Must be declared

    private List<double> _Readings = new List<double> ( );
    private List<DateTime> _Reading_Timestamps = new List<DateTime> ( );

    private bool _Is_Recording = false;
    private bool _Chart_Needs_Refresh = false;
    private List<(DateTime Time, double Value)> _Recorded_Points =
      new List<(DateTime, double)> ( );

    private DateTime _Last_Chart_Refresh = DateTime.Now;

    private string _Instrument_Name = "";
    private int _Selected_Address = 0;

    // Cache for performance
    private PointF [ ] _Cached_Points = null;
    private int _Last_Cached_Count = 0;



    // Memory management
    private bool _Memory_Warning_Shown = false;

    // Timers
    private System.Windows.Forms.Timer _Timer;


    // Status labels
    private ToolStripStatusLabel _Memory_Status_Label;
    private ToolStripStatusLabel _Performance_Status_Label;

    // Recording state


    private int _View_Offset = 0;
    private bool _Auto_Scroll = true;



    // ========================================
    // FIELDS
    // ========================================

    private Meter_Type _Selected_Meter;

    // Data storage
    //  private readonly List<double> _Readings = new List<double>();
    //  private readonly List<DateTime> _Reading_Timestamps = new List<DateTime>();

    // State
    private CancellationTokenSource? _Cts;
    private bool _Is_Running;
    private string _Current_Unit = "V";

    // Recording
    //   private bool _Is_Recording;
    //   private readonly List<(DateTime Time, double Value)> _Recorded_Points =
    //       new List<(DateTime, double)>();
    private string _Record_Function = "";
    private string _Record_Unit = "";

    // UI
    private List<int> _Filtered_Indices = new List<int> ( );
    private bool _Show_Time_Axis = true;

    // *** NEW: Rolling graph settings ***
    private bool _Enable_Rolling = false;
    private int _Max_Display_Points = 100;

    // Theme
    private Chart_Theme _Theme = Chart_Theme.Load ( );

    private ToolTip _Chart_Tooltip = null!;
    private Point _Last_Mouse_Position = Point.Empty;
    private int _Hover_Point_Index = -1;
    private const int HOVER_RADIUS = 200;

   
    private Button _Stats_Toggle_Button;

    private int _Paint_Count = 0;
    private double _Actual_FPS = 0;
    private readonly Stopwatch _FPS_Stopwatch = Stopwatch.StartNew ( );

    // private StatusStrip? _Status_Panel;
    private Panel _Status_Panel;

    private static readonly (string Label, string Cmd_3458A,
      string Cmd_34401A, string Unit) [ ] _Functions =
    {
      ( "DC Voltage",    "DCV",    "CONF:VOLT:DC",  "V" ),
      ( "AC Voltage",    "ACV",    "CONF:VOLT:AC",  "V" ),
      ( "AC+DC Voltage", "ACDCV",  "",              "V" ),
      ( "DC Current",    "DCI",    "CONF:CURR:DC",  "A" ),
      ( "AC Current",    "ACI",    "CONF:CURR:AC",  "A" ),
      ( "AC+DC Current", "ACDCI",  "",              "A" ),
      ( "2-Wire Ohms",   "OHM",    "CONF:RES",      "Ohm" ),
      ( "4-Wire Ohms",   "OHMF",   "CONF:FRES",     "Ohm" ),
      ( "Frequency",     "FREQ",   "CONF:FREQ",     "Hz" ),
      ( "Period",        "PER",    "CONF:PER",       "s" ),
      ( "Direct Samp AC","DSAC",   "",              "V" ),
      ( "Direct Samp DC","DSDC",   "",              "V" ),
      ( "Sub-Sample AC", "SSAC",   "",              "V" ),
      ( "Sub-Sample DC", "SSDC",   "",              "V" ),
      ( "Continuity",    "",       "CONF:CONT",     "Ohm" ),
      ( "Diode",         "",       "CONF:DIOD",     "V" ),
      ( "Temperature",   "TEMP?",  "",              "\u00b0C" ),
    };




    public Single_Instrument_Poll_Form (
      Instrument_Comm Comm, Meter_Type Meter, int Address, Application_Settings Settings )
    {
      InitializeComponent ( );

      _Comm = Comm;
      _Settings = Settings;

      _Comm.Instrument_Settle_Ms = _Settings.Instrument_Settle_Ms;
      _Comm.Prologix_Fetch_Ms = _Settings.Prologix_Fetch_Ms;

      // CREATE TIMERS FIRST - before Apply_Settings()
      _Auto_Save_Timer = new System.Windows.Forms.Timer ( );
      _Auto_Save_Timer.Tick += Auto_Save_Timer_Tick;

      _Chart_Refresh_Timer = new System.Windows.Forms.Timer ( );
      _Chart_Refresh_Timer.Tick += Chart_Refresh_Timer_Tick;


 

      _Chart_Tooltip = new ToolTip
      {
        AutoPopDelay = 5000,     // Stay visible for 5 seconds
        InitialDelay = 100,      // Show after 100ms hover
        ReshowDelay = 100,       // Quick reshow
        ShowAlways = true,
        BackColor = Color.LightYellow,
        ForeColor = Color.Black
      };

      // Wire up mouse events
      //      Chart_Panel.MouseMove += Chart_Panel_MouseMove;
      //   Chart_Panel.MouseMove += Chart_Panel_MouseMove;
      //   Chart_Panel.MouseLeave += Chart_Panel_MouseLeave;


      typeof ( Panel ).InvokeMember ( "DoubleBuffered",
      System.Reflection.BindingFlags.SetProperty |
      System.Reflection.BindingFlags.Instance |
      System.Reflection.BindingFlags.NonPublic,
      null, Chart_Panel, new object [ ] { true } );

      _Comm = Comm;
      _Selected_Meter = Meter;
      _Selected_Address = Address;
      Capture_Trace.Write ( $"address: {Address}" );

      string Meter_Name = Meter switch
      {
        Meter_Type.HP34401A => "HP34401A",
        Meter_Type.HP33120A => "HP33120A",
        _ => "HP 3458A"
      };

      if ( _Selected_Address != 0 )
      {
        Meter_Name += $" (GPIB {_Selected_Address})";
      }

      Text = $"{Meter_Name} - Reader";
      Title_Label.Text = $"{Meter_Name} Connected";

      Populate_Function_Combo ( );

      Graph_Style_Combo.Items.Add ( "Line" );
      Graph_Style_Combo.Items.Add ( "Bar" );
      Graph_Style_Combo.Items.Add ( "Scatter" );
      Graph_Style_Combo.Items.Add ( "Step" );
      Graph_Style_Combo.Items.Add ( "Histogram" );
      Graph_Style_Combo.Items.Add ( "Pie" );
      Graph_Style_Combo.SelectedIndex = 0;

      Chart_Panel.BackColor = _Theme.Background;

      Rolling_Check.Enabled = true;
      Max_Points_Numeric.Enabled = true;
   


    Capture_Trace.Write ( "Constructor: about to call Query_Instrument_Name" );
      Query_Instrument_Name ( );
      Capture_Trace.Write ( "Constructor: returned from Query_Instrument_Name" );

      Capture_Trace.Write ( "Constructor: about to call Initialize_Status_Panel" );
      Initialize_Status_Panel ( );

      Capture_Trace.Write ( "Constructor: about to call Apply_Settings" );
      Apply_Settings ( );
      Capture_Trace.Write ( "Constructor: Apply_Settings complete" );

      NPLC_Textbox.Text = _Settings.Default_NPLC.ToString ( );

    }




    private string Get_Instrument_Display_Name ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      string Meter_Label = _Selected_Meter switch
      {
        Meter_Type.HP34401A => "HP34401A",
        Meter_Type.HP33120A => "HP33120A",
        Meter_Type.HP3458A => "HP 3458A",
        _ => "Unknown Meter"
      };

      if ( _Comm.Mode == Connection_Mode.Prologix_GPIB )
      {
        string Name = !string.IsNullOrWhiteSpace ( _Instrument_Name )
            ? _Instrument_Name
            : Meter_Label;
        return $"{Name} @ GPIB {_Comm.GPIB_Address}";
      }
      else
      {
        string Port = _Comm.Port_Name ?? "Serial";
        return $"{Meter_Label} @ {Port}";
      }
    }




    private void Query_Instrument_Name ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( $"Is_Connected = {_Comm.Is_Connected}" );
      Capture_Trace.Write ( $"Mode         = {_Comm.Mode}" );
      Capture_Trace.Write ( $"Meter        = {_Selected_Meter}" );


      if ( !_Comm.Is_Connected || _Comm.Mode != Connection_Mode.Prologix_GPIB )
      {
        Capture_Trace.Write ( "Skipping - not connected or not GPIB" );
        return;
      }
      try
      {
        string IDN;
        if ( _Selected_Meter == Meter_Type.HP3458A )
        {
          Capture_Trace.Write ( "Sending 'ID' to 3458A" );
          _Comm.Send_Instrument_Command ( "ID?" );
          Thread.Sleep ( _Settings.Instrument_Settle_Ms );

          using var Cts = new CancellationTokenSource ( TimeSpan.FromSeconds ( 3 ) );
          Capture_Trace.Write ( "Reading response..." );
          IDN = _Comm.Read_Instrument ( Cts.Token );
          Capture_Trace.Write ( $"Got response: [{IDN}]" );
        }
        else
        {
          Capture_Trace.Write ( "Sending '*IDN?' to non-3458A" );
          IDN = _Comm.Query_Instrument ( "*IDN?" );
          Capture_Trace.Write ( $"Got response: [{IDN}]" );
        }
        if ( !string.IsNullOrWhiteSpace ( IDN ) )
        {
          string [ ] Parts = IDN.Split ( ',' );
          _Instrument_Name = Parts.Length >= 2 ? Parts [ 1 ].Trim ( ) : IDN.Trim ( );
          Capture_Trace.Write ( $"Parsed instrument name: [{_Instrument_Name}]" );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"EXCEPTION: {Ex.Message}" );
      }
    }





    private void Auto_Save_Timer_Tick ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Is_Recording && _Recorded_Points.Count > 0 )
      {
        Save_Recorded_Data ( );
      }
    }

    private void Chart_Refresh_Timer_Tick ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Chart_Needs_Refresh )
      {
        Chart_Panel.Invalidate ( );
        _Chart_Needs_Refresh = false;
      }
    }

    private void Apply_Settings ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );


      Capture_Trace.Write ( $"Meter     = {_Selected_Meter}" );
      Capture_Trace.Write ( $"Connected = {_Comm?.Is_Connected}" );

      if ( !string.IsNullOrWhiteSpace ( _Settings.Default_Window_Title ) )
      {
        this.Text = _Settings.Default_Window_Title;
      }

      // 1. Save folder (create if needed)
      if ( !string.IsNullOrEmpty ( _Settings.Default_Save_Folder ) )
      {
        try
        {
          Directory.CreateDirectory ( _Settings.Default_Save_Folder );
        }
        catch { }
      }

      // 2. Auto-save timer (now it exists!)
      if ( _Settings.Enable_Auto_Save )
      {
        _Auto_Save_Timer.Interval = _Settings.Auto_Save_Interval_Minutes * 60 * 1000;
        _Auto_Save_Timer.Start ( );
      }
      else
      {
        _Auto_Save_Timer.Stop ( );
      }

      // 3. Apply GPIB timeout to comm
      if ( _Comm != null )
      {
        _Comm.Read_Timeout_Ms = _Settings.Default_GPIB_Timeout_Ms;
      }

      // 4. Chart refresh rate (now it exists!)
      bool Should_Throttle = _Settings.Throttle_When_Many_Points &&
                            _Readings.Count > _Settings.Throttle_Point_Threshold;

      int Refresh_Rate = Should_Throttle
        ? _Settings.Chart_Refresh_Rate_Ms * 2
        : _Settings.Chart_Refresh_Rate_Ms;

      _Chart_Refresh_Timer.Interval = Refresh_Rate;


      // Default NPLC
      if ( NPLC_Combo.Items.Contains ( _Settings.Default_NPLC ) )
      {
        NPLC_Combo.SelectedItem = _Settings.Default_NPLC;
      }

      NPLC_Combo.Enabled = ( _Selected_Meter == Meter_Type.HP3458A );

    }






    private void Position_Status_Panel ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      int Chart_Top = Chart_Panel.Top;
      int Chart_Right = Chart_Panel.Right;
      int Chart_Height = Chart_Panel.Height;

      _Status_Panel.Location = new Point (
          Chart_Right - _Status_Panel.Width - 10,
          Chart_Top );
      _Status_Panel.Height = Math.Min ( 300, Chart_Height );
    }

    private void Update_Status_Panel ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Status_Panel.Controls.Clear ( );
      _Status_Panel.BackColor = _Theme.Background;

      if ( _Readings.Count == 0 )
      {
        var No_Data_Label = new Label
        {
          Text = "No data yet",
          Location = new Point ( 10, 10 ),
          ForeColor = _Theme.Foreground,
          AutoSize = true
        };
        _Status_Panel.Controls.Add ( No_Data_Label );
        return;
      }

      // Get visible range for accurate stats
      var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  _Readings.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

      // Clamp visible count to actual data
      int Actual_Count = Math.Min ( Visible_Count, _Readings.Count - Start_Index );
      if ( Actual_Count <= 0 )
        return;

      int End_Index = Start_Index + Actual_Count;

      // Calculate statistics on VISIBLE data
      double Min = double.MaxValue;
      double Max = double.MinValue;
      double Sum = 0;

      for ( int I = Start_Index; I < End_Index; I++ )  // ← Fixed
      {
        double V = _Readings [ I ];
        if ( V < Min )
          Min = V;
        if ( V > Max )
          Max = V;
        Sum += V;
      }

      double Avg = Sum / Actual_Count;  // ← Use Actual_Count
      double Range = Max - Min;

      // Standard deviation
      double SumSq = 0;
      for ( int I = Start_Index; I < End_Index; I++ )
      {
        double D = _Readings [ I ] - Avg;
        SumSq += D * D;
      }
      double Standard_Deviation = Math.Sqrt ( SumSq / Actual_Count );

      double Last = _Readings [ _Readings.Count - 1 ];

      // Trend
      string Trend = "—";
      if ( Actual_Count >= 10 )  // ← Use Actual_Count
      {
        int Recent_Start = Math.Max ( Start_Index, End_Index - 5 );
        int Prev_Start = Math.Max ( Start_Index, End_Index - 10 );

        double Recent = 0;
        for ( int I = Recent_Start; I < End_Index; I++ )
          Recent += _Readings [ I ];
        Recent /= ( End_Index - Recent_Start );

        double Previous = 0;
        int Prev_End = Math.Min ( Prev_Start + 5, End_Index );
        for ( int I = Prev_Start; I < Prev_End; I++ )
          Previous += _Readings [ I ];
        Previous /= ( Prev_End - Prev_Start );

        double Change = ( Recent - Previous ) / Previous * 100;

        if ( Math.Abs ( Change ) < 0.1 )
          Trend = "→";
        else
          Trend = Change > 0 ? "↑" : "↓";
      }

      // Sample rate and timing
      string Rate = "—";
      string Duration = "—";
      string Average_Delta = "—";

      if ( _Reading_Timestamps.Count >= 2 && Actual_Count >= 2 )  // ← Use Actual_Count
      {
        // Safety check for timestamp access
        int Last_Timestamp_Index = Math.Min ( End_Index - 1, _Reading_Timestamps.Count - 1 );
        int First_Timestamp_Index = Math.Min ( Start_Index, _Reading_Timestamps.Count - 1 );

        if ( Last_Timestamp_Index > First_Timestamp_Index )
        {
          TimeSpan Elapsed = _Reading_Timestamps [ Last_Timestamp_Index ] -
                            _Reading_Timestamps [ First_Timestamp_Index ];
          Duration = Multimeter_Common_Helpers_Class.Format_Time_Span ( Elapsed );

          double Samples_Per_Second = ( Actual_Count - 1 ) / Elapsed.TotalSeconds;  // ← Use Actual_Count
          Rate = $"{Samples_Per_Second:F2} S/s";

          double Total_Ms = 0;
          for ( int I = Start_Index + 1; I < End_Index && I < _Reading_Timestamps.Count; I++ )  // ← Fixed
          {
            Total_Ms += ( _Reading_Timestamps [ I ] - _Reading_Timestamps [ I - 1 ] ).TotalMilliseconds;
          }
          double Average_Interval = Total_Ms / ( Actual_Count - 1 );  // ← Use Actual_Count
          Average_Delta = $"{Average_Interval:F1}ms";
        }
      }

      int Y = 10;

      // Title
      var Title_Label = new Label
      {
        Text = "Statistics",
        Location = new Point ( 10, Y ),
        Font = new Font ( this.Font.FontFamily, 9f, FontStyle.Bold ),
        ForeColor = _Theme.Foreground,
        AutoSize = true
      };

      _Status_Panel.Controls.Add ( Title_Label );

      Y += 25;

      // Show if rolling/zoomed
      if ( _Enable_Rolling && _Readings.Count > _Max_Display_Points )
      {
        var Range_Label = new Label
        {
          Location = new Point ( 10, Y ),
          Size = new Size ( 180, 14 ),
          Text = $"Showing: {Actual_Count} / {_Readings.Count}",  // ← Use Actual_Count
          Font = new Font ( "Consolas", 7f, FontStyle.Italic ),
          ForeColor = Color.FromArgb ( 180, _Theme.Foreground ),
          AutoSize = false
        };
        _Status_Panel.Controls.Add ( Range_Label );

        Y += 16;
      }

      var Stats = new [ ]
      {
    ("Last        :", Format_Value(Last,               _Current_Unit, _Selected_Meter), Trend),
    ("Average     :", Format_Value(Avg,                _Current_Unit, _Selected_Meter), ""),
    ("Std Dev (σ) :", Format_Value(Standard_Deviation, _Current_Unit, _Selected_Meter), ""),
    ("Min         :", Format_Value(Min,                _Current_Unit, _Selected_Meter), ""),
    ("Max         :", Format_Value(Max,                _Current_Unit, _Selected_Meter), ""),
    ("Range       :", Format_Value(Range,              _Current_Unit, _Selected_Meter), ""),
    ("Count       :", Actual_Count.ToString(),                                          ""),
    ("Duration    :", Duration,                                                         ""),
    ("Rate        :", Rate,                                                             ""),
    ("Avg Δt      :", Average_Delta,                                                    "")
};

      foreach ( var (Label, Value, Extra) in Stats )
      {
        var Stat_Label = new Label
        {
          Location = new Point ( 10, Y ),
          Size = new Size ( 180, 16 ),
          Text = $"{Label,-12} {Value} {Extra}",  // ← capitals
          Font = new Font ( "Consolas", 7.5f ),
          ForeColor = _Theme.Foreground,
          AutoSize = false
        };
        _Status_Panel.Controls.Add ( Stat_Label );
        Y += 18;
      }
    }

    private void Stats_Toggle_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Status_Panel.Visible = !_Status_Panel.Visible;

      // Update button text (button1 is the stats button in designer)
      button1.Text = _Status_Panel.Visible ? "Hide Stats" : "Show Stats";

      if ( _Status_Panel.Visible )
      {
        Position_Status_Panel ( );
        _Status_Panel.BringToFront ( );
      }
    }


    private void Chart_Panel_MouseMove_Test ( object? sender, MouseEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );



      // Just show coordinates - if this works, events are wired correctly
      _Chart_Tooltip.Show ( $"Mouse: {e.X}, {e.Y}", Chart_Panel, e.X + 10, e.Y + 10 );
    }


    private void Chart_Panel_MouseLeave ( object? sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Hover_Point_Index != -1 )
      {
        _Hover_Point_Index = -1;
        _Chart_Tooltip.Hide ( Chart_Panel );
        Chart_Panel.Invalidate ( );
      }

      Restore_Normal_Performance_Mode ( );

    }




    private void Populate_Function_Combo ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      Function_Combo.Items.Clear ( );
      _Filtered_Indices.Clear ( );

      for ( int I = 0; I < _Functions.Length; I++ )
      {
        string Cmd = _Selected_Meter == Meter_Type.HP34401A
          ? _Functions [ I ].Cmd_34401A
          : _Functions [ I ].Cmd_3458A;

        if ( !string.IsNullOrEmpty ( Cmd ) )
        {
          _Filtered_Indices.Add ( I );
          Function_Combo.Items.Add ( _Functions [ I ].Label );
        }
      }

      if ( Function_Combo.Items.Count > 0 )
      {
        Function_Combo.SelectedIndex = 0;
      }
    }





    protected override async void OnFormClosing ( FormClosingEventArgs E )
    {
      if ( _Is_Running || _Is_Recording )
      {
        E.Cancel = true;  // Prevent immediate close
        await Graceful_Shutdown ( );
        // Now actually close
        this.Close ( );
        return;
      }
      base.OnFormClosing ( E );
    }

    private async Task Graceful_Shutdown ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Only do cleanup here if Close_Button didn't already handle it
      if ( _Is_Running || _Is_Recording )
      {
        Capture_Trace.Write ( "Cleanup needed (Close_Button was not used)" );
        _Chart_Refresh_Timer?.Stop ( );
        _Auto_Save_Timer?.Stop ( );
        Stop_Reading ( );
        Set_Local_Mode ( );
      }
      else
      {
        Capture_Trace.Write ( "Already cleaned up, skipping" );
      }


    }











    private void Start_Stop_Button_Click (
      object Sender, EventArgs E )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Is_Running )
      {
        Stop_Reading ( );
      }
      else
      {
        Start_Reading ( );
      }
    }

    private void Continuous_Check_Changed (
      object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Readings_Label.Enabled = !Continuous_Check.Checked;
      Readings_Numeric.Enabled =
        !Continuous_Check.Checked && !_Is_Running;
    }

    private void Theme_Button_Click (
    object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      using var Dlg = new Theme_Settings_Form ( _Theme );
      if ( Dlg.ShowDialog ( this ) == DialogResult.OK )
      {
        _Theme.Copy_From ( Dlg.Result );
        _Theme.Save ( );
        Chart_Panel.BackColor = _Theme.Background;

        // Update stats panel colors
        if ( _Status_Panel != null )
        {
          _Status_Panel.BackColor = _Theme.Background;
       //   Update_Status_Panel ( );  // Refresh to apply new foreground color
        }

        // Update stats button colors (assuming you named it button1)
        if ( button1 != null )
        {
          button1.BackColor = _Theme.Background;
          button1.ForeColor = _Theme.Foreground;
        }

        Chart_Panel.Invalidate ( );
      }
    }

    private void Save_Chart_Button_Click (
      object Sender, EventArgs E )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      using var Bmp = new Bitmap (
        Chart_Panel.ClientSize.Width,
        Chart_Panel.ClientSize.Height );
      Chart_Panel.DrawToBitmap ( Bmp,
        new Rectangle ( 0, 0,
          Bmp.Width, Bmp.Height ) );

      string Folder = Path.Combine (
        AppContext.BaseDirectory, "Graph_Captures" );
      Directory.CreateDirectory ( Folder );

      string Default_Name =
        $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}" +
        $"_{Function_Combo.Text.Replace ( " ", "_" )}.png";

      using var Dlg = new SaveFileDialog ( );
      Dlg.Filter = "PNG Image|*.png";
      Dlg.InitialDirectory = Folder;
      Dlg.FileName = Default_Name;

      if ( Dlg.ShowDialog ( this ) == DialogResult.OK )
      {
        Bmp.Save ( Dlg.FileName,
          System.Drawing.Imaging.ImageFormat.Png );
      }
    }
    private void Chart_Panel_Resize (
      object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Status_Panel != null && _Status_Panel.Visible )
      {
        Position_Status_Panel ( );
      }

      Chart_Panel.Invalidate ( );
    }

    private void Graph_Style_Combo_Changed (
      object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Chart_Panel.Invalidate ( );
    }

    private void Clear_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( _Is_Running )
      {
        return;
      }

      _Readings.Clear ( );
      _Reading_Timestamps.Clear ( ); // *** NEW: Clear timestamps too ***
      Current_Value_Label.Text = "---";
      Cycle_Count_Text_Box.Text = "";
      //Update_Status_Panel ( );
      Chart_Panel.Invalidate ( );
    }



    private void Stop_Reading ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      _Cts?.Cancel ( );
      _Chart_Refresh_Timer?.Stop ( );
      _Auto_Save_Timer?.Stop ( );


      // Flush any leftover bytes from an in-flight read
      try
      {
        if ( _Comm.Is_Connected )
        {
          Capture_Trace.Write ( "flushing serial buffer" );
          _Comm.Flush_Buffers ( );  // ← whatever your discard/flush method is
        }
      }
      catch { }


    }



    // ===== Recording / Loading =====

    private void Record_Button_Click (
      object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( _Is_Recording )
      {
        Stop_Recording ( );
      }
      else
      {
        Start_Recording ( );
      }
    }

    private void Start_Recording ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Combo_Index = Function_Combo.SelectedIndex;
      if ( Combo_Index < 0 || Combo_Index >= _Filtered_Indices.Count )
        return;

      int Func_Index = _Filtered_Indices [ Combo_Index ];
      _Record_Function = _Functions [ Func_Index ].Label;
      _Record_Unit = _Functions [ Func_Index ].Unit;
      _Recorded_Points.Clear ( );

      _Is_Recording = true;
      Multimeter_Common_Helpers_Class.Start_Recording_UI ( Record_Button );  // ← replaces 3 lines
    }

    private void Stop_Recording ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Multimeter_Common_Helpers_Class.Stop_Recording (
        ref _Is_Recording,
        Record_Button,
        ( ) => _Recorded_Points.Count,
        Save_Recorded_Data );
    }

    // Single form:
    private void Save_Recorded_Data ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Recorded_Points.Count == 0 )
        return;

      string File_Path = Path.Combine (
        Multimeter_Common_Helpers_Class.Get_Graph_Captures_Folder ( ),
        $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{_Record_Function}.csv" );

      Multimeter_Common_Helpers_Class.Save_Single_Series_CSV (
        File_Path, _Record_Function, _Record_Unit,
        _Recorded_Points, _Selected_Meter );

      var (Min, Max, Avg, _, Range, Duration, _, _)
        = Multimeter_Common_Helpers_Class.Calculate_Stats ( _Recorded_Points );

      MessageBox.Show (
        $"Saved {_Recorded_Points.Count} points to:\n{File_Path}\n\n" +
        $"Average: {Multimeter_Common_Helpers_Class.Format_Value ( Avg, _Record_Unit, _Selected_Meter )}\n" +
        $"Range: {Multimeter_Common_Helpers_Class.Format_Value ( Range, _Record_Unit, _Selected_Meter )}\n" +
        $"Duration: {Multimeter_Common_Helpers_Class.Format_Time_Span ( Duration )}",
        "Recording Saved", MessageBoxButtons.OK, MessageBoxIcon.Information );
    }


 


    private void Load_Recorded_File ( string File_Path )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !Multimeter_Common_Helpers_Class.Load_CSV_Preamble (
          File_Path, out var Lines, out int Header_Index,
          out var Flat_Stats, out _ ) )
        return;

      string Unit = Flat_Stats.ContainsKey ( "Unit" ) ? Flat_Stats [ "Unit" ] : "V";

      var (Values, Timestamps) = Multimeter_Common_Helpers_Class.Parse_Single_Column_Data (
        Lines, Header_Index );

      _Readings.Clear ( );
      _Readings.AddRange ( Values );
      _Reading_Timestamps.Clear ( );

      if ( Timestamps.Count == Values.Count )
        _Reading_Timestamps.AddRange ( Timestamps );
      else
      {
        DateTime Base_Time = DateTime.Now;
        for ( int I = 0; I < Values.Count; I++ )
          _Reading_Timestamps.Add ( Base_Time.AddSeconds ( I ) );
      }

      Rolling_Check.Checked = false;
      _Current_Unit = Unit;

      if ( Values.Count > 0 )
      {
        Current_Value_Label.Text = Multimeter_Common_Helpers_Class.Format_Value (
          Values [ Values.Count - 1 ], Unit, _Selected_Meter );
        Cycle_Count_Text_Box.Text = $"Loaded {Values.Count} points from file";
      }
      else
      {
        Current_Value_Label.Text = "No data";
        Cycle_Count_Text_Box.Text = "No data points loaded";
      }

      if ( Flat_Stats.Count > 0 )
        Update_Status_Panel_With_File_Stats ( Flat_Stats );
      else
        Update_Status_Panel ( );

      Multimeter_Common_Helpers_Class.Update_Scrollbar_Range (
        Pan_Scrollbar, _Readings.Count,
        _Max_Display_Points, _Auto_Scroll, ref _View_Offset );

      Chart_Panel.Invalidate ( );
    }


    private void Update_Status_Panel_With_File_Stats ( Dictionary<string, string> File_Stats )
    {
      _Status_Panel.Controls.Clear ( );
      _Status_Panel.BackColor = _Theme.Background;

      int y = 10;

      var Title_Label = new Label
      {
        Text = "Statistics (from file)",
        Location = new Point ( 10, y ),
        Font = new Font ( this.Font.FontFamily, 9f, FontStyle.Bold ),
        ForeColor = _Theme.Foreground,
        AutoSize = true
      };
      _Status_Panel.Controls.Add ( Title_Label );
      y += 25;

      // Display saved stats
      foreach ( var kvp in File_Stats )
      {
        var Stat_Label = new Label
        {
          Location = new Point ( 10, y ),
          Size = new Size ( 180, 16 ),
          Text = $"{kvp.Key,-12}: {kvp.Value}",
          Font = new Font ( "Consolas", 10f ),
          ForeColor = _Theme.Foreground,
          AutoSize = false
        };
        _Status_Panel.Controls.Add ( Stat_Label );
        y += 18;
      }
    }


    private void Load_Button_Click (
      object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Is_Running )
      {
        MessageBox.Show (
          "Stop the current reading before loading.",
          "Reading in Progress",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      string Folder = Multimeter_Common_Helpers_Class.Get_Graph_Captures_Folder ( );

      using var Dlg = new OpenFileDialog ( );
      Dlg.Title = "Load Recorded Data";
      Dlg.Filter = "CSV files (*.csv)|*.csv";
      if ( Directory.Exists ( Folder ) )
      {
        Dlg.InitialDirectory = Folder;
      }

      if ( Dlg.ShowDialog ( ) != DialogResult.OK )
      {
        return;
      }

      Load_Recorded_File ( Dlg.FileName );
    }

  





    private void Draw_Line_Chart ( Graphics G,
      PointF [ ] Points, int Count, Pen Line_Pen,
      Brush Dot_Brush, int Margin_Top, float Baseline )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Gradient fill under the line
      if ( Count >= 2 )
      {
        PointF [ ] Fill_Points =
          new PointF [ Count + 2 ];
        Array.Copy ( Points, Fill_Points, Count );
        Fill_Points [ Count ] = new PointF (
          Points [ Count - 1 ].X, Baseline );
        Fill_Points [ Count + 1 ] = new PointF (
          Points [ 0 ].X, Baseline );

        using var Fill_Brush =
          new System.Drawing.Drawing2D
            .LinearGradientBrush (
              new PointF ( 0, Margin_Top ),
              new PointF ( 0, Baseline ),
              Color.FromArgb ( 60,
                Line_Pen.Color ),
              Color.FromArgb ( 5,
                Line_Pen.Color ) );
        G.FillPolygon ( Fill_Brush, Fill_Points );
      }

      // Draw the line
      if ( Count >= 2 )
      {
        G.DrawLines ( Line_Pen, Points );
      }

      // Draw data point dots
      float Dot_Size = Count > 100 ? 3f :
        Count > 50 ? 4f : 5f;
      foreach ( PointF P in Points )
      {
        G.FillEllipse ( Dot_Brush,
          P.X - Dot_Size / 2, P.Y - Dot_Size / 2,
          Dot_Size, Dot_Size );
      }
    }

    private void Draw_Bar_Chart ( Graphics G,
      PointF [ ] Points, int Count, int Margin_Left,
      int Chart_W, float Baseline )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      float Bar_Width = Math.Max ( 2f,
        ( Chart_W / (float) Count ) * 0.7f );
      float Half_Bar = Bar_Width / 2;

      Color Bar_Color = _Theme.Line_Colors [ 0 ];
      using var Bar_Brush =
        new SolidBrush ( Bar_Color );
      using var Bar_Border_Pen =
        new Pen ( Color.FromArgb (
          (int) ( Bar_Color.R * 0.7 ),
          (int) ( Bar_Color.G * 0.7 ),
          (int) ( Bar_Color.B * 0.7 ) ), 1f );

      foreach ( PointF P in Points )
      {
        float Bar_H = Baseline - P.Y;
        if ( Bar_H < 1 )
        {
          Bar_H = 1;
        }
        RectangleF Bar_Rect = new RectangleF (
          P.X - Half_Bar, P.Y,
          Bar_Width, Bar_H );
        G.FillRectangle ( Bar_Brush, Bar_Rect );
        G.DrawRectangle ( Bar_Border_Pen,
          Bar_Rect.X, Bar_Rect.Y,
          Bar_Rect.Width, Bar_Rect.Height );
      }
    }

    private void Draw_Scatter_Chart ( Graphics G,
      PointF [ ] Points, int Count, Brush Dot_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      float Dot_Size = Count > 100 ? 5f :
        Count > 50 ? 7f : 9f;
      using var Ring_Pen =
        new Pen ( _Theme.Line_Colors [ 0 ], 1.5f );

      foreach ( PointF P in Points )
      {
        G.FillEllipse ( Dot_Brush,
          P.X - Dot_Size / 2, P.Y - Dot_Size / 2,
          Dot_Size, Dot_Size );
        G.DrawEllipse ( Ring_Pen,
          P.X - Dot_Size / 2 - 1,
          P.Y - Dot_Size / 2 - 1,
          Dot_Size + 2, Dot_Size + 2 );
      }
    }

    private void Draw_Step_Chart ( Graphics G,
      PointF [ ] Points, int Count, Pen Line_Pen,
      int Margin_Top, float Baseline )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Count < 2 )
      {
        return;
      }

      // Build step path: horizontal then vertical
      var Step_Points = new List<PointF> ( );
      Step_Points.Add ( Points [ 0 ] );
      for ( int I = 1; I < Count; I++ )
      {
        // Horizontal to the next X at previous Y
        Step_Points.Add (
          new PointF ( Points [ I ].X,
            Points [ I - 1 ].Y ) );
        // Vertical to the next Y
        Step_Points.Add ( Points [ I ] );
      }

      PointF [ ] Step_Array = Step_Points.ToArray ( );

      // Gradient fill under the step line
      PointF [ ] Fill_Points =
        new PointF [ Step_Array.Length + 2 ];
      Array.Copy ( Step_Array, Fill_Points,
        Step_Array.Length );
      Fill_Points [ Step_Array.Length ] = new PointF (
        Step_Array [ Step_Array.Length - 1 ].X,
        Baseline );
      Fill_Points [ Step_Array.Length + 1 ] = new PointF (
        Step_Array [ 0 ].X, Baseline );

      using var Fill_Brush =
        new System.Drawing.Drawing2D
          .LinearGradientBrush (
            new PointF ( 0, Margin_Top ),
            new PointF ( 0, Baseline ),
            Color.FromArgb ( 60,
              Line_Pen.Color ),
            Color.FromArgb ( 5,
              Line_Pen.Color ) );
      G.FillPolygon ( Fill_Brush, Fill_Points );

      // Draw the step line
      G.DrawLines ( Line_Pen, Step_Array );
    }

    private int Get_Bin_Count ( int N )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Sturges' rule, clamped to 5-30
      int Bins = (int) Math.Ceiling (
        1.0 + 3.322 * Math.Log10 ( N ) );
      return Math.Clamp ( Bins, 5, 30 );
    }

    private Color Get_Bin_Color ( int Index )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Color [ ] Base = _Theme.Line_Colors;
      int Cycle = Index / Base.Length;
      Color C = Base [ Index % Base.Length ];

      if ( Cycle == 0 )
        return C;

      // Lighten for additional cycles
      int Shift = Cycle * 40;
      return Color.FromArgb (
        Math.Min ( 255, C.R + Shift ),
        Math.Min ( 255, C.G + Shift ),
        Math.Min ( 255, C.B + Shift ) );
    }

    private void Draw_Histogram ( Graphics G,
      int W, int H, int Margin_Left, int Margin_Right,
      int Margin_Top, int Margin_Bottom,
      int Chart_W, int Chart_H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Count = _Readings.Count;
      double Min_V = _Readings.Min ( );
      double Max_V = _Readings.Max ( );
      double Range = Max_V - Min_V;

      int Num_Bins = Get_Bin_Count ( Count );

      // Handle all-same-value case
      if ( Range < 1e-12 )
      {
        Range = Math.Abs ( Max_V ) * 0.1;
        if ( Range < 1e-12 )
          Range = 1.0;
        Min_V -= Range / 2;
        Max_V += Range / 2;
        Range = Max_V - Min_V;
      }

      double Bin_Width = Range / Num_Bins;

      // Count readings per bin
      int [ ] Bin_Counts = new int [ Num_Bins ];
      foreach ( double V in _Readings )
      {
        int Bin = (int) ( ( V - Min_V ) / Bin_Width );
        if ( Bin >= Num_Bins )
          Bin = Num_Bins - 1;
        if ( Bin < 0 )
          Bin = 0;
        Bin_Counts [ Bin ]++;
      }

      int Max_Count = Bin_Counts.Max ( );
      if ( Max_Count == 0 )
        Max_Count = 1;

      // Pad Y range slightly
      double Y_Max = Max_Count * 1.1;

      // Draw grid and Y-axis labels (frequency)
      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Label_Font = new Font ( "Consolas", 7.5F );
      using var Label_Brush =
        new SolidBrush ( _Theme.Labels );

      int Num_Grid_Lines = 5;
      for ( int I = 0; I <= Num_Grid_Lines; I++ )
      {
        double Fraction = (double) I / Num_Grid_Lines;
        int Y_Pos = H - Margin_Bottom -
          (int) ( Fraction * Chart_H );

        G.DrawLine ( Grid_Pen,
          Margin_Left, Y_Pos,
          W - Margin_Right, Y_Pos );

        int Label_Val = (int) Math.Round (
          Fraction * Y_Max );
        string Label_Text = Label_Val.ToString ( );
        var Label_Size = G.MeasureString (
          Label_Text, Label_Font );
        G.DrawString ( Label_Text, Label_Font,
          Label_Brush,
          Margin_Left - Label_Size.Width - 6,
          Y_Pos - Label_Size.Height / 2 );
      }

      // Draw bars
      float Bar_Spacing = Chart_W / (float) Num_Bins;
      float Bar_W = Bar_Spacing * 0.8f;
      float Gap = Bar_Spacing * 0.1f;

      Color Bar_Color = _Theme.Line_Colors [ 0 ];
      using var Bar_Brush = new SolidBrush ( Bar_Color );
      using var Bar_Border_Pen = new Pen (
        Color.FromArgb (
          (int) ( Bar_Color.R * 0.7 ),
          (int) ( Bar_Color.G * 0.7 ),
          (int) ( Bar_Color.B * 0.7 ) ), 1f );

      float Baseline = H - Margin_Bottom;

      for ( int I = 0; I < Num_Bins; I++ )
      {
        float X = Margin_Left + I * Bar_Spacing + Gap;
        float Bar_H = (float) (
          ( Bin_Counts [ I ] / Y_Max ) * Chart_H );
        if ( Bar_H < 1 && Bin_Counts [ I ] > 0 )
          Bar_H = 1;

        RectangleF Rect = new RectangleF (
          X, Baseline - Bar_H, Bar_W, Bar_H );
        G.FillRectangle ( Bar_Brush, Rect );
        G.DrawRectangle ( Bar_Border_Pen,
          Rect.X, Rect.Y, Rect.Width, Rect.Height );

        // Frequency label above bar
        if ( Bin_Counts [ I ] > 0 )
        {
          string Freq_Text = Bin_Counts [ I ].ToString ( );
          var Freq_Size = G.MeasureString (
            Freq_Text, Label_Font );
          G.DrawString ( Freq_Text, Label_Font,
            Label_Brush,
            X + Bar_W / 2 - Freq_Size.Width / 2,
            Baseline - Bar_H - Freq_Size.Height - 2 );
        }
      }

      // Normal distribution curve overlay
      double Mean = _Readings.Average ( );
      double Sum_Sq = 0;
      foreach ( double V in _Readings )
      {
        double D = V - Mean;
        Sum_Sq += D * D;
      }
      double Std_Dev = Math.Sqrt ( Sum_Sq / Count );

      if ( Std_Dev > 1e-15 )
      {
        Color Curve_Color = _Theme.Line_Colors [ 1 ];
        using var Curve_Pen = new Pen (
          Curve_Color, 2.5f );

        int Num_Pts = 100;
        PointF [ ] Curve_Pts = new PointF [ Num_Pts ];
        double Scale = Bin_Width * Count;

        for ( int I = 0; I < Num_Pts; I++ )
        {
          double X_Val = Min_V +
            ( I / (double) ( Num_Pts - 1 ) ) * Range;
          double Z = ( X_Val - Mean ) / Std_Dev;
          double PDF = ( 1.0 / ( Std_Dev *
            Math.Sqrt ( 2.0 * Math.PI ) ) ) *
            Math.Exp ( -0.5 * Z * Z );
          double Freq = PDF * Scale;

          float Px = Margin_Left +
            (float) ( ( X_Val - Min_V ) / Range *
              Chart_W );
          float Py = Baseline -
            (float) ( ( Freq / Y_Max ) * Chart_H );

          Curve_Pts [ I ] = new PointF ( Px, Py );
        }

        G.DrawLines ( Curve_Pen, Curve_Pts );

        // Mean line (dashed)
        Color Mean_Color = _Theme.Line_Colors [ 2 ];
        using var Mean_Pen = new Pen (
          Mean_Color, 2f );
        Mean_Pen.DashStyle =
          System.Drawing.Drawing2D.DashStyle.Dash;

        float Mean_X = Margin_Left +
          (float) ( ( Mean - Min_V ) / Range *
            Chart_W );
        G.DrawLine ( Mean_Pen, Mean_X, Margin_Top,
          Mean_X, Baseline );

        // Sigma lines
        using var Sigma_Pen = new Pen (
          Color.FromArgb ( 120, Mean_Color ), 1.5f );
        Sigma_Pen.DashStyle =
          System.Drawing.Drawing2D.DashStyle.Dot;

        using var Sigma_Font =
          new Font ( "Consolas", 7F );
        using var Sigma_Brush =
          new SolidBrush ( Mean_Color );

        // Label for mean
        G.DrawString ( "\u03bc", Sigma_Font,
          Sigma_Brush,
          Mean_X + 3, Margin_Top + 2 );

        double [ ] Sigmas = { -2, -1, 1, 2 };
        string [ ] Sigma_Labels =
          { "-2\u03c3", "-1\u03c3",
            "+1\u03c3", "+2\u03c3" };

        for ( int I = 0; I < Sigmas.Length; I++ )
        {
          double Sv = Mean + Sigmas [ I ] * Std_Dev;
          if ( Sv < Min_V || Sv > Max_V )
            continue;

          float Sx = Margin_Left +
            (float) ( ( Sv - Min_V ) / Range *
              Chart_W );
          G.DrawLine ( Sigma_Pen, Sx, Margin_Top,
            Sx, Baseline );
          G.DrawString ( Sigma_Labels [ I ],
            Sigma_Font, Sigma_Brush,
            Sx + 3, Margin_Top + 2 );
        }
      }

      // X-axis labels (bin center values)
      using var X_Font = new Font ( "Consolas", 6.5F );
      int Label_Step = Math.Max ( 1,
        Num_Bins / 8 );

      for ( int I = 0; I < Num_Bins; I += Label_Step )
      {
        double Bin_Center = Min_V +
          ( I + 0.5 ) * Bin_Width;
        string X_Label = Format_Value (
          Bin_Center, _Current_Unit, _Selected_Meter );
        var X_Size = G.MeasureString (
          X_Label, X_Font );
        float X_Pos = Margin_Left +
          I * Bar_Spacing + Bar_Spacing / 2;
        G.DrawString ( X_Label, X_Font, Label_Brush,
          X_Pos - X_Size.Width / 2,
          H - Margin_Bottom + 4 );
      }

      // Title label
      using var Title_Font = new Font ( "Segoe UI", 7.5F );
      string Title = $"Distribution  ({Count} readings)";
      var Title_Size = G.MeasureString (
        Title, Title_Font );
      G.DrawString ( Title, Title_Font, Label_Brush,
        Margin_Left + Chart_W / 2 -
          Title_Size.Width / 2,
        H - Margin_Bottom + 18 );
    }

    private void Draw_Pie_Chart ( Graphics G,
      int W, int H, int Margin_Left, int Margin_Right,
      int Margin_Top, int Margin_Bottom,
      int Chart_W, int Chart_H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Count = _Readings.Count;
      double Min_V = _Readings.Min ( );
      double Max_V = _Readings.Max ( );
      double Range = Max_V - Min_V;

      int Num_Bins = Get_Bin_Count ( Count );

      // Handle all-same-value case
      if ( Range < 1e-12 )
      {
        Range = Math.Abs ( Max_V ) * 0.1;
        if ( Range < 1e-12 )
          Range = 1.0;
        Min_V -= Range / 2;
        Max_V += Range / 2;
        Range = Max_V - Min_V;
      }

      double Bin_Width = Range / Num_Bins;

      // Count readings per bin
      int [ ] Bin_Counts = new int [ Num_Bins ];
      foreach ( double V in _Readings )
      {
        int Bin = (int) ( ( V - Min_V ) / Bin_Width );
        if ( Bin >= Num_Bins )
          Bin = Num_Bins - 1;
        if ( Bin < 0 )
          Bin = 0;
        Bin_Counts [ Bin ]++;
      }

      // Layout: pie on the left, legend on the right
      int Legend_W = 200;
      int Pie_Area_W = Chart_W - Legend_W;
      if ( Pie_Area_W < 100 )
      {
        Pie_Area_W = Chart_W;
        Legend_W = 0;
      }

      int Diameter = Math.Min ( Pie_Area_W, Chart_H )
        - 20;
      if ( Diameter < 40 )
        Diameter = 40;

      int Pie_X = Margin_Left +
        ( Pie_Area_W - Diameter ) / 2;
      int Pie_Y = Margin_Top +
        ( Chart_H - Diameter ) / 2;

      Rectangle Pie_Rect = new Rectangle (
        Pie_X, Pie_Y, Diameter, Diameter );

      // Draw slices
      float Start_Angle = -90f;
      using var Outline_Pen = new Pen (
        _Theme.Background, 2f );

      for ( int I = 0; I < Num_Bins; I++ )
      {
        if ( Bin_Counts [ I ] == 0 )
          continue;

        float Sweep = 360f * Bin_Counts [ I ] / Count;
        Color Slice_Color = Get_Bin_Color ( I );
        using var Slice_Brush =
          new SolidBrush ( Slice_Color );

        G.FillPie ( Slice_Brush, Pie_Rect,
          Start_Angle, Sweep );
        G.DrawPie ( Outline_Pen, Pie_Rect,
          Start_Angle, Sweep );

        Start_Angle += Sweep;
      }

      // Draw legend
      if ( Legend_W > 0 )
      {
        using var Legend_Font =
          new Font ( "Consolas", 7.5F );
        using var Label_Brush =
          new SolidBrush ( _Theme.Labels );

        int Leg_X = Margin_Left + Pie_Area_W + 10;
        int Leg_Y = Margin_Top + 10;
        int Row_H = 20;

        // Title
        using var Title_Font =
          new Font ( "Segoe UI", 8F, FontStyle.Bold );
        G.DrawString ( "Distribution", Title_Font,
          Label_Brush, Leg_X, Leg_Y );
        Leg_Y += Row_H + 4;

        for ( int I = 0; I < Num_Bins; I++ )
        {
          if ( Bin_Counts [ I ] == 0 )
            continue;

          if ( Leg_Y + Row_H > H - Margin_Bottom )
            break;

          Color Swatch_Color = Get_Bin_Color ( I );
          using var Swatch_Brush =
            new SolidBrush ( Swatch_Color );

          G.FillRectangle ( Swatch_Brush,
            Leg_X, Leg_Y + 2, 12, 12 );

          double Bin_Low = Min_V + I * Bin_Width;
          double Bin_High = Bin_Low + Bin_Width;
          double Pct = 100.0 * Bin_Counts [ I ] / Count;

          string Entry_Text =
            $"{Format_Value ( Bin_Low, _Current_Unit, _Selected_Meter )}" +
            $" - {Format_Value ( Bin_High, _Current_Unit, _Selected_Meter )}" +
            $"  ({Pct:F1}%)";

          G.DrawString ( Entry_Text, Legend_Font,
            Label_Brush, Leg_X + 18, Leg_Y );

          Leg_Y += Row_H;
        }
      }
    }

    private void Draw_Stats_Overlay ( Graphics G,
      int W, int Margin_Top, int Margin_Right,
      bool Bottom = false, int H = 0,
      int Margin_Bottom = 0 )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Readings.Count == 0 )
        return;

      int Count = _Readings.Count;
      double Min_V = _Readings.Min ( );
      double Max_V = _Readings.Max ( );
      double Mean = _Readings.Average ( );
      double Sum_Sq = 0;
      foreach ( double V in _Readings )
      {
        double D = V - Mean;
        Sum_Sq += D * D;
      }
      double Std_Dev = Math.Sqrt ( Sum_Sq / Count );

      string [ ] Lines =
      {
        $"Mean:    {Format_Value ( Mean, _Current_Unit, _Selected_Meter  )}",
        $"Std Dev: {Format_Value ( Std_Dev, _Current_Unit , _Selected_Meter)}",
        $"Min:     {Format_Value ( Min_V, _Current_Unit , _Selected_Meter)}",
        $"Max:     {Format_Value ( Max_V, _Current_Unit , _Selected_Meter)}",
        $"Count:   {Count}"
      };

      using var Stats_Font = new Font ( "Consolas", 7.5F );
      using var Text_Brush =
        new SolidBrush ( _Theme.Labels );

      // Measure box size
      float Line_H = G.MeasureString ( "X",
        Stats_Font ).Height + 2;
      float Box_W = 0;
      foreach ( string L in Lines )
      {
        float Lw = G.MeasureString ( L,
          Stats_Font ).Width;
        if ( Lw > Box_W )
          Box_W = Lw;
      }

      float Padding = 8;
      float Box_H = Lines.Length * Line_H + Padding * 2;
      Box_W += Padding * 2;

      float Box_X = W - Margin_Right - Box_W - 5;
      float Box_Y = Bottom
        ? H - Margin_Bottom - Box_H - 5
        : Margin_Top + 5;

      // Semi-transparent background
      using var Bg_Brush = new SolidBrush (
        Color.FromArgb ( 180, _Theme.Background ) );
      using var Border_Pen = new Pen (
        _Theme.Grid, 1f );

      G.FillRectangle ( Bg_Brush,
        Box_X, Box_Y, Box_W, Box_H );
      G.DrawRectangle ( Border_Pen,
        Box_X, Box_Y, Box_W, Box_H );

      // Draw text lines
      float Y = Box_Y + Padding;
      foreach ( string L in Lines )
      {
        G.DrawString ( L, Stats_Font, Text_Brush,
          Box_X + Padding, Y );
        Y += Line_H;
      }
    }



    private void Max_Points_Numeric_ValueChanged ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Max_Display_Points = (int) Max_Points_Numeric.Value;
      Capture_Trace.Write ( $"Max display points changed to: {_Max_Display_Points}" );

      // If rolling is enabled and we're actively polling, trim the data
      if ( _Enable_Rolling && _Is_Running )
      {
        if ( _Readings.Count > _Max_Display_Points )
        {
          int Points_To_Remove = _Readings.Count - _Max_Display_Points;
          _Readings.RemoveRange ( 0, Points_To_Remove );
          _Reading_Timestamps.RemoveRange ( 0, Points_To_Remove );

          Capture_Trace.Write ( $"Trimmed {Points_To_Remove} points to match new max" );
        }
      }

      Chart_Panel.Invalidate ( );
    }





    // ========================================
    // NEW: ROLLING GRAPH EVENT HANDLERS
    // ========================================

    private void Rolling_Check_CheckedChanged ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Enable_Rolling = Rolling_Check.Checked;
      Max_Points_Numeric.Enabled = Rolling_Check.Checked;

      if ( _Enable_Rolling )
      {
        //     Capture_Trace.Write ( $"Rolling/Zoom mode enabled: {_Max_Display_Points} points" );

        // DON'T trim data when not actively polling
        // Just change the view window
        if ( !_Is_Running )
        {
          Capture_Trace.Write ( "Zoom mode for loaded data - view window adjusted" );
        }
        else
        {
          // Only trim when actively polling (to save memory)
          if ( _Readings.Count > _Max_Display_Points )
          {
            int Points_To_Remove = _Readings.Count - _Max_Display_Points;
            _Readings.RemoveRange ( 0, Points_To_Remove );
            _Reading_Timestamps.RemoveRange ( 0, Points_To_Remove );

            Capture_Trace.Write ( $"Trimmed {Points_To_Remove} old points during active polling" );
          }
        }
      }
      else
      {
        Capture_Trace.Write ( "Rolling/Zoom mode disabled - showing all data" );
      }

      Chart_Panel.Invalidate ( );
    }


   




    // ========================================
    // MODIFIED: START_READING (with rolling)
    // ========================================

    private async void Start_Reading ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Comm.Is_Connected )
      {
        MessageBox.Show ( "Not connected. Please connect first.",
            "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning );
        return;
      }

      int Combo_Index = Function_Combo.SelectedIndex;
      if ( Combo_Index < 0 || Combo_Index >= _Filtered_Indices.Count )
        return;

      int Func_Index = _Filtered_Indices [ Combo_Index ];
      var Selected = _Functions [ Func_Index ];

      string Configure_Cmd = _Selected_Meter == Meter_Type.HP34401A
          ? Selected.Cmd_34401A : Selected.Cmd_3458A;

      string Unit = Selected.Unit;

      bool Is_Query_Mode = Configure_Cmd.EndsWith ( "?" );

      Capture_Trace.Write ( $"Configure_Cmd = {Configure_Cmd}" );
      Capture_Trace.Write ( $"Is_Query_Mode = {Is_Query_Mode}" );


      _Current_Unit = Unit;

      _Is_Running = true;
      _Cts = new CancellationTokenSource ( );
      Start_Stop_Button.Text = "Stop";
      Readings_Numeric.Enabled = false;
      Delay_Numeric.Enabled = false;
      Function_Combo.Enabled = false;
      Clear_Button.Enabled = false;
      Continuous_Check.Enabled = true;
      Rolling_Check.Enabled = true;
      Max_Points_Numeric.Enabled = true;

      // Start timers
      _Chart_Refresh_Timer?.Start ( );
      if ( _Settings.Enable_Auto_Save )
        _Auto_Save_Timer?.Start ( );

      bool Continuous = Continuous_Check.Checked;
      int Total_Readings = (int) Readings_Numeric.Value;
      int Delay_Ms = (int) Delay_Numeric.Value;

      CancellationToken Token = _Cts.Token;
      Capture_Trace.Write ( "Using " + Function_Combo.SelectedItem );

      try
      {
        if ( !Is_Query_Mode )
        {
          await Task.Run ( ( ) => _Comm.Send_Instrument_Command ( Configure_Cmd ), Token );

          if ( _Selected_Meter == Meter_Type.HP3458A )
          {
            string NPLC_Value = _Settings.Default_NPLC;
            await Task.Run ( ( ) => _Comm.Send_Instrument_Command (
                $"NPLC {NPLC_Value}" ), Token );
            await Task.Delay ( 2000, Token );

            // Flush any stale responses from configure commands before reading
            await Task.Run ( ( ) => _Comm.Flush_Buffers ( ), Token );
            await Task.Delay ( 200, Token );
          }
          else if ( _Selected_Meter == Meter_Type.HP34401A )
          {
            string Measurement_Label = Function_Combo.Text.Trim ( );
            string NPLC_Value = _Settings.Default_NPLC;
            string? NPLC_Cmd = Multimeter_Common_Helpers_Class.Build_NPLC_Command ( Measurement_Label, NPLC_Value );

            if ( NPLC_Cmd != null )
            {
              Capture_Trace.Write ( $"Sending NPLC command: {NPLC_Cmd} to HP34401A" );
              await Task.Run ( ( ) => _Comm.Send_Instrument_Command ( NPLC_Cmd ), Token );
              await Task.Delay ( 200, Token );
            }
            else
            {
              Capture_Trace.Write ( $"NPLC not applicable for {Measurement_Label} on HP34401A, skipping" );
            }

            // Calculate how long one measurement takes at this NPLC setting
            // and set the read timeout to comfortably exceed it
            if ( double.TryParse ( NPLC_Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double NPLC_Double ) )
            {
              int Measurement_Ms = (int) ( ( NPLC_Double / 60.0 ) * 1000 ) + 500; // 500ms headroom
              int Required_Timeout = Math.Max ( _Settings.Default_GPIB_Timeout_Ms, Measurement_Ms );
              _Comm.Read_Timeout_Ms = Required_Timeout;
              Capture_Trace.Write ( $"Set read timeout to {Required_Timeout}ms for NPLC {NPLC_Value}" );
            }

            await Task.Delay ( 300, Token );
          }
        }



        int I = 0;
        while ( Continuous || I < Total_Readings )
        {
          Token.ThrowIfCancellationRequested ( );

          Capture_Trace.Write ( $"Loop Count = {I}" );

          DateTime Reading_Time = DateTime.Now;

          string Response;
          if ( Is_Query_Mode )
          {
            Response = await Task.Run ( ( ) =>
                _Comm.Query_Instrument ( Configure_Cmd ), Token );
          }
          else if ( _Selected_Meter == Meter_Type.HP34401A )
          {
            Response = await Task.Run ( ( ) =>
                _Comm.Query_Instrument ( "READ?" ), Token );
          }
          else
          {
            double NPLC = double.Parse ( _Settings.Default_NPLC, CultureInfo.InvariantCulture );
            int Required_Wait_Ms = (int) ( ( NPLC / 60.0 ) * 1000 ) + 200;
            await Task.Delay ( Required_Wait_Ms, Token );

            // With ++auto 1, sending ++read eoi addresses instrument to talk
            await Task.Run ( ( ) => _Comm.Raw_Write_Prologix ( "++read eoi" ), Token );
            Response = await Task.Run ( ( ) =>
                _Comm.Read_Instrument ( Token ), Token ) ?? "";
          }


          Capture_Trace.Write ( $"Response =  {Response}" );


          Token.ThrowIfCancellationRequested ( );
          if ( double.TryParse ( Response,
              System.Globalization.NumberStyles.Float,
              System.Globalization.CultureInfo.InvariantCulture,
              out double Value ) )
          {
            _Readings.Add ( Value );
            _Reading_Timestamps.Add ( Reading_Time );
            if ( _Enable_Rolling && _Readings.Count > _Max_Display_Points )
            {
              _Readings.RemoveAt ( 0 );
              _Reading_Timestamps.RemoveAt ( 0 );
            }
            if ( _Is_Recording )
              _Recorded_Points.Add ( (Reading_Time, Value) );
            Current_Value_Label.Text = Format_Value ( Value, Unit, _Selected_Meter );
            string Rate_Info = "";
            if ( _Reading_Timestamps.Count >= 2 )
            {
              TimeSpan Last_Interval =
                  _Reading_Timestamps [ _Reading_Timestamps.Count - 1 ] -
                  _Reading_Timestamps [ _Reading_Timestamps.Count - 2 ];
              double Samples_Per_Sec = 1000.0 / Last_Interval.TotalMilliseconds;
              Rate_Info = $"  [{Samples_Per_Sec:F2} S/s]";
            }
            if ( Continuous )
            {

              Cycle_Count_Text_Box.Text =
                  $"Reading {_Readings.Count}  (Continuous){Rate_Info}";

              Capture_Trace.Write ( $"Reading {_Readings.Count}  (Continuous){Rate_Info}" );
            }
            else
            {
              Cycle_Count_Text_Box.Text =
                  $"Reading {I + 1} of {Total_Readings}  " +
                  $"(Total: {_Readings.Count}){Rate_Info}";

              Capture_Trace.Write ( $"Reading {I + 1} of {Total_Readings}  " );
              Capture_Trace.Write ( $"(Total: {_Readings.Count}){Rate_Info}" );
            }

            Update_Status_Panel ( );
            Request_Chart_Refresh ( );
          }
          else if ( !string.IsNullOrWhiteSpace ( Response ) )  // ← add from here
          {
            Capture_Trace.Write ( $"Non-numeric response: [{Response}]" );

          }

          I++;
          if ( ( Continuous || I < Total_Readings ) && Delay_Ms > 0 )
            await Task.Delay ( Delay_Ms, Token );
        }
      }
      catch ( OperationCanceledException )
      {
        // Stopped by user
      }
      finally
      {
        Finish_Reading ( );
      }
    }

    private void Finish_Reading ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Is_Running = false;
      _Cts?.Dispose ( );
      _Cts = null;
      Start_Stop_Button.Text = "Start";
      Readings_Numeric.Enabled = !Continuous_Check.Checked;
      Delay_Numeric.Enabled = true;
      Function_Combo.Enabled = true;
      Clear_Button.Enabled = true;
      Continuous_Check.Enabled = true;
      Rolling_Check.Enabled = true;
      Max_Points_Numeric.Enabled = _Enable_Rolling;

      _Chart_Refresh_Timer?.Stop ( );
      _Auto_Save_Timer?.Stop ( );


      // Flush any leftover bytes from an in-flight read
      try
      {
        if ( _Comm.Is_Connected )
        {
          Capture_Trace.Write ( "Stop_Reading: flushing serial buffer" );
          _Comm.Flush_Buffers ( );  // ← whatever your discard/flush method is

          // Restore default read timeout
          _Comm.Read_Timeout_Ms = _Settings.Default_GPIB_Timeout_Ms;

          // Single form - Update_Scrollbar_Range:
          Multimeter_Common_Helpers_Class.Update_Scrollbar_Range (
            Pan_Scrollbar, _Readings.Count,
            _Max_Display_Points, _Auto_Scroll, ref _View_Offset );
        }
      }
      catch { }



      // Force one final repaint to show last reading
      Chart_Panel.Invalidate ( );
      Update_Status_Panel ( );
    }



  










    // ========================================
    // MODIFIED: CHART_PANEL_PAINT (with rolling)
    // ========================================



    private void Chart_Panel_Paint ( object? Sender, PaintEventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );


      Multimeter_Common_Helpers_Class.Track_FPS (
   ref _Paint_Count,
   ref _Actual_FPS,
   _FPS_Stopwatch,
   Update_Performance_Status );


      Graphics G = E.Graphics;
      G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

      int W = Chart_Panel.ClientSize.Width;
      int H = Chart_Panel.ClientSize.Height;

      using var Bg_Brush = new SolidBrush ( _Theme.Background );
      G.FillRectangle ( Bg_Brush, 0, 0, W, H );

      // ========== ADD INSTRUMENT NAME HERE ==========
      // Draw instrument name at top left
      if ( _Comm.Is_Connected && _Comm.Mode == Connection_Mode.Prologix_GPIB )
      {
        string Instrument_Name = Get_Instrument_Display_Name ( );
        using ( var Name_Font = new Font ( this.Font.FontFamily, 10f, FontStyle.Bold ) )
        using ( var Name_Brush = new SolidBrush ( _Theme.Foreground ) )
        {
          G.DrawString ( Instrument_Name, Name_Font, Name_Brush, 10, 5 );
        }
      }
      // ==============================================

      int Margin_Left = 80;
      int Margin_Right = 20;
      int Margin_Top = 20;
      int Margin_Bottom = 50;

      int Chart_W = W - Margin_Left - Margin_Right;
      int Chart_H = H - Margin_Top - Margin_Bottom;

      if ( Chart_W < 10 || Chart_H < 10 )
        return;

      if ( _Readings.Count == 0 )
      {
        Draw_Empty_State_Single ( G, Margin_Left, Margin_Top, Chart_H );
        return;
      }

      if ( _Readings.Count == 0 )
      {
        Draw_Empty_State_Single ( G, Margin_Left, Margin_Top, Chart_H );
        return;
      }

      int Style = Graph_Style_Combo.SelectedIndex;

      // Histogram and Pie use full dataset
      if ( Style == 4 || Style == 5 )
      {
        if ( Style == 4 )
          Draw_Histogram ( G, W, H, Margin_Left, Margin_Right,
              Margin_Top, Margin_Bottom, Chart_W, Chart_H );
        else
          Draw_Pie_Chart ( G, W, H, Margin_Left, Margin_Right,
              Margin_Top, Margin_Bottom, Chart_W, Chart_H );

        //    Draw_Stats_Overlay_With_Time ( G, W, Margin_Top, Margin_Right,
        //        Bottom: Style == 5, H: H, Margin_Bottom: Margin_Bottom );
        return;
      }

      // Get visible data range
      var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  _Readings.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

      // Calculate Y-axis range from visible points
      var (Padded_Min, Padded_Max, Padded_Range) =
        Calculate_Y_Range_Single ( Start_Index, Visible_Count );

      // Draw grid and axes
      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Label_Font = new Font ( "Consolas", 7.5F );
      using var Label_Brush = new SolidBrush ( _Theme.Labels );

      Draw_Y_Axis_Single (
        G, Padded_Min, Padded_Range,
        W, H, Chart_H,
        Margin_Left, Margin_Right, Margin_Bottom,
        Grid_Pen, Label_Font, Label_Brush );

      Draw_X_Grid_And_Labels (
        G, Start_Index, Visible_Count,
        W, H, Chart_W,
        Margin_Left, Margin_Top, Margin_Bottom,
        Grid_Pen, Label_Font, Label_Brush );

      // Build point array from visible range
      PointF [ ] Points = Build_Point_Array_Single (
        Start_Index, Visible_Count,
        Padded_Min, Padded_Range,
        Margin_Left, Margin_Bottom,
        Chart_W, Chart_H, H );

      // Draw chart based on style
      float Baseline = H - Margin_Bottom;
      Color Line_Color = _Theme.Line_Colors [ 0 ];

      Draw_Chart_By_Style (
        G, Points, Visible_Count, Style,
        Line_Color, Margin_Left, Margin_Top,
        Chart_W, Baseline );

      // Draw overlays
      Draw_Last_Point_Highlight ( G, Points, Visible_Count, Line_Color );

      Draw_X_Axis_Label (
        G, Start_Index, Visible_Count,
        W, H, Chart_W,
        Margin_Left, Margin_Bottom,
        Label_Font, Label_Brush );

      //    Draw_Stats_Overlay_With_Time ( G, W, Margin_Top, Margin_Right );

      Draw_Hover_Point_Highlight (
        G, Start_Index, Visible_Count,
        Padded_Min, Padded_Range,
        Margin_Left, Margin_Bottom,
        Chart_W, Chart_H, H );
    }


    private void Draw_Empty_State_Single (
  Graphics G,
  int Margin_Left,
  int Margin_Top,
  int Chart_H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      using var Empty_Font = new Font ( "Segoe UI", 10F );
      using var Empty_Brush = new SolidBrush ( _Theme.Labels );

      G.DrawString ( "No data. Press Start to begin reading.",
        Empty_Font, Empty_Brush,
        Margin_Left + 20, Margin_Top + Chart_H / 2 );
    }



    private (double Min, double Max, double Range) Calculate_Y_Range_Single (
      int Start_Index,
      int Visible_Count )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Safety checks
      if ( Visible_Count == 0 || _Readings.Count == 0 )
        return (0, 1, 1);

      // Clamp the range to actual data bounds
      int End_Index = Math.Min ( Start_Index + Visible_Count, _Readings.Count );
      if ( Start_Index >= _Readings.Count )
        return (0, 1, 1);

      double Min_V = double.MaxValue;
      double Max_V = double.MinValue;

      for ( int I = Start_Index; I < End_Index; I++ )  // ← Use End_Index instead
      {
        if ( _Readings [ I ] < Min_V )
          Min_V = _Readings [ I ];
        if ( _Readings [ I ] > Max_V )
          Max_V = _Readings [ I ];
      }

      double Range = Max_V - Min_V;
      if ( Range < 1e-12 )
      {
        Range = Math.Abs ( Max_V ) * 0.1;
        if ( Range < 1e-12 )
          Range = 1.0;
      }

      double Padded_Min = Min_V - Range * 0.1;
      double Padded_Max = Max_V + Range * 0.1;
      double Padded_Range = Padded_Max - Padded_Min;

      return (Padded_Min, Padded_Max, Padded_Range);
    }
    private void Draw_Y_Axis_Single (
  Graphics G,
  double Padded_Min,
  double Padded_Range,
  int W,
  int H,
  int Chart_H,
  int Margin_Left,
  int Margin_Right,
  int Margin_Bottom,
  Pen Grid_Pen,
  Font Label_Font,
  Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Num_Grid_Lines = 6;

      for ( int I = 0; I <= Num_Grid_Lines; I++ )
      {
        double Fraction = (double) I / Num_Grid_Lines;
        double Value = Padded_Min + Fraction * Padded_Range;
        int Y_Pos = H - Margin_Bottom - (int) ( Fraction * Chart_H );

        G.DrawLine ( Grid_Pen, Margin_Left, Y_Pos, W - Margin_Right, Y_Pos );

        string Label_Text = Format_Value ( Value, _Current_Unit, _Selected_Meter );
        var Label_Size = G.MeasureString ( Label_Text, Label_Font );
        G.DrawString ( Label_Text, Label_Font, Label_Brush,
          Margin_Left - Label_Size.Width - 6,
          Y_Pos - Label_Size.Height / 2 );
      }
    }

    private void Draw_X_Grid_And_Labels (
  Graphics G,
  int Start_Index,
  int Visible_Count,
  int W,
  int H,
  int Chart_W,
  int Margin_Left,
  int Margin_Top,
  int Margin_Bottom,
  Pen Grid_Pen,
  Font Label_Font,
  Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Num_V_Lines = Math.Min ( 10, Visible_Count - 1 );
      int Total_Count = _Readings.Count;

      if ( Num_V_Lines <= 0 || _Reading_Timestamps.Count != Total_Count )
        return;

      for ( int I = 0; I <= Num_V_Lines; I++ )
      {
        double Fraction = (double) I / Num_V_Lines;
        int X_Pos = Margin_Left + (int) ( Fraction * Chart_W );
        G.DrawLine ( Grid_Pen, X_Pos, Margin_Top, X_Pos, H - Margin_Bottom );

        if ( _Show_Time_Axis )
        {
          int Index = Start_Index + (int) ( Fraction * ( Visible_Count - 1 ) );
          TimeSpan Elapsed = _Reading_Timestamps [ Index ] -
                            _Reading_Timestamps [ Start_Index ];
          string Time_Label = Multimeter_Common_Helpers_Class.Format_Time_Span ( Elapsed );

          var Time_Size = G.MeasureString ( Time_Label, Label_Font );
          G.DrawString ( Time_Label, Label_Font, Label_Brush,
            X_Pos - Time_Size.Width / 2,
            H - Margin_Bottom + 4 );
        }
        else
        {
          int Reading_Num = (int) ( Fraction * ( Visible_Count - 1 ) ) + 1;
          string Num_Label = Reading_Num.ToString ( );
          var Num_Size = G.MeasureString ( Num_Label, Label_Font );
          G.DrawString ( Num_Label, Label_Font, Label_Brush,
            X_Pos - Num_Size.Width / 2,
            H - Margin_Bottom + 4 );
        }
      }
    }


    private PointF [ ] Build_Point_Array_Single (
      int Start_Index,
      int Visible_Count,
      double Padded_Min,
      double Padded_Range,
      int Margin_Left,
      int Margin_Bottom,
      int Chart_W,
      int Chart_H,
      int H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Safety checks
      if ( Visible_Count == 0 || _Readings.Count == 0 )
        return Array.Empty<PointF> ( );

      // Clamp visible count to actual data available
      int Actual_Count = Math.Min ( Visible_Count, _Readings.Count - Start_Index );
      if ( Actual_Count <= 0 )
        return Array.Empty<PointF> ( );

      PointF [ ] Points = new PointF [ Actual_Count ];

      for ( int I = 0; I < Actual_Count; I++ )
      {
        int Data_Index = Start_Index + I;
        double Normalized = ( _Readings [ Data_Index ] - Padded_Min ) / Padded_Range;
        float X = Margin_Left +
          ( Actual_Count == 1 ? Chart_W / 2f
            : I * ( Chart_W / (float) ( Actual_Count - 1 ) ) );
        float Y = H - Margin_Bottom - (float) ( Normalized * Chart_H );
        Points [ I ] = new PointF ( X, Y );
      }

      return Points;
    }

    private void Draw_Chart_By_Style (
  Graphics G,
  PointF [ ] Points,
  int Visible_Count,
  int Style,
  Color Line_Color,
  int Margin_Left,
  int Margin_Top,
  int Chart_W,
  float Baseline )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Color Dot_Color = Color.FromArgb ( 200, Line_Color.R, Line_Color.G, Line_Color.B );
      using var Line_Pen = new Pen ( Line_Color, 2f );
      using var Dot_Brush = new SolidBrush ( Dot_Color );

      switch ( Style )
      {
        case 1:
          Draw_Bar_Chart ( G, Points, Visible_Count, Margin_Left, Chart_W, Baseline );
          break;
        case 2:
          Draw_Scatter_Chart ( G, Points, Visible_Count, Dot_Brush );
          break;
        case 3:
          Draw_Step_Chart ( G, Points, Visible_Count, Line_Pen, Margin_Top, Baseline );
          break;
        default:
          Draw_Line_Chart ( G, Points, Visible_Count, Line_Pen, Dot_Brush,
            Margin_Top, Baseline );
          break;
      }
    }

    private void Draw_Last_Point_Highlight (
  Graphics G,
  PointF [ ] Points,
  int Visible_Count,
  Color Line_Color )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Visible_Count == 0 )
        return;

      PointF Last = Points [ Visible_Count - 1 ];
      using var Glow_Pen = new Pen ( Color.FromArgb ( 80, Line_Color ), 6f );
      G.DrawEllipse ( Glow_Pen, Last.X - 5, Last.Y - 5, 10, 10 );
      using var Last_Brush = new SolidBrush ( Color.White );
      G.FillEllipse ( Last_Brush, Last.X - 3, Last.Y - 3, 6, 6 );
    }



    private void Draw_X_Axis_Label (
      Graphics G,
      int Start_Index,
      int Visible_Count,
      int W,
      int H,
      int Chart_W,
      int Margin_Left,
      int Margin_Bottom,
      Font Label_Font,
      Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      int Total_Count = _Readings.Count;

      // Clamp visible count to actual data
      int Actual_Count = Math.Min ( Visible_Count, Total_Count - Start_Index );

      string X_Text;
      if ( _Show_Time_Axis && _Reading_Timestamps.Count == Total_Count && Actual_Count >= 2 )
      {
        // Safety check before accessing timestamps
        int End_Index = Start_Index + Actual_Count - 1;
        if ( End_Index < _Reading_Timestamps.Count && Start_Index < _Reading_Timestamps.Count )
        {
          TimeSpan Total_Time = _Reading_Timestamps [ End_Index ] -
                               _Reading_Timestamps [ Start_Index ];
          if ( _Enable_Rolling && Total_Count > _Max_Display_Points )
          {
            X_Text = $"Elapsed Time (Rolling: {Actual_Count} of {Total_Count} points)  " +
                    $"(Window: {Multimeter_Common_Helpers_Class.Format_Time_Span ( Total_Time )})";
          }
          else
          {
            X_Text = $"Elapsed Time  (Total: {Multimeter_Common_Helpers_Class.Format_Time_Span ( Total_Time )})";
          }
        }
        else
        {
          // Fallback if timestamps are invalid
          X_Text = $"Reading  (Showing {Actual_Count} of {Total_Count})";
        }
      }
      else
      {
        if ( _Enable_Rolling && Total_Count > _Max_Display_Points )
        {
          X_Text = $"Reading  (Showing {Actual_Count} of {Total_Count})";
        }
        else
        {
          X_Text = $"Reading  (1 - {Total_Count})";
        }
      }

      var X_Size = G.MeasureString ( X_Text, Label_Font );
      G.DrawString ( X_Text, Label_Font, Label_Brush,
        Margin_Left + Chart_W / 2 - X_Size.Width / 2,
        H - Margin_Bottom + 28 );
    }


    private void Draw_Hover_Point_Highlight (
      Graphics G,
      int Start_Index,
      int Visible_Count,
      double Padded_Min,
      double Padded_Range,
      int Margin_Left,
      int Margin_Bottom,
      int Chart_W,
      int Chart_H,
      int H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Hover_Point_Index < 0 || _Hover_Point_Index >= _Readings.Count )
        return;

      var (Hover_Start_Index, Hover_Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  _Readings.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

      // Clamp visible count to actual data
      int Actual_Visible_Count = Math.Min ( Hover_Visible_Count, _Readings.Count - Hover_Start_Index );
      int Hover_End_Index = Hover_Start_Index + Actual_Visible_Count;

      // Check if hovered point is in visible range
      if ( _Hover_Point_Index < Hover_Start_Index || _Hover_Point_Index >= Hover_End_Index )
        return;

      // Calculate position for the hovered point
      int Point_Offset = _Hover_Point_Index - Hover_Start_Index;
      double Normalized = ( _Readings [ _Hover_Point_Index ] - Padded_Min ) / Padded_Range;

      float Point_X = Margin_Left +
        ( Actual_Visible_Count == 1 ? Chart_W / 2f
          : Point_Offset * ( Chart_W / (float) ( Actual_Visible_Count - 1 ) ) );
      float Point_Y = H - Margin_Bottom - (float) ( Normalized * Chart_H );

      // Draw highlight circle
      using var Highlight_Pen = new Pen ( Color.Yellow, 3f );
      G.DrawEllipse ( Highlight_Pen, Point_X - 8, Point_Y - 8, 16, 16 );

      // Draw center dot
      using var Hover_Dot_Brush = new SolidBrush ( Color.Red );
      G.FillEllipse ( Hover_Dot_Brush, Point_X - 4, Point_Y - 4, 8, 8 );
    }









    // *** NEW: Enhanced stats with rolling support ***
    private void Draw_Stats_Overlay_With_Time ( Graphics G,
        int W, int Margin_Top, int Margin_Right,
        bool Bottom = false, int H = 0, int Margin_Bottom = 0 )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Readings.Count == 0 )
        return;

      var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  _Readings.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
      int Total_Count = _Readings.Count;

      // Calculate stats for visible points only
      double Min_V = double.MaxValue;
      double Max_V = double.MinValue;
      double Sum = 0;

      for ( int I = Start_Index; I < Start_Index + Visible_Count; I++ )
      {
        double V = _Readings [ I ];
        if ( V < Min_V )
          Min_V = V;
        if ( V > Max_V )
          Max_V = V;
        Sum += V;
      }

      double Mean = Sum / Visible_Count;
      double Sum_Sq = 0;
      for ( int I = Start_Index; I < Start_Index + Visible_Count; I++ )
      {
        double D = _Readings [ I ] - Mean;
        Sum_Sq += D * D;
      }
      double Std_Dev = Math.Sqrt ( Sum_Sq / Visible_Count );

      var Lines = new List<string>
            {
                $"Mean:    {Format_Value(Mean, _Current_Unit, _Selected_Meter)}",
                $"Std Dev: {Format_Value(Std_Dev, _Current_Unit, _Selected_Meter)}",
                $"Min:     {Format_Value(Min_V, _Current_Unit, _Selected_Meter)}",
                $"Max:     {Format_Value(Max_V, _Current_Unit, _Selected_Meter)}",
            };

      // Show visible vs total count
      if ( _Enable_Rolling && Total_Count > _Max_Display_Points )
      {
        Lines.Add ( $"Showing: {Visible_Count} / {Total_Count}" );
      }
      else
      {
        Lines.Add ( $"Count:   {Visible_Count}" );
      }

      // Add timing statistics
      if ( _Reading_Timestamps.Count == Total_Count && Visible_Count >= 2 )
      {
        TimeSpan Total_Time = _Reading_Timestamps [ Start_Index + Visible_Count - 1 ] -
                             _Reading_Timestamps [ Start_Index ];
        double Avg_Rate = ( Visible_Count - 1 ) / Total_Time.TotalSeconds;

        Lines.Add ( $"Duration: {Multimeter_Common_Helpers_Class.Format_Time_Span ( Total_Time )}" );
        Lines.Add ( $"Avg Rate: {Avg_Rate:F2} S/s" );

        double Total_Ms = 0;
        for ( int I = Start_Index + 1; I < Start_Index + Visible_Count; I++ )
        {
          Total_Ms += ( _Reading_Timestamps [ I ] -
                      _Reading_Timestamps [ I - 1 ] ).TotalMilliseconds;
        }
        double Avg_Interval = Total_Ms / ( Visible_Count - 1 );
        Lines.Add ( $"Avg ∆t:  {Avg_Interval:F1}ms" );
      }

      using var Stats_Font = new Font ( "Consolas", 7.5F );
      using var Text_Brush = new SolidBrush ( _Theme.Labels );

      float Line_H = G.MeasureString ( "X", Stats_Font ).Height + 2;
      float Box_W = 0;
      foreach ( string L in Lines )
      {
        float Lw = G.MeasureString ( L, Stats_Font ).Width;
        if ( Lw > Box_W )
          Box_W = Lw;
      }

      float Padding = 8;
      float Box_H = Lines.Count * Line_H + Padding * 2;
      Box_W += Padding * 2;

      float Box_X = W - Margin_Right - Box_W - 5;
      float Box_Y = Bottom ? H - Margin_Bottom - Box_H - 5 : Margin_Top + 5;

      using var Bg_Brush = new SolidBrush ( Color.FromArgb ( 180, _Theme.Background ) );
      using var Border_Pen = new Pen ( _Theme.Grid, 1f );

      G.FillRectangle ( Bg_Brush, Box_X, Box_Y, Box_W, Box_H );
      G.DrawRectangle ( Border_Pen, Box_X, Box_Y, Box_W, Box_H );

      float Y = Box_Y + Padding;
      foreach ( string L in Lines )
      {
        G.DrawString ( L, Stats_Font, Text_Brush, Box_X + Padding, Y );
        Y += Line_H;
      }
    }



    // ============================================================================
    // CORRECTED Format_Value METHOD for 8.5 Digit Precision
    // ============================================================================
    //
    // Replace the Format_Value method in Voltage_Reader_Form.cs with this version
    // to display the full 8.5 digits of precision from your HP 3458A meter.
    //
    // The issue was that F6 only shows 6 decimal places, but an 8.5 digit meter
    // can show up to 8 significant digits with proper resolution.
    // ============================================================================


    private static string Format_Value ( double Value, string Unit, Meter_Type Meter = Meter_Type.HP34401A )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      double Abs = Math.Abs ( Value );

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

      // V or A - digit precision depends on meter
      bool Is_HP = Meter == Meter_Type.HP3458A;

      if ( Abs >= 1.0 )
        return Is_HP
            ? $"{Value:F10} {Unit}"   // 10 digits for 3458A
            : $"{Value:F8} {Unit}";   // 8.5 digit for 34401A
      if ( Abs >= 0.001 )
        return Is_HP
            ? $"{Value * 1000:F8} m{Unit}"
            : $"{Value * 1000:F6} m{Unit}";
      if ( Abs >= 0.000001 )
        return Is_HP
            ? $"{Value * 1e6:F6} u{Unit}"
            : $"{Value * 1e6:F5} u{Unit}";
      if ( Abs >= 0.000000001 )
        return Is_HP
            ? $"{Value * 1e9:F4} n{Unit}"
            : $"{Value * 1e9:F3} n{Unit}";
      return $"{Value:E2} {Unit}";
    }




    private static string old_Format_Value ( double Value, string Unit, Meter_Type Meter )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      double Abs = Math.Abs ( Value );

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
      {
        return $"{Value:F2} \u00b0C";
      }

      // V or A - 8.5 digit precision
      if ( Abs >= 1.0 )
        return $"{Value:F8} {Unit}";
      if ( Abs >= 0.001 )
        return $"{Value * 1000:F6} m{Unit}";
      if ( Abs >= 0.000001 )
        return $"{Value * 1e6:F5} u{Unit}";
      if ( Abs >= 0.000000001 )
        return $"{Value * 1e9:F3} n{Unit}";
      return $"{Value:E2} {Unit}";
    }


    private void Chart_Panel_MouseMove ( object? sender, MouseEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Enable_High_Performance_Mode ( );

      if ( _Readings.Count == 0 )
      {

        if ( _Hover_Point_Index != -1 )
        {
          _Hover_Point_Index = -1;
          _Chart_Tooltip.Hide ( Chart_Panel );
          Chart_Panel.Invalidate ( );
        }
        return;
      }

      int Style = Graph_Style_Combo.SelectedIndex;


      // Only for line/scatter, not pie/histogram
      if ( Style == 4 || Style == 5 )
      {

        if ( _Hover_Point_Index != -1 )
        {
          _Hover_Point_Index = -1;
          _Chart_Tooltip.Hide ( Chart_Panel );
          Chart_Panel.Invalidate ( );
        }
        return;
      }

      if ( Math.Abs ( e.X - _Last_Mouse_Position.X ) < 3 &&
          Math.Abs ( e.Y - _Last_Mouse_Position.Y ) < 3 )
      {

        return;
      }

      _Last_Mouse_Position = e.Location;


      int Closest_Index = Find_Closest_Point ( e.X, e.Y );



      if ( Closest_Index != _Hover_Point_Index )
      {
        _Hover_Point_Index = Closest_Index;


        if ( _Hover_Point_Index >= 0 )
        {

          Show_Point_Tooltip ( _Hover_Point_Index, e.X, e.Y );
        }
        else
        {

          _Chart_Tooltip.Hide ( Chart_Panel );
        }

        Chart_Panel.Invalidate ( );

        Request_Chart_Refresh ( );
      }
    }
    private int Find_Closest_Point ( int Mouse_X, int Mouse_Y )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Readings.Count == 0 )
        return -1;

      int W = Chart_Panel.ClientSize.Width;
      int H = Chart_Panel.ClientSize.Height;
      int Margin_Left = 80;
      int Margin_Right = 20;
      int Margin_Top = 20;
      int Margin_Bottom = 50;
      int Chart_W = W - Margin_Left - Margin_Right;
      int Chart_H = H - Margin_Top - Margin_Bottom;

      var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  _Readings.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

      if ( Visible_Count == 0 )
        return -1;

      // Clamp visible count to actual data
      int Actual_Count = Math.Min ( Visible_Count, _Readings.Count - Start_Index );
      if ( Actual_Count <= 0 )
        return -1;

      int End_Index = Start_Index + Actual_Count;

      // Calculate min/max for Y scaling
      double Min_V = double.MaxValue;
      double Max_V = double.MinValue;

      for ( int I = Start_Index; I < End_Index; I++ )  // ← Use End_Index
      {
        if ( _Readings [ I ] < Min_V )
          Min_V = _Readings [ I ];
        if ( _Readings [ I ] > Max_V )
          Max_V = _Readings [ I ];
      }

      double Range = Max_V - Min_V;
      if ( Range < 1e-12 )
        Range = Math.Abs ( Max_V ) * 0.1;
      if ( Range < 1e-12 )
        Range = 1.0;

      double Padded_Min = Min_V - Range * 0.1;
      double Padded_Max = Max_V + Range * 0.1;
      double Padded_Range = Padded_Max - Padded_Min;

      int Closest_Index = -1;
      double Closest_X_Distance = double.MaxValue;

      for ( int I = 0; I < Actual_Count; I++ )  // ← Use Actual_Count
      {
        int Data_Index = Start_Index + I;
        double Normalized = ( _Readings [ Data_Index ] - Padded_Min ) / Padded_Range;

        float Point_X = Margin_Left +
            ( Actual_Count == 1 ? Chart_W / 2f
                : I * ( Chart_W / (float) ( Actual_Count - 1 ) ) );
        float Point_Y = H - Margin_Bottom - (float) ( Normalized * Chart_H );

        // Only check X distance (time axis)
        double X_Dist = Math.Abs ( Point_X - Mouse_X );

        if ( X_Dist < Closest_X_Distance )
        {
          Closest_X_Distance = X_Dist;
          Closest_Index = Data_Index;
        }
      }

      // Only show tooltip if mouse is within chart area horizontally
      if ( Closest_X_Distance < Chart_W / ( Actual_Count * 2.0 ) )
      {
        return Closest_Index;
      }

      return -1;
    }
    private void Show_Point_Tooltip ( int Index, int Mouse_X, int Mouse_Y )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Index < 0 || Index >= _Readings.Count )
        return;

      double Value = _Readings [ Index ];
      DateTime Time = _Reading_Timestamps [ Index ];

      int Reading_Number = Index + 1;
      TimeSpan Elapsed = Time - _Reading_Timestamps [ 0 ];

      string Tooltip_Text =
          $"Reading: {Reading_Number}\n" +
          $"Time: {Time:HH:mm:ss.fff}\n" +
          $"Elapsed: {Multimeter_Common_Helpers_Class.Format_Time_Span ( Elapsed )}\n" +
          $"Value: {Format_Value ( Value, _Current_Unit, _Selected_Meter )}";

      _Chart_Tooltip.Show ( Tooltip_Text, Chart_Panel, Mouse_X + 15, Mouse_Y - 10 );
    }







    private void Initialize_Chart_Refresh_Timer ( )
    {
      _Chart_Refresh_Timer = new System.Windows.Forms.Timer ( );
      _Chart_Refresh_Timer.Tick += Chart_Refresh_Timer_Tick;
      Update_Chart_Refresh_Rate ( );
      _Chart_Refresh_Timer.Start ( );
    }



    private void Update_Chart_Refresh_Rate ( )
    {
      int Base_Rate = _Settings.Chart_Refresh_Rate_Ms;

      // Apply throttling if enabled and threshold exceeded
      if ( _Settings.Throttle_When_Many_Points &&
          _Readings.Count > _Settings.Throttle_Point_Threshold )
      {
        // Throttle based on data size
        int Multiplier = 1;

        if ( _Readings.Count > _Settings.Throttle_Point_Threshold * 10 )
          Multiplier = 4; // Very slow for huge datasets
        else if ( _Readings.Count > _Settings.Throttle_Point_Threshold * 5 )
          Multiplier = 3;
        else if ( _Readings.Count > _Settings.Throttle_Point_Threshold * 2 )
          Multiplier = 2;

        _Chart_Refresh_Timer.Interval = Base_Rate * Multiplier;
      }
      else
      {
        _Chart_Refresh_Timer.Interval = Base_Rate;
      }
    }

    private void Request_Chart_Refresh ( )
    {
      // Instead of Chart_Panel.Invalidate(), use this
      _Chart_Needs_Refresh = true;

      // Also update refresh rate dynamically as data grows
      Update_Chart_Refresh_Rate ( );
    }

    // Replace all Chart_Panel.Invalidate() calls with:
    private void old_Add_Reading ( double value )
    {
      _Readings.Add ( value );
      _Reading_Timestamps.Add ( DateTime.Now );
      Multimeter_Common_Helpers_Class.Check_Memory_Limit (
        _Settings,
        ( ) => _Readings.Count,       // single list
        Stop_Recording,
        Show_Memory_Warning,
        ref _Memory_Warning_Shown );
    }

    // Show refresh rate in status bar
    private void Update_Performance_Status ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Multimeter_Common_Helpers_Class.Update_Performance_Status (
        _Performance_Status_Label,
        _Memory_Status_Label,
        _Actual_FPS,
        _Readings.Count,
        _Readings.Count,
        _Settings.Max_Data_Points_In_Memory,
        _Settings.Warning_Threshold_Percent );
    }

    // Optional: Manual override for smooth animations
    private void Enable_High_Performance_Mode ( )
    {
      // Temporarily boost refresh rate for smooth interactions
      _Chart_Refresh_Timer.Interval = 16; // ~60 FPS
    }

    private void Restore_Normal_Performance_Mode ( )
    {
      Update_Chart_Refresh_Rate ( );
    }






    private void Stop_Recording_Due_To_Memory_Limit ( )
    {
      _Is_Recording = false;
      Start_Stop_Button.Text = "Start";
      _Timer.Stop ( );

      var Result = MessageBox.Show (
        $"Maximum data points ({_Settings.Max_Data_Points_In_Memory:N0}) reached.\n" +
        "Recording has been stopped to prevent memory issues.\n\n" +
        "Would you like to save the recorded data?",
        "Memory Limit Reached",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning );

      if ( Result == DialogResult.Yes )
      {
        Save_Recorded_Data ( );
      }
    }

    private void Show_Memory_Warning ( int Current, int Max )
    {
      int Percent = ( Current * 100 ) / Max;

      var Dlg = new Form
      {
        Text = "Memory Warning",
        Size = new Size ( 450, 220 ),
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MaximizeBox = false,
        MinimizeBox = false,
        StartPosition = FormStartPosition.CenterParent
      };

      var Icon_Box = new PictureBox
      {
        Location = new Point ( 20, 20 ),
        Size = new Size ( 48, 48 ),
        Image = SystemIcons.Warning.ToBitmap ( )
      };
      Dlg.Controls.Add ( Icon_Box );

      var Message_Label = new Label
      {
        Location = new Point ( 80, 20 ),
        Size = new Size ( 340, 60 ),
        Text = $"Memory usage is at {Percent}% of the limit.\n\n" +
               $"Current: {Current:N0} points\n" +
               $"Limit: {Max:N0} points",
        Font = new Font ( this.Font.FontFamily, 9f )
      };
      Dlg.Controls.Add ( Message_Label );

      var Progress_Bar = new ProgressBar
      {
        Location = new Point ( 80, 90 ),
        Size = new Size ( 340, 20 ),
        Minimum = 0,
        Maximum = 100,
        Value = Percent
      };
      Dlg.Controls.Add ( Progress_Bar );

      var Continue_Button = new Button
      {
        Text = "Continue Recording",
        Location = new Point ( 80, 130 ),
        Size = new Size ( 140, 30 ),
        DialogResult = DialogResult.OK
      };
      Dlg.Controls.Add ( Continue_Button );

      var Stop_Save_Button = new Button
      {
        Text = "Stop && Save",
        Location = new Point ( 230, 130 ),
        Size = new Size ( 100, 30 )
      };
      Stop_Save_Button.Click += ( s, e ) =>
      {
        Dlg.DialogResult = DialogResult.Yes;
        Dlg.Close ( );
      };
      Dlg.Controls.Add ( Stop_Save_Button );

      var Stop_Button = new Button
      {
        Text = "Stop",
        Location = new Point ( 340, 130 ),
        Size = new Size ( 80, 30 )
      };
      Stop_Button.Click += ( s, e ) =>
      {
        Dlg.DialogResult = DialogResult.No;
        Dlg.Close ( );
      };
      Dlg.Controls.Add ( Stop_Button );

      Dlg.AcceptButton = Continue_Button;

      var Result = Dlg.ShowDialog ( );

      if ( Result == DialogResult.Yes )
      {
        Stop_Recording ( );
        Save_Recorded_Data ( );
      }
      else if ( Result == DialogResult.No )
      {
        Stop_Recording ( );
      }
      // DialogResult.OK = continue recording
    }

    private void Update_Memory_Status ( int Current, int Max )
    {
      int Percent = ( Current * 100 ) / Max;

      // Assuming you have a status strip
      if ( _Memory_Status_Label != null )
      {
        _Memory_Status_Label.Text = $"Memory: {Current:N0} / {Max:N0} ({Percent}%)";

        // Color code based on usage
        if ( Percent >= 90 )
          _Memory_Status_Label.ForeColor = Color.Red;
        else if ( Percent >= _Settings.Warning_Threshold_Percent )
          _Memory_Status_Label.ForeColor = Color.Orange;
        else
          _Memory_Status_Label.ForeColor = Color.Green;
      }
    }

    private void Clear_Data_With_Memory_Reset ( )
    {
      _Readings.Clear ( );
      _Reading_Timestamps.Clear ( );
      _Memory_Warning_Shown = false;
      Update_Memory_Status ( 0, _Settings.Max_Data_Points_In_Memory );
      Chart_Panel.Invalidate ( );
    }


    private PointF [ ] Build_Point_Array_Single_With_Caching (
      int Start_Index,
      int Visible_Count,
      double Padded_Min,
      double Padded_Range,
      int Margin_Left,
      int Margin_Bottom,
      int Chart_W,
      int Chart_H,
      int H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Check if we can reuse cached points
      if ( _Cached_Points != null &&
          _Last_Cached_Count == _Readings.Count &&
          _Cached_Points.Length == Visible_Count )
      {
        return _Cached_Points;
      }

      // Safety checks
      if ( Visible_Count == 0 || _Readings.Count == 0 )
        return Array.Empty<PointF> ( );

      int Actual_Count = Math.Min ( Visible_Count, _Readings.Count - Start_Index );
      if ( Actual_Count <= 0 )
        return Array.Empty<PointF> ( );

      // Apply downsampling for very large datasets
      if ( _Settings.Throttle_When_Many_Points &&
          Actual_Count > 10000 )
      {
        return Build_Downsampled_Points (
          Start_Index, Actual_Count, Padded_Min, Padded_Range,
          Margin_Left, Margin_Bottom, Chart_W, Chart_H, H );
      }

      // Build normal points
      PointF [ ] Points = new PointF [ Actual_Count ];

      for ( int I = 0; I < Actual_Count; I++ )
      {
        int Data_Index = Start_Index + I;
        double Normalized = ( _Readings [ Data_Index ] - Padded_Min ) / Padded_Range;
        float X = Margin_Left +
          ( Actual_Count == 1 ? Chart_W / 2f
            : I * ( Chart_W / (float) ( Actual_Count - 1 ) ) );
        float Y = H - Margin_Bottom - (float) ( Normalized * Chart_H );
        Points [ I ] = new PointF ( X, Y );
      }

      // Cache results
      _Cached_Points = Points;
      _Last_Cached_Count = _Readings.Count;

      return Points;
    }

    private PointF [ ] Build_Downsampled_Points (
      int Start_Index,
      int Count,
      double Padded_Min,
      double Padded_Range,
      int Margin_Left,
      int Margin_Bottom,
      int Chart_W,
      int Chart_H,
      int H )
    {
      // Downsample to max 5000 points for rendering
      int Target_Points = Math.Min ( Count, 5000 );
      int Step = Count / Target_Points;

      var Points = new List<PointF> ( );

      for ( int I = 0; I < Count; I += Step )
      {
        int Data_Index = Start_Index + I;
        if ( Data_Index >= _Readings.Count )
          break;

        double Normalized = ( _Readings [ Data_Index ] - Padded_Min ) / Padded_Range;
        float X = Margin_Left + ( I * Chart_W / (float) Count );
        float Y = H - Margin_Bottom - (float) ( Normalized * Chart_H );
        Points.Add ( new PointF ( X, Y ) );
      }

      return Points.ToArray ( );
    }

    private void Invalidate_Point_Cache ( )
    {
      _Cached_Points = null;
      _Last_Cached_Count = 0;
    }

    // Call when data changes
    private void Add_Reading ( double value )
    {
      _Readings.Add ( value );
      _Reading_Timestamps.Add ( DateTime.Now );

      Invalidate_Point_Cache ( ); // Clear cache when data changes

      Multimeter_Common_Helpers_Class.Check_Memory_Limit (
         _Settings,
         ( ) => _Readings.Count,       // single list
         Stop_Recording,
         Show_Memory_Warning,
         ref _Memory_Warning_Shown );
    }






    private void Initialize_Status_Panel ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      (_Memory_Status_Label, _Performance_Status_Label) =
    Multimeter_Common_Helpers_Class.Initialize_Status_Strip ( this, _Settings );

      // Store reference to the strip itself
      _Status_Panel = this.Controls.OfType<Panel> ( ).First ( );

      Update_Performance_Status ( );
    }


    private void old_Initialize_Status_Panel ( )
    {
      var Status_Strip = new StatusStrip ( );

      _Memory_Status_Label = new ToolStripStatusLabel
      {
        Name = "Memory_Status_Label",
        Text = "Memory: 0 / 0 (0%)",
        BorderSides = ToolStripStatusLabelBorderSides.Right
      };
      Status_Strip.Items.Add ( _Memory_Status_Label );

      _Performance_Status_Label = new ToolStripStatusLabel
      {
        Name = "Performance_Status_Label",
        Text = "Refresh: 0 FPS | Points: 0",
        BorderSides = ToolStripStatusLabelBorderSides.Right
      };
      Status_Strip.Items.Add ( _Performance_Status_Label );

      var Spring_Label = new ToolStripStatusLabel
      {
        Spring = true
      };
      Status_Strip.Items.Add ( Spring_Label );

      this.Controls.Add ( Status_Strip );
    }






    private string Get_Filename_From_Pattern ( )
    {
      string Filename = _Settings.Filename_Pattern;

      Filename = Filename.Replace ( "{date}", DateTime.Now.ToString ( "yyyy-MM-dd" ) );
      Filename = Filename.Replace ( "{time}", DateTime.Now.ToString ( "HH-mm-ss" ) );
      Filename = Filename.Replace ( "{function}", _Record_Function );

      if ( !Filename.EndsWith ( ".csv", StringComparison.OrdinalIgnoreCase ) )
        Filename += ".csv";

      return Filename;
    }



    private double Get_NPLC_Value ( )
    {
      if ( NPLC_Combo.SelectedItem == null )
        return 1.0;

      if ( double.TryParse ( NPLC_Combo.SelectedItem.ToString ( ),
          System.Globalization.NumberStyles.Float,
          System.Globalization.CultureInfo.InvariantCulture,
          out double Value ) )
        return Value;

      return 1.0;  // fallback
    }

    private void NPLC_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Comm.Is_Connected )
      {
        MessageBox.Show ( "Not connected.",
            "Connection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning );
        return;
      }

      if ( _Selected_Meter != Meter_Type.HP3458A )
      {
        MessageBox.Show ( "NPLC is only applicable to the 3458A.",
            "Not Applicable", MessageBoxButtons.OK, MessageBoxIcon.Information );
        return;
      }


      double NPLC_Value = Get_NPLC_Value ( );

      try
      {
        _Comm.Send_Instrument_Command ( $"NPLC {NPLC_Value.ToString (
            System.Globalization.CultureInfo.InvariantCulture )}" );
        //      Capture_Trace.Write ( $"NPLC set to {NPLC_Value}" );
      }
      catch ( Exception Ex )
      {
        MessageBox.Show ( $"Failed to set NPLC: {Ex.Message}",
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
      }
    }

    private void Set_Local_Mode ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Comm.Is_Connected )
      {
        Capture_Trace.Write ( "Not connected, skipping" );
        return;
      }

      try
      {
        switch ( _Selected_Meter )
        {
          case Meter_Type.HP34401A:
          case Meter_Type.HP33120A:
            Capture_Trace.Write ( "Set_Local_Mode: sending CLS?" );
            _Comm.Send_Instrument_Command ( "CLS?" );
            break;

          case Meter_Type.HP3458A:
            Capture_Trace.Write ( "3458A - GTL handled by ++loc only" );
            Capture_Trace.Write ( "skipping instrument command" );
            // LOCAL is not a valid written command on the 3458A
            // The Prologix ++loc below asserts GTL on the bus which is correct
            break;
        }

        if ( _Comm.Mode == Connection_Mode.Prologix_GPIB )
        {
          Capture_Trace.Write ( "Set_Local_Mode: sending ++loc to Prologix" );
          _Comm.Send_Prologix_Command ( "++loc" );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Set_Local_Mode: EXCEPTION: {Ex.Message}" );
      }
    }

    private async void Close_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( "Close_Button_Click: beginning graceful shutdown" );

      // Disable the button immediately to prevent double-clicks
      if ( Sender is Button Btn )
        Btn.Enabled = false;

      // 1. Stop active polling gracefully
      if ( _Is_Running )
      {
        Capture_Trace.Write ( "Close_Button_Click: cancelling active polling" );
        _Cts?.Cancel ( );

        // Wait for the in-flight read to complete/timeout
        // rather than yanking the rug out
        int Wait_Ms = 0;
        while ( _Is_Running && Wait_Ms < 5000 )
        {
          await Task.Delay ( 100 );
          Wait_Ms += 100;
        }

        if ( _Is_Running )
        {
          Capture_Trace.Write ( "Close_Button_Click: WARNING - polling did not stop within 5s, forcing" );
        }
        else
        {
          Capture_Trace.Write ( "Close_Button_Click: polling stopped cleanly" );
        }
      }

      // 2. Stop recording and save if active
      if ( _Is_Recording )
      {
        Capture_Trace.Write ( "Close_Button_Click: stopping active recording" );
        Stop_Recording ( );
      }

      // 3. Stop timers
      Capture_Trace.Write ( "Close_Button_Click: stopping timers" );
      _Chart_Refresh_Timer?.Stop ( );
      _Auto_Save_Timer?.Stop ( );

      // 4. Flush serial buffer (discard any in-flight response bytes)
      try
      {
        if ( _Comm.Is_Connected )
        {
          Capture_Trace.Write ( "Close_Button_Click: flushing serial buffers" );
          _Comm.Flush_Buffers ( );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Close_Button_Click: flush error (non-fatal): {Ex.Message}" );
      }

      // 5. Return meter to local control
      Capture_Trace.Write ( "Close_Button_Click: setting local mode" );
      Set_Local_Mode ( );

      // 6. Dispose CTS
      _Cts?.Dispose ( );
      _Cts = null;

      Capture_Trace.Write ( "Close_Button_Click: shutdown complete, closing form" );
      this.Close ( );
    }


    private void Pan_Scrollbar_Scroll ( object sender, ScrollEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Auto_Scroll_Check.Checked = false;

      int Total_Points = _Readings.Count;
      if ( Total_Points == 0 )
        return;

      int Max_Offset = Math.Max ( 0, Total_Points - _Max_Display_Points );
      if ( Max_Offset == 0 )
        return;

      int Scrollbar_Max = Pan_Scrollbar.Maximum - Pan_Scrollbar.LargeChange + 1;
      int Clamped_Value = Math.Max ( 0, Math.Min ( Scrollbar_Max, e.NewValue ) );
      // Convert scrollbar position to a view offset
      // 0 = most recent, Max_Offset = oldest
      int New_Offset = (int) ( ( (double) Clamped_Value / Scrollbar_Max ) * Max_Offset );

      // Store offset — add this field to your class if not present:
      // private int _View_Offset = 0;
      _View_Offset = New_Offset;

      if ( _View_Offset == 0 )
        Auto_Scroll_Check.Checked = true;

      Chart_Panel.Invalidate ( );
    }

    private void Pan_Scrollbar_ValueChanged ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Auto_Scroll_Check.Checked = false;

      int Total_Points = _Readings.Count;
      if ( Total_Points == 0 )
        return;

      int Max_Offset = Math.Max ( 0, Total_Points - _Max_Display_Points );
      if ( Max_Offset == 0 )
        return;

      int Scrollbar_Max = Pan_Scrollbar.Maximum - Pan_Scrollbar.LargeChange + 1;
      int Clamped_Value = Math.Max ( 0, Math.Min ( Scrollbar_Max, Pan_Scrollbar.Value ) );
      _View_Offset = (int) ( ( (double) Clamped_Value / Scrollbar_Max ) * Max_Offset );

      if ( _View_Offset == 0 )
        Auto_Scroll_Check.Checked = true;

      Chart_Panel.Invalidate ( );
    }

    private void Auto_Scroll_Check_CheckedChanged ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Auto_Scroll = Auto_Scroll_Check.Checked;

      if ( _Auto_Scroll )
      {
        _View_Offset = 0;
        if ( Pan_Scrollbar != null && Pan_Scrollbar.Enabled )
          Pan_Scrollbar.Value = 0;
        Chart_Panel.Invalidate ( );
      }
    }


  }
}
