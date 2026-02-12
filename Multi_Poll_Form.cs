namespace Multimeter_Controller
{
  public partial class Multi_Poll_Form : Form
  {
    private Instrument_Comm _Comm = null!;
    private readonly List<Instrument_Series> _Series =
      new List<Instrument_Series> ( );
    private CancellationTokenSource? _Cts;
    private bool _Is_Running;
    private int _Cycle_Count;

    private bool _Is_Recording;
    private DateTime _Record_Start;
    private string _Record_Query = "";

    private Chart_Theme _Theme = Chart_Theme.Load ( );

    private static readonly (string Label, string Cmd_3458A,
      string Cmd_34401A, string Unit) [ ] _Measurements =
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
      ( "Period",        "PER",    "CONF:PER",      "s" ),
      ( "Temperature",   "TEMP?",  "",              "\u00b0C" ),
    };

    public Multi_Poll_Form ( )
    {
      InitializeComponent ( );
    }

    public Multi_Poll_Form (
      Instrument_Comm Comm,
      List<(string Name, int Address, Meter_Type Type)>
        Instruments )
    {
      InitializeComponent ( );
      _Comm = Comm;

      for ( int I = 0; I < Instruments.Count; I++ )
      {
        var Inst = Instruments [ I ];
        _Series.Add ( new Instrument_Series
        {
          Name = Inst.Name,
          Address = Inst.Address,
          Type = Inst.Type,
          Points = new List<(DateTime Time, double Value)> ( ),
          Line_Color = _Theme.Line_Colors [
            I % _Theme.Line_Colors.Length ]
        } );
      }

      Chart_Panel.BackColor = _Theme.Background;
      Text = $"Multi-Instrument Poller  " +
        $"({Instruments.Count} instruments)";

      Populate_Measurement_Combo ( );

      // Set default NPLC value
      NPLC_Combo.SelectedIndex = 4; // Default to 100 NPLC
    }

    private void Populate_Measurement_Combo ( )
    {
      Measurement_Combo.Items.Clear ( );

      // Find common measurements supported by all instruments
      foreach ( var Measurement in _Measurements )
      {
        bool Supported_By_All = true;

        foreach ( var Inst in _Series )
        {
          string Cmd = Inst.Type == Meter_Type.HP_34401A
            ? Measurement.Cmd_34401A
            : Measurement.Cmd_3458A;

          if ( string.IsNullOrEmpty ( Cmd ) )
          {
            Supported_By_All = false;
            break;
          }
        }

        if ( Supported_By_All )
        {
          Measurement_Combo.Items.Add ( Measurement.Label );
        }
      }

      // Default to DC Voltage if available
      int Dc_Index = Measurement_Combo.Items
        .IndexOf ( "DC Voltage" );
      if ( Dc_Index >= 0 )
      {
        Measurement_Combo.SelectedIndex = Dc_Index;
      }
      else if ( Measurement_Combo.Items.Count > 0 )
      {
        Measurement_Combo.SelectedIndex = 0;
      }
    }

    private void Measurement_Combo_Changed (
      object Sender, EventArgs E )
    {
      if ( Measurement_Combo.SelectedIndex < 0 ||
        Measurement_Combo.SelectedItem == null )
      {
        return;
      }

      string Selected = Measurement_Combo.SelectedItem
        .ToString ( ) ?? "";

      // Find the measurement and update Query_Text
      for ( int I = 0; I < _Measurements.Length; I++ )
      {
        if ( _Measurements [ I ].Label == Selected )
        {
          // Use 3458A command by default
          // (will be translated per-instrument in polling loop)
          Query_Text.Text = _Measurements [ I ].Cmd_3458A;
          break;
        }
      }
    }

    protected override void OnFormClosing (
      FormClosingEventArgs E )
    {
      Stop_Polling ( );
      base.OnFormClosing ( E );
    }

    // ===== Polling =====

    private void Start_Stop_Button_Click (
      object Sender, EventArgs E )
    {
      if ( _Is_Running )
      {
        Stop_Polling ( );
      }
      else
      {
        Start_Polling ( );
      }
    }

    private async void Start_Polling ( )
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

      string Command = Query_Text.Text.Trim ( );
      if ( string.IsNullOrEmpty ( Command ) )
      {
        return;
      }

      _Is_Running = true;
      _Cts = new CancellationTokenSource ( );
      _Cycle_Count = 0;
      Start_Stop_Button.Text = "Stop";
      Status_Label.Text = "Polling...";
      Status_Label.ForeColor = Color.Green;
      Query_Text.Enabled = false;
      Delay_Numeric.Enabled = false;
      NPLC_Combo.Enabled = false;
      Measurement_Combo.Enabled = false;
      Continuous_Check.Enabled = false;
      Clear_Button.Enabled = false;
      Load_Button.Enabled = false;

      int Delay_Ms = (int) Delay_Numeric.Value;
      int Original_Address = _Comm.GPIB_Address;
      CancellationToken Token = _Cts.Token;

      // Get NPLC value
      string NPLC_Value = NPLC_Combo.SelectedItem?.ToString ( )
        ?? "10";

      // Pre-configure HP 34401A instruments on first cycle
      bool [ ] Configured = new bool [ _Series.Count ];

      try
      {
        // Configure NPLC for all instruments
        Status_Label.Text = "Configuring NPLC...";
        Progress_Label.Text = "Setting integration time";

        for ( int I = 0; I < _Series.Count; I++ )
        {
          Token.ThrowIfCancellationRequested ( );

          var S = _Series [ I ];

          await Task.Run ( ( ) =>
          {
            _Comm.Change_GPIB_Address ( S.Address );
          }, Token );

          await Task.Delay ( 50, Token );

          await Task.Run ( ( ) =>
          {
            // Clear the instrument to reset GPIB state
            _Comm.Send_Prologix_Command ( "++clr" );
          }, Token );

          await Task.Delay ( 100, Token );

          // Send NPLC command based on meter type
          string NPLC_Cmd = S.Type == Meter_Type.HP_34401A
            ? $"VOLT:DC:NPLC {NPLC_Value}"
            : $"NPLC {NPLC_Value}";

          await Task.Run ( ( ) =>
            _Comm.Send_Instrument_Command ( NPLC_Cmd ),
            Token );

          await Task.Delay ( 200, Token ); // Longer delay
        }

        Status_Label.Text = "Polling...";
        Progress_Label.Text = "";
        await Task.Delay ( 300, Token );
        while ( !Token.IsCancellationRequested )
        {
          _Cycle_Count++;
          DateTime Cycle_Time = DateTime.Now;

          for ( int I = 0; I < _Series.Count; I++ )
          {
            Token.ThrowIfCancellationRequested ( );

            var S = _Series [ I ];
            Progress_Label.Text =
              $"Cycle {_Cycle_Count}: querying " +
              $"{S.Name} (GPIB {S.Address})";

            await Task.Run ( ( ) =>
            {
              _Comm.Change_GPIB_Address ( S.Address );
            }, Token );

            await Task.Delay ( 50, Token );

            // Configure HP 34401A on first cycle
            if ( !Configured [ I ] &&
              S.Type == Meter_Type.HP_34401A )
            {
              string Config_Cmd =
                Get_Config_Command_For_34401A ( Command );
              if ( !string.IsNullOrEmpty ( Config_Cmd ) &&
                !Config_Cmd.EndsWith ( "?" ) )
              {
                await Task.Run ( ( ) =>
                  _Comm.Send_Instrument_Command (
                    Config_Cmd ), Token );
                await Task.Delay ( 300, Token );
              }
              Configured [ I ] = true;
            }

            // Translate command for this instrument's type
            string Inst_Command = Translate_Command_For_Instrument (
              Command, S.Type );

            string Response = await Task.Run ( ( ) =>
              _Comm.Query_Instrument ( Inst_Command ), Token );

            Token.ThrowIfCancellationRequested ( );

            if ( double.TryParse ( Response,
              System.Globalization.NumberStyles.Float,
              System.Globalization.CultureInfo
                .InvariantCulture,
              out double Value ) )
            {
              S.Points.Add ( (Cycle_Time, Value) );
            }
          }

          Cycle_Label.Text = $"Cycle {_Cycle_Count}";
          Chart_Panel.Invalidate ( );

          await Task.Delay ( Delay_Ms, Token );
        }
      }
      catch ( OperationCanceledException )
      {
        // Stopped by user
      }
      catch ( TimeoutException )
      {
        // Write or read timeout - already logged by Instrument_Comm
        MessageBox.Show (
          "Operation timed out. Check instrument connection " +
          "and GPIB bus status.",
          "Timeout Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
      }
      catch ( Exception Ex )
      {
        MessageBox.Show (
          $"Polling error: {Ex.Message}",
          "Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error );
      }
      finally
      {
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

    private void Stop_Polling ( )
    {
      _Cts?.Cancel ( );
    }

    private void Finish_Polling ( )
    {
      _Is_Running = false;
      _Cts?.Dispose ( );
      _Cts = null;
      Start_Stop_Button.Text = "Start";
      Status_Label.Text = "Idle";
      Status_Label.ForeColor = Color.Gray;
      Query_Text.Enabled = true;
      Delay_Numeric.Enabled = true;
      NPLC_Combo.Enabled = true;
      Measurement_Combo.Enabled = true;
      Continuous_Check.Enabled = true;
      Clear_Button.Enabled = true;
      Load_Button.Enabled = true;
    }

    private string Translate_Command_For_Instrument (
      string Base_Command, Meter_Type Meter )
    {
      // Find the measurement that matches the base command
      foreach ( var Measurement in _Measurements )
      {
        if ( Measurement.Cmd_3458A == Base_Command )
        {
          // Translate to the appropriate command for
          // this meter type
          if ( Meter == Meter_Type.HP_34401A )
          {
            string Cmd = Measurement.Cmd_34401A;
            if ( !string.IsNullOrEmpty ( Cmd ) )
            {
              // 34401A needs READ? for configured
              // measurements
              if ( !Cmd.EndsWith ( "?" ) )
              {
                return "READ?";
              }
              return Cmd;
            }
          }

          // Default to 3458A command
          return Base_Command;
        }
      }

      // If not found in measurement list,
      // return as-is (custom command)
      return Base_Command;
    }

    private string Get_Config_Command_For_34401A (
      string Base_Command )
    {
      // Find the measurement that matches the base command
      foreach ( var Measurement in _Measurements )
      {
        if ( Measurement.Cmd_3458A == Base_Command )
        {
          return Measurement.Cmd_34401A;
        }
      }

      return Base_Command;
    }

    private void Clear_Button_Click (
      object Sender, EventArgs E )
    {
      if ( _Is_Running )
      {
        return;
      }

      foreach ( var S in _Series )
      {
        S.Points.Clear ( );
      }

      _Cycle_Count = 0;
      Cycle_Label.Text = "";
      Progress_Label.Text = "";
      Chart_Panel.Invalidate ( );
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
      _Record_Query = Query_Text.Text.Trim ( );
      _Record_Start = DateTime.Now;

      foreach ( var S in _Series )
      {
        S.Points.Clear ( );
      }

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

      int Total_Points = 0;
      foreach ( var S in _Series )
      {
        Total_Points += S.Points.Count;
      }

      if ( Total_Points == 0 )
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
      string File_Name =
        $"{Timestamp}_Multi_{_Record_Query.Replace ( "?", "" )}.csv";
      string File_Path = Path.Combine ( Folder, File_Name );

      // Collect all unique timestamps across all series
      var All_Times = new SortedSet<DateTime> ( );
      foreach ( var S in _Series )
      {
        foreach ( var P in S.Points )
        {
          All_Times.Add ( P.Time );
        }
      }

      using var Writer = new StreamWriter ( File_Path );
      Writer.WriteLine ( $"# Query: {_Record_Query}" );
      Writer.WriteLine (
        $"# Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" );
      Writer.WriteLine (
        $"# Instruments: {_Series.Count}" );

      for ( int I = 0; I < _Series.Count; I++ )
      {
        Writer.WriteLine (
          $"# Instrument {I + 1}: {_Series [ I ].Name} " +
          $"(GPIB {_Series [ I ].Address})" );
      }

      // Header row
      Writer.Write ( "Timestamp" );
      foreach ( var S in _Series )
      {
        Writer.Write ( $",{S.Name} (GPIB {S.Address})" );
      }
      Writer.WriteLine ( );

      // Build lookup dictionaries for each series
      var Lookups =
        new List<Dictionary<DateTime, double>> ( );
      foreach ( var S in _Series )
      {
        var Dict = new Dictionary<DateTime, double> ( );
        foreach ( var P in S.Points )
        {
          Dict [ P.Time ] = P.Value;
        }
        Lookups.Add ( Dict );
      }

      foreach ( DateTime T in All_Times )
      {
        Writer.Write ( $"{T:yyyy-MM-dd HH:mm:ss.fff}" );
        for ( int I = 0; I < _Series.Count; I++ )
        {
          if ( Lookups [ I ].TryGetValue ( T,
            out double Val ) )
          {
            Writer.Write ( $",{Val.ToString (
              System.Globalization.CultureInfo
                .InvariantCulture )}" );
          }
          else
          {
            Writer.Write ( "," );
          }
        }
        Writer.WriteLine ( );
      }

      MessageBox.Show (
        $"Saved {All_Times.Count} rows to:\n{File_Path}",
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
          "Stop polling before loading.",
          "Polling in Progress",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      string Folder = Get_Graph_Captures_Folder ( );

      using var Dlg = new OpenFileDialog ( );
      Dlg.Title = "Load Multi-Poll Data";
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
      string [ ] Lines = File.ReadAllLines ( File_Path );

      // Find header row (first non-comment, non-empty line)
      int Header_Index = -1;
      for ( int I = 0; I < Lines.Length; I++ )
      {
        if ( !Lines [ I ].StartsWith ( "#" )
          && !string.IsNullOrWhiteSpace ( Lines [ I ] ) )
        {
          Header_Index = I;
          break;
        }
      }

      if ( Header_Index < 0 )
      {
        MessageBox.Show (
          "No valid data found in file.",
          "Load Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      string [ ] Headers =
        Lines [ Header_Index ].Split ( ',' );

      int Col_Count = Headers.Length - 1; // minus Timestamp

      if ( Col_Count <= 0 )
      {
        MessageBox.Show (
          "No instrument columns found.",
          "Load Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      // Rebuild series from file columns
      _Series.Clear ( );
      for ( int I = 0; I < Col_Count; I++ )
      {
        _Series.Add ( new Instrument_Series
        {
          Name = Headers [ I + 1 ].Trim ( ),
          Address = 0,
          Type = Meter_Type.Keysight_3458A,
          Points =
            new List<(DateTime Time, double Value)> ( ),
          Line_Color = _Theme.Line_Colors [
            I % _Theme.Line_Colors.Length ]
        } );
      }

      // Parse data rows
      for ( int I = Header_Index + 1; I < Lines.Length; I++ )
      {
        string Line = Lines [ I ].Trim ( );
        if ( string.IsNullOrEmpty ( Line )
          || Line.StartsWith ( "#" ) )
        {
          continue;
        }

        string [ ] Parts = Line.Split ( ',' );
        if ( Parts.Length < 2 )
        {
          continue;
        }

        if ( !DateTime.TryParse ( Parts [ 0 ],
          out DateTime T ) )
        {
          continue;
        }

        for ( int J = 1;
          J < Parts.Length && J - 1 < _Series.Count; J++ )
        {
          if ( double.TryParse ( Parts [ J ],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo
              .InvariantCulture,
            out double Val ) )
          {
            _Series [ J - 1 ].Points.Add ( (T, Val) );
          }
        }
      }

      int Total = 0;
      foreach ( var S in _Series )
      {
        Total += S.Points.Count;
      }

      Progress_Label.Text =
        $"Loaded {_Series.Count} instruments, " +
        $"{Total} points";
      Chart_Panel.Invalidate ( );
    }

    private static string Get_Graph_Captures_Folder ( )
    {
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

      string Fallback = Path.Combine (
        Base, "Graph_Captures" );
      Directory.CreateDirectory ( Fallback );
      return Fallback;
    }

    // ===== Chart Rendering (Stacked Subplots) =====

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

      using var Bg_Brush = new SolidBrush ( _Theme.Background );
      G.FillRectangle ( Bg_Brush, 0, 0, W, H );

      if ( _Series.Count == 0 )
      {
        using var Empty_Font = new Font ( "Segoe UI", 10F );
        using var Empty_Brush =
          new SolidBrush ( _Theme.Labels );
        G.DrawString (
          "No instruments. Add instruments and press Start.",
          Empty_Font, Empty_Brush, 20, H / 2 );
        return;
      }

      bool Has_Data = false;
      foreach ( var S in _Series )
      {
        if ( S.Points.Count > 0 )
        {
          Has_Data = true;
          break;
        }
      }

      if ( !Has_Data )
      {
        using var Empty_Font = new Font ( "Segoe UI", 10F );
        using var Empty_Brush =
          new SolidBrush ( _Theme.Labels );

        int Y_Pos = 30;
        G.DrawString (
          "Press Start to begin polling. Instruments:",
          Empty_Font, Empty_Brush, 20, Y_Pos );
        Y_Pos += 25;

        for ( int I = 0; I < _Series.Count; I++ )
        {
          var S = _Series [ I ];
          using var Dot_Brush =
            new SolidBrush ( S.Line_Color );
          G.FillEllipse ( Dot_Brush,
            30, Y_Pos + 3, 10, 10 );
          G.DrawString (
            $"{S.Name}  (GPIB {S.Address})",
            Empty_Font, Empty_Brush, 48, Y_Pos );
          Y_Pos += 22;
        }
        return;
      }

      // Compute shared time range across all series
      DateTime Time_Min = DateTime.MaxValue;
      DateTime Time_Max = DateTime.MinValue;
      foreach ( var S in _Series )
      {
        if ( S.Points.Count == 0 )
        {
          continue;
        }
        if ( S.Points [ 0 ].Time < Time_Min )
        {
          Time_Min = S.Points [ 0 ].Time;
        }
        if ( S.Points [ S.Points.Count - 1 ].Time > Time_Max )
        {
          Time_Max = S.Points [ S.Points.Count - 1 ].Time;
        }
      }

      double Time_Range_Sec =
        ( Time_Max - Time_Min ).TotalSeconds;
      if ( Time_Range_Sec < 0.001 )
      {
        Time_Range_Sec = 1.0;
      }

      int Margin_Left = 80;
      int Margin_Right = 20;
      int Margin_Top = 10;
      int Margin_Bottom = 30;
      int Gap = 8;

      int Chart_W = W - Margin_Left - Margin_Right;
      int Total_H = H - Margin_Top - Margin_Bottom;
      int Subplot_Count = _Series.Count;
      int Subplot_H =
        ( Total_H - Gap * ( Subplot_Count - 1 ) )
        / Subplot_Count;

      if ( Chart_W < 10 || Subplot_H < 30 )
      {
        return;
      }

      using var Grid_Pen = new Pen ( _Theme.Grid, 1f );
      using var Sep_Pen = new Pen ( _Theme.Separator, 1f );
      Sep_Pen.DashStyle =
        System.Drawing.Drawing2D.DashStyle.Dash;
      using var Label_Font =
        new Font ( "Consolas", 7.5F );
      using var Name_Font =
        new Font ( "Segoe UI", 8F, FontStyle.Bold );
      using var Label_Brush =
        new SolidBrush ( _Theme.Labels );
      using var X_Label_Font =
        new Font ( "Segoe UI", 7.5F );

      for ( int SI = 0; SI < _Series.Count; SI++ )
      {
        var S = _Series [ SI ];
        int Sub_Top = Margin_Top
          + SI * ( Subplot_H + Gap );
        int Sub_Bottom = Sub_Top + Subplot_H;

        // Instrument name label
        using var Name_Brush =
          new SolidBrush ( S.Line_Color );
        G.DrawString ( $"{S.Name} (GPIB {S.Address})",
          Name_Font, Name_Brush,
          Margin_Left + 4, Sub_Top + 2 );

        // Draw separator between subplots
        if ( SI > 0 )
        {
          int Sep_Y = Sub_Top - Gap / 2;
          G.DrawLine ( Sep_Pen,
            Margin_Left, Sep_Y,
            W - Margin_Right, Sep_Y );
        }

        if ( S.Points.Count == 0 )
        {
          G.DrawString ( "No data",
            Label_Font, Label_Brush,
            Margin_Left + 4, Sub_Top + 20 );
          continue;
        }

        // Y-axis range for this subplot
        double Min_V = double.MaxValue;
        double Max_V = double.MinValue;
        foreach ( var P in S.Points )
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

        int Label_Top = Sub_Top + 18;
        int Plot_H = Sub_Bottom - Label_Top;

        // Grid lines (horizontal)
        int Num_Grid = 4;
        for ( int I = 0; I <= Num_Grid; I++ )
        {
          double Fraction = (double) I / Num_Grid;
          double Value = Padded_Min +
            Fraction * Padded_Range;
          int Y = Sub_Bottom -
            (int) ( Fraction * Plot_H );

          G.DrawLine ( Grid_Pen,
            Margin_Left, Y,
            W - Margin_Right, Y );

          string Lbl = Format_Value ( Value );
          var Lbl_Size = G.MeasureString (
            Lbl, Label_Font );
          G.DrawString ( Lbl, Label_Font, Label_Brush,
            Margin_Left - Lbl_Size.Width - 4,
            Y - Lbl_Size.Height / 2 );
        }

        // Build data points
        int Count = S.Points.Count;
        PointF [ ] Points = new PointF [ Count ];
        for ( int I = 0; I < Count; I++ )
        {
          double Time_Sec =
            ( S.Points [ I ].Time - Time_Min ).TotalSeconds;
          double Time_Frac = Time_Sec / Time_Range_Sec;
          double V_Frac =
            ( S.Points [ I ].Value - Padded_Min )
            / Padded_Range;

          float X = Margin_Left +
            (float) ( Time_Frac * Chart_W );
          float Y = Sub_Bottom -
            (float) ( V_Frac * Plot_H );

          Points [ I ] = new PointF ( X, Y );
        }

        // Gradient fill under line
        using var Line_Pen =
          new Pen ( S.Line_Color, 2f );
        Color Fill_Top = Color.FromArgb (
          60, S.Line_Color );
        Color Fill_Bottom = Color.FromArgb (
          5, S.Line_Color );

        if ( Count >= 2 )
        {
          PointF [ ] Fill_Points =
            new PointF [ Count + 2 ];
          Array.Copy ( Points, Fill_Points, Count );
          Fill_Points [ Count ] = new PointF (
            Points [ Count - 1 ].X, Sub_Bottom );
          Fill_Points [ Count + 1 ] = new PointF (
            Points [ 0 ].X, Sub_Bottom );

          using var Fill_Brush =
            new System.Drawing.Drawing2D
              .LinearGradientBrush (
                new PointF ( 0, Label_Top ),
                new PointF ( 0, Sub_Bottom ),
                Fill_Top, Fill_Bottom );
          G.FillPolygon ( Fill_Brush, Fill_Points );
          G.DrawLines ( Line_Pen, Points );
        }

        // Data point dots
        using var Dot_Brush = new SolidBrush (
          Color.FromArgb ( 200,
            S.Line_Color.R,
            S.Line_Color.G,
            S.Line_Color.B ) );
        float Dot_Size = Count > 100 ? 3f :
          Count > 50 ? 4f : 5f;
        foreach ( PointF P in Points )
        {
          G.FillEllipse ( Dot_Brush,
            P.X - Dot_Size / 2, P.Y - Dot_Size / 2,
            Dot_Size, Dot_Size );
        }

        // Highlight last point
        if ( Count > 0 )
        {
          PointF Last = Points [ Count - 1 ];
          using var Glow_Pen = new Pen (
            Color.FromArgb ( 80, S.Line_Color ), 6f );
          G.DrawEllipse ( Glow_Pen,
            Last.X - 5, Last.Y - 5, 10, 10 );
          using var Last_Brush =
            new SolidBrush ( Color.White );
          G.FillEllipse ( Last_Brush,
            Last.X - 3, Last.Y - 3, 6, 6 );

          // Latest value text
          string Val_Text = Format_Value (
            S.Points [ Count - 1 ].Value );
          var Val_Size = G.MeasureString (
            Val_Text, Label_Font );
          float Tx = Last.X + 8;
          if ( Tx + Val_Size.Width > W - Margin_Right )
          {
            Tx = Last.X - Val_Size.Width - 8;
          }
          G.DrawString ( Val_Text, Label_Font,
            Name_Brush, Tx,
            Last.Y - Val_Size.Height / 2 );
        }
      }

      // X-axis time labels (bottom)
      int Num_X_Labels = Math.Min ( 8,
        (int) ( Chart_W / 80.0 ) );
      if ( Num_X_Labels < 2 )
        Num_X_Labels = 2;

      for ( int I = 0; I <= Num_X_Labels; I++ )
      {
        double Fraction = (double) I / Num_X_Labels;
        int X_Pos = Margin_Left +
          (int) ( Fraction * Chart_W );

        double Sec = Fraction * Time_Range_Sec;
        string Time_Text;
        if ( Time_Range_Sec < 60 )
        {
          Time_Text = $"{Sec:F1}s";
        }
        else if ( Time_Range_Sec < 3600 )
        {
          Time_Text = $"{Sec / 60:F1}m";
        }
        else
        {
          Time_Text = $"{Sec / 3600:F1}h";
        }

        var Ts = G.MeasureString (
          Time_Text, X_Label_Font );
        G.DrawString ( Time_Text, X_Label_Font,
          Label_Brush,
          X_Pos - Ts.Width / 2,
          H - Margin_Bottom + 8 );

        // Vertical grid line
        G.DrawLine ( Grid_Pen,
          X_Pos, Margin_Top,
          X_Pos, H - Margin_Bottom );
      }
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

        // Update series line colors from theme palette
        for ( int I = 0; I < _Series.Count; I++ )
        {
          _Series [ I ].Line_Color =
            _Theme.Line_Colors [
              I % _Theme.Line_Colors.Length ];
        }

        Chart_Panel.Invalidate ( );
      }
    }

    private void Chart_Panel_Resize (
      object? Sender, EventArgs E )
    {
      Chart_Panel.Invalidate ( );
    }

    private static string Format_Value ( double Value )
    {
      double Abs = Math.Abs ( Value );

      if ( Abs >= 1e6 )
        return $"{Value / 1e6:F2}M";
      if ( Abs >= 1e3 )
        return $"{Value / 1e3:F2}k";
      if ( Abs >= 1.0 )
        return $"{Value:F4}";
      if ( Abs >= 0.001 )
        return $"{Value * 1000:F2}m";
      if ( Abs >= 0.000001 )
        return $"{Value * 1e6:F2}u";
      if ( Abs < 1e-12 )
        return "0";
      return $"{Value:E2}";
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
      } = new List<(DateTime, double)> ( );
      public Color Line_Color
      {
        get; set;
      }
    }
  }
}
