
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    GPU_Helper.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Centralised GPU detection, enumeration, live sensor monitoring, and
//   session-delta reporting.  Four cooperating types cover the full pipeline
//   from hardware discovery through real-time data collection to formatted
//   popup display:
//
//     GPU_Helper          Low-level detection and DXGI adapter enumeration.
//     GPU_Data_Collector  One-shot snapshot of WMI, DXGI, and LHM data.
//     GPU_Snapshot        LHM-based live sensor capture and session diffing.
//     Sensor_Value /
//     Snapshot /
//     Snapshot_Delta      Plain data bags passed between the above.
//
// ── GPU_Helper ────────────────────────────────────────────────────────────────
//
//   Static helper that answers two questions: "is any usable GPU present?" and
//   "what adapters are installed?"  All detection uses a three-pass strategy so
//   that a failure in one subsystem does not prevent detection via another.
//
//   DETECTION PASSES (applied in order, short-circuits on first positive)
//
//     Pass 1 — LibreHardwareMonitor (LHM)
//       Opens an LHM Computer with IsGpuEnabled = true and checks whether any
//       hardware entry has type GpuNvidia, GpuAmd, or GpuIntel.  Most reliable
//       on gaming/workstation hardware.  May fail on locked-down or minimal
//       Windows installs that deny driver access.
//
//     Pass 2 — WMI Win32_VideoController
//       Queries Name and AdapterCompatibility.  Excludes entries matching
//       "Microsoft Basic", "Remote Desktop", "VirtualBox", "VMware", or whose
//       AdapterCompatibility is "Microsoft".  Faster than DXGI but less
//       information-rich; used as a middle-ground fallback.
//
//     Pass 3 — DXGI (CreateDXGIFactory / IDXGIFactory.EnumAdapters)
//       Enumerates adapters via the native DXGI COM interface, filters out
//       software renderers by VendorId and description string, and returns true
//       if any real adapter remains.  Most authoritative source of VRAM and
//       adapter identity; used last because it requires dxgi.dll p/invoke.
//
//   PUBLIC METHODS
//
//     GPU_Available() → bool
//       Runs all three passes (LHM → WMI → DXGI) and returns true if any pass
//       finds a non-software GPU.  Used by Application_Settings.Detect_Hardware()
//       to set Discrete_GPU_Available.  Each pass is independently try/caught;
//       failures are traced but do not propagate.
//
//     Discrete_GPU_Available() → bool
//       Stricter variant that requires NVIDIA, AMD, or Qualcomm vendor ID in
//       the DXGI pass and excludes Intel from the WMI pass.  Called by
//       Application_Settings when deciding whether GPU rendering is viable.
//
//     Get_Adapter_Info() → List<DxgiAdapterInfo>
//       Enumerates all non-Microsoft adapters via DXGI and returns a list of
//       DxgiAdapterInfo records.  VendorId is mapped to a human-readable name:
//         0x10DE → "NVIDIA", 0x1002 → "AMD", 0x8086 → "Intel",
//         0x5143 → "Qualcomm", anything else → "Unknown (0xNNNN)".
//       Capped at 16 adapters as a safety guard against malformed DXGI state.
//       Used by GPU_Data_Collector.Collect() and Discrete_GPU_Available().
//
//     Format_Bytes( object? Raw ) → string
//       Converts a WMI AdapterRAM object (stored as a boxed ulong string) to
//       a human-readable "N.N GB" or "N MB" string.  Returns "N/A" for null,
//       unparseable, or zero values.
//
//     Format_Bytes_Long( long Bytes ) → string
//       Same conversion for a typed long (used with DXGI DedicatedVideoMemory /
//       SharedSystemMemory, which are nint values cast to long).
//
//     Format_WMI_Date( string? Wmi_Date ) → string
//       Converts a WMI CIM_DATETIME string (yyyyMMddHHmmss...) to MM/DD/YYYY.
//       Returns "N/A" for null, whitespace, or strings shorter than 8 chars.
//
//   RECORD
//
//     DxgiAdapterInfo( Description, VendorName, DedicatedVideoMemory,
//                      SharedSystemMemory )
//       Immutable record returned by Get_Adapter_Info().  DedicatedVideoMemory
//       and SharedSystemMemory are raw nint values cast to long (bytes).  Use
//       Format_Bytes_Long() to display them.
//
//   PRIVATE METHODS
//
//     Is_Software_Adapter( DXGI_ADAPTER_DESC ) → bool
//       Authoritative software-renderer filter used by Check_Via_DXGI().
//       Checks VendorId first (0x1414 Microsoft, 0x15AD VMware, 0x80EE
//       VirtualBox, 0x1AB8 Parallels), then falls back to description-string
//       matching for WARP, Remote Desktop, and Hyper-V entries that may report
//       an unexpected VendorId.
//
//     Check_Via_DXGI() → bool
//       Core DXGI enumeration loop.  Creates a factory via CreateDXGIFactory,
//       iterates adapters up to the 16-adapter cap, calls Is_Software_Adapter()
//       on each descriptor, and returns true on the first non-software adapter.
//       Releases COM objects via Marshal.ReleaseComObject() in finally blocks
//       to avoid reference leaks.
//
//   DXGI COM INTEROP  (private, nested inside GPU_Helper)
//
//     IDXGIFactory  IID 7b7166ec-21c7-44ae-b21a-c9ae321ae369
//       _VtblGap0_3()  — skips the three IUnknown vtable slots
//       EnumAdapters( uint Index, out IDXGIAdapter ) → HRESULT [PreserveSig]
//         Returns DXGI_ERROR_NOT_FOUND (0x887A0002) past the last adapter.
//
//     IDXGIAdapter  IID 2411e7e1-12ac-4ccf-bd14-9798e8534dc0
//       _VtblGap0_3()  — same IUnknown gap pattern
//       GetDesc( out DXGI_ADAPTER_DESC ) → HRESULT [PreserveSig]
//
//     DXGI_ADAPTER_DESC  Sequential / Unicode layout
//       Description           char[128] — driver-supplied adapter name
//       VendorId              PCI vendor (0x10DE NVIDIA, 0x1002 AMD, etc.)
//       DeviceId / SubSysId / Revision  PCI identity fields
//       DedicatedVideoMemory  GPU-local VRAM bytes (nint = SIZE_T)
//       DedicatedSystemMemory System RAM reserved exclusively for adapter
//       SharedSystemMemory    System RAM available as shared video memory
//       AdapterLuid           Session-scoped 64-bit adapter ID; marshalled
//                             as FILETIME for binary-layout compatibility;
//                             not used but required for correct struct size
//
//     CreateDXGIFactory  [DllImport("dxgi.dll")]
//       Entry point for factory creation.  Throws DllNotFoundException on
//       systems without dxgi.dll; caught by all callers.
//
// ── GPU_Data_Collector ────────────────────────────────────────────────────────
//
//   Performs a single synchronous collection sweep across all three data
//   sources (WMI, DXGI, LHM) and returns the results in a GPU_Data bag.
//   Intended for the GPU information popup; not called on a timer.
//
//   DATA CLASSES
//
//     WMI_Controller
//       Flattened view of one Win32_VideoController row:
//       Name, DriverVersion, DriverDate (formatted via Format_WMI_Date),
//       Status, VRAM (formatted via Format_Bytes), Resolution, RefreshRate,
//       ColorDepth, VideoMode.  Software adapters are silently skipped during
//       collection.
//
//     LHM_Sensor_Row
//       One display row for the LHM section: Is_Header flag, Name, Value
//       string, Color for RichTextBox rendering.  Header rows carry only a
//       hardware name and no value; sensor rows carry formatted value + unit.
//       Sensor types and their display colors:
//         Temperature  °C    green (< 80°C) / dark-orange (≥ 80°C)
//         Load         %     dark-yellow
//         Fan Speed    RPM   teal-blue
//         Clock        MHz   dark-gray
//         Power        W     purple
//         Memory (SmallData) MB  dark-gray
//       Sensors whose name starts with "D3D" are skipped (per-process D3D
//       activity entries are real but too noisy for the summary view).
//
//     GPU_Data
//       Root container returned by Collect():
//         WMI_Controllers   List<WMI_Controller>      (empty on WMI failure)
//         WMI_Error         string?                   (null on success)
//         DXGI_Adapters     List<DxgiAdapterInfo>     (empty on DXGI failure)
//         DXGI_Error        string?                   (null on success)
//         LHM_Sensors       List<LHM_Sensor_Row>      (empty on LHM failure)
//         LHM_Error         string?                   (null on success)
//       Each source is independently try/caught so a failure in one does not
//       prevent data collection from the others.
//
//   Collect() → GPU_Data
//     Runs WMI → DXGI → LHM in sequence, populating the relevant fields of
//     GPU_Data.  For LHM, calls HW.Update() before reading sensors.  On an
//     LHM access/privilege exception the error message is replaced with a
//     user-friendly "try running as administrator" hint.
//
// ── Sensor_Value / Snapshot / Snapshot_Delta ─────────────────────────────────
//
//   Plain data bags with no logic, used as the currency between GPU_Snapshot
//   methods.
//
//   Sensor_Value       Name (string) + Value (float)
//
//   Snapshot           Taken (DateTime) + six typed sensor lists:
//                        Load, Temperature, Memory, Clock, Power, Fan
//                      Each list holds Sensor_Value entries for one LHM read.
//
//   Snapshot_Delta     Duration (TimeSpan) + six parallel delta lists, each
//                      entry a (Name, Start, End, Delta) value tuple.
//                      Produced by GPU_Snapshot.Diff().
//
// ── GPU_Snapshot ──────────────────────────────────────────────────────────────
//
//   Provides LHM-based snapshot capture and session-level delta reporting.
//   Callers take a baseline snapshot before polling starts and an end snapshot
//   when it stops, then call Show_GPU_Session_Summary() to display the diff.
//
//   Capture() → Snapshot
//     Opens an LHM Computer, iterates GPU hardware (NVIDIA / AMD / Intel),
//     calls HW.Update(), and distributes sensor values into the appropriate
//     Snapshot lists.  Sensors whose name starts with "D3D" are skipped.
//     Returns an empty (but non-null) Snapshot if LHM is unavailable.
//
//   Diff( Snapshot Start, Snapshot End ) → Snapshot_Delta
//     Pairs each sensor in End with the matching name in Start and computes
//     (Start, End, Delta) tuples.  Sensors that appear in End but not Start
//     are silently dropped; new sensors that appeared mid-session are not
//     included in the delta.  Duration is End.Taken − Start.Taken.
//
//   Show_GPU_Session_Summary( Form Owner, Snapshot Baseline, Snapshot End_Snap )
//     Calls Diff() then renders the result into a Rich_Text_Popup with
//     per-section headings (Load, Temperature, Memory, Clock, Power, Fan).
//     Empty sections are omitted entirely.  If all delta lists are empty
//     (typically when LHM could not read sensors) a single warning row is
//     shown instead.  Value formatting:
//       Format_Delta( Start, End, Delta, Unit )
//         Fixed-width columns: start value (10 chars) → end value (10 chars)
//         delta arrow+magnitude (6 chars, left-padded).  Arrow: ▲ if Δ > 0.5,
//         ▼ if Δ < −0.5, space otherwise.
//       Delta_Color( float D ) → Color
//         Orange for D > 10 (notable rise), green for D < −10 (notable drop),
//         dark-gray otherwise.
//       Temp_Color( float End ) → Color
//         Red for End > 85°C, orange for > 70°C, green otherwise.
//       Format_Duration( TimeSpan T )
//         "Xh YYm ZZs" / "Xm YYs" / "Xs" depending on magnitude.
//         Internal visibility so GPU analysis forms can reuse it.
//
// NOTES
//   • All three detection/collection classes are internal static — nothing
//     outside the Multimeter_Controller namespace references them directly.
//   • Each public entry point is independently try/caught so a missing driver,
//     insufficient privilege, or absent dxgi.dll never surfaces as an unhandled
//     exception to the caller.
//   • LHM requires that the process have sufficient driver-access privileges to
//     read hardware sensors.  When it does not, error messages containing
//     "access" or "privilege" are replaced with a human-readable hint.
//   • The 16-adapter cap in Get_Adapter_Info() and Check_Via_DXGI() is a
//     safety guard against a pathological DXGI state returning success
//     indefinitely; normal systems have 1–3 adapters.
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════



using Rich_Text_Popup_Namespace;
using System.Management;
using System.Runtime.InteropServices;
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


  internal static class GPU_Snapshot
    {
    

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
                                                   Snapshot Baseline,
                                                   Snapshot End_Snap )
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

