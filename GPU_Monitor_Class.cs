// ════════════════════════════════════════════════════════════════════════════════
// FILE:    GPU_Monitor.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Polls GPU hardware sensors on a background thread at a configurable
//   interval and raises an event with the latest readings.  Designed to
//   run alongside the instrument polling loop without interfering with
//   GPIB timing or UI responsiveness.
//
//   All sensor reads happen on a dedicated background thread.
//   The UI receives updates via the Data_Updated event which the
//   consumer marshals to the UI thread via BeginInvoke.
//
// NOTES
//   Requires LibreHardwareMonitorLib NuGet package.
//   May require administrator privileges for full sensor access.
// ════════════════════════════════════════════════════════════════════════════════

using LibreHardwareMonitor.Hardware;
using Microsoft.VisualBasic.Devices;
using System.Diagnostics;

namespace Multimeter_Controller
{
  public class GPU_Stats
  {
    public string GPU_Name { get; set; }       = "Unknown";
    public float  Core_Load { get; set; }      = 0f; // %
    public float  Memory_Load { get; set; }    = 0f; // %
    public float  Core_Clock { get; set; }     = 0f; // MHz
    public float  Memory_Clock { get; set; }   = 0f; // MHz
    public float  Temperature { get; set; }    = 0f; // °C
    public float  Power { get; set; }          = 0f; // W
    public float  Memory_Used { get; set; }    = 0f; // MB
    public float  Memory_Total { get; set; }   = 0f; // MB
    public float  Fan_Speed { get; set; }      = 0f; // RPM
    public bool   Is_Available { get; set; }   = false;
    public string Render_Backend { get; set; } = ""; // "SkiaSharp/OpenGL"
  }

  public class GPU_Monitor : IDisposable
  {
    // ── Events ────────────────────────────────────────────────────────
    public event EventHandler<GPU_Stats>? Data_Updated;

    // ── State ─────────────────────────────────────────────────────────
    private LibreHardwareMonitor.Hardware.Computer? _Computer;
    private Thread? _Poll_Thread;
    private CancellationTokenSource? _Cts;
    private bool   _Disposed = false;
    private int    _Poll_Interval_Ms;
    private string _Render_Backend;

    public bool    Is_Running { get; private set; } = false;
    public bool    Is_Open { get; private set; }    = false;

    public GPU_Monitor( int Poll_Interval_Ms = 2000, string Render_Backend = "SkiaSharp/OpenGL" )
    {
      _Poll_Interval_Ms = Poll_Interval_Ms;
      _Render_Backend   = Render_Backend;
    }

    // ── Public API ────────────────────────────────────────────────────

    public void Show( Form Owner )
    {
      if ( Is_Open )
        return;

      _Gpu_Form             = new GPU_Monitor_Form( this );
      _Gpu_Form.FormClosed += ( s, e ) =>
      {
        Is_Open = false;
        Stop();
      };
      _Gpu_Form.Show( Owner );
      Is_Open = true;

      Start();
    }

    public void Close()
    {
      _Gpu_Form?.Close();
      Is_Open = false;
    }

    public void Start()
    {
      if ( Is_Running )
        return;

      _Cts       = new CancellationTokenSource();
      Is_Running = true;

      _Poll_Thread = new Thread( () => Poll_Loop( _Cts.Token ) ) {
        IsBackground = true,
        Name         = "GPU_Monitor_Thread",
        Priority     = ThreadPriority.BelowNormal, // ← never steals from poll loop
      };
      _Poll_Thread.Start();
    }

    public void Stop()
    {
      if ( ! Is_Running )
        return;
      _Cts?.Cancel();
      Is_Running = false;
    }

    // ── Background poll loop ──────────────────────────────────────────

    private void Poll_Loop( CancellationToken Token )
    {
      try
      {
        _Computer = new LibreHardwareMonitor.Hardware.Computer {
          IsGpuEnabled = true,
        };
        _Computer.Open();

        while ( ! Token.IsCancellationRequested )
        {
          var Stats = Read_GPU_Stats();
          Data_Updated?.Invoke( this, Stats );

          // Sleep in small increments so cancellation is responsive
          for ( int I = 0; I < _Poll_Interval_Ms / 100; I++ )
          {
            if ( Token.IsCancellationRequested )
              break;
            Thread.Sleep( 100 );
          }
        }
      }
      catch ( OperationCanceledException )
      {
      }
      catch ( Exception Ex )
      {
        System.Diagnostics.Debug.WriteLine( $"GPU_Monitor error: {Ex.Message}" );
      }
      finally
      {
        _Computer?.Close();
        _Computer = null;
      }
    }

    private GPU_Stats Read_GPU_Stats()
    {
      var Stats = new GPU_Stats {
        Render_Backend = _Render_Backend,
        Is_Available   = false,
      };

      if ( _Computer == null )
        return Stats;

      try
      {
        foreach ( var Hardware in _Computer.Hardware )
        {
          if ( Hardware.HardwareType != HardwareType.GpuNvidia &&
               Hardware.HardwareType != HardwareType.GpuAmd &&
               Hardware.HardwareType != HardwareType.GpuIntel )
            continue;

          Hardware.Update();
          Stats.GPU_Name     = Hardware.Name;
          Stats.Is_Available = true;

          foreach ( var Sensor in Hardware.Sensors )
          {
            if ( Sensor.Value == null )
              continue;
            float V = Sensor.Value.Value;

            switch ( Sensor.SensorType )
            {
              case SensorType.Load when Sensor.Name.Contains( "Core" ) :
                Stats.Core_Load = V;
                break;
              case SensorType.Load when Sensor.Name.Contains( "Memory" ) :
                Stats.Memory_Load = V;
                break;
              case SensorType.Clock when Sensor.Name.Contains( "Core" ) :
                Stats.Core_Clock = V;
                break;
              case SensorType.Clock when Sensor.Name.Contains( "Memory" ) :
                Stats.Memory_Clock = V;
                break;
              case SensorType.Temperature :
                Stats.Temperature = V;
                break;
              case SensorType.Power when Sensor.Name.Contains( "Package" ) :
                Stats.Power = V;
                break;
              case SensorType.SmallData when Sensor.Name.Contains( "Used" ) :
                Stats.Memory_Used = V;
                break;
              case SensorType.SmallData when Sensor.Name.Contains( "Total" ) :
                Stats.Memory_Total = V;
                break;
              case SensorType.Fan :
                Stats.Fan_Speed = V;
                break;
            }
          }
          break; // first GPU only
        }
      }
      catch ( Exception Ex )
      {
        System.Diagnostics.Debug.WriteLine( $"GPU read error: {Ex.Message}" );
      }

      return Stats;
    }

    // ── Dispose ───────────────────────────────────────────────────────

    private GPU_Monitor_Form? _Gpu_Form;

    public void Dispose()
    {
      if ( _Disposed )
        return;
      _Disposed = true;
      Stop();
      _Computer?.Close();
      _Cts?.Dispose();
    }
  }
}
