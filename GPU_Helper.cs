using Rich_Text_Popup_Namespace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  internal static class GPU_Helper
  {
    // ── DXGI P/Invoke ────────────────────────────────────────────────────────
    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid( "7b7166ec-21c7-44ae-b21a-c9ae321ae369" )]
    [System.Runtime.InteropServices.InterfaceType(
      System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown )]
    private interface IDXGIFactory
    {
      void _VtblGap0_3();
      [System.Runtime.InteropServices.PreserveSig]
      int EnumAdapters( uint Index, [System.Runtime.InteropServices.Out] out IDXGIAdapter Adapter );
    }

    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid( "2411e7e1-12ac-4ccf-bd14-9798e8534dc0" )]
    [System.Runtime.InteropServices.InterfaceType(
      System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown )]
    private interface IDXGIAdapter
    {
      void _VtblGap0_3();
      [System.Runtime.InteropServices.PreserveSig]
      int GetDesc( out DXGI_ADAPTER_DESC Desc );
    }

    [System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential,
                                                   CharSet =
                                                     System.Runtime.InteropServices.CharSet.Unicode )]
    private struct DXGI_ADAPTER_DESC
    {
      [System.Runtime.InteropServices.MarshalAs( System.Runtime.InteropServices.UnmanagedType.ByValTStr,
                                                  SizeConst = 128 )]
      public string Description;
      public uint VendorId;
      public uint DeviceId;
      public uint SubSysId;
      public uint Revision;
      public nint DedicatedVideoMemory;
      public nint DedicatedSystemMemory;
      public nint SharedSystemMemory;
      public System.Runtime.InteropServices.ComTypes.FILETIME AdapterLuid;
    }

    [System.Runtime.InteropServices.DllImport( "dxgi.dll" )]
    private static extern int CreateDXGIFactory( [System.Runtime.InteropServices.In] ref Guid Riid, [
      System.Runtime.InteropServices.Out
    ] out IDXGIFactory PpFactory );

    // ── Public data bag ───────────────────────────────────────────────────────
    public record DxgiAdapterInfo( string Description,
                                               string VendorName,
                                               long DedicatedVideoMemory,
                                               long SharedSystemMemory );

    // ── Public API ────────────────────────────────────────────────────────────
  


    public static bool GPU_Available()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      // Pass 1: LHM
      try
      {
        var Computer = new LibreHardwareMonitor.Hardware.Computer { IsGpuEnabled = true };
        Computer.Open();
        bool Found = Computer.Hardware.Any(
          H => H.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuNvidia ||
               H.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuAmd ||
               H.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuIntel );
        Computer.Close();
        if (Found)
        {
          Capture_Trace.Write( "GPU found via LHM" );
          return true;
        }
      }
      catch (Exception Ex)
      {
        Capture_Trace.Write( $"LHM pass failed: {Ex.Message}" );
      }

      // Pass 2: WMI
      try
      {
        using var Searcher = new ManagementObjectSearcher(
          "SELECT Name, AdapterCompatibility FROM Win32_VideoController" );

        foreach (ManagementObject Obj in Searcher.Get())
        {
          var Name = Obj[ "Name" ]?.ToString() ?? "";
          var Compat = Obj[ "AdapterCompatibility" ]?.ToString() ?? "";

          bool Is_Software =
            Name.IndexOf( "Microsoft Basic", StringComparison.OrdinalIgnoreCase ) >= 0 ||
            Name.IndexOf( "Remote Desktop", StringComparison.OrdinalIgnoreCase ) >= 0 ||
            Name.IndexOf( "VirtualBox", StringComparison.OrdinalIgnoreCase ) >= 0 ||
            Name.IndexOf( "VMware", StringComparison.OrdinalIgnoreCase ) >= 0 ||
            Compat.Equals( "Microsoft", StringComparison.OrdinalIgnoreCase );

          Capture_Trace.Write( $"WMI adapter '{Name}' — software={Is_Software}" );

          if (!Is_Software)
          {
            Capture_Trace.Write( "GPU found via WMI" );
            return true;
          }
        }
      }
      catch (Exception Ex)
      {
        Capture_Trace.Write( $"WMI pass failed: {Ex.Message}" );
      }

      // Pass 3: DXGI
      try
      {
        bool Found = Check_Via_DXGI();
        Capture_Trace.Write( $"DXGI returned: {Found}" );
        return Found;
      }
      catch (Exception Ex)
      {
        Capture_Trace.Write( $"DXGI pass failed: {Ex.Message}" );
      }

      return false;
    }

    public static List<DxgiAdapterInfo> Get_Adapter_Info()
    {
      var Result = new List<DxgiAdapterInfo>();
      var Iid = new Guid( "7b7166ec-21c7-44ae-b21a-c9ae321ae369" );

      if (CreateDXGIFactory( ref Iid, out var Factory ) < 0 || Factory == null)
        return Result;

      const int DXGI_ERROR_NOT_FOUND = unchecked((int) 0x887A0002);
      const int Max_Adapters = 16; // safety cap

      try
      {
        for (uint I = 0; I < Max_Adapters; I++)
        {
          int Hr = Factory.EnumAdapters( I, out var Adapter );
          if (Hr == DXGI_ERROR_NOT_FOUND || Adapter == null)
            break;
          if (Hr < 0)
            break; // any other COM error

          Adapter.GetDesc( out var Desc );
          if (Desc.VendorId == 0x1414)
            continue;

          Result.Add( new DxgiAdapterInfo( Desc.Description,
                                           Desc.VendorId switch
                                           {
                                             0x10DE => "NVIDIA",
                                             0x1002 => "AMD",
                                             0x8086 => "Intel",
                                             0x5143 => "Qualcomm",
                                             _ => $"Unknown (0x{Desc.VendorId:X4})"
                                           },
                                           Desc.DedicatedVideoMemory,
                                           Desc.SharedSystemMemory ) );
        }
      }
      catch
      {
      }

      return Result;
    }

    public static string Format_Bytes( object? Raw )
    {
      if (Raw == null)
        return "N/A";
      if (!ulong.TryParse( Raw.ToString(), out var Bytes ) || Bytes == 0)
        return "N/A";
      return Bytes >= 1024UL * 1024 * 1024 ? $"{Bytes / (1024.0 * 1024 * 1024):F1} GB"
                                           : $"{Bytes / (1024.0 * 1024):F0} MB";
    }

    public static string Format_Bytes_Long( long Bytes )
    {
      if (Bytes <= 0)
        return "N/A";
      return Bytes >= 1024L * 1024 * 1024 ? $"{Bytes / (1024.0 * 1024 * 1024):F1} GB"
                                          : $"{Bytes / (1024.0 * 1024):F0} MB";
    }

    public static string Format_WMI_Date( string? Wmi_Date )
    {
      if (string.IsNullOrWhiteSpace( Wmi_Date ) || Wmi_Date.Length < 8)
        return "N/A";
      return $"{Wmi_Date[ 4..6 ]}/{Wmi_Date[ 6..8 ]}/{Wmi_Date[ 0..4 ]}";
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private static bool Is_Software_Adapter( DXGI_ADAPTER_DESC Desc )
    {
      // Microsoft software renderer / WARP
      if (Desc.VendorId == 0x1414)
        return true;

      // Known virtual/software vendors
      if (Desc.VendorId == 0x15AD || // VMware
           Desc.VendorId == 0x80EE || // VirtualBox
           Desc.VendorId == 0x1AB8)  // Parallels
        return true;

      // Fallback: description-based filter
      var Name = Desc.Description ?? "";
      if (Name.IndexOf( "Microsoft Basic", StringComparison.OrdinalIgnoreCase ) >= 0 ||
           Name.IndexOf( "WARP", StringComparison.OrdinalIgnoreCase ) >= 0 ||
           Name.IndexOf( "Remote Desktop", StringComparison.OrdinalIgnoreCase ) >= 0 ||
           Name.IndexOf( "VMware", StringComparison.OrdinalIgnoreCase ) >= 0 ||
           Name.IndexOf( "VirtualBox", StringComparison.OrdinalIgnoreCase ) >= 0 ||
           Name.IndexOf( "Hyper-V", StringComparison.OrdinalIgnoreCase ) >= 0)
        return true;

      return false;
    }

    private static bool Check_Via_DXGI()
    {
      using var Block = Trace_Block.Start_If_Enabled();

      var Iid = typeof( IDXGIFactory ).GUID;
      if (CreateDXGIFactory( ref Iid, out var Factory ) < 0 || Factory == null)
        return false;

      try
      {
        const int DXGI_ERROR_NOT_FOUND = unchecked((int) 0x887A0002);
        const int Max_Adapters = 16;

        for (uint I = 0; I < Max_Adapters; I++)
        {
          int Hr = Factory.EnumAdapters( I, out var Adapter );

          if (Hr == DXGI_ERROR_NOT_FOUND)
            break;
          if (Hr < 0 || Adapter == null)
            break;

          try
          {
            Adapter.GetDesc( out var Desc );
            Capture_Trace.Write( $"DXGI adapter [{Desc.VendorId:X4}]: {Desc.Description}" );

            if (!Is_Software_Adapter( Desc ))
              return true;
          }
          finally
          {
            Marshal.ReleaseComObject( Adapter );
          }
        }
      }
      catch (Exception Ex)
      {
        Capture_Trace.Write( $"DXGI enumeration failed: {Ex.Message}" );
      }
      finally
      {
        Marshal.ReleaseComObject( Factory );
      }

      return false;
    }

    public static bool Discrete_GPU_Available()
    {
      // Pass 1: LHM
      try
      {
        var Computer = new LibreHardwareMonitor.Hardware.Computer { IsGpuEnabled = true };
        Computer.Open();
        bool Found = Computer.Hardware.Any(
          H => H.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuNvidia ||
               H.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuAmd );
        Computer.Close();
        if (Found)
          return true;
      }
      catch { }

      // Pass 2: WMI
      try
      {
        using var Searcher = new ManagementObjectSearcher(
          "SELECT Name, AdapterCompatibility FROM Win32_VideoController" );

        foreach (ManagementObject Obj in Searcher.Get())
        {
          var Name = Obj[ "Name" ]?.ToString() ?? "";
          var Compat = Obj[ "AdapterCompatibility" ]?.ToString() ?? "";

          bool Is_Software =
            Name.IndexOf( "Microsoft Basic", StringComparison.OrdinalIgnoreCase ) >= 0 ||
            Name.IndexOf( "Remote Desktop", StringComparison.OrdinalIgnoreCase ) >= 0 ||
            Name.IndexOf( "VirtualBox", StringComparison.OrdinalIgnoreCase ) >= 0 ||
            Name.IndexOf( "VMware", StringComparison.OrdinalIgnoreCase ) >= 0 ||
            Name.IndexOf( "Intel", StringComparison.OrdinalIgnoreCase ) >= 0 ||
            Compat.Equals( "Microsoft", StringComparison.OrdinalIgnoreCase );

          if (!Is_Software)
            return true;
        }
      }
      catch { }

      // Pass 3: DXGI
      try
      {
        return Get_Adapter_Info().Any( A => A.VendorName is "NVIDIA" or "AMD" or "Qualcomm" );
      }
      catch { }

      return false;
    }


  }
    internal static class GPU_Data_Collector
    {
      public class WMI_Controller
      {
        public string Name = "";
        public string DriverVersion = "";
        public string DriverDate = "";
        public string Status = "";
        public string VRAM = "";
        public string Resolution = "";
        public string RefreshRate = "";
        public string ColorDepth = "";
        public string VideoMode = "";
      }

      public class LHM_Sensor_Row
      {
        public bool Is_Header = false;
        public string Name = "";
        public string Value = "";
        public Color Color = Color.Black;
      }

      public class GPU_Data
      {
        public List<WMI_Controller> WMI_Controllers = new();
        public string? WMI_Error = null;
        public List<GPU_Helper.DxgiAdapterInfo> DXGI_Adapters = new();
        public string? DXGI_Error = null;
        public List<LHM_Sensor_Row> LHM_Sensors = new();
        public string? LHM_Error = null;
      }

      public static GPU_Data Collect()
      {
        var Data = new GPU_Data();

        // ── WMI ─────────────────────────────────────────────────────────────
        try
        {
          using var Searcher = new System.Management.ManagementObjectSearcher( "SELECT * FROM " +
                                                                               "Win32_VideoController" );

          foreach (System.Management.ManagementObject Obj in Searcher.Get())
          {
            var Name = Obj[ "Name" ]?.ToString() ?? "Unknown";
            var Compat = Obj[ "AdapterCompatibility" ]?.ToString() ?? "";

            bool Is_Software = Name.IndexOf( "Microsoft Basic", StringComparison.OrdinalIgnoreCase ) >= 0 ||
                               Name.IndexOf( "Remote Desktop", StringComparison.OrdinalIgnoreCase ) >= 0 ||
                               Name.IndexOf( "VirtualBox", StringComparison.OrdinalIgnoreCase ) >= 0 ||
                               Name.IndexOf( "VMware", StringComparison.OrdinalIgnoreCase ) >= 0 ||
                               Compat.IndexOf( "Microsoft", StringComparison.OrdinalIgnoreCase ) >= 0;

            if (Is_Software)
              continue;

            Data.WMI_Controllers.Add( new WMI_Controller
            {
              Name = Name,
              DriverVersion = Obj[ "DriverVersion" ]?.ToString() ?? "N/A",
              DriverDate = GPU_Helper.Format_WMI_Date( Obj[ "DriverDate" ]?.ToString() ),
              Status = Obj[ "Status" ]?.ToString() ?? "N/A",
              VRAM = GPU_Helper.Format_Bytes( Obj[ "AdapterRAM" ] ),
              Resolution = $"{Obj[ "CurrentHorizontalResolution" ]} x {Obj[ "CurrentVerticalResolution" ]}",
              RefreshRate = $"{Obj[ "CurrentRefreshRate" ]} Hz",
              ColorDepth = $"{Obj[ "CurrentBitsPerPixel" ]} bpp",
              VideoMode = Obj[ "VideoModeDescription" ]?.ToString() ?? "N/A"
            } );
          }
        }
        catch (Exception Ex)
        {
          Data.WMI_Error = Ex.Message;
        }

        // ── DXGI ────────────────────────────────────────────────────────────
        try
        {
          Data.DXGI_Adapters = GPU_Helper.Get_Adapter_Info();
        }
        catch (Exception Ex)
        {
          Data.DXGI_Error = Ex.Message;
        }

        // ── LHM ─────────────────────────────────────────────────────────────
        try
        {
          var Computer = new LibreHardwareMonitor.Hardware.Computer { IsGpuEnabled = true };
          Computer.Open();

          foreach (var HW in Computer.Hardware)
          {
            bool Is_GPU = HW.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuNvidia ||
                          HW.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuAmd ||
                          HW.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuIntel;

            if (!Is_GPU)
              continue;
            HW.Update();

            Data.LHM_Sensors.Add( new LHM_Sensor_Row { Is_Header = true, Name = HW.Name } );

            foreach (var Sensor in HW.Sensors)
            {
              if (Sensor.Value == null)
                continue;

              var (Label, Unit, Row_Color) =
                Sensor.SensorType switch
                {
                  LibreHardwareMonitor.Hardware.SensorType.Temperature =>
                                             ("Temperature",
                                               "°C",
                                               Sensor.Value > 80 ? Color.FromArgb( 180, 60, 0 )
                                                                 : Color.FromArgb( 0, 140, 0 )),
                  LibreHardwareMonitor.Hardware.SensorType.Load =>
                  ("Load", "%", Color.FromArgb( 160, 120, 0 )),
                  LibreHardwareMonitor.Hardware.SensorType.Fan =>
                  ("Fan Speed", " RPM", Color.FromArgb( 0, 120, 160 )),
                  LibreHardwareMonitor.Hardware.SensorType.Clock =>
                  ("Clock", " MHz", Color.FromArgb( 40, 40, 40 )),
                  LibreHardwareMonitor.Hardware.SensorType.Power =>
                  ("Power", " W", Color.FromArgb( 120, 0, 160 )),
                  LibreHardwareMonitor.Hardware.SensorType.SmallData =>
                  ("Memory", " MB", Color.FromArgb( 40, 40, 40 )),
                  _ => (null, null, Color.Transparent)
                };

              if (Label == null)
                continue;

              Data.LHM_Sensors.Add( new LHM_Sensor_Row
              {
                Name = Sensor.Name,
                Value = $"{Sensor.Value:F1}{Unit}",
                Color = Row_Color
              } );
            }
          }

          Computer.Close();
        }
        catch (Exception Ex)
        {
          Data.LHM_Error = Ex.Message.Contains( "access" ) || Ex.Message.Contains( "privilege" )
                             ? "Sensor access unavailable — try running as administrator."
                             : $"LHM error: {Ex.Message}";
        }

        return Data;
      }




    }

    internal static class GPU_Snapshot
    {
      // ── Data bags ──────────────────────────────────────────────────────────────
      public class Sensor_Value
      {
        public string Name = "";
        public float Value = 0f;
      }

      public class Snapshot
      {
        public DateTime Taken = DateTime.Now;
        public List<Sensor_Value> Load = new();
        public List<Sensor_Value> Temperature = new();
        public List<Sensor_Value> Memory = new();
        public List<Sensor_Value> Clock = new();
        public List<Sensor_Value> Power = new();
        public List<Sensor_Value> Fan = new();
      }

      public class Snapshot_Delta
      {
        public TimeSpan Duration;
        public List<(string Name, float Start, float End, float Delta)> Load = new();
        public List<(string Name, float Start, float End, float Delta)> Temperature = new();
        public List<(string Name, float Start, float End, float Delta)> Memory = new();
        public List<(string Name, float Start, float End, float Delta)> Clock = new();
        public List<(string Name, float Start, float End, float Delta)> Power = new();
        public List<(string Name, float Start, float End, float Delta)> Fan = new();
      }

      // ── Capture ────────────────────────────────────────────────────────────────
      public static Snapshot Capture()
      {
        var S = new Snapshot();

        try
        {
          var Computer = new LibreHardwareMonitor.Hardware.Computer { IsGpuEnabled = true };
          Computer.Open();

          foreach (var HW in Computer.Hardware)
          {
            if (HW.HardwareType != LibreHardwareMonitor.Hardware.HardwareType.GpuNvidia &&
                 HW.HardwareType != LibreHardwareMonitor.Hardware.HardwareType.GpuAmd &&
                 HW.HardwareType != LibreHardwareMonitor.Hardware.HardwareType.GpuIntel)
              continue;

            HW.Update();

            foreach (var Sensor in HW.Sensors)
            {
              if (Sensor.Value == null)
                continue;


            // the D3D sensors (D3D 3D, D3D Compute_0,
            // multiple D3D Video Decode, D3D Copy entries)
            // are coming from LHM reporting per-process D3D activity.
            // These are real but noisy. You can filter them out in
            // GPU_Data_Collector or GPU_Snapshot.Capture() by
            // skipping sensors whose name starts with D3D:

            if (Sensor.Name.StartsWith( "D3D", StringComparison.OrdinalIgnoreCase ))
              continue;


            var Row = new Sensor_Value { Name = Sensor.Name, Value = Sensor.Value.Value };

              switch (Sensor.SensorType)
              {
                case LibreHardwareMonitor.Hardware.SensorType.Load:
                  S.Load.Add( Row );
                  break;
                case LibreHardwareMonitor.Hardware.SensorType.Temperature:
                  S.Temperature.Add( Row );
                  break;
                case LibreHardwareMonitor.Hardware.SensorType.SmallData:
                  S.Memory.Add( Row );
                  break;
                case LibreHardwareMonitor.Hardware.SensorType.Clock:
                  S.Clock.Add( Row );
                  break;
                case LibreHardwareMonitor.Hardware.SensorType.Power:
                  S.Power.Add( Row );
                  break;
                case LibreHardwareMonitor.Hardware.SensorType.Fan:
                  S.Fan.Add( Row );
                  break;
              }
            }
          }

          Computer.Close();
        }
        catch
        {
        }

        return S;
      }

      // ── Diff ───────────────────────────────────────────────────────────────────
      public static Snapshot_Delta Diff( Snapshot Start, Snapshot End )
      {
        var D = new Snapshot_Delta { Duration = End.Taken - Start.Taken };

        D.Load = Diff_List( Start.Load, End.Load );
        D.Temperature = Diff_List( Start.Temperature, End.Temperature );
        D.Memory = Diff_List( Start.Memory, End.Memory );
        D.Clock = Diff_List( Start.Clock, End.Clock );
        D.Power = Diff_List( Start.Power, End.Power );
        D.Fan = Diff_List( Start.Fan, End.Fan );

        return D;
      }

      private static List<(string Name, float Start, float End, float Delta)> Diff_List(
        List<Sensor_Value> Start, List<Sensor_Value> End )
      {
        var Result = new List<(string, float, float, float)>();

        foreach (var E in End)
        {
          var S = Start.FirstOrDefault( X => X.Name == E.Name );
          if (S == null)
            continue;
          Result.Add( (E.Name, S.Value, E.Value, E.Value - S.Value) );
        }

        return Result;
      }

      // ── Summary Popup ──────────────────────────────────────────────────────────
      public static void Show_GPU_Session_Summary( Form Owner,
                                                   GPU_Snapshot.Snapshot Baseline,
                                                   GPU_Snapshot.Snapshot End_Snap )
      {
        var Delta = GPU_Snapshot.Diff( Baseline, End_Snap );

        const int Col = 28;

        using var Popup = new Rich_Text_Popup( "GPU Session Summary", 560, 600, Resizable: true );

        Popup.Add_Title( "GPU Session Summary" );
        Popup.Add_Row( "Session Duration", Format_Duration( Delta.Duration ), null, Col );
        Popup.Add_Blank();

        // ── Load ──────────────────────────────────────────────────────────────────
        if (Delta.Load.Count > 0)
        {
          Popup.Add_Heading_Mono( "Core Load (%)" );
          foreach (var (Name, Start, End, D) in Delta.Load)
            Popup.Add_Row( Name, Format_Delta( Start, End, D, "%" ), Delta_Color( D ), Col );
          Popup.Add_Blank();
        }

        // ── Temperature ───────────────────────────────────────────────────────────
        if (Delta.Temperature.Count > 0)
        {
          Popup.Add_Heading_Mono( "Temperature (°C)" );
          foreach (var (Name, Start, End, D) in Delta.Temperature)
            Popup.Add_Row( Name, Format_Delta( Start, End, D, "°C" ), Temp_Color( End ), Col );
          Popup.Add_Blank();
        }

        // ── Memory ────────────────────────────────────────────────────────────────
        if (Delta.Memory.Count > 0)
        {
          Popup.Add_Heading_Mono( "Memory (MB)" );
          foreach (var (Name, Start, End, D) in Delta.Memory)
            Popup.Add_Row( Name, Format_Delta( Start, End, D, " MB" ), Delta_Color( D ), Col );
          Popup.Add_Blank();
        }

        // ── Clock ─────────────────────────────────────────────────────────────────
        if (Delta.Clock.Count > 0)
        {
          Popup.Add_Heading_Mono( "Clock Speed (MHz)" );
          foreach (var (Name, Start, End, D) in Delta.Clock)
            Popup.Add_Row( Name, Format_Delta( Start, End, D, " MHz" ), null, Col );
          Popup.Add_Blank();
        }

        // ── Power ─────────────────────────────────────────────────────────────────
        if (Delta.Power.Count > 0)
        {
          Popup.Add_Heading_Mono( "Power (W)" );
          foreach (var (Name, Start, End, D) in Delta.Power)
            Popup.Add_Row( Name, Format_Delta( Start, End, D, " W" ), null, Col );
          Popup.Add_Blank();
        }

        // ── Fan ───────────────────────────────────────────────────────────────────
        if (Delta.Fan.Count > 0)
        {
          Popup.Add_Heading_Mono( "Fan Speed (RPM)" );
          foreach (var (Name, Start, End, D) in Delta.Fan)
            Popup.Add_Row( Name, Format_Delta( Start, End, D, " RPM" ), null, Col );
          Popup.Add_Blank();
        }

        // ── No data ───────────────────────────────────────────────────────────────
        if (Delta.Load.Count == 0 && Delta.Temperature.Count == 0 && Delta.Memory.Count == 0 &&
             Delta.Clock.Count == 0)
        {
          Popup.Add_Warning( "  No sensor data — try running as administrator." );
        }

        Popup.Show_Popup( Owner );
      }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    // In GPU_Snapshot
    internal static string Format_Duration( TimeSpan T )
    {
      if (T.TotalHours >= 1)
        return $"{(int) T.TotalHours}h {T.Minutes:D2}m {T.Seconds:D2}s";
      if (T.TotalMinutes >= 1)
        return $"{T.Minutes}m {T.Seconds:D2}s";
      return $"{T.Seconds}s";
    }

    private static string Format_Delta( float Start, float End, float D, string Unit )
      {
        string Arrow = D > 0.5f ? "▲" : D < -0.5f ? "▼" : " ";
        string Start_Str = $"{Start:F1}{Unit}";
        string End_Str = $"{End:F1}{Unit}";
        string Delta_Str = $"{Arrow}{Math.Abs( D ):F1}";

        // Fixed column widths: start=10, end=10, delta=6
        return $"{Start_Str.PadRight( 10 )}→  {End_Str.PadRight( 10 )}{Delta_Str.PadLeft( 6 )}";
      }

      private static Color Delta_Color( float D ) => D > 10f ? Color.FromArgb( 180, 60, 0 )
          : // orange — notable rise
            D < -10f
                                                       ? Color.FromArgb( 0, 120, 0 )
                                                       :                             // green  — notable drop
                                                       Color.FromArgb( 40, 40, 40 ); // neutral

      private static Color Temp_Color( float End ) => End > 85f ? Color.FromArgb( 180, 0, 0 )
          : // red    — hot
            End > 70f
                                                        ? Color.FromArgb( 180, 60, 0 )
                                                        :                            // orange — warm
                                                        Color.FromArgb( 0, 120, 0 ); // green  — cool
    }
  }

