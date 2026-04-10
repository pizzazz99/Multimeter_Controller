// ============================================================================
// File:        Command_Dictionary_Class.cs
// Project:     Multimeter_Controller
// Description: Central enum definitions, command entry model, meter type
//              extensions, and command dictionary router for all supported
//              GPIB instruments.
//
// Instruments supported:
//   HP 3458A    — 8.5-digit DMM (legacy HP-IB command set)
//   HP 34401A   — 6.5-digit DMM (SCPI)
//   HP 34411A   — 6.5-digit DMM, high-speed (SCPI)
//   HP 34420A   — Nano-volt / micro-ohm meter (SCPI)
//   HP 3456A    — 6.5-digit DMM (legacy HP-IB command set)
//   HP 33120A   — 15 MHz function / arbitrary waveform generator (SCPI)
//   HP 33220A   — 20 MHz function / arbitrary waveform generator (SCPI)
//   HP 53132A   — 225 MHz universal counter (SCPI)
//   HP 53181A   — 225 MHz frequency counter (SCPI)
//   HP 4263B    — 100 Hz–100 kHz LCR meter (SCPI)
//   Generic GPIB — fallback, no dictionary
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

using System.Diagnostics;

namespace Multimeter_Controller
{
  // ==========================================================================
  // Meter_Type — one value per supported instrument
  // ==========================================================================
  public enum Meter_Type
  {
    HP3458,
    HP34401,
    HP34411,
    HP33120,
    HP33220,
    HP34420,
    HP53132,
    HP53181,
    HP4263,
    HP3456,
    Generic_GPIB
  }

  // ==========================================================================
  // Command_Category — functional groupings used for UI filtering
  //
  //   Modulation   — added for function generators (AM, FM, FSK, burst, sweep)
  //   Compensation — added for LCR meters (open/short/load correction routines)
  //   Subprogram   — retained for HP 3458A compatibility
  // ==========================================================================
  public enum Command_Category
  {
    Measurement,
    Configuration,
    Trigger,
    Math,
    Modulation,
    Compensation,
    System,
    Memory,
    IO,
    Calibration,
    Subprogram
  }

  // ==========================================================================
  // CommandMode — describes which forms of a command are available
  // ==========================================================================
  public enum CommandMode
  {
    None,
    Set_Only,
    Query_Only,
    Both
  }

  // ==========================================================================
  // Command_Entry — data record for a single instrument command
  // ==========================================================================
  public class Command_Entry
  {
    public string Command
    {
      get; set;
    }
    public string Syntax
    {
      get; set;
    }
    public string Description
    {
      get; set;
    }
    public Command_Category Category
    {
      get; set;
    }
    public string Parameters
    {
      get; set;
    }
    public string Query_Form
    {
      get; set;
    }
    public string Default_Value
    {
      get; set;
    }
    public string Example
    {
      get; set;
    }

    // ── Derived properties ────────────────────────────────────────────────

    public bool Can_Execute => !string.IsNullOrWhiteSpace( Command );
    public bool Can_Query => !string.IsNullOrWhiteSpace( Query_Form );
    public bool Has_Command => !string.IsNullOrWhiteSpace( Command );
    public bool Has_Query => !string.IsNullOrWhiteSpace( Query_Form );

    public bool Is_Query =>
        GetBaseToken( Command ).EndsWith( "?" );

    public string Query_Command
    {
      get
      {
        string Base_Token = GetBaseToken( Command );
        return Base_Token.EndsWith( "?" ) ? Base_Token : Base_Token + "?";
      }
    }

    public string Execute_Command
    {
      get
      {
        string Base_Token = GetBaseToken( Command );
        return Base_Token.EndsWith( "?" )
            ? Base_Token.TrimEnd( '?' )
            : Base_Token;
      }
    }

    // ── Constructor ───────────────────────────────────────────────────────

