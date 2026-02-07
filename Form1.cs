// ============================================================================
// File:        Form1.cs
// Project:     Keysight 3458A Multimeter Controller
// Description: Main application window for controlling a Keysight (HP) 3458A
//              8.5-digit digital multimeter via a Prologix GPIB-USB adapter.
//
// Purpose:
//   This form serves as the primary user interface, providing three main
//   functional areas:
//
//   1. Command Reference List (left panel):
//      Displays all supported 3458A GPIB commands in a scrollable ListBox.
//      When a command is selected, its full details (syntax, parameters,
//      query form, default value, and example) are shown in a read-only
//      detail TextBox beside the list. A button opens the full searchable
//      command dictionary in a separate modal dialog (Dictionary_Form).
//
//   2. Connection Settings (right panel):
//      Configures the serial port connection to the Prologix GPIB-USB-HS
//      adapter. The user can select:
//        - COM port (with a refresh button to re-scan available ports)
//        - Baud rate (default 115200)
//        - Data bits (7 or 8, default 8)
//        - Parity (None, Odd, Even, Mark, Space; default None)
//        - Stop bits (One, OnePointFive, Two; default One)
//        - Flow control / handshake (None, XOnXOff, RtsCts, RtsCtsXOnXOff;
//          default None)
//      Below the serial settings, the Prologix-specific GPIB options are:
//        - GPIB address (0-30, default 22 for the 3458A)
//        - EOS mode (CR+LF, CR, LF, None; default LF)
//        - Auto Read checkbox (++auto; default checked)
//        - EOI Enabled checkbox (++eoi; default checked)
//      A "Defaults" button restores all settings to their recommended values.
//      The "Connect" / "Disconnect" button opens or closes the serial port
//      and configures the Prologix adapter. While connected, all serial
//      settings are disabled to prevent changes mid-session.
//
//   3. Communication Event Handling:
//      Subscribes to three events from the Prologix_Serial_Comm class:
//        - Connection_Changed: updates the status label and toggles
//          UI enable/disable state (thread-safe via Invoke).
//        - Error_Occurred: displays errors in a MessageBox (thread-safe).
//        - Data_Received: placeholder for future data display/logging.
//
// Hardware Context:
//   - Instrument: Keysight / HP / Agilent 3458A Digital Multimeter
//   - Interface:  Prologix GPIB-USB-HS Controller (USB-to-GPIB bridge)
//   - Protocol:   Prologix ++ commands configure the adapter; instrument
//                 commands are sent as plain-text GPIB strings.
//
// Dependencies:
//   - System.IO.Ports (SerialPort for USB-serial communication)
//   - Command_Dictionary.cs (static command reference data)
//   - Prologix_Serial_Comm.cs (serial/GPIB communication layer)
//   - Dictionary_Form.cs (full searchable command dictionary dialog)
//   - Form1.Designer.cs (WinForms designer-generated layout code)
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

using System.IO.Ports;
using System.Windows.Forms;

namespace Multimeter_Controller
{
  public partial class Form1 : Form
  {
    private List<Command_Entry> _All_Commands;
    private readonly Prologix_Serial_Comm _Comm;
    private Meter_Type _Selected_Meter = Meter_Type.Keysight_3458A;
    private CancellationTokenSource? _Scan_Cts;
    private bool _Is_Scanning;

    private readonly List<(string Name, int Address, Meter_Type Type)>
      _Instruments = new List<(string, int, Meter_Type)> ( );

    public Form1 ( )
    {
      InitializeComponent ( );

      Layout_Connection_Group ( );

      _All_Commands = Command_Dictionary.Get_All_Commands (
        _Selected_Meter );
      _Comm = new Prologix_Serial_Comm ( );

      _Comm.Connection_Changed += Comm_Connection_Changed;
      _Comm.Error_Occurred += Comm_Error_Occurred;
      _Comm.Data_Received += Comm_Data_Received;

      Populate_Command_List ( );
      Populate_Connection_Controls ( );

      // Populate instrument type combo
      Instrument_Type_Combo.Items.Add ( "Keysight 3458A" );
      Instrument_Type_Combo.Items.Add ( "HP 34401A" );
      Instrument_Type_Combo.Items.Add ( "HP 33120A" );
      Instrument_Type_Combo.SelectedIndex = 0;
    }


