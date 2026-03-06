using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// File:        HP34420A_Command_Dictionary_Class.cs
// Project:     HP3458 Multimeter Controller
// Description: Command reference dictionary for the HP / Agilent HP 34420A
//              7.5-digit nano-volt / micro-ohm meter. Uses standard SCPI
//              command syntax.
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

namespace Multimeter_Controller
{
  public static class HP34420_Command_Dictionary_Class
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

      Commands.Sort ( ( A, B ) =>
        string.Compare ( A.Command, B.Command,
          StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
