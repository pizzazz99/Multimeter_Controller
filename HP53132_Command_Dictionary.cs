using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// File:        HP53132A_Command_Dictionary_Class.cs
// Project:     HP3458 Multimeter Controller
// Description: Command reference dictionary for the HP / Agilent HP 53132A
//              225 MHz Universal Counter. Uses standard SCPI command syntax.
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

namespace Multimeter_Controller
{
  public static class HP53132_Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands ( )
    {
      var Commands = new List<Command_Entry>
      {
        // ===== Measurement Commands =====
        new Command_Entry (
          Command:"MEAS:FREQ?",
          Syntax:"MEASure:FREQuency? [<expected>[,<resolution>]][,<channel>]",
          Description:"Measure frequency on specified channel and return reading",
          Category:Command_Category.Measurement,
          Parameters:"expected: frequency hint in Hz|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)|(@3)",
          Query_Form:"MEAS:FREQ?",
          Default_Value:"AUTO, channel 1",
          Example:"MEAS:FREQ? 10E6,1,(@1)" ),

        new Command_Entry (
          Command:"MEAS:FREQ:RAT?",
          Syntax:"MEASure:FREQuency:RATio? [<expected>[,<resolution>]]",
          Description:"Measure frequency ratio of channel 2 / channel 1",
          Category:Command_Category.Measurement,
          Parameters:"expected: ratio hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"MEAS:FREQ:RAT?",
          Default_Value:"AUTO",
          Example:"MEAS:FREQ:RAT?" ),

        new Command_Entry (
          Command:"MEAS:PER?",
          Syntax:"MEASure:PERiod? [<expected>[,<resolution>]][,<channel>]",
          Description:"Measure period on specified channel and return reading",
          Category:Command_Category.Measurement,
          Parameters:"expected: period hint in seconds|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)",
          Query_Form:"MEAS:PER?",
          Default_Value:"AUTO, channel 1",
          Example:"MEAS:PER? 100E-9,1E-12,(@1)" ),

        new Command_Entry (
          Command:"MEAS:PWID?",
          Syntax:"MEASure:PWIDth? [<expected>[,<resolution>]][,<channel>]",
          Description:"Measure positive pulse width and return reading",
          Category:Command_Category.Measurement,
          Parameters:"expected: width hint in seconds|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)",
          Query_Form:"MEAS:PWID?",
          Default_Value:"AUTO, channel 1",
          Example:"MEAS:PWID? (@1)" ),

        new Command_Entry (
          Command:"MEAS:NWID?",
          Syntax:"MEASure:NWIDth? [<expected>[,<resolution>]][,<channel>]",
          Description:"Measure negative pulse width and return reading",
          Category:Command_Category.Measurement,
          Parameters:"expected: width hint in seconds|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)",
          Query_Form:"MEAS:NWID?",
          Default_Value:"AUTO, channel 1",
          Example:"MEAS:NWID? (@1)" ),

        new Command_Entry (
          Command:"MEAS:DCYC?",
          Syntax:"MEASure:DCYCle? [<expected>[,<resolution>]][,<channel>]",
          Description:"Measure duty cycle and return reading",
          Category:Command_Category.Measurement,
          Parameters:"expected: duty cycle hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)",
          Query_Form:"MEAS:DCYC?",
          Default_Value:"AUTO, channel 1",
          Example:"MEAS:DCYC? (@1)" ),

        new Command_Entry (
          Command:"MEAS:TINT?",
          Syntax:"MEASure:TINTerval?",
          Description:"Measure time interval from channel 1 (start) to channel 2 (stop)",
          Category:Command_Category.Measurement,
          Parameters:"None",
          Query_Form:"MEAS:TINT?",
          Default_Value:"N/A",
          Example:"MEAS:TINT?" ),

        new Command_Entry (
          Command:"MEAS:PHAS?",
          Syntax:"MEASure:PHASe?",
          Description:"Measure phase between channel 1 and channel 2 in degrees",
          Category:Command_Category.Measurement,
          Parameters:"None",
          Query_Form:"MEAS:PHAS?",
          Default_Value:"N/A",
          Example:"MEAS:PHAS?" ),

        new Command_Entry (
          Command:"MEAS:TOT:IMM?",
          Syntax:"MEASure:TOTalize:IMMediate?",
          Description:"Return immediate totalize count from channel 1",
          Category:Command_Category.Measurement,
          Parameters:"None",
          Query_Form:"MEAS:TOT:IMM?",
          Default_Value:"N/A",
          Example:"MEAS:TOT:IMM?" ),

        new Command_Entry (
          Command:"MEAS:VOLT:MAX?",
          Syntax:"MEASure:VOLTage:MAXimum? [<channel>]",
          Description:"Measure peak maximum voltage on specified channel",
          Category:Command_Category.Measurement,
          Parameters:"channel: (@1)|(@2)",
          Query_Form:"MEAS:VOLT:MAX?",
          Default_Value:"channel 1",
          Example:"MEAS:VOLT:MAX? (@1)" ),

        new Command_Entry (
          Command:"MEAS:VOLT:MIN?",
          Syntax:"MEASure:VOLTage:MINimum? [<channel>]",
          Description:"Measure peak minimum voltage on specified channel",
          Category:Command_Category.Measurement,
          Parameters:"channel: (@1)|(@2)",
          Query_Form:"MEAS:VOLT:MIN?",
          Default_Value:"channel 1",
          Example:"MEAS:VOLT:MIN? (@1)" ),

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
          Command:"CONF:FREQ",
          Syntax:"CONFigure:FREQuency [<expected>[,<resolution>]][,<channel>]",
          Description:"Configure frequency measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"expected: frequency hint in Hz|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)|(@3)",
          Query_Form:"CONF:FREQ?",
          Default_Value:"AUTO, channel 1",
          Example:"CONF:FREQ 10E6,1,(@1)" ),

