# Form1.cs — Keysight 3458A Multimeter Controller

## Overview

`Form1.cs` is the main application window for controlling a **Keysight (HP) 3458A 8.5-digit digital multimeter** via a **Prologix GPIB-USB adapter**. It provides the primary user interface and orchestrates all instrument communication, configuration, and command interaction.

---

## Hardware Context

| Item | Detail |
|---|---|
| Instrument | Keysight / HP / Agilent 3458A Digital Multimeter |
| Interface | Prologix GPIB-USB-HS Controller (USB-to-GPIB bridge) |
| Protocol | Prologix `++` commands configure the adapter; instrument commands are sent as plain-text GPIB strings |

---

## Functional Areas

### 1. Command Reference List (Left Panel)

Displays all supported 3458A GPIB commands in a scrollable `ListBox`. Selecting a command populates a read-only detail TextBox showing:

- Command syntax
- Parameters
- Query form
- Default value
- Example usage

An **Open Dictionary** button launches the full searchable command dictionary in a separate modal dialog (`Dictionary_Form`).

### 2. Connection Settings (Right Panel)

Configures the serial port connection to the Prologix GPIB-USB-HS adapter.

**Serial Port Settings**

| Setting | Options | Default |
|---|---|---|
| COM Port | Auto-scanned (refreshable) | First available |
| Baud Rate | Standard rates | 115200 |
| Data Bits | 7, 8 | 8 |
| Parity | None, Odd, Even, Mark, Space | None |
| Stop Bits | One, OnePointFive, Two | One |
| Flow Control | None, XOnXOff, RtsCts, RtsCtsXOnXOff | None |

**Prologix / GPIB Settings**

| Setting | Options | Default |
|---|---|---|
| GPIB Address | 0–30 | 22 (3458A default) |
| EOS Mode | CR+LF, CR, LF, None | LF |
| Auto Read (`++auto`) | Checkbox | Checked |
| EOI Enabled (`++eoi`) | Checkbox | Checked |

A **Defaults** button restores all settings to their recommended values. While connected, all serial settings are disabled to prevent mid-session changes.

### 3. Instrument Management

Instruments are maintained in an internal list (`_Instruments`) and displayed in a bound `ListBox`. Supported operations:

- **Add Instrument** — Verifies GPIB address if connected, detects meter type, prompts on mismatch, initializes the instrument to remote mode.
- **Remove Instrument** — Removes the selected instrument from the list.
- **Select Instrument** — Sets the active instrument for command targeting, restores its NPLC value.
- **Scan Bus** — Scans the GPIB bus for all responding instruments (cancellable), auto-adds or refreshes discovered devices.

### 4. NPLC Control

Each instrument carries an individual **NPLC (Number of Power Line Cycles)** value that controls integration time and measurement accuracy. The NPLC combo is populated based on the selected meter type's supported values. An **Apply NPLC to All** button propagates the current NPLC to all compatible instruments in the list.

### 5. Connection Modes

Three transport modes are supported:

| Mode | Description |
|---|---|
| Prologix GPIB | GPIB-USB-HS adapter via COM port |
| Direct Serial (RS-232) | RS-232 direct serial connection |
| Ethernet | Prologix Ethernet adapter (TCP/IP) |

A **Find Prologix** button scans the local subnet and auto-fills the IP address field when an Ethernet adapter is detected.

### 6. Command Execution

- The **Send Command** text box accepts any GPIB command, including Prologix `++` commands.
- Commands ending in `?` are treated as queries and their response is displayed.
- All executed commands are logged to a **Command History** list (max 50 entries). Double-clicking a history entry re-loads it into the send box.
- A **Diag** button sends the current command via a raw diagnostic path for low-level troubleshooting.

### 7. Multi-Instrument Poll

The **Multi Poll** button opens `Multi_Instrument_Poll_Form`, passing a cloned snapshot of the current instrument list. Any pending NPLC edits are committed before cloning.

### 8. Execution Trace

A toggleable **Trace** button activates `Trace_Execution`, which logs internal method entry/exit for diagnostics. The button background turns yellow while tracing is active.

---

## Communication Event Handling

The form subscribes to three events from `Instrument_Comm`:

| Event | Behavior |
|---|---|
| `Connection_Changed` | Updates the status label (`Connected` / `Disconnected`) and toggles UI enable/disable state. Thread-safe via `Invoke`. |
| `Error_Occurred` | Displays errors in a `MessageBox`. Thread-safe. |
| `Data_Received` | Placeholder for future data display or logging. |

---

## Key Private Fields

| Field | Type | Purpose |
|---|---|---|
| `_All_Commands` | `List<Command_Entry>` | Commands for the currently selected meter type |
| `_Comm` | `Instrument_Comm` | Serial/GPIB communication layer |
| `_Selected_Meter` | `Meter_Type` | Currently active meter type |
| `_Instruments` | `List<Instrument>` | All instruments in the session |
| `_Selected_Index` | `int` | Index of the currently selected instrument |
| `_Settings` | `Application_Settings` | Persisted application settings |
| `_Scan_Cts` | `CancellationTokenSource?` | Cancellation token for active bus scans |
| `_Is_Scanning` | `bool` | Guards against concurrent bus scans |

---

## Instrument Initialization Sequences

On adding or connecting an instrument, `Initialize_Remote_For_Instrument` runs a type-specific initialization:

**HP34401 / HP33120 / HP34420 / HP53132:**
1. Set Prologix address
2. Disable auto-read
3. Suppress beeper
4. Send `*CLS`
5. Send `SYSTEM:REMOTE`
6. Re-enable beeper

**HP3458A:**
1. Optionally send `RESET` (3-second settle delay)
2. Optionally send `END ALWAYS`
3. Send `TRIG AUTO`
4. Send `GPIB <address>`
5. Send `NPLC <value>`

---

## Thread Safety

`Safe_UI_Update` provides a general-purpose thread-safe UI dispatcher. All event handlers from `Instrument_Comm` use `InvokeRequired` checks before touching UI elements, preventing cross-thread exceptions during async operations.

---

## Dependencies

| Dependency | Purpose |
|---|---|
| `System.IO.Ports` | SerialPort for USB-serial communication |
| `Command_Dictionary_Class.cs` | Static command reference data |
| `Instrument_Comm.cs` | Serial/GPIB communication layer |
| `Dictionary_Form.cs` | Full searchable command dictionary dialog |
| `Multi_Instrument_Poll_Form.cs` | Multi-instrument simultaneous polling |
| `Session_Settings_Form.cs` | Runtime session configuration |
| `Settings_Form.cs` | Persisted application settings editor |
| `Recording_Playback_Form.cs` | Playback of recorded measurement data |
| `NPLC_Info_Form.cs` | NPLC reference information |
| `Trace_Execution_Namespace` | Execution tracing / diagnostics |
| `Form1.Designer.cs` | WinForms designer-generated layout |

---

## Project Info

| Item | Detail |
|---|---|
| Author | Mike |
| Framework | .NET 9.0, Windows Forms |
| Namespace | `Multimeter_Controller` |