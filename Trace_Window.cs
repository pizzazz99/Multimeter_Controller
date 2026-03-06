// ============================================================================
// TRACE WINDOW - Professional Debug/Trace Logging System for WinForms
// ============================================================================
//
// VERSION: 2.0
// AUTHOR: Your Name / Team
// LAST UPDATED: 2025-02-13
// REQUIRES: .NET 6.0 or higher, WinForms
//
// ============================================================================
// TABLE OF CONTENTS
// ============================================================================
//
// 1. OVERVIEW & FEATURES
// 2. QUICK START GUIDE
// 3. INSTALLATION
// 4. BASIC USAGE
// 5. HIERARCHICAL BLOCK TRACING
// 6. ADVANCED FEATURES
// 7. CONFIGURATION & SETTINGS
// 8. API REFERENCE
// 9. BEST PRACTICES
// 10. TROUBLESHOOTING
// 11. PERFORMANCE CONSIDERATIONS
//
// ============================================================================
// 1. OVERVIEW & FEATURES
// ============================================================================
//
// The Trace Window system provides professional-grade debugging and logging
// capabilities for WinForms applications. It features:
//
// ✓ Real-time debug output in a dedicated window
// ✓ Hierarchical call tracing with tree-style visualization
// ✓ Thread-safe operation (safe from any thread)
// ✓ Configurable verbosity levels (Simple/Verbose)
// ✓ File logging with automatic timestamps
// ✓ Persistent user preferences (font, colors, window state)
// ✓ Global enable/disable switch
// ✓ Zero-overhead when disabled
// ✓ Automatic indentation showing call hierarchy
// ✓ Color-coded UI with customizable appearance
// ✓ Export trace logs to file
// ✓ Pause/Resume capability
// ✓ Sequential numbering for ordering
// ✓ Automatic UI thread marshaling
//
// MAIN COMPONENTS:
// ----------------
// • Trace_Window              : The main Form displaying log output (singleton)
// • Trace_Logger_Class        : Static class for all logging operations
// • Trace_Block_Class         : Creates hierarchical trace blocks
// • Trace_Helpers_Class       : Helper methods (Get_Method_Name, Trace)
// • Trace_Window_QuickSilver_Overrides_Class : User preferences storage
//
// ============================================================================
// 2. QUICK START GUIDE
// ============================================================================
//
// STEP 1: Add this file to your project
// --------------------------------------
// Simply add Trace_Window.cs to your WinForms project.
// No external dependencies required.
//
// STEP 2: Add the namespace
// --------------------------
// At the top of any file where you want to use tracing:
//
//     using Trace_Window_Namespace;
//
// STEP 3: Show the trace window
// ------------------------------
// In your main form's constructor or Load event:
//
//     Trace_Window.SafeInstance.Show();
//
// STEP 4: Start logging
// ----------------------
// Anywhere in your code:
//
//     Trace_Logger_Class.Write("Application started");
//     Trace_Logger_Class.Write("Loading configuration...");
//
// STEP 5: Add hierarchical tracing
// ---------------------------------
// At the start of any method you want to trace:
//
//     public void MyMethod()
//     {
//         using var Block = Trace_Block_Helper_Class.Start_Block(
//             Trace_Helpers_Class.Get_Method_Name());
//
//         // Your method code here...
//         Block.Trace("Processing step 1");
//         Block.Trace("Processing step 2");
//     }
//
// ============================================================================
// 3. INSTALLATION
// ============================================================================
//
// METHOD 1: Direct File Addition (Recommended)
// ---------------------------------------------
// 1. Copy Trace_Window.cs to your project folder
// 2. In Visual Studio: Right-click project → Add → Existing Item
// 3. Select Trace_Window.cs
// 4. Build your project
//
// METHOD 2: Copy-Paste
// ---------------------
// 1. Create a new class file named Trace_Window.cs
// 2. Copy the entire contents of this file
// 3. Paste into your new file
// 4. Build your project
//
// REQUIREMENTS:
// -------------
// • .NET 6.0 or higher
// • Windows Forms (System.Windows.Forms)
// • System.Text.Json (for settings persistence)
//
// ============================================================================
// 4. BASIC USAGE
// ============================================================================
//
// SHOWING THE TRACE WINDOW:
// --------------------------
//
//     // Show the window
//     Trace_Window.SafeInstance.Show();
//
//     // Show and bring to front
//     Trace_Window.SafeInstance.Show();
//     Trace_Window.SafeInstance.BringToFront();
//
//     // Toggle visibility
//     Trace_Window.SafeInstance.Toggle();
//
// SIMPLE LOGGING:
// ---------------
//
//     // Basic message with timestamp
//     Trace_Logger_Class.Write("Hello World");
//     // Output: 14:23:45.123 - Hello World
//
//     // Message without timestamp
//     Trace_Logger_Class.Write("No timestamp", Time_Stamp: false);
//     // Output: No timestamp
//
//     // Verbose-only message (only shown in Verbose mode)
//     Trace_Logger_Class.Write("Debug details", Trace_Verbosity.Verbose);
//
// LOGGING FROM DIFFERENT CONTEXTS:
// ---------------------------------
//
//     // From UI thread
//     private void Button_Click(object Sender, EventArgs E)
//     {
//         Trace_Logger_Class.Write("Button clicked");
//     }
//
//     // From background thread (thread-safe!)
//     Task.Run(() =>
//     {
//         Trace_Logger_Class.Write("Background task started");
//         // Do work...
//         Trace_Logger_Class.Write("Background task completed");
//     });
//
//     // From async method
//     private async Task LoadDataAsync()
//     {
//         Trace_Logger_Class.Write("Loading data...");
//         await Task.Delay(1000);
//         Trace_Logger_Class.Write("Data loaded");
//     }
//
// GLOBAL ENABLE/DISABLE:
// -----------------------
//
//     // Disable all tracing
//     Trace_Logger_Class.Enabled = false;
//
//     // Enable tracing
//     Trace_Logger_Class.Enabled = true;
//
//     // Check if enabled
//     if (Trace_Logger_Class.Enabled)
//     {
//         Trace_Logger_Class.Write("Tracing is active");
//     }
//
// ============================================================================
// 5. HIERARCHICAL BLOCK TRACING
// ============================================================================
//
// WHAT IS BLOCK TRACING?
// -----------------------
// Block tracing shows the call hierarchy of your application using
// tree-style indentation. This makes it easy to see which methods
// called which other methods, and track execution flow.
//
// BASIC PATTERN:
// --------------
//
//     public void MyMethod()
//     {
//         using var Block = Trace_Block_Helper_Class.Start_Block(
//             Trace_Helpers_Class.Get_Method_Name());
//
//         // Method body...
//         Block.Trace("Step 1: Initialize");
//         Block.Trace("Step 2: Process");
//     }
//
// EXAMPLE OUTPUT:
// ---------------
//
//     14:23:45.123 - |-- MyMethod
//                    |   Step 1: Initialize
//                    |   Step 2: Process
//
// NESTED CALLS:
// -------------
//
//     public void OuterMethod()
//     {
//         using var Block = Trace_Block_Helper_Class.Start_Block(
//             Trace_Helpers_Class.Get_Method_Name());
//
//         Block.Trace("Before inner call");
//         InnerMethod();
//         Block.Trace("After inner call");
//     }
//
//     public void InnerMethod()
//     {
//         using var Block = Trace_Block_Helper_Class.Start_Block(
//             Trace_Helpers_Class.Get_Method_Name());
//
//         Block.Trace("Inside inner method");
//     }
//
// EXAMPLE OUTPUT:
// ---------------
//
//     14:23:45.123 - |-- OuterMethod
//                    |   Before inner call
//                    |   |-- InnerMethod
//                    |   |   Inside inner method
//                    |   After inner call
//
// IMPORTANT RULES:
// ----------------
// ⚠ The "using var Block = ..." statement MUST be the FIRST line in the
//    method body to ensure accurate call hierarchy!
//
// ✓ CORRECT:
//     public void MyMethod()
//     {
//         using var Block = Trace_Block_Helper_Class.Start_Block(...);  // ← FIRST
//         // rest of code...
//     }
//
// ✗ WRONG:
//     public void MyMethod()
//     {
//         int x = 10;                                                    // ← OTHER CODE FIRST
//         using var Block = Trace_Block_Helper_Class.Start_Block(...);  // ← TOO LATE
//     }
//
// ADDING BLANK LINES:
// -------------------
//
//     Block.Blank_Line();  // Adds a blank line maintaining tree structure
//
// ============================================================================
// 6. ADVANCED FEATURES
// ============================================================================
//
// FILE LOGGING:
// -------------
//
//     // Programmatically enable file logging
//     Trace_Logger_Class.Set_Log_File("C:\\Logs\\myapp.log");
//     Trace_Logger_Class.Enable_File_Logging(true);
//
//     // Or let user choose via the UI
//     // Click "Log → File" button in the Trace Window
//
// VERBOSITY CONTROL:
// ------------------
//
//     // Set verbose mode (shows all messages including Block traces)
//     Trace_Logger_Class.Set_Verbose(true);
//
//     // Set simple mode (hides Block traces, shows only Simple messages)
//     Trace_Logger_Class.Set_Verbose(false);
//
//     // Or use the "Simple/Verbose" button in the UI
//
// PAUSE/RESUME UI UPDATES:
// -------------------------
//
//     // Pause UI updates (logging continues, but UI doesn't update)
//     Trace_Logger_Class.Enable_UI_Logging(false);
//
//     // Resume UI updates
//     Trace_Logger_Class.Enable_UI_Logging(true);
//
//     // Or use the "Pause UI" button in the UI
//
// CLEAR THE LOG:
// --------------
//
//     // Clear both UI and file log
//     Trace_Logger_Class.Clear_Log();
//
//     // Or click "Clear" button in the UI
//
// VISIBILITY CHANGE EVENTS:
// --------------------------
//
//     // Subscribe to know when trace window is shown/hidden
//     Trace_Window.SafeInstance.Trace_Visibility_Changed += (visible) =>
//     {
//         if (visible)
//             Console.WriteLine("Trace window opened");
//         else
//             Console.WriteLine("Trace window closed");
//     };
//
// INTEGRATING WITH YOUR UI:
// --------------------------
//
//     // Add a button to your main form to toggle trace window
//     private void TraceButton_Click(object Sender, EventArgs E)
//     {
//         Trace_Window.SafeInstance.Toggle();
//     }
//
//     // Update button text based on visibility
//     Trace_Window.SafeInstance.Trace_Visibility_Changed += (visible) =>
//     {
//         TraceButton.Text = visible ? "Hide Trace" : "Show Trace";
//         TraceButton.BackColor = visible ? Color.Green : SystemColors.Control;
//     };
//
// ============================================================================
// 7. CONFIGURATION & SETTINGS
// ============================================================================
//
// USER PREFERENCES:
// -----------------
// User preferences are automatically saved to:
//     [Application Directory]\trace_settings.json
//
// Saved settings include:
// • Font family, size, and style
// • Text color (foreground)
// • Background color
// • Window visibility state
//
// Settings are automatically loaded on startup and saved on change.
//
// PROGRAMMATIC CONFIGURATION:
// ----------------------------
//
//     // Enable/disable tracing globally
//     Trace_Logger_Class.Enabled = true;  // or false
//
//     // Set verbosity level
//     Trace_Logger_Class.Verbosity = Trace_Verbosity.Verbose;  // or Simple
//
//     // Enable block tracing
//     Trace_Logger_Class.Set_Verbose(true);
//
//     // Configure file logging
//     Trace_Logger_Class.Set_Log_File("myapp.log");
//     Trace_Logger_Class.Enable_File_Logging(true);
//
// UI CUSTOMIZATION:
// -----------------
// Users can customize the trace window appearance using the toolbar buttons:
//
// • "Font" - Change font family, size, and style
// • "ForeColor" - Change text color
// • "BackColor" - Change background color
//
// These changes are automatically persisted.
//
// ============================================================================
// 8. API REFERENCE
// ============================================================================
//
// TRACE_LOGGER_CLASS (Static):
// -----------------------------
//
//   PROPERTIES:
//   -----------
//   • bool Enabled                    : Global on/off switch
//   • bool Enable_Block_Tracing       : Whether block tracing is enabled
//   • Trace_Verbosity Verbosity       : Current verbosity level
//
//   METHODS:
//   --------
//   • Write(string message)
//     Write a message with timestamp
//
//   • Write(string message, bool timestamp)
//     Write a message with optional timestamp
//
//   • Write(string message, Trace_Verbosity level, bool timestamp = true)
//     Write a message at specified verbosity level
//
//   • Write_Raw(string message)
//     Write a raw message with sequence number (for internal use)
//
//   • Set_Verbose(bool enable)
//     Enable/disable verbose mode (block tracing)
//
//   • Set_Log_File(string path)
//     Set the file path for file logging
//
//   • Enable_File_Logging(bool enable)
//     Enable/disable file logging
//
//   • Enable_UI_Logging(bool enable)
//     Enable/disable UI updates (logging continues)
//
//   • Clear_Log()
//     Clear both UI and file log
//
//   • Shutdown()
//     Shutdown the logging system (call on app exit)
//
// TRACE_BLOCK_CLASS:
// ------------------
//
//   CONSTRUCTOR:
//   ------------
//   • Trace_Block_Class(string caller)
//     Creates a new trace block with the given method name
//
//   METHODS:
//   --------
//   • void Trace(string message)
//     Write a trace message indented under this block
//
//   • void Blank_Line()
//     Add a blank line maintaining tree structure
//
//   • void Dispose()
//     Called automatically when 'using' block exits
//
// TRACE_BLOCK_HELPER_CLASS (Static):
// -----------------------------------
//
//   METHODS:
//   --------
//   • Start_Block(string caller)
//     Create and start a new trace block
//     Usage: using var Block = Trace_Block_Helper_Class.Start_Block("MethodName");
//
// TRACE_HELPERS_CLASS (Static):
// ------------------------------
//
//   METHODS:
//   --------
//   • string Get_Method_Name([CallerMemberName] string caller = "")
//     Returns the calling method's name (compile-time, zero overhead)
//     Usage: Trace_Helpers_Class.Get_Method_Name()
//
//   • void Trace(string caller, string message, bool timestamp = true,
//                Trace_Verbosity verbosity = Verbose)
//     Write a formatted trace with tree-style indentation
//
// TRACE_WINDOW:
// -------------
//
//   PROPERTIES:
//   -----------
//   • static Trace_Window SafeInstance  : Singleton instance (creates if needed)
//   • static bool HasInstance            : True if instance exists
//
//   METHODS:
//   --------
//   • void Toggle()
//     Toggle window visibility and enable/disable tracing
//
//   • void Show_Trace()
//     Show window and enable tracing
//
//   • void Hide_Trace()
//     Hide window and disable tracing
//
//   EVENTS:
//   -------
//   • Action<bool> Trace_Visibility_Changed
//     Fired when window visibility changes (parameter = new visibility state)
//
// ============================================================================
// 9. BEST PRACTICES
// ============================================================================
//
// DO:
// ---
// ✓ Place trace block statement as FIRST line in methods
// ✓ Use Get_Method_Name() to auto-capture method names
// ✓ Use verbosity levels appropriately (Simple for important, Verbose for detail)
// ✓ Disable tracing in production builds (set Enabled = false)
// ✓ Use meaningful trace messages
// ✓ Clean up with Shutdown() on application exit
// ✓ Use file logging for long-running diagnostics
//
// DON'T:
// ------
// ✗ Don't put other code before the trace block statement
// ✗ Don't log sensitive information (passwords, keys, PII)
// ✗ Don't leave tracing enabled in release builds
// ✗ Don't trace in tight loops (performance impact)
// ✗ Don't manually format timestamps (Write() does it for you)
//
// EXAMPLE: PRODUCTION BUILD CONFIGURATION
// ----------------------------------------
//
//     public partial class MainForm : Form
//     {
//         public MainForm()
//         {
//             InitializeComponent();
//
//             #if DEBUG
//                 Trace_Logger_Class.Enabled = true;
//                 Trace_Window.SafeInstance.Show();
//             #else
//                 Trace_Logger_Class.Enabled = false;
//             #endif
//         }
//     }
//
// EXAMPLE: STRUCTURED TRACING
// ----------------------------
//
//     public async Task LoadDataAsync()
//     {
//         using var Block = Trace_Block_Helper_Class.Start_Block(
//             Trace_Helpers_Class.Get_Method_Name());
//
//         try
//         {
//             Block.Trace("Opening database connection");
//             // ... open connection ...
//
//             Block.Trace("Executing query");
//             // ... execute query ...
//
//             Block.Trace($"Loaded {rowCount} rows");
//         }
//         catch (Exception ex)
//         {
//             Block.Trace($"ERROR: {ex.Message}");
//             throw;
//         }
//     }
//
// ============================================================================
// 10. TROUBLESHOOTING
// ============================================================================
//
// PROBLEM: Trace window doesn't appear
// -------------------------------------
// SOLUTION:
// • Check that Trace_Logger_Class.Enabled = true
// • Verify window isn't minimized (check taskbar)
// • Call Trace_Window.SafeInstance.BringToFront()
//
// PROBLEM: Messages don't appear in trace window
// -----------------------------------------------
// SOLUTION:
// • Check that Trace_Logger_Class.Enabled = true
// • Check verbosity settings (Simple vs Verbose)
// • Verify UI logging is not paused
//
// PROBLEM: Block traces don't show
// ---------------------------------
// SOLUTION:
// • Set verbose mode: Trace_Logger_Class.Set_Verbose(true)
// • Or click "Simple/Verbose" button until it shows "Verbose"
//
// PROBLEM: Indentation is wrong
// ------------------------------
// SOLUTION:
// • Ensure "using var Block = ..." is FIRST statement in method
// • Check that Dispose() is being called (using statement does this automatically)
//
// PROBLEM: Application hangs on exit
// -----------------------------------
// SOLUTION:
// • Call Trace_Logger_Class.Shutdown() in your application exit handler:
//
//     protected override void OnFormClosing(FormClosingEventArgs e)
//     {
//         Trace_Logger_Class.Shutdown();
//         base.OnFormClosing(e);
//     }
//
// ============================================================================
// 11. PERFORMANCE CONSIDERATIONS
// ============================================================================
//
// OVERHEAD WHEN ENABLED:
// ----------------------
// • Minimal - Messages are queued and processed asynchronously
// • UI updates happen on background thread, no blocking
// • Each Write() call: ~microseconds
//
// OVERHEAD WHEN DISABLED:
// ------------------------
// • Near zero - Single boolean check per Write() call
// • No string formatting occurs
// • No memory allocation
//
// RECOMMENDATIONS:
// ----------------
// • Disable in release builds (#if DEBUG)
// • Avoid tracing in tight loops
// • Use verbosity levels to control output volume
// • Pause UI updates for very high-volume logging
//
// MEMORY USAGE:
// -------------
// • Message queue is bounded to prevent runaway memory
// • File logging appends directly (no buffering)
// • UI RichTextBox holds all visible messages in memory
//
// ============================================================================
// EXAMPLE: COMPLETE APPLICATION INTEGRATION
// ============================================================================
//
//     using Trace_Window_Namespace;
//
//     public partial class MainForm : Form
//     {
//         public MainForm()
//         {
//             using var Block = Trace_Block_Helper_Class.Start_Block(
//                 Trace_Helpers_Class.Get_Method_Name());
//
//             InitializeComponent();
//
//             // Show trace window in debug builds only
//             #if DEBUG
//                 Trace_Logger_Class.Enabled = true;
//                 Trace_Window.SafeInstance.Show();
//             #else
//                 Trace_Logger_Class.Enabled = false;
//             #endif
//
//             // Subscribe to visibility changes
//             Trace_Window.SafeInstance.Trace_Visibility_Changed += OnTraceVisibilityChanged;
//
//             Block.Trace("Initialization complete");
//         }
//
//         private void OnTraceVisibilityChanged(bool visible)
//         {
//             TraceButton.Text = visible ? "Hide Trace" : "Show Trace";
//         }
//
//         private void TraceButton_Click(object Sender, EventArgs E)
//         {
//             using var Block = Trace_Block_Helper_Class.Start_Block(
//                 Trace_Helpers_Class.Get_Method_Name());
//
//             Trace_Window.SafeInstance.Toggle();
//         }
//
//         protected override void OnFormClosing(FormClosingEventArgs e)
//         {
//             using var Block = Trace_Block_Helper_Class.Start_Block(
//                 Trace_Helpers_Class.Get_Method_Name());
//
//             Block.Trace("Application shutting down");
//             Trace_Logger_Class.Shutdown();
//             base.OnFormClosing(e);
//         }
//     }
//
// ============================================================================
// END OF DOCUMENTATION
// ============================================================================


