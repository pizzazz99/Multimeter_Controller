// ============================================================================
// File:        Command_Dictionary_Class.cs
// Project:     Keysight 3458A Multimeter Controller
// Description: Complete command reference dictionary for the Keysight (HP)
//              3458A 8.5-digit digital multimeter. Contains every supported
//              GPIB command with its syntax, parameters, query form, default
//              value, and usage example.
//
// Purpose:
//   This file provides a structured, in-memory command reference that the
//   application uses to populate both the main form's quick-reference list
//   and the full searchable dictionary window (Dictionary_Form). Each
//   command is represented as a Command_Entry object containing all of
//   the metadata a user needs to construct valid instrument commands.
//
// Architecture:
//   - Command_Category enum: Classifies commands into functional groups
//     for filtering and organization:
//       * Measurement  - DCV, ACV, ACDCV, DCI, ACI, ACDCI, OHM, OHMF,
//                        FREQ, PER, DSAC, DSDC, SSAC, SSDC
//       * Configuration - RANGE, ARANGE, NPLC, APER, NDIG, RES, AZERO,
//                         FIXEDZ, LFILTER, ACBAND, SETACV, LFREQ, OCOMP,
//                         DELAY
//       * Trigger       - TARM, TRIG, NRDGS, TIMER, SWEEP, LEVEL, SLOPE
//       * Math          - MATH, MMATH, NULL, SCALE, PERC, DB, DBM, FILTER,
//                         STAT, RMATH, PFAIL
//       * System        - RESET, PRESET, ID, ERR, ERRSTR, AUXERR, STB,
//                         SRQ, EMASK, END, LINE, TEMP, REV, OPT, TEST,
//                         SCRATCH
//       * Memory        - MEM, MSIZE, RMEM, MCOUNT, MFORMAT, SSTATE,
//                         RSTATE
//       * IO            - DISP, BEEP, OFORMAT, INBUF, TBUFF, GPIB,
//                         EXTOUT, LOCK
//       * Calibration   - ACAL, CAL, CALNUM, CALSTR, SECURE
//       * Subprogram    - SUB, SUBEND, CALL, CONT, PAUSE, DELSUB
//
//   - Command_Entry class: Data model for a single command with properties:
//       * Command       - The mnemonic (e.g., "DCV", "NPLC", "TARM")
//       * Syntax        - Full syntax string including optional parameters
//       * Description   - Human-readable description of what the command does
//       * Category      - Command_Category enum value for grouping/filtering
//       * Parameters    - Detailed parameter descriptions and allowed values
//       * Query_Form    - The query variant of the command (e.g., "DCV?")
//       * Default_Value - Factory default or power-on value
//       * Example       - A practical usage example
//
//   - Command_Dictionary_Class static class: Provides a single factory method
//     Get_All_Commands() that returns a List<Command_Entry> containing
//     all 70+ commands. Commands are organized by category within the
//     list for readability. The list is built fresh on each call so
//     callers receive an independent copy.
//
// Data Source:
//   Command definitions are based on the Keysight / HP 3458A User's Guide
//   and Programming Reference (HP part number 03458-90014). Parameters,
//   ranges, defaults, and syntax follow the instrument's GPIB command set.
//
// Usage:
//   List<Command_Entry> commands = Command_Dictionary_Class.Get_All_Commands();
//   // Iterate, filter by category, search by keyword, etc.
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

using System.Data.SqlTypes;

namespace Multimeter_Controller
{
  public enum Meter_Type
  {
    Keysight_3458A,
    HP_34401A,
    HP_33120A
  }

  public enum Command_Category
  {
    Measurement,
    Configuration,
    Trigger,
    Math,
    System,
    Memory,
    IO,
    Calibration,
    Subprogram
  }


  public enum CommandMode
  {
    None,       // Neither set nor query
    Set_Only,    // Only set string is defined
    Query_Only,  // Only query string is defined
    Both        // Both query and set strings defined
  }

  public class Command_Entry
  {


    public string Command
    {
      get; set;
    }



    public bool Can_Execute =>
        !string.IsNullOrWhiteSpace ( Command );

    public bool Can_Query =>
        !string.IsNullOrWhiteSpace ( Query_Form );


    // Derived properties

    public bool Is_Query =>
        GetBaseToken ( Command ).EndsWith ( "?" );


    public bool Has_Command =>
        !string.IsNullOrWhiteSpace ( Command );

    public bool Has_Query =>
        !string.IsNullOrWhiteSpace ( Query_Form );


    public string Query_Command
    {
      get
      {
        string baseToken = GetBaseToken ( Command );

        if ( baseToken.EndsWith ( "?" ) )
          return baseToken;

        return baseToken + "?";
      }
    }

