# Keysight 3458A Multimeter Controller

A Windows Forms desktop application for controlling **GPIB test instruments** over a **Prologix GPIB-USB-HS** adapter. Originally built for the Keysight (HP/Agilent) 3458A 8.5-Digit Digital Multimeter, it now supports multiple instrument types on the same GPIB bus.

## Overview

The 3458A is one of the highest-precision digital multimeters ever produced, capable of 8.5 digits of resolution on DC voltage measurements. This application provides a graphical interface for connecting to instruments via a Prologix USB-to-GPIB bridge, browsing full GPIB command sets, taking measurements with real-time charting, and polling multiple instruments simultaneously.

## Supported Instruments

| Instrument | Type | Command Dictionary |
|-----------|------|-------------------|
| **Keysight / HP / Agilent 3458A** | 8.5-Digit DMM | 70+ commands across 9 categories |
| **HP 34401A** | 6.5-Digit DMM | Full command set |
| **HP 33120A** | Function Generator | Full command set |

## Features

### Multi-Instrument Management
- **Add/Remove instruments** with name, GPIB address, and meter type
- **GPIB bus scan** to auto-detect connected instruments and identify types from ID strings
- **Live instrument switching** - change the active instrument on-the-fly while connected
- **Address verification** when adding instruments (queries the instrument for its ID)

### Command Reference
- **Quick-reference list** on the main form showing supported commands with brief descriptions (changes per instrument type)
- **Detail panel** displaying full command metadata (syntax, parameters, query form, default value, example) when a command is selected
- **Full dictionary window** with a searchable, filterable DataGridView containing every command
  - Filter by category (Measurement, Configuration, Trigger, Math, System, Memory, I/O, Calibration, Subprogram)
  - Real-time text search across command names, syntax, descriptions, and parameters
  - Double-click any row for a detailed popup

### Voltage Reader (Single Instrument)
- **Measurement functions**: DC/AC Voltage, DC/AC Current, 2-Wire/4-Wire Ohms, Frequency, Period, Temperature, and more (filtered per meter type)
- **Configurable readings**: fixed count or continuous mode with adjustable delay
- **Real-time charting** with four graph styles:
  - Line (with gradient fill under the curve)
  - Bar
  - Scatter
  - Step
- **Current value display** with auto-scaling SI units (V, mV, uV, kHz, MOhm, etc.)
- **Data recording** to timestamped CSV files
- **Load and replay** previously recorded data

### Multi-Instrument Poller
- **Simultaneous polling** of all instruments on the GPIB bus
- **Stacked subplot chart** with one subplot per instrument, shared time axis
- **Per-instrument line colors** from a configurable 4-color palette
- **Cycle counter** and per-instrument progress reporting
- **Data recording** to CSV with aligned timestamps across instruments
- **Load and replay** multi-instrument recordings

### Chart Theming
- **Customizable chart colors** shared across Voltage Reader and Multi-Instrument Poller
- **Theme Settings dialog** with clickable color swatches for:
  - Chart background
  - Grid lines
  - Axis labels
  - Subplot separators
  - 4 line/series colors
- **Built-in presets**: Dark (Grafana-style) and Light (lab-friendly)
- **Persistent settings** saved to `chart_theme.json` and restored on startup

### Serial / GPIB Connection
- Configurable serial port settings:
  - COM port selection with refresh capability
  - Baud rate (9600 to 921600; default 115200)
  - Data bits (7 or 8; default 8)
  - Parity (None, Odd, Even, Mark, Space; default None)
  - Stop bits (One, OnePointFive, Two; default One)
  - Flow control / handshake (None, XOnXOff, RtsCts; default None)
- Prologix GPIB-USB adapter settings:
  - GPIB address (0-30; default 22)
  - EOS mode (CR+LF, CR, LF, None; default LF)
  - Auto Read after query (`++auto`; default enabled)
  - EOI assertion (`++eoi`; default enabled)
- One-click "Defaults" button to restore recommended settings
- Connection status indicator with color-coded label
- Diagnostic command button for raw Prologix commands (`++ver`, etc.)
- Response logging panel for all command/response interactions

## Hardware Requirements

| Component | Details |
|-----------|---------|
| **Instruments** | Keysight 3458A, HP 34401A, HP 33120A (or any GPIB instrument) |
| **GPIB Adapter** | Prologix GPIB-USB-HS Controller |
| **Connection** | USB (host PC) to GPIB (instrument) via Prologix adapter |
| **Operating System** | Windows 10/11 (x64) |

## Default Connection Settings

These defaults match the recommended configuration for a Prologix GPIB-USB-HS adapter:

