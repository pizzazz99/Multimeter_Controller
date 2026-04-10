// =============================================================================
// FILE:     HP3456_Command_Dictionary_Class.cs
// PROJECT:  Multimeter_Controller
// =============================================================================
//
// DESCRIPTION:
//   Static command dictionary for the HP/Agilent 3456A 6.5-digit digital
//   voltmeter. Provides a structured, searchable registry of all supported
//   HP-IB instrument program codes, organized by functional category. Each
//   entry captures the full command syntax, description, valid parameter
//   ranges, query form, factory default value, and a usage example.
//
//   This class is intended as the single source of truth for instrument command
//   metadata. It is designed to support command validation, UI population,
//   documentation generation, and runtime lookup by command name or query form.
//
// -----------------------------------------------------------------------------
// INSTRUMENT:
//   HP / Hewlett-Packard 3456A Digital Voltmeter
//   Interface:    HP-IB (IEEE-488, pre-488.2)
//   Default Addr: 22  (set via rear-panel DIP switches)
//   Firmware Ref: Compatible with all standard 3456A ROM revisions
//                 (ROM checksum variants: U5/U7/U8 sets for serial > 2015A03070)
//
// -----------------------------------------------------------------------------
// COMMAND SYNTAX NOTES:
//
//   The 3456A uses a terse single-pass programming language (HPML / HP-IB
//   program codes) that differs fundamentally from the keyword-based syntax
//   of later instruments such as the 3458A.
//
//   - Commands are short mnemonic codes (F1, R2, T3, Z1, FL1, etc.).
//   - Multiple codes may be concatenated in a single HP-IB write string
//     without delimiters, e.g. "S0F1R1M0T4".
//   - Numeric register values are written as plain ASCII digits immediately
//     followed by the store command "ST" and register name, e.g. "10STN".
//   - A "W" (word separator) must be used when a numeric value would be
//     ambiguous following another digit, e.g. "F1W10STN".
//   - Spaces in command strings are legal and ignored; they are used here
//     in examples for readability only.
//   - The instrument has no query form for most settings; state readback is
//     obtained via the Status Byte (serial poll) or by reading the output
//     buffer after certain commands.
//
// -----------------------------------------------------------------------------
// COMMAND CATEGORIES:
//
//   Measurement   — Function/mode selection codes (F1–F5 with shift S0/S1).
//                   F1=DCV, F2=ACV, F3=ACV+DCV, F4=2W Ohms, F5=4W Ohms.
//                   Shift S1 selects ratio variants and O.C. Ohms modes.
//
//   Configuration — Range codes (R1–R9), integration time register (I),
//                   digits register (G), settling delay register (D),
//                   autozero (Z0/Z1), analog filter (FL0/FL1),
//                   and output format (P0/P1).
//
//   Trigger       — Trigger mode selection (T1–T4) and readings-per-trigger
//                   register (N). T1=internal auto, T2=external, T3=single
//                   software trigger, T4=hold (requires separate trigger).
//
//   Math          — Math mode selection (M0–M9): off, pass/fail, statistics,
//                   null, thermistor (°F/°C), scale, %error, and dB.
//                   Register store (ST) and recall (RE) commands for
//                   math registers (L, U, Y, Z, R) and read-only
//                   result registers (M, V, C).
//
//   Memory        — Reading storage enable (RS0/RS1) and recall (RE used
//                   with the R register). Up to 350 readings may be stored.
//
//   I/O           — System output mode (SO0/SO1), display control (D0/D1),
//                   packed vs. ASCII output format (P0/P1), EOI control
//                   (O0/O1), numeric word separator (W), front/rear terminal
//                   sense (SW1), and service request mask (SM).
//
//   System        — Home/reset (H), clear-continue (CL1), self-test (TE0/TE1),
//                   and program memory load/execute (L1, Q, X1).
//
// -----------------------------------------------------------------------------
// KEY METHODS:
//
//   Get_All_Commands()
//     Returns a List<Command_Entry> containing all registered commands,
//     sorted alphabetically by Command name (case-insensitive). The list
//     is rebuilt on every call — it is not cached.
//
//   Get_Command_By_Name(string Command_Name)
//     Performs a case-insensitive lookup against both the Command field and
//     the Query_Form field of every registered entry. Returns the matching
//     Command_Entry, or null if no match is found. Trims leading/trailing
//     whitespace from the input before comparison.
//
// -----------------------------------------------------------------------------
// DATA MODEL — Command_Entry fields:
//
//   Command       The primary mnemonic sent to the instrument (e.g. "F1").
//   Syntax        Full syntax string including optional parameters.
//   Description   Human-readable description of the command's purpose.
//   Category      Command_Category enum value for grouping/filtering.
//   Parameters    Enumeration of valid parameter values and their meanings.
//   Query_Form    The query variant of the command if one exists; for the
//                 3456A this is almost always an empty string — state is
//                 read via the status byte or dedicated readback commands.
//   Default_Value The factory default (power-on or Home) state for this
//                 setting.
//   Example       A representative usage string suitable for direct HP-IB
//                 transmission (spaces included for readability; the
//                 instrument ignores spaces).
//
// -----------------------------------------------------------------------------
// USAGE NOTES:
//
//   - All string comparisons in Get_Command_By_Name use
//     StringComparison.OrdinalIgnoreCase for culture-invariant matching.
//
//   - The 3456A does NOT support SCPI. Do not send keyword commands such
//     as "DCV", "NPLC", or "AZERO" — these will cause a syntax error.
//
//   - Register values are stored with "STx" (store to register x) and
//     recalled with "REx" (recall from register x), e.g. "100STI" sets
//     NPLC to 100, "REN" reads back the number-of-readings register.
//
//   - The Math mode register M0–M9 is set separately from math register
//     values. Set the mode first, then store the required limit or
//     reference value into the relevant register (L, U, Y, Z as needed).
//
//   - The DBm reference impedance is stored in register R (e.g. "600STR").
//
//   - Reading storage (RS1) stores up to 350 readings in a circular FIFO.
//     Recalled via "RER" after disabling storage (RS0).
//
//   - The Home command (H) resets to the user-defined home state stored in
//     program memory. "CL1" performs a clear-continue without clearing
//     program memory. "H" with no program stored resets to factory default.
//
//   - Service Request Mask (SM) argument is three octal digits,
//     e.g. "SM004" enables SRQ on data-ready.
//
//   - Packed output (P1) delivers 4 bytes per reading in BCD format.
//     ASCII output (P0) delivers 14 bytes: +RDDDDDDDD+D[CR][LF].
//
// -----------------------------------------------------------------------------
// DEPENDENCIES:
//   System.Collections.Generic    List<T>
//   System.Linq                   FirstOrDefault (LINQ extension)
//   System.StringComparison       OrdinalIgnoreCase
//   Command_Entry                 Data record defined elsewhere in namespace
//   Command_Category              Enum defined elsewhere in namespace
//
// -----------------------------------------------------------------------------
// MAINTENANCE:
//   To add a command: append a new Command_Entry to the list in
//   Get_All_Commands(). The list is re-sorted alphabetically on every call,
//   so insertion order does not matter.
//
//   To deprecate a command: add a note to the Description field rather than
//   removing the entry, to preserve backward compatibility with any consumers
//   relying on dictionary lookup.
//
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  public static class HP3456_Command_Dictionary_Class
  {
    public static Command_Entry? Get_Command_By_Name( string Command_Name )
    {
      if ( string.IsNullOrWhiteSpace( Command_Name ) )
        return null;

      Command_Name = Command_Name.Trim();

      return Get_All_Commands().FirstOrDefault(
        c => string.Equals( c.Command, Command_Name, StringComparison.OrdinalIgnoreCase ) ||
             string.Equals( c.Query_Form, Command_Name, StringComparison.OrdinalIgnoreCase ) );
    }

    public static List<Command_Entry> Get_All_Commands()
    {
      var Commands = new List<Command_Entry> {
        // ===== Measurement Commands =====
        // Function codes — unshifted (S0) mode
        new Command_Entry( Command: "F1",
                           Syntax: "[S0]F1",
                           Description: "Select DC voltage measurement function (DCV)",
                           Category: Command_Category.Measurement,
                           Parameters: ( "No parameters. Precede with S0 (default) for DCV, or S1 for " +
                                         "DCV/DCV Ratio." ),
                           Query_Form: "",
                           Default_Value: "F1 (DCV) on power-on / Home",
                           Example: "S0F1" ),

        new Command_Entry( Command: "F2",
                           Syntax: "[S0]F2",
                           Description: "Select AC voltage measurement function (ACV, true RMS)",
                           Category: Command_Category.Measurement,
                           Parameters: "No parameters. Precede with S0 for ACV, or S1 for ACV/DCV Ratio.",
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "S0F2" ),

        new Command_Entry( Command: "F3",
                           Syntax: "[S0]F3",
                           Description: ( "Select AC+DC voltage measurement function (true RMS of combined " +
                                          "signal)" ),
                           Category: Command_Category.Measurement,
                           Parameters: ( "No parameters. Precede with S0 for ACV+DCV, or S1 for " +
                                         "ACV+DCV/DCV Ratio." ),
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "S0F3" ),

        new Command_Entry( Command: "F4",
                           Syntax: "[S0]F4",
                           Description: "Select 2-wire resistance measurement function",
                           Category: Command_Category.Measurement,
                           Parameters: ( "No parameters. Precede with S0 for 2W Ohms, or S1 for O.C. " +
                                         "(Offset Compensated) 2W Ohms." ),
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "S0F4" ),

        new Command_Entry( Command: "F5",
                           Syntax: "[S0]F5",
                           Description: "Select 4-wire resistance measurement function",
                           Category: Command_Category.Measurement,
                           Parameters: ( "No parameters. Precede with S0 for 4W Ohms, or S1 for O.C. " +
                                         "(Offset Compensated) 4W Ohms." ),
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "S0F5" ),

        // Shift modifier
        new Command_Entry( Command: "S0",
                           Syntax: "S0",
                           Description: "Select unshifted function mode (normal measurement functions)",
                           Category: Command_Category.Measurement,
                           Parameters: ( "None. S0 selects: F1=DCV, F2=ACV, F3=ACV+DCV, F4=2W Ohms, F5=4W " +
                                         "Ohms." ),
                           Query_Form: "",
                           Default_Value: "S0 (unshifted)",
                           Example: "S0F1" ),

        new Command_Entry( Command: "S1",
                           Syntax: "S1",
                           Description: "Select shifted function mode (ratio and O.C. ohms variants)",
                           Category: Command_Category.Measurement,
                           Parameters: ( "None. S1 selects: F1=DCV/DCV Ratio, F2=ACV/DCV Ratio, " +
                                         "F3=ACV+DCV/DCV Ratio, F4=O.C. 2W Ohms, F5=O.C. 4W Ohms." ),
                           Query_Form: "",
                           Default_Value: "S0 (unshifted is default)",
                           Example: "S1F4" ),

        // ===== Configuration Commands =====
        // Range codes
        new Command_Entry( Command: "R1",
                           Syntax: "R1",
                           Description: "Select auto-range (instrument selects range automatically)",
                           Category: Command_Category.Configuration,
                           Parameters: ( "None. Applies to all functions. Upranges at 120% full-scale; " +
                                         "downranges below 11% full-scale." ),
                           Query_Form: "",
                           Default_Value: "R1 (autorange)",
                           Example: "F1R1" ),

        new Command_Entry( Command: "R2",
                           Syntax: "R2",
                           Description: ( "Select 100 mV range (volts functions) or 1 kΩ range (ohms " +
                                          "functions)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "Volts: 100 mV full-scale. Ohms: 1 kΩ full-scale.",
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "F1R2" ),

        new Command_Entry( Command: "R3",
                           Syntax: "R3",
                           Description: ( "Select 1000 mV (1 V) range (volts functions) or 1 kΩ range " +
                                          "(ohms functions)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "Volts: 1000 mV full-scale. Ohms: 1 kΩ full-scale.",
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "F1R3" ),

        new Command_Entry( Command: "R4",
                           Syntax: "R4",
                           Description: ( "Select 10 V range (volts functions) or 10 kΩ range (ohms " +
                                          "functions)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "Volts: 10 V full-scale. Ohms: 10 kΩ full-scale.",
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "F1R4" ),

        new Command_Entry( Command: "R5",
                           Syntax: "R5",
                           Description: ( "Select 100 V range (volts functions) or 100 kΩ range (ohms " +
                                          "functions)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "Volts: 100 V full-scale. Ohms: 100 kΩ full-scale.",
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "F1R5" ),

        new Command_Entry( Command: "R6",
                           Syntax: "R6",
                           Description: ( "Select 1000 V range (volts functions) or 1 MΩ range (ohms " +
                                          "functions)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "Volts: 1000 V full-scale. Ohms: 1 MΩ full-scale.",
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "F1R6" ),

        new Command_Entry( Command: "R7",
                           Syntax: "R7",
                           Description: "Select 10 MΩ range (ohms functions only; invalid for voltage)",
                           Category: Command_Category.Configuration,
                           Parameters: "Ohms only: 10 MΩ full-scale. Invalid for F1, F2, F3.",
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "F4R7" ),

        new Command_Entry( Command: "R8",
                           Syntax: "R8",
                           Description: "Select 100 MΩ range (ohms functions only; invalid for voltage)",
                           Category: Command_Category.Configuration,
                           Parameters: "Ohms only: 100 MΩ full-scale. Invalid for F1, F2, F3.",
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "F4R8" ),

        new Command_Entry( Command: "R9",
                           Syntax: "R9",
                           Description: ( "Select 1000 MΩ (1 GΩ) range (ohms functions only; invalid for " +
                                          "voltage)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "Ohms only: 1000 MΩ full-scale. Invalid for F1, F2, F3.",
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "F4R9" ),

        // Integration time (NPLC) — stored via register I
        new Command_Entry( Command: "STI",
                           Syntax: "<value>STI",
                           Description: ( "Store number of power line cycles (NPLC / integration time) " +
                                          "into register I" ),
                           Category: Command_Category.Configuration,
                           Parameters: ( "value: 1 to 100 (integer). Front panel exposes 1, 10, 100 only. " +
                                         "Recommended via HP-IB: 1 | 2 | 4 | 8 | 16 | 32 | 64 | 100. " +
                                         "Default: 100." ),
                           Query_Form: "REI",
                           Default_Value: "100",
                           Example: "100STI" ),

        new Command_Entry( Command: "REI",
                           Syntax: "REI",
                           Description: ( "Recall (query) the current integration time register I (NPLC " +
                                          "setting)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "None. Returns the current NPLC value stored in register I.",
                           Query_Form: "REI",
                           Default_Value: "N/A (read-only recall)",
                           Example: "REI" ),

        // Digits — stored via register G
        new Command_Entry( Command: "STG",
                           Syntax: "<value>STG",
                           Description: "Store number of display digits into register G",
                           Category: Command_Category.Configuration,
                           Parameters: ( "value: 3|4|5|6 (digits displayed; 6 = maximum 6.5-digit " +
                                         "resolution)" ),
                           Query_Form: "REG",
                           Default_Value: "6",
                           Example: "6STG" ),

        new Command_Entry( Command: "REG",
                           Syntax: "REG",
                           Description: "Recall (query) the current digits register G",
                           Category: Command_Category.Configuration,
                           Parameters: "None. Returns the current digits value stored in register G.",
                           Query_Form: "REG",
                           Default_Value: "N/A (read-only recall)",
                           Example: "REG" ),

        // Settling delay — stored via register D
        new Command_Entry( Command: "STD",
                           Syntax: "<value>STD",
                           Description: "Store settling delay (in seconds) into register D",
                           Category: Command_Category.Configuration,
                           Parameters: ( "value: 0.0 to 999.9 seconds. Inserted between trigger and start " +
                                         "of measurement." ),
                           Query_Form: "RED",
                           Default_Value: "0",
                           Example: "0.5STD" ),

        new Command_Entry( Command: "RED",
                           Syntax: "RED",
                           Description: "Recall (query) the current settling delay register D",
                           Category: Command_Category.Configuration,
                           Parameters: "None. Returns the current delay value stored in register D.",
                           Query_Form: "RED",
                           Default_Value: "N/A (read-only recall)",
                           Example: "RED" ),

        // Autozero
        new Command_Entry( Command: "Z0",
                           Syntax: "Z0",
                           Description: ( "Disable autozero (single offset measurement taken at mode " +
                                          "change; no per-reading zeroing)" ),
                           Category: Command_Category.Configuration,
                           Parameters: ( "None. Disabling autozero increases reading rate; useful for " +
                                         "high-impedance measurements (eliminates input switching)." ),
                           Query_Form: "",
                           Default_Value: "Z1 (ON is default)",
                           Example: "Z0" ),

        new Command_Entry( Command: "Z1",
                           Syntax: "Z1",
                           Description: ( "Enable autozero (instrument shorts input and takes an offset " +
                                          "reading before each measurement)" ),
                           Category: Command_Category.Configuration,
                           Parameters: "None. Recommended for highest accuracy.",
                           Query_Form: "",
                           Default_Value: "Z1 (ON)",
                           Example: "Z1" ),

        // Analog filter
        new Command_Entry( Command: "FL0",
                           Syntax: "FL0",
                           Description: "Disable analog low-pass filter",
                           Category: Command_Category.Configuration,
                           Parameters: ( "None. Filter is a 3-pole active filter with >60 dB attenuation " +
                                         "at ≥50 Hz." ),
                           Query_Form: "",
                           Default_Value: "FL0 (OFF)",
                           Example: "FL0" ),

        new Command_Entry( Command: "FL1",
                           Syntax: "FL1",
                           Description: "Enable analog low-pass filter (>60 dB at ≥50 Hz)",
                           Category: Command_Category.Configuration,
                           Parameters: ( "None. In ACV or ACV+DCV mode, filter is applied to the AC " +
                                         "converter output. Recommended for measurements below 400 Hz." ),
                           Query_Form: "",
                           Default_Value: "FL0 (OFF is default)",
                           Example: "FL1" ),

        // Output format
        new Command_Entry( Command: "P0",
                           Syntax: "P0",
                           Description: "Select ASCII output format (14 bytes per reading)",
                           Category: Command_Category.Configuration,
                           Parameters: ( "Format: +RDDDDDDDD+D[CR][LF] where R=overrange flag (0/1), " +
                                         "DDDDDDDD=7 mantissa digits, +D=signed exponent. Readings " +
                                         "separated by commas when N>1." ),
                           Query_Form: "",
                           Default_Value: "P0 (ASCII)",
                           Example: "P0" ),

        new Command_Entry( Command: "P1",
                           Syntax: "P1",
                           Description: "Select packed BCD output format (4 bytes per reading)",
                           Category: Command_Category.Configuration,
                           Parameters: ( "Format: 4 bytes: [exponent+sign+OR][BCD12][BCD34][BCD56]. " +
                                         "Decimal point implicit at OR bit. Faster transfer than P0." ),
                           Query_Form: "",
                           Default_Value: "P0 (ASCII is default)",
                           Example: "P1" ),

        // ===== Trigger Commands =====
        new Command_Entry( Command: "T1",
                           Syntax: "T1",
                           Description: ( "Select internal auto-trigger mode (instrument triggers " +
                                          "continuously and automatically)" ),
                           Category: Command_Category.Trigger,
                           Parameters: ( "None. Instrument repeats measurements at maximum rate determined " +
                                         "by integration time and function." ),
                           Query_Form: "",
                           Default_Value: "T1 (internal auto)",
                           Example: "T1" ),

        new Command_Entry( Command: "T2",
                           Syntax: "T2",
                           Description: ( "Select external trigger mode (measurement triggered by hardware " +
                                          "signal on rear-panel EXT TRIG input)" ),
                           Category: Command_Category.Trigger,
                           Parameters: ( "None. Negative TTL edge on EXT TRIG initiates measurement. If " +
                                         "triggered during an active measurement cycle, the cycle is " +
                                         "aborted and restarted." ),
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "T2" ),

        new Command_Entry( Command: "T3",
                           Syntax: "T3",
                           Description: ( "Software single trigger — executes one measurement cycle when " +
                                          "this code is sent" ),
                           Category: Command_Category.Trigger,
                           Parameters: ( "None. Sends a single trigger to the instrument immediately when " +
                                         "parsed. Can be used within a stored program." ),
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "T3" ),

        new Command_Entry( Command: "T4",
                           Syntax: "T4",
                           Description: ( "Hold mode — instrument waits for a GET (Group Execute Trigger) " +
                                          "or T3 command before each measurement" ),
                           Category: Command_Category.Trigger,
                           Parameters: ( "None. Instrument holds in standby until a bus trigger (GET) or " +
                                         "an explicit T3 is received." ),
                           Query_Form: "",
                           Default_Value: "Not default",
                           Example: "T4" ),

        // Number of readings register
        new Command_Entry( Command: "STN",
                           Syntax: "<value>STN",
                           Description: "Store number of readings per trigger into register N",
                           Category: Command_Category.Trigger,
                           Parameters: ( "value: 1 to 350 (maximum is limited by available reading storage " +
                                         "memory). Readings are output as a comma-separated sequence in " +
                                         "ASCII mode." ),
                           Query_Form: "REN",
                           Default_Value: "1",
                           Example: "10STN" ),

        new Command_Entry( Command: "REN",
                           Syntax: "REN",
                           Description: "Recall (query) the number-of-readings register N",
                           Category: Command_Category.Trigger,
                           Parameters: "None.",
                           Query_Form: "REN",
                           Default_Value: "N/A (read-only recall)",
                           Example: "REN" ),

        // ===== Math Commands =====
        new Command_Entry( Command: "M0",
                           Syntax: "M0",
                           Description: "Disable all math functions (pass-through measurement)",
                           Category: Command_Category.Math,
                           Parameters: "None.",
                           Query_Form: "",
                           Default_Value: "M0 (OFF)",
                           Example: "M0" ),

        new Command_Entry( Command: "M1",
                           Syntax: "M1",
                           Description: ( "Enable pass/fail math mode — compares reading against lower (L) " +
                                          "and upper (U) limit registers" ),
                           Category: Command_Category.Math,
                           Parameters: ( "Requires limits pre-loaded: <low>STL and <high>STU. Outputs " +
                                         "reading; SRQ on limit failure if SM bit 200 is set." ),
                           Query_Form: "",
                           Default_Value: "M0 (OFF is default)",
                           Example: "4.9STL 5.1STU M1" ),

        new Command_Entry( Command: "M2",
                           Syntax: "M2",
                           Description: ( "Enable statistics math mode — accumulates mean, variance, and " +
                                          "count over successive readings" ),
                           Category: Command_Category.Math,
                           Parameters: ( "Results in read-only registers: M (mean), V (variance), C " +
                                         "(count). Read with REM, REV, REC." ),
                           Query_Form: "",
                           Default_Value: "M0 (OFF is default)",
                           Example: "M2" ),

        new Command_Entry( Command: "M3",
                           Syntax: "M3",
                           Description: ( "Enable null math mode — subtracts the value stored in register " +
                                          "Z from each reading" ),
                           Category: Command_Category.Math,
                           Parameters: "Load null reference: <value>STZ. Result = reading - Z.",
                           Query_Form: "",
                           Default_Value: "M0 (OFF is default)",
                           Example: "0.00015STZ M3" ),

        new Command_Entry( Command: "M4",
                           Syntax: "M4",
                           Description: ( "Enable thermistor math mode — converts resistance reading to " +
                                          "temperature in °F" ),
                           Category: Command_Category.Math,
                           Parameters: ( "Instrument uses built-in Steinhart-Hart coefficients for " +
                                         "thermistor conversion. Use appropriate ohms function (F4 or F5)." ),
                           Query_Form: "",
                           Default_Value: "M0 (OFF is default)",
                           Example: "S0F4R1M4" ),

        new Command_Entry( Command: "M5",
                           Syntax: "M5",
                           Description: ( "Enable thermistor math mode — converts resistance reading to " +
                                          "temperature in °C" ),
                           Category: Command_Category.Math,
                           Parameters: ( "Instrument uses built-in Steinhart-Hart coefficients for " +
                                         "thermistor conversion. Use appropriate ohms function (F4 or F5)." ),
                           Query_Form: "",
                           Default_Value: "M0 (OFF is default)",
                           Example: "S0F4R1M5" ),

        new Command_Entry( Command: "M6",
                           Syntax: "M6",
                           Description: ( "Enable scale math mode — applies formula: result = (reading - " +
                                          "Z) / Y" ),
                           Category: Command_Category.Math,
                           Parameters: ( "Load registers: <offset>STZ, <scale_factor>STY. Result = (X - Z) " +
                                         "/ Y." ),
                           Query_Form: "",
                           Default_Value: "M0 (OFF is default)",
                           Example: "0STZ 1000STY M6" ),

        new Command_Entry( Command: "M7",
                           Syntax: "M7",
                           Description: ( "Enable percent error math mode — result = ((reading - Y) / Y) × " +
                                          "100" ),
                           Category: Command_Category.Math,
                           Parameters: ( "Load reference: <nominal>STY. Result is percent deviation from " +
                                         "nominal." ),
                           Query_Form: "",
                           Default_Value: "M0 (OFF is default)",
                           Example: "5.0STY M7" ),

        new Command_Entry( Command: "M8",
                           Syntax: "M8",
                           Description: "Enable dB math mode — result = 20 × log10(reading / Y)",
                           Category: Command_Category.Math,
                           Parameters: ( "Load voltage reference: <ref_volts>STY. Result in dB relative to " +
                                         "Y." ),
                           Query_Form: "",
                           Default_Value: "M0 (OFF is default)",
                           Example: "1.0STY M8" ),

        new Command_Entry( Command: "M9",
                           Syntax: "M9",
                           Description: ( "Enable dBm math mode — result = 10 × log10(reading² / R × " +
                                          "1000), where R is the impedance stored in register R" ),
                           Category: Command_Category.Math,
                           Parameters: ( "Load reference impedance: <ohms>STR. Valid impedances: 50–8000 " +
                                         "Ω. Result in dBm." ),
                           Query_Form: "",
                           Default_Value: "M0 (OFF is default)",
                           Example: "600STR M9" ),

        // Math register store commands
        new Command_Entry( Command: "STL",
                           Syntax: "<value>STL",
                           Description: ( "Store value into lower limit register L (used by pass/fail math " +
                                          "mode M1)" ),
                           Category: Command_Category.Math,
                           Parameters: "value: numeric lower limit in measurement units.",
                           Query_Form: "REL",
                           Default_Value: "0",
                           Example: "4.9STL" ),

        new Command_Entry( Command: "STU",
                           Syntax: "<value>STU",
                           Description: ( "Store value into upper limit register U (used by pass/fail math " +
                                          "mode M1)" ),
                           Category: Command_Category.Math,
                           Parameters: "value: numeric upper limit in measurement units.",
                           Query_Form: "REU",
                           Default_Value: "0",
                           Example: "5.1STU" ),

        new Command_Entry( Command: "STY",
                           Syntax: "<value>STY",
                           Description: ( "Store value into Y register (reference for dB, %error, and " +
                                          "scale math modes)" ),
                           Category: Command_Category.Math,
                           Parameters: "value: reference value in measurement units.",
                           Query_Form: "REY",
                           Default_Value: "0",
                           Example: "5.0STY" ),

        new Command_Entry( Command: "STZ",
                           Syntax: "<value>STZ",
                           Description: ( "Store value into Z register (null offset for null math mode M3, " +
                                          "and offset for scale mode M6)" ),
                           Category: Command_Category.Math,
                           Parameters: "value: null offset or scale offset in measurement units.",
                           Query_Form: "REZ",
                           Default_Value: "0",
                           Example: "0.00015STZ" ),

        new Command_Entry( Command: "STR",
                           Syntax: "<value>STR",
                           Description: ( "Store value into R register (reference impedance for dBm math " +
                                          "mode M9, or general-purpose storage)" ),
                           Category: Command_Category.Math,
                           Parameters: ( "value: impedance in ohms for dBm; 50 to 8000 typical. Also used " +
                                         "as general scratch register." ),
                           Query_Form: "RER",
                           Default_Value: "600",
                           Example: "50STR" ),

        // Read-only math result registers
        new Command_Entry( Command: "REM",
                           Syntax: "REM",
                           Description: ( "Recall mean register M (read-only result from statistics math " +
                                          "mode M2)" ),
                           Category: Command_Category.Math,
                           Parameters: ( "None. Returns mean of all readings accumulated since M2 was " +
                                         "enabled." ),
                           Query_Form: "REM",
                           Default_Value: "N/A (read-only)",
                           Example: "REM" ),

        new Command_Entry( Command: "REV",
                           Syntax: "REV",
                           Description: ( "Recall variance register V (read-only result from statistics " +
                                          "math mode M2)" ),
                           Category: Command_Category.Math,
                           Parameters: "None. Returns variance of accumulated readings.",
                           Query_Form: "REV",
                           Default_Value: "N/A (read-only)",
                           Example: "REV" ),

        new Command_Entry( Command: "REC",
                           Syntax: "REC",
                           Description: ( "Recall count register C (read-only result from statistics math " +
                                          "mode M2)" ),
                           Category: Command_Category.Math,
                           Parameters: "None. Returns total number of readings accumulated.",
                           Query_Form: "REC",
                           Default_Value: "N/A (read-only)",
                           Example: "REC" ),

        // ===== Memory Commands =====
        new Command_Entry( Command: "RS0",
                           Syntax: "RS0",
                           Description: ( "Disable reading storage (stop storing readings in internal " +
                                          "memory)" ),
                           Category: Command_Category.Memory,
                           Parameters: "None. After disabling, stored readings can be recalled with RER.",
                           Query_Form: "",
                           Default_Value: "RS0 (OFF)",
                           Example: "RS0" ),

        new Command_Entry( Command: "RS1",
                           Syntax: "RS1",
                           Description: ( "Enable reading storage (store up to 350 readings in internal " +
                                          "circular FIFO memory)" ),
                           Category: Command_Category.Memory,
                           Parameters: ( "None. Memory holds the most recent 350 readings. Oldest readings " +
                                         "are overwritten when full." ),
                           Query_Form: "",
                           Default_Value: "RS0 (OFF is default)",
                           Example: "RS1" ),

        new Command_Entry( Command: "RER",
                           Syntax: "RER",
                           Description: ( "Recall readings from internal storage memory (also used to " +
                                          "recall the R register value)" ),
                           Category: Command_Category.Memory,
                           Parameters: ( "None. In reading storage context, outputs stored readings over " +
                                         "HP-IB. In math context, returns value of register R." ),
                           Query_Form: "RER",
                           Default_Value: "N/A",
                           Example: "RS0 RER" ),

        // ===== I/O Commands =====
        new Command_Entry( Command: "SO0",
                           Syntax: "SO0",
                           Description: ( "Disable system output mode (instrument outputs readings only " +
                                          "when addressed to talk)" ),
                           Category: Command_Category.IO,
                           Parameters: "None. Standard HP-IB talker mode.",
                           Query_Form: "",
                           Default_Value: "SO0 (OFF)",
                           Example: "SO0" ),

        new Command_Entry( Command: "SO1",
                           Syntax: "SO1",
                           Description: ( "Enable system output mode (instrument asserts SRQ and outputs " +
                                          "reading automatically when data is ready)" ),
                           Category: Command_Category.IO,
                           Parameters: ( "None. Useful for interrupt-driven acquisition; instrument " +
                                         "signals controller via SRQ when reading is available." ),
                           Query_Form: "",
                           Default_Value: "SO0 (OFF is default)",
                           Example: "SO1" ),

        new Command_Entry( Command: "D0",
                           Syntax: "D0",
                           Description: ( "Disable front panel display (blanks the display; reduces EMI " +
                                          "and speeds up acquisition slightly)" ),
                           Category: Command_Category.IO,
                           Parameters: "None.",
                           Query_Form: "",
                           Default_Value: "D1 (ON)",
                           Example: "D0" ),

        new Command_Entry( Command: "D1",
                           Syntax: "D1",
                           Description: "Enable front panel display",
                           Category: Command_Category.IO,
                           Parameters: "None.",
                           Query_Form: "",
                           Default_Value: "D1 (ON)",
                           Example: "D1" ),

        new Command_Entry( Command: "O0",
                           Syntax: "O0",
                           Description: "Disable EOI (End-Or-Identify) assertion on last byte of output",
                           Category: Command_Category.IO,
                           Parameters: ( "None. Disabling EOI can increase transfer speed in some bus " +
                                         "configurations." ),
                           Query_Form: "",
                           Default_Value: "O1 (EOI enabled)",
                           Example: "O0" ),

        new Command_Entry( Command: "O1",
                           Syntax: "O1",
                           Description: ( "Enable EOI assertion on last byte of output (default HP-IB " +
                                          "behavior)" ),
                           Category: Command_Category.IO,
                           Parameters: "None.",
                           Query_Form: "",
                           Default_Value: "O1 (ON)",
                           Example: "O1" ),

        new Command_Entry( Command: "W",
                           Syntax: "W",
                           Description: ( "Word (numeric) separator — used to delimit a numeric value from " +
                                          "a following digit to prevent ambiguity" ),
                           Category: Command_Category.IO,
                           Parameters: ( "None. Functionally equivalent to a space but explicit in code. " +
                                         "Example: F1W10STN stores 10 into N without the leading '1' being " +
                                         "parsed as part of F1." ),
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "F1W10STN" ),

        new Command_Entry( Command: "SM",
                           Syntax: "SM<mask>",
                           Description: ( "Set service request (SRQ) mask — defines which events cause an " +
                                          "SRQ on the HP-IB" ),
                           Category: Command_Category.IO,
                           Parameters: ( "mask: 3-digit octal value. Bits: 001=front panel SRQ, " +
                                         "002=program complete, 004=data ready, 010=trigger too fast, " +
                                         "020=illegal state/syntax error, 040=program memory error, " +
                                         "100=service request bit, 200=limits failure (pass/fail)." ),
                           Query_Form: "",
                           Default_Value: "SM000 (no SRQ)",
                           Example: "SM004" ),

        new Command_Entry( Command: "SW1",
                           Syntax: "SW1",
                           Description: "Query front/rear input terminal switch position",
                           Category: Command_Category.IO,
                           Parameters: ( "None. Returns 0 = front terminals active, 1 = rear terminals " +
                                         "active." ),
                           Query_Form: "SW1",
                           Default_Value: "N/A (query only)",
                           Example: "SW1" ),

        // ===== System Commands =====
        new Command_Entry( Command: "H",
                           Syntax: "H",
                           Description: ( "Home command — restores instrument to the configuration stored " +
                                          "as the Home state in program memory" ),
                           Category: Command_Category.System,
                           Parameters: ( "None. If no Home state has been stored, instrument resets to " +
                                         "factory default. Does NOT clear program memory (use CL1 to clear " +
                                         "and reset)." ),
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "H" ),

        new Command_Entry( Command: "CL1",
                           Syntax: "CL1",
                           Description: ( "Clear-continue — resets instrument state and clears program " +
                                          "memory, then continues in local/remote mode" ),
                           Category: Command_Category.System,
                           Parameters: ( "None. Equivalent to a Device Clear (DCL) bus command. Resets all " +
                                         "settings to factory defaults and erases stored program." ),
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "CL1" ),

        new Command_Entry( Command: "TE0",
                           Syntax: "TE0",
                           Description: "Disable self-test mode",
                           Category: Command_Category.System,
                           Parameters: "None.",
                           Query_Form: "",
                           Default_Value: "TE0 (OFF)",
                           Example: "TE0" ),

        new Command_Entry( Command: "TE1",
                           Syntax: "TE1",
                           Description: ( "Execute self-test — performs analog gain, offset, and digital " +
                                          "checks; reports pass/fail result" ),
                           Category: Command_Category.System,
                           Parameters: ( "None. Input terminals must be floating and GUARD switch in the " +
                                         "IN position. Returns '100' on pass; displays failing test number " +
                                         "on failure. Result also output over HP-IB when instrument is " +
                                         "addressed to talk." ),
                           Query_Form: "",
                           Default_Value: "TE0 (OFF is default)",
                           Example: "TE1" ),

        // ===== Program Memory Commands (Subprogram) =====
        new Command_Entry( Command: "L1",
                           Syntax: "L1",
                           Description: ( "Begin storing program codes into instrument program memory " +
                                          "(load program mode ON)" ),
                           Category: Command_Category.Subprogram,
                           Parameters: ( "None. All subsequent codes sent to the instrument are stored, " +
                                         "not executed, until Q (end) is received. The stored program " +
                                         "becomes the Home state." ),
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "L1" ),

        new Command_Entry( Command: "Q",
                           Syntax: "Q",
                           Description: "End program memory load sequence (load program mode OFF)",
                           Category: Command_Category.Subprogram,
                           Parameters: ( "None. Terminates the program sequence begun with L1. The " +
                                         "instrument returns to normal execution mode." ),
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "Q" ),

        new Command_Entry( Command: "X1",
                           Syntax: "X1",
                           Description: "Execute the stored program in instrument program memory",
                           Category: Command_Category.Subprogram,
                           Parameters: ( "None. Runs the program stored by L1...Q. SRQ bit 002 is set when " +
                                         "execution completes, if enabled in SM mask." ),
                           Query_Form: "",
                           Default_Value: "N/A",
                           Example: "X1" ),
      };

      Commands.Sort( ( A, B ) => string.Compare( A.Command, B.Command, StringComparison.OrdinalIgnoreCase ) );

      return Commands;
    }
  }
}
