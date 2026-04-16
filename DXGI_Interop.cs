
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    DXGI_Interop.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Minimal COM interop layer for the DirectX Graphics Infrastructure (DXGI)
//   API, used exclusively to enumerate graphics adapters and read their
//   hardware description at startup.  This allows the application to detect
//   whether a discrete GPU is present without taking a dependency on any
//   managed DirectX wrapper library.
//
//   The only consumer is Application_Settings.Detect_Discrete_GPU(), which
//   calls CreateDXGIFactory() (p/invoked in Application_Settings.cs), casts
//   the returned pointer to IDXGIFactory, iterates adapters via EnumAdapters(),
//   and reads DXGI_ADAPTER_DESC.DedicatedVideoMemory and .Description to
//   classify each adapter as discrete, integrated, or software.
//
// WHY RAW COM RATHER THAN A LIBRARY
//   The application targets .NET 8 / WinForms and has no other DirectX usage.
//   Pulling in SharpDX, Vortice.Windows, or similar packages solely for GPU
//   detection would add significant package weight.  The three declarations
//   here replicate the exact subset of DXGI needed — factory creation,
//   adapter enumeration, and descriptor retrieval — with zero extra overhead.
//
// TYPES
//
//   IDXGIFactory  (COM interface, IID 7b7166ec-21c7-44ae-b21a-c9ae321ae369)
//     The root DXGI factory object, obtained by calling the native
//     CreateDXGIFactory export from dxgi.dll (p/invoked in
//     Application_Settings).  Only one method from the full DXGI factory
//     vtable is exposed here:
//
//       _VtblGap0_3()
//         Placeholder that accounts for the three inherited IUnknown slots
//         (QueryInterface, AddRef, Release) that sit before EnumAdapters in
//         the COM vtable.  The CLR COM interop layer requires that all vtable
//         slots up to the first used method be declared, even if never called.
//         Declaring them as a single void gap method with a generated name is
//         the standard pattern for skipping inherited-interface slots.
//
//       EnumAdapters( uint Index, out IDXGIAdapter Adapter ) → HRESULT
//         Returns the adapter at the given zero-based index.  Returns
//         DXGI_ERROR_NOT_FOUND (0x887A0002) when Index exceeds the number of
//         installed adapters, which is used as the loop-termination sentinel
//         in Detect_Discrete_GPU().  [PreserveSig] is required so the raw
//         HRESULT is returned to managed code rather than being converted to
//         a COMException — this lets the caller check for NOT_FOUND without
//         catching exceptions in the hot enumeration loop.
//
//   IDXGIAdapter  (COM interface, IID 2411e7e1-12ac-4ccf-bd14-9798e8534dc0)
//     Represents a single graphics adapter (physical or software).  Obtained
//     from IDXGIFactory.EnumAdapters().  Again only the directly needed
//     method is declared:
//
//       _VtblGap0_3()
//         Same IUnknown vtable-gap pattern as IDXGIFactory above.
//
//       GetDesc( out DXGI_ADAPTER_DESC Desc ) → HRESULT
//         Fills a DXGI_ADAPTER_DESC with the adapter's human-readable name,
//         vendor/device IDs, and memory sizes.  [PreserveSig] is applied for
//         the same reason as EnumAdapters — raw HRESULT propagation.
//
//   DXGI_ADAPTER_DESC  (blittable struct, Sequential / Unicode layout)
//     Direct memory-layout mirror of the native DXGI_ADAPTER_DESC structure.
//     Field-by-field mapping to the native struct:
//
//       Description           char[128] UTF-16 string — the display name of
//                             the adapter as reported by the driver, e.g.
//                             "NVIDIA GeForce RTX 4090" or
//                             "Intel(R) UHD Graphics 770".  Marshalled with
//                             UnmanagedType.ByValTStr / SizeConst=128 to
//                             match the fixed native array exactly.
//
//       VendorId              PCI vendor identifier.
//                               0x10DE = NVIDIA
//                               0x1002 = AMD / ATI
//                               0x8086 = Intel
//                               0x1414 = Microsoft (WARP / Basic Display)
//
//       DeviceId              PCI device identifier — driver-specific SKU code.
//
//       SubSysId              PCI subsystem identifier (board vendor + model).
//
//       Revision              PCI revision number.
//
//       DedicatedVideoMemory  Bytes of GPU-local VRAM (nint = SIZE_T).
//                             Non-zero for discrete cards; zero or very small
//                             for integrated GPUs that share system RAM.
//                             Used in Detect_Discrete_GPU() as a fallback
//                             heuristic: anything below 512 MB is treated as
//                             integrated even if the name string doesn't match
//                             a known integrated pattern.
//
//       DedicatedSystemMemory Bytes of system RAM reserved exclusively for
//                             this adapter.  Typically zero for both discrete
//                             and integrated adapters on modern hardware.
//
//       SharedSystemMemory    Bytes of system RAM that the adapter can use
//                             as video memory (shared with the CPU).  Large
//                             on integrated GPUs; present but small on
//                             discrete cards that support resizable BAR.
//
//       AdapterLuid           Locally unique 64-bit identifier for this
//                             adapter instance, valid only for the lifetime
//                             of the current Windows session.  Marshalled as
//                             FILETIME because both are 64-bit structs with
//                             identical binary layout.  Not used by this
//                             application but must be present to keep the
//                             struct the correct size for native marshalling.
//
// VTABLE GAP PATTERN — DETAIL
//   The native DXGI vtable layout is (slot 0 = first entry):
//     0  IUnknown::QueryInterface
//     1  IUnknown::AddRef
//     2  IUnknown::Release
//     3  IDXGIObject::SetPrivateData       ← not used
//     ...additional IDXGIObject slots...
//     N  IDXGIFactory::EnumAdapters        ← first slot we need
//   Declaring _VtblGap0_3() as a single no-return void method with the
//   [ComImport] machinery causes the CLR to skip exactly 3 vtable slots
//   (0, 1, 2) before wiring up EnumAdapters.  The number in the gap name
//   is a count, not an index.  If DXGI ever adds a slot before EnumAdapters
//   the gap count must be updated to match or the wrong native function will
//   be called silently.
//
// NOTES
//   • All three types are internal — nothing outside this file and
//     Application_Settings needs to reference DXGI types directly.
//   • The interop types are declaration-only; no implementation code lives
//     here.  The actual p/invoke entry point (CreateDXGIFactory) is declared
//     as a DllImport in Application_Settings.cs rather than here to keep the
//     hardware-detection logic co-located with the code that uses it.
//   • On systems where dxgi.dll is not present (pre-Vista, or heavily
//     stripped Windows installations) the p/invoke will throw a
//     DllNotFoundException, which Detect_Discrete_GPU() catches and
//     treats as "no discrete GPU found".
//
// REFERENCES
//   DXGI_ADAPTER_DESC (Windows SDK)  — 
//     https://learn.microsoft.com/windows/win32/api/dxgi/ns-dxgi-dxgi_adapter_desc
//   IDXGIFactory::EnumAdapters       —
//     https://learn.microsoft.com/windows/win32/api/dxgi/nf-dxgi-idxgifactory-enumadapters
//   IDXGIAdapter::GetDesc            —
//     https://learn.microsoft.com/windows/win32/api/dxgi/nf-dxgi-idxgiadapter-getdesc
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{

  [ System.Runtime.InteropServices.ComImport ]
  [ System.Runtime.InteropServices.Guid( "7b7166ec-21c7-44ae-b21a-c9ae321ae369" ) ]
  [ System.Runtime.InteropServices.InterfaceType(
    System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown ) ]
  internal interface IDXGIFactory
  {
    void _VtblGap0_3();
    [ System.Runtime.InteropServices.PreserveSig ]
    int EnumAdapters( uint Index, [ System.Runtime.InteropServices.Out ] out IDXGIAdapter Adapter );
  }

  [ System.Runtime.InteropServices.ComImport ]
  [ System.Runtime.InteropServices.Guid( "2411e7e1-12ac-4ccf-bd14-9798e8534dc0" ) ]
  [ System.Runtime.InteropServices.InterfaceType(
    System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown ) ]
  internal interface IDXGIAdapter
  {
    void _VtblGap0_3();
    [ System.Runtime.InteropServices.PreserveSig ]
    int GetDesc( out DXGI_ADAPTER_DESC Desc );
  }

  [ System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential,
                                                 CharSet = System.Runtime.InteropServices.CharSet.Unicode ) ]
  internal struct DXGI_ADAPTER_DESC
  {
    [ System.Runtime.InteropServices.MarshalAs( System.Runtime.InteropServices.UnmanagedType.ByValTStr,
                                                SizeConst = 128 ) ]
    public string                                           Description;
    public uint                                             VendorId;
    public uint                                             DeviceId;
    public uint                                             SubSysId;
    public uint                                             Revision;
    public nint                                             DedicatedVideoMemory;
    public nint                                             DedicatedSystemMemory;
    public nint                                             SharedSystemMemory;
    public System.Runtime.InteropServices.ComTypes.FILETIME AdapterLuid;
  }
}