        new Command_Entry (
          Command:"CONF:FREQ:RAT",
          Syntax:"CONFigure:FREQuency:RATio [<expected>[,<resolution>]]",
          Description:"Configure frequency ratio measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"expected: ratio hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF",
          Query_Form:"CONF:FREQ:RAT?",
          Default_Value:"AUTO",
          Example:"CONF:FREQ:RAT" ),

        new Command_Entry (
          Command:"CONF:PER",
          Syntax:"CONFigure:PERiod [<expected>[,<resolution>]][,<channel>]",
          Description:"Configure period measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"expected: period hint in seconds|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)",
          Query_Form:"CONF:PER?",
          Default_Value:"AUTO, channel 1",
          Example:"CONF:PER (@1)" ),

        new Command_Entry (
          Command:"CONF:PWID",
          Syntax:"CONFigure:PWIDth [<expected>[,<resolution>]][,<channel>]",
          Description:"Configure positive pulse width measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"expected: width hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)",
          Query_Form:"CONF:PWID?",
          Default_Value:"AUTO, channel 1",
          Example:"CONF:PWID (@1)" ),

        new Command_Entry (
          Command:"CONF:NWID",
          Syntax:"CONFigure:NWIDth [<expected>[,<resolution>]][,<channel>]",
          Description:"Configure negative pulse width measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"expected: width hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)",
          Query_Form:"CONF:NWID?",
          Default_Value:"AUTO, channel 1",
          Example:"CONF:NWID (@1)" ),

        new Command_Entry (
          Command:"CONF:DCYC",
          Syntax:"CONFigure:DCYCle [<expected>[,<resolution>]][,<channel>]",
          Description:"Configure duty cycle measurement (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"expected: duty cycle hint|MIN|MAX|DEF, resolution: MIN|MAX|DEF, channel: (@1)|(@2)",
          Query_Form:"CONF:DCYC?",
          Default_Value:"AUTO, channel 1",
          Example:"CONF:DCYC (@1)" ),

        new Command_Entry (
          Command:"CONF:TINT",
          Syntax:"CONFigure:TINTerval",
          Description:"Configure time interval measurement, ch1=start ch2=stop (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"None",
          Query_Form:"CONF:TINT?",
          Default_Value:"N/A",
          Example:"CONF:TINT" ),

        new Command_Entry (
          Command:"CONF:PHAS",
          Syntax:"CONFigure:PHASe",
          Description:"Configure phase measurement between channel 1 and channel 2 (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"None",
          Query_Form:"CONF:PHAS?",
          Default_Value:"N/A",
          Example:"CONF:PHAS" ),

        new Command_Entry (
          Command:"CONF:TOT:IMM",
          Syntax:"CONFigure:TOTalize:IMMediate",
          Description:"Configure immediate totalize on channel 1 (does not trigger)",
          Category:Command_Category.Configuration,
          Parameters:"None",
          Query_Form:"CONF:TOT:IMM?",
          Default_Value:"N/A",
          Example:"CONF:TOT:IMM" ),

        // ===== Input / Channel Commands =====
        new Command_Entry (
          Command:"INP:COUP",
          Syntax:"INPut[<channel>]:COUPling <coupling>",
          Description:"Set input coupling for specified channel",
          Category:Command_Category.Configuration,
          Parameters:"coupling: AC|DC, channel: 1|2 (default 1)",
          Query_Form:"INP:COUP?",
          Default_Value:"AC",
          Example:"INP1:COUP DC" ),

        new Command_Entry (
          Command:"INP:IMP",
          Syntax:"INPut[<channel>]:IMPedance <impedance>",
          Description:"Set input impedance for specified channel",
          Category:Command_Category.Configuration,
          Parameters:"impedance: 50|1E6 (ohms), channel: 1|2 (default 1)",
          Query_Form:"INP:IMP?",
          Default_Value:"1E6",
          Example:"INP1:IMP 50" ),

        new Command_Entry (
          Command:"INP:LEV",
          Syntax:"INPut[<channel>]:LEVel <level>",
          Description:"Set trigger level voltage for specified channel",
          Category:Command_Category.Configuration,
          Parameters:"level: voltage in volts|MIN|MAX, channel: 1|2 (default 1)",
          Query_Form:"INP:LEV?",
          Default_Value:"0.0",
          Example:"INP1:LEV 1.5" ),

        new Command_Entry (
          Command:"INP:LEV:AUTO",
          Syntax:"INPut[<channel>]:LEVel:AUTO <mode>",
          Description:"Enable or disable automatic trigger level for specified channel",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF, channel: 1|2 (default 1)",
          Query_Form:"INP:LEV:AUTO?",
          Default_Value:"ON",
          Example:"INP1:LEV:AUTO ON" ),

        new Command_Entry (
          Command:"INP:SLOP",
          Syntax:"INPut[<channel>]:SLOPe <slope>",
          Description:"Set trigger slope for specified channel",
          Category:Command_Category.Configuration,
          Parameters:"slope: POSitive|NEGative, channel: 1|2 (default 1)",
          Query_Form:"INP:SLOP?",
          Default_Value:"POSitive",
          Example:"INP1:SLOP POS" ),

        new Command_Entry (
          Command:"INP:FILT",
          Syntax:"INPut[<channel>]:FILTer <mode>",
          Description:"Enable or disable 100 kHz low-pass filter on specified channel",
          Category:Command_Category.Configuration,
          Parameters:"mode: ON|OFF, channel: 1|2 (default 1)",
          Query_Form:"INP:FILT?",
          Default_Value:"OFF",
          Example:"INP1:FILT ON" ),

        new Command_Entry (
          Command:"INP:ATT",
          Syntax:"INPut[<channel>]:ATTenuator <attenuation>",
          Description:"Set input attenuator for specified channel",
          Category:Command_Category.Configuration,
          Parameters:"attenuation: 1|10, channel: 1|2 (default 1)",
          Query_Form:"INP:ATT?",
          Default_Value:"1",
          Example:"INP1:ATT 10" ),

        // ===== Trigger / Gate Commands =====
        new Command_Entry (
          Command:"TRIG:SOUR",
          Syntax:"TRIGger:SOURce <source>",
          Description:"Select trigger source",
          Category:Command_Category.Trigger,
          Parameters:"source: IMMediate|BUS|EXTernal|INTernal2",
          Query_Form:"TRIG:SOUR?",
          Default_Value:"IMMediate",
          Example:"TRIG:SOUR BUS" ),

        new Command_Entry (
          Command:"TRIG:COUN",
          Syntax:"TRIGger:COUNt <count>",
          Description:"Set number of triggers to accept before returning to idle",
          Category:Command_Category.Trigger,
          Parameters:"count: 1 to 1000000|MIN|MAX|INFinity",
          Query_Form:"TRIG:COUN?",
          Default_Value:"1",
          Example:"TRIG:COUN 100" ),

        new Command_Entry (
          Command:"SAMP:COUN",
          Syntax:"SAMPle:COUNt <count>",
          Description:"Set number of readings per trigger",
          Category:Command_Category.Trigger,
          Parameters:"count: 1 to 1000000|MIN|MAX",
          Query_Form:"SAMP:COUN?",
          Default_Value:"1",
          Example:"SAMP:COUN 100" ),

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
          Command:"ABOR",
          Syntax:"ABORt",
          Description:"Abort measurement and return to idle state",
          Category:Command_Category.Trigger,
          Parameters:"None",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"ABOR" ),

        new Command_Entry (
          Command:"*TRG",
          Syntax:"*TRG",
          Description:"Send a bus trigger (trigger source must be BUS)",
          Category:Command_Category.Trigger,
          Parameters:"None (requires TRIG:SOUR BUS)",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"*TRG" ),

        new Command_Entry (
          Command:"FREQ:ARM:STAR:SOUR",
          Syntax:"FREQuency:ARM:STARt:SOURce <source>",
          Description:"Set gate start arming source for frequency measurement",
          Category:Command_Category.Trigger,
          Parameters:"source: IMMediate|EXTernal|TIME|DINTernal",
          Query_Form:"FREQ:ARM:STAR:SOUR?",
          Default_Value:"IMMediate",
          Example:"FREQ:ARM:STAR:SOUR IMM" ),

        new Command_Entry (
          Command:"FREQ:ARM:STOP:SOUR",
          Syntax:"FREQuency:ARM:STOP:SOURce <source>",
          Description:"Set gate stop arming source for frequency measurement",
          Category:Command_Category.Trigger,
          Parameters:"source: TIME|EXTernal|DINTernal",
          Query_Form:"FREQ:ARM:STOP:SOUR?",
          Default_Value:"TIME",
          Example:"FREQ:ARM:STOP:SOUR TIME" ),

        new Command_Entry (
          Command:"FREQ:ARM:STOP:TIM",
          Syntax:"FREQuency:ARM:STOP:TIMe <seconds>",
          Description:"Set gate time for frequency measurement",
          Category:Command_Category.Trigger,
          Parameters:"seconds: 1E-3 to 1000|MIN|MAX",
          Query_Form:"FREQ:ARM:STOP:TIM?",
          Default_Value:"0.1",
          Example:"FREQ:ARM:STOP:TIM 1" ),

        // ===== Math / Statistics Commands =====
        new Command_Entry (
          Command:"CALC:MATH:EXPR",
          Syntax:"CALCulate:MATH:EXPRession <expression>",
          Description:"Set math expression applied to measurements",
          Category:Command_Category.Math,
          Parameters:"expression: quoted string e.g. \"(MEAS1)\" or \"(MEAS1)*1E6\"",
          Query_Form:"CALC:MATH:EXPR?",
          Default_Value:"(MEAS1)",
          Example:"CALC:MATH:EXPR \"(MEAS1)*1E6\"" ),

        new Command_Entry (
          Command:"CALC:MATH:STAT",
          Syntax:"CALCulate:MATH:STATe <mode>",
          Description:"Enable or disable math expression",
          Category:Command_Category.Math,
          Parameters:"mode: ON|OFF",
          Query_Form:"CALC:MATH:STAT?",
          Default_Value:"OFF",
          Example:"CALC:MATH:STAT ON" ),

        new Command_Entry (
          Command:"CALC2:FORM",
          Syntax:"CALCulate2:FORMat <format>",
          Description:"Select statistics function",
          Category:Command_Category.Math,
          Parameters:"format: MEAN|SDEViation|MAX|MIN|PEAK|ALLAN",
          Query_Form:"CALC2:FORM?",
          Default_Value:"MEAN",
          Example:"CALC2:FORM MEAN" ),

        new Command_Entry (
          Command:"CALC2:STAT",
          Syntax:"CALCulate2:STATe <mode>",
          Description:"Enable or disable statistics",
          Category:Command_Category.Math,
          Parameters:"mode: ON|OFF",
          Query_Form:"CALC2:STAT?",
          Default_Value:"OFF",
          Example:"CALC2:STAT ON" ),

        new Command_Entry (
          Command:"CALC2:IMM",
          Syntax:"CALCulate2:IMMediate",
          Description:"Perform the selected statistics calculation immediately",
          Category:Command_Category.Math,
          Parameters:"None",
          Query_Form:"CALC2:IMM?",
          Default_Value:"N/A",
          Example:"CALC2:IMM" ),

        new Command_Entry (
          Command:"CALC3:FORM",
          Syntax:"CALCulate3:FORMat <format>",
          Description:"Select limit check format",
          Category:Command_Category.Math,
          Parameters:"format: LLIM|ULIM",
          Query_Form:"CALC3:FORM?",
          Default_Value:"LLIM",
          Example:"CALC3:FORM LLIM" ),

        new Command_Entry (
          Command:"CALC3:LIM:LOW",
          Syntax:"CALCulate3:LIMit:LOWer <value>",
          Description:"Set lower limit for limit checking",
          Category:Command_Category.Math,
          Parameters:"value: numeric|MIN|MAX",
          Query_Form:"CALC3:LIM:LOW?",
          Default_Value:"0",
          Example:"CALC3:LIM:LOW 9.99E6" ),

        new Command_Entry (
          Command:"CALC3:LIM:UPP",
          Syntax:"CALCulate3:LIMit:UPPer <value>",
          Description:"Set upper limit for limit checking",
          Category:Command_Category.Math,
          Parameters:"value: numeric|MIN|MAX",
          Query_Form:"CALC3:LIM:UPP?",
          Default_Value:"0",
          Example:"CALC3:LIM:UPP 10.01E6" ),

        new Command_Entry (
          Command:"CALC3:STAT",
          Syntax:"CALCulate3:STATe <mode>",
          Description:"Enable or disable limit checking",
          Category:Command_Category.Math,
          Parameters:"mode: ON|OFF",
          Query_Form:"CALC3:STAT?",
          Default_Value:"OFF",
          Example:"CALC3:STAT ON" ),

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
          Parameters:"register: 0 to 9",
          Query_Form:"",
          Default_Value:"N/A",
          Example:"*SAV 1" ),

        new Command_Entry (
          Command:"*RCL",
          Syntax:"*RCL <register>",
          Description:"Recall instrument state from memory register",
          Category:Command_Category.System,
          Parameters:"register: 0 to 9",
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
          Description:"Display a text message on the front panel",
          Category:Command_Category.IO,
          Parameters:"string: up to 12 characters in quotes",
          Query_Form:"DISP:TEXT?",
          Default_Value:"N/A",
          Example:"DISP:TEXT \"COUNTING\"" ),

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
          Command:"FORM",
          Syntax:"FORMat:DATA <type>",
          Description:"Set data output format",
          Category:Command_Category.IO,
          Parameters:"type: ASCii|REAL,32|REAL,64",
          Query_Form:"FORM?",
          Default_Value:"ASCii",
          Example:"FORM ASC" ),

        new Command_Entry (
          Command:"HCOP:SDUMP:DATA?",
          Syntax:"HCOPy:SDUMp:DATA?",
          Description:"Return screen dump data",
          Category:Command_Category.IO,
          Parameters:"None",
          Query_Form:"HCOP:SDUMP:DATA?",
          Default_Value:"N/A",
          Example:"HCOP:SDUMP:DATA?" ),

        // ===== Memory / Data Commands =====
        new Command_Entry (
          Command:"DATA:POIN?",
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
          Example:"DATA:FEED RDG_STORE,\"CALC2\"" ),

        new Command_Entry (
          Command:"DATA:REM?",
          Syntax:"DATA:REMove? <count>",
          Description:"Remove and return specified number of readings from memory",
          Category:Command_Category.Memory,
          Parameters:"count: 1 to DATA:POIN?|MIN|MAX|INFinity",
          Query_Form:"DATA:REM?",
          Default_Value:"N/A",
          Example:"DATA:REM? 10" ),
      };

      Commands.Sort ( ( A, B ) =>
        string.Compare ( A.Command, B.Command,
          StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