    private void Layout_Connection_Group ( )
    {
      int Grp_X = 1080;
      int Lbl_X = 10;
      int Ctl_X = 110;
      int Ctl_W = 110;


      int Grp_Y = 12;
      int Grp_W = 240;
      int Grp_H = 503;




      int Row_H = 30;
      int Y = 22;


      // int Row_H = Font.Height + 14;
      // int Y = Font.Height + 6;


      // GroupBox
      Connection_Group.Location = new Point ( Grp_X, Grp_Y );
      Connection_Group.Size = new Size ( Grp_W, Grp_H );
      Connection_Group.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

      // ================= COM Port =================
      COM_Port_Label.Location = new Point ( Lbl_X, Y + 3 );
      COM_Port_Label.Text = "COM Port:";

      COM_Port_Combo.Location = new Point ( Ctl_X, Y );
      COM_Port_Combo.Size = new Size ( Ctl_W - 35, 23 );

      Refresh_Ports_Button.Location =
          new Point ( Ctl_X + Ctl_W - 30, Y - 1 );
      Refresh_Ports_Button.Size = new Size ( 30, 25 );
      Refresh_Ports_Button.Text = "\u21BB";

      Y += Row_H;

      // ================= Baud Rate =================
      Baud_Rate_Label.Location = new Point ( Lbl_X, Y + 3 );
      Baud_Rate_Label.Text = "Baud Rate:";

      Baud_Rate_Combo.Location = new Point ( Ctl_X, Y );
      Baud_Rate_Combo.Size = new Size ( Ctl_W, 23 );

      Y += Row_H;

      // ================= Data Bits =================
      Data_Bits_Label.Location = new Point ( Lbl_X, Y + 3 );
      Data_Bits_Label.Text = "Data Bits:";

      Data_Bits_Combo.Location = new Point ( Ctl_X, Y );
      Data_Bits_Combo.Size = new Size ( Ctl_W, 23 );

      Y += Row_H;

      // ================= Parity =================
      Parity_Label.Location = new Point ( Lbl_X, Y + 3 );
      Parity_Label.Text = "Parity:";

      Parity_Combo.Location = new Point ( Ctl_X, Y );
      Parity_Combo.Size = new Size ( Ctl_W, 23 );

      Y += Row_H;

      // ================= Stop Bits =================
      Stop_Bits_Label.Location = new Point ( Lbl_X, Y + 3 );
      Stop_Bits_Label.Text = "Stop Bits:";

      Stop_Bits_Combo.Location = new Point ( Ctl_X, Y );
      Stop_Bits_Combo.Size = new Size ( Ctl_W, 23 );

      Y += Row_H;

      // ================= Flow Control =================
      Flow_Control_Label.Location = new Point ( Lbl_X, Y + 3 );
      Flow_Control_Label.Text = "Flow Control:";

      Flow_Control_Combo.Location = new Point ( Ctl_X, Y );
      Flow_Control_Combo.Size = new Size ( Ctl_W, 23 );

      Y += Row_H + 10;

      // ================= Prologix Header =================
      var Prologix_Header = new Label
      {
        AutoSize = true,
        Font = new Font ( "Segoe UI", 9F, FontStyle.Bold ),
        Location = new Point ( Lbl_X, Y ),
        Text = "Prologix Settings"
      };
      Connection_Group.Controls.Add ( Prologix_Header );

      Y += 22;

      // ================= GPIB Address =================
      GPIB_Address_Label.Location = new Point ( Lbl_X, Y + 3 );
      GPIB_Address_Label.Text = "GPIB Address:";

      GPIB_Address_Numeric.Location = new Point ( Ctl_X, Y );
      GPIB_Address_Numeric.Size = new Size ( 60, 23 );

      Y += Row_H;

      // ================= EOS Mode =================
      EOS_Mode_Label.Location = new Point ( Lbl_X, Y + 3 );
      EOS_Mode_Label.Text = "EOS Mode:";

      EOS_Mode_Combo.Location = new Point ( Ctl_X, Y );
      EOS_Mode_Combo.Size = new Size ( Ctl_W, 23 );

      Y += Row_H;

      // ================= Auto Read =================
      Auto_Read_Check.Location = new Point ( Lbl_X, Y );
      Auto_Read_Check.Text = "Auto Read (++auto)";

      Y += 24;

      // ================= EOI =================
      EOI_Check.Location = new Point ( Lbl_X, Y );
      EOI_Check.Text = "EOI Enabled (++eoi)";

      Y += 30;

      // ================= Defaults Button =================
      Defaults_Button.Location = new Point ( Lbl_X, Y );
      Defaults_Button.Size = new Size ( 90, 30 );
      Defaults_Button.Text = "Defaults";

      Y += 36;

      // ================= Status + Connect =================
      Connection_Status_Label.Location = new Point ( Lbl_X, Y + 5 );
      Connection_Status_Label.Text = "Disconnected";
      Connection_Status_Label.ForeColor = Color.Red;

      Connect_Button.Location = new Point ( Ctl_X, Y );
      Connect_Button.Size = new Size ( Ctl_W, 30 );
      Connect_Button.Text = "Connect";
    }




