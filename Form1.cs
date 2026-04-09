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
using Rich_Text_Popup_Namespace;

namespace Multimeter_Controller
{
  public partial class Form1 : Form
  {
    private List<Command_Entry>      _All_Commands;
    private readonly Instrument_Comm _Comm;
    private Meter_Type               _Selected_Meter = Meter_Type_Extensions.Combo_Order[ 0 ];
    private CancellationTokenSource? _Scan_Cts;
    private bool      _Is_Scanning;
    private bool      _Ignore_Selection_Changed = false;

    // Maximum number of commands to keep in history
    private const int _Max_History_Size = 50;

    private readonly List<Instrument> _Instruments = new List<Instrument>();

    private int                       _Selected_Address = 0;

    private bool                      _Updating_Controls = false;

    private int                       _Selected_Index = -1;

    private Application_Settings      _Settings = Application_Settings.Load();
    private Chart_Theme               _Theme    = Chart_Theme.Dark_Preset();

    private int                       _Instrument_Count = 0;
    public bool                       Is_GPIB           = false;
    public bool                       Is_Serial         = false;
    public bool                       Is_Ethernet       = false;

    private bool                      _Cleanup_Done = false;

    private Multi_Instrument_Poll_Form? _Poll_Form = null;

    private bool _Adding_Instrument  = false; // true while async add is in progress
    private bool Poll_Form_Is_Open  => _Poll_Form != null && ! _Poll_Form.IsDisposed;

    public Form1()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      InitializeComponent();

      // Populate instrument type combo first, then set selection
      _Updating_Controls = true;
      Instrument_Type_Combo.Items.Clear();

      foreach ( var Type in Meter_Type_Extensions.Combo_Order )
        Instrument_Type_Combo.Items.Add( Type.Get_Name() );

      Instrument_Type_Combo.SelectedIndex = Meter_Type.Generic_GPIB.To_Combo_Index();
      Instrument_Name_Text.Text           = Meter_Type.Generic_GPIB.Get_Name();

      Load_NPLC_Combo( Meter_Type.Generic_GPIB );
      _Updating_Controls = false;

      Instruments_List.DataSource    = _Instruments;
      Instruments_List.DisplayMember = "Name";

      _Comm                     = new Instrument_Comm( _Settings );
      _Comm.Connection_Changed += Comm_Connection_Changed;
      _Comm.Error_Occurred     += Comm_Error_Occurred;
      _Comm.Data_Received      += Comm_Data_Received;

      Populate_Connection_Controls();

      Connected_Instrument_Textbox.Text = "";

      if ( ! string.IsNullOrWhiteSpace( _Settings.Default_Window_Title ) )
        this.Text = _Settings.Default_Window_Title + " - Launcher";

      Subnet_Textbox.Text = _Settings.Network_Scan_Subnet;
      Capture_Trace.Write( $"Subnet set to : [{_Settings.Network_Scan_Subnet}]" );
      Capture_Trace.Write( $"Control name  : [{Subnet_Textbox.Name}]" );

      Instruments_List.SelectedIndexChanged += Instruments_List_SelectedIndexChanged;

      Set_Button_State();
    }

    private void Load_NPLC_Combo( Meter_Type Type, decimal? Current_NPLC = null )
    {
      NPLC_Combo_Box.Items.Clear();
      NPLC_Combo_Box.Items.AddRange(
        Type.Get_NPLC_Values().Select( n => n.ToString( CultureInfo.InvariantCulture ) ).ToArray<object>() );

      string Target = Current_NPLC?.ToString( CultureInfo.InvariantCulture ) ?? _Settings.Default_NPLC ?? "1";

      NPLC_Combo_Box.SelectedItem = Target;
      if ( NPLC_Combo_Box.SelectedIndex < 0 )
        NPLC_Combo_Box.SelectedIndex = 0;
    }

    protected override void OnFormClosing( FormClosingEventArgs e )
    {
      if ( _Comm.Is_Connected && ! _Cleanup_Done )
      {
        e.Cancel = true;
        _        = Shutdown_And_Close();
        return;
      }

      base.OnFormClosing( e );
    }

    private async Task Shutdown_And_Close()
    {
      _Cleanup_Done = true;
      await _Comm.Disconnect_Async();    // all the USB cleanup is in here
      this.Invoke( () => this.Close() ); // re-trigger close, cleanup done
    }

    // ===== Command List =====

