using System.IO.Ports;

namespace Multimeter_Controller
{
  public enum Prologix_Eos_Mode
  {
    CR_LF = 0,
    CR = 1,
    LF = 2,
    None = 3
  }

  public class Scan_Result
  {
    public int Address
    {
      get; set;
    }
    public string ID_String { get; set; } = "";
    public Meter_Type? Detected_Type
    {
      get; set;
    }
  }

  public class Prologix_Serial_Comm : IDisposable
  {
    private SerialPort? _Port;
    private bool _Disposed;

    // Configurable serial settings
    public string Port_Name { get; set; } = "";
    public int Baud_Rate { get; set; } = 115200;
    public int Data_Bits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits Stop_Bits { get; set; } = StopBits.One;
    public Handshake Flow_Control { get; set; } = Handshake.None;

    // Configurable Prologix settings
    public int GPIB_Address { get; set; } = 22;
    public bool Auto_Read { get; set; } = true;
    public bool EOI_Enabled { get; set; } = true;
    public Prologix_Eos_Mode EOS_Mode { get; set; } = Prologix_Eos_Mode.LF;

    // Configurable timeouts
    public int Read_Timeout_Ms { get; set; } = 5000;
    public int Write_Timeout_Ms { get; set; } = 3000;
    public int Prologix_Read_Timeout_Ms { get; set; } = 3000;

    public bool Is_Connected => _Port?.IsOpen == true;

    public event EventHandler<string>? Data_Received;
    public event EventHandler<string>? Error_Occurred;
    public event EventHandler<bool>? Connection_Changed;

    public static string [ ] Get_Available_Ports ( )
    {
      return SerialPort.GetPortNames ( );
    }

    public static int [ ] Get_Available_Baud_Rates ( )
    {
      return new int [ ]
      {
        9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600
      };
    }

    public static int [ ] Get_Available_Data_Bits ( )
    {
      return new int [ ] { 7, 8 };
    }

    public void Connect ( )
    {
      if ( Is_Connected )
      {
        Disconnect ( );
      }

      if ( string.IsNullOrEmpty ( Port_Name ) )
      {
        Raise_Error ( "No COM port selected." );
        return;
      }

      try
      {
        _Port = new SerialPort
        {
          PortName = Port_Name,
          BaudRate = Baud_Rate,
          DataBits = Data_Bits,
          Parity = Parity,
          StopBits = Stop_Bits,
          Handshake = Flow_Control,
          ReadTimeout = Read_Timeout_Ms,
          WriteTimeout = Write_Timeout_Ms,
          NewLine = "\n",
          DtrEnable = true,
          RtsEnable = true
        };

        _Port.Open ( );
        Thread.Sleep ( 200 );

        _Port.DiscardInBuffer ( );
        _Port.DiscardOutBuffer ( );

        Configure_Prologix ( );
        Connection_Changed?.Invoke ( this, true );
      }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Connection failed: {Ex.Message}" );
        _Port?.Dispose ( );
        _Port = null;
      }
    }

