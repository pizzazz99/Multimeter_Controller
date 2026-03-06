// ============================================================================
// File:        HP33120_Command_Dictionary_Class.cs
// Project:     HP3458 Multimeter Controller
// Description: Command reference dictionary for the HP / Agilent 33120A
//              15 MHz Function / Arbitrary Waveform Generator. Uses standard
//              SCPI command syntax.
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

// ============================================================================
// File:        HP33120_Command_Dictionary.cs
// Project:     HP3458 Multimeter Controller
// Description: Command reference dictionary for the HP / Agilent 33120A
//              15 MHz Function / Arbitrary Waveform Generator. Uses standard
//              SCPI command syntax.
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

namespace Multimeter_Controller
{
  public static class HP33120_Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands ( )
    {
      var Commands = new List<Command_Entry>
      {
        // ===== Output Configuration Commands =====
        new Command_Entry (
          Command:"APPLy:SIN",
          Syntax:"APPLy:SINusoid [<freq>[,<amp>[,<offset>]]]",
          Description:"Output a sine wave with specified parameters",
          Category:Command_Category.Configuration,
          Parameters:"freq: 100 uHz to 15 MHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          Query_Form:"APPLy?",
          Default_Value: "1 kHz, 100 mVpp, 0 V",
          Example: "APPLy:SIN 1000,1.0,0" ),

        new Command_Entry (
          Command:"APPLy:SQU",
          Syntax:"APPLy:SQUare [<freq>[,<amp>[,<offset>]]]",
          Description:"Output a square wave with specified parameters",
          Category:Command_Category.Configuration,
          Parameters:"freq: 100 uHz to 15 MHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          Query_Form:"APPLy?",
          Default_Value: "1 kHz, 100 mVpp, 0 V",
          Example: "APPLy:SQU 1000,1.0,0" ),

        new Command_Entry (
          Command:"APPLy:TRI",
          Syntax:"APPLy:TRIangle [<freq>[,<amp>[,<offset>]]]",
          Description:"Output a triangle wave with specified parameters",
          Category:Command_Category.Configuration,
          Parameters:"freq: 100 uHz to 100 kHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          Query_Form:"APPLy?",
          Default_Value: "1 kHz, 100 mVpp, 0 V",
          Example: "APPLy:TRI 1000,1.0,0" ),

        new Command_Entry (
          Command:"APPLy:RAMP",
          Syntax:"APPLy:RAMP [<freq>[,<amp>[,<offset>]]]",
          Description:"Output a ramp wave with specified parameters",
          Category:Command_Category.Configuration,
          Parameters:"freq: 100 uHz to 100 kHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          Query_Form:"APPLy?",
          Default_Value: "1 kHz, 100 mVpp, 0 V",
          Example: "APPLy:RAMP 500,2.0,0" ),

        new Command_Entry (
          Command:"APPLy:NOIS",
          Syntax:"APPLy:NOISe [<amp>[,<offset>]]",
          Description:"Output gaussian white noise",
          Category:Command_Category.Configuration,
          Parameters:"amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          Query_Form:"APPLy?",
          Default_Value: "100 mVpp, 0 V",
          Example: "APPLy:NOIS 1.0,0" ),

        new Command_Entry (
          Command:"APPLy:DC",
          Syntax:"APPLy:DC [<offset>]",
          Description:"Output a DC voltage level",
          Category:Command_Category.Configuration,
          Parameters:"offset: -5 V to +5 V (50 ohm)",
          Query_Form:"APPLy?",
          Default_Value: "0 V",
          Example: "APPLy:DC DEF,DEF,2.5" ),

        new Command_Entry (
          Command:"APPLy:USER",
          Syntax:"APPLy:USER [<freq>[,<amp>[,<offset>]]]",
          Description:"Output the currently selected arbitrary waveform",
          Category:Command_Category.Configuration,
          Parameters:"freq: 100 uHz to 6 MHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          Query_Form:"APPLy?",
          Default_Value: "1 kHz, 100 mVpp, 0 V",
          Example: "APPLy:USER 5000,1.0,0" ),

        new Command_Entry (
          Command:"FUNC",
          Syntax:"FUNCtion:SHAPe <shape>",
          Description:"Select output waveform shape",
          Category:Command_Category.Configuration,
          Parameters:"shape: SINusoid|SQUare|TRIangle|RAMP|NOISe|DC|USER",
          Query_Form:"FUNC?",
          Default_Value: "SIN",
          Example: "FUNC SIN" ),

        new Command_Entry (
          Command:"FREQ",
          Syntax:"FREQuency <frequency>",
          Description:"Set output frequency",
          Category:Command_Category.Configuration,
          Parameters:"frequency: 100 uHz to 15 MHz (waveform dependent)|MIN|MAX",
          Query_Form:"FREQ?",
          Default_Value: "1 kHz",
          Example: "FREQ 10000" ),

        new Command_Entry (
          Command:"VOLT",
          Syntax:"VOLTage <amplitude>",
          Description:"Set output amplitude (peak-to-peak)",
          Category:Command_Category.Configuration,
          Parameters:"amplitude: 10 mVpp to 10 Vpp (50 ohm)|MIN|MAX",
          Query_Form:"VOLT?",
          Default_Value: "100 mVpp",
          Example: "VOLT 1.5" ),

        new Command_Entry (
          Command:"VOLT:OFFS",
          Syntax:"VOLTage:OFFSet <offset>",
          Description:"Set DC offset voltage",
          Category:Command_Category.Configuration,
          Parameters:"offset: depends on amplitude setting|MIN|MAX",
          Query_Form:"VOLT:OFFS?",
          Default_Value: "0 V",
          Example: "VOLT:OFFS 0.5" ),

        new Command_Entry (
          Command:"VOLT:UNIT",
          Syntax:"VOLTage:UNIT <unit>",
          Description:"Set amplitude units",
          Category:Command_Category.Configuration,
          Parameters:"unit: VPP|VRMS|DBM",
          Query_Form:"VOLT:UNIT?",
          Default_Value: "VPP",
          Example: "VOLT:UNIT VPP" ),

        new Command_Entry (
          Command:"FUNC:SQU:DCYC",
          Syntax:"FUNCtion:SQUare:DCYCle <percent>",
          Description:"Set square wave duty cycle",
          Category:Command_Category.Configuration,
          Parameters:"percent: 20% to 80%|MIN|MAX",
          Query_Form:"FUNC:SQU:DCYC?",
          Default_Value: "50%",
          Example: "FUNC:SQU:DCYC 25" ),

        new Command_Entry (
          Command:"OUTP:LOAD",
          Syntax:"OUTPut:LOAD <impedance>",
          Description:"Set expected output termination impedance",
          Category:Command_Category.IO,
          Parameters:"impedance: 1 to 10000 ohms|INFinity|MIN|MAX",
          Query_Form:"OUTP:LOAD?",
          Default_Value: "50 ohm",
          Example: "OUTP:LOAD 50" ),

        new Command_Entry (
          Command:"OUTP",
          Syntax:"OUTPut <state>",
          Description:"Enable or disable the output",
          Category:Command_Category.IO,
          Parameters:"state: ON|OFF",
          Query_Form:"OUTP?",
          Default_Value: "OFF",
          Example: "OUTP ON" ),

        new Command_Entry (
          Command:"OUTP:SYNC",
          Syntax:"OUTPut:SYNC <state>",
          Description:"Enable or disable the Sync output",
          Category:Command_Category.IO,
          Parameters:"state: ON|OFF",
          Query_Form:"OUTP:SYNC?",
          Default_Value: "OFF",
          Example: "OUTP:SYNC ON" ),

        // ===== Modulation Commands =====
        new Command_Entry (
          Command:"AM:STAT",
          Syntax:"AM:STATe <state>",
          Description:"Enable or disable amplitude modulation",
          Category:Command_Category.Configuration,
          Parameters:"state: ON|OFF",
          Query_Form:"AM:STAT?",
          Default_Value: "OFF",
          Example: "AM:STAT ON" ),

        new Command_Entry (
          Command:"AM:DEPT",
          Syntax:"AM:DEPTh <depth>",
          Description:"Set AM modulation depth",
          Category:Command_Category.Configuration,
          Parameters:"depth: 0% to 120%|MIN|MAX",
          Query_Form:"AM:DEPT?",
          Default_Value: "100%",
          Example: "AM:DEPT 80" ),

        new Command_Entry (
          Command:"AM:INT:FUNC",
          Syntax:"AM:INTernal:FUNCtion <shape>",
          Description:"Set AM internal modulating waveform shape",
          Category:Command_Category.Configuration,
          Parameters:"shape: SINusoid|SQUare|TRIangle|RAMP|NOISe|USER",
          Query_Form:"AM:INT:FUNC?",
          Default_Value: "SIN",
          Example: "AM:INT:FUNC SIN" ),

        new Command_Entry (
          Command:"AM:INT:FREQ",
          Syntax:"AM:INTernal:FREQuency <frequency>",
          Description:"Set AM internal modulating frequency",
          Category:Command_Category.Configuration,
          Parameters:"frequency: 10 mHz to 20 kHz|MIN|MAX",
          Query_Form:"AM:INT:FREQ?",
          Default_Value: "100 Hz",
          Example: "AM:INT:FREQ 1000" ),

        new Command_Entry (
          Command:"AM:SOUR",
          Syntax:"AM:SOURce <source>",
          Description:"Select AM modulation source",
          Category:Command_Category.Configuration,
          Parameters:"source: INTernal|EXTernal (external via rear Modulation In)",
          Query_Form:"AM:SOUR?",
          Default_Value: "INT",
          Example: "AM:SOUR INT" ),

        new Command_Entry (
          Command:"FM:STAT",
          Syntax:"FM:STATe <state>",
          Description:"Enable or disable frequency modulation",
          Category:Command_Category.Configuration,
          Parameters:"state: ON|OFF",
          Query_Form:"FM:STAT?",
          Default_Value: "OFF",
          Example: "FM:STAT ON" ),

        new Command_Entry (
          Command:"FM:DEV",
          Syntax:"FM:DEViation <deviation>",
          Description:"Set FM frequency deviation",
          Category:Command_Category.Configuration,
          Parameters:"deviation: in Hz|MIN|MAX (carrier +/- deviation must be in range)",
          Query_Form:"FM:DEV?",
          Default_Value: "100 Hz",
          Example: "FM:DEV 5000" ),

        new Command_Entry (
          Command:"FM:INT:FUNC",
          Syntax:"FM:INTernal:FUNCtion <shape>",
          Description:"Set FM internal modulating waveform shape",
          Category:Command_Category.Configuration,
          Parameters:"shape: SINusoid|SQUare|TRIangle|RAMP|NOISe|USER",
          Query_Form:"FM:INT:FUNC?",
          Default_Value: "SIN",
          Example: "FM:INT:FUNC SIN" ),

        new Command_Entry (
          Command:"FM:INT:FREQ",
          Syntax:"FM:INTernal:FREQuency <frequency>",
          Description:"Set FM internal modulating frequency",
          Category:Command_Category.Configuration,
          Parameters:"frequency: 10 mHz to 20 kHz|MIN|MAX",
          Query_Form:"FM:INT:FREQ?",
          Default_Value: "100 Hz",
          Example: "FM:INT:FREQ 500" ),

        new Command_Entry (
          Command:"FSK:STAT",
          Syntax:"FSKey:STATe <state>",
          Description:"Enable or disable frequency-shift keying",
          Category:Command_Category.Configuration,
          Parameters:"state: ON|OFF",
          Query_Form:"FSK:STAT?",
          Default_Value: "OFF",
          Example: "FSK:STAT ON" ),

        new Command_Entry (
          Command:"FSK:FREQ",
          Syntax:"FSKey:FREQuency <frequency>",
          Description:"Set FSK hop frequency",
          Category:Command_Category.Configuration,
          Parameters:"frequency: 100 uHz to 15 MHz|MIN|MAX",
          Query_Form:"FSK:FREQ?",
          Default_Value: "100 Hz",
          Example: "FSK:FREQ 2000" ),

        new Command_Entry (
          Command:"FSK:INT:RATE",
          Syntax:"FSKey:INTernal:RATE <rate>",
          Description:"Set internal FSK rate",
          Category:Command_Category.Configuration,
          Parameters:"rate: 10 mHz to 50 kHz|MIN|MAX",
          Query_Form:"FSK:INT:RATE?",
          Default_Value: "10 Hz",
          Example: "FSK:INT:RATE 100" ),

        new Command_Entry (
          Command:"FSK:SOUR",
          Syntax:"FSKey:SOURce <source>",
          Description:"Select FSK trigger source",
          Category:Command_Category.Configuration,
          Parameters:"source: INTernal|EXTernal",
          Query_Form:"FSK:SOUR?",
          Default_Value: "INT",
          Example: "FSK:SOUR INT" ),

        // ===== Burst Commands =====
        new Command_Entry (
          Command:"BURS:STAT",
          Syntax:"BURSt:STATe <state>",
          Description:"Enable or disable burst mode",
          Category:Command_Category.Trigger,
          Parameters:"state: ON|OFF",
          Query_Form:"BURS:STAT?",
          Default_Value: "OFF",
          Example: "BURS:STAT ON" ),

        new Command_Entry (
          Command:"BURS:NCYC",
          Syntax:"BURSt:NCYCles <count>",
          Description:"Set number of cycles per burst",
          Category:Command_Category.Trigger,
          Parameters:"count: 1 to 50000|INFinity|MIN|MAX",
          Query_Form:"BURS:NCYC?",
          Default_Value: "1",
          Example: "BURS:NCYC 5" ),

        new Command_Entry (
          Command:"BURS:INT:PER",
          Syntax:"BURSt:INTernal:PERiod <period>",
          Description:"Set internal burst period (time between bursts)",
          Category:Command_Category.Trigger,
          Parameters:"period: 1 us to 500 s|MIN|MAX",
          Query_Form:"BURS:INT:PER?",
          Default_Value: "10 ms",
          Example: "BURS:INT:PER 0.05" ),

        new Command_Entry (
          Command:"BURS:PHAS",
          Syntax:"BURSt:PHASe <degrees>",
          Description:"Set burst starting phase",
          Category:Command_Category.Trigger,
          Parameters:"degrees: -360 to +360|MIN|MAX",
          Query_Form:"BURS:PHAS?",
          Default_Value: "0",
          Example: "BURS:PHAS 90" ),

        new Command_Entry (
          Command:"BURS:SOUR",
          Syntax:"BURSt:SOURce <source>",
          Description:"Select burst trigger source",
          Category:Command_Category.Trigger,
          Parameters:"source: INTernal|EXTernal|BUS",
          Query_Form:"BURS:SOUR?",
          Default_Value: "INT",
          Example: "BURS:SOUR INT" ),

        // ===== Trigger Commands =====
        new Command_Entry (
          Command:"TRIG:SOUR",
          Syntax:"TRIGger:SOURce <source>",
          Description:"Select trigger source for sweep and burst",
          Category:Command_Category.Trigger,
          Parameters:"source: IMMediate|EXTernal|BUS",
          Query_Form:"TRIG:SOUR?",
          Default_Value: "IMM",
          Example: "TRIG:SOUR BUS" ),

        new Command_Entry (
          Command:"TRIG:SLOP",
          Syntax:"TRIGger:SLOPe <edge>",
          Description:"Set external trigger slope",
          Category:Command_Category.Trigger,
          Parameters:"edge: POSitive|NEGative",
          Query_Form:"TRIG:SLOP?",
          Default_Value: "POS",
          Example: "TRIG:SLOP POS" ),

        new Command_Entry (
          Command:"*TRG",
          Syntax:"*TRG",
          Description:"Send a bus trigger (trigger source must be BUS)",
          Category:Command_Category.Trigger,
          Parameters:"None (requires TRIG:SOUR BUS)",
          Query_Form:"",
          Default_Value: "N/A",
          Example: "*TRG" ),

        // ===== Sweep Commands =====
        new Command_Entry (
          Command:"SWE:STAT",
          Syntax:"SWEep:STATe <state>",
          Description:"Enable or disable frequency sweep",
          Category:Command_Category.Configuration,
          Parameters:"state: ON|OFF",
          Query_Form:"SWE:STAT?",
          Default_Value: "OFF",
          Example: "SWE:STAT ON" ),

        new Command_Entry (
          Command:"SWE:SPAC",
          Syntax:"SWEep:SPACing <type>",
          Description:"Set sweep spacing (linear or logarithmic)",
          Category:Command_Category.Configuration,
          Parameters:"type: LINear|LOGarithmic",
          Query_Form:"SWE:SPAC?",
          Default_Value: "LIN",
          Example: "SWE:SPAC LIN" ),

        new Command_Entry (
          Command:"SWE:TIME",
          Syntax:"SWEep:TIME <seconds>",
          Description:"Set sweep time",
          Category:Command_Category.Configuration,
          Parameters:"seconds: 1 ms to 500 s|MIN|MAX",
          Query_Form:"SWE:TIME?",
          Default_Value: "1 s",
          Example: "SWE:TIME 10" ),

        new Command_Entry (
          Command:"FREQ:STAR",
          Syntax:"FREQuency:STARt <frequency>",
          Description:"Set sweep start frequency",
          Category:Command_Category.Configuration,
          Parameters:"frequency: 100 uHz to 15 MHz|MIN|MAX",
          Query_Form:"FREQ:STAR?",
          Default_Value: "100 Hz",
          Example: "FREQ:STAR 100" ),

        new Command_Entry (
          Command:"FREQ:STOP",
          Syntax:"FREQuency:STOP <frequency>",
          Description:"Set sweep stop frequency",
          Category:Command_Category.Configuration,
          Parameters:"frequency: 100 uHz to 15 MHz|MIN|MAX",
          Query_Form:"FREQ:STOP?",
          Default_Value: "1 kHz",
          Example: "FREQ:STOP 10000" ),

        new Command_Entry (
          Command:"MARK:FREQ",
          Syntax:"MARKer:FREQuency <frequency>",
          Description:"Set marker frequency (Sync output goes high at this point)",
          Category:Command_Category.Configuration,
          Parameters:"frequency: start to stop freq|MIN|MAX",
          Query_Form:"MARK:FREQ?",
          Default_Value: "500 Hz",
          Example: "MARK:FREQ 5000" ),

        new Command_Entry (
          Command:"MARK:STAT",
          Syntax:"MARKer:STATe <state>",
          Description:"Enable or disable sweep frequency marker",
          Category:Command_Category.Configuration,
          Parameters:"state: ON|OFF",
          Query_Form:"MARK:STAT?",
          Default_Value: "OFF",
          Example: "MARK:STAT ON" ),

        // ===== Arbitrary Waveform Commands =====
        new Command_Entry (
          Command:"DATA:DAC",
          Syntax:"DATA:DAC VOLATILE,<value>,<value>,...",
          Description:"Download arbitrary waveform data (DAC values -2047 to +2047)",
          Category:Command_Category.Memory,
          Parameters:"DAC values: -2047 to +2047, comma separated, 8 to 16000 points",
          Query_Form:"",
          Default_Value: "N/A",
          Example: "DATA:DAC VOLATILE,2047,0,-2047,0" ),

        new Command_Entry (
          Command:"DATA:COPY",
          Syntax:"DATA:COPY <dest_name>[,VOLATILE]",
          Description:"Copy volatile waveform to non-volatile memory",
          Category:Command_Category.Memory,
          Parameters:"dest_name: up to 8 characters",
          Query_Form:"",
          Default_Value: "N/A",
          Example: "DATA:COPY MY_WAVE" ),

        new Command_Entry (
          Command:"DATA:DEL",
          Syntax:"DATA:DELete <name>",
          Description:"Delete an arbitrary waveform from non-volatile memory",
          Category:Command_Category.Memory,
          Parameters:"name: waveform name or ALL (built-in waveforms cannot be deleted)",
          Query_Form:"",
          Default_Value: "N/A",
          Example: "DATA:DEL MY_WAVE" ),

        new Command_Entry (
          Command:"DATA:CAT?",
          Syntax:"DATA:CATalog?",
          Description:"List all arbitrary waveforms in non-volatile memory",
          Category:Command_Category.Memory,
          Parameters:"None",
          Query_Form:"DATA:CAT?",
          Default_Value: "N/A",
          Example: "DATA:CAT?" ),

        new Command_Entry (
          Command:"DATA:NVOL:FREE?",
          Syntax:"DATA:NVOLatile:FREE?",
          Description:"Query free non-volatile memory for arbitrary waveforms",
          Category:Command_Category.Memory,
          Parameters:"None (returns number of available points)",
          Query_Form:"DATA:NVOL:FREE?",
          Default_Value: "N/A",
          Example: "DATA:NVOL:FREE?" ),

        new Command_Entry (
          Command:"FUNC:USER",
          Syntax:"FUNCtion:USER <name>",
          Description:"Select an arbitrary waveform by name",
          Category:Command_Category.Memory,
          Parameters:"name: built-in or user-defined waveform name",
          Query_Form:"FUNC:USER?",
          Default_Value: "N/A",
          Example: "FUNC:USER MY_WAVE" ),

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
          Query_Form:"",
          Default_Value: "N/A",
          "*CLS" ),