    public string Execute_Command
    {
      get
      {
        string baseToken = GetBaseToken ( Command );

        if ( baseToken.EndsWith ( "?" ) )
          return baseToken.TrimEnd ( '?' );

        return baseToken;
      }
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




    public Command_Entry (
     string Command,
     string Syntax,
     string Description,
     Command_Category Category,
     string Parameters = "",
     string Query_Form = "",
     string Default_Value = "",
     string Example = ""
 )
    {
      this.Command = Command;
      this.Syntax = Syntax;
      this.Description = Description;
      this.Category = Category;
      this.Parameters = Parameters;

      // Only auto-generate Query_Form if:
      // 1. Query_Form is null or empty
      // 2. Command does NOT start with '*'
      // 3. Command does NOT already end with '?'
      if ( Query_Form == null )
      {
        // Explicitly NOT queryable
        this.Query_Form = null;
      }
      else if ( string.IsNullOrWhiteSpace ( Query_Form ) )
      {
        // Auto-generate query form
        this.Query_Form = Command.EndsWith ( "?" )
            ? Command
            : Command + "?";
      }
      else
      {
        // Explicit query form provided
        this.Query_Form = Query_Form;
      }

      this.Default_Value = Default_Value;
      this.Example = Example;
    }

    public override string ToString ( )
    {
      return Command ?? Query_Form ?? "<unknown>";
    }

    private string GetBaseToken ( string input )
    {
      if ( string.IsNullOrWhiteSpace ( input ) )
        return string.Empty;

      string trimmed = input.Trim ( );

      int spaceIndex = trimmed.IndexOf ( ' ' );
      return spaceIndex > 0
          ? trimmed.Substring ( 0, spaceIndex )
          : trimmed;
    }

    public bool Is_Query_Only ( )
    {
      // No query form defined → not query-only
      if ( string.IsNullOrWhiteSpace ( Query_Form ) )
        return false;

      // Commands starting with * but not ending with ? are not query-only
      if ( Command.StartsWith ( "*" ) && !Command.EndsWith ( "?" ) )
        return false;

      // If Syntax ends with '?' or Query_Form exists, and Example is a query → query-only
      if ( !string.IsNullOrWhiteSpace ( Query_Form ) )
      {
        // If Example is same as Query_Form, assume query-only
        if ( Example.Trim ( ) == Query_Form.Trim ( ) )
          return true;

        // If Syntax itself ends with ?, treat as query-only
        if ( Syntax.Trim ( ).EndsWith ( "?" ) )
          return true;
      }

      return false;
    }


    public bool Supports_Set ( )
    {
      // If command has parameters or Example is a set command → supports set
      if ( !string.IsNullOrWhiteSpace ( Example ) &&
          Example.Trim ( ) != Query_Form?.Trim ( ) )
        return true;

      return false;
    }




    public CommandMode Get_Command_Mode ( )
    {
      // Commands starting with * and ending with ? are query-only
      if ( Command.StartsWith ( "*" ) && Command.EndsWith ( "?" ) )
        return CommandMode.Query_Only;

      // Commands starting with * but without ? are set-only
      if ( Command.StartsWith ( "*" ) )
        return CommandMode.Set_Only;

      bool Has_Query = !string.IsNullOrWhiteSpace ( Query_Form );
      bool Has_Set = true; // assume normal commands can be set

      if ( Has_Query && Has_Set )
      {
        return CommandMode.Both;
      }

      if ( Has_Query )
      {
        return CommandMode.Query_Only;
      }

      if ( Has_Set )
      {
        return CommandMode.Set_Only;
      }

      return CommandMode.None;
    }








    public bool Supports_Query ( )
    {
      return !string.IsNullOrWhiteSpace ( Command );
    }








    private bool Has_Real_Parameters ( string Parameters )
    {
      return !string.IsNullOrWhiteSpace ( Parameters ) &&
             !Parameters.Equals ( "None", StringComparison.OrdinalIgnoreCase );
    }




    /*
    public Command_Entry (
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
      this.Query_Form = Query_Form;
      this.Default_Value = Default_Value;
      this.Example = Example;
    }
    */
  }

  public static class Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands (
      Meter_Type Meter = Meter_Type.Keysight_3458A )
    {
      switch ( Meter )
      {
        case Meter_Type.HP_34401A:
          return HP_34401A_Command_Dictionary_Class.Get_All_Commands ( );

        case Meter_Type.HP_33120A:
          return HP_33120A_Command_Dictionary_Class.Get_All_Commands ( );

        case Meter_Type.Keysight_3458A:
          return Keysight_3458A_Command_Dictionary_Class.Get_All_Commands ( );

        default:
          throw new ArgumentOutOfRangeException (
              nameof ( Meter ),
              Meter,
              "Unsupported meter type" );
      }
    }
  }
}
