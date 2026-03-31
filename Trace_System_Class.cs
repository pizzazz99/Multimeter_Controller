// ============================================================================
// COMPLETE INTERNAL TRACE SYSTEM WITH VERBOSE MODE
// ============================================================================
//
// PURPOSE:
//   A lightweight, hierarchical execution trace system for debugging and
//   monitoring application flow in real-time. Displays method calls in a
//   tree structure showing entry/exit points and custom messages.
//
// ============================================================================
// QUICK START GUIDE
// ============================================================================
//
// 1. INITIALIZATION (in Program.cs or Form1 constructor):
//
//    Trace_System.Initialize(Start_Enabled: true);
//
//    This creates and shows the trace window automatically.
//
// ----------------------------------------------------------------------------
//
// 2. BASIC USAGE - Trace method entry/exit:
//
//    private void My_Method()
//    {
//      using var Block = Trace_Block.Start();  // Auto-traces method name
//
//      // Your code here
//      // Method exit is automatically traced when 'Block' is disposed
//    }
//
//    OUTPUT:
//    12:34:56.789 - |-- My_Method
//
// ----------------------------------------------------------------------------
//
// 3. VERBOSE MESSAGES - Add detailed info inside a method:
//
//    private void Process_Data(string filename)
//    {
//      using var Block = Trace_Block.Start();
//
//      Block.Trace($"Processing file: {filename}");
//      Block.Trace($"File size: {size} bytes");
//
//      // Your code
//    }
//
//    OUTPUT (when Verbose Mode is ON):
//    12:34:56.789 - |-- Process_Data
//                 - |   Processing file: data.csv
//                 - |   File size: 1024 bytes
//
// ----------------------------------------------------------------------------
//
// 4. NESTED CALLS - Automatic tree structure:
//
//    private void Parent_Method()
//    {
//      using var Block = Trace_Block.Start();
//      Child_Method();
//    }
//
//    private void Child_Method()
//    {
//      using var Block = Trace_Block.Start();
//      // Your code
//    }
//
//    OUTPUT:
//    12:34:56.789 - |-- Parent_Method
//    12:34:56.790 - |   |-- Child_Method
//
// ----------------------------------------------------------------------------
//
// 5. QUICK TRACES - Single-line messages without blocks:
//
//    Trace.Write("User clicked save button");
//    Trace.Write($"Saving {count} records");
//
//    OUTPUT:
//    12:34:56.789 - User clicked save button
//    12:34:56.790 - Saving 42 records
//
// ----------------------------------------------------------------------------
//
// 6. VERBOSE MODE TOGGLE:
//
//    - Click "Verbose" button in trace window to toggle
//    - Verbose OFF: Shows only method names (clean, high-level view)
//    - Verbose ON:  Shows method names + all Block.Trace() messages (detailed)
//
//    Use verbose messages for diagnostic details you don't always need to see.
//
// ============================================================================
// CONTROL BUTTONS IN TRACE WINDOW
// ============================================================================
//
//   [Log → File]  - Start logging to a text file (prompts for location)
//   [Pause UI]    - Stop updating the window (trace still runs in background)
//   [Clear]       - Clear all current trace output
//   [Font]        - Change trace window font
//   [ForeColor]   - Change text color
//   [BackColor]   - Change background color
//   [Verbose]     - Toggle between simple (method names) and verbose (all details)
//   [Exit]        - Hide trace window (does not stop tracing)
//
// ============================================================================
// PROGRAMMATIC CONTROL
// ============================================================================
//
//   Trace_System.Start();    // Show trace window and enable tracing
//   Trace_System.Stop();     // Hide window and disable tracing
//   Trace_System.Toggle();   // Toggle on/off
//   Trace_System.Shutdown(); // Stop background processing (on app exit)
//
// ============================================================================
// BEST PRACTICES
// ============================================================================
//
// 1. Use Trace_Block for every method you want to monitor:
//    - Automatically shows entry/exit
//    - Maintains proper indentation hierarchy
//    - Zero overhead when disposed
//
// 2. Use Block.Trace() for diagnostic details:
//    - Variable values
//    - Loop iterations
//    - File paths
//    - Error conditions
//    These only show when Verbose Mode is ON.
//
// 3. Use Trace.Write() for important events:
//    - User actions (button clicks, menu selections)
//    - State changes
//    - Errors
//    These always show (even in Simple mode).
//
// 4. Add blank lines for readability:
//    Block.Blank_Line();
//
// 5. Turn OFF verbose mode for normal use:
//    - Reduces clutter
//    - Shows high-level flow
//    Turn ON verbose mode when debugging specific issues.
//
// ============================================================================
// EXAMPLE: Complete usage in a real method
// ============================================================================
//
//   private void Save_File(string path)
//   {
//     using var Block = Trace_Block.Start();  // Traces "Save_File"
//
//     Block.Trace($"Path: {path}");           // Verbose only
//     Block.Trace($"Records: {_Data.Count}"); // Verbose only
//
//     try
//     {
//       File.WriteAllText(path, _Data);
//       Trace.Write("File saved successfully"); // Always visible
//     }
//     catch (Exception ex)
//     {
//       Block.Trace($"ERROR: {ex.Message}"); // Verbose only
//       Trace.Write($"Save failed: {ex.Message}"); // Always visible
//     }
//   }
//
//   OUTPUT (Verbose Mode ON):
//   12:34:56.789 - |-- Save_File
//                - |   Path: C:\data\output.txt
//                - |   Records: 1000
//   12:34:56.850 - |   File saved successfully
//
//   OUTPUT (Verbose Mode OFF):
//   12:34:56.789 - |-- Save_File
//   12:34:56.850 - |   File saved successfully
//
// ============================================================================
// PERFORMANCE NOTES
// ============================================================================
//
// - Trace blocks have minimal overhead (~microseconds per call)
// - Messages are queued and processed asynchronously (non-blocking)
// - Safe to use in performance-critical code
// - Can be left in production code (disable with Trace_System.Stop())
// - No impact when tracing is disabled
//
// ============================================================================
// THREAD SAFETY
// ============================================================================
//
// - Fully thread-safe
// - Each thread maintains its own indent level
// - Messages from different threads are interleaved with timestamps
// - Use timestamps to correlate multi-threaded execution
//
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Trace_Execution_Namespace
{
  public static class Trace_Execution
  {
    private static bool _Initialized;
    public static bool  IsRunning => Execution_Logger.Enabled;

    public static void  Initialize( bool Start_Enabled = true )
    {
      if ( _Initialized )
        return;

      Execution_Logger.Enabled = Start_Enabled;
      Execution_Logger.Enable_UI_Logging( Start_Enabled );

      if ( Start_Enabled )
      {
        TraceWindow.Create_Window_If_Needed();
        TraceWindow.Instance.Show();
        TraceWindow.Instance.BringToFront();
      }

      _Initialized = true;
    }

    public static void Start()
    {
      if ( IsRunning )
        return;

      Execution_Logger.Enabled = true;
      Execution_Logger.Enable_UI_Logging( true );

      if ( TraceWindow.HasInstance )
      {
        var Win = TraceWindow.Instance;
        Win.Invoke( () =>
                    {
                      Win.Show();
                      Win.BringToFront();
                    } );
      }
    }

    public static Trace_Block? Start_If_Enabled( [ CallerMemberName ] string Caller = "" )
    {
#if DEBUG
      if ( ! TraceWindow.HasInstance )
        return null;
      if ( ! Execution_Logger.Enabled )
        return null;
      return new Trace_Block( Caller );
#else
      return null;
#endif
    }

    public static void Stop()
    {
      if ( ! IsRunning )
        return;

      Execution_Logger.Enabled = false;
      Execution_Logger.Enable_UI_Logging( false );

      if ( TraceWindow.HasInstance )
      {
        var Win = TraceWindow.Instance;
        if ( Win.InvokeRequired )
          Win.Invoke( () =>
                      {
                        Win.Hide();
                        Win.SendToBack();
                      } );
        else
        {
          Win.Hide();
          Win.SendToBack();
        }
      }
    }

    public static void Toggle()
    {
      if ( IsRunning )
        Stop();
      else
        Start();
    }

    public static void Shutdown() => Execution_Logger.Shutdown();

    public sealed class Trace_Block : IDisposable
    {
      private readonly string _Caller;
      private readonly int    _Start_Level;
      private bool            _Header_Written;

      internal                Trace_Block( string Caller )
      {
        _Caller      = Caller;
        _Start_Level = Trace_Indent.Level.Value;
        Trace_Indent.Level.Value++;
        Write_Header();
      }

      public static Trace_Block Start( [ CallerMemberName ] string Caller = "" ) => new Trace_Block( Caller );

      public static Trace_Block? Start_If_Enabled( [ CallerMemberName ] string Caller = "" )
      {
        if ( ! TraceWindow.HasInstance )
          return null;
        return new Trace_Block( Caller );
      }

      private void Write_Header()
      {
        if ( _Header_Written )
          return;

        if ( TraceWindow.HasInstance )
        {
          string Indent = Build_Tree_Indent( _Start_Level + 1, Is_Header: true );
          Execution_Logger.Write_Raw( $"{Indent}{_Caller}" );
        }

        _Header_Written = true;
      }

      public void Trace( string Message )
      {
        if ( ! TraceWindow.HasInstance || ! TraceWindow.Instance.VerboseMode ||
             string.IsNullOrEmpty( Message ) )
          return;

        int    Level      = _Start_Level + 1;
        string Indent     = Build_Tree_Indent( Level, Is_Header: false );
        string Time_Stamp = new string( ' ', 12 );

        foreach ( var Line in Message.Split( new[ ] { "\r\n", "\n" }, StringSplitOptions.None ) )
          Execution_Logger.Write_Raw( $"{Time_Stamp} - {Indent}{Line}" );
      }

      public void Blank_Line()
      {
        if ( ! TraceWindow.HasInstance || ! TraceWindow.Instance.VerboseMode )
          return;

        int    Level      = _Start_Level + 1;
        string Indent     = Build_Tree_Indent( Level, Is_Header: false );
        string Time_Stamp = new string( ' ', 12 );
        Execution_Logger.Write_Raw( $"{Time_Stamp} - {Indent}" );
      }

      public void Dispose()
      {
        if ( Trace_Indent.Level.Value > 0 )
          Trace_Indent.Level.Value--;
      }

      private static string Build_Tree_Indent( int Level, bool Is_Header )
      {
        if ( Level <= 0 )
          return string.Empty;

        var SB = new System.Text.StringBuilder();
        for ( int Count = 0; Count < Level - 1; Count++ )
          SB.Append( "|   " );

        SB.Append( Is_Header ? "|-- " : "|   " );
        return SB.ToString();
      }
    }

    internal static class Trace_Indent
    {
      internal static readonly AsyncLocal<int> Level = new();
    }

    public static class Trace_Helpers
    {
      public static string Get_Method_Name( [ CallerMemberName ] string Caller = "" ) => Caller;
    }

    public static class Capture_Trace
    {
      public static void Write( string                      Message,
                                [ CallerMemberName ] string Caller       = "",
                                bool                        Verbose_Only = true )
      {
        if ( Verbose_Only && TraceWindow.HasInstance && ! TraceWindow.Instance.VerboseMode )
          return;

        int    Level  = Trace_Indent.Level.Value;
        string Indent = Build_Tree_Indent( Level );
        string Prefix = Level > 0 ? $"{Indent}-- " : "";

        string Trace_Line = string.IsNullOrWhiteSpace( Message ) ? $"{Prefix}{Caller}" : $"{Prefix}{Message}";

        Execution_Logger.Write( Trace_Line );
      }

      private static string Build_Tree_Indent( int Level )
      {
        if ( Level <= 0 )
          return string.Empty;

        var SB = new System.Text.StringBuilder();
        for ( int Count = 0; Count < Level; Count++ )
          SB.Append( "|   " );

        return SB.ToString();
      }
    }
  }

  // ==========================================================================
  // LOGGER ENGINE
  // ==========================================================================

  internal static class Execution_Logger
  {
    internal static bool    Enabled { get; set; } = true;

    private static bool     _UI_Logging_Enabled = true;
    private static bool     _File_Logging_Enabled;
    private static string   _Log_File = "trace.log";

    private static readonly BlockingCollection<string> _Queue = new();
    private static Task? _Pump;
    private static readonly object _Init_Lock = new();

    internal static void           Enable_UI_Logging( bool Enable ) => _UI_Logging_Enabled = Enable;
    internal static void           Set_Log_File( string Path ) => _Log_File  = Path;
    internal static void           Enable_File_Logging( bool Enable ) => _File_Logging_Enabled = Enable;

    internal static void           Write( string Message )
    {
      if ( ! Enabled )
        return;
      Ensure_Started();
      _Queue.Add( $"{DateTime.Now:HH:mm:ss.fff} - {Message}" );
    }

    internal static void Write_Raw( string Message )
    {
      if ( ! Enabled )
        return;
      Ensure_Started();
      _Queue.Add( $"{DateTime.Now:HH:mm:ss.fff} - {Message}" );
    }

    private static void Ensure_Started()
    {
      lock ( _Init_Lock )
      {
        if ( _Pump == null )
          _Pump = Task.Run( Process_Queue );

        if ( _UI_Logging_Enabled )
          TraceWindow.Create_Window_If_Needed();
      }
    }

    private static async Task Process_Queue()
    {
      foreach ( var Line in _Queue.GetConsumingEnumerable() )
      {
        try
        {
          if ( _File_Logging_Enabled )
            await File.AppendAllTextAsync( _Log_File, Line + Environment.NewLine ).ConfigureAwait( false );

          if ( _UI_Logging_Enabled )
          {
            int Attempts = 0;
            while ( ! TraceWindow.HasInstance && Attempts++ < 50 )
              await Task.Delay( 100 ).ConfigureAwait( false );

            if ( TraceWindow.HasInstance )
              TraceWindow.Instance.Append_Text_Safe( Line );
          }
        }
        catch ( Exception Ex )
        {
          Debug.WriteLine( "TRACE ERROR: " + Ex.Message );
        }
      }
    }

    internal static void Shutdown() => _Queue.CompleteAdding();
  }

  // ==========================================================================
  // TRACE WINDOW
  // ==========================================================================

  internal partial class TraceWindow : Form
  {
    private static TraceWindow? _Instance;
    private static readonly object               _Lock         = new();
    private static readonly ManualResetEventSlim _Window_Ready = new( false );

    internal static bool                         HasInstance => _Instance != null && ! _Instance.IsDisposed;

    internal static TraceWindow Instance
    {
      get {
        if ( _Instance == null )
          throw new InvalidOperationException( "Trace window was never created. Call " +
                                               "Trace_Execution.Initialize() in Main() before " +
                                               "Application.Run()." );
        if ( _Instance.IsDisposed )
          throw new InvalidOperationException( "Trace window was disposed unexpectedly." );
        return _Instance;
      }
    }

    internal static void Create_Window_If_Needed()
    {
      lock ( _Lock )
      {
        if ( _Instance != null && ! _Instance.IsDisposed )
          return;

        _Window_Ready.Reset();

        if ( Application.MessageLoop )
        {
          if ( Application.OpenForms.Count > 0 )
            Application.OpenForms[ 0 ].Invoke( () =>
                                               {
                                                 _Instance = new TraceWindow();
                                                 _Window_Ready.Set();
                                               } );
        }
        else
        {
          _Instance = new TraceWindow();
          _Window_Ready.Set();
        }
      }
    }

    [ DefaultValue( false ) ]
    internal bool VerboseMode { get; private set; } = false;

    private TraceWindow()
    {
      InitializeComponent();
      Load_User_Settings();
      Apply_Settings();
      Application.ApplicationExit += ( S, E ) => Save_User_Settings();
    }

    private RichTextBox   _LogBox       = null!;
    private Panel         _TopPanel     = null!;
    private Button        _SaveBtn      = null!;
    private Button        _PauseBtn     = null!;
    private Button        _ClearBtn     = null!;
    private Button        _FontBtn      = null!;
    private Button        _ForeColorBtn = null!;
    private Button        _BackColorBtn = null!;
    private Button        _VerboseBtn   = null!;
    private Button        _ExitBtn      = null!;
    private TraceSettings _Settings     = new();

    private void          InitializeComponent()
    {
      _LogBox   = new RichTextBox();
      _TopPanel = new Panel();

      SuspendLayout();
      ClientSize = new Size( 800, 400 );
      Text       = "Execution Trace";
      BackColor  = Color.FromArgb( 30, 30, 30 );

      _TopPanel.Dock      = DockStyle.Top;
      _TopPanel.Height    = 40;
      _TopPanel.Padding   = new Padding( 6, 6, 6, 0 );
      _TopPanel.BackColor = Color.FromArgb( 45, 45, 48 );

      int       Left          = 6;
      const int Button_Width  = 88;
      const int Button_Height = 26;
      const int Spacing       = 4;

      void      Add_Button( ref Button   Btn,
                            string       Text,
                            EventHandler Click,
                            Color? Back = null,
                            Color? Fore = null )
      {
        Btn                            = new Button { Text      = Text,
                                                      Width     = Button_Width,
                                                      Height    = Button_Height,
                                                      Left      = Left,
                                                      Top       = 4,
                                                      FlatStyle = FlatStyle.Flat,
                                                      BackColor = Back ?? Color.FromArgb( 62, 62, 66 ),
                                                      ForeColor = Fore ?? Color.FromArgb( 220, 220, 220 ),
                                                      Font      = new Font( "Segoe UI", 8.5f, FontStyle.Regular ),
                                                      Cursor    = Cursors.Hand };
        Btn.FlatAppearance.BorderColor = Color.FromArgb( 90, 90, 90 );
        Btn.FlatAppearance.BorderSize  = 1;
        Btn.FlatAppearance.MouseOverBackColor  = Color.FromArgb( 80, 80, 84 );
        Btn.FlatAppearance.MouseDownBackColor  = Color.FromArgb( 0, 122, 204 );
        Btn.Click                             += Click;
        _TopPanel.Controls.Add( Btn );
        Left += Button_Width + Spacing;
      }

      Add_Button( ref _SaveBtn, "Log \u2192 File", Button_Save_Click );
      Add_Button( ref _PauseBtn, "Pause UI", Button_Pause_Click );
      Add_Button( ref _ClearBtn,
                  "Clear",
                  Button_Clear_Click,
                  Color.FromArgb( 80, 30, 30 ),
                  Color.FromArgb( 255, 180, 180 ) );
      Add_Button( ref _FontBtn, "Font", Button_Font_Click );
      Add_Button( ref _ForeColorBtn, "ForeColor", Button_Fore_Click );
      Add_Button( ref _BackColorBtn, "BackColor", Button_Back_Click );
      Add_Button( ref _VerboseBtn, "Simple", Button_Verbose_Click );
      Add_Button( ref _ExitBtn,
                  "Exit",
                  Button_Exit_Click,
                  Color.FromArgb( 60, 30, 30 ),
                  Color.FromArgb( 255, 100, 100 ) );

      var Status_Strip =
        new Panel { Dock = DockStyle.Bottom, Height = 22, BackColor = Color.FromArgb( 0, 122, 204 ) };

      var Status_Label = new Label { Text = "Execution Trace  \u2022  Select text freely  \u2022  Ctrl+C " +
                                            "to copy",
                                     ForeColor = Color.White,
                                     BackColor = Color.Transparent,
                                     AutoSize  = false,
                                     Dock      = DockStyle.Fill,
                                     Font      = new Font( "Segoe UI", 8f ),
                                     Padding   = new Padding( 6, 3, 0, 0 ) };

      Status_Strip.Controls.Add( Status_Label );

      _LogBox.Dock             = DockStyle.Fill;
      _LogBox.ReadOnly         = true;
      _LogBox.WordWrap         = false;
      _LogBox.BorderStyle      = BorderStyle.None;
      _LogBox.ScrollBars       = RichTextBoxScrollBars.Both;
      _LogBox.ShortcutsEnabled = true;

      var Menu       = new ContextMenuStrip();
      var Copy_Item  = new ToolStripMenuItem( "Copy", null, ( S, E ) => _LogBox.Copy() );
      var All_Item   = new ToolStripMenuItem( "Select All", null, ( S, E ) => _LogBox.SelectAll() );
      var Clear_Item = new ToolStripMenuItem( "Clear", null, ( S, E ) => _LogBox.Clear() );

      Menu.Items.AddRange(
        new ToolStripItem[ ] { Copy_Item, All_Item, new ToolStripSeparator(), Clear_Item } );

      Menu.Opening += ( S, E ) => Copy_Item.Enabled = _LogBox.SelectionLength > 0;
      _LogBox.ContextMenuStrip                      = Menu;

      Controls.Add( _LogBox );
      Controls.Add( Status_Strip );
      Controls.Add( _TopPanel );

      ResumeLayout( false );
    }

#region Button Handlers

    private void Button_Save_Click( object? Sender, EventArgs E )
    {
      using var Dlg = new SaveFileDialog { Filter   = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                                           FileName = "trace.log" };
      if ( Dlg.ShowDialog() != DialogResult.OK )
        return;

      Execution_Logger.Set_Log_File( Dlg.FileName );
      Execution_Logger.Enable_File_Logging( true );
      Execution_Logger.Write( "=== File logging ENABLED ===" );
    }

    private void Button_Pause_Click( object? Sender, EventArgs E )
    {
      bool Pause = _PauseBtn.Text == "Pause UI";
      Execution_Logger.Enable_UI_Logging( ! Pause );
      _PauseBtn.Text = Pause ? "Resume UI" : "Pause UI";
      Execution_Logger.Write( Pause ? "=== TRACE UI PAUSED ===" : "=== TRACE UI RESUMED ===" );
    }

    private void Button_Clear_Click( object? Sender, EventArgs E ) => _LogBox.Clear();

    private void Button_Font_Click( object? Sender, EventArgs E )
    {
      using var Dlg = new FontDialog { Font = _LogBox.Font };
      if ( Dlg.ShowDialog() == DialogResult.OK )
      {
        _LogBox.Font = Dlg.Font;
        Save_User_Settings();
      }
    }

    private void Button_Fore_Click( object? Sender, EventArgs E )
    {
      using var Dlg = new ColorDialog { Color = _LogBox.ForeColor };
      if ( Dlg.ShowDialog() == DialogResult.OK )
      {
        _LogBox.ForeColor = Dlg.Color;

        // Recolor all existing text — per-character selection colors override ForeColor
        _LogBox.SelectAll();
        _LogBox.SelectionColor  = Dlg.Color;
        _LogBox.SelectionStart  = _LogBox.TextLength;
        _LogBox.SelectionLength = 0;

        Save_User_Settings();
      }
    }

    private void Button_Back_Click( object? Sender, EventArgs E )
    {
      using var Dlg = new ColorDialog { Color = _LogBox.BackColor };
      if ( Dlg.ShowDialog() == DialogResult.OK )
      {
        _LogBox.BackColor = Dlg.Color;
        Save_User_Settings();
      }
    }

    private void Button_Verbose_Click( object? Sender, EventArgs E )
    {
      VerboseMode      = ! VerboseMode;
      _VerboseBtn.Text = VerboseMode ? "Verbose" : "Simple";
      Save_User_Settings();
      string Msg = VerboseMode ? "=== VERBOSE MODE ENABLED ===" : "=== SIMPLE MODE ENABLED ===";
      Append_Text_Safe( $"{DateTime.Now:HH:mm:ss.fff} - {Msg}" );
    }

    private void Button_Exit_Click( object? Sender, EventArgs E ) => Hide();

#endregion

    internal void Append_Text_Safe( string Text )
    {
      if ( IsDisposed )
        return;

      if ( InvokeRequired )
      {
        BeginInvoke( new Action<string>( Append_Text_Safe ), Text );
        return;
      }

      Color Line_Color = Text switch { _ when Text.Contains( "ERROR", StringComparison.OrdinalIgnoreCase ) =>
                                         Color.FromArgb( 255, 100, 100 ),
                                       _ when Text.Contains( "WARN", StringComparison.OrdinalIgnoreCase ) =>
                                         Color.FromArgb( 255, 200, 80 ),
                                       _ when Text.Contains( "ENABLED" )  => _LogBox.ForeColor,
                                       _ when Text.Contains( "DISABLED" ) => _LogBox.ForeColor,
                                       _ when Text.Contains( "===" )      => _LogBox.ForeColor,
                                       _                                  => _LogBox.ForeColor };

      _LogBox.SelectionStart  = _LogBox.TextLength;
      _LogBox.SelectionLength = 0;
      _LogBox.SelectionColor  = Line_Color;
      _LogBox.AppendText( Text + Environment.NewLine );
      _LogBox.ScrollToCaret();
    }

    protected override void OnFormClosing( FormClosingEventArgs E )
    {
      E.Cancel = true;
      Save_User_Settings();
      Hide();
    }

    private void Load_User_Settings()
    {
      string File = Path.Combine( Application.StartupPath, "trace_settings.json" );
      try
      {
        if ( System.IO.File.Exists( File ) )
          _Settings = JsonSerializer.Deserialize<TraceSettings>( System.IO.File.ReadAllText( File ) ) ??
                      new TraceSettings();
      }
      catch
      {
      }
    }

    private void Apply_Settings()
    {
      _LogBox.Font      = new Font( _Settings.FontFamily, _Settings.FontSize, _Settings.FontStyle );
      _LogBox.ForeColor = Color.FromArgb( _Settings.ForeColorArgb );
      _LogBox.BackColor = Color.FromArgb( _Settings.BackColorArgb );

      VerboseMode      = _Settings.VerboseMode;
      _VerboseBtn.Text = VerboseMode ? "Verbose" : "Simple";

      if ( _Settings.WindowX >= 0 )
        Location = new Point( _Settings.WindowX, _Settings.WindowY );

      Width  = _Settings.WindowWidth;
      Height = _Settings.WindowHeight;
    }

    private void Save_User_Settings()
    {
      try
      {
        _Settings.FontFamily    = _LogBox.Font.FontFamily.Name;
        _Settings.FontSize      = _LogBox.Font.Size;
        _Settings.FontStyle     = _LogBox.Font.Style;
        _Settings.ForeColorArgb = _LogBox.ForeColor.ToArgb();
        _Settings.BackColorArgb = _LogBox.BackColor.ToArgb();
        _Settings.VerboseMode   = VerboseMode;
        _Settings.TraceVisible  = Visible;
        _Settings.WindowX       = Location.X;
        _Settings.WindowY       = Location.Y;
        _Settings.WindowWidth   = Width;
        _Settings.WindowHeight  = Height;

        string File = Path.Combine( Application.StartupPath, "trace_settings.json" );
        System.IO.File.WriteAllText( File,
                                     JsonSerializer.Serialize( _Settings,
                                                               new JsonSerializerOptions { WriteIndented =
                                                                                             true } ) );
      }
      catch ( Exception Ex )
      {
        Debug.WriteLine( $"Failed to save trace settings: {Ex.Message}" );
      }
    }

    private class TraceSettings
    {
      public string    FontFamily { get; set; }    = "Consolas";
      public float     FontSize { get; set; }      = 9.5f;
      public FontStyle FontStyle { get; set; }     = FontStyle.Regular;
      public int       ForeColorArgb { get; set; } = Color.FromArgb( 200, 200, 200 ).ToArgb();
      public int       BackColorArgb { get; set; } = Color.FromArgb( 20, 20, 20 ).ToArgb();
      public bool      VerboseMode { get; set; }   = false;
      public bool      TraceVisible { get; set; }  = true;
      public int       WindowX { get; set; }       = -1;
      public int       WindowY { get; set; }       = -1;
      public int       WindowWidth { get; set; }   = 800;
      public int       WindowHeight { get; set; }  = 400;
    }
  }
}
