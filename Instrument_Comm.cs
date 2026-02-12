using System.CodeDom;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq.Expressions;
using System.Text;

namespace Multimeter_Controller
{
  public enum Connection_Mode
  {
    Prologix_GPIB,
    Direct_Serial
  }

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

  public class Instrument_Comm : IDisposable
  {
    private SerialPort? _Port;
    private bool _Disposed;

    // Connection mode
    public Connection_Mode Mode
    {
      get; set;
    } =
      Connection_Mode.Prologix_GPIB;

    // Configurable serial settings
    public string Port_Name { get; set; } = "";
    public int Baud_Rate { get; set; } = 115200;
    public int Data_Bits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits Stop_Bits { get; set; } = StopBits.One;
    public Handshake Flow_Control { get; set; } = Handshake.None;

    // Configurable Prologix settings (ignored in Direct_Serial mode)
    public int GPIB_Address { get; set; } = 22;
    public bool Auto_Read { get; set; } = false;
    public bool EOI_Enabled { get; set; } = true;
    public Prologix_Eos_Mode EOS_Mode { get; set; } = Prologix_Eos_Mode.LF;

    // Configurable timeouts
    // Read timeout increased to 15s to support NPLC=100
    // (NPLC=100 takes ~3-6 seconds per measurement)
    public int Read_Timeout_Ms { get; set; } = 15000;
    public int Write_Timeout_Ms { get; set; } = 5000; // 5s - allows for slow instruments
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

        if ( Mode == Connection_Mode.Prologix_GPIB )
        {
          Configure_Prologix ( );
        }

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
          // Forcefully clear any pending operations
          try
          {
            _Port.DiscardInBuffer ( );
            _Port.DiscardOutBuffer ( );
          }
          catch { /* Ignore errors during cleanup */ }

          // Disable control lines to release the port
          try
          {
            _Port.DtrEnable = false;
            _Port.RtsEnable = false;
          }
          catch { /* Ignore errors during cleanup */ }

          // Close the base stream first (more forceful)
          try
          {
            _Port.BaseStream?.Close ( );
          }
          catch { /* Ignore errors during cleanup */ }

