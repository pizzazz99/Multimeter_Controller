// ============================================================================
// File:        Instrument_Class.cs
// Project:     Multimeter_Controller
// Description: Instrument and Instrument_Series data models supporting all
//              GPIB instrument types — DMMs, function generators, frequency
//              counters, and LCR meters.
//
// Changes from previous version:
//   - Compute_Display_Digits() extended for HP34411, HP53132, HP53181,
//     HP33120, HP33220, HP4263
//   - Comms_Overhead_Ms extended for all new types
//   - Poll_Delay_Ms now routes non-DMM types through Poll_Interval_Ms
//     instead of calculating from NPLC (which is meaningless for them)
//   - Poll_Interval_Ms property added — fixed poll period in ms for
//     non-DMM instruments; ignored for DMMs (they use NPLC-derived timing)
//   - Is_DMM convenience property added
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

namespace Multimeter_Controller
{
  public class Instrument
  {
    // ── Identity ──────────────────────────────────────────────────────────
    public string Name { get; set; } = "";
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
    public bool Is_Master { get; set; } = false;

    // ── Type ──────────────────────────────────────────────────────────────
    private Meter_Type _Type;
    public Meter_Type Type
    {
      get => _Type;
      set => _Type = value;
    }

    // True for instruments whose primary job is voltage / current / resistance
    // measurement.  False for function generators, counters, and LCR meters.
    public bool Is_DMM =>
        _Type == Meter_Type.HP3458 ||
        _Type == Meter_Type.HP34401 ||
        _Type == Meter_Type.HP34411 ||
        _Type == Meter_Type.HP34420 ||
        _Type == Meter_Type.HP3456 ||
        _Type == Meter_Type.Generic_GPIB;

    // ── NPLC (DMMs only) ──────────────────────────────────────────────────
    private decimal _NPLC = 1m;
    public decimal NPLC
    {
      get => _NPLC;
      set => _NPLC = value;
    }

    // ── Poll interval for non-DMM instruments (ms) ────────────────────────
    // For function generators, counters, and LCR meters the user sets a
    // fixed poll period directly instead of deriving it from NPLC.
    // Default 500 ms gives a comfortable 2 readings/sec without hammering
    // the GPIB bus.
    private int _Poll_Interval_Ms = 500;
    public int Poll_Interval_Ms
    {
      get => _Poll_Interval_Ms;
      set => _Poll_Interval_Ms = Math.Max( 50, value );   // floor at 50 ms
    }

    // ── Poll delay used by the poller loop ───────────────────────────────
    // DMMs:     derived from NPLC + comms overhead  (existing behaviour)
    // Non-DMMs: use the user-configured Poll_Interval_Ms
    public int Poll_Delay_Ms =>
        Is_DMM
            ? (int) ((double) _NPLC / 60.0 * 1000) + Comms_Overhead_Ms
            : _Poll_Interval_Ms + Comms_Overhead_Ms;

    // ── Display digits ───────────────────────────────────────────────────
    public int Display_Digits => Compute_Display_Digits( _Type, _NPLC );

    public static int Compute_Display_Digits( Meter_Type Type, decimal NPLC ) =>
        Type switch
        {
          // ── DMMs ────────────────────────────────────────────────────────
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

          // HP 34411A: digit count tracks NPLC like the 3458A but tops at 6.5
          Meter_Type.HP34411 =>
              NPLC >= 100 ? 6 :
              NPLC >= 10 ? 6 :
              NPLC >= 1 ? 6 :
              NPLC >= 0.06m ? 5 :
              NPLC >= 0.006m ? 4 : 3,

          // ── Frequency counters ──────────────────────────────────────────
          // Display "digits" here represents the number of significant digits
          // in a frequency reading at the default gate time.
          Meter_Type.HP53132 => 12,   // up to 12 digits with 10 s gate
          Meter_Type.HP53181 => 10,   // up to 10 digits with 1 s gate

          // ── Function generators ─────────────────────────────────────────
          // Represents the frequency resolution digits shown on the display.
          Meter_Type.HP33120 => 9,    // 15 MHz, 9-digit frequency resolution
          Meter_Type.HP33220 => 9,    // 20 MHz, 9-digit frequency resolution

          // ── LCR meter ───────────────────────────────────────────────────
          Meter_Type.HP4263 => 5,    // 5-digit measurement display

          Meter_Type.Generic_GPIB => 6,
          _ => 6,
        };

