# Multimeter Controller

## Overview

A Windows Forms application for controlling multiple GPIB instruments simultaneously. Supports HP/Keysight bench meters across four transport modes: Prologix GPIB-USB, Prologix Ethernet, Direct RS-232, and NI-VISA (including remote VISA servers). Instruments can be polled in parallel, with real-time charting, CSV recording, and statistical analysis.

---

## Supported Instruments

| Model | Type | Notes |
|---|---|---|
| HP / Keysight 3458A | 8.5-digit DMM | Legacy HP command set; `ID?` identification |
| HP / Keysight 34401A | 6.5-digit DMM | SCPI |
| HP / Keysight 34420A | 7.5-digit Nano-voltmeter | SCPI |
| HP 3456A | 6.5-digit DMM | Pre-SCPI numeric probe |
| HP 53132A | Universal Counter | SCPI |
| HP 33120A | Function Generator | SCPI |
| Generic GPIB | Any SCPI instrument | Sends `MEAS:…` commands |

---

## Connection Modes

### 1. Prologix GPIB-USB

USB-to-GPIB adapter via a COM port. Prologix `++` commands configure the adapter; instrument commands are plain-text GPIB strings.

| Setting | Options | Default |
|---|---|---|
| COM Port | Auto-scanned (refreshable) | First available |
| Baud Rate | Standard rates | 9600 |
| Data Bits | 7, 8 | 8 |
| Parity | None, Odd, Even, Mark, Space | None |
| Stop Bits | One, OnePointFive, Two | One |
| Flow Control | None, XOnXOff, RtsCts | None |
| GPIB Address | 0–30 | 22 |
| EOS Mode | CR+LF, CR, LF, None | LF |

### 2. Direct Serial (RS-232)

RS-232 direct serial connection to an instrument's built-in serial port. Same serial settings as Prologix GPIB-USB.

### 3. Prologix Ethernet

Prologix GPIB-ETHERNET adapter over TCP/IP. The IP address field accepts a manually entered address or is auto-filled by the **Find Prologix** subnet scan button.

### 4. NI-VISA

Uses the NI-VISA runtime (must be installed separately) to communicate with instruments. Supports both local GPIB controllers and **remote VISA servers** (instruments on another machine accessed over the network).

**Prerequisites**

- NI-VISA runtime must be installed on the host machine (available via NI Package Manager).
- The `NationalInstruments.Visa` NuGet package (v25.5.0.13) provides the managed .NET wrapper.
- Compatible hardware includes GPIB-USB-HS, GPIB-USB-B, and PCI/PCIe GPIB controller cards.

**Workflow — no resource string required**

No VISA resource string needs to be entered. The full workflow is:

1. Select **NI-VISA** in the Connection Mode drop-down.
2. Click **Connect** — the NI-VISA `ResourceManager` is initialised.
3. Click **Scan Bus** — the application calls `ResourceManager.Find("GPIB?*INSTR")` to enumerate every GPIB instrument NI-VISA knows about, then probes each one.

**How discovery works**

`Find("GPIB?*INSTR")` returns instruments from all sources NI-VISA is aware of:

- GPIB controllers installed locally on this machine.
- Remote VISA servers configured in **NI-MAX** (Measurement & Automation Explorer) or discovered via mDNS/Bonjour advertisement.

Each scan result carries the full VISA resource string (e.g. `GPIB0::22::INSTR` for local, or `visa://hostname/GPIB0::22::INSTR` for remote), so instruments at the same GPIB address number on different buses are kept separate.

**Session management**

Sessions are opened lazily — a session is created for an instrument address only when the user selects that instrument. Sessions are pooled for the connection lifetime and disposed on disconnect. The `ResourceManager` is held as a long-lived field; disposing it while sessions are open would invalidate them.

**Differences from Prologix**

| Feature | Prologix | NI-VISA |
|---|---|---|
| Address switching | `++addr N` command | Session looked up from pool by address |
| Read trigger | `++read eoi` command | Not needed — EOI detected natively by driver |
| EOS mode | `++eos N` command | Not needed — handled by VISA driver |
| Bus scan | Address 0–30 loop with read/write | `ResourceManager.Find()` + per-address probe |

**Remote VISA server requirements**

- The remote machine must run NI-VISA Server (Windows) or an equivalent VISA-LAN compatible server.
- TCP port 4000 must be accessible between the local and remote machines.
- The remote server must be added in NI-MAX on the local machine so that NI-VISA knows its hostname.

---

## Instrument Management

### Adding an Instrument

1. Select the connection mode.
2. For NI-VISA: click **Connect** (no resource string needed). For Prologix Ethernet: enter the IP address. For serial modes: select the COM port.
3. Click **Connect**.
4. Select the instrument **Type** and optionally set NPLC and role.
5. Click **Add Instrument**.

