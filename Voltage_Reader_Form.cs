// ============================================================================
// File:        Voltage_Reader_Form.cs
// Project:     Keysight 3458A Multimeter Controller
// Description: Reads DC voltage from the 3458A in a loop with a user-
//              adjustable cycle count and delay, plotting results in a
//              dynamically updating bar graph.
// ============================================================================

namespace Multimeter_Controller
{
  public partial class Voltage_Reader_Form : Form
  {
    private Instrument_Comm _Comm = null!;
    private Meter_Type _Meter;
    private readonly List<double> _Readings = new List<double> ( );
    private CancellationTokenSource? _Cts;
    private bool _Is_Running;
    private string _Current_Unit = "V";

    private bool _Is_Recording;
    private readonly List<(DateTime Time, double Value)>
      _Recorded_Points = new List<(DateTime, double)> ( );
    private string _Record_Function = "";
    private string _Record_Unit = "";

    private List<int> _Filtered_Indices = new List<int> ( );

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

    public Voltage_Reader_Form ( )
    {
      InitializeComponent ( );
    }

    public Voltage_Reader_Form (
      Instrument_Comm Comm, Meter_Type Meter )
    {
      InitializeComponent ( );
      _Comm = Comm;
      _Meter = Meter;

      string Meter_Name = Meter switch
      {
        Meter_Type.HP_34401A => "HP 34401A",
        Meter_Type.HP_33120A => "HP 33120A",
        _ => "Keysight 3458A"
      };

      Text = $"{Meter_Name} - Voltage Reader";
      Title_Label.Text = $"{Meter_Name} Voltage Reader";

      Populate_Function_Combo ( );

      Graph_Style_Combo.Items.Add ( "Line" );
      Graph_Style_Combo.Items.Add ( "Bar" );
      Graph_Style_Combo.Items.Add ( "Scatter" );
      Graph_Style_Combo.Items.Add ( "Step" );
      Graph_Style_Combo.Items.Add ( "Histogram" );
      Graph_Style_Combo.Items.Add ( "Pie" );
      Graph_Style_Combo.SelectedIndex = 0;

      Chart_Panel.BackColor = _Theme.Background;
    }