    // ── Comms overhead ───────────────────────────────────────────────────
    // Additional latency on top of the integration / poll time.
    // Values are conservative estimates for Prologix GPIB-USB at 9600 baud
    // or Prologix GPIB-Ethernet.  Adjust if your bus runs faster.
    public int Comms_Overhead_Ms => Type switch
    {
      // DMMs
      Meter_Type.HP3458 => 333,   // slow HP-IB framing
      Meter_Type.HP34401 => 20,
      Meter_Type.HP34411 => 15,    // faster DSP path than 34401
      Meter_Type.HP34420 => 25,
      Meter_Type.HP3456 => 50,    // older HP-IB instrument

      // Frequency counters — fast ASCII response once gate closes
      Meter_Type.HP53132 => 30,
      Meter_Type.HP53181 => 30,

      // Function generators — query response is a short ASCII string
      Meter_Type.HP33120 => 25,
      Meter_Type.HP33220 => 25,

      // LCR meter — measurement + ASCII response
      Meter_Type.HP4263 => 40,

      Meter_Type.Generic_GPIB => 50,
      _ => 20
    };

    // ── Display helpers ───────────────────────────────────────────────────
    public string Display => $"{Name}  (GPIB {Address})";

    public string DisplayString( string Transport_Mode )
    {
      bool Is_GPIB =
          Transport_Mode == "Prologix_GPIB" ||
          Transport_Mode == "Prologix_Ethernet";
      return Is_GPIB ? $"{Name}  (GPIB {Address})" : Name;
    }
  }

  // ==========================================================================
  // Instrument_Series — wraps Instrument and accumulates time-series data
  // ==========================================================================
  public class Instrument_Series
  {

    //
    //
    // DESCRIPTION:
    //   Corrected timing properties for Instrument_Series. Replace the existing
    //   Integration_Ms, Settle_Ms, Readings_Per_Min, NPLC_Warning_Text, and
    //   NPLC_Warning_Color property definitions in Instrument_Class.cs with
    //   these versions.
    //
    //   The fix ensures that non-DMM instruments (function generators, frequency
    //   counters, LCR meters) never use the NPLC-derived timing formula, which
    //   is meaningless for those types. Instead they use Poll_Interval_Ms, which
    //   is a user-configurable fixed poll period set directly on the Instrument.
    //
    // -----------------------------------------------------------------------------
    // PROPERTY BEHAVIOUR SUMMARY:
    //
    //   Property              DMM                        Non-DMM
    //   ─────────────────     ────────────────────────   ────────────────────────
    //   Integration_Ms        NPLC / 60 * 1000           Poll_Interval_Ms
    //   Settle_Ms             Integration_Ms * 2          Poll_Interval_Ms
    //   Readings_Per_Min      60000 / Settle_Ms           60000 / Poll_Interval_Ms
    //   Get_Readings_Per_Min  60000 / (Settle_Ms * N)     60000 / (Poll_Interval_Ms * N)
    //   NPLC_Warning_Text     Slow/Moderate/Fast          "Fixed interval"
    //   NPLC_Warning_Color    Orange/Blue/Black           Color.Gray
    //
    // -----------------------------------------------------------------------------
    // INTEGRATION NOTE:
    //   These properties replace the equivalently-named properties in the
    //   Instrument_Series class inside Instrument_Class.cs. No other changes
    //   are required — all other properties and methods remain as-is.
    //
    // =============================================================================







    public Instrument Instrument { get; set; } = new Instrument();

    // ── Forwarded properties ──────────────────────────────────────────────
    public decimal NPLC
    {
      get => Instrument.NPLC;
      set => Instrument.NPLC = value;
    }

    public int Poll_Interval_Ms
    {
      get => Instrument.Poll_Interval_Ms;
      set => Instrument.Poll_Interval_Ms = value;
    }

    public string Name => Instrument?.Name ?? "";
    public string Role => Instrument.Meter_Roll;
    public int Address => Instrument.Address;
    public Meter_Type Type => Instrument.Type;
    public bool Is_DMM => Instrument.Is_DMM;

    public bool Visible
    {
      get => Instrument.Visible;
      set => Instrument.Visible = value;
    }

    public int Display_Digits => Instrument.Display_Digits;
    public Color Line_Color
    {
      get; set;
    }

    // ── Error tracking ────────────────────────────────────────────────────
    public int Disconnect_Count { get; set; } = 0;
    public int Comm_Error_Count { get; set; } = 0;
    public int Consecutive_Errors { get; set; } = 0;
    public int Total_Errors { get; set; } = 0;

    // ── Recording state ───────────────────────────────────────────────────
    public bool Is_Recording { get; set; } = false;
    public Dictionary<string, string> File_Stats { get; set; } = null;
    public List<(DateTime Time, double Value)> Points { get; set; } = new();

