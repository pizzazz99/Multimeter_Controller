// =============================================================================
// FILE:     HP4263_Command_Dictionary_Class.cs
// PROJECT:  Multimeter_Controller
// =============================================================================
//
// DESCRIPTION:
//   Static command dictionary for the HP / Agilent 4263B 100 Hz – 100 kHz
//   LCR meter. Provides a structured, searchable registry of all supported
//   SCPI instrument commands, organized by functional category. Each entry
//   captures the full command syntax, description, valid parameter ranges,
//   query form, factory default value, and a usage example.
//
//   This class is the single source of truth for 4263B command metadata
//   within the Multimeter_Controller namespace. It is designed to support
//   command validation, UI population, documentation generation, and runtime
//   lookup.
//
// -----------------------------------------------------------------------------
// INSTRUMENT:
//   HP / Agilent 4263B LCR Meter
//   Command Set:  SCPI (Standard Commands for Programmable Instruments)
//   Interface:    GPIB (IEEE-488.2)
//   Test Freq:    100 Hz, 120 Hz, 1 kHz, 10 kHz, 20 kHz, 100 kHz
//   Test Signal:  5 mVrms to 1 Vrms (open circuit) into test fixture
//   Measurement:  Primary + secondary parameter pairs (e.g. Cp-D, Ls-Q,
//                 Z-theta, R-X, and more)
//
// -----------------------------------------------------------------------------
// MEASUREMENT PARAMETER PAIRS:
//   The 4263B always returns TWO values per measurement — a primary and a
//   secondary parameter. The pair is selected with FUNC:IMP:TYPE.
//   Common pairs:
//     CPD    — Parallel capacitance (Cp) + dissipation factor (D)
//     CSD    — Series capacitance (Cs) + dissipation factor (D)
//     CPQ    — Parallel capacitance (Cp) + quality factor (Q)
//     CSQ    — Series capacitance (Cs) + quality factor (Q)
//     CPRP   — Parallel capacitance (Cp) + parallel resistance (Rp)
//     CSRS   — Series capacitance (Cs) + series resistance (Rs)
//     LPD    — Parallel inductance (Lp) + dissipation factor (D)
//     LSD    — Series inductance (Ls) + dissipation factor (D)
//     LPQ    — Parallel inductance (Lp) + quality factor (Q)
//     LSQ    — Series inductance (Ls) + quality factor (Q)
//     LPRP   — Parallel inductance (Lp) + parallel resistance (Rp)
//     LSRS   — Series inductance (Ls) + series resistance (Rs)
//     RX     — Resistance (R) + reactance (X)
//     ZTD    — Impedance magnitude (Z) + phase angle theta (degrees)
//     ZTR    — Impedance magnitude (Z) + phase angle theta (radians)
//     GB     — Conductance (G) + susceptance (B)
//     YTD    — Admittance magnitude (Y) + phase angle theta (degrees)
//     YTR    — Admittance magnitude (Y) + phase angle theta (radians)
//     VDID   — DC voltage (Vdc) + DC current (Idc) monitor
//
// -----------------------------------------------------------------------------
// FETCH RESPONSE FORMAT:
//   FETCH? returns a comma-separated string:
//     <primary_value>,<secondary_value>,<status>
//   Status codes:
//     0  — Normal measurement
//     1  — Overrange on primary
//     2  — Overrange on secondary
//     3  — Overrange on both
//     4  — Signal source overload
//   Always check status before using measurement values.
//
// -----------------------------------------------------------------------------
// COMMAND CATEGORIES:
//
//   Measurement   — MEAS:IMP? (one-shot impedance), FETCH?, READ?.
//
//   Configuration — Measurement function/parameter pair (FUNC:IMP:TYPE),
//                   test frequency (FREQ), test signal level (VOLT or CURR),
//                   measurement range (FUNC:IMP:RANG / FUNC:IMP:RANG:AUTO),
//                   integration time / averaging (APER), DC bias voltage
//                   (BIAS:VOLT / BIAS:STAT), DC bias current (BIAS:CURR),
//                   cable length correction (CORR:LENG), output data format
//                   (FORM).
//
//   Compensation  — Open compensation (CORR:OPEN / CORR:OPEN:STAT /
//                   CORR:OPEN:EXEC), short compensation (CORR:SHOR /
//                   CORR:SHOR:STAT / CORR:SHOR:EXEC), load compensation
//                   (CORR:LOAD / CORR:LOAD:STAT / CORR:LOAD:EXEC /
//                   CORR:LOAD:TYPE), and compensation clear (CORR:CLE).
//
//   Trigger       — Trigger source (TRIG:SOUR), trigger delay (TRIG:DEL),
//                   sample count (SAMP:COUN), initiate (INIT), abort (ABOR),
//                   and bus trigger (*TRG).
//
//   Math          — Limit testing (CALC:LIM:TYPE / CALC:LIM:COMP /
//                   CALC:LIM:LOW / CALC:LIM:UPP / CALC:LIM:STAT /
//                   CALC:LIM:FAIL?).
//
//   System        — *IDN?, *RST, *CLS, *OPC, *OPC?, *TST?, SYST:ERR?,
//                   SYST:VERS?, *STB?, *SRE, *ESE, *ESR?.
//
//   IO            — Display (DISP / DISP:PAGE), beeper (SYST:BEEP),
//                   data format (FORM).
//
//   Memory        — *SAV, *RCL.
//
//   Calibration   — CAL:COUN?, CAL:SEC:STAT, CAL:STR.
//
// -----------------------------------------------------------------------------
// POLLING USE CASE — DRIFT MONITORING:
//   To monitor component measurement drift across multiple 4263B units,
//   poll the following at regular intervals:
//     FETCH?        Returns primary value, secondary value, and status code
//     FUNC:IMP:TYPE? Returns the currently selected parameter pair
//     FREQ?         Returns the current test frequency
//   Ensure TRIG:SOUR is set to BUS or EXTernal and INIT has been sent
//   before continuous polling. For one-shot polling use MEAS:IMP? which
//   handles configure + trigger + fetch in a single command.
//
// -----------------------------------------------------------------------------
// COMPENSATION WORKFLOW:
//   Open / short / load compensation removes fixture and cable parasitics.
//   Typical workflow:
//     1. Set cable length:   CORR:LENG 1       (0, 1, or 2 metres)
//     2. Connect open fixture, execute open compensation:
//                            CORR:OPEN:EXEC
//                            CORR:OPEN:STAT ON
//     3. Connect shorted fixture, execute short compensation:
//                            CORR:SHOR:EXEC
//                            CORR:SHOR:STAT ON
//     4. Optionally connect load reference, execute load compensation:
//                            CORR:LOAD:TYPE CPD
//                            CORR:LOAD:EXEC
//                            CORR:LOAD:STAT ON
//   All compensation data is stored in non-volatile memory.
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
  public static class HP4263_Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands()
    {
      var Commands = new List<Command_Entry> {
        // ===== Measurement Commands =====

        new Command_Entry( Command: "MEAS:IMP?",
                           Syntax: "MEASure:IMPedance?",
                           Description: ( "One-shot impedance measurement — configure, trigger, and return " +
                                          "primary + secondary values" ),
                           Category: Command_Category.Measurement,
                           Parameters: "None (uses current FUNC:IMP:TYPE setting)",
                           Query_Form: "MEAS:IMP?",
                           Default_Value: "N/A",
                           Example: "MEAS:IMP?" ),

        new Command_Entry( Command: "READ?",
                           Syntax: "READ?",
                           Description: ( "Trigger measurement and return primary value, secondary value, " +
                                          "and status" ),
                           Category: Command_Category.Measurement,
                           Parameters: "None (uses current configuration)",
                           Query_Form: "READ?",
                           Default_Value: "N/A",
                           Example: "READ?" ),

        new Command_Entry( Command: "FETCH?",
                           Syntax: "FETCH?",
                           Description: ( "Return last completed measurement without re-triggering " +
                                          "(returns primary,secondary,status)" ),
                           Category: Command_Category.Measurement,
                           Parameters: ( "None (status: 0=normal, 1=pri overrange, 2=sec overrange, " +
                                         "3=both, 4=overload)" ),
                           Query_Form: "FETCH?",
                           Default_Value: "N/A",
                           Example: "FETCH?" ),

        // ===== Configuration Commands =====

        new Command_Entry( Command: "FUNC:IMP:TYPE",
                           Syntax: "FUNCtion:IMPedance:TYPE <parameter_pair>",
                           Description: ( "Select impedance measurement parameter pair (primary + " +
                                          "secondary)" ),
                           Category: Command_Category.Configuration,
                           Parameters: ( "CPD|CSD|CPQ|CSQ|CPRP|CSRS|LPD|LSD|LPQ|LSQ|LPRP|LSRS|RX|ZTD|ZTR|" +
                                         "GB|YTD|YTR|VDID" ),
                           Query_Form: "FUNC:IMP:TYPE?",
                           Default_Value: "CPD",
                           Example: "FUNC:IMP:TYPE CPD" ),

        new Command_Entry( Command: "FREQ",
                           Syntax: "FREQuency <frequency>",
                           Description: "Set test signal frequency",
                           Category: Command_Category.Configuration,
                           Parameters: "frequency: 100 | 120 | 1000 | 10000 | 20000 | 100000 (Hz)",
                           Query_Form: "FREQ?",
                           Default_Value: "1000",
                           Example: "FREQ 1000" ),

        new Command_Entry( Command: "VOLT",
                           Syntax: "VOLTage:LEVel <voltage>",
                           Description: "Set test signal AC voltage level (constant voltage mode)",
                           Category: Command_Category.Configuration,
                           Parameters: "voltage: 5e-3 to 1.0 Vrms | MIN | MAX",
                           Query_Form: "VOLT?",
                           Default_Value: "1.0",
                           Example: "VOLT 1.0" ),

        new Command_Entry( Command: "CURR",
                           Syntax: "CURRent:LEVel <current>",
                           Description: "Set test signal AC current level (constant current mode)",
                           Category: Command_Category.Configuration,
                           Parameters: "current: 100e-6 to 100e-3 Arms | MIN | MAX",
                           Query_Form: "CURR?",
                           Default_Value: "N/A",
                           Example: "CURR 10e-3" ),

        new Command_Entry( Command: "FUNC:IMP:RANG",
                           Syntax: "FUNCtion:IMPedance:RANGe <impedance>",
                           Description: "Set measurement range manually by specifying expected impedance",
                           Category: Command_Category.Configuration,
                           Parameters: "impedance: 10 | 100 | 1e3 | 10e3 | 100e3 | MIN | MAX (Ohms)",
                           Query_Form: "FUNC:IMP:RANG?",
                           Default_Value: "AUTO",
                           Example: "FUNC:IMP:RANG 1e3" ),

        new Command_Entry( Command: "FUNC:IMP:RANG:AUTO",
                           Syntax: "FUNCtion:IMPedance:RANGe:AUTO <mode>",
                           Description: "Enable or disable automatic impedance range selection",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON | OFF | ONCE",
                           Query_Form: "FUNC:IMP:RANG:AUTO?",
                           Default_Value: "ON",
                           Example: "FUNC:IMP:RANG:AUTO ON" ),

        new Command_Entry( Command: "APER",
                           Syntax: "APERture <time>[,<averages>]",
                           Description: "Set integration time and number of averages per measurement",
                           Category: Command_Category.Configuration,
                           Parameters: "time: SHORt | MEDium | LONG, averages: 1 to 256 | MIN | MAX",
                           Query_Form: "APER?",
                           Default_Value: "MEDium, 1",
                           Example: "APER LONG,8" ),

        new Command_Entry( Command: "BIAS:VOLT",
                           Syntax: "BIAS:VOLTage:LEVel <voltage>",
                           Description: "Set DC bias voltage applied during measurement",
                           Category: Command_Category.Configuration,
                           Parameters: "voltage: 0 to 2.0 V | MIN | MAX",
                           Query_Form: "BIAS:VOLT?",
                           Default_Value: "0",
                           Example: "BIAS:VOLT 1.5" ),

        new Command_Entry( Command: "BIAS:CURR",
                           Syntax: "BIAS:CURRent:LEVel <current>",
                           Description: "Set DC bias current applied during measurement",
                           Category: Command_Category.Configuration,
                           Parameters: "current: 0 to 100e-3 A | MIN | MAX",
                           Query_Form: "BIAS:CURR?",
                           Default_Value: "0",
                           Example: "BIAS:CURR 10e-3" ),

        new Command_Entry( Command: "BIAS:STAT",
                           Syntax: "BIAS:STATe <mode>",
                           Description: "Enable or disable DC bias during measurement",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "BIAS:STAT?",
                           Default_Value: "OFF",
                           Example: "BIAS:STAT ON" ),

        new Command_Entry( Command: "CORR:LENG",
                           Syntax: "CORRection:LENGth <metres>",
                           Description: ( "Set cable length for compensation — affects parasitic " +
                                          "correction model" ),
                           Category: Command_Category.Configuration,
                           Parameters: "metres: 0 | 1 | 2",
                           Query_Form: "CORR:LENG?",
                           Default_Value: "0",
                           Example: "CORR:LENG 1" ),

        new Command_Entry( Command: "FORM",
                           Syntax: "FORMat:DATA <type>",
                           Description: "Set output data format for measurement results",
                           Category: Command_Category.Configuration,
                           Parameters: "type: ASCii | REAL,32 | REAL,64",
                           Query_Form: "FORM?",
                           Default_Value: "ASCii",
                           Example: "FORM ASC" ),

        // ===== Compensation Commands =====

        new Command_Entry( Command: "CORR:OPEN",
                           Syntax: "CORRection:OPEN",
                           Description: ( "Perform open compensation measurement at current frequency " +
                                          "(stores raw data)" ),
                           Category: Command_Category.Compensation,
                           Parameters: "None (connect open-circuit fixture before executing)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CORR:OPEN" ),

        new Command_Entry( Command: "CORR:OPEN:EXEC",
                           Syntax: "CORRection:OPEN:EXECute",
                           Description: ( "Execute open compensation — measure and store open-circuit " +
                                          "correction data" ),
                           Category: Command_Category.Compensation,
                           Parameters: "None (connect open-circuit fixture before executing)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CORR:OPEN:EXEC" ),

        new Command_Entry( Command: "CORR:OPEN:STAT",
                           Syntax: "CORRection:OPEN:STATe <mode>",
                           Description: "Enable or disable open compensation correction",
                           Category: Command_Category.Compensation,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "CORR:OPEN:STAT?",
                           Default_Value: "OFF",
                           Example: "CORR:OPEN:STAT ON" ),

        new Command_Entry( Command: "CORR:SHOR",
                           Syntax: "CORRection:SHORt",
                           Description: ( "Perform short compensation measurement at current frequency " +
                                          "(stores raw data)" ),
                           Category: Command_Category.Compensation,
                           Parameters: "None (connect shorted fixture before executing)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CORR:SHOR" ),

        new Command_Entry( Command: "CORR:SHOR:EXEC",
                           Syntax: "CORRection:SHORt:EXECute",
                           Description: ( "Execute short compensation — measure and store short-circuit " +
                                          "correction data" ),
                           Category: Command_Category.Compensation,
                           Parameters: "None (connect shorted fixture before executing)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CORR:SHOR:EXEC" ),

        new Command_Entry( Command: "CORR:SHOR:STAT",
                           Syntax: "CORRection:SHORt:STATe <mode>",
                           Description: "Enable or disable short compensation correction",
                           Category: Command_Category.Compensation,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "CORR:SHOR:STAT?",
                           Default_Value: "OFF",
                           Example: "CORR:SHOR:STAT ON" ),

        new Command_Entry( Command: "CORR:LOAD:TYPE",
                           Syntax: "CORRection:LOAD:TYPE <parameter_pair>",
                           Description: "Select parameter pair for load compensation reference component",
                           Category: Command_Category.Compensation,
                           Parameters: ( "parameter_pair: CPD | CSD | CPQ | CSQ | CPRP | CSRS | LPD | LSD " +
                                         "| LPQ | LSQ | LPRP | LSRS | RX | ZTD | ZTR | GB | YTD | YTR" ),
                           Query_Form: "CORR:LOAD:TYPE?",
                           Default_Value: "CPD",
                           Example: "CORR:LOAD:TYPE CPD" ),

        new Command_Entry( Command: "CORR:LOAD:EXEC",
                           Syntax: "CORRection:LOAD:EXECute",
                           Description: ( "Execute load compensation — measure and store load correction " +
                                          "data" ),
                           Category: Command_Category.Compensation,
                           Parameters: "None (connect reference load component before executing)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CORR:LOAD:EXEC" ),

        new Command_Entry( Command: "CORR:LOAD:STAT",
                           Syntax: "CORRection:LOAD:STATe <mode>",
                           Description: "Enable or disable load compensation correction",
                           Category: Command_Category.Compensation,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "CORR:LOAD:STAT?",
                           Default_Value: "OFF",
                           Example: "CORR:LOAD:STAT ON" ),

        new Command_Entry( Command: "CORR:CLE",
                           Syntax: "CORRection:CLEar",
                           Description: "Clear all stored compensation data (open, short, and load)",
                           Category: Command_Category.Compensation,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CORR:CLE" ),

        // ===== Trigger Commands =====

        new Command_Entry( Command: "TRIG:SOUR",
                           Syntax: "TRIGger:SOURce <source>",
                           Description: "Select trigger source for measurements",
                           Category: Command_Category.Trigger,
                           Parameters: "source: INTernal | BUS | EXTernal",
                           Query_Form: "TRIG:SOUR?",
                           Default_Value: "INTernal",
                           Example: "TRIG:SOUR INT" ),

        new Command_Entry( Command: "TRIG:DEL",
                           Syntax: "TRIGger:DELay <seconds>",
                           Description: "Set delay between trigger event and start of measurement",
                           Category: Command_Category.Trigger,
                           Parameters: "seconds: 0 to 60 s | MIN | MAX",
                           Query_Form: "TRIG:DEL?",
                           Default_Value: "0",
                           Example: "TRIG:DEL 0.01" ),

        new Command_Entry( Command: "SAMP:COUN",
                           Syntax: "SAMPle:COUNt <count>",
                           Description: "Set number of measurements per trigger event",
                           Category: Command_Category.Trigger,
                           Parameters: "count: 1 to 999 | MIN | MAX",
                           Query_Form: "SAMP:COUN?",
                           Default_Value: "1",
                           Example: "SAMP:COUN 10" ),

        new Command_Entry( Command: "INIT",
                           Syntax: "INITiate",
                           Description: "Move trigger system from idle to armed/waiting state",
                           Category: Command_Category.Trigger,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "INIT" ),

        new Command_Entry( Command: "ABOR",
                           Syntax: "ABORt",
                           Description: "Abort current measurement and return trigger system to idle",
                           Category: Command_Category.Trigger,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "ABOR" ),

        new Command_Entry( Command: "*TRG",
                           Syntax: "*TRG",
                           Description: "Send a bus trigger (TRIG:SOUR must be BUS)",
                           Category: Command_Category.Trigger,
                           Parameters: "None (requires TRIG:SOUR BUS)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*TRG" ),

        // ===== Math Commands =====

        new Command_Entry( Command: "CALC:LIM:TYPE",
                           Syntax: "CALCulate:LIMit:TYPE <parameter>",
                           Description: "Select which measurement parameter limit testing applies to",
                           Category: Command_Category.Math,
                           Parameters: "parameter: A (primary) | B (secondary)",
                           Query_Form: "CALC:LIM:TYPE?",
                           Default_Value: "A",
                           Example: "CALC:LIM:TYPE A" ),

        new Command_Entry( Command: "CALC:LIM:COMP",
                           Syntax: "CALCulate:LIMit:COMParator <mode>",
                           Description: "Select limit comparator mode",
                           Category: Command_Category.Math,
                           Parameters: "mode: ABS | DEV | PDEV (absolute, deviation, percent deviation)",
                           Query_Form: "CALC:LIM:COMP?",
                           Default_Value: "ABS",
                           Example: "CALC:LIM:COMP ABS" ),

        new Command_Entry( Command: "CALC:LIM:LOW",
                           Syntax: "CALCulate:LIMit:LOWer <value>",
                           Description: "Set lower limit for limit testing",
                           Category: Command_Category.Math,
                           Parameters: "value: any numeric in current measurement units",
                           Query_Form: "CALC:LIM:LOW?",
                           Default_Value: "0",
                           Example: "CALC:LIM:LOW 90e-12" ),

        new Command_Entry( Command: "CALC:LIM:UPP",
                           Syntax: "CALCulate:LIMit:UPPer <value>",
                           Description: "Set upper limit for limit testing",
                           Category: Command_Category.Math,
                           Parameters: "value: any numeric in current measurement units",
                           Query_Form: "CALC:LIM:UPP?",
                           Default_Value: "0",
                           Example: "CALC:LIM:UPP 110e-12" ),

        new Command_Entry( Command: "CALC:LIM:STAT",
                           Syntax: "CALCulate:LIMit:STATe <mode>",
                           Description: "Enable or disable limit testing",
                           Category: Command_Category.Math,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "CALC:LIM:STAT?",
                           Default_Value: "OFF",
                           Example: "CALC:LIM:STAT ON" ),

        new Command_Entry( Command: "CALC:LIM:FAIL?",
                           Syntax: "CALCulate:LIMit:FAIL?",
                           Description: ( "Query limit test result for last measurement (0 = pass, 1 = " +
                                          "fail)" ),
                           Category: Command_Category.Math,
                           Parameters: "None (read-only)",
                           Query_Form: "CALC:LIM:FAIL?",
                           Default_Value: "N/A",
                           Example: "CALC:LIM:FAIL?" ),

        // ===== System Commands =====

        new Command_Entry( Command: "*IDN?",
                           Syntax: "*IDN?",
                           Description: "Query instrument identification string",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "*IDN?",
                           Default_Value: "N/A",
                           Example: "*IDN?" ),

        new Command_Entry( Command: "*RST",
                           Syntax: "*RST",
                           Description: "Reset instrument to factory default state",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*RST" ),

        new Command_Entry( Command: "*CLS",
                           Syntax: "*CLS",
                           Description: "Clear status registers and error queue",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "*CLS" ),

        new Command_Entry( Command: "*OPC?",
                           Syntax: "*OPC?",
                           Description: ( "Query operation complete (returns 1 when all pending operations " +
                                          "finish)" ),
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "*OPC?",
                           Default_Value: "N/A",
                           Example: "*OPC?" ),

        new Command_Entry( Command: "*OPC",
                           Syntax: "*OPC",
                           Description: ( "Set OPC bit in Standard Event register when all pending " +
                                          "operations complete" ),
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*OPC" ),

        new Command_Entry( Command: "*TST?",
                           Syntax: "*TST?",
                           Description: "Perform self-test and return result (0 = pass)",
                           Category: Command_Category.System,
                           Parameters: "None (returns 0 for pass, non-zero for fail)",
                           Query_Form: "*TST?",
                           Default_Value: "N/A",
                           Example: "*TST?" ),

        new Command_Entry( Command: "SYST:ERR?",
                           Syntax: "SYSTem:ERRor?",
                           Description: "Query and clear one error from the error queue",
                           Category: Command_Category.System,
                           Parameters: "None (returns error number and message string)",
                           Query_Form: "SYST:ERR?",
                           Default_Value: "N/A",
                           Example: "SYST:ERR?" ),

        new Command_Entry( Command: "SYST:VERS?",
                           Syntax: "SYSTem:VERSion?",
                           Description: "Query SCPI version supported by instrument",
                           Category: Command_Category.System,
                           Parameters: "None",
                           Query_Form: "SYST:VERS?",
                           Default_Value: "N/A",
                           Example: "SYST:VERS?" ),

        new Command_Entry( Command: "*STB?",
                           Syntax: "*STB?",
                           Description: "Query status byte register",
                           Category: Command_Category.System,
                           Parameters: "None (returns decimal value of status byte)",
                           Query_Form: "*STB?",
                           Default_Value: "N/A",
                           Example: "*STB?" ),

        new Command_Entry( Command: "*SRE",
                           Syntax: "*SRE <value>",
                           Description: "Set Service Request Enable register",
                           Category: Command_Category.System,
                           Parameters: "value: 0–255 (bit mask)",
                           Query_Form: "*SRE?",
                           Default_Value: "0",
                           Example: "*SRE 32" ),

        new Command_Entry( Command: "*ESE",
                           Syntax: "*ESE <value>",
                           Description: "Set Standard Event Status Enable register",
                           Category: Command_Category.System,
                           Parameters: "value: 0–255 (bit mask)",
                           Query_Form: "*ESE?",
                           Default_Value: "0",
                           Example: "*ESE 1" ),

        new Command_Entry( Command: "*ESR?",
                           Syntax: "*ESR?",
                           Description: "Query and clear Standard Event Status register",
                           Category: Command_Category.System,
                           Parameters: "None (returns decimal value of register)",
                           Query_Form: "*ESR?",
                           Default_Value: "N/A",
                           Example: "*ESR?" ),

        // ===== IO Commands =====

        new Command_Entry( Command: "DISP",
                           Syntax: "DISPlay <mode>",
                           Description: "Enable or disable front panel display",
                           Category: Command_Category.IO,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "DISP?",
                           Default_Value: "ON",
                           Example: "DISP ON" ),

        new Command_Entry( Command: "DISP:PAGE",
                           Syntax: "DISPlay:PAGE <page>",
                           Description: "Select front panel display page",
                           Category: Command_Category.IO,
                           Parameters: "page: MEAS | SETUP | LIST | CORR | LIMIT",
                           Query_Form: "DISP:PAGE?",
                           Default_Value: "MEAS",
                           Example: "DISP:PAGE MEAS" ),

        new Command_Entry( Command: "SYST:BEEP",
                           Syntax: "SYSTem:BEEPer",
                           Description: "Issue a single beep from the front panel",
                           Category: Command_Category.IO,
                           Parameters: "None",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "SYST:BEEP" ),

        // ===== Memory Commands =====

        new Command_Entry( Command: "*SAV",
                           Syntax: "*SAV <register>",
                           Description: "Save current instrument state to memory register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*SAV 1" ),

        new Command_Entry( Command: "*RCL",
                           Syntax: "*RCL <register>",
                           Description: "Recall instrument state from memory register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "*RCL 1" ),

        // ===== Calibration Commands =====

        new Command_Entry( Command: "CAL:SEC:STAT",
                           Syntax: "CALibration:SECure:STATe <mode>,<code>",
                           Description: "Enable or disable calibration security",
                           Category: Command_Category.Calibration,
                           Parameters: "mode: ON | OFF, code: security code string",
                           Query_Form: "CAL:SEC:STAT?",
                           Default_Value: "ON",
                           Example: "CAL:SEC:STAT OFF,HP04263" ),

        new Command_Entry( Command: "CAL:COUN?",
                           Syntax: "CALibration:COUNt?",
                           Description: "Query the number of times the instrument has been calibrated",
                           Category: Command_Category.Calibration,
                           Parameters: "None (read-only)",
                           Query_Form: "CAL:COUN?",
                           Default_Value: "N/A",
                           Example: "CAL:COUN?" ),

        new Command_Entry( Command: "CAL:STR",
                           Syntax: "CALibration:STRing <message>",
                           Description: "Store a calibration message string",
                           Category: Command_Category.Calibration,
                           Parameters: "message: up to 40 characters in double quotes",
                           Query_Form: "CAL:STR?",
                           Default_Value: "N/A",
                           Example: "CAL:STR \"Cal 2026-01-15\"" ),
      };

      Commands.Sort( ( A, B ) => string.Compare( A.Command, B.Command, StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