    private void Populate_Function_Combo ( )
    {
      Function_Combo.Items.Clear ( );
      _Filtered_Indices.Clear ( );

      for ( int I = 0; I < _Functions.Length; I++ )
      {
        string Cmd = _Meter == Meter_Type.HP_34401A
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

    protected override void OnFormClosing ( FormClosingEventArgs E )
    {
      Stop_Reading ( );
      base.OnFormClosing ( E );
    }

    private void Start_Stop_Button_Click (
      object Sender, EventArgs E )
    {
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
      Readings_Label.Enabled = !Continuous_Check.Checked;
      Readings_Numeric.Enabled =
        !Continuous_Check.Checked && !_Is_Running;
    }

    private void Theme_Button_Click (
      object Sender, EventArgs E )
    {
      using var Dlg = new Theme_Settings_Form ( _Theme );
      if ( Dlg.ShowDialog ( this ) == DialogResult.OK )
      {
        _Theme.Copy_From ( Dlg.Result );
        _Theme.Save ( );
        Chart_Panel.BackColor = _Theme.Background;
        Chart_Panel.Invalidate ( );
      }
    }

    private void Save_Chart_Button_Click (
      object Sender, EventArgs E )
    {
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
      Chart_Panel.Invalidate ( );
    }

    private void Graph_Style_Combo_Changed (
      object Sender, EventArgs E )
    {
      Chart_Panel.Invalidate ( );
    }

    private void Clear_Button_Click (
      object Sender, EventArgs E )
    {
      if ( _Is_Running )
      {
        return;
      }

      _Readings.Clear ( );
      Current_Value_Label.Text = "---";
      Progress_Label.Text = "";
      Chart_Panel.Invalidate ( );
    }

    private async void Start_Reading ( )
    {
      if ( !_Comm.Is_Connected )
      {
        MessageBox.Show (
          "Not connected. Please connect first.",
          "Connection Required",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      int Combo_Index = Function_Combo.SelectedIndex;
      if ( Combo_Index < 0
        || Combo_Index >= _Filtered_Indices.Count )
      {
        return;
      }

      int Func_Index = _Filtered_Indices [ Combo_Index ];
      var Selected = _Functions [ Func_Index ];
      string Configure_Cmd = _Meter == Meter_Type.HP_34401A
        ? Selected.Cmd_34401A
        : Selected.Cmd_3458A;
      string Unit = Selected.Unit;

      bool Is_Query_Mode = Configure_Cmd.EndsWith ( "?" );
      _Current_Unit = Unit;

      _Is_Running = true;
      _Cts = new CancellationTokenSource ( );
      Start_Stop_Button.Text = "Stop";
      Status_Label.Text = "Reading...";
      Status_Label.ForeColor = Color.Green;
      Readings_Numeric.Enabled = false;
      Delay_Numeric.Enabled = false;
      Function_Combo.Enabled = false;
      Clear_Button.Enabled = false;
      Continuous_Check.Enabled = false;

      bool Continuous = Continuous_Check.Checked;
      int Total_Readings = (int) Readings_Numeric.Value;
      int Delay_Ms = (int) Delay_Numeric.Value;
      int Readings_Taken = _Readings.Count;

      CancellationToken Token = _Cts.Token;

      try
      {
        // Configure the instrument for selected function
        // (skip for query-mode functions like TEMP?)
        if ( !Is_Query_Mode )
        {
          await Task.Run ( ( ) =>
            _Comm.Send_Instrument_Command ( Configure_Cmd ),
            Token );

          // Allow the instrument to configure and take
          // its first measurement
          await Task.Delay ( 300, Token );
        }

        int I = 0;
        while ( Continuous || I < Total_Readings )
        {
          Token.ThrowIfCancellationRequested ( );

          string Response;

          if ( Is_Query_Mode )
          {
            // Query-mode: send the query each iteration
            // (e.g., TEMP?)
            Response = await Task.Run ( ( ) =>
              _Comm.Query_Instrument ( Configure_Cmd ),
              Token );
          }
          else if ( _Meter == Meter_Type.HP_34401A )
          {
            // 34401A needs explicit READ? to trigger
            // and return a measurement
            Response = await Task.Run ( ( ) =>
              _Comm.Query_Instrument ( "READ?" ),
              Token );
          }
          else
          {
            // 3458A continuously measures once configured
            Response = await Task.Run ( ( ) =>
              _Comm.Read_Instrument ( Token ),
              Token );
          }

          Token.ThrowIfCancellationRequested ( );

          if ( double.TryParse ( Response,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out double Value ) )
          {
            _Readings.Add ( Value );
            if ( _Is_Recording )
            {
              _Recorded_Points.Add (
                (DateTime.Now, Value) );
            }
            Readings_Taken = _Readings.Count;

            Current_Value_Label.Text =
              Format_Value ( Value, Unit );

            if ( Continuous )
            {
              Progress_Label.Text =
                $"Reading {Readings_Taken}  (Continuous)";
            }
            else
            {
              Progress_Label.Text =
                $"Reading {I + 1} of {Total_Readings}  " +
                $"(Total: {Readings_Taken})";
            }

            Chart_Panel.Invalidate ( );
          }
          else if ( !string.IsNullOrEmpty ( Response ) )
          {
            Progress_Label.Text =
              $"Reading {I + 1}: unexpected response";
          }

          I++;

          bool Has_More = Continuous || I < Total_Readings;
          if ( Has_More )
          {
            await Task.Delay ( Delay_Ms, Token );
          }
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

    private void Stop_Reading ( )
    {
      _Cts?.Cancel ( );
    }

    private void Finish_Reading ( )
    {
      _Is_Running = false;
      _Cts?.Dispose ( );
      _Cts = null;
      Start_Stop_Button.Text = "Start";
      Status_Label.Text = "Idle";
      Status_Label.ForeColor = Color.Gray;
      Readings_Numeric.Enabled = !Continuous_Check.Checked;
      Delay_Numeric.Enabled = true;
      Function_Combo.Enabled = true;
      Clear_Button.Enabled = true;
      Continuous_Check.Enabled = true;
    }

    // ===== Recording / Loading =====

    private void Record_Button_Click (
      object Sender, EventArgs E )
    {
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
      int Combo_Index = Function_Combo.SelectedIndex;
      if ( Combo_Index < 0
        || Combo_Index >= _Filtered_Indices.Count )
      {
        return;
      }

      int Func_Index = _Filtered_Indices [ Combo_Index ];
      _Record_Function = _Functions [ Func_Index ].Label;
      _Record_Unit = _Functions [ Func_Index ].Unit;
      _Recorded_Points.Clear ( );
      _Is_Recording = true;
      Record_Button.Text = "Stop Rec";
      Record_Button.BackColor = Color.IndianRed;
      Record_Button.ForeColor = Color.White;
    }

    private void Stop_Recording ( )
    {
      _Is_Recording = false;
      Record_Button.Text = "Record";
      Record_Button.BackColor = SystemColors.Control;
      Record_Button.ForeColor = SystemColors.ControlText;

      if ( _Recorded_Points.Count == 0 )
      {
        MessageBox.Show (
          "No data points were captured.",
          "Nothing to Save",
          MessageBoxButtons.OK,
          MessageBoxIcon.Information );
        return;
      }

      Save_Recorded_Data ( );
    }

    private void Save_Recorded_Data ( )
    {
      string Folder = Get_Graph_Captures_Folder ( );
      Directory.CreateDirectory ( Folder );

      string Timestamp = DateTime.Now.ToString (
        "yyyy-MM-dd_HH-mm-ss" );
      string File_Name = $"{Timestamp}_{_Record_Function}.csv";
      string File_Path = Path.Combine ( Folder, File_Name );

      using var Writer = new StreamWriter ( File_Path );
      Writer.WriteLine (
        $"# Function: {_Record_Function}" );
      Writer.WriteLine ( $"# Unit: {_Record_Unit}" );
      Writer.WriteLine (
        $"# Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" );
      Writer.WriteLine (
        $"# Points: {_Recorded_Points.Count}" );
      Writer.WriteLine ( "Timestamp,Value" );

      foreach ( var P in _Recorded_Points )
      {
        Writer.WriteLine (
          $"{P.Time:yyyy-MM-dd HH:mm:ss.fff}," +
          $"{P.Value.ToString ( System.Globalization.CultureInfo.InvariantCulture )}" );
      }

      MessageBox.Show (
        $"Saved {_Recorded_Points.Count} points to:\n" +
        $"{File_Path}",
        "Recording Saved",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information );
    }

    private void Load_Button_Click (
      object Sender, EventArgs E )
    {
      if ( _Is_Running )
      {
        MessageBox.Show (
          "Stop the current reading before loading.",
          "Reading in Progress",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      string Folder = Get_Graph_Captures_Folder ( );

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

    private void Load_Recorded_File ( string File_Path )
    {
      var Values = new List<double> ( );
      string Unit = "V";

      foreach ( string Raw_Line in
        File.ReadAllLines ( File_Path ) )
      {
        string Line = Raw_Line.Trim ( );

        if ( Line.StartsWith ( "# Unit:" ) )
        {
          Unit = Line.Substring ( 7 ).Trim ( );
          continue;
        }

        if ( Line.StartsWith ( "#" ) ||
          Line.StartsWith ( "Timestamp" ) ||
          string.IsNullOrEmpty ( Line ) )
        {
          continue;
        }

        string [ ] Parts = Line.Split ( ',' );
        if ( Parts.Length >= 2 &&
          double.TryParse ( Parts [ 1 ],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out double Val ) )
        {
          Values.Add ( Val );
        }
      }

      if ( Values.Count == 0 )
      {
        MessageBox.Show (
          "No valid data points found in file.",
          "Load Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      _Readings.Clear ( );
      _Readings.AddRange ( Values );
      _Current_Unit = Unit;
      Current_Value_Label.Text =
        Format_Value ( Values [ Values.Count - 1 ], Unit );
      Progress_Label.Text =
        $"Loaded {Values.Count} points from file";
      Chart_Panel.Invalidate ( );
    }

    private static string Get_Graph_Captures_Folder ( )
    {
      // Walk up from the executable to find the project
      // folder that contains Graph_Captures, or fall back
      // to creating it next to the executable.
      string Base = AppContext.BaseDirectory;
      string? Dir = Base;
      while ( Dir != null )
      {
        string Candidate = Path.Combine (
          Dir, "Graph_Captures" );
        if ( Directory.Exists ( Candidate ) )
        {
          return Candidate;
        }
        Dir = Directory.GetParent ( Dir )?.FullName;
      }

      // Fallback: create next to executable
      string Fallback = Path.Combine (
        Base, "Graph_Captures" );
      Directory.CreateDirectory ( Fallback );
      return Fallback;
    }

    private Chart_Theme _Theme = Chart_Theme.Load ( );

    // ===== Grafana-Style Chart Rendering =====

    private void Chart_Panel_Paint (
      object? Sender, PaintEventArgs E )
    {
      Graphics G = E.Graphics;
      G.SmoothingMode =
        System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      G.TextRenderingHint =
        System.Drawing.Text.TextRenderingHint
          .ClearTypeGridFit;

      int W = Chart_Panel.ClientSize.Width;
      int H = Chart_Panel.ClientSize.Height;

      // Dark background
      using var Bg_Brush = new SolidBrush ( _Theme.Background );
      G.FillRectangle ( Bg_Brush, 0, 0, W, H );

      int Margin_Left = 80;
      int Margin_Right = 20;
      int Margin_Top = 20;
      int Margin_Bottom = 35;

      int Chart_W = W - Margin_Left - Margin_Right;
      int Chart_H = H - Margin_Top - Margin_Bottom;

      if ( Chart_W < 10 || Chart_H < 10 )
      {
        return;
      }

      if ( _Readings.Count == 0 )
      {
        using var Empty_Font = new Font ( "Segoe UI", 10F );
        using var Empty_Brush =
          new SolidBrush ( _Theme.Labels );
        G.DrawString (
          "No data. Press Start to begin reading.",
          Empty_Font, Empty_Brush,
          Margin_Left + 20, Margin_Top + Chart_H / 2 );
        return;
      }

      int Style = Graph_Style_Combo.SelectedIndex;

      // Histogram and Pie handle their own layout
      if ( Style == 4 ) // Histogram
      {
        Draw_Histogram ( G, W, H,
          Margin_Left, Margin_Right, Margin_Top,
          Margin_Bottom, Chart_W, Chart_H );
        Draw_Stats_Overlay ( G, W, Margin_Top,
          Margin_Right );
        return;
      }

      if ( Style == 5 ) // Pie
      {
        Draw_Pie_Chart ( G, W, H,
          Margin_Left, Margin_Right, Margin_Top,
          Margin_Bottom, Chart_W, Chart_H );
        Draw_Stats_Overlay ( G, W, Margin_Top,
          Margin_Right, Bottom: true, H: H,
          Margin_Bottom: Margin_Bottom );
        return;
      }

      // Determine Y-axis range
      double Min_V = _Readings.Min ( );
      double Max_V = _Readings.Max ( );

      double Range = Max_V - Min_V;
      if ( Range < 1e-12 )
      {
        Range = Math.Abs ( Max_V ) * 0.1;
        if ( Range < 1e-12 )
        {
          Range = 1.0;
        }
      }

      double Padded_Min = Min_V - Range * 0.1;
      double Padded_Max = Max_V + Range * 0.1;
      double Padded_Range = Padded_Max - Padded_Min;

      // Draw grid lines and Y-axis labels
      int Num_Grid_Lines = 6;
      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      Grid_Pen.DashStyle =
        System.Drawing.Drawing2D.DashStyle.Solid;
      using var Label_Font =
        new Font ( "Consolas", 7.5F );
      using var Label_Brush =
        new SolidBrush ( _Theme.Labels );

      for ( int I = 0; I <= Num_Grid_Lines; I++ )
      {
        double Fraction = (double) I / Num_Grid_Lines;
        double Value = Padded_Min +
          Fraction * Padded_Range;
        int Y_Pos = H - Margin_Bottom -
          (int) ( Fraction * Chart_H );

        G.DrawLine ( Grid_Pen,
          Margin_Left, Y_Pos,
          W - Margin_Right, Y_Pos );

        string Label_Text =
          Format_Value ( Value, _Current_Unit );
        var Label_Size = G.MeasureString (
          Label_Text, Label_Font );
        G.DrawString ( Label_Text, Label_Font,
          Label_Brush,
          Margin_Left - Label_Size.Width - 6,
          Y_Pos - Label_Size.Height / 2 );
      }

      // Draw vertical grid lines
      int Count = _Readings.Count;
      int Num_V_Lines = Math.Min ( 10, Count - 1 );
      if ( Num_V_Lines > 0 )
      {
        for ( int I = 0; I <= Num_V_Lines; I++ )
        {
          double Fraction = (double) I / Num_V_Lines;
          int X_Pos = Margin_Left +
            (int) ( Fraction * Chart_W );
          G.DrawLine ( Grid_Pen, X_Pos, Margin_Top,
            X_Pos, H - Margin_Bottom );
        }
      }

      // Build data points
      PointF [ ] Points = new PointF [ Count ];
      for ( int I = 0; I < Count; I++ )
      {
        double Normalized =
          ( _Readings [ I ] - Padded_Min ) / Padded_Range;
        float X = Margin_Left +
          ( Count == 1 ? Chart_W / 2f
            : I * ( Chart_W / (float) ( Count - 1 ) ) );
        float Y = H - Margin_Bottom -
          (float) ( Normalized * Chart_H );
        Points [ I ] = new PointF ( X, Y );
      }

      float Baseline = H - Margin_Bottom;

      Color Line_Color = _Theme.Line_Colors [ 0 ];
      Color Dot_Color = Color.FromArgb ( 200,
        Line_Color.R, Line_Color.G, Line_Color.B );
      using var Line_Pen = new Pen ( Line_Color, 2f );
      using var Dot_Brush = new SolidBrush ( Dot_Color );

      switch ( Style )
      {
        case 1: // Bar
          Draw_Bar_Chart ( G, Points, Count,
            Margin_Left, Chart_W, Baseline );
          break;

        case 2: // Scatter
          Draw_Scatter_Chart ( G, Points, Count,
            Dot_Brush );
          break;

        case 3: // Step
          Draw_Step_Chart ( G, Points, Count,
            Line_Pen, Margin_Top, Baseline );
          break;

        default: // Line (0)
          Draw_Line_Chart ( G, Points, Count,
            Line_Pen, Dot_Brush, Margin_Top, Baseline );
          break;
      }

      // Highlight last point (all styles)
      if ( Count > 0 )
      {
        PointF Last = Points [ Count - 1 ];
        using var Glow_Pen = new Pen (
          Color.FromArgb ( 80, Line_Color ), 6f );
        G.DrawEllipse ( Glow_Pen,
          Last.X - 5, Last.Y - 5, 10, 10 );
        using var Last_Brush =
          new SolidBrush ( Color.White );
        G.FillEllipse ( Last_Brush,
          Last.X - 3, Last.Y - 3, 6, 6 );
      }

      // X-axis label
      using var X_Label_Font =
        new Font ( "Segoe UI", 7.5F );
      string X_Text = $"Reading  (1 - {Count})";
      var X_Size = G.MeasureString (
        X_Text, X_Label_Font );
      G.DrawString ( X_Text, X_Label_Font,
        Label_Brush,
        Margin_Left + Chart_W / 2 - X_Size.Width / 2,
        H - Margin_Bottom + 14 );

      // Stats overlay
      Draw_Stats_Overlay ( G, W, Margin_Top,
        Margin_Right );
    }

    private void Draw_Line_Chart ( Graphics G,
      PointF [ ] Points, int Count, Pen Line_Pen,
      Brush Dot_Brush, int Margin_Top, float Baseline )
    {
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
      // Sturges' rule, clamped to 5-30
      int Bins = (int) Math.Ceiling (
        1.0 + 3.322 * Math.Log10 ( N ) );
      return Math.Clamp ( Bins, 5, 30 );
    }

    private Color Get_Bin_Color ( int Index )
    {
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
          Bin_Center, _Current_Unit );
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
            $"{Format_Value ( Bin_Low, _Current_Unit )}" +
            $" - {Format_Value ( Bin_High, _Current_Unit )}" +
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
        $"Mean:    {Format_Value ( Mean, _Current_Unit )}",
        $"Std Dev: {Format_Value ( Std_Dev, _Current_Unit )}",
        $"Min:     {Format_Value ( Min_V, _Current_Unit )}",
        $"Max:     {Format_Value ( Max_V, _Current_Unit )}",
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

    private static string Format_Value (
      double Value, string Unit )
    {
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

      // V or A
      if ( Abs >= 1.0 )
        return $"{Value:F6} {Unit}";
      if ( Abs >= 0.001 )
        return $"{Value * 1000:F3} m{Unit}";
      if ( Abs >= 0.000001 )
        return $"{Value * 1e6:F2} u{Unit}";
      return $"{Value:E2} {Unit}";
    }
  }
}
