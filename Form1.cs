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
//      Subscribes to three events from the Instrument_Comm class:
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
//   - Command_Dictionary_Class.cs (static command reference data)
//   - Instrument_Comm.cs (serial/GPIB communication layer)
//   - Dictionary_Form.cs (full searchable command dictionary dialog)
//   - Form1.Designer.cs (WinForms designer-generated layout code)
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

using Multimeter_Controller.Properties;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO.Ports;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Trace_Execution_Namespace;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  public partial class Form1 : Form
  {
    private List<Command_Entry> _All_Commands;
    private readonly Instrument_Comm _Comm;
    private Meter_Type _Selected_Meter = Meter_Type_Extensions.Combo_Order [ 0 ];
    private CancellationTokenSource? _Scan_Cts;
    private bool _Is_Scanning;
    private bool _Ignore_Selection_Changed = false;

    // Maximum number of commands to keep in history
    private const int _Max_History_Size = 50;



    private readonly List<Instrument> _Instruments = new List<Instrument> ( );

    private int _Selected_Address = 0;



    private bool _Updating_Controls = false;


    private int _Selected_Index = -1;

    private Application_Settings _Settings = Application_Settings.Load ( );
    private Chart_Theme _Theme = Chart_Theme.Dark_Preset ( );

    private int _Instrument_Count = 0;
    public bool Is_GPIB = false;
    public bool Is_Serial = false;
    public bool Is_Ethernet = false;

    private bool _Cleanup_Done = false;
    private NPLC_Info_Form? _NPLC_Info_Form;
    private Session_Settings_Form? _Session_Settings_Form;

    public Form1 ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      InitializeComponent ( );

      // Populate instrument type combo first, then set selection
      _Updating_Controls = true;
      Instrument_Type_Combo.Items.Clear ( );

      foreach ( var Type in Meter_Type_Extensions.Combo_Order )
        Instrument_Type_Combo.Items.Add ( Type.Get_Name ( ) );

      Instrument_Type_Combo.SelectedIndex = Meter_Type.Generic_GPIB.To_Combo_Index ( );
      Instrument_Name_Text.Text = Meter_Type.Generic_GPIB.Get_Name ( );

      Load_NPLC_Combo ( Meter_Type.Generic_GPIB );
      _Updating_Controls = false;

      Instruments_List.DataSource = _Instruments;
      Instruments_List.DisplayMember = "Name";

      _Comm = new Instrument_Comm ( _Settings );
      _Comm.Connection_Changed += Comm_Connection_Changed;
      _Comm.Error_Occurred += Comm_Error_Occurred;
      _Comm.Data_Received += Comm_Data_Received;

      Populate_Connection_Controls ( );

      Connected_Instrument_Textbox.Text = "";

      if ( !string.IsNullOrWhiteSpace ( _Settings.Default_Window_Title ) )
        this.Text = _Settings.Default_Window_Title + " - Launcher";

      Subnet_Textbox.Text = _Settings.Network_Scan_Subnet;
      Capture_Trace.Write ( $"Subnet set to : [{_Settings.Network_Scan_Subnet}]" );
      Capture_Trace.Write ( $"Control name  : [{Subnet_Textbox.Name}]" );

      Set_Button_State ( );
    }







    private void Load_NPLC_Combo ( Meter_Type Type, decimal? Current_NPLC = null )
    {
      NPLC_Combo_Box.Items.Clear ( );
      NPLC_Combo_Box.Items.AddRange (
          Type.Get_NPLC_Values ( )
              .Select ( n => n.ToString ( CultureInfo.InvariantCulture ) )
              .ToArray<object> ( ) );

      string Target = Current_NPLC?.ToString ( CultureInfo.InvariantCulture )
                      ?? _Settings.Default_NPLC
                      ?? "1";

      NPLC_Combo_Box.SelectedItem = Target;
      if ( NPLC_Combo_Box.SelectedIndex < 0 )
        NPLC_Combo_Box.SelectedIndex = 0;
    }




    protected override void OnFormClosing ( FormClosingEventArgs e )
    {
      if ( _Comm.Is_Connected && !_Cleanup_Done )
      {
        e.Cancel = true;
        _ = Shutdown_And_Close ( );
        return;
      }

      _NPLC_Info_Form?.Close ( );

      base.OnFormClosing ( e );
    }

    private async Task Shutdown_And_Close ( )
    {
      _Cleanup_Done = true;
      await _Comm.Disconnect_Async ( );   // all the USB cleanup is in here
      this.Invoke ( ( ) => this.Close ( ) );  // re-trigger close, cleanup done
    }






    // ===== Command List =====

    private void Populate_Command_List ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

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



      if ( _Ignore_Selection_Changed )
        return;


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

      Send_Command_Text_Box.Text = Selected.Command;

      Update_Button_State ( Selected );


      Execute_Button.Refresh ( );

    }






    private void Apply_Settings ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // ===== WINDOW TITLE =====

      if ( !string.IsNullOrWhiteSpace ( _Settings.Default_Window_Title ) )
        this.Text = _Settings.Default_Window_Title;
      else
        this.Text = "W&W Co.  Since 1969";

      // ===== SAVE FOLDER =====

      if ( !string.IsNullOrEmpty ( _Settings.Default_Save_Folder ) )
      {
        try
        {
          Directory.CreateDirectory ( _Settings.Default_Save_Folder );
        }
        catch { }
      }


      // ===== GPIB SETTINGS =====

      if ( _Comm != null )
      {
        _Comm.Read_Timeout_Ms = _Settings.Default_GPIB_Timeout_Ms;
      }

      Subnet_Textbox.Text = _Settings.Network_Scan_Subnet;
      IP_Address_Textbox.Text = _Settings.Default_IP_Address;

      Capture_Trace.Write ( $"Subnet set to: [{_Settings.Network_Scan_Subnet}] Control name: [{Subnet_Textbox.Name}]" );
      Capture_Trace.Write ( "Settings applied successfully" );
    }




    protected override void OnLoad ( EventArgs E )
    {
      base.OnLoad ( E );

    }

    private void Open_Dictionary_Button_Click (
    object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      using var Dictionary_Window =
        new Dictionary_Form ( _Selected_Meter );
      Dictionary_Window.ShowDialog ( this );
    }


    public void Safe_UI_Update ( Action UI_Action )
    {
      /******************************************************************************
       * Safe_UI_Update - Safely execute UI updates from any thread
       *
       * PURPOSE:
       *   Ensures that UI updates are performed on the main UI thread, avoiding
       *   cross-thread exceptions. Swallows exceptions if the form is disposed.
       *
       * OPERATIONS:
       *   1. Checks if form is valid, not disposed, and handle is created
       *   2. If invoke required, uses BeginInvoke to run the action on UI thread
       *   3. Otherwise executes action directly
       *   4. Catches and ignores ObjectDisposedException, InvalidOperationException,
       *      or unexpected exceptions
       *
       * PARAMETERS:
       *   UI_Action - Action delegate containing UI update code
       *
       * RETURNS:
       *   void
       *
       * SIDE EFFECTS:
       *   - Safely executes UI updates
       *   - Prevents cross-thread exceptions
       *
       * THREAD SAFETY:
       *   Can be called from any thread
       *
       * NOTES:
       *   Designed for multi-threaded scenarios where background threads may
       *   need to update the UI.
       ******************************************************************************/


      using var Block = Trace_Block.Start_If_Enabled ( );

      try
      {
        if ( this == null || this.IsDisposed || !this.IsHandleCreated )
          return;

        if ( InvokeRequired )
        {
          // Check handle again to avoid race between check and invoke
          if ( this.IsHandleCreated && !this.IsDisposed )
            BeginInvoke ( UI_Action );
        }
        else
        {
          UI_Action ( );
        }
      }
      catch ( ObjectDisposedException )
      {
        // Ignore – form is closing
      }
      catch ( InvalidOperationException )
      {
        // Ignore – handle already destroyed
      }
      catch
      {
        // Swallow any unexpected error (should never occur)
      }

      Capture_Trace.Write ( "\n" );
    }



    private void Update_Trace_Button_State ( bool Trace_Visible )
    {


      using var Block = Trace_Block.Start_If_Enabled ( );



      if ( InvokeRequired )
      {
        BeginInvoke ( new Action ( ( ) => Update_Trace_Button_State ( Trace_Visible ) ) );
        return;
      }

      Button_Show_Execution_Trace.Text = Trace_Visible ? "Trace OFF" : "Trace ON";
      Button_Show_Execution_Trace.BackColor = Trace_Visible ? Color.Yellow : SystemColors.Control;
    }



    private void Trace_Window_Trace_Visibility_Changed ( bool Visible )
    {



      if ( InvokeRequired )
      {
        BeginInvoke ( ( ) => Trace_Window_Trace_Visibility_Changed ( Visible ) );
        return;
      }
      Button_Show_Execution_Trace.Text = Visible ? "Trace OFF" : "Trace ON";
      Button_Show_Execution_Trace.BackColor = Visible ? Color.Green : SystemColors.Control;
    }



    private void Button_Show_Execution_Trace_Click ( object? Sender, EventArgs Args )
    {
      Trace_Execution.Toggle ( );
      Button_Show_Execution_Trace.Text = Trace_Execution.IsRunning ? "Trace OFF" : "Trace ON";
      Button_Show_Execution_Trace.BackColor = Trace_Execution.IsRunning ? Color.Yellow : SystemColors.Control;
    }








    private void old_Multi_Poll_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Cursor = Cursors.WaitCursor;
      if ( _Instruments.Count == 0 )
      {
        MessageBox.Show (
            "No instruments configured. Please scan for instruments first.",
            "No Instruments",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning );
        Cursor = Cursors.Default;
        return;
      }

      var Cloned = _Instruments.Select ( Ins => new Instrument
      {
        Name = $"{Ins.Name}",
        Address = Ins.Address,
        Type = Ins.Type,
        Visible = Ins.Visible,
        NPLC = Ins.NPLC,
        Meter_Roll = Ins.Meter_Roll
      } ).ToList ( );

      var Form = new Multi_Instrument_Poll_Form ( _Comm, Cloned, _Settings, _Selected_Meter );
      Cursor = Cursors.Default;
      Form.Show ( );
    }

    private void Multi_Poll_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Cursor = Cursors.WaitCursor;

      if ( _Instruments.Count == 0 )
      {
        MessageBox.Show (
            "No instruments configured. Please scan for instruments first.",
            "No Instruments",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning );
        Cursor = Cursors.Default;
        return;
      }

      // ── Commit any pending NPLC edit before cloning ───────────────────
      Commit_Current_Instrument_Edits ( );

      var Cloned = _Instruments.Select ( Ins => new Instrument
      {
        Name = Ins.Name,
        Address = Ins.Address,
        Type = Ins.Type,
        Visible = Ins.Visible,
        NPLC = Ins.NPLC,
        Meter_Roll = Ins.Meter_Roll
      } ).ToList ( );

      // ── Verify NPLC values before opening ─────────────────────────────
      foreach ( var I in Cloned )
        Capture_Trace.Write ( $"Clone: {I.Name} NPLC={I.NPLC}" );

      var Form = new Multi_Instrument_Poll_Form ( _Comm, Cloned, _Settings, _Selected_Meter );
      Cursor = Cursors.Default;
      Form.Show ( );
    }

    private void Commit_Current_Instrument_Edits ( )
    {
      if ( _Selected_Index < 0 || _Selected_Index >= _Instruments.Count )
        return;

      if ( !decimal.TryParse (
              NPLC_Combo_Box.SelectedItem?.ToString ( ),
              NumberStyles.Number,
              CultureInfo.InvariantCulture,
              out decimal NPLC ) )
        return;

      _Instruments [ _Selected_Index ].NPLC = NPLC;
      Capture_Trace.Write ( $"Committed NPLC {NPLC} for {_Instruments [ _Selected_Index ].Name}" );
    }


    // ===== Instrument List =====


    private async void Add_Instrument_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Cursor = Cursors.WaitCursor;
      _Updating_Controls = true;
      Capture_Trace.Write ( "Adding Instrument" );

      int Address = (int) GPIB_Address_Numeric.Value;
      Meter_Type Type = Meter_Type_Extensions.From_Combo_Index ( Instrument_Type_Combo.SelectedIndex );
      string Name = Instrument_Name_Text.Text.Trim ( );

      if ( string.IsNullOrEmpty ( Name ) )
        Name = Meter_Type_Extensions.Get_Name ( Type );

      // ── Duplicate address check ───────────────────────────────────────
      if ( _Instruments.Any ( i => i.Address == Address ) )
      {
        _Updating_Controls = false;
        MessageBox.Show (
            $"An instrument at GPIB address {Address} is already in the list.",
            "Duplicate Address",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning );
        Cursor = Cursors.Default;
        return;
      }

      if ( _Comm.Is_Connected )
      {
        try
        {
          if ( _Comm.Is_Address_Verified ( Address ) )
          {
            Capture_Trace.Write ( $"Address {Address} ({Type}) already verified - skipping." );
            Append_Response ( $"[Address {Address} already verified - skipping check]" );
          }
          else
          {
            Capture_Trace.Write ( $"[Verifying GPIB address {Address}...]" );
            Append_Response ( $"[Verifying GPIB address {Address}...]" );

            string ID_Response = await Task.Run ( ( ) =>
                _Comm.Verify_GPIB_Address ( Address, Is_Legacy_HP ( Type ), Restore_Address: false ) );

            if ( string.IsNullOrEmpty ( ID_Response ) )
            {
              Append_Response ( $"[No instrument at address {Address} - not added]" );
              Cursor = Cursors.Default;
              return;
            }

            _Comm.Mark_Address_Verified ( Address );
            Append_Response ( $"[Verified at address {Address}: {ID_Response}]" );

            Meter_Type Detected = Multimeter_Common_Helpers_Class.Get_Meter_Type ( ID_Response );

            // ── Type mismatch check ───────────────────────────────────
            if ( Detected != Meter_Type.Generic_GPIB && Detected != Type )
            {
              string Detected_Name = Meter_Type_Extensions.Get_Name ( Detected );
              string Selected_Name = Meter_Type_Extensions.Get_Name ( Type );
              DialogResult Result = DialogResult.No;

              this.Invoke ( ( ) =>
              {
                Result = MessageBox.Show (
                    $"The instrument at address {Address} identified as:\n\n" +
                    $"  Detected:  {Detected_Name}\n" +
                    $"  Selected:  {Selected_Name}\n\n" +
                    $"Do you want to add it as {Detected_Name} instead?",
                    "Instrument Mismatch",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning );
              } );

              if ( Result == DialogResult.Cancel )
              {
                Cursor = Cursors.Default;
                return;
              }

              if ( Result == DialogResult.Yes )
              {
                Type = Detected;
                this.Invoke ( ( ) => Load_NPLC_Combo ( Type ) );
                Append_Response ( $"[Instrument type changed from {Selected_Name} to {Detected_Name}]" );
              }
              else
              {
                Append_Response ( $"[Warning: adding as {Selected_Name} but instrument identified as {Detected_Name}]" );
              }
            }

            // ── Update name to match final type if still at default ───
            if ( Name == Meter_Type_Extensions.Get_Name (
                Meter_Type_Extensions.From_Combo_Index ( Instrument_Type_Combo.SelectedIndex ) ) )
              Name = Meter_Type_Extensions.Get_Name ( Type );
          }
        }
        finally
        {
          _Updating_Controls = false;
        }
      }
      else
      {
        _Updating_Controls = false;
      }

      // ── Create and add instrument ─────────────────────────────────────
      decimal NPLC = decimal.TryParse ( NPLC_Combo_Box.SelectedItem?.ToString ( ), out decimal parsed )
          ? parsed : 1m;

      decimal Default_NPLC = Meter_Type_Extensions.Get_Default_NPLC ( Type );
      if ( NPLC != Default_NPLC )
      {
        Capture_Trace.Write ( $"NPLC overridden by user: {Default_NPLC} → {NPLC}" );
        Append_Response ( $"[Note: NPLC set to {NPLC} (default is {Default_NPLC})]" );
      }

      var Inst = new Instrument
      {
        Name = Name,
        Address = Address,
        Type = Type,
        Verified = true,
        Visible = true,
        NPLC = NPLC,
        Meter_Roll = Roll_Name_Textbox.Text.Trim ( )
      };

      _Instruments.Add ( Inst );

      try
      {
        await Initialize_Remote_For_Instrument ( Inst );
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Failed to initialize instrument at address {Address}: {Ex.Message}" );
        Append_Response ( $"[Warning: initialization failed for address {Address}: {Ex.Message}]" );
      }
      finally
      {
        Roll_Name_Textbox.Enabled = false;
        Refresh_Instrument_List ( );
        Set_Button_State ( );
        Populate_Command_List ( );
        Cursor = Cursors.Default;
      }
    }





    private void Refresh_Instrument_List ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      Instruments_List.DataSource = null;
      Instruments_List.DataSource = _Instruments
          .Where ( i => i.Visible )
          .ToList ( );
      Instruments_List.DisplayMember = "Display";
    }


    public void Set_GPID_Controls ( bool State )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );


      Select_Instrument_Button.Enabled = State;
      GPIB_Address_Label.Enabled = State;
      GPIB_Address_Numeric.Enabled = State;

      Remove_Instrument_Button.Enabled = State;
      Scan_Bus_Button.Enabled = State;
      Saved_Instruments_Label.Enabled = State;
      Instruments_List.Enabled = State;


    }
    private void IP_Address_Text_TextChanged ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Settings.Default_IP_Address = IP_Address_Textbox.Text.Trim ( );
      _Settings.Save ( );
    }

    private void Subnet_Textbox_TextChanged ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Settings.Network_Scan_Subnet = Subnet_Textbox.Text.Trim ( ).TrimEnd ( '.' );
      _Settings.Save ( );
    }


    private void Instrument_Type_Combo_SelectedIndexChanged ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Updating_Controls )
        return;

      _Selected_Meter = Meter_Type_Extensions.From_Combo_Index ( Instrument_Type_Combo.SelectedIndex );
      _Comm.Connected_Meter = _Selected_Meter;
      _All_Commands = Command_Dictionary_Class.Get_All_Commands ( _Selected_Meter );

      Command_List_Label.Text = $"{_Selected_Meter.Get_Name ( )} Commands";
      Instrument_Name_Text.Text = _Selected_Meter.Get_Name ( );  // ← updates on every selection
      Detail_Text_Box.Text = "";
      Send_Command_Text_Box.Text = "";

      Load_NPLC_Combo ( _Selected_Meter );
    }

    private async Task Initialize_Remote_For_Instrument ( Instrument inst )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Comm.Is_Connected )
        return;

      // --- Prologix interface setup ---
      if ( _Comm.Mode == Connection_Mode.Prologix_GPIB ||
          _Comm.Mode == Connection_Mode.Prologix_Ethernet )
      {
        _Comm.Flush_Buffers ( );

        string ver = await Task.Run ( ( ) => _Comm.Query_Prologix_Version ( ) );
        Append_Response ( $"> Prologix version: {ver}" );
        Capture_Trace.Write ( $"> Prologix version: {ver}" );
      }

      Capture_Trace.Write ( $"Setting {inst.Type} at address {inst.Address} to remote mode..." );

      switch ( inst.Type )
      {
        case Meter_Type.HP33120:
        case Meter_Type.HP34401:
        case Meter_Type.HP34420:
        case Meter_Type.HP53132:
          // Switch to correct address first
          await _Comm.Send_Prologix_CommandAsync ( $"++addr {inst.Address}" );
          await Task.Delay ( 50 );
          await _Comm.Send_Prologix_CommandAsync ( "++auto 0" );
          await Task.Delay ( 50 );

          // Suppress beep before going remote
          Append_Response ( $"> Turning {inst.Name} Beep Off" );
          await _Comm.Send_Instrument_CommandAsync ( inst.Address, "SYSTEM:BEEPER:STATE OFF" );
          await Task.Delay ( 100 );

          Capture_Trace.Write ( "Clearing..." );
          Append_Response ( $"> Clearing {inst.Name}" );
          await _Comm.Send_Instrument_CommandAsync ( inst.Address, "*CLS" );
          await Task.Delay ( 200 );   // ← give it time to clear
          Append_Response ( "> *CLS" );

          Append_Response ( $"> Setting {inst.Name} to remote mode" );
          Capture_Trace.Write ( "Setting HP34401 to remote mode..." );
          await _Comm.Send_Instrument_CommandAsync ( inst.Address, "SYSTEM:REMOTE" );
          await Task.Delay ( 200 );
          Append_Response ( "> SYSTEM:REMOTE" );

          // turn on beeps
          Append_Response ( $"> Turning {inst.Name} Beep on" );
          await _Comm.Send_Instrument_CommandAsync ( inst.Address, "SYSTEM:BEEPER:STATE ON" );
          await Task.Delay ( 100 );

          Append_Response ( $"> Clearing {inst.Name}" );
          Capture_Trace.Write ( "Clearing..." );
          await _Comm.Send_Instrument_CommandAsync ( inst.Address, "*CLS" );
          await Task.Delay ( 200 );   // ← give it time to clear
          Append_Response ( "> *CLS" );


          break;

        case Meter_Type.HP3458:
          if ( _Settings.Send_Reset_On_Connect_3458 )
          {
            Capture_Trace.Write ( "Sending Reset..." );
            await _Comm.Send_Instrument_CommandAsync ( inst.Address, "RESET" );
            await Task.Delay ( 3000 );

            Capture_Trace.Write ( "Sending ++addr..." );
            await _Comm.Send_Prologix_CommandAsync ( $"++addr {inst.Address}" );
            await Task.Delay ( 50 );
          }

          if ( _Settings.Send_End_Always_3458 )
          {
            // Do not beep during reset
            Capture_Trace.Write ( "Sending Beep off..." );
            await _Comm.Send_Instrument_CommandAsync ( inst.Address, "BEEP 0" );
            await Task.Delay ( 200 );

            Capture_Trace.Write ( "Sending End Always..." );
            await _Comm.Send_Instrument_CommandAsync ( inst.Address, "END ALWAYS" );
            await Task.Delay ( 200 );

            //     Capture_Trace.Write ( "Sending Beep on..." );
            // Beep for errors
            //    await _Comm.Send_Instrument_CommandAsync ( inst.Address, "BEEP 1" );
            //    await Task.Delay ( 200 );
          }

          Capture_Trace.Write ( "Sending TRIG AUTO..." );
          await _Comm.Send_Instrument_CommandAsync ( inst.Address, "TRIG AUTO" );
          await Task.Delay ( 200 );

          Capture_Trace.Write ( "Sending GPIB..." );
          await _Comm.Send_Instrument_CommandAsync ( inst.Address, $"GPIB {inst.Address}" );
          await Task.Delay ( 100 );

          Capture_Trace.Write ( "Sending +addr..." );
          await _Comm.Send_Prologix_CommandAsync ( $"++addr {inst.Address}" );
          await Task.Delay ( 50 );

          Capture_Trace.Write ( "Sending NPLC..." );
          await _Comm.Send_Instrument_CommandAsync ( inst.Address, $"NPLC {_Settings.Default_NPLC_3458}" );
          await Task.Delay ( _Settings.NPLC_Apply_Delay_Ms );

          Capture_Trace.Write ( "Connected to HP3458..." );
          Append_Response ( $"> Connected to HP3458 at address {inst.Address}" );
          break;

        default:
          Capture_Trace.Write ( $"No specific init for meter type: {inst.Type}" );
          Append_Response ( $"> Connected to {inst.Type} at address {inst.Address}" );
          break;
      }

      // Update UI for this instrument
      Connected_Instrument_Textbox.Text = inst.Name;
    }


    private static bool Is_Legacy_HP ( Meter_Type Type ) => Type switch
    {
      Meter_Type.HP34401 => false,
      Meter_Type.HP33120 => false,
      Meter_Type.HP34420 => false,
      Meter_Type.HP53132 => false,
      Meter_Type.HP3458 => true,
      _ => false
    };




    private void Remove_Instrument_Button_Click (
      object Sender, EventArgs E )
    {
      int Index = Instruments_List.SelectedIndex;
      if ( Index < 0 )
      {
        return;
      }

      _Instruments.RemoveAt ( Index );
      Instruments_List.DataSource = null;
      Instruments_List.DataSource = _Instruments;
    }

    private void Select_Instrument_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      int Index = Instruments_List.SelectedIndex;
      if ( Index < 0 || Index >= _Instruments.Count )
        return;

      _Selected_Index = Index;
      var Instrument = _Instruments [ Index ];
      _Selected_Meter = Instrument.Type;
      _Selected_Address = Instrument.Address;

      _Comm.Change_GPIB_Address ( Instrument.Address );

      _Updating_Controls = true;
      Instrument_Type_Combo.SelectedIndex = _Selected_Meter.To_Combo_Index ( );
      Load_NPLC_Combo ( _Selected_Meter, Instrument.NPLC );  // restore this instrument's NPLC
      _Updating_Controls = false;

      Populate_Command_List ( );
    }




    private async void Scan_Bus_Button_Click ( object sender, EventArgs e )
    {
      // If already scanning, cancel and let the finally below clean up
      if ( _Is_Scanning )
      {
        _Scan_Cts?.Cancel ( );
        Scan_Bus_Button.Text = "Canceling...";
        Scan_Bus_Button.Enabled = false;
        Append_Response ( "[Canceling bus scan...]" );
        return;
      }

      if ( !_Comm.Is_Connected )
        return;

      _Is_Scanning = true;
      _Scan_Cts = new CancellationTokenSource ( );
      Scan_Bus_Button.Text = "Stop Scan";
      Append_Response ( "[Scanning GPIB bus...]" );

      try
      {
        await Run_Bus_Scan ( _Scan_Cts.Token );
      }
      catch ( OperationCanceledException )
      {
        Append_Response ( "[Bus scan canceled]" );
      }
      catch ( Exception ex )
      {
        Append_Response ( $"[Error during bus scan: {ex.Message}]" );
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

    private async Task Run_Bus_Scan ( CancellationToken Token )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      var Progress = new Progress<string> ( msg => Append_Response ( msg ) );

      List<Scan_Result> Results = await _Comm.Scan_GPIB_BusAsync ( Progress, Token );

      foreach ( var result in Results )
      {


        Meter_Type type = result.Detected_Type ?? Meter_Type.HP3458;
        string name = result.ID_String.Length > 40 ? result.ID_String.Substring ( 0, 40 ) : result.ID_String;

        var existing = _Instruments.FirstOrDefault ( i => i.Address == result.Address );

        if ( existing != null )
        {
          existing.Verified = true;
          existing.Visible = true;
          existing.Name = name;
          Append_Response ( $"[Instrument at GPIB {result.Address} refreshed]" );
        }
        else
        {
          _Instruments.Add ( new Instrument
          {
            Name = name,
            Address = result.Address,
            Type = type,
            Verified = true,
            Visible = true
          } );
          Append_Response ( $"[Detected new instrument at GPIB {result.Address}]" );
        }
      }

      Refresh_Instrument_List ( );

      if ( Results.Count == 0 )
        Append_Response ( "No instruments found on the GPIB bus." );



    }



    private void Update_Command_Mode ( Command_Entry Entry )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Entry == null )
      {
        Execute_Button.Enabled = false;
        //   Query_Button.Enabled = false;
        return;
      }

      Execute_Button.Enabled = Entry.Has_Command;
      //    Query_Button.Enabled = Entry.Has_Query;
    }






    private void Execute_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      string Input = Send_Command_Text_Box.Text.Trim ( );
      if ( string.IsNullOrWhiteSpace ( Input ) )
        return;

      Capture_Trace.Write ( $"Command -> {Input}" );

      // Parse command and optional parameters
      string [ ] Parts = Input.Split ( ' ', 2 );
      string Base_Token = Parts [ 0 ].TrimEnd ( '?' );
      bool Is_Query = Parts [ 0 ].EndsWith ( '?' );
      string Parameters = Parts.Length > 1 ? Parts [ 1 ].Trim ( ) : "";

      // Rebuild final command preserving query suffix and parameters
      string Final_Command = Base_Token
          + ( Is_Query ? "?" : "" )
          + ( string.IsNullOrEmpty ( Parameters ) ? "" : " " + Parameters );

      Execute_Command ( Final_Command );
    }





    private void Execute_Command ( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Comm.Is_Connected )
        return;

      try
      {
        Capture_Trace.Write ( $"Command -> {Command}" );
        Add_Command_To_History ( Command );

        if ( Command.StartsWith ( "++" ) )
        {
          _Comm.Send_Prologix_Command ( Command );
        }
        else if ( Command.EndsWith ( "?" ) )
        {
          // Query - send and read response
          string Response = _Comm.Query_Instrument ( Command );
          if ( !string.IsNullOrWhiteSpace ( Response ) )
            Append_Response ( $"< {Response}" );
          else
            Append_Response ( "< (no response)" );
        }
        else
        {
          _Comm.Send_Instrument_Command ( Command );
        }
      }
      catch ( Exception Ex )
      {
        Add_Command_To_History ( $"ERROR: {Ex.Message}" );
      }
    }

    private void Execute_Query ( string Command )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !_Comm.Is_Connected )
        return;

      try
      {
        Capture_Trace.Write ( "Query -> " + Command );

        Add_Command_To_History ( $"{Command}" );

        string Response = _Comm.Query_Instrument ( Command );
        Append_Response ( $"< {Response}" );

      }
      catch ( Exception Ex )
      {
        Add_Command_To_History ( $"ERROR: {Ex.Message}" );
      }
    }




    private void Empty_Windows ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      _Instruments.Clear ( );
      Refresh_Instrument_List ( );   // ← rebind after clear
      Command_List.Items.Clear ( );
      Send_Command_Text_Box.Clear ( );
      Command_History_List_Box.Items.Clear ( );
      Connected_Instrument_Textbox.Clear ( );
      Detail_Text_Box.Clear ( );
      Response_Text_Box.Clear ( );
    }

    private void Clear_Command_And_Details ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );


      // Clear selection safely
      _Ignore_Selection_Changed = true;

      Command_List.ClearSelected ( );
      //  Detail_Text_Box.Clear ( );
      Send_Command_Text_Box.Clear ( );

      _Ignore_Selection_Changed = false;

    }
    private async void Diag_Button_Click (
      object Sender, EventArgs E )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );
      string Command = Send_Command_Text_Box.Text.Trim ( );

      /*
      if ( string.IsNullOrEmpty ( Command ) )
      {
        Command = "++ver";
      }
      */

      if ( !_Comm.Is_Connected )
      {
        Append_Response ( "[Not connected]" );
        return;
      }

      Diag_Button.Enabled = false;
      Append_Response ( "" );
      Append_Response ( "" );
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
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Response_Text_Box.Text.Length > 0 )
      {
        Response_Text_Box.AppendText ( "\r\n" );
      }
      Response_Text_Box.AppendText ( Text );
    }

    // ===== Connection Controls =====

    private void Populate_Connection_Controls ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      // Connection mode
      Connection_Mode_Combo.Items.Clear ( );
      Connection_Mode_Combo.Items.Add ( "Prologix GPIB" );
      Connection_Mode_Combo.Items.Add ( "Direct Serial (RS-232)" );
      Connection_Mode_Combo.Items.Add ( "Ethernet" );
      Connection_Mode_Combo.SelectedIndex = 1;

      Set_GPID_Controls ( State: false );

      // COM ports
      Refresh_Ports ( );

      // Baud rates
      Baud_Rate_Combo.Items.Clear ( );
      foreach ( int Rate in
        Instrument_Comm.Get_Available_Baud_Rates ( ) )
      {
        Baud_Rate_Combo.Items.Add ( Rate );
      }
      Baud_Rate_Combo.SelectedItem = 115200;

      // Data bits
      Data_Bits_Combo.Items.Clear ( );
      foreach ( int Bits in
        Instrument_Comm.Get_Available_Data_Bits ( ) )
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


      Read_Timeout_Combo_Box.Items.Clear ( );
      foreach ( int Timeout in
        Instrument_Comm.Get_Available_Read_Timeouts ( ) )
      {
        Read_Timeout_Combo_Box.Items.Add ( Timeout );
      }
      Read_Timeout_Combo_Box.SelectedItem = 3000;



      // GPIB address (0-30) - stays editable while
      // connected so the user can switch instruments
      //   GPIB_Address_Numeric.Minimum = 0;
      //  GPIB_Address_Numeric.Maximum = 30;
      //   GPIB_Address_Numeric.Value = 22;
      //   GPIB_Address_Numeric.ValueChanged +=
      //     GPIB_Address_Numeric_ValueChanged;

      IP_Address_Textbox.Text = _Settings.Default_IP_Address;
      //   IP_Port_Numeric.Minimum = 1;
      //    IP_Port_Numeric.Maximum = 65535;
      ///   IP_Port_Numeric.Value = _Settings.Default_Prologic_Port;


      // EOI default to checked, auto-read off to avoid
      // errors on non-query commands
      //  Auto_Read_Check.Checked = false;
      //  EOI_Check.Checked = true;

      Update_Connection_Status ( false );
    }




    private void Refresh_Ports ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      string? Previous_Selection =
        COM_Port_Combo.SelectedItem?.ToString ( );

      COM_Port_Combo.Items.Clear ( );
      string [ ] Ports =
        Instrument_Comm.Get_Available_Ports ( );

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
      using var Block = Trace_Block.Start_If_Enabled ( );

      Refresh_Ports ( );
    }

    private void Defaults_Button_Click (
      object Sender, EventArgs E )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Connection_Mode_Combo.SelectedIndex == 1 )
      {
        Capture_Trace.Write ( "Setting defaults for Direct Serial connection" );

        // Defaults for Direct Serial (RS-232)
        Baud_Rate_Combo.SelectedItem = 9600;
        Data_Bits_Combo.SelectedItem = 8;
        Parity_Combo.SelectedItem = Parity.None;
        Stop_Bits_Combo.SelectedItem = StopBits.One;
        Flow_Control_Combo.SelectedItem = Handshake.None;
        Read_Timeout_Combo_Box.SelectedItem = 3000;

        Connected_Instrument_Textbox.Text = "";
        _Selected_Address = 0;
        Set_GPID_Controls ( State: false );
      }
      else if ( Connection_Mode_Combo.SelectedIndex == 2 )
      {
        Capture_Trace.Write ( "Setting defaults for Ethernet connection" );
        IP_Address_Textbox.Text = "192.168.1.100";  // matches Ethernet_Host default in your class
                                                    //  IP_Port_Numeric.Value = 1234;             // matches Ethernet_Port default in your class
                                                    // GPIB_Address_Numeric.Value = 22;
        _Selected_Address = 22;
        Set_GPID_Controls ( State: true );
      }
      else
      {
        Capture_Trace.Write ( "Setting defaults for Prologix GPIB-USB adapter" );
        // Defaults for Prologix GPIB-USB-HS adapter
        Baud_Rate_Combo.SelectedItem = 9600;
        Data_Bits_Combo.SelectedItem = 8;
        Parity_Combo.SelectedItem = Parity.None;
        Stop_Bits_Combo.SelectedItem = StopBits.Two;
        Flow_Control_Combo.SelectedItem = Handshake.RequestToSend;
        Read_Timeout_Combo_Box.SelectedItem = 3000;

        //    GPIB_Address_Numeric.Value = 22;
        GPIB_Address_Numeric.Value = 22;
        _Selected_Address = (int) GPIB_Address_Numeric.Value;

        Set_GPID_Controls ( State: true );
      }
    }



    private async void Connect_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( _Comm.Is_Connected )
      {
        Connect_Button.Enabled = false;
        try
        {
          Capture_Trace.Write ( "Disconnecting..." );
          await _Comm.Disconnect_Async ( );   // does everything, off UI thread
          Capture_Trace.Write ( "Clearing Windows" );
          Empty_Windows ( );
          Set_Button_State ( );
        }
        finally
        {
          Connect_Button.Enabled = true;
        }
        return;
      }


      // --- Setup transport only ---
      Is_Ethernet = Connection_Mode_Combo.SelectedIndex == 2;
      Capture_Trace.Write ( $"Selected connection mode: {( Is_Ethernet ? "Ethernet" : Connection_Mode_Combo.SelectedItem )}" );
      if ( Is_Ethernet )
      {
        _Comm.Mode = Connection_Mode.Prologix_Ethernet;
        _Comm.Ethernet_Host = IP_Address_Textbox.Text.Trim ( );
        _Comm.Ethernet_Port = (int) _Settings.Default_Prologic_Port;

        Capture_Trace.Write ( $"Comm Mode -> {_Comm.Mode}" );
        Capture_Trace.Write ( $"Host      -> {_Comm.Ethernet_Host}" );
        Capture_Trace.Write ( $"Port      -> {_Comm.Ethernet_Port}" );
      }
      else
      {
        if ( COM_Port_Combo.SelectedItem == null )
        {
          MessageBox.Show (
              "Please select a COM port.",
              "Connection Error",
              MessageBoxButtons.OK,
              MessageBoxIcon.Warning );
          return;
        }

        _Comm.Mode = Connection_Mode_Combo.SelectedIndex == 0
            ? Connection_Mode.Prologix_GPIB
            : Connection_Mode.Direct_Serial;

        _Comm.Port_Name = COM_Port_Combo.SelectedItem.ToString ( )!;
        _Comm.Baud_Rate = (int) Baud_Rate_Combo.SelectedItem!;
        _Comm.Data_Bits = (int) Data_Bits_Combo.SelectedItem!;
        _Comm.Parity = (Parity) Parity_Combo.SelectedItem!;
        _Comm.Stop_Bits = (StopBits) Stop_Bits_Combo.SelectedItem!;
        _Comm.Flow_Control = (Handshake) Flow_Control_Combo.SelectedItem!;
        _Comm.Read_Timeout_Ms = (int) Read_Timeout_Combo_Box.SelectedItem!;


        Capture_Trace.Write ( $"Comm Mode       -> {_Comm.Mode}" );
        Capture_Trace.Write ( $"Port            -> {_Comm.Port_Name}" );
        Capture_Trace.Write ( $"Baud            -> {_Comm.Baud_Rate}" );
        Capture_Trace.Write ( $"Data Bits       -> {_Comm.Data_Bits}" );
        Capture_Trace.Write ( $"Parity          -> {_Comm.Parity.ToString ( )}" );
        Capture_Trace.Write ( $"Stop Bits       -> {_Comm.Stop_Bits}" );
        Capture_Trace.Write ( $"Flow Control    -> {_Comm.Flow_Control.ToString ( )}" );
        Capture_Trace.Write ( $"Read Timeout MS -> {_Comm.Read_Timeout_Ms}" );


      }

      // --- Connect ---
      Connect_Button.Enabled = false;
      try
      {
        await Task.Run ( ( ) => _Comm.Connect ( ) );
        Capture_Trace.Write ( "Connection established." );

        if ( _Comm.Mode == Connection_Mode.Prologix_GPIB ||
            _Comm.Mode == Connection_Mode.Prologix_Ethernet )
        {
          _Comm.Configure_Prologix_Transport_Only ( );
        }
      }
      catch ( Exception ex )
      {
        Capture_Trace.Write ( $"Connection failed: {ex.Message}" );
        // optionally: MessageBox.Show(...) or status label update
      }
      finally
      {
        Connect_Button.Enabled = true;
        Set_Button_State ( );
      }
    }



    private void Update_Connection_Status ( bool Connected )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Connected )
      {
        Connection_Status_Label.Text = "Connected";
        Connection_Status_Label.ForeColor = Color.Green;
        Connect_Button.Text = "Disconnect";

        // Disable settings while connected
        Connection_Mode_Combo.Enabled = false;
        COM_Port_Combo.Enabled = false;
        Baud_Rate_Combo.Enabled = false;
        Data_Bits_Combo.Enabled = false;
        Parity_Combo.Enabled = false;
        Stop_Bits_Combo.Enabled = false;
        Flow_Control_Combo.Enabled = false;
        Refresh_Ports_Button.Enabled = false;
        Defaults_Button.Enabled = false;


        //   Initialize_Remote_Connection ( );


      }
      else
      {
        Connection_Status_Label.Text = "Disconnected";
        Connection_Status_Label.ForeColor = Color.Red;
        Connect_Button.Text = "Connect";

        // Enable settings while disconnected
        Connection_Mode_Combo.Enabled = true;
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
    private void Connection_Mode_Combo_SelectedIndexChanged ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Is_GPIB = Connection_Mode_Combo.SelectedIndex == 0;
      Is_Serial = Connection_Mode_Combo.SelectedIndex == 1;
      Is_Ethernet = Connection_Mode_Combo.SelectedIndex == 2;

      // Serial controls only relevant for serial/GPIB-USB
      // COM port needed for both GPIB-USB and Direct Serial
      COM_Port_Combo.Enabled = Is_GPIB || Is_Serial;
      COM_Port_Label.Enabled = Is_GPIB || Is_Serial;
      Scan_Bus_Button.Enabled = Is_GPIB || Is_Ethernet;

      // RS-232 settings only relevant for Direct Serial
      // (Prologix GPIB-USB uses fixed settings, Ethernet has none)
      Baud_Rate_Combo.Enabled = Is_GPIB || Is_Serial;
      Baud_Rate_Label.Enabled = Is_GPIB || Is_Serial;
      Data_Bits_Combo.Enabled = Is_GPIB || Is_Serial;
      Data_Bits_Label.Enabled = Is_GPIB || Is_Serial;
      Parity_Combo.Enabled = Is_GPIB || Is_Serial;
      Parity_Label.Enabled = Is_GPIB || Is_Serial;
      Stop_Bits_Combo.Enabled = Is_GPIB || Is_Serial;
      Stop_Bits_Label.Enabled = Is_GPIB || Is_Serial;
      Flow_Control_Combo.Enabled = Is_GPIB || Is_Serial;
      Flow_Control_Label.Enabled = Is_GPIB || Is_Serial;
      Read_Timeout_Combo_Box.Enabled = Is_GPIB || Is_Serial;
      Read_Timeout_Label.Enabled = Is_GPIB || Is_Serial;

      // Ethernet controls
      Find_Prologix_Button.Enabled = Is_Ethernet;
      IP_Address_Textbox.Enabled = Is_Ethernet;
      IP_Address_Label.Enabled = Is_Ethernet;
      Subnet_Textbox.Enabled = Is_Ethernet;

      Set_GPID_Controls ( State: Is_GPIB || Is_Ethernet );

      Set_Button_State ( );


    }


    private void Set_Button_State ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Elements that are active if instruments are present

      Multi_Poll_Button.Enabled = _Instruments.Count > 0;
      Meter_Info_Button.Enabled = true;

      Open_Dictionary_Button.Enabled = _Instruments.Count > 0;
      Diag_Button.Enabled = _Instruments.Count > 0;
      Execute_Button.Enabled = _Instruments.Count > 0;
      Remove_Instrument_Button.Enabled = _Instruments.Count > 0;
      NPLC_Combo_Box.Enabled = _Instruments.Count > 0;
      Instrument_Name_Label.Enabled = _Instruments.Count > 0;
      Instrument_Name_Text.Enabled = _Instruments.Count > 0;
      Command_List.Enabled = _Instruments.Count > 0;
      Command_History_List_Box.Enabled = _Instruments.Count > 0;
      Detail_Text_Box.Enabled = _Instruments.Count > 0;
      Send_Command_Text_Box.Enabled = _Instruments.Count > 0;
      History_Label.Enabled = _Instruments.Count > 0;
      Send_Command_Label.Enabled = _Instruments.Count > 0;
      Response_Label.Enabled = _Instruments.Count > 0;
      NPLC_Label.Enabled = _Instruments.Count > 0;
      Apply_NPLC_To_All_Button.Enabled =
      NPLC_Combo_Box.SelectedIndex >= 0 && _Instruments.Count > 1;

      // Elements that are active if connection has been made
      Instrument_Type_Combo.Enabled = _Comm.Is_Connected;
      Instrument_Type_Label.Enabled = _Comm.Is_Connected;
      GPIB_Address_Numeric.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet );
      GPIB_Address_Label.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet );
      Add_Instrument_Button.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet );
      NPLC_Combo_Box.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet || Is_Serial );
      Roll_Name_Textbox.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet || Is_Serial );
      Meter_Roll_Label.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet || Is_Serial );


      Select_Instrument_Button.Enabled = _Instruments.Count > 0;


      Scan_Bus_Button.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet );
    }


    private void Comm_Connection_Changed (
     object? Sender, bool Connected )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

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

      using var Block = Trace_Block.Start_If_Enabled ( );

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

      using var Block = Trace_Block.Start_If_Enabled ( );
      // Future use: log or display received data
    }

    private void GPIB_Address_Numeric_ValueChanged (
       object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Updating_Controls = true;

      // Always keep UI controls in sync
      //    Instrument_Address_Numeric.Value = GPIB_Address_Numeric.Value;

      // Only update hardware if connected
      if ( _Comm.Is_Connected )
      {
        _Comm.Change_GPIB_Address ( (int) GPIB_Address_Numeric.Value );
      }

      _Updating_Controls = false;
    }


    private void Show_Error ( string Message )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      MessageBox.Show (
        Message,
        "Communication Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error );
    }


    private void Add_Command_To_History ( string Command )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( string.IsNullOrWhiteSpace ( Command ) )
        return; // Don't add empty commands

      // Check for duplicates: remove existing one if present
      //    int Existing_Index = Command_History_List_Box.Items.IndexOf ( Command );

      //    if ( Existing_Index >= 0 )
      //    {
      //      Command_History_List_Box.Items.RemoveAt ( Existing_Index );
      //    }

      // Add to bottom
      Command_History_List_Box.Items.Add ( Command );

      // Trim history if it exceeds max size
      while ( Command_History_List_Box.Items.Count > _Max_History_Size )
      {
        // Remove oldest (top item)
        Command_History_List_Box.Items.RemoveAt ( 0 );
      }

      // Scroll to newest command
      int Last_Index = Command_History_List_Box.Items.Count - 1;
      Command_History_List_Box.SelectedIndex = Last_Index;
      Command_History_List_Box.TopIndex = Last_Index;
    }


    private void Update_Button_State ( Command_Entry Entry )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Entry == null )
      {
        Execute_Button.Enabled = false;
        //   Query_Button.Enabled = false;
        return;
      }

      CommandMode Mode = Entry.Get_Command_Mode ( );

      switch ( Mode )
      {
        case CommandMode.Both:
          Execute_Button.Enabled = true;
          //   Query_Button.Enabled = true;
          break;

        case CommandMode.Query_Only:
          Execute_Button.Enabled = true;
          //   Query_Button.Enabled = true;
          break;

        case CommandMode.Set_Only:
          Execute_Button.Enabled = true;
          //   Query_Button.Enabled = false;
          break;

        case CommandMode.None:
        default:
          Execute_Button.Enabled = false;
          //  Query_Button.Enabled = false;
          break;
      }
    }








    private void Command_History_ListBox_DoubleClick ( object Sender, EventArgs E )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Command_History_List_Box.SelectedItem != null )
      {
        string? History_Text = Command_History_List_Box.SelectedItem.ToString ( );
        if ( string.IsNullOrWhiteSpace ( History_Text ) )
          return;

        Send_Command_Text_Box.Text = History_Text;
        Send_Command_Text_Box.Focus ( );
        Send_Command_Text_Box.SelectionStart = Send_Command_Text_Box.Text.Length;

        // Match history command to a Command_Entry and update buttons
        string Raw_Token = History_Text.Split ( ' ', 2 ) [ 0 ];
        string Trimmed_Token = Raw_Token.TrimEnd ( '?' );

        Command_Entry? Matched = _All_Commands?.FirstOrDefault ( C =>
          string.Equals ( C.Command, Raw_Token,
            StringComparison.OrdinalIgnoreCase )
          || string.Equals ( C.Command, Trimmed_Token,
            StringComparison.OrdinalIgnoreCase ) );

        Update_Button_State ( Matched );
      }
    }







    private string Get_Local_Subnet ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !string.IsNullOrWhiteSpace ( _Settings.Network_Scan_Subnet ) )
      {
        string Subnet = _Settings.Network_Scan_Subnet.TrimEnd ( '.' );
        Capture_Trace.Write ( $"Using user-specified subnet: {Subnet}" );
        return Subnet;
      }

      try
      {
        // Get all network interfaces with their unicast addresses
        foreach ( var NI in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces ( ) )
        {
          // Skip loopback, tunnels, VPN, virtual adapters
          if ( NI.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback )
            continue;
          if ( NI.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up )
            continue;
          if ( NI.Description.Contains ( "Virtual", StringComparison.OrdinalIgnoreCase ) ||
               NI.Description.Contains ( "Hyper-V", StringComparison.OrdinalIgnoreCase ) ||
               NI.Description.Contains ( "VPN", StringComparison.OrdinalIgnoreCase ) ||
               NI.Description.Contains ( "Tunnel", StringComparison.OrdinalIgnoreCase ) )
            continue;

          foreach ( var UA in NI.GetIPProperties ( ).UnicastAddresses )
          {
            if ( UA.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork )
              continue;
            string IP = UA.Address.ToString ( );
            string [ ] Parts = IP.Split ( '.' );
            string Subnet = $"{Parts [ 0 ]}.{Parts [ 1 ]}.{Parts [ 2 ]}";
            Capture_Trace.Write ( $"Selected adapter: {NI.Description} -> {IP} -> subnet {Subnet}" );
            return Subnet;
          }
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Failed to detect local subnet: {Ex.Message}" );
      }

      Capture_Trace.Write ( $"No suitable adapter found, using setting: {_Settings.Network_Scan_Subnet}" );
      return _Settings.Network_Scan_Subnet;
    }
    private async void Find_Prologix_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      string Original_Text = Find_Prologix_Button.Text;
      Find_Prologix_Button.Enabled = false;
      Find_Prologix_Button.Text = "Scanning...";

      string Subnet = Get_Local_Subnet ( );

      await Task.Delay ( 500 );

      Capture_Trace.Write ( $"Subnet detected: {Subnet}" );

      var Found = await Multimeter_Common_Helpers_Class.Scan_For_Prologix (
         Subnet, _Settings.Prologic_Scan_Timeout_MS, null,
         Msg => Capture_Trace.Write ( Msg ) );

      Find_Prologix_Button.Enabled = true;
      Find_Prologix_Button.Text = Original_Text;

      if ( Found.Count == 0 )
      {
        MessageBox.Show ( "No Prologix device found on the network.",
          "Scan Complete", MessageBoxButtons.OK,
          MessageBoxIcon.Information );
        return;
      }
      if ( Found.Count == 1 )
      {
        IP_Address_Textbox.Text = Found [ 0 ];
        _Settings.Default_IP_Address = Found [ 0 ];

        MessageBox.Show ( $"Found Prologix at {Found [ 0 ]}",
          "Scan Complete", MessageBoxButtons.OK,
          MessageBoxIcon.Information );
        _Settings.Save ( );
      }
      else
      {
        string Selected = Show_IP_Selection_Dialog ( Found );
        if ( !string.IsNullOrEmpty ( Selected ) )
          IP_Address_Textbox.Text = Selected;
      }
    }



    private string Show_IP_Selection_Dialog ( List<string> IPs )
    {
      using var Dialog = new Form ( );
      Dialog.Text = "Select Prologix Device";
      Dialog.Size = new Size ( 300, 200 );
      Dialog.StartPosition = FormStartPosition.CenterParent;
      Dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
      Dialog.MaximizeBox = false;
      Dialog.MinimizeBox = false;

      var List_Box = new ListBox ( );
      List_Box.Dock = DockStyle.Fill;
      foreach ( var IP in IPs )
        List_Box.Items.Add ( IP );
      List_Box.SelectedIndex = 0;

      var OK_Button = new Button ( );
      OK_Button.Text = "Select";
      OK_Button.Dock = DockStyle.Bottom;
      OK_Button.DialogResult = DialogResult.OK;

      Dialog.Controls.Add ( List_Box );
      Dialog.Controls.Add ( OK_Button );
      Dialog.AcceptButton = OK_Button;

      return Dialog.ShowDialog ( ) == DialogResult.OK
        ? List_Box.SelectedItem?.ToString ( ) ?? ""
        : "";
    }


    private void Meter_Info_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // var assembly = System.Reflection.Assembly.GetExecutingAssembly ( );
      // var names = assembly.GetManifestResourceNames ( );
      // MessageBox.Show ( string.Join ( "\n", names ) );

      string content;
      var assembly = System.Reflection.Assembly.GetExecutingAssembly ( );
      string resourceName = assembly.GetManifestResourceNames ( )
                        .Single ( s => s.EndsWith ( "Meter_Info.txt" ) );

      using ( var stream = assembly.GetManifestResourceStream ( resourceName ) )
      using ( var reader = new System.IO.StreamReader ( stream ) )
      {
        content = reader.ReadToEnd ( );
      }

      Form popup = new Form ( );
      popup.Text = "Meter Info";
      popup.Size = new Size ( 600, 500 );
      popup.StartPosition = FormStartPosition.CenterParent;
      RichTextBox rtb = new RichTextBox ( );
      rtb.Text = content;
      rtb.ReadOnly = true;
      rtb.Dock = DockStyle.Fill;
      rtb.Font = new Font ( "Courier New", 10 );
      rtb.BackColor = this.BackColor;
      rtb.ForeColor = this.ForeColor;
      popup.Controls.Add ( rtb );
      popup.ShowDialog ( this );
    }

   

    private void Session_Settings_Button_Click ( object Sender, EventArgs E )
    {
      if ( _Session_Settings_Form == null || _Session_Settings_Form.IsDisposed )
      {
        _Session_Settings_Form = new Session_Settings_Form ( _Settings, _Instruments, _Settings.Current_Theme );
        _Session_Settings_Form.FormClosed += ( s, e ) =>
        {
          _Session_Settings_Form = null;
          Session_Settings_Button.Text = "Session Settings";
        };
        _Session_Settings_Form.Show ( this );
        Session_Settings_Button.Text = "Hide Settings";
      }
      else
      {
        _Session_Settings_Form.Close ( );
        _Session_Settings_Form = null;
        Session_Settings_Button.Text = "Session Settings";
      }
    }

    private void Settings_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      using var Dlg = new Settings_Form ( _Settings );

      if ( Dlg.ShowDialog ( this ) == DialogResult.OK )
      {
        _Settings = Dlg.Get_Settings ( );
        _Settings.Save ( );

        // Apply settings immediately to this running instance
        //     Apply_Settings ( );

        MessageBox.Show (
          "Settings saved successfully.\n\n" +
          "Settings have been applied to this window.",
          "Settings",
          MessageBoxButtons.OK,
          MessageBoxIcon.Information );
      }
    }

    private void NPLC_Combo_Box_SelectedIndexChanged ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( _Updating_Controls )
        return;
      if ( _Selected_Index < 0 || _Selected_Index >= _Instruments.Count )
        return;

      if ( !decimal.TryParse (
              NPLC_Combo_Box.SelectedItem?.ToString ( ),
              NumberStyles.Number,
              CultureInfo.InvariantCulture,
              out decimal NPLC ) )
        return;

      var Inst = _Instruments [ _Selected_Index ];
      decimal Default = Meter_Type_Extensions.Get_Default_NPLC ( Inst.Type );
      Inst.NPLC = NPLC;

      if ( NPLC != Default )
      {
        Capture_Trace.Write ( $"NPLC overridden: {Inst.Name} default={Default} selected={NPLC}" );
        Append_Response ( $"[{Inst.Name}: NPLC set to {NPLC} (default is {Default})]" );
      }
      else
      {
        Capture_Trace.Write ( $"NPLC set to default {NPLC} for {Inst.Name}" );
      }

      Update_NPLC_Display ( );   // refresh integration/settle time labels if you have them

      _Settings.Default_NPLC = NPLC.ToString ( CultureInfo.InvariantCulture );
      _Settings.Save ( );
    }

    private void Apply_NPLC_To_All_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( NPLC_Combo_Box.SelectedItem is not string Value ||
    !decimal.TryParse ( Value, NumberStyles.Number,
                        CultureInfo.InvariantCulture, out decimal NPLC ) )
      {
        MessageBox.Show (
            "Please select a valid NPLC value first.",
            "No NPLC Selected",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning );
        return;
      }

      int Count = 0;
      foreach ( var Instrument in _Instruments )
      {
        if ( Instrument.Type.Get_NPLC_Values ( ).Contains ( NPLC ) )
        {
          Instrument.NPLC = NPLC;
          Count++;
        }
        else
        {
          Capture_Trace.Write ( $"[{Instrument.Name}] NPLC {NPLC} not valid for {Instrument.Type.Get_Name ( )} — skipped." );
          Append_Response ( $"[{Instrument.Name}] NPLC {NPLC} not supported — skipped." );
        }
      }

      _Settings.Default_NPLC = Value;
      _Settings.Save ( );

      Capture_Trace.Write ( $"NPLC {NPLC} applied to {Count} of {_Instruments.Count} instruments." );
      Append_Response ( $"[NPLC {NPLC} applied to {Count} of {_Instruments.Count} instruments]" );
    }


    private void Update_NPLC_Display ( )
    {
      if ( _Selected_Index < 0 || _Selected_Index >= _Instruments.Count )
        return;

      // Nothing to display on main form — NPLC data shown on polling form only
      // Just ensure the instrument list reflects the change
      Refresh_Instrument_List ( );
    }


    private void Roll_Name_Textbox_Leave ( object sender, EventArgs e )
    {
      if ( Roll_Name_Textbox is null || !Roll_Name_Textbox.Enabled )
        return;

      string val = Roll_Name_Textbox.Text.Trim ( );
      if ( !string.IsNullOrEmpty ( val ) && ( _Instruments?.Any ( i => i.Meter_Roll == val ) ?? false ) )
      {
        MessageBox.Show (
            $"The role '{val}' is already in use.",
            "Duplicate Name",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning );
        Roll_Name_Textbox.Focus ( );
      }
    }

    private void Display_Recording_Data_Button_Click ( object sender, EventArgs e )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      Cursor = Cursors.WaitCursor;

      var Form = new Recording_Playback_Form ( _Settings );
      Cursor = Cursors.Default;
      Form.Show ( );
    }

    private void NPLC_Info_Button_Click ( object Sender, EventArgs E )
    {
      if ( _NPLC_Info_Form == null || _NPLC_Info_Form.IsDisposed )
      {
        _NPLC_Info_Form = new NPLC_Info_Form ( );
        _NPLC_Info_Form.Show ( this );
      }
      else
      {
        _NPLC_Info_Form.Close ( );
        _NPLC_Info_Form = null;
      }
    }
  }
}
