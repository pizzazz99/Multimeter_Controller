
// =============================================================================
// FILE:     HP3458_Command_Dictionary_Class.cs
// PROJECT:  Multimeter_Controller
// =============================================================================
//
// DESCRIPTION:
//   Static command dictionary for the HP/Agilent 3458A 8.5-digit multimeter.
//   Provides a structured, searchable registry of all supported GPIB instrument
//   commands, organized by functional category. Each entry captures the full
//   command syntax, description, valid parameter ranges, query form, factory
//   default value, and a usage example.
//
//   This class is intended as the single source of truth for instrument command
//   metadata. It is designed to support command validation, UI population,
//   documentation generation, and runtime lookup by command name or query form.
//
// -----------------------------------------------------------------------------
// INSTRUMENT:
//   HP / Agilent 3458A Multimeter
//   Interface:    GPIB (IEEE-488.2)
//   Default Addr: 22
//   Firmware Ref: Compatible with all standard 3458A firmware revisions
//
// -----------------------------------------------------------------------------
// COMMAND CATEGORIES:
//
//   Measurement   — Function selectors (DCV, ACV, OHM, FREQ, etc.) and
//                   sampling modes (DSAC, DSDC, SSAC, SSDC). These commands
//                   configure the active measurement function and optionally
//                   set range and resolution in a single call.
//
//   Configuration — Settings that modify how a measurement is taken, including
//                   integration time (NPLC / APER), autorange (ARANGE), digit
//                   count (NDIG), autozero (AZERO), input impedance (FIXEDZ),
//                   AC bandwidth (ACBAND), and offset compensation (OCOMP).
//
//   Trigger       — Trigger arming (TARM), trigger source selection (TRIG),
//                   readings-per-trigger (NRDGS), sweep configuration (SWEEP),
//                   timer interval (TIMER), and level/slope triggering.
//
//   Math          — Post-measurement math operations including null offset,
//                   scaling, percentage, dB/dBm, digital filtering, statistics
//                   (STAT / RMATH), and pass/fail limit testing (PFAIL).
//
//   Memory        — Reading memory control (MEM, MSIZE, MFORMAT), memory
//                   recall (RMEM, MCOUNT), and instrument state save/recall
//                   (SSTATE / RSTATE, registers 0–9).
//
//   I/O           — Display control (DISP), output data format (OFORMAT),
//                   GPIB address (GPIB), input/output buffering (INBUF, TBUFF),
//                   external output signal (EXTOUT), and front panel lock (LOCK).
//
//   System        — Identification (ID?), error reporting (ERR?, ERRSTR?,
//                   AUXERR?), status registers (STB?, SRQ, EMASK), self-test
//                   (TEST), reset/preset, power line reference (LFREQ, LINE?),
//                   temperature query (TEMP?), and firmware revision (REV?).
//
//   Calibration   — Auto-calibration (ACAL), manual calibration steps (CAL),
//                   calibration count query (CALNUM?), calibration string label
//                   (CALSTR), and calibration security (SECURE).
//
//   Subprogram    — Stored subprogram creation (SUB / SUBEND), execution
//                   (CALL), deletion (DELSUB), and flow control (PAUSE / CONT).
//
// -----------------------------------------------------------------------------
// KEY METHODS:
//
//   Get_All_Commands()
//     Returns a List<Command_Entry> containing all registered commands,
//     sorted alphabetically by Command name (case-insensitive). This list
//     is rebuilt on every call — it is not cached.
//
//   Get_Command_By_Name(string Command_Name)
//     Performs a case-insensitive lookup against both the Command field and
//     the Query_Form field of every registered entry. Returns the matching
//     Command_Entry, or null if no match is found. Trims leading/trailing
//     whitespace from the input before comparison.
//
// -----------------------------------------------------------------------------
// DATA MODEL — Command_Entry fields:
//
//   Command       The primary mnemonic sent to the instrument (e.g. "DCV").
//   Syntax        Full syntax string including optional parameters.
//   Description   Human-readable description of the command's purpose.
//   Category      Command_Category enum value for grouping/filtering.
//   Parameters    Enumeration of valid parameter values and their meanings.
//   Query_Form    The query variant of the command (e.g. "DCV?"), if one
//                 exists. Empty string if the command has no query form.
//   Default_Value The factory default state returned or applied by the
//                 instrument for this setting.
//   Example       A representative usage string suitable for direct GPIB
//                 transmission.
//
// -----------------------------------------------------------------------------
// USAGE NOTES:
//
//   - All string comparisons in Get_Command_By_Name use
//     StringComparison.OrdinalIgnoreCase for culture-invariant matching.
//
//   - Commands with no query form (e.g. RESET, BEEP, SCRATCH) have an
//     empty string in Query_Form and will not match a "CMD?" lookup.
//
//   - RMATH? and MCOUNT? are registered as their query form in the Command
//     field because they are read-only queries with no write counterpart.
//
//   - Resistance ranges are expressed in scientific notation (e.g. "1E6")
//     to match the literal strings accepted and returned by the instrument.
//
//   - NPLC value 0 selects synchronous sub-sampling mode; it is not a
//     "zero integration time" setting.
//
//   - The DELAY command accepts -1 as a special value meaning auto-delay
//     (instrument-calculated based on function and range).
//
// -----------------------------------------------------------------------------
// DEPENDENCIES:
//   System.Collections.Generic    List<T>
//   System.Linq                   FirstOrDefault (LINQ extension)
//   System.StringComparison       OrdinalIgnoreCase
//   Command_Entry                 Data record defined elsewhere in namespace
//   Command_Category              Enum defined elsewhere in namespace
//
// -----------------------------------------------------------------------------
// MAINTENANCE:
//   To add a command: append a new Command_Entry to the list in
//   Get_All_Commands(). The list is re-sorted alphabetically on every call,
//   so insertion order does not matter.
//
//   To deprecate a command: add a note to the Description field rather than
//   removing the entry, to preserve backward compatibility with any consumers
//   relying on dictionary lookup.
//
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  public static class HP3458_Command_Dictionary_Class
  {

    public class Test_Profile : IInstrument_Test_Profile
    {
      public string              Reset_Command   => "RESET";
      public string              Error_Query     => "ERRSTR?";
      public bool                Has_Error_Queue => true;

      public List<Command_Entry> Get_Commands() => HP3458_Command_Dictionary_Class.Get_All_Commands();

      public IEnumerable<Command_Test_Result> Run_Sequences( Func<string, string> Query, Action<string> Send )
      {
        foreach ( var R in Test_DCV_Sequence( Query, Send ) )
          yield return R;
        foreach ( var R in Test_Stat_Sequence( Query, Send ) )
          yield return R;
      }

      // ── Sequenced Tests ───────────────────────────────────────────────────

      private static IEnumerable<Command_Test_Result> Test_DCV_Sequence( Func<string, string> Query,
                                                                         Action<string>       Send )
      {
        var                 Seq_Cmd = new Command_Entry( Command: "DCV [sequence]",
                                                         Syntax: "DCV → TRIG AUTO → NRDGS 1 → reading",
                                                         Description: "Sequenced DCV measurement test",
                                                         Category: Command_Category.Measurement,
                                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;

        try
        {
          Send( "DCV 10" );
          Send( "TRIG AUTO" );
          Send( "NRDGS 1,AUTO" );
          string Reading = Query( "." ); // HP3458A: "." triggers and reads
          bool   OK      = double.TryParse( Reading,
                                            System.Globalization.NumberStyles.Float,
                                            System.Globalization.CultureInfo.InvariantCulture,
                                            out _ );
          Result         = OK ? Command_Test_Result.Pass( Seq_Cmd, Reading )
                              : Command_Test_Result.Fail( Seq_Cmd, $"Non-numeric response: {Reading}" );
        }
        catch ( Exception Ex )
        {
          Result = Command_Test_Result.Fail( Seq_Cmd, Ex.Message );
        }

        yield return Result;
      }

      private static IEnumerable<Command_Test_Result> Test_Stat_Sequence( Func<string, string> Query,
                                                                          Action<string>       Send )
      {
        var Seq_Cmd = new Command_Entry( Command: "STAT [sequence]",
                                         Syntax: "DCV → MATH STAT → readings × 5 → RMATH?",
                                         Description: "Sequenced statistics accumulation test",
                                         Category: Command_Category.Math,
                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;

        try
        {
          Send( "DCV 10" );
          Send( "TRIG AUTO" );
          Send( "NRDGS 1,AUTO" );
          Send( "MATH STAT" );

          // Accumulate 5 readings
          for ( int I = 0; I < 5; I++ )
            try
            {
              Query( "." );
            }
            catch
            {
            }

          string Stats = Query( "RMATH?" );
          bool   OK    = ! string.IsNullOrWhiteSpace( Stats );

          Result = OK ? Command_Test_Result.Pass( Seq_Cmd, Stats.Trim() )
                      : Command_Test_Result.Fail( Seq_Cmd, "Empty response from RMATH?" );
        }
        catch ( Exception Ex )
        {
          Result = Command_Test_Result.Fail( Seq_Cmd, Ex.Message );
        }
        finally
        {
          try
          {
            Send( "MATH OFF" );
          }
          catch
          {
          }
        }

        yield return Result;
      }
    }

    public static Command_Entry? Get_Command_By_Name( string Command_Name )
    {
      if ( string.IsNullOrWhiteSpace( Command_Name ) )
        return null;

      Command_Name = Command_Name.Trim();

      return Get_All_Commands().FirstOrDefault(
        c => string.Equals( c.Command, Command_Name, StringComparison.OrdinalIgnoreCase ) ||
             string.Equals( c.Query_Form, Command_Name, StringComparison.OrdinalIgnoreCase ) );
    }

    public static List<Command_Entry> Get_All_Commands()
    {
      var Commands = new List<Command_Entry> {
        // ===== Measurement Commands =====
        new Command_Entry( Command: "DCV",
                           Syntax: "DCV [<range>[,<resolution>]]",
                           Description: "Configure DC voltage measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.1|1|10|100|1000|AUTO, resolution: 4-8.5 digits",
                           Query_Form: "DCV?",
                           Default_Value: "AUTO range, max resolution",
                           Example: "DCV 10,0.0001",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "ACV",
                           Syntax: "ACV [<range>[,<resolution>]]",
                           Description: "Configure AC voltage measurement (RMS)",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.1|1|10|100|1000|AUTO, resolution: 4-6.5 digits",
                           Query_Form: "ACV?",
                           Default_Value: "AUTO range, max resolution",
                           Example: "ACV 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "ACDCV",
                           Syntax: "ACDCV [<range>[,<resolution>]]",
                           Description: "Configure AC+DC voltage measurement (true RMS)",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.1|1|10|100|1000|AUTO, resolution: 4-6.5 digits",
                           Query_Form: "ACDCV?",
                           Default_Value: "AUTO range, max resolution",
                           Example: "ACDCV 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DCI",
                           Syntax: "DCI [<range>[,<resolution>]]",
                           Description: "Configure DC current measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.0001|0.001|0.01|0.1|1|AUTO, resolution: 4-8.5 digits",
                           Query_Form: "DCI?",
                           Default_Value: "AUTO range, max resolution",
                           Example: "DCI 0.1,0.00001",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "ACI",
                           Syntax: "ACI [<range>[,<resolution>]]",
                           Description: "Configure AC current measurement (RMS)",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.0001|0.001|0.01|0.1|1|AUTO, resolution: 4-6.5 digits",
                           Query_Form: "ACI?",
                           Default_Value: "AUTO range, max resolution",
                           Example: "ACI 1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "ACDCI",
                           Syntax: "ACDCI [<range>[,<resolution>]]",
                           Description: "Configure AC+DC current measurement (true RMS)",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.0001|0.001|0.01|0.1|1|AUTO, resolution: 4-6.5 digits",
                           Query_Form: "ACDCI?",
                           Default_Value: "AUTO range, max resolution",
                           Example: "ACDCI 0.1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "OHM",
                           Syntax: "OHM [<range>[,<resolution>]]",
                           Description: "Configure 2-wire resistance measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 10|100|1E3|1E4|1E5|1E6|1E7|1E8|1E9|AUTO",
                           Query_Form: "OHM?",
                           Default_Value: "AUTO range, max resolution",
                           Example: "OHM 1E6",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "OHMF",
                           Syntax: "OHMF [<range>[,<resolution>]]",
                           Description: "Configure 4-wire resistance measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 10|100|1E3|1E4|1E5|1E6|1E7|1E8|1E9|AUTO",
                           Query_Form: "OHMF?",
                           Default_Value: "AUTO range, max resolution",
                           Example: "OHMF 1E3,0.001",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "FREQ",
                           Syntax: "FREQ [<range>[,<resolution>]]",
                           Description: "Configure frequency measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: voltage range for gating, resolution: Hz",
                           Query_Form: "FREQ?",
                           Default_Value: "AUTO range",
                           Example: "FREQ 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "PER",
                           Syntax: "PER [<range>[,<resolution>]]",
                           Description: "Configure period measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: voltage range for gating, resolution: seconds",
                           Query_Form: "PER?",
                           Default_Value: "AUTO range",
                           Example: "PER 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DSAC",
                           Syntax: "DSAC [<range>]",
                           Description: "Configure direct sampling AC voltage measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.1|1|10|100|1000|AUTO",
                           Query_Form: "DSAC?",
                           Default_Value: "AUTO range",
                           Example: "DSAC 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DSDC",
                           Syntax: "DSDC [<range>]",
                           Description: "Configure direct sampling DC voltage measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.1|1|10|100|1000|AUTO",
                           Query_Form: "DSDC?",
                           Default_Value: "AUTO range",
                           Example: "DSDC 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SSAC",
                           Syntax: "SSAC [<range>]",
                           Description: "Configure sub-sampling AC voltage measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.1|1|10|100|1000|AUTO",
                           Query_Form: "SSAC?",
                           Default_Value: "AUTO range",
                           Example: "SSAC 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SSDC",
                           Syntax: "SSDC [<range>]",
                           Description: "Configure sub-sampling DC voltage measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.1|1|10|100|1000|AUTO",
                           Query_Form: "SSDC?",
                           Default_Value: "AUTO range",
                           Example: "SSDC 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== Configuration Commands =====
        new Command_Entry( Command: "RANGE?",
                           Syntax: "RANGE [<range>]",
                           Description: "Set or query the measurement range",
                           Category: Command_Category.Configuration,
                           Parameters: "range: numeric value or AUTO, MIN, MAX, DEF",
                           Query_Form: "RANGE?",
                           Default_Value: "AUTO",
                           Example: "RANGE 10",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "ARANGE",
                           Syntax: "ARANGE <ON|OFF>",
                           Description: "Enable or disable autorange",
                           Category: Command_Category.Configuration,
                           Parameters: "ON|OFF",
                           Query_Form: "ARANGE?",
                           Default_Value: "ON",
                           Example: "ARANGE OFF",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "NPLC",
                           Syntax: "NPLC <PLCs>",
                           Description: "Set integration time in power line cycles",
                           Category: Command_Category.Configuration,
                           Parameters: "PLCs: 0|1|2|10|50|100 (0 = sync sub-sampling)",
                           Query_Form: "NPLC?",
                           Default_Value: "10",
                           Example: "NPLC 100",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "APER",
                           Syntax: "APER <seconds>",
                           Description: "Set integration aperture time in seconds",
                           Category: Command_Category.Configuration,
                           Parameters: "seconds: 500E-9 to 1.0",
                           Query_Form: "APER?",
                           Default_Value: "Determined by NPLC",
                           Example: "APER 0.1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "NDIG",
                           Syntax: "NDIG <digits>",
                           Description: "Set the number of display digits",
                           Category: Command_Category.Configuration,
                           Parameters: "digits: 3 to 8",
                           Query_Form: "NDIG?",
                           Default_Value: "8",
                           Example: "NDIG 6",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "RES",
                           Syntax: "RES <resolution>",
                           Description: "Set measurement resolution",
                           Category: Command_Category.Configuration,
                           Parameters: "resolution: numeric value in measurement units",
                           Query_Form: "RES?",
                           Default_Value: "Function-dependent",
                           Example: "RES 0.0001",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "AZERO",
                           Syntax: "AZERO <ON|OFF|ONCE>",
                           Description: "Control auto-zero function",
                           Category: Command_Category.Configuration,
                           Parameters: ( "ON: auto-zero every reading, OFF: disable, ONCE: single " + "auto" +
                                                                                                      "-zer" +
                                                                                                      "o" ),
                           Query_Form: "AZERO?",
                           Default_Value: "ON",
                           Example: "AZERO OFF",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "FIXEDZ",
                           Syntax: "FIXEDZ <ON|OFF>",
                           Description: "Enable/disable fixed input impedance (10 MOhm) for DCV",
                           Category: Command_Category.Configuration,
                           Parameters: "ON: fixed 10 MOhm, OFF: >10 GOhm on 100mV-10V ranges",
                           Query_Form: "FIXEDZ?",
                           Default_Value: "OFF",
                           Example: "FIXEDZ ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "LFILTER",
                           Syntax: "LFILTER <ON|OFF>",
                           Description: "Enable/disable analog low-pass filter",
                           Category: Command_Category.Configuration,
                           Parameters: "ON|OFF",
                           Query_Form: "LFILTER?",
                           Default_Value: "OFF",
                           Example: "LFILTER ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "ACBAND",
                           Syntax: "ACBAND <low_freq>[,<high_freq>]",
                           Description: "Set AC measurement bandwidth limits",
                           Category: Command_Category.Configuration,
                           Parameters: "low_freq: 1-500000 Hz, high_freq: 1-500000 Hz",
                           Query_Form: "ACBAND?",
                           Default_Value: "2 Hz, 2 MHz",
                           Example: "ACBAND 20,100000",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SETACV",
                           Syntax: "SETACV <SYNC|RNDM>",
                           Description: "Set AC voltage measurement coupling method",
                           Category: Command_Category.Configuration,
                           Parameters: "SYNC: synchronous, RNDM: random sampling",
                           Query_Form: "SETACV?",
                           Default_Value: "SYNC",
                           Example: "SETACV RNDM",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "LFREQ",
                           Syntax: "LFREQ <50|60>",
                           Description: "Set power line frequency reference",
                           Category: Command_Category.Configuration,
                           Parameters: "50: 50 Hz line, 60: 60 Hz line",
                           Query_Form: "LFREQ?",
                           Default_Value: "Auto-detected",
                           Example: "LFREQ 60",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "OCOMP",
                           Syntax: "OCOMP <ON|OFF>",
                           Description: "Enable/disable offset-compensated ohms",
                           Category: Command_Category.Configuration,
                           Parameters: "ON|OFF (only for OHM/OHMF)",
                           Query_Form: "OCOMP?",
                           Default_Value: "OFF",
                           Example: "OCOMP ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DELAY",
                           Syntax: "DELAY <seconds>",
                           Description: "Set trigger delay time",
                           Category: Command_Category.Configuration,
                           Parameters: "seconds: -1 (auto) or 0 to 3600",
                           Query_Form: "DELAY?",
                           Default_Value: "-1 (auto)",
                           Example: "DELAY 0.5",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== Trigger Commands =====
        new Command_Entry( Command: "TARM",
                           Syntax: "TARM <event>[,<count>]",
                           Description: "Set trigger arm event and optional count",
                           Category: Command_Category.Trigger,
                           Parameters: "event: AUTO|EXT|SGL|HOLD|SYN",
                           Query_Form: "TARM?",
                           Default_Value: "AUTO",
                           Example: "TARM SGL",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "TRIG",
                           Syntax: "TRIG <event>[,<count>]",
                           Description: "Set trigger event source",
                           Category: Command_Category.Trigger,
                           Parameters: "event: AUTO|EXT|SGL|HOLD|SYN|LEVEL|LINE",
                           Query_Form: "TRIG?",
                           Default_Value: "AUTO",
                           Example: "TRIG EXT",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "NRDGS",
                           Syntax: "NRDGS <count>[,<event>]",
                           Description: "Set number of readings per trigger",
                           Category: Command_Category.Trigger,
                           Parameters: ( "count: 1 to 16777215, event: " + "AUTO|EXT|SGL|HOLD|SYN|TIMER|" +
                                                                           "LINE|LEVEL" ),
                           Query_Form: "NRDGS?",
                           Default_Value: "1,AUTO",
                           Example: "NRDGS 100,TIMER",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "TIMER",
                           Syntax: "TIMER <seconds>",
                           Description: "Set timer interval for timed triggers",
                           Category: Command_Category.Trigger,
                           Parameters: "seconds: 0.0001 to 3600",
                           Query_Form: "TIMER?",
                           Default_Value: "1",
                           Example: "TIMER 0.01",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SWEEP",
                           Syntax: "SWEEP <interval>[,<count>]",
                           Description: "Configure a sweep of measurements at fixed intervals",
                           Category: Command_Category.Trigger,
                           Parameters: "interval: seconds between readings, count: number of readings",
                           Query_Form: "SWEEP?",
                           Default_Value: "N/A",
                           Example: "SWEEP 0.001,1000",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "LEVEL",
                           Syntax: "LEVEL <level>[,<edge>]",
                           Description: "Set trigger level and edge for level triggering",
                           Category: Command_Category.Trigger,
                           Parameters: "level: trigger voltage, edge: AC|DC",
                           Query_Form: "LEVEL?",
                           Default_Value: "0,DC",
                           Example: "LEVEL 1.5,DC",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SLOPE",
                           Syntax: "SLOPE <POS|NEG>",
                           Description: "Set external trigger slope",
                           Category: Command_Category.Trigger,
                           Parameters: "POS: positive edge, NEG: negative edge",
                           Query_Form: "SLOPE?",
                           Default_Value: "POS",
                           Example: "SLOPE NEG",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== Math Commands =====
        new Command_Entry( Command: "MATH",
                           Syntax: "MATH <function>",
                           Description: "Select math operation applied to readings",
                           Category: Command_Category.Math,
                           Parameters: "function: OFF|CONT|DB|DBM|FILTER|NULL|PERC|PFAIL|RMS|SCALE|STAT",
                           Query_Form: "MATH?",
                           Default_Value: "OFF",
                           Example: "MATH NULL",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "MMATH",
                           Syntax: "MMATH <function>[,<function2>...]",
                           Description: "Enable multiple simultaneous math operations",
                           Category: Command_Category.Math,
                           Parameters: "function: combination of MATH functions",
                           Query_Form: "MMATH?",
                           Default_Value: "OFF",
                           Example: "MMATH NULL,FILTER",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "NULL",
                           Syntax: "NULL [<value>]",
                           Description: "Set null offset value for null math function",
                           Category: Command_Category.Math,
                           Parameters: "value: offset in measurement units (omit to use current reading)",
                           Query_Form: "NULL?",
                           Default_Value: "0",
                           Example: "NULL 0.00015",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SCALE",
                           Syntax: "SCALE <gain>[,<offset>]",
                           Description: "Set scale/offset: result = (reading * gain) + offset",
                           Category: Command_Category.Math,
                           Parameters: "gain: multiplier, offset: additive offset",
                           Query_Form: "SCALE?",
                           Default_Value: "1,0",
                           Example: "SCALE 1000,0",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "PERC",
                           Syntax: "PERC <target>",
                           Description: "Set percent target for percent math function",
                           Category: Command_Category.Math,
                           Parameters: "target: 100% reference value",
                           Query_Form: "PERC?",
                           Default_Value: "1",
                           Example: "PERC 5.0",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DB",
                           Syntax: "DB <reference>",
                           Description: "Set dB reference for dB math function",
                           Category: Command_Category.Math,
                           Parameters: "reference: reference voltage in volts",
                           Query_Form: "DB?",
                           Default_Value: "1",
                           Example: "DB 1.0",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DBM",
                           Syntax: "DBM <impedance>",
                           Description: "Set reference impedance for dBm math function",
                           Category: Command_Category.Math,
                           Parameters: "impedance: reference impedance in ohms (50-8000)",
                           Query_Form: "DBM?",
                           Default_Value: "600",
                           Example: "DBM 50",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "FILTER",
                           Syntax: "FILTER <count>",
                           Description: "Set number of readings for averaging digital filter",
                           Category: Command_Category.Math,
                           Parameters: "count: 1 to 100",
                           Query_Form: "FILTER?",
                           Default_Value: "10",
                           Example: "FILTER 20",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "STAT",
                           Syntax: "STAT <ON|OFF>",
                           Description: "Enable/disable statistics accumulation",
                           Category: Command_Category.Math,
                           Parameters: "ON|OFF - reports mean, std dev, min, max, count",
                           Query_Form: "STAT?",
                           Default_Value: "OFF",
                           Example: "STAT ON",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "RMATH?",
                           Syntax: "RMATH",
                           Description: "Query math register (accumulated stats result)",
                           Category: Command_Category.Math,
                           Parameters: "Returns statistic values",
                           Query_Form: "RMATH?",
                           Default_Value: "N/A",
                           Example: "RMATH",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "PFAIL",
                           Syntax: "PFAIL <low>,<high>",
                           Description: "Set pass/fail limits for pass/fail math function",
                           Category: Command_Category.Math,
                           Parameters: "low: lower limit, high: upper limit",
                           Query_Form: "PFAIL?",
                           Default_Value: "N/A",
                           Example: "PFAIL 4.9,5.1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== System Commands =====
        new Command_Entry( Command: "RESET",
                           Syntax: "RESET",
                           Description: "Reset instrument to factory default state",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "RESET",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "PRESET",
                           Syntax: "PRESET",
                           Description: "Preset instrument to fast DCV configuration",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "PRESET?",
                           Default_Value: "N/A",
                           Example: "PRESET",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "ID?",
                           Syntax: "ID?",
                           Description: "Query instrument identification string",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "ID?",
                           Default_Value: "N/A",
                           Example: "ID?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "ERR?",
                           Syntax: "ERR?",
                           Description: "Query and clear error register",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "ERR?",
                           Default_Value: "N/A",
                           Example: "ERR?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "ERRSTR?",
                           Syntax: "ERRSTR?",
                           Description: "Query error string description for last error",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "ERRSTR?",
                           Default_Value: "N/A",
                           Example: "ERRSTR?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "AUXERR?",
                           Syntax: "AUXERR?",
                           Description: "Query auxiliary error register for additional detail",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "AUXERR?",
                           Default_Value: "N/A",
                           Example: "AUXERR?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "STB?",
                           Syntax: "STB?",
                           Description: "Query status byte register",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "STB?",
                           Default_Value: "N/A",
                           Example: "STB?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "SRQ",
                           Syntax: "SRQ <mask>",
                           Description: "Set service request enable mask",
                           Category: Command_Category.System,
                           Parameters: "mask: bit mask for SRQ conditions",
                           Query_Form: "SRQ?",
                           Default_Value: "0",
                           Example: "SRQ 16",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "EMASK",
                           Syntax: "EMASK <mask>",
                           Description: "Set event status mask for SRQ generation",
                           Category: Command_Category.System,
                           Parameters: "mask: bit mask for event conditions",
                           Query_Form: "EMASK?",
                           Default_Value: "0",
                           Example: "EMASK 1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "END",
                           Syntax: "END <mode>",
                           Description: "Set GPIB end-or-identify (EOI) mode",
                           Category: Command_Category.System,
                           Parameters: "mode: OFF|ON|ALWAYS",
                           Query_Form: "END?",
                           Default_Value: "ALWAYS",
                           Example: "END ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "LINE?",
                           Syntax: "LINE?",
                           Description: "Query measured power line frequency",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "LINE?",
                           Default_Value: "N/A",
                           Example: "LINE?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "TEMP?",
                           Syntax: "TEMP?",
                           Description: "Query internal instrument temperature",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "TEMP?",
                           Default_Value: "N/A",
                           Example: "TEMP?,Test_Behavior: Test_Behavior.Query_Safe" ),

        new Command_Entry( Command: "REV?",
                           Syntax: "REV?",
                           Description: "Query firmware revision string",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "REV?",
                           Default_Value: "N/A",
                           Example: "REV?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "OPT?",
                           Syntax: "OPT?",
                           Description: "Query installed options",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "OPT?",
                           Default_Value: "N/A",
                           Example: "OPT?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "TEST",
                           Syntax: "TEST",
                           Description: "Execute self-test and report results",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "TEST?",
                           Default_Value: "N/A",
                           Example: "TEST",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "SCRATCH",
                           Syntax: "SCRATCH",
                           Description: "Clear subprogram memory and reading memory",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "SCRATCH",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        // ===== Memory Commands =====
        new Command_Entry( Command: "MEM",
                           Syntax: "MEM <mode>",
                           Description: "Enable/disable reading memory storage",
                           Category: Command_Category.Memory,
                           Parameters: "mode: OFF|FIFO|LIFO|CONT",
                           Query_Form: "MEM?",
                           Default_Value: "OFF",
                           Example: "MEM FIFO",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "MSIZE",
                           Syntax: "MSIZE <bytes>",
                           Description: "Set reading memory size in bytes",
                           Category: Command_Category.Memory,
                           Parameters: "bytes: 0 to available memory",
                           Query_Form: "MSIZE?",
                           Default_Value: "0",
                           Example: "MSIZE 50000",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "RMEM",
                           Syntax: "RMEM <start>,<count>[,<format>]",
                           Description: "Recall readings from memory",
                           Category: Command_Category.Memory,
                           Parameters: "start: first reading (1-based), count: number of readings",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "RMEM 1,100",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "MCOUNT?",
                           Syntax: "MCOUNT?",
                           Description: "Query number of readings stored in memory",
                           Category: Command_Category.Memory,
                           Parameters: "None",
                           Query_Form: "MCOUNT?",
                           Default_Value: "N/A",
                           Example: "MCOUNT?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MFORMAT",
                           Syntax: "MFORMAT <format>",
                           Description: "Set memory storage format for readings",
                           Category: Command_Category.Memory,
                           Parameters: "format: SREAL|DREAL|ASCII",
                           Query_Form: "MFORMAT?",
                           Default_Value: "DREAL",
                           Example: "MFORMAT SREAL",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SSTATE",
                           Syntax: "SSTATE <register>",
                           Description: "Save current instrument state to register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 0 to 9",
                           Query_Form: "SSTATE?",
                           Default_Value: "N/A",
                           Example: "SSTATE 1",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "RSTATE",
                           Syntax: "RSTATE <register>",
                           Description: "Recall saved instrument state from register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 0 to 9",
                           Query_Form: "RSTATE?",
                           Default_Value: "N/A",
                           Example: "RSTATE 1",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        // ===== I/O Commands =====
        new Command_Entry( Command: "DISP",
                           Syntax: "DISP <mode>[,<message>]",
                           Description: "Control front panel display",
                           Category: Command_Category.IO,
                           Parameters: "mode: OFF|ON|MSG (MSG displays custom text)",
                           Query_Form: "DISP?",
                           Default_Value: "ON",
                           Example: "DISP MSG,\"TESTING\"",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "BEEP",
                           Syntax: "BEEP",
                           Description: "Emit a single beep from the instrument",
                           Category: Command_Category.IO,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "BEEP",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "OFORMAT",
                           Syntax: "OFORMAT <format>",
                           Description: "Set output data format for readings",
                           Category: Command_Category.IO,
                           Parameters: "format: ASCII|SINT|DINT|SREAL|DREAL",
                           Query_Form: "OFORMAT?",
                           Default_Value: "ASCII",
                           Example: "OFORMAT DREAL",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INBUF",
                           Syntax: "INBUF <ON|OFF>",
                           Description: "Enable/disable input buffer for command queuing",
                           Category: Command_Category.IO,
                           Parameters: "ON|OFF",
                           Query_Form: "INBUF?",
                           Default_Value: "ON",
                           Example: "INBUF ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "TBUFF",
                           Syntax: "TBUFF <ON|OFF>",
                           Description: "Enable/disable output transfer buffer",
                           Category: Command_Category.IO,
                           Parameters: "ON|OFF",
                           Query_Form: "TBUFF?",
                           Default_Value: "OFF",
                           Example: "TBUFF ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "GPIB",
                           Syntax: "GPIB <address>",
                           Description: "Set GPIB address (stored in non-volatile memory)",
                           Category: Command_Category.IO,
                           Parameters: "address: 0 to 30",
                           Query_Form: "GPIB?",
                           Default_Value: "22",
                           Example: "GPIB 22",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "EXTOUT",
                           Syntax: "EXTOUT <signal>",
                           Description: "Set external output (rear BNC) signal source",
                           Category: Command_Category.IO,
                           Parameters: "signal: ICOMP|EGUARD|DGRADE|PFAIL|RCOMP|OFF",
                           Query_Form: "EXTOUT?",
                           Default_Value: "OFF",
                           Example: "EXTOUT ICOMP",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "LOCK",
                           Syntax: "LOCK <ON|OFF>",
                           Description: "Lock/unlock front panel controls",
                           Category: Command_Category.IO,
                           Parameters: "ON: lock panel, OFF: unlock panel",
                           Query_Form: "LOCK?",
                           Default_Value: "OFF",
                           Example: "LOCK ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== Calibration Commands =====
        new Command_Entry( Command: "ACAL",
                           Syntax: "ACAL <type>",
                           Description: "Perform auto-calibration",
                           Category: Command_Category.Calibration,
                           Parameters: "type: DCV|AC|OHMS|ALL",
                           Query_Form: "ACAL?",
                           Default_Value: "N/A",
                           Example: "ACAL ALL",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "CAL",
                           Syntax: "CAL <step>",
                           Description: "Perform manual calibration step",
                           Category: Command_Category.Calibration,
                           Parameters: "step: calibration step number",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CAL 1",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "CALNUM?",
                           Syntax: "CALNUM?",
                           Description: "Query calibration count (number of calibrations performed)",
                           Category: Command_Category.Calibration,
                           Parameters: "None",
                           Query_Form: "CALNUM?",
                           Default_Value: "N/A",
                           Example: "CALNUM?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "CALSTR",
                           Syntax: "CALSTR <string>",
                           Description: "Set calibration string (user-defined label)",
                           Category: Command_Category.Calibration,
                           Parameters: "string: up to 40 characters",
                           Query_Form: "CALSTR?",
                           Default_Value: "N/A",
                           Example: "CALSTR \"CAL 2024-01-15\"",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "SECURE",
                           Syntax: "SECURE <code>",
                           Description: "Set calibration security code",
                           Category: Command_Category.Calibration,
                           Parameters: "code: security code string",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "SECURE HP03458",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        // ===== Subprogram Commands =====
        new Command_Entry( Command: "SUB",
                           Syntax: "SUB <name>",
                           Description: "Begin subprogram definition",
                           Category: Command_Category.Subprogram,
                           Parameters: "name: subprogram name string",
                           Query_Form: "SUB?",
                           Default_Value: "N/A",
                           Example: "SUB MY_TEST",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "SUBEND",
                           Syntax: "SUBEND",
                           Description: "End subprogram definition",
                           Category: Command_Category.Subprogram,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "SUBEND",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "CALL",
                           Syntax: "CALL <name>",
                           Description: "Execute a stored subprogram",
                           Category: Command_Category.Subprogram,
                           Parameters: "name: subprogram name string",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CALL MY_TEST",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "CONT",
                           Syntax: "CONT",
                           Description: "Continue execution after a pause in subprogram",
                           Category: Command_Category.Subprogram,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CONT",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "PAUSE",
                           Syntax: "PAUSE",
                           Description: "Pause subprogram execution until CONT is received",
                           Category: Command_Category.Subprogram,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "PAUSE",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "DELSUB",
                           Syntax: "DELSUB <name>",
                           Description: "Delete a stored subprogram from memory",
                           Category: Command_Category.Subprogram,
                           Parameters: "name: subprogram name to delete",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "DELSUB MY_TEST",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),
      };

      Commands.Sort( ( A, B ) => string.Compare( A.Command, B.Command, StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
