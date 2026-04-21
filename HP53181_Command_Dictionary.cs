
// =============================================================================
// FILE:     HP53181_Command_Dictionary_Class.cs
// PROJECT:  Multimeter_Controller
// =============================================================================
//
// DESCRIPTION:
//   Static command dictionary for the HP / Agilent 53181A 225 MHz frequency
//   counter. Provides a structured, searchable registry of all supported SCPI
//   instrument commands, organized by functional category. Each entry captures
//   the full command syntax, description, valid parameter ranges, query form,
//   factory default value, and a usage example.
//
//   This class is the single source of truth for 53181A command metadata within
//   the Multimeter_Controller namespace. It is designed to support command
//   validation, UI population, documentation generation, and runtime lookup.
//
// -----------------------------------------------------------------------------
// INSTRUMENT:
//   HP / Agilent 53181A Frequency Counter
//   Command Set:  SCPI (Standard Commands for Programmable Instruments)
//   Interface:    GPIB (IEEE-488.2)
//   Channel 1:    225 MHz, 50 Ohm or 1 MOhm input
//   Channel 2:    Option 010 — 6 GHz microwave input (single-channel)
//
// -----------------------------------------------------------------------------
// RELATIONSHIP TO HP 53132A:
//   The 53181A is a single-channel, lower-cost sibling of the 53132A.
//   Key differences:
//     - Single channel (no Channel 2 for ratio/heterodyne measurements
//       unless Option 010 microwave channel is fitted)
//     - Maximum gate time: 1 second (vs 1000 seconds on 53132A)
//     - No time interval or pulse-width measurement functions
//     - Same SCPI command set structure — most CONFigure and SENSe
//       commands are identical
//     - Maximum reading resolution: ~10 digits at 1 s gate time
//
// -----------------------------------------------------------------------------
// COMMAND SET OVERVIEW:
//   MEAS:FREQ?    One-shot frequency measurement — configures, triggers,
//                 returns one reading. Resets all settings to defaults.
//   MEAS:FREQ:RAT? One-shot frequency ratio (CH1/CH2) — requires Option 010.
//   CONF:FREQ     Configure frequency measurement without triggering.
//   READ?         Trigger and return reading using current configuration.
//   FETCH?        Return last reading without re-triggering.
//   INIT          Arm the trigger system (move from idle to armed state).
//   ABOR          Abort measurement and return to idle.
//
// -----------------------------------------------------------------------------
// COMMAND CATEGORIES:
//
//   Measurement   — One-shot measurement queries (MEAS:FREQ?, MEAS:FREQ:RAT?,
//                   MEAS:PER?, MEAS:TOT?) plus READ? and FETCH?.
//
//   Configuration — Function setup (CONF:FREQ, CONF:PER, CONF:TOT) and
//                   per-function parameters: gate time (SENS:FREQ:GATE:TIME),
//                   input coupling (INP:COUP), input impedance (INP:IMP),
//                   input slope (INP:SLOP), trigger level (INP:LEV),
//                   sensitivity (INP:SENS), noise rejection (INP:NREJ),
//                   channel (SENS:FUNC:CONC), and format (FORM).
//
//   Trigger       — Trigger source (TRIG:SOUR), trigger count (TRIG:COUN),
//                   sample count (SAMP:COUN), initiate (INIT), abort (ABOR),
//                   and bus trigger (*TRG).
//
//   Math          — Post-acquisition math (CALC:MATH:EXPR), scale/offset
//                   (CALC:SCAL:GAIN / CALC:SCAL:OFFS / CALC:SCAL:STAT),
//                   limit testing (CALC:LIM:LOW / CALC:LIM:UPP /
//                   CALC:LIM:STAT), statistics (CALC:AVER:TYPE /
//                   CALC:AVER:STAT), and statistics queries
//                   (CALC:AVER:MIN? / MAX? / MEAN? / SDEV? / COUN?).
//
//   System        — *IDN?, *RST, *CLS, *OPC, *OPC?, *TST?, SYST:ERR?,
//                   SYST:VERS?, *STB?, *SRE, *ESE, *ESR?.
//
//   IO            — Display (DISP / DISP:TEXT / DISP:TEXT:CLE),
//                   beeper (SYST:BEEP), data format (FORM).
//
//   Memory        — *SAV, *RCL, DATA:FEED, DATA:POINts?.
//
//   Calibration   — CAL:COUN?, CAL:SEC:STAT, CAL:STR.
//
// -----------------------------------------------------------------------------
// GATE TIME NOTES:
//   Gate time directly controls measurement resolution and speed.
//   At 1 s gate time the 53181A can resolve approximately 10 digits of
//   frequency. Shorter gate times trade resolution for speed:
//     Gate = 1 s   → ~10 digit resolution, 1 reading/sec
//     Gate = 0.1 s → ~9 digit resolution,  ~10 readings/sec
//     Gate = 0.01s → ~8 digit resolution,  ~100 readings/sec (approx)
//   Set gate time with: SENS:FREQ:GATE:TIME <seconds>
//
// -----------------------------------------------------------------------------
// INPUT CHANNEL NOTES:
//   Channel 1 accepts signals from DC to 225 MHz.
//   Input impedance is selectable: 50 Ohm or 1 MOhm.
//   Trigger level is set in volts with INP:LEV <volts>.
//   Coupling is AC or DC with INP:COUP AC|DC.
//   For microwave inputs (Option 010), use channel 2 commands (INP2:xxx).
//
// -----------------------------------------------------------------------------
// DEPENDENCIES:
//   System.Collections.Generic    List<T>
//   System.StringComparison       OrdinalIgnoreCase
//   Command_Entry                 Data record defined in Command_Dictionary_Class.cs
//   Command_Category              Enum defined in Command_Dictionary_Class.cs
//
// -----------------------------------------------------------------------------
// MAINTENANCE:
//   To add a command: append a new Command_Entry to the list in
//   Get_All_Commands(). The list is re-sorted alphabetically on every call,
//   so insertion order does not matter.
//
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  public static class HP53181_Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands()
    {
      var Commands = new List<Command_Entry> {

        // ===== Measurement Commands =====
        new Command_Entry( Command: "MEAS:FREQ?",
                           Syntax: "MEASure:FREQuency? [<expected>[,<resolution>]]",
                           Description: ( "One-shot frequency measurement — configure, trigger, and return " +
                                          "reading" ),
                           Category: Command_Category.Measurement,
                           Parameters: ( "expected: expected frequency in Hz | MIN | MAX | DEF, " +
                                         "resolution: MIN | MAX | DEF" ),
                           Query_Form: "MEAS:FREQ?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:FREQ?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:FREQ:RAT?",
                           Syntax: "MEASure:FREQuency:RATio?",
                           Description: ( "One-shot CH1/CH2 frequency ratio measurement (requires Option " +
                                          "010)" ),
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "MEAS:FREQ:RAT?",
                           Default_Value: "N/A",
                           Example: "MEAS:FREQ:RAT?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:PER?",
                           Syntax: "MEASure:PERiod? [<expected>[,<resolution>]]",
                           Description: ( "One-shot period measurement — configure, trigger, and return " +
                                          "reading" ),
                           Category: Command_Category.Measurement,
                           Parameters: "expected: expected period in seconds | MIN | MAX | DEF",
                           Query_Form: "MEAS:PER?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:PER?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:TOT?",
                           Syntax: "MEASure:TOTalize?",
                           Description: "One-shot totalize measurement — count events on CH1",
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "MEAS:TOT?",
                           Default_Value: "N/A",
                           Example: "MEAS:TOT?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "READ?",
                           Syntax: "READ?",
                           Description: ( "Initiate measurement and return reading using current " +
                                          "configuration" ),
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "READ?",
                           Default_Value: "N/A",
                           Example: "READ?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "FETCH?",
                           Syntax: "FETCH?",
                           Description: "Return last completed reading without re-triggering",
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "FETCH?",
                           Default_Value: "N/A",
                           Example: "FETCH?",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        // ===== Configuration Commands =====
        new Command_Entry( Command: "CONF:FREQ",
                           Syntax: "CONFigure:FREQuency [<expected>[,<resolution>]]",
                           Description: "Configure frequency measurement without triggering",
                           Category: Command_Category.Configuration,
                           Parameters: "expected: expected frequency in Hz | MIN | MAX | DEF",
                           Query_Form: "CONF:FREQ?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:FREQ 10e6",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:FREQ:RAT",
                           Syntax: "CONFigure:FREQuency:RATio",
                           Description: ( "Configure CH1/CH2 frequency ratio measurement (requires Option " +
                                          "010)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "None",
                           Query_Form: "CONF:FREQ:RAT?",
                           Default_Value: "N/A",
                           Example: "CONF:FREQ:RAT",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:PER",
                           Syntax: "CONFigure:PERiod [<expected>[,<resolution>]]",
                           Description: "Configure period measurement without triggering",
                           Category: Command_Category.Configuration,
                           Parameters: "expected: expected period in seconds | MIN | MAX | DEF",
                           Query_Form: "CONF:PER?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:PER 100e-9",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:TOT",
                           Syntax: "CONFigure:TOTalize",
                           Description: "Configure totalize (event count) measurement without triggering",
                           Category: Command_Category.Configuration,
                           Parameters: "None",
                           Query_Form: "CONF:TOT?",
                           Default_Value: "N/A",
                           Example: "CONF:TOT",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SENS:FREQ:GATE:TIME",
                           Syntax: "SENSe:FREQuency:GATE:TIME <seconds>",
                           Description: "Set frequency measurement gate time",
                           Category: Command_Category.Configuration,
                           Parameters: "seconds: 1e-3 to 1 | MIN | MAX (longer gate = more resolution)",
                           Query_Form: "SENS:FREQ:GATE:TIME?",
                           Default_Value: "0.1",
                           Example: "SENS:FREQ:GATE:TIME 1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SENS:FREQ:GATE:SOUR",
                           Syntax: "SENSe:FREQuency:GATE:SOURce <source>",
                           Description: "Select gate time source — internal timer or external signal",
                           Category: Command_Category.Configuration,
                           Parameters: "source: TIME | EXT",
                           Query_Form: "SENS:FREQ:GATE:SOUR?",
                           Default_Value: "TIME",
                           Example: "SENS:FREQ:GATE:SOUR TIME",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:COUP",
                           Syntax: "INPut:COUPling <coupling>",
                           Description: "Set Channel 1 input coupling",
                           Category: Command_Category.Configuration,
                           Parameters: "coupling: AC | DC",
                           Query_Form: "INP:COUP?",
                           Default_Value: "AC",
                           Example: "INP:COUP DC",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:IMP",
                           Syntax: "INPut:IMPedance <impedance>",
                           Description: "Set Channel 1 input impedance",
                           Category: Command_Category.Configuration,
                           Parameters: "impedance: 50 | 1e6 (50 Ohm or 1 MOhm)",
                           Query_Form: "INP:IMP?",
                           Default_Value: "1e6",
                           Example: "INP:IMP 50",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:LEV",
                           Syntax: "INPut:LEVel <volts>",
                           Description: "Set Channel 1 trigger level voltage",
                           Category: Command_Category.Configuration,
                           Parameters: "volts: -5.125 to +5.125 | MIN | MAX",
                           Query_Form: "INP:LEV?",
                           Default_Value: "0",
                           Example: "INP:LEV 0.5",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:LEV:AUTO",
                           Syntax: "INPut:LEVel:AUTO <mode>",
                           Description: "Enable or disable automatic trigger level (50% of input signal)",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "INP:LEV:AUTO?",
                           Default_Value: "ON",
                           Example: "INP:LEV:AUTO ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:SLOP",
                           Syntax: "INPut:SLOPe <slope>",
                           Description: "Set Channel 1 trigger slope",
                           Category: Command_Category.Configuration,
                           Parameters: "slope: POSitive | NEGative",
                           Query_Form: "INP:SLOP?",
                           Default_Value: "POSitive",
                           Example: "INP:SLOP POS",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:NREJ",
                           Syntax: "INPut:NREJect <mode>",
                           Description: "Enable or disable noise rejection on Channel 1",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "INP:NREJ?",
                           Default_Value: "OFF",
                           Example: "INP:NREJ ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:SENS",
                           Syntax: "INPut:SENSitivity <sensitivity>",
                           Description: "Set Channel 1 input sensitivity",
                           Category: Command_Category.Configuration,
                           Parameters: "sensitivity: LOW | MED | HIGH",
                           Query_Form: "INP:SENS?",
                           Default_Value: "MED",
                           Example: "INP:SENS HIGH",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP2:COUP",
                           Syntax: "INPut2:COUPling <coupling>",
                           Description: "Set Channel 2 (Option 010 microwave) input coupling",
                           Category: Command_Category.Configuration,
                           Parameters: "coupling: AC | DC",
                           Query_Form: "INP2:COUP?",
                           Default_Value: "AC",
                           Example: "INP2:COUP DC",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP2:LEV",
                           Syntax: "INPut2:LEVel <volts>",
                           Description: "Set Channel 2 (Option 010 microwave) trigger level",
                           Category: Command_Category.Configuration,
                           Parameters: "volts: -5.125 to +5.125 | MIN | MAX",
                           Query_Form: "INP2:LEV?",
                           Default_Value: "0",
                           Example: "INP2:LEV 0.0",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "FORM",
                           Syntax: "FORMat:DATA <type>",
                           Description: "Set output data format",
                           Category: Command_Category.Configuration,
                           Parameters: "type: ASCii | REAL,32 | REAL,64",
                           Query_Form: "FORM?",
                           Default_Value: "ASCii",
                           Example: "FORM ASC",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== Trigger Commands =====
        new Command_Entry( Command: "TRIG:SOUR",
                           Syntax: "TRIGger:SOURce <source>",
                           Description: "Select trigger source",
                           Category: Command_Category.Trigger,
                           Parameters: "source: IMMediate | BUS | EXTernal",
                           Query_Form: "TRIG:SOUR?",
                           Default_Value: "IMMediate",
                           Example: "TRIG:SOUR IMM",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "TRIG:COUN",
                           Syntax: "TRIGger:COUNt <count>",
                           Description: "Set number of measurements per INIT command",
                           Category: Command_Category.Trigger,
                           Parameters: "count: 1 to 1e9 | MIN | MAX | INFinity",
                           Query_Form: "TRIG:COUN?",
                           Default_Value: "1",
                           Example: "TRIG:COUN 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SAMP:COUN",
                           Syntax: "SAMPle:COUNt <count>",
                           Description: "Set number of readings returned per trigger event",
                           Category: Command_Category.Trigger,
                           Parameters: "count: 1 | MIN | MAX",
                           Query_Form: "SAMP:COUN?",
                           Default_Value: "1",
                           Example: "SAMP:COUN 1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INIT",
                           Syntax: "INITiate",
                           Description: "Move trigger system from idle to armed/waiting state",
                           Category: Command_Category.Trigger,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "INIT",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "ABOR",
                           Syntax: "ABORt",
                           Description: "Abort current measurement and return trigger system to idle",
                           Category: Command_Category.Trigger,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "ABOR",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "*TRG",
                           Syntax: "*TRG",
                           Description: "Send a bus trigger (TRIG:SOUR must be BUS)",
                           Category: Command_Category.Trigger,
                           Parameters: "None (requires TRIG:SOUR BUS)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*TRG",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        // ===== Math Commands =====
        new Command_Entry( Command: "CALC:MATH:EXPR",
                           Syntax: "CALCulate:MATH:EXPRession <expression>",
                           Description: "Set post-acquisition math expression applied to each reading",
                           Category: Command_Category.Math,
                           Parameters: "expression: FEED1 | (FEED1*<value>) | (FEED1+<value>) etc.",
                           Query_Form: "CALC:MATH:EXPR?",
                           Default_Value: "FEED1",
                           Example: "CALC:MATH:EXPR (FEED1*1000)",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:MATH:STAT",
                           Syntax: "CALCulate:MATH:STATe <mode>",
                           Description: "Enable or disable post-acquisition math",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "CALC:MATH:STAT?",
                           Default_Value: "OFF",
                           Example: "CALC:MATH:STAT ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:SCAL:GAIN",
                           Syntax: "CALCulate:SCALe:GAIN <value>",
                           Description: "Set scale gain factor applied to each reading",
                           Category: Command_Category.Math,
                           Parameters: "value: any numeric (multiplier)",
                           Query_Form: "CALC:SCAL:GAIN?",
                           Default_Value: "1",
                           Example: "CALC:SCAL:GAIN 1000",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:SCAL:OFFS",
                           Syntax: "CALCulate:SCALe:OFFSet <value>",
                           Description: "Set scale offset added to each reading after gain",
                           Category: Command_Category.Math,
                           Parameters: "value: any numeric (offset)",
                           Query_Form: "CALC:SCAL:OFFS?",
                           Default_Value: "0",
                           Example: "CALC:SCAL:OFFS 0.0",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:SCAL:STAT",
                           Syntax: "CALCulate:SCALe:STATe <mode>",
                           Description: "Enable or disable scale (gain/offset) math",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "CALC:SCAL:STAT?",
                           Default_Value: "OFF",
                           Example: "CALC:SCAL:STAT ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:LIM:LOW",
                           Syntax: "CALCulate:LIMit:LOWer <value>",
                           Description: "Set lower limit for limit testing",
                           Category: Command_Category.Math,
                           Parameters: "value: any numeric in current units",
                           Query_Form: "CALC:LIM:LOW?",
                           Default_Value: "0",
                           Example: "CALC:LIM:LOW 9.999e6",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:LIM:UPP",
                           Syntax: "CALCulate:LIMit:UPPer <value>",
                           Description: "Set upper limit for limit testing",
                           Category: Command_Category.Math,
                           Parameters: "value: any numeric in current units",
                           Query_Form: "CALC:LIM:UPP?",
                           Default_Value: "0",
                           Example: "CALC:LIM:UPP 10.001e6",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:LIM:STAT",
                           Syntax: "CALCulate:LIMit:STATe <mode>",
                           Description: "Enable or disable limit testing",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "CALC:LIM:STAT?",
                           Default_Value: "OFF",
                           Example: "CALC:LIM:STAT ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:LIM:FAIL?",
                           Syntax: "CALCulate:LIMit:FAIL?",
                           Description: "Query limit test result (0 = pass, 1 = fail)",
                           Category: Command_Category.Math,
                           Parameters: "None (read-only)",
                           Query_Form: "CALC:LIM:FAIL?",
                           Default_Value: "N/A",
                           Example: "CALC:LIM:FAIL?",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "CALC:AVER:TYPE",
                           Syntax: "CALCulate:AVERage:TYPE <type>",
                           Description: "Select statistics accumulation type",
                           Category: Command_Category.Math,
                           Parameters: "type: MEANonly | ALL",
                           Query_Form: "CALC:AVER:TYPE?",
                           Default_Value: "MEANonly",
                           Example: "CALC:AVER:TYPE ALL",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:AVER:STAT",
                           Syntax: "CALCulate:AVERage:STATe <mode>",
                           Description: "Enable or disable statistics accumulation",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "CALC:AVER:STAT?",
                           Default_Value: "OFF",
                           Example: "CALC:AVER:STAT ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:AVER:MIN?",
                           Syntax: "CALCulate:AVERage:MINimum?",
                           Description: "Query minimum reading from statistics register",
                           Category: Command_Category.Math,
                           Parameters: "None (read-only)",
                           Query_Form: "CALC:AVER:MIN?",
                           Default_Value: "N/A",
                           Example: "CALC:AVER:MIN?",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "CALC:AVER:MAX?",
                           Syntax: "CALCulate:AVERage:MAXimum?",
                           Description: "Query maximum reading from statistics register",
                           Category: Command_Category.Math,
                           Parameters: "None (read-only)",
                           Query_Form: "CALC:AVER:MAX?",
                           Default_Value: "N/A",
                           Example: "CALC:AVER:MAX?",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "CALC:AVER:MEAN?",
                           Syntax: "CALCulate:AVERage:MEAN?",
                           Description: "Query mean of readings from statistics register",
                           Category: Command_Category.Math,
                           Parameters: "None (read-only)",
                           Query_Form: "CALC:AVER:MEAN?",
                           Default_Value: "N/A",
                           Example: "CALC:AVER:MEAN?",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "CALC:AVER:SDEV?",
                           Syntax: "CALCulate:AVERage:SDEViation?",
                           Description: "Query standard deviation of readings from statistics register",
                           Category: Command_Category.Math,
                           Parameters: "None (read-only)",
                           Query_Form: "CALC:AVER:SDEV?",
                           Default_Value: "N/A",
                           Example: "CALC:AVER:SDEV?",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "CALC:AVER:COUN?",
                           Syntax: "CALCulate:AVERage:COUNt?",
                           Description: "Query number of readings in statistics register",
                           Category: Command_Category.Math,
                           Parameters: "None (read-only)",
                           Query_Form: "CALC:AVER:COUN?",
                           Default_Value: "N/A",
                           Example: "CALC:AVER:COUN?",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

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
                           Description: ( "Set OPC bit in Standard Event register when all pending " +
                                          "operations complete" ),
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
                           Parameters: "None (returns error number and message string)",
                           Query_Form: "SYST:ERR?",
                           Default_Value: "N/A",
                           Example: "SYST:ERR?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

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
                           Parameters: "value: 0–255 (bit mask)",
                           Query_Form: "*SRE?",
                           Default_Value: "0",
                           Example: "*SRE 32",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "*ESE",
                           Syntax: "*ESE <value>",
                           Description: "Set Standard Event Status Enable register",
                           Category: Command_Category.System,
                           Parameters: "value: 0–255 (bit mask)",
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

        // ===== IO Commands =====
        new Command_Entry( Command: "DISP",
                           Syntax: "DISPlay <mode>",
                           Description: "Enable or disable front panel display",
                           Category: Command_Category.IO,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "DISP?",
                           Default_Value: "ON",
                           Example: "DISP ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DISP:TEXT",
                           Syntax: "DISPlay:TEXT <string>",
                           Description: "Display a text message on the front panel",
                           Category: Command_Category.IO,
                           Parameters: "string: up to 12 characters in double quotes",
                           Query_Form: "DISP:TEXT?",
                           Default_Value: "N/A",
                           Example: "DISP:TEXT \"10 MHZ REF\"",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DISP:TEXT:CLE",
                           Syntax: "DISPlay:TEXT:CLEar",
                           Description: "Clear the front panel text message",
                           Category: Command_Category.IO,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "DISP:TEXT:CLE",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "SYST:BEEP",
                           Syntax: "SYSTem:BEEPer",
                           Description: "Issue a single beep from the front panel",
                           Category: Command_Category.IO,
                           Parameters: "None",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "SYST:BEEP",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        // ===== Memory Commands =====
        new Command_Entry( Command: "DATA:FEED",
                           Syntax: "DATA:FEED <destination>",
                           Description: "Select reading memory destination",
                           Category: Command_Category.Memory,
                           Parameters: ( "destination: RDG_STORE,\"\" (disable) | RDG_STORE,\"CALC\" " +
                                         "(enable)" ),
                           Query_Form: "DATA:FEED?",
                           Default_Value: "Disabled",
                           Example: "DATA:FEED RDG_STORE,\"CALC\"",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "DATA:POINts?",
                           Syntax: "DATA:POINts?",
                           Description: "Query number of readings currently stored in memory",
                           Category: Command_Category.Memory,
                           Parameters: "None (read-only)",
                           Query_Form: "DATA:POINts?",
                           Default_Value: "N/A",
                           Example: "DATA:POINts?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "*SAV",
                           Syntax: "*SAV <register>",
                           Description: "Save current instrument state to memory register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 0 | 1 | 2",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*SAV 1",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "*RCL",
                           Syntax: "*RCL <register>",
                           Description: "Recall instrument state from memory register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 0 | 1 | 2",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "*RCL 1",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        // ===== Calibration Commands =====
        new Command_Entry( Command: "CAL:SEC:STAT",
                           Syntax: "CALibration:SECure:STATe <mode>,<code>",
                           Description: "Enable or disable calibration security",
                           Category: Command_Category.Calibration,
                           Parameters: "mode: ON | OFF, code: security code string",
                           Query_Form: "CAL:SEC:STAT?",
                           Default_Value: "ON",
                           Example: "CAL:SEC:STAT OFF,53181",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "CAL:COUN?",
                           Syntax: "CALibration:COUNt?",
                           Description: "Query the number of times the instrument has been calibrated",
                           Category: Command_Category.Calibration,
                           Parameters: "None (read-only)",
                           Query_Form: "CAL:COUN?",
                           Default_Value: "N/A",
                           Example: "CAL:COUN?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "CAL:STR",
                           Syntax: "CALibration:STRing <message>",
                           Description: "Store a calibration message string",
                           Category: Command_Category.Calibration,
                           Parameters: "message: up to 40 characters in double quotes",
                           Query_Form: "CAL:STR?",
                           Default_Value: "N/A",
                           Example: "CAL:STR \"Cal 2026-01-15\"",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),
      };

      Commands.Sort( ( A, B ) => string.Compare( A.Command, B.Command, StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }

    public class Test_Profile : IInstrument_Test_Profile
    {
      public string              Reset_Command   => "*RST";
      public string              Error_Query     => "SYST:ERR?";
      public bool                Has_Error_Queue => true;

      public List<Command_Entry> Get_Commands() => HP53181_Command_Dictionary_Class.Get_All_Commands();

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
        var Seq_Cmd = new Command_Entry( Command: "CONF:FREQ → READ? [sequence]",
                                         Syntax: "CONF:FREQ → SENS:FREQ:GATE:TIME 0.1 → READ?",
                                         Description: "Sequenced frequency measurement test",
                                         Category: Command_Category.Measurement,
                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;
        try
        {
          Send( "CONF:FREQ" );
          Send( "SENS:FREQ:GATE:TIME 0.1" );
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
        var Seq_Cmd = new Command_Entry( Command: "CALC:AVER [sequence]",
                                         Syntax: ( "CONF:FREQ → CALC:AVER:TYPE ALL → CALC:AVER:STAT ON → " +
                                                   "5x READ? → CALC:AVER:MEAN?" ),
                                         Description: "Sequenced statistics accumulation test",
                                         Category: Command_Category.Math,
                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;
        try
        {
          Send( "CONF:FREQ" );
          Send( "SENS:FREQ:GATE:TIME 0.1" );
          Send( "CALC:AVER:TYPE ALL" );
          Send( "CALC:AVER:STAT ON" );
          for ( int I = 0; I < 5; I++ )
            try
            {
              Query( "READ?" );
            }
            catch
            {
            }

          string Mean  = Query( "CALC:AVER:MEAN?" );
          string Count = Query( "CALC:AVER:COUN?" );
          bool   OK    = double.TryParse( Mean.Trim(),
                                          System.Globalization.NumberStyles.Float,
                                          System.Globalization.CultureInfo.InvariantCulture,
                                          out _ );
          Result = OK ? Command_Test_Result.Pass( Seq_Cmd, $"Mean={Mean.Trim()} Count={Count.Trim()}" )
                      : Command_Test_Result.Fail( Seq_Cmd, $"Non-numeric CALC:AVER:MEAN? response: {Mean}" );
        }
        catch ( Exception Ex )
        {
          Result = Command_Test_Result.Fail( Seq_Cmd, Ex.Message );
        }
        finally
        {
          try
          {
            Send( "CALC:AVER:STAT OFF" );
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