          // Then close the port
          _Port.Close ( );
        }
      }
      catch ( Exception Ex )
      {
        // Log but don't fail - we're disconnecting anyway
        Raise_Error ( $"Disconnect warning: {Ex.Message}" );
      }
      finally
      {
        // Always dispose and clear reference
        try
        {
          _Port.Dispose ( );
        }
        catch { /* Force cleanup even if dispose fails */ }

        _Port = null;
        Connection_Changed?.Invoke ( this, false );
      }
    }

    private void Configure_Prologix ( )
    {
      // Set controller mode first
      Send_Prologix_Command ( "++mode 1" );
      Thread.Sleep ( 50 );

      // Set auto-read from configuration
      // auto 0: only read when we explicitly send ++read eoi
      // auto 1: Prologix reads after every command (can cause
      //         errors on commands like *CLS that have no response)
      Send_Prologix_Command ( $"++auto {( Auto_Read ? 1 : 0 )}" );
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

      // Confirm auto-read setting
      Send_Prologix_Command ( $"++auto {( Auto_Read ? 1 : 0 )}" );
      Thread.Sleep ( 50 );
    }

    public void Send_Prologix_Command ( string Command )
    {
      if ( Mode == Connection_Mode.Direct_Serial )
      {
        return;
      }

      if ( _Port == null || !_Port.IsOpen )
      {
        throw new InvalidOperationException ( "Not connected." );
      }

      try
      {
        _Port.WriteLine ( Command );
      }
      catch ( TimeoutException )
      {
        // Let caller handle timeout
        throw;
      }
      catch ( Exception Ex )
      {
        throw;
      }
    }

    // Check if instrument has data ready using serial poll
    // Returns true if MAV (Message Available) bit is set
    // Note: May return false if instrument is in talk mode
    public bool Is_Data_Available ( )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        return false;
      }

      try
      {
        _Port.DiscardInBuffer ( );

        if ( Mode == Connection_Mode.Prologix_GPIB )
        {
          // Use Prologix serial poll command
          _Port.WriteLine ( "++spoll" );
        }
        else
        {
          // Direct RS-232: Query instrument status byte
          _Port.WriteLine ( "*STB?" );
        }

        // Wait briefly for response (reduced for responsiveness)
        Thread.Sleep ( 50 );

        if ( _Port.BytesToRead > 0 )
        {
          string Response = _Port.ReadLine ( ).Trim ( );

          if ( int.TryParse ( Response, out int Status_Byte ) )
          {
            // Bit 4 (value 16) is MAV (Message Available)
            bool MAV = ( Status_Byte & 0x10 ) != 0;
            return MAV;
          }
        }

        return false;
      }
      catch ( TimeoutException )
      {
        // Timeout likely means instrument is in talk mode
        // Return true to trigger a read attempt
        return true;
      }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Status poll failed: {Ex.Message}" );
        return false;
      }
    }







    public void Send_Instrument_Command ( string Command )
    {
      if ( _Port == null || !_Port.IsOpen )
        throw new InvalidOperationException ( "Not connected." );

      // Build the exact string that will be sent
      string New_Line = _Port.NewLine ?? "";
      string Full_Command = Command + New_Line;

      // Debug display with escaped control chars
      string Visible = Full_Command
        .Replace ( "\r", "\\r" )
        .Replace ( "\n", "\\n" );

      // Debug.WriteLine ( $"TX [{Visible}]" );


      Command = Command.Trim ( );

      try
      {
        _Port.DiscardInBuffer ( );
        _Port.WriteLine ( Command );

      }
      catch ( TimeoutException )
      {
        throw;
      }
    }

    public string Query_Instrument ( string Command )
    {
      return Query_Instrument ( Command,
        CancellationToken.None );
    }

    public string Query_Instrument ( string Command,
      CancellationToken Token )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        Raise_Error ( "Not connected." );
        return "";
      }

      try
      {

        // Debug.WriteLine ( $"Querying: {Command}" );
        // Send the query
        Send_Instrument_Command ( Command );

        // Use Read_Instrument with cancellation support
        return Read_Instrument ( Token );
      }
      catch ( OperationCanceledException )
      {
        throw; // Propagate cancellation
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
      if ( Mode == Connection_Mode.Direct_Serial )
      {
        // In direct serial mode, just query the instrument
        return Query_Instrument ( "*IDN?" );
      }

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
      if ( Mode == Connection_Mode.Direct_Serial )
      {
        return;
      }

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

    public string? Read_Instrument ( CancellationToken Token = default )
    {
      if ( _Port == null || !_Port.IsOpen )
      {
        Raise_Error ( "Not connected." );
        return "";
      }

      try
      {


        if ( Mode == Connection_Mode.Prologix_GPIB )
        {
          // GPIB: Send ++read eoi
          bool Read_Sent = false;
          int Retry_Count = 0;
          const int Max_Retries = 3;

          while ( !Read_Sent && Retry_Count < Max_Retries )
          {
            Token.ThrowIfCancellationRequested ( );

            try
            {
              _Port.WriteLine ( "++read eoi" );
              Read_Sent = true;
            }
            catch ( TimeoutException )
            {
              Retry_Count++;
              if ( Retry_Count < Max_Retries )
                Thread.Sleep ( 100 );
            }
          }

          if ( !Read_Sent && _Port.BytesToRead == 0 )
          {
            Raise_Error ( "Failed to send ++read eoi after retries." );
            return "";
          }

          // Wait for response
          int Buffer_Wait = 0;
          while ( _Port.BytesToRead == 0 )
          {
            Token.ThrowIfCancellationRequested ( );
            Thread.Sleep ( 10 );
            Buffer_Wait += 10;
            if ( Buffer_Wait >= Read_Timeout_Ms )
            {
              Raise_Error ( "Timeout waiting for GPIB buffer." );
              return "";
            }
          }

          // Read whatever is in the buffer
          return Read_Response ( );
        }
        else
        {
          // RS-232 branch
          int Elapsed = 0;
          while ( _Port.BytesToRead == 0 )
          {
            Token.ThrowIfCancellationRequested ( );
            Thread.Sleep ( 20 );
            Elapsed += 20;
            if ( Elapsed >= Read_Timeout_Ms )
            {
              Raise_Error ( "Timeout waiting for RS-232 response." );
              return "";
            }
          }

          Thread.Sleep ( 100 ); // allow remaining bytes
          try
          {
            return Read_Response ( );
          }
          catch ( TimeoutException Tex )
          {
            Console.WriteLine ( $"Serial timeout: {Tex.Message}" );
            return null;
          }
          catch ( Exception Ex )
          {
            Console.WriteLine ( $"Serial read error: {Ex.Message}" );
            return null;
          }
        }
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




    public string Read_Response ( int Timeout_Ms = 2000 )
    {
      if ( _Port == null || !_Port.IsOpen )
        throw new InvalidOperationException ( "Port not open." );

      int Elapsed = 0;
      int Poll_Interval = 20;

      // wait for at least 1 byte
      while ( _Port.BytesToRead == 0 )
      {
        Thread.Sleep ( Poll_Interval );
        Elapsed += Poll_Interval;
        if ( Elapsed >= Timeout_Ms )
        {
          throw new TimeoutException ( "Timeout waiting for serial data." );
        }
      }

      // wait for the complete response by reading until no new data arrives
      StringBuilder Response_Builder = new StringBuilder ( );
      while ( true )
      {
        string Chunk = _Port.ReadExisting ( );
        if ( Chunk.Length > 0 )
        {
          Response_Builder.Append ( Chunk );

          // check if we received a line terminator indicating end of response
          if ( Chunk.Contains ( '\n' ) || Chunk.Contains ( '\r' ) )
            break;
        }

        // wait briefly for more data to arrive
        Thread.Sleep ( 50 );

        // if no more data and we already have some, we're done
        if ( _Port.BytesToRead == 0 && Response_Builder.Length > 0 )
          break;

        Elapsed += 50;
        if ( Elapsed >= Timeout_Ms )
          break;
      }

      string Response = Response_Builder.ToString ( ).Trim ( );

      Data_Received?.Invoke ( this, Response );
      return Response;
    }





    public string Query_Prologix_Version ( )
    {
      if ( Mode == Connection_Mode.Direct_Serial )
      {
        return "[Direct Serial - no Prologix adapter]";
      }

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

      Result.AppendLine ( "" );
      Result.AppendLine ( "" );
      Result.AppendLine ( $"  Mode      -> {Mode}" );
      Result.AppendLine ( $"  Port      -> {_Port.PortName}" );
      Result.AppendLine ( $"  Baud Rate -> {_Port.BaudRate}" );
      Result.AppendLine ( $"  DTR       -> {_Port.DtrEnable}" );
      Result.AppendLine ( $"  RTS       -> {_Port.RtsEnable}" );
      Result.AppendLine ( $"  Handshake -> {_Port.Handshake}" );
      Result.AppendLine ( $"  CTS       -> {_Port.CtsHolding}" );
      Result.AppendLine ( $"  DSR       -> {_Port.DsrHolding}" );
      Result.AppendLine ( $"  CD        -> {_Port.CDHolding}" );
      Result.AppendLine ( "" );

      // Try three terminator styles
      string [ ] Terminators = { "\r\n", "\n", "\r", "" };
      string [ ] Terminator_Names = { "CR+LF", "LF", "CR", "None" };

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

      Result.AppendLine ( "" );

      return Result.ToString ( ).TrimEnd ( );
    }

    public List<Scan_Result> Scan_GPIB_Bus (
      IProgress<string>? Progress = null,
      CancellationToken Token = default )
    {
      var Results = new List<Scan_Result> ( );

      if ( Mode == Connection_Mode.Direct_Serial )
      {
        Raise_Error (
          "Bus scanning is not available in Direct Serial mode." );
        return Results;
      }

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

        if ( Mode == Connection_Mode.Prologix_GPIB )
        {
          _Port.WriteLine ( "++read eoi" );
        }

        return Read_Response ( Prologix_Read_Timeout_Ms );
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
