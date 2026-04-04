// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Instrument.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Defines the two core data-model classes used throughout the application:
//   Instrument (hardware identity and configuration) and Instrument_Series
//   (time-series data + O(1) running statistics for one instrument per session).
//
// ── CLASS: Instrument ────────────────────────────────────────────────────────
//
//   Represents a single physical instrument on the GPIB bus.  Holds identity,
//   address, and measurement configuration.  Shared by reference into every
//   Instrument_Series that wraps it.
//
//   Key properties
//     Name            Display name assigned by the user or auto-detected.
//     Meter_Roll      Role label (e.g. "DUT", "Reference") for multi-meter runs.
//     Address         GPIB bus address (0–30).
//     Verified        True once the address has been confirmed via IDN/ID? query.
//     Visible         Controls chart series visibility; toggled from the legend.
//     NPLC            Integration time in power-line cycles; drives Display_Digits.
//     Type            Meter_Type enum; drives Display_Digits lookup.
//     Display_Digits  Computed property — delegates to Compute_Display_Digits().
//
//   Compute_Display_Digits(Meter_Type, decimal NPLC)
//     Static pure function mapping (type, NPLC) → significant digit count:
//       HP3458A  — 4–10 digits depending on NPLC (≥0.01 → 4, ≥1000 → 10)
//       HP34401  — fixed 6 digits
//       HP34420  — fixed 7 digits
//       HP3456   — fixed 6 digits
//       others   — fixed 6 digits
//
//   Display / DisplayString(transportMode)
//     Short formatted strings for list boxes and combo boxes.
//     DisplayString omits the GPIB address in Direct Serial mode.
//
// ── CLASS: Instrument_Series ─────────────────────────────────────────────────
//
//   Wraps an Instrument with a time-series point list and incremental
//   (Welford) running statistics so that mean, variance, min, max, and RMS
//   are available in O(1) without iterating Points on every chart repaint.
//
//   Data
//     Points          List<(DateTime Time, double Value)> — the raw sample log.
//     Is_Recording    True while the polling loop is actively appending points.
//     File_Stats      Optional Dictionary loaded from a recorded CSV preamble;
//                     null during live sessions.
//
//   Error tracking
//     Consecutive_Errors   Resets to 0 on each successful read; used to
//                          trigger instrument disable after the configured
//                          threshold.
//     Total_Errors         Cumulative error count for the session.
//     Disconnect_Count     Number of times the instrument was lost and re-found.
//     Comm_Error_Count     Number of communication-level errors (timeout, etc.)
//
//   NPLC-derived properties (computed from Instrument.NPLC)
//     Integration_Ms       NPLC × (1000 / 60)  — integration window in ms.
//     Settle_Ms            Integration_Ms × 2   — accounts for autozero.
//     Readings_Per_Min     60 000 / Settle_Ms   — single-instrument throughput.
//     Get_Readings_Per_Min(n)  Same but divided by instrument count for
//                          multi-meter round-robin estimates.
//     NPLC_Warning_Text    "Slow settle" / "Moderate settle" / "Fast"
//     NPLC_Warning_Color   Orange / Blue / Black — for status label coloring.
//
//   Incremental statistics  (Welford online algorithm)
//     Add_Point_Value(double)   Must be called every time a point is appended
//                               to Points.  Updates _Stat_N, _Stat_Mean,
//                               _Stat_M2 (for variance), _Stat_Min, _Stat_Max,
//                               and _Stat_Sum_Sq (for RMS) in O(1).
//     Reset_Stats()             Clears Points and all accumulators; resets all
//                               error counters and File_Stats.
//
//   O(1) stat accessors
//     Get_Average()      Welford mean.
//     Get_StdDev()       Sample standard deviation (Bessel-corrected, N−1).
//     Get_Min()          Running minimum.
//     Get_Max()          Running maximum.
//     Get_RMS()          Root-mean-square from _Stat_Sum_Sq.
//     Get_Range()        Max − Min.
//     Get_Peak_To_Peak() Alias for Get_Range().
//     Get_Last()         Most recent point value; 0 if Points is empty.
//
//   O(n) accessors  (kept for callers that require them)
//     Get_Trend()        Compares the mean of the last 5 points to the
//                        preceding 5; returns "↑", "↓", or "→" (flat < 0.1%).
//                        Returns "—" with fewer than 10 points.
//     Get_Sample_Rate()  (Count − 1) / total elapsed seconds across all points.
//     Get_Average_From(source)  Average over an arbitrary point list.
//
// NOTES
//   • Add_Point_Value() and Points.Add() must be called together by the caller —
//     the class does not enforce this pairing internally.
//   • Instrument_Series does not own the Instrument; the same Instrument
//     instance may be referenced from the polling form and the series list
//     simultaneously.
//   • Display_Digits is a pass-through to Instrument.Display_Digits, ensuring
//     chart renderers always see the current value without caching stale digits.
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════

