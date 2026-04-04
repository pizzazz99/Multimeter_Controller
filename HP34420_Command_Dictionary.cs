// =============================================================================
// FILE:     HP34420_Command_Dictionary_Class.cs
// PROJECT:  Multimeter_Controller
// =============================================================================
//
// DESCRIPTION:
//   Static command dictionary for the HP / Agilent 34420A Nano Volt / Micro
//   Ohm Meter. Provides a structured, searchable registry of all supported
//   SCPI instrument commands, organized by functional category. Each entry
//   captures the full command syntax, description, valid parameter ranges,
//   query form, factory default value, and a usage example.
//
//   The 34420A is a specialized low-level DC instrument optimized for
//   nanovolt-range voltage measurement and micro-ohm resistance measurement.
//   Its command set is deliberately narrower than the general-purpose 34401A —
//   there is no AC voltage, no current, no frequency, no diode, and no
//   continuity function. The distinguishing features relative to the 34401A
//   are the DC voltage ratio function, per-function NPLC and range commands,
//   a built-in digital averaging filter, and SYST:LOC for explicit local mode
//   return.
//
// -----------------------------------------------------------------------------
// INSTRUMENT:
//   HP / Agilent 34420A Nano Volt / Micro Ohm Meter
//   Command Set:  SCPI (Standard Commands for Programmable Instruments)
//   Interface:    GPIB (IEEE-488.2) / RS-232
//   DC Voltage:   100 nV resolution (100 mV range, MAX NPLC)
//   Resistance:   2-wire and 4-wire, up to 100 MΩ
//
// -----------------------------------------------------------------------------
// COMMAND SET OVERVIEW:
//   This dictionary uses SCPI short-form mnemonics with colon-separated node
//   paths (e.g. "CONF:VOLT:DC", "FRES:NPLC"). The long-form expansion is
//   shown in the Syntax field of each entry.
//
//   IMPORTANT — MEAS vs CONF vs READ?/FETCH? distinction:
//     MEAS:xxxx?   Combines CONFigure + INITiate + FETCh into a single call.
//                  Resets all settings to defaults for the selected function,
//                  then returns one reading. Use for quick one-shot measurements.
//     CONF:xxxx    Configures the function and range without triggering.
//                  Follow with INIT (or READ?) to acquire readings.
//     READ?        Equivalent to INIT + FETCH? — triggers the measurement
//                  state machine and returns the result. Respects current config.
//     FETCH?       Returns the last completed reading without re-triggering.
//                  Only valid after INIT has been issued.
//
//   IMPORTANT — DC voltage ratio (VOLT:DC:RAT):
//     The ratio function divides the Input terminal voltage by the Sense
//     terminal voltage and returns the dimensionless ratio. It is unique to
//     the 34420A within this instrument family. Both terminals must be driven;
//     the Sense terminal uses a separate low-level input path. This function
//     is not present on the 34401A.
//
//   IMPORTANT — Digital averaging filter (VOLT:DC:AVER):
//     The 34420A provides a software averaging filter independent of NPLC.
//     MOVing mode averages the last N readings on a rolling basis (low latency,
//     continuous output). REPeat mode accumulates exactly N readings and
//     outputs one result (higher latency, lower noise). Enable with
//     VOLT:DC:AVER:STAT ON; set count with VOLT:DC:AVER:COUN.
//     The filter applies to DC voltage only — resistance measurements use
//     NPLC alone for noise rejection.
//
// -----------------------------------------------------------------------------
// COMMAND CATEGORIES:
//
//   Measurement   — One-shot measurement queries (MEAS:VOLT:DC?,
//                   MEAS:VOLT:DC:RAT?, MEAS:RES?, MEAS:FRES?) plus READ? and
//                   FETCH?. These commands return a reading immediately or
//                   retrieve the last triggered result.
//
//   Configuration — Function setup commands (CONF:VOLT:DC, CONF:VOLT:DC:RAT,
//                   CONF:RES, CONF:FRES) and per-function parameter commands:
//                   DC voltage range (VOLT:DC:RANG / VOLT:DC:RANG:AUTO),
//                   DC voltage integration time (VOLT:DC:NPLC),
//                   DC voltage resolution (VOLT:DC:RES),
//                   resistance range (RES:RANG / RES:RANG:AUTO),
//                   resistance integration time (RES:NPLC),
//                   4-wire resistance range (FRES:RANG / FRES:RANG:AUTO),
//                   4-wire resistance integration time (FRES:NPLC),
//                   autozero (ZERO:AUTO), and digital filter
//                   (VOLT:DC:AVER:STAT / VOLT:DC:AVER:TCON / VOLT:DC:AVER:COUN).
//
//   Trigger       — Trigger source (TRIG:SOUR), trigger delay (TRIG:DEL /
//                   TRIG:DEL:AUTO), trigger count (TRIG:COUN), sample count
//                   per trigger (SAMP:COUN), trigger initiation (INIT), and
//                   bus trigger (*TRG).
//
//   Math          — Math function selection (CALC:FUNC) and enable (CALC:STAT),
//                   null offset (CALC:NULL:OFFS), limit test bounds
//                   (CALC:LIM:LOW / CALC:LIM:UPP), and statistics queries
//                   (CALC:AVER:MIN?, CALC:AVER:MAX?, CALC:AVER:AVER?,
//                   CALC:AVER:COUN?). Note: DB and DBM math functions are
//                   listed in CALC:FUNC parameters but are of limited utility
//                   on a nanovolt meter — verify firmware support before use.
//
//   System        — Identification (*IDN?), reset (*RST), clear status (*CLS),
//                   operation complete (*OPC / *OPC?), self-test (*TST?),
//                   error queue (SYST:ERR?), remote/local mode (SYST:REM /
//                   SYST:LOC), SCPI version (SYST:VERS?), status/event
//                   registers (*STB?, *SRE, *ESE, *ESR?), and state save/recall
//                   (*SAV / *RCL, registers 0–2).
//
//   I/O           — Display enable/disable (DISP), custom display text
//                   (DISP:TEXT / DISP:TEXT:CLE), beeper (SYST:BEEP /
//                   SYST:BEEP:STAT), terminal query (ROUT:TERM?), and output
//                   data format (FORM).
//
//   Memory        — Reading memory point count (DATA:POINts?) and reading
//                   memory destination routing (DATA:FEED).
//
//   Calibration   — Calibration security (CAL:SEC:STAT), calibration count
//                   query (CAL:COUN?), and calibration string label (CAL:STR).
//
// -----------------------------------------------------------------------------
// KEY METHOD:
//
//   Get_All_Commands()
//     Returns a List<Command_Entry> containing all registered commands,
//     sorted alphabetically by Command name (case-insensitive). The list
//     is rebuilt on every call — it is not cached.
//
//   NOTE: This class does not implement Get_Command_By_Name(). If name-based
//   lookup is needed, add it following the pattern in HP3458_Command_Dictionary_
//   Class, with a null guard on Query_Form before the query-form comparison
//   branch (several entries in this dictionary use null for Query_Form).
//
// -----------------------------------------------------------------------------
// DATA MODEL — Command_Entry fields:
//
//   Command       The primary SCPI short-form mnemonic (e.g. "CONF:VOLT:DC").
//                 IEEE-488.2 common commands use the asterisk prefix (e.g. "*RST").
//   Syntax        Full long-form syntax string including optional parameters.
//   Description   Human-readable description of the command's purpose.
//   Category      Command_Category enum value for grouping/filtering.
//   Parameters    Enumeration of valid parameter values and their meanings.
//   Query_Form    The query variant of the command, or null / empty string if
//                 no query form exists. Callers must null-check before use —
//                 *CLS, SYST:BEEP, *RCL, and INIT all use null or empty here.
//   Default_Value The factory default state for this setting after *RST.
//   Example       A representative usage string suitable for direct GPIB/RS-232
//                 transmission.
//
// -----------------------------------------------------------------------------
// USAGE NOTES:
//
//   - ZERO:AUTO ONCE performs a single autozero measurement and then disables
//     further autozero. This is the recommended mode for high-speed logging
//     where the overhead of per-reading autozero is unacceptable but a baseline
//     correction is still needed.
//
//   - VOLT:DC:NPLC 100 is the maximum integration time (100 power line cycles).
//     At 60 Hz this is approximately 1.67 seconds per reading. At this setting
//     the instrument achieves its lowest noise floor and is the appropriate
//     mode for nanovolt-level measurements.
//
//   - RES:NPLC and FRES:NPLC are independent of VOLT:DC:NPLC. Each function
//     retains its own NPLC setting. A CONF:xxxx or MEAS:xxxx? call does not
//     reset the other function's NPLC.
//
//   - The digital filter (VOLT:DC:AVER) stacks on top of NPLC integration —
//     total settling time is approximately NPLC × filter_count × line_period.
//     With NPLC 100 and AVER:COUN 10 at 60 Hz, one output reading takes
//     roughly 16.7 seconds. Size the filter count accordingly.
//
//   - SYST:LOC is present in this dictionary (unique among the instruments in
//     this project). It explicitly returns the front panel to local control
//     over RS-232, where GPIB GTL is not available. Over GPIB, asserting GTL
//     is the preferred method; SYST:LOC is the RS-232 equivalent.
//
//   - *SAV / *RCL support registers 0–2 (three registers, same as the 34401A).
//     Register 0 is recalled automatically at power-on.
//
//   - Query_Form is null for *CLS, SYST:BEEP, and *RCL. It is an empty string
//     for INIT and *TRG. Callers must handle both cases.
//
// -----------------------------------------------------------------------------
// DEPENDENCIES:
//   System                        (using directive present in source)
//   System.Collections.Generic    List<T>
//   System.Linq                   (using directive present; not used here)
//   System.Text                   (using directive present; not used here)
//   System.Threading.Tasks        (using directive present; not used here)
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
//   To add name-based lookup: implement Get_Command_By_Name() following the
//   pattern in HP3458_Command_Dictionary_Class, with explicit null guards on
//   Query_Form before any string comparison involving that field.
//
//   Unused using directives (System.Linq, System.Text, System.Threading.Tasks)
//   can be removed without impact — they are boilerplate from the file template
//   and are not referenced anywhere in this class.
//
// =============================================================================

