// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Trace_Execution.cs
// PROJECT: Multimeter_Controller  (namespace: Trace_Execution_Namespace)
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Lightweight, always-on execution tracer for DEBUG builds.  Produces an
//   indented call-tree log in a floating RichTextBox window and optionally
//   writes the same output to a .txt file.  All public API is static; callers
//   never manage lifetimes directly.  In RELEASE builds every Trace_Block
//   allocation compiles away to a null return, so there is zero runtime cost.
//
// ── QUICK-START ───────────────────────────────────────────────────────────────
//
//   1. Call once at application startup (typically in Program.Main):
//
//        Trace_Execution.Initialize();          // shows window, enables tracing
//        Trace_Execution.Initialize( false );   // initialises without showing
//
//   2. In every method you want to trace, add ONE line at the top:
//
//        using var Block = Trace_Block.Start_If_Enabled();
//        // or equivalently:
//        using static Trace_Execution_Namespace.Trace_Execution;
//        using var Block = Trace_Block.Start_If_Enabled();
//
//      The [CallerMemberName] attribute captures the method name automatically;
//      you never pass a string.  When the using block exits the method, the
//      indent level is decremented.
//
//   3. Write inline messages with Capture_Trace.Write():
//
//        Capture_Trace.Write( $"Value = {Value}" );
//        Capture_Trace.Write( $"Series count: {_Series.Count}" );
//
//   4. Toggle or stop tracing at runtime:
//
//        Trace_Execution.Toggle();   // flip on/off
//        Trace_Execution.Stop();     // hide window, suppress output
//        Trace_Execution.Start();    // re-enable and show window
//
//   5. Call at application exit:
//
//        Trace_Execution.Shutdown(); // completes the queue pump
//
// ── TYPICAL USAGE PATTERN ─────────────────────────────────────────────────────
//
//   // Top of every traced method:
//   private void Apply_Settings()
//   {
//     using var Block = Trace_Block.Start_If_Enabled();
//     // ... method body ...
//     Capture_Trace.Write( "Settings applied successfully" );
//   }
//
//   // The trace window will show something like:
//   //   12:34:56.789 - |-- Apply_Settings
//   //   12:34:56.790 -     |   Settings applied successfully
//   //   12:34:56.791 -     |-- Load_Settings         ← nested call
//   //   12:34:56.792 -         |   Loading from disk
//
// ── OUTPUT FORMAT ─────────────────────────────────────────────────────────────
//
//   Every line is prefixed with "HH:mm:ss.fff - " then a tree-indent string:
//
//     Root call (depth 0):   "|-- Method_Name"
//     Nested call (depth 1): "|   |-- Nested_Method"
//     Inline message:        "|   |   message text"
//
//   Depth is tracked per async context via AsyncLocal<int> so concurrent
//   async chains maintain independent indent levels.
//
// ── VERBOSE / SIMPLE MODES ────────────────────────────────────────────────────
//
//   Verbose   Shows all Capture_Trace.Write() messages.  Default.
//   Simple    Suppresses inline messages; only method entry headers appear.
//             Toggle via the "Verbose/Simple" button in the trace window.
//
//   Capture_Trace.Write() respects VerboseMode by default.  To write a message
//   that appears in both modes:
//
//     Capture_Trace.Write( "Always visible", Verbose_Only: false );
//
// ── COLOR CODING IN THE TRACE WINDOW ─────────────────────────────────────────
//
//   Lines containing "ERROR"   → Red    (RGB 255, 100, 100)
//   Lines containing "WARN"    → Amber  (RGB 255, 200, 80)
//   All other lines            → current ForeColor (user-configurable)
//
// ────────────────────────────────────────────────────────────────────────────────
// TYPE REFERENCE
// ────────────────────────────────────────────────────────────────────────────────
//
// ── Trace_Execution (public static) ──────────────────────────────────────────
//
//   IsRunning → bool
//     True when Execution_Logger.Enabled is true.  Safe to poll at any time.
//
//   Initialize( bool Start_Enabled = true )
//     Must be called once before any other method, typically in Program.Main()
//     before Application.Run().  Creates the TraceWindow if Start_Enabled is
//     true.  Idempotent — subsequent calls are no-ops.
//
//   Start()
//     Enables logging and shows the trace window.  No-op if already running.
//     If the window was previously created it is shown via Invoke(); otherwise
//     the next Write() call will create it via Ensure_Started().
//
//   Stop()
//     Disables logging and hides the trace window without destroying it.
//     The window's text buffer is preserved; Start() will re-show it.
//
//   Toggle()
//     Calls Stop() if running, Start() if not.  Convenient for a menu item or
//     debug hotkey.
//
//   Start_If_Enabled( [CallerMemberName] string Caller = "" ) → Trace_Block?
//     Factory method used in the "using var Block = …" pattern.  In DEBUG
//     builds returns a new Trace_Block (which writes the method header and
//     increments indent) if the window exists and logging is enabled; returns
//     null otherwise.  In RELEASE builds always returns null — the entire
//     tracing path is compiled away.
//
//   Shutdown()
//     Calls Execution_Logger.Shutdown() which marks the BlockingCollection as
//     complete so the background pump thread drains and exits cleanly.  Call
//     this in Application.ApplicationExit or at the end of Main().
//
// ── Trace_Block (public sealed, IDisposable) ──────────────────────────────────
//
//   The primary per-method tracing handle obtained via Start_If_Enabled().
//   Wrap in a using statement — never store and manually Dispose().
//
//   Construction
//     Records the caller name and current AsyncLocal indent level, increments
//     the level, and writes the "|-- MethodName" header line immediately.
//
//   Trace( string Message )
//     Writes an indented message at the current depth, but only when
//     VerboseMode is active.  Multi-line messages (newlines in the string)
//     are split and each line is indented separately.  Safe to call at any
//     point while the using block is open.
//
//   Blank_Line()
//     Writes an empty indented line to visually separate groups of trace
//     messages.  Verbose-only.
//
//   Start( [CallerMemberName] string Caller = "" ) → Trace_Block   [static]
//     Unconditional factory — always creates a block regardless of enabled
//     state.  Use only when you explicitly want tracing regardless of the
//     global flag (rare).
//
//   Start_If_Enabled( [CallerMemberName] string Caller = "" ) → Trace_Block?
//     Instance-level equivalent of Trace_Execution.Start_If_Enabled().
//     Returns null if the window does not exist.
//
//   Dispose()
//     Decrements AsyncLocal indent level.  Called automatically by the using
//     statement when the method exits (normally or via exception).
//
// ── Capture_Trace (public static) ────────────────────────────────────────────
//
//   Write( string Message,
//          [CallerMemberName] string Caller = "",
//          bool Verbose_Only = true )
//     The primary way to emit an inline diagnostic message from within a
//     traced method.  Reads the current AsyncLocal indent level and prefixes
//     accordingly.  When Message is null or whitespace the Caller name is
//     used as the message text.
//
//     Verbose_Only = true  (default): suppressed when VerboseMode is off.
//     Verbose_Only = false           : always written when logging is enabled.
//
//     Does NOT require a Trace_Block to be in scope — it can be called from
//     any context and will simply use the ambient indent level (which may be
//     0 if no block is open).
//
// ── Execution_Logger (internal static) ───────────────────────────────────────
//
//   The queue-based I/O engine.  Not part of the public API; described here
//   for completeness.
//
//   All writes go to a BlockingCollection<string> (_Queue).  A single
//   background Task (Process_Queue) drains the queue and dispatches each line
//   to the TraceWindow and/or a log file.
//
//   Write( string )         Prepends "HH:mm:ss.fff - " then enqueues.
//   Write_Raw( string )     Same prepend logic; semantically identical to Write
//                           in this implementation.
//   Enable_UI_Logging()     Toggles delivery to TraceWindow (Pause button).
//   Enable_File_Logging()   Toggles delivery to the log file.
//   Set_Log_File( path )    Changes the output file path (default "trace.log").
//   Ensure_Started()        Lazily starts the pump Task and creates the
//                           TraceWindow if needed.  Thread-safe via _Init_Lock.
//   Shutdown()              Completes the BlockingCollection so the pump exits.
//
// ── TraceWindow (internal partial Form) ──────────────────────────────────────
//
//   The floating trace output window.  Created on the UI thread via
//   Create_Window_If_Needed(); subsequent access through the Instance property.
//   Closing the window hides it (OnFormClosing cancels the close) rather than
//   disposing it — the buffer is preserved across hide/show cycles.
//
//   Toolbar buttons:
//     "Log → File"   Opens SaveFileDialog, enables file logging to chosen path.
//     "Pause UI"     Suspends delivery to the RichTextBox (queue continues).
//                    Becomes "Resume UI" while paused.
//     "Clear"        Clears the RichTextBox display buffer.
//     "Font"         Opens FontDialog; persisted to trace_settings.json.
//     "ForeColor"    Opens ColorDialog; re-applies to all existing text.
//     "BackColor"    Opens ColorDialog.
//     "Verbose/Simple" Toggles VerboseMode.
//     "Exit"         Hides the window (same as close button).
//
//   Context menu on the log box: Copy, Select All, Clear.
//
//   Settings persistence:
//     trace_settings.json alongside the executable stores font family/size/
//     style, foreground and background colors (as ARGB ints), VerboseMode,
//     window position, and window size.  Loaded in the constructor, saved on
//     each settings change and on ApplicationExit.
//
//   Append_Text_Safe( string )
//     Thread-safe append: BeginInvoke()s to the UI thread if called from a
//     background thread.  Applies color coding (ERROR → red, WARN → amber,
//     everything else → current ForeColor) and auto-scrolls to the caret.
//
// ── Trace_Indent (internal static) ───────────────────────────────────────────
//
//   Holds a single AsyncLocal<int> Level.  Each Trace_Block increments on
//   construction and decrements on Dispose().  AsyncLocal ensures that
//   concurrent async continuations maintain independent depth counters — a
//   deeply-nested async chain does not corrupt the indent of an unrelated
//   parallel operation.
//
// ── TraceSettings (private class, nested in TraceWindow) ─────────────────────
//
//   Plain JSON-serialisable settings bag.  Default values:
//     FontFamily    "Consolas"
//     FontSize      9.5f
//     FontStyle     Regular
//     ForeColorArgb Color(200, 200, 200)  — light gray
//     BackColorArgb Color(20, 20, 20)     — near-black
//     VerboseMode   false
//     TraceVisible  true
//     WindowX/Y     -1 (system default position)
//     WindowWidth   800
//     WindowHeight  400
//
// ── RELEASE BUILD BEHAVIOR ───────────────────────────────────────────────────
//
//   The outer Trace_Execution.Start_If_Enabled() is wrapped in #if DEBUG and
//   returns null in RELEASE builds.  Because every caller stores the result
//   in a nullable using variable:
//
//     using var Block = Trace_Block.Start_If_Enabled();
//
//   … and null is a valid IDisposable target that simply calls no Dispose(),
//   the entire tracing path — including queue allocation, string formatting,
//   and indent management — is eliminated by the JIT in release.  No
//   performance cost in production builds.
//
// ── THREAD SAFETY NOTES ──────────────────────────────────────────────────────
//
//   • _Queue is a BlockingCollection — thread-safe for concurrent producers.
//   • _Init_Lock guards TraceWindow creation and pump startup.
//   • AsyncLocal<int> provides per-async-context indent isolation.
//   • Append_Text_Safe() marshals to the UI thread via BeginInvoke().
//   • TraceWindow creation in Ensure_Started() uses Invoke() on the first
//     open form when a message loop is already running, ensuring the window
//     is always created on the UI thread.
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

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

      try
      {
        if ( _Queue.IsAddingCompleted )
          return;

        _Queue.Add( $"{DateTime.Now:HH:mm:ss.fff} - {Message}" );
      }
      catch ( Exception Ex ) when ( Ex is InvalidOperationException or ObjectDisposedException )
      {
        // Queue completed or disposed — shutting down, silently drop message
      }
    }

    internal static void Write_Raw( string Message )
    {
      if ( ! Enabled )
        return;

      Ensure_Started();

      try
      {
        if ( _Queue.IsAddingCompleted )
          return;

        _Queue.Add( $"{DateTime.Now:HH:mm:ss.fff} - {Message}" );
      }
      catch ( Exception Ex ) when ( Ex is InvalidOperationException or ObjectDisposedException )
      {
        // Queue completed or disposed — shutting down, silently drop message
      }
    }

    private static void Ensure_Started()
    {
      lock ( _Init_Lock )
      {
        if ( _Queue.IsAddingCompleted )
          return;

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

    [ DefaultValue( true ) ]
    internal bool VerboseMode { get; private set; } = true;

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

      var Status_Label =
        new Label { Text      = "Execution Trace  \u2022  Select text freely  \u2022  Ctrl+C " + "to copy",
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
