using Trace_Execution_Namespace;

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
      Application.Run ( new Form1 ( ) );
    }
  }
}
