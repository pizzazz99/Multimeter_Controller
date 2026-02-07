// ============================================================================
// File:        HP33120A_Command_Dictionary.cs
// Project:     Keysight 3458A Multimeter Controller
// Description: Command reference dictionary for the HP / Agilent 33120A
//              15 MHz Function / Arbitrary Waveform Generator. Uses standard
//              SCPI command syntax.
//
// Author:       Mike
// Framework:    .NET 9.0, Windows Forms
// ============================================================================

namespace Multimeter_Controller
{
  public static class HP33120A_Command_Dictionary
  {
    public static List<Command_Entry> Get_All_Commands ( )
    {
      var Commands = new List<Command_Entry>
      {
        // ===== Output Configuration Commands =====
        new Command_Entry (
          "APPLy:SIN",
          "APPLy:SINusoid [<freq>[,<amp>[,<offset>]]]",
          "Output a sine wave with specified parameters",
          Command_Category.Configuration,
          "freq: 100 uHz to 15 MHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          "APPLy?",
          "1 kHz, 100 mVpp, 0 V",
          "APPLy:SIN 1000,1.0,0" ),

        new Command_Entry (
          "APPLy:SQU",
          "APPLy:SQUare [<freq>[,<amp>[,<offset>]]]",
          "Output a square wave with specified parameters",
          Command_Category.Configuration,
          "freq: 100 uHz to 15 MHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          "APPLy?",
          "1 kHz, 100 mVpp, 0 V",
          "APPLy:SQU 1000,1.0,0" ),

        new Command_Entry (
          "APPLy:TRI",
          "APPLy:TRIangle [<freq>[,<amp>[,<offset>]]]",
          "Output a triangle wave with specified parameters",
          Command_Category.Configuration,
          "freq: 100 uHz to 100 kHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          "APPLy?",
          "1 kHz, 100 mVpp, 0 V",
          "APPLy:TRI 1000,1.0,0" ),

        new Command_Entry (
          "APPLy:RAMP",
          "APPLy:RAMP [<freq>[,<amp>[,<offset>]]]",
          "Output a ramp wave with specified parameters",
          Command_Category.Configuration,
          "freq: 100 uHz to 100 kHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          "APPLy?",
          "1 kHz, 100 mVpp, 0 V",
          "APPLy:RAMP 500,2.0,0" ),

        new Command_Entry (
          "APPLy:NOIS",
          "APPLy:NOISe [<amp>[,<offset>]]",
          "Output gaussian white noise",
          Command_Category.Configuration,
          "amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          "APPLy?",
          "100 mVpp, 0 V",
          "APPLy:NOIS 1.0,0" ),

        new Command_Entry (
          "APPLy:DC",
          "APPLy:DC [<offset>]",
          "Output a DC voltage level",
          Command_Category.Configuration,
          "offset: -5 V to +5 V (50 ohm)",
          "APPLy?",
          "0 V",
          "APPLy:DC DEF,DEF,2.5" ),

        new Command_Entry (
          "APPLy:USER",
          "APPLy:USER [<freq>[,<amp>[,<offset>]]]",
          "Output the currently selected arbitrary waveform",
          Command_Category.Configuration,
          "freq: 100 uHz to 6 MHz, amp: 10 mVpp to 10 Vpp (50 ohm), offset: volts",
          "APPLy?",
          "1 kHz, 100 mVpp, 0 V",
          "APPLy:USER 5000,1.0,0" ),

        new Command_Entry (
          "FUNC",
          "FUNCtion:SHAPe <shape>",
          "Select output waveform shape",
          Command_Category.Configuration,
          "shape: SINusoid|SQUare|TRIangle|RAMP|NOISe|DC|USER",
          "FUNC?",
          "SIN",
          "FUNC SIN" ),

        new Command_Entry (
          "FREQ",
          "FREQuency <frequency>",
          "Set output frequency",
          Command_Category.Configuration,
          "frequency: 100 uHz to 15 MHz (waveform dependent)|MIN|MAX",
          "FREQ?",
          "1 kHz",
          "FREQ 10000" ),

        new Command_Entry (
          "VOLT",
          "VOLTage <amplitude>",
          "Set output amplitude (peak-to-peak)",
          Command_Category.Configuration,
          "amplitude: 10 mVpp to 10 Vpp (50 ohm)|MIN|MAX",
          "VOLT?",
          "100 mVpp",
          "VOLT 1.5" ),

        new Command_Entry (
          "VOLT:OFFS",
          "VOLTage:OFFSet <offset>",
          "Set DC offset voltage",
          Command_Category.Configuration,
          "offset: depends on amplitude setting|MIN|MAX",
          "VOLT:OFFS?",
          "0 V",
          "VOLT:OFFS 0.5" ),

        new Command_Entry (
          "VOLT:UNIT",
          "VOLTage:UNIT <unit>",
          "Set amplitude units",
          Command_Category.Configuration,
          "unit: VPP|VRMS|DBM",
          "VOLT:UNIT?",
          "VPP",
          "VOLT:UNIT VPP" ),

        new Command_Entry (
          "FUNC:SQU:DCYC",
          "FUNCtion:SQUare:DCYCle <percent>",
          "Set square wave duty cycle",
          Command_Category.Configuration,
          "percent: 20% to 80%|MIN|MAX",
          "FUNC:SQU:DCYC?",
          "50%",
          "FUNC:SQU:DCYC 25" ),

        new Command_Entry (
          "OUTP:LOAD",
          "OUTPut:LOAD <impedance>",
          "Set expected output termination impedance",
          Command_Category.IO,
          "impedance: 1 to 10000 ohms|INFinity|MIN|MAX",
          "OUTP:LOAD?",
          "50 ohm",
          "OUTP:LOAD 50" ),

        new Command_Entry (
          "OUTP",
          "OUTPut <state>",
          "Enable or disable the output",
          Command_Category.IO,
          "state: ON|OFF",
          "OUTP?",
          "OFF",
          "OUTP ON" ),

        new Command_Entry (
          "OUTP:SYNC",
          "OUTPut:SYNC <state>",
          "Enable or disable the Sync output",
          Command_Category.IO,
          "state: ON|OFF",
          "OUTP:SYNC?",
          "ON",
          "OUTP:SYNC ON" ),

        // ===== Modulation Commands =====
        new Command_Entry (
          "AM:STAT",
          "AM:STATe <state>",
          "Enable or disable amplitude modulation",
          Command_Category.Configuration,
          "state: ON|OFF",
          "AM:STAT?",
          "OFF",
          "AM:STAT ON" ),

        new Command_Entry (
          "AM:DEPT",
          "AM:DEPTh <depth>",
          "Set AM modulation depth",
          Command_Category.Configuration,
          "depth: 0% to 120%|MIN|MAX",
          "AM:DEPT?",
          "100%",
          "AM:DEPT 80" ),

        new Command_Entry (
          "AM:INT:FUNC",
          "AM:INTernal:FUNCtion <shape>",
          "Set AM internal modulating waveform shape",
          Command_Category.Configuration,
          "shape: SINusoid|SQUare|TRIangle|RAMP|NOISe|USER",
          "AM:INT:FUNC?",
          "SIN",
          "AM:INT:FUNC SIN" ),

        new Command_Entry (
          "AM:INT:FREQ",
          "AM:INTernal:FREQuency <frequency>",
          "Set AM internal modulating frequency",
          Command_Category.Configuration,
          "frequency: 10 mHz to 20 kHz|MIN|MAX",
          "AM:INT:FREQ?",
          "100 Hz",
          "AM:INT:FREQ 1000" ),

        new Command_Entry (
          "AM:SOUR",
          "AM:SOURce <source>",
          "Select AM modulation source",
          Command_Category.Configuration,
          "source: INTernal|EXTernal (external via rear Modulation In)",
          "AM:SOUR?",
          "INT",
          "AM:SOUR INT" ),

        new Command_Entry (
          "FM:STAT",
          "FM:STATe <state>",
          "Enable or disable frequency modulation",
          Command_Category.Configuration,
          "state: ON|OFF",
          "FM:STAT?",
          "OFF",
          "FM:STAT ON" ),

        new Command_Entry (
          "FM:DEV",
          "FM:DEViation <deviation>",
          "Set FM frequency deviation",
          Command_Category.Configuration,
          "deviation: in Hz|MIN|MAX (carrier +/- deviation must be in range)",
          "FM:DEV?",
          "100 Hz",
          "FM:DEV 5000" ),

        new Command_Entry (
          "FM:INT:FUNC",
          "FM:INTernal:FUNCtion <shape>",
          "Set FM internal modulating waveform shape",
          Command_Category.Configuration,
          "shape: SINusoid|SQUare|TRIangle|RAMP|NOISe|USER",
          "FM:INT:FUNC?",
          "SIN",
          "FM:INT:FUNC SIN" ),

        new Command_Entry (
          "FM:INT:FREQ",
          "FM:INTernal:FREQuency <frequency>",
          "Set FM internal modulating frequency",
          Command_Category.Configuration,
          "frequency: 10 mHz to 20 kHz|MIN|MAX",
          "FM:INT:FREQ?",
          "100 Hz",
          "FM:INT:FREQ 500" ),

        new Command_Entry (
          "FSK:STAT",
          "FSKey:STATe <state>",
          "Enable or disable frequency-shift keying",
          Command_Category.Configuration,
          "state: ON|OFF",
          "FSK:STAT?",
          "OFF",
          "FSK:STAT ON" ),

        new Command_Entry (
          "FSK:FREQ",
          "FSKey:FREQuency <frequency>",
          "Set FSK hop frequency",
          Command_Category.Configuration,
          "frequency: 100 uHz to 15 MHz|MIN|MAX",
          "FSK:FREQ?",
          "100 Hz",
          "FSK:FREQ 2000" ),

        new Command_Entry (
          "FSK:INT:RATE",
          "FSKey:INTernal:RATE <rate>",
          "Set internal FSK rate",
          Command_Category.Configuration,
          "rate: 10 mHz to 50 kHz|MIN|MAX",
          "FSK:INT:RATE?",
          "10 Hz",
          "FSK:INT:RATE 100" ),

        new Command_Entry (
          "FSK:SOUR",
          "FSKey:SOURce <source>",
          "Select FSK trigger source",
          Command_Category.Configuration,
          "source: INTernal|EXTernal",
          "FSK:SOUR?",
          "INT",
          "FSK:SOUR INT" ),

        // ===== Burst Commands =====
        new Command_Entry (
          "BURS:STAT",
          "BURSt:STATe <state>",
          "Enable or disable burst mode",
          Command_Category.Trigger,
          "state: ON|OFF",
          "BURS:STAT?",
          "OFF",
          "BURS:STAT ON" ),

        new Command_Entry (
          "BURS:NCYC",
          "BURSt:NCYCles <count>",
          "Set number of cycles per burst",
          Command_Category.Trigger,
          "count: 1 to 50000|INFinity|MIN|MAX",
          "BURS:NCYC?",
          "1",
          "BURS:NCYC 5" ),

        new Command_Entry (
          "BURS:INT:PER",
          "BURSt:INTernal:PERiod <period>",
          "Set internal burst period (time between bursts)",
          Command_Category.Trigger,
          "period: 1 us to 500 s|MIN|MAX",
          "BURS:INT:PER?",
          "10 ms",
          "BURS:INT:PER 0.05" ),

        new Command_Entry (
          "BURS:PHAS",
          "BURSt:PHASe <degrees>",
          "Set burst starting phase",
          Command_Category.Trigger,
          "degrees: -360 to +360|MIN|MAX",
          "BURS:PHAS?",
          "0",
          "BURS:PHAS 90" ),

        new Command_Entry (
          "BURS:SOUR",
          "BURSt:SOURce <source>",
          "Select burst trigger source",
          Command_Category.Trigger,
          "source: INTernal|EXTernal|BUS",
          "BURS:SOUR?",
          "INT",
          "BURS:SOUR INT" ),

        // ===== Trigger Commands =====
        new Command_Entry (
          "TRIG:SOUR",
          "TRIGger:SOURce <source>",
          "Select trigger source for sweep and burst",
          Command_Category.Trigger,
          "source: IMMediate|EXTernal|BUS",
          "TRIG:SOUR?",
          "IMM",
          "TRIG:SOUR BUS" ),

        new Command_Entry (
          "TRIG:SLOP",
          "TRIGger:SLOPe <edge>",
          "Set external trigger slope",
          Command_Category.Trigger,
          "edge: POSitive|NEGative",
          "TRIG:SLOP?",
          "POS",
          "TRIG:SLOP POS" ),

        new Command_Entry (
          "*TRG",
          "*TRG",
          "Send a bus trigger (trigger source must be BUS)",
          Command_Category.Trigger,
          "None (requires TRIG:SOUR BUS)",
          "",
          "N/A",
          "*TRG" ),

        // ===== Sweep Commands =====
        new Command_Entry (
          "SWE:STAT",
          "SWEep:STATe <state>",
          "Enable or disable frequency sweep",
          Command_Category.Configuration,
          "state: ON|OFF",
          "SWE:STAT?",
          "OFF",
          "SWE:STAT ON" ),

        new Command_Entry (
          "SWE:SPAC",
          "SWEep:SPACing <type>",
          "Set sweep spacing (linear or logarithmic)",
          Command_Category.Configuration,
          "type: LINear|LOGarithmic",
          "SWE:SPAC?",
          "LIN",
          "SWE:SPAC LIN" ),

        new Command_Entry (
          "SWE:TIME",
          "SWEep:TIME <seconds>",
          "Set sweep time",
          Command_Category.Configuration,
          "seconds: 1 ms to 500 s|MIN|MAX",
          "SWE:TIME?",
          "1 s",
          "SWE:TIME 10" ),

        new Command_Entry (
          "FREQ:STAR",
          "FREQuency:STARt <frequency>",
          "Set sweep start frequency",
          Command_Category.Configuration,
          "frequency: 100 uHz to 15 MHz|MIN|MAX",
          "FREQ:STAR?",
          "100 Hz",
          "FREQ:STAR 100" ),

        new Command_Entry (
          "FREQ:STOP",
          "FREQuency:STOP <frequency>",
          "Set sweep stop frequency",
          Command_Category.Configuration,
          "frequency: 100 uHz to 15 MHz|MIN|MAX",
          "FREQ:STOP?",
          "1 kHz",
          "FREQ:STOP 10000" ),

        new Command_Entry (
          "MARK:FREQ",
          "MARKer:FREQuency <frequency>",
          "Set marker frequency (Sync output goes high at this point)",
          Command_Category.Configuration,
          "frequency: start to stop freq|MIN|MAX",
          "MARK:FREQ?",
          "500 Hz",
          "MARK:FREQ 5000" ),

        new Command_Entry (
          "MARK:STAT",
          "MARKer:STATe <state>",
          "Enable or disable sweep frequency marker",
          Command_Category.Configuration,
          "state: ON|OFF",
          "MARK:STAT?",
          "OFF",
          "MARK:STAT ON" ),

        // ===== Arbitrary Waveform Commands =====
        new Command_Entry (
          "DATA:DAC",
          "DATA:DAC VOLATILE,<value>,<value>,...",
          "Download arbitrary waveform data (DAC values -2047 to +2047)",
          Command_Category.Memory,
          "DAC values: -2047 to +2047, comma separated, 8 to 16000 points",
          "",
          "N/A",
          "DATA:DAC VOLATILE,2047,0,-2047,0" ),

        new Command_Entry (
          "DATA:COPY",
          "DATA:COPY <dest_name>[,VOLATILE]",
          "Copy volatile waveform to non-volatile memory",
          Command_Category.Memory,
          "dest_name: up to 8 characters",
          "",
          "N/A",
          "DATA:COPY MY_WAVE" ),

        new Command_Entry (
          "DATA:DEL",
          "DATA:DELete <name>",
          "Delete an arbitrary waveform from non-volatile memory",
          Command_Category.Memory,
          "name: waveform name or ALL (built-in waveforms cannot be deleted)",
          "",
          "N/A",
          "DATA:DEL MY_WAVE" ),

        new Command_Entry (
          "DATA:CAT?",
          "DATA:CATalog?",
          "List all arbitrary waveforms in non-volatile memory",
          Command_Category.Memory,
          "None",
          "DATA:CAT?",
          "N/A",
          "DATA:CAT?" ),

        new Command_Entry (
          "DATA:NVOL:FREE?",
          "DATA:NVOLatile:FREE?",
          "Query free non-volatile memory for arbitrary waveforms",
          Command_Category.Memory,
          "None (returns number of available points)",
          "DATA:NVOL:FREE?",
          "N/A",
          "DATA:NVOL:FREE?" ),

        new Command_Entry (
          "FUNC:USER",
          "FUNCtion:USER <name>",
          "Select an arbitrary waveform by name",
          Command_Category.Memory,
          "name: built-in or user-defined waveform name",
          "FUNC:USER?",
          "N/A",
          "FUNC:USER MY_WAVE" ),

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
          "Query operation complete (returns 1 when done)",
          Command_Category.System,
          "None",
          "*OPC?",
          "N/A",
          "*OPC?" ),

        new Command_Entry (
          "*OPC",
          "*OPC",
          "Set OPC bit in Standard Event register when operations complete",
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

        new Command_Entry (
          "*SAV",
          "*SAV <register>",
          "Save current instrument state to memory register",
          Command_Category.Memory,
          "register: 0|1|2|3",
          "",
          "N/A",
          "*SAV 1" ),

        new Command_Entry (
          "*RCL",
          "*RCL <register>",
          "Recall instrument state from memory register",
          Command_Category.Memory,
          "register: 0|1|2|3",
          "",
          "N/A",
          "*RCL 1" ),

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
          "Issue a single beep",
          Command_Category.IO,
          "None",
          "",
          "N/A",
          "SYST:BEEP" ),

        // ===== Calibration Commands =====
        new Command_Entry (
          "CAL:SEC:STAT",
          "CALibration:SECure:STATe <mode>,<code>",
          "Enable or disable calibration security",
          Command_Category.Calibration,
          "mode: ON|OFF, code: security code string",
          "CAL:SEC:STAT?",
          "ON (secured)",
          "CAL:SEC:STAT OFF,HP033120" ),

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
          "CAL:STR \"Cal 2026-02-07\"" ),

        new Command_Entry (
          "PHAS",
          "PHASe <degrees>",
          "Set output phase adjustment relative to sync",
          Command_Category.Configuration,
          "degrees: -360 to +360|MIN|MAX",
          "PHAS?",
          "0",
          "PHAS 90" ),

        new Command_Entry (
          "PHAS:REF",
          "PHASe:REFerence",
          "Set the current phase as zero reference",
          Command_Category.Configuration,
          "None",
          "",
          "N/A",
          "PHAS:REF" ),
      };

      Commands.Sort ( ( A, B ) =>
        string.Compare ( A.Command, B.Command,
          StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
