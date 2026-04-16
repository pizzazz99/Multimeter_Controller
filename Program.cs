using Trace_Execution_Namespace;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  internal static class Program
  {
    [STAThread]
    static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault( false );

      string Crash_Log = System.IO.Path.Combine(
        Environment.GetFolderPath( Environment.SpecialFolder.Desktop ),
        "MultiPoller_Crash.txt" );

      // Initialize trace BEFORE anything else
      Trace_Execution.Initialize( true );

      // Unhandled exception on the UI thread
      Application.ThreadException += ( Sender, E ) =>
      {
        Capture_Trace.Write( $"UI thread exception: {E.Exception.GetType().Name} - {E.Exception.Message}" );
        Capture_Trace.Write( $"{E.Exception.StackTrace}" );
        Trace_Execution.Shutdown();

        System.IO.File.WriteAllText( Crash_Log,
          $"UI Thread Exception at {DateTime.Now}\r\n\r\n" +
          $"Type:    {E.Exception.GetType().FullName}\r\n" +
          $"Message: {E.Exception.Message}\r\n\r\n" +
          $"Stack:\r\n{E.Exception.StackTrace}\r\n\r\n" +
          $"Inner:   {E.Exception.InnerException?.GetType().FullName}\r\n" +
          $"         {E.Exception.InnerException?.Message}\r\n" +
          $"         {E.Exception.InnerException?.StackTrace}" );

        MessageBox.Show(
          $"An unexpected error occurred:\n\n{E.Exception.Message}\n\nSee desktop crash log.",
          "Unhandled Exception",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error );
      };

      // Unhandled exception on background threads
      AppDomain.CurrentDomain.UnhandledException += ( Sender, E ) =>
      {
        string Message = E.ExceptionObject?.ToString() ?? "Unknown exception";
        Capture_Trace.Write( $"Unhandled exception (terminating={E.IsTerminating}): {Message}" );
        Trace_Execution.Shutdown();

        System.IO.File.WriteAllText( Crash_Log,
          $"Unhandled Exception at {DateTime.Now}\r\n\r\n{Message}" );

        if (E.IsTerminating)
          MessageBox.Show(
            $"A fatal error occurred and the application must close.\n\nSee desktop crash log.",
            "Fatal Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error );
      };

      // Unobserved Task exceptions
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

      // Wrap Application.Run to catch field initializer / constructor crashes
      try
      {
        Application.Run( new Form1() );
      }
      catch (Exception ex)
      {
        System.IO.File.WriteAllText( Crash_Log,
          $"Crash at {DateTime.Now}\r\n\r\n" +
          $"Type:    {ex.GetType().FullName}\r\n" +
          $"Message: {ex.Message}\r\n\r\n" +
          $"Stack:\r\n{ex.StackTrace}\r\n\r\n" +
          $"Inner:   {ex.InnerException?.GetType().FullName}\r\n" +
          $"         {ex.InnerException?.Message}\r\n" +
          $"         {ex.InnerException?.StackTrace}" );

        MessageBox.Show( $"Crash — see desktop log.\n\n{ex.Message}",
                         "Fatal Error",
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Error );
      }
    }
  }
}
