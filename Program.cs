using Trace_Execution_Namespace;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  internal static class Program
  {
    [STAThread]
    static void Main ( )
    {
      Application.EnableVisualStyles ( );
      Application.SetCompatibleTextRenderingDefault ( false );

      using var Block = Trace_Block.Start_If_Enabled();

      Trace_Execution.Initialize ( true );

      // Unhandled exception on the UI thread
      Application.ThreadException += ( Sender, E ) =>
      {
        Capture_Trace.Write( $"UI thread exception: {E.Exception.GetType().Name} - {E.Exception.Message}" );
        Capture_Trace.Write( $"{E.Exception.StackTrace}" );
        Trace_Execution.Shutdown(); // force flush before showing dialog
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{E.Exception.Message}\n\n{E.Exception.StackTrace}",
            "Unhandled Exception",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error );
      };


      // Unhandled exception on background threads
      AppDomain.CurrentDomain.UnhandledException += ( Sender, E ) =>
      {
        string Message = E.ExceptionObject?.ToString() ?? "Unknown exception";
        Capture_Trace.Write( $"Unhandled exception (terminating={E.IsTerminating}): {Message}" );
        Trace_Execution.Shutdown(); // force flush
        if (E.IsTerminating)
        {
          // Write to a guaranteed fallback log file
          try
          {
            string Path = System.IO.Path.Combine(
                Environment.GetFolderPath( Environment.SpecialFolder.Desktop ),
                "MultiPoller_Crash.txt" );
            System.IO.File.WriteAllText( Path,
                $"Crash at {DateTime.Now}\n\n{Message}" );
          }
          catch { }

          MessageBox.Show(
              $"A fatal error occurred and the application must close:\n\n{Message}",
              "Fatal Error",
              MessageBoxButtons.OK,
              MessageBoxIcon.Error );
        }
      };

      // Unobserved Task exceptions (fire-and-forget tasks that throw)
      TaskScheduler.UnobservedTaskException += ( Sender, E ) =>
      {
        Capture_Trace.Write( $"Unobserved task exception: {E.Exception.Message}" );
        Capture_Trace.Write( $"{E.Exception.StackTrace}" );
        E.SetObserved();
      };

      // App shutting down cleanly
      Application.ApplicationExit += ( Sender, E ) =>
      {
        Capture_Trace.Write( "Application.ApplicationExit fired" );
      };

      AppDomain.CurrentDomain.ProcessExit += ( Sender, E ) =>
      {
        Capture_Trace.Write( "ProcessExit fired" );
        Trace_Execution.Shutdown();
      };

      Application.Run ( new Form1 ( ) );
    }
  }
}