namespace Multimeter_Controller
{
  public class Instrument
  {

    public int Poll_Delay_Ms => (int) ((double) NPLC / 60.0 * 1000) + Comms_Overhead_Ms;
    public bool Is_Master { get; set; } = false;

    public string Name { get; set; } = "";
    /// <summary>Full VISA resource string for this instrument, if it was discovered
    /// or added via NI-VISA.  Empty for Prologix/serial instruments.</summary>
    public string Visa_Resource_String { get; set; } = "";
    public string Meter_Roll { get; set; } = string.Empty;
    public int Address
    {
      get; set;
    }
    public bool Verified
    {
      get; set;
    }
    public bool Visible { get; set; } = true;

    private decimal _NPLC = 1m;
    public decimal NPLC
    {
      get => _NPLC;
      set => _NPLC = value;   // no more Compute_Display_Digits call
    }

    public int Display_Digits => Compute_Display_Digits( _Type, _NPLC );

    private Meter_Type _Type;
    public Meter_Type Type
    {
      get => _Type;
      set => _Type = value;   // no more Compute_Display_Digits call
    }

    public static int Compute_Display_Digits( Meter_Type Type, decimal NPLC ) =>
        Type switch
        {
          Meter_Type.HP3458 =>
              NPLC >= 1000 ? 10 :
              NPLC >= 100 ? 9 :
              NPLC >= 10 ? 8 :
              NPLC >= 1 ? 7 :
              NPLC >= 0.1m ? 6 :
              NPLC >= 0.01m ? 5 : 4,
          Meter_Type.HP34401 => 6,
          Meter_Type.HP34420 => 7,
          Meter_Type.HP3456 => 6,
          _ => 6,
        };


    public int Comms_Overhead_Ms => Type switch
    {
      Meter_Type.HP3458 => 333,  // ~333ms comms overhead on top of NPLC time
      Meter_Type.HP34401 => 20,
      Meter_Type.Generic_GPIB => 50,
      _ => 20
    };

    public string Display => $"{Name}  (GPIB {Address})";

    public string DisplayString( string Transport_Mode )
    {
      bool Is_GPIB = Transport_Mode == "Prologix_GPIB" || Transport_Mode == "Prologix_Ethernet";
      return Is_GPIB ? $"{Name}  (GPIB {Address})" : Name;
    }
  }






  // ===== Data Model =====

  public class Instrument_Series
  {
    public Instrument Instrument { get; set; } = new Instrument();

    public decimal NPLC
    {
      get => Instrument.NPLC;
      set => Instrument.NPLC = value;
    }

    public int Disconnect_Count { get; set; } = 0;
    public int Comm_Error_Count { get; set; } = 0;
    public string Name => Instrument?.Name ?? "";

    public string Role => Instrument.Meter_Roll;
    public int Address => Instrument.Address;
    public Meter_Type Type => Instrument.Type;
    public bool Visible
    {
      get => Instrument.Visible;
      set => Instrument.Visible = value;
    }
    public Dictionary<string, string>? File_Stats { get; set; } = null;
    public List<(DateTime Time, double Value)> Points { get; set; } = new();
    public bool Is_Recording { get; set; } = false;
    public int Display_Digits => Instrument.Display_Digits;
    public int Consecutive_Errors { get; set; } = 0;
    public int Total_Errors { get; set; } = 0;
    public Color Line_Color
    {
      get; set;
    }


