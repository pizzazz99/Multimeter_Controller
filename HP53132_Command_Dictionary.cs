
// =============================================================================
// FILE:     HP53132_Command_Dictionary_Class.cs
// PROJECT:  Multimeter_Controller
// =============================================================================
//
// DESCRIPTION:
//   Static command dictionary for the HP / Agilent 53132A Universal Counter.
//   Provides a structured, searchable registry of all supported SCPI instrument
//   commands, organized by functional category. Each entry captures the full
//   command syntax, description, valid parameter ranges, query form, factory
//   default value, and a usage example.
//
//   The 53132A is a time and frequency instrument, fundamentally different in
//   character from the voltmeters and source in this project. Its primary
//   measurements are frequency, period, time interval, phase, pulse width,
//   duty cycle, totalize, and peak voltage. It follows the same Command_Entry /
//   Command_Category conventions used by the other dictionaries in the
//   Multimeter_Controller namespace for project-wide consistency.
//
// -----------------------------------------------------------------------------
// INSTRUMENT:
//   HP / Agilent 53132A Universal Counter
//   Command Set:  SCPI (Standard Commands for Programmable Instruments)
//   Interface:    GPIB (IEEE-488.2) / RS-232
//   Channels:     2 main input channels + 1 optional high-frequency channel 3
//   Frequency:    DC to 225 MHz (ch1/ch2), optional 3 GHz (ch3)
//   Resolution:   Up to 12 digits/second (with 1 s gate time)
//
// -----------------------------------------------------------------------------
// COMMAND SET OVERVIEW:
//   This dictionary uses SCPI short-form mnemonics with colon-separated node
//   paths (e.g. "CONF:FREQ", "INP1:LEV"). The long-form expansion is shown in
//   the Syntax field of each entry.
//
//   IMPORTANT — MEAS vs CONF vs READ?/FETCH? distinction:
//     MEAS:xxxx?   Combines CONFigure + INITiate + FETCh into a single call.
//                  Resets all channel and gate settings to defaults for the
//                  selected function, then returns one reading. Use for quick
//                  one-shot measurements only.
//     CONF:xxxx    Configures the function without triggering. Preserves
//                  existing channel and gate arm settings. Follow with INIT
//                  (or READ?) to acquire readings.
//     READ?        Equivalent to INIT + FETCH? — triggers the measurement
//                  state machine and returns the result using current config.
//     FETCH?       Returns the last completed reading without re-triggering.
//                  Only valid after INIT has been issued. Returns stale data
//                  if called before any measurement has been initiated.
//     ABOR         Immediately returns the instrument to idle without waiting
//                  for the current measurement to complete. Use before
//                  reconfiguring a running measurement.
//
//   IMPORTANT — Channel parameter syntax:
//     Channel selectors use the SCPI list format: (@1), (@2), or (@3).
//     The channel number may also be embedded directly in the command node
//     for input configuration commands: INP1:LEV, INP2:COUP, etc.
//     Channel 3 is only available if the optional high-frequency module is
//     installed; it supports frequency measurement only (not period, pulse
//     width, time interval, phase, or totalize).
//
//   IMPORTANT — Three-subsystem math architecture (CALC / CALC2 / CALC3):
//     CALC       Math expression on raw measurement values. Primarily used
//                to apply unit scaling (e.g. multiply Hz by 1E6 to display
//                in MHz). Enable with CALC:MATH:STAT ON.
//     CALC2      Statistics on the post-CALC values. Selects mean, standard
//                deviation, min, max, peak-to-peak, or Allan deviation.
//                Enable with CALC2:STAT ON. DATA:FEED routes readings into
//                CALC2 accumulation via RDG_STORE,"CALC2".
//     CALC3      Limit checking against lower and upper bounds. Pass/fail
//                output can be routed to the rear-panel BNC via hardware
//                configuration. Enable with CALC3:STAT ON.
//     All three subsystems are independent and may be enabled simultaneously.
//
// -----------------------------------------------------------------------------
// COMMAND CATEGORIES:
//
//   Measurement   — One-shot measurement queries: frequency (MEAS:FREQ?),
//                   frequency ratio (MEAS:FREQ:RAT?), period (MEAS:PER?),
//                   positive pulse width (MEAS:PWID?), negative pulse width
//                   (MEAS:NWID?), duty cycle (MEAS:DCYC?), time interval
//                   (MEAS:TINT?), phase (MEAS:PHAS?), immediate totalize
//                   (MEAS:TOT:IMM?), peak maximum voltage (MEAS:VOLT:MAX?),
//                   peak minimum voltage (MEAS:VOLT:MIN?), plus READ? and
//                   FETCH?.
//
//   Configuration — Function setup commands (CONF:FREQ, CONF:FREQ:RAT,
//                   CONF:PER, CONF:PWID, CONF:NWID, CONF:DCYC, CONF:TINT,
//                   CONF:PHAS, CONF:TOT:IMM) and per-channel input parameter
//                   commands: coupling (INP:COUP), impedance (INP:IMP),
//                   trigger level (INP:LEV / INP:LEV:AUTO), trigger slope
//                   (INP:SLOP), low-pass filter (INP:FILT), and attenuator
//                   (INP:ATT).
//
//   Trigger       — Trigger source (TRIG:SOUR), trigger count (TRIG:COUN),
//                   sample count per trigger (SAMP:COUN), initiation (INIT),
//                   abort (ABOR), bus trigger (*TRG), and gate arming
//                   (FREQ:ARM:STAR:SOUR, FREQ:ARM:STOP:SOUR,
//                   FREQ:ARM:STOP:TIM).
//
//   Math          — CALC math expression (CALC:MATH:EXPR / CALC:MATH:STAT),
//                   CALC2 statistics (CALC2:FORM / CALC2:STAT / CALC2:IMM),
//                   and CALC3 limit checking (CALC3:FORM / CALC3:LIM:LOW /
//                   CALC3:LIM:UPP / CALC3:STAT).
//
//   System        — Identification (*IDN?), reset (*RST), clear status (*CLS),
//                   operation complete (*OPC / *OPC?), self-test (*TST?),
//                   error queue (SYST:ERR?), remote/local mode (SYST:REM /
//                   SYST:LOC), SCPI version (SYST:VERS?), status/event
//                   registers (*STB?, *SRE, *ESE, *ESR?), and state save/recall
//                   (*SAV / *RCL, registers 0–9).
//
//   I/O           — Display enable/disable (DISP), custom display text
//                   (DISP:TEXT / DISP:TEXT:CLE), output data format (FORM),
//                   and screen dump (HCOP:SDUMP:DATA?).
//
//   Memory        — Reading memory point count (DATA:POIN?), memory destination
//                   routing (DATA:FEED), and destructive memory read
//                   (DATA:REM?).
//
// -----------------------------------------------------------------------------
// GATE ARMING:
//   Gate time (the measurement window) is controlled by the ARM subsystem
//   under the FREQuency node rather than through NPLC or aperture as in the
//   voltmeters. The stop source defaults to TIME with a 0.1 s gate. Setting
//   FREQ:ARM:STOP:TIM to a longer interval increases resolution at the cost
//   of throughput: frequency resolution in Hz ≈ 1 / gate_time. For 12-digit
//   resolution at 10 MHz, use a 1 s gate (FREQ:ARM:STOP:TIM 1).
//
//   EXTernal arming allows the gate to be opened and closed by a signal on
//   the rear-panel Ext Arm BNC, enabling triggered or gated counting
//   synchronized to an external event.
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
//   Command       The primary SCPI short-form mnemonic (e.g. "CONF:FREQ").
//                 IEEE-488.2 common commands use the asterisk prefix (e.g. "*RST").
//                 Input channel commands embed the channel number in the node:
//                 "INP:COUP" is the template; "INP1:COUP" is the actual command
//                 sent to the instrument for channel 1.
//   Syntax        Full long-form syntax string including optional parameters
//                 and channel selectors.
//   Description   Human-readable description of the command's purpose.
//   Category      Command_Category enum value for grouping/filtering.
//   Parameters    Enumeration of valid parameter values and their meanings,
//                 including channel selectors where applicable.
//   Query_Form    The query variant of the command, or null / empty string if
//                 no query form exists. Callers must null-check before use —
//                 *CLS, *RCL, ABOR, INIT, *TRG, SYST:REM, SYST:LOC, and
//                 *RST use null or empty in this dictionary.
//   Default_Value The factory default state for this setting after *RST.
//   Example       A representative usage string suitable for direct GPIB/RS-232
//                 transmission.
//
// -----------------------------------------------------------------------------
// USAGE NOTES:
//
//   - *SAV / *RCL support registers 0–9 (ten registers — more than any other
//     instrument in this project).
//
//   - DATA:REM? is destructive: it removes the returned readings from memory.
//     Unlike DATA:POIN? (non-destructive count query), repeated DATA:REM?
//     calls drain the memory buffer. Use DATA:POIN? to check available count
//     before calling DATA:REM?.
//
//   - DATA:FEED routes readings into CALC2 accumulation using the string
//     "CALC2" (not "CALC" as used in the 34401A and 34420A). Passing the
//     wrong string silently disables memory storage with no error.
//
//   - INP:FILT enables a hardware 100 kHz low-pass filter. It should be
//     engaged when measuring low-frequency signals in the presence of
//     high-frequency noise. Enabling it on high-frequency signals will cause
//     the counter to miss transitions and return erroneous readings.
//
//   - INP:ATT 10 selects the ÷10 hardware attenuator, extending the usable
//     input range to ±50 V. The trigger level (INP:LEV) is always specified
//     in the post-attenuation domain — a physical 5 V threshold with ÷10
//     attenuation is entered as INP1:LEV 0.5.
//
//   - INP:LEV:AUTO ON enables automatic trigger level sensing based on the
//     signal's 50% amplitude point. Disable auto-level and set INP:LEV
//     manually when the signal is noisy, has an unusual duty cycle, or when
//     consistent triggering at a specific threshold is required.
//
//   - HCOP:SDUMP:DATA? returns a binary bitmap of the front panel display.
//     The format is instrument-specific; consult the 53132A programmer's
//     guide for the pixel layout and encoding before parsing the response.
//
//   - CALC:MATH:EXPR accepts a quoted arithmetic expression referencing
//     MEAS1 (the primary measurement result). Scaling to convenient units
//     is the primary use case, e.g. "(MEAS1)*1E-6" to convert Hz to MHz.
//     The expression parser is limited — complex expressions may not be
//     supported on all firmware revisions.
//
//   - Query_Form is null for *CLS and *RCL. It is an empty string for
//     ABOR, INIT, *TRG, *RST, *OPC, SYST:REM, SYST:LOC, and *SAV.
//     Callers must handle both cases.
//
// -----------------------------------------------------------------------------
// KNOWN SOURCE FILE ISSUE:
//   The file contains a duplicate using-directive block at the top:
//     using System;
//     using System.Collections.Generic;
//     using System.Linq;
//     using System.Text;
//     using System.Threading.Tasks;
//   The first two lines are redundant. Remove them (or the second block's
//   duplicates) to suppress compiler warnings. The unused directives
//   (System.Linq, System.Text, System.Threading.Tasks) can also be removed
//   without impact — they are boilerplate from the file template.
//
// -----------------------------------------------------------------------------
// DEPENDENCIES:
//   System                        (using directive present in source, duplicated)
//   System.Collections.Generic    List<T> (using directive present, duplicated)
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
//   To add channel 3 commands: channel 3 accepts only frequency measurement
//   commands. Register entries with channel selector (@3) in the Parameters
//   field and verify that CONF:FREQ and MEAS:FREQ? examples include (@3)
//   variants where useful.
//
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  public static class HP53132_Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands()
    {
      var Commands = new List<Command_Entry> {

        // ===== Measurement Commands =====
        new Command_Entry( Command: "MEAS:FREQ?",
                           Syntax: "MEASure:FREQuency? [<expected>[,<resolution>]][,<channel>]",
                           Description: "Measure frequency on specified channel and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: ( "expected: frequency hint in Hz|MIN|MAX|DEF, resolution: " + "MIN|" +
                                                                                                      "MAX|" +
                                                                                                      "DEF," +
                                                                                                      " cha" +
                                                                                                      "nnel" +
                                                                                                      ": " +
                                                                                                      "(@1)" +
                                                                                                      "|(@" +
                                                                                                      "2)|(" +
                                                                                                      "@3)" ),
                           Query_Form: "MEAS:FREQ?",
                           Default_Value: "AUTO, channel 1",
                           Example: "MEAS:FREQ? 10E6,1,(@1)",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:FREQ:RAT?",
                           Syntax: "MEASure:FREQuency:RATio? [<expected>[,<resolution>]]",
                           Description: "Measure frequency ratio of channel 2 / channel 1",
                           Category: Command_Category.Measurement,
                           Parameters: "expected: ratio hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "MEAS:FREQ:RAT?",
                           Default_Value: "AUTO",
                           Example: "MEAS:FREQ:RAT?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:PER?",
                           Syntax: "MEASure:PERiod? [<expected>[,<resolution>]][,<channel>]",
                           Description: "Measure period on specified channel and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: ( "expected: period hint in seconds|MIN|MAX|DEF, resolution: " + "MI" +
                                                                                                        "N|" +
                                                                                                        "MA" +
                                                                                                        "X|" +
                                                                                                        "DE" +
                                                                                                        "F," +
                                                                                                        " c" +
                                                                                                        "ha" +
                                                                                                        "nn" +
                                                                                                        "el" +
                                                                                                        ": " +
                                                                                                        "(@" +
                                                                                                        "1)" +
                                                                                                        "|(" +
                                                                                                        "@2" +
                                                                                                        ")" ),
                           Query_Form: "MEAS:PER?",
                           Default_Value: "AUTO, channel 1",
                           Example: "MEAS:PER? 100E-9,1E-12,(@1)",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:PWID?",
                           Syntax: "MEASure:PWIDth? [<expected>[,<resolution>]][,<channel>]",
                           Description: "Measure positive pulse width and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: ( "expected: width hint in seconds|MIN|MAX|DEF, resolution: " + "MIN" +
                                                                                                       "|MA" +
                                                                                                       "X|" +
                                                                                                       "DEF" +
                                                                                                       ", " +
                                                                                                       "cha" +
                                                                                                       "nne" +
                                                                                                       "l: " +
                                                                                                       "(@" +
                                                                                                       "1)|" +
                                                                                                       "(@" +
                                                                                                       "2)" ),
                           Query_Form: "MEAS:PWID?",
                           Default_Value: "AUTO, channel 1",
                           Example: "MEAS:PWID? (@1)",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:NWID?",
                           Syntax: "MEASure:NWIDth? [<expected>[,<resolution>]][,<channel>]",
                           Description: "Measure negative pulse width and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: ( "expected: width hint in seconds|MIN|MAX|DEF, resolution: " + "MIN" +
                                                                                                       "|MA" +
                                                                                                       "X|" +
                                                                                                       "DEF" +
                                                                                                       ", " +
                                                                                                       "cha" +
                                                                                                       "nne" +
                                                                                                       "l: " +
                                                                                                       "(@" +
                                                                                                       "1)|" +
                                                                                                       "(@" +
                                                                                                       "2)" ),
                           Query_Form: "MEAS:NWID?",
                           Default_Value: "AUTO, channel 1",
                           Example: "MEAS:NWID? (@1)",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:DCYC?",
                           Syntax: "MEASure:DCYCle? [<expected>[,<resolution>]][,<channel>]",
                           Description: "Measure duty cycle and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: ( "expected: duty cycle hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF, " +
                                         "channel: (@1)|(@2)" ),
                           Query_Form: "MEAS:DCYC?",
                           Default_Value: "AUTO, channel 1",
                           Example: "MEAS:DCYC? (@1)",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:TINT?",
                           Syntax: "MEASure:TINTerval?",
                           Description: ( "Measure time interval from channel 1 (start) to channel 2 " +
                                          "(stop)" ),
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "MEAS:TINT?",
                           Default_Value: "N/A",
                           Example: "MEAS:TINT?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:PHAS?",
                           Syntax: "MEASure:PHASe?",
                           Description: "Measure phase between channel 1 and channel 2 in degrees",
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "MEAS:PHAS?",
                           Default_Value: "N/A",
                           Example: "MEAS:PHAS?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:TOT:IMM?",
                           Syntax: "MEASure:TOTalize:IMMediate?",
                           Description: "Return immediate totalize count from channel 1",
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "MEAS:TOT:IMM?",
                           Default_Value: "N/A",
                           Example: "MEAS:TOT:IMM?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:VOLT:MAX?",
                           Syntax: "MEASure:VOLTage:MAXimum? [<channel>]",
                           Description: "Measure peak maximum voltage on specified channel",
                           Category: Command_Category.Measurement,
                           Parameters: "channel: (@1)|(@2)",
                           Query_Form: "MEAS:VOLT:MAX?",
                           Default_Value: "channel 1",
                           Example: "MEAS:VOLT:MAX? (@1)",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:VOLT:MIN?",
                           Syntax: "MEASure:VOLTage:MINimum? [<channel>]",
                           Description: "Measure peak minimum voltage on specified channel",
                           Category: Command_Category.Measurement,
                           Parameters: "channel: (@1)|(@2)",
                           Query_Form: "MEAS:VOLT:MIN?",
                           Default_Value: "channel 1",
                           Example: "MEAS:VOLT:MIN? (@1)",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "READ?",
                           Syntax: "READ?",
                           Description: "Initiate a measurement and return the reading",
                           Category: Command_Category.Measurement,
                           Parameters: "None (uses current configuration)",
                           Query_Form: "READ?",
                           Default_Value: "N/A",
                           Example: "READ?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "FETCH?",
                           Syntax: "FETCH?",
                           Description: "Return the last reading without triggering a new measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "FETCH?",
                           Default_Value: "N/A",
                           Example: "FETCH?",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        // ===== Configuration Commands =====
        new Command_Entry( Command: "CONF:FREQ",
                           Syntax: "CONFigure:FREQuency [<expected>[,<resolution>]][,<channel>]",
                           Description: "Configure frequency measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: ( "expected: frequency hint in Hz|MIN|MAX|DEF, resolution: " + "MIN|" +
                                                                                                      "MAX|" +
                                                                                                      "DEF," +
                                                                                                      " cha" +
                                                                                                      "nnel" +
                                                                                                      ": " +
                                                                                                      "(@1)" +
                                                                                                      "|(@" +
                                                                                                      "2)|(" +
                                                                                                      "@3)" ),
                           Query_Form: "CONF:FREQ?",
                           Default_Value: "AUTO, channel 1",
                           Example: "CONF:FREQ 10E6,1,(@1)",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:FREQ:RAT",
                           Syntax: "CONFigure:FREQuency:RATio [<expected>[,<resolution>]]",
                           Description: "Configure frequency ratio measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "expected: ratio hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "CONF:FREQ:RAT?",
                           Default_Value: "AUTO",
                           Example: "CONF:FREQ:RAT",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:PER",
                           Syntax: "CONFigure:PERiod [<expected>[,<resolution>]][,<channel>]",
                           Description: "Configure period measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: ( "expected: period hint in seconds|MIN|MAX|DEF, resolution: " + "MI" +
                                                                                                        "N|" +
                                                                                                        "MA" +
                                                                                                        "X|" +
                                                                                                        "DE" +
                                                                                                        "F," +
                                                                                                        " c" +
                                                                                                        "ha" +
                                                                                                        "nn" +
                                                                                                        "el" +
                                                                                                        ": " +
                                                                                                        "(@" +
                                                                                                        "1)" +
                                                                                                        "|(" +
                                                                                                        "@2" +
                                                                                                        ")" ),
                           Query_Form: "CONF:PER?",
                           Default_Value: "AUTO, channel 1",
                           Example: "CONF:PER (@1)",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:PWID",
                           Syntax: "CONFigure:PWIDth [<expected>[,<resolution>]][,<channel>]",
                           Description: "Configure positive pulse width measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: ( "expected: width hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF, " +
                                         "channel: (@1)|(@2)" ),
                           Query_Form: "CONF:PWID?",
                           Default_Value: "AUTO, channel 1",
                           Example: "CONF:PWID (@1)",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:NWID",
                           Syntax: "CONFigure:NWIDth [<expected>[,<resolution>]][,<channel>]",
                           Description: "Configure negative pulse width measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: ( "expected: width hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF, " +
                                         "channel: (@1)|(@2)" ),
                           Query_Form: "CONF:NWID?",
                           Default_Value: "AUTO, channel 1",
                           Example: "CONF:NWID (@1)",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:DCYC",
                           Syntax: "CONFigure:DCYCle [<expected>[,<resolution>]][,<channel>]",
                           Description: "Configure duty cycle measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: ( "expected: duty cycle hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF, " +
                                         "channel: (@1)|(@2)" ),
                           Query_Form: "CONF:DCYC?",
                           Default_Value: "AUTO, channel 1",
                           Example: "CONF:DCYC (@1)",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:TINT",
                           Syntax: "CONFigure:TINTerval",
                           Description: ( "Configure time interval measurement, ch1=start ch2=stop (does " +
                                          "not trigger)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "None",
                           Query_Form: "CONF:TINT?",
                           Default_Value: "N/A",
                           Example: "CONF:TINT",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:PHAS",
                           Syntax: "CONFigure:PHASe",
                           Description: ( "Configure phase measurement between channel 1 and channel 2 " +
                                          "(does not trigger)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "None",
                           Query_Form: "CONF:PHAS?",
                           Default_Value: "N/A",
                           Example: "CONF:PHAS",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:TOT:IMM",
                           Syntax: "CONFigure:TOTalize:IMMediate",
                           Description: "Configure immediate totalize on channel 1 (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "None",
                           Query_Form: "CONF:TOT:IMM?",
                           Default_Value: "N/A",
                           Example: "CONF:TOT:IMM",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:COUP",
                           Syntax: "INPut[<channel>]:COUPling <coupling>",
                           Description: "Set input coupling for specified channel",
                           Category: Command_Category.Configuration,
                           Parameters: "coupling: AC|DC, channel: 1|2 (default 1)",
                           Query_Form: "INP:COUP?",
                           Default_Value: "AC",
                           Example: "INP1:COUP DC",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:IMP",
                           Syntax: "INPut[<channel>]:IMPedance <impedance>",
                           Description: "Set input impedance for specified channel",
                           Category: Command_Category.Configuration,
                           Parameters: "impedance: 50|1E6 (ohms), channel: 1|2 (default 1)",
                           Query_Form: "INP:IMP?",
                           Default_Value: "1E6",
                           Example: "INP1:IMP 50",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:LEV",
                           Syntax: "INPut[<channel>]:LEVel <level>",
                           Description: "Set trigger level voltage for specified channel",
                           Category: Command_Category.Configuration,
                           Parameters: "level: voltage in volts|MIN|MAX, channel: 1|2 (default 1)",
                           Query_Form: "INP:LEV?",
                           Default_Value: "0.0",
                           Example: "INP1:LEV 1.5",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:LEV:AUTO",
                           Syntax: "INPut[<channel>]:LEVel:AUTO <mode>",
                           Description: "Enable or disable automatic trigger level for specified channel",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON|OFF, channel: 1|2 (default 1)",
                           Query_Form: "INP:LEV:AUTO?",
                           Default_Value: "ON",
                           Example: "INP1:LEV:AUTO ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:SLOP",
                           Syntax: "INPut[<channel>]:SLOPe <slope>",
                           Description: "Set trigger slope for specified channel",
                           Category: Command_Category.Configuration,
                           Parameters: "slope: POSitive|NEGative, channel: 1|2 (default 1)",
                           Query_Form: "INP:SLOP?",
                           Default_Value: "POSitive",
                           Example: "INP1:SLOP POS",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:FILT",
                           Syntax: "INPut[<channel>]:FILTer <mode>",
                           Description: "Enable or disable 100 kHz low-pass filter on specified channel",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON|OFF, channel: 1|2 (default 1)",
                           Query_Form: "INP:FILT?",
                           Default_Value: "OFF",
                           Example: "INP1:FILT ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:ATT",
                           Syntax: "INPut[<channel>]:ATTenuator <attenuation>",
                           Description: "Set input attenuator for specified channel",
                           Category: Command_Category.Configuration,
                           Parameters: "attenuation: 1|10, channel: 1|2 (default 1)",
                           Query_Form: "INP:ATT?",
                           Default_Value: "1",
                           Example: "INP1:ATT 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== Trigger / Gate Commands =====
        new Command_Entry( Command: "TRIG:SOUR",
                           Syntax: "TRIGger:SOURce <source>",
                           Description: "Select trigger source",
                           Category: Command_Category.Trigger,
                           Parameters: "source: IMMediate|BUS|EXTernal|INTernal2",
                           Query_Form: "TRIG:SOUR?",
                           Default_Value: "IMMediate",
                           Example: "TRIG:SOUR BUS",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "TRIG:COUN",
                           Syntax: "TRIGger:COUNt <count>",
                           Description: "Set number of triggers to accept before returning to idle",
                           Category: Command_Category.Trigger,
                           Parameters: "count: 1 to 1000000|MIN|MAX|INFinity",
                           Query_Form: "TRIG:COUN?",
                           Default_Value: "1",
                           Example: "TRIG:COUN 100",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SAMP:COUN",
                           Syntax: "SAMPle:COUNt <count>",
                           Description: "Set number of readings per trigger",
                           Category: Command_Category.Trigger,
                           Parameters: "count: 1 to 1000000|MIN|MAX",
                           Query_Form: "SAMP:COUN?",
                           Default_Value: "1",
                           Example: "SAMP:COUN 100",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INIT",
                           Syntax: "INITiate",
                           Description: "Change trigger state from idle to wait-for-trigger",
                           Category: Command_Category.Trigger,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "INIT",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "ABOR",
                           Syntax: "ABORt",
                           Description: "Abort measurement and return to idle state",
                           Category: Command_Category.Trigger,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "ABOR",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "*TRG",
                           Syntax: "*TRG",
                           Description: "Send a bus trigger (trigger source must be BUS)",
                           Category: Command_Category.Trigger,
                           Parameters: "None (requires TRIG:SOUR BUS)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*TRG",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "FREQ:ARM:STAR:SOUR",
                           Syntax: "FREQuency:ARM:STARt:SOURce <source>",
                           Description: "Set gate start arming source for frequency measurement",
                           Category: Command_Category.Trigger,
                           Parameters: "source: IMMediate|EXTernal|TIME|DINTernal",
                           Query_Form: "FREQ:ARM:STAR:SOUR?",
                           Default_Value: "IMMediate",
                           Example: "FREQ:ARM:STAR:SOUR IMM",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "FREQ:ARM:STOP:SOUR",
                           Syntax: "FREQuency:ARM:STOP:SOURce <source>",
                           Description: "Set gate stop arming source for frequency measurement",
                           Category: Command_Category.Trigger,
                           Parameters: "source: TIME|EXTernal|DINTernal",
                           Query_Form: "FREQ:ARM:STOP:SOUR?",
                           Default_Value: "TIME",
                           Example: "FREQ:ARM:STOP:SOUR TIME",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "FREQ:ARM:STOP:TIM",
                           Syntax: "FREQuency:ARM:STOP:TIMe <seconds>",
                           Description: "Set gate time for frequency measurement",
                           Category: Command_Category.Trigger,
                           Parameters: "seconds: 1E-3 to 1000|MIN|MAX",
                           Query_Form: "FREQ:ARM:STOP:TIM?",
                           Default_Value: "0.1",
                           Example: "FREQ:ARM:STOP:TIM 1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== Math / Statistics Commands =====
        new Command_Entry( Command: "CALC:MATH:EXPR",
                           Syntax: "CALCulate:MATH:EXPRession <expression>",
                           Description: "Set math expression applied to measurements",
                           Category: Command_Category.Math,
                           Parameters: "expression: quoted string e.g. \"(MEAS1)\" or \"(MEAS1)*1E6\"",
                           Query_Form: "CALC:MATH:EXPR?",
                           Default_Value: "(MEAS1)",
                           Example: "CALC:MATH:EXPR \"(MEAS1)*1E6\"",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:MATH:STAT",
                           Syntax: "CALCulate:MATH:STATe <mode>",
                           Description: "Enable or disable math expression",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON|OFF",
                           Query_Form: "CALC:MATH:STAT?",
                           Default_Value: "OFF",
                           Example: "CALC:MATH:STAT ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC2:FORM",
                           Syntax: "CALCulate2:FORMat <format>",
                           Description: "Select statistics function",
                           Category: Command_Category.Math,
                           Parameters: "format: MEAN|SDEViation|MAX|MIN|PEAK|ALLAN",
                           Query_Form: "CALC2:FORM?",
                           Default_Value: "MEAN",
                           Example: "CALC2:FORM MEAN",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC2:STAT",
                           Syntax: "CALCulate2:STATe <mode>",
                           Description: "Enable or disable statistics",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON|OFF",
                           Query_Form: "CALC2:STAT?",
                           Default_Value: "OFF",
                           Example: "CALC2:STAT ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC2:IMM",
                           Syntax: "CALCulate2:IMMediate",
                           Description: "Perform the selected statistics calculation immediately",
                           Category: Command_Category.Math,
                           Parameters: "None",
                           Query_Form: "CALC2:IMM?",
                           Default_Value: "N/A",
                           Example: "CALC2:IMM",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "CALC3:FORM",
                           Syntax: "CALCulate3:FORMat <format>",
                           Description: "Select limit check format",
                           Category: Command_Category.Math,
                           Parameters: "format: LLIM|ULIM",
                           Query_Form: "CALC3:FORM?",
                           Default_Value: "LLIM",
                           Example: "CALC3:FORM LLIM",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC3:LIM:LOW",
                           Syntax: "CALCulate3:LIMit:LOWer <value>",
                           Description: "Set lower limit for limit checking",
                           Category: Command_Category.Math,
                           Parameters: "value: numeric|MIN|MAX",
                           Query_Form: "CALC3:LIM:LOW?",
                           Default_Value: "0",
                           Example: "CALC3:LIM:LOW 9.99E6",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC3:LIM:UPP",
                           Syntax: "CALCulate3:LIMit:UPPer <value>",
                           Description: "Set upper limit for limit checking",
                           Category: Command_Category.Math,
                           Parameters: "value: numeric|MIN|MAX",
                           Query_Form: "CALC3:LIM:UPP?",
                           Default_Value: "0",
                           Example: "CALC3:LIM:UPP 10.01E6",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC3:STAT",
                           Syntax: "CALCulate3:STATe <mode>",
                           Description: "Enable or disable limit checking",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON|OFF",
                           Query_Form: "CALC3:STAT?",
                           Default_Value: "OFF",
                           Example: "CALC3:STAT ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== System Commands =====
        new Command_Entry( Command: "*IDN?",
                           Syntax: "*IDN?",
                           Description: "Query instrument identification string",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "*IDN?",
                           Default_Value: "N/A",
                           Example: "*IDN?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "*RST",
                           Syntax: "*RST",
                           Description: "Reset instrument to factory default state",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*RST",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "*CLS",
                           Syntax: "*CLS",
                           Description: "Clear status registers and error queue",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "*CLS",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "*OPC?",
                           Syntax: "*OPC?",
                           Description: ( "Query operation complete (returns 1 when all pending operations " +
                                          "finish)" ),
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "*OPC?",
                           Default_Value: "N/A",
                           Example: "*OPC?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "*OPC",
                           Syntax: "*OPC",
                           Description: ( "Set OPC bit in Standard Event register when all pending " + "ope" +
                                                                                                       "rat" +
                                                                                                       "ion" +
                                                                                                       "s " +
                                                                                                       "com" +
                                                                                                       "ple" +
                                                                                                       "te" ),
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*OPC",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "*TST?",
                           Syntax: "*TST?",
                           Description: "Perform self-test and return result (0 = pass)",
                           Category: Command_Category.System,
                           Parameters: "None (returns 0 for pass, non-zero for fail)",
                           Query_Form: "*TST?",
                           Default_Value: "N/A",
                           Example: "*TST?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "SYST:ERR?",
                           Syntax: "SYSTem:ERRor?",
                           Description: "Query and clear one error from the error queue",
                           Category: Command_Category.System,
                           Parameters: "None (returns error number and message)",
                           Query_Form: "SYST:ERR?",
                           Default_Value: "N/A",
                           Example: "SYST:ERR?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "SYST:REM",
                           Syntax: "SYSTem:REMote",
                           Description: "Set instrument to remote mode",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "SYST:REM",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "SYST:LOC",
                           Syntax: "SYSTem:LOCal",
                           Description: "Return instrument to local (front panel) control",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "SYST:LOC",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "SYST:VERS?",
                           Syntax: "SYSTem:VERSion?",
                           Description: "Query SCPI version supported by instrument",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "SYST:VERS?",
                           Default_Value: "N/A",
                           Example: "SYST:VERS?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "*STB?",
                           Syntax: "*STB?",
                           Description: "Query status byte register",
                           Category: Command_Category.System,
                           Parameters: "None (returns decimal value of status byte)",
                           Query_Form: "*STB?",
                           Default_Value: "N/A",
                           Example: "*STB?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "*SRE",
                           Syntax: "*SRE <value>",
                           Description: "Set Service Request Enable register",
                           Category: Command_Category.System,
                           Parameters: "value: 0-255 (bit mask)",
                           Query_Form: "*SRE?",
                           Default_Value: "0",
                           Example: "*SRE 32",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "*ESE",
                           Syntax: "*ESE <value>",
                           Description: "Set Standard Event Status Enable register",
                           Category: Command_Category.System,
                           Parameters: "value: 0-255 (bit mask)",
                           Query_Form: "*ESE?",
                           Default_Value: "0",
                           Example: "*ESE 1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "*ESR?",
                           Syntax: "*ESR?",
                           Description: "Query and clear Standard Event Status register",
                           Category: Command_Category.System,
                           Parameters: "None (returns decimal value of register)",
                           Query_Form: "*ESR?",
                           Default_Value: "N/A",
                           Example: "*ESR?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "*SAV",
                           Syntax: "*SAV <register>",
                           Description: "Save current instrument state to memory register",
                           Category: Command_Category.System,
                           Parameters: "register: 0 to 9",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*SAV 1",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "*RCL",
                           Syntax: "*RCL <register>",
                           Description: "Recall instrument state from memory register",
                           Category: Command_Category.System,
                           Parameters: "register: 0 to 9",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "*RCL 1",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        // ===== IO / Display Commands =====
        new Command_Entry( Command: "DISP",
                           Syntax: "DISPlay <mode>",
                           Description: "Enable or disable front panel display",
                           Category: Command_Category.IO,
                           Parameters: "mode: ON|OFF",
                           Query_Form: "DISP?",
                           Default_Value: "ON",
                           Example: "DISP ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DISP:TEXT",
                           Syntax: "DISPlay:TEXT <string>",
                           Description: "Display a text message on the front panel",
                           Category: Command_Category.IO,
                           Parameters: "string: up to 12 characters in quotes",
                           Query_Form: "DISP:TEXT?",
                           Default_Value: "N/A",
                           Example: "DISP:TEXT \"COUNTING\"",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DISP:TEXT:CLE",
                           Syntax: "DISPlay:TEXT:CLEar",
                           Description: "Clear the displayed text message",
                           Category: Command_Category.IO,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "DISP:TEXT:CLE",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "FORM",
                           Syntax: "FORMat:DATA <type>",
                           Description: "Set data output format",
                           Category: Command_Category.IO,
                           Parameters: "type: ASCii|REAL,32|REAL,64",
                           Query_Form: "FORM?",
                           Default_Value: "ASCii",
                           Example: "FORM ASC",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "HCOP:SDUMP:DATA?",
                           Syntax: "HCOPy:SDUMp:DATA?",
                           Description: "Return screen dump data",
                           Category: Command_Category.IO,
                           Parameters: "None",
                           Query_Form: "HCOP:SDUMP:DATA?",
                           Default_Value: "N/A",
                           Example: "HCOP:SDUMP:DATA?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        // ===== Memory / Data Commands =====
        new Command_Entry( Command: "DATA:POIN?",
                           Syntax: "DATA:POINts?",
                           Description: "Query number of readings stored in internal memory",
                           Category: Command_Category.Memory,
                           Parameters: "None",
                           Query_Form: "DATA:POIN?",
                           Default_Value: "N/A",
                           Example: "DATA:POIN?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "DATA:FEED",
                           Syntax: "DATA:FEED <destination>",
                           Description: "Select reading memory destination",
                           Category: Command_Category.Memory,
                           Parameters: ( "destination: RDG_STORE,\"\" (disable) | RDG_STORE,\"CALC\" " +
                                         "(enable)" ),
                           Query_Form: "DATA:FEED?",
                           Default_Value: "Disabled",
                           Example: "DATA:FEED RDG_STORE,\"CALC2\"",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "DATA:REM?",
                           Syntax: "DATA:REMove? <count>",
                           Description: "Remove and return specified number of readings from memory",
                           Category: Command_Category.Memory,
                           Parameters: "count: 1 to DATA:POIN?|MIN|MAX|INFinity",
                           Query_Form: "DATA:REM?",
                           Default_Value: "N/A",
                           Example: "DATA:REM? 10",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),
      };

      Commands.Sort( ( A, B ) => string.Compare( A.Command, B.Command, StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
    public class Test_Profile : IInstrument_Test_Profile
    {
      public string              Reset_Command   => "*RST";
      public string              Error_Query     => "SYST:ERR?";
      public bool                Has_Error_Queue => true;

      public List<Command_Entry> Get_Commands() => HP53132_Command_Dictionary_Class.Get_All_Commands();

      public IEnumerable<Command_Test_Result> Run_Sequences( Func<string, string> Query, Action<string> Send )
      {
        foreach ( var R in Test_Frequency_Sequence( Query, Send ) )
          yield return R;
        foreach ( var R in Test_Statistics_Sequence( Query, Send ) )
          yield return R;
      }

      private static IEnumerable<Command_Test_Result> Test_Frequency_Sequence( Func<string, string> Query,
                                                                               Action<string>       Send )
      {
        var                 Seq_Cmd = new Command_Entry( Command: "CONF:FREQ → READ? [sequence]",
                                                         Syntax: "CONF:FREQ → FREQ:ARM:STOP:TIM 0.1 → READ?",
                                                         Description: "Sequenced frequency measurement test",
                                                         Category: Command_Category.Measurement,
                                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;
        try
        {
          Send( "CONF:FREQ" );
          Send( "FREQ:ARM:STOP:TIM 0.1" );
          string Reading = Query( "READ?" );
          bool   OK      = double.TryParse( Reading.Trim(),
                                            System.Globalization.NumberStyles.Float,
                                            System.Globalization.CultureInfo.InvariantCulture,
                                            out _ );
          Result         = OK ? Command_Test_Result.Pass( Seq_Cmd, Reading.Trim() )
                              : Command_Test_Result.Fail( Seq_Cmd, $"Non-numeric response: {Reading}" );
        }
        catch ( Exception Ex )
        {
          Result = Command_Test_Result.Fail( Seq_Cmd, Ex.Message );
        }

        yield return Result;
      }

      private static IEnumerable<Command_Test_Result> Test_Statistics_Sequence( Func<string, string> Query,
                                                                                Action<string>       Send )
      {
        var Seq_Cmd = new Command_Entry( Command: "CALC2 [sequence]",
                                         Syntax: ( "CONF:FREQ → CALC2:FORM MEAN → CALC2:STAT ON → 5x READ? " +
                                                   "→ CALC2:IMM?" ),
                                         Description: "Sequenced statistics mean test",
                                         Category: Command_Category.Math,
                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;
        try
        {
          Send( "CONF:FREQ" );
          Send( "FREQ:ARM:STOP:TIM 0.1" );
          Send( "CALC2:FORM MEAN" );
          Send( "CALC2:STAT ON" );
          for ( int I = 0; I < 5; I++ )
            try
            {
              Query( "READ?" );
            }
            catch
            {
            }

          string Mean = Query( "CALC2:IMM?" );
          bool   OK   = double.TryParse( Mean.Trim(),
                                         System.Globalization.NumberStyles.Float,
                                         System.Globalization.CultureInfo.InvariantCulture,
                                         out _ );
          Result      = OK ? Command_Test_Result.Pass( Seq_Cmd, $"Mean={Mean.Trim()}" )
                           : Command_Test_Result.Fail( Seq_Cmd, $"Non-numeric CALC2:IMM? response: {Mean}" );
        }
        catch ( Exception Ex )
        {
          Result = Command_Test_Result.Fail( Seq_Cmd, Ex.Message );
        }
        finally
        {
          try
          {
            Send( "CALC2:STAT OFF" );
          }
          catch
          {
          }
        }

        yield return Result;
      }
    }
  }
}