    public Command_Entry(
      string Command,
      string Syntax,
      string Description,
      Command_Category Category,
      string Parameters = "",
      string Query_Form = "",
      string Default_Value = "",
      string Example = "" )
    {
      this.Command = Command;
      this.Syntax = Syntax;
      this.Description = Description;
      this.Category = Category;
      this.Parameters = Parameters;

      // Query_Form resolution:
      //   null  → explicitly not queryable
      //   ""    → auto-generate by appending '?' to Command
      //   other → use value as-is
      if (Query_Form == null)
      {
        this.Query_Form = null;
      }
      else if (string.IsNullOrWhiteSpace( Query_Form ))
      {
        this.Query_Form = Command.EndsWith( "?" )
            ? Command
            : Command + "?";
      }
      else
      {
        this.Query_Form = Query_Form;
      }

      this.Default_Value = Default_Value;
      this.Example = Example;
    }

    public override string ToString() =>
        Command ?? Query_Form ?? "<unknown>";

    // ── Helper methods ────────────────────────────────────────────────────

    private string GetBaseToken( string Input )
    {
      if (string.IsNullOrWhiteSpace( Input ))
        return string.Empty;

      string Trimmed = Input.Trim();
      int Space_Index = Trimmed.IndexOf( ' ' );
      return Space_Index > 0
          ? Trimmed.Substring( 0, Space_Index )
          : Trimmed;
    }

    public bool Is_Query_Only()
    {
      if (string.IsNullOrWhiteSpace( Query_Form ))
        return false;

      if (Command.StartsWith( "*" ) && !Command.EndsWith( "?" ))
        return false;

      if (!string.IsNullOrWhiteSpace( Query_Form ))
      {
        if (Example.Trim() == Query_Form.Trim())
          return true;

        if (Syntax.Trim().EndsWith( "?" ))
          return true;
      }

      return false;
    }

    public bool Supports_Set()
    {
      if (!string.IsNullOrWhiteSpace( Example ) &&
           Example.Trim() != Query_Form?.Trim())
        return true;

      return false;
    }

    public CommandMode Get_Command_Mode()
    {
      if (Command.StartsWith( "*" ) && Command.EndsWith( "?" ))
        return CommandMode.Query_Only;

      if (Command.StartsWith( "*" ))
        return CommandMode.Set_Only;

      bool Has_Query = !string.IsNullOrWhiteSpace( Query_Form );
      bool Has_Set = true;

      if (Has_Query && Has_Set)
        return CommandMode.Both;
      if (Has_Query)
        return CommandMode.Query_Only;
      if (Has_Set)
        return CommandMode.Set_Only;

      return CommandMode.None;
    }
  }

  // ==========================================================================
  // Meter_Type_Extensions — display names, NPLC tables, combo ordering
  // ==========================================================================
  public static class Meter_Type_Extensions
  {
    // Order in which meter types appear in the UI combo box
    public static readonly Meter_Type[] Combo_Order = new Meter_Type[]
    {
      Meter_Type.Generic_GPIB,
      Meter_Type.HP34401,
      Meter_Type.HP34411,
      Meter_Type.HP33120,
      Meter_Type.HP33220,
      Meter_Type.HP34420,
      Meter_Type.HP53132,
      Meter_Type.HP53181,
      Meter_Type.HP4263,
      Meter_Type.HP3456,
      Meter_Type.HP3458,
    };

    public static string Get_Name( this Meter_Type type ) => type switch
    {
      Meter_Type.HP3458 => "HP 3458A",
      Meter_Type.HP34401 => "HP 34401A",
      Meter_Type.HP34411 => "HP 34411A",
      Meter_Type.HP33120 => "HP 33120A",
      Meter_Type.HP33220 => "HP 33220A",
      Meter_Type.HP34420 => "HP 34420A",
      Meter_Type.HP53132 => "HP 53132A",
      Meter_Type.HP53181 => "HP 53181A",
      Meter_Type.HP4263 => "HP 4263B",
      Meter_Type.HP3456 => "HP 3456A",
      Meter_Type.Generic_GPIB => "Generic GPIB",
      _ => throw new ArgumentOutOfRangeException( nameof( type ) )
    };