        new Command_Entry (
          Command:"*OPC?",
          Syntax:"*OPC?",
          Description:"Query operation complete (returns 1 when done)",
          Category:Command_Category.System,
          Parameters:"None",
          Query_Form:"*OPC?",
          Default_Value: "N/A",
          Example: "*OPC?" ),

        new Command_Entry (
          Command:"*OPC",
          Syntax:"*OPC",
          Description:"Set OPC bit in Standard Event register when operations complete",
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

        new Command_Entry (
          Command:"*SAV",
          Syntax:"*SAV <register>",
          Description:"Save current instrument state to memory register",
          Category:Command_Category.Memory,
          Parameters:"register: 0|1|2|3",
          Query_Form:"*SAV?",
          Default_Value: "N/A",
          Example: "*SAV 1" ),

        new Command_Entry (
          Command:"*RCL",
          Syntax:"*RCL <register>",
          Description:"Recall instrument state from memory register",
          Category:Command_Category.Memory,
          Parameters:"register: 0|1|2|3",
          Query_Form:"*RCL?",
          Default_Value: "N/A",
          Example: "*RCL 1" ),

        // ===== IO Commands =====
        new Command_Entry (
          Command:"DISP",
          Syntax:"DISPlay <mode>",
          Description:"Enable or disable front panel display",
          Category:Command_Category.IO,
          Parameters:"mode: ON|OFF",
          Query_Form:"DISP?",
          Default_Value: "ON",
          Example: "DISP OFF" ),

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
          Description:"Issue a single beep",
          Category:Command_Category.IO,
          Parameters:"None",
          Query_Form:"SYST:BEEP?",
          Default_Value: "N/A",
          Example: "SYST:BEEP" ),

