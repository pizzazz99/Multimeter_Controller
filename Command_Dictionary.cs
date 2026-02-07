// ============================================================================
// File:        Command_Dictionary.cs
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
//   - Command_Dictionary static class: Provides a single factory method
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
//   List<Command_Entry> commands = Command_Dictionary.Get_All_Commands();
//   // Iterate, filter by category, search by keyword, etc.
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

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
  }

  public static class Command_Dictionary
  {
    public static List<Command_Entry> Get_All_Commands (
      Meter_Type Meter = Meter_Type.Keysight_3458A )
    {
      if ( Meter == Meter_Type.HP_34401A )
      {
        return HP34401A_Command_Dictionary.Get_All_Commands ( );
      }

      if ( Meter == Meter_Type.HP_33120A )
      {
        return HP33120A_Command_Dictionary.Get_All_Commands ( );
      }

      var Commands = new List<Command_Entry>
      {
        // ===== Measurement Commands =====
        new Command_Entry (
          "DCV",
          "DCV [<range>[,<resolution>]]",
          "Configure DC voltage measurement",
          Command_Category.Measurement,
          "range: 0.1|1|10|100|1000|AUTO, resolution: 4-8.5 digits",
          "DCV?",
          "AUTO range, max resolution",
          "DCV 10,0.0001" ),

        new Command_Entry (
          "ACV",
          "ACV [<range>[,<resolution>]]",
          "Configure AC voltage measurement (RMS)",
          Command_Category.Measurement,
          "range: 0.1|1|10|100|1000|AUTO, resolution: 4-6.5 digits",
          "ACV?",
          "AUTO range, max resolution",
          "ACV 10" ),

        new Command_Entry (
          "ACDCV",
          "ACDCV [<range>[,<resolution>]]",
          "Configure AC+DC voltage measurement (true RMS)",
          Command_Category.Measurement,
          "range: 0.1|1|10|100|1000|AUTO, resolution: 4-6.5 digits",
          "ACDCV?",
          "AUTO range, max resolution",
          "ACDCV 10" ),

        new Command_Entry (
          "DCI",
          "DCI [<range>[,<resolution>]]",
          "Configure DC current measurement",
          Command_Category.Measurement,
          "range: 0.0001|0.001|0.01|0.1|1|AUTO, resolution: 4-8.5 digits",
          "DCI?",
          "AUTO range, max resolution",
          "DCI 0.1,0.00001" ),

        new Command_Entry (
          "ACI",
          "ACI [<range>[,<resolution>]]",
          "Configure AC current measurement (RMS)",
          Command_Category.Measurement,
          "range: 0.0001|0.001|0.01|0.1|1|AUTO, resolution: 4-6.5 digits",
          "ACI?",
          "AUTO range, max resolution",
          "ACI 1" ),

        new Command_Entry (
          "ACDCI",
          "ACDCI [<range>[,<resolution>]]",
          "Configure AC+DC current measurement (true RMS)",
          Command_Category.Measurement,
          "range: 0.0001|0.001|0.01|0.1|1|AUTO, resolution: 4-6.5 digits",
          "ACDCI?",
          "AUTO range, max resolution",
          "ACDCI 0.1" ),

        new Command_Entry (
          "OHM",
          "OHM [<range>[,<resolution>]]",
          "Configure 2-wire resistance measurement",
          Command_Category.Measurement,
          "range: 10|100|1E3|1E4|1E5|1E6|1E7|1E8|1E9|AUTO",
          "OHM?",
          "AUTO range, max resolution",
          "OHM 1E6" ),

        new Command_Entry (
          "OHMF",
          "OHMF [<range>[,<resolution>]]",
          "Configure 4-wire resistance measurement",
          Command_Category.Measurement,
          "range: 10|100|1E3|1E4|1E5|1E6|1E7|1E8|1E9|AUTO",
          "OHMF?",
          "AUTO range, max resolution",
          "OHMF 1E3,0.001" ),

        new Command_Entry (
          "FREQ",
          "FREQ [<range>[,<resolution>]]",
          "Configure frequency measurement",
          Command_Category.Measurement,
          "range: voltage range for gating, resolution: Hz",
          "FREQ?",
          "AUTO range",
          "FREQ 10" ),

        new Command_Entry (
          "PER",
          "PER [<range>[,<resolution>]]",
          "Configure period measurement",
          Command_Category.Measurement,
          "range: voltage range for gating, resolution: seconds",
          "PER?",
          "AUTO range",
          "PER 10" ),

        new Command_Entry (
          "DSAC",
          "DSAC [<range>]",
          "Configure direct sampling AC voltage measurement",
          Command_Category.Measurement,
          "range: 0.1|1|10|100|1000|AUTO",
          "DSAC?",
          "AUTO range",
          "DSAC 10" ),

        new Command_Entry (
          "DSDC",
          "DSDC [<range>]",
          "Configure direct sampling DC voltage measurement",
          Command_Category.Measurement,
          "range: 0.1|1|10|100|1000|AUTO",
          "DSDC?",
          "AUTO range",
          "DSDC 10" ),

        new Command_Entry (
          "SSAC",
          "SSAC [<range>]",
          "Configure sub-sampling AC voltage measurement",
          Command_Category.Measurement,
          "range: 0.1|1|10|100|1000|AUTO",
          "SSAC?",
          "AUTO range",
          "SSAC 10" ),

        new Command_Entry (
          "SSDC",
          "SSDC [<range>]",
          "Configure sub-sampling DC voltage measurement",
          Command_Category.Measurement,
          "range: 0.1|1|10|100|1000|AUTO",
          "SSDC?",
          "AUTO range",
          "SSDC 10" ),

        // ===== Configuration Commands =====
        new Command_Entry (
          "RANGE",
          "RANGE [<range>]",
          "Set or query the measurement range",
          Command_Category.Configuration,
          "range: numeric value or AUTO, MIN, MAX, DEF",
          "RANGE?",
          "AUTO",
          "RANGE 10" ),

        new Command_Entry (
          "ARANGE",
          "ARANGE <ON|OFF>",
          "Enable or disable autorange",
          Command_Category.Configuration,
          "ON|OFF",
          "ARANGE?",
          "ON",
          "ARANGE OFF" ),

        new Command_Entry (
          "NPLC",
          "NPLC <PLCs>",
          "Set integration time in power line cycles",
          Command_Category.Configuration,
          "PLCs: 0|1|2|10|50|100 (0 = sync sub-sampling)",
          "NPLC?",
          "10",
          "NPLC 100" ),

        new Command_Entry (
          "APER",
          "APER <seconds>",
          "Set integration aperture time in seconds",
          Command_Category.Configuration,
          "seconds: 500E-9 to 1.0",
          "APER?",
          "Determined by NPLC",
          "APER 0.1" ),

        new Command_Entry (
          "NDIG",
          "NDIG <digits>",
          "Set the number of display digits",
          Command_Category.Configuration,
          "digits: 3 to 8",
          "NDIG?",
          "8",
          "NDIG 6" ),

        new Command_Entry (
          "RES",
          "RES <resolution>",
          "Set measurement resolution",
          Command_Category.Configuration,
          "resolution: numeric value in measurement units",
          "RES?",
          "Function-dependent",
          "RES 0.0001" ),

        new Command_Entry (
          "AZERO",
          "AZERO <ON|OFF|ONCE>",
          "Control auto-zero function",
          Command_Category.Configuration,
          "ON: auto-zero every reading, OFF: disable, ONCE: single auto-zero",
          "AZERO?",
          "ON",
          "AZERO OFF" ),

        new Command_Entry (
          "FIXEDZ",
          "FIXEDZ <ON|OFF>",
          "Enable/disable fixed input impedance (10 MOhm) for DCV",
          Command_Category.Configuration,
          "ON: fixed 10 MOhm, OFF: >10 GOhm on 100mV-10V ranges",
          "FIXEDZ?",
          "OFF",
          "FIXEDZ ON" ),

        new Command_Entry (
          "LFILTER",
          "LFILTER <ON|OFF>",
          "Enable/disable analog low-pass filter",
          Command_Category.Configuration,
          "ON|OFF",
          "LFILTER?",
          "OFF",
          "LFILTER ON" ),

        new Command_Entry (
          "ACBAND",
          "ACBAND <low_freq>[,<high_freq>]",
          "Set AC measurement bandwidth limits",
          Command_Category.Configuration,
          "low_freq: 1-500000 Hz, high_freq: 1-500000 Hz",
          "ACBAND?",
          "2 Hz, 2 MHz",
          "ACBAND 20,100000" ),

        new Command_Entry (
          "SETACV",
          "SETACV <SYNC|RNDM>",
          "Set AC voltage measurement coupling method",
          Command_Category.Configuration,
          "SYNC: synchronous, RNDM: random sampling",
          "SETACV?",
          "SYNC",
          "SETACV RNDM" ),

        new Command_Entry (
          "LFREQ",
          "LFREQ <50|60>",
          "Set power line frequency reference",
          Command_Category.Configuration,
          "50: 50 Hz line, 60: 60 Hz line",
          "LFREQ?",
          "Auto-detected",
          "LFREQ 60" ),

        new Command_Entry (
          "OCOMP",
          "OCOMP <ON|OFF>",
          "Enable/disable offset-compensated ohms",
          Command_Category.Configuration,
          "ON|OFF (only for OHM/OHMF)",
          "OCOMP?",
          "OFF",
          "OCOMP ON" ),

        new Command_Entry (
          "DELAY",
          "DELAY <seconds>",
          "Set trigger delay time",
          Command_Category.Configuration,
          "seconds: -1 (auto) or 0 to 3600",
          "DELAY?",
          "-1 (auto)",
          "DELAY 0.5" ),

        // ===== Trigger Commands =====
        new Command_Entry (
          "TARM",
          "TARM <event>[,<count>]",
          "Set trigger arm event and optional count",
          Command_Category.Trigger,
          "event: AUTO|EXT|SGL|HOLD|SYN",
          "TARM?",
          "AUTO",
          "TARM SGL" ),

        new Command_Entry (
          "TRIG",
          "TRIG <event>[,<count>]",
          "Set trigger event source",
          Command_Category.Trigger,
          "event: AUTO|EXT|SGL|HOLD|SYN|LEVEL|LINE",
          "TRIG?",
          "AUTO",
          "TRIG EXT" ),

        new Command_Entry (
          "NRDGS",
          "NRDGS <count>[,<event>]",
          "Set number of readings per trigger",
          Command_Category.Trigger,
          "count: 1 to 16777215, event: AUTO|EXT|SGL|HOLD|SYN|TIMER|LINE|LEVEL",
          "NRDGS?",
          "1,AUTO",
          "NRDGS 100,TIMER" ),

        new Command_Entry (
          "TIMER",
          "TIMER <seconds>",
          "Set timer interval for timed triggers",
          Command_Category.Trigger,
          "seconds: 0.0001 to 3600",
          "TIMER?",
          "1",
          "TIMER 0.01" ),

        new Command_Entry (
          "SWEEP",
          "SWEEP <interval>[,<count>]",
          "Configure a sweep of measurements at fixed intervals",
          Command_Category.Trigger,
          "interval: seconds between readings, count: number of readings",
          "SWEEP?",
          "N/A",
          "SWEEP 0.001,1000" ),

        new Command_Entry (
          "LEVEL",
          "LEVEL <level>[,<edge>]",
          "Set trigger level and edge for level triggering",
          Command_Category.Trigger,
          "level: trigger voltage, edge: AC|DC",
          "LEVEL?",
          "0,DC",
          "LEVEL 1.5,DC" ),

        new Command_Entry (
          "SLOPE",
          "SLOPE <POS|NEG>",
          "Set external trigger slope",
          Command_Category.Trigger,
          "POS: positive edge, NEG: negative edge",
          "SLOPE?",
          "POS",
          "SLOPE NEG" ),

        // ===== Math Commands =====
        new Command_Entry (
          "MATH",
          "MATH <function>",
          "Select math operation applied to readings",
          Command_Category.Math,
          "function: OFF|CONT|DB|DBM|FILTER|NULL|PERC|PFAIL|RMS|SCALE|STAT",
          "MATH?",
          "OFF",
          "MATH NULL" ),

        new Command_Entry (
          "MMATH",
          "MMATH <function>[,<function2>...]",
          "Enable multiple simultaneous math operations",
          Command_Category.Math,
          "function: combination of MATH functions",
          "MMATH?",
          "OFF",
          "MMATH NULL,FILTER" ),

        new Command_Entry (
          "NULL",
          "NULL [<value>]",
          "Set null offset value for null math function",
          Command_Category.Math,
          "value: offset in measurement units (omit to use current reading)",
          "NULL?",
          "0",
          "NULL 0.00015" ),

        new Command_Entry (
          "SCALE",
          "SCALE <gain>[,<offset>]",
          "Set scale/offset: result = (reading * gain) + offset",
          Command_Category.Math,
          "gain: multiplier, offset: additive offset",
          "SCALE?",
          "1,0",
          "SCALE 1000,0" ),

        new Command_Entry (
          "PERC",
          "PERC <target>",
          "Set percent target for percent math function",
          Command_Category.Math,
          "target: 100% reference value",
          "PERC?",
          "1",
          "PERC 5.0" ),

        new Command_Entry (
          "DB",
          "DB <reference>",
          "Set dB reference for dB math function",
          Command_Category.Math,
          "reference: reference voltage in volts",
          "DB?",
          "1",
          "DB 1.0" ),

        new Command_Entry (
          "DBM",
          "DBM <impedance>",
          "Set reference impedance for dBm math function",
          Command_Category.Math,
          "impedance: reference impedance in ohms (50-8000)",
          "DBM?",
          "600",
          "DBM 50" ),

        new Command_Entry (
          "FILTER",
          "FILTER <count>",
          "Set number of readings for averaging digital filter",
          Command_Category.Math,
          "count: 1 to 100",
          "FILTER?",
          "10",
          "FILTER 20" ),

        new Command_Entry (
          "STAT",
          "STAT <ON|OFF>",
          "Enable/disable statistics accumulation",
          Command_Category.Math,
          "ON|OFF - reports mean, std dev, min, max, count",
          "STAT?",
          "OFF",
          "STAT ON" ),

        new Command_Entry (
          "RMATH",
          "RMATH",
          "Query math register (accumulated stats result)",
          Command_Category.Math,
          "Returns statistic values",
          "RMATH?",
          "N/A",
          "RMATH" ),

        new Command_Entry (
          "PFAIL",
          "PFAIL <low>,<high>",
          "Set pass/fail limits for pass/fail math function",
          Command_Category.Math,
          "low: lower limit, high: upper limit",
          "PFAIL?",
          "N/A",
          "PFAIL 4.9,5.1" ),

        // ===== System Commands =====
        new Command_Entry (
          "RESET",
          "RESET",
          "Reset instrument to factory default state",
          Command_Category.System,
          "None",
          "",
          "N/A",
          "RESET" ),

        new Command_Entry (
          "PRESET",
          "PRESET",
          "Preset instrument to fast DCV configuration",
          Command_Category.System,
          "None",
          "",
          "N/A",
          "PRESET" ),

        new Command_Entry (
          "ID",
          "ID?",
          "Query instrument identification string",
          Command_Category.System,
          "None",
          "ID?",
          "N/A",
          "ID?" ),

        new Command_Entry (
          "ERR",
          "ERR?",
          "Query and clear error register",
          Command_Category.System,
          "None",
          "ERR?",
          "N/A",
          "ERR?" ),

        new Command_Entry (
          "ERRSTR",
          "ERRSTR?",
          "Query error string description for last error",
          Command_Category.System,
          "None",
          "ERRSTR?",
          "N/A",
          "ERRSTR?" ),

        new Command_Entry (
          "AUXERR",
          "AUXERR?",
          "Query auxiliary error register for additional detail",
          Command_Category.System,
          "None",
          "AUXERR?",
          "N/A",
          "AUXERR?" ),

        new Command_Entry (
          "STB",
          "STB?",
          "Query status byte register",
          Command_Category.System,
          "None",
          "STB?",
          "N/A",
          "STB?" ),

        new Command_Entry (
          "SRQ",
          "SRQ <mask>",
          "Set service request enable mask",
          Command_Category.System,
          "mask: bit mask for SRQ conditions",
          "SRQ?",
          "0",
          "SRQ 16" ),

        new Command_Entry (
          "EMASK",
          "EMASK <mask>",
          "Set event status mask for SRQ generation",
          Command_Category.System,
          "mask: bit mask for event conditions",
          "EMASK?",
          "0",
          "EMASK 1" ),

        new Command_Entry (
          "END",
          "END <mode>",
          "Set GPIB end-or-identify (EOI) mode",
          Command_Category.System,
          "mode: OFF|ON|ALWAYS",
          "END?",
          "ALWAYS",
          "END ON" ),

        new Command_Entry (
          "LINE",
          "LINE?",
          "Query measured power line frequency",
          Command_Category.System,
          "None",
          "LINE?",
          "N/A",
          "LINE?" ),

        new Command_Entry (
          "TEMP",
          "TEMP?",
          "Query internal instrument temperature",
          Command_Category.System,
          "None",
          "TEMP?",
          "N/A",
          "TEMP?" ),

        new Command_Entry (
          "REV",
          "REV?",
          "Query firmware revision string",
          Command_Category.System,
          "None",
          "REV?",
          "N/A",
          "REV?" ),

        new Command_Entry (
          "OPT",
          "OPT?",
          "Query installed options",
          Command_Category.System,
          "None",
          "OPT?",
          "N/A",
          "OPT?" ),

        new Command_Entry (
          "TEST",
          "TEST",
          "Execute self-test and report results",
          Command_Category.System,
          "None",
          "TEST?",
          "N/A",
          "TEST" ),

        new Command_Entry (
          "SCRATCH",
          "SCRATCH",
          "Clear subprogram memory and reading memory",
          Command_Category.System,
          "None",
          "",
          "N/A",
          "SCRATCH" ),

        // ===== Memory Commands =====
        new Command_Entry (
          "MEM",
          "MEM <mode>",
          "Enable/disable reading memory storage",
          Command_Category.Memory,
          "mode: OFF|FIFO|LIFO|CONT",
          "MEM?",
          "OFF",
          "MEM FIFO" ),

        new Command_Entry (
          "MSIZE",
          "MSIZE <bytes>",
          "Set reading memory size in bytes",
          Command_Category.Memory,
          "bytes: 0 to available memory",
          "MSIZE?",
          "0",
          "MSIZE 50000" ),

        new Command_Entry (
          "RMEM",
          "RMEM <start>,<count>[,<format>]",
          "Recall readings from memory",
          Command_Category.Memory,
          "start: first reading (1-based), count: number of readings",
          "",
          "N/A",
          "RMEM 1,100" ),

        new Command_Entry (
          "MCOUNT",
          "MCOUNT?",
          "Query number of readings stored in memory",
          Command_Category.Memory,
          "None",
          "MCOUNT?",
          "N/A",
          "MCOUNT?" ),

        new Command_Entry (
          "MFORMAT",
          "MFORMAT <format>",
          "Set memory storage format for readings",
          Command_Category.Memory,
          "format: SREAL|DREAL|ASCII",
          "MFORMAT?",
          "DREAL",
          "MFORMAT SREAL" ),

        new Command_Entry (
          "SSTATE",
          "SSTATE <register>",
          "Save current instrument state to register",
          Command_Category.Memory,
          "register: 0 to 9",
          "",
          "N/A",
          "SSTATE 1" ),

        new Command_Entry (
          "RSTATE",
          "RSTATE <register>",
          "Recall saved instrument state from register",
          Command_Category.Memory,
          "register: 0 to 9",
          "",
          "N/A",
          "RSTATE 1" ),

        // ===== I/O Commands =====
        new Command_Entry (
          "DISP",
          "DISP <mode>[,<message>]",
          "Control front panel display",
          Command_Category.IO,
          "mode: OFF|ON|MSG (MSG displays custom text)",
          "DISP?",
          "ON",
          "DISP MSG,\"TESTING\"" ),

        new Command_Entry (
          "BEEP",
          "BEEP",
          "Emit a single beep from the instrument",
          Command_Category.IO,
          "None",
          "",
          "N/A",
          "BEEP" ),

        new Command_Entry (
          "OFORMAT",
          "OFORMAT <format>",
          "Set output data format for readings",
          Command_Category.IO,
          "format: ASCII|SINT|DINT|SREAL|DREAL",
          "OFORMAT?",
          "ASCII",
          "OFORMAT DREAL" ),

        new Command_Entry (
          "INBUF",
          "INBUF <ON|OFF>",
          "Enable/disable input buffer for command queuing",
          Command_Category.IO,
          "ON|OFF",
          "INBUF?",
          "ON",
          "INBUF ON" ),

        new Command_Entry (
          "TBUFF",
          "TBUFF <ON|OFF>",
          "Enable/disable output transfer buffer",
          Command_Category.IO,
          "ON|OFF",
          "TBUFF?",
          "OFF",
          "TBUFF ON" ),

        new Command_Entry (
          "GPIB",
          "GPIB <address>",
          "Set GPIB address (stored in non-volatile memory)",
          Command_Category.IO,
          "address: 0 to 30",
          "GPIB?",
          "22",
          "GPIB 22" ),

        new Command_Entry (
          "EXTOUT",
          "EXTOUT <signal>",
          "Set external output (rear BNC) signal source",
          Command_Category.IO,
          "signal: ICOMP|EGUARD|DGRADE|PFAIL|RCOMP|OFF",
          "EXTOUT?",
          "OFF",
          "EXTOUT ICOMP" ),

        new Command_Entry (
          "LOCK",
          "LOCK <ON|OFF>",
          "Lock/unlock front panel controls",
          Command_Category.IO,
          "ON: lock panel, OFF: unlock panel",
          "LOCK?",
          "OFF",
          "LOCK ON" ),

        // ===== Calibration Commands =====
        new Command_Entry (
          "ACAL",
          "ACAL <type>",
          "Perform auto-calibration",
          Command_Category.Calibration,
          "type: DCV|AC|OHMS|ALL",
          "",
          "N/A",
          "ACAL ALL" ),

        new Command_Entry (
          "CAL",
          "CAL <step>",
          "Perform manual calibration step",
          Command_Category.Calibration,
          "step: calibration step number",
          "",
          "N/A",
          "CAL 1" ),

        new Command_Entry (
          "CALNUM",
          "CALNUM?",
          "Query calibration count (number of calibrations performed)",
          Command_Category.Calibration,
          "None",
          "CALNUM?",
          "N/A",
          "CALNUM?" ),

        new Command_Entry (
          "CALSTR",
          "CALSTR <string>",
          "Set calibration string (user-defined label)",
          Command_Category.Calibration,
          "string: up to 40 characters",
          "CALSTR?",
          "N/A",
          "CALSTR \"CAL 2024-01-15\"" ),

        new Command_Entry (
          "SECURE",
          "SECURE <code>",
          "Set calibration security code",
          Command_Category.Calibration,
          "code: security code string",
          "",
          "N/A",
          "SECURE HP03458" ),

        // ===== Subprogram Commands =====
        new Command_Entry (
          "SUB",
          "SUB <name>",
          "Begin subprogram definition",
          Command_Category.Subprogram,
          "name: subprogram name string",
          "",
          "N/A",
          "SUB MY_TEST" ),

        new Command_Entry (
          "SUBEND",
          "SUBEND",
          "End subprogram definition",
          Command_Category.Subprogram,
          "None",
          "",
          "N/A",
          "SUBEND" ),

        new Command_Entry (
          "CALL",
          "CALL <name>",
          "Execute a stored subprogram",
          Command_Category.Subprogram,
          "name: subprogram name string",
          "",
          "N/A",
          "CALL MY_TEST" ),

        new Command_Entry (
          "CONT",
          "CONT",
          "Continue execution after a pause in subprogram",
          Command_Category.Subprogram,
          "None",
          "",
          "N/A",
          "CONT" ),

        new Command_Entry (
          "PAUSE",
          "PAUSE",
          "Pause subprogram execution until CONT is received",
          Command_Category.Subprogram,
          "None",
          "",
          "N/A",
          "PAUSE" ),

        new Command_Entry (
          "DELSUB",
          "DELSUB <name>",
          "Delete a stored subprogram from memory",
          Command_Category.Subprogram,
          "name: subprogram name to delete",
          "",
          "N/A",
          "DELSUB MY_TEST" ),
      };

      Commands.Sort ( ( A, B ) =>
        string.Compare ( A.Command, B.Command,
          StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
