
// =============================================================================
// FILE:     HP33220_Command_Dictionary_Class.cs
// PROJECT:  Multimeter_Controller
// =============================================================================
//
// DESCRIPTION:
//   Static command dictionary for the HP / Agilent 33220A 20 MHz function /
//   arbitrary waveform generator. Provides a structured, searchable registry
//   of all supported SCPI instrument commands, organized by functional
//   category. Each entry captures the full command syntax, description, valid
//   parameter ranges, query form, factory default value, and a usage example.
//
//   This class is the single source of truth for 33220A command metadata
//   within the Multimeter_Controller namespace. It is designed to support
//   command validation, UI population, documentation generation, and runtime
//   lookup.
//
// -----------------------------------------------------------------------------
// INSTRUMENT:
//   HP / Agilent 33220A Function / Arbitrary Waveform Generator
//   Command Set:  SCPI (Standard Commands for Programmable Instruments)
//   Interface:    GPIB (IEEE-488.2) / USB / RS-232
//   Frequency:    1 µHz to 20 MHz (sine/square), 200 kHz (ramp/triangle),
//                 5 MHz (pulse), 25 MHz max (square at reduced amplitude)
//   Amplitude:    10 mVpp to 10 Vpp into 50 Ohm (20 Vpp into open circuit)
//   Waveforms:    Sine, Square, Ramp, Pulse, Noise, DC, Arbitrary (up to
//                 64K points)
//
// -----------------------------------------------------------------------------
// RELATIONSHIP TO HP 33120A:
//   The 33220A is the direct successor to the 33120A. Key improvements:
//     - Frequency extended from 15 MHz to 20 MHz
//     - Pulse waveform with independent rise/fall/width control added
//     - Arbitrary waveform memory extended to 64K points (vs 16K)
//     - Linear and logarithmic frequency sweep added
//     - Burst mode extended with gated burst capability
//     - AM, FM, PM, FSK, and PWM modulation added
//     - USB interface added alongside GPIB and RS-232
//   The core APPL, FREQ, VOLT, FUNC, OUTP, and TRIG command structure is
//   essentially identical to the 33120A. Existing 33120A programs require
//   only minor changes to run on the 33220A.
//
// -----------------------------------------------------------------------------
// COMMAND SET OVERVIEW:
//
//   APPLy commands are the fastest way to configure and enable output:
//     APPL:SIN <freq>,<amp>,<offset>   Configure and enable sine wave
//     APPL:SQU <freq>,<amp>,<offset>   Configure and enable square wave
//     APPL:RAMP <freq>,<amp>,<offset>  Configure and enable ramp wave
//     APPL:PULS <freq>,<amp>,<offset>  Configure and enable pulse wave
//     APPL:NOIS <amp>,<offset>         Configure and enable noise output
//     APPL:DC <offset>                 Configure and enable DC offset only
//     APPL:USER <freq>,<amp>,<offset>  Configure and enable arbitrary waveform
//
//   Individual function / parameter commands allow finer control without
//   reconfiguring the entire output:
//     FUNC        Select waveform type
//     FREQ        Set frequency
//     VOLT        Set amplitude (Vpp)
//     VOLT:OFFS   Set DC offset
//     VOLT:HIGH / VOLT:LOW   Set high/low voltage levels directly
//
// -----------------------------------------------------------------------------
// COMMAND CATEGORIES:
//
//   Configuration — Waveform type (FUNC), frequency (FREQ / FREQ:MODE),
//                   amplitude (VOLT / VOLT:UNIT / VOLT:OFFS / VOLT:HIGH /
//                   VOLT:LOW / VOLT:RANG:AUTO), output load (OUTP:LOAD),
//                   output polarity (OUTP:POL), output state (OUTP),
//                   APPLy one-shot setup commands, pulse parameters
//                   (FUNC:PULS:WIDT / FUNC:PULS:TRAN / FUNC:PULS:DCYC),
//                   ramp symmetry (FUNC:RAMP:SYMM), square duty cycle
//                   (FUNC:SQU:DCYC), and arbitrary waveform commands
//                   (DATA:ARB, DATA:ARB:DAC, FUNC:ARB, MMEM:STOR:DATA,
//                   MMEM:LOAD:DATA).
//
//   Modulation    — AM (AM:SOUR / AM:INT:FUNC / AM:INT:FREQ / AM:DEPT /
//                   AM:STAT), FM (FM:SOUR / FM:INT:FUNC / FM:INT:FREQ /
//                   FM:DEV / FM:STAT), PM (PM:SOUR / PM:INT:FUNC /
//                   PM:INT:FREQ / PM:DEV / PM:STAT), FSK (FSK:FREQ /
//                   FSK:INT:RATE / FSK:SOUR / FSK:STAT), PWM (PWM:SOUR /
//                   PWM:INT:FUNC / PWM:INT:FREQ / PWM:DEV / PWM:STAT).
//
//   Trigger       — Trigger source (TRIG:SOUR), trigger slope (TRIG:SLOP),
//                   trigger delay (TRIG:DEL), trigger output (OUTP:TRIG /
//                   OUTP:TRIG:SLOP), burst mode (BURS:MODE / BURS:NCYC /
//                   BURS:INT:PER / BURS:PHAS / BURS:STAT), sweep trigger
//                   (SWE:SPAC / SWE:TIME / SWE:HTIM / SWE:STAT), and
//                   bus trigger (*TRG).
//
//   System        — *IDN?, *RST, *CLS, *OPC, *OPC?, *TST?, SYST:ERR?,
//                   SYST:VERS?, *STB?, *SRE, *ESE, *ESR?.
//
//   IO            — Display (DISP / DISP:TEXT / DISP:TEXT:CLE),
//                   beeper (SYST:BEEP / SYST:BEEP:STAT).
//
//   Memory        — *SAV, *RCL, MMEM:STOR:STAT, MMEM:LOAD:STAT.
//
//   Calibration   — CAL:SEC:STAT, CAL:COUN?, CAL:STR.
//
// -----------------------------------------------------------------------------
// POLLING USE CASE — DRIFT MONITORING:
//   To monitor frequency or amplitude drift across multiple 33220A units,
//   poll the following queries at regular intervals:
//     FREQ?         Returns current output frequency in Hz
//     VOLT?         Returns current output amplitude in Vpp (or selected unit)
//     VOLT:OFFS?    Returns current DC offset in volts
//     OUTP?         Returns output state (1 = on, 0 = off)
//   The instrument returns these values in the units currently configured
//   by VOLT:UNIT (VPP | VRMS | DBM).
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
  public static class HP33220_Command_Dictionary_Class
  {
    public static List<Command_Entry> Get_All_Commands()
    {
      var Commands = new List<Command_Entry> {
        // ===== Configuration Commands =====

        // ── APPLy one-shot setup ─────────────────────────────────────────

        new Command_Entry( Command: "APPL:SIN",
                           Syntax: "APPLy:SINusoid [<frequency>[,<amplitude>[,<offset>]]]",
                           Description: "Configure and enable sine wave output in one command",
                           Category: Command_Category.Configuration,
                           Parameters: ( "frequency: 1e-6 to 20e6 Hz | DEF | MIN | MAX, amplitude: 10e-3 " +
                                         "to 10 Vpp | DEF, offset: -5 to +5 V | DEF" ),
                           Query_Form: "APPL?",
                           Default_Value: "1000 Hz, 1 Vpp, 0 V offset",
                           Example: "APPL:SIN 10e6,1.0,0.0" ),

        new Command_Entry( Command: "APPL:SQU",
                           Syntax: "APPLy:SQUare [<frequency>[,<amplitude>[,<offset>]]]",
                           Description: "Configure and enable square wave output in one command",
                           Category: Command_Category.Configuration,
                           Parameters: ( "frequency: 1e-6 to 20e6 Hz | DEF | MIN | MAX, amplitude: 10e-3 " +
                                         "to 10 Vpp | DEF" ),
                           Query_Form: "APPL?",
                           Default_Value: "1000 Hz, 1 Vpp, 0 V offset",
                           Example: "APPL:SQU 1e6,2.0,0.0" ),

        new Command_Entry( Command: "APPL:RAMP",
                           Syntax: "APPLy:RAMP [<frequency>[,<amplitude>[,<offset>]]]",
                           Description: "Configure and enable ramp wave output in one command",
                           Category: Command_Category.Configuration,
                           Parameters: ( "frequency: 1e-6 to 200e3 Hz | DEF | MIN | MAX, amplitude: 10e-3 " +
                                         "to 10 Vpp | DEF" ),
                           Query_Form: "APPL?",
                           Default_Value: "1000 Hz, 1 Vpp, 0 V offset",
                           Example: "APPL:RAMP 1000,1.0,0.0" ),

        new Command_Entry( Command: "APPL:PULS",
                           Syntax: "APPLy:PULSe [<frequency>[,<amplitude>[,<offset>]]]",
                           Description: "Configure and enable pulse wave output in one command",
                           Category: Command_Category.Configuration,
                           Parameters: ( "frequency: 500e-6 to 5e6 Hz | DEF | MIN | MAX, amplitude: 10e-3 " +
                                         "to 10 Vpp | DEF" ),
                           Query_Form: "APPL?",
                           Default_Value: "1000 Hz, 1 Vpp, 0 V offset",
                           Example: "APPL:PULS 1e3,3.3,1.65" ),

        new Command_Entry( Command: "APPL:NOIS",
                           Syntax: "APPLy:NOISe [<amplitude>[,<offset>]]",
                           Description: "Configure and enable white noise output in one command",
                           Category: Command_Category.Configuration,
                           Parameters: "amplitude: 10e-3 to 10 Vpp | DEF, offset: -5 to +5 V | DEF",
                           Query_Form: "APPL?",
                           Default_Value: "1 Vpp, 0 V offset",
                           Example: "APPL:NOIS 1.0,0.0" ),

        new Command_Entry( Command: "APPL:DC",
                           Syntax: "APPLy:DC DEF,DEF,<offset>",
                           Description: "Configure and enable DC output (offset only) in one command",
                           Category: Command_Category.Configuration,
                           Parameters: "offset: -5 to +5 V (into 50 Ohm load)",
                           Query_Form: "APPL?",
                           Default_Value: "0 V",
                           Example: "APPL:DC DEF,DEF,2.5" ),

        new Command_Entry( Command: "APPL:USER",
                           Syntax: "APPLy:USER [<frequency>[,<amplitude>[,<offset>]]]",
                           Description: "Configure and enable arbitrary waveform output in one command",
                           Category: Command_Category.Configuration,
                           Parameters: ( "frequency: 1e-6 to 6e6 Hz | DEF | MIN | MAX, amplitude: 10e-3 to " +
                                         "10 Vpp | DEF" ),
                           Query_Form: "APPL?",
                           Default_Value: "1000 Hz, 1 Vpp, 0 V offset",
                           Example: "APPL:USER 1e3,1.0,0.0" ),

        // ── Waveform function and frequency ──────────────────────────────

        new Command_Entry( Command: "FUNC",
                           Syntax: "FUNCtion <waveform>",
                           Description: "Select output waveform type",
                           Category: Command_Category.Configuration,
                           Parameters: "waveform: SIN | SQU | RAMP | PULS | NOIS | DC | USER",
                           Query_Form: "FUNC?",
                           Default_Value: "SIN",
                           Example: "FUNC SIN" ),

        new Command_Entry( Command: "FREQ",
                           Syntax: "FREQuency <frequency>",
                           Description: "Set output frequency",
                           Category: Command_Category.Configuration,
                           Parameters: ( "frequency: 1e-6 to 20e6 Hz | MIN | MAX (limit depends on " +
                                         "waveform)" ),
                           Query_Form: "FREQ?",
                           Default_Value: "1000",
                           Example: "FREQ 10e6" ),

        new Command_Entry( Command: "FREQ:MODE",
                           Syntax: "FREQuency:MODE <mode>",
                           Description: "Select frequency mode — fixed, sweep, or list",
                           Category: Command_Category.Configuration,
                           Parameters: "mode: CW | FIXed | SWEep | LIST",
                           Query_Form: "FREQ:MODE?",
                           Default_Value: "CW",
                           Example: "FREQ:MODE SWE" ),

        new Command_Entry( Command: "FREQ:STAR",
                           Syntax: "FREQuency:STARt <frequency>",
                           Description: "Set sweep start frequency",
                           Category: Command_Category.Configuration,
                           Parameters: "frequency: 1e-6 to 20e6 Hz | MIN | MAX",
                           Query_Form: "FREQ:STAR?",
                           Default_Value: "100",
                           Example: "FREQ:STAR 100" ),

        new Command_Entry( Command: "FREQ:STOP",
                           Syntax: "FREQuency:STOP <frequency>",
                           Description: "Set sweep stop frequency",
                           Category: Command_Category.Configuration,
                           Parameters: "frequency: 1e-6 to 20e6 Hz | MIN | MAX",
                           Query_Form: "FREQ:STOP?",
                           Default_Value: "10000",
                           Example: "FREQ:STOP 10e3" ),

        new Command_Entry( Command: "FREQ:CENT",
                           Syntax: "FREQuency:CENTer <frequency>",
                           Description: "Set sweep center frequency",
                           Category: Command_Category.Configuration,
                           Parameters: "frequency: 1e-6 to 20e6 Hz | MIN | MAX",
                           Query_Form: "FREQ:CENT?",
                           Default_Value: "550",
                           Example: "FREQ:CENT 5050" ),

        new Command_Entry( Command: "FREQ:SPAN",
                           Syntax: "FREQuency:SPAN <frequency>",
                           Description: "Set sweep frequency span",
                           Category: Command_Category.Configuration,
                           Parameters: "frequency: 0 to 20e6 Hz | MIN | MAX",
                           Query_Form: "FREQ:SPAN?",
                           Default_Value: "9900",
                           Example: "FREQ:SPAN 9900" ),

        // ── Amplitude and offset ─────────────────────────────────────────

        new Command_Entry( Command: "VOLT",
                           Syntax: "VOLTage <amplitude>",
                           Description: "Set output amplitude",
                           Category: Command_Category.Configuration,
                           Parameters: "amplitude: 10e-3 to 10 Vpp (into 50 Ohm) | MIN | MAX",
                           Query_Form: "VOLT?",
                           Default_Value: "1.0",
                           Example: "VOLT 2.0" ),

        new Command_Entry( Command: "VOLT:UNIT",
                           Syntax: "VOLTage:UNIT <unit>",
                           Description: "Select amplitude units for VOLT commands and queries",
                           Category: Command_Category.Configuration,
                           Parameters: "unit: VPP | VRMS | DBM",
                           Query_Form: "VOLT:UNIT?",
                           Default_Value: "VPP",
                           Example: "VOLT:UNIT VPP" ),

        new Command_Entry( Command: "VOLT:OFFS",
                           Syntax: "VOLTage:OFFSet <offset>",
                           Description: "Set DC offset voltage",
                           Category: Command_Category.Configuration,
                           Parameters: ( "offset: -5 to +5 V (into 50 Ohm, limited by amplitude) | MIN | " +
                                         "MAX" ),
                           Query_Form: "VOLT:OFFS?",
                           Default_Value: "0",
                           Example: "VOLT:OFFS 0.5" ),

        new Command_Entry( Command: "VOLT:HIGH",
                           Syntax: "VOLTage:HIGH <voltage>",
                           Description: ( "Set waveform high level directly (alternative to amplitude + " +
                                          "offset)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "voltage: -4.999 to +5 V | MIN | MAX",
                           Query_Form: "VOLT:HIGH?",
                           Default_Value: "0.5",
                           Example: "VOLT:HIGH 3.3" ),

        new Command_Entry( Command: "VOLT:LOW",
                           Syntax: "VOLTage:LOW <voltage>",
                           Description: ( "Set waveform low level directly (alternative to amplitude + " +
                                          "offset)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "voltage: -5 to +4.999 V | MIN | MAX",
                           Query_Form: "VOLT:LOW?",
                           Default_Value: "-0.5",
                           Example: "VOLT:LOW 0.0" ),

        new Command_Entry( Command: "VOLT:RANG:AUTO",
                           Syntax: "VOLTage:RANGe:AUTO <mode>",
                           Description: ( "Enable or disable automatic amplitude ranging (affects " +
                                          "attenuator switching)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "VOLT:RANG:AUTO?",
                           Default_Value: "ON",
                           Example: "VOLT:RANG:AUTO ON" ),

        // ── Output control ───────────────────────────────────────────────

        new Command_Entry( Command: "OUTP",
                           Syntax: "OUTPut <state>",
                           Description: "Enable or disable the front panel output connector",
                           Category: Command_Category.Configuration,
                           Parameters: "state: ON | OFF | 1 | 0",
                           Query_Form: "OUTP?",
                           Default_Value: "OFF",
                           Example: "OUTP ON" ),

        new Command_Entry( Command: "OUTP:LOAD",
                           Syntax: "OUTPut:LOAD <impedance>",
                           Description: ( "Set expected output load impedance (affects displayed " +
                                          "amplitude)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "impedance: 1 to 10000 Ohms | INFinity | MIN | MAX",
                           Query_Form: "OUTP:LOAD?",
                           Default_Value: "50",
                           Example: "OUTP:LOAD 50" ),

        new Command_Entry( Command: "OUTP:POL",
                           Syntax: "OUTPut:POLarity <polarity>",
                           Description: "Set output polarity — normal or inverted",
                           Category: Command_Category.Configuration,
                           Parameters: "polarity: NORM | INV",
                           Query_Form: "OUTP:POL?",
                           Default_Value: "NORM",
                           Example: "OUTP:POL NORM" ),

        // ── Waveform-specific parameters ─────────────────────────────────

        new Command_Entry( Command: "FUNC:SQU:DCYC",
                           Syntax: "FUNCtion:SQUare:DCYCle <percent>",
                           Description: "Set square wave duty cycle",
                           Category: Command_Category.Configuration,
                           Parameters: "percent: 20 to 80 (%) | MIN | MAX (frequency dependent)",
                           Query_Form: "FUNC:SQU:DCYC?",
                           Default_Value: "50",
                           Example: "FUNC:SQU:DCYC 50" ),

        new Command_Entry( Command: "FUNC:RAMP:SYMM",
                           Syntax: "FUNCtion:RAMP:SYMMetry <percent>",
                           Description: ( "Set ramp wave symmetry (0% = sawtooth, 100% = triangle, 50% = " +
                                          "default ramp)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "percent: 0 to 100 (%) | MIN | MAX",
                           Query_Form: "FUNC:RAMP:SYMM?",
                           Default_Value: "100",
                           Example: "FUNC:RAMP:SYMM 50" ),

        new Command_Entry( Command: "FUNC:PULS:WIDT",
                           Syntax: "FUNCtion:PULSe:WIDTh <seconds>",
                           Description: "Set pulse width",
                           Category: Command_Category.Configuration,
                           Parameters: "seconds: 20e-9 to period - 20e-9 | MIN | MAX",
                           Query_Form: "FUNC:PULS:WIDT?",
                           Default_Value: "500e-6",
                           Example: "FUNC:PULS:WIDT 100e-6" ),

        new Command_Entry( Command: "FUNC:PULS:TRAN",
                           Syntax: "FUNCtion:PULSe:TRANsition <seconds>",
                           Description: "Set pulse rise/fall transition time",
                           Category: Command_Category.Configuration,
                           Parameters: "seconds: 5e-9 to 1e-6 | MIN | MAX",
                           Query_Form: "FUNC:PULS:TRAN?",
                           Default_Value: "5e-9",
                           Example: "FUNC:PULS:TRAN 10e-9" ),

        new Command_Entry( Command: "FUNC:PULS:DCYC",
                           Syntax: "FUNCtion:PULSe:DCYCle <percent>",
                           Description: "Set pulse duty cycle (alternative to FUNC:PULS:WIDT)",
                           Category: Command_Category.Configuration,
                           Parameters: "percent: 0 to 100 (%) | MIN | MAX",
                           Query_Form: "FUNC:PULS:DCYC?",
                           Default_Value: "50",
                           Example: "FUNC:PULS:DCYC 25" ),

        // ── Arbitrary waveform ───────────────────────────────────────────

        new Command_Entry( Command: "FUNC:ARB",
                           Syntax: "FUNCtion:ARBitrary <waveform_name>",
                           Description: ( "Select a named arbitrary waveform from memory as the active " +
                                          "USER waveform" ),
                           Category: Command_Category.Configuration,
                           Parameters: ( "waveform_name: name string of a waveform stored in volatile or " +
                                         "non-volatile memory" ),
                           Query_Form: "FUNC:ARB?",
                           Default_Value: "N/A",
                           Example: "FUNC:ARB \"MYWAVE\"" ),

        new Command_Entry( Command: "DATA:ARB",
                           Syntax: "DATA:ARBitrary <waveform_name>,<data_values>",
                           Description: ( "Download floating-point arbitrary waveform data to volatile " +
                                          "memory" ),
                           Category: Command_Category.Configuration,
                           Parameters: ( "waveform_name: string, data_values: comma-separated floats -1.0 " +
                                         "to +1.0 (min 8, max 65536 points)" ),
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "DATA:ARB MYWAVE,0.0,0.5,1.0,0.5,0.0,-0.5,-1.0,-0.5" ),

        new Command_Entry( Command: "DATA:ARB:DAC",
                           Syntax: "DATA:ARBitrary:DAC <waveform_name>,<dac_values>",
                           Description: "Download 16-bit DAC arbitrary waveform data to volatile memory",
                           Category: Command_Category.Configuration,
                           Parameters: ( "waveform_name: string, dac_values: comma-separated integers 0 to " +
                                         "65535" ),
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "DATA:ARB:DAC MYWAVE,32767,49151,65535,49151,32767,16383,0,16383" ),

        new Command_Entry( Command: "MMEM:STOR:DATA",
                           Syntax: "MMEMory:STORe:DATA <waveform_name>,\"<filename>\"",
                           Description: ( "Save arbitrary waveform from volatile memory to non-volatile " +
                                          "file" ),
                           Category: Command_Category.Configuration,
                           Parameters: "waveform_name: string, filename: quoted filename string",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "MMEM:STOR:DATA MYWAVE,\"INT:\\MYWAVE.ARB\"" ),

        new Command_Entry( Command: "MMEM:LOAD:DATA",
                           Syntax: "MMEMory:LOAD:DATA <waveform_name>",
                           Description: ( "Load arbitrary waveform from non-volatile file into volatile " +
                                          "memory" ),
                           Category: Command_Category.Configuration,
                           Parameters: "waveform_name: string (file name without path)",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "MMEM:LOAD:DATA \"INT:\\MYWAVE.ARB\"" ),

        // ===== Modulation Commands =====

        // ── AM modulation ────────────────────────────────────────────────

        new Command_Entry( Command: "AM:STAT",
                           Syntax: "AM:STATe <mode>",
                           Description: "Enable or disable AM modulation",
                           Category: Command_Category.Modulation,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "AM:STAT?",
                           Default_Value: "OFF",
                           Example: "AM:STAT ON" ),

        new Command_Entry( Command: "AM:DEPT",
                           Syntax: "AM:DEPTh <percent>",
                           Description: "Set AM modulation depth",
                           Category: Command_Category.Modulation,
                           Parameters: "percent: 0 to 120 (%) | MIN | MAX",
                           Query_Form: "AM:DEPT?",
                           Default_Value: "100",
                           Example: "AM:DEPT 80" ),

        new Command_Entry( Command: "AM:SOUR",
                           Syntax: "AM:SOURce <source>",
                           Description: "Select AM modulation source",
                           Category: Command_Category.Modulation,
                           Parameters: "source: INT | EXT",
                           Query_Form: "AM:SOUR?",
                           Default_Value: "INT",
                           Example: "AM:SOUR INT" ),

        new Command_Entry( Command: "AM:INT:FUNC",
                           Syntax: "AM:INTernal:FUNCtion <waveform>",
                           Description: "Set internal AM modulating waveform shape",
                           Category: Command_Category.Modulation,
                           Parameters: "waveform: SIN | SQU | RAMP | NRAMP | TRI | NOIS | USER",
                           Query_Form: "AM:INT:FUNC?",
                           Default_Value: "SIN",
                           Example: "AM:INT:FUNC SIN" ),

        new Command_Entry( Command: "AM:INT:FREQ",
                           Syntax: "AM:INTernal:FREQuency <frequency>",
                           Description: "Set internal AM modulating frequency",
                           Category: Command_Category.Modulation,
                           Parameters: "frequency: 2e-3 to 20e3 Hz | MIN | MAX",
                           Query_Form: "AM:INT:FREQ?",
                           Default_Value: "100",
                           Example: "AM:INT:FREQ 1e3" ),

        // ── FM modulation ────────────────────────────────────────────────

        new Command_Entry( Command: "FM:STAT",
                           Syntax: "FM:STATe <mode>",
                           Description: "Enable or disable FM modulation",
                           Category: Command_Category.Modulation,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "FM:STAT?",
                           Default_Value: "OFF",
                           Example: "FM:STAT ON" ),

        new Command_Entry( Command: "FM:DEV",
                           Syntax: "FM:DEViation <deviation>",
                           Description: "Set FM peak frequency deviation",
                           Category: Command_Category.Modulation,
                           Parameters: "deviation: 1e-6 to 10e6 Hz | MIN | MAX (carrier freq dependent)",
                           Query_Form: "FM:DEV?",
                           Default_Value: "100",
                           Example: "FM:DEV 1e3" ),

        new Command_Entry( Command: "FM:SOUR",
                           Syntax: "FM:SOURce <source>",
                           Description: "Select FM modulation source",
                           Category: Command_Category.Modulation,
                           Parameters: "source: INT | EXT",
                           Query_Form: "FM:SOUR?",
                           Default_Value: "INT",
                           Example: "FM:SOUR INT" ),

        new Command_Entry( Command: "FM:INT:FUNC",
                           Syntax: "FM:INTernal:FUNCtion <waveform>",
                           Description: "Set internal FM modulating waveform shape",
                           Category: Command_Category.Modulation,
                           Parameters: "waveform: SIN | SQU | RAMP | NRAMP | TRI | NOIS | USER",
                           Query_Form: "FM:INT:FUNC?",
                           Default_Value: "SIN",
                           Example: "FM:INT:FUNC SIN" ),

        new Command_Entry( Command: "FM:INT:FREQ",
                           Syntax: "FM:INTernal:FREQuency <frequency>",
                           Description: "Set internal FM modulating frequency",
                           Category: Command_Category.Modulation,
                           Parameters: "frequency: 2e-3 to 20e3 Hz | MIN | MAX",
                           Query_Form: "FM:INT:FREQ?",
                           Default_Value: "10",
                           Example: "FM:INT:FREQ 1e3" ),

        // ── PM modulation ────────────────────────────────────────────────

        new Command_Entry( Command: "PM:STAT",
                           Syntax: "PM:STATe <mode>",
                           Description: "Enable or disable PM (phase modulation)",
                           Category: Command_Category.Modulation,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "PM:STAT?",
                           Default_Value: "OFF",
                           Example: "PM:STAT ON" ),

        new Command_Entry( Command: "PM:DEV",
                           Syntax: "PM:DEViation <degrees>",
                           Description: "Set PM peak phase deviation",
                           Category: Command_Category.Modulation,
                           Parameters: "degrees: 0 to 360 | MIN | MAX",
                           Query_Form: "PM:DEV?",
                           Default_Value: "180",
                           Example: "PM:DEV 90" ),

        new Command_Entry( Command: "PM:SOUR",
                           Syntax: "PM:SOURce <source>",
                           Description: "Select PM modulation source",
                           Category: Command_Category.Modulation,
                           Parameters: "source: INT | EXT",
                           Query_Form: "PM:SOUR?",
                           Default_Value: "INT",
                           Example: "PM:SOUR INT" ),

        new Command_Entry( Command: "PM:INT:FUNC",
                           Syntax: "PM:INTernal:FUNCtion <waveform>",
                           Description: "Set internal PM modulating waveform shape",
                           Category: Command_Category.Modulation,
                           Parameters: "waveform: SIN | SQU | RAMP | NRAMP | TRI | NOIS | USER",
                           Query_Form: "PM:INT:FUNC?",
                           Default_Value: "SIN",
                           Example: "PM:INT:FUNC SIN" ),

        new Command_Entry( Command: "PM:INT:FREQ",
                           Syntax: "PM:INTernal:FREQuency <frequency>",
                           Description: "Set internal PM modulating frequency",
                           Category: Command_Category.Modulation,
                           Parameters: "frequency: 2e-3 to 20e3 Hz | MIN | MAX",
                           Query_Form: "PM:INT:FREQ?",
                           Default_Value: "10",
                           Example: "PM:INT:FREQ 1e3" ),

        // ── FSK modulation ───────────────────────────────────────────────

        new Command_Entry( Command: "FSK:STAT",
                           Syntax: "FSKey:STATe <mode>",
                           Description: "Enable or disable FSK (frequency shift keying) modulation",
                           Category: Command_Category.Modulation,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "FSK:STAT?",
                           Default_Value: "OFF",
                           Example: "FSK:STAT ON" ),

        new Command_Entry( Command: "FSK:FREQ",
                           Syntax: "FSKey:FREQuency <frequency>",
                           Description: "Set FSK hop (alternate) frequency",
                           Category: Command_Category.Modulation,
                           Parameters: "frequency: 1e-6 to 20e6 Hz | MIN | MAX",
                           Query_Form: "FSK:FREQ?",
                           Default_Value: "100",
                           Example: "FSK:FREQ 100e3" ),

        new Command_Entry( Command: "FSK:INT:RATE",
                           Syntax: "FSKey:INTernal:RATE <rate>",
                           Description: "Set FSK internal switching rate",
                           Category: Command_Category.Modulation,
                           Parameters: "rate: 2e-3 to 100e3 Hz | MIN | MAX",
                           Query_Form: "FSK:INT:RATE?",
                           Default_Value: "10",
                           Example: "FSK:INT:RATE 1e3" ),

        new Command_Entry( Command: "FSK:SOUR",
                           Syntax: "FSKey:SOURce <source>",
                           Description: "Select FSK keying source",
                           Category: Command_Category.Modulation,
                           Parameters: "source: INT | EXT",
                           Query_Form: "FSK:SOUR?",
                           Default_Value: "INT",
                           Example: "FSK:SOUR INT" ),

        // ── PWM modulation ───────────────────────────────────────────────

        new Command_Entry( Command: "PWM:STAT",
                           Syntax: "PWM:STATe <mode>",
                           Description: ( "Enable or disable PWM (pulse width modulation) — pulse waveform " +
                                          "only" ),
                           Category: Command_Category.Modulation,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "PWM:STAT?",
                           Default_Value: "OFF",
                           Example: "PWM:STAT ON" ),

        new Command_Entry( Command: "PWM:DEV",
                           Syntax: "PWM:DEViation <seconds>",
                           Description: "Set PWM pulse width deviation",
                           Category: Command_Category.Modulation,
                           Parameters: "seconds: 0 to pulse_width | MIN | MAX",
                           Query_Form: "PWM:DEV?",
                           Default_Value: "10e-6",
                           Example: "PWM:DEV 50e-6" ),

        new Command_Entry( Command: "PWM:SOUR",
                           Syntax: "PWM:SOURce <source>",
                           Description: "Select PWM modulation source",
                           Category: Command_Category.Modulation,
                           Parameters: "source: INT | EXT",
                           Query_Form: "PWM:SOUR?",
                           Default_Value: "INT",
                           Example: "PWM:SOUR INT" ),

        new Command_Entry( Command: "PWM:INT:FUNC",
                           Syntax: "PWM:INTernal:FUNCtion <waveform>",
                           Description: "Set internal PWM modulating waveform shape",
                           Category: Command_Category.Modulation,
                           Parameters: "waveform: SIN | SQU | RAMP | NRAMP | TRI | NOIS | USER",
                           Query_Form: "PWM:INT:FUNC?",
                           Default_Value: "SIN",
                           Example: "PWM:INT:FUNC SIN" ),

        new Command_Entry( Command: "PWM:INT:FREQ",
                           Syntax: "PWM:INTernal:FREQuency <frequency>",
                           Description: "Set internal PWM modulating frequency",
                           Category: Command_Category.Modulation,
                           Parameters: "frequency: 2e-3 to 20e3 Hz | MIN | MAX",
                           Query_Form: "PWM:INT:FREQ?",
                           Default_Value: "10",
                           Example: "PWM:INT:FREQ 1e3" ),

        // ===== Trigger Commands =====

        new Command_Entry( Command: "TRIG:SOUR",
                           Syntax: "TRIGger:SOURce <source>",
                           Description: "Select trigger source for burst and sweep modes",
                           Category: Command_Category.Trigger,
                           Parameters: "source: IMMediate | EXTernal | BUS",
                           Query_Form: "TRIG:SOUR?",
                           Default_Value: "IMMediate",
                           Example: "TRIG:SOUR IMM" ),

        new Command_Entry( Command: "TRIG:SLOP",
                           Syntax: "TRIGger:SLOPe <slope>",
                           Description: "Select external trigger edge polarity",
                           Category: Command_Category.Trigger,
                           Parameters: "slope: POSitive | NEGative",
                           Query_Form: "TRIG:SLOP?",
                           Default_Value: "POSitive",
                           Example: "TRIG:SLOP POS" ),

        new Command_Entry( Command: "TRIG:DEL",
                           Syntax: "TRIGger:DELay <seconds>",
                           Description: "Set trigger-to-output delay for burst mode",
                           Category: Command_Category.Trigger,
                           Parameters: "seconds: 0 to 85 s | MIN | MAX",
                           Query_Form: "TRIG:DEL?",
                           Default_Value: "0",
                           Example: "TRIG:DEL 0.001" ),

        new Command_Entry( Command: "OUTP:TRIG",
                           Syntax: "OUTPut:TRIGger <state>",
                           Description: "Enable or disable trigger output on rear-panel Sync connector",
                           Category: Command_Category.Trigger,
                           Parameters: "state: ON | OFF",
                           Query_Form: "OUTP:TRIG?",
                           Default_Value: "OFF",
                           Example: "OUTP:TRIG ON" ),

        new Command_Entry( Command: "OUTP:TRIG:SLOP",
                           Syntax: "OUTPut:TRIGger:SLOPe <slope>",
                           Description: "Set trigger output edge polarity",
                           Category: Command_Category.Trigger,
                           Parameters: "slope: POSitive | NEGative",
                           Query_Form: "OUTP:TRIG:SLOP?",
                           Default_Value: "POSitive",
                           Example: "OUTP:TRIG:SLOP POS" ),

        // ── Burst mode ───────────────────────────────────────────────────

        new Command_Entry( Command: "BURS:STAT",
                           Syntax: "BURSt:STATe <mode>",
                           Description: "Enable or disable burst mode",
                           Category: Command_Category.Trigger,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "BURS:STAT?",
                           Default_Value: "OFF",
                           Example: "BURS:STAT ON" ),

        new Command_Entry( Command: "BURS:MODE",
                           Syntax: "BURSt:MODE <mode>",
                           Description: "Select burst mode — triggered N-cycle or gated",
                           Category: Command_Category.Trigger,
                           Parameters: "mode: TRIG | GAT",
                           Query_Form: "BURS:MODE?",
                           Default_Value: "TRIG",
                           Example: "BURS:MODE TRIG" ),

        new Command_Entry( Command: "BURS:NCYC",
                           Syntax: "BURSt:NCYCles <count>",
                           Description: "Set number of cycles per burst (triggered burst mode)",
                           Category: Command_Category.Trigger,
                           Parameters: "count: 1 to 50000 | INFinity | MIN | MAX",
                           Query_Form: "BURS:NCYC?",
                           Default_Value: "1",
                           Example: "BURS:NCYC 10" ),

        new Command_Entry( Command: "BURS:INT:PER",
                           Syntax: "BURSt:INTernal:PERiod <seconds>",
                           Description: "Set burst period for internally triggered burst",
                           Category: Command_Category.Trigger,
                           Parameters: "seconds: 1e-6 to 500 s | MIN | MAX",
                           Query_Form: "BURS:INT:PER?",
                           Default_Value: "10e-3",
                           Example: "BURS:INT:PER 1e-3" ),

        new Command_Entry( Command: "BURS:PHAS",
                           Syntax: "BURSt:PHASe <degrees>",
                           Description: "Set burst start phase",
                           Category: Command_Category.Trigger,
                           Parameters: "degrees: -360 to +360 | MIN | MAX",
                           Query_Form: "BURS:PHAS?",
                           Default_Value: "0",
                           Example: "BURS:PHAS 0" ),

        // ── Sweep mode ───────────────────────────────────────────────────

        new Command_Entry( Command: "SWE:STAT",
                           Syntax: "SWEep:STATe <mode>",
                           Description: "Enable or disable frequency sweep mode",
                           Category: Command_Category.Trigger,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "SWE:STAT?",
                           Default_Value: "OFF",
                           Example: "SWE:STAT ON" ),

        new Command_Entry( Command: "SWE:SPAC",
                           Syntax: "SWEep:SPACing <spacing>",
                           Description: "Select sweep spacing — linear or logarithmic",
                           Category: Command_Category.Trigger,
                           Parameters: "spacing: LIN | LOG",
                           Query_Form: "SWE:SPAC?",
                           Default_Value: "LIN",
                           Example: "SWE:SPAC LIN" ),

        new Command_Entry( Command: "SWE:TIME",
                           Syntax: "SWEep:TIME <seconds>",
                           Description: "Set sweep time (start to stop)",
                           Category: Command_Category.Trigger,
                           Parameters: "seconds: 1e-3 to 3600 s | MIN | MAX",
                           Query_Form: "SWE:TIME?",
                           Default_Value: "1",
                           Example: "SWE:TIME 5" ),

        new Command_Entry( Command: "SWE:HTIM",
                           Syntax: "SWEep:HTIMe <seconds>",
                           Description: "Set sweep hold time at stop frequency before restarting",
                           Category: Command_Category.Trigger,
                           Parameters: "seconds: 0 to 3600 s | MIN | MAX",
                           Query_Form: "SWE:HTIM?",
                           Default_Value: "0",
                           Example: "SWE:HTIM 0" ),

        new Command_Entry( Command: "*TRG",
                           Syntax: "*TRG",
                           Description: "Send a bus trigger (TRIG:SOUR must be BUS)",
                           Category: Command_Category.Trigger,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*TRG" ),

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

        new Command_Entry( Command: "DISP:TEXT",
                           Syntax: "DISPlay:TEXT <string>",
                           Description: "Display a text message on the front panel",
                           Category: Command_Category.IO,
                           Parameters: "string: up to 11 characters in double quotes",
                           Query_Form: "DISP:TEXT?",
                           Default_Value: "N/A",
                           Example: "DISP:TEXT \"10 MHZ OUT\"" ),

        new Command_Entry( Command: "DISP:TEXT:CLE",
                           Syntax: "DISPlay:TEXT:CLEar",
                           Description: "Clear the front panel text message",
                           Category: Command_Category.IO,
                           Parameters: "None",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "DISP:TEXT:CLE" ),

        new Command_Entry( Command: "SYST:BEEP",
                           Syntax: "SYSTem:BEEPer",
                           Description: "Issue a single beep from the front panel",
                           Category: Command_Category.IO,
                           Parameters: "None",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "SYST:BEEP" ),

        new Command_Entry( Command: "SYST:BEEP:STAT",
                           Syntax: "SYSTem:BEEPer:STATe <mode>",
                           Description: "Enable or disable the front panel beeper",
                           Category: Command_Category.IO,
                           Parameters: "mode: ON | OFF",
                           Query_Form: "SYST:BEEP:STAT?",
                           Default_Value: "ON",
                           Example: "SYST:BEEP:STAT ON" ),

        // ===== Memory Commands =====

        new Command_Entry( Command: "*SAV",
                           Syntax: "*SAV <register>",
                           Description: "Save current instrument state to memory register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 0 | 1 | 2 | 3 | 4",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "*SAV 1" ),

        new Command_Entry( Command: "*RCL",
                           Syntax: "*RCL <register>",
                           Description: "Recall instrument state from memory register",
                           Category: Command_Category.Memory,
                           Parameters: "register: 0 | 1 | 2 | 3 | 4",
                           Query_Form: null,
                           Default_Value: "N/A",
                           Example: "*RCL 1" ),

        new Command_Entry( Command: "MMEM:STOR:STAT",
                           Syntax: "MMEMory:STORe:STATe <register>,\"<filename>\"",
                           Description: "Save instrument state to a named file in non-volatile memory",
                           Category: Command_Category.Memory,
                           Parameters: "register: 0–4, filename: quoted path string",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "MMEM:STOR:STAT 1,\"INT:\\STATE1.STA\"" ),

        new Command_Entry( Command: "MMEM:LOAD:STAT",
                           Syntax: "MMEMory:LOAD:STATe <register>,\"<filename>\"",
                           Description: ( "Recall instrument state from a named file in non-volatile " +
                                          "memory" ),
                           Category: Command_Category.Memory,
                           Parameters: "register: 0–4, filename: quoted path string",
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "MMEM:LOAD:STAT 1,\"INT:\\STATE1.STA\"" ),

        // ===== Calibration Commands =====

        new Command_Entry( Command: "CAL:SEC:STAT",
                           Syntax: "CALibration:SECure:STATe <mode>,<code>",
                           Description: "Enable or disable calibration security",
                           Category: Command_Category.Calibration,
                           Parameters: "mode: ON | OFF, code: security code string",
                           Query_Form: "CAL:SEC:STAT?",
                           Default_Value: "ON",
                           Example: "CAL:SEC:STAT OFF,HP033220" ),

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
