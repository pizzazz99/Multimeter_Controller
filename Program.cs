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
      Trace_Execution.Initialize ( true );

      // Unhandled exception on the UI thread
      Application.ThreadException += ( Sender, E ) =>
      {
        Capture_Trace.Write( $"UI thread exception: {E.Exception.GetType().Name} - {E.Exception.Message}" );
        Capture_Trace.Write( $"{E.Exception.StackTrace}" );
      };

      // Unhandled exception on background threads
      AppDomain.CurrentDomain.UnhandledException += ( Sender, E ) =>
      {
        Capture_Trace.Write( $"Unhandled exception (terminating={E.IsTerminating}): {E.ExceptionObject}" );
      };

      // Unobserved Task exceptions (fire-and-forget tasks that throw)
      TaskScheduler.UnobservedTaskException += ( Sender, E ) =>
      {
        Capture_Trace.Write( $"Unobserved task exception: {E.Exception.Message}" );
        E.SetObserved(); // prevents process termination
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
