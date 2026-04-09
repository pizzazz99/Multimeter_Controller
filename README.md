# Multimeter Controller
### Multi-Instrument GPIB Polling & Data Logging — .NET 9.0 / Windows Forms

---

## Overview

Multimeter Controller is a Windows Forms application for controlling, polling, and logging data from multiple HP / Agilent / Keysight test instruments simultaneously over GPIB (IEEE-488). It connects via a **Prologix GPIB-USB** or **Prologix GPIB-Ethernet** adapter and supports a broad range of instrument types — precision DMMs, function generators, frequency counters, and LCR meters.

The application provides:

- A **command dictionary** for every supported instrument — browse, search, and execute SCPI or HP-IB commands directly from the UI
- A **multi-instrument poller** that logs readings from all connected instruments simultaneously to time-series charts
- **Drift monitoring** across multiple units of the same instrument type
- **Statistics** (mean, std dev, min, max, RMS, trend) computed incrementally with O(1) accessors
- **Session and instrument configuration** persisted across runs

---

## Supported Instruments

| Model | Type | Command Set | Interface |
|-------|------|-------------|-----------|
| HP 3458A | 8.5-digit DMM | HP-IB (legacy) | GPIB only |
| HP 34401A | 6.5-digit DMM | SCPI | GPIB + RS-232 |
| HP 34411A | 6.5-digit high-speed DMM | SCPI | GPIB + USB + LAN |
| HP 34420A | 7.5-digit nano-volt / micro-ohm meter | SCPI | GPIB + RS-232 |
| HP 3456A | 6.5-digit voltmeter (legacy) | HP-IB (legacy) | GPIB only |
| HP 33120A | 15 MHz function / ARB generator | SCPI | GPIB + RS-232 |
| HP 33220A | 20 MHz function / ARB generator | SCPI | GPIB + USB + RS-232 |
| HP 53132A | 225 MHz universal counter | SCPI | GPIB + RS-232 |
| HP 53181A | 225 MHz frequency counter | SCPI | GPIB only |
| HP 4263B | 100 Hz – 100 kHz LCR meter | SCPI | GPIB only |
| Generic GPIB | Any GPIB instrument | User-defined | GPIB |

> **Note:** The HP 3458A and HP 3456A predate the SCPI standard and use proprietary HP-IB command sets. Their dictionaries reflect the native command syntax documented in the original HP programming references.

---

## Connection Hardware

All instruments communicate via GPIB (IEEE-488) through one of:

- **Prologix GPIB-USB** — connects via a COM port (USB virtual serial)
- **Prologix GPIB-Ethernet** — connects via TCP/IP; the application can auto-scan the local subnet to locate the adapter

Each instrument requires a unique GPIB bus address (1–30), set on the instrument's front panel and matched in the application.

**Typical default addresses:**

| Instrument | Address |
|------------|---------|
| HP 3458A | 10 |
| HP 34401A | 22 |
| HP 34411A | 23 |
| HP 34420A | 5 |
| HP 3456A | 13 |
| HP 33120A | 11 |
| HP 33220A | 12 |
| HP 53132A | 7 |
| HP 53181A | 8 |
| HP 4263B | 17 |

---

## Architecture

### Project Structure

```
Multimeter_Controller/
├── Command_Dictionary_Class.cs       — Enums, Command_Entry model, router
├── Instrument_Class.cs               — Instrument and Instrument_Series models
├── HP3458_Command_Dictionary_Class.cs
├── HP34401_Command_Dictionary_Class.cs
├── HP34411_Command_Dictionary_Class.cs
├── HP34420_Command_Dictionary_Class.cs
├── HP3456_Command_Dictionary_Class.cs
├── HP33120_Command_Dictionary_Class.cs
├── HP33220_Command_Dictionary_Class.cs
├── HP53132_Command_Dictionary_Class.cs
├── HP53181_Command_Dictionary_Class.cs
├── HP4263_Command_Dictionary_Class.cs
├── Instrument_Comm.cs                — GPIB / serial communication layer
├── Form1.cs                          — Main launcher form
├── Multi_Instrument_Poll_Form.cs     — Multi-instrument polling and charting
├── Dictionary_Form.cs                — Full searchable command dictionary dialog
├── Settings_Form.cs                  — Application settings editor
├── Recording_Playback_Form.cs        — Recorded session playback
├── Application_Settings.cs          — Persistent settings model
└── Rich_Text_Popup_Namespace/        — Fluent popup builder
```