    protected override void OnFormClosed ( FormClosedEventArgs E )
    {
      _Comm.Dispose ( );
      base.OnFormClosed ( E );
    }

    // ===== Command List =====

    private void Populate_Command_List ( )
    {
      Command_List.Items.Clear ( );
      foreach ( Command_Entry Entry in _All_Commands )
      {
        Command_List.Items.Add (
          $"{Entry.Command}  -  {Entry.Description}" );
      }
    }

    private void Command_List_Selected_Index_Changed (
      object Sender, EventArgs E )
    {
      if ( Command_List.SelectedIndex < 0
        || Command_List.SelectedIndex >= _All_Commands.Count )
      {
        Detail_Text_Box.Text = "";
        return;
      }

      Command_Entry Selected =
        _All_Commands [ Command_List.SelectedIndex ];
      Detail_Text_Box.Text =
        $"Command:     {Selected.Command}\r\n" +
        $"Syntax:      {Selected.Syntax}\r\n" +
        $"Category:    {Selected.Category}\r\n" +
        $"Description: {Selected.Description}\r\n" +
        $"Parameters:  {Selected.Parameters}\r\n" +
        $"Query Form:  {Selected.Query_Form}\r\n" +
        $"Default:     {Selected.Default_Value}\r\n" +
        $"Example:     {Selected.Example}";

      Command_Input_Text_Box.Text = Selected.Example;
    }


    private void Open_Dictionary_Button_Click (
      object Sender, EventArgs E )
    {
      using var Dictionary_Window =
        new Dictionary_Form ( _Selected_Meter );
      Dictionary_Window.ShowDialog ( this );
    }

    private void Voltage_Reader_Button_Click (
      object Sender, EventArgs E )
    {
      var Voltage_Window =
        new Voltage_Reader_Form ( _Comm, _Selected_Meter );
      Voltage_Window.Show ( this );
    }