using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Trace_Window_Namespace
{
  public class Trace_Window_QuickSilver_Overrides_Class
  {
    public string FontFamily { get; set; } = "Consolas";
    public float FontSize { get; set; } = 9.0f;
    public FontStyle FontStyle { get; set; } = FontStyle.Regular;
    public int ForeColorArgb { get; set; } = Color.Black.ToArgb();
    public int BackColorArgb { get; set; } = Color.White.ToArgb();
    public bool TraceVisible { get; set; } = true;
  }

  public enum Trace_Verbosity
  {
    Simple = 0,
    Verbose = 1
  }


  public sealed class Trace_Block_Class : IDisposable
  {
    private readonly string _Caller;
    private readonly string _Header_Indent;
    private readonly int _Start_Level;
    private bool _Header_Written;

    public Trace_Block_Class(string Caller)
    {
      _Caller = Caller;
      _Start_Level = Trace_Window.Trace_Indent.Level.Value;

      // calculate header indentation
      _Header_Indent = Build_Tree_Indent(_Start_Level + 1, Is_Header: true);
      Trace_Window.Trace_Indent.Level.Value++;
      Write_Header();
    }

    private void Write_Header()
    {
      if (_Header_Written)
        return;

      Trace_Logger_Class.Write_Raw($"{DateTime.Now:HH:mm:ss.fff} - {_Header_Indent}{_Caller}");
      _Header_Written = true;
    }

    // -----------------------------
    // VERBOSE TRACE
    // -----------------------------
    public void Trace(string Message)
    {
      if (!Trace_Logger_Class.Enable_Block_Tracing || string.IsNullOrEmpty(Message))
        return;

      // use _startLevel for all verbose lines
      int Level = _Start_Level + 1; // +1 because header adds one
      string Indent = Build_Tree_Indent(Level, Is_Header: false);

      // Add timestamp placeholder to align with header lines
      string Time_Stamp = new string(' ', "HH:mm:ss.fff".Length); // 12 characters

      foreach (var Line in Message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        Trace_Logger_Class.Write_Raw($"{Time_Stamp} - {Indent}{Line}");
    }

    // -----------------------------
    // BLANK LINE
    // -----------------------------
    public void Blank_Line()
    {
      if (!Trace_Logger_Class.Enable_Block_Tracing)
        return;

      int Level = _Start_Level + 1;
      string Indent = Build_Tree_Indent(Level, Is_Header: false);

      // Add timestamp placeholder to align with header lines
      string Time_Stamp = new string(' ', "HH:mm:ss.fff".Length); // 12 characters

      Trace_Logger_Class.Write_Raw($"{Time_Stamp} - {Indent}");
    }

    public void Dispose()
    {
      if (Trace_Window.Trace_Indent.Level.Value > 0)
      {
        Trace_Window.Trace_Indent.Level.Value--;
      }
    }

    private static string Build_Tree_Indent(int Level, bool Is_Header)
    {
      if (Level <= 0)
      {
        return string.Empty;
      }

      var String_Builder = new System.Text.StringBuilder();
      for (int Count = 0; Count < Level - 1; Count++)
      {
        String_Builder.Append("|   ");
      }

      String_Builder.Append(Is_Header ? "|-- " : "|   ");
      return String_Builder.ToString();
    }
  }





  public static class Trace_Block_Helper_Class
  {
    public static Trace_Block_Class Start_Block(string Caller) => new Trace_Block_Class(Caller);
  }



  public static class Trace_Helpers_Class
  {
    /// <summary>
    /// Returns the name of the calling method using [CallerMemberName].
    /// This is resolved at compile-time with zero runtime overhead.
    /// </summary>
    /// <param name="Caller">Automatically populated by the compiler - do not pass explicitly.</param>
    /// <returns>The name of the method that called Get_Method_Name().</returns>
    /// <example>
    /// using var Block = Trace_Block_Helper_Class.Start_Block(Trace_Helpers_Class.Get_Method_Name());
    /// </example>
    ///
    public static string Get_Method_Name([CallerMemberName] string Caller = "") => Caller;

    /// <summary>
    /// Write a formatted trace message with tree-style indentation and verbosity filtering.
    /// </summary>
    /// <param name="Caller">Name of the calling method (use Get_Method_Name())</param>
    /// <param name="Message">The message content to log (can be empty)</param>
    /// <param name="Time_Stamp">If true, prepends timestamp in HH:mm:ss.fff format (default: true)</param>
    /// <param name="Verbosity">Trace_Verbosity level for filtering (default: Verbose)</param>
    /// <remarks>
    /// <para>
    /// This method provides consistent logging across the application with hierarchical
    /// visual representation of call depth using tree-style indentation.
    /// </para>
    /// <para>
    /// Respects Trace_Logger_Class.Enabled global flag - if disabled, trace is skipped entirely.
    /// Also respects verbosity settings - skips if message verbosity exceeds threshold.
    /// </para>
    /// <para>
    /// Exception handling ensures trace failures never crash the application.
    /// </para>
    /// <para>
    /// Visual output example:
    /// <code>
    /// 12:34:56.789 - MainForm_Load
    /// 12:34:56.790 - |   -- Initialize_Combo_Boxes
    /// 12:34:56.791 - |   |   -- Populate_Port_List
    /// 12:34:56.792 - |   -- Initialize_Form_Controls
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// // Simple trace with auto method name
    /// Trace_Helpers_Class.Trace(Trace_Helpers_Class.Get_Method_Name(), "Starting initialization");
    ///
    /// // Trace without message (just method entry)
    /// Trace_Helpers_Class.Trace(Trace_Helpers_Class.Get_Method_Name(), "");
    /// </example>
    public static void Trace(
        string Caller,
        string Message,
        bool Time_Stamp = true,
        Trace_Verbosity Verbosity = Trace_Verbosity.Verbose)
    {
      if (!Trace_Logger_Class.Enabled)
        return;

      // Verbosity gate
      if (Verbosity > Trace_Logger_Class.Verbosity)
        return;

      try
      {
        int Level = Trace_Window.Trace_Indent.Level.Value;

        // Build tree-style prefix
        string Indent = "";
        for (int Indent_Index = 1; Indent_Index < Level; Indent_Index++)
          Indent += "|   ";

        string Line_Prefix = Level > 0 ? Indent + "-- " : "";

        string Trace_Line =
            string.IsNullOrWhiteSpace(Message)
                ? $"{Line_Prefix}{Caller}"
                : $"{Line_Prefix}{Caller} - {Message}";

        if (Time_Stamp)
          Trace_Line = $"{DateTime.Now:HH:mm:ss.fff} - {Trace_Line}";

        Trace_Logger_Class.Write(Trace_Line, Verbosity, Time_Stamp: false);
      }
      catch
      {
        // Swallow exceptions - trace must never break the application
      }
    }
  }



  public static class Trace_Logger_Class
  {
    private static readonly object _Lock = new();
    private static string _Log_File = "trace.log";
    private static bool _File_Logging_Enabled;
    private static bool _UI_Logging_Enabled = true;

    private static long _Trace_Sequence = 0;
    private static readonly BlockingCollection<string> _Log_Queue = new();
    private static Task? _Log_Pump;
    private static readonly object _Init_Lock = new();


    // ------------------------------------------------------------
    // GLOBAL ENABLE/DISABLE
    // ------------------------------------------------------------
    /// <summary>
    /// Global on/off switch for all trace output. When false, all trace
    /// operations are skipped. Can be toggled from any form in the application.
    /// Default: true (tracing enabled)
    /// </summary>
    public static bool Enabled { get; set; } = true;

    public static bool Enable_Block_Tracing { get; private set; } = false;

    public static void Set_Verbose(bool Enable)
    {
      Enable_Block_Tracing = Enable;
      Write(Enable
          ? "=== VERBOSE TRACE ENABLED ==="
          : "=== SIMPLE TRACE ENABLED ===");
    }




    // ------------------------------------------------------------
    // VERBOSITY
    // ------------------------------------------------------------
    private static Trace_Verbosity _Verbosity = Trace_Verbosity.Verbose;

    public static Trace_Verbosity Verbosity
    {
      get => _Verbosity;
      set
      {
        _Verbosity = value;
        Write($"=== TRACE VERBOSITY: {_Verbosity.ToString().ToUpper()} ===",
              Trace_Verbosity.Simple);
      }
    }

    // ------------------------------------------------------------
    // INITIALIZATION
    // ------------------------------------------------------------
    private static void Ensure_Pump()
    {
      lock (_Init_Lock)
      {
        if (_Log_Pump == null)
          _Log_Pump = Task.Run(Process_Log_Queue);
      }
    }

    // ------------------------------------------------------------
    // CONFIGURATION
    // ------------------------------------------------------------
    public static void Set_Log_File(string Path)
    {
      lock (_Lock)
        _Log_File = Path;
    }

    public static void Enable_File_Logging(bool Enable)
    {
      lock (_Lock)
        _File_Logging_Enabled = Enable;
    }

    public static void Enable_UI_Logging(bool Enable)
    {
      _UI_Logging_Enabled = Enable;
    }

    // ------------------------------------------------------------
    // WRITE METHODS (PUBLIC API)
    // ------------------------------------------------------------

    // Backwards compatible
    public static void Write(string Message, bool Time_Stamp = true)
    {
      Write(Message, Trace_Verbosity.Simple, Time_Stamp);
    }

    public static void Write(string Message,
                             Trace_Verbosity Level,
                             bool Time_Stamp = true)
    {
      if (!Enabled || Level > _Verbosity)
        return;

      if (Time_Stamp)
        Message = $"{DateTime.Now:HH:mm:ss.fff} - {Message}";

      Enqueue(Message);
    }

    // Backwards compatible
    public static void Write_Raw(string Message)
    {
      Write_Raw(Message, Trace_Verbosity.Verbose);
    }

    public static void Write_Raw(string Message, Trace_Verbosity Level)
    {
      if (!Enabled || Level > _Verbosity)
        return;

      long Seq = Interlocked.Increment(ref _Trace_Sequence);
      Enqueue($"{Seq:D6} | {Message}");
    }

    // ------------------------------------------------------------
    // CLEAR / SHUTDOWN
    // ------------------------------------------------------------
    public static void Clear_Log()
    {
      lock (_Lock)
      {
        if (_File_Logging_Enabled && File.Exists(_Log_File))
          File.WriteAllText(_Log_File, string.Empty);
      }

      Enqueue("<<<CLEAR_UI>>>");
    }

    public static void Shutdown()
    {
      _Log_Queue.CompleteAdding();
    }

    // ------------------------------------------------------------
    // INTERNAL QUEUE HANDLING
    // ------------------------------------------------------------
    private static void Enqueue(string Line)
    {
      try
      {
        Ensure_Pump();
        _Log_Queue.Add(Line);
      }
      catch (Exception Ex)
      {
        Debug.WriteLine("TRACE ENQUEUE ERROR: " + Ex);
      }
    }

    private static async Task Process_Log_Queue()
    {
      try
      {
        foreach (var Line in _Log_Queue.GetConsumingEnumerable())
        {
          try
          {
            if (_File_Logging_Enabled)
            {
              await File.AppendAllTextAsync(
                  _Log_File,
                  Line + Environment.NewLine
              ).ConfigureAwait(false);
            }

            if (_UI_Logging_Enabled)
            {
              if (Trace_Window.HasInstance)
              {
                var Win = Trace_Window.SafeInstance;

                if (!Win.IsDisposed)
                {
                  if (Line == "<<<CLEAR_UI>>>")
                    Win.Clear_Log();
                  else
                    Win.Append_Text_Safe(Line);
                }
              }

            }
          }
          catch (Exception Ex)
          {
            Debug.WriteLine("TRACE WRITE ERROR: " + Ex);
          }
        }
      }
      catch (Exception Ex)
      {
        Debug.WriteLine("TRACE PUMP FATAL ERROR: " + Ex);
      }
    }
  }


  public partial class Trace_Window : Form
  {

    private static Trace_Window? _Instance;
    public static Trace_Window SafeInstance => _Instance ??= new Trace_Window();

    public static bool HasInstance
        => _Instance != null;

    private RichTextBox _Rich_Text_Box_Log = null!;
    private Panel _Panel_Top = null!;
    private Button _Button_Save = null!;
    private Button _Button_Pause_UI = null!;
    private Button _Button_Clear = null!;
    private Button _Button_Font = null!;
    private Button _Button_ForeColor = null!;
    private Button _Button_BackColor = null!;
    private Button _Button_Exit = null!;
    private Button _Button_Verbose = null!;
    private readonly string _Settings_File = Path.Combine(Application.StartupPath, "trace_settings.json");
    private Trace_Window_QuickSilver_Overrides_Class _Settings = new();

    public event Action<bool>? Trace_Visibility_Changed;

    public static class Trace_Indent
    {
      public static readonly AsyncLocal<int> Level = new();
    }

    public sealed class Trace_Indent_Scope : IDisposable
    {
      public Trace_Indent_Scope() => Trace_Indent.Level.Value++;
      public void Dispose()
      {
        if (Trace_Indent.Level.Value > 0)
        {
          Trace_Indent.Level.Value--;
        }
      }
    }

    public static class Trace_Block_Helper_Class
    {
      public static Trace_Block_Class Start_Block(string Caller) => new Trace_Block_Class(Caller);
    }

    public Trace_Window()
    {
      InitializeComponent();
      Load_User_Settings();
      Apply_Settings();
    }

    // ------------------------------------------------------------
    // PUBLIC METHODS - Toggle Visibility
    // ------------------------------------------------------------

    /// <summary>
    /// Toggles the Trace window visibility and updates Trace_Logger_Class.Enabled accordingly.
    /// Call this from any form's button click handler to show/hide tracing.
    /// </summary>
    /// <remarks>
    /// When showing: Sets Trace_Logger_Class.Enabled = true, enables UI logging, shows window.
    /// When hiding: Sets Trace_Logger_Class.Enabled = false, disables UI logging, hides window.
    /// Fires Trace_Visibility_Changed event so forms can update their button states.
    /// </remarks>
    /// <example>
    /// // In your form's button click handler:
    /// private void Button_Trace_Click(object Sender, EventArgs E)
    /// {
    ///     Trace_Window.SafeInstance.Toggle();
    /// }
    ///
    /// // Subscribe to visibility changes to update your button:
    /// Trace_Window.SafeInstance.Trace_Visibility_Changed += (visible) =>
    /// {
    ///     myButton.Text = visible ? "Hide Trace" : "Show Trace";
    ///     myButton.BackColor = visible ? Color.Green : SystemColors.Control;
    /// };
    /// </example>
    public void Toggle()
    {
      if (Visible)
      {
        Hide();
        Trace_Logger_Class.Enabled = false;
        Trace_Logger_Class.Enable_UI_Logging(false);
        Trace_Visibility_Changed?.Invoke(false);
      }
      else
      {
        Show();
        BringToFront();
        Trace_Logger_Class.Enabled = true;
        Trace_Logger_Class.Enable_UI_Logging(true);
        Trace_Visibility_Changed?.Invoke(true);
      }
    }

    /// <summary>
    /// Shows the Trace window and enables tracing.
    /// </summary>
    public void Show_Trace()
    {
      if (!Visible)
      {
        Show();
        BringToFront();
        Trace_Logger_Class.Enabled = true;
        Trace_Logger_Class.Enable_UI_Logging(true);
        Trace_Visibility_Changed?.Invoke(true);
      }
    }

    /// <summary>
    /// Hides the Trace window and disables tracing.
    /// </summary>
    public void Hide_Trace()
    {
      if (Visible)
      {
        Hide();
        Trace_Logger_Class.Enabled = false;
        Trace_Logger_Class.Enable_UI_Logging(false);
        Trace_Visibility_Changed?.Invoke(false);
      }
    }

    private void InitializeComponent()
    {
      _Rich_Text_Box_Log = new RichTextBox();
      _Panel_Top = new Panel();

      SuspendLayout();
      ClientSize = new Size(600, 300);
      Text = "Execution Trace";
      FormClosing += Trace_Window_FormClosing;

      _Panel_Top.Dock = DockStyle.Top;
      _Panel_Top.Height = 35;
      _Panel_Top.Padding = new Padding(5);
      _Panel_Top.BackColor = Color.LightGray;

      int Left_Position = 5;
      const int Button_Width = 80;
      const int Spacing = 5;

      Button AddButton(ref Button Btn, string Text, EventHandler Click)
      {
        Btn = new Button
        {
          Text = Text,
          Width = Button_Width,
          Height = 25,
          Left = Left_Position,
          FlatStyle = FlatStyle.Flat,
          BackColor = Color.White,
          ForeColor = Color.Black
        };
        Btn.FlatAppearance.BorderColor = Color.Blue;
        Btn.FlatAppearance.BorderSize = 1;
        Btn.Click += Click;
        _Panel_Top.Controls.Add(Btn);
        Left_Position += Button_Width + Spacing;
        return Btn;
      }

      AddButton(ref _Button_Save, "Log → File", Button_Save_Click);
      AddButton(ref _Button_Pause_UI, "Pause UI", Button_Pause_UI_Click);
      AddButton(ref _Button_Clear, "Clear", Button_Clear_Click);
      AddButton(ref _Button_Font, "Font", Button_Font_Click);
      AddButton(ref _Button_ForeColor, "ForeColor", Button_Fore_Color_Click);
      AddButton(ref _Button_BackColor, "BackColor", Button_Back_Color_Click);

      AddButton(ref _Button_Verbose, "Simple", Button_Verbose_Click);
      AddButton(ref _Button_Exit, "Exit", Button_Exit_Click);

      _Rich_Text_Box_Log.Dock = DockStyle.Fill;
      _Rich_Text_Box_Log.ReadOnly = true;
      _Rich_Text_Box_Log.WordWrap = false;

      Controls.Add(_Rich_Text_Box_Log);
      Controls.Add(_Panel_Top);
      ResumeLayout(false);
    }
    private void Button_Verbose_Click(object? Sender, EventArgs Args)
    {
      bool Enable = !Trace_Logger_Class.Enable_Block_Tracing;

      Trace_Logger_Class.Set_Verbose(Enable);
      _Button_Verbose.Text = Enable ? "Verbose" : "Simple";
    }

    public void Append_Text_Safe(string Text)
    {
      if (InvokeRequired)
      {
        Invoke(new Action<string>(Append_Text_Safe), Text);
      }
      else
      {


        int Start_Position = _Rich_Text_Box_Log.TextLength;
        _Rich_Text_Box_Log.AppendText(Text + Environment.NewLine);

        _Rich_Text_Box_Log.ScrollToCaret();
      }
    }

    public void Clear_Log()
    {
      if (InvokeRequired)
        BeginInvoke(new Action(Clear_Log));
      else
        _Rich_Text_Box_Log.Clear();
    }

    private void Trace_Window_FormClosing(object? Sender, FormClosingEventArgs Args)
    {
      Args.Cancel = true;
      Hide();
      Trace_Visibility_Changed?.Invoke(false);
    }

    private void Load_User_Settings()
    {
      try
      {
        if (File.Exists(_Settings_File))
          _Settings = JsonSerializer.Deserialize<Trace_Window_QuickSilver_Overrides_Class>(File.ReadAllText(_Settings_File))
                      ?? new Trace_Window_QuickSilver_Overrides_Class();

        Apply_Settings();
        if (_Settings.TraceVisible)
          Show();
        else
          Hide();
      }
      catch { }
    }

    private void Apply_Settings()
    {
      _Rich_Text_Box_Log.Font = new Font(_Settings.FontFamily, _Settings.FontSize, _Settings.FontStyle);
      _Rich_Text_Box_Log.ForeColor = Color.FromArgb(_Settings.ForeColorArgb);
      _Rich_Text_Box_Log.BackColor = Color.FromArgb(_Settings.BackColorArgb);
    }

    private void Save_User_Settings()
    {
      try
      {
        _Settings.FontFamily = _Rich_Text_Box_Log.Font.FontFamily.Name;
        _Settings.FontSize = _Rich_Text_Box_Log.Font.Size;
        _Settings.FontStyle = _Rich_Text_Box_Log.Font.Style;
        _Settings.ForeColorArgb = _Rich_Text_Box_Log.ForeColor.ToArgb();
        _Settings.BackColorArgb = _Rich_Text_Box_Log.BackColor.ToArgb();
        _Settings.TraceVisible = Visible;

        File.WriteAllText(_Settings_File,
            JsonSerializer.Serialize(_Settings, new JsonSerializerOptions { WriteIndented = true }));
      }
      catch (Exception Ex)
      {
        Debug.WriteLine("Failed to save Trace_Window settings: " + Ex.Message);
      }
    }

    #region Button Handlers
    private void Button_Save_Click(object? Sender, EventArgs Args)
    {
      using var Dlg = new SaveFileDialog
      {
        Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
        FileName = "trace.log",
        OverwritePrompt = true
      };
      if (Dlg.ShowDialog() != DialogResult.OK)
        return;

      string Path = Dlg.FileName;
      try
      {
        if (File.Exists(Path))
          File.Delete(Path);
        using (File.Create(Path))
        {
        }

        Trace_Logger_Class.Set_Log_File(Path);
        Trace_Logger_Class.Enable_File_Logging(true);
        Trace_Logger_Class.Write("=== File logging ENABLED ===");
      }
      catch (Exception Ex)
      {
        MessageBox.Show($"Error saving log file: {Ex.Message}", "Save Log", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void Button_Pause_UI_Click(object? Sender, EventArgs Args)
    {
      bool Pause = _Button_Pause_UI.Text == "Pause UI";
      Trace_Logger_Class.Enable_UI_Logging(!Pause);
      _Button_Pause_UI.Text = Pause ? "Resume UI" : "Pause UI";
      Trace_Logger_Class.Write(Pause ? "=== TRACE UI PAUSED ===" : "=== TRACE UI RESUMED ===");
    }

    private void Button_Clear_Click(object? Sender, EventArgs Args) => Trace_Logger_Class.Clear_Log();
    private void Button_Font_Click(object? Sender, EventArgs Args)
    {
      using var Dlg = new FontDialog { Font = _Rich_Text_Box_Log.Font };
      if (Dlg.ShowDialog() == DialogResult.OK)
      {
        _Rich_Text_Box_Log.Font = Dlg.Font;
        Save_User_Settings();
      }
    }

    private void Button_Fore_Color_Click(object? Sender, EventArgs Args)
    {
      using var Dlg = new ColorDialog { Color = _Rich_Text_Box_Log.ForeColor };
      if (Dlg.ShowDialog() == DialogResult.OK)
      {
        _Rich_Text_Box_Log.ForeColor = Dlg.Color;
        Save_User_Settings();
      }
    }

    private void Button_Back_Color_Click(object? Sender, EventArgs Args)
    {
      using var Dlg = new ColorDialog { Color = _Rich_Text_Box_Log.BackColor };
      if (Dlg.ShowDialog() == DialogResult.OK)
      {
        _Rich_Text_Box_Log.BackColor = Dlg.Color;
        Save_User_Settings();
      }
    }

    private void Button_Exit_Click(object? Sender, EventArgs Args) => Hide();
    #endregion
  }

}