    // ── NPLC calculated properties ───────────────────────────────────
    public double Integration_Ms => (double) NPLC * (1000.0 / 60.0);
    public double Settle_Ms => Integration_Ms * 2.0;   // autozero doubles it
    public int Readings_Per_Min => Settle_Ms > 0 ? (int) (60000.0 / Settle_Ms) : 0;

    public string NPLC_Warning_Text => Settle_Ms >= 1000 ? "Slow settle"
                                     : Settle_Ms >= 200 ? "Moderate settle"
                                     : "Fast";

    public Color NPLC_Warning_Color => Settle_Ms >= 1000 ? Color.Orange
                                     : Settle_Ms >= 200 ? Color.Blue
                                     : Color.Black;


    public int Get_Readings_Per_Min( int Instrument_Count = 1 )
    => Settle_Ms > 0
        ? (int) (60000.0 / (Settle_Ms * Instrument_Count))
        : 0;




    // ── Incremental stats ────────────────────────────────────────────
    private int _Stat_N = 0;
    private double _Stat_Mean = 0;
    private double _Stat_M2 = 0;
    private double _Stat_Min = double.MaxValue;
    private double _Stat_Max = double.MinValue;
    private double _Stat_Sum_Sq = 0;

    // Call this every time you add a point - replaces all O(n) scans
    public void Add_Point_Value( double Value )
    {
      _Stat_N++;
      double Delta = Value - _Stat_Mean;
      _Stat_Mean += Delta / _Stat_N;
      _Stat_M2 += Delta * (Value - _Stat_Mean);
      if (Value < _Stat_Min)
        _Stat_Min = Value;
      if (Value > _Stat_Max)
        _Stat_Max = Value;
      _Stat_Sum_Sq += Value * Value;
    }
    public void Reset_Stats()
    {
      Points.Clear();
      Points.Capacity = 0;
      _Stat_N = 0;
      _Stat_Mean = 0;
      _Stat_M2 = 0;
      _Stat_Min = double.MaxValue;
      _Stat_Max = double.MinValue;
      _Stat_Sum_Sq = 0;
      Consecutive_Errors = 0;
      Total_Errors = 0;
      Disconnect_Count = 0;
      Comm_Error_Count = 0;
      File_Stats = null;
    }

    // ── O(1) stat accessors ──────────────────────────────────────────
    public double Get_Average() => _Stat_N > 0 ? _Stat_Mean : 0.0;
    public double Get_StdDev() => _Stat_N > 1 ? Math.Sqrt( _Stat_M2 / (_Stat_N - 1) ) : 0.0;
    public double Get_Min() => _Stat_N > 0 ? _Stat_Min : 0.0;
    public double Get_Max() => _Stat_N > 0 ? _Stat_Max : 0.0;
    public double Get_RMS() => _Stat_N > 0 ? Math.Sqrt( _Stat_Sum_Sq / _Stat_N ) : 0.0;
    public double Get_Range() => Get_Max() - Get_Min();
    public double Get_Peak_To_Peak() => Get_Range();
    public double Get_Last() => Points.Count > 0 ? Points[ Points.Count - 1 ].Value : 0.0;
    public int Get_Consecutive_Errors() => Consecutive_Errors;

    // ── These were already fine ──────────────────────────────────────
    public string Get_Trend()
    {
      if (Points == null || Points.Count < 10)
        return "—";
      double Recent = Points.Skip( Points.Count - 5 ).Average( P => P.Value );
      double Previous = Points.Skip( Points.Count - 10 ).Take( 5 ).Average( P => P.Value );
      double Change = (Recent - Previous) / Previous * 100;
      if (Math.Abs( Change ) < 0.1)
        return "→";
      return Change > 0 ? "↑" : "↓";
    }

    public double Get_Sample_Rate()
    {
      if (Points == null || Points.Count < 2)
        return 0.0;
      TimeSpan Elapsed = Points[ Points.Count - 1 ].Time - Points[ 0 ].Time;
      return (Points.Count - 1) / Elapsed.TotalSeconds;
    }

    // ── Kept for any callers that use it directly ────────────────────
    public double Get_Average_From( List<(DateTime Time, double Value)> Source )
    {
      if (Source == null || Source.Count == 0)
        return 0.0;
      return Source.Average( P => P.Value );
    }
  }
}