        // ===== Calibration Commands =====
        new Command_Entry (
          Command:"CAL:SEC:STAT",
          Syntax:"CALibration:SECure:STATe <mode>,<code>",
          Description:"Enable or disable calibration security",
          Category:Command_Category.Calibration,
          Parameters:"mode: ON|OFF, code: security code string",
          Query_Form:"CAL:SEC:STAT?",
          Default_Value: "OFF",
          Example: "CAL:SEC:STAT ON,HP033120" ),

        new Command_Entry (
          Command:"CAL:COUN?",
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
          Example: "CAL:STR \"Cal 2026-02-07\"" ),

        new Command_Entry (
          Command:"PHAS",
          Syntax:"PHASe <degrees>",
          Description:"Set output phase adjustment relative to sync",
          Category:Command_Category.Configuration,
          Parameters:"degrees: -360 to +360|MIN|MAX",
          Query_Form:"PHAS?",
          Default_Value: "0",
          Example: "PHAS 90" ),

        new Command_Entry (
          Command:"PHAS:REF",
          Syntax:"PHASe:REFerence",
          Description:"Set the current phase as zero reference",
          Category:Command_Category.Configuration,
          Parameters:"None",
          Query_Form:"PHAS:REF?",
          Default_Value: "N/A",
          Example: "PHAS:REF" ),
      };

      Commands.Sort ( ( A, B ) =>
        string.Compare ( A.Command, B.Command,
          StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}


