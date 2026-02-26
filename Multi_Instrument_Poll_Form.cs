
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Channels;
using System.Windows.Forms;
using System.Xml.Linq;
using Trace_Execution_Namespace;
using static Trace_Execution_Namespace.Trace_Execution;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Multimeter_Controller
{
  public partial class Multi_Instrument_Poll_Form : Form
  {



    private int _View_Offset = 0;  // 0 = most recent, positive = looking back in time

    private bool _Auto_Scroll = true;  // Auto-scroll to most recent dat

    //   private TrackBar _Zoom_Slider;
    private Label _Zoom_Label;

    private bool _Combined_View = false;
    private bool _Normalized_View = false;

    private Instrument_Comm _Comm;
    private List<Instrument_Series> _Series = new List<Instrument_Series> ( );

    // Settings and management
    private Application_Settings _Settings;
    private GPIB_Manager _GPIB_Manager;

    // Error tracking
    private Dictionary<string, int> _Error_Counts = new Dictionary<string, int> ( );
    private Dictionary<string, DateTime> _Last_Success = new Dictionary<string, DateTime> ( );

    // Memory and performance
    private bool _Memory_Warning_Shown = false;
    private System.Windows.Forms.Timer _Chart_Refresh_Timer;
    private System.Windows.Forms.Timer _Auto_Save_Timer;

    private ToolStripStatusLabel _Memory_Status_Label;
    private ToolStripStatusLabel _Performance_Status_Label;

    private ToolTip _Chart_Tooltip;
    private Point _Last_Mouse_Position = Point.Empty;
    private DateTime _Last_Tooltip_Update = DateTime.MinValue;

    private string _Current_Graph_Style = "Line";

    private Meter_Type _Selected_Meter;

    private bool _Is_Running = false;
    private double _Zoom_Factor = 1.0;  // 1.0 = normal, >1 = zoomed in, <1 = zoomed out

    //  private System.Windows.Forms.Timer _Poll_Timer;

    private Dictionary<string, Label> _Stats_Labels = new Dictionary<string, Label> ( );
    private DateTime _Last_Legend_Update = DateTime.MinValue;

    private FlowLayoutPanel? _Legend_Panel;
    private CancellationTokenSource? _Cts;
    private List<double> _Readings = new List<double> ( );
    private int _Cycle_Count;
    private bool _Poll_Error_Shown = false;
    private bool _Is_Recording;
    private DateTime _Record_Start;
    private string _Record_Query = "";
    private List<int> _Filtered_Indices = new List<int> ( );
    private Chart_Theme _Theme = Chart_Theme.Load ( );
    private bool _Enable_Rolling = true;
    private int _Max_Display_Points = 10;


    private readonly List<DateTime> _Reading_Timestamps = new List<DateTime> ( );

    private Panel _Legend_Panel_2;
    private Button _Legend_Toggle_Button;

    private Color _Foreground_Color = Color.Black;
    private int _Paint_Count = 0;
    private double _Actual_FPS = 0;
    private readonly Stopwatch _FPS_Stopwatch = Stopwatch.StartNew ( );


    private static readonly (string Label,
      string Cmd_3458,
      string Cmd_34401,
      string Cmd_Generic_GPIB,
      string Unit) [ ] _Measurements =
   {
  ( "DC Voltage",  "DCV",   "MEAS:VOLT:DC?", "MEAS:VOLT:DC?", "V"          ),
  ( "AC Voltage",  "ACV",   "MEAS:VOLT:AC?", "MEAS:VOLT:AC?", "V"          ),
  ( "DC Current",  "DCI",   "MEAS:CURR:DC?", "MEAS:CURR:DC?", "A"          ),
  ( "AC Current",  "ACI",   "MEAS:CURR:AC?", "MEAS:CURR:AC?", "A"          ),
  ( "2-Wire Ohms", "OHM",   "MEAS:RES?",     "MEAS:RES?",     "Ohm"        ),
  ( "4-Wire Ohms", "OHMF",  "MEAS:FRES?",    "MEAS:FRES?",    "Ohm"        ),
  ( "Frequency",   "FREQ",  "MEAS:FREQ?",    "MEAS:FREQ?",    "Hz"         ),
  ( "Period",      "PER",   "MEAS:PER?",     "MEAS:PER?",     "s"          ),
  ( "Continuity",  "",      "MEAS:CONT?",    "MEAS:CONT?",    "Ohm"        ),
  ( "Diode",       "",      "MEAS:DIOD?",    "MEAS:DIOD?",    "V"          ),
  ( "Temperature", "TEMP?", "",              "",              "\u00b0C"    ),
};


    public Multi_Instrument_Poll_Form (
     Instrument_Comm Comm,
     List<(string Name, int Address, Meter_Type Meter)> Instruments,
     Application_Settings settings, Meter_Type Selected_Meter )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      InitializeComponent ( );


      // Load settings FIRST
      _Settings = settings;  // ← Use the passed settings, don't reload from disk
      NPLC_Delay_Textbox.ReadOnly = true;
      NPLC_Delay_Textbox.BackColor = SystemColors.Control;

      _Settings.Theme_Changed += ( s, e ) =>
      {
        _Theme = _Settings.Current_Theme;
        Apply_Theme_To_Current_Values_Panel ( );
        Chart_Panel.Invalidate ( );
      };

      Initialize_Current_Values_Display ( );

      _Selected_Meter = Selected_Meter;

      typeof ( Panel ).InvokeMember ( "DoubleBuffered",
        System.Reflection.BindingFlags.SetProperty |
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.NonPublic,
        null, Chart_Panel, new object [ ] { true } );

      _Comm = Comm;


      // Create series
      for ( int I = 0; I < Instruments.Count; I++ )
      {
        var Inst = Instruments [ I ];
        _Series.Add ( new Instrument_Series
        {
          Name = Inst.Name,
          Address = Inst.Address,
          Type = Inst.Meter,
          Points = new List<(DateTime Time, double Value)> ( ),
          Line_Color = _Theme.Line_Colors [ I % _Theme.Line_Colors.Length ],
          Visible = true
        } );

        // Initialize error tracking
        _Error_Counts [ Inst.Name ] = 0;
        _Last_Success [ Inst.Name ] = DateTime.Now;
      }


      // Initialize tooltip
      _Chart_Tooltip = new ToolTip ( );
      _Chart_Tooltip.AutoPopDelay = 5000;
      _Chart_Tooltip.InitialDelay = 100;
      _Chart_Tooltip.ReshowDelay = 100;
      _Chart_Tooltip.ShowAlways = true;

      // Wire up mouse move event
      Chart_Panel.MouseMove += Chart_Panel_MouseMove;


      Create_Legend_Panel ( );
      Initialize_Legend_Panel ( );

      Chart_Panel.BackColor = _Theme.Background;
      Text = $"Multi-Instrument Poller ({Instruments.Count} instruments)";

      Populate_Measurement_Combo ( );

      // CREATE TIMERS BUT DON'T START THEM
      //   _Poll_Timer = new System.Windows.Forms.Timer ( );
      //    _Poll_Timer.Tick += Poll_Timer_Tick;

      _Auto_Save_Timer = new System.Windows.Forms.Timer ( );
      _Auto_Save_Timer.Tick += Auto_Save_Timer_Tick;

      _Chart_Refresh_Timer = new System.Windows.Forms.Timer ( );
      _Chart_Refresh_Timer.Interval = 100;
      _Chart_Refresh_Timer.Tick += ( s, e ) =>
      {
        Chart_Panel.Invalidate ( );
        Update_Legend ( );
        Update_Current_Values_Display ( );
      };

      // Initialize GPIB manager with settings AND comm
      _GPIB_Manager = new GPIB_Manager ( _Settings, _Comm );


      Normalize_Button.Visible = _Combined_View;


      Update_Graph_Style_Availability ( );

      Capture_Trace.Write ( "Constructor: about to call Query_Instrument_Name" );
      Query_Instrument_Name ( );
      Capture_Trace.Write ( "Constructor: returned from Query_Instrument_Name" );

      Capture_Trace.Write ( "Constructor: about to call Initialize_Stats_Panel" );
      Initialize_Status_Panel ( );

      Capture_Trace.Write ( "Constructor: about to call Apply_Settings" );
      Apply_Settings ( );
      Capture_Trace.Write ( "Constructor: Apply_Settings complete" );


      NPLC_Textbox.Text = _Settings.Default_NPLC.ToString ( CultureInfo.InvariantCulture );

      Update_Settle_Display ( );

      Legend_Toggle_Button.Text = "Show Stats";


      Block?.Trace ( $"_Chart_Tooltip initialized: {_Chart_Tooltip != null}" );
      Block?.Trace ( $"Show_Tooltips_On_Hover = {_Settings.Show_Tooltips_On_Hover}" );
      Block?.Trace ( $"Tooltip_Distance_Threshold = {_Settings.Tooltip_Distance_Threshold}" );

    }




    private string Current_Unit
    {
      get
      {
        string Measurement = Measurement_Combo.Text.Trim ( );
        return Measurement.Contains ( "Voltage" ) ? "V"
             : Measurement.Contains ( "Current" ) ? "A"
             : Measurement.Contains ( "Resistance" ) ? "Ω"
             : Measurement.Contains ( "Frequency" ) ? "Hz"
             : Measurement.Contains ( "Temperature" ) ? "°C"
             : Measurement.Contains ( "Capacitance" ) ? "F"
             : "";
      }
    }

    private bool _Updating_Combo = false;

    private void Update_Graph_Style_Availability ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      _Updating_Combo = true;
      try
      {
        bool Multi = _Series.Count > 1;
        string Current_Selection = Graph_Style_Combo.SelectedItem?.ToString ( ) ?? "Line";
        Graph_Style_Combo.Items.Clear ( );
        Graph_Style_Combo.Items.Add ( "Line" );
        Graph_Style_Combo.Items.Add ( "Scatter" );
        Graph_Style_Combo.Items.Add ( "Step" );
        if ( !Multi )
        {
          Graph_Style_Combo.Items.Add ( "Bar" );
          Graph_Style_Combo.Items.Add ( "Histogram" );
          Graph_Style_Combo.Items.Add ( "Pie" );
        }
        if ( Graph_Style_Combo.Items.Contains ( Current_Selection ) )
          Graph_Style_Combo.SelectedItem = Current_Selection;
        else
          Graph_Style_Combo.SelectedIndex = 0;
      }
      finally
      {
        _Updating_Combo = false;
        // Sync style after rebuild is complete
        _Current_Graph_Style = Graph_Style_Combo.SelectedItem?.ToString ( ) ?? "Line";
      }
    }

    private void Graph_Style_Combo_SelectedIndexChanged ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( _Updating_Combo )
        return;
      if ( Graph_Style_Combo.SelectedItem == null )
        return;
      _Current_Graph_Style = Graph_Style_Combo.SelectedItem.ToString ( )!;
      Block?.Trace ( $"Graph style changed to: [{_Current_Graph_Style}]" );
      Chart_Panel.Invalidate ( );
    }

    private void Chart_Panel_MouseMove ( object sender, MouseEventArgs e )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );
      Block?.Trace ( $"MouseMove at ({e.X}, {e.Y})" );

      // Check if tooltips are enabled
      if ( !_Settings.Show_Tooltips_On_Hover )
      {

        Block?.Trace ( "Tooltips disabled in settings" );
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
      var (Series, Point_Index, Distance) = Find_Closest_Point ( e.Location );

      // Use settings for distance threshold
      if ( Series != null && Distance < _Settings.Tooltip_Distance_Threshold )
      {
        var Point_Data = Series.Points [ Point_Index ];

        string Tooltip_Text = $"{Series.Name}\n" +
                             $"Time: {Point_Data.Time:HH:mm:ss.fff}\n" +
                             $"Value: {Point_Data.Value:F6}";

        _Chart_Tooltip.Show ( Tooltip_Text, Chart_Panel,
                             e.Location.X + 15, e.Location.Y - 40,
                             _Settings.Tooltip_Display_Duration_Ms );
      }
      else
      {
        _Chart_Tooltip.Hide ( Chart_Panel );
      }
    }


    private (Instrument_Series Series, int Index, double Distance) Find_Closest_Point ( Point Mouse_Pos )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Instrument_Series Closest_Series = null;
      int Closest_Index = -1;
      double Min_Distance = double.MaxValue;

      int W = Chart_Panel.ClientSize.Width;
      int H = Chart_Panel.ClientSize.Height;

      // Get time range
      var (Time_Min, Time_Max) = Calculate_Time_Range ( );
      double Time_Range_Sec = ( Time_Max - Time_Min ).TotalSeconds;
      if ( Time_Range_Sec < 0.001 )
        Time_Range_Sec = 1.0;

      if ( _Combined_View )
      {
        // Combined view logic
        int Margin_Left = 80;
        int Margin_Right = 20;
        int Margin_Top = 40;
        int Margin_Bottom = 50;
        int Chart_W = W - Margin_Left - Margin_Right;
        int Chart_H = H - Margin_Top - Margin_Bottom;

        // Calculate Y range
        double Global_Min = double.MaxValue;
        double Global_Max = double.MinValue;

        foreach ( var S in _Series.Where ( s => s.Visible && s.Points.Count > 0 ) )
        {
          var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
          if ( Visible_Count == 0 )
            continue;

          for ( int i = Start_Index; i < Start_Index + Visible_Count; i++ )
          {
            if ( S.Points [ i ].Value < Global_Min )
              Global_Min = S.Points [ i ].Value;
            if ( S.Points [ i ].Value > Global_Max )
              Global_Max = S.Points [ i ].Value;
          }
        }

        if ( Global_Min == double.MaxValue )
          return (null, -1, double.MaxValue);

        double Range = Global_Max - Global_Min;
        double Padding = Math.Max ( Range * 0.5, 0.0001 );
        double Padded_Min = Global_Min - Padding;
        double Padded_Max = Global_Max + Padding;
        double Padded_Range = Padded_Max - Padded_Min;

        if ( Padded_Range == 0 )
          Padded_Range = 0.001;

        // Apply zoom if active
        double Display_Min = Padded_Min;
        double Display_Range = Padded_Range;

        if ( _Zoom_Factor > 0 && _Zoom_Factor != 1.0 )
        {
          double Center = ( Padded_Max + Padded_Min ) / 2.0;
          double Zoomed_Range = Padded_Range / _Zoom_Factor;
          Display_Min = Center - ( Zoomed_Range / 2.0 );
          double Display_Max = Center + ( Zoomed_Range / 2.0 );
          Display_Range = Display_Max - Display_Min;
        }

        // Check each series
        foreach ( var S in _Series.Where ( s => s.Visible && s.Points.Count > 0 ) )
        {
          var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
          if ( Visible_Count == 0 )
            continue;

          DateTime Visible_Time_Min = S.Points [ Start_Index ].Time;
          DateTime Visible_Time_Max = S.Points [ Start_Index + Visible_Count - 1 ].Time;
          double Visible_Time_Range_Sec = ( Visible_Time_Max - Visible_Time_Min ).TotalSeconds;
          if ( Visible_Time_Range_Sec < 0.001 )
            Visible_Time_Range_Sec = 1.0;

          for ( int i = 0; i < Visible_Count; i++ )
          {
            int Data_Index = Start_Index + i;
            var P = S.Points [ Data_Index ];

            double Time_Offset = ( P.Time - Visible_Time_Min ).TotalSeconds;
            float X_Ratio = (float) ( Time_Offset / Visible_Time_Range_Sec );
            float X = Margin_Left + ( X_Ratio * Chart_W );

            double Y_Normalized = ( P.Value - Display_Min ) / Display_Range;
            float Y = H - Margin_Bottom - (float) ( Y_Normalized * Chart_H );

            double Distance = Math.Sqrt ( Math.Pow ( Mouse_Pos.X - X, 2 ) + Math.Pow ( Mouse_Pos.Y - Y, 2 ) );

            if ( Distance < Min_Distance )
            {
              Min_Distance = Distance;
              Closest_Series = S;
              Closest_Index = Data_Index;
            }
          }
        }
      }
      else
      {
        // Split view logic
        int Margin_Left = 80;
        int Margin_Right = 20;
        int Margin_Top = 10;
        int Margin_Bottom = 30;
        int Gap = 8;

        int Chart_W = W - Margin_Left - Margin_Right;
        int Total_H = H - Margin_Top - Margin_Bottom;

        int Subplot_Count = _Series.Count ( S => S.Visible );
        int Subplot_H = ( Total_H - Gap * ( Subplot_Count - 1 ) ) / Subplot_Count;

        int SI = 0;
        foreach ( var S in _Series )
        {
          if ( !S.Visible || S.Points.Count == 0 )
            continue;

          int Sub_Top = Margin_Top + SI * ( Subplot_H + Gap );
          int Sub_Bottom = Sub_Top + Subplot_H;
          int Label_Top = Sub_Top + 18;
          int Plot_H = Sub_Bottom - Label_Top;

          // Check if mouse is in this subplot
          if ( Mouse_Pos.Y < Label_Top || Mouse_Pos.Y > Sub_Bottom )
          {
            SI++;
            continue;
          }

          var (Padded_Min, Padded_Max, Padded_Range) = Calculate_Y_Range ( S.Points );

          // Apply zoom
          double Display_Min = Padded_Min;
          double Display_Range = Padded_Range;

          if ( _Zoom_Factor > 0 && _Zoom_Factor != 1.0 )
          {
            double Center = ( Padded_Max + Padded_Min ) / 2.0;
            double Zoomed_Range = Padded_Range / _Zoom_Factor;
            Display_Min = Center - ( Zoomed_Range / 2.0 );
            double Display_Max = Center + ( Zoomed_Range / 2.0 );
            Display_Range = Display_Max - Display_Min;
          }

          var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
          if ( Visible_Count == 0 )
          {
            SI++;
            continue;
          }

          DateTime Visible_Time_Min = S.Points [ Start_Index ].Time;
          DateTime Visible_Time_Max = S.Points [ Start_Index + Visible_Count - 1 ].Time;
          double Visible_Time_Range_Sec = ( Visible_Time_Max - Visible_Time_Min ).TotalSeconds;
          if ( Visible_Time_Range_Sec < 0.001 )
            Visible_Time_Range_Sec = 1.0;

          for ( int i = 0; i < Visible_Count; i++ )
          {
            int Data_Index = Start_Index + i;
            var P = S.Points [ Data_Index ];

            double Time_Offset = ( P.Time - Visible_Time_Min ).TotalSeconds;
            float X_Ratio = (float) ( Time_Offset / Visible_Time_Range_Sec );
            float X = Margin_Left + ( X_Ratio * Chart_W );

            double Y_Normalized = ( P.Value - Display_Min ) / Display_Range;
            float Y = Sub_Bottom - (float) ( Y_Normalized * Plot_H );

            double Distance = Math.Sqrt ( Math.Pow ( Mouse_Pos.X - X, 2 ) + Math.Pow ( Mouse_Pos.Y - Y, 2 ) );

            if ( Distance < Min_Distance )
            {
              Min_Distance = Distance;
              Closest_Series = S;
              Closest_Index = Data_Index;
            }
          }

          SI++;
        }
      }

      return (Closest_Series, Closest_Index, Min_Distance);
    }





    private void Chart_Refresh_Timer_Tick ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Block?.Trace ( "Chart refresh tick - calling Invalidate()" );
      Chart_Panel.Invalidate ( );
    }


    private void Draw_Combined_Legend ( Graphics G, int W, int Margin_Top, int Margin_Right )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      int X = W - 220;
      int Y = 5;

      using ( var Font = new Font ( this.Font.FontFamily, 8f ) )
      using ( var Brush = new SolidBrush ( _Theme.Foreground ) )
      {
        int Color_Index = 0;
        foreach ( var S in _Series.Where ( s => s.Visible ) )
        {
          Color Line_Color = _Theme.Line_Colors [ Color_Index % _Theme.Line_Colors.Length ];

          // Draw color box
          using ( var Color_Brush = new SolidBrush ( Line_Color ) )
          {
            G.FillRectangle ( Color_Brush, X, Y, 14, 14 );
          }
          using ( var Border_Pen = new Pen ( _Theme.Foreground, 1f ) )
          {
            G.DrawRectangle ( Border_Pen, X, Y, 14, 14 );
          }

          // Draw name and last value
          string Display = S.Points.Count > 0
            ? $"{S.Name}: {S.Get_Last ( ):F6}"
            : S.Name;
          G.DrawString ( Display, Font, Brush, X + 20, Y );

          Y += 18;
          Color_Index++;
        }
      }
    }















    private void Draw_Split_View ( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Block?.Trace ( $"Draw_Split_View called: W={W}, H={H}" );
      Block?.Trace ( $"Total series: {_Series.Count}, Visible: {_Series.Count ( s => s.Visible )}" );

      var (Time_Min, Time_Max) = Calculate_Time_Range ( );
      double Time_Range_Sec = ( Time_Max - Time_Min ).TotalSeconds;

      Block?.Trace ( $"Time range: {Time_Range_Sec:F2} seconds" );


      if ( Time_Range_Sec < 0.001 )
        Time_Range_Sec = 1.0;

      int Margin_Left = 80;
      int Margin_Right = 20;
      int Margin_Top = 10;
      int Margin_Bottom = 30;
      int Gap = 8;

      int Chart_W = W - Margin_Left - Margin_Right;
      int Total_H = H - Margin_Top - Margin_Bottom;

      int Subplot_Count = _Series.Count ( S => S.Visible );
      int Subplot_H = ( Total_H - Gap * ( Subplot_Count - 1 ) ) / Subplot_Count;

      if ( Chart_W < 10 || Subplot_H < 30 )
        return;

      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Sep_Pen = new Pen ( _Theme.Separator, 1f );
      Sep_Pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

      using var Label_Font = new Font ( "Consolas", 7.5F );
      using var Name_Font = new Font ( "Segoe UI", 8F, FontStyle.Bold );
      using var Label_Brush = new SolidBrush ( _Theme.Labels );

      int SI = 0;
      for ( int I = 0; I < _Series.Count; I++ )
      {
        var S = _Series [ I ];
        if ( !S.Visible )
          continue;

        int Sub_Top = Margin_Top + SI * ( Subplot_H + Gap );
        int Sub_Bottom = Sub_Top + Subplot_H;

        Draw_Subplot (
          G, S, SI,
          Sub_Top, Sub_Bottom,
          Margin_Left, Margin_Right,
          W, Time_Min, Time_Range_Sec, Chart_W,
          Grid_Pen, Sep_Pen, Label_Font, Name_Font, Label_Brush );

        SI++;
      }

      // Draw time axis - skip for styles that don't use time
      if ( _Current_Graph_Style != "Histogram" && _Current_Graph_Style != "Pie" )
      {
        Draw_Time_Axis (
          G, H, Chart_W, Time_Range_Sec,
          Margin_Left, Margin_Bottom, Margin_Top,
          Grid_Pen, Label_Brush );
      }
    }




    private void Draw_Y_Axis ( Graphics G, double Min, double Range, int W, int H,
      int Chart_H, int Margin_Left, int Margin_Right, int Margin_Bottom,
      Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );



      // Draw 5 horizontal grid lines and labels
      for ( int I = 0; I <= 4; I++ )
      {
        float Y_Ratio = I / 4f;
        float Y = H - Margin_Bottom - ( Y_Ratio * Chart_H );

        // Grid line
        G.DrawLine ( Grid_Pen, Margin_Left, Y, W - Margin_Right, Y );

        // Label
        double Value = Min + ( Range * Y_Ratio );
        string Label = Format_Axis_Value ( Value );

        SizeF Label_Size = G.MeasureString ( Label, Label_Font );
        G.DrawString ( Label, Label_Font, Label_Brush,
          Margin_Left - Label_Size.Width - 5, Y - Label_Size.Height / 2 );
      }
    }








    private PointF [ ] Build_Point_Array_Combined (
      Instrument_Series Series,
      DateTime Start_Time,
      TimeSpan Duration,
      double Padded_Min,
      double Padded_Range,
      int Margin_Left,
      int Margin_Bottom,
      int Chart_W,
      int Chart_H,
      int H )
    {
      if ( Series.Points.Count == 0 )
        return Array.Empty<PointF> ( );

      var Points = new PointF [ Series.Points.Count ];

      for ( int I = 0; I < Series.Points.Count; I++ )
      {
        var P = Series.Points [ I ];

        // X position based on time
        double Time_Offset_Seconds = ( P.Time - Start_Time ).TotalSeconds;
        float X_Ratio = (float) ( Time_Offset_Seconds / Duration.TotalSeconds );
        float X = Margin_Left + ( X_Ratio * Chart_W );

        // Y position based on value
        double Normalized = ( P.Value - Padded_Min ) / Padded_Range;
        float Y = H - Margin_Bottom - (float) ( Normalized * Chart_H );

        Points [ I ] = new PointF ( X, Y );
      }

      return Points;
    }

    private void Draw_Mini_Legend ( Graphics G, int W, int Margin_Top, int Margin_Right )
    {
      int X = W - 200;
      int Y = Margin_Top;

      using ( var Font = new Font ( this.Font.FontFamily, 8f ) )
      using ( var Brush = new SolidBrush ( _Theme.Foreground ) )
      {
        int Series_Index = 0;
        foreach ( var S in _Series.Where ( s => s.Visible ) )
        {
          Color Line_Color = _Theme.Line_Colors [ Series_Index % _Theme.Line_Colors.Length ];

          // Draw color box
          using ( var Color_Brush = new SolidBrush ( Line_Color ) )
          {
            G.FillRectangle ( Color_Brush, X, Y, 12, 12 );
          }

          // Draw name
          string Display = S.Points.Count > 0
            ? $"{S.Name}: {S.Get_Last ( ):F6}"
            : S.Name;
          G.DrawString ( Display, Font, Brush, X + 18, Y - 2 );

          Y += 18;
          Series_Index++;
        }
      }
    }


    private void Auto_Save_Timer_Tick ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Is_Running && _Series.Any ( s => s.Points.Count > 0 ) )
      {
        // Auto-save in background
        try
        {
          string Folder = _Settings.Default_Save_Folder;
          Directory.CreateDirectory ( Folder );

          string Timestamp = DateTime.Now.ToString ( "yyyy-MM-dd_HH-mm-ss" );
          string File_Name = $"{Timestamp}_Multi_AutoSave.csv";
          string File_Path = Path.Combine ( Folder, File_Name );

          Save_To_File ( File_Path );

          Show_Progress ( $"Auto-saved: {File_Name}", _Foreground_Color );

        }
        catch ( Exception ex )
        {
          Show_Progress ( $"Auto-save failed: {ex.Message}", _Foreground_Color );

        }
      }
    }


    protected override void OnFormClosing ( FormClosingEventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Only do cleanup here if Close_Button didn't already handle it
      if ( _Is_Running || _Is_Recording )
      {
        Capture_Trace.Write ( "Cleanup needed (Close_Button was not used)" );
        // _Poll_Timer?.Stop ( );
        _Auto_Save_Timer?.Stop ( );
        _Chart_Refresh_Timer?.Stop ( );
        _Chart_Tooltip?.Dispose ( );  // ADD THIS
        Stop_Polling ( );
        // Set_Local_Mode ( );
        base.OnFormClosing ( E );
      }
      else
      {
        Capture_Trace.Write ( "Already cleaned up, skipping" );
      }

      base.OnFormClosing ( E );
    }




    private void Stop_Recording_Due_To_Memory_Limit ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      _Is_Running = false;
      Start_Stop_Button.Text = "Start";
      //  _Poll_Timer.Stop ( );

      var Result = MessageBox.Show (
        $"Maximum data points ({_Settings.Max_Data_Points_In_Memory:N0}) reached.\n" +
        $"Total across all instruments: {_Series.Sum ( s => s.Points.Count ):N0}\n\n" +
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

    private void Show_Memory_Warning ( int current, int max )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      int Percent = ( current * 100 ) / max;

      var Result = MessageBox.Show (
        $"Memory usage is at {Percent}% of the limit.\n\n" +
        $"Current: {current:N0} points across {_Series.Count} instruments\n" +
        $"Limit: {max:N0} points\n\n" +
        "Continue recording?",
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
        _Is_Running = false;
        Start_Stop_Button.Text = "Start";
        //     _Poll_Timer.Stop ( );
        Save_Recorded_Data ( );
      }
      else
      {
        // Cancel - just stop
        _Is_Running = false;
        Start_Stop_Button.Text = "Start";
        //    _Poll_Timer.Stop ( );
      }
    }

    private void Update_Memory_Status ( int current, int max )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );
      int Percent = ( current * 100 ) / max;

      if ( _Memory_Status_Label != null )
      {
        _Memory_Status_Label.Text = $"Memory: {current:N0} / {max:N0} ({Percent}%)";

        /*
        if ( Percent >= 90 )
          _Memory_Status_Label.BackColor = Color.Red;
        else if ( Percent >= _Settings.Warning_Threshold_Percent )
          _Memory_Status_Label.BackColor = Color.Yellow;
        else
          _Memory_Status_Label.BackColor = Color.Green;
        */
      }
    }



    private void Update_Performance_Status ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Multimeter_Common_Helpers_Class.Update_Performance_Status (
        _Performance_Status_Label,
        _Memory_Status_Label,
        _Actual_FPS,
        _Series.Sum ( s => s.Points.Count ),
        _Series.Sum ( s => s.Points.Count ),
        _Settings.Max_Data_Points_In_Memory,
        _Settings.Warning_Threshold_Percent );
    }





    private void Initialize_Legend_Panel ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );


      // Create legend panel only (button is in designer now)
      _Legend_Panel_2 = new Panel
      {
        AutoScroll = true,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = _Theme.Background,
        Dock = DockStyle.None,
        Width = 200,
        Visible = false  // Start hidden
      };

      this.Controls.Add ( _Legend_Panel_2 );
      Position_Legend_Panel ( );
      Update_Legend ( );
    }

    private void Legend_Toggle_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Legend_Panel_2.Visible = !_Legend_Panel_2.Visible;

      // Update button text
      Legend_Toggle_Button.Text = _Legend_Panel_2.Visible ? "Hide Stats" : "Show Stats";

      // Reposition in case window was resized
      if ( _Legend_Panel_2.Visible )
      {
        Position_Legend_Panel ( );
        _Legend_Panel_2.BringToFront ( );
      }
    }

    private void Position_Legend_Panel ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Find the chart panel's position
      int Chart_Top = Chart_Panel.Top;
      int Chart_Right = Chart_Panel.Right;
      int Chart_Height = Chart_Panel.Height;

      // Position legend panel at the right edge, aligned with chart top
      _Legend_Panel_2.Location = new Point (
          Chart_Right - _Legend_Panel_2.Width - 10,
          Chart_Top );
      _Legend_Panel_2.Height = Math.Min ( 250, Chart_Height );
    }

    private void Chart_Panel_Resize ( object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Legend_Panel_2 != null && _Legend_Panel_2.Visible )
      {
        Position_Legend_Panel ( );
      }

      Chart_Panel.Invalidate ( );
    }




    private void Create_Legend_Panel ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Remove old panel if exists
      if ( _Legend_Panel != null )
      {
        Controls.Remove ( _Legend_Panel );
        _Legend_Panel.Dispose ( );
      }

      _Legend_Panel = new FlowLayoutPanel
      {
        Height = 30,
        Dock = DockStyle.Top,
        AutoScroll = false,
        WrapContents = false,
        Padding = new Padding ( 10, 5, 10, 5 )
      };

      // Add label
      var Lbl = new Label
      {
        Text = "Show:",
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding ( 0, 5, 10, 0 )
      };
      _Legend_Panel.Controls.Add ( Lbl );

      // Add checkbox for each series
      for ( int I = 0; I < _Series.Count; I++ )
      {
        var S = _Series [ I ];
        var Cb = new CheckBox
        {
          Text = $"{S.Name} (GPIB {S.Address})",
          Checked = S.Visible,
          AutoSize = true,
          Margin = new Padding ( 5, 3, 15, 0 ),
          Tag = I  // Store index
        };

        Cb.CheckedChanged += Legend_CheckBox_Changed;
        _Legend_Panel.Controls.Add ( Cb );
      }

      Controls.Add ( _Legend_Panel );
      _Legend_Panel.BringToFront ( );
    }




    private void Continuous_Check_Changed (
      object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      bool Checked = Continuous_Check.Checked;
      Cycles_Label.Enabled = !Checked;
      Cycles_Numeric.Enabled = !Checked && !_Is_Running;
    }



    private void Update_Settle_Display ( )
    {
      int Settle_Ms = Multimeter_Common_Helpers_Class.Calculate_Settle_Ms (
        NPLC_Textbox.Text.Trim ( ) );
      NPLC_Delay_Textbox.Text = $"{Settle_Ms} ms";
    }




    private async void Start_Polling ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Comm.Is_Connected )
      {
        MessageBox.Show (
            "Not connected. Please connect first.",
            "Connection Required",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning );
        return;
      }

      if ( _Series.Count == 0 )
      {
        MessageBox.Show (
            "No instruments in the list.\n" +
            "Add instruments on the main form first.",
            "No Instruments",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning );
        return;
      }



      int Combo_Index = Measurement_Combo.SelectedIndex;
      if ( Combo_Index < 0 || Combo_Index >= _Filtered_Indices.Count )
        return;

      int Func_Index = _Filtered_Indices [ Combo_Index ];
      var Selected = _Measurements [ Func_Index ];

      string Command = Measurement_Combo.Text.Trim ( );
      Capture_Trace.Write ( $"Starting poll with command: '{Command}'" );

      if ( string.IsNullOrEmpty ( Command ) )
        return;

      // Determine command and unit for first instrument (they should all be same type)
      string Configure_Cmd = _Series.Count > 0 && _Series [ 0 ].Type == Meter_Type.HP34401
          ? Selected.Cmd_34401
          : Selected.Cmd_3458;

      string Unit = Selected.Unit;
      bool Is_Query_Mode = Configure_Cmd.EndsWith ( "?" );

      Capture_Trace.Write ( $"Configure command: {Configure_Cmd}" );
      Capture_Trace.Write ( $"Unit             : {Unit}" );
      Capture_Trace.Write ( $"Query mode       : {Is_Query_Mode}" );

      _Is_Running = true;
      _Cts = new CancellationTokenSource ( );
      _Cycle_Count = 0;
      Start_Stop_Button.Text = "Stop";
      Show_Progress ( "Polling...", _Foreground_Color );


      Delay_Numeric.Enabled = true;

      Measurement_Combo.Enabled = false;
      Continuous_Check.Enabled = false;
      Cycles_Numeric.Enabled = false;
      Clear_Button.Enabled = false;
      Load_Button.Enabled = false;


      bool Continuous = Continuous_Check.Checked;
      int Total_Cycles = (int) Cycles_Numeric.Value;
      int Original_Address = _Comm.GPIB_Address;
      CancellationToken Token = _Cts.Token;

      // Get NPLC value
      string NPLC_Value = NPLC_Textbox.Text.Trim ( );

      // Track configuration state per instrument
      bool [ ] Configured = new bool [ _Series.Count ];

      _Comm.Instrument_Settle_Ms = Multimeter_Common_Helpers_Class.Calculate_Settle_Ms ( NPLC_Value );
      NPLC_Delay_Textbox.Text = _Comm.Instrument_Settle_Ms.ToString ( );

      try
      {
        // Suppress error dialogs during polling
        _Comm.Error_Occurred -= On_Poll_Error;
        _Comm.Error_Occurred += On_Poll_Error;

        // ========================================
        // PHASE 1: CONFIGURE ALL INSTRUMENTS
        // ========================================
        Show_Progress ( "Setting up measurements", _Foreground_Color );
        for ( int I = 0; I < _Series.Count; I++ )
        {
          Token.ThrowIfCancellationRequested ( );
          var S = _Series [ I ];
          Capture_Trace.Write ( $"" );
          Capture_Trace.Write ( $"=== Configuring {S.Name} (GPIB {S.Address}) ===" );

          // Switch to this instrument
          await Task.Run ( ( ) => _Comm.Change_GPIB_Address ( S.Address ), Token );
          await Task.Delay ( 50, Token );

          // Set ++auto 0 for ALL instruments
          Capture_Trace.Write ( $"Setting ++auto 0 for {S.Type}" );
          await Task.Run ( ( ) => _Comm.Send_Prologix_Command ( "++auto 0" ), Token );
          await Task.Delay ( 50, Token );

          // Clear GPIB state
          await Task.Run ( ( ) => _Comm.Send_Prologix_Command ( "++clr" ), Token );
          await Task.Delay ( 100, Token );

          // Configure NPLC
          if ( S.Type != Meter_Type.Generic_GPIB )
          {
            string NPLC_Cmd = S.Type == Meter_Type.HP34401
                ? $"VOLT:DC:NPLC {NPLC_Value}"
                : $"NPLC {NPLC_Value}";

            Capture_Trace.Write ( $"Sending NPLC command: {NPLC_Cmd}" );
            await Task.Run ( ( ) => _Comm.Send_Instrument_Command ( NPLC_Cmd ), Token );
            await Task.Delay ( 200, Token );
          }

          // Configure measurement function
          if ( S.Type == Meter_Type.HP3458 )
          {
            string Config_Command = Get_Command_For_Series ( S, Selected );
            Capture_Trace.Write ( $"Sending config command: {Config_Command} to {S.Type}" );
            await Task.Run ( ( ) => _Comm.Send_Instrument_Command ( Config_Command ), Token );
            await Task.Delay ( 200, Token );


            // Restart trigger after TRIG HOLD from verify
            await Task.Run ( ( ) => _Comm.Send_Instrument_Command ( "TRIG AUTO" ), Token );
            await Task.Delay ( 100, Token );

            // Prime - wait for first reading so buffer is ready for poll loop
            Capture_Trace.Write ( $"Priming 3458..." );
            await Task.Run ( ( ) => _Comm.Raw_Write_Prologix ( "++read eoi" ), Token );


            string Prime = await Task.Run ( ( ) =>
                _Comm.Read_Instrument ( Token ), Token ) ?? "";
            Capture_Trace.Write ( $"Prime response: {Prime.Split ( '\n' ) [ 0 ]}" );
            await Task.Delay ( 50, Token );
          }
          else if ( S.Type == Meter_Type.HP34401 )
          {
            // Get the selected measurement label (e.g. "DC Voltage")
            string Measurement_Label = Measurement_Combo.Text.Trim ( );

            // First, configure the function
            string Conf_Cmd = Get_Command_For_Series ( S, Selected );

            // Convert MEAS: to CONF: so it configures without triggering a read
            if ( Conf_Cmd.StartsWith ( "MEAS:" ) )
              Conf_Cmd = "CONF:" + Conf_Cmd.Substring ( 5 );

            Capture_Trace.Write ( $"Sending CONF command: {Conf_Cmd} to HP34401" );
            await Task.Run ( ( ) => _Comm.Send_Instrument_Command ( Conf_Cmd ), Token );
            await Task.Delay ( 200, Token );

            // Then set NPLC if supported for this function


            string? NPLC_Cmd = Multimeter_Common_Helpers_Class.Build_NPLC_Command ( Measurement_Label, NPLC_Value );

            if ( NPLC_Cmd != null )
            {
              Capture_Trace.Write ( $"Sending NPLC command: {NPLC_Cmd} to HP34401" );
              await Task.Run ( ( ) => _Comm.Send_Instrument_Command ( NPLC_Cmd ), Token );
              await Task.Delay ( 200, Token );
            }
            else
            {
              Capture_Trace.Write ( $"NPLC not applicable for {Measurement_Label} on HP34401, skipping" );
            }
          }
          else
          {
            string Config_Command = Get_Command_For_Series ( S, Selected );
            Capture_Trace.Write ( $"Sending config command: {Config_Command} to {S.Type}" );
            await Task.Run ( ( ) => _Comm.Send_Instrument_Command ( Config_Command ), Token );
            await Task.Delay ( 200, Token );
          }

          Configured [ I ] = true;
          Capture_Trace.Write ( $"{S.Name} configured successfully" );
        }

        Show_Progress ( "Polling...", _Foreground_Color );
        await Task.Delay ( 300, Token );

        // ========================================
        // PHASE 2: POLLING LOOP
        // ========================================

        this.Invoke ( ( ) => Initialize_Current_Values_Display ( ) );

        int Previous_Address = -1;

        while ( !Token.IsCancellationRequested &&
               ( Continuous || _Cycle_Count < Total_Cycles ) )
        {
          _Cycle_Count++;
          DateTime Cycle_Time = DateTime.Now;

          Capture_Trace.Write ( $"" );
          Capture_Trace.Write ( $"╔═══════════════════════════════════════╗" );
          Capture_Trace.Write ( $"║  CYCLE {_Cycle_Count}" );
          Capture_Trace.Write ( $"╠═══════════════════════════════════════╣" );

          for ( int I = 0; I < _Series.Count; I++ )
          {
            Token.ThrowIfCancellationRequested ( );

            var S = _Series [ I ];
            Show_Progress ( $"Cycle {_Cycle_Count}: querying {S.Name} (GPIB {S.Address})", _Foreground_Color );

            Capture_Trace.Write ( $"║  Reading {S.Name} (GPIB {S.Address})" );

            // Switch address if needed
            if ( S.Address != Previous_Address )
            {
              Capture_Trace.Write ( $"║  Switching to GPIB address {S.Address}" );
              await Task.Run ( ( ) => _Comm.Change_GPIB_Address ( S.Address ), Token );
              await Task.Delay ( 50, Token );
              await Task.Run ( ( ) => _Comm.Send_Prologix_Command ( "++auto 0" ), Token );
              await Task.Delay ( 50, Token );
              // NO ++clr here - it kills the measurement queue
              Previous_Address = S.Address;
            }

            // Read measurement
            string Response;
            if ( S.Type == Meter_Type.HP3458 )
            {
              Capture_Trace.Write ( $"║  3458: ++read eoi" );
              await Task.Run ( ( ) => _Comm.Raw_Write_Prologix ( "++read eoi" ), Token );
              Response = await Task.Run ( ( ) =>
                  _Comm.Read_Instrument ( Token ), Token ) ?? "";
            }
            else if ( S.Type == Meter_Type.HP34401 )
            {
              Capture_Trace.Write ( $"║  34401: READ?" );
              Response = await Task.Run ( ( ) =>
                  _Comm.Query_Instrument ( "READ?", Token ), Token );
            }
            else
            {
              string Instrument_Command = Get_Command_For_Series ( S, Selected );
              Capture_Trace.Write ( $"║  Sending query: {Instrument_Command} to {S.Type}" );
              Response = await Task.Run ( ( ) =>
                  _Comm.Query_Instrument ( Instrument_Command, Token ), Token );
            }

            Capture_Trace.Write ( $"║  Response: {Response}" );
            Token.ThrowIfCancellationRequested ( );

            if ( double.TryParse ( Response,
             System.Globalization.NumberStyles.Float,
             System.Globalization.CultureInfo.InvariantCulture,
             out double Value ) )
            {
              S.Points.Add ( (DateTime.Now, Value) );
              Capture_Trace.Write ( $"║  Parsed value: {Value}" );
            }
            else
            {
              Capture_Trace.Write ( $"║  ERROR: Could not parse response" );
            }
          }

          Capture_Trace.Write ( $"╚═══════════════════════════════════════╝" );

          // Update current values grid
          this.Invoke ( ( ) => Update_Current_Values_Display ( ) );

          // Update display
          if ( Continuous )
            Cycle_Text_Box.Text = $"Cycle {_Cycle_Count}  (Continuous)  [{_Actual_FPS} S/s]";
          else
            Cycle_Text_Box.Text = $"Cycle {_Cycle_Count} of {Total_Cycles}";

          // Chart_Panel.Invalidate ( );

          Update_Legend ( );

          // Delay before next cycle
          // Delay before next cycle - read live so user can adjust during polling
          bool Has_More = Continuous || _Cycle_Count < Total_Cycles;
          if ( Has_More )
          {
            int Delay_Ms = (int) ( Delay_Numeric.Value );
            await Task.Delay ( Delay_Ms, Token );
          }
        }
      }
      catch ( OperationCanceledException )
      {
        Capture_Trace.Write ( "Polling cancelled by user" );
      }
      catch ( TimeoutException Ex )
      {
        Capture_Trace.Write ( $"Timeout error: {Ex.Message}" );

        if ( !_Poll_Error_Shown )
        {
          _Poll_Error_Shown = true;
          _Cts?.Cancel ( );  // ← Force stop BEFORE showing dialog

          MessageBox.Show (
              "Operation timed out. Check instrument connection and GPIB bus status.",
              "Timeout Error",
              MessageBoxButtons.OK,
              MessageBoxIcon.Warning );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Polling error: {Ex.GetType ( ).Name} - {Ex.Message}" );

        if ( !_Poll_Error_Shown )
        {
          _Poll_Error_Shown = true;
          _Cts?.Cancel ( );  // ← Force stop BEFORE showing dialog

          MessageBox.Show (
              $"Polling error: {Ex.Message}",
              "Error",
              MessageBoxButtons.OK,
              MessageBoxIcon.Error );
        }
      }

      finally
      {
        // Cleanup
        _Comm.Error_Occurred -= On_Poll_Error;

        // Restore original address
        try
        {
          _Comm.Change_GPIB_Address ( Original_Address );
        }
        catch
        {
          // Ignore errors during cleanup
        }

        Finish_Polling ( );
      }
    }



    private string Get_Command_For_Series ( Instrument_Series S,
      (string Label, string Cmd_3458, string Cmd_34401, string Cmd_Generic_GPIB, string Unit) Entry )
    {
      return S.Type switch
      {
        Meter_Type.HP3458 => Entry.Cmd_3458,
        Meter_Type.HP34401 => Entry.Cmd_34401,
        _ => Entry.Cmd_Generic_GPIB
      };
    }
























    private void Stop_Polling ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Cts?.Cancel ( );
    }

    // Swallow errors during polling so that Raise_Error
    // does not deadlock by calling MessageBox via Invoke
    // while the UI thread is awaiting Task.Run.
    private void On_Poll_Error ( object? Sender, string Message )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );
      // Log to Progress_Label instead of blocking with a dialog
      if ( InvokeRequired )
      {
        BeginInvoke ( ( ) =>
          Show_Progress ( $"Error: {Message}", _Foreground_Color ) );
      }
      else
      {
        Show_Progress ( $"Error: {Message}", _Foreground_Color );
      }
    }

    private void Finish_Polling ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      _Is_Running = false;
      _Poll_Error_Shown = false;

      _Cts?.Dispose ( );
      _Cts = null;
      Start_Stop_Button.Text = "Start";
      Show_Progress ( "Idle", _Foreground_Color );

      Measurement_Combo.Enabled = true;
      Delay_Numeric.Enabled = true;


      Continuous_Check.Enabled = true;
      Cycles_Numeric.Enabled = !Continuous_Check.Checked;
      Clear_Button.Enabled = true;
      Load_Button.Enabled = true;

      Rolling_Check.Enabled = true;
      Max_Points_Numeric.Enabled = _Enable_Rolling;




      Update_Legend ( );

    }

    private string Translate_Command_For_Instrument (
      string Base_Command, Meter_Type Meter )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Find the measurement that matches the base command
      foreach ( var Measurement in _Measurements )
      {
        if ( Measurement.Cmd_3458 == Base_Command )
        {
          // Translate to the appropriate command for
          // this meter type
          if ( Meter == Meter_Type.HP34401 )
          {
            string Cmd = Measurement.Cmd_34401;
            if ( !string.IsNullOrEmpty ( Cmd ) )
            {
              // 34401 needs READ? for configured
              // measurements
              if ( !Cmd.EndsWith ( "?" ) )
              {
                return "READ?";
              }
              return Cmd;
            }
          }

          // Default to 3458 command
          return Base_Command;
        }
      }

      // If not found in measurement list,
      // return as-is (custom command)
      return Base_Command;
    }

    private string Get_Config_Command_For_34401 (
      string Base_Command )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Find the measurement that matches the base command
      foreach ( var Measurement in _Measurements )
      {
        if ( Measurement.Cmd_3458 == Base_Command )
        {
          return Measurement.Cmd_34401;
        }
      }

      return Base_Command;
    }

    private void Clear_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Is_Running )
      {
        return;
      }

      // Check if we should prompt
      if ( _Settings.Prompt_Before_Clear && _Series.Any ( s => s.Points.Count > 0 ) )
      {
        var Result = MessageBox.Show (
          "Clear all data?\n\nThis cannot be undone.",
          "Clear Data",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Question );

        if ( Result != DialogResult.Yes )
          return;
      }

      foreach ( var S in _Series )
      {
        S.Points.Clear ( );
      }

      _Cycle_Count = 0;
      Cycle_Text_Box.Text = "";

      Show_Progress ( "", _Foreground_Color );

      Update_Legend ( );
      Update_Graph_Style_Availability ( );
      Chart_Panel.Invalidate ( );
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

      _Record_Query = Measurement_Combo.Text.Trim ( );
      _Record_Start = DateTime.Now;

      foreach ( var S in _Series )
        S.Points.Clear ( );

      Multimeter_Common_Helpers_Class.Check_Memory_Limit (
        _Settings,
        ( ) => _Series.Sum ( s => s.Points.Count ),
        Stop_Recording,
        Show_Memory_Warning,
        ref _Memory_Warning_Shown );

      Update_Performance_Status ( );

      _Is_Recording = true;
      Multimeter_Common_Helpers_Class.Start_Recording_UI ( Record_Button );  // ← replaces 3 lines
    }

    private void Stop_Recording ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Multimeter_Common_Helpers_Class.Stop_Recording (
        ref _Is_Recording,
        Record_Button,
        ( ) => _Series.Sum ( s => s.Points.Count ),
        Save_Recorded_Data );
    }

    private void Save_Recorded_Data ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Series.Count == 0 || _Series.All ( s => s.Points.Count == 0 ) )
        return;

      string File_Path = Path.Combine (
        Multimeter_Common_Helpers_Class.Get_Graph_Captures_Folder ( ),
        $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_Multi.csv" );

      var Series_Data = _Series
        .Select ( s => (s.Name, s.Points) )
        .ToList ( );

      Multimeter_Common_Helpers_Class.Save_Multi_Series_CSV ( File_Path, Series_Data );

      int Total_Points = _Series.Sum ( s => s.Points.Count );
      string Summary = $"Saved {_Series.Count} instruments, {Total_Points} total points to:\n{File_Path}\n\n";

      foreach ( var S in _Series.Where ( s => s.Points.Count > 0 ) )
        Summary += $"{S.Name}: {S.Points.Count} pts, Avg: {S.Get_Average ( ):F7}\n";

      MessageBox.Show ( Summary, "Recording Saved",
        MessageBoxButtons.OK, MessageBoxIcon.Information );
    }




    private void Load_Recorded_File ( string File_Path )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !Multimeter_Common_Helpers_Class.Load_CSV_Preamble (
          File_Path, out var Lines, out int Header_Index,
          out _, out var Sectioned_Stats ) )
        return;

      string [ ] Headers = Lines [ Header_Index ].Split ( ',' );
      int Col_Count = Headers.Length - 1;

      if ( Col_Count <= 0 )
      {
        MessageBox.Show ( "No instrument columns found.", "Load Error",
          MessageBoxButtons.OK, MessageBoxIcon.Warning );
        return;
      }

      _Series.Clear ( );

      for ( int I = 0; I < Col_Count; I++ )
      {
        string Name = Headers [ I + 1 ].Trim ( );
        int Address = 0;

        if ( Name.Contains ( "@" ) )
        {
          string [ ] Parts = Name.Split ( '@' );
          if ( Parts.Length >= 2 )
          {
            string Addr_Part = Parts [ 1 ].Trim ( );
            if ( Addr_Part.Contains ( "(" ) )
              Addr_Part = Addr_Part.Substring ( 0, Addr_Part.IndexOf ( "(" ) ).Trim ( );
            string Addr_Str = new string ( Addr_Part.Where ( char.IsDigit ).ToArray ( ) );
            int.TryParse ( Addr_Str, out Address );
          }
        }

        _Series.Add ( new Instrument_Series
        {
          Name = Name,
          Address = Address,
          Type = Meter_Type.HP3458,
          Points = new List<(DateTime Time, double Value)> ( ),
          Line_Color = _Theme.Line_Colors [ I % _Theme.Line_Colors.Length ],
          File_Stats = Sectioned_Stats.ContainsKey ( Name ) ? Sectioned_Stats [ Name ] : null
        } );
      }

      // Parse data rows
      for ( int I = Header_Index + 1; I < Lines.Length; I++ )
      {
        string Line = Lines [ I ].Trim ( );
        if ( string.IsNullOrEmpty ( Line ) || Line.StartsWith ( "#" ) )
          continue;

        string [ ] Parts = Line.Split ( ',' );
        if ( Parts.Length < 2 || !DateTime.TryParse ( Parts [ 0 ], out DateTime T ) )
          continue;

        for ( int J = 1; J < Parts.Length && J - 1 < _Series.Count; J++ )
        {
          if ( double.TryParse ( Parts [ J ],
              NumberStyles.Float, CultureInfo.InvariantCulture, out double Val ) )
            _Series [ J - 1 ].Points.Add ( (T, Val) );
        }
      }

      Multimeter_Common_Helpers_Class.Check_Memory_Limit (
    _Settings,
    ( ) => _Series.Sum ( s => s.Points.Count ),
    ( ) =>
    {
      Stop_Recording ( );
      if ( InvokeRequired )
        this.Invoke ( ( ) => Update_Graph_Style_Availability ( ) );
      else
        Update_Graph_Style_Availability ( );
    },
    Show_Memory_Warning, ref _Memory_Warning_Shown );

      int Total = _Series.Sum ( s => s.Points.Count );
      Show_Progress ( $"Loaded {_Series.Count} instruments, {Total} points", _Foreground_Color );

      Update_Performance_Status ( );
      Update_Legend ( );

      Multimeter_Common_Helpers_Class.Update_Scrollbar_Range (
        Pan_Scrollbar, _Series.Max ( s => s.Points.Count ),
        _Max_Display_Points, _Auto_Scroll, ref _View_Offset );

      Update_Graph_Style_Availability ( );
      Chart_Panel.Invalidate ( );
    }


    private void Chart_Panel_Paint ( object? sender, PaintEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Multimeter_Common_Helpers_Class.Track_FPS (
        ref _Paint_Count, ref _Actual_FPS,
        _FPS_Stopwatch, Update_Performance_Status );

      Graphics G = e.Graphics;
      G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

      int W = Chart_Panel.ClientSize.Width;
      int H = Chart_Panel.ClientSize.Height;

      using var Bg_Brush = new SolidBrush ( _Theme.Background );
      G.FillRectangle ( Bg_Brush, 0, 0, W, H );

      if ( _Series.Count == 0 )
      {
        Draw_Empty_State ( G, W, H,
          "No instruments. Add instruments and press Start." );
        return;
      }

      bool Has_Data = _Series.Any ( s => s.Visible && s.Points.Count > 0 );

      if ( !Has_Data )
      {
        Draw_Instrument_List ( G, H );
        return;
      }

      if ( _Combined_View || _Current_Graph_Style == "Pie" )
        Draw_Combined_View ( G, W, H );
      else
        Draw_Split_View ( G, W, H );

      Draw_Position_Indicator ( e.Graphics, W, H );

      Block?.Trace ( "Paint complete" );
    }






    private void Draw_Multi_Line ( Graphics G,
  int W, int H, int Margin_Left, int Margin_Right,
  int Margin_Top, int Margin_Bottom,
  int Chart_W, int Chart_H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      var (Min_V, Max_V) = Get_Y_Range ( );
      float Baseline = H - Margin_Bottom;
      Draw_Grid_And_Axes ( G, W, H, Margin_Left, Margin_Right,
        Margin_Top, Margin_Bottom, Chart_W, Chart_H, Min_V, Max_V );

      for ( int S_Idx = 0; S_Idx < _Series.Count; S_Idx++ )
      {
        var S = _Series [ S_Idx ];
        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
          S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

        int Actual_Count = Math.Min ( Visible_Count, S.Points.Count - Start_Index );
        if ( Actual_Count < 1 )
          continue;

        PointF [ ] Points = Build_Points ( S.Points, Start_Index, Actual_Count,
          Margin_Left, Chart_W, Chart_H, Margin_Top, Min_V, Max_V );

        Color Line_Color = _Theme.Line_Colors [ S_Idx % _Theme.Line_Colors.Length ];
        using var Line_Pen = new Pen ( Line_Color, 1.5f );
        using var Dot_Brush = new SolidBrush ( Line_Color );

        Draw_Line_Chart ( G, Points, Actual_Count,
          Line_Pen, Dot_Brush, Margin_Top, Baseline );
      }
    }


    private void Draw_Multi_Scatter ( Graphics G,
      int W, int H, int Margin_Left, int Margin_Right,
      int Margin_Top, int Margin_Bottom,
      int Chart_W, int Chart_H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      var (Min_V, Max_V) = Get_Y_Range ( );
      Draw_Grid_And_Axes ( G, W, H, Margin_Left, Margin_Right,
        Margin_Top, Margin_Bottom, Chart_W, Chart_H, Min_V, Max_V );

      for ( int S_Idx = 0; S_Idx < _Series.Count; S_Idx++ )
      {
        var S = _Series [ S_Idx ];
        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
          S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

        int Actual_Count = Math.Min ( Visible_Count, S.Points.Count - Start_Index );
        if ( Actual_Count < 1 )
          continue;

        PointF [ ] Points = Build_Points ( S.Points, Start_Index, Actual_Count,
          Margin_Left, Chart_W, Chart_H, Margin_Top, Min_V, Max_V );

        Color Line_Color = _Theme.Line_Colors [ S_Idx % _Theme.Line_Colors.Length ];
        using var Dot_Brush = new SolidBrush ( Line_Color );

        Draw_Scatter_Chart ( G, Points, Actual_Count, Dot_Brush );
      }
    }


    private void Draw_Multi_Step ( Graphics G,
      int W, int H, int Margin_Left, int Margin_Right,
      int Margin_Top, int Margin_Bottom,
      int Chart_W, int Chart_H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      var (Min_V, Max_V) = Get_Y_Range ( );
      float Baseline = H - Margin_Bottom;
      Draw_Grid_And_Axes ( G, W, H, Margin_Left, Margin_Right,
        Margin_Top, Margin_Bottom, Chart_W, Chart_H, Min_V, Max_V );

      for ( int S_Idx = 0; S_Idx < _Series.Count; S_Idx++ )
      {
        var S = _Series [ S_Idx ];
        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
          S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

        int Actual_Count = Math.Min ( Visible_Count, S.Points.Count - Start_Index );
        if ( Actual_Count < 1 )
          continue;

        PointF [ ] Points = Build_Points ( S.Points, Start_Index, Actual_Count,
          Margin_Left, Chart_W, Chart_H, Margin_Top, Min_V, Max_V );

        Color Line_Color = _Theme.Line_Colors [ S_Idx % _Theme.Line_Colors.Length ];
        using var Line_Pen = new Pen ( Line_Color, 1.5f );

        Draw_Step_Chart ( G, Points, Actual_Count,
          Line_Pen, Margin_Top, Baseline );
      }
    }




    private List<double> Get_Single_Series_Readings ( )
    {
      if ( _Series.Count == 0 )
        return new List<double> ( );
      return _Series [ 0 ].Points.Select ( p => p.Value ).ToList ( );
    }

    private void Draw_Single_Bar ( Graphics G,
      int W, int H, int Margin_Left, int Margin_Right,
      int Margin_Top, int Margin_Bottom,
      int Chart_W, int Chart_H )
    {
      var Readings = Get_Single_Series_Readings ( );
      if ( Readings.Count == 0 )
        return;

      var (Min_V, Max_V) = Get_Y_Range ( );
      float Baseline = H - Margin_Bottom;

      Draw_Grid_And_Axes ( G, W, H, Margin_Left, Margin_Right,
        Margin_Top, Margin_Bottom, Chart_W, Chart_H, Min_V, Max_V );

      var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
        Readings.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

      int Actual_Count = Math.Min ( Visible_Count, Readings.Count - Start_Index );
      if ( Actual_Count < 1 )
        return;

      PointF [ ] Points = Build_Points ( _Series [ 0 ].Points,
        Start_Index, Actual_Count,
        Margin_Left, Chart_W, Chart_H, Margin_Top, Min_V, Max_V );

      Draw_Bar_Chart ( G, Points, Actual_Count,
        Margin_Left, Chart_W, Baseline );
    }

    private void Draw_Single_Histogram ( Graphics G,
     int W, int H, int Margin_Left, int Margin_Right,
     int Margin_Top, int Margin_Bottom,
     int Chart_W, int Chart_H )
    {
      var Saved = _Readings;
      _Readings = Get_Single_Series_Readings ( );

      Draw_Histogram ( G, W, H, Margin_Left, Margin_Right,  // ← was Draw_Single_Histogram
        Margin_Top, Margin_Bottom, Chart_W, Chart_H );

      _Readings = Saved;
    }

    private void Draw_Single_Pie ( Graphics G,
      int W, int H, int Margin_Left, int Margin_Right,
      int Margin_Top, int Margin_Bottom,
      int Chart_W, int Chart_H )
    {
      var Saved = _Readings;
      _Readings = Get_Single_Series_Readings ( );

      Draw_Pie_Chart ( G, W, H, Margin_Left, Margin_Right,
        Margin_Top, Margin_Bottom, Chart_W, Chart_H );

      _Readings = Saved;
    }




    private (double Min_V, double Max_V) Get_Y_Range ( )
    {
      double Min_V = double.MaxValue;
      double Max_V = double.MinValue;

      foreach ( var S in _Series )
      {
        foreach ( var P in S.Points )
        {
          if ( P.Value < Min_V )
            Min_V = P.Value;
          if ( P.Value > Max_V )
            Max_V = P.Value;
        }
      }

      if ( Min_V == double.MaxValue )
        return (0, 1);
      if ( Math.Abs ( Max_V - Min_V ) < 1e-12 )
      {
        double Pad = Math.Abs ( Max_V ) * 0.1;
        if ( Pad < 1e-12 )
          Pad = 1.0;
        Min_V -= Pad;
        Max_V += Pad;
      }

      return (Min_V, Max_V);
    }


    private PointF [ ] Build_Points (
      List<(DateTime Time, double Value)> Points,
      int Start_Index, int Count,
      int Margin_Left, int Chart_W, int Chart_H,
      int Margin_Top, double Min_V, double Max_V )
    {
      var Result = new PointF [ Count ];
      double Range = Max_V - Min_V;

      for ( int I = 0; I < Count; I++ )
      {
        float X = Margin_Left + (float) I / ( Count - 1 ) * Chart_W;
        float Y = Margin_Top + (float) ( ( Max_V - Points [ Start_Index + I ].Value )
                                        / Range * Chart_H );
        Result [ I ] = new PointF ( X, Y );
      }

      return Result;
    }













    /*
    private void old_Chart_Panel_Paint ( object? Sender, PaintEventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );


      Multimeter_Common_Helpers_Class.Track_FPS (
   ref _Paint_Count,
   ref _Actual_FPS,
   _FPS_Stopwatch,
   Update_Performance_Status );

      switch ( Current_Graph_Style )
      {
        case "Line":
          Draw_Line_Chart ( e.Graphics );
          break;
        case "Scatter":
          Draw_Scatter_Chart ( e.Graphics );
          break;
        case "Step":
          Draw_Step_Chart ( e.Graphics );
          break;
        case "Bar":
          Draw_Bar_Chart ( e.Graphics );
          break;
        case "Histogram":
          Draw_Histogram ( e.Graphics );
          break;
        case "Pie":
          Draw_Pie_Chart ( e.Graphics );
          break;
        default:
          Draw_Line_Chart ( e.Graphics );
          break;
      }

      Block?.Trace ( $"PAINT CALLED - Series: {_Series.Count}" );
      Block?.Trace ( $"_Combined_View = {_Combined_View}" );

      Graphics G = E.Graphics;
      G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

      int W = Chart_Panel.ClientSize.Width;
      int H = Chart_Panel.ClientSize.Height;

      Block?.Trace ( $"Chart size: W={W}, H={H}" );

      using var Bg_Brush = new SolidBrush ( _Theme.Background );
      G.FillRectangle ( Bg_Brush, 0, 0, W, H );

      if ( _Series.Count == 0 )
      {
        Block?.Trace ( "No series - drawing empty state" );
        Draw_Empty_State ( G, W, H, "No instruments. Add instruments and press Start." );
        return;
      }

      bool Has_Data = false;
      foreach ( var S in _Series )
      {
        if ( S.Visible && S.Points.Count > 0 )
        {
          Has_Data = true;
          Block?.Trace ( $"Found data: {S.Name} has {S.Points.Count} points" );
          break;
        }
      }

      if ( !Has_Data )
      {
        Block?.Trace ( "No data - drawing instrument list" );
        Draw_Instrument_List ( G, H );
        return;
      }

      Block?.Trace ( "Drawing charts..." );

      // Branch based on view mode
      if ( _Combined_View )
      {
        Block?.Trace ( "Drawing combined view" );
        Draw_Combined_View ( G, W, H );
      }
      else
      {
        Block?.Trace ( "Drawing split view" );
        Draw_Split_View ( G, W, H );
      }

      Draw_Position_Indicator ( E.Graphics, W, H );

      Block?.Trace ( "Paint complete" );
    }

    */


    private void Draw_Empty_State (
  Graphics G, int W, int H, string Message )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      using var Empty_Font = new Font ( "Segoe UI", 10F );
      using var Empty_Brush = new SolidBrush ( _Theme.Labels );
      G.DrawString ( Message, Empty_Font, Empty_Brush, 20, H / 2 );
    }


    private void Draw_Instrument_List ( Graphics G, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      using var Empty_Font = new Font ( "Segoe UI", 10F );
      using var Empty_Brush = new SolidBrush ( _Theme.Labels );

      int Y_Pos = 30;
      G.DrawString (
        "Press Start to begin polling. Instruments:",
        Empty_Font, Empty_Brush, 20, Y_Pos );
      Y_Pos += 25;

      for ( int I = 0; I < _Series.Count; I++ )
      {
        var S = _Series [ I ];
        using var Dot_Brush = new SolidBrush ( S.Line_Color );
        G.FillEllipse ( Dot_Brush, 30, Y_Pos + 3, 10, 10 );
        G.DrawString (
          $"{S.Name}  (GPIB {S.Address})",
          Empty_Font, Empty_Brush, 48, Y_Pos );
        Y_Pos += 22;
      }
    }



    private (DateTime Min, DateTime Max) Calculate_Time_Range ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      DateTime Time_Min = DateTime.MaxValue;
      DateTime Time_Max = DateTime.MinValue;

      if ( _Enable_Rolling )
      {
        foreach ( var S in _Series )
        {
          if ( !S.Visible || S.Points.Count == 0 )
            continue;

          var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
          if ( Visible_Count == 0 )
            continue;

          if ( S.Points [ Start_Index ].Time < Time_Min )
            Time_Min = S.Points [ Start_Index ].Time;

          if ( S.Points [ Start_Index + Visible_Count - 1 ].Time > Time_Max )
            Time_Max = S.Points [ Start_Index + Visible_Count - 1 ].Time;
        }
      }
      else
      {
        foreach ( var S in _Series )
        {
          if ( !S.Visible || S.Points.Count == 0 )
            continue;

          if ( S.Points [ 0 ].Time < Time_Min )
            Time_Min = S.Points [ 0 ].Time;

          if ( S.Points [ S.Points.Count - 1 ].Time > Time_Max )
            Time_Max = S.Points [ S.Points.Count - 1 ].Time;
        }
      }

      return (Time_Min, Time_Max);
    }



    private void Draw_Subplot (
  Graphics G,
  Instrument_Series S,
  int Subplot_Index,
  int Sub_Top,
  int Sub_Bottom,
  int Margin_Left,
  int Margin_Right,
  int W,
  DateTime Time_Min,
  double Time_Range_Sec,
  int Chart_W,
  Pen Grid_Pen,
  Pen Sep_Pen,
  Font Label_Font,
  Font Name_Font,
  Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Block?.Trace ( $"Drawing subplot for {S.Name}, Points: {S.Points.Count}, Visible: {S.Visible}" );

      // Instrument name label
      using var Name_Brush = new SolidBrush ( S.Line_Color );
      G.DrawString (
        $"{S.Name} (GPIB {S.Address})",
        Name_Font, Name_Brush,
        Margin_Left + 4, Sub_Top + 2 );

      // Draw separator between subplots
      if ( Subplot_Index > 0 )
      {
        int Sep_Y = Sub_Top - 8 / 2;
        G.DrawLine ( Sep_Pen,
          Margin_Left, Sep_Y,
          W - Margin_Right, Sep_Y );
      }

      if ( S.Points.Count == 0 )
      {
        G.DrawString ( "No data",
          Label_Font, Label_Brush,
          Margin_Left + 4, Sub_Top + 20 );
        return;
      }

      // Calculate Y-axis range
      var (Padded_Min, Padded_Max, Padded_Range) =
        Calculate_Y_Range ( S.Points );

      // **APPLY ZOOM HERE**
      double Display_Min = Padded_Min;
      double Display_Max = Padded_Max;
      double Display_Range = Padded_Range;

      if ( _Zoom_Factor > 0 && _Zoom_Factor != 1.0 )
      {
        double Center = ( Padded_Max + Padded_Min ) / 2.0;
        double Zoomed_Range = Padded_Range / _Zoom_Factor;

        Display_Min = Center - ( Zoomed_Range / 2.0 );
        Display_Max = Center + ( Zoomed_Range / 2.0 );
        Display_Range = Display_Max - Display_Min;

        Block?.Trace ( $"{S.Name} ZOOM: {_Zoom_Factor:F2}x -> [{Display_Min:F6}, {Display_Max:F6}]" );
      }

      int Label_Top = Sub_Top + 18;
      int Plot_H = Sub_Bottom - Label_Top;

      // Draw grid and Y-axis labels (use Display values)
      Draw_Y_Axis (
        G, Display_Min, Display_Range,
        Sub_Bottom, Plot_H,
        Margin_Left, Margin_Right, W,
        Grid_Pen, Label_Font, Label_Brush );
      // Build point array (use Display values)
      PointF [ ] Points = Build_Point_Array (
        S.Points, Time_Min, Time_Range_Sec,
        Display_Min, Display_Range,
        Margin_Left, Chart_W,
        Sub_Bottom, Plot_H );
      // Draw based on selected style
      switch ( _Current_Graph_Style )
      {
        case "Scatter":
          Draw_Data_Dots ( G, Points, S.Line_Color );
          break;
        case "Step":
          Draw_Step_And_Fill ( G, Points, S.Line_Color, Sub_Bottom, Label_Top );
          Draw_Data_Dots ( G, Points, S.Line_Color );
          break;
        case "Bar":
          Draw_Subplot_Bars ( G, Points, S.Line_Color, Sub_Bottom, Label_Top );
          break;
        case "Histogram":
          Draw_Subplot_Histogram ( G, S.Points, S.Line_Color,
            Margin_Left, Chart_W, Sub_Bottom, Label_Top,
            Display_Min, Display_Range );
          break;
        default: // Line
          Draw_Line_And_Fill ( G, Points, S.Line_Color, Sub_Bottom, Label_Top );
          Draw_Data_Dots ( G, Points, S.Line_Color );
          break;
      }
      // Highlight last point with value
      Draw_Last_Point (
        G, Points, S.Points, S.Line_Color,
        W, Margin_Right, Label_Font, Name_Brush );
    }







    private void new_Draw_Subplot (
      Graphics G,
      Instrument_Series S,
      int Subplot_Index,
      int Sub_Top,
      int Sub_Bottom,
      int Margin_Left,
      int Margin_Right,
      int W,
      DateTime Time_Min,
      double Time_Range_Sec,
      int Chart_W,
      Pen Grid_Pen,
      Pen Sep_Pen,
      Font Label_Font,
      Font Name_Font,
      Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Instrument name label
      using var Name_Brush = new SolidBrush ( S.Line_Color );
      G.DrawString (
        $"{S.Name} (GPIB {S.Address})",
        Name_Font, Name_Brush,
        Margin_Left + 4, Sub_Top + 2 );

      // Draw separator between subplots
      if ( Subplot_Index > 0 )
      {
        int Sep_Y = Sub_Top - 8 / 2;
        G.DrawLine ( Sep_Pen,
          Margin_Left, Sep_Y,
          W - Margin_Right, Sep_Y );
      }

      if ( S.Points.Count == 0 )
      {
        G.DrawString ( "No data",
          Label_Font, Label_Brush,
          Margin_Left + 4, Sub_Top + 20 );
        return;
      }

      // Calculate Y-axis range
      var (Padded_Min, Padded_Max, Padded_Range) =
        Calculate_Y_Range ( S.Points );

      // **APPLY ZOOM HERE**
      double Display_Min = Padded_Min;
      double Display_Max = Padded_Max;
      double Display_Range = Padded_Range;

      if ( _Zoom_Factor > 0 && _Zoom_Factor != 1.0 )
      {
        double Center = ( Padded_Max + Padded_Min ) / 2.0;
        double Zoomed_Range = Padded_Range / _Zoom_Factor;

        Display_Min = Center - ( Zoomed_Range / 2.0 );
        Display_Max = Center + ( Zoomed_Range / 2.0 );
        Display_Range = Display_Max - Display_Min;

        Block?.Trace ( $"{S.Name} ZOOM: {_Zoom_Factor:F2}x -> [{Display_Min:F6}, {Display_Max:F6}]" );
      }

      int Label_Top = Sub_Top + 18;
      int Plot_H = Sub_Bottom - Label_Top;

      // Draw grid and Y-axis labels (use Display values)
      Draw_Y_Axis (
        G, Display_Min, Display_Range,
        Sub_Bottom, Plot_H,
        Margin_Left, Margin_Right, W,
        Grid_Pen, Label_Font, Label_Brush );

      // Build point array (use Display values)
      PointF [ ] Points = Build_Point_Array (
        S.Points, Time_Min, Time_Range_Sec,
        Display_Min, Display_Range,
        Margin_Left, Chart_W,
        Sub_Bottom, Plot_H );

      // Set clipping for this subplot
      var Clip_Rect = new Rectangle ( Margin_Left, Label_Top, Chart_W, Plot_H );
      G.SetClip ( Clip_Rect );

      // Draw gradient fill and line
      Draw_Line_And_Fill (
        G, Points, S.Line_Color,
        Sub_Bottom, Label_Top );

      // Draw data point dots
      Draw_Data_Dots ( G, Points, S.Line_Color );

      // Highlight last point with value
      Draw_Last_Point (
        G, Points, S.Points, S.Line_Color,
        W, Margin_Right, Label_Font, Name_Brush );

      // Reset clipping
      G.ResetClip ( );
    }
    private (double Min, double Max, double Range) Calculate_Y_Range (
  List<(DateTime Time, double Value)> Points )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      double Min_V = double.MaxValue;
      double Max_V = double.MinValue;

      foreach ( var P in Points )
      {
        if ( P.Value < Min_V )
          Min_V = P.Value;
        if ( P.Value > Max_V )
          Max_V = P.Value;
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


    private void Draw_Y_Axis (
  Graphics G,
  double Padded_Min,
  double Padded_Range,
  int Sub_Bottom,
  int Plot_H,
  int Margin_Left,
  int Margin_Right,
  int W,
  Pen Grid_Pen,
  Font Label_Font,
  Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Num_Grid = 4;
      for ( int I = 0; I <= Num_Grid; I++ )
      {
        double Fraction = (double) I / Num_Grid;
        double Value = Padded_Min + Fraction * Padded_Range;
        int Y = Sub_Bottom - (int) ( Fraction * Plot_H );

        G.DrawLine ( Grid_Pen,
          Margin_Left, Y,
          W - Margin_Right, Y );

        string Lbl = Format_Value ( Value );
        var Lbl_Size = G.MeasureString ( Lbl, Label_Font );
        G.DrawString ( Lbl, Label_Font, Label_Brush,
          Margin_Left - Lbl_Size.Width - 4,
          Y - Lbl_Size.Height / 2 );
      }
    }

    private PointF [ ] Build_Point_Array (
      List<(DateTime Time, double Value)> Points,
      DateTime Time_Min,
      double Time_Range_Sec,
      double Padded_Min,
      double Padded_Range,
      int Margin_Left,
      int Chart_W,
      int Sub_Bottom,
      int Plot_H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Get visible range
      var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

      if ( Visible_Count == 0 )
        return new PointF [ 0 ];

      PointF [ ] Result = new PointF [ Visible_Count ];

      // Recalculate time range for VISIBLE points only
      DateTime Visible_Time_Min = Points [ Start_Index ].Time;
      DateTime Visible_Time_Max = Points [ Start_Index + Visible_Count - 1 ].Time;
      double Visible_Time_Range_Sec = ( Visible_Time_Max - Visible_Time_Min ).TotalSeconds;

      // Handle single point or zero range
      if ( Visible_Time_Range_Sec < 0.001 )
        Visible_Time_Range_Sec = 1.0;

      for ( int I = 0; I < Visible_Count; I++ )
      {
        int Data_Index = Start_Index + I;

        // Calculate time fraction based on VISIBLE range
        double Time_Sec = ( Points [ Data_Index ].Time - Visible_Time_Min ).TotalSeconds;
        double Time_Frac = Time_Sec / Visible_Time_Range_Sec;

        double V_Frac = ( Points [ Data_Index ].Value - Padded_Min ) / Padded_Range;

        float X = Margin_Left + (float) ( Time_Frac * Chart_W );
        float Y = Sub_Bottom - (float) ( V_Frac * Plot_H );

        Result [ I ] = new PointF ( X, Y );
      }

      return Result;
    }



    private void Draw_Line_And_Fill (
  Graphics G,
  PointF [ ] Points,
  Color Line_Color,
  int Sub_Bottom,
  int Label_Top )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Count = Points.Length;
      if ( Count < 2 )
        return;

      using var Line_Pen = new Pen ( Line_Color, 2f );
      Color Fill_Top = Color.FromArgb ( 60, Line_Color );
      Color Fill_Bottom = Color.FromArgb ( 5, Line_Color );

      PointF [ ] Fill_Points = new PointF [ Count + 2 ];
      Array.Copy ( Points, Fill_Points, Count );
      Fill_Points [ Count ] = new PointF (
        Points [ Count - 1 ].X, Sub_Bottom );
      Fill_Points [ Count + 1 ] = new PointF (
        Points [ 0 ].X, Sub_Bottom );

      using var Fill_Brush =
        new System.Drawing.Drawing2D.LinearGradientBrush (
          new PointF ( 0, Label_Top ),
          new PointF ( 0, Sub_Bottom ),
          Fill_Top, Fill_Bottom );

      G.FillPolygon ( Fill_Brush, Fill_Points );
      G.DrawLines ( Line_Pen, Points );
    }


    private void Draw_Data_Dots (
  Graphics G,
  PointF [ ] Points,
  Color Line_Color )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Count = Points.Length;

      if ( Count > 150 )
        return;


      using var Dot_Brush = new SolidBrush (
        Color.FromArgb ( 200,
          Line_Color.R,
          Line_Color.G,
          Line_Color.B ) );

      float Dot_Size = Count > 100 ? 3f :
        Count > 50 ? 4f : 5f;

      foreach ( PointF P in Points )
      {
        G.FillEllipse ( Dot_Brush,
          P.X - Dot_Size / 2, P.Y - Dot_Size / 2,
          Dot_Size, Dot_Size );
      }
    }


    private void Draw_Last_Point (
  Graphics G,
  PointF [ ] Points,
  List<(DateTime Time, double Value)> Data_Points,
  Color Line_Color,
  int W,
  int Margin_Right,
  Font Label_Font,
  Brush Name_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Count = Points.Length;
      if ( Count == 0 )
        return;

      PointF Last = Points [ Count - 1 ];

      using var Glow_Pen = new Pen (
        Color.FromArgb ( 80, Line_Color ), 6f );
      G.DrawEllipse ( Glow_Pen,
        Last.X - 5, Last.Y - 5, 10, 10 );

      using var Last_Brush = new SolidBrush ( Color.White );
      G.FillEllipse ( Last_Brush,
        Last.X - 3, Last.Y - 3, 6, 6 );

      // Latest value text
      string Val_Text = Format_Value ( Data_Points [ Count - 1 ].Value );
      var Val_Size = G.MeasureString ( Val_Text, Label_Font );
      float Tx = Last.X + 8;
      if ( Tx + Val_Size.Width > W - Margin_Right )
        Tx = Last.X - Val_Size.Width - 8;

      G.DrawString ( Val_Text, Label_Font, Name_Brush, Tx,
        Last.Y - Val_Size.Height / 2 );
    }

    private void Draw_Time_Axis (
  Graphics G,
  int H,
  int Chart_W,
  double Time_Range_Sec,
  int Margin_Left,
  int Margin_Bottom,
  int Margin_Top,
  Pen Grid_Pen,
  Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      using var X_Label_Font = new Font ( "Segoe UI", 7.5F );

      int Num_X_Labels = Math.Min ( 8, (int) ( Chart_W / 80.0 ) );
      if ( Num_X_Labels < 2 )
        Num_X_Labels = 2;

      for ( int I = 0; I <= Num_X_Labels; I++ )
      {
        double Fraction = (double) I / Num_X_Labels;
        int X_Pos = Margin_Left + (int) ( Fraction * Chart_W );

        double Sec = Fraction * Time_Range_Sec;
        string Time_Text = Format_Time_Label ( Sec, Time_Range_Sec );

        var Ts = G.MeasureString ( Time_Text, X_Label_Font );
        G.DrawString ( Time_Text, X_Label_Font, Label_Brush,
          X_Pos - Ts.Width / 2,
          H - Margin_Bottom + 8 );

        // Vertical grid line
        G.DrawLine ( Grid_Pen,
          X_Pos, Margin_Top,
          X_Pos, H - Margin_Bottom );
      }
    }


    private string Format_Time_Label ( double Seconds, double Time_Range_Sec )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Time_Range_Sec < 60 )
        return $"{Seconds:F1}s";

      if ( Time_Range_Sec < 3600 )
        return $"{Seconds / 60:F1}m";

      return $"{Seconds / 3600:F1}h";
    }




    private void Theme_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      var Next_Theme = Multimeter_Common_Helpers_Class.Get_Next_Theme ( _Theme );
      _Settings.Set_Theme ( Next_Theme );
    }




    private string Format_Axis_Value ( double Value )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      double Abs_Value = Math.Abs ( Value );

      if ( Abs_Value >= 1000 )
        return Value.ToString ( "F2" );
      else if ( Abs_Value >= 100 )
        return Value.ToString ( "F3" );
      else if ( Abs_Value >= 10 )
        return Value.ToString ( "F4" );
      else if ( Abs_Value >= 1 )
        return Value.ToString ( "F5" );
      else if ( Abs_Value >= 0.1 )
        return Value.ToString ( "F6" );
      else if ( Abs_Value >= 0.01 )
        return Value.ToString ( "F7" );
      else if ( Abs_Value >= 0.001 )
        return Value.ToString ( "F8" );
      else if ( Abs_Value >= 0.0001 )
        return Value.ToString ( "F9" );
      else
        return Value.ToString ( "F11" );
    }





    private string Format_Value ( double Value )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      double Abs_Value = Math.Abs ( Value );

      // Use decimal notation with appropriate precision
      if ( Abs_Value >= 1000 )
      {
        return Value.ToString ( "F2" );  // 1234.56
      }
      else if ( Abs_Value >= 100 )
      {
        return Value.ToString ( "F3" );  // 123.456
      }
      else if ( Abs_Value >= 10 )
      {
        return Value.ToString ( "F4" );  // 12.3456
      }
      else if ( Abs_Value >= 1 )
      {
        return Value.ToString ( "F5" );  // 1.23456
      }
      else if ( Abs_Value >= 0.1 )
      {
        return Value.ToString ( "F6" );  // 0.123456
      }
      else if ( Abs_Value >= 0.01 )
      {
        return Value.ToString ( "F7" );  // 0.0123456
      }
      else if ( Abs_Value >= 0.001 )
      {
        return Value.ToString ( "F8" );  // 0.00123456
      }
      else if ( Abs_Value >= 0.0001 )
      {
        return Value.ToString ( "F9" );  // 0.000123456
      }
      else
      {
        return Value.ToString ( "F11" ); // 0.00000123456
      }
    }



    // ===== Data Model =====

    private class Instrument_Series
    {
      public string Name { get; set; } = "";
      public int Address
      {
        get; set;
      }
      public Meter_Type Type
      {
        get; set;
      }
      public List<(DateTime Time, double Value)> Points
      {
        get; set;
      }
          = new List<(DateTime, double)> ( );
      public Color Line_Color
      {
        get; set;
      }
      public bool Visible { get; set; } = true;

      // Existing methods
      public double Get_Average ( )
      {
        if ( Points == null || Points.Count == 0 )
          return 0.0;
        return Points.Average ( P => P.Value );
      }

      public double Get_Min ( )
      {
        if ( Points == null || Points.Count == 0 )
          return 0.0;
        return Points.Min ( P => P.Value );
      }

      public double Get_Max ( )
      {
        if ( Points == null || Points.Count == 0 )
          return 0.0;
        return Points.Max ( P => P.Value );
      }

      public double Get_StdDev ( )
      {
        if ( Points == null || Points.Count < 2 )
          return 0.0;
        double Average = Get_Average ( );
        double Sum_Of_Squares = Points.Sum ( P => Math.Pow ( P.Value - Average, 2 ) );
        return Math.Sqrt ( Sum_Of_Squares / Points.Count );
      }

      // New methods
      public double Get_Range ( )
      {
        return Get_Max ( ) - Get_Min ( );
      }

      public double Get_Last ( )
      {
        if ( Points == null || Points.Count == 0 )
          return 0.0;
        return Points [ Points.Count - 1 ].Value;
      }

      public double Get_Peak_To_Peak ( )
      {
        return Get_Range ( );
      }

      public double Get_RMS ( )
      {
        if ( Points == null || Points.Count == 0 )
          return 0.0;


        double Sum_Of_Squares = Points.Sum ( P => P.Value * P.Value );
        return Math.Sqrt ( Sum_Of_Squares / Points.Count );
      }

      public string Get_Trend ( )
      {
        if ( Points == null || Points.Count < 10 )
          return "—";

        // Compare average of last 5 vs previous 5
        double Recent = Points.Skip ( Points.Count - 5 ).Average ( P => P.Value );
        double Previous = Points.Skip ( Points.Count - 10 ).Take ( 5 ).Average ( P => P.Value );

        double Change = ( Recent - Previous ) / Previous * 100;

        if ( Math.Abs ( Change ) < 0.1 )
          return "→";  // Stable
        return Change > 0 ? "↑" : "↓";
      }

      public double Get_Sample_Rate ( )
      {
        if ( Points == null || Points.Count < 2 )
          return 0.0;

        TimeSpan Elapsed = Points [ Points.Count - 1 ].Time - Points [ 0 ].Time;
        return ( Points.Count - 1 ) / Elapsed.TotalSeconds;
      }


      public Dictionary<string, string> File_Stats { get; set; } = null;

    }

    private void Rolling_Check_CheckedChanged ( object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Enable_Rolling = Rolling_Check.Checked;
      Max_Points_Numeric.Enabled = Rolling_Check.Checked;

      if ( _Enable_Rolling )
      {
        Capture_Trace.Write ( $"Rolling/Zoom mode enabled: {_Max_Display_Points} points" );

        // DON'T trim data when not actively polling
        // Just change the view window
        if ( !_Is_Running )
        {
          Capture_Trace.Write ( "Zoom mode for loaded data - view window adjusted" );
        }
        else
        {
          // Only trim when actively polling
          foreach ( var S in _Series )
          {
            if ( S.Points.Count > _Max_Display_Points )
            {
              int Points_To_Remove = S.Points.Count - _Max_Display_Points;
              S.Points.RemoveRange ( 0, Points_To_Remove );
            }
          }
        }
      }
      else
      {
        Capture_Trace.Write ( "Rolling/Zoom mode disabled - showing all data" );
      }

      Chart_Panel.Invalidate ( );
    }

    private void Max_Points_Numeric_ValueChanged ( object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Max_Display_Points = (int) Max_Points_Numeric.Value;
      Capture_Trace.Write ( $"Max display points changed to: {_Max_Display_Points}" );

      // If rolling is enabled and we're actively polling, trim the data
      if ( _Enable_Rolling && _Is_Running )
      {
        foreach ( var S in _Series )
        {
          if ( S.Points.Count > _Max_Display_Points )
          {
            int Points_To_Remove = S.Points.Count - _Max_Display_Points;
            S.Points.RemoveRange ( 0, Points_To_Remove );
          }
        }
      }

      Chart_Panel.Invalidate ( );
    }


    private void Legend_CheckBox_Changed (
  object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Sender is not CheckBox Cb || Cb.Tag is not int Index )
        return;

      // Count visible series
      int Visible_Count = 0;
      foreach ( var S in _Series )
      {
        if ( S.Visible )
          Visible_Count++;
      }

      // Prevent hiding the last visible series
      if ( Visible_Count == 1 && _Series [ Index ].Visible && !Cb.Checked )
      {
        Cb.Checked = true;  // Force back to checked
        MessageBox.Show (
          "At least one instrument must remain visible.",
          "Cannot Hide",
          MessageBoxButtons.OK,
          MessageBoxIcon.Information );
        return;
      }

      _Series [ Index ].Visible = Cb.Checked;
      Chart_Panel.Invalidate ( );
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






    private void Initialize_Chart_Refresh_Timer ( )
    {
      _Chart_Refresh_Timer = new System.Windows.Forms.Timer ( );
      _Chart_Refresh_Timer.Tick += ( s, e ) => Chart_Panel.Invalidate ( );
      _Chart_Refresh_Timer.Interval = _Settings.Chart_Refresh_Rate_Ms;

    }

    private void Initialize_Status_Panel ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      (_Memory_Status_Label, _Performance_Status_Label) =
        Multimeter_Common_Helpers_Class.Initialize_Status_Strip ( this, _Settings, _Series.Count );

      Update_Memory_Status ( 0, _Settings.Max_Data_Points_In_Memory );
    }










    private bool Should_Disable_Instrument ( Instrument_Series series )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Error_Counts.ContainsKey ( series.Name ) )
        _Error_Counts [ series.Name ] = 0;

      _Error_Counts [ series.Name ]++;

      // Disable after 10 consecutive errors
      return _Error_Counts [ series.Name ] >= 10;
    }

    private void Disable_Instrument ( Instrument_Series series )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      series.Visible = false;

      var Result = MessageBox.Show (
        $"Instrument '{series.Name}' at address {series.Address} has been disabled due to repeated errors.\n\n" +
        $"Last successful read: {_Last_Success [ series.Name ]:HH:mm:ss}\n" +
        $"Error count: {_Error_Counts [ series.Name ]}\n\n" +
        "Please check the connection. Would you like to re-enable it now?",
        "Instrument Disabled",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning );

      if ( Result == DialogResult.Yes )
      {
        // Reset error count and re-enable
        _Error_Counts [ series.Name ] = 0;
        series.Visible = true;

        Show_Progress ( $"Re-enabled {series.Name}", _Foreground_Color );

      }

      Update_Legend ( );
    }

    private void Update_Error_Status ( string instrument, string error )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Truncate long error messages
      string Short_Error = error.Length > 50
        ? error.Substring ( 0, 47 ) + "..."
        : error;

      int Error_Count = _Error_Counts.ContainsKey ( instrument )
        ? _Error_Counts [ instrument ]
        : 0;

      Show_Progress ( $"Error on {instrument} ({Error_Count} errors): {Short_Error}", _Foreground_Color );

    }

    // Add a manual reset errors button
    private void Reset_Errors_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Error_Counts.Clear ( );
      foreach ( var Series in _Series )
      {
        _Error_Counts [ Series.Name ] = 0;
        Series.Visible = true;
      }

      Show_Progress ( "Error counts reset - all instruments enabled", _Foreground_Color );


      Update_Legend ( );
    }


    private void Save_To_File ( string File_Path )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Series.Count == 0 || _Series.All ( s => s.Points.Count == 0 ) )
        return;

      using var Writer = new StreamWriter ( File_Path );

      // Header
      Writer.WriteLine ( $"# Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" );
      Writer.WriteLine ( $"# Instruments: {_Series.Count}" );

      int Total_Points = _Series.Sum ( s => s.Points.Count );
      Writer.WriteLine ( $"# Total Points: {Total_Points}" );
      Writer.WriteLine ( "#" );

      // Statistics for each instrument
      Writer.WriteLine ( "# Statistics:" );
      foreach ( var S in _Series )
      {
        if ( S.Points.Count == 0 )
          continue;

        // Calculate stats
        double min = S.Get_Min ( );
        double max = S.Get_Max ( );
        double avg = S.Get_Average ( );
        double stdDev = S.Get_StdDev ( );
        double range = S.Get_Range ( );
        double last = S.Get_Last ( );

        Writer.WriteLine ( $"# [{S.Name}]" );
        Writer.WriteLine ( $"#   Points: {S.Points.Count}" );
        Writer.WriteLine ( $"#   Last: {last:F6}" );
        Writer.WriteLine ( $"#   Average: {avg:F6}" );
        Writer.WriteLine ( $"#   Std Dev: {stdDev:F6}" );
        Writer.WriteLine ( $"#   Min: {min:F6}" );
        Writer.WriteLine ( $"#   Max: {max:F6}" );

        // Duration and rate
        if ( S.Points.Count >= 2 )
        {
          TimeSpan duration = S.Points [ S.Points.Count - 1 ].Time - S.Points [ 0 ].Time;
          double rate = S.Get_Sample_Rate ( );

          Writer.WriteLine ( $"#   Duration: {Multimeter_Common_Helpers_Class.Format_Time_Span ( duration )}" );
          Writer.WriteLine ( $"#   Rate: {rate:F2} S/s" );

          // Average interval
          double totalMs = 0;
          for ( int i = 1; i < S.Points.Count; i++ )
          {
            totalMs += ( S.Points [ i ].Time - S.Points [ i - 1 ].Time ).TotalMilliseconds;
          }
          double avgInterval = totalMs / ( S.Points.Count - 1 );
          Writer.WriteLine ( $"#   Avg Δt: {avgInterval:F1} ms" );
        }
      }

      Writer.WriteLine ( "#" );

      // Build column headers
      Writer.Write ( "Timestamp" );
      foreach ( var S in _Series )
      {
        Writer.Write ( $",{S.Name}" );
      }
      Writer.WriteLine ( );

      // Find all unique timestamps across all series
      var All_Timestamps = new SortedSet<DateTime> ( );
      foreach ( var S in _Series )
      {
        foreach ( var P in S.Points )
        {
          All_Timestamps.Add ( P.Time );
        }
      }

      // Write data rows
      foreach ( var Time in All_Timestamps )
      {
        Writer.Write ( $"{Time:yyyy-MM-dd HH:mm:ss.fff}" );

        foreach ( var S in _Series )
        {
          // Find value at this timestamp
          var Point = S.Points.FirstOrDefault ( p => p.Time == Time );
          if ( Point != default )
          {
            Writer.Write ( $",{Point.Value.ToString ( CultureInfo.InvariantCulture )}" );
          }
          else
          {
            Writer.Write ( "," ); // Empty cell if no data
          }
        }
        Writer.WriteLine ( );
      }
    }






    // Update your existing Save button click handler to use this
    private void Save_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Series.Count == 0 || _Series.All ( s => s.Points.Count == 0 ) )
      {
        MessageBox.Show (
          "No data to save.",
          "Save Data",
          MessageBoxButtons.OK,
          MessageBoxIcon.Information );
        return;
      }

      string Folder = _Settings.Default_Save_Folder;

      using var Dlg = new SaveFileDialog ( );
      Dlg.Title = "Save Recorded Data";
      Dlg.Filter = "CSV files (*.csv)|*.csv";
      Dlg.InitialDirectory = Folder;

      // Generate default filename from pattern
      string Default_Name = _Settings.Filename_Pattern;
      Default_Name = Default_Name.Replace ( "{date}", DateTime.Now.ToString ( "yyyy-MM-dd" ) );
      Default_Name = Default_Name.Replace ( "{time}", DateTime.Now.ToString ( "HH-mm-ss" ) );
      Default_Name = Default_Name.Replace ( "{function}", "Multi" );

      if ( !Default_Name.EndsWith ( ".csv", StringComparison.OrdinalIgnoreCase ) )
        Default_Name += ".csv";

      Dlg.FileName = Default_Name;

      if ( Dlg.ShowDialog ( ) != DialogResult.OK )
        return;

      try
      {
        Save_To_File ( Dlg.FileName );

        // Summary message
        int Total_Points = _Series.Sum ( s => s.Points.Count );
        string Summary = $"Saved {_Series.Count} instruments, {Total_Points} total points to:\n{Dlg.FileName}\n\n";

        foreach ( var S in _Series )
        {
          if ( S.Points.Count > 0 )
          {
            double avg = S.Get_Average ( );
            Summary += $"{S.Name}: {S.Points.Count} pts, Avg: {avg:F6}\n";
          }
        }

        MessageBox.Show (
          Summary,
          "Recording Saved",
          MessageBoxButtons.OK,
          MessageBoxIcon.Information );
      }
      catch ( Exception ex )
      {
        MessageBox.Show (
          $"Failed to save file:\n{ex.Message}",
          "Save Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error );
      }
    }





    private void Apply_Settings ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // ===== DISPLAY SETTINGS =====

      // Tooltip settings
      if ( _Chart_Tooltip != null )
      {
        _Chart_Tooltip.AutoPopDelay = _Settings.Tooltip_Display_Duration_Ms;
        // Tooltip distance is checked in Chart_Panel_MouseMove
      }

      // Chart refresh rate
      _Chart_Refresh_Timer.Interval = _Settings.Chart_Refresh_Rate_Ms;
      Update_Chart_Refresh_Rate ( );

      // Default max display points
      _Max_Display_Points = _Settings.Default_Max_Display_Points;
      Max_Points_Numeric.Value = _Settings.Default_Max_Display_Points;

      // View mode defaults (only apply if no data yet)
      if ( _Series.All ( s => s.Points.Count == 0 ) )
      {
        _Combined_View = _Settings.Default_To_Combined_View;
        _Normalized_View = _Settings.Default_To_Normalized_View;

        View_Mode_Button.Text = _Combined_View ? "Split View" : "Combined View";
        Normalize_Button.Text = _Normalized_View ? "Absolute" : "Normalize";
        Normalize_Button.Visible = _Combined_View;
      }

      // Legend visibility
      if ( _Legend_Panel_2 != null )
      {
        _Legend_Panel_2.Visible = _Settings.Show_Legend_On_Startup;
        Legend_Toggle_Button.Text = _Legend_Panel_2.Visible ? "Hide Stats" : "Show Stats";
      }

      // ===== POLLING SETTINGS =====

      // Default delay (only if not currently running)
      if ( !_Is_Running )
      {
        decimal Delay_Ms = Math.Max ( Delay_Numeric.Minimum,
          Math.Min ( Delay_Numeric.Maximum, _Settings.Default_Poll_Delay_Ms ) );
        Delay_Numeric.Value = Delay_Ms;
      }

      string NPLC_Value = NPLC_Textbox.Text.Trim ( );

      Update_Settle_Display ( );

      // Default measurement type
      if ( Measurement_Combo.Items.Contains ( _Settings.Default_Measurement_Type ) )
      {
        Measurement_Combo.SelectedItem = _Settings.Default_Measurement_Type;
      }

      // Default continuous polling
      Continuous_Check.Checked = _Settings.Default_Continuous_Poll;

      // ===== FILE SETTINGS =====

      // Save folder
      if ( !string.IsNullOrEmpty ( _Settings.Default_Save_Folder ) )
      {
        try
        {
          Directory.CreateDirectory ( _Settings.Default_Save_Folder );
        }
        catch { }
      }

      // Auto-save timer
      if ( _Settings.Enable_Auto_Save )
      {
        _Auto_Save_Timer.Interval = _Settings.Auto_Save_Interval_Minutes * 60 * 1000;
        if ( _Is_Running )
          _Auto_Save_Timer.Start ( );
      }
      else
      {
        _Auto_Save_Timer.Stop ( );
      }

      // ===== UI SETTINGS =====

      // Window title
      if ( !string.IsNullOrWhiteSpace ( _Settings.Default_Window_Title ) )
      {
        this.Text = $"{_Settings.Default_Window_Title} - Multi-Poll ({_Series.Count} instruments)";
      }

      // ===== ZOOM SETTINGS =====

      // Default zoom level
      if ( Zoom_Slider != null )
      {
        Zoom_Slider.Value = _Settings.Default_Zoom_Level;
        Update_Zoom_From_Slider ( );
      }

      // ===== TRIGGER UPDATES =====

      Multimeter_Common_Helpers_Class.Update_Scrollbar_Range (
 Pan_Scrollbar, _Series.Max ( s => s.Points.Count ),
 _Max_Display_Points, _Auto_Scroll, ref _View_Offset );
      Update_Legend ( );
      Chart_Panel.Invalidate ( );

      Block?.Trace ( "Settings applied successfully" );
    }

    private void Update_Chart_Refresh_Rate ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );  // Add trace

      int Total_Points = _Series.Sum ( s => s.Points.Count );
      int Base_Rate = _Settings.Chart_Refresh_Rate_Ms;

      if ( _Settings.Throttle_When_Many_Points &&
          Total_Points > _Settings.Throttle_Point_Threshold )
      {
        int Multiplier = 1;

        if ( Total_Points > _Settings.Throttle_Point_Threshold * 10 )
          Multiplier = 4;
        else if ( Total_Points > _Settings.Throttle_Point_Threshold * 5 )
          Multiplier = 3;
        else if ( Total_Points > _Settings.Throttle_Point_Threshold * 2 )
          Multiplier = 2;

        _Chart_Refresh_Timer.Interval = Base_Rate * Multiplier;
      }
      else
      {
        _Chart_Refresh_Timer.Interval = Base_Rate;
      }

      Update_Performance_Status ( );
    }




    private void Start_Stop_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Block?.Trace ( $"_Is_Running = {_Is_Running}" );

      if ( !_Is_Running )
      {
        Block?.Trace ( "Starting polling..." );

        // Start chart refresh timer
        _Chart_Refresh_Timer.Start ( );

        // Start auto-save if enabled
        if ( _Settings.Enable_Auto_Save )
          _Auto_Save_Timer.Start ( );

        // Start async polling loop directly
        Start_Polling ( );  // ← this is all you need
      }
      else
      {
        Block?.Trace ( "Stopping polling..." );
        Stop_Polling ( );   // ← cancels the CTS

        _Chart_Refresh_Timer.Stop ( );
        _Auto_Save_Timer.Stop ( );

        if ( _Settings.Auto_Save_On_Stop && _Series.Any ( s => s.Points.Count > 0 ) )
          Auto_Save_Timer_Tick ( null, null );

        Chart_Panel.Invalidate ( );
        Multimeter_Common_Helpers_Class.Check_Memory_Limit (
         _Settings,
         ( ) => _Series.Sum ( s => s.Points.Count ),
         ( ) =>
         {
           Stop_Recording ( );
           if ( InvokeRequired )
             this.Invoke ( ( ) => Update_Graph_Style_Availability ( ) );
           else
             Update_Graph_Style_Availability ( );
         },
         Show_Memory_Warning,
         ref _Memory_Warning_Shown );

        Update_Performance_Status ( );
      }
    }

    /*
    private void Delay_Numeric_ValueChanged ( object Sender, EventArgs E )
    {
      int Interval_Ms = (int) ( Delay_Numeric.Value * 1000 );
    //  _Poll_Timer.Interval = Interval_Ms;
    }
    */


    /*
    private void Poll_Timer_Tick ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Block?.Trace ( "=== POLL TICK FIRED ===" );
      Block?.Trace ( $"_Is_Running = {_Is_Running}" );
      Block?.Trace ( $"Series count = {_Series.Count}" );

      foreach ( var Series in _Series )
      {
        Block?.Trace ( $"Series: {Series.Name}, Visible: {Series.Visible}, Points: {Series.Points.Count}" );

        if ( !Series.Visible )
        {
          Block?.Trace ( $"  Skipping (not visible)" );
          continue;
        }

        Block?.Trace ( $"  Attempting to read address {Series.Address}" );

        try
        {
          Block?.Trace ( $"  Calling GPIB_Manager.Read_Measurement({Series.Address})" );
          //    double Value = _GPIB_Manager.Read_Measurement ( Series.Address );
          string Command = Get_Measurement_Command ( Series.Type );
          Block?.Trace ( $"  Command for {Series.Type}: [{Command}]" );
          double Value = _GPIB_Manager.Read_Measurement ( Series.Address, Command );
          Block?.Trace ( $"  Got value: {Value}" );

          Series.Points.Add ( (DateTime.Now, Value) );

          Block?.Trace ( $"  Added to Points list. Count now: {Series.Points.Count}" );

          _Error_Counts [ Series.Name ] = 0;
          _Last_Success [ Series.Name ] = DateTime.Now;
        }
        catch ( Exception ex )
        {
          Block?.Trace ( $"  EXCEPTION: {ex.GetType ( ).Name}" );
          Block?.Trace ( $"  Message: {ex.Message}" );
          Block?.Trace ( $"  StackTrace: {ex.StackTrace}" );

          Update_Error_Status ( Series.Name, ex.Message );

          if ( Should_Disable_Instrument ( Series ) )
          {
            Disable_Instrument ( Series );
          }
        }
      }

      Block?.Trace ( "=== POLL TICK COMPLETE ===" );
      Multimeter_Common_Helpers_Class.Check_Memory_Limit (
  _Settings,
  ( ) => _Series.Sum ( s => s.Points.Count ),  // summed across series
  Stop_Recording,
  Show_Memory_Warning,
  ref _Memory_Warning_Shown );
      Update_Performance_Status ( );
      Update_Legend_Throttled ( );
      Update_Scrollbar_Range ( );

      Chart_Panel.Invalidate ( );
    }

    */


    private string Get_Measurement_Command ( Meter_Type Type )
    {
      int Filtered_Index = _Filtered_Indices [ Measurement_Combo.SelectedIndex ];
      var Entry = _Measurements [ Filtered_Index ];

      return Type switch
      {
        Meter_Type.HP3458 => Entry.Cmd_3458,
        Meter_Type.HP34401 => Entry.Cmd_34401,
        _ => Entry.Cmd_Generic_GPIB  // SCPI default for generic instruments
      };
    }


    private string old_Get_Measurement_Command ( Meter_Type Type )
    {
      int Filtered_Index = _Filtered_Indices [ Measurement_Combo.SelectedIndex ];
      var Entry = _Measurements [ Filtered_Index ];
      return Type == Meter_Type.HP3458
          ? Entry.Cmd_3458
          : Entry.Cmd_34401;
    }

    private void Update_Legend_Throttled ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Only update legend every 2 seconds to reduce flicker
      if ( ( DateTime.Now - _Last_Legend_Update ).TotalSeconds < 2.0 )
        return;

      _Last_Legend_Update = DateTime.Now;
      Update_Legend ( );
    }















    private void Update_Legend ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Block?.Trace ( $"Series count: {_Series.Count}" );
      Block?.Trace ( $"Stats labels count: {_Stats_Labels.Count}" );

      // Only rebuild if we don't have labels for all series
      if ( _Stats_Labels.Count != _Series.Count )
      {
        Block?.Trace ( "Rebuilding legend (series count changed)" );
        Build_Legend_Controls ( );
      }
      else
      {
        Block?.Trace ( "Updating stats only (no rebuild)" );
        Update_Legend_Stats_Only ( );
      }
    }

    private void Build_Legend_Controls ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Legend_Panel_2.SuspendLayout ( );
      _Legend_Panel_2.Controls.Clear ( );
      _Stats_Labels.Clear ( );
      _Legend_Panel_2.BackColor = _Theme.Background;

      int Y_Pos = 5;

      for ( int I = 0; I < _Series.Count; I++ )
      {
        var S = _Series [ I ];

        var Legend_Item = new Panel
        {
          Location = new Point ( 5, Y_Pos ),
          Size = new Size ( 185, 120 ),
          BackColor = _Theme.Background
        };

        var Color_Box = new Panel
        {
          Location = new Point ( 2, 4 ),
          Size = new Size ( 14, 14 ),
          BackColor = _Theme.Line_Colors [ I % _Theme.Line_Colors.Length ],
          BorderStyle = BorderStyle.FixedSingle
        };

        var Name_Label = new Label
        {
          Location = new Point ( 20, 2 ),
          Size = new Size ( 160, 16 ),
          Text = S.Name,
          ForeColor = _Theme.Foreground,
          Font = new Font ( this.Font.FontFamily, 8f, FontStyle.Bold ),
          AutoSize = false
        };

        var Stats_Label = new Label
        {
          Location = new Point ( 20, 18 ),
          Size = new Size ( 160, 85 ),
          Text = "No data",
          ForeColor = _Theme.Foreground,
          Font = new Font ( "Consolas", 10f ),
          AutoSize = false
        };

        // Store reference with UNIQUE key (name + address)
        string Key = $"{S.Name}_{S.Address}";  // ← Changed from S.Name
        _Stats_Labels [ Key ] = Stats_Label;

        Legend_Item.Controls.Add ( Color_Box );
        Legend_Item.Controls.Add ( Name_Label );
        Legend_Item.Controls.Add ( Stats_Label );
        _Legend_Panel_2.Controls.Add ( Legend_Item );
        Y_Pos += 123;
      }

      _Legend_Panel_2.ResumeLayout ( );

      // Now update with actual stats (if any data exists)
      Update_Legend_Stats_Only ( );
    }

    private void Update_Legend_Stats_Only ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Block?.Trace ( $"Updating stats for {_Series.Count} series" );

      foreach ( var S in _Series )
      {
        string Key = $"{S.Name}_{S.Address}";  // ← Use same key format

        if ( !_Stats_Labels.ContainsKey ( Key ) )
        {
          Block?.Trace ( $"  {S.Name}: Label not found in dictionary!" );
          continue;
        }

        int Point_Count = S.Points.Count;
        Block?.Trace ( $"  {S.Name}: {Point_Count} points" );

        string Stats_Text;

        if ( Point_Count > 0 )
        {
          try
          {
            double Average = S.Get_Average ( );
            double Standard_Deviation = S.Get_StdDev ( );
            double Minimum = S.Get_Min ( );
            double Maximum = S.Get_Max ( );
            double Last = S.Get_Last ( );
            string Trend = S.Get_Trend ( );

            Stats_Text =
              $"Last: {Last:F6} {Trend}\n" +
              $"Avg : {Average:F6}\n" +
              $"σ   : {Standard_Deviation:F6}\n" +
              $"Min : {Minimum:F6}\n" +
              $"Max : {Maximum:F6}\n" +
              $"N   = {Point_Count}";

            Block?.Trace ( $"    Stats calculated successfully" );
          }
          catch ( Exception ex )
          {
            Block?.Trace ( $"    ERROR calculating stats: {ex.Message}" );
            Stats_Text = "Error";
          }
        }
        else
        {
          Stats_Text = "No data";
          Block?.Trace ( $"    No data yet" );
        }

        // Update the label text using the unique key
        _Stats_Labels [ Key ].Text = Stats_Text;
      }
    }


    private void a_Draw_Combined_Normalized ( Graphics G, int W, int H, int Chart_W, int Chart_H,
  DateTime Time_Min, double Time_Range_Sec, int Margin_Left, int Margin_Right, int Margin_Bottom,
  Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Y axis shows 0-100%
      Draw_Y_Axis_Percentage ( G, W, H, Chart_H,
        Margin_Left, Margin_Right, Margin_Bottom, Grid_Pen, Label_Font, Label_Brush );

      // Draw each series normalized to its own range
      int Color_Index = 0;
      foreach ( var S in _Series )
      {
        if ( !S.Visible || S.Points.Count == 0 )
          continue;

        Color Line_Color = _Theme.Line_Colors [ Color_Index % _Theme.Line_Colors.Length ];

        // Get visible points only
        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

        if ( Visible_Count == 0 )
        {
          Color_Index++;
          continue;
        }

        // Calculate min/max for VISIBLE points only
        double Series_Min = double.MaxValue;
        double Series_Max = double.MinValue;

        for ( int i = Start_Index; i < Start_Index + Visible_Count; i++ )
        {
          if ( S.Points [ i ].Value < Series_Min )
            Series_Min = S.Points [ i ].Value;
          if ( S.Points [ i ].Value > Series_Max )
            Series_Max = S.Points [ i ].Value;
        }

        double Series_Range = Series_Max - Series_Min;

        if ( Series_Range == 0 )
          Series_Range = 1;

        Block?.Trace ( $"{S.Name}: {Series_Min:F6} to {Series_Max:F6}" );

        // Recalculate time range for VISIBLE points
        DateTime Visible_Time_Min = S.Points [ Start_Index ].Time;
        DateTime Visible_Time_Max = S.Points [ Start_Index + Visible_Count - 1 ].Time;
        double Visible_Time_Range_Sec = ( Visible_Time_Max - Visible_Time_Min ).TotalSeconds;

        if ( Visible_Time_Range_Sec < 0.001 )
          Visible_Time_Range_Sec = 1.0;

        var Points = new List<PointF> ( );

        // Build points from VISIBLE range only
        for ( int i = 0; i < Visible_Count; i++ )
        {
          int Data_Index = Start_Index + i;
          var P = S.Points [ Data_Index ];

          double Time_Offset = ( P.Time - Visible_Time_Min ).TotalSeconds;
          float X_Ratio = (float) ( Time_Offset / Visible_Time_Range_Sec );
          float X = Margin_Left + ( X_Ratio * Chart_W );

          // Normalize to 0-1 based on THIS series' own range
          double Normalized = ( P.Value - Series_Min ) / Series_Range;
          float Y = H - Margin_Bottom - (float) ( Normalized * Chart_H );

          Points.Add ( new PointF ( X, Y ) );
        }

        // Draw line
        if ( Points.Count > 1 )
        {
          using ( var Line_Pen = new Pen ( Line_Color, 2f ) )
          {
            G.DrawLines ( Line_Pen, Points.ToArray ( ) );
          }
        }

        // Draw dots
        foreach ( var Point in Points )
        {
          using ( var Brush = new SolidBrush ( Line_Color ) )
          {
            G.FillEllipse ( Brush, Point.X - 2, Point.Y - 2, 4, 4 );
          }
        }

        // Draw last point
        if ( Points.Count > 0 )
        {
          var Last = Points [ Points.Count - 1 ];
          using ( var Brush = new SolidBrush ( Line_Color ) )
          {
            G.FillEllipse ( Brush, Last.X - 4, Last.Y - 4, 8, 8 );
          }
        }

        Color_Index++;
      }
    }





    private void Draw_Y_Axis_Percentage ( Graphics G, int W, int H, int Chart_H,
  int Margin_Left, int Margin_Right, int Margin_Bottom,
  Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Draw 0%, 25%, 50%, 75%, 100%
      for ( int I = 0; I <= 4; I++ )
      {
        float Y_Ratio = I / 4f;
        float Y = H - Margin_Bottom - ( Y_Ratio * Chart_H );

        // Grid line
        G.DrawLine ( Grid_Pen, Margin_Left, Y, W - Margin_Right, Y );

        // Label
        string Label = $"{I * 25}%";
        SizeF Label_Size = G.MeasureString ( Label, Label_Font );
        G.DrawString ( Label, Label_Font, Label_Brush,
          Margin_Left - Label_Size.Width - 5, Y - Label_Size.Height / 2 );
      }
    }



    private void Draw_Combined_Legend_With_Ranges ( Graphics G, int W, int Margin_Top, int Margin_Right )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int X = W - 280;
      int Y = 5;

      using ( var Font = new Font ( this.Font.FontFamily, 7.5f ) )
      using ( var Brush = new SolidBrush ( _Theme.Foreground ) )
      {
        int Color_Index = 0;
        foreach ( var S in _Series.Where ( s => s.Visible && s.Points.Count > 0 ) )
        {
          Color Line_Color = _Theme.Line_Colors [ Color_Index % _Theme.Line_Colors.Length ];

          // Draw color box
          using ( var Color_Brush = new SolidBrush ( Line_Color ) )
          {
            G.FillRectangle ( Color_Brush, X, Y, 14, 14 );
          }
          using ( var Border_Pen = new Pen ( _Theme.Foreground, 1f ) )
          {
            G.DrawRectangle ( Border_Pen, X, Y, 14, 14 );
          }

          // Show name and actual range
          double Min = S.Get_Min ( );
          double Max = S.Get_Max ( );
          double Last = S.Get_Last ( );

          string Display = $"{S.Name}";
          G.DrawString ( Display, Font, Brush, X + 20, Y );
          Y += 15;

          string Range = $"  {Min:F6} - {Max:F6}";
          G.DrawString ( Range, Font, Brush, X + 20, Y );
          Y += 15;

          string Current = $"  Now: {Last:F6}";
          G.DrawString ( Current, Font, Brush, X + 20, Y );
          Y += 20;

          Color_Index++;
        }
      }
    }




    private void View_Mode_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Combined_View = !_Combined_View;

      var Button = (Button) sender;
      Button.Text = _Combined_View ? "Split View" : "Combined View";

      // Show/hide normalize button
      Normalize_Button.Visible = _Combined_View;

      // Enable/disable zoom slider based on view mode and normalization
      Zoom_Slider.Enabled = !( _Combined_View && _Normalized_View );

      Block?.Trace ( $"View mode: {( _Combined_View ? "Combined" : "Split" )}" );

      Chart_Panel.Invalidate ( );
    }

    private void Normalize_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Normalized_View = !_Normalized_View;

      var Button = (Button) sender;
      Button.Text = _Normalized_View ? "Absolute" : "Normalize";

      // Disable zoom slider when normalized
      Zoom_Slider.Enabled = !_Normalized_View;

      Block?.Trace ( $"Normalized: {_Normalized_View}" );

      Chart_Panel.Invalidate ( );
    }

    private void Draw_Combined_View ( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Block?.Trace ( $"_Current_Graph_Style = [{_Current_Graph_Style}]" );

      Block?.Trace ( $"Normalized mode: {_Normalized_View}" );

      var (Time_Min, Time_Max) = Calculate_Time_Range ( );
      double Time_Range_Sec = ( Time_Max - Time_Min ).TotalSeconds;
      if ( Time_Range_Sec < 0.001 )
        Time_Range_Sec = 1.0;

      int Margin_Left = 80;
      int Margin_Right = 20;
      int Margin_Top = 40;
      int Margin_Bottom = 50;

      int Chart_W = W - Margin_Left - Margin_Right;
      int Chart_H = H - Margin_Top - Margin_Bottom;

      if ( Chart_W < 10 || Chart_H < 10 )
        return;

      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Label_Font = new Font ( "Consolas", 7.5F );
      using var Name_Font = new Font ( "Segoe UI", 8F, FontStyle.Bold );
      using var Label_Brush = new SolidBrush ( _Theme.Labels );

      // Draw time axis (same for both modes)
      Draw_Time_Axis ( G, H, Chart_W, Time_Range_Sec,
        Margin_Left, Margin_Bottom, Margin_Top, Grid_Pen, Label_Brush );

      // Draw instrument names at top
      int Name_X = Margin_Left + 5;
      int Name_Y = 5;

      int Color_Index = 0;
      foreach ( var S in _Series.Where ( s => s.Visible ) )
      {
        Color Line_Color = _Theme.Line_Colors [ Color_Index % _Theme.Line_Colors.Length ];

        // Draw color box
        using ( var Color_Brush = new SolidBrush ( Line_Color ) )
        {
          G.FillRectangle ( Color_Brush, Name_X, Name_Y + 2, 14, 14 );
        }
        using ( var Border_Pen = new Pen ( _Theme.Foreground, 1f ) )
        {
          G.DrawRectangle ( Border_Pen, Name_X, Name_Y + 2, 14, 14 );
        }

        // Draw instrument name
        using ( var Name_Brush = new SolidBrush ( Line_Color ) )
        {
          G.DrawString ( S.Name, Name_Font, Name_Brush, Name_X + 20, Name_Y );
        }

        SizeF Name_Size = G.MeasureString ( S.Name, Name_Font );
        Name_X += (int) Name_Size.Width + 40;

        Color_Index++;
      }

      // Branch based on normalized vs absolute
      switch ( _Current_Graph_Style )
      {
        case "Scatter":
          Draw_Combined_Scatter ( G, W, H, Chart_W, Chart_H,
            Time_Min, Time_Range_Sec, Margin_Left, Margin_Right, Margin_Bottom,
            Grid_Pen, Label_Font, Label_Brush );
          break;

        case "Step":
          Draw_Combined_Step ( G, W, H, Chart_W, Chart_H,
            Time_Min, Time_Range_Sec, Margin_Left, Margin_Right, Margin_Bottom,
            Grid_Pen, Label_Font, Label_Brush );
          break;

        case "Bar":
          Draw_Single_Bar_Combined ( G, W, H, Chart_W, Chart_H,
            Margin_Left, Margin_Right, Margin_Bottom );
          break;

        case "Histogram":
          Draw_Single_Histogram ( G, W, H, Margin_Left, Margin_Right,
            Margin_Top, Margin_Bottom, Chart_W, Chart_H );
          break;

        case "Pie":
          Draw_Single_Pie ( G, W, H, Margin_Left, Margin_Right,
            Margin_Top, Margin_Bottom, Chart_W, Chart_H );
          break;

        default: // Line
          if ( _Normalized_View )
            Draw_Combined_Normalized ( G, W, H, Chart_W, Chart_H,
              Time_Min, Time_Range_Sec, Margin_Left, Margin_Right, Margin_Bottom,
              Grid_Pen, Label_Font, Label_Brush );
          else
            Draw_Combined_Absolute ( G, W, H, Chart_W, Chart_H,
              Time_Min, Time_Range_Sec, Margin_Left, Margin_Right, Margin_Bottom,
              Grid_Pen, Label_Font, Label_Brush );
          break;
      }

      Block?.Trace ( $"_Current_Graph_Style = [{_Current_Graph_Style}]" );
    }



    private void Draw_Combined_Scatter ( Graphics G, int W, int H,
  int Chart_W, int Chart_H,
  DateTime Time_Min, double Time_Range_Sec,
  int Margin_Left, int Margin_Right, int Margin_Bottom,
  Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Y range calculation identical to Absolute
      double Global_Min = double.MaxValue;
      double Global_Max = double.MinValue;

      foreach ( var S in _Series.Where ( s => s.Visible && s.Points.Count > 0 ) )
      {
        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
          S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if ( Visible_Count == 0 )
          continue;
        for ( int I = Start_Index; I < Start_Index + Visible_Count; I++ )
        {
          if ( S.Points [ I ].Value < Global_Min )
            Global_Min = S.Points [ I ].Value;
          if ( S.Points [ I ].Value > Global_Max )
            Global_Max = S.Points [ I ].Value;
        }
      }

      if ( Global_Min == double.MaxValue )
        return;

      double Range = Global_Max - Global_Min;
      double Padding = Math.Max ( Range * 0.5, 0.0001 );
      double Display_Min = Global_Min - Padding;
      double Display_Max = Global_Max + Padding;
      double Display_Range = Display_Max - Display_Min;
      if ( Display_Range == 0 )
        Display_Range = 0.001;

      Draw_Y_Axis ( G, Display_Min, Display_Range, W, H, Chart_H,
        Margin_Left, Margin_Right, Margin_Bottom, Grid_Pen, Label_Font, Label_Brush );

      int Color_Index = 0;
      foreach ( var S in _Series )
      {
        if ( !S.Visible || S.Points.Count == 0 )
        {
          Color_Index++;
          continue;
        }

        Color Line_Color = _Theme.Line_Colors [ Color_Index % _Theme.Line_Colors.Length ];

        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
          S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if ( Visible_Count == 0 )
        {
          Color_Index++;
          continue;
        }

        DateTime Visible_Time_Min = S.Points [ Start_Index ].Time;
        DateTime Visible_Time_Max = S.Points [ Start_Index + Visible_Count - 1 ].Time;
        double Visible_Time_Range = Math.Max (
          ( Visible_Time_Max - Visible_Time_Min ).TotalSeconds, 0.001 );

        float Dot_Size = Visible_Count > 100 ? 5f : Visible_Count > 50 ? 7f : 9f;

        using var Dot_Brush = new SolidBrush ( Line_Color );
        using var Ring_Pen = new Pen ( Line_Color, 1.5f );

        for ( int I = 0; I < Visible_Count; I++ )
        {
          var P = S.Points [ Start_Index + I ];
          float X = Margin_Left + (float) (
            ( P.Time - Visible_Time_Min ).TotalSeconds / Visible_Time_Range * Chart_W );
          double Y_Norm = ( P.Value - Display_Min ) / Display_Range;
          float Y = H - Margin_Bottom - (float) ( Y_Norm * Chart_H );

          G.FillEllipse ( Dot_Brush,
            X - Dot_Size / 2, Y - Dot_Size / 2, Dot_Size, Dot_Size );
          G.DrawEllipse ( Ring_Pen,
            X - Dot_Size / 2 - 1, Y - Dot_Size / 2 - 1,
            Dot_Size + 2, Dot_Size + 2 );
        }

        Color_Index++;
      }
    }


    private void Draw_Combined_Step ( Graphics G, int W, int H,
      int Chart_W, int Chart_H,
      DateTime Time_Min, double Time_Range_Sec,
      int Margin_Left, int Margin_Right, int Margin_Bottom,
      Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Y range identical to Absolute
      double Global_Min = double.MaxValue;
      double Global_Max = double.MinValue;

      foreach ( var S in _Series.Where ( s => s.Visible && s.Points.Count > 0 ) )
      {
        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
          S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if ( Visible_Count == 0 )
          continue;
        for ( int I = Start_Index; I < Start_Index + Visible_Count; I++ )
        {
          if ( S.Points [ I ].Value < Global_Min )
            Global_Min = S.Points [ I ].Value;
          if ( S.Points [ I ].Value > Global_Max )
            Global_Max = S.Points [ I ].Value;
        }
      }

      if ( Global_Min == double.MaxValue )
        return;

      double Range = Global_Max - Global_Min;
      double Padding = Math.Max ( Range * 0.5, 0.0001 );
      double Display_Min = Global_Min - Padding;
      double Display_Max = Global_Max + Padding;
      double Display_Range = Display_Max - Display_Min;
      if ( Display_Range == 0 )
        Display_Range = 0.001;

      Draw_Y_Axis ( G, Display_Min, Display_Range, W, H, Chart_H,
        Margin_Left, Margin_Right, Margin_Bottom, Grid_Pen, Label_Font, Label_Brush );

      int Color_Index = 0;
      foreach ( var S in _Series )
      {
        if ( !S.Visible || S.Points.Count == 0 )
        {
          Color_Index++;
          continue;
        }

        Color Line_Color = _Theme.Line_Colors [ Color_Index % _Theme.Line_Colors.Length ];

        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
          S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if ( Visible_Count == 0 )
        {
          Color_Index++;
          continue;
        }

        DateTime Visible_Time_Min = S.Points [ Start_Index ].Time;
        DateTime Visible_Time_Max = S.Points [ Start_Index + Visible_Count - 1 ].Time;
        double Visible_Time_Range = Math.Max (
          ( Visible_Time_Max - Visible_Time_Min ).TotalSeconds, 0.001 );

        // Build step points
        var Step_Points = new List<PointF> ( );

        for ( int I = 0; I < Visible_Count; I++ )
        {
          var P = S.Points [ Start_Index + I ];
          float X = Margin_Left + (float) (
            ( P.Time - Visible_Time_Min ).TotalSeconds / Visible_Time_Range * Chart_W );
          double Y_Norm = ( P.Value - Display_Min ) / Display_Range;
          float Y = H - Margin_Bottom - (float) ( Y_Norm * Chart_H );

          if ( I > 0 )
            Step_Points.Add ( new PointF ( X, Step_Points [ Step_Points.Count - 1 ].Y ) );

          Step_Points.Add ( new PointF ( X, Y ) );
        }

        if ( Step_Points.Count > 1 )
        {
          using var Line_Pen = new Pen ( Line_Color, 1.5f );
          G.DrawLines ( Line_Pen, Step_Points.ToArray ( ) );
        }

        Color_Index++;
      }
    }

    private void Draw_Single_Bar_Combined ( Graphics G, int W, int H,
  int Chart_W, int Chart_H,
  int Margin_Left, int Margin_Right, int Margin_Bottom )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Only works with first visible series
      var S = _Series.FirstOrDefault ( s => s.Visible && s.Points.Count > 0 );
      if ( S == null )
        return;

      var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
        S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

      int Actual_Count = Math.Min ( Visible_Count, S.Points.Count - Start_Index );
      if ( Actual_Count < 1 )
        return;

      double Min_V = double.MaxValue;
      double Max_V = double.MinValue;
      for ( int I = Start_Index; I < Start_Index + Actual_Count; I++ )
      {
        if ( S.Points [ I ].Value < Min_V )
          Min_V = S.Points [ I ].Value;
        if ( S.Points [ I ].Value > Max_V )
          Max_V = S.Points [ I ].Value;
      }

      double Padding = Math.Max ( ( Max_V - Min_V ) * 0.1, 0.0001 );
      double Display_Min = Min_V - Padding;
      double Display_Max = Max_V + Padding;
      double Display_Range = Display_Max - Display_Min;
      if ( Display_Range == 0 )
        Display_Range = 0.001;

      float Baseline = H - Margin_Bottom;
      float Bar_Width = Math.Max ( 2f, ( Chart_W / (float) Actual_Count ) * 0.7f );
      float Half_Bar = Bar_Width / 2;

      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Label_Font = new Font ( "Consolas", 7.5f );
      using var Label_Brush = new SolidBrush ( _Theme.Labels );

      Draw_Y_Axis ( G, Display_Min, Display_Range, W, H, Chart_H,
        Margin_Left, Margin_Right, Margin_Bottom,
        Grid_Pen, Label_Font, Label_Brush );

      Color Bar_Color = _Theme.Line_Colors [ 0 ];
      using var Bar_Brush = new SolidBrush ( Bar_Color );
      using var Bar_Border_Pen = new Pen (
        Color.FromArgb (
          (int) ( Bar_Color.R * 0.7 ),
          (int) ( Bar_Color.G * 0.7 ),
          (int) ( Bar_Color.B * 0.7 ) ), 1f );

      for ( int I = 0; I < Actual_Count; I++ )
      {
        var P = S.Points [ Start_Index + I ];
        float X = Margin_Left + (float) I / Math.Max ( 1, Actual_Count - 1 ) * Chart_W;
        double Y_N = ( P.Value - Display_Min ) / Display_Range;
        float Y = H - Margin_Bottom - (float) ( Y_N * Chart_H );
        float Bar_H = Math.Max ( 1f, Baseline - Y );

        RectangleF Rect = new RectangleF (
          X - Half_Bar, Y, Bar_Width, Bar_H );

        G.FillRectangle ( Bar_Brush, Rect );
        G.DrawRectangle ( Bar_Border_Pen,
          Rect.X, Rect.Y, Rect.Width, Rect.Height );
      }
    }

    private void Draw_Combined_Absolute ( Graphics G, int W, int H, int Chart_W, int Chart_H,
    DateTime Time_Min, double Time_Range_Sec, int Margin_Left, int Margin_Right, int Margin_Bottom,
    Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Calculate global Y range from VISIBLE points only
      double Global_Min = double.MaxValue;
      double Global_Max = double.MinValue;

      foreach ( var S in _Series.Where ( s => s.Visible && s.Points.Count > 0 ) )
      {
        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

        if ( Visible_Count == 0 )
          continue;

        for ( int i = Start_Index; i < Start_Index + Visible_Count; i++ )
        {
          if ( S.Points [ i ].Value < Global_Min )
            Global_Min = S.Points [ i ].Value;
          if ( S.Points [ i ].Value > Global_Max )
            Global_Max = S.Points [ i ].Value;
        }
      }

      if ( Global_Min == double.MaxValue )
        return;

      double Range = Global_Max - Global_Min;

      // MUCH better padding for visualization
      double Padding = Math.Max ( Range * 0.5, 0.0001 );
      double Padded_Min = Global_Min - Padding;
      double Padded_Max = Global_Max + Padding;
      double Padded_Range = Padded_Max - Padded_Min;

      if ( Padded_Range == 0 )
        Padded_Range = 0.001;

      Block?.Trace ( $"Range: {Global_Min:F6} to {Global_Max:F6}" );
      Block?.Trace ( $"Padded: {Padded_Min:F6} to {Padded_Max:F6}" );

      // **APPLY ZOOM HERE**
      double Display_Min = Padded_Min;
      double Display_Max = Padded_Max;
      double Display_Range = Padded_Range;

      if ( _Zoom_Factor > 0 && _Zoom_Factor != 1.0 )
      {
        double Center = ( Padded_Max + Padded_Min ) / 2.0;
        double Zoomed_Range = Padded_Range / _Zoom_Factor;

        Display_Min = Center - ( Zoomed_Range / 2.0 );
        Display_Max = Center + ( Zoomed_Range / 2.0 );
        Display_Range = Display_Max - Display_Min;

        Block?.Trace ( $"Combined ZOOM: {_Zoom_Factor:F2}x -> [{Display_Min:F6}, {Display_Max:F6}]" );
      }

      // Y axis shows actual voltages (use Display values)
      Draw_Y_Axis ( G, Display_Min, Display_Range, W, H, Chart_H,
        Margin_Left, Margin_Right, Margin_Bottom, Grid_Pen, Label_Font, Label_Brush );

      // Draw each series
      int Color_Index = 0;
      foreach ( var S in _Series )
      {
        if ( !S.Visible || S.Points.Count == 0 )
          continue;

        Color Line_Color = _Theme.Line_Colors [ Color_Index % _Theme.Line_Colors.Length ];

        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );

        if ( Visible_Count == 0 )
        {
          Color_Index++;
          continue;
        }

        // Recalculate time range for VISIBLE points
        DateTime Visible_Time_Min = S.Points [ Start_Index ].Time;
        DateTime Visible_Time_Max = S.Points [ Start_Index + Visible_Count - 1 ].Time;
        double Visible_Time_Range_Sec = ( Visible_Time_Max - Visible_Time_Min ).TotalSeconds;

        if ( Visible_Time_Range_Sec < 0.001 )
          Visible_Time_Range_Sec = 1.0;

        var Points = new List<PointF> ( );

        for ( int i = 0; i < Visible_Count; i++ )
        {
          int Data_Index = Start_Index + i;
          var P = S.Points [ Data_Index ];

          double Time_Offset = ( P.Time - Visible_Time_Min ).TotalSeconds;
          float X_Ratio = (float) ( Time_Offset / Visible_Time_Range_Sec );
          float X = Margin_Left + ( X_Ratio * Chart_W );

          // Use Display_Min and Display_Range for Y calculation
          double Y_Normalized = ( P.Value - Display_Min ) / Display_Range;
          float Y = H - Margin_Bottom - (float) ( Y_Normalized * Chart_H );

          Points.Add ( new PointF ( X, Y ) );
        }

        // Draw line
        if ( Points.Count > 1 )
        {
          using ( var Line_Pen = new Pen ( Line_Color, 2f ) )
          {
            G.DrawLines ( Line_Pen, Points.ToArray ( ) );
          }
        }

        // Draw dots
        foreach ( var Point in Points )
        {
          using ( var Brush = new SolidBrush ( Line_Color ) )
          {
            G.FillEllipse ( Brush, Point.X - 2, Point.Y - 2, 4, 4 );
          }
        }

        // Draw last point
        if ( Points.Count > 0 )
        {
          var Last = Points [ Points.Count - 1 ];
          using ( var Brush = new SolidBrush ( Line_Color ) )
          {
            G.FillEllipse ( Brush, Last.X - 4, Last.Y - 4, 8, 8 );
          }
        }

        Color_Index++;
      }
    }





    private void Draw_Combined_Normalized ( Graphics G, int W, int H, int Chart_W, int Chart_H,
      DateTime Time_Min, double Time_Range_Sec, int Margin_Left, int Margin_Right, int Margin_Bottom,
      Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );





      Draw_Y_Axis_Percentage ( G, W, H, Chart_H,
        Margin_Left, Margin_Right, Margin_Bottom, Grid_Pen, Label_Font, Label_Brush );

      int Color_Index = 0;
      foreach ( var S in _Series )
      {
        if ( !S.Visible || S.Points.Count == 0 )
          continue;

        var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
  S.Points.Count, _Enable_Rolling, _Max_Display_Points, _View_Offset );
        if ( Visible_Count == 0 )
        {
          Color_Index++;
          continue;
        }

        double Series_Min = double.MaxValue;
        double Series_Max = double.MinValue;
        for ( int i = Start_Index; i < Start_Index + Visible_Count; i++ )
        {
          if ( S.Points [ i ].Value < Series_Min )
            Series_Min = S.Points [ i ].Value;
          if ( S.Points [ i ].Value > Series_Max )
            Series_Max = S.Points [ i ].Value;
        }

        double Series_Range = Series_Max - Series_Min;

        Color Line_Color = _Theme.Line_Colors [ Color_Index % _Theme.Line_Colors.Length ];

        // --- Draw the normalized line ---
        using var Line_Pen = new Pen ( Line_Color, 1.5f );
        var Points = new List<PointF> ( Visible_Count );

        for ( int i = Start_Index; i < Start_Index + Visible_Count; i++ )
        {
          var Pt = S.Points [ i ];
          double Elapsed_Sec = ( Pt.Time - Time_Min ).TotalSeconds;
          float X = Margin_Left + (float) ( Elapsed_Sec / Time_Range_Sec * Chart_W );

          // Normalize to 0-1, then map to chart height
          double Normalized = Series_Range > 0
            ? ( Pt.Value - Series_Min ) / Series_Range
            : 0.5;
          float Y = H - Margin_Bottom - (float) ( Normalized * Chart_H );

          Points.Add ( new PointF ( X, Y ) );
        }

        if ( Points.Count > 1 )
          G.DrawLines ( Line_Pen, Points.ToArray ( ) );
        // ---

        // Value labels
        using var Value_Brush = new SolidBrush ( Color.FromArgb ( 180, Line_Color ) );

        // Top label (100% = Series_Max)
        string Top_Label = Format_Axis_Value ( Series_Max );
        float Top_Y = H - Margin_Bottom - Chart_H;
        G.DrawString ( Top_Label, Label_Font, Value_Brush,
            Margin_Left + 4, Top_Y + Color_Index * 12 );

        // Bottom label (0% = Series_Min)
        string Bot_Label = Format_Axis_Value ( Series_Min );
        float Bot_Y = H - Margin_Bottom - 12;
        G.DrawString ( Bot_Label, Label_Font, Value_Brush,
            Margin_Left + 4, Bot_Y - Color_Index * 12 );

        Color_Index++;
      }
    }


    private void B_Draw_Combined_Normalized ( Graphics G, int W, int H, int Chart_W, int Chart_H,
DateTime Time_Min, double Time_Range_Sec, int Margin_Left, int Margin_Right, int Margin_Bottom,
Pen Grid_Pen, Font Label_Font, Brush Label_Brush )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Y axis shows 0-100%
      Draw_Y_Axis_Percentage ( G, W, H, Chart_H,
        Margin_Left, Margin_Right, Margin_Bottom, Grid_Pen, Label_Font, Label_Brush );

      // Draw each series normalized to its own range
      int Color_Index = 0;
      foreach ( var S in _Series )
      {
        if ( !S.Visible || S.Points.Count == 0 )
          continue;

        Color Line_Color = _Theme.Line_Colors [ Color_Index % _Theme.Line_Colors.Length ];

        double Series_Min = S.Get_Min ( );
        double Series_Max = S.Get_Max ( );
        double Series_Range = Series_Max - Series_Min;

        if ( Series_Range == 0 )
          Series_Range = 1;

        Block?.Trace ( $"{S.Name}: {Series_Min:F6} to {Series_Max:F6}" );

        var Points = new List<PointF> ( );

        foreach ( var P in S.Points )
        {
          double Time_Offset = ( P.Time - Time_Min ).TotalSeconds;
          float X_Ratio = (float) ( Time_Offset / Time_Range_Sec );
          float X = Margin_Left + ( X_Ratio * Chart_W );

          // Normalize to 0-1 based on THIS series' own range
          double Normalized = ( P.Value - Series_Min ) / Series_Range;
          float Y = H - Margin_Bottom - (float) ( Normalized * Chart_H );

          Points.Add ( new PointF ( X, Y ) );
        }

        // Draw line
        if ( Points.Count > 1 )
        {
          using ( var Line_Pen = new Pen ( Line_Color, 2f ) )
          {
            G.DrawLines ( Line_Pen, Points.ToArray ( ) );
          }
        }

        // Draw dots
        foreach ( var Point in Points )
        {
          using ( var Brush = new SolidBrush ( Line_Color ) )
          {
            G.FillEllipse ( Brush, Point.X - 2, Point.Y - 2, 4, 4 );
          }
        }

        // Draw last point
        if ( Points.Count > 0 )
        {
          var Last = Points [ Points.Count - 1 ];
          using ( var Brush = new SolidBrush ( Line_Color ) )
          {
            G.FillEllipse ( Brush, Last.X - 4, Last.Y - 4, 8, 8 );
          }
        }

        Color_Index++;
      }
    }



    public void Show_Progress ( string Message, Color Foreground_Color )
    {
      Progress_Text_Box.Text = Message;
      Progress_Text_Box.ForeColor = Foreground_Color;

    }

    private void Chart_Panel_Mouse_Wheel ( object sender, MouseEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Enable_Rolling )
        return;

      // Zoom in/out by 10 points per scroll
      int Delta = e.Delta > 0 ? 10 : -10;
      int New_Value = _Max_Display_Points + Delta;

      // Clamp between 10 and total points
      int Total_Points = _Series.Max ( s => s.Points.Count );
      New_Value = Math.Max ( 10, Math.Min ( New_Value, Total_Points ) );

      _Max_Display_Points = New_Value;
      Max_Points_Numeric.Value = New_Value;

      Block?.Trace ( $"Zoomed to {_Max_Display_Points} points" );
      Chart_Panel.Invalidate ( );
    }









    // Add keyboard support for panning:
    protected override bool ProcessCmdKey ( ref Message msg, Keys keyData )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Total_Points = _Series.Max ( s => s?.Points?.Count ?? 0 );
      int Max_Offset = Math.Max ( 0, Total_Points - _Max_Display_Points );

      switch ( keyData )
      {
        case Keys.Left:
          // Pan left (look back in time)
          if ( _Enable_Rolling )
          {
            _Auto_Scroll = false;
            _View_Offset = Math.Min ( _View_Offset + 10, Max_Offset );
            Update_Scrollbar_Position ( );
            Chart_Panel.Invalidate ( );
          }
          return true;

        case Keys.Right:
          // Pan right (move toward present)
          if ( _Enable_Rolling )
          {
            _View_Offset = Math.Max ( 0, _View_Offset - 10 );
            if ( _View_Offset == 0 )
              _Auto_Scroll = true;
            Update_Scrollbar_Position ( );
            Chart_Panel.Invalidate ( );
          }
          return true;

        case Keys.Home:
          // Jump to oldest data
          if ( _Enable_Rolling )
          {
            _Auto_Scroll = false;
            _View_Offset = Max_Offset;
            Update_Scrollbar_Position ( );
            Chart_Panel.Invalidate ( );
          }
          return true;

        case Keys.End:
          // Jump to most recent data
          _Auto_Scroll = true;
          _View_Offset = 0;
          Update_Scrollbar_Position ( );
          Chart_Panel.Invalidate ( );
          return true;

        case Keys.PageUp:
          // Pan left by one screen
          if ( _Enable_Rolling )
          {
            _Auto_Scroll = false;
            _View_Offset = Math.Min ( _View_Offset + _Max_Display_Points, Max_Offset );
            Update_Scrollbar_Position ( );
            Chart_Panel.Invalidate ( );
          }
          return true;

        case Keys.PageDown:
          // Pan right by one screen
          if ( _Enable_Rolling )
          {
            _View_Offset = Math.Max ( 0, _View_Offset - _Max_Display_Points );
            if ( _View_Offset == 0 )
              _Auto_Scroll = true;
            Update_Scrollbar_Position ( );
            Chart_Panel.Invalidate ( );
          }
          return true;
      }

      return base.ProcessCmdKey ( ref msg, keyData );
    }





    // Add mouse wheel support for horizontal scrolling:
    private void new_Chart_Panel_MouseWheel ( object sender, MouseEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // If Ctrl is held, zoom in/out
      if ( ModifierKeys.HasFlag ( Keys.Control ) )
      {
        int Delta = e.Delta > 0 ? -20 : 20;
        int New_Value = _Max_Display_Points + Delta;

        int Total_Points = _Series.Max ( s => s?.Points?.Count ?? 0 );
        New_Value = Math.Max ( 10, Math.Min ( New_Value, Total_Points ) );

        _Max_Display_Points = New_Value;
        Max_Points_Numeric.Value = New_Value;

        Multimeter_Common_Helpers_Class.Update_Scrollbar_Range (
  Pan_Scrollbar, _Series.Max ( s => s.Points.Count ),
  _Max_Display_Points, _Auto_Scroll, ref _View_Offset );
        Chart_Panel.Invalidate ( );
      }
      else if ( _Enable_Rolling )
      {
        // Otherwise, pan left/right
        _Auto_Scroll = false;

        int Total_Points = _Series.Max ( s => s?.Points?.Count ?? 0 );
        int Max_Offset = Math.Max ( 0, Total_Points - _Max_Display_Points );

        int Delta = e.Delta > 0 ? -50 : 50;  // Scroll wheel up = go forward in time
        _View_Offset = Math.Max ( 0, Math.Min ( Max_Offset, _View_Offset + Delta ) );

        if ( _View_Offset == 0 )
          _Auto_Scroll = true;

        Update_Scrollbar_Position ( );
        Chart_Panel.Invalidate ( );
      }
    }
    private void Draw_Position_Indicator ( Graphics G, int W, int H )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Enable_Rolling )
        return;

      int Total_Points = _Series.Max ( s => s?.Points?.Count ?? 0 );
      if ( Total_Points <= _Max_Display_Points )
        return;

      // Draw a small indicator showing current position
      int Indicator_Y = H - 45;
      int Indicator_W = 200;
      int Indicator_H = 20;
      int Indicator_X = W - Indicator_W - 20;

      // Background
      using ( var Bg_Brush = new SolidBrush ( Color.FromArgb ( 200, _Theme.Background ) ) )
      {
        G.FillRectangle ( Bg_Brush, Indicator_X, Indicator_Y, Indicator_W, Indicator_H );
      }

      // Border
      using ( var Border_Pen = new Pen ( _Theme.Grid, 1f ) )
      {
        G.DrawRectangle ( Border_Pen, Indicator_X, Indicator_Y, Indicator_W, Indicator_H );
      }

      // Position bar
      int Bar_W = (int) ( (double) _Max_Display_Points / Total_Points * Indicator_W );
      int Max_Offset = Total_Points - _Max_Display_Points;
      int Bar_X = Indicator_X + (int) ( ( 1.0 - (double) _View_Offset / Max_Offset ) * ( Indicator_W - Bar_W ) );

      using ( var Bar_Brush = new SolidBrush ( Color.FromArgb ( 150, Color.LightBlue ) ) )
      {
        G.FillRectangle ( Bar_Brush, Bar_X, Indicator_Y + 2, Bar_W, Indicator_H - 4 );
      }

      // Text
      string Position_Text = _Auto_Scroll ? "Live" : $"-{_View_Offset} pts";
      using ( var Text_Font = new Font ( "Segoe UI", 8F ) )
      using ( var Text_Brush = new SolidBrush ( _Theme.Foreground ) )
      {
        var Text_Size = G.MeasureString ( Position_Text, Text_Font );
        G.DrawString ( Position_Text, Text_Font, Text_Brush,
            Indicator_X + ( Indicator_W - Text_Size.Width ) / 2,
            Indicator_Y + ( Indicator_H - Text_Size.Height ) / 2 );
      }
    }





    private void Update_Scrollbar_Position ( )
    {


      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !Pan_Scrollbar.Enabled || Pan_Scrollbar.Maximum == 0 )
        return;

      int Total_Points = _Series.Max ( s => s?.Points?.Count ?? 0 );
      int Max_Offset = Math.Max ( 0, Total_Points - _Max_Display_Points );

      if ( Max_Offset == 0 )
      {
        Pan_Scrollbar.Value = 0;
        return;
      }

      // Convert offset to scrollbar value using the ACTUAL scrollbar range
      // 0 = most recent (offset 0)
      // Max = oldest (offset = Max_Offset)
      int Scrollbar_Max = Pan_Scrollbar.Maximum - Pan_Scrollbar.LargeChange + 1;
      int Scrollbar_Value = (int) ( (double) _View_Offset / Max_Offset * Scrollbar_Max );
      Scrollbar_Value = Math.Max ( 0, Math.Min ( Scrollbar_Max, Scrollbar_Value ) );

      Block?.Trace ( $"Setting scrollbar value to {Scrollbar_Value} (offset={_View_Offset}, max_offset={Max_Offset})" );

      Pan_Scrollbar.Value = Scrollbar_Value;
    }

    private bool _Updating_Scroll = false;

    private void Pan_Scrollbar_Scroll ( object sender, ScrollEventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      _Updating_Scroll = true;
      Auto_Scroll_Check.Checked = false;
      _Updating_Scroll = false;

      int Total_Points = _Series.Max ( s => s?.Points?.Count ?? 0 );
      if ( Total_Points == 0 )
        return;
      int Max_Offset = Math.Max ( 0, Total_Points - _Max_Display_Points );
      if ( Max_Offset == 0 )
        return;
      int Scrollbar_Max = Pan_Scrollbar.Maximum - Pan_Scrollbar.LargeChange + 1;
      int Clamped_Value = Math.Max ( 0, Math.Min ( Scrollbar_Max, e.NewValue ) );
      _View_Offset = (int) ( ( (double) Clamped_Value / Scrollbar_Max ) * Max_Offset );
      Block?.Trace ( $"Pan scrollbar: value={Clamped_Value}, offset={_View_Offset}, max_offset={Max_Offset}" );
      if ( _View_Offset == 0 )
      {
        _Updating_Scroll = true;
        Auto_Scroll_Check.Checked = true;
        _Updating_Scroll = false;
      }
      Chart_Panel.Invalidate ( );
    }

    private void Pan_Scrollbar_ValueChanged ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      _Updating_Scroll = true;
      Auto_Scroll_Check.Checked = false;
      _Updating_Scroll = false;

      int Total_Points = _Series.Max ( s => s?.Points?.Count ?? 0 );
      if ( Total_Points == 0 )
        return;
      int Max_Offset = Math.Max ( 0, Total_Points - _Max_Display_Points );
      if ( Max_Offset == 0 )
        return;
      int Scrollbar_Max = Pan_Scrollbar.Maximum - Pan_Scrollbar.LargeChange + 1;
      int Clamped_Value = Math.Max ( 0, Math.Min ( Scrollbar_Max, Pan_Scrollbar.Value ) );
      _View_Offset = (int) ( ( (double) Clamped_Value / Scrollbar_Max ) * Max_Offset );
      if ( _View_Offset == 0 )
      {
        _Updating_Scroll = true;
        Auto_Scroll_Check.Checked = true;
        _Updating_Scroll = false;
      }
      Chart_Panel.Invalidate ( );
    }

    private void Auto_Scroll_Check_CheckedChanged ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( _Updating_Scroll )
        return;
      _Auto_Scroll = Auto_Scroll_Check.Checked;
      if ( _Auto_Scroll )
      {
        _View_Offset = 0;
        if ( Pan_Scrollbar != null && Pan_Scrollbar.Enabled )
          Pan_Scrollbar.Value = 0;
        Chart_Panel.Invalidate ( );
      }
    }





    private void Zoom_Slider_Scroll ( object sender, EventArgs e )
    {
      Update_Zoom_From_Slider ( );
    }

    private void Zoom_Slider_ValueChanged ( object sender, EventArgs e )
    {
      Update_Zoom_From_Slider ( );
    }

    private void Update_Zoom_From_Slider ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      int Slider_Value = Zoom_Slider.Value;

      if ( Slider_Value < 50 )
      {
        // Zoom out: 1-49 maps to 0.1x - 1.0x
        _Zoom_Factor = 0.1 + ( Slider_Value / 50.0 ) * 0.9;
      }
      else
      {
        // Zoom in: 50-100 maps to 1.0x - 10x
        _Zoom_Factor = 1.0 + ( ( Slider_Value - 50 ) / 50.0 ) * 9.0;
      }

      Block?.Trace ( $"Zoom slider value: {Slider_Value}, Zoom factor: {_Zoom_Factor:F2}x" );

      Chart_Panel.Invalidate ( );
    }




    private Task Set_Local_Mode ( int Address, Meter_Type Type )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( !_Comm.Is_Connected )
      {
        Capture_Trace.Write ( "Not connected, skipping" );
        return Task.CompletedTask;
      }
      try
      {
        if ( _Comm.Mode == Connection_Mode.Prologix_GPIB )
        {
          Capture_Trace.Write ( $"Selecting GPIB address {Address}" );
          _Comm.Send_Prologix_Command ( $"++addr {Address}" );
        }
        switch ( Type )
        {
          case Meter_Type.HP34401:
          case Meter_Type.HP33120:
            Capture_Trace.Write ( "Sending CLS?" );
            _Comm.Send_Instrument_Command ( "CLS?" );
            break;
          case Meter_Type.HP3458:
            Capture_Trace.Write ( "3458 - GTL handled by ++loc only" );
            Capture_Trace.Write ( "skipping instrument command" );
            break;
        }
        if ( _Comm.Mode == Connection_Mode.Prologix_GPIB )
        {
          Capture_Trace.Write ( $"Sending ++loc to Prologix for address {Address}" );
          _Comm.Send_Prologix_Command ( "++loc" );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Exception: [{Address}] : {Ex.Message}" );
      }
      return Task.CompletedTask;
    }







    private async Task Perform_Close_Operations ( string Context )
    {

      Capture_Trace.Write ( "Beginning graceful shutdown" );


      // 1. Stop active polling gracefully
      if ( _Is_Running )
      {
        Capture_Trace.Write ( "Cancelling active polling" );
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
          Capture_Trace.Write ( "WARNING - polling did not stop within 5s, forcing" );
        }
        else
        {
          Capture_Trace.Write ( "Polling stopped cleanly" );
        }
      }

      // 2. Stop recording and save if active
      if ( _Is_Recording )
      {
        Capture_Trace.Write ( "Stopping active recording" );
        Stop_Recording ( );
      }

      // 3. Stop timers
      Capture_Trace.Write ( "Stopping timers" );
      _Chart_Refresh_Timer?.Stop ( );
      _Auto_Save_Timer?.Stop ( );

      // 4. Flush serial buffer (discard any in-flight response bytes)
      try
      {
        if ( _Comm.Is_Connected )
        {
          Capture_Trace.Write ( "Flushing serial buffers" );
          _Comm.Flush_Buffers ( );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Flush error (non-fatal): {Ex.Message}" );
      }


      // 5. Return all instruments to local control
      Capture_Trace.Write ( "Setting local mode" );
      Capture_Trace.Write ( "Setting local mode for all instruments" );

      foreach ( var Series in _Series )
      {
        Capture_Trace.Write ( $"Releasing {Series.Name} at GPIB {Series.Address}" );
        await Set_Local_Mode ( Series.Address, Series.Type );
      }

      // 6. Dispose CTS
      _Cts?.Dispose ( );
      _Cts = null;

    }


    private async void Close_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Disable the button immediately to prevent double-clicks
      if ( Sender is Button Btn )
        Btn.Enabled = false;

      await Perform_Close_Operations ( "Closing" );


      Capture_Trace.Write ( "Shutdown complete, closing form" );
      this.Close ( );
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
        if ( _Selected_Meter == Meter_Type.HP3458 )
        {
          Capture_Trace.Write ( "Sending 'ID' to 3458" );
          _Comm.Send_Instrument_Command ( "ID?" );
          Thread.Sleep ( _Settings.Instrument_Settle_Ms );

          using var Cts = new CancellationTokenSource ( TimeSpan.FromSeconds ( 3 ) );
          Capture_Trace.Write ( "Reading response..." );
          IDN = _Comm.Read_Instrument ( Cts.Token );
          Capture_Trace.Write ( $"Got response: [{IDN}]" );
        }
        else
        {
          Capture_Trace.Write ( "Sending '*IDN?' to non-3458" );
          IDN = _Comm.Query_Instrument ( "*IDN?" );
          Capture_Trace.Write ( $"Got response: [{IDN}]" );
        }
        if ( !string.IsNullOrWhiteSpace ( IDN ) )
        {
          //     string [ ] Parts = IDN.Split ( ',' );
          //     _Instrument_Name = Parts.Length >= 2 ? Parts [ 1 ].Trim ( ) : IDN.Trim ( );
          //     Capture_Trace.Write ( $"Parsed instrument name: [{_Instrument_Name}]" );
          Capture_Trace.Write ( $"Name: [{IDN}]" );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"EXCEPTION: {Ex.Message}" );
      }
    }

    private void Populate_Measurement_Combo ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Unsubscribe to prevent commands firing during population
      Measurement_Combo.SelectedIndexChanged -= Measurement_Combo_Changed;

      Measurement_Combo.Items.Clear ( );
      _Filtered_Indices.Clear ( );

      for ( int I = 0; I < _Measurements.Length; I++ )
      {
        string Cmd = _Series.Count > 0 && _Series [ 0 ].Type == Meter_Type.HP34401
            ? _Measurements [ I ].Cmd_34401
            : _Measurements [ I ].Cmd_3458;
        if ( !string.IsNullOrEmpty ( Cmd ) )
        {
          _Filtered_Indices.Add ( I );
          Measurement_Combo.Items.Add ( _Measurements [ I ].Label );
        }
      }

      // Default to DC Voltage if available, otherwise first item
      int Dc_Index = Measurement_Combo.Items.IndexOf ( "DC Voltage" );
      if ( Dc_Index >= 0 )
        Measurement_Combo.SelectedIndex = Dc_Index;
      else if ( Measurement_Combo.Items.Count > 0 )
        Measurement_Combo.SelectedIndex = 0;

      // Re-subscribe now that population is complete
      Measurement_Combo.SelectedIndexChanged += Measurement_Combo_Changed;
    }


    private void Measurement_Combo_Changed ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( Measurement_Combo.SelectedItem == null )
        return;

      string Selected = Measurement_Combo.SelectedItem.ToString ( );
      Block?.Trace ( $"Measurement changed to: {Selected}" );
      Show_Progress ( $"Measurement changed to: {Selected}", _Foreground_Color );

      if ( !_Comm.Is_Connected )
      {
        Block?.Trace ( "Not connected, skipping configuration" );
        return;
      }

      foreach ( var S in _Series )
      {
        try
        {
          int Filtered_Index = _Filtered_Indices [ Measurement_Combo.SelectedIndex ];
          var Entry = _Measurements [ Filtered_Index ];

          string Command = Get_Command_For_Series ( S, Entry );  // ← replaces the two-type switch

          if ( string.IsNullOrEmpty ( Command ) )
          {
            Block?.Trace ( $"  {S.Name} has no command for {Selected}, skipping" );
            continue;
          }

          Block?.Trace ( $"Configuring {S.Type} {S.Name} @ {S.Address} with [{Command}]" );
          _Comm.Change_GPIB_Address ( S.Address );
          _Comm.Send_Instrument_Command ( Command );
          Thread.Sleep ( 50 );
          Block?.Trace ( $"  {S.Name} configured successfully" );
        }
        catch ( Exception Ex )
        {
          Block?.Trace ( $"  ERROR configuring {S.Name}: {Ex.Message}" );
          MessageBox.Show (
              $"Error configuring {S.Name} for {Selected}:\n{Ex.Message}",
              "Configuration Error",
              MessageBoxButtons.OK,
              MessageBoxIcon.Warning );
        }
      }
      Block?.Trace ( "All instruments configured" );
    }

    private void Update_Current_Values_Display ( )
    {
      foreach ( var S in _Series )
      {
        if ( S.Points.Count == 0 )
          continue;

        double Latest = S.Points [ S.Points.Count - 1 ].Value;
        string Display = Multimeter_Common_Helpers_Class.Format_Value (
          Latest, Current_Unit, S.Type );

        var Value_Label = Current_Values_Panel.Controls [ $"Value_{S.Address}" ] as Label;
        if ( Value_Label != null )
          Value_Label.Text = Display;
      }
    }


    private void Initialize_Current_Values_Display ( )
    {
      Current_Values_Panel.Controls.Clear ( );
      Current_Values_Panel.BackColor = _Theme.Background;
      Current_Values_Panel.SuspendLayout ( );

      int Y = 5;

      foreach ( var S in _Series )
      {
        var Dot = new Label
        {
          Name = $"Dot_{S.Address}",
          Text = "●",
          Location = new Point ( 5, Y ),
          Size = new Size ( 16, 16 ),
          Font = new Font ( "Consolas", 8f ),
          ForeColor = S.Line_Color,
          AutoSize = false
        };

        var Name_Label = new Label
        {
          Name = $"Name_{S.Address}",
          Text = S.Name,
          Location = new Point ( 22, Y ),
          Size = new Size ( 160, 16 ),
          Font = new Font ( "Consolas", 7.5f ),
          ForeColor = _Theme.Labels,
          AutoSize = false
        };

        var Value_Label = new Label
        {
          Name = $"Value_{S.Address}",
          Text = "---",
          Location = new Point ( 22, Y + 16 ),
          Size = new Size ( 160, 16 ),
          Font = new Font ( "Consolas", 8.5f, FontStyle.Bold ),
          ForeColor = _Theme.Foreground,
          AutoSize = false
        };

        Current_Values_Panel.Controls.Add ( Dot );
        Current_Values_Panel.Controls.Add ( Name_Label );
        Current_Values_Panel.Controls.Add ( Value_Label );

        Y += 38;
      }

      Current_Values_Panel.ResumeLayout ( );
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
        string X_Label = Multimeter_Common_Helpers_Class.Format_Value (
          Bin_Center, Current_Unit, _Selected_Meter );
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
      // Measure the widest legend entry to size the legend properly
      int Legend_W = 0;
      using ( var Temp_Font = new Font ( "Consolas", 7.5F ) )
      using ( var Temp_G = Graphics.FromHwnd ( IntPtr.Zero ) )
      {
        double Bin_Width_Temp = Range / Num_Bins;
        for ( int I = 0; I < Num_Bins; I++ )
        {
          if ( Bin_Counts [ I ] == 0 )
            continue;
          double Bin_Low = Min_V + I * Bin_Width_Temp;
          double Bin_High = Bin_Low + Bin_Width_Temp;
          double Pct = 100.0 * Bin_Counts [ I ] / Count;
          string Entry_Text =
            $"{Multimeter_Common_Helpers_Class.Format_Value ( Bin_Low, Current_Unit, _Selected_Meter )}" +
            $" - {Multimeter_Common_Helpers_Class.Format_Value ( Bin_High, Current_Unit, _Selected_Meter )}" +
            $"  ({Pct:F1}%)";
          int Entry_W = (int) Temp_G.MeasureString ( Entry_Text, Temp_Font ).Width;
          if ( Entry_W > Legend_W )
            Legend_W = Entry_W;
        }
      }
      Legend_W += 40; // swatch + padding
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
            $"{Multimeter_Common_Helpers_Class.Format_Value ( Bin_Low, Current_Unit, _Selected_Meter )}" +
            $" - {Multimeter_Common_Helpers_Class.Format_Value ( Bin_High, Current_Unit, _Selected_Meter )}" +
            $"  ({Pct:F1}%)";

          G.DrawString ( Entry_Text, Legend_Font,
            Label_Brush, Leg_X + 18, Leg_Y );

          Leg_Y += Row_H;
        }
      }
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

    private void Draw_Grid_And_Axes ( Graphics G,
  int W, int H, int Margin_Left, int Margin_Right,
  int Margin_Top, int Margin_Bottom,
  int Chart_W, int Chart_H,
  double Min_V, double Max_V )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Label_Brush = new SolidBrush ( _Theme.Labels );
      using var Axis_Pen = new Pen ( _Theme.Separator, 1f );
      using var Label_Font = new Font ( "Consolas", 7.5f );

      float Baseline = H - Margin_Bottom;

      // Horizontal grid lines and Y-axis labels
      int Num_Grid_Lines = 5;
      for ( int I = 0; I <= Num_Grid_Lines; I++ )
      {
        double Fraction = (double) I / Num_Grid_Lines;
        int Y_Pos = H - Margin_Bottom - (int) ( Fraction * Chart_H );
        double Y_Val = Min_V + Fraction * ( Max_V - Min_V );

        G.DrawLine ( Grid_Pen,
          Margin_Left, Y_Pos,
          W - Margin_Right, Y_Pos );

        string Y_Label = Multimeter_Common_Helpers_Class.Format_Value (
          Y_Val, Current_Unit, _Selected_Meter );
        var Label_Size = G.MeasureString ( Y_Label, Label_Font );
        G.DrawString ( Y_Label, Label_Font, Label_Brush,
          Margin_Left - Label_Size.Width - 4,
          Y_Pos - Label_Size.Height / 2 );
      }

      // Vertical grid lines and X-axis time labels
      int Num_V_Lines = 6;
      for ( int I = 0; I <= Num_V_Lines; I++ )
      {
        int X_Pos = Margin_Left + (int) ( (double) I / Num_V_Lines * Chart_W );

        G.DrawLine ( Grid_Pen,
          X_Pos, Margin_Top,
          X_Pos, Baseline );

        // Time label from the series with most points
        var Best_Series = _Series
          .OrderByDescending ( s => s.Points.Count )
          .FirstOrDefault ( );

        if ( Best_Series != null && Best_Series.Points.Count >= 2 )
        {
          var (Start_Index, Visible_Count) = Multimeter_Common_Helpers_Class.Get_Visible_Range (
            Best_Series.Points.Count, _Enable_Rolling,
            _Max_Display_Points, _View_Offset );

          int Actual_Count = Math.Min ( Visible_Count,
            Best_Series.Points.Count - Start_Index );

          if ( Actual_Count >= 2 )
          {
            int Pt_Index = Start_Index + (int) ( (double) I / Num_V_Lines * ( Actual_Count - 1 ) );
            Pt_Index = Math.Clamp ( Pt_Index, 0, Best_Series.Points.Count - 1 );
            DateTime Pt_Time = Best_Series.Points [ Pt_Index ].Time;
            string X_Label = Pt_Time.ToString ( "HH:mm:ss" );
            var X_Size = G.MeasureString ( X_Label, Label_Font );
            G.DrawString ( X_Label, Label_Font, Label_Brush,
              X_Pos - X_Size.Width / 2,
              Baseline + 4 );
          }
        }
      }

      // Axis border
      G.DrawRectangle ( Axis_Pen,
        Margin_Left, Margin_Top,
        Chart_W, Chart_H );
    }


    private int Calculate_Y_Axis_Width ( Graphics G, double Min_V, double Max_V )
    {
      using var Measure_Font = new Font ( "Consolas", 7.5f );
      float Max_Label_Width = 0;

      int Num_Grid_Lines = 5;
      for ( int I = 0; I <= Num_Grid_Lines; I++ )
      {
        double Fraction = (double) I / Num_Grid_Lines;
        double Y_Val = Min_V + Fraction * ( Max_V - Min_V );
        string Y_Label = Multimeter_Common_Helpers_Class.Format_Value (
          Y_Val, Current_Unit, _Selected_Meter );
        float Label_W = G.MeasureString ( Y_Label, Measure_Font ).Width;
        if ( Label_W > Max_Label_Width )
          Max_Label_Width = Label_W;
      }

      return (int) Max_Label_Width + 10;  // 10px padding gap from axis
    }


    private void Apply_Theme_To_Current_Values_Panel ( )
    {
      Current_Values_Panel.BackColor = _Theme.Background;

      foreach ( Control C in Current_Values_Panel.Controls )
      {
        if ( C is Label L )
        {
          // Dot labels keep their series color
          if ( L.Name.StartsWith ( "Dot_" ) )
            continue;

          // Name labels use Labels color
          if ( L.Name.StartsWith ( "Name_" ) )
            L.ForeColor = _Theme.Labels;

          // Value labels use Foreground color
          if ( L.Name.StartsWith ( "Value_" ) )
            L.ForeColor = _Theme.Foreground;
        }
      }
    }







    private void Draw_Step_And_Fill (
  Graphics G,
  PointF [ ] Points,
  Color Line_Color,
  int Sub_Bottom,
  int Label_Top )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      int Count = Points.Length;
      if ( Count < 2 )
        return;
      // Build step points: for each segment, go horizontal then vertical
      List<PointF> Step_Points = new ( Count * 2 );
      for ( int I = 0; I < Count - 1; I++ )
      {
        Step_Points.Add ( Points [ I ] );
        Step_Points.Add ( new PointF ( Points [ I + 1 ].X, Points [ I ].Y ) );
      }
      Step_Points.Add ( Points [ Count - 1 ] );
      PointF [ ] SP = Step_Points.ToArray ( );
      // Fill
      PointF [ ] Fill_Points = new PointF [ SP.Length + 2 ];
      Array.Copy ( SP, Fill_Points, SP.Length );
      Fill_Points [ SP.Length ] = new PointF ( SP [ SP.Length - 1 ].X, Sub_Bottom );
      Fill_Points [ SP.Length + 1 ] = new PointF ( SP [ 0 ].X, Sub_Bottom );
      Color Fill_Top = Color.FromArgb ( 60, Line_Color );
      Color Fill_Bottom = Color.FromArgb ( 5, Line_Color );
      using var Fill_Brush = new System.Drawing.Drawing2D.LinearGradientBrush (
        new PointF ( 0, Label_Top ),
        new PointF ( 0, Sub_Bottom ),
        Fill_Top, Fill_Bottom );
      G.FillPolygon ( Fill_Brush, Fill_Points );
      // Line
      using var Line_Pen = new Pen ( Line_Color, 2f );
      G.DrawLines ( Line_Pen, SP );
    }

    private void Draw_Subplot_Bars (
      Graphics G,
      PointF [ ] Points,
      Color Line_Color,
      int Sub_Bottom,
      int Label_Top )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      int Count = Points.Length;
      if ( Count == 0 )
        return;
      // Bar width based on spacing, capped to reasonable size
      float Bar_W = Count > 1
        ? Math.Min ( ( Points [ 1 ].X - Points [ 0 ].X ) * 0.7f, 20f )
        : 10f;
      Color Fill_Top = Color.FromArgb ( 180, Line_Color );
      Color Fill_Bottom = Color.FromArgb ( 60, Line_Color );
      using var Border_Pen = new Pen ( Line_Color, 1f );
      foreach ( PointF P in Points )
      {
        float Bar_H = Sub_Bottom - P.Y;
        if ( Bar_H < 1f )
          continue;

        RectangleF Rect = new (
          P.X - Bar_W / 2, P.Y,
          Bar_W, Bar_H );
        using var Fill_Brush = new System.Drawing.Drawing2D.LinearGradientBrush (
          new PointF ( 0, P.Y ),
          new PointF ( 0, Sub_Bottom ),
          Fill_Top, Fill_Bottom );
        G.FillRectangle ( Fill_Brush, Rect );
        G.DrawRectangle ( Border_Pen, Rect.X, Rect.Y, Rect.Width, Rect.Height );
      }
    }

    private void Draw_Subplot_Histogram (
    Graphics G,
    List<(DateTime Time, double Value)> Raw_Points,
    Color Line_Color,
    int Margin_Left,
    int Chart_W,
    int Sub_Bottom,
    int Label_Top,
    double Display_Min,
    double Display_Range )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      int Count = Raw_Points.Count;
      if ( Count == 0 )
        return;

      List<double> Values = Raw_Points.Select ( P => P.Value ).ToList ( );
      double Min_V = Values.Min ( );
      double Max_V = Values.Max ( );
      double Range = Max_V - Min_V;
      int Num_Bins = Get_Bin_Count ( Count );

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

      int [ ] Bin_Counts = new int [ Num_Bins ];
      foreach ( double V in Values )
      {
        int Bin = (int) ( ( V - Min_V ) / Bin_Width );
        Bin = Math.Clamp ( Bin, 0, Num_Bins - 1 );
        Bin_Counts [ Bin ]++;
      }

      int Max_Count = Math.Max ( 1, Bin_Counts.Max ( ) );
      double Y_Max = Max_Count * 1.1;
      int Plot_H = Sub_Bottom - Label_Top;

      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Label_Font = new Font ( "Consolas", 7.5F );
      using var Label_Brush = new SolidBrush ( _Theme.Labels );

      // Y-axis grid lines
      int Num_Grid = 5;
      for ( int I = 0; I <= Num_Grid; I++ )
      {
        double Fraction = (double) I / Num_Grid;
        int Y_Pos = Sub_Bottom - (int) ( Fraction * Plot_H );
        G.DrawLine ( Grid_Pen, Margin_Left, Y_Pos,
          Margin_Left + Chart_W, Y_Pos );
        int Label_Val = (int) Math.Round ( Fraction * Y_Max );
        string Label_Text = Label_Val.ToString ( );
        var Label_Size = G.MeasureString ( Label_Text, Label_Font );
        G.DrawString ( Label_Text, Label_Font, Label_Brush,
          Margin_Left - Label_Size.Width - 6,
          Y_Pos - Label_Size.Height / 2 );
      }

      // Bars
      float Bar_Spacing = Chart_W / (float) Num_Bins;
      float Bar_W = Bar_Spacing * 0.8f;
      float Gap = Bar_Spacing * 0.1f;

      using var Bar_Brush = new SolidBrush ( Line_Color );
      using var Bar_Border_Pen = new Pen (
        Color.FromArgb (
          (int) ( Line_Color.R * 0.7 ),
          (int) ( Line_Color.G * 0.7 ),
          (int) ( Line_Color.B * 0.7 ) ), 1f );

      for ( int I = 0; I < Num_Bins; I++ )
      {
        float Bar_H = (float) ( ( Bin_Counts [ I ] / Y_Max ) * Plot_H );
        if ( Bar_H < 1f && Bin_Counts [ I ] > 0 )
          Bar_H = 1f;
        if ( Bar_H < 1f )
          continue;

        float X = Margin_Left + I * Bar_Spacing + Gap;
        float Y = Sub_Bottom - Bar_H;
        RectangleF Rect = new ( X, Y, Bar_W, Bar_H );
        G.FillRectangle ( Bar_Brush, Rect );
        G.DrawRectangle ( Bar_Border_Pen,
          Rect.X, Rect.Y, Rect.Width, Rect.Height );

        // Count label above bar
        if ( Bin_Counts [ I ] > 0 )
        {
          string Freq_Text = Bin_Counts [ I ].ToString ( );
          var Freq_Size = G.MeasureString ( Freq_Text, Label_Font );
          G.DrawString ( Freq_Text, Label_Font, Label_Brush,
            X + Bar_W / 2 - Freq_Size.Width / 2,
            Y - Freq_Size.Height - 2 );
        }
      }

      // Bell curve overlay
      double Mean = Values.Average ( );
      double Sum_Sq = Values.Sum ( V => ( V - Mean ) * ( V - Mean ) );
      double Std_Dev = Math.Sqrt ( Sum_Sq / Count );

      if ( Std_Dev > 1e-15 )
      {
        Color Curve_Color = _Theme.Line_Colors [
          1 % _Theme.Line_Colors.Length ];
        using var Curve_Pen = new Pen ( Curve_Color, 2.5f );

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
            (float) ( ( X_Val - Min_V ) / Range * Chart_W );
          float Py = Sub_Bottom -
            (float) ( ( Freq / Y_Max ) * Plot_H );

          Curve_Pts [ I ] = new PointF ( Px, Py );
        }
        G.DrawLines ( Curve_Pen, Curve_Pts );

        // Mean line
        Color Mean_Color = _Theme.Line_Colors [
          2 % _Theme.Line_Colors.Length ];
        using var Mean_Pen = new Pen ( Mean_Color, 2f );
        Mean_Pen.DashStyle =
          System.Drawing.Drawing2D.DashStyle.Dash;
        float Mean_X = Margin_Left +
          (float) ( ( Mean - Min_V ) / Range * Chart_W );
        G.DrawLine ( Mean_Pen, Mean_X, Label_Top, Mean_X, Sub_Bottom );

        // Sigma lines
        using var Sigma_Pen = new Pen (
          Color.FromArgb ( 120, Mean_Color ), 1.5f );
        Sigma_Pen.DashStyle =
          System.Drawing.Drawing2D.DashStyle.Dot;
        using var Sigma_Font = new Font ( "Consolas", 7F );
        using var Sigma_Brush = new SolidBrush ( Mean_Color );

        G.DrawString ( "\u03bc", Sigma_Font, Sigma_Brush,
          Mean_X + 3, Label_Top + 2 );

        double [ ] Sigmas = { -2, -1, 1, 2 };
        string [ ] Sigma_Labels =
          { "-2\u03c3", "-1\u03c3", "+1\u03c3", "+2\u03c3" };

        for ( int I = 0; I < Sigmas.Length; I++ )
        {
          double Sv = Mean + Sigmas [ I ] * Std_Dev;
          if ( Sv < Min_V || Sv > Max_V )
            continue;
          float Sx = Margin_Left +
            (float) ( ( Sv - Min_V ) / Range * Chart_W );
          G.DrawLine ( Sigma_Pen, Sx, Label_Top, Sx, Sub_Bottom );
          G.DrawString ( Sigma_Labels [ I ], Sigma_Font,
            Sigma_Brush, Sx + 3, Label_Top + 2 );
        }
      }

      // X-axis bin labels
      using var X_Font = new Font ( "Consolas", 6.5F );
      int Label_Step = Math.Max ( 1, Num_Bins / 8 );
      for ( int I = 0; I < Num_Bins; I += Label_Step )
      {
        double Bin_Center = Min_V + ( I + 0.5 ) * Bin_Width;
        string X_Label = Multimeter_Common_Helpers_Class.Format_Value (
          Bin_Center, Current_Unit, _Selected_Meter );
        var X_Size = G.MeasureString ( X_Label, X_Font );
        float X_Pos = Margin_Left +
          I * Bar_Spacing + Bar_Spacing / 2;
        G.DrawString ( X_Label, X_Font, Label_Brush,
          X_Pos - X_Size.Width / 2,
          Sub_Bottom + 4 );
      }

      // Footer
      using var Title_Font = new Font ( "Segoe UI", 7.5F );
      string Title = $"Distribution  ({Count} readings)";
      var Title_Size = G.MeasureString ( Title, Title_Font );
      G.DrawString ( Title, Title_Font, Label_Brush,
        Margin_Left + Chart_W / 2 - Title_Size.Width / 2,
        Sub_Bottom + 18 );
    }

    private void Delay_Numeric_ValueChanged ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      _Settings.Default_Poll_Delay_Ms = (int) Delay_Numeric.Value;
      _Settings.Save ( );
    }

    private void NPLC_Delay_Textbox_TextChanged ( object sender, EventArgs e )
    {
      Update_Settle_Display ( );
    }
  }
}