### Key Classes

#### `Meter_Type` (enum)
Identifies each supported instrument. Used throughout for routing, display, NPLC tables, and comms overhead.

```csharp
HP3458, HP34401, HP34411, HP33120, HP33220,
HP34420, HP53132, HP53181, HP4263, HP3456, Generic_GPIB
```

#### `Command_Category` (enum)
Groups commands for UI filtering. Two categories were added to support non-DMM instruments:

```csharp
Measurement, Configuration, Trigger, Math,
Modulation,      // function generators — AM, FM, PM, FSK, PWM
Compensation,    // LCR meters — open/short/load correction
System, Memory, IO, Calibration, Subprogram
```

#### `Command_Entry`
Data record for a single instrument command. Fields: `Command`, `Syntax`, `Description`, `Category`, `Parameters`, `Query_Form`, `Default_Value`, `Example`. Derived properties: `Can_Execute`, `Can_Query`, `Is_Query`, `Get_Command_Mode()`.

#### `Command_Dictionary_Class`
Static router — `Get_All_Commands(Meter_Type)` dispatches to the correct instrument dictionary and returns `List<Command_Entry>`.

#### `Meter_Type_Extensions`
Extension methods on `Meter_Type`:

| Method | Purpose |
|--------|---------|
| `Get_Name()` | Human-readable instrument name |
| `Is_Legacy_HP()` | True for pre-SCPI HP-IB instruments |
| `Is_Non_DMM()` | True for generators, counters, LCR meter |
| `Get_NPLC_Values()` | Valid NPLC values for UI spinner |
| `Get_Default_NPLC()` | Factory default NPLC |
| `To_Combo_Index()` / `From_Combo_Index()` | UI combo box mapping |

#### `Instrument`
Per-instrument configuration: name, GPIB address, type, NPLC, poll interval, visibility, master flag.

Key properties:

| Property | DMM behaviour | Non-DMM behaviour |
|----------|--------------|-------------------|
| `Is_DMM` | true | false |
| `Poll_Delay_Ms` | Derived from NPLC + overhead | `Poll_Interval_Ms` + overhead |
| `Poll_Interval_Ms` | Not used | User-configurable (floor 50 ms) |
| `Display_Digits` | NPLC-dependent | Fixed by instrument type |
| `Comms_Overhead_Ms` | Per-type estimate | Per-type estimate |

#### `Instrument_Series`
Wraps `Instrument` and accumulates time-series measurement data. Provides O(1) statistics via Welford's online algorithm.

Timing properties correctly handle DMM vs non-DMM:

| Property | DMM | Non-DMM |
|----------|-----|---------|
| `Integration_Ms` | `NPLC × (1000/60)` | `Poll_Interval_Ms` |
| `Settle_Ms` | `Integration_Ms × 2` | `Poll_Interval_Ms` |
| `Readings_Per_Min` | `60000 / Settle_Ms` | `60000 / Poll_Interval_Ms` |
| `NPLC_Warning_Text` | Slow / Moderate / Fast | "Fixed interval" |
| `NPLC_Warning_Color` | Orange / Blue / Black | Gray |

---

## NPLC — Number of Power Line Cycles

NPLC is the primary accuracy/speed control for integrating DMMs. It specifies the measurement integration window as a multiple of the AC mains period.

```
At 60 Hz:  1 PLC = 16.667 ms
At 50 Hz:  1 PLC = 20.000 ms

T_integration = NPLC × (1 / f_line)
```

Increasing NPLC improves accuracy two ways simultaneously:

1. **Deterministic cancellation** of periodic 50/60 Hz interference (perfect at integer NPLC values)
2. **Statistical averaging** of random noise — improves at √NPLC rate

Autozero doubles effective measurement time at any NPLC setting. The application accounts for this in `Settle_Ms`.

**Non-DMM instruments** (generators, counters, LCR meter) do not have an NPLC concept. They use a user-configurable `Poll_Interval_Ms` fixed period instead. The NPLC spinner is disabled for these instrument types in the UI.

---

## Drift Monitoring Use Cases

The multi-instrument poller can monitor any measurable parameter across multiple units simultaneously. Useful scenarios:

