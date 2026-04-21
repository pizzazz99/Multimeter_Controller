

// =============================================================================
// FILE:     HP34411_Command_Dictionary_Class.cs
// PROJECT:  Multimeter_Controller
// =============================================================================
//
// DESCRIPTION:
//   Static command dictionary for the HP / Agilent 34411A 6.5-digit high-speed
//   multimeter. Provides a structured, searchable registry of all supported SCPI
//   instrument commands, organized by functional category. Each entry captures
//   the full command syntax, description, valid parameter ranges, query form,
//   factory default value, and a usage example.
//
//   This class is the single source of truth for 34411A command metadata within
//   the Multimeter_Controller namespace. It is designed to support command
//   validation, UI population, documentation generation, and runtime lookup.
//
// -----------------------------------------------------------------------------
// INSTRUMENT:
//   HP / Agilent / Keysight 34411A Multimeter
//   Command Set:  SCPI (Standard Commands for Programmable Instruments)
//   Interface:    GPIB (IEEE-488.2) / USB / LAN
//   Resolution:   Up to 6.5 digits
//   Max Speed:    50,000 readings/sec (in burst mode)
//
// -----------------------------------------------------------------------------
// RELATIONSHIP TO HP 34401A:
//   The 34411A is largely command-compatible with the 34401A but adds:
//     - Higher speed burst sampling (SAMP:SOUR TIMer, SAMP:TIM)
//     - Extended NPLC range down to 0.001 PLC
//     - Histogram math (CALC:TRAN:HIST)
//     - Larger internal memory (up to 50,000 readings)
//     - DATA:LAST? for the most recent reading without initiating
//     - SAMP:COUN up to 50,000
//     - SENSe subsystem long-form commands accepted
//
// -----------------------------------------------------------------------------
// COMMAND SET OVERVIEW:
//   This dictionary uses SCPI short-form mnemonics with colon-separated node
//   paths. Both short-form and long-form expansions are accepted by the
//   instrument. This file registers the short-form as the Command field.
//
//   IMPORTANT — MEAS vs CONF vs READ/FETCH distinction:
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
// -----------------------------------------------------------------------------
// COMMAND CATEGORIES:
//
//   Measurement   — One-shot measurement queries (MEAS:VOLT:DC?, MEAS:RES?,
//                   MEAS:FREQ?, MEAS:CONT?, MEAS:DIOD?, etc.) plus READ?,
//                   FETCH?, and DATA:LAST?.
//
//   Configuration — Function setup (CONF:VOLT:DC, CONF:RES, etc.) and
//                   per-function parameter commands: range, autorange, NPLC,
//                   resolution, input impedance, autozero, AC bandwidth,
//                   and aperture time (VOLT:DC:APER).
//
//   Trigger       — Trigger source (TRIG:SOUR), trigger delay (TRIG:DEL /
//                   TRIG:DEL:AUTO), trigger count (TRIG:COUN), sample count
//                   (SAMP:COUN), sample source (SAMP:SOUR), sample timer
//                   (SAMP:TIM), trigger initiation (INIT), and bus trigger (*TRG).
//
//   Math          — Math function selection (CALC:FUNC) and enable (CALC:STAT),
//                   null offset (CALC:NULL:OFFS), dB reference (CALC:DB:REF),
//                   dBm impedance (CALC:DBM:REF), limit bounds
//                   (CALC:LIM:LOW / CALC:LIM:UPP), statistics queries
//                   (CALC:AVER:MIN? / MAX? / AVER? / COUN?), and histogram
//                   configuration (CALC:TRAN:HIST:POIN, CALC:TRAN:HIST:RANG).
//
//   System        — *IDN?, *RST, *CLS, *OPC, *OPC?, *TST?, SYST:ERR?,
//                   SYST:VERS?, *STB?, *SRE, *ESE, *ESR?.
//
//   IO            — Display (DISP / DISP:TEXT / DISP:TEXT:CLE), beeper
//                   (SYST:BEEP / SYST:BEEP:STAT), terminal query (ROUT:TERM?),
//                   data format (FORM).
//
//   Memory        — DATA:POINts?, DATA:LAST?, DATA:FEED, DATA:REM, *SAV, *RCL.
//
//   Calibration   — CAL:SEC:STAT, CAL:COUN?, CAL:STR.
//
// -----------------------------------------------------------------------------
// NOTABLE 34411A-SPECIFIC COMMANDS:
//
//   SAMP:SOUR     Controls whether samples are triggered by the trigger event
//                 (IMMediate) or by an internal timer (TIMer). TIMer mode
//                 enables the highest throughput burst acquisition.
//
//   SAMP:TIM      Sets the sample interval in TIMer mode. Minimum is 20 µs
//                 (50,000 readings/sec). Must set SAMP:SOUR TIM first.
//
//   VOLT:DC:APER  Sets integration time directly in seconds as an alternative
//                 to NPLC. Equivalent to NPLC / line_frequency.
//
//   DATA:LAST?    Returns the most recently completed reading without
//                 re-initiating the trigger system. Useful for continuous
//                 monitoring without disturbing the trigger state machine.
//
//   CALC:TRAN:HIST:POIN  Sets the number of histogram bins (1–100 bins).
//
//   CALC:TRAN:HIST:RANG  Sets the histogram measurement range. AUTO lets
//                        the instrument determine the range dynamically.
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
//   To deprecate a command: add a note to the Description field rather than
//   removing the entry, to preserve backward compatibility.
//
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  public static class HP34411_Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands()
    {
      var Commands = new List<Command_Entry> {

        // ===== Measurement Commands =====
        new Command_Entry( Command: "MEAS:VOLT:DC?",
                           Syntax: "MEASure:VOLTage:DC? [<range>[,<resolution>]]",
                           Description: "Measure DC voltage and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.1|1|10|100|1000|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "MEAS:VOLT:DC?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:VOLT:DC? 10,0.001",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:VOLT:AC?",
                           Syntax: "MEASure:VOLTage:AC? [<range>[,<resolution>]]",
                           Description: "Measure AC voltage (true RMS) and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.1|1|10|100|750|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "MEAS:VOLT:AC?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:VOLT:AC? 10,0.001",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:CURR:DC?",
                           Syntax: "MEASure:CURRent:DC? [<range>[,<resolution>]]",
                           Description: "Measure DC current and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "MEAS:CURR:DC?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:CURR:DC? 1,0.0001",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:CURR:AC?",
                           Syntax: "MEASure:CURRent:AC? [<range>[,<resolution>]]",
                           Description: "Measure AC current (true RMS) and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "MEAS:CURR:AC?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:CURR:AC? 1,0.001",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:RES?",
                           Syntax: "MEASure:RESistance? [<range>[,<resolution>]]",
                           Description: "Measure 2-wire resistance and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
                           Query_Form: "MEAS:RES?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:RES? 1e3,0.1",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:FRES?",
                           Syntax: "MEASure:FRESistance? [<range>[,<resolution>]]",
                           Description: "Measure 4-wire resistance and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
                           Query_Form: "MEAS:FRES?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:FRES? 1e3,0.1",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:FREQ?",
                           Syntax: "MEASure:FREQuency? [<range>[,<resolution>]]",
                           Description: "Measure frequency and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "range: signal voltage range, resolution: MIN|MAX|DEF",
                           Query_Form: "MEAS:FREQ?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:FREQ? 1,MIN",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:PER?",
                           Syntax: "MEASure:PERiod? [<range>[,<resolution>]]",
                           Description: "Measure period and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "range: signal voltage range, resolution: MIN|MAX|DEF",
                           Query_Form: "MEAS:PER?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "MEAS:PER? 1,MIN",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:CONT?",
                           Syntax: "MEASure:CONTinuity?",
                           Description: "Measure continuity (fixed 1 kOhm range) and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "None (fixed range, fixed 5.5-digit resolution)",
                           Query_Form: "MEAS:CONT?",
                           Default_Value: "1 kOhm range",
                           Example: "MEAS:CONT?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "MEAS:DIOD?",
                           Syntax: "MEASure:DIODe?",
                           Description: "Measure diode forward voltage and return reading",
                           Category: Command_Category.Measurement,
                           Parameters: "None (fixed 1 VDC range, 1 mA test current)",
                           Query_Form: "MEAS:DIOD?",
                           Default_Value: "1 VDC range",
                           Example: "MEAS:DIOD?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "READ?",
                           Syntax: "READ?",
                           Description: "Initiate a measurement and return the reading(s)",
                           Category: Command_Category.Measurement,
                           Parameters: "None (uses current configuration)",
                           Query_Form: "READ?",
                           Default_Value: "N/A",
                           Example: "READ?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "FETCH?",
                           Syntax: "FETCH?",
                           Description: "Return last reading(s) without triggering a new measurement",
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "FETCH?",
                           Default_Value: "N/A",
                           Example: "FETCH?",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "DATA:LAST?",
                           Syntax: "DATA:LAST?",
                           Description: ( "Return most recently completed reading without disturbing " +
                                          "trigger state" ),
                           Category: Command_Category.Measurement,
                           Parameters: "None",
                           Query_Form: "DATA:LAST?",
                           Default_Value: "N/A",
                           Example: "DATA:LAST?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        // ===== Configuration Commands =====
        new Command_Entry( Command: "CONF:VOLT:DC",
                           Syntax: "CONFigure:VOLTage:DC [<range>[,<resolution>]]",
                           Description: "Configure DC voltage measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "range: 0.1|1|10|100|1000|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "CONF:VOLT:DC?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:VOLT:DC 10,0.001",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:VOLT:AC",
                           Syntax: "CONFigure:VOLTage:AC [<range>[,<resolution>]]",
                           Description: "Configure AC voltage measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "range: 0.1|1|10|100|750|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "CONF:VOLT:AC?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:VOLT:AC 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:CURR:DC",
                           Syntax: "CONFigure:CURRent:DC [<range>[,<resolution>]]",
                           Description: "Configure DC current measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "CONF:CURR:DC?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:CURR:DC 1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:CURR:AC",
                           Syntax: "CONFigure:CURRent:AC [<range>[,<resolution>]]",
                           Description: "Configure AC current measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
                           Query_Form: "CONF:CURR:AC?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:CURR:AC 1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:RES",
                           Syntax: "CONFigure:RESistance [<range>[,<resolution>]]",
                           Description: "Configure 2-wire resistance measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
                           Query_Form: "CONF:RES?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:RES 1e3",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:FRES",
                           Syntax: "CONFigure:FRESistance [<range>[,<resolution>]]",
                           Description: "Configure 4-wire resistance measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
                           Query_Form: "CONF:FRES?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:FRES 1e3",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:FREQ",
                           Syntax: "CONFigure:FREQuency [<range>[,<resolution>]]",
                           Description: "Configure frequency measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "range: signal voltage range, resolution: MIN|MAX|DEF",
                           Query_Form: "CONF:FREQ?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:FREQ 1,MIN",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:PER",
                           Syntax: "CONFigure:PERiod [<range>[,<resolution>]]",
                           Description: "Configure period measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "range: signal voltage range, resolution: MIN|MAX|DEF",
                           Query_Form: "CONF:PER?",
                           Default_Value: "AUTO range, default resolution",
                           Example: "CONF:PER 1,MIN",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:CONT",
                           Syntax: "CONFigure:CONTinuity",
                           Description: "Configure continuity measurement (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "None (fixed 1 kOhm range)",
                           Query_Form: "CONF:CONT?",
                           Default_Value: "1 kOhm range",
                           Example: "CONF:CONT",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CONF:DIOD",
                           Syntax: "CONFigure:DIODe",
                           Description: "Configure diode test (does not trigger)",
                           Category: Command_Category.Configuration,
                           Parameters: "None (fixed 1 VDC range, 1 mA test current)",
                           Query_Form: "CONF:DIOD?",
                           Default_Value: "1 VDC range",
                           Example: "CONF:DIOD",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "VOLT:DC:RANG",
                           Syntax: "VOLTage:DC:RANGe <range>",
                           Description: "Set DC voltage measurement range",
                           Category: Command_Category.Configuration,
                           Parameters: "range: 0.1|1|10|100|1000|MIN|MAX",
                           Query_Form: "VOLT:DC:RANG?",
                           Default_Value: "AUTO",
                           Example: "VOLT:DC:RANG 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "VOLT:DC:RANG:AUTO",
                           Syntax: "VOLTage:DC:RANGe:AUTO <mode>",
                           Description: "Enable or disable DC voltage autoranging",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON|OFF|ONCE",
                           Query_Form: "VOLT:DC:RANG:AUTO?",
                           Default_Value: "ON",
                           Example: "VOLT:DC:RANG:AUTO ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "VOLT:DC:NPLC",
                           Syntax: "VOLTage:DC:NPLCycles <nplc>",
                           Description: "Set DC voltage integration time in power line cycles",
                           Category: Command_Category.Configuration,
                           Parameters: "nplc: 0.001|0.006|0.02|0.06|0.2|1|2|10|100|MIN|MAX",
                           Query_Form: "VOLT:DC:NPLC?",
                           Default_Value: "1",
                           Example: "VOLT:DC:NPLC 1",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "VOLT:DC:APER",
                           Syntax: "VOLTage:DC:APERture <seconds>",
                           Description: ( "Set DC voltage integration time in seconds (alternative to " +
                                          "NPLC)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "seconds: 166e-6 to 1.667 | MIN | MAX",
                           Query_Form: "VOLT:DC:APER?",
                           Default_Value: "16.67e-3 (1 PLC at 60 Hz)",
                           Example: "VOLT:DC:APER 0.001",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "VOLT:DC:RES",
                           Syntax: "VOLTage:DC:RESolution <resolution>",
                           Description: "Set DC voltage measurement resolution",
                           Category: Command_Category.Configuration,
                           Parameters: "resolution: in volts | MIN | MAX",
                           Query_Form: "VOLT:DC:RES?",
                           Default_Value: "Depends on range and NPLC",
                           Example: "VOLT:DC:RES 0.0001",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INP:IMP:AUTO",
                           Syntax: "INPut:IMPedance:AUTO <mode>",
                           Description: "Enable or disable high-impedance (>10 GOhm) input for DCV",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON|OFF (ON = >10 GOhm on 100mV, 1V, 10V ranges only)",
                           Query_Form: "INP:IMP:AUTO?",
                           Default_Value: "OFF (10 MOhm on all ranges)",
                           Example: "INP:IMP:AUTO ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "ZERO:AUTO",
                           Syntax: "ZERO:AUTO <mode>",
                           Description: "Enable or disable autozero",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON|OFF|ONCE",
                           Query_Form: "ZERO:AUTO?",
                           Default_Value: "ON",
                           Example: "ZERO:AUTO ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DET:BAND",
                           Syntax: "DETector:BANDwidth <bandwidth>",
                           Description: "Set AC signal detector filter bandwidth",
                           Category: Command_Category.Configuration,
                           Parameters: "bandwidth: 3|20|200|MIN|MAX (Hz)",
                           Query_Form: "DET:BAND?",
                           Default_Value: "20",
                           Example: "DET:BAND 20",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== Trigger Commands =====
        new Command_Entry( Command: "TRIG:SOUR",
                           Syntax: "TRIGger:SOURce <source>",
                           Description: "Select trigger source",
                           Category: Command_Category.Trigger,
                           Parameters: "source: IMMediate|BUS|EXTernal",
                           Query_Form: "TRIG:SOUR?",
                           Default_Value: "IMMediate",
                           Example: "TRIG:SOUR BUS",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "TRIG:DEL",
                           Syntax: "TRIGger:DELay <seconds>",
                           Description: "Set trigger delay",
                           Category: Command_Category.Trigger,
                           Parameters: "seconds: 0 to 3600 | MIN | MAX",
                           Query_Form: "TRIG:DEL?",
                           Default_Value: "AUTO",
                           Example: "TRIG:DEL 0.5",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "TRIG:DEL:AUTO",
                           Syntax: "TRIGger:DELay:AUTO <mode>",
                           Description: "Enable or disable automatic trigger delay",
                           Category: Command_Category.Trigger,
                           Parameters: "mode: ON|OFF",
                           Query_Form: "TRIG:DEL:AUTO?",
                           Default_Value: "ON",
                           Example: "TRIG:DEL:AUTO ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "TRIG:COUN",
                           Syntax: "TRIGger:COUNt <count>",
                           Description: "Set number of triggers to accept before returning to idle",
                           Category: Command_Category.Trigger,
                           Parameters: "count: 1 to 50000 | MIN | MAX | INFinity",
                           Query_Form: "TRIG:COUN?",
                           Default_Value: "1",
                           Example: "TRIG:COUN 10",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SAMP:COUN",
                           Syntax: "SAMPle:COUNt <count>",
                           Description: "Set number of readings per trigger event",
                           Category: Command_Category.Trigger,
                           Parameters: "count: 1 to 50000 | MIN | MAX",
                           Query_Form: "SAMP:COUN?",
                           Default_Value: "1",
                           Example: "SAMP:COUN 1000",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SAMP:SOUR",
                           Syntax: "SAMPle:SOURce <source>",
                           Description: "Select sample timing source — IMMediate or internal TIMer",
                           Category: Command_Category.Trigger,
                           Parameters: "source: IMMediate|TIMer",
                           Query_Form: "SAMP:SOUR?",
                           Default_Value: "IMMediate",
                           Example: "SAMP:SOUR TIM",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "SAMP:TIM",
                           Syntax: "SAMPle:TIMer <interval>",
                           Description: ( "Set sample interval when SAMP:SOUR is TIMer (min 20 µs = 50k " +
                                          "rdgs/sec)" ),
                           Category: Command_Category.Trigger,
                           Parameters: "interval: 20e-6 to 3600 seconds | MIN | MAX",
                           Query_Form: "SAMP:TIM?",
                           Default_Value: "500e-3",
                           Example: "SAMP:TIM 100e-6",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "INIT",
                           Syntax: "INITiate",
                           Description: "Change trigger state from idle to wait-for-trigger",
                           Category: Command_Category.Trigger,
                           Parameters: "None",
                           Query_Form: "INIT?",
                           Default_Value: "N/A",
                           Example: "INIT",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        new Command_Entry( Command: "*TRG",
                           Syntax: "*TRG",
                           Description: "Send a bus trigger (trigger source must be BUS)",
                           Category: Command_Category.Trigger,
                           Parameters: "None (requires TRIG:SOUR BUS)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*TRG",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

        // ===== Math Commands =====
        new Command_Entry( Command: "CALC:FUNC",
                           Syntax: "CALCulate:FUNCtion <function>",
                           Description: "Select math function",
                           Category: Command_Category.Math,
                           Parameters: "function: NULL|DB|DBM|AVERage|LIMit",
                           Query_Form: "CALC:FUNC?",
                           Default_Value: "NULL",
                           Example: "CALC:FUNC NULL",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:STAT",
                           Syntax: "CALCulate:STATe <mode>",
                           Description: "Enable or disable math operations",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON|OFF",
                           Query_Form: "CALC:STAT?",
                           Default_Value: "OFF",
                           Example: "CALC:STAT ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:NULL:OFFS",
                           Syntax: "CALCulate:NULL:OFFSet <value>",
                           Description: "Set null (offset) value for null math function",
                           Category: Command_Category.Math,
                           Parameters: "value: -1e15 to +1e15 | MIN | MAX",
                           Query_Form: "CALC:NULL:OFFS?",
                           Default_Value: "0",
                           Example: "CALC:NULL:OFFS 0.5",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:DB:REF",
                           Syntax: "CALCulate:DB:REFerence <value>",
                           Description: "Set dB reference value",
                           Category: Command_Category.Math,
                           Parameters: "value: -200 to +200 dBm | MIN | MAX",
                           Query_Form: "CALC:DB:REF?",
                           Default_Value: "0",
                           Example: "CALC:DB:REF 1.0",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:DBM:REF",
                           Syntax: "CALCulate:DBM:REFerence <impedance>",
                           Description: "Set dBm reference impedance",
                           Category: Command_Category.Math,
                           Parameters: "impedance: 50 to 8000 ohms | MIN | MAX",
                           Query_Form: "CALC:DBM:REF?",
                           Default_Value: "600",
                           Example: "CALC:DBM:REF 50",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:LIM:LOW",
                           Syntax: "CALCulate:LIMit:LOWer <value>",
                           Description: "Set lower limit for limit testing",
                           Category: Command_Category.Math,
                           Parameters: "value: -1e15 to +1e15 | MIN | MAX",
                           Query_Form: "CALC:LIM:LOW?",
                           Default_Value: "0",
                           Example: "CALC:LIM:LOW 0.9",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:LIM:UPP",
                           Syntax: "CALCulate:LIMit:UPPer <value>",
                           Description: "Set upper limit for limit testing",
                           Category: Command_Category.Math,
                           Parameters: "value: -1e15 to +1e15 | MIN | MAX",
                           Query_Form: "CALC:LIM:UPP?",
                           Default_Value: "0",
                           Example: "CALC:LIM:UPP 1.1",
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

        new Command_Entry( Command: "CALC:AVER:AVER?",
                           Syntax: "CALCulate:AVERage:AVERage?",
                           Description: "Query average of readings from statistics register",
                           Category: Command_Category.Math,
                           Parameters: "None (read-only)",
                           Query_Form: "CALC:AVER:AVER?",
                           Default_Value: "N/A",
                           Example: "CALC:AVER:AVER?",
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

        new Command_Entry( Command: "CALC:TRAN:HIST:POIN",
                           Syntax: "CALCulate:TRANsform:HISTogram:POINts <count>",
                           Description: "Set number of histogram bins",
                           Category: Command_Category.Math,
                           Parameters: "count: 1 to 100 | MIN | MAX",
                           Query_Form: "CALC:TRAN:HIST:POIN?",
                           Default_Value: "100",
                           Example: "CALC:TRAN:HIST:POIN 100",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "CALC:TRAN:HIST:RANG",
                           Syntax: "CALCulate:TRANsform:HISTogram:RANGe:AUTO <mode>",
                           Description: "Enable or disable automatic histogram range",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON|OFF",
                           Query_Form: "CALC:TRAN:HIST:RANG?",
                           Default_Value: "ON",
                           Example: "CALC:TRAN:HIST:RANG ON",
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
                           Parameters: "None (returns error number and message string)",
                           Query_Form: "SYST:ERR?",
                           Default_Value: "N/A",
                           Example: "SYST:ERR?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "SYST:REM",
                           Syntax: "SYSTem:REMote",
                           Description: "Place instrument in remote mode (locks front panel)",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "SYST:REM",
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
                           Parameters: "mode: ON|OFF",
                           Query_Form: "DISP?",
                           Default_Value: "ON",
                           Example: "DISP ON",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DISP:TEXT",
                           Syntax: "DISPlay:TEXT <string>",
                           Description: "Display a text message on the front panel (max 12 characters)",
                           Category: Command_Category.IO,
                           Parameters: "string: up to 12 characters in double quotes",
                           Query_Form: "DISP:TEXT?",
                           Default_Value: "N/A",
                           Example: "DISP:TEXT \"TESTING\"",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        new Command_Entry( Command: "DISP:TEXT:CLE",
                           Syntax: "DISPlay:TEXT:CLEar",
                           Description: "Clear the front panel text message",
                           Category: Command_Category.IO,
                           Parameters: "None",
                           Query_Form: "DISP:TEXT:CLE?",
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

        new Command_Entry( Command: "SYST:BEEP:STAT",
                           Syntax: "SYSTem:BEEPer:STATe <mode>",
                           Description: "Enable or disable beeper for limit test and continuity",
                           Category: Command_Category.IO,
                           Parameters: "mode: ON|OFF",
                           Query_Form: null,
                           Default_Value: "ON",
                           Example: "SYST:BEEP:STAT ON",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "ROUT:TERM?",
                           Syntax: "ROUTe:TERMinals?",
                           Description: "Query which terminal set is active (front or rear)",
                           Category: Command_Category.IO,
                           Parameters: "None (returns FRON or REAR)",
                           Query_Form: "ROUT:TERM?",
                           Default_Value: "N/A",
                           Example: "ROUT:TERM?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "FORM",
                           Syntax: "FORMat:DATA <type>",
                           Description: "Set output data format",
                           Category: Command_Category.IO,
                           Parameters: "type: ASCii|REAL,32|REAL,64",
                           Query_Form: "FORM?",
                           Default_Value: "ASCii",
                           Example: "FORM ASC",
                           Test_Behavior: Test_Behavior.Write_Then_Query ),

        // ===== Memory Commands =====
        new Command_Entry( Command: "DATA:POINts?",
                           Syntax: "DATA:POINts?",
                           Description: "Query number of readings stored in internal memory",
                           Category: Command_Category.Memory,
                           Parameters: "None (read-only, max 50000)",
                           Query_Form: "DATA:POINts?",
                           Default_Value: "N/A",
                           Example: "DATA:POINts?",
                           Test_Behavior: Test_Behavior.Query_Safe ),

        new Command_Entry( Command: "DATA:REM",
                           Syntax: "DATA:REMove? <count>",
                           Description: "Remove and return the specified number of readings from memory",
                           Category: Command_Category.Memory,
                           Parameters: "count: 1 to 50000 | MIN | MAX",
                           Query_Form: "DATA:REM?",
                           Default_Value: "N/A",
                           Example: "DATA:REM? 100",
                           Test_Behavior: Test_Behavior.Requires_Sequence ),

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

        new Command_Entry( Command: "*SAV",
                           Syntax: "*SAV <register>",
                           Description: "Save current instrument state to memory register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 0|1|2|3|4",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*SAV 1",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        new Command_Entry( Command: "*RCL",
                           Syntax: "*RCL <register>",
                           Description: "Recall instrument state from memory register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 0|1|2|3|4",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "*RCL 1",
                           Test_Behavior: Test_Behavior.Skip_Destructive ),

        // ===== Calibration Commands =====
        new Command_Entry( Command: "CAL:SEC:STAT",
                           Syntax: "CALibration:SECure:STATe <mode>,<code>",
                           Description: "Enable or disable calibration security",
                           Category: Command_Category.Calibration,
                           Parameters: "mode: ON|OFF, code: security code string",
                           Query_Form: "CAL:SEC:STAT?",
                           Default_Value: "ON",
                           Example: "CAL:SEC:STAT OFF,HP034411",
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
                           Description: "Store a calibration message string (max 40 characters)",
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
      public string Reset_Command => "*RST";
      public string Error_Query => "SYST:ERR?";
      public bool Has_Error_Queue => true;

      public List<Command_Entry> Get_Commands() => HP34411_Command_Dictionary_Class.Get_All_Commands();

      public IEnumerable<Command_Test_Result> Run_Sequences( Func<string, string> Query, Action<string> Send )
      {
        foreach (var R in Test_Fetch_Sequence( Query, Send ))
          yield return R;
        foreach (var R in Test_Bus_Trigger_Sequence( Query, Send ))
          yield return R;
        foreach (var R in Test_Math_Average_Sequence( Query, Send ))
          yield return R;
        foreach (var R in Test_Data_Last_Sequence( Query, Send ))
          yield return R;
      }

      private static IEnumerable<Command_Test_Result> Test_Fetch_Sequence( Func<string, string> Query,
                                                                           Action<string> Send )
      {
        var Seq_Cmd = new Command_Entry( Command: "FETCH? [sequence]",
                                                         Syntax: "CONF → INIT → FETCH?",
                                                         Description: "Sequenced fetch test",
                                                         Category: Command_Category.Measurement,
                                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;
        try
        {
          Send( "CONF:VOLT:DC 10" );
          Send( "INIT" );
          string Reading = Query( "FETCH?" );
          bool OK = double.TryParse( Reading,
                                            System.Globalization.NumberStyles.Float,
                                            System.Globalization.CultureInfo.InvariantCulture,
                                            out _ );
          Result = OK ? Command_Test_Result.Pass( Seq_Cmd, Reading )
                              : Command_Test_Result.Fail( Seq_Cmd, $"Non-numeric response: {Reading}" );
        }
        catch (Exception Ex)
        {
          Result = Command_Test_Result.Fail( Seq_Cmd, Ex.Message );
        }

        yield return Result;
      }

      private static IEnumerable<Command_Test_Result> Test_Bus_Trigger_Sequence( Func<string, string> Query,
                                                                                 Action<string> Send )
      {
        var Seq_Cmd = new Command_Entry( Command: "*TRG [sequence]",
                                         Syntax: "CONF → TRIG:SOUR BUS → INIT → *TRG → FETCH?",
                                         Description: "Sequenced bus trigger test",
                                         Category: Command_Category.Trigger,
                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;
        try
        {
          Send( "CONF:VOLT:DC" );
          Send( "TRIG:SOUR BUS" );
          Send( "INIT" );
          Send( "*TRG" );
          string Reading = Query( "FETCH?" );
          bool OK = double.TryParse( Reading,
                                            System.Globalization.NumberStyles.Float,
                                            System.Globalization.CultureInfo.InvariantCulture,
                                            out _ );
          Result = OK ? Command_Test_Result.Pass( Seq_Cmd, Reading )
                              : Command_Test_Result.Fail( Seq_Cmd, $"Non-numeric response: {Reading}" );
        }
        catch (Exception Ex)
        {
          Result = Command_Test_Result.Fail( Seq_Cmd, Ex.Message );
        }
        finally
        {
          try
          {
            Send( "TRIG:SOUR IMM" );
          }
          catch
          {
          }
        }

        yield return Result;
      }

      private static IEnumerable<Command_Test_Result> Test_Math_Average_Sequence( Func<string, string> Query,
                                                                                  Action<string> Send )
      {
        var Seq_Cmd = new Command_Entry( Command: "CALC:AVER [sequence]",
                                         Syntax: "CONF → CALC:FUNC → CALC:STAT → READ x5 → CALC:AVER:*?",
                                         Description: "Sequenced math average test",
                                         Category: Command_Category.Math,
                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;
        try
        {
          Send( "CONF:VOLT:DC" );
          Send( "CALC:FUNC AVERage" );
          Send( "CALC:STAT ON" );
          for (int I = 0; I < 5; I++)
            try
            {
              Query( "READ?" );
            }
            catch
            {
            }

          string Min = Query( "CALC:AVER:MIN?" );
          string Max = Query( "CALC:AVER:MAX?" );
          string Avg = Query( "CALC:AVER:AVER?" );
          string Count = Query( "CALC:AVER:COUN?" );
          bool OK = !string.IsNullOrWhiteSpace( Min ) && !string.IsNullOrWhiteSpace( Max ) &&
                         !string.IsNullOrWhiteSpace( Avg ) && !string.IsNullOrWhiteSpace( Count );
          Result =
            OK ? Command_Test_Result
                   .Pass( Seq_Cmd, $"Min={Min.Trim()} Max={Max.Trim()} Avg={Avg.Trim()} N={Count.Trim()}" )
               : Command_Test_Result.Fail( Seq_Cmd, "One or more CALC:AVER queries returned empty" );
        }
        catch (Exception Ex)
        {
          Result = Command_Test_Result.Fail( Seq_Cmd, Ex.Message );
        }
        finally
        {
          try
          {
            Send( "CALC:STAT OFF" );
          }
          catch
          {
          }
        }

        yield return Result;
      }

      private static IEnumerable<Command_Test_Result> Test_Data_Last_Sequence( Func<string, string> Query,
                                                                               Action<string> Send )
      {
        var Seq_Cmd = new Command_Entry( Command: "DATA:LAST? [sequence]",
                                         Syntax: "CONF:VOLT:DC → READ? → DATA:LAST?",
                                         Description: "Sequenced DATA:LAST? test — 34411A specific",
                                         Category: Command_Category.Measurement,
                                         Test_Behavior: Test_Behavior.Requires_Sequence );

        Command_Test_Result Result;
        try
        {
          Send( "CONF:VOLT:DC" );
          Query( "READ?" );
          string Last = Query( "DATA:LAST?" );
          bool OK = double.TryParse( Last,
                                         System.Globalization.NumberStyles.Float,
                                         System.Globalization.CultureInfo.InvariantCulture,
                                         out _ );
          Result = OK ? Command_Test_Result.Pass( Seq_Cmd, Last.Trim() )
                           : Command_Test_Result.Fail( Seq_Cmd, $"Non-numeric DATA:LAST? response: {Last}" );
        }
        catch (Exception Ex)
        {
          Result = Command_Test_Result.Fail( Seq_Cmd, Ex.Message );
        }

        yield return Result;
      }
    }
  }
}