    private void Multi_Poll_Button_Click (
      object Sender, EventArgs E )
    {
      if ( _Instruments.Count == 0 )
      {
        MessageBox.Show (
          "Add at least one instrument to the list first.",
          "No Instruments",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      var Multi_Window =
        new Multi_Poll_Form ( _Comm, _Instruments );
      Multi_Window.Show ( this );
    }

    // ===== Instrument List =====


 private async void Add_Instrument_Button_Click (
      object Sender, EventArgs E )
    {
      int Address = (int) Instrument_Address_Numeric.Value;
      Meter_Type Type = Instrument_Type_Combo.SelectedIndex switch
      {
        1 => Meter_Type.HP_34401A,
        2 => Meter_Type.HP_33120A,
        _ => Meter_Type.Keysight_3458A
      };

      string Name = Instrument_Name_Text.Text.Trim ( );
      if ( string.IsNullOrEmpty ( Name ) )
      {
        Name = Get_Meter_Name ( Type );
      }

      // If connected, verify the instrument at this address
      if ( _Comm.Is_Connected )
      {
        Add_Instrument_Button.Enabled = false;
        Append_Response (
          $"[Verifying GPIB address {Address}...]" );

        try
        {
          string ID_Response = await Task.Run ( ( ) =>
            _Comm.Verify_GPIB_Address ( Address ) );

          if ( string.IsNullOrEmpty ( ID_Response ) )
          {
            DialogResult Result = MessageBox.Show (
              $"No instrument responded at GPIB address {Address}.\n\n" +
              "Add it to the list anyway?",
              "Verification Failed",
              MessageBoxButtons.YesNo,
              MessageBoxIcon.Warning );

            if ( Result != DialogResult.Yes )
            {
              Append_Response (
                $"[No instrument at address {Address} - not added]" );
              return;
            }

            Append_Response (
              $"[No response at address {Address} - added anyway]" );
          }
          else
          {
            Append_Response (
              $"[Verified at address {Address}: {ID_Response}]" );

            // If user left the default name, use the ID response
            if ( Name == Get_Meter_Name ( Type ) )
            {
              Name = ID_Response.Length > 40
                ? ID_Response.Substring ( 0, 40 )
                : ID_Response;
            }
          }
        }
        finally
        {
          Add_Instrument_Button.Enabled = true;
        }
      }

      _Instruments.Add ( (Name, Address, Type) );
      Instruments_List.Items.Add (
        $"{Name}  (GPIB {Address}, {Get_Meter_Name ( Type )})" );
    }


    private void Instrument_Type_Combo_SelectedIndexChanged (
      object Sender, EventArgs E )
    {
      Meter_Type Type = Instrument_Type_Combo.SelectedIndex switch
      {
        1 => Meter_Type.HP_34401A,
        2 => Meter_Type.HP_33120A,
        _ => Meter_Type.Keysight_3458A
      };

      Instrument_Name_Text.Text = Get_Meter_Name ( Type );

      _Selected_Meter = Type;
      _All_Commands = Command_Dictionary.Get_All_Commands (
        _Selected_Meter );
      Populate_Command_List ( );
      Detail_Text_Box.Text = "";
      Command_Input_Text_Box.Text = "";
    }

    private static string Get_Meter_Name ( Meter_Type Type )
    {
      return Type switch
      {
        Meter_Type.HP_34401A => "HP 34401A",
        Meter_Type.HP_33120A => "HP 33120A",
        _ => "Keysight 3458A"
      };
    }

    private void Remove_Instrument_Button_Click (
      object Sender, EventArgs E )
    {
      int Index = Instruments_List.SelectedIndex;
      if ( Index < 0 )
      {
        return;
      }

      _Instruments.RemoveAt ( Index );
      Instruments_List.Items.RemoveAt ( Index );
    }

    private void Select_Instrument_Button_Click (
      object Sender, EventArgs E )
    {
      int Index = Instruments_List.SelectedIndex;
      if ( Index < 0 || Index >= _Instruments.Count )
      {
        return;
      }

      var Instrument = _Instruments [ Index ];

      // Switch GPIB address
      _Comm.Change_GPIB_Address ( Instrument.Address );
      GPIB_Address_Numeric.Value = Instrument.Address;

      // Switch meter type and refresh command list
      Instrument_Type_Combo.SelectedIndex = Instrument.Type switch
      {
        Meter_Type.HP_34401A => 1,
        Meter_Type.HP_33120A => 2,
        _ => 0
      };
    }

    private async void Scan_Bus_Button_Click (
      object Sender, EventArgs E )
    {
      if ( _Is_Scanning )
      {
        _Scan_Cts?.Cancel ( );
        return;
      }

      await Run_Bus_Scan ( );
    }

    private async Task Run_Bus_Scan ( )
    {
      if ( !_Comm.Is_Connected )
      {
        return;
      }

      _Is_Scanning = true;
      _Scan_Cts = new CancellationTokenSource ( );
      Scan_Bus_Button.Text = "Stop Scan";
      Append_Response ( "[Scanning GPIB bus...]" );

      var Progress = new Progress<string> ( Message =>
      {
        Append_Response ( Message );
      } );

      try
      {
        CancellationToken Token = _Scan_Cts.Token;

        List<Scan_Result> Results = await Task.Run ( ( ) =>
          _Comm.Scan_GPIB_Bus ( Progress, Token ) );

        // Clear existing instrument list and repopulate
        _Instruments.Clear ( );
        Instruments_List.Items.Clear ( );

        foreach ( Scan_Result Result in Results )
        {
          Meter_Type Type =
            Result.Detected_Type ?? Meter_Type.Keysight_3458A;

          string Type_Name = Type switch
          {
            Meter_Type.HP_34401A => "HP 34401A",
            Meter_Type.HP_33120A => "HP 33120A",
            _ => "Keysight 3458A"
          };

          // Use ID string as name, truncate if too long
          string Name = Result.ID_String.Length > 40
            ? Result.ID_String.Substring ( 0, 40 )
            : Result.ID_String;

          _Instruments.Add ( (Name, Result.Address, Type) );
          Instruments_List.Items.Add (
            $"{Name}  (GPIB {Result.Address}, {Type_Name})" );
        }

        if ( Results.Count == 0 )
        {
          Append_Response (
            "No instruments found on the GPIB bus." );
        }
      }
      finally
      {
        _Is_Scanning = false;
        _Scan_Cts?.Dispose ( );
        _Scan_Cts = null;
        Scan_Bus_Button.Text = "Scan Bus";
        Scan_Bus_Button.Enabled = _Comm.Is_Connected;
      }
    }

    private void Send_Button_Click (
      object Sender, EventArgs E )
    {
      string Command = Command_Input_Text_Box.Text.Trim ( );
      if ( string.IsNullOrEmpty ( Command ) )
      {
        return;
      }

      if ( !_Comm.Is_Connected )
      {
        Append_Response ( "[Not connected]" );
        return;
      }

      _Comm.Send_Instrument_Command ( Command );
      Append_Response ( $"> {Command}" );
    }

    private async void Query_Button_Click (
      object Sender, EventArgs E )
    {
      string Command = Command_Input_Text_Box.Text.Trim ( );
      if ( string.IsNullOrEmpty ( Command ) )
      {
        return;
      }

      if ( !_Comm.Is_Connected )
      {
        Append_Response ( "[Not connected]" );
        return;
      }

      Query_Button.Enabled = false;
      Send_Button.Enabled = false;

      try
      {
        string Response = await Task.Run ( ( ) =>
          _Comm.Query_Instrument ( Command ) );

        Append_Response ( $"> {Command}" );

        if ( !string.IsNullOrEmpty ( Response ) )
        {
          Append_Response ( $"  {Response}" );
        }
        else
        {
          Append_Response ( "  [No response]" );
        }
      }
      finally
      {
        Query_Button.Enabled = true;
        Send_Button.Enabled = true;
      }
    }

    private async void Diag_Button_Click (
      object Sender, EventArgs E )
    {
      string Command = Command_Input_Text_Box.Text.Trim ( );
      if ( string.IsNullOrEmpty ( Command ) )
      {
        Command = "++ver";
      }

      if ( !_Comm.Is_Connected )
      {
        Append_Response ( "[Not connected]" );
        return;
      }

      Diag_Button.Enabled = false;
      Append_Response ( $"[DIAG] Sending: {Command}" );

      try
      {
        string Result = await Task.Run ( ( ) =>
          _Comm.Raw_Diagnostic ( Command ) );
        Append_Response ( $"[DIAG] {Result}" );
      }
      finally
      {
        Diag_Button.Enabled = true;
      }
    }

    private void Append_Response ( string Text )
    {
      if ( Response_Text_Box.Text.Length > 0 )
      {
        Response_Text_Box.AppendText ( "\r\n" );
      }
      Response_Text_Box.AppendText ( Text );
    }

    // ===== Connection Controls =====

    private void Populate_Connection_Controls ( )
    {
      // COM ports
      Refresh_Ports ( );

      // Baud rates
      Baud_Rate_Combo.Items.Clear ( );
      foreach ( int Rate in
        Prologix_Serial_Comm.Get_Available_Baud_Rates ( ) )
      {
        Baud_Rate_Combo.Items.Add ( Rate );
      }
      Baud_Rate_Combo.SelectedItem = 115200;

      // Data bits
      Data_Bits_Combo.Items.Clear ( );
      foreach ( int Bits in
        Prologix_Serial_Comm.Get_Available_Data_Bits ( ) )
      {
        Data_Bits_Combo.Items.Add ( Bits );
      }
      Data_Bits_Combo.SelectedItem = 8;

      // Parity
      Parity_Combo.Items.Clear ( );
      foreach ( Parity P in Enum.GetValues ( typeof ( Parity ) ) )
      {
        Parity_Combo.Items.Add ( P );
      }
      Parity_Combo.SelectedItem = Parity.None;

      // Stop bits
      Stop_Bits_Combo.Items.Clear ( );
      Stop_Bits_Combo.Items.Add ( StopBits.One );
      Stop_Bits_Combo.Items.Add ( StopBits.OnePointFive );
      Stop_Bits_Combo.Items.Add ( StopBits.Two );
      Stop_Bits_Combo.SelectedItem = StopBits.One;

      // Flow control
      Flow_Control_Combo.Items.Clear ( );
      foreach ( Handshake H in
        Enum.GetValues ( typeof ( Handshake ) ) )
      {
        Flow_Control_Combo.Items.Add ( H );
      }
      Flow_Control_Combo.SelectedItem = Handshake.None;

      // GPIB address (0-30) - stays editable while
      // connected so the user can switch instruments
      GPIB_Address_Numeric.Minimum = 0;
      GPIB_Address_Numeric.Maximum = 30;
      GPIB_Address_Numeric.Value = 22;
      GPIB_Address_Numeric.ValueChanged +=
        GPIB_Address_Numeric_ValueChanged;

      // EOS mode
      EOS_Mode_Combo.Items.Clear ( );
      EOS_Mode_Combo.Items.Add ( "CR+LF" );
      EOS_Mode_Combo.Items.Add ( "CR" );
      EOS_Mode_Combo.Items.Add ( "LF" );
      EOS_Mode_Combo.Items.Add ( "None" );
      EOS_Mode_Combo.SelectedIndex = 2; // LF

      // EOI default to checked, auto-read off to avoid
      // errors on non-query commands
      Auto_Read_Check.Checked = false;
      EOI_Check.Checked = true;

      Update_Connection_Status ( false );
    }

    private void Refresh_Ports ( )
    {
      string? Previous_Selection =
        COM_Port_Combo.SelectedItem?.ToString ( );

      COM_Port_Combo.Items.Clear ( );
      string [ ] Ports =
        Prologix_Serial_Comm.Get_Available_Ports ( );

      foreach ( string Port in Ports )
      {
        COM_Port_Combo.Items.Add ( Port );
      }

      if ( Previous_Selection != null
        && COM_Port_Combo.Items.Contains ( Previous_Selection ) )
      {
        COM_Port_Combo.SelectedItem = Previous_Selection;
      }
      else if ( COM_Port_Combo.Items.Count > 0 )
      {
        COM_Port_Combo.SelectedIndex = 0;
      }
    }

    private void Refresh_Ports_Button_Click (
      object Sender, EventArgs E )
    {
      Refresh_Ports ( );
    }

    private void Defaults_Button_Click (
      object Sender, EventArgs E )
    {
      // Recommended defaults for Keysight 3458A
      // via Prologix GPIB-USB-HS adapter
      Baud_Rate_Combo.SelectedItem = 9600;
      Data_Bits_Combo.SelectedItem = 8;
      Parity_Combo.SelectedItem = Parity.None;
      Stop_Bits_Combo.SelectedItem = StopBits.Two;
      Flow_Control_Combo.SelectedItem = Handshake.None;
      GPIB_Address_Numeric.Value = 22;
      EOS_Mode_Combo.SelectedIndex = 2; // LF
      Auto_Read_Check.Checked = false;
      EOI_Check.Checked = true;
    }

    private void Connect_Button_Click (
      object Sender, EventArgs E )
    {
      if ( _Comm.Is_Connected )
      {
        _Comm.Disconnect ( );
        return;
      }

      if ( COM_Port_Combo.SelectedItem == null )
      {
        MessageBox.Show (
          "Please select a COM port.",
          "Connection Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      // Apply all settings from UI
      _Comm.Port_Name = COM_Port_Combo.SelectedItem.ToString ( )!;
      _Comm.Baud_Rate = (int) Baud_Rate_Combo.SelectedItem!;
      _Comm.Data_Bits = (int) Data_Bits_Combo.SelectedItem!;
      _Comm.Parity = (Parity) Parity_Combo.SelectedItem!;
      _Comm.Stop_Bits = (StopBits) Stop_Bits_Combo.SelectedItem!;
      _Comm.Flow_Control =
        (Handshake) Flow_Control_Combo.SelectedItem!;
      _Comm.GPIB_Address = (int) GPIB_Address_Numeric.Value;
      _Comm.Auto_Read = Auto_Read_Check.Checked;
      _Comm.EOI_Enabled = EOI_Check.Checked;
      _Comm.EOS_Mode =
        (Prologix_Eos_Mode) EOS_Mode_Combo.SelectedIndex;

      _Comm.Connect ( );
    }

    private void Update_Connection_Status ( bool Connected )
    {
      if ( Connected )
      {
        Connection_Status_Label.Text = "Connected";
        Connection_Status_Label.ForeColor = Color.Green;
        Connect_Button.Text = "Disconnect";

        // Disable settings while connected
        COM_Port_Combo.Enabled = false;
        Baud_Rate_Combo.Enabled = false;
        Data_Bits_Combo.Enabled = false;
        Parity_Combo.Enabled = false;
        Stop_Bits_Combo.Enabled = false;
        Flow_Control_Combo.Enabled = false;
        Refresh_Ports_Button.Enabled = false;
        Defaults_Button.Enabled = false;

        // Enable scan button
        Scan_Bus_Button.Enabled = true;
      }
      else
      {
        Connection_Status_Label.Text = "Disconnected";
        Connection_Status_Label.ForeColor = Color.Red;
        Connect_Button.Text = "Connect";

        // Enable settings while disconnected
        COM_Port_Combo.Enabled = true;
        Baud_Rate_Combo.Enabled = true;
        Data_Bits_Combo.Enabled = true;
        Parity_Combo.Enabled = true;
        Stop_Bits_Combo.Enabled = true;
        Flow_Control_Combo.Enabled = true;
        Refresh_Ports_Button.Enabled = true;
        Defaults_Button.Enabled = true;

        // Disable scan while disconnected
        Scan_Bus_Button.Enabled = false;
      }
    }

    // ===== Event Handlers =====

    private void Comm_Connection_Changed (
      object? Sender, bool Connected )
    {
      if ( InvokeRequired )
      {
        Invoke ( ( ) => Update_Connection_Status ( Connected ) );
      }
      else
      {
        Update_Connection_Status ( Connected );
      }
    }

    private void Comm_Error_Occurred (
      object? Sender, string Message )
    {
      if ( InvokeRequired )
      {
        Invoke ( ( ) => Show_Error ( Message ) );
      }
      else
      {
        Show_Error ( Message );
      }
    }

    private void Comm_Data_Received (
      object? Sender, string Data )
    {
      // Future use: log or display received data
    }

    private void GPIB_Address_Numeric_ValueChanged (
      object? Sender, EventArgs E )
    {
      if ( _Comm.Is_Connected )
      {
        _Comm.Change_GPIB_Address (
          (int) GPIB_Address_Numeric.Value );
      }
    }

    private void Show_Error ( string Message )
    {
      MessageBox.Show (
        Message,
        "Communication Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error );
    }
  }
}