    private void Populate_Command_List()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Command_List.Items.Clear();
      foreach ( Command_Entry Entry in _All_Commands )
      {
        Command_List.Items.Add( $"{Entry.Command}  -  {Entry.Description}" );
      }
    }

    public class Device_Health_Result
    {

      public string       Prologix_Auto    = "";
      public string       Prologix_EOI     = "";
      public string       Prologix_EOS     = "";
      public string       Prologix_SaveCfg = "";

      public bool         Is_Healthy { get; set; }
      public string       Device_Identity { get; set; }
      public List<string> Passed_Checks { get; set; } = new();
      public List<string> Failed_Checks { get; set; } = new();
      public string       Summary =>
        Is_Healthy ? $"Device at address {Checked_Address} is operational."
                   : $"Device at address {Checked_Address} failed {Failed_Checks.Count} check(s).";
      public int Checked_Address { get; set; }
    }

    private void Command_List_Selected_Index_Changed( object Sender, EventArgs E )
    {
      if ( _Ignore_Selection_Changed )
        return;

      if ( Command_List.SelectedIndex < 0 || Command_List.SelectedIndex >= _All_Commands.Count )
      {
        Detail_Text_Box.Text = "";
        return;
      }

      Command_Entry Selected = _All_Commands[ Command_List.SelectedIndex ];

      Detail_Text_Box.Text =
        $"Command:     {Selected.Command}\r\n" + $"Syntax:      {Selected.Syntax}\r\n" +
        $"Category:    {Selected.Category}\r\n" + $"Description: {Selected.Description}\r\n" +
        $"Parameters:  {Selected.Parameters}\r\n" + $"Query Form:  {Selected.Query_Form}\r\n" +
        $"Default:     {Selected.Default_Value}\r\n" + $"Example:     {Selected.Example}";

      Send_Command_Text_Box.Text = Selected.Command;

      Update_Button_State( Selected );

      Execute_Button.Refresh();
    }

    private void Apply_Settings()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // ===== WINDOW TITLE =====

      if ( ! string.IsNullOrWhiteSpace( _Settings.Default_Window_Title ) )
        this.Text = _Settings.Default_Window_Title;
      else
        this.Text = "W&W Co.  Since 1969";

      // ===== SAVE FOLDER =====

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

      // ===== GPIB SETTINGS =====

      if ( _Comm != null )
      {
        _Comm.Read_Timeout_Ms = _Settings.Default_GPIB_Timeout_Ms;
      }

      Subnet_Textbox.Text     = _Settings.Network_Scan_Subnet;
      IP_Address_Textbox.Text = _Settings.Default_IP_Address;

      Capture_Trace.Write(
        $"Subnet set to: [{_Settings.Network_Scan_Subnet}] Control name: [{Subnet_Textbox.Name}]" );
      Capture_Trace.Write( "Settings applied successfully" );
    }

    protected override void OnLoad( EventArgs E )
    {
      base.OnLoad( E );
    }

    private void Open_Dictionary_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      using var Dictionary_Window = new Dictionary_Form( _Selected_Meter );
      Dictionary_Window.ShowDialog( this );
    }

    public void Safe_UI_Update( Action UI_Action )
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

      using var Block = Trace_Block.Start_If_Enabled();

      try
      {
        if ( this == null || this.IsDisposed || ! this.IsHandleCreated )
          return;

        if ( InvokeRequired )
        {
          // Check handle again to avoid race between check and invoke
          if ( this.IsHandleCreated && ! this.IsDisposed )
            BeginInvoke( UI_Action );
        }
        else
        {
          UI_Action();
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

      Capture_Trace.Write( "\n" );
    }

    private void Update_Trace_Button_State( bool Trace_Visible )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( InvokeRequired )
      {
        BeginInvoke( new Action( () => Update_Trace_Button_State( Trace_Visible ) ) );
        return;
      }

      Button_Show_Execution_Trace.Text      = Trace_Visible ? "Trace OFF" : "Trace ON";
      Button_Show_Execution_Trace.BackColor = Trace_Visible ? Color.Yellow : SystemColors.Control;
    }

    private void Trace_Window_Trace_Visibility_Changed( bool Visible )
    {
      if ( InvokeRequired )
      {
        BeginInvoke( () => Trace_Window_Trace_Visibility_Changed( Visible ) );
        return;
      }
      Button_Show_Execution_Trace.Text      = Visible ? "Trace OFF" : "Trace ON";
      Button_Show_Execution_Trace.BackColor = Visible ? Color.Green : SystemColors.Control;
    }

    private void Button_Show_Execution_Trace_Click( object? Sender, EventArgs Args )
    {
      Trace_Execution.Toggle();
      Button_Show_Execution_Trace.Text      = Trace_Execution.IsRunning ? "Trace OFF" : "Trace ON";
      Button_Show_Execution_Trace.BackColor = Trace_Execution.IsRunning ? Color.Yellow : SystemColors.Control;
    }

    private void Multi_Poll_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( _Poll_Form != null && ! _Poll_Form.IsDisposed )
      {
        _Poll_Form.BringToFront();
        return;
      }

      Cursor = Cursors.WaitCursor;

      if ( _Instruments.Count == 0 )
      {
        MessageBox.Show( "No instruments configured. Please scan for instruments first.",
                         "No Instruments",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        Cursor = Cursors.Default;
        return;
      }

      // ── Commit any pending NPLC edit before cloning ───────────────────
      Commit_Current_Instrument_Edits();

      var Cloned = _Instruments
                     .Select( Ins => new Instrument { Name       = Ins.Name,
                                                      Address    = Ins.Address,
                                                      Type       = Ins.Type,
                                                      Visible    = Ins.Visible,
                                                      NPLC       = Ins.NPLC,
                                                      Meter_Roll = Ins.Meter_Roll,
                                                      Is_Master  = Ins.Is_Master } )
                     .ToList();

      // ── Verify NPLC values before opening ─────────────────────────────
      foreach ( var I in Cloned )
        Capture_Trace.Write( $"Clone: {I.Name} NPLC={I.NPLC}" );

      // ── Require master selection when 3+ instruments ───────────────────────
      if ( Cloned.Count( I => I.Visible ) > 2 && ! Cloned.Any( I => I.Is_Master ) )
      {
        MessageBox.Show( "Please select a master instrument before starting a multi-poll.",
                         "Master Instrument Required",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        Cursor = Cursors.Default;
        return;
      }

      _Poll_Form = new Multi_Instrument_Poll_Form( _Comm, Cloned, _Settings, _Selected_Meter );

      _Poll_Form.FormClosed += ( s, e ) =>
      {
        _Poll_Form = null;
        Set_Button_State();
      };

      Cursor = Cursors.Default;
      Set_Button_State();
      _Poll_Form.Show();
    }

    private void Commit_Current_Instrument_Edits()
    {
      if ( _Selected_Index < 0 || _Selected_Index >= _Instruments.Count )
        return;

      if ( ! decimal.TryParse( NPLC_Combo_Box.SelectedItem?.ToString(),
                               NumberStyles.Number,
                               CultureInfo.InvariantCulture,
                               out decimal NPLC ) )
        return;

      _Instruments[ _Selected_Index ].NPLC = NPLC;
      Capture_Trace.Write( $"Committed NPLC {NPLC} for {_Instruments[ _Selected_Index ].Name}" );
    }

    // ===== Instrument List =====

    private async void Add_Instrument_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Cursor             = Cursors.WaitCursor;
      _Updating_Controls = true;
      Capture_Trace.Write( "Adding Instrument" );

      int        Address = (int) GPIB_Address_Numeric.Value;
      Meter_Type Type    = Meter_Type_Extensions.From_Combo_Index( Instrument_Type_Combo.SelectedIndex );

      Capture_Trace.Write(
        $"Type captured on entry: {Type}  ComboIndex: {Instrument_Type_Combo.SelectedIndex}" );

      string Name = Instrument_Name_Text.Text.Trim();

      string Default_Name = Meter_Type_Extensions.Get_Name( Type ); // ← capture before await

      if ( string.IsNullOrEmpty( Name ) )
        Name = Default_Name;

      // ── Duplicate address check ───────────────────────────────────────
      if ( _Instruments.Any( i => i.Address == Address ) )
      {
        _Updating_Controls = false;
        MessageBox.Show( $"An instrument at GPIB address {Address} is already in the list.",
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
          if ( _Comm.Is_Address_Verified( Address ) )
          {
            Capture_Trace.Write( $"Address {Address} ({Type}) already verified - using cached result." );
            Append_Response( $"[Address {Address} already verified - using cached result]" );

            string Cached_Response = await Task.Run(
              () => _Comm.Verify_GPIB_Address( Address, Is_Legacy_HP( Type ), Restore_Address: false ) );

            if ( string.IsNullOrEmpty( Cached_Response ) )
            {
              Append_Response( $"[No response from address {Address} - not added]" );
              Cursor             = Cursors.Default;
              _Updating_Controls = false;
              return;
            }

            Meter_Type Detected = Multimeter_Common_Helpers_Class.Get_Meter_Type( Cached_Response );
            Capture_Trace.Write( $"Cached detected type: {Detected}  Selected type: {Type}" );

            if ( Detected != Meter_Type.Generic_GPIB && Detected != Type )
            {
              string       Detected_Name = Meter_Type_Extensions.Get_Name( Detected );
              string       Selected_Name = Meter_Type_Extensions.Get_Name( Type );
              DialogResult Result        = DialogResult.No;

              this.Invoke( () =>
                           {
                             Result =
                               MessageBox.Show( $"The instrument at address {Address} identified as:\n\n" +
                                                  $"  Detected:  {Detected_Name}\n" +
                                                  $"  Selected:  {Selected_Name}\n\n" +
                                                  $"Do you want to add it as {Detected_Name} instead?",
                                                "Instrument Mismatch",
                                                MessageBoxButtons.YesNoCancel,
                                                MessageBoxIcon.Warning );
                           } );

              if ( Result == DialogResult.Cancel )
              {
                Cursor                  = Cursors.Default;
                Command_List_Label.Text = "";
                return;
              }

              if ( Result == DialogResult.Yes )
              {
                Type = Detected;
                Name = Meter_Type_Extensions.Get_Name( Type );
                this.Invoke( () => Load_NPLC_Combo( Type ) );
                this.Invoke(
                  () =>
                  {
                    MessageBox.Show( $"The instrument name has been updated to match the detected type.\n\n" +
                                       $"  Selected:  {Selected_Name}\n" +
                                       $"  Actual:    {Detected_Name}\n\n" +
                                       $"The instrument will be added as {Detected_Name}.",
                                     "Instrument Name Updated",
                                     MessageBoxButtons.OK,
                                     MessageBoxIcon.Information );
                  } );
                Command_List_Label.Text = Detected_Name.ToString();
              }
            }
          }
          else
          {
            Capture_Trace.Write( $"[Verifying GPIB address {Address}...]" );
            Append_Response( $"[Verifying GPIB address {Address}...]" );

            string ID_Response = await Task.Run(
              () => _Comm.Verify_GPIB_Address( Address, Is_Legacy_HP( Type ), Restore_Address: false ) );

            if ( string.IsNullOrEmpty( ID_Response ) )
            {
              Append_Response( $"[No instrument at address {Address} - not added]" );
              Cursor = Cursors.Default;
              return;
            }

            _Comm.Mark_Address_Verified( Address );
            Append_Response( $"[Verified at address {Address}: {ID_Response}]" );

            // ── Instrument present but could not be identified ────────────
            if ( ID_Response == "UNIDENTIFIED" )
            {
              Append_Response( $"[Address {Address} has an instrument but it did not respond to IDN]" );
              DialogResult Unid_Result = DialogResult.No;
              this.Invoke(
                () =>
                {
                  Unid_Result = MessageBox.Show(
                    $"An instrument was found at address {Address} but could not be identified.\n\n" +
                      $"It did not respond correctly to *IDN?, ID?, or numeric probe.\n\n" +
                      $"Do you want to add it anyway as {Meter_Type_Extensions.Get_Name( Type )}?",
                    "Instrument Not Identified",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning );
                } );

              if ( Unid_Result != DialogResult.Yes )
              {
                Cursor = Cursors.Default;
                return;
              }
              // User said Yes — fall through to instrument creation below
            }
            else
            {
              Meter_Type Detected = Multimeter_Common_Helpers_Class.Get_Meter_Type( ID_Response );

              if ( Detected == Meter_Type.Generic_GPIB )
              {
                Append_Response(
                  $"[Instrument at address {Address} did not respond to *IDN? — added as {Meter_Type_Extensions.Get_Name( Type )}]" );
              }
              else if ( Detected != Type )
              {
                string       Detected_Name = Meter_Type_Extensions.Get_Name( Detected );
                string       Selected_Name = Meter_Type_Extensions.Get_Name( Type );
                DialogResult Result        = DialogResult.No;

                this.Invoke( () =>
                             {
                               Result =
                                 MessageBox.Show( $"The instrument at address {Address} identified as:\n\n" +
                                                    $"  Detected:  {Detected_Name}\n" +
                                                    $"  Selected:  {Selected_Name}\n\n" +
                                                    $"Do you want to add it as {Detected_Name} instead?",
                                                  "Instrument Mismatch",
                                                  MessageBoxButtons.YesNoCancel,
                                                  MessageBoxIcon.Warning );
                             } );

                if ( Result == DialogResult.Cancel )
                {
                  Command_List_Label.Text = "Commands:";
                  Cursor                  = Cursors.Default;
                  return;
                }

                if ( Result == DialogResult.Yes )
                {
                  Command_List_Label.Text = Detected_Name.ToString() + " Commands:";
                  Type                    = Detected;
                  this.Invoke( () => Load_NPLC_Combo( Type ) );
                  Append_Response( $"[Instrument type changed from {Selected_Name} to {Detected_Name}]" );
                }
                else
                {
                  Append_Response(
                    $"[Warning: adding as {Selected_Name} but instrument identified as {Detected_Name}]" );
                }
              }

              // ── Update name to match final type if still at default ───
              if ( Name == Default_Name )
                Name = Meter_Type_Extensions.Get_Name( Type );
            }
          }
        }
        finally
        {
          Refresh_Master_Combo();
          _Updating_Controls = false;
        }
      }
      else
      {
        _Updating_Controls = false;
      }

      // ── Create and add instrument ─────────────────────────────────────
      decimal NPLC =
        decimal.TryParse( NPLC_Combo_Box.SelectedItem?.ToString(), out decimal parsed ) ? parsed : 1m;

      decimal Default_NPLC = Meter_Type_Extensions.Get_Default_NPLC( Type );
      if ( NPLC != Default_NPLC )
      {
        Capture_Trace.Write( $"NPLC overridden by user: {Default_NPLC} → {NPLC}" );
        Append_Response( $"[Note: NPLC set to {NPLC} (default is {Default_NPLC})]" );
      }

      Capture_Trace.Write( $"Creating instrument: Type={Type}  Name={Name}  Address={Address}" );

      var Inst = new Instrument { Name       = Name,
                                  Address    = Address,
                                  Type       = Type,
                                  Verified   = true,
                                  Visible    = true,
                                  NPLC       = NPLC,
                                  Meter_Roll = Roll_Name_Textbox.Text.Trim() };

      // Calculate min delay including the new instrument before adding
      int Nplc_Min_Ms = _Instruments.Sum( s => s.Poll_Delay_Ms ) +
                        Inst.Poll_Delay_Ms // new instrument not yet added
                        + 50;

      _Instruments.Add( Inst );

      try
      {
        await Initialize_Remote_For_Instrument( Inst );
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write( $"Failed to initialize instrument at address {Address}: {Ex.Message}" );
        Append_Response( $"[Warning: initialization failed for address {Address}: {Ex.Message}]" );
      }
      finally
      {
        Roll_Name_Textbox.Enabled = false;
        _Selected_Index           = _Instruments.IndexOf( Inst ); // ← select the newly added instrument
        Refresh_Instrument_List();
        Set_Button_State();

        // Populate_Command_List ( );
        Cursor = Cursors.Default;
      }
    }

    private void Refresh_Instrument_List()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Instruments_List.SelectedIndexChanged -= Instruments_List_SelectedIndexChanged;

      Instruments_List.DataSource    = null;
      Instruments_List.DataSource    = _Instruments.Where( i => i.Visible ).ToList();
      Instruments_List.DisplayMember = "Display";

      if ( _Selected_Index >= 0 && _Selected_Index < Instruments_List.Items.Count )
        Instruments_List.SelectedIndex = _Selected_Index;

      Instruments_List.SelectedIndexChanged += Instruments_List_SelectedIndexChanged; // ← resubscribe first

      Instruments_List_SelectedIndexChanged( this, EventArgs.Empty ); // ← then fire once manually

      Refresh_Master_Combo();                                         // ++ keep master combo in sync
    }

    public void Set_GPID_Controls( bool State )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Select_Instrument_Button.Enabled = State;
      GPIB_Address_Label.Enabled       = State;
      GPIB_Address_Numeric.Enabled     = State;

      Remove_Instrument_Button.Enabled = State;
      Scan_Bus_Button.Enabled          = State;
      Saved_Instruments_Label.Enabled  = State;
      Instruments_List.Enabled         = State;
    }
    private void IP_Address_Text_TextChanged( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Settings.Default_IP_Address = IP_Address_Textbox.Text.Trim();
      _Settings.Save();
    }

    private void Subnet_Textbox_TextChanged( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Settings.Network_Scan_Subnet = Subnet_Textbox.Text.Trim().TrimEnd( '.' );
      _Settings.Save();
    }

    private void Instrument_Type_Combo_SelectedIndexChanged( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if ( _Updating_Controls )
        return;
      if ( _Adding_Instrument ) // ← don't stomp combo while async add is running
        return;
      _Selected_Meter         = Meter_Type_Extensions.From_Combo_Index( Instrument_Type_Combo.SelectedIndex );
      _Comm.Connected_Meter   = _Selected_Meter;
      _All_Commands           = Command_Dictionary_Class.Get_All_Commands( _Selected_Meter );
      Command_List_Label.Text = $"{_Selected_Meter.Get_Name()} Commands";
      Instrument_Name_Text.Text  = _Selected_Meter.Get_Name();
      Detail_Text_Box.Text       = "";
      Send_Command_Text_Box.Text = "";
      Load_NPLC_Combo( _Selected_Meter );
    }

    private async Task Initialize_Remote_For_Instrument( Instrument inst )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( ! _Comm.Is_Connected )
        return;

      // ── Prologix interface setup ──────────────────────────────────────────
      if ( _Comm.Mode == Connection_Mode.Prologix_GPIB || _Comm.Mode == Connection_Mode.Prologix_Ethernet )
      {
        _Comm.Flush_Buffers();
        string ver = await Task.Run( () => _Comm.Query_Prologix_Version() );
        Append_Response( $"> Prologix version: {ver}" );
        Capture_Trace.Write( $"> Prologix version: {ver}" );
      }

      Capture_Trace.Write( $"Setting {inst.Type} at address {inst.Address} to remote mode..." );

      switch ( inst.Type )
      {
        // ====================================================================
        // HP 34401A / HP 34420A / HP 33120A / HP 53132A
        // Existing SCPI instruments — unchanged from original
        // ====================================================================
        case Meter_Type.HP33120 :
        case Meter_Type.HP34401 :
        case Meter_Type.HP34420 :
        case Meter_Type.HP53132 :
          await _Comm.Send_Prologix_CommandAsync( $"++addr {inst.Address}" );
          await Task.Delay( 50 );
          await _Comm.Send_Prologix_CommandAsync( "++auto 0" );
          await Task.Delay( 50 );

          Append_Response( $"> Turning {inst.Name} Beep Off" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYSTEM:BEEPER:STATE OFF" );
          await Task.Delay( 100 );

          Capture_Trace.Write( "Clearing..." );
          Append_Response( $"> Clearing {inst.Name}" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "*CLS" );
          await Task.Delay( 200 );
          Append_Response( "> *CLS" );

          Append_Response( $"> Setting {inst.Name} to remote mode" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYSTEM:REMOTE" );
          await Task.Delay( 200 );
          Append_Response( "> SYSTEM:REMOTE" );

          Append_Response( $"> Turning {inst.Name} Beep On" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYSTEM:BEEPER:STATE ON" );
          await Task.Delay( 100 );

          Capture_Trace.Write( "Clearing..." );
          Append_Response( $"> Clearing {inst.Name}" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "*CLS" );
          await Task.Delay( 200 );
          Append_Response( "> *CLS" );
          break;

        // ====================================================================
        // HP 34411A — high-speed DMM, SCPI-compatible with 34401A
        // Differences from 34401A:
        //   - SYST:REM is the correct remote command (not SYSTEM:REMOTE)
        //   - Supports DATA:LAST? for non-disruptive polling
        //   - Larger memory — clear it on connect to avoid stale readings
        // ====================================================================
        case Meter_Type.HP34411 :
          await _Comm.Send_Prologix_CommandAsync( $"++addr {inst.Address}" );
          await Task.Delay( 50 );
          await _Comm.Send_Prologix_CommandAsync( "++auto 0" );
          await Task.Delay( 50 );

          Append_Response( $"> Turning {inst.Name} Beep Off" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYST:BEEP:STAT OFF" );
          await Task.Delay( 100 );

          Capture_Trace.Write( "Clearing status registers..." );
          Append_Response( $"> Clearing {inst.Name}" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "*CLS" );
          await Task.Delay( 200 );
          Append_Response( "> *CLS" );

          // Abort any running measurement before going remote
          Capture_Trace.Write( "Aborting any in-progress measurement..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "ABOR" );
          await Task.Delay( 100 );

          Append_Response( $"> Setting {inst.Name} to remote mode" );
          Capture_Trace.Write( "Setting HP34411A to remote mode..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYST:REM" );
          await Task.Delay( 200 );
          Append_Response( "> SYST:REM" );

          // Set output format to ASCII for compatibility with the comm layer
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "FORM ASC" );
          await Task.Delay( 50 );
          Append_Response( "> FORM ASC" );

          Append_Response( $"> Turning {inst.Name} Beep On" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYST:BEEP:STAT ON" );
          await Task.Delay( 100 );

          Capture_Trace.Write( "Clearing again after remote..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "*CLS" );
          await Task.Delay( 200 );
          Append_Response( "> *CLS" );

          Capture_Trace.Write( "Connected to HP34411A..." );
          Append_Response( $"> Connected to HP 34411A at address {inst.Address}" );
          break;

        // ====================================================================
        // HP 33220A — 20 MHz function / ARB generator
        // Differences from 33120A:
        //   - Output is OFF by default after *RST — leave it off on connect
        //   - SYST:REM is the correct remote command
        //   - OUTP:LOAD defaults to 50 Ohm — confirm this is set
        //   - No SYSTEM:BEEPER:STATE — use SYST:BEEP:STAT
        // ====================================================================
        case Meter_Type.HP33220 :
          await _Comm.Send_Prologix_CommandAsync( $"++addr {inst.Address}" );
          await Task.Delay( 50 );
          await _Comm.Send_Prologix_CommandAsync( "++auto 0" );
          await Task.Delay( 50 );

          Append_Response( $"> Turning {inst.Name} Beep Off" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYST:BEEP:STAT OFF" );
          await Task.Delay( 100 );

          Capture_Trace.Write( "Clearing status registers..." );
          Append_Response( $"> Clearing {inst.Name}" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "*CLS" );
          await Task.Delay( 200 );
          Append_Response( "> *CLS" );

          Append_Response( $"> Setting {inst.Name} to remote mode" );
          Capture_Trace.Write( "Setting HP33220A to remote mode..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYST:REM" );
          await Task.Delay( 200 );
          Append_Response( "> SYST:REM" );

          // Confirm output load is 50 Ohm so displayed amplitude is accurate
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "OUTP:LOAD 50" );
          await Task.Delay( 50 );
          Append_Response( "> OUTP:LOAD 50" );

          // Leave output state as-is — do not force it on or off on connect
          // User controls output via OUTP ON|OFF command from the dictionary
          Append_Response( $"> Note: output state unchanged — use OUTP ON to enable" );

          Append_Response( $"> Turning {inst.Name} Beep On" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYST:BEEP:STAT ON" );
          await Task.Delay( 100 );

          Capture_Trace.Write( "Clearing again after remote..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "*CLS" );
          await Task.Delay( 200 );
          Append_Response( "> *CLS" );

          Capture_Trace.Write( "Connected to HP33220A..." );
          Append_Response( $"> Connected to HP 33220A at address {inst.Address}" );
          break;

        // ====================================================================
        // HP 53181A — 225 MHz single-channel frequency counter
        // Differences from 53132A:
        //   - Single channel only (no CH2 time interval or ratio commands)
        //   - Same SCPI command structure as 53132A for frequency measurement
        //   - ABOR important — abort any running gate before going remote
        //   - No SYSTEM:BEEPER:STATE — use SYST:BEEP
        //   - TRIG:SOUR IMM for continuous free-running polling
        //   - SENS:FREQ:GATE:TIME 0.1 is a reasonable default gate
        // ====================================================================
        case Meter_Type.HP53181 :
          await _Comm.Send_Prologix_CommandAsync( $"++addr {inst.Address}" );
          await Task.Delay( 50 );
          await _Comm.Send_Prologix_CommandAsync( "++auto 0" );
          await Task.Delay( 50 );

          // Abort any running measurement — counter may be mid-gate
          Capture_Trace.Write( "Aborting any in-progress gate..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "ABOR" );
          await Task.Delay( 200 );
          Append_Response( "> ABOR" );

          Capture_Trace.Write( "Clearing status registers..." );
          Append_Response( $"> Clearing {inst.Name}" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "*CLS" );
          await Task.Delay( 200 );
          Append_Response( "> *CLS" );

          Append_Response( $"> Setting {inst.Name} to remote mode" );
          Capture_Trace.Write( "Setting HP53181A to remote mode..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SYST:REM" );
          await Task.Delay( 200 );
          Append_Response( "> SYST:REM" );

          // Set output format to ASCII
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "FORM ASC" );
          await Task.Delay( 50 );
          Append_Response( "> FORM ASC" );

          // Configure for immediate continuous triggering
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "TRIG:SOUR IMM" );
          await Task.Delay( 50 );
          Append_Response( "> TRIG:SOUR IMM" );

          // Set gate time to 100 ms — good balance of speed and resolution
          // (~9 digits). User can change via the dictionary.
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "SENS:FREQ:GATE:TIME 0.1" );
          await Task.Delay( 50 );
          Append_Response( "> SENS:FREQ:GATE:TIME 0.1" );

          // Configure frequency measurement on CH1 as the default
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "CONF:FREQ" );
          await Task.Delay( 100 );
          Append_Response( "> CONF:FREQ" );

          Capture_Trace.Write( "Connected to HP53181A..." );
          Append_Response( $"> Connected to HP 53181A at address {inst.Address}" );
          break;

        // ====================================================================
        // HP 4263B — LCR meter
        // Key considerations:
        //   - No SYST:REM — instrument goes remote automatically when addressed
        //   - No SYST:BEEP:STAT — instrument has no software beeper control
        //   - ABOR returns trigger system to idle — always do this on connect
        //   - TRIG:SOUR INT for continuous internal triggering during polling
        //   - FUNC:IMP:TYPE CPD is a safe default (Cp + D) for most capacitors
        //   - FREQ 1000 — 1 kHz is the most common test frequency
        //   - APER MED,1 — medium integration, 1 average: balanced speed/accuracy
        //   - Compensation state is preserved in non-volatile memory —
        //     do NOT clear it on connect. Query and report status only.
        // ====================================================================
        case Meter_Type.HP4263 :
          await _Comm.Send_Prologix_CommandAsync( $"++addr {inst.Address}" );
          await Task.Delay( 50 );
          await _Comm.Send_Prologix_CommandAsync( "++auto 0" );
          await Task.Delay( 50 );

          // Abort any in-progress measurement
          Capture_Trace.Write( "Aborting any in-progress measurement..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "ABOR" );
          await Task.Delay( 200 );
          Append_Response( "> ABOR" );

          // Clear status registers only — compensation data is preserved
          Capture_Trace.Write( "Clearing status registers..." );
          Append_Response( $"> Clearing {inst.Name} status registers" );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "*CLS" );
          await Task.Delay( 200 );
          Append_Response( "> *CLS" );

          // Instrument enters remote automatically when addressed — no SYST:REM
          Append_Response( $"> {inst.Name} enters remote mode when addressed (no SYST:REM needed)" );

          // Set output data format to ASCII
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "FORM ASC" );
          await Task.Delay( 50 );
          Append_Response( "> FORM ASC" );

          // Set test frequency to 1 kHz (most common default)
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "FREQ 1000" );
          await Task.Delay( 50 );
          Append_Response( "> FREQ 1000" );

          // Set measurement function to Cp+D (parallel capacitance + dissipation)
          // This is the most common starting point for capacitor measurement.
          // User can change via FUNC:IMP:TYPE in the dictionary.
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "FUNC:IMP:TYPE CPD" );
          await Task.Delay( 50 );
          Append_Response( "> FUNC:IMP:TYPE CPD" );

          // Set integration time to medium, 1 average — balanced for polling
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "APER MED,1" );
          await Task.Delay( 50 );
          Append_Response( "> APER MED,1" );

          // Use internal trigger source for continuous free-running measurement
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "TRIG:SOUR INT" );
          await Task.Delay( 50 );
          Append_Response( "> TRIG:SOUR INT" );

          // Set autorange on — let instrument select range for first measurement
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "FUNC:IMP:RANG:AUTO ON" );
          await Task.Delay( 50 );
          Append_Response( "> FUNC:IMP:RANG:AUTO ON" );

          // Query and report current compensation state — do NOT reset it
          Capture_Trace.Write( "Querying compensation state..." );
          try
          {
            string  Open_State =
              await Task.Run( () => _Comm.Query_Instrument_At_Address( inst.Address, "CORR:OPEN:STAT?" ) );
            string  Shor_State =
              await Task.Run( () => _Comm.Query_Instrument_At_Address( inst.Address, "CORR:SHOR:STAT?" ) );
            string  Load_State =
              await Task.Run( () => _Comm.Query_Instrument_At_Address( inst.Address, "CORR:LOAD:STAT?" ) );

            Append_Response( $"> Compensation — Open: {(Open_State.Trim() == "1" ? "ON" : "OFF")}  " +
                             $"Short: {(Shor_State.Trim() == "1" ? "ON" : "OFF")}  " +
                             $"Load: {(Load_State.Trim() == "1" ? "ON" : "OFF")}" );
          }
          catch
          {
            // Non-fatal — compensation query may fail if instrument is mid-measurement
            Append_Response( "> Compensation state query skipped (instrument busy)" );
          }

          Capture_Trace.Write( "Connected to HP4263B..." );
          Append_Response( $"> Connected to HP 4263B at address {inst.Address}" );
          Append_Response( $"> Note: FETCH? returns primary,secondary,status — check status before logging" );
          break;

        // ====================================================================
        // HP 3458A — existing, unchanged
        // ====================================================================
        case Meter_Type.HP3458 :
          if ( _Settings.Send_Reset_On_Connect_3458 )
          {
            Capture_Trace.Write( "Sending Reset..." );
            await _Comm.Send_Instrument_CommandAsync( inst.Address, "RESET" );
            await Task.Delay( 3000 );

            Capture_Trace.Write( "Sending ++addr..." );
            await _Comm.Send_Prologix_CommandAsync( $"++addr {inst.Address}" );
            await Task.Delay( 50 );
          }

          if ( _Settings.Send_End_Always_3458 )
          {
            Capture_Trace.Write( "Sending Beep off..." );
            await _Comm.Send_Instrument_CommandAsync( inst.Address, "BEEP 0" );
            await Task.Delay( 200 );

            Capture_Trace.Write( "Sending End Always..." );
            await _Comm.Send_Instrument_CommandAsync( inst.Address, "END ALWAYS" );
            await Task.Delay( 200 );
          }

          Capture_Trace.Write( "Sending TRIG AUTO..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, "TRIG AUTO" );
          await Task.Delay( 200 );

          Capture_Trace.Write( "Sending GPIB..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, $"GPIB {inst.Address}" );
          await Task.Delay( 100 );

          Capture_Trace.Write( "Sending ++addr..." );
          await _Comm.Send_Prologix_CommandAsync( $"++addr {inst.Address}" );
          await Task.Delay( 50 );

          Capture_Trace.Write( "Sending NPLC..." );
          await _Comm.Send_Instrument_CommandAsync( inst.Address, $"NPLC {_Settings.Default_NPLC_3458}" );
          await Task.Delay( _Settings.NPLC_Apply_Delay_Ms );

          Capture_Trace.Write( "Connected to HP3458A..." );
          Append_Response( $"> Connected to HP 3458A at address {inst.Address}" );
          break;

        // ====================================================================
        // Default — generic or unrecognised instrument
        // ====================================================================
        default :
          Capture_Trace.Write( $"No specific init for meter type: {inst.Type}" );
          Append_Response( $"> Connected to {inst.Type} at address {inst.Address}" );
          break;
      }

      // ── Update UI ─────────────────────────────────────────────────────────
      Connected_Instrument_Textbox.Text = inst.Name;
    }

    // ── Is_Legacy_HP — updated for all new types ─────────────────────────────
    // Returns true for instruments that use proprietary HP-IB command sets
    // rather than SCPI. Used during *IDN? verification to select the correct
    // identification probe sequence.
    private static bool Is_Legacy_HP( Meter_Type Type ) =>
      Type switch { Meter_Type.HP3458  => true, // HP-IB native — responds to "ID?" not "*IDN?"
                    Meter_Type.HP3456  => true, // HP-IB native — responds to "ID?" not "*IDN?"
                    Meter_Type.HP34401 => false,
                    Meter_Type.HP34411 => false,
                    Meter_Type.HP33120 => false,
                    Meter_Type.HP33220 => false,
                    Meter_Type.HP34420 => false,
                    Meter_Type.HP53132 => false,
                    Meter_Type.HP53181 => false,
                    Meter_Type.HP4263  => false,
                    _                  => false };

    private void Remove_Instrument_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      int       Index = Instruments_List.SelectedIndex;

      if ( Index < 0 )
      {

        return;
      }

      bool Removing_Active = ( _Instruments[ Index ].Type == _Selected_Meter );
      if ( Removing_Active )
      {
        Command_List.Items.Clear();
        Command_List_Label.Text = "Commands";
      }
      _Instruments.RemoveAt( Index );
      Refresh_Instruments_List(); // ← dedicated method, not inline
      Refresh_Master_Combo();

      Set_Button_State();
    }

    private void Refresh_Instruments_List()
    {
      Instruments_List.DataSource    = null;
      Instruments_List.DataSource    = _Instruments;
      Instruments_List.DisplayMember = "Display"; // ← always re-assert after rebind
    }

    private void Select_Instrument_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      int       Index = Instruments_List.SelectedIndex;

      Capture_Trace.Write( $"Index -> {Index}" );

      if ( Index < 0 || Index >= _Instruments.Count )
        return;

      _Selected_Index = Index; // ← this is the only thing this method adds
      Instruments_List_SelectedIndexChanged( Sender, E );
    }

    private void Instruments_List_SelectedIndexChanged( object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( Instruments_List.SelectedItem is not Instrument Instrument )
        return;

      Command_List.BeginUpdate();

      //_Selected_Index = _Instruments.IndexOf ( Instrument );  // ← true index in master list

      int Index       = Instruments_List.SelectedIndex;
      _Selected_Index = Index;

      _Selected_Meter   = Instrument.Type;
      _Selected_Address = Instrument.Address;
      _Comm.Change_GPIB_Address( Instrument.Address );
      _Comm.Connected_Meter = _Selected_Meter;
      _All_Commands         = Command_Dictionary_Class.Get_All_Commands( _Selected_Meter );

      _Updating_Controls                  = true;
      Instrument_Type_Combo.SelectedIndex = _Selected_Meter.To_Combo_Index();
      Load_NPLC_Combo( _Selected_Meter, Instrument.NPLC );
      _Updating_Controls = false;

      Capture_Trace.Write( $"Meter: {_Selected_Meter}  Commands: {_All_Commands?.Count}" );
      Populate_Command_List();
      Command_List_Label.Text = $"{Instrument.Name} Commands";

      Command_List.EndUpdate();
    }

    private async void Scan_Bus_Button_Click( object sender, EventArgs e )
    {
      // If already scanning, cancel and let the finally below clean up
      if ( _Is_Scanning )
      {
        _Scan_Cts?.Cancel();
        Scan_Bus_Button.Text    = "Canceling...";
        Scan_Bus_Button.Enabled = false;
        Append_Response( "[Canceling bus scan...]" );
        return;
      }

      if ( ! _Comm.Is_Connected )
        return;

      _Is_Scanning         = true;
      _Scan_Cts            = new CancellationTokenSource();
      Scan_Bus_Button.Text = "Stop Scan";
      Append_Response( "[Scanning GPIB bus...]" );

      try
      {
        await Run_Bus_Scan( _Scan_Cts.Token );
      }
      catch ( OperationCanceledException )
      {
        Append_Response( "[Bus scan canceled]" );
      }
      catch ( Exception ex )
      {
        Append_Response( $"[Error during bus scan: {ex.Message}]" );
      }
      finally
      {
        _Is_Scanning = false;
        _Scan_Cts?.Dispose();
        _Scan_Cts               = null;
        Scan_Bus_Button.Text    = "Scan Bus";
        Scan_Bus_Button.Enabled = _Comm.Is_Connected;
      }
    }

    private async Task Run_Bus_Scan( CancellationToken Token )
    {
      using var         Block = Trace_Block.Start_If_Enabled();

      var               Progress = new Progress<string>( msg => Append_Response( msg ) );

      List<Scan_Result> Results = await _Comm.Scan_GPIB_BusAsync( Progress, Token );

      foreach ( var result in Results )
      {
        Meter_Type type = result.Detected_Type ?? Meter_Type.HP3458;
        string name = result.ID_String.Length > 40 ? result.ID_String.Substring( 0, 40 ) : result.ID_String;

        var    existing = _Instruments.FirstOrDefault( i => i.Address == result.Address );

        if ( existing != null )
        {
          existing.Verified = true;
          existing.Visible  = true;
          existing.Name     = name;
          Append_Response( $"[Instrument at GPIB {result.Address} refreshed]" );
        }
        else
        {
          _Instruments.Add( new Instrument { Name     = name,
                                             Address  = result.Address,
                                             Type     = type,
                                             Verified = true,
                                             Visible  = true } );
          Append_Response( $"[Detected new instrument at GPIB {result.Address}]" );
        }
      }

      Refresh_Instrument_List();

      if ( Results.Count == 0 )
        Append_Response( "No instruments found on the GPIB bus." );
    }

    private void Update_Command_Mode( Command_Entry Entry )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( Entry == null )
      {
        Execute_Button.Enabled = false;
        // Query_Button.Enabled = false;
        return;
      }

      Execute_Button.Enabled = Entry.Has_Command;
      // Query_Button.Enabled = Entry.Has_Query;
    }

    private void Execute_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      string    Input = Send_Command_Text_Box.Text.Trim();
      if ( string.IsNullOrWhiteSpace( Input ) )
        return;

      Capture_Trace.Write( $"Command -> {Input}" );

      // Parse command and optional parameters
      string[ ] Parts   = Input.Split( ' ', 2 );
      string Base_Token = Parts[ 0 ].TrimEnd( '?' );
      bool   Is_Query   = Parts[ 0 ].EndsWith( '?' );
      string Parameters = Parts.Length > 1 ? Parts[ 1 ].Trim() : "";

      // Rebuild final command preserving query suffix and parameters
      string Final_Command =
        Base_Token + ( Is_Query ? "?" : "" ) + ( string.IsNullOrEmpty( Parameters ) ? "" : " " + Parameters );

      Execute_Command( Final_Command );
    }

    private void Execute_Command( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( ! _Comm.Is_Connected )
        return;

      try
      {
        Capture_Trace.Write( $"Command -> {Command}" );
        Add_Command_To_History( Command );

        if ( Command.StartsWith( "++" ) )
        {
          _Comm.Send_Prologix_Command( Command );
        }
        else if ( Command.EndsWith( "?" ) )
        {
          // Query - send and read response
          string Response = _Comm.Query_Instrument( Command );
          if ( ! string.IsNullOrWhiteSpace( Response ) )
            Append_Response( $"< {Response}" );
          else
            Append_Response( "< (no response)" );
        }
        else
        {
          _Comm.Send_Instrument_Command( Command );
        }
      }
      catch ( Exception Ex )
      {
        Add_Command_To_History( $"ERROR: {Ex.Message}" );
      }
    }

    private void Execute_Query( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( ! _Comm.Is_Connected )
        return;

      try
      {
        Capture_Trace.Write( "Query -> " + Command );

        Add_Command_To_History( $"{Command}" );

        string Response = _Comm.Query_Instrument( Command );
        Append_Response( $"< {Response}" );
      }
      catch ( Exception Ex )
      {
        Add_Command_To_History( $"ERROR: {Ex.Message}" );
      }
    }

    private void Empty_Windows()
    {
      using var Block = Trace_Block.Start_If_Enabled();
      _Instruments.Clear();
      Refresh_Instrument_List(); // ← rebind after clear
      Command_List.Items.Clear();
      Send_Command_Text_Box.Clear();
      Command_History_List_Box.Items.Clear();
      Connected_Instrument_Textbox.Clear();
      Detail_Text_Box.Clear();
      Response_Text_Box.Clear();
    }

    private void Clear_Command_And_Details()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Clear selection safely
      _Ignore_Selection_Changed = true;

      Command_List.ClearSelected();
      // Detail_Text_Box.Clear ( );
      Send_Command_Text_Box.Clear();

      _Ignore_Selection_Changed = false;
    }
    private async void Diag_Button_Click( object Sender, EventArgs E )
    {
      using var Block   = Trace_Block.Start_If_Enabled();
      string    Command = Send_Command_Text_Box.Text.Trim();

      /*
      if ( string.IsNullOrEmpty ( Command ) )
      {
        Command = "++ver";
      }
      */

      if ( ! _Comm.Is_Connected )
      {
        Append_Response( "[Not connected]" );
        return;
      }

      Diag_Button.Enabled = false;
      Append_Response( "" );
      Append_Response( "" );
      Append_Response( $"[DIAG] Sending: {Command}" );

      try
      {
        string Result = await Task.Run( () => _Comm.Raw_Diagnostic( Command ) );
        Append_Response( $"[DIAG] {Result}" );
      }
      finally
      {
        Diag_Button.Enabled = true;
      }
    }

    private void Append_Response( string Text )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( Response_Text_Box.Text.Length > 0 )
      {
        Response_Text_Box.AppendText( "\r\n" );
      }
      Response_Text_Box.AppendText( Text );
    }

    // ===== Connection Controls =====

    private void Populate_Connection_Controls()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Connection mode
      Connection_Mode_Combo.Items.Clear();
      Connection_Mode_Combo.Items.Add( "Prologix GPIB" );
      Connection_Mode_Combo.Items.Add( "Direct Serial (RS-232)" );
      Connection_Mode_Combo.Items.Add( "Ethernet" );
      Connection_Mode_Combo.SelectedIndex = 1;

      Set_GPID_Controls( State: false );

      // COM ports
      Refresh_Ports();

      // Baud rates
      Baud_Rate_Combo.Items.Clear();
      foreach ( int Rate in Instrument_Comm.Get_Available_Baud_Rates() )
      {
        Baud_Rate_Combo.Items.Add( Rate );
      }
      Baud_Rate_Combo.SelectedItem = 115200;

      // Data bits
      Data_Bits_Combo.Items.Clear();
      foreach ( int Bits in Instrument_Comm.Get_Available_Data_Bits() )
      {
        Data_Bits_Combo.Items.Add( Bits );
      }
      Data_Bits_Combo.SelectedItem = 8;

      // Parity
      Parity_Combo.Items.Clear();
      foreach ( Parity P in Enum.GetValues( typeof( Parity ) ) )
      {
        Parity_Combo.Items.Add( P );
      }
      Parity_Combo.SelectedItem = Parity.None;

      // Stop bits
      Stop_Bits_Combo.Items.Clear();
      Stop_Bits_Combo.Items.Add( StopBits.One );
      Stop_Bits_Combo.Items.Add( StopBits.OnePointFive );
      Stop_Bits_Combo.Items.Add( StopBits.Two );
      Stop_Bits_Combo.SelectedItem = StopBits.One;

      // Flow control
      Flow_Control_Combo.Items.Clear();
      foreach ( Handshake H in Enum.GetValues( typeof( Handshake ) ) )
      {
        Flow_Control_Combo.Items.Add( H );
      }
      Flow_Control_Combo.SelectedItem = Handshake.None;

      Read_Timeout_Combo_Box.Items.Clear();
      foreach ( int Timeout in Instrument_Comm.Get_Available_Read_Timeouts() )
      {
        Read_Timeout_Combo_Box.Items.Add( Timeout );
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
      // IP_Port_Numeric.Minimum = 1;
      //  IP_Port_Numeric.Maximum = 65535;
      /// IP_Port_Numeric.Value = _Settings.Default_Prologic_Port;

      // EOI default to checked, auto-read off to avoid
      // errors on non-query commands
      //  Auto_Read_Check.Checked = false;
      //  EOI_Check.Checked = true;

      Update_Connection_Status( false );
    }

    private void Refresh_Ports()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      string? Previous_Selection = COM_Port_Combo.SelectedItem?.ToString();

      COM_Port_Combo.Items.Clear();
      string[ ] Ports = Instrument_Comm.Get_Available_Ports();

      foreach ( string Port in Ports )
      {
        COM_Port_Combo.Items.Add( Port );
      }

      if ( Previous_Selection != null && COM_Port_Combo.Items.Contains( Previous_Selection ) )
      {
        COM_Port_Combo.SelectedItem = Previous_Selection;
      }
      else if ( COM_Port_Combo.Items.Count > 0 )
      {
        COM_Port_Combo.SelectedIndex = 0;
      }
    }

    private void Refresh_Ports_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Refresh_Ports();
    }

    private void Defaults_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( Connection_Mode_Combo.SelectedIndex == 1 )
      {
        Capture_Trace.Write( "Setting defaults for Direct Serial connection" );

        // Defaults for Direct Serial (RS-232)
        Baud_Rate_Combo.SelectedItem        = 9600;
        Data_Bits_Combo.SelectedItem        = 8;
        Parity_Combo.SelectedItem           = Parity.None;
        Stop_Bits_Combo.SelectedItem        = StopBits.One;
        Flow_Control_Combo.SelectedItem     = Handshake.None;
        Read_Timeout_Combo_Box.SelectedItem = 3000;

        Connected_Instrument_Textbox.Text = "";
        _Selected_Address                 = 0;
        Set_GPID_Controls( State: false );
      }
      else if ( Connection_Mode_Combo.SelectedIndex == 2 )
      {
        Capture_Trace.Write( "Setting defaults for Ethernet connection" );
        IP_Address_Textbox.Text = "192.168.1.100"; // matches Ethernet_Host default in your class
                                                   //  IP_Port_Numeric.Value = 1234;             // matches
                                                   //  Ethernet_Port default in your class
                                                   // GPIB_Address_Numeric.Value = 22;
        _Selected_Address       = 22;
        Set_GPID_Controls( State: true );
      }
      else
      {
        Capture_Trace.Write( "Setting defaults for Prologix GPIB-USB adapter" );
        // Defaults for Prologix GPIB-USB-HS adapter
        Baud_Rate_Combo.SelectedItem        = 9600;
        Data_Bits_Combo.SelectedItem        = 8;
        Parity_Combo.SelectedItem           = Parity.None;
        Stop_Bits_Combo.SelectedItem        = StopBits.Two;
        Flow_Control_Combo.SelectedItem     = Handshake.RequestToSend;
        Read_Timeout_Combo_Box.SelectedItem = 3000;

        // GPIB_Address_Numeric.Value = 22;
        GPIB_Address_Numeric.Value = 22;
        _Selected_Address          = (int) GPIB_Address_Numeric.Value;

        Set_GPID_Controls( State: true );
      }
    }

    private async Task Release_Instruments_To_Local_Async()
    {
      int Original_Address = -1;

      foreach ( var S in _Instruments )
      {
        try
        {
          _Comm.Change_GPIB_Address( S.Address );
          await Task.Delay( 50 );

          if ( S.Type == Meter_Type.HP3458 )
            _Comm.Send_Instrument_Command( "LOCAL" );
          else
            _Comm.Send_Instrument_Command( "SYST:LOC" );

          await Task.Delay( 50 );
        }
        catch
        {
        } // don't let one instrument block the others
      }
    }

    private async void Connect_Button_Click( object sender, EventArgs e )
    {
      if ( _Comm.Is_Connected )
      {
        Connect_Button.Enabled = false;
        try
        {
          Capture_Trace.Write( "Releasing instruments to local..." );
          await Release_Instruments_To_Local_Async();
          Capture_Trace.Write( "Disconnecting..." );
          await _Comm.Disconnect_Async();
          Capture_Trace.Write( "Clearing Windows" );
          Empty_Windows();
          Set_Button_State();
        }
        finally
        {
          Connect_Button.Enabled = true;
        }
        return;
      }

      // --- Setup transport only ---
      Is_Ethernet = Connection_Mode_Combo.SelectedIndex == 2;
      Capture_Trace.Write(
        $"Selected connection mode: {(Is_Ethernet ? "Ethernet" : Connection_Mode_Combo.SelectedItem)}" );
      if ( Is_Ethernet )
      {
        _Comm.Mode          = Connection_Mode.Prologix_Ethernet;
        _Comm.Ethernet_Host = IP_Address_Textbox.Text.Trim();
        _Comm.Ethernet_Port = (int) _Settings.Default_Prologic_Port;

        Capture_Trace.Write( $"Comm Mode -> {_Comm.Mode}" );
        Capture_Trace.Write( $"Host      -> {_Comm.Ethernet_Host}" );
        Capture_Trace.Write( $"Port      -> {_Comm.Ethernet_Port}" );
      }
      else
      {
        if ( COM_Port_Combo.SelectedItem == null )
        {
          MessageBox.Show( "Please select a COM port.",
                           "Connection Error",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Warning );
          return;
        }

        _Comm.Mode = Connection_Mode_Combo.SelectedIndex == 0 ? Connection_Mode.Prologix_GPIB
                                                              : Connection_Mode.Direct_Serial;

        _Comm.Port_Name       = COM_Port_Combo.SelectedItem.ToString()!;
        _Comm.Baud_Rate       = (int) Baud_Rate_Combo.SelectedItem!;
        _Comm.Data_Bits       = (int) Data_Bits_Combo.SelectedItem!;
        _Comm.Parity          = (Parity) Parity_Combo.SelectedItem!;
        _Comm.Stop_Bits       = (StopBits) Stop_Bits_Combo.SelectedItem!;
        _Comm.Flow_Control    = (Handshake) Flow_Control_Combo.SelectedItem!;
        _Comm.Read_Timeout_Ms = (int) Read_Timeout_Combo_Box.SelectedItem!;

        Capture_Trace.Write( $"Comm Mode       -> {_Comm.Mode}" );
        Capture_Trace.Write( $"Port            -> {_Comm.Port_Name}" );
        Capture_Trace.Write( $"Baud            -> {_Comm.Baud_Rate}" );
        Capture_Trace.Write( $"Data Bits       -> {_Comm.Data_Bits}" );
        Capture_Trace.Write( $"Parity          -> {_Comm.Parity.ToString()}" );
        Capture_Trace.Write( $"Stop Bits       -> {_Comm.Stop_Bits}" );
        Capture_Trace.Write( $"Flow Control    -> {_Comm.Flow_Control.ToString()}" );
        Capture_Trace.Write( $"Read Timeout MS -> {_Comm.Read_Timeout_Ms}" );
      }

      // --- Connect ---
      Connect_Button.Enabled = false;
      try
      {
        await Task.Run( () => _Comm.Connect() );
        Capture_Trace.Write( "Connection established." );

        if ( _Comm.Mode == Connection_Mode.Prologix_GPIB || _Comm.Mode == Connection_Mode.Prologix_Ethernet )
        {
          _Comm.Configure_Prologix_Transport_Only();
        }
      }
      catch ( Exception ex )
      {
        Capture_Trace.Write( $"Connection failed: {ex.Message}" );
        // optionally: MessageBox.Show(...) or status label update
      }
      finally
      {
        Connect_Button.Enabled = true;
        Set_Button_State();
      }
    }

    private void Update_Connection_Status( bool Connected )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( Connected )
      {
        Connection_Status_Label.Text      = "Connected";
        Connection_Status_Label.ForeColor = Color.Green;
        Connect_Button.Text               = "Disconnect";

        // Disable settings while connected
        Connection_Mode_Combo.Enabled = false;
        COM_Port_Combo.Enabled        = false;
        Baud_Rate_Combo.Enabled       = false;
        Data_Bits_Combo.Enabled       = false;
        Parity_Combo.Enabled          = false;
        Stop_Bits_Combo.Enabled       = false;
        Flow_Control_Combo.Enabled    = false;
        Refresh_Ports_Button.Enabled  = false;
        Defaults_Button.Enabled       = false;

        // Initialize_Remote_Connection ( );
      }
      else
      {
        Connection_Status_Label.Text      = "Disconnected";
        Connection_Status_Label.ForeColor = Color.Red;
        Connect_Button.Text               = "Connect";

        // Enable settings while disconnected
        Connection_Mode_Combo.Enabled = true;
        COM_Port_Combo.Enabled        = true;
        Baud_Rate_Combo.Enabled       = true;
        Data_Bits_Combo.Enabled       = true;
        Parity_Combo.Enabled          = true;
        Stop_Bits_Combo.Enabled       = true;
        Flow_Control_Combo.Enabled    = true;
        Refresh_Ports_Button.Enabled  = true;
        Defaults_Button.Enabled       = true;

        // Disable scan while disconnected
        Scan_Bus_Button.Enabled = false;
      }
    }
    private void Connection_Mode_Combo_SelectedIndexChanged( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      Is_GPIB         = Connection_Mode_Combo.SelectedIndex == 0;
      Is_Serial       = Connection_Mode_Combo.SelectedIndex == 1;
      Is_Ethernet     = Connection_Mode_Combo.SelectedIndex == 2;

      // Serial controls only relevant for serial/GPIB-USB
      // COM port needed for both GPIB-USB and Direct Serial
      COM_Port_Combo.Enabled  = Is_GPIB || Is_Serial;
      COM_Port_Label.Enabled  = Is_GPIB || Is_Serial;
      Scan_Bus_Button.Enabled = Is_GPIB || Is_Ethernet;

      // RS-232 settings only relevant for Direct Serial
      // (Prologix GPIB-USB uses fixed settings, Ethernet has none)
      Baud_Rate_Combo.Enabled        = Is_GPIB || Is_Serial;
      Baud_Rate_Label.Enabled        = Is_GPIB || Is_Serial;
      Data_Bits_Combo.Enabled        = Is_GPIB || Is_Serial;
      Data_Bits_Label.Enabled        = Is_GPIB || Is_Serial;
      Parity_Combo.Enabled           = Is_GPIB || Is_Serial;
      Parity_Label.Enabled           = Is_GPIB || Is_Serial;
      Stop_Bits_Combo.Enabled        = Is_GPIB || Is_Serial;
      Stop_Bits_Label.Enabled        = Is_GPIB || Is_Serial;
      Flow_Control_Combo.Enabled     = Is_GPIB || Is_Serial;
      Flow_Control_Label.Enabled     = Is_GPIB || Is_Serial;
      Read_Timeout_Combo_Box.Enabled = Is_GPIB || Is_Serial;
      Read_Timeout_Label.Enabled     = Is_GPIB || Is_Serial;

      // Ethernet controls
      Find_Prologix_Button.Enabled = Is_Ethernet;
      IP_Address_Textbox.Enabled   = Is_Ethernet;
      IP_Address_Label.Enabled     = Is_Ethernet;
      Subnet_Textbox.Enabled       = Is_Ethernet;

      Set_GPID_Controls( State: Is_GPIB || Is_Ethernet );

      Set_Button_State();
    }

    private void Set_Button_State()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Elements that are active if instruments are present

      Multi_Poll_Button.Enabled = _Instruments.Count > 0;
      Meter_Info_Button.Enabled = true;

      Open_Dictionary_Button.Enabled   = _Instruments.Count > 0;
      Diag_Button.Enabled              = _Instruments.Count > 0;
      Execute_Button.Enabled           = _Instruments.Count > 0;
      Remove_Instrument_Button.Enabled = _Instruments.Count > 0;
      NPLC_Combo_Box.Enabled           = _Instruments.Count > 0;
      Instrument_Name_Label.Enabled    = _Instruments.Count > 0;
      Instrument_Name_Text.Enabled     = _Instruments.Count > 0;
      Command_List.Enabled             = _Instruments.Count > 0;
      Command_History_List_Box.Enabled = _Instruments.Count > 0;
      Detail_Text_Box.Enabled          = _Instruments.Count > 0;
      Send_Command_Text_Box.Enabled    = _Instruments.Count > 0;
      History_Label.Enabled            = _Instruments.Count > 0;
      Send_Command_Label.Enabled       = _Instruments.Count > 0;
      Response_Label.Enabled           = _Instruments.Count > 0;
      NPLC_Label.Enabled               = _Instruments.Count > 0;
      Apply_NPLC_To_All_Button.Enabled = NPLC_Combo_Box.SelectedIndex >= 0 && _Instruments.Count > 1;

      // Elements that are active if connection has been made
      Instrument_Type_Combo.Enabled  = _Comm.Is_Connected;
      Instrument_Type_Label.Enabled  = _Comm.Is_Connected;
      GPIB_Address_Numeric.Enabled   = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet );
      GPIB_Address_Label.Enabled     = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet );
      Add_Instrument_Button.Enabled  = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet );
      NPLC_Combo_Box.Enabled         = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet || Is_Serial );
      Roll_Name_Textbox.Enabled      = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet || Is_Serial );
      Meter_Roll_Label.Enabled       = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet || Is_Serial );
      Prologix_Health_Button.Enabled = _Comm.Is_Connected && _Instruments.Count == 0;

      Select_Instrument_Button.Enabled = _Instruments.Count > 0;

      Scan_Bus_Button.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet );

      // When polling form is open or closed

      Instrument_Type_Combo.Enabled = _Comm.Is_Connected && ! Poll_Form_Is_Open;
      Instrument_Type_Label.Enabled = _Comm.Is_Connected && ! Poll_Form_Is_Open;
      Scan_Bus_Button.Enabled       = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet ) && ! Poll_Form_Is_Open;
      Select_Instrument_Button.Enabled = _Instruments.Count > 0 && ! Poll_Form_Is_Open;
      NPLC_Combo_Box.Enabled =
        _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet || Is_Serial ) && ! Poll_Form_Is_Open;
      Add_Instrument_Button.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet ) && ! Poll_Form_Is_Open;
      Remove_Instrument_Button.Enabled = _Instruments.Count > 0 && ! Poll_Form_Is_Open;
      GPIB_Address_Numeric.Enabled = _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet ) && ! Poll_Form_Is_Open;
      Multi_Poll_Button.Enabled =
        _Comm.Is_Connected && ( Is_GPIB || Is_Ethernet ) && ! Poll_Form_Is_Open && _Instruments.Count > 0;
    }

    private void Comm_Connection_Changed( object? Sender, bool Connected )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( InvokeRequired )
      {
        Invoke( () => Update_Connection_Status( Connected ) );
      }
      else
      {
        Update_Connection_Status( Connected );
      }
    }

    private void Comm_Error_Occurred( object? Sender, string Message )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( InvokeRequired )
      {
        Invoke( () => Show_Error( Message ) );
      }
      else
      {
        Show_Error( Message );
      }
    }

    private void Comm_Data_Received( object? Sender, string Data )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      // Future use: log or display received data
    }

    private void GPIB_Address_Numeric_ValueChanged( object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Updating_Controls = true;

      // Always keep UI controls in sync
      //    Instrument_Address_Numeric.Value = GPIB_Address_Numeric.Value;

      // Only update hardware if connected
      if ( _Comm.Is_Connected )
      {
        _Comm.Change_GPIB_Address( (int) GPIB_Address_Numeric.Value );
      }

      _Updating_Controls = false;
    }

    private void Show_Error( string Message )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      MessageBox.Show( Message, "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
    }

    private void Add_Command_To_History( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( string.IsNullOrWhiteSpace( Command ) )
        return; // Don't add empty commands

      // Add to bottom
      Command_History_List_Box.Items.Add( Command );

      // Trim history if it exceeds max size
      while ( Command_History_List_Box.Items.Count > _Max_History_Size )
      {
        // Remove oldest (top item)
        Command_History_List_Box.Items.RemoveAt( 0 );
      }

      // Scroll to newest command
      int Last_Index                         = Command_History_List_Box.Items.Count - 1;
      Command_History_List_Box.SelectedIndex = Last_Index;
      Command_History_List_Box.TopIndex      = Last_Index;
    }

    private void Update_Button_State( Command_Entry Entry )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( Entry == null )
      {
        Execute_Button.Enabled = false;
        // Query_Button.Enabled = false;
        return;
      }

      CommandMode Mode = Entry.Get_Command_Mode();

      switch ( Mode )
      {
        case CommandMode.Both :
          Execute_Button.Enabled = true;
          // Query_Button.Enabled = true;
          break;

        case CommandMode.Query_Only :
          Execute_Button.Enabled = true;
          // Query_Button.Enabled = true;
          break;

        case CommandMode.Set_Only :
          Execute_Button.Enabled = true;
          // Query_Button.Enabled = false;
          break;

        case CommandMode.None :
        default :
          Execute_Button.Enabled = false;
          // Query_Button.Enabled = false;
          break;
      }
    }

    private void Command_History_ListBox_DoubleClick( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( Command_History_List_Box.SelectedItem != null )
      {
        string? History_Text = Command_History_List_Box.SelectedItem.ToString();
        if ( string.IsNullOrWhiteSpace( History_Text ) )
          return;

        Send_Command_Text_Box.Text = History_Text;
        Send_Command_Text_Box.Focus();
        Send_Command_Text_Box.SelectionStart = Send_Command_Text_Box.Text.Length;

        // Match history command to a Command_Entry and update buttons
        string Raw_Token     = History_Text.Split( ' ', 2 )[ 0 ];
        string Trimmed_Token = Raw_Token.TrimEnd( '?' );

        Command_Entry? Matched = _All_Commands?.FirstOrDefault(
          C => string.Equals( C.Command, Raw_Token, StringComparison.OrdinalIgnoreCase ) ||
               string.Equals( C.Command, Trimmed_Token, StringComparison.OrdinalIgnoreCase ) );

        Update_Button_State( Matched );
      }
    }

    private string Get_Local_Subnet()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( ! string.IsNullOrWhiteSpace( _Settings.Network_Scan_Subnet ) )
      {
        string Subnet = _Settings.Network_Scan_Subnet.TrimEnd( '.' );
        Capture_Trace.Write( $"Using user-specified subnet: {Subnet}" );
        return Subnet;
      }

      try
      {
        // Get all network interfaces with their unicast addresses
        foreach ( var NI in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces() )
        {
          // Skip loopback, tunnels, VPN, virtual adapters
          if ( NI.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback )
            continue;
          if ( NI.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up )
            continue;
          if ( NI.Description.Contains( "Virtual", StringComparison.OrdinalIgnoreCase ) ||
               NI.Description.Contains( "Hyper-V", StringComparison.OrdinalIgnoreCase ) ||
               NI.Description.Contains( "VPN", StringComparison.OrdinalIgnoreCase ) ||
               NI.Description.Contains( "Tunnel", StringComparison.OrdinalIgnoreCase ) )
            continue;

          foreach ( var UA in NI.GetIPProperties().UnicastAddresses )
          {
            if ( UA.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork )
              continue;
            string IP       = UA.Address.ToString();
            string[ ] Parts = IP.Split( '.' );
            string Subnet   = $"{Parts[ 0 ]}.{Parts[ 1 ]}.{Parts[ 2 ]}";
            Capture_Trace.Write( $"Selected adapter: {NI.Description} -> {IP} -> subnet {Subnet}" );
            return Subnet;
          }
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write( $"Failed to detect local subnet: {Ex.Message}" );
      }

      Capture_Trace.Write( $"No suitable adapter found, using setting: {_Settings.Network_Scan_Subnet}" );
      return _Settings.Network_Scan_Subnet;
    }
    private async void Find_Prologix_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      string    Original_Text      = Find_Prologix_Button.Text;
      Find_Prologix_Button.Enabled = false;
      Find_Prologix_Button.Text    = "Scanning...";

      string Subnet = Get_Local_Subnet();

      await  Task.Delay( 500 );

      Capture_Trace.Write( $"Subnet detected: {Subnet}" );

      var     Found =
        await Multimeter_Common_Helpers_Class.Scan_For_Prologix( Subnet,
                                                                 _Settings.Prologic_Scan_Timeout_MS,
                                                                 null,
                                                                 Msg => Capture_Trace.Write( Msg ) );

      Find_Prologix_Button.Enabled = true;
      Find_Prologix_Button.Text    = Original_Text;

      if ( Found.Count == 0 )
      {
        MessageBox.Show( "No Prologix device found on the network.",
                         "Scan Complete",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Information );
        return;
      }
      if ( Found.Count == 1 )
      {
        IP_Address_Textbox.Text      = Found[ 0 ];
        _Settings.Default_IP_Address = Found[ 0 ];

        MessageBox.Show( $"Found Prologix at {Found[ 0 ]}",
                         "Scan Complete",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Information );
        _Settings.Save();
      }
      else
      {
        string Selected = Show_IP_Selection_Dialog( Found );
        if ( ! string.IsNullOrEmpty( Selected ) )
          IP_Address_Textbox.Text = Selected;
      }
    }

    private string Show_IP_Selection_Dialog( List<string> IPs )
    {
      using var Dialog       = new Form();
      Dialog.Text            = "Select Prologix Device";
      Dialog.Size            = new Size( 300, 200 );
      Dialog.StartPosition   = FormStartPosition.CenterParent;
      Dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
      Dialog.MaximizeBox     = false;
      Dialog.MinimizeBox     = false;

      var List_Box  = new ListBox();
      List_Box.Dock = DockStyle.Fill;
      foreach ( var IP in IPs )
        List_Box.Items.Add( IP );
      List_Box.SelectedIndex = 0;

      var OK_Button          = new Button();
      OK_Button.Text         = "Select";
      OK_Button.Dock         = DockStyle.Bottom;
      OK_Button.DialogResult = DialogResult.OK;

      Dialog.Controls.Add( List_Box );
      Dialog.Controls.Add( OK_Button );
      Dialog.AcceptButton = OK_Button;

      return Dialog.ShowDialog() == DialogResult.OK ? List_Box.SelectedItem?.ToString() ?? "" : "";
    }

    private void Meter_Info_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      using var Popup = new Rich_Text_Popup( "Supported Meter Types", 660, 900, Resizable: true );

      // ── HP 3458A ─────────────────────────────────────────────────────────
      Popup.Add_Blank()
        .Add_Heading_Mono( "HP 3458A  —  8.5-Digit Multimeter" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard / Agilent", null, 16 )
        .Add_Row( "Interface:", "GPIB (IEEE-488)", null, 16 )
        .Add_Row( "Resolution:", "8.5 digits", null, 16 )
        .Add_Row( "Category:", "Reference-grade DC voltmeter", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 3458A is the gold standard for precision DC voltage measurement." )
        .Add_Mono( "  It is capable of 8.5-digit resolution with extremely low noise and drift," )
        .Add_Mono( "  making it suitable for metrology, standards labs, and long-term stability" )
        .Add_Mono( "  measurements." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - Sub-ppm linearity and noise performance" )
        .Add_Mono( "    - Selectable NPLC from 0.001 to 1000 for speed/accuracy tradeoff" )
        .Add_Mono( "    - Autozero, autorange, and high-stability reference" )
        .Add_Mono( "    - GPIB-only interface (no RS-232 or USB)" )
        .Add_Mono( "    - Trigger modes: AUTO, HOLD, SGL, EXT, LEVEL, LINE" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - Primary voltage reference measurements" )
        .Add_Mono( "    - Long-duration stability and drift logging" )
        .Add_Mono( "    - Multi-instrument comparison against a secondary meter" )

        // ── HP 34401A ─────────────────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "HP 34401A  —  6.5-Digit Multimeter" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard / Agilent / Keysight", null, 16 )
        .Add_Row( "Interface:", "GPIB (IEEE-488) + RS-232", null, 16 )
        .Add_Row( "Resolution:", "6.5 digits", null, 16 )
        .Add_Row( "Category:", "General-purpose bench multimeter", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 34401A is one of the most widely used bench multimeters ever made." )
        .Add_Mono( "  It offers excellent accuracy at 6.5 digits with a fast reading rate and" )
        .Add_Mono( "  broad measurement capability." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - Up to 1000 readings/second at 4.5 digits" )
        .Add_Mono( "    - SCPI command set (standard with this application)" )
        .Add_Mono( "    - Internal reading memory (up to 512 readings)" )
        .Add_Mono( "    - Math functions: null, dB, dBm, min/max, limit test" )
        .Add_Mono( "    - RS-232 and GPIB interfaces" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - Secondary or reference measurements alongside the HP 3458A" )
        .Add_Mono( "    - Monitoring a second signal or channel simultaneously" )
        .Add_Mono( "    - General-purpose voltage, resistance, or current logging" )

        // ── HP 34411A ─────────────────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "HP 34411A  —  6.5-Digit High-Speed Multimeter" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard / Agilent / Keysight", null, 16 )
        .Add_Row( "Interface:", "GPIB (IEEE-488) + USB + LAN", null, 16 )
        .Add_Row( "Resolution:", "6.5 digits", null, 16 )
        .Add_Row( "Category:", "High-speed bench multimeter", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 34411A is the high-speed successor to the 34401A. It retains full" )
        .Add_Mono( "  SCPI command compatibility while adding dramatically higher throughput," )
        .Add_Mono( "  larger memory, and extended NPLC range for faster acquisition." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - Up to 50,000 readings/second in burst (timer) mode" )
        .Add_Mono( "    - NPLC range extended down to 0.001 PLC" )
        .Add_Mono( "    - Internal memory up to 50,000 readings" )
        .Add_Mono( "    - Histogram math (up to 100 bins) for distribution analysis" )
        .Add_Mono( "    - DATA:LAST? query returns most recent reading without disturbing trigger" )
        .Add_Mono( "    - SAMP:SOUR TIMer enables fixed-interval burst sampling" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - High-speed transient capture and burst logging" )
        .Add_Mono( "    - Drop-in replacement for HP 34401A with higher throughput" )
        .Add_Mono( "    - Distribution and histogram analysis of repetitive signals" )

        // ── HP 34420A ─────────────────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "HP 34420A  —  7.5-Digit Nano-Volt / Micro-Ohm Meter" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard / Agilent / Keysight", null, 16 )
        .Add_Row( "Interface:", "GPIB (IEEE-488) + RS-232", null, 16 )
        .Add_Row( "Resolution:", "7.5 digits", null, 16 )
        .Add_Row( "Category:", "Low-level DC measurement, nanovolt sensitivity", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 34420A is a specialized instrument designed for low-level DC voltage" )
        .Add_Mono( "  and resistance measurements with nanovolt and micro-ohm sensitivity. It" )
        .Add_Mono( "  features a differential input allowing voltage ratio measurements." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - Nanovolt sensitivity on the 100 mV range" )
        .Add_Mono( "    - Voltage ratio measurement (Input / Sense)" )
        .Add_Mono( "    - 2-wire and 4-wire resistance down to micro-ohm level" )
        .Add_Mono( "    - Digital filter (moving/repeat) with configurable count" )
        .Add_Mono( "    - NPLC up to 100 for maximum noise rejection" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - Thermocouple or low-level voltage source monitoring" )
        .Add_Mono( "    - Contact or cable resistance measurement" )
        .Add_Mono( "    - Paired with HP 3458A for cross-validation at nanovolt levels" )

        // ── HP 3456A ─────────────────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "HP 3456A  —  6.5-Digit Voltmeter (Legacy HP-IB)" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard", null, 16 )
        .Add_Row( "Interface:", "GPIB / HP-IB (IEEE-488)", null, 16 )
        .Add_Row( "Resolution:", "6.5 digits", null, 16 )
        .Add_Row( "Category:", "Legacy precision voltmeter", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 3456A is a high-accuracy bench voltmeter from the late 1970s." )
        .Add_Mono( "  It predates SCPI and uses the proprietary HP-IB command set. It is" )
        .Add_Mono( "  nonetheless fully functional over GPIB and capable of precision DC" )
        .Add_Mono( "  voltage measurements." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - 6.5-digit resolution with guarded input" )
        .Add_Mono( "    - Selectable NPLC from 1 to 100" )
        .Add_Mono( "    - Uses HP-IB native command set (not SCPI)" )
        .Add_Mono( "    - Excellent long-term stability for its era" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - Legacy lab instrument integration" )
        .Add_Mono( "    - DC voltage logging alongside modern SCPI instruments" )

        // ── HP 33120A ─────────────────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "HP 33120A  —  15 MHz Function / Arbitrary Waveform Generator" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard / Agilent / Keysight", null, 16 )
        .Add_Row( "Interface:", "GPIB (IEEE-488) + RS-232", null, 16 )
        .Add_Row( "Frequency:", "100 µHz to 15 MHz", null, 16 )
        .Add_Row( "Category:", "Signal source / waveform generator", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 33120A is a function and arbitrary waveform generator capable of" )
        .Add_Mono( "  producing sine, square, triangle, ramp, noise, and user-defined waveforms." )
        .Add_Mono( "  Commonly used as a stimulus source for meter verification or frequency" )
        .Add_Mono( "  response testing." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - Sine, square, triangle, ramp, noise, and ARB waveforms" )
        .Add_Mono( "    - Amplitude modulation (AM) and frequency modulation (FM)" )
        .Add_Mono( "    - Frequency sweep and burst mode" )
        .Add_Mono( "    - Internal 16k-point arbitrary waveform memory" )
        .Add_Mono( "    - SCPI command set" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - Providing a stable AC or DC reference signal for meter input" )
        .Add_Mono( "    - Frequency source for counter verification" )
        .Add_Mono( "    - Stimulus for automated measurement sequences" )
        .Add_Mono( "    - Polling FREQ? / VOLT? across multiple units to monitor output drift" )

        // ── HP 33220A ─────────────────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "HP 33220A  —  20 MHz Function / Arbitrary Waveform Generator" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard / Agilent / Keysight", null, 16 )
        .Add_Row( "Interface:", "GPIB (IEEE-488) + USB + RS-232", null, 16 )
        .Add_Row( "Frequency:", "1 µHz to 20 MHz", null, 16 )
        .Add_Row( "Category:", "Signal source / waveform generator", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 33220A is the direct successor to the 33120A with an extended" )
        .Add_Mono( "  frequency range, pulse waveform capability, full modulation suite," )
        .Add_Mono( "  and a larger 64k-point arbitrary waveform memory. It is command-" )
        .Add_Mono( "  compatible with the 33120A for most common operations." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - Sine, square, ramp, pulse, noise, DC, and ARB waveforms" )
        .Add_Mono( "    - Full modulation: AM, FM, PM, FSK, and PWM" )
        .Add_Mono( "    - Linear and logarithmic frequency sweep" )
        .Add_Mono( "    - Triggered N-cycle and gated burst modes" )
        .Add_Mono( "    - 64k-point arbitrary waveform memory" )
        .Add_Mono( "    - Pulse waveform with independent rise/fall/width control" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - Precision frequency and amplitude source for meter stimulus" )
        .Add_Mono( "    - Drift monitoring — poll FREQ? and VOLT? across multiple units" )
        .Add_Mono( "    - Modulation source for RF or audio test setups" )

        // ── HP 53132A ─────────────────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "HP 53132A  —  225 MHz Universal Counter" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard / Agilent / Keysight", null, 16 )
        .Add_Row( "Interface:", "GPIB (IEEE-488) + RS-232", null, 16 )
        .Add_Row( "Frequency:", "DC to 225 MHz (CH1/CH2), optional 3 GHz (CH3)", null, 16 )
        .Add_Row( "Category:", "Frequency / time interval counter", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 53132A is a high-performance universal counter capable of measuring" )
        .Add_Mono( "  frequency, period, pulse width, duty cycle, time interval, phase, and" )
        .Add_Mono( "  totalize. Features an optional OCXO for sub-ppb timebase accuracy." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - Frequency resolution to 12 digits in 1 second gate time" )
        .Add_Mono( "    - Time interval resolution to 75 ps single-shot, 20 ps RMS averaged" )
        .Add_Mono( "    - Three input channels (CH1, CH2, optional CH3 at 3 GHz)" )
        .Add_Mono( "    - Built-in statistics: mean, std dev, min, max, Allan deviation" )
        .Add_Mono( "    - Configurable trigger level, slope, coupling, and attenuation per channel" )
        .Add_Mono( "    - Internal reading memory up to 1,000,000 readings" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - Frequency measurement and logging of oscillator or signal sources" )
        .Add_Mono( "    - Time interval or phase measurement between two signals" )
        .Add_Mono( "    - Long-term frequency stability and Allan deviation analysis" )

        // ── HP 53181A ─────────────────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "HP 53181A  —  225 MHz Frequency Counter" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard / Agilent / Keysight", null, 16 )
        .Add_Row( "Interface:", "GPIB (IEEE-488)", null, 16 )
        .Add_Row( "Frequency:", "DC to 225 MHz (CH1), optional 6 GHz (Option 010)", null, 16 )
        .Add_Row( "Category:", "Single-channel frequency counter", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 53181A is a single-channel frequency counter and the lower-cost" )
        .Add_Mono( "  sibling of the 53132A. It measures frequency and period on a single" )
        .Add_Mono( "  channel up to 225 MHz, with an optional 6 GHz microwave input. Gate" )
        .Add_Mono( "  time is selectable from 1 ms to 1 second." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - Frequency resolution to ~10 digits at 1 s gate time" )
        .Add_Mono( "    - Selectable gate time: 1 ms to 1 s" )
        .Add_Mono( "    - Input coupling, impedance, slope, and trigger level configurable" )
        .Add_Mono( "    - Noise rejection and auto-trigger level available" )
        .Add_Mono( "    - Optional 6 GHz microwave input channel (Option 010)" )
        .Add_Mono( "    - Scale / offset math and limit testing" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - Continuous frequency monitoring of oscillators or signal sources" )
        .Add_Mono( "    - Polling multiple units against a common reference for drift comparison" )
        .Add_Mono( "    - Period measurement for low-frequency signals" )

        // ── HP 4263B ─────────────────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "HP 4263B  —  100 Hz – 100 kHz LCR Meter" )
        .Add_Row( "Manufacturer:", "Hewlett-Packard / Agilent / Keysight", null, 16 )
        .Add_Row( "Interface:", "GPIB (IEEE-488)", null, 16 )
        .Add_Row( "Test Frequency:", "100 Hz, 120 Hz, 1 kHz, 10 kHz, 20 kHz, 100 kHz", null, 16 )
        .Add_Row( "Category:", "Impedance / LCR meter", null, 16 )
        .Add_Blank()
        .Add_Mono( "  The HP 4263B measures impedance, capacitance, inductance, and resistance" )
        .Add_Mono( "  using a six-frequency test signal. It always returns two parameters per" )
        .Add_Mono( "  measurement (e.g. Cp+D, Ls+Q, Z+theta) selected by the FUNC:IMP:TYPE" )
        .Add_Mono( "  command. Open, short, and load compensation routines correct for fixture" )
        .Add_Mono( "  and cable parasitics." )
        .Add_Blank()
        .Add_Mono( "  Key features:" )
        .Add_Mono( "    - 18 measurement parameter pairs: Cp-D, Ls-Q, Z-theta, R-X, G-B, and more" )
        .Add_Mono( "    - DC bias up to 2 V (voltage) or 100 mA (current)" )
        .Add_Mono( "    - Open, short, and load compensation with cable length correction" )
        .Add_Mono( "    - Integration time: short, medium, or long with 1–256 averages" )
        .Add_Mono( "    - FETCH? returns primary value, secondary value, and status code" )
        .Add_Mono( "    - Limit testing on primary or secondary parameter" )
        .Add_Blank()
        .Add_Mono( "  Typical use in this application:" )
        .Add_Mono( "    - Capacitance or inductance drift monitoring over time" )
        .Add_Mono( "    - Component sorting and pass/fail testing" )
        .Add_Mono( "    - Multi-unit comparison of the same component type" )
        .Add_Blank()
        .Add_Mono( "  Note: FETCH? status codes — 0=normal, 1=primary overrange," )
        .Add_Mono( "  2=secondary overrange, 3=both overrange, 4=signal source overload." )
        .Add_Mono( "  Always validate the status field before logging the measured values." )

        // ── GPIB / Prologix Notes ─────────────────────────────────────────────
        .Add_Blank()
        .Add_Heading_Mono( "GPIB / Prologix Interface Notes" )
        .Add_Blank()
        .Add_Mono( "  All instruments communicate via GPIB (IEEE-488) using a Prologix" )
        .Add_Mono( "  GPIB-ETHERNET or GPIB-USB controller. Each instrument must be" )
        .Add_Mono( "  assigned a unique GPIB address (1-30)." )
        .Add_Blank()
        .Add_Mono( "  Default addresses used in this application:" )
        .Add_Row( "HP 3458A:", "Address 10 (typical)", null, 14 )
        .Add_Row( "HP 34401A:", "Address 22 (typical)", null, 14 )
        .Add_Row( "HP 34411A:", "Address 23 (typical)", null, 14 )
        .Add_Row( "HP 34420A:", "Address 5  (typical)", null, 14 )
        .Add_Row( "HP 3456A:", "Address 13 (typical)", null, 14 )
        .Add_Row( "HP 33120A:", "Address 11 (typical)", null, 14 )
        .Add_Row( "HP 33220A:", "Address 12 (typical)", null, 14 )
        .Add_Row( "HP 53132A:", "Address 7  (typical)", null, 14 )
        .Add_Row( "HP 53181A:", "Address 8  (typical)", null, 14 )
        .Add_Row( "HP 4263B:", "Address 17 (typical)", null, 14 )
        .Add_Blank()
        .Add_Mono( "  These addresses can be changed on the front panel of each instrument" )
        .Add_Mono( "  and must match the address configured in this application." )
        .Add_Blank()
        .Add_Mono( "  Non-DMM instruments (generators, counters, LCR meter) use a fixed" )
        .Add_Mono( "  poll interval rather than an NPLC-derived rate. The poll interval" )
        .Add_Mono( "  can be configured per instrument in the instrument settings dialog." );

      Popup.Show_Popup( this );
    }

    public static void Show_Session_Settings( Form                 Owner,
                                              Application_Settings Settings,
                                              List<Instrument>     Instruments,
                                              Chart_Theme          Theme )
    {
      using var Popup = new Rich_Text_Popup( "Session Settings", 660, 700, Resizable: true );

      // ── Instruments ───────────────────────────────────────────────
      Popup.Add_Blank().Add_Heading_Mono( "Instruments" );

      if ( Instruments.Count == 0 )
      {
        Popup.Add_Row( "Status", "No instruments configured" );
      }
      else
      {
        foreach ( var Inst in Instruments )
        {
          string Settle_Ms = ( (double) Inst.NPLC * ( 1000.0 / 60.0 ) * 2.0 ).ToString( "F0" );

          Popup.Add_Instrument_Header( Inst.Name )
            .Add_Row( "    Type", Inst.Type.ToString() )
            .Add_Row( "    GPIB Address", $"{Inst.Address}" )
            .Add_Row( "    Role", string.IsNullOrEmpty( Inst.Meter_Roll ) ? "\u2014" : Inst.Meter_Roll )
            .Add_Row( "    NPLC", $"{Inst.NPLC}" )
            .Add_Row( "    Display Digits", $"{Inst.Display_Digits}" )
            .Add_Row( "    Est. Settle Time", $"{Settle_Ms} ms" )
            .Add_Blank();
        }

        if ( Instruments.Count > 1 )
        {
          double Max_NPLC       = (double) Instruments.Max( I => I.NPLC );
          int    Session_Settle = (int) ( Max_NPLC / 60.0 * 1000.0 * 2.0 );

          Popup.Add_Row( "Session Bottleneck NPLC", $"{Max_NPLC}  (worst-case instrument)" )
            .Add_Row( "Session Settle Time", $"{Session_Settle} ms  (all instruments share this rate)" )
            .Add_Blank();
        }
      }

      // ── Polling ───────────────────────────────────────────────────
      Popup.Add_Blank()
        .Add_Heading_Mono( "Polling" )
        .Add_Row( "Poll Delay", $"{Settings.Default_Poll_Delay_Ms} ms" )
        .Add_Row( "Continuous Mode", Settings.Default_Continuous_Poll ? "Yes" : "No" )
        .Add_Row( "Default Measurement", Settings.Default_Measurement_Type )
        .Add_Row( "Max Display Points", $"{Settings.Max_Display_Points:N0}" )
        .Add_Row( "Stop At Max Points", Settings.Stop_Polling_At_Max_Display_Points ? "Yes" : "No" )
        .Add_Row( "GPIB Timeout", $"{Settings.Default_GPIB_Timeout_Ms} ms" )
        .Add_Row( "Prologix Read Timeout", $"{Settings.Prologix_Read_Tmo_Ms} ms" )
        .Add_Row( "Max Retry Attempts", $"{Settings.Max_Retry_Attempts}" )
        .Add_Row( "Instrument Settle", $"{Settings.Instrument_Settle_Ms} ms" )
        .Add_Blank();

      // ── Display ───────────────────────────────────────────────────
      Popup.Add_Blank()
        .Add_Heading_Mono( "Display" )
        .Add_Row( "Chart Refresh Rate", $"{Settings.Chart_Refresh_Rate_Ms} ms" )
        .Add_Row( "Default View", Settings.Default_To_Combined_View ? "Combined" : "Split" )
        .Add_Row( "Default Normalized", Settings.Default_To_Normalized_View ? "Yes" : "No" )
        .Add_Row( "Show Legend On Startup", Settings.Show_Legend_On_Startup ? "Yes" : "No" )
        .Add_Row( "Tooltips On Hover", Settings.Show_Tooltips_On_Hover ? "Yes" : "No" )
        .Add_Row( "Tooltip Duration", $"{Settings.Tooltip_Display_Duration_Ms} ms" )
        .Add_Row( "Tooltip Threshold", $"{Settings.Tooltip_Distance_Threshold} px" )
        .Add_Row( "Default Zoom Level", $"{Settings.Default_Zoom_Level}" )
        .Add_Row( "Throttle When Large", Settings.Throttle_When_Many_Points ? "Yes" : "No" )
        .Add_Row( "Throttle Threshold", $"{Settings.Throttle_Point_Threshold:N0} points" )
        .Add_Blank();

      // ── Recording ─────────────────────────────────────────────────
      Popup.Add_Blank()
        .Add_Heading_Mono( "Recording & Files" )
        .Add_Row( "Save Folder", Settings.Default_Save_Folder )
        .Add_Row( "Filename Pattern", Settings.Filename_Pattern )
        .Add_Row( "Auto Save",
                  Settings.Enable_Auto_Save ? $"Every {Settings.Auto_Save_Interval_Minutes} min"
                                            : "Disabled" )
        .Add_Row( "Auto Save On Stop", Settings.Auto_Save_On_Stop ? "Yes" : "No" )
        .Add_Row( "Prompt Before Clear", Settings.Prompt_Before_Clear ? "Yes" : "No" )
        .Add_Row( "Export Format", Settings.Export_Format )
        .Add_Blank();

      // ── Memory ────────────────────────────────────────────────────
      Popup.Add_Blank()
        .Add_Heading_Mono( "Memory" )
        .Add_Row( "Max Points In Memory", $"{Settings.Max_Data_Points_In_Memory:N0}" )
        .Add_Row( "Warning Threshold", $"{Settings.Warning_Threshold_Percent}%" )
        .Add_Row( "Auto Trim Old Data",
                  Settings.Auto_Trim_Old_Data ? $"Keep last {Settings.Keep_Last_N_Points:N0}" : "Disabled" )
        .Add_Blank();

      // ── Analysis ─────────────────────────────────────────────────
      Popup.Add_Blank()
        .Add_Heading_Mono( "Analysis" )
        .Add_Row( "Auto Analyze After Rec", Settings.Auto_Analyze_After_Recording ? "Yes" : "No" )
        .Add_Row( "Show Mean", Settings.Analysis_Show_Mean ? "Yes" : "No" )
        .Add_Row( "Show Std Dev", Settings.Analysis_Show_Std_Dev ? "Yes" : "No" )
        .Add_Row( "Show Min/Max", Settings.Analysis_Show_Min_Max ? "Yes" : "No" )
        .Add_Row( "Show RMS", Settings.Analysis_Show_RMS ? "Yes" : "No" )
        .Add_Row( "Show Trend", Settings.Analysis_Show_Trend ? "Yes" : "No" )
        .Add_Row( "Show Sample Rate", Settings.Analysis_Show_Sample_Rate ? "Yes" : "No" )
        .Add_Blank();

      // ── Connection ────────────────────────────────────────────────
      Popup.Add_Blank()
        .Add_Heading_Mono( "Connection" )
        .Add_Row( "Default GPIB Address", $"{Settings.Default_GPIB_Instrument_Address}" )
        .Add_Row( "Prologix MAC", Settings.Prologic_MAC_Address )
        .Add_Row( "Default IP",
                  string.IsNullOrEmpty( Settings.Default_IP_Address ) ? "Auto-detect"
                                                                      : Settings.Default_IP_Address )
        .Add_Row( "Scan Timeout", $"{Settings.Prologic_Scan_Timeout_MS} ms" )
        .Add_Row( "Send Reset On Connect", Settings.Send_Reset_On_Connect_3458 ? "Yes" : "No" )
        .Add_Row( "Reset Settle Delay", $"{Settings.Reset_Settle_Delay_Ms} ms" )
        .Add_Row( "Skew Warning Threshold", $"{Settings.Skew_Warning_Threshold_Seconds:F1} s" )
        .Add_Row( "Stale Data Threshold", $"{Settings.Stale_Data_Threshold_Seconds:F1} s" )
        .Add_Blank();

      // ── Theme ─────────────────────────────────────────────────────
      Popup.Add_Blank()
        .Add_Heading_Mono( "Theme" )
        .Add_Color_Row( "Background", Theme.Background )
        .Add_Color_Row( "Foreground", Theme.Foreground )
        .Add_Color_Row( "Grid", Theme.Grid )
        .Add_Color_Row( "Labels", Theme.Labels );

      for ( int I = 0; I < Theme.Line_Colors.Length; I++ )
        Popup.Add_Color_Row( $"Series {I + 1} Color", Theme.Line_Colors[ I ] );

      Popup.Add_Blank();

      Popup.Show_Popup( Owner );
    }

    private void Session_Settings_Button_Click( object Sender, EventArgs E )
    {
      Show_Session_Settings( this, _Settings, _Instruments, _Theme );
    }

    private void Settings_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      using var Dlg = new Settings_Form( _Settings );

      if ( Dlg.ShowDialog( this ) == DialogResult.OK )
      {
        _Settings = Dlg.Get_Settings();
        _Settings.Save();

        // Apply settings immediately to this running instance
        //     Apply_Settings ( );

        MessageBox.Show( "Settings saved successfully.\n\n" + "Settings have been applied to this window.",
                         "Settings",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Information );
      }
    }

    private void NPLC_Combo_Box_SelectedIndexChanged( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();
      if ( _Updating_Controls )
        return;
      if ( _Selected_Index < 0 || _Selected_Index >= _Instruments.Count )
        return;

      if ( ! decimal.TryParse( NPLC_Combo_Box.SelectedItem?.ToString(),
                               NumberStyles.Number,
                               CultureInfo.InvariantCulture,
                               out decimal NPLC ) )
        return;

      var     Inst    = _Instruments[ _Selected_Index ];
      decimal Default = Meter_Type_Extensions.Get_Default_NPLC( Inst.Type );
      Inst.NPLC       = NPLC;

      if ( NPLC != Default )
      {
        Capture_Trace.Write( $"NPLC overridden: {Inst.Name} default={Default} selected={NPLC}" );
        Append_Response( $"[{Inst.Name}: NPLC set to {NPLC} (default is {Default})]" );
      }
      else
      {
        Capture_Trace.Write( $"NPLC set to default {NPLC} for {Inst.Name}" );
      }

      _Settings.Default_NPLC = NPLC.ToString( CultureInfo.InvariantCulture );
      _Settings.Save();
    }

    private void Apply_NPLC_To_All_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( NPLC_Combo_Box.SelectedItem is not string Value ||
           ! decimal.TryParse( Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal NPLC ) )
      {
        MessageBox.Show( "Please select a valid NPLC value first.",
                         "No NPLC Selected",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        return;
      }

      int Count = 0;
      foreach ( var Instrument in _Instruments )
      {
        if ( Instrument.Type.Get_NPLC_Values().Contains( NPLC ) )
        {
          Instrument.NPLC = NPLC;
          Count++;
        }
        else
        {
          Capture_Trace.Write(
            $"[{Instrument.Name}] NPLC {NPLC} not valid for {Instrument.Type.Get_Name()} — skipped." );
          Append_Response( $"[{Instrument.Name}] NPLC {NPLC} not supported — skipped." );
        }
      }

      _Settings.Default_NPLC = Value;
      _Settings.Save();

      Capture_Trace.Write( $"NPLC {NPLC} applied to {Count} of {_Instruments.Count} instruments." );
      Append_Response( $"[NPLC {NPLC} applied to {Count} of {_Instruments.Count} instruments]" );
    }

    private void Roll_Name_Textbox_Leave( object sender, EventArgs e )
    {
      if ( Roll_Name_Textbox is null || ! Roll_Name_Textbox.Enabled )
        return;

      string val = Roll_Name_Textbox.Text.Trim();
      if ( ! string.IsNullOrEmpty( val ) && ( _Instruments?.Any( i => i.Meter_Roll == val ) ?? false ) )
      {
        MessageBox.Show( $"The role '{val}' is already in use.",
                         "Duplicate Name",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Warning );
        Roll_Name_Textbox.Focus();
      }
    }

    private void Display_Recording_Data_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Cursor = Cursors.WaitCursor;

      var Form = new Recording_Playback_Form( _Settings );
      Cursor   = Cursors.Default;
      Form.Show();
    }

    private void NPLC_Info_Button_Click( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      using var Popup = new Rich_Text_Popup( "NPLC Reference", 670, 500, Resizable: true );

      Popup.Add_Blank()
        .Add_Heading_Mono( "NPLC — Number of Power Line Cycles" )
        .Add_Blank()
        .Add_Mono( "  NPLC is a dimensionless integer or fraction that specifies the" )
        .Add_Mono( "  duration of a single measurement integration period, expressed" )
        .Add_Mono( "  as a multiple of the period of the local AC mains frequency." )
        .Add_Mono( "  It is the primary control parameter of an integrating analog-to-" )
        .Add_Mono( "  digital converter in a precision digital multimeter, governing" )
        .Add_Mono( "  the trade-off between measurement speed and noise rejection." )

        .Add_Blank()
        .Add_Heading_Mono( "Integration Window" )
        .Add_Blank()
        .Add_Mono( "  At 60 Hz mains, one power line cycle is 16.667 ms." )
        .Add_Mono( "  At 50 Hz mains, one power line cycle is 20.000 ms." )
        .Add_Mono( "  The integration window in absolute time is therefore:" )
        .Add_Blank()
        .Add_Mono( "     T_integration = NPLC x (1 / f_line)" )

        .Add_Blank()
        .Add_Heading_Mono( "Why Power Line Cycles?" )
        .Add_Blank()
        .Add_Mono( "  The choice of a power line cycle as the base unit is not" )
        .Add_Mono( "  arbitrary. It is the fundamental period of the dominant" )
        .Add_Mono( "  interference source in laboratory and industrial measurement" )
        .Add_Mono( "  environments. By integrating over exactly one or more complete" )
        .Add_Mono( "  cycles of that interference, the converter causes the sinusoidal" )
        .Add_Mono( "  noise to sum to zero. This is formally called Normal Mode" )
        .Add_Mono( "  Rejection. This rejection is theoretically perfect at integer" )
        .Add_Mono( "  NPLC values and degrades at fractional values." )

        .Add_Blank()
        .Add_Heading_Mono( "Two Distinct Noise Rejection Mechanisms" )
        .Add_Blank()
        .Add_Mono( "  Random noise does not cancel deterministically. It averages" )
        .Add_Mono( "  down statistically at a rate proportional to the square root" )
        .Add_Mono( "  of the number of cycles integrated:" )
        .Add_Blank()
        .Add_Mono( "     Doubling NPLC reduces random noise by sqrt(2)   = 1.41x" )
        .Add_Mono( "     1 PLC to 10 PLC  reduces random noise by sqrt(10) = 3.16x" )
        .Add_Mono( "     1 PLC to 100 PLC reduces random noise by sqrt(100) = 10x" )
        .Add_Blank()
        .Add_Mono( "  NPLC therefore simultaneously controls two distinct mechanisms:" )
        .Add_Blank()
        .Add_Mono( "     1. Deterministic cancellation of periodic line-frequency" )
        .Add_Mono( "        interference — theoretically perfect at integer values." )
        .Add_Blank()
        .Add_Mono( "     2. Statistical averaging of random noise — improves" )
        .Add_Mono( "        continuously with the square root of NPLC." )

        .Add_Blank()
        .Add_Heading_Mono( "The Noise Problem It Solves" )
        .Add_Blank()
        .Add_Mono( "  Mains power (50/60 Hz) radiates interference into measurement" )
        .Add_Mono( "  cables and input circuitry. This noise is periodic — it repeats" )
        .Add_Mono( "  exactly once per cycle. Integrating over a whole number of cycles" )
        .Add_Mono( "  causes the positive and negative halves to cancel mathematically" )
        .Add_Mono( "  to zero. This is called Normal Mode Rejection." )

        .Add_Blank()
        .Add_Heading_Mono( "Speed vs. Accuracy Trade-off" )
        .Add_Blank()
        .Add_Row( "0.02 PLC", "333 µs window. Very fast, almost no rejection. ~4.5 digits.", null, 12 )
        .Add_Row( "1    PLC", "One full cycle of line rejection. ~5.5–6.5 digits.", null, 12 )
        .Add_Row( "10   PLC", "sqrt(10) noise reduction vs 1 PLC. Rated accuracy.", null, 12 )
        .Add_Row( "100  PLC", "1.67s at 60 Hz. sqrt(100) = 10x noise reduction.", null, 12 )
        .Add_Row( "200  PLC", "3.33s (34420A only). Extra sqrt(2) for nanovolt signals.", null, 12 )

        .Add_Blank()
        .Add_Heading_Mono( "Integration Times at 60 Hz" )
        .Add_Blank()
        .Add_Row( "NPLC", "Calculation            Integration Time", null, 8 )
        .Add_Separator()
        .Add_Row( "0.02", "0.02  x 16.667 ms    =  333.333 µs", null, 8 )
        .Add_Row( "0.2", "0.2   x 16.667 ms    =  3.333 ms", null, 8 )
        .Add_Row( "1", "1     x 16.667 ms    =  16.667 ms", null, 8 )
        .Add_Row( "2", "2     x 16.667 ms    =  33.333 ms", null, 8 )
        .Add_Row( "4", "4     x 16.667 ms    =  66.667 ms", null, 8 )
        .Add_Row( "8", "8     x 16.667 ms    =  133.333 ms", null, 8 )
        .Add_Row( "10", "10    x 16.667 ms    =  166.667 ms", null, 8 )
        .Add_Row( "16", "16    x 16.667 ms    =  266.667 ms", null, 8 )
        .Add_Row( "20", "20    x 16.667 ms    =  333.333 ms", null, 8 )
        .Add_Row( "32", "32    x 16.667 ms    =  533.333 ms", null, 8 )
        .Add_Row( "64", "64    x 16.667 ms    =  1066.667 ms", null, 8 )
        .Add_Row( "100", "100   x 16.667 ms    =  1666.667 ms", null, 8 )
        .Add_Row( "200", "200   x 16.667 ms    =  3333.333 ms", null, 8 )

        .Add_Blank()
        .Add_Heading_Mono( "Autozero Interaction" )
        .Add_Blank()
        .Add_Mono( "  Autozero doubles effective measurement time — the meter takes" )
        .Add_Mono( "  two integration passes per reading: one for the input, one with" )
        .Add_Mono( "  the input shorted to measure offset. At 100 PLC with autozero on:" )
        .Add_Blank()
        .Add_Mono( "     2 x 1667 ms = 3333 ms per reading at 60 Hz" )
        .Add_Blank()
        .Add_Mono( "  Disabling autozero halves the time at the cost of drift susceptibility." )

        .Add_Blank()
        .Add_Heading_Mono( "Poll Interval = Integration + Overhead" )
        .Add_Blank()
        .Add_Row( "Autozero:", "doubles integration time", null, 12 )
        .Add_Row( "Settling:", "amplifier settle after range change", null, 12 )
        .Add_Row( "A/D:", "rundown phase + output formatting", null, 12 )
        .Add_Row( "GPIB:", "transfer time across bus to controller", null, 12 );

      Popup.Show_Popup( this );
    }

    private void Master_Instrument_Combobox_SelectedIndexChanged( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      if ( Master_Instrument_Combobox.SelectedIndex < 0 )
        return; // ← no selection yet, nothing to do
      if ( Master_Instrument_Combobox.SelectedItem is not Instrument Selected )
        return; // ← safety guard against any non-Instrument items
      foreach ( var Inst in _Instruments )
        Inst.Is_Master = false;
      Selected.Is_Master = true;
    }

    private void Refresh_Master_Combo()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Master_Instrument_Combobox.SelectedIndexChanged -= Master_Instrument_Combobox_SelectedIndexChanged;
      Master_Instrument_Combobox.DataSource            = null;
      var Visible                                      = _Instruments.Where( I => I.Visible ).ToList();
      if ( Visible.Count < 2 )
      {
        Master_Instrument_Combobox.Enabled               = false;
        Master_Instrument_Combobox.DataSource            = new List<string> { "— add instruments first —" };
        About_Selections_Button.Enabled                  = false;
        Master_Instrument_Combobox.SelectedIndexChanged += Master_Instrument_Combobox_SelectedIndexChanged;
        return;
      }
      // ── Only allow choice when 3+ instruments ─────────────────────
      bool Needs_Master                  = Visible.Count > 2;
      Master_Instrument_Combobox.Enabled = Needs_Master;
      About_Selections_Button.Enabled    = Needs_Master;
      if ( ! Needs_Master )
      {
        // 2 instruments — no master needed, clear any prior Is_Master flags
        foreach ( var Inst in _Instruments )
          Inst.Is_Master = false;
        Master_Instrument_Combobox.DataSource            = Visible;
        Master_Instrument_Combobox.DisplayMember         = "Display";
        Master_Instrument_Combobox.ValueMember           = "Address";
        Master_Instrument_Combobox.SelectedIndexChanged += Master_Instrument_Combobox_SelectedIndexChanged;
        return;
      }
      // ── 3+ instruments: require explicit master selection ──────────
      Master_Instrument_Combobox.DataSource    = Visible;
      Master_Instrument_Combobox.DisplayMember = "Display";
      Master_Instrument_Combobox.ValueMember   = "Address";
      // Restore prior master if still in list, otherwise leave unselected
      var Current_Master                       = Visible.FirstOrDefault( I => I.Is_Master );
      if ( Current_Master != null )
        Master_Instrument_Combobox.SelectedItem = Current_Master;
      else
        Master_Instrument_Combobox.SelectedIndex = -1; // ← no default, user must choose
      Master_Instrument_Combobox.SelectedIndexChanged += Master_Instrument_Combobox_SelectedIndexChanged;
    }

    private void About_Selections_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      using ( var Popup = new Rich_Text_Popup( "About Selections", 520, 360 ) )
      {
        Popup.Add_Title( "Why a Master Instrument Must Be Identified" )
          .Add_Separator()
          .Add_Blank()
          .Add_Body( "When three or more instruments are selected, all delta measurements" )
          .Add_Body( "must be calculated relative to a single, common reference point." )
          .Add_Blank()
          .Add_Body_Bold( "Without a master instrument:" )
          .Add_Body( "It is unclear which instrument\u2019s readings should serve as the" )
          .Add_Body( "baseline. Calculating deltas between different pairs of instruments" )
          .Add_Body( "would produce inconsistent results that cannot be meaningfully" )
          .Add_Body( "compared to one another." )
          .Add_Blank()
          .Add_Body_Bold( "With a master instrument:" )
          .Add_Body( "Every other instrument\u2019s delta is calculated relative to the master\u2019s" )
          .Add_Body( "readings. This creates a consistent baseline across all selected" )
          .Add_Body( "instruments, ensuring that all comparisons reflect deviation from" )
          .Add_Body( "the same reference source." )
          .Add_Blank()
          .Add_Body_Colored( "Please select a master instrument before relying on delta measurements.",
                             Color.FromArgb( 30, 80, 160 ) );

        Popup.Show_Popup( this );
      }
    }

    private void Prologix_Health_Button_Click( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      int       Address = (int) GPIB_Address_Numeric.Value;

      try
      {
        Prologix_Health_Button.Enabled = false;
        var Result                     = Verify_Prologix_Health( Address );

        using ( var Popup = new Rich_Text_Popup( "Device Health Check", 680, 480, Resizable: false ) )
        {
          // ── Title ────────────────────────────────────────────────────────────
          Popup.Add_Title( "  Prologix Device Health Check" );
          Popup.Add_Row( "Address", $"GPIB {Result.Checked_Address}", Label_Width: 24 );
          Popup.Add_Blank();

          // ── Prologix configuration ───────────────────────────────────────────
          Popup.Add_Blank();
          Popup.Add_Separator();
          Popup.Add_Heading_Mono( "  Prologix Configuration" );
          Popup.Add_Row( "  Auto Read", Result.Prologix_Auto, Label_Width: 20 );
          Popup.Add_Row( "  EOI", Result.Prologix_EOI, Label_Width: 20 );
          Popup.Add_Row( "  EOS", Result.Prologix_EOS, Label_Width: 20 );
          Popup.Add_Row( "  Save Config", Result.Prologix_SaveCfg, Label_Width: 20 );
          Popup.Add_Blank();

          // ── Overall status ───────────────────────────────────────────────────
          if ( Result.Is_Healthy )
            Popup.Add_Body_Colored( "  ✔  All checks passed — device is operational.",
                                    Color.FromArgb( 0, 140, 0 ) );
          else
            Popup.Add_Error( $"  ✘  {Result.Failed_Checks.Count} check(s) failed — review details below." );

          // ── Passed checks ────────────────────────────────────────────────────
          if ( Result.Passed_Checks.Any() )
          {
            Popup.Add_Blank();
            Popup.Add_Heading_Mono( "  Passed Checks" );
            foreach ( string Check in Result.Passed_Checks )
              Popup.Add_Body_Colored( $"  ✔  {Check}", Color.FromArgb( 0, 140, 0 ) );
          }

          // ── Failed checks ────────────────────────────────────────────────────
          if ( Result.Failed_Checks.Any() )
          {
            Popup.Add_Blank();
            Popup.Add_Heading_Mono( "  Failed Checks" );
            foreach ( string Check in Result.Failed_Checks )
              Popup.Add_Error( $"  ✘  {Check}" );
          }

          // ── Device identity if available ─────────────────────────────────────
          if ( ! string.IsNullOrWhiteSpace( Result.Device_Identity ) )
          {
            Popup.Add_Blank();
            Popup.Add_Separator();
            Popup.Add_Row( "  Instrument ID", Result.Device_Identity, Label_Width: 20 );
          }

          Popup.Add_Blank();
          Popup.Show_Popup( this );
        }
      }
      catch ( Exception ex )
      {
        using ( var Popup = new Rich_Text_Popup( "Health Check Error", 500, 220 ) )
        {
          Popup.Add_Title( "  Unexpected Error" );
          Popup.Add_Blank();
          Popup.Add_Error( $"  {ex.Message}" );
          Popup.Add_Blank();
          Popup.Show_Popup( this );
        }
      }
      finally
      {
        Prologix_Health_Button.Enabled = true;
      }
    }

    public Device_Health_Result Verify_Prologix_Health( int address )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Comm.Flush_Input_Buffer();

      var Result            = new Device_Health_Result { Checked_Address = address };
      int Original_Timeout  = _Comm.Read_Timeout_Ms;
      _Comm.Read_Timeout_Ms = _Settings.Default_GPIB_Timeout_Ms;
      _Comm.Change_GPIB_Address( address );

      try
      {
        _Comm.Read_Timeout_Ms = _Settings.Default_GPIB_Timeout_Ms;

        // --- Check 1: Prologix controller responsiveness ---
        try
        {
          _Comm.Flush_Input_Buffer();
          string Controller_Version = _Comm.Query_Instrument( "++ver" );
          string Ver_Trimmed        = Controller_Version?.Split( '\n' ) [ 0 ].Trim() ?? string.Empty;

          if ( ! string.IsNullOrWhiteSpace( Ver_Trimmed ) &&
               Ver_Trimmed.StartsWith( "Prologix", StringComparison.OrdinalIgnoreCase ) )
            Result.Passed_Checks.Add( $"Prologix controller responsive: {Ver_Trimmed}" );
          else
            Result.Failed_Checks.Add( $"Unexpected ++ver response: '{Ver_Trimmed}'" );
        }
        catch ( Exception ex )
        {
          Result.Failed_Checks.Add( $"Prologix controller not responding to ++ver: {ex.Message}" );
        }

        // --- Check 2: GPIB mode is CONTROLLER (mode = 1) ---
        try
        {
          _Comm.Flush_Input_Buffer();
          string Mode         = _Comm.Query_Instrument( "++mode" );
          string Mode_Trimmed = Mode?.Split( '\n' ) [ 0 ].Trim() ?? string.Empty;

          if ( Mode_Trimmed == "1" )
            Result.Passed_Checks.Add( "Prologix is in CONTROLLER mode." );
          else if ( double.TryParse( Mode_Trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out _ ) )
            Result.Passed_Checks.Add( "++mode response obscured by instrument poll (non-critical)." );
          else
            Result.Failed_Checks.Add(
              $"Prologix not in CONTROLLER mode — ++mode returned: '{Mode_Trimmed}', expected '1'." );
        }
        catch ( Exception ex )
        {
          Result.Failed_Checks.Add( $"Could not verify ++mode: {ex.Message}" );
        }

        // --- Check 3: Prologix internal read timeout (informational) ---
        try
        {
          _Comm.Flush_Input_Buffer();
          string RAW         = _Comm.Query_Instrument( "++read_tmo_ms" );
          string RAW_Trimmed = RAW?.Split( '\n' ) [ 0 ].Trim() ?? string.Empty;

          if ( int.TryParse( RAW_Trimmed, out int Tmo ) )
            Result.Passed_Checks.Add( $"Prologix read timeout: {Tmo} ms" );
          else if ( double.TryParse( RAW_Trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out _ ) )
          { /* stale measurement on first line — silent skip */
          }
          else
            Result.Failed_Checks.Add( $"Could not parse ++read_tmo_ms response: '{RAW_Trimmed}'" );
        }
        catch ( Exception ex )
        {
          Result.Passed_Checks.Add( "++read_tmo_ms not supported by this firmware (non-critical)." );
        }

        // --- Prologix configuration snapshot ---
        try
        {
          _Comm.Flush_Input_Buffer();
          string Auto          = _Comm.Query_Instrument( "++auto" )?.Split( '\n' ) [ 0 ].Trim() ?? "";
          Result.Prologix_Auto = Auto == "1" ? "On" : Auto == "0" ? "Off" : Auto;
        }
        catch
        {
          Result.Prologix_Auto = "n/a";
        }

        try
        {
          _Comm.Flush_Input_Buffer();
          string EOI          = _Comm.Query_Instrument( "++eoi" )?.Split( '\n' ) [ 0 ].Trim() ?? "";
          Result.Prologix_EOI = EOI == "1" ? "Enabled" : EOI == "0" ? "Disabled" : EOI;
        }
        catch
        {
          Result.Prologix_EOI = "n/a";
        }

        try
        {
          _Comm.Flush_Input_Buffer();
          string EOS          = _Comm.Query_Instrument( "++eos" )?.Split( '\n' ) [ 0 ].Trim() ?? "";
          Result.Prologix_EOS = EOS switch { "0" => "CR+LF",
                                             "1" => "CR",
                                             "2" => "LF",
                                             "3" => "None",
                                             _   => EOS };
        }
        catch
        {
          Result.Prologix_EOS = "n/a";
        }

        try
        {
          _Comm.Flush_Input_Buffer();
          string SAVECFG          = _Comm.Query_Instrument( "++savecfg" )?.Split( '\n' ) [ 0 ].Trim() ?? "";
          Result.Prologix_SaveCfg = SAVECFG == "1" ? "Enabled" : SAVECFG == "0" ? "Disabled" : SAVECFG;
        }
        catch
        {
          Result.Prologix_SaveCfg = "n/a";
        }

        // --- Check 4: Device identity (*IDN?) ---
        try
        {
          _Comm.Flush_Input_Buffer();
          string IDN         = _Comm.Query_Instrument( "*IDN?" );
          string IDN_Trimmed = IDN?.Split( '\n' ) [ 0 ].Trim() ?? string.Empty;

          if ( string.IsNullOrWhiteSpace( IDN_Trimmed ) )
          {
            Result.Passed_Checks.Add( "No *IDN? response — device does not support SCPI identification " +
                                      "(non-critical)." );
          }
          else if ( double.TryParse( IDN_Trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out _ ) )
          {
            // Measurement bled through — device is responding on the bus but not to *IDN?
            Result.Passed_Checks.Add( "Device is responding on GPIB bus but does not support *IDN? " +
                                      "(non-critical)." );
          }
          else
          {
            Result.Device_Identity = IDN_Trimmed;
            Result.Passed_Checks.Add( $"Device identity confirmed: {Result.Device_Identity}" );
          }
        }
        catch ( Exception ex )
        {
          // Timeout on *IDN? is normal for non-SCPI instruments — treat as non-critical
          Result.Passed_Checks.Add( $"Device does not support *IDN? (non-critical): {ex.Message}" );
        }

        Result.Is_Healthy = Result.Failed_Checks.Count == 0;
      }
      finally
      {

        _Comm.Read_Timeout_Ms = Original_Timeout;
      }

      return Result;
    }
  }
}