    // True for instruments that use proprietary HP-IB command sets
    // rather than full SCPI
    public static bool Is_Legacy_HP( this Meter_Type type ) =>
        type == Meter_Type.HP3458 || type == Meter_Type.HP3456;

    // True for instruments that are not DMMs (no NPLC concept)
    public static bool Is_Non_DMM( this Meter_Type type ) =>
        type == Meter_Type.HP33120 ||
        type == Meter_Type.HP33220 ||
        type == Meter_Type.HP53132 ||
        type == Meter_Type.HP53181 ||
        type == Meter_Type.HP4263;

    // NPLC value list for the UI spinner.
    // Non-DMM instruments return [ 1m ] as a placeholder —
    // the Poll_Interval_Ms property on Instrument is used instead.
    public static decimal[] Get_NPLC_Values( this Meter_Type type ) => type switch
    {
      Meter_Type.HP3458 => [ 0.001m, 0.01m, 0.1m, 1m, 10m, 100m ],
      Meter_Type.HP34401 => [ 0.02m, 0.2m, 1m, 10m, 100m ],
      Meter_Type.HP34411 => [ 0.001m, 0.006m, 0.02m, 0.06m, 0.2m, 1m, 2m, 10m, 100m ],
      Meter_Type.HP34420 => [ 0.02m, 0.2m, 1m, 2m, 10m, 20m, 100m, 200m ],
      Meter_Type.HP3456 => [ 1m, 2m, 4m, 8m, 16m, 32m, 64m, 100m ],
      Meter_Type.HP53132 => [ 1m ],   // counter  — NPLC not applicable
      Meter_Type.HP53181 => [ 1m ],   // counter  — NPLC not applicable
      Meter_Type.HP33120 => [ 1m ],   // function gen — NPLC not applicable
      Meter_Type.HP33220 => [ 1m ],   // function gen — NPLC not applicable
      Meter_Type.HP4263 => [ 1m ],   // LCR meter — NPLC not applicable
      Meter_Type.Generic_GPIB => [ 0.1m, 1m, 10m ],
      _ => throw new ArgumentOutOfRangeException( nameof( type ) )
    };

    public static decimal Get_Default_NPLC( this Meter_Type type ) => type switch
    {
      Meter_Type.HP3458 => 10m,
      Meter_Type.HP34401 => 10m,
      Meter_Type.HP34411 => 1m,
      Meter_Type.HP34420 => 10m,
      Meter_Type.HP3456 => 1m,
      Meter_Type.HP53132 => 1m,
      Meter_Type.HP53181 => 1m,
      Meter_Type.HP33120 => 1m,
      Meter_Type.HP33220 => 1m,
      Meter_Type.HP4263 => 1m,
      Meter_Type.Generic_GPIB => 1m,
      _ => 1m
    };

    public static int To_Combo_Index( this Meter_Type type ) =>
        Array.IndexOf( Combo_Order, type );

    public static Meter_Type From_Combo_Index( int index ) =>
        index >= 0 && index < Combo_Order.Length
            ? Combo_Order[ index ]
            : Meter_Type.Generic_GPIB;
  }

  // ==========================================================================
  // Command_Dictionary_Class — routes Get_All_Commands() to the correct
  // instrument dictionary based on Meter_Type
  // ==========================================================================
  public static class Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands(
        Meter_Type Meter = Meter_Type.HP3458 )
    {
      switch (Meter)
      {
        case Meter_Type.HP3458:
          return HP3458_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.HP34401:
          return HP34401_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.HP34411:
          return HP34411_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.HP33120:
          return HP33120_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.HP33220:
          return HP33220_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.HP34420:
          return HP34420_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.HP53132:
          return HP53132_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.HP53181:
          return HP53181_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.HP4263:
          return HP4263_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.HP3456:
          return HP3456_Command_Dictionary_Class.Get_All_Commands();

        case Meter_Type.Generic_GPIB:
          return [];

        default:
          Debug.WriteLine(
              $"[Command_Dictionary] Unsupported meter type: {Meter}" );
          return [];
      }
    }
  }
}