| Instruments | Query to poll | What you see |
|-------------|--------------|-------------|
| Multiple HP 33120A / 33220A | `FREQ?` | Frequency drift between generators |
| Multiple HP 33120A / 33220A | `VOLT?` | Amplitude drift between generators |
| Multiple HP 53132A / 53181A | `READ?` | Counter-to-counter frequency measurement variation |
| Multiple HP 3458A | `READ?` | Long-term DC voltage reference drift |
| Multiple HP 4263B | `FETCH?` | Component measurement drift over temperature |
| Mixed DMMs | `READ?` | Cross-instrument comparison against master reference |

When three or more instruments are polled simultaneously, one must be designated as the **master**. All delta calculations are computed relative to the master's readings to ensure consistent baselines.

---

## HP 4263B — Special Handling

The 4263B always returns **two values per measurement** plus a status code:

```
FETCH? → <primary>,<secondary>,<status>
```

Status codes:

| Code | Meaning |
|------|---------|
| 0 | Normal measurement |
| 1 | Primary parameter overrange |
| 2 | Secondary parameter overrange |
| 3 | Both parameters overrange |
| 4 | Signal source overload |

Always validate the status field before logging values. The 4263B dictionary includes a full `Compensation` category covering open, short, and load correction workflows.

**Compensation workflow:**
```
CORR:LENG 1          → set cable length (0, 1, or 2 metres)
CORR:OPEN:EXEC       → measure open-circuit correction
CORR:OPEN:STAT ON    → enable open correction
CORR:SHOR:EXEC       → measure short-circuit correction
CORR:SHOR:STAT ON    → enable short correction
```

---

## Command Dictionary

Each instrument dictionary is a static class with a `Get_All_Commands()` method returning `List<Command_Entry>`, sorted alphabetically. The router in `Command_Dictionary_Class` dispatches based on `Meter_Type`.

Every `Command_Entry` contains:

- `Command` — short-form SCPI mnemonic or HP-IB command
- `Syntax` — full long-form syntax with optional parameters
- `Description` — human-readable purpose
- `Category` — `Command_Category` enum value for UI filtering
- `Parameters` — valid parameter values and ranges
- `Query_Form` — query variant (`null` = not queryable)
- `Default_Value` — factory default after `*RST`
- `Example` — ready-to-send example string

`Query_Form` semantics:
- `null` — command has no query form (e.g. `*CLS`, `SYST:BEEP`)
- `""` — auto-generate by appending `?` to `Command`
- any other string — use as-is

---

## Instrument Initialization

On `Add_Instrument_Button_Click`, the application:

1. Checks for duplicate GPIB addresses
2. Optionally verifies the instrument by sending `*IDN?` (or equivalent for legacy HP-IB)
3. Detects the instrument type from the IDN response and offers correction if mismatched
4. Calls `Initialize_Remote_For_Instrument()` which sends type-specific setup commands

Type-specific initialization:
- **HP 34401A / 33120A / 34420A / 53132A** — suppresses beep, sends `*CLS`, sets `SYSTEM:REMOTE`
- **HP 3458A** — optional `RESET`, sets `END ALWAYS`, `TRIG AUTO`, applies `NPLC`
- **New types (34411A, 33220A, 53181A, 4263B)** — fall through to the `default` case; add type-specific cases as needed

---

## Session Settings

The `Application_Settings` class persists all configurable parameters between runs. Key settings:

| Group | Settings |
|-------|---------|
| Polling | Poll delay, continuous mode, max display points, GPIB timeout |
| Display | Chart refresh rate, combined/split view, legend, tooltips, zoom |
| Recording | Save folder, filename pattern, auto-save interval |
| Memory | Max points in memory, trim threshold |
| Analysis | Mean, std dev, min/max, RMS, trend, sample rate |
| Connection | Default GPIB address, Prologix IP, scan timeout, skew threshold |

---

## Requirements

- **OS:** Windows 10 / 11 (64-bit)
- **Framework:** .NET 9.0
- **Hardware:** Prologix GPIB-USB or Prologix GPIB-Ethernet adapter
- **Instruments:** Any combination from the supported list above

---

## Building

```bash
git clone <repo>
cd Multimeter_Controller
dotnet build -c Release
dotnet run
```

No external NuGet packages beyond the .NET 9.0 Windows Forms SDK are required. `System.Windows.Forms.DataVisualization.Charting` is used for the live chart — this is included in the .NET Windows Forms workload.

---

## Authors

Mike — W&W Co., Since 1969