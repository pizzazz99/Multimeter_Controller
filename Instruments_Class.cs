using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO.Ports;
using System.Threading.Channels;
using System.Windows.Forms;
using System.Xml.Linq;
using Trace_Execution_Namespace;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Trace_Execution_Namespace.Trace_Execution;




namespace Multimeter_Controller
{
  public class Instrument
  {
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

    private decimal _NPLC = 1m;
    public decimal NPLC
    {
      get => _NPLC;
      set => _NPLC = value;   // no more Compute_Display_Digits call
    }

    public int Display_Digits => Compute_Display_Digits ( _Type, _NPLC );

    private Meter_Type _Type;
    public Meter_Type Type
    {
      get => _Type;
      set => _Type = value;   // no more Compute_Display_Digits call
    }

    public static int Compute_Display_Digits ( Meter_Type Type, decimal NPLC ) =>
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

    public string Display => $"{Name}  (GPIB {Address})";

    public string DisplayString ( string Transport_Mode )
    {
      bool Is_GPIB = Transport_Mode == "Prologix_GPIB" || Transport_Mode == "Prologix_Ethernet";
      return Is_GPIB ? $"{Name}  (GPIB {Address})" : Name;
    }
  }


  // ===== Data Model =====

  public class Instrument_Series
  {
    public Instrument Instrument
    {
      get; set;
    }

    public decimal NPLC
    {
      get => Instrument.NPLC;
      set => Instrument.NPLC = value;
    }

    public int Disconnect_Count { get; set; } = 0;
    public int Comm_Error_Count { get; set; } = 0;
    public string Name => Instrument.Name;

    public string Role => Instrument.Meter_Roll;
    public int Address => Instrument.Address;
    public Meter_Type Type => Instrument.Type;
    public bool Visible
    {
      get => Instrument.Visible;
      set => Instrument.Visible = value;
    }
    public Dictionary<string, string> File_Stats { get; set; } = null;
    public List<(DateTime Time, double Value)> Points { get; set; } = new ( );
    public bool Is_Recording { get; set; } = false;
    public int Display_Digits => Instrument.Display_Digits;
    public int Consecutive_Errors { get; set; } = 0;
    public int Total_Errors { get; set; } = 0;
    public Color Line_Color
    {
      get; set;
    }


    // ── NPLC calculated properties ───────────────────────────────────
    public double Integration_Ms => (double) NPLC * ( 1000.0 / 60.0 );
    public double Settle_Ms => Integration_Ms * 2.0;   // autozero doubles it
    public int Readings_Per_Min => Settle_Ms > 0 ? (int) ( 60000.0 / Settle_Ms ) : 0;

    public string NPLC_Warning_Text => Settle_Ms >= 1000 ? "Slow settle"
                                     : Settle_Ms >= 200 ? "Moderate settle"
                                     : "Fast";

    public Color NPLC_Warning_Color => Settle_Ms >= 1000 ? Color.Orange
                                     : Settle_Ms >= 200 ? Color.Blue
                                     : Color.Black;


    public int Get_Readings_Per_Min ( int Instrument_Count = 1 )
    => Settle_Ms > 0
        ? (int) ( 60000.0 / ( Settle_Ms * Instrument_Count ) )
        : 0;




    // ── In Instrument_Series ─────────────────────────────────────────────



   

  


    // ── Incremental stats ────────────────────────────────────────────
    private int _Stat_N = 0;
    private double _Stat_Mean = 0;
    private double _Stat_M2 = 0;
    private double _Stat_Min = double.MaxValue;
    private double _Stat_Max = double.MinValue;
    private double _Stat_Sum_Sq = 0;

    // Call this every time you add a point - replaces all O(n) scans
    public void Add_Point_Value ( double Value )
    {
      _Stat_N++;
      double Delta = Value - _Stat_Mean;
      _Stat_Mean += Delta / _Stat_N;
      _Stat_M2 += Delta * ( Value - _Stat_Mean );
      if ( Value < _Stat_Min )
        _Stat_Min = Value;
      if ( Value > _Stat_Max )
        _Stat_Max = Value;
      _Stat_Sum_Sq += Value * Value;
    }
    public void Reset_Stats ( )
    {
      Points.Clear ( );
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
    public double Get_Average ( ) => _Stat_N > 0 ? _Stat_Mean : 0.0;
    public double Get_StdDev ( ) => _Stat_N > 1 ? Math.Sqrt ( _Stat_M2 / ( _Stat_N - 1 ) ) : 0.0;
    public double Get_Min ( ) => _Stat_N > 0 ? _Stat_Min : 0.0;
    public double Get_Max ( ) => _Stat_N > 0 ? _Stat_Max : 0.0;
    public double Get_RMS ( ) => _Stat_N > 0 ? Math.Sqrt ( _Stat_Sum_Sq / _Stat_N ) : 0.0;
    public double Get_Range ( ) => Get_Max ( ) - Get_Min ( );
    public double Get_Peak_To_Peak ( ) => Get_Range ( );
    public double Get_Last ( ) => Points.Count > 0 ? Points [ Points.Count - 1 ].Value : 0.0;
    public int Get_Consecutive_Errors ( ) => Consecutive_Errors;

    // ── These were already fine ──────────────────────────────────────
    public string Get_Trend ( )
    {
      if ( Points == null || Points.Count < 10 )
        return "—";
      double Recent = Points.Skip ( Points.Count - 5 ).Average ( P => P.Value );
      double Previous = Points.Skip ( Points.Count - 10 ).Take ( 5 ).Average ( P => P.Value );
      double Change = ( Recent - Previous ) / Previous * 100;
      if ( Math.Abs ( Change ) < 0.1 )
        return "→";
      return Change > 0 ? "↑" : "↓";
    }

    public double Get_Sample_Rate ( )
    {
      if ( Points == null || Points.Count < 2 )
        return 0.0;
      TimeSpan Elapsed = Points [ Points.Count - 1 ].Time - Points [ 0 ].Time;
      return ( Points.Count - 1 ) / Elapsed.TotalSeconds;
    }

    // ── Kept for any callers that use it directly ────────────────────
    public double Get_Average_From ( List<(DateTime Time, double Value)> Source )
    {
      if ( Source == null || Source.Count == 0 )
        return 0.0;
      return Source.Average ( P => P.Value );
    }
  }
}