    public void Disconnect ( )
    {
      if ( _Port == null )
      {
        return;
      }

      try
      {
        if ( _Port.IsOpen )
        {
          _Port.Close ( );
        }
      }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Disconnect error: {Ex.Message}" );
      }
      finally
      {
        _Port.Dispose ( );
        _Port = null;
        Connection_Changed?.Invoke ( this, false );
      }
    }

    private void Configure_Prologix ( )
    {
      // Set controller mode first
      Send_Prologix_Command ( "++mode 1" );
      Thread.Sleep ( 50 );

      // Disable auto-read immediately to prevent
      // unwanted GPIB bus activity
      Send_Prologix_Command ( "++auto 0" );
      Thread.Sleep ( 50 );

      // Prevent Prologix from saving config to EEPROM
      // so stale settings don't cause issues on next connect
      Send_Prologix_Command ( "++savecfg 0" );
      Thread.Sleep ( 50 );

      // Reset the GPIB bus to a known state
      Send_Prologix_Command ( "++ifc" );
      Thread.Sleep ( 100 );

      // Set GPIB address
      Send_Prologix_Command ( $"++addr {GPIB_Address}" );
      Thread.Sleep ( 50 );

      // Set EOI assertion
      Send_Prologix_Command (
        $"++eoi {( EOI_Enabled ? 1 : 0 )}" );
      Thread.Sleep ( 50 );

      // Set EOS mode (line terminator for GPIB)
      Send_Prologix_Command (
        $"++eos {(int) EOS_Mode}" );
      Thread.Sleep ( 50 );

      // Send device clear to the addressed instrument
      Send_Prologix_Command ( "++clr" );
      Thread.Sleep ( 100 );

      // Set auto-read to desired value last
      Send_Prologix_Command (
        $"++auto {( Auto_Read ? 1 : 0 )}" );
      Thread.Sleep ( 50 );
    }

    public void Send_Prologix_Command ( string Command )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        Raise_Error ( "Not connected." );
        return;
      }

      try
      {
        _Port.WriteLine ( Command );
      }
      catch ( Exception Ex )
      {
        Raise_Error (
          $"Prologix command failed: {Ex.Message}" );
      }
    }

    public void Send_Instrument_Command ( string Command )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        Raise_Error ( "Not connected." );
        return;
      }

      try
      {
        _Port.WriteLine ( Command );
      }
      catch ( Exception Ex )
      {
        Raise_Error (
          $"Send failed: {Ex.Message}" );
      }
    }

    public string Query_Instrument ( string Command )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        Raise_Error ( "Not connected." );
        return "";
      }

      try
      {
        // Clear any pending data
        _Port.DiscardInBuffer ( );

        // Send the query
        _Port.WriteLine ( Command );

        if ( Auto_Read )
        {
          // With auto-read, Prologix automatically
          // requests data from the instrument
          string Response = _Port.ReadLine ( ).Trim ( );
          Data_Received?.Invoke ( this, Response );
          return Response;
        }
        else
        {
          // Without auto-read, must explicitly request
          Thread.Sleep ( 100 );
          _Port.WriteLine ( "++read eoi" );
          string Response = _Port.ReadLine ( ).Trim ( );
          Data_Received?.Invoke ( this, Response );
          return Response;
        }
      }
      catch ( TimeoutException )
      {
        Raise_Error (
          $"Timeout waiting for response to: {Command}" );
        return "";
      }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Query failed: {Ex.Message}" );
        return "";
      }
    }

    public string Verify_GPIB_Address ( int Address )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        return "";
      }

      int Original_Address = GPIB_Address;
      int Original_Timeout = _Port.ReadTimeout;
      bool Original_Auto = Auto_Read;

      try
      {
        _Port.ReadTimeout = Prologix_Read_Timeout_Ms;

        // Disable auto-read for manual query
        _Port.WriteLine ( "++auto 0" );
        Thread.Sleep ( 50 );

        // Switch to the target address
        _Port.WriteLine ( $"++addr {Address}" );
        Thread.Sleep ( 50 );

        // Try *IDN? first (SCPI standard)
        string Response = Try_Query ( "*IDN?" );

        // If no SCPI response, try ID? (older HP format)
        if ( string.IsNullOrEmpty ( Response ) )
        {
          Response = Try_Query ( "ID?" );
        }

        return Response;
      }
      catch ( Exception )
      {
        return "";
      }
      finally
      {
        // Restore original settings
        _Port.ReadTimeout = Original_Timeout;

        _Port.WriteLine ( $"++addr {Original_Address}" );
        Thread.Sleep ( 50 );

        _Port.WriteLine (
          $"++auto {( Original_Auto ? 1 : 0 )}" );
        Thread.Sleep ( 50 );

        GPIB_Address = Original_Address;
      }
    }

    public void Change_GPIB_Address ( int New_Address )
    {
      if ( New_Address < 0 || New_Address > 30 )
      {
        Raise_Error (
          "GPIB address must be 0-30." );
        return;
      }

      GPIB_Address = New_Address;

      if ( Is_Connected )
      {
        Send_Prologix_Command ( $"++addr {GPIB_Address}" );
        Thread.Sleep ( 50 );
      }
    }

    public string Read_Instrument (
      CancellationToken Token = default )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        Raise_Error ( "Not connected." );
        return "";
      }

      try
      {
        _Port.DiscardInBuffer ( );
        _Port.WriteLine ( "++read eoi" );

        // Poll for response so cancellation token can
        // interrupt instead of blocking on ReadLine
        int Elapsed = 0;
        while ( _Port.BytesToRead == 0 )
        {
          Token.ThrowIfCancellationRequested ( );
          Thread.Sleep ( 50 );
          Elapsed += 50;
          if ( Elapsed >= Read_Timeout_Ms )
          {
            Raise_Error (
              "Timeout waiting for instrument response." );
            return "";
          }
        }

        // Allow remaining bytes to arrive
        Thread.Sleep ( 100 );

        string Response = _Port.ReadExisting ( ).Trim ( );
        Data_Received?.Invoke ( this, Response );
        return Response;
      }
      catch ( OperationCanceledException )
      {
        throw;
      }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Read failed: {Ex.Message}" );
        return "";
      }
    }

    public string Query_Prologix_Version ( )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        return "";
      }

      try
      {
        _Port.DiscardInBuffer ( );
        _Port.WriteLine ( "++ver" );
        return _Port.ReadLine ( ).Trim ( );
      }
      catch
      {
        return "";
      }
    }

    public string Raw_Diagnostic ( string Command )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        return "[Port not open]";
      }

      var Result = new System.Text.StringBuilder ( );
      Result.AppendLine (
        $"Port: {_Port.PortName}  Baud: {_Port.BaudRate}  " +
        $"DTR: {_Port.DtrEnable}  RTS: {_Port.RtsEnable}  " +
        $"Handshake: {_Port.Handshake}" );
      Result.AppendLine (
        $"CTS: {_Port.CtsHolding}  DSR: {_Port.DsrHolding}  " +
        $"CD: {_Port.CDHolding}" );

      // Try three terminator styles
      string [ ] Terminators = { "\r\n", "\n", "\r" };
      string [ ] Terminator_Names = { "CR+LF", "LF", "CR" };

      for ( int I = 0; I < Terminators.Length; I++ )
      {
        try
        {
          _Port.DiscardInBuffer ( );
          _Port.DiscardOutBuffer ( );
          Thread.Sleep ( 50 );

          byte [ ] Out_Bytes = System.Text.Encoding.ASCII
            .GetBytes ( Command + Terminators [ I ] );
          _Port.BaseStream.Write ( Out_Bytes, 0,
            Out_Bytes.Length );
          _Port.BaseStream.Flush ( );

          // Wait up to 2 seconds for any bytes
          int Elapsed = 0;
          while ( _Port.BytesToRead == 0 && Elapsed < 2000 )
          {
            Thread.Sleep ( 50 );
            Elapsed += 50;
          }

          if ( _Port.BytesToRead == 0 )
          {
            Result.AppendLine (
              $"[{Terminator_Names [ I ]}] No response" );
            continue;
          }

          // Let remaining bytes arrive
          Thread.Sleep ( 200 );

          byte [ ] Raw = new byte [ _Port.BytesToRead ];
          _Port.Read ( Raw, 0, Raw.Length );

          string Text =
            System.Text.Encoding.ASCII.GetString ( Raw );
          string Hex = BitConverter.ToString ( Raw );

          Result.AppendLine (
            $"[{Terminator_Names [ I ]}] " +
            $"\"{Text.Replace ( "\r", "\\r" )
              .Replace ( "\n", "\\n" )}\"" );
          Result.AppendLine ( $"  Hex: {Hex}" );

          // If we got a response, no need to try other
          // terminators
          break;
        }
        catch ( Exception Ex )
        {
          Result.AppendLine (
            $"[{Terminator_Names [ I ]}] Error: {Ex.Message}" );
        }
      }

      return Result.ToString ( ).TrimEnd ( );
    }

    public List<Scan_Result> Scan_GPIB_Bus (
      IProgress<string>? Progress = null,
      CancellationToken Token = default )
    {
      var Results = new List<Scan_Result> ( );

      if ( _Port == null || !_Port.IsOpen )
      {
        Raise_Error ( "Not connected." );
        return Results;
      }

      int Original_Address = GPIB_Address;
      int Original_Timeout = _Port.ReadTimeout;
      bool Original_Auto = Auto_Read;

      try
      {
        // Use short timeout for scanning empty addresses
        _Port.ReadTimeout = 200;

        // Disable auto-read during scan
        _Port.WriteLine ( "++auto 0" );
        Thread.Sleep ( 50 );

        for ( int Addr = 0; Addr <= 30; Addr++ )
        {
          Token.ThrowIfCancellationRequested ( );

          Progress?.Report ( $"Scanning GPIB address {Addr}..." );

          // Switch to this address
          _Port.WriteLine ( $"++addr {Addr}" );
          Thread.Sleep ( 40 );

          // Try *IDN? first (SCPI standard)
          string Response = Try_Query ( "*IDN?" );

          // If no SCPI response, try ID? (older HP format)
          if ( string.IsNullOrEmpty ( Response ) )
          {
            Response = Try_Query ( "ID?" );
          }

          if ( !string.IsNullOrEmpty ( Response ) )
          {
            Meter_Type? Detected = null;
            string Upper = Response.ToUpperInvariant ( );

            if ( Upper.Contains ( "3458" ) )
            {
              Detected = Meter_Type.Keysight_3458A;
            }
            else if ( Upper.Contains ( "34401" ) )
            {
              Detected = Meter_Type.HP_34401A;
            }
            else if ( Upper.Contains ( "33120" ) )
            {
              Detected = Meter_Type.HP_33120A;
            }

            Results.Add ( new Scan_Result
            {
              Address = Addr,
              ID_String = Response,
              Detected_Type = Detected
            } );

            Progress?.Report (
              $"  Found at {Addr}: {Response}" );
          }
        }
      }
      catch ( OperationCanceledException )
      {
        Progress?.Report ( "Scan cancelled." );
      }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Scan error: {Ex.Message}" );
      }
      finally
      {
        // Restore original settings
        _Port.ReadTimeout = Original_Timeout;

        _Port.WriteLine ( $"++addr {Original_Address}" );
        Thread.Sleep ( 50 );

        _Port.WriteLine (
          $"++auto {( Original_Auto ? 1 : 0 )}" );
        Thread.Sleep ( 50 );

        GPIB_Address = Original_Address;
      }

      Progress?.Report (
        $"Scan complete. Found {Results.Count} instrument(s)." );

      return Results;
    }

    private string Try_Query ( string Command )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        return "";
      }

      try
      {
        _Port.DiscardInBuffer ( );
        _Port.WriteLine ( Command );
        Thread.Sleep ( 50 );
        _Port.WriteLine ( "++read eoi" );
        return _Port.ReadLine ( ).Trim ( );
      }
      catch ( TimeoutException )
      {
        return "";
      }
      catch
      {
        return "";
      }
    }

    private void Raise_Error ( string Message )
    {
      Error_Occurred?.Invoke ( this, Message );
    }

    public void Dispose ( )
    {
      if ( !_Disposed )
      {
        Disconnect ( );
        _Disposed = true;
      }
      GC.SuppressFinalize ( this );
    }
  }
}
