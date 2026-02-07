// ============================================================================
// File:        HP34401A_Command_Dictionary.cs
// Project:     Keysight 3458A Multimeter Controller
// Description: Command reference dictionary for the HP / Agilent / Keysight
//              34401A 6.5-digit digital multimeter. Uses standard SCPI
//              command syntax.
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

namespace Multimeter_Controller
{
  public static class HP34401A_Command_Dictionary
  {
    public static List<Command_Entry> Get_All_Commands ( )
    {
      var Commands = new List<Command_Entry>
      {
        // ===== Measurement Commands =====
        new Command_Entry (
          "MEAS:VOLT:DC?",
          "MEASure:VOLTage:DC? [<range>[,<resolution>]]",
          "Measure DC voltage and return reading",
          Command_Category.Measurement,
          "range: 0.1|1|10|100|1000|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          "MEAS:VOLT:DC?",
          "AUTO range, default resolution",
          "MEAS:VOLT:DC? 10,0.001" ),

        new Command_Entry (
          "MEAS:VOLT:AC?",
          "MEASure:VOLTage:AC? [<range>[,<resolution>]]",
          "Measure AC voltage (RMS) and return reading",
          Command_Category.Measurement,
          "range: 0.1|1|10|100|750|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          "MEAS:VOLT:AC?",
          "AUTO range, default resolution",
          "MEAS:VOLT:AC? 10,0.001" ),

        new Command_Entry (
          "MEAS:CURR:DC?",
          "MEASure:CURRent:DC? [<range>[,<resolution>]]",
          "Measure DC current and return reading",
          Command_Category.Measurement,
          "range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          "MEAS:CURR:DC?",
          "AUTO range, default resolution",
          "MEAS:CURR:DC? 1,0.0001" ),

        new Command_Entry (
          "MEAS:CURR:AC?",
          "MEASure:CURRent:AC? [<range>[,<resolution>]]",
          "Measure AC current (RMS) and return reading",
          Command_Category.Measurement,
          "range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          "MEAS:CURR:AC?",
          "AUTO range, default resolution",
          "MEAS:CURR:AC? 1,0.001" ),

        new Command_Entry (
          "MEAS:RES?",
          "MEASure:RESistance? [<range>[,<resolution>]]",
          "Measure 2-wire resistance and return reading",
          Command_Category.Measurement,
          "range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          "MEAS:RES?",
          "AUTO range, default resolution",
          "MEAS:RES? 1e3,0.1" ),

        new Command_Entry (
          "MEAS:FRES?",
          "MEASure:FRESistance? [<range>[,<resolution>]]",
          "Measure 4-wire resistance and return reading",
          Command_Category.Measurement,
          "range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          "MEAS:FRES?",
          "AUTO range, default resolution",
          "MEAS:FRES? 1e3,0.1" ),

        new Command_Entry (
          "MEAS:FREQ?",
          "MEASure:FREQuency? [<range>[,<resolution>]]",
          "Measure frequency and return reading",
          Command_Category.Measurement,
          "range: signal voltage range, resolution: MIN|MAX|DEF",
          "MEAS:FREQ?",
          "AUTO range, default resolution",
          "MEAS:FREQ? 1,MIN" ),

        new Command_Entry (
          "MEAS:PER?",
          "MEASure:PERiod? [<range>[,<resolution>]]",
          "Measure period and return reading",
          Command_Category.Measurement,
          "range: signal voltage range, resolution: MIN|MAX|DEF",
          "MEAS:PER?",
          "AUTO range, default resolution",
          "MEAS:PER? 1,MIN" ),

        new Command_Entry (
          "MEAS:CONT?",
          "MEASure:CONTinuity?",
          "Measure continuity and return reading (fixed 1 kOhm range)",
          Command_Category.Measurement,
          "None (fixed range, fixed 5.5 digit resolution)",
          "MEAS:CONT?",
          "1 kOhm range",
          "MEAS:CONT?" ),

        new Command_Entry (
          "MEAS:DIOD?",
          "MEASure:DIODe?",
          "Measure diode forward voltage and return reading",
          Command_Category.Measurement,
          "None (fixed 1 VDC range, 1 mA test current)",
          "MEAS:DIOD?",
          "1 VDC range",
          "MEAS:DIOD?" ),

        new Command_Entry (
          "READ?",
          "READ?",
          "Initiate a measurement and return the reading",
          Command_Category.Measurement,
          "None (uses current configuration)",
          "READ?",
          "N/A",
          "READ?" ),

        new Command_Entry (
          "FETCH?",
          "FETCh?",
          "Return the last reading without triggering a new measurement",
          Command_Category.Measurement,
          "None",
          "FETCH?",
          "N/A",
          "FETCH?" ),

        // ===== Configuration Commands =====
        new Command_Entry (
          "CONF:VOLT:DC",
          "CONFigure:VOLTage:DC [<range>[,<resolution>]]",
          "Configure DC voltage measurement (does not trigger)",
          Command_Category.Configuration,
          "range: 0.1|1|10|100|1000|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          "CONF:VOLT:DC?",
          "AUTO range, default resolution",
          "CONF:VOLT:DC 10,0.001" ),

        new Command_Entry (
          "CONF:VOLT:AC",
          "CONFigure:VOLTage:AC [<range>[,<resolution>]]",
          "Configure AC voltage measurement (does not trigger)",
          Command_Category.Configuration,
          "range: 0.1|1|10|100|750|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          "CONF:VOLT:AC?",
          "AUTO range, default resolution",
          "CONF:VOLT:AC 10" ),

        new Command_Entry (
          "CONF:CURR:DC",
          "CONFigure:CURRent:DC [<range>[,<resolution>]]",
          "Configure DC current measurement (does not trigger)",
          Command_Category.Configuration,
          "range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          "CONF:CURR:DC?",
          "AUTO range, default resolution",
          "CONF:CURR:DC 1" ),

        new Command_Entry (
          "CONF:CURR:AC",
          "CONFigure:CURRent:AC [<range>[,<resolution>]]",
          "Configure AC current measurement (does not trigger)",
          Command_Category.Configuration,
          "range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          "CONF:CURR:AC?",
          "AUTO range, default resolution",
          "CONF:CURR:AC 1" ),

        new Command_Entry (
          "CONF:RES",
          "CONFigure:RESistance [<range>[,<resolution>]]",
          "Configure 2-wire resistance measurement (does not trigger)",
          Command_Category.Configuration,
          "range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          "CONF:RES?",
          "AUTO range, default resolution",
          "CONF:RES 1e3" ),

        new Command_Entry (
          "CONF:FRES",
          "CONFigure:FRESistance [<range>[,<resolution>]]",
          "Configure 4-wire resistance measurement (does not trigger)",
          Command_Category.Configuration,
          "range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          "CONF:FRES?",
          "AUTO range, default resolution",
          "CONF:FRES 1e3" ),

        new Command_Entry (
          "CONF:FREQ",
          "CONFigure:FREQuency [<range>[,<resolution>]]",
          "Configure frequency measurement (does not trigger)",
          Command_Category.Configuration,
          "range: signal voltage range, resolution: MIN|MAX|DEF",
          "CONF:FREQ?",
          "AUTO range, default resolution",
          "CONF:FREQ 1,MIN" ),

        new Command_Entry (
          "CONF:PER",
          "CONFigure:PERiod [<range>[,<resolution>]]",
          "Configure period measurement (does not trigger)",
          Command_Category.Configuration,
          "range: signal voltage range, resolution: MIN|MAX|DEF",
          "CONF:PER?",
          "AUTO range, default resolution",
          "CONF:PER 1,MIN" ),

        new Command_Entry (
          "CONF:CONT",
          "CONFigure:CONTinuity",
          "Configure continuity measurement (does not trigger)",
          Command_Category.Configuration,
          "None (fixed 1 kOhm range)",
          "CONF:CONT?",
          "1 kOhm range",
          "CONF:CONT" ),

        new Command_Entry (
          "CONF:DIOD",
          "CONFigure:DIODe",
          "Configure diode test (does not trigger)",
          Command_Category.Configuration,
          "None (fixed 1 VDC range, 1 mA test current)",
          "CONF:DIOD?",
          "1 VDC range",
          "CONF:DIOD" ),

        new Command_Entry (
          "VOLT:DC:RANG",
          "VOLTage:DC:RANGe <range>",
          "Set DC voltage range",
          Command_Category.Configuration,
          "range: 0.1|1|10|100|1000|MIN|MAX",
          "VOLT:DC:RANG?",
          "AUTO",
          "VOLT:DC:RANG 10" ),

        new Command_Entry (
          "VOLT:DC:RANG:AUTO",
          "VOLTage:DC:RANGe:AUTO <mode>",
          "Enable or disable DC voltage autoranging",
          Command_Category.Configuration,
          "mode: ON|OFF|ONCE",
          "VOLT:DC:RANG:AUTO?",
          "ON",
          "VOLT:DC:RANG:AUTO ON" ),

        new Command_Entry (
          "VOLT:DC:NPLC",
          "VOLTage:DC:NPLCycles <nplc>",
          "Set DC voltage integration time in power line cycles",
          Command_Category.Configuration,
          "nplc: 0.02|0.2|1|10|100|MIN|MAX",
          "VOLT:DC:NPLC?",
          "10",
          "VOLT:DC:NPLC 10" ),

        new Command_Entry (
          "VOLT:DC:RES",
          "VOLTage:DC:RESolution <resolution>",
          "Set DC voltage measurement resolution",
          Command_Category.Configuration,
          "resolution: in volts, MIN|MAX",
          "VOLT:DC:RES?",
          "Depends on range and NPLC",
          "VOLT:DC:RES 0.0001" ),

        new Command_Entry (
          "INP:IMP:AUTO",
          "INPut:IMPedance:AUTO <mode>",
          "Enable or disable high-impedance (>10 GOhm) input for DCV",
          Command_Category.Configuration,
          "mode: ON|OFF (ON = >10 GOhm on 100mV, 1V, 10V ranges)",
          "INP:IMP:AUTO?",
          "OFF (10 MOhm on all ranges)",
          "INP:IMP:AUTO ON" ),

        new Command_Entry (
          "ZERO:AUTO",
          "ZERO:AUTO <mode>",
          "Enable or disable auto-zero",
          Command_Category.Configuration,
          "mode: ON|OFF|ONCE",
          "ZERO:AUTO?",
          "ON",
          "ZERO:AUTO ON" ),

        new Command_Entry (
          "DET:BAND",
          "DETector:BANDwidth <bandwidth>",
          "Set AC signal filter bandwidth",
          Command_Category.Configuration,
          "bandwidth: 3|20|200|MIN|MAX (Hz)",
          "DET:BAND?",
          "20",
          "DET:BAND 20" ),

        // ===== Trigger Commands =====
        new Command_Entry (
          "TRIG:SOUR",
          "TRIGger:SOURce <source>",
          "Select trigger source",
          Command_Category.Trigger,
          "source: IMMediate|BUS|EXTernal",
          "TRIG:SOUR?",
          "IMMediate",
          "TRIG:SOUR BUS" ),

        new Command_Entry (
          "TRIG:DEL",
          "TRIGger:DELay <seconds>",
          "Set trigger delay",
          Command_Category.Trigger,
          "seconds: 0 to 3600|MIN|MAX",
          "TRIG:DEL?",
          "AUTO",
          "TRIG:DEL 0.5" ),

        new Command_Entry (
          "TRIG:DEL:AUTO",
          "TRIGger:DELay:AUTO <mode>",
          "Enable or disable automatic trigger delay",
          Command_Category.Trigger,
          "mode: ON|OFF",
          "TRIG:DEL:AUTO?",
          "ON",
          "TRIG:DEL:AUTO ON" ),

        new Command_Entry (
          "TRIG:COUN",
          "TRIGger:COUNt <count>",
          "Set number of triggers to accept before returning to idle",
          Command_Category.Trigger,
          "count: 1 to 50000|MIN|MAX|INFinity",
          "TRIG:COUN?",
          "1",
          "TRIG:COUN 10" ),

        new Command_Entry (
          "SAMP:COUN",
          "SAMPle:COUNt <count>",
          "Set number of readings per trigger",
          Command_Category.Trigger,
          "count: 1 to 50000|MIN|MAX",
          "SAMP:COUN?",
          "1",
          "SAMP:COUN 5" ),

        new Command_Entry (
          "INIT",
          "INITiate",
          "Change trigger state from idle to wait-for-trigger",
          Command_Category.Trigger,
          "None",
          "",
          "N/A",
          "INIT" ),

        new Command_Entry (
          "*TRG",
          "*TRG",
          "Send a bus trigger (trigger source must be BUS)",
          Command_Category.Trigger,
          "None (requires TRIG:SOUR BUS)",
          "",
          "N/A",
          "*TRG" ),

        // ===== Math Commands =====
        new Command_Entry (
          "CALC:FUNC",
          "CALCulate:FUNCtion <function>",
          "Select math function",
          Command_Category.Math,
          "function: NULL|DB|DBM|AVERage|MIN|MAX|LIMit",
          "CALC:FUNC?",
          "NULL",
          "CALC:FUNC NULL" ),

        new Command_Entry (
          "CALC:STAT",
          "CALCulate:STATe <mode>",
          "Enable or disable math operations",
          Command_Category.Math,
          "mode: ON|OFF",
          "CALC:STAT?",
          "OFF",
          "CALC:STAT ON" ),

        new Command_Entry (
          "CALC:NULL:OFFS",
          "CALCulate:NULL:OFFSet <value>",
          "Set null (offset) value for null math function",
          Command_Category.Math,
          "value: -1e15 to +1e15|MIN|MAX",
          "CALC:NULL:OFFS?",
          "0",
          "CALC:NULL:OFFS 0.5" ),

        new Command_Entry (
          "CALC:DB:REF",
          "CALCulate:DB:REFerence <value>",
          "Set dB reference value",
          Command_Category.Math,
          "value: -200 to +200 dBm|MIN|MAX",
          "CALC:DB:REF?",
          "0",
          "CALC:DB:REF 1.0" ),

        new Command_Entry (
          "CALC:DBM:REF",
          "CALCulate:DBM:REFerence <impedance>",
          "Set dBm reference impedance",
          Command_Category.Math,
          "impedance: 50 to 8000 ohms|MIN|MAX",
          "CALC:DBM:REF?",
          "600",
          "CALC:DBM:REF 50" ),

        new Command_Entry (
          "CALC:LIM:LOW",
          "CALCulate:LIMit:LOWer <value>",
          "Set lower limit for limit testing",
          Command_Category.Math,
          "value: -1e15 to +1e15|MIN|MAX",
          "CALC:LIM:LOW?",
          "0",
          "CALC:LIM:LOW 0.9" ),

        new Command_Entry (
          "CALC:LIM:UPP",
          "CALCulate:LIMit:UPPer <value>",
          "Set upper limit for limit testing",
          Command_Category.Math,
          "value: -1e15 to +1e15|MIN|MAX",
          "CALC:LIM:UPP?",
          "0",
          "CALC:LIM:UPP 1.1" ),

        new Command_Entry (
          "CALC:AVER:MIN?",
          "CALCulate:AVERage:MINimum?",
          "Query minimum reading from math register",
          Command_Category.Math,
          "None",
          "CALC:AVER:MIN?",
          "N/A",
          "CALC:AVER:MIN?" ),

        new Command_Entry (
          "CALC:AVER:MAX?",
          "CALCulate:AVERage:MAXimum?",
          "Query maximum reading from math register",
          Command_Category.Math,
          "None",
          "CALC:AVER:MAX?",
          "N/A",
          "CALC:AVER:MAX?" ),

        new Command_Entry (
          "CALC:AVER:AVER?",
          "CALCulate:AVERage:AVERage?",
          "Query average of readings from math register",
          Command_Category.Math,
          "None",
          "CALC:AVER:AVER?",
          "N/A",
          "CALC:AVER:AVER?" ),

        new Command_Entry (
          "CALC:AVER:COUN?",
          "CALCulate:AVERage:COUNt?",
          "Query number of readings in math register",
          Command_Category.Math,
          "None",
          "CALC:AVER:COUN?",
          "N/A",
          "CALC:AVER:COUN?" ),

        // ===== System Commands =====
        new Command_Entry (
          "*IDN?",
          "*IDN?",
          "Query instrument identification string",
          Command_Category.System,
          "None",
          "*IDN?",
          "N/A",
          "*IDN?" ),

        new Command_Entry (
          "*RST",
          "*RST",
          "Reset instrument to factory default state",
          Command_Category.System,
          "None",
          "",
          "N/A",
          "*RST" ),

        new Command_Entry (
          "*CLS",
          "*CLS",
          "Clear status registers and error queue",
          Command_Category.System,
          "None",
          "",
          "N/A",
          "*CLS" ),

        new Command_Entry (
          "*OPC?",
          "*OPC?",
          "Query operation complete (returns 1 when all pending operations finish)",
          Command_Category.System,
          "None",
          "*OPC?",
          "N/A",
          "*OPC?" ),

        new Command_Entry (
          "*OPC",
          "*OPC",
          "Set OPC bit in Standard Event register when all pending operations complete",
          Command_Category.System,
          "None",
          "",
          "N/A",
          "*OPC" ),

        new Command_Entry (
          "*TST?",
          "*TST?",
          "Perform self-test and return result (0 = pass)",
          Command_Category.System,
          "None (returns 0 for pass, 1 for fail)",
          "*TST?",
          "N/A",
          "*TST?" ),

        new Command_Entry (
          "SYST:ERR?",
          "SYSTem:ERRor?",
          "Query and clear one error from the error queue",
          Command_Category.System,
          "None (returns error number and message)",
          "SYST:ERR?",
          "N/A",
          "SYST:ERR?" ),

        new Command_Entry (
          "SYST:VERS?",
          "SYSTem:VERSion?",
          "Query SCPI version supported by instrument",
          Command_Category.System,
          "None",
          "SYST:VERS?",
          "N/A",
          "SYST:VERS?" ),

        new Command_Entry (
          "*STB?",
          "*STB?",
          "Query status byte register",
          Command_Category.System,
          "None (returns decimal value of status byte)",
          "*STB?",
          "N/A",
          "*STB?" ),

        new Command_Entry (
          "*SRE",
          "*SRE <value>",
          "Set Service Request Enable register",
          Command_Category.System,
          "value: 0-255 (bit mask)",
          "*SRE?",
          "0",
          "*SRE 32" ),

        new Command_Entry (
          "*ESE",
          "*ESE <value>",
          "Set Standard Event Status Enable register",
          Command_Category.System,
          "value: 0-255 (bit mask)",
          "*ESE?",
          "0",
          "*ESE 1" ),

        new Command_Entry (
          "*ESR?",
          "*ESR?",
          "Query and clear Standard Event Status register",
          Command_Category.System,
          "None (returns decimal value of register)",
          "*ESR?",
          "N/A",
          "*ESR?" ),

        // ===== IO Commands =====
        new Command_Entry (
          "DISP",
          "DISPlay <mode>",
          "Enable or disable front panel display",
          Command_Category.IO,
          "mode: ON|OFF",
          "DISP?",
          "ON",
          "DISP ON" ),

        new Command_Entry (
          "DISP:TEXT",
          "DISPlay:TEXT <string>",
          "Display a text message on the front panel (max 12 chars)",
          Command_Category.IO,
          "string: up to 12 characters in quotes",
          "DISP:TEXT?",
          "N/A",
          "DISP:TEXT \"HELLO\"" ),

        new Command_Entry (
          "DISP:TEXT:CLE",
          "DISPlay:TEXT:CLEar",
          "Clear the displayed text message",
          Command_Category.IO,
          "None",
          "",
          "N/A",
          "DISP:TEXT:CLE" ),

        new Command_Entry (
          "SYST:BEEP",
          "SYSTem:BEEPer",
          "Issue a single beep from the front panel",
          Command_Category.IO,
          "None",
          "",
          "N/A",
          "SYST:BEEP" ),

        new Command_Entry (
          "SYST:BEEP:STAT",
          "SYSTem:BEEPer:STATe <mode>",
          "Enable or disable beeper for limit test and continuity",
          Command_Category.IO,
          "mode: ON|OFF",
          "SYST:BEEP:STAT?",
          "ON",
          "SYST:BEEP:STAT ON" ),

        new Command_Entry (
          "ROUT:TERM?",
          "ROUTe:TERMinals?",
          "Query which terminal set is active (front or rear)",
          Command_Category.IO,
          "None (returns FRON or REAR)",
          "ROUT:TERM?",
          "N/A",
          "ROUT:TERM?" ),

        new Command_Entry (
          "FORM",
          "FORMat:DATA <type>",
          "Set data output format",
          Command_Category.IO,
          "type: ASCii|REAL,32|REAL,64",
          "FORM?",
          "ASCii",
          "FORM ASC" ),

        // ===== Memory / Data Commands =====
        new Command_Entry (
          "DATA:POIN?",
          "DATA:POINts?",
          "Query number of readings stored in internal memory",
          Command_Category.Memory,
          "None",
          "DATA:POIN?",
          "N/A",
          "DATA:POIN?" ),

        new Command_Entry (
          "DATA:COUN?",
          "DATA:COUNt?",
          "Query number of readings stored in internal memory",
          Command_Category.Memory,
          "None",
          "DATA:COUN?",
          "N/A",
          "DATA:COUN?" ),

        new Command_Entry (
          "DATA:FEED",
          "DATA:FEED <destination>",
          "Select reading memory destination",
          Command_Category.Memory,
          "destination: RDG_STORE,\"\" (disable) | RDG_STORE,\"CALC\" (enable)",
          "DATA:FEED?",
          "Disabled",
          "DATA:FEED RDG_STORE,\"CALC\"" ),

        new Command_Entry (
          "DATA:DEL?",
          "DATA:DELete?",
          "Remove all readings from memory",
          Command_Category.Memory,
          "None",
          "",
          "N/A",
          "DATA:DEL" ),

        new Command_Entry (
          "MEM:STAT:NAME?",
          "MEMory:STATe:NAME?",
          "Query stored instrument state name",
          Command_Category.Memory,
          "None",
          "MEM:STAT:NAME?",
          "N/A",
          "MEM:STAT:NAME?" ),

        new Command_Entry (
          "*SAV",
          "*SAV <register>",
          "Save current instrument state to memory register",
          Command_Category.Memory,
          "register: 0|1|2",
          "",
          "N/A",
          "*SAV 1" ),

        new Command_Entry (
          "*RCL",
          "*RCL <register>",
          "Recall instrument state from memory register",
          Command_Category.Memory,
          "register: 0|1|2",
          "",
          "N/A",
          "*RCL 1" ),

        // ===== Calibration Commands =====
        new Command_Entry (
          "CAL:SEC:STAT",
          "CALibration:SECure:STATe <mode>,<code>",
          "Enable or disable calibration security",
          Command_Category.Calibration,
          "mode: ON|OFF, code: security code string",
          "CAL:SEC:STAT?",
          "ON (secured)",
          "CAL:SEC:STAT OFF,HP034401" ),

        new Command_Entry (
          "CAL:COUN?",
          "CALibration:COUNt?",
          "Query the number of times the instrument has been calibrated",
          Command_Category.Calibration,
          "None",
          "CAL:COUN?",
          "N/A",
          "CAL:COUN?" ),

        new Command_Entry (
          "CAL:STR",
          "CALibration:STRing <message>",
          "Store a calibration message (max 40 chars)",
          Command_Category.Calibration,
          "message: up to 40 characters in quotes",
          "CAL:STR?",
          "N/A",
          "CAL:STR \"Cal 2026-02-06\"" ),
      };

      Commands.Sort ( ( A, B ) =>
        string.Compare ( A.Command, B.Command,
          StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