    // ── NPLC-derived timing (DMMs) ────────────────────────────────────────
  
  

    // ── Incremental statistics (Welford online algorithm) ─────────────────
    private int _Stat_N = 0;
    private double _Stat_Mean = 0;
    private double _Stat_M2 = 0;
    private double _Stat_Min = double.MaxValue;
    private double _Stat_Max = double.MinValue;
    private double _Stat_Sum_Sq = 0;

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

    // ── O(1) stat accessors ───────────────────────────────────────────────
    public double Get_Average() => _Stat_N > 0 ? _Stat_Mean : 0.0;
    public double Get_StdDev() => _Stat_N > 1 ? Math.Sqrt( _Stat_M2 / (_Stat_N - 1) ) : 0.0;
    public double Get_Min() => _Stat_N > 0 ? _Stat_Min : 0.0;
    public double Get_Max() => _Stat_N > 0 ? _Stat_Max : 0.0;
    public double Get_RMS() => _Stat_N > 0 ? Math.Sqrt( _Stat_Sum_Sq / _Stat_N ) : 0.0;
    public double Get_Range() => Get_Max() - Get_Min();
    public double Get_Peak_To_Peak() => Get_Range();
    public double Get_Last() => Points.Count > 0
                                          ? Points[ Points.Count - 1 ].Value
                                          : 0.0;
    public int Get_Consecutive_Errors() => Consecutive_Errors;

    // ── Trend and sample rate ─────────────────────────────────────────────
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

      TimeSpan Elapsed =
          Points[ Points.Count - 1 ].Time - Points[ 0 ].Time;
      return (Points.Count - 1) / Elapsed.TotalSeconds;
    }

    // ── Legacy helper kept for callers that pass a source list directly ───
    public double Get_Average_From(
        List<(DateTime Time, double Value)> Source )
    {
      if (Source == null || Source.Count == 0)
        return 0.0;
      return Source.Average( P => P.Value );
    }


    
   

    // True integration time in milliseconds.
    // For DMMs:     derived from NPLC — one power line cycle at 60 Hz = 16.67 ms.
    //               Autozero doubles the effective settle time (handled in Settle_Ms).
    // For non-DMMs: the NPLC concept does not apply. Return Poll_Interval_Ms so
    //               that all downstream timing calculations remain meaningful.
    public double Integration_Ms =>
        Is_DMM
            ? (double) NPLC * (1000.0 / 60.0)
            : Poll_Interval_Ms;

    // Effective settle time — the minimum time to wait between readings.
    // For DMMs:     Integration_Ms * 2 accounts for the autozero cycle which
    //               doubles the actual measurement time at any given NPLC.
    // For non-DMMs: Poll_Interval_Ms is the full period between polls.
    //               There is no autozero equivalent, so no doubling is applied.
    public double Settle_Ms =>
        Is_DMM
            ? Integration_Ms * 2.0
            : Poll_Interval_Ms;

    // Estimated readings per minute at current settings with a single instrument.
    // Returns 0 if Settle_Ms is zero (should not occur in practice).
    public int Readings_Per_Min =>
        Settle_Ms > 0
            ? (int) (60000.0 / Settle_Ms)
            : 0;

    // Estimated readings per minute shared across N instruments on the same
    // GPIB bus. Each instrument adds its own Settle_Ms to the round-robin cycle.
    // For non-DMM instruments the same Poll_Interval_Ms is used per slot.
    public int Get_Readings_Per_Min( int Instrument_Count = 1 ) =>
        Settle_Ms > 0
            ? (int) (60000.0 / (Settle_Ms * Math.Max( 1, Instrument_Count )))
            : 0;

    // Human-readable speed indicator shown in the UI.
    // Non-DMM instruments always show "Fixed interval" since NPLC is not
    // applicable — the rate is controlled by Poll_Interval_Ms instead.
    public string NPLC_Warning_Text =>
        !Is_DMM ? "Fixed interval" :
        Settle_Ms >= 1000 ? "Slow settle" :
        Settle_Ms >= 200 ? "Moderate settle" :
                               "Fast";

    // Colour-coded speed indicator for UI binding.
    // Gray  — non-DMM, NPLC not applicable
    // Orange — DMM, very slow settle (>= 1 second)
    // Blue   — DMM, moderate settle (>= 200 ms)
    // Black  — DMM, fast settle (< 200 ms)
    public Color NPLC_Warning_Color =>
        !Is_DMM ? Color.Gray :
        Settle_Ms >= 1000 ? Color.Orange :
        Settle_Ms >= 200 ? Color.Blue :
                               Color.Black;





  }
}