When adding, the application verifies the GPIB address by querying `*IDN?` (SCPI) and `ID?` (legacy HP). If the detected type differs from the selected type, a mismatch dialog offers to correct it.

### NPLC

Each instrument carries an individual NPLC (Number of Power Line Cycles) value controlling integration time and measurement resolution. **Apply NPLC to All** propagates the current value to all compatible instruments.

### Operations

| Button | Action |
|---|---|
| Add Instrument | Verifies address, detects type, adds to list |
| Remove Instrument | Removes selected instrument |
| Select Instrument | Sets active instrument for command targeting |
| Scan Bus | Scans GPIB bus 0–30 for responding instruments |

---

## Multi-Instrument Poll

**Multi Poll** opens the polling form with a snapshot of the current instrument list. Each instrument is configured independently (measurement function, NPLC) before the polling loop starts. Phase 1 configures all instruments; Phase 2 reads them in round-robin.

### HP 3458A polling (NI-VISA / Prologix)

- NPLC ≥ 10: `TRIG SGL` is sent each cycle; the code waits for integration before reading.
- NPLC < 10: `TRIG AUTO` is sent at setup; each polling cycle issues a read without a trigger command.

### Other instruments

`Query_Instrument` sends the appropriate `MEAS:…` or `READ?` SCPI command and reads the response in one round-trip.

---

## Command Execution

The **Send Command** text box accepts any GPIB command, including Prologix `++` commands.

- Commands ending in `?` are treated as queries; the response is displayed.
- All executed commands are logged to a **Command History** list (max 50 entries).
- Double-clicking a history entry reloads it into the send box.
- The **Diag** button sends the command via the raw diagnostic path.

---

## Settings

Persisted as JSON in `%AppData%\Multimeter_Controller\multi_poll_settings.json`.

| Category | Key Settings |
|---|---|
| Polling | Default poll delay, GPIB timeout, max retry attempts, instrument settle time |
| Data | Max display points, auto-save interval, CSV filename pattern |
| Memory | Max points in memory, auto-trim, warning threshold |
| Display | Chart refresh rate, line thickness, data-dot size, zoom level |
| Analysis | Per-stat toggles for mean, std-dev, min/max, RMS, trend |
| Connection | Default GPIB address, Prologix MAC, scan timeout |

---

## Communication Architecture

### `Instrument_Comm`

Low-level hardware abstraction. Handles all four transport modes behind a single API:

- `Connect()` / `Disconnect_Async()` / `Dispose()`
- `Query_Instrument()` — write command + read response
- `Change_GPIB_Address()` — switch active GPIB address (NI-VISA: switches pooled session)
- `Verify_GPIB_Address()` — three-pass identification (`*IDN?`, `ID?`, numeric probe)
- `Scan_GPIB_BusAsync()` — full bus scan (31 addresses)

**NI-VISA session management**

The `ResourceManager` is stored as a field (`_Resource_Manager`) to ensure sessions remain valid for the lifetime of the connection. Sessions are pooled per GPIB address in `_Visa_Session_Pool`. The `Read_Timeout_Ms` property setter propagates timeout changes to all open sessions immediately.

**Verification timeout**

During `Verify_GPIB_Address`, the NI-VISA session timeout is temporarily reduced to 1.5 s per probe query. If Pass 1 (`*IDN?`) returns nothing, `Session.Clear()` is called to reset any pending GPIB bus state before Pass 2 (`ID?`).

### `GPIB_Manager`

Orchestration layer between polling and `Instrument_Comm`. Applies retry-with-exponential-backoff (up to `Max_Retry_Attempts`) and saves/restores the read timeout around every measurement read.

---

## Instrument Initialization Sequences

### HP34401 / HP33120 / HP34420 / HP53132
1. Set Prologix address (non-VISA modes)
2. Disable auto-read
3. Suppress beeper
4. `*CLS`
5. `SYSTEM:REMOTE`
6. Re-enable beeper

### HP 3458A
1. Optionally send `RESET` (3-second settle delay)
2. Optionally send `END ALWAYS`
3. `TRIG AUTO`
4. `GPIB <address>`
5. `NPLC <value>`

---

## Dependencies

| Dependency | Purpose |
|---|---|
| `NationalInstruments.Visa` (NuGet) | .NET wrapper for NI-VISA runtime |
| `IVI Foundation VISA` (NuGet) | Common VISA interface (`IMessageBasedSession`) |
| `System.IO.Ports` | SerialPort for USB/serial transports |
| `System.Windows.Forms.DataVisualization` | Chart rendering |
| NI-VISA runtime | Must be installed on the host machine separately |

---

## Project Info

| Item | Detail |
|---|---|
| Framework | .NET 9.0, Windows Forms |
| Target | win-x64 (self-contained) |
| Namespace | `Multimeter_Controller` |
