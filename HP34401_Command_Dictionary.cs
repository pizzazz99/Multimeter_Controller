// ============================================================================
// File:        HP34401_Command_Dictionary_Class.cs
// Project:     HP3458 Multimeter Controller
// Description: Command reference dictionary for the HP / Agilent / HP
//              34401 6.5-digit digital multimeter. Uses standard SCPI
//              command syntax.
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

namespace Multimeter_Controller
{
  public static class HP34401_Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands ( )
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
          Default_Value: "AUTO range, default resolution",
          Example: "MEAS:VOLT:DC? 10,0.001" ),

        new Command_Entry (
          Command:"MEAS:VOLT:AC?",
          Syntax:"MEASure:VOLTage:AC? [<range>[,<resolution>]]",
          Description:"Measure AC voltage (RMS) and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: 0.1|1|10|100|750|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"MEAS:VOLT:AC?",
          Default_Value: "AUTO range, default resolution",
          Example: "MEAS:VOLT:AC? 10,0.001" ),

        new Command_Entry (
          Command:"MEAS:CURR:DC?",
          Syntax:"MEASure:CURRent:DC? [<range>[,<resolution>]]",
          Description:"Measure DC current and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"MEAS:CURR:DC?",
          Default_Value: "AUTO range, default resolution",
          Example: "MEAS:CURR:DC? 1,0.0001" ),

        new Command_Entry (
          Command:"MEAS:CURR:AC?",
          Syntax:"MEASure:CURRent:AC? [<range>[,<resolution>]]",
          Description:"Measure AC current (RMS) and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"MEAS:CURR:AC?",
          Default_Value: "AUTO range, default resolution",
          Example: "MEAS:CURR:AC? 1,0.001" ),

        new Command_Entry (
          Command:"MEAS:RES?",
          Syntax:"MEASure:RESistance? [<range>[,<resolution>]]",
          Description:"Measure 2-wire resistance and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          Query_Form:"MEAS:RES?",
          Default_Value: "AUTO range, default resolution",
          Example: "MEAS:RES? 1e3,0.1" ),

        new Command_Entry (
          Command:"MEAS:FRES?",
          Syntax:"MEASure:FRESistance? [<range>[,<resolution>]]",
          Description:"Measure 4-wire resistance and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          Query_Form:"MEAS:FRES?",
          Default_Value: "AUTO range, default resolution",
          Example: "MEAS:FRES? 1e3,0.1" ),

        new Command_Entry (
          Command:"MEAS:FREQ?",
          Syntax:"MEASure:FREQuency? [<range>[,<resolution>]]",
          Description:"Measure frequency and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: signal voltage range, resolution: MIN|MAX|DEF",
          Query_Form:"MEAS:FREQ?",
          Default_Value: "AUTO range, default resolution",
          Example: "MEAS:FREQ? 1,MIN" ),

        new Command_Entry (
          Command:"MEAS:PER?",
          Syntax:"MEASure:PERiod? [<range>[,<resolution>]]",
          Description:"Measure period and return reading",
          Category:Command_Category.Measurement,
          Parameters:"range: signal voltage range, resolution: MIN|MAX|DEF",
          Query_Form:"MEAS:PER?",
          Default_Value: "AUTO range, default resolution",
          Example: "MEAS:PER? 1,MIN" ),

        new Command_Entry (
          Command:"MEAS:CONT?",
          Syntax:"MEASure:CONTinuity?",
          Description:"Measure continuity and return reading (fixed 1 kOhm range)",
          Category:Command_Category.Measurement,
          Parameters:"None (fixed range, fixed 5.5 digit resolution)",
          Query_Form:"MEAS:CONT?",
          Default_Value: "1 kOhm range",
          Example: "MEAS:CONT?" ),

        new Command_Entry (
          Command:"MEAS:DIOD?",
          Syntax:"MEASure:DIODe?",
          Description:"Measure diode forward voltage and return reading",
          Category:Command_Category.Measurement,
          Parameters:"None (fixed 1 VDC range, 1 mA test current)",
          Query_Form:"MEAS:DIOD?",
          Default_Value: "1 VDC range",
          Example: "MEAS:DIOD?" ),

        new Command_Entry (
          Command:"READ?",
          Syntax:"READ?",
          Description:"Initiate a measurement and return the reading",
          Category:Command_Category.Measurement,
          Parameters:"None (uses current configuration)",
          Query_Form:"READ?",
          Default_Value: "N/A",
          Example: "READ?" ),

        new Command_Entry (
          Command:"FETCH?",
          Syntax:"FETCH?",
          Description:"Return the last reading without triggering a new measurement",
          Category:Command_Category.Measurement,
          Parameters:"None",
          Query_Form:"FETCH?",
          Default_Value: "N/A",
          Example: "FETCH?" ),

        // ===== Configuration Commands =====
        new Command_Entry (
          Command:"CONF:VOLT:DC",
          Syntax:"CONFigure:VOLTage:DC [<range>[,<resolution>]]",
          Description:"Configure DC voltage measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 0.1|1|10|100|1000|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"CONF:VOLT:DC?",
          Default_Value: "AUTO range, default resolution",
          Example: "CONF:VOLT:DC 10,0.001" ),

        new Command_Entry (
          Command:"CONF:VOLT:AC",
          Syntax:"CONFigure:VOLTage:AC [<range>[,<resolution>]]",
          Description:"Configure AC voltage measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 0.1|1|10|100|750|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"CONF:VOLT:AC?",
          Default_Value: "AUTO range, default resolution",
          Example: "CONF:VOLT:AC 10" ),

        new Command_Entry (
          Command:"CONF:CURR:DC",
          Syntax:"CONFigure:CURRent:DC [<range>[,<resolution>]]",
          Description:"Configure DC current measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"CONF:CURR:DC?",
          Default_Value: "AUTO range, default resolution",
          Example: "CONF:CURR:DC 1" ),

        new Command_Entry (
          Command:"CONF:CURR:AC",
          Syntax:"CONFigure:CURRent:AC [<range>[,<resolution>]]",
          Description:"Configure AC current measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 0.01|0.1|1|3|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"CONF:CURR:AC?",
          Default_Value: "AUTO range, default resolution",
          Example: "CONF:CURR:AC 1" ),

        new Command_Entry (
          Command:"CONF:RES",
          Syntax:"CONFigure:RESistance [<range>[,<resolution>]]",
          Description:"Configure 2-wire resistance measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          Query_Form:"CONF:RES?",
          Default_Value: "AUTO range, default resolution",
          Example: "CONF:RES 1e3" ),

        new Command_Entry (
          Command:"CONF:FRES",
          Syntax:"CONFigure:FRESistance [<range>[,<resolution>]]",
          Description:"Configure 4-wire resistance measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: 100|1e3|10e3|100e3|1e6|10e6|100e6|MIN|MAX|DEF",
          Query_Form:"CONF:FRES?",
          Default_Value: "AUTO range, default resolution",
          Example: "CONF:FRES 1e3" ),

        new Command_Entry (
          Command:"CONF:FREQ",
          Syntax:"CONFigure:FREQuency [<range>[,<resolution>]]",
          Description:"Configure frequency measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: signal voltage range, resolution: MIN|MAX|DEF",
          Query_Form:"CONF:FREQ?",
          Default_Value: "AUTO range, default resolution",
          Example: "CONF:FREQ 1,MIN" ),

        new Command_Entry (
          Command:"CONF:PER",
          Syntax:"CONFigure:PERiod [<range>[,<resolution>]]",
          Description:"Configure period measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"range: signal voltage range, resolution: MIN|MAX|DEF",
          Query_Form:"CONF:PER?",
          Default_Value: "AUTO range, default resolution",
          Example: "CONF:PER 1,MIN" ),

        new Command_Entry (
          Command:"CONF:CONT",
          Syntax:"CONFigure:CONTinuity",
          Description:"Configure continuity measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"None (fixed 1 kOhm range)",
          Query_Form:"CONF:CONT?",
          Default_Value: "1 kOhm range",
          Example: "CONF:CONT" ),

        new Command_Entry (
          Command:"CONF:DIOD",
          Syntax:"CONFigure:DIODe",
          Description:"Configure diode test (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"None (fixed 1 VDC range, 1 mA test current)",
          Query_Form:"CONF:DIOD?",
          Default_Value: "1 VDC range",
          Example: "CONF:DIOD" ),

        new Command_Entry (
          Command:"VOLT:DC:RANG",
          Syntax:"VOLTage:DC:RANGe <range>",
          Description:"Set DC voltage range",
          Category:Command_Category.Configuration,
          Parameters:"range: 0.1|1|10|100|1000|MIN|MAX",
          Query_Form:"VOLT:DC:RANG?",
          Default_Value: "AUTO",
          Example: "VOLT:DC:RANG 10" ),

        new Command_Entry (
          Command:"VOLT:DC:RANG:AUTO",
          Syntax:"VOLTage:DC:RANGe:AUTO <mode>",
          Description:"Enable or disable DC voltage autoranging",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF|ONCE",
          Query_Form:"VOLT:DC:RANG:AUTO?",
          Default_Value: "ON",
          Example: "VOLT:DC:RANG:AUTO ON" ),

        new Command_Entry (
          Command:"VOLT:DC:NPLC",
          Syntax:"VOLTage:DC:NPLCycles <nplc>",
          Description:"Set DC voltage integration time in power line cycles",
          Category:Command_Category.Configuration,
          Parameters:"nplc: 0.02|0.2|1|10|100|MIN|MAX",
          Query_Form:"VOLT:DC:NPLC?",
          Default_Value: "10",
          Example: "VOLT:DC:NPLC 10" ),

        new Command_Entry (
          Command:"VOLT:DC:RES",
          Syntax:"VOLTage:DC:RESolution <resolution>",
          Description:"Set DC voltage measurement resolution",
          Category:Command_Category.Configuration,
          Parameters:"resolution: in volts, MIN|MAX",
          Query_Form:"VOLT:DC:RES?",
          Default_Value: "Depends on range and NPLC",
          Example: "VOLT:DC:RES 0.0001" ),

        new Command_Entry (
          Command:"INP:IMP:AUTO",
          Syntax:"INPut:IMPedance:AUTO <mode>",
          Description:"Enable or disable high-impedance (>10 GOhm) input for DCV",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF (ON = >10 GOhm on 100mV, 1V, 10V ranges)",
          Query_Form:"INP:IMP:AUTO?",
          Default_Value: "OFF (10 MOhm on all ranges)",
          Example: "INP:IMP:AUTO ON" ),

        new Command_Entry (
          Command:"ZERO:AUTO",
          Syntax:"ZERO:AUTO <mode>",
          Description:"Enable or disable auto-zero",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF|ONCE",
          Query_Form:"ZERO:AUTO?",
          Default_Value: "ON",
          Example: "ZERO:AUTO ON" ),

        new Command_Entry (
          Command:"DET:BAND",
          Syntax:"DETector:BANDwidth <bandwidth>",
          Description:"Set AC signal filter bandwidth",
          Category:Command_Category.Configuration,
          Parameters:"bandwidth: 3|20|200|MIN|MAX (Hz)",
          Query_Form:"DET:BAND?",
          Default_Value: "20",
          Example: "DET:BAND 20" ),

        // ===== Trigger Commands =====
        new Command_Entry (
          Command:"TRIG:SOUR",
          Syntax:"TRIGger:SOURce <source>",
          Description:"Select trigger source",
          Category:Command_Category.Trigger,
          Parameters:"source: IMMediate|BUS|EXTernal",
          Query_Form:"TRIG:SOUR?",
          Default_Value: "IMMediate",
          Example: "TRIG:SOUR BUS" ),

        new Command_Entry (
          Command:"TRIG:DEL",
          Syntax:"TRIGger:DELay <seconds>",
          Description:"Set trigger delay",
          Category:Command_Category.Trigger,
          Parameters:"seconds: 0 to 3600|MIN|MAX",
          Query_Form:"TRIG:DEL?",
          Default_Value: "AUTO",
          Example: "TRIG:DEL 0.5" ),

        new Command_Entry (
          Command:"TRIG:DEL:AUTO",
          Syntax:"TRIGger:DELay:AUTO <mode>",
          Description:"Enable or disable automatic trigger delay",
          Category:Command_Category.Trigger,
          Parameters:"mode: ON|OFF",
          Query_Form:"TRIG:DEL:AUTO?",
          Default_Value: "ON",
          Example: "TRIG:DEL:AUTO ON" ),

        new Command_Entry (
          Command:"TRIG:COUN",
          Syntax:"TRIGger:COUNt <count>",
          Description:"Set number of triggers to accept before returning to idle",
          Category:Command_Category.Trigger,
          Parameters:"count: 1 to 50000|MIN|MAX|INFinity",
          Query_Form:"TRIG:COUN?",
          Default_Value: "1",
          Example: "TRIG:COUN 10" ),

        new Command_Entry (
          Command:"SAMP:COUN",
          Syntax:"SAMPle:COUNt <count>",
          Description:"Set number of readings per trigger",
          Category:Command_Category.Trigger,
          Parameters:"count: 1 to 50000|MIN|MAX",
          Query_Form:"SAMP:COUN?",
          Default_Value: "1",
          Example: "SAMP:COUN 5" ),

        new Command_Entry (
          Command:"INIT",
          Syntax:"INITiate",
          Description:"Change trigger state from idle to wait-for-trigger",
          Category:Command_Category.Trigger,
          Parameters:"None",
          Query_Form:"INIT?",
          Default_Value: "N/A",
          Example: "INIT" ),

        new Command_Entry (
          Command:"*TRG",
          Syntax:"*TRG",
          Description:"Send a bus trigger (trigger source must be BUS)",
          Category:Command_Category.Trigger,
          Parameters:"None (requires TRIG:SOUR BUS)",
          Query_Form:"",
          Default_Value: "N/A",
          Example: "*TRG" ),

        // ===== Math Commands =====
        new Command_Entry (
          Command:"CALC:FUNC",
          Syntax:"CALCulate:FUNCtion <function>",
          Description:"Select math function",
          Category:Command_Category.Math,
          Parameters:"function: NULL|DB|DBM|AVERage|MIN|MAX|LIMit",
          Query_Form:"CALC:FUNC?",
          Default_Value: "NULL",
          Example: "CALC:FUNC NULL" ),

        new Command_Entry (
          Command:"CALC:STAT",
          Syntax:"CALCulate:STATe <mode>",
          Description:"Enable or disable math operations",
          Category:Command_Category.Math,
          Parameters:"mode: ON|OFF",
          Query_Form:"CALC:STAT?",
          Default_Value: "OFF",
          Example: "CALC:STAT ON" ),

        new Command_Entry (
          Command:"CALC:NULL:OFFS",
          Syntax:"CALCulate:NULL:OFFSet <value>",
          Description:"Set null (offset) value for null math function",
          Category:Command_Category.Math,
          Parameters:"value: -1e15 to +1e15|MIN|MAX",
          Query_Form:"CALC:NULL:OFFS?",
          Default_Value: "0",
          Example: "CALC:NULL:OFFS 0.5" ),

        new Command_Entry (
          Command:"CALC:DB:REF",
          Syntax:"CALCulate:DB:REFerence <value>",
          Description:"Set dB reference value",
          Category:Command_Category.Math,
          Parameters:"value: -200 to +200 dBm|MIN|MAX",
          Query_Form:"CALC:DB:REF?",
          Default_Value: "0",
          Example: "CALC:DB:REF 1.0" ),

        new Command_Entry (
          Command:"CALC:DBM:REF",
          Syntax:"CALCulate:DBM:REFerence <impedance>",
          Description:"Set dBm reference impedance",
          Category:Command_Category.Math,
          Parameters:"impedance: 50 to 8000 ohms|MIN|MAX",
          Query_Form:"CALC:DBM:REF?",
          Default_Value: "600",
          Example: "CALC:DBM:REF 50" ),

        new Command_Entry (
          Command:"CALC:LIM:LOW",
          Syntax:"CALCulate:LIMit:LOWer <value>",
          Description:"Set lower limit for limit testing",
          Category:Command_Category.Math,
          Parameters:"value: -1e15 to +1e15|MIN|MAX",
          Query_Form:"CALC:LIM:LOW?",
          Default_Value: "0",
          Example: "CALC:LIM:LOW 0.9" ),

        new Command_Entry (
          Command:"CALC:LIM:UPP",
          Syntax:"CALCulate:LIMit:UPPer <value>",
          Description:"Set upper limit for limit testing",
          Category:Command_Category.Math,
          Parameters:"value: -1e15 to +1e15|MIN|MAX",
          Query_Form:"CALC:LIM:UPP?",
          Default_Value: "0",
          Example: "CALC:LIM:UPP 1.1" ),

        new Command_Entry (
          Command:"CALC:AVER:MIN?",
          Syntax:"CALCulate:AVERage:MINimum?",
          Description:"Query minimum reading from math register",
          Category:Command_Category.Math,
          Parameters:"None",
          Query_Form:"CALC:AVER:MIN?",
          Default_Value: "N/A",
          Example: "CALC:AVER:MIN?" ),

        new Command_Entry (
          Command:"CALC:AVER:MAX?",
          Syntax:"CALCulate:AVERage:MAXimum?",
          Description:"Query maximum reading from math register",
          Category:Command_Category.Math,
          Parameters:"None",
          Query_Form:"CALC:AVER:MAX?",
          Default_Value: "N/A",
          Example: "CALC:AVER:MAX?" ),

        new Command_Entry (
          Command:"CALC:AVER:AVER?",
          Syntax:"CALCulate:AVERage:AVERage?",
          Description:"Query average of readings from math register",
          Category:Command_Category.Math,
          Parameters:"None",
          Query_Form:"CALC:AVER:AVER?",
          Default_Value: "N/A",
          Example: "CALC:AVER:AVER?" ),

        new Command_Entry (
          Command:"CALC:AVER:COUN?",
          Syntax:"CALCulate:AVERage:COUNt?",
          Description:"Query number of readings in math register",
          Category:Command_Category.Math,
          Parameters:"None",
          Query_Form:"CALC:AVER:COUN?",
          Default_Value: "N/A",
          Example: "CALC:AVER:COUN?" ),

        // ===== System Commands =====
        new Command_Entry (
          Command:"*IDN?",
          Syntax:"*IDN?",
          Description:"Query instrument identification string",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"*IDN?",
          Default_Value: "N/A",
          Example: "*IDN?" ),

        new Command_Entry (
          Command:"*RST",
          Syntax:"*RST",
          Description:"Reset instrument to factory default state",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"",
          Default_Value: "N/A",
          Example: "*RST" ),

        new Command_Entry (
          Command:"*CLS",
          Syntax:"*CLS",
          Description:"Clear status registers and error queue",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:null,
          Default_Value: "N/A",
          Example: "*CLS" ),

        new Command_Entry (
          Command:"*OPC?",
          Syntax:"*OPC?",
          Description:"Query operation complete (returns 1 when all pending operations finish)",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"*OPC?",
          Default_Value: "N/A",
          Example: "*OPC?" ),

        new Command_Entry (
          Command:"*OPC",
          Syntax:"*OPC",
          Description:"Set OPC bit in Standard Event register when all pending operations complete",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"",
          Default_Value: "N/A",
          Example: "*OPC" ),

        new Command_Entry (
          Command:"*TST?",
          Syntax:"*TST?",
          Description:"Perform self-test and return result (0 = pass)",
          Category:Command_Category.System,
          Parameters:"None (returns 0 for pass, 1 for fail)",
          Query_Form:"*TST?",
          Default_Value: "N/A",
          Example: "*TST?" ),

        new Command_Entry (
          Command:"SYST:ERR?",
          Syntax:"SYSTem:ERRor?",
          Description:"Query and clear one error from the error queue",
          Category:Command_Category.System,
          Parameters:"None (returns error number and message)",
          Query_Form:"SYST:ERR?",
          Default_Value: "N/A",
          Example: "SYST:ERR?" ),


           new Command_Entry (
          Command:"SYST:REM",
          Syntax:"SYSTem:REMote?",
          Description:"Set remote mode",
          Category:Command_Category.System,
          Parameters:"None (returns error number and message)",
          Query_Form:"",
          Default_Value: "N/A",
          Example: "SYST:REM" ),

        new Command_Entry (
          Command:"SYST:VERS?",
          Syntax:"SYSTem:VERSion?",
          Description:"Query SCPI version supported by instrument",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"SYST:VERS?",
          Default_Value: "N/A",
          Example: "SYST:VERS?" ),

        new Command_Entry (
          Command:"*STB?",
          Syntax:"*STB?",
          Description:"Query status byte register",
          Category:Command_Category.System,
          Parameters:"None (returns decimal value of status byte)",
          Query_Form:"*STB?",
          Default_Value: "N/A",
          Example: "*STB?" ),

        new Command_Entry (
          Command:"*SRE",
          Syntax:"*SRE <value>",
          Description:"Set Service Request Enable register",
          Category:Command_Category.System,
          Parameters:"value: 0-255 (bit mask)",
          Query_Form:"*SRE?",
          Default_Value: "0",
          Example: "*SRE 32" ),

        new Command_Entry (
          Command:"*ESE",
          Syntax:"*ESE <value>",
          Description:"Set Standard Event Status Enable register",
          Category:Command_Category.System,
          Parameters:"value: 0-255 (bit mask)",
          Query_Form:"*ESE?",
          Default_Value: "0",
          Example: "*ESE 1" ),

        new Command_Entry (
          Command:"*ESR?",
          Syntax:"*ESR?",
          Description:"Query and clear Standard Event Status register",
          Category:Command_Category.System,
          Parameters:"None (returns decimal value of register)",
          Query_Form:"*ESR?",
          Default_Value: "N/A",
          Example: "*ESR?" ),

        // ===== IO Commands =====
        new Command_Entry (
          Command:"DISP",
          Syntax:"DISPlay <mode>",
          Description:"Enable or disable front panel display",
          Category:Command_Category.IO,
          Parameters:"mode: ON|OFF",
          Query_Form:"DISP?",
          Default_Value: "ON",
          Example: "DISP ON" ),

        new Command_Entry (
          Command:"DISP:TEXT",
          Syntax:"DISPlay:TEXT <string>",
          Description:"Display a text message on the front panel (max 12 chars)",
          Category:Command_Category.IO,
          Parameters:"string: up to 12 characters in quotes",
          Query_Form:"DISP:TEXT?",
          Default_Value: "N/A",
          Example: "DISP:TEXT \"HELLO\"" ),

        new Command_Entry (
          Command:"DISP:TEXT:CLE",
          Syntax:"DISPlay:TEXT:CLEar",
          Description:"Clear the displayed text message",
          Category:Command_Category.IO,
          Parameters:"None",
          Query_Form:"DISP:TEXT:CLE?",
          Default_Value: "N/A",
          Example: "DISP:TEXT:CLE" ),

        new Command_Entry (
          Command:"SYST:BEEP",
          Syntax:"SYSTem:BEEPer",
          Description:"Issue a single beep from the front panel",
          Category:Command_Category.IO,
          Parameters:"None",
          Query_Form: null,
          Default_Value: "N/A",
          Example: "SYST:BEEP" ),

        new Command_Entry (
          Command:"SYST:BEEP:STAT",
          Syntax:"SYSTem:BEEPer:STATe <mode>",
          Description:"Enable or disable beeper for limit test and continuity",
          Category:Command_Category.IO,
          Parameters:"mode: ON|OFF",
          Query_Form: null,
          Default_Value: "ON",
          Example: "SYST:BEEP:STAT ON" ),

        new Command_Entry (
          Command:"ROUT:TERM?",
          Syntax:"ROUTe:TERMinals?",
          Description:"Query which terminal set is active (front or rear)",
          Category:Command_Category.IO,
          Parameters:"None (returns FRON or REAR)",
          Query_Form:"ROUT:TERM?",
          Default_Value: "N/A",
          Example: "ROUT:TERM?" ),

        new Command_Entry (
          Command:"FORM",
          Syntax:"FORMat:DATA <type>",
          Description:"Set data output format",
          Category:Command_Category.IO,
          Parameters:"type: ASCii|REAL,32|REAL,64",
          Query_Form:"FORM?",
          Default_Value: "ASCii",
          Example: "FORM ASC" ),

        // ===== Memory / Data Commands =====
        new Command_Entry (
          Command:"DATA:POINts?",
          Syntax:"DATA:POINts?",
          Description:"Query number of readings stored in internal memory",
          Category:Command_Category.Memory,
          Parameters:"None",
          Query_Form:"DATA:POIN?",
          Default_Value: "N/A",
          Example: "DATA:POIN?" ),

        new Command_Entry (
          Command:"DATA:COUNt?",
          Syntax:"DATA:COUNt?",
          Description:"Query number of readings stored in internal memory",
          Category:Command_Category.Memory,
          Parameters:"None",
          Query_Form:"DATA:COUN?",
          Default_Value: "N/A",
          Example: "DATA:COUN?" ),

        new Command_Entry (
          Command:"DATA:FEED",
          Syntax:"DATA:FEED <destination>",
          Description:"Select reading memory destination",
          Category:Command_Category.Memory,
          Parameters:"destination: RDG_STORE,\"\" (disable) | RDG_STORE,\"CALC\" (enable)",
          Query_Form:"DATA:FEED?",
          Default_Value: "Disabled",
          Example: "DATA:FEED RDG_STORE,\"CALC\"" ),

        new Command_Entry (
          Command:"DATA:DEL?",
          Syntax:"DATA:DELete?",
          Description:"Remove all readings from memory",
          Category:Command_Category.Memory,
          Parameters:"None",
          Query_Form:"DATA:DEL?",
          Default_Value: "N/A",
          Example: "DATA:DEL" ),

        new Command_Entry (
          Command:"MEMory:STATe:NAME?",
          Syntax:"MEMory:STATe:NAME?",
          Description:"Query stored instrument state name",
          Category:Command_Category.Memory,
          Parameters:"None",
          Query_Form:"MEM:STAT:NAME?",
          Default_Value: "N/A",
          Example: "MEM:STAT:NAME?" ),

        new Command_Entry (
          Command:"*SAV",
          Syntax:"*SAV <register>",
          Description:"Save current instrument state to memory register",
          Category:Command_Category.Memory,
          Parameters:"register: 0|1|2",
          Query_Form:"*SAV?",
          Default_Value: "N/A",
          Example: "*SAV 1" ),

        new Command_Entry (
          Command:"*RCL",
          Syntax:"*RCL <register>",
          Description:"Recall instrument state from memory register",
          Category:Command_Category.Memory,
          Parameters:"register: 0|1|2",
          Query_Form:null,
          Default_Value: "N/A",
          Example: "*RCL 1" ),

        // ===== Calibration Commands =====
        new Command_Entry (
          Command:"CAL:SEC:STAT",
          Syntax:"CALibration:SECure:STATe <mode>,<code>",
          Description:"Enable or disable calibration security",
          Category:Command_Category.Calibration,
          Parameters:"mode: ON|OFF, code: security code string",
          Query_Form:"CAL:SEC:STAT?",
          Default_Value: "OFF",
          Example: "CAL:SEC:STAT ON,HP034401" ),

        new Command_Entry (
          Command:"CAL:COUN",
          Syntax:"CALibration:COUNt?",
          Description:"Query the number of times the instrument has been calibrated",
          Category:Command_Category.Calibration,
          Parameters:"None",
          Query_Form:"CAL:COUN?",
          Default_Value: "N/A",
          Example: "CAL:COUN?" ),

        new Command_Entry (
          Command:"CAL:STR",
          Syntax:"CALibration:STRing <message>",
          Description:"Store a calibration message (max 40 chars)",
          Category:Command_Category.Calibration,
          Parameters:"message: up to 40 characters in quotes",
          Query_Form:"CAL:STR?",
          Default_Value: "N/A",
          Example: "CAL:STR \"Cal 2026-02-06\"" ),
      };

      Commands.Sort ( ( A, B ) =>
        string.Compare ( A.Command, B.Command,
          StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