namespace Multimeter_Controller
{
  public static class HP34420_Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands()
    {
      var Commands = new List<Command_Entry>
      {
        // ===== Measurement Commands =====
        new Command_Entry (
          Command:"MEAS:VOLT:DC?",
          Syntax:"MEASure:VOLTage:DC? [<range>[,<resolution>]]",
          Description:"Measure DC voltage and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: 0.1|1|10|100|1000|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"MEAS:VOLT:DC?",
          Default_Value:"AUTO range, default resolution",
          Example:"MEAS:VOLT:DC? 1,MIN" ),

        new Command_Entry (
          Command:"MEAS:VOLT:DC:RAT?",
          Syntax:"MEASure:VOLTage:DC:RATio? [<range>[,<resolution>]]",
          Description:"Measure DC voltage ratio (input/sense) and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: 0.1|1|10|100|1000|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"MEAS:VOLT:DC:RAT?",
          Default_Value:"AUTO range, default resolution",
          Example:"MEAS:VOLT:DC:RAT? 1,MIN" ),

        new Command_Entry (
          Command:"MEAS:RES?",
          Syntax:"MEASure:RESistance? [<range>[,<resolution>]]",
          Description:"Measure 2-wire resistance and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          Query_Form:"MEAS:RES?",
          Default_Value:"AUTO range, default resolution",
          Example:"MEAS:RES? 1e3,MIN" ),

        new Command_Entry (
          Command:"MEAS:FRES?",
          Syntax:"MEASure:FRESistance? [<range>[,<resolution>]]",
          Description:"Measure 4-wire resistance and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          Query_Form:"MEAS:FRES?",
          Default_Value:"AUTO range, default resolution",
          Example:"MEAS:FRES? 1e3,MIN" ),

        new Command_Entry (
          Command:"READ?",
          Syntax:"READ?",
          Description:"Initiate a measurement and return the reading",
          Category:Command_Category.Measurement,
          Parameters:"None (uses current configuration)",
          Query_Form:"READ?",
          Default_Value:"N/A",
          Example:"READ?" ),

        new Command_Entry (
          Command:"FETCH?",
          Syntax:"FETCH?",
          Description:"Return the last reading without triggering a new measurement",
          Category:Command_Category.Measurement,
          Parameters:"None",
          Query_Form:"FETCH?",
          Default_Value:"N/A",
          Example:"FETCH?" ),

        // ===== Configuration Commands =====
        new Command_Entry (
          Command:"CONF:VOLT:DC",
          Syntax:"CONFigure:VOLTage:DC [<range>[,<resolution>]]",
          Description:"Configure DC voltage measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 0.1|1|10|100|1000|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"CONF:VOLT:DC?",
          Default_Value:"AUTO range, default resolution",
          Example:"CONF:VOLT:DC 1,MIN" ),

        new Command_Entry (
          Command:"CONF:VOLT:DC:RAT",
          Syntax:"CONFigure:VOLTage:DC:RATio [<range>[,<resolution>]]",
          Description:"Configure DC voltage ratio measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 0.1|1|10|100|1000|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"CONF:VOLT:DC:RAT?",
          Default_Value:"AUTO range, default resolution",
          Example:"CONF:VOLT:DC:RAT 1,MIN" ),

        new Command_Entry (
          Command:"CONF:RES",
          Syntax:"CONFigure:RESistance [<range>[,<resolution>]]",
          Description:"Configure 2-wire resistance measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          Query_Form:"CONF:RES?",
          Default_Value:"AUTO range, default resolution",
          Example:"CONF:RES 1e3" ),

        new Command_Entry (
          Command:"CONF:FRES",
          Syntax:"CONFigure:FRESistance [<range>[,<resolution>]]",
          Description:"Configure 4-wire resistance measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          Query_Form:"CONF:FRES?",
          Default_Value:"AUTO range, default resolution",
          Example:"CONF:FRES 1e3" ),

        new Command_Entry (
          Command:"VOLT:DC:RANG",
          Syntax:"VOLTage:DC:RANGe <range>",
          Description:"Set DC voltage range",
          Category:Command_Category.Configuration,
          Parameters:"range: 0.1|1|10|100|1000|MIN|MAX",
          Query_Form:"VOLT:DC:RANG?",
          Default_Value:"AUTO",
          Example:"VOLT:DC:RANG 1" ),

        new Command_Entry (
          Command:"VOLT:DC:RANG:AUTO",
          Syntax:"VOLTage:DC:RANGe:AUTO <mode>",
          Description:"Enable or disable DC voltage autoranging",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF|ONCE",
          Query_Form:"VOLT:DC:RANG:AUTO?",
          Default_Value:"ON",
          Example:"VOLT:DC:RANG:AUTO ON" ),

        new Command_Entry (
          Command:"VOLT:DC:NPLC",
          Syntax:"VOLTage:DC:NPLCycles <nplc>",
          Description:"Set DC voltage integration time in power line cycles",
          Category:Command_Category.Configuration,
          Parameters:"nplc: 0.02|0.2|1|10|100|MIN|MAX",
          Query_Form:"VOLT:DC:NPLC?",
          Default_Value:"10",
          Example:"VOLT:DC:NPLC 100" ),

        new Command_Entry (
          Command:"VOLT:DC:RES",
          Syntax:"VOLTage:DC:RESolution <resolution>",
          Description:"Set DC voltage measurement resolution",
          Category:Command_Category.Configuration,
          Parameters:"resolution: in volts, MIN|MAX",
          Query_Form:"VOLT:DC:RES?",
          Default_Value:"Depends on range and NPLC",
          Example:"VOLT:DC:RES MIN" ),

        new Command_Entry (
          Command:"RES:NPLC",
          Syntax:"RESistance:NPLCycles <nplc>",
          Description:"Set resistance integration time in power line cycles",
          Category:Command_Category.Configuration,
          Parameters:"nplc: 0.02|0.2|1|10|100|MIN|MAX",
          Query_Form:"RES:NPLC?",
          Default_Value:"10",
          Example:"RES:NPLC 10" ),

        new Command_Entry (
          Command:"RES:RANG",
          Syntax:"RESistance:RANGe <range>",
          Description:"Set 2-wire resistance range",
          Category:Command_Category.Configuration,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX",
          Query_Form:"RES:RANG?",
          Default_Value:"AUTO",
          Example:"RES:RANG 1e3" ),

        new Command_Entry (
          Command:"RES:RANG:AUTO",
          Syntax:"RESistance:RANGe:AUTO <mode>",
          Description:"Enable or disable resistance autoranging",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF|ONCE",
          Query_Form:"RES:RANG:AUTO?",
          Default_Value:"ON",
          Example:"RES:RANG:AUTO ON" ),

        new Command_Entry (
          Command:"FRES:NPLC",
          Syntax:"FRESistance:NPLCycles <nplc>",
          Description:"Set 4-wire resistance integration time in power line cycles",
          Category:Command_Category.Configuration,
          Parameters:"nplc: 0.02|0.2|1|10|100|MIN|MAX",
          Query_Form:"FRES:NPLC?",
          Default_Value:"10",
          Example:"FRES:NPLC 10" ),

        new Command_Entry (
          Command:"FRES:RANG",
          Syntax:"FRESistance:RANGe <range>",
          Description:"Set 4-wire resistance range",
          Category:Command_Category.Configuration,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX",
          Query_Form:"FRES:RANG?",
          Default_Value:"AUTO",
          Example:"FRES:RANG 1e3" ),

        new Command_Entry (
          Command:"FRES:RANG:AUTO",
          Syntax:"FRESistance:RANGe:AUTO <mode>",
          Description:"Enable or disable 4-wire resistance autoranging",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF|ONCE",
          Query_Form:"FRES:RANG:AUTO?",
          Default_Value:"ON",
          Example:"FRES:RANG:AUTO ON" ),

        new Command_Entry (
          Command:"ZERO:AUTO",
          Syntax:"ZERO:AUTO <mode>",
          Description:"Enable or disable auto-zero",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF|ONCE",
          Query_Form:"ZERO:AUTO?",
          Default_Value:"ON",
          Example:"ZERO:AUTO ON" ),

        new Command_Entry (
          Command:"VOLT:DC:AVER:TCON",
          Syntax:"VOLTage:DC:AVERage:TCONtrol <type>",
          Description:"Set digital filter type for DC voltage",
          Category:Command_Category.Configuration,
          Parameters:"type: MOVing|REPeat",
          Query_Form:"VOLT:DC:AVER:TCON?",
          Default_Value:"MOVing",
          Example:"VOLT:DC:AVER:TCON MOVing" ),

        new Command_Entry (
          Command:"VOLT:DC:AVER:COUN",
          Syntax:"VOLTage:DC:AVERage:COUNt <count>",
          Description:"Set digital filter count for DC voltage averaging",
          Category:Command_Category.Configuration,
          Parameters:"count: 2 to 100|MIN|MAX",
          Query_Form:"VOLT:DC:AVER:COUN?",
          Default_Value:"10",
          Example:"VOLT:DC:AVER:COUN 10" ),

        new Command_Entry (
          Command:"VOLT:DC:AVER:STAT",
          Syntax:"VOLTage:DC:AVERage:STATe <mode>",
          Description:"Enable or disable digital filter for DC voltage",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF",
          Query_Form:"VOLT:DC:AVER:STAT?",
          Default_Value:"OFF",
          Example:"VOLT:DC:AVER:STAT ON" ),

        // ===== Trigger Commands =====
        new Command_Entry (
          Command:"TRIG:SOUR",
          Syntax:"TRIGger:SOURce <source>",
          Description:"Select trigger source",
          Category:Command_Category.Trigger,
          Parameters:"source: IMMediate|BUS|EXTernal",
          Query_Form:"TRIG:SOUR?",
          Default_Value:"IMMediate",
          Example:"TRIG:SOUR BUS" ),

        new Command_Entry (
          Command:"TRIG:DEL",
          Syntax:"TRIGger:DELay <seconds>",
          Description:"Set trigger delay",
          Category:Command_Category.Trigger,
          Parameters:"seconds: 0 to 3600|MIN|MAX",
          Query_Form:"TRIG:DEL?",
          Default_Value:"AUTO",
          Example:"TRIG:DEL 0.5" ),

        new Command_Entry (
          Command:"TRIG:DEL:AUTO",
          Syntax:"TRIGger:DELay:AUTO <mode>",
          Description:"Enable or disable automatic trigger delay",
          Category:Command_Category.Trigger,
          Parameters:"mode: ON|OFF",
          Query_Form:"TRIG:DEL:AUTO?",
          Default_Value:"ON",
          Example:"TRIG:DEL:AUTO ON" ),

        new Command_Entry (
          Command:"TRIG:COUN",
          Syntax:"TRIGger:COUNt <count>",
          Description:"Set number of triggers to accept before returning to idle",
          Category:Command_Category.Trigger,
          Parameters:"count: 1 to 50000|MIN|MAX|INFinity",
          Query_Form:"TRIG:COUN?",
          Default_Value:"1",
          Example:"TRIG:COUN 10" ),

        new Command_Entry (
          Command:"SAMP:COUN",
          Syntax:"SAMPle:COUNt <count>",
          Description:"Set number of readings per trigger",
          Category:Command_Category.Trigger,
          Parameters:"count: 1 to 50000|MIN|MAX",
          Query_Form:"SAMP:COUN?",
          Default_Value:"1",
          Example:"SAMP:COUN 5" ),

        new Command_Entry (
          Command:"INIT",
          Syntax:"INITiate",
          Description:"Change trigger state from idle to wait-for-trigger",
          Category:Command_Category.Trigger,
          Parameters:"None",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"INIT" ),

        new Command_Entry (
          Command:"*TRG",
          Syntax:"*TRG",
          Description:"Send a bus trigger (trigger source must be BUS)",
          Category:Command_Category.Trigger,
          Parameters:"None (requires TRIG:SOUR BUS)",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"*TRG" ),

        // ===== Math Commands =====
        new Command_Entry (
          Command:"CALC:FUNC",
          Syntax:"CALCulate:FUNCtion <function>",
          Description:"Select math function",
          Category:Command_Category.Math,
          Parameters:"function: NULL|DB|DBM|AVERage|MIN|MAX|LIMit",
          Query_Form:"CALC:FUNC?",
          Default_Value:"NULL",
          Example:"CALC:FUNC NULL" ),

        new Command_Entry (
          Command:"CALC:STAT",
          Syntax:"CALCulate:STATe <mode>",
          Description:"Enable or disable math operations",
          Category:Command_Category.Math,
          Parameters:"mode: ON|OFF",
          Query_Form:"CALC:STAT?",
          Default_Value:"OFF",
          Example:"CALC:STAT ON" ),

        new Command_Entry (
          Command:"CALC:NULL:OFFS",
          Syntax:"CALCulate:NULL:OFFSet <value>",
          Description:"Set null (offset) value for null math function",
          Category:Command_Category.Math,
          Parameters:"value: -1e15 to +1e15|MIN|MAX",
          Query_Form:"CALC:NULL:OFFS?",
          Default_Value:"0",
          Example:"CALC:NULL:OFFS 0.000001" ),

        new Command_Entry (
          Command:"CALC:LIM:LOW",
          Syntax:"CALCulate:LIMit:LOWer <value>",
          Description:"Set lower limit for limit testing",
          Category:Command_Category.Math,
          Parameters:"value: -1e15 to +1e15|MIN|MAX",
          Query_Form:"CALC:LIM:LOW?",
          Default_Value:"0",
          Example:"CALC:LIM:LOW -0.000001" ),

        new Command_Entry (
          Command:"CALC:LIM:UPP",
          Syntax:"CALCulate:LIMit:UPPer <value>",
          Description:"Set upper limit for limit testing",
          Category:Command_Category.Math,
          Parameters:"value: -1e15 to +1e15|MIN|MAX",
          Query_Form:"CALC:LIM:UPP?",
          Default_Value:"0",
          Example:"CALC:LIM:UPP 0.000001" ),

        new Command_Entry (
          Command:"CALC:AVER:MIN?",
          Syntax:"CALCulate:AVERage:MINimum?",
          Description:"Query minimum reading from math register",
          Category:Command_Category.Math,
          Parameters:"None",
          Query_Form:"CALC:AVER:MIN?",
          Default_Value:"N/A",
          Example:"CALC:AVER:MIN?" ),

        new Command_Entry (
          Command:"CALC:AVER:MAX?",
          Syntax:"CALCulate:AVERage:MAXimum?",
          Description:"Query maximum reading from math register",
          Category:Command_Category.Math,
          Parameters:"None",
          Query_Form:"CALC:AVER:MAX?",
          Default_Value:"N/A",
          Example:"CALC:AVER:MAX?" ),

        new Command_Entry (
          Command:"CALC:AVER:AVER?",
          Syntax:"CALCulate:AVERage:AVERage?",
          Description:"Query average of readings from math register",
          Category:Command_Category.Math,
          Parameters:"None",
          Query_Form:"CALC:AVER:AVER?",
          Default_Value:"N/A",
          Example:"CALC:AVER:AVER?" ),

        new Command_Entry (
          Command:"CALC:AVER:COUN?",
          Syntax:"CALCulate:AVERage:COUNt?",
          Description:"Query number of readings in math register",
          Category:Command_Category.Math,
          Parameters:"None",
          Query_Form:"CALC:AVER:COUN?",
          Default_Value:"N/A",
          Example:"CALC:AVER:COUN?" ),

        // ===== System Commands =====
        new Command_Entry (
          Command:"*IDN?",
          Syntax:"*IDN?",
          Description:"Query instrument identification string",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"*IDN?",
          Default_Value:"N/A",
          Example:"*IDN?" ),

        new Command_Entry (
          Command:"*RST",
          Syntax:"*RST",
          Description:"Reset instrument to factory default state",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"*RST" ),

        new Command_Entry (
          Command:"*CLS",
          Syntax:"*CLS",
          Description:"Clear status registers and error queue",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:null,
          Default_Value:"N/A",
          Example:"*CLS" ),

        new Command_Entry (
          Command:"*OPC?",
          Syntax:"*OPC?",
          Description:"Query operation complete (returns 1 when all pending operations finish)",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"*OPC?",
          Default_Value:"N/A",
          Example:"*OPC?" ),

        new Command_Entry (
          Command:"*OPC",
          Syntax:"*OPC",
          Description:"Set OPC bit in Standard Event register when all pending operations complete",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"*OPC" ),

        new Command_Entry (
          Command:"*TST?",
          Syntax:"*TST?",
          Description:"Perform self-test and return result (0 = pass)",
          Category:Command_Category.System,
          Parameters:"None (returns 0 for pass, non-zero for fail)",
          Query_Form:"*TST?",
          Default_Value:"N/A",
          Example:"*TST?" ),

        new Command_Entry (
          Command:"SYST:ERR?",
          Syntax:"SYSTem:ERRor?",
          Description:"Query and clear one error from the error queue",
          Category:Command_Category.System,
          Parameters:"None (returns error number and message)",
          Query_Form:"SYST:ERR?",
          Default_Value:"N/A",
          Example:"SYST:ERR?" ),

        new Command_Entry (
          Command:"SYST:REM",
          Syntax:"SYSTem:REMote",
          Description:"Set instrument to remote mode",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"SYST:REM" ),

        new Command_Entry (
          Command:"SYST:LOC",
          Syntax:"SYSTem:LOCal",
          Description:"Return instrument to local (front panel) control",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"SYST:LOC" ),

        new Command_Entry (
          Command:"SYST:VERS?",
          Syntax:"SYSTem:VERSion?",
          Description:"Query SCPI version supported by instrument",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"SYST:VERS?",
          Default_Value:"N/A",
          Example:"SYST:VERS?" ),

        new Command_Entry (
          Command:"*STB?",
          Syntax:"*STB?",
          Description:"Query status byte register",
          Category:Command_Category.System,
          Parameters:"None (returns decimal value of status byte)",
          Query_Form:"*STB?",
          Default_Value:"N/A",
          Example:"*STB?" ),

        new Command_Entry (
          Command:"*SRE",
          Syntax:"*SRE <value>",
          Description:"Set Service Request Enable register",
          Category:Command_Category.System,
          Parameters:"value: 0-255 (bit mask)",
          Query_Form:"*SRE?",
          Default_Value:"0",
          Example:"*SRE 32" ),

        new Command_Entry (
          Command:"*ESE",
          Syntax:"*ESE <value>",
          Description:"Set Standard Event Status Enable register",
          Category:Command_Category.System,
          Parameters:"value: 0-255 (bit mask)",
          Query_Form:"*ESE?",
          Default_Value:"0",
          Example:"*ESE 1" ),

        new Command_Entry (
          Command:"*ESR?",
          Syntax:"*ESR?",
          Description:"Query and clear Standard Event Status register",
          Category:Command_Category.System,
          Parameters:"None (returns decimal value of register)",
          Query_Form:"*ESR?",
          Default_Value:"N/A",
          Example:"*ESR?" ),

        new Command_Entry (
          Command:"*SAV",
          Syntax:"*SAV <register>",
          Description:"Save current instrument state to memory register",
          Category:Command_Category.System,
          Parameters:"register: 0|1|2",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"*SAV 1" ),

        new Command_Entry (
          Command:"*RCL",
          Syntax:"*RCL <register>",
          Description:"Recall instrument state from memory register",
          Category:Command_Category.System,
          Parameters:"register: 0|1|2",
          Query_Form:null,
          Default_Value:"N/A",
          Example:"*RCL 1" ),

        // ===== IO / Display Commands =====
        new Command_Entry (
          Command:"DISP",
          Syntax:"DISPlay <mode>",
          Description:"Enable or disable front panel display",
          Category:Command_Category.IO,
          Parameters:"mode: ON|OFF",
          Query_Form:"DISP?",
          Default_Value:"ON",
          Example:"DISP ON" ),

        new Command_Entry (
          Command:"DISP:TEXT",
          Syntax:"DISPlay:TEXT <string>",
          Description:"Display a text message on the front panel (max 12 chars)",
          Category:Command_Category.IO,
          Parameters:"string: up to 12 characters in quotes",
          Query_Form:"DISP:TEXT?",
          Default_Value:"N/A",
          Example:"DISP:TEXT \"HELLO\"" ),

        new Command_Entry (
          Command:"DISP:TEXT:CLE",
          Syntax:"DISPlay:TEXT:CLEar",
          Description:"Clear the displayed text message",
          Category:Command_Category.IO,
          Parameters:"None",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"DISP:TEXT:CLE" ),

        new Command_Entry (
          Command:"SYST:BEEP",
          Syntax:"SYSTem:BEEPer",
          Description:"Issue a single beep from the front panel",
          Category:Command_Category.IO,
          Parameters:"None",
          Query_Form:null,
          Default_Value:"N/A",
          Example:"SYST:BEEP" ),

        new Command_Entry (
          Command:"SYST:BEEP:STAT",
          Syntax:"SYSTem:BEEPer:STATe <mode>",
          Description:"Enable or disable beeper",
          Category:Command_Category.IO,
          Parameters:"mode: ON|OFF",
          Query_Form:"SYST:BEEP:STAT?",
          Default_Value:"ON",
          Example:"SYST:BEEP:STAT ON" ),

        new Command_Entry (
          Command:"ROUT:TERM?",
          Syntax:"ROUTe:TERMinals?",
          Description:"Query which terminal set is active (front or rear)",
          Category:Command_Category.IO,
          Parameters:"None (returns FRON or REAR)",
          Query_Form:"ROUT:TERM?",
          Default_Value:"N/A",
          Example:"ROUT:TERM?" ),

        new Command_Entry (
          Command:"FORM",
          Syntax:"FORMat:DATA <type>",
          Description:"Set data output format",
          Category:Command_Category.IO,
          Parameters:"type: ASCii|REAL,32|REAL,64",
          Query_Form:"FORM?",
          Default_Value:"ASCii",
          Example:"FORM ASC" ),

        // ===== Memory / Data Commands =====
        new Command_Entry (
          Command:"DATA:POINts?",
          Syntax:"DATA:POINts?",
          Description:"Query number of readings stored in internal memory",
          Category:Command_Category.Memory,
          Parameters:"None",
          Query_Form:"DATA:POIN?",
          Default_Value:"N/A",
          Example:"DATA:POIN?" ),

        new Command_Entry (
          Command:"DATA:FEED",
          Syntax:"DATA:FEED <destination>",
          Description:"Select reading memory destination",
          Category:Command_Category.Memory,
          Parameters:"destination: RDG_STORE,\"\" (disable) | RDG_STORE,\"CALC\" (enable)",
          Query_Form:"DATA:FEED?",
          Default_Value:"Disabled",
          Example:"DATA:FEED RDG_STORE,\"CALC\"" ),

        // ===== Calibration Commands =====
        new Command_Entry (
          Command:"CAL:SEC:STAT",
          Syntax:"CALibration:SECure:STATe <mode>,<code>",
          Description:"Enable or disable calibration security",
          Category:Command_Category.Calibration,
          Parameters:"mode: ON|OFF, code: security code string",
          Query_Form:"CAL:SEC:STAT?",
          Default_Value:"OFF",
          Example:"CAL:SEC:STAT ON,HP034420" ),

        new Command_Entry (
          Command:"CAL:COUN?",
          Syntax:"CALibration:COUNt?",
          Description:"Query the number of times the instrument has been calibrated",
          Category:Command_Category.Calibration,
          Parameters:"None",
          Query_Form:"CAL:COUN?",
          Default_Value:"N/A",
          Example:"CAL:COUN?" ),

        new Command_Entry (
          Command:"CAL:STR",
          Syntax:"CALibration:STRing <message>",
          Description:"Store a calibration message (max 40 chars)",
          Category:Command_Category.Calibration,
          Parameters:"message: up to 40 characters in quotes",
          Query_Form:"CAL:STR?",
          Default_Value:"N/A",
          Example:"CAL:STR \"Cal 2026-02-06\"" ),
      };

      Commands.Sort( ( A, B ) =>
        string.Compare( A.Command, B.Command,
          StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