| Setting | Default Value |
|---------|---------------|
| Baud Rate | 115200 |
| Data Bits | 8 |
| Parity | None |
| Stop Bits | One |
| Flow Control | None |
| GPIB Address | 22 |
| EOS Mode | LF |
| Auto Read | Enabled |
| EOI | Enabled |

## Project Structure

```
Multimeter_Controller/
├── Program.cs                         # Application entry point
├── Form1.cs / .Designer.cs            # Main window (instruments, commands, connection)
├── Voltage_Reader_Form.cs / .Designer  # Single-instrument reader with charting
├── Multi_Poll_Form.cs / .Designer      # Multi-instrument poller with subplots
├── Theme_Settings_Form.cs / .Designer  # Chart theme customization dialog
├── Chart_Theme.cs                     # Shared theme data + JSON persistence
├── Command_Dictionary.cs             # Keysight 3458A command reference
├── HP34401A_Command_Dictionary.cs    # HP 34401A command reference
├── HP33120A_Command_Dictionary.cs    # HP 33120A command reference
├── Dictionary_Form.cs / .Designer     # Searchable command dictionary dialog
├── Prologix_Serial_Comm.cs           # Serial/GPIB communication layer
├── Graph_Captures/                    # Recorded measurement data (CSV files)
└── Properties/
    ├── AssemblyInfo.cs
    ├── Resources.Designer.cs
    └── Settings.Designer.cs
```

### Key Files

- **Form1.cs** - Main application window. Manages the instrument list (add, remove, scan, switch), displays a quick-reference command list that updates per meter type, and provides the full connection settings panel for the Prologix adapter.

- **Voltage_Reader_Form.cs** - Single-instrument measurement window. Configures the instrument for a selected measurement function, takes readings in a loop, and plots results in real-time with four selectable graph styles. Supports recording to CSV and replaying saved data.

- **Multi_Poll_Form.cs** - Multi-instrument polling window. Queries all instruments on the bus in round-robin fashion, displaying results as stacked subplots with per-instrument line colors. Supports recording aligned multi-instrument data to CSV.

- **Chart_Theme.cs** - Shared theme class used by both chart forms. Stores background, grid, label, separator, and 4 line colors. Persists to `chart_theme.json` and includes Dark and Light presets.

- **Theme_Settings_Form.cs** - Modal dialog for customizing chart colors. Presents clickable color swatches that open the system ColorDialog, with preset buttons for quick theme switching.

- **Command_Dictionary.cs / HP34401A_Command_Dictionary.cs / HP33120A_Command_Dictionary.cs** - Static command references for each supported instrument type. Each entry includes syntax, parameter descriptions, query form, default value, and usage example.

- **Dictionary_Form.cs** - Modal dialog presenting the full command dictionary in a DataGridView with real-time search filtering and category-based filtering.

- **Prologix_Serial_Comm.cs** - Communication layer that manages the serial port connection and Prologix adapter configuration. Provides methods for sending Prologix `++` commands, instrument commands, and queries. Raises events for connection changes, errors, and received data.

## Supported 3458A Command Categories

| Category | Count | Description |
|----------|-------|-------------|
| Measurement | 14 | DCV, ACV, ACDCV, DCI, ACI, ACDCI, OHM, OHMF, FREQ, PER, DSAC, DSDC, SSAC, SSDC |
| Configuration | 14 | RANGE, ARANGE, NPLC, APER, NDIG, RES, AZERO, FIXEDZ, LFILTER, ACBAND, SETACV, LFREQ, OCOMP, DELAY |
| Trigger | 7 | TARM, TRIG, NRDGS, TIMER, SWEEP, LEVEL, SLOPE |
| Math | 11 | MATH, MMATH, NULL, SCALE, PERC, DB, DBM, FILTER, STAT, RMATH, PFAIL |
| System | 16 | RESET, PRESET, ID, ERR, ERRSTR, AUXERR, STB, SRQ, EMASK, END, LINE, TEMP, REV, OPT, TEST, SCRATCH |
| Memory | 7 | MEM, MSIZE, RMEM, MCOUNT, MFORMAT, SSTATE, RSTATE |
| I/O | 8 | DISP, BEEP, OFORMAT, INBUF, TBUFF, GPIB, EXTOUT, LOCK |
| Calibration | 5 | ACAL, CAL, CALNUM, CALSTR, SECURE |
| Subprogram | 6 | SUB, SUBEND, CALL, CONT, PAUSE, DELSUB |

## Build Requirements

- **.NET 9.0** (Windows target)
- **Windows Forms** (`net9.0-windows`)
- **Runtime:** `win-x64`
- **IDE:** Visual Studio 2022+ or VS Code with C# Dev Kit

## Building

```bash
dotnet build
dotnet run
```

Or open the `.csproj` file in Visual Studio and press F5.

## License

Private project - not licensed for distribution.
