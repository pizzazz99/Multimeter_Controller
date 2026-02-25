// ============================================================================
// File:        Form1.cs
// Project:     HP 3458A Multimeter Controller
// Description: Main application window for controlling a HP (HP) 3458A
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
//   - Instrument: HP / HP / Agilent 3458A Digital Multimeter
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


using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Windows.Forms;
using  Trace_Execution_Namespace;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static Trace_Execution_Namespace.Trace_Execution;


namespace Multimeter_Controller
{
  public partial class Form1 : Form
  {

    private Form _Voltage_Window;


    private int _Selected_Address = 0;

    private List<Command_Entry> _All_Commands;
    private readonly Instrument_Comm _Comm;
    private Meter_Type _Selected_Meter = Meter_Type.HP3458A;
    private CancellationTokenSource? _Scan_Cts;
    private bool _Is_Scanning;
    private bool _Ignore_Selection_Changed = false;

    private bool _Updating_Controls = false;

    private const int _Max_History_Size = 50;

    // Multi-instrument list (for launching multi-poll)
    private readonly List<(string Name, int Address, Meter_Type Type)>
      _Instruments = new List<(string, int, Meter_Type)> ( );


    private Application_Settings _Settings = Application_Settings.Load ( );
    private Chart_Theme _Theme = Chart_Theme.Dark_Preset ( );


    public Form1 ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      InitializeComponent ( );

      // Load settings at startup
      //  _Theme = _Settings.Current_Theme;

      //      _Settings.Theme_Changed += ( s, e ) =>
      //      {
      //        this.BackColor = _Settings.Current_Theme.Background;
      //        this.ForeColor = _Settings.Current_Theme.Foreground;
      //       // update any labels, panels on main form that are theme dependent
      //       Invalidate ( );
      //     };

      _Comm = new Instrument_Comm ( _Settings );
      _Comm.Connection_Changed += Comm_Connection_Changed;
      _Comm.Error_Occurred += Comm_Error_Occurred;
      _Comm.Data_Received += Comm_Data_Received;


      Populate_Connection_Controls ( );

      // Populate instrument type combo
      Instrument_Type_Combo.Items.Add ( "HP 3458A" );
      Instrument_Type_Combo.Items.Add ( "HP34401A" );
      Instrument_Type_Combo.Items.Add ( "HP33120A" );
      Instrument_Type_Combo.SelectedIndex = 1;
      Connected_Instrument_Textbox.Text = "";



      // Apply to launcher window
      if ( !string.IsNullOrWhiteSpace ( _Settings.Default_Window_Title ) )
      {
        this.Text = _Settings.Default_Window_Title + " - Launcher";
      }

      Subnet_Textbox.Text = _Settings.Network_Scan_Subnet;
      Block?.Trace ( $"Subnet set to: [{_Settings.Network_Scan_Subnet}] Control name: [{Subnet_Textbox.Name}]" );
    }


    private void Theme_Button_Click ( object sender, EventArgs e )
    {
      var Next_Theme = Multimeter_Common_Helpers_Class.Get_Next_Theme ( _Theme );
      _Settings.Set_Theme ( Next_Theme );
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
      IP_Address_Text.Text = _Settings.Default_IP_Address;
     
      Block?.Trace ( $"Subnet set to: [{_Settings.Network_Scan_Subnet}] Control name: [{Subnet_Textbox.Name}]" );
      Block?.Trace ( "Settings applied successfully" );
    }




    protected override void OnLoad ( EventArgs E )
    {
      base.OnLoad ( E );

      Trace_Execution.Initialize ( true );  // start visible


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
      /******************************************************************************
       * Update_Trace_Button_State - Updates the execution trace toggle button UI
       *
       * PURPOSE:
       *   Updates the text and background color of the "Show Execution Trace"
       *   button based on the current visibility state of the trace window.
       *   Ensures UI updates occur on the main thread.
       *
       * OPERATIONS:
       *   1. Logs entry into the method for debugging using Trace
       *   2. Checks if invocation is required (UI thread)
       *      - If so, uses BeginInvoke to marshal the call to the UI thread
       *   3. Sets button text to "Trace OFF" if Trace_Visible is true, otherwise "Trace ON"
       *   4. Sets button background color to Yellow when trace is visible, default otherwise
       *
       * PARAMETERS:
       *   Trace_Visible - Boolean indicating whether the trace window is currently visible
       *
       * RETURNS:
       *   void
       *
       * SIDE EFFECTS:
       *   - Updates buttonShowExecutionTrace.Text
       *   - Updates buttonShowExecutionTrace.BackColor
       *   - May queue a BeginInvoke action if called from a non-UI thread
       *
       * THREAD SAFETY:
       *   Ensures thread-safe updates of UI controls using BeginInvoke when necessary
       *
       * NOTES:
       *   BeginInvoke is used instead of Invoke to avoid blocking the calling thread
       *   and to prevent potential deadlocks during cross-thread UI updates.
       ******************************************************************************/


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
      /******************************************************************************
       * Trace_Window_TraceVisibilityChanged - Update button state on trace visibility
       *
       * PURPOSE:
       *   Updates the trace toggle button state when the visibility of the Trace window changes.
       *
       * OPERATIONS:
       *   1. Calls Update_Trace_Button_State with the current visibility
       *
       * PARAMETERS:
       *   sender  - object raising the event
       *   visible - bool indicating if Trace window is visible
       *
       * RETURNS:
       *   void
       *
       * SIDE EFFECTS:
       *   - Updates UI button state
       *
       * THREAD SAFETY:
       *   Must be called from UI thread
       *
       * NOTES:
       *   Typically invoked by Trace window itself when its visibility changes.
       ******************************************************************************/


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
      /******************************************************************************
       * buttonShowExecutionTrace_Click - Handle Trace Window Toggle Button
       *
       * PURPOSE:
       *   Event handler for the "Show Execution Trace" button click. Toggles the
       *   visibility of the Trace_Window debug output panel, allowing users to
       *   show or hide real-time execution tracing during motor operations.
       *
       * OPERATIONS:
       *   1. Starts a trace block for this method invocation
       *   2. Delegates to Toggle_Trace_Window() for actual visibility toggle
       *   3. Ends trace block with blank line separator
       *
       * PARAMETERS:
       *   sender - The button control that raised the click event
       *   e      - Event arguments (unused)
       *
       * RETURNS:
       *   void
       *
       * SIDE EFFECTS:
       *   - Toggles Trace_Window visibility (show/hide)
       *   - Updates Capture_Trace flag
       *   - Changes button appearance (color, text)
       *   - Enables or disables UI logging via Execution_Logger_Class
       *
       * THREAD SAFETY:
       *   Must be called on UI thread (standard button click handler)
       *
       * NOTES:
       *   This is a thin wrapper that delegates to Toggle_Trace_Window() which
       *   contains the actual implementation logic. The wrapper pattern allows
       *   Toggle_Trace_Window() to be called programmatically from other locations.
       ******************************************************************************/

      using var Block = Trace_Block.Start_If_Enabled ( );


      if ( Trace_Execution.IsRunning )
        Trace_Execution.Stop ( );
      else
        Trace_Execution.Start ( );
    }



    private Command_Entry _Selected_Command
    {
      get
      {
        if ( Command_List.SelectedIndex < 0 )
          return null;

        return _All_Commands [ Command_List.SelectedIndex ];
      }
    }



    protected override void OnFormClosed ( FormClosedEventArgs E )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      _Comm.Dispose ( );
      base.OnFormClosed ( E );
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


    private void Open_Dictionary_Button_Click (
      object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      using var Dictionary_Window =
        new Dictionary_Form ( _Selected_Meter );
      Dictionary_Window.ShowDialog ( this );
    }

    private void Single_Poll_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( $"Connected_Meter = {_Comm.Connected_Meter}" );

      if ( _Voltage_Window == null || _Voltage_Window.IsDisposed )
      {

        _Voltage_Window = new Single_Instrument_Poll_Form ( _Comm, _Selected_Meter, _Selected_Address, _Settings );
      }

      _Voltage_Window.Show ( );
      _Voltage_Window.BringToFront ( );


    }

    private void Multi_Poll_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Instruments.Count == 0 )
      {
        MessageBox.Show (
          "No instruments configured. Please scan for instruments first.",
          "No Instruments",
          MessageBoxButtons.OK,
          MessageBoxIcon.Warning );
        return;
      }

      // Make names unique if there are duplicates
      var Unique_Instruments = new List<(string Name, int Address, Meter_Type Type)> ( );
      foreach ( var Inst in _Instruments )
      {
        string Unique_Name = $"{Inst.Item1} @ {Inst.Address}";
        Unique_Instruments.Add ( (Unique_Name, Inst.Address, Inst.Type) );
      }

      var Form = new Multi_Instrument_Poll_Form ( _Comm, Unique_Instruments, _Settings, _Selected_Meter );
      Form.Show ( );
    }

    // ===== Instrument List =====


    private async void Add_Instrument_Button_Click (
         object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      _Updating_Controls = true;
      Capture_Trace.Write ( "Adding Instrument" );


      Instrument_Type_Combo.Enabled = true;
      Instrument_Type_Label.Enabled = true;

      int Address = (int) Instrument_Address_Numeric.Value;

      Meter_Type Type = Instrument_Type_Combo.SelectedIndex switch
      {
        1 => Meter_Type.HP34401A,
        2 => Meter_Type.HP33120A,
        _ => Meter_Type.HP3458A
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

        Capture_Trace.Write ( $"[Verifying GPIB address {Address}...]" );

        Append_Response ( $"[Verifying GPIB address {Address}...]" );

        try
        {
          string ID_Response = await Task.Run ( ( ) =>
    _Comm.Verify_GPIB_Address ( Address, Is_Legacy_HP ( Type ) ) );

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
        _Updating_Controls = false;
      }

      _Instruments.Add ( (Name, Address, Type) );

      bool Is_GPIB = Connection_Mode_Combo.SelectedIndex == 0 ||
                 Connection_Mode_Combo.SelectedIndex == 2;

      string Display = Is_GPIB
        ? $"{Name}  (GPIB {Address}, {Get_Meter_Name ( Type )})"
        : $"{Name}  ({Get_Meter_Name ( Type )})";
      Instruments_List.Items.Add ( Display );
    }

    private static bool Is_Legacy_HP ( Meter_Type Type ) => Type switch
    {
      Meter_Type.HP34401A => false,
      Meter_Type.HP33120A => false,
      Meter_Type.HP3458A => true,
      _ => false
    };




    private void Remove_Instrument_Button_Click (
    object Sender, EventArgs E )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( "Removing Name of Instrument" );

      int Index = Instruments_List.SelectedIndex;
      if ( Index < 0 )
      {
        return;
      }

      _Instruments.RemoveAt ( Index );
      Instruments_List.Items.RemoveAt ( Index );
    }









    public void Set_GPID_Controls ( bool State )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      GPIB_Address_Numeric.Enabled = State;
      Select_Instrument_Button.Enabled = State;
      Instrument_Address_Label.Enabled = State;
      Instrument_Address_Numeric.Enabled = State;
      // Add_Instrument_Button.Enabled = State;
      Remove_Instrument_Button.Enabled = State;
      Scan_Bus_Button.Enabled = State;
      Saved_Instruments_Label.Enabled = State;
      Instruments_List.Enabled = State;
      Instrument_Type_Combo.Enabled = true;
      Instrument_Type_Label.Enabled = true;


    }


    private void Instrument_Type_Combo_SelectedIndexChanged (
      object Sender, EventArgs E )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Updating_Controls )
        return;


      Meter_Type Type = Instrument_Type_Combo.SelectedIndex switch
      {
        1 => Meter_Type.HP34401A,
        2 => Meter_Type.HP33120A,
        _ => Meter_Type.HP3458A
      };



      // Set button text to reflect selected meter type
      Open_Dictionary_Button.Text = "Dictionary";
      Command_List_Label.Text = Type + " Commands";

      _Selected_Meter = Type;
      _Comm.Connected_Meter = Type;

      Instrument_Name_Text.Text = _Selected_Meter.ToString ( );

      _All_Commands = Command_Dictionary_Class.Get_All_Commands (
        _Selected_Meter );


      //  Populate_Command_List ( );
      Detail_Text_Box.Text = "";
      Send_Command_Text_Box.Text = "";
    }

    private static string Get_Meter_Name ( Meter_Type Type )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( "Getting Name of Instrument" );


      return Type switch
      {
        Meter_Type.HP34401A => "HP34401A",
        Meter_Type.HP33120A => "HP33120A",
        _ => "HP 3458A"
      };
    }



    private void Select_Instrument_Button_Click (
      object Sender, EventArgs E )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      int Index = Instruments_List.SelectedIndex;
      if ( Index < 0 || Index >= _Instruments.Count )
      {
        return;
      }

      var Instrument = _Instruments [ Index ];

      // Switch GPIB address
      _Comm.Change_GPIB_Address ( Instrument.Address );
      GPIB_Address_Numeric.Value = Instrument.Address;
      _Selected_Meter = Instrument.Name.ToString ( ).Contains ( "34401A" )
        ? Meter_Type.HP34401A
        : Instrument.Name.ToString ( ).Contains ( "33120A" )
          ? Meter_Type.HP33120A
          : Meter_Type.HP3458A;

      _Selected_Address = Instrument.Address;

      // Switch meter type and refresh command list
      Instrument_Type_Combo.SelectedIndex = Instrument.Type switch
      {
        Meter_Type.HP34401A => 1,
        Meter_Type.HP33120A => 2,
        _ => 0
      };


      Populate_Command_List ( );


    }

    private async void Scan_Bus_Button_Click (
      object Sender, EventArgs E )
    {


      using var Block = Trace_Block.Start_If_Enabled ( );


      if ( _Is_Scanning )
      {
        _Scan_Cts?.Cancel ( );
        return;
      }

      await Run_Bus_Scan ( );
    }

    private async Task Run_Bus_Scan ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

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
            Result.Detected_Type ?? Meter_Type.HP3458A;

          string Type_Name = Type switch
          {
            Meter_Type.HP34401A => "HP34401A",
            Meter_Type.HP33120A => "HP33120A",
            _ => "HP 3458A"
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




    private void old_Query_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      var Selected = _Selected_Command;
      if ( Selected == null )
        return;

      string Input = Send_Command_Text_Box.Text.Trim ( );
      if ( string.IsNullOrWhiteSpace ( Input ) )
        return;

      string [ ] Parts = Input.Split ( ' ', 2 );

      string Base_Token = Parts [ 0 ].TrimEnd ( '?' ) + "?";
      string Final_Command = Parts.Length > 1
          ? Base_Token + " " + Parts [ 1 ]
          : Base_Token;

      Execute_Query ( Final_Command );
    }



    private void Query_Button_Click ( object Sender, EventArgs E )
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

      Instruments_List.Items.Clear ( );
      Command_List.Items.Clear ( );
      Send_Command_Text_Box.Clear ( );
      Command_History_List_Box.Items.Clear ( );
      Send_Command_Text_Box.Clear ( );
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



    private void Connection_Mode_Combo_SelectedIndexChanged ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      bool Is_GPIB = Connection_Mode_Combo.SelectedIndex == 0;
      bool Is_Serial = Connection_Mode_Combo.SelectedIndex == 1;
      bool Is_Ethernet = Connection_Mode_Combo.SelectedIndex == 2;

      // Serial controls only relevant for serial/GPIB-USB
      // COM port needed for both GPIB-USB and Direct Serial
      COM_Port_Combo.Enabled = Is_GPIB || Is_Serial;
      COM_Port_Label.Enabled = Is_GPIB || Is_Serial;

      // RS-232 settings only relevant for Direct Serial
      // (Prologix GPIB-USB uses fixed settings, Ethernet has none)
      Baud_Rate_Combo.Enabled = Is_Serial;
      Baud_Rate_Label.Enabled = Is_Serial;
      Data_Bits_Combo.Enabled = Is_Serial;
      Data_Bits_Label.Enabled = Is_Serial;
      Parity_Combo.Enabled = Is_Serial;
      Parity_Label.Enabled = Is_Serial;
      Stop_Bits_Combo.Enabled = Is_Serial;
      Stop_Bits_Label.Enabled = Is_Serial;
      Flow_Control_Combo.Enabled = Is_Serial;
      Flow_Control_Label.Enabled = Is_Serial;

      // Ethernet controls
      Find_Prologic_Button.Enabled = Is_Ethernet;
      IP_Address_Text.Enabled = Is_Ethernet;
      Selected_IP_Address_Label.Enabled = Is_Ethernet;
      Subnet_Textbox.Enabled = Is_Ethernet;
      //   IP_Port_Numeric.Enabled = Is_Ethernet;
      //    IP_Port_Label.Enabled = Is_Ethernet;

      // GPIB address only relevant for GPIB modes
      Set_GPID_Controls ( State: Is_GPIB || Is_Ethernet );
    }














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

      // GPIB address (0-30) - stays editable while
      // connected so the user can switch instruments
      GPIB_Address_Numeric.Minimum = 0;
      GPIB_Address_Numeric.Maximum = 30;
      GPIB_Address_Numeric.Value = 22;
      GPIB_Address_Numeric.ValueChanged +=
        GPIB_Address_Numeric_ValueChanged;

      IP_Address_Text.Text = _Settings.Default_IP_Address;
      //   IP_Port_Numeric.Minimum = 1;
      //    IP_Port_Numeric.Maximum = 65535;
      ///   IP_Port_Numeric.Value = _Settings.Default_Prologic_Port;


      // EOI default to checked, auto-read off to avoid
      // errors on non-query commands
      //  Auto_Read_Check.Checked = false;
      //  EOI_Check.Checked = true;

      Update_Connection_Status ( false );
    }



    private static Prologix_Eos_Mode Get_EOS_Mode ( Meter_Type Type ) => Type switch
    {
      Meter_Type.HP3458A => Prologix_Eos_Mode.LF,    // 3458A wants LF only
      Meter_Type.HP34401A => Prologix_Eos_Mode.CR_LF, // 34401A wants CR+LF
      Meter_Type.HP33120A => Prologix_Eos_Mode.CR_LF, // 33120A wants CR+LF
      _ => Prologix_Eos_Mode.LF      // safe default
    };




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

        Connected_Instrument_Textbox.Text = "";
        _Selected_Address = 0;
        Set_GPID_Controls ( State: false );
      }
      else if ( Connection_Mode_Combo.SelectedIndex == 2 )
      {
        Capture_Trace.Write ( "Setting defaults for Ethernet connection" );
        IP_Address_Text.Text = "192.168.1.100";  // matches Ethernet_Host default in your class
                                                 //  IP_Port_Numeric.Value = 1234;             // matches Ethernet_Port default in your class
        GPIB_Address_Numeric.Value = 22;
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
        Flow_Control_Combo.SelectedItem = Handshake.None;
        GPIB_Address_Numeric.Value = 22;
        Instrument_Address_Numeric.Value = GPIB_Address_Numeric.Value;
        _Selected_Address = (int) GPIB_Address_Numeric.Value;

        Set_GPID_Controls ( State: true );
      }
    }

    private async void Connect_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Comm.Is_Connected )
      {
        Capture_Trace.Write ( "Disconnecting..." );
        _Comm.Disconnect ( );

        Capture_Trace.Write ( "Clearing Windows" );
        Empty_Windows ( );

        Capture_Trace.Write ( "Clearing out Instrument list" );
        _Instruments.Clear ( );

        return;
      }


      // Resolve meter type first so it can drive other settings
      Meter_Type Type = Instrument_Type_Combo.SelectedIndex switch
      {
        1 => Meter_Type.HP34401A,
        2 => Meter_Type.HP33120A,
        _ => Meter_Type.HP3458A
      };

      bool Is_Ethernet = Connection_Mode_Combo.SelectedIndex == 2;

      if ( Is_Ethernet )
      {
        _Comm.Mode = Connection_Mode.Prologix_Ethernet;
        _Comm.Ethernet_Host = IP_Address_Text.Text.Trim ( );

        //        _Comm.Ethernet_Port = (int) IP_Port_Numeric.Value;
        _Comm.Ethernet_Port = (int) _Settings.Default_Prologic_Port;

        _Comm.GPIB_Address = (int) GPIB_Address_Numeric.Value;
        _Selected_Address = (int) GPIB_Address_Numeric.Value;
        _Comm.EOS_Mode = Get_EOS_Mode ( Type );

        Capture_Trace.Write ( $"Comm Mode    -> {_Comm.Mode}" );
        Capture_Trace.Write ( $"Host         -> {_Comm.Ethernet_Host}" );
        Capture_Trace.Write ( $"Port         -> {_Comm.Ethernet_Port}" );
        Capture_Trace.Write ( $"GPIB Address -> {_Comm.GPIB_Address}" );
        Capture_Trace.Write ( $"EOS Mode     -> {_Comm.EOS_Mode}" );

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
        _Comm.GPIB_Address = (int) GPIB_Address_Numeric.Value;
        _Selected_Address = (int) GPIB_Address_Numeric.Value;
        _Comm.EOS_Mode = Get_EOS_Mode ( Type );
      }


      Connect_Button.Enabled = false;

      try
      {
        await Task.Run ( ( ) =>
        {
          _Comm.Connect ( );
        } );

        Populate_Command_List ( );
        Initialize_Remote_Connection ( );
      }
      finally
      {
        Connect_Button.Enabled = true;
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
        Instrument_Type_Combo.Enabled = false;
        Instrument_Type_Label.Enabled = false;

        //   Initialize_Remote_Connection ( );

        // Enable scan button only in GPIB mode
        bool Is_GPIB = _Comm.Mode == Connection_Mode.Prologix_GPIB;
        Scan_Bus_Button.Enabled = Is_GPIB;
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
        Instrument_Type_Combo.Enabled = true;
        Instrument_Type_Label.Enabled = true;

        // Disable scan while disconnected
        Scan_Bus_Button.Enabled = false;
      }
    }

    private void old_Connection_Mode_Combo_SelectedIndexChanged (
      object? Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      bool Is_GPIB = Connection_Mode_Combo.SelectedIndex == 0;

      Set_GPID_Controls ( State: Is_GPIB );

      // Show/hide Prologix-specific controls
      Prologix_Header_Label.Visible = Is_GPIB;
      GPIB_Address_Label.Visible = Is_GPIB;
      GPIB_Address_Numeric.Visible = Is_GPIB;

      Add_Instrument_Button.Enabled = Is_GPIB;
      Remove_Instrument_Button.Enabled = Is_GPIB;
      Select_Instrument_Button.Enabled = Is_GPIB;
      Saved_Instruments_Label.Enabled = Is_GPIB;
      Instruments_List.Enabled = Is_GPIB;

      //EOS_Mode_Label.Visible = Is_GPIB;
      //EOS_Mode_Combo.Visible = Is_GPIB;
      //   Auto_Read_Check.Visible = Is_GPIB;
      //    EOI_Check.Visible = Is_GPIB;

      // Update instrument group title
      Instruments_Group.Text = Is_GPIB
        ? "GPIB Instruments"
        : "Instruments";

      // Hide scan bus in direct serial mode
      Scan_Bus_Button.Visible = Is_GPIB;
    }

    // ===== Event Handlers =====

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
      Instrument_Address_Numeric.Value = GPIB_Address_Numeric.Value;

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


    public void Initialize_Remote_Connection ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      // If we are talking to an HP meter, we MUST establish remote communication
      if ( _Comm.Is_Connected )
      {



        if ( _Comm.Mode == Connection_Mode.Prologix_GPIB ||
             _Comm.Mode == Connection_Mode.Prologix_Ethernet )
        {

          /*
          Capture_Trace.Write ( "Setting Prologix adapter to ++mode 1" );
          _Comm.Send_Prologix_Command ( "++mode 1" );
         

          Capture_Trace.Write ( "Setting Prologix adapter to ++auto 0" );
          _Comm.Send_Prologix_Command ( "++auto 0" );
          

          Capture_Trace.Write ( "Setting Prologix adapter to ++eos 2" );
          _Comm.Send_Prologix_Command ( "++eos 2" );
         

          Capture_Trace.Write ( "Setting Prologix adapter to ++read_tmo_ms 5000" );
          _Comm.Send_Prologix_Command ( "++read_tmo_ms 5000" );
          */

          _Comm.Flush_Buffers ( );

          string Ver = _Comm.Query_Prologix_Version ( );
          Append_Response ( $"> Prologix version: {Ver}" );
          Capture_Trace.Write ( $"> Prologix version: {Ver}" );


        }


        Capture_Trace.Write ( "Setting Meters to remote mode..." );

        switch ( _Selected_Meter )
        {
          case Meter_Type.HP33120A:
          case Meter_Type.HP34401A:

            Capture_Trace.Write ( "Clearing..." );
            _Comm.Send_Instrument_Command ( "*CLS" );
            Append_Response ( "> *CLS" );

            Capture_Trace.Write ( "Setting HP to remote mode..." );
            _Comm.Send_Instrument_Command ( "SYSTEM:REMOTE" );
            Append_Response ( "> SYSTEM:REMOTE" );




            break;

          case Meter_Type.HP3458A:
            if ( _Settings.Send_Reset_On_Connect_3458A )
            {
              _Comm.Send_Instrument_Command ( "RESET" );
              Thread.Sleep ( 3000 );
              _Comm.Send_Prologix_Command ( $"++addr  {_Selected_Address}" );
              Thread.Sleep ( 50 );
            }
            if ( _Settings.Send_End_Always_3458A )
            {
              _Comm.Send_Instrument_Command ( "END ALWAYS" );
              Thread.Sleep ( 200 );
            }
            _Comm.Send_Instrument_Command ( "TRIG AUTO" );  // ← always HOLD, never AUTO
            Thread.Sleep ( 200 );
            _Comm.Send_Instrument_Command ( $"GPIB {_Selected_Address}" );
            Thread.Sleep ( 100 );
            _Comm.Send_Prologix_Command ( $"++addr {_Selected_Address}" );
            Thread.Sleep ( 50 );
            _Comm.Send_Instrument_Command ( $"NPLC {_Settings.Default_NPLC_3458A}" );
            Thread.Sleep ( _Settings.NPLC_Apply_Delay_Ms );
            Append_Response ( "> Connected to 3458A" );
            break;

          default:
            Capture_Trace.Write ( $"No specific init for meter type: {_Selected_Meter}" );
            Append_Response ( $"> Connected to {_Selected_Meter}" );
            break;
        }

        Connected_Instrument_Textbox.Text = _Selected_Meter.ToString ( );
        Command_History_List_Box.ClearSelected ( );
      }
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
    public static string Get_Line_Terminator ( Meter_Type Type ) => Type switch
    {

      Meter_Type.HP3458A => "\n",    // LF only - pre-SCPI HP
      Meter_Type.HP34401A => "\n",    // LF
      Meter_Type.HP33120A => "\n",    // LF
      _ => "\n"     // safe default
    };

    private void EOS_Mode_Combo_SelectedIndexChanged ( object sender, EventArgs e )
    {

    }

    private void Button_Info_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      string [ ] Query_Commands = _All_Commands
        .Where ( C => C.Command.EndsWith ( "?" ) )
        .Select ( C => C.Command )
        .ToArray ( );

      Info_Popup Popup = new Info_Popup ( _Comm, Query_Commands, _Selected_Meter );
      Popup.Show ( );
    }

    private void NPLC_Combo_SelectedIndexChanged ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      _Settings.Default_NPLC = NPLC_Combo.SelectedItem?.ToString ( ) ?? "10";
    }




    private async Task<string> Find_Prologix_IP ( string OUI = null )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      string Prologix_OUI = OUI ?? _Settings.Prologic_MAC_Address;

      // Step 1: Get local subnet
      string Local_IP = Get_Local_IP ( );
      if ( string.IsNullOrEmpty ( Local_IP ) )
      {
        Block?.Trace ( "Could not determine local IP" );
        return null;
      }

      Block?.Trace ( $"Local IP: {Local_IP}, scanning for OUI: {Prologix_OUI}" );

      // Step 2: Ping sweep to populate ARP cache
      string Subnet = Local_IP.Substring ( 0, Local_IP.LastIndexOf ( '.' ) + 1 );
      Block?.Trace ( $"Scanning subnet: {Subnet}0/24" );

      var Ping_Tasks = new List<Task> ( );
      for ( int I = 1; I <= 254; I++ )
      {
        string Target = Subnet + I;
        Ping_Tasks.Add ( Task.Run ( async ( ) =>
        {
          try
          {
            using var P = new System.Net.NetworkInformation.Ping ( );
            await P.SendPingAsync ( Target, 100 );
          }
          catch { }
        } ) );
      }

      await Task.WhenAll ( Ping_Tasks );
      await Task.Delay ( 500 );

      // Step 3: Read ARP table and find Prologix OUI
      try
      {
        var Proc = Process.Start ( new ProcessStartInfo
        {
          FileName = "arp",
          Arguments = "-a",
          RedirectStandardOutput = true,
          UseShellExecute = false,
          CreateNoWindow = true
        } );

        string Output = await Proc.StandardOutput.ReadToEndAsync ( );
        await Proc.WaitForExitAsync ( );

        Block?.Trace ( $"ARP output:\n{Output}" );

        foreach ( string Line in Output.Split ( '\n' ) )
        {
          if ( Line.Contains ( Prologix_OUI, StringComparison.OrdinalIgnoreCase ) )
          {
            Block?.Trace ( $"Found Prologix line: {Line.Trim ( )}" );

            string [ ] Parts = Line.Trim ( ).Split (
              new char [ ] { ' ', '\t' },
              StringSplitOptions.RemoveEmptyEntries );

            if ( Parts.Length >= 2 &&
                 System.Net.IPAddress.TryParse ( Parts [ 0 ], out _ ) )
            {
              Block?.Trace ( $"Prologix found at: {Parts [ 0 ]}  MAC: {Parts [ 1 ]}" );
              return Parts [ 0 ];
            }
          }
        }
      }
      catch ( Exception Ex )
      {
        Block?.Trace ( $"ARP scan error: {Ex.Message}" );
      }

      return null;
    }


    private string Get_Local_IP ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      try
      {
        // Connect to a public address to determine which local interface is active
        using var Socket = new System.Net.Sockets.UdpClient ( );
        Socket.Connect ( "8.8.8.8", 80 );
        string IP = ( (System.Net.IPEndPoint) Socket.Client.LocalEndPoint ).Address.ToString ( );
        Block?.Trace ( $"Local IP: {IP}" );
        return IP;
      }
      catch ( Exception Ex )
      {
        Block?.Trace ( $"Get_Local_IP error: {Ex.Message}" );
        return null;
      }
    }

    private async void Find_Prologix_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      string Original_Text = Find_Prologic_Button.Text;
      Find_Prologic_Button.Enabled = false;
      Find_Prologic_Button.Text = "Scanning...";

      string Subnet = Get_Local_Subnet ( );
      var Found = await Multimeter_Common_Helpers_Class.Scan_For_Prologix (
   Subnet, 200, null,
   Msg => Capture_Trace.Write ( Msg ) );

      Find_Prologic_Button.Enabled = true;
      Find_Prologic_Button.Text = Original_Text;

      if ( Found.Count == 0 )
      {
        MessageBox.Show ( "No Prologix device found on the network.",
          "Scan Complete", MessageBoxButtons.OK,
          MessageBoxIcon.Information );
        return;
      }
      if ( Found.Count == 1 )
      {
        IP_Address_Text.Text = Found [ 0 ];
        MessageBox.Show ( $"Found Prologix at {Found [ 0 ]}",
          "Scan Complete", MessageBoxButtons.OK,
          MessageBoxIcon.Information );
      }
      else
      {
        string Selected = Show_IP_Selection_Dialog ( Found );
        if ( !string.IsNullOrEmpty ( Selected ) )
          IP_Address_Text.Text = Selected;
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
















    private async void old_Find_Prologix_Button_Click ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Find_Prologic_Button.Enabled = false;
      Find_Prologic_Button.Text = "Scanning...";


      // Build a temporary settings snapshot from current UI values
      // so Find uses whatever the user has typed, not the saved settings
      string MAC_Address = _Settings.Prologic_MAC_Address;

      var IP = await Find_Prologix_IP ( MAC_Address );

      Find_Prologic_Button.Enabled = true;
      Find_Prologic_Button.Text = "Find Prologix on Network";

      if ( IP != null )
      {
        IP_Address_Text.Text = IP;

      }
      else
      {

        IP_Address_Text.Text = "Not found on network";
      }
    }






    private async Task<string> Find_Prologix_IP_Local ( string OUI = null )
    {
      string Prologix_OUI = string.IsNullOrWhiteSpace ( OUI )
        ? _Settings.Prologic_MAC_Address
        : OUI;

      // Get local IP
      string Local_IP = null;
      try
      {
        using var Socket = new System.Net.Sockets.UdpClient ( );
        Socket.Connect ( "8.8.8.8", 80 );
        Local_IP = ( (System.Net.IPEndPoint) Socket.Client.LocalEndPoint ).Address.ToString ( );
      }
      catch { return null; }

      // Ping sweep to populate ARP cache
      string Subnet = Local_IP.Substring ( 0, Local_IP.LastIndexOf ( '.' ) + 1 );
      var Ping_Tasks = new List<Task> ( );
      for ( int I = 1; I <= 254; I++ )
      {
        string Target = Subnet + I;
        Ping_Tasks.Add ( Task.Run ( async ( ) =>
        {
          try
          {
            using var P = new System.Net.NetworkInformation.Ping ( );
            await P.SendPingAsync ( Target, 100 );
          }
          catch { }
        } ) );
      }
      await Task.WhenAll ( Ping_Tasks );
      await Task.Delay ( 500 );

      // Read ARP table
      try
      {
        var Proc = System.Diagnostics.Process.Start ( new System.Diagnostics.ProcessStartInfo
        {
          FileName = "arp",
          Arguments = "-a",
          RedirectStandardOutput = true,
          UseShellExecute = false,
          CreateNoWindow = true
        } );

        string Output = await Proc.StandardOutput.ReadToEndAsync ( );
        await Proc.WaitForExitAsync ( );

        foreach ( string Line in Output.Split ( '\n' ) )
        {
          if ( Line.Contains ( Prologix_OUI, StringComparison.OrdinalIgnoreCase ) )
          {
            string [ ] Parts = Line.Trim ( ).Split (
              new char [ ] { ' ', '\t' },
              StringSplitOptions.RemoveEmptyEntries );

            if ( Parts.Length >= 2 &&
                 System.Net.IPAddress.TryParse ( Parts [ 0 ], out _ ) )
              return Parts [ 0 ];
          }
        }
      }
      catch { }

      return null;
    }

    private void Reset_Defaults_Button_Click ( object sender, EventArgs e )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      var Result = MessageBox.Show (
        "Reset all settings to defaults? This cannot be undone.",
        "Reset Defaults",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning );

      if ( Result != DialogResult.Yes )
        return;

      // Delete existing settings file
      string App_Data = Environment.GetFolderPath (
        Environment.SpecialFolder.ApplicationData );

      string File_Path = Path.Combine (
        App_Data, "Multimeter_Controller", "multi_poll_settings.json" );

      try
      {
        if ( File.Exists ( File_Path ) )
          File.Delete ( File_Path );
      }
      catch ( Exception Ex )
      {
        MessageBox.Show ( $"Could not delete settings file: {Ex.Message}",
          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
        return;
      }

      // Recreate with defaults
      _Settings = new Application_Settings ( );
      _Settings.Initialize_Default_Save_Folder ( );
      _Settings.Validate_And_Fix ( );
      _Settings.Save ( );

      // Apply to UI
      Apply_Settings ( );

      MessageBox.Show ( "Settings reset to defaults.",
        "Reset Complete", MessageBoxButtons.OK,
        MessageBoxIcon.Information );
    }


    private string Get_Local_Subnet ( )
    {
      // Use user-specified subnet if set
      if ( !string.IsNullOrWhiteSpace ( _Settings.Network_Scan_Subnet ) )
        return _Settings.Network_Scan_Subnet.TrimEnd ( '.' );

      // Auto-detect from local IP
      var Host = System.Net.Dns.GetHostEntry (
        System.Net.Dns.GetHostName ( ) );
      foreach ( var Addr in Host.AddressList )
      {
        if ( Addr.AddressFamily ==
          System.Net.Sockets.AddressFamily.InterNetwork )
        {
          string IP = Addr.ToString ( );
          string [ ] Parts = IP.Split ( '.' );
          return $"{Parts [ 0 ]}.{Parts [ 1 ]}.{Parts [ 2 ]}";
        }
      }
      return "192.168.1";
    }




    private void IP_Address_Text_TextChanged ( object sender, EventArgs e )
    {
      _Settings.Default_IP_Address = IP_Address_Text.Text.Trim ( );
      _Settings.Save ( );
    }

    private void Subnet_Textbox_TextChanged ( object sender, EventArgs e )
    {
      _Settings.Network_Scan_Subnet = Subnet_Textbox.Text.Trim ( ).TrimEnd ( '.' );
      _Settings.Save ( );
    }

    
  }
}
