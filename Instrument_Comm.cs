using System.CodeDom;
using System.Diagnostics;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using Trace_Execution_Namespace;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  public enum Connection_Mode
  {
    Direct_Serial,
    Prologix_GPIB,
    Prologix_Ethernet
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

    private readonly Application_Settings _Settings;

    public Instrument_Comm ( Application_Settings Settings )
    {
      _Settings = Settings;
    }




    // =========================================================
    // PRIVATE STATE
    // =========================================================
    private SerialPort? _Port;
    private TcpClient? _Tcp_Client;
    private NetworkStream? _Tcp_Stream;
    private bool _Disposed;
    private bool _Is_First_Connect = true;

    // Add this property to Instrument_Comm:
    public Meter_Type Connected_Meter { get; set; } = Meter_Type.HP34401A;

    // =========================================================
    // CONNECTION PROPERTIES
    // =========================================================
    public Connection_Mode Mode { get; set; } = Connection_Mode.Prologix_GPIB;
    public string Port_Name { get; set; } = "";
    public int Baud_Rate { get; set; } = 115200;
    public int Data_Bits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits Stop_Bits { get; set; } = StopBits.One;
    public Handshake Flow_Control { get; set; } = Handshake.None;

    public int Instrument_Settle_Ms { get; set; } = 200;
    public int Prologix_Fetch_Ms { get; set; } = 1000;

    // Ethernet-specific
    public string Ethernet_Host { get; set; } = "192.168.1.100";
    public int Ethernet_Port { get; set; } = 1234;  // Prologix default

    // GPIB settings
    public int GPIB_Address { get; set; } = 22;
    public bool Auto_Read { get; set; } = false;
    public bool EOI_Enabled { get; set; } = false;  // off by default
    public Prologix_Eos_Mode EOS_Mode { get; set; } = Prologix_Eos_Mode.LF;

    // Timeouts
    public int Read_Timeout_Ms { get; set; } = 15000;
    public int Write_Timeout_Ms { get; set; } = 5000;
    public int Prologix_Read_Timeout_Ms { get; set; } = 3000;

    public bool Is_Connected =>
        Mode == Connection_Mode.Prologix_Ethernet
            ? _Tcp_Client?.Connected == true
            : _Port?.IsOpen == true;

    // =========================================================
    // EVENTS
    // =========================================================
    public event EventHandler<string>? Data_Received;
    public event EventHandler<string>? Error_Occurred;
    public event EventHandler<bool>? Connection_Changed;

    // =========================================================
    // STATIC HELPERS
    // =========================================================
    public static string [ ] Get_Available_Ports ( ) => SerialPort.GetPortNames ( );
    public static int [ ] Get_Available_Baud_Rates ( ) =>
        new [ ] { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
    public static int [ ] Get_Available_Data_Bits ( ) => new [ ] { 7, 8 };

    // =========================================================
    // CONNECT / DISCONNECT
    // =========================================================
    public void Connect ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Is_Connected )
        Disconnect ( );

      try
      {
        switch ( Mode )
        {
          case Connection_Mode.Prologix_Ethernet:
            Connect_Ethernet ( );
            break;

          case Connection_Mode.Direct_Serial:
          case Connection_Mode.Prologix_GPIB:
            Connect_Serial ( );
            break;
        }

        if ( Mode == Connection_Mode.Prologix_GPIB ||
            Mode == Connection_Mode.Prologix_Ethernet )
        {
          Configure_Prologix ( );
        }

        Connection_Changed?.Invoke ( this, true );
      }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Connection failed: {Ex.Message}" );
        Cleanup_Connections ( );
      }
    }

    public void Discard_Input_Buffer ( )
    {
      if ( _Port == null || !_Port.IsOpen )
        return;
      _Port.DiscardInBuffer ( );
    }


    public void Flush_Buffers ( )
    {
      if ( Mode == Connection_Mode.Prologix_Ethernet )
      {
        // No buffer to flush on TCP - just drain any pending data
        if ( _Tcp_Stream?.DataAvailable == true )
        {
          byte [ ] Drain = new byte [ 1024 ];
          while ( _Tcp_Stream.DataAvailable )
            _Tcp_Stream.Read ( Drain, 0, Drain.Length );
        }
        return;
      }
      _Port?.DiscardInBuffer ( );
      _Port?.DiscardOutBuffer ( );
    }



    private void Connect_Serial ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( string.IsNullOrEmpty ( Port_Name ) )
      {
        Raise_Error ( "No COM port selected." );
        return;
      }

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
    }

    private void Connect_Ethernet ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( $"Connecting to Prologix at {Ethernet_Host}:{Ethernet_Port}" );

      _Tcp_Client = new TcpClient ( );
      _Tcp_Client.Connect ( Ethernet_Host, Ethernet_Port );
      _Tcp_Client.ReceiveTimeout = Read_Timeout_Ms;
      _Tcp_Client.SendTimeout = Write_Timeout_Ms;
      _Tcp_Stream = _Tcp_Client.GetStream ( );
      Thread.Sleep ( 200 );
    }

    public void Disconnect ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      try
      {
        if ( _Port?.IsOpen == true )
        {
          try
          {
            _Port.DiscardInBuffer ( );
          }
          catch { }
          try
          {
            _Port.DiscardOutBuffer ( );
          }
          catch { }
          try
          {
            _Port.DtrEnable = false;
          }
          catch { }
          try
          {
            _Port.RtsEnable = false;
          }
          catch { }
          try
          {
            _Port.BaseStream?.Close ( );
          }
          catch { }
          _Port.Close ( );
        }

        _Tcp_Stream?.Close ( );
        _Tcp_Client?.Close ( );
      }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Disconnect warning: {Ex.Message}" );
      }
      finally
      {
        Cleanup_Connections ( );
        Connection_Changed?.Invoke ( this, false );
      }
    }

    private void Cleanup_Connections ( )
    {
      try
      {
        _Port?.Dispose ( );
      }
      catch { }
      try
      {
        _Tcp_Stream?.Dispose ( );
      }
      catch { }
      try
      {
        _Tcp_Client?.Dispose ( );
      }
      catch { }
      _Port = null;
      _Tcp_Stream = null;
      _Tcp_Client = null;
    }

    // =========================================================
    // PROLOGIX CONFIGURATION
    // =========================================================
    private void Configure_Prologix ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Always send these - safe to repeat, critical for correct operation
      Raw_Write ( "++mode 1" );
      Thread.Sleep ( 50 );
      Raw_Write ( $"++auto {( _Settings.Prologix_Auto_Read ? 1 : 0 )}" );
      Thread.Sleep ( 50 );
      Raw_Write ( $"++addr {GPIB_Address}" );
      Thread.Sleep ( 50 );
      Raw_Write ( $"++read_tmo_ms {_Settings.Prologix_Read_Tmo_Ms}" );
      Thread.Sleep ( 50 );
      Raw_Write ( $"++eos {(int) EOS_Mode}" );
      Thread.Sleep ( 50 );


      
     


      Capture_Trace.Write ( "Prologix basic config sent" );

      // Only do bus reset on first connect, not on address changes
      if ( _Is_First_Connect )
      {
        Raw_Write ( "++ifc" );
        Thread.Sleep ( 100 );
        Raw_Write ( "++savecfg 0" );
        Thread.Sleep ( 50 );
        _Is_First_Connect = false;

        Capture_Trace.Write ( "Prologix first-connect reset done" );
      }
    }



    // =========================================================
    // LOW-LEVEL WRITE  (works for both serial and ethernet)
    // =========================================================
    private void Raw_Write ( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( $"{Command}" );

      byte [ ] Bytes = Encoding.ASCII.GetBytes ( Command + "\r\n" );

      if ( Mode == Connection_Mode.Prologix_Ethernet )
      {
        if ( _Tcp_Stream == null )
          throw new InvalidOperationException ( "Not connected." );
        _Tcp_Stream.Write ( Bytes, 0, Bytes.Length );
        _Tcp_Stream.Flush ( );
      }
      else
      {
        if ( _Port == null || !_Port.IsOpen )
          throw new InvalidOperationException ( "Not connected." );
        _Port.Write ( Bytes, 0, Bytes.Length );
      }
    }

    // =========================================================
    // PUBLIC SEND METHODS
    // =========================================================
    public void Send_Prologix_Command ( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( Mode == Connection_Mode.Direct_Serial )
        return;
      Raw_Write_Prologix ( Command );
    }

    public void Raw_Write_Prologix ( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      byte [ ] Bytes = Encoding.ASCII.GetBytes ( Command + "\r\n" );
      Write_Bytes ( Bytes );
    }

    public void Send_Instrument_Command ( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Raw_Write_Instrument ( Command.Trim ( ), Connected_Meter );
    }


   

    private void Raw_Write_Instrument ( string Command, Meter_Type Meter )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // 3458A wants LF only, others want CR+LF
      string Terminator = Meter == Meter_Type.HP3458A ? "\n" : "\r\n";

      Capture_Trace.Write ( $"Command        : [{Command}]" );
      Capture_Trace.Write ( $"terminator hex : [{BitConverter.ToString ( Encoding.ASCII.GetBytes ( Terminator ) )}]" );
      Capture_Trace.Write ( $"Meter          : [{Meter}]" );


      byte [ ] Bytes = Encoding.ASCII.GetBytes ( Command + Terminator );
      Write_Bytes ( Bytes );
    }

    private void Write_Bytes ( byte [ ] Bytes )
    {
      if ( Mode == Connection_Mode.Prologix_Ethernet )
      {
        if ( _Tcp_Stream == null )
          throw new InvalidOperationException ( "Not connected." );
        _Tcp_Stream.Write ( Bytes, 0, Bytes.Length );
        _Tcp_Stream.Flush ( );
      }
      else
      {
        if ( _Port == null || !_Port.IsOpen )
          throw new InvalidOperationException ( "Not connected." );
        _Port.Write ( Bytes, 0, Bytes.Length );
      }
    }











    // =========================================================
    // QUERY  overloads
    // =========================================================
   
    public string Query_Instrument ( string Command ) =>
    Query_Instrument ( Command, Instrument_Settle_Ms, CancellationToken.None );

    
    public string Query_Instrument ( string Command, CancellationToken Token ) =>
        Query_Instrument ( Command, Instrument_Settle_Ms, Token );


    public string Query_Instrument ( string Command, int Settle_Ms, CancellationToken Token )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( $"Query Command        : [{Command}]" );
      Capture_Trace.Write ( $"Instrument Settle Ms : [{Instrument_Settle_Ms}]" );
      Capture_Trace.Write ( $"Prologic Fetch MS    : [{Prologix_Fetch_Ms}]" );


      Send_Instrument_Command ( Command );
      Thread.Sleep ( Settle_Ms );

      Raw_Write_Prologix ( "++read eoi" );
      Thread.Sleep ( Prologix_Fetch_Ms );

      return Read_Instrument ( Token ) ?? "";


      
    }

    public string old_Query_Instrument ( string Command, CancellationToken Token )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( $"Command : [{Command}]" );
      Capture_Trace.Write ( $"hex     : [{BitConverter.ToString ( Encoding.ASCII.GetBytes ( Command ) )}]" );

      try
      {
        Send_Instrument_Command ( Command );
        Thread.Sleep ( Instrument_Settle_Ms );
        Raw_Write_Prologix ( "++read eoi" );
        Thread.Sleep ( Prologix_Fetch_Ms );
        return Read_Instrument ( Token ) ?? "";
      }
      catch ( OperationCanceledException ) { throw; }
      catch ( TimeoutException )
      {
        Raise_Error ( $"Timeout waiting for response to: {Command}" );
        return "";
      }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Query failed: {Ex.Message}" );
        return "";
      }
    }

    // =========================================================
    // READ
    // =========================================================
    public string? Read_Instrument ( CancellationToken Token = default )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      try
      {
        return Mode == Connection_Mode.Prologix_Ethernet
            ? Read_Ethernet ( Token )
            : Read_Serial ( Token );
      }
      catch ( OperationCanceledException ) { throw; }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Read failed: {Ex.Message}" );
        return "";
      }
    }

    private string Read_Serial ( CancellationToken Token )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Port == null || !_Port.IsOpen )
      {
        Raise_Error ( "Not connected." );
        return "";
      }

      Capture_Trace.Write ( $"BytesToRead on entry: {_Port.BytesToRead}" );

      int Elapsed = 0;
      while ( _Port.BytesToRead == 0 )
      {
        Token.ThrowIfCancellationRequested ( );
        Thread.Sleep ( 10 );
        Elapsed += 10;
        if ( Elapsed >= Read_Timeout_Ms )
        {
          Raise_Error ( $"Timeout waiting for response after {Elapsed}ms." );
          return "";
        }
      }

      Capture_Trace.Write ( $"Got {_Port.BytesToRead} bytes after {Elapsed}ms" );
      return Read_Response_Serial ( );
    }

    private string Read_Ethernet ( CancellationToken Token )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Tcp_Stream == null )
      {
        Raise_Error ( "Not connected." );
        return "";
      }

      var Buffer = new StringBuilder ( );
      byte [ ] Byte_Buf = new byte [ 1024 ];
      int Elapsed = 0;

      while ( true )
      {
        Token.ThrowIfCancellationRequested ( );

        if ( _Tcp_Stream.DataAvailable )
        {
          int Count = _Tcp_Stream.Read ( Byte_Buf, 0, Byte_Buf.Length );
          string Chunk = Encoding.ASCII.GetString ( Byte_Buf, 0, Count );
          Buffer.Append ( Chunk );

          if ( Chunk.Contains ( '\n' ) || Chunk.Contains ( '\r' ) )
            break;
        }
        else
        {
          if ( Buffer.Length > 0 )
            break;  // got data, no more arriving

          Thread.Sleep ( 10 );
          Elapsed += 10;
          if ( Elapsed >= Read_Timeout_Ms )
          {
            Raise_Error ( "Timeout waiting for Ethernet response." );
            return "";
          }
        }
      }

      string Response = Buffer.ToString ( ).Trim ( );
      Data_Received?.Invoke ( this, Response );
      return Response;
    }

    private string Read_Response_Serial ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Port == null || !_Port.IsOpen )
        throw new InvalidOperationException ( "Port not open." );

      var Buffer = new StringBuilder ( );
      int Elapsed = 0;

      while ( Elapsed < Read_Timeout_Ms )
      {
        if ( _Port.BytesToRead > 0 )
        {
          Buffer.Append ( _Port.ReadExisting ( ) );
          // Check for terminator
          if ( Buffer.ToString ( ).Contains ( '\n' ) )
            break;
          // Reset elapsed when data arrives - keep waiting for more
          Elapsed = 0;
        }
        else
        {
          Thread.Sleep ( 10 );
          Elapsed += 10;
          // If we have data and nothing arrived for 50ms, response is complete
          if ( Buffer.Length > 0 && Elapsed >= 50 )
            break;
        }
      }

      string Response = Buffer.ToString ( ).Trim ( );
      //  if ( Response.Length <= 2 )
      //    Capture_Trace.Write ( $"Read_Response_Serial - Short: [{Response}] " +
      //        $"hex:[{BitConverter.ToString ( Encoding.ASCII.GetBytes ( Response ) )}]" );
      Data_Received?.Invoke ( this, Response );
      return Response;
    }



    // =========================================================
    // UTILITY
    // =========================================================
    public void Change_GPIB_Address ( int New_Address )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Mode == Connection_Mode.Direct_Serial )
        return;

      if ( New_Address < 0 || New_Address > 30 )
      {
        Raise_Error ( "GPIB address must be 0-30." );
        return;
      }

      GPIB_Address = New_Address;
      if ( Is_Connected )
      {
        Send_Prologix_Command ( $"++addr {GPIB_Address}" );
        Thread.Sleep ( 50 );
      }
    }

    public string Query_Prologix_Version ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Mode == Connection_Mode.Direct_Serial )
        return "[Direct Serial - no Prologix adapter]";

      if ( !Is_Connected )
        return "";

      try
      {
        Raw_Write ( "++ver" );
        Thread.Sleep ( 100 );
        return Mode == Connection_Mode.Prologix_Ethernet
            ? Read_Ethernet ( CancellationToken.None )
            : Read_Response_Serial ( );
      }
      catch { return ""; }
    }

    public bool Is_Data_Available ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !Is_Connected )
        return false;

      try
      {
        Raw_Write ( "++spoll" );
        Thread.Sleep ( 50 );

        string Response = Mode == Connection_Mode.Prologix_Ethernet
            ? Read_Ethernet ( CancellationToken.None )
            : Read_Response_Serial ( );

        if ( int.TryParse ( Response, out int Status_Byte ) )
          return ( Status_Byte & 0x10 ) != 0;  // MAV bit

        return false;
      }
      catch ( TimeoutException ) { return true; }
      catch ( Exception Ex )
      {
        Raise_Error ( $"Status poll failed: {Ex.Message}" );
        return false;
      }
    }

    public List<Scan_Result> Scan_GPIB_Bus (
        IProgress<string>? Progress = null,
        CancellationToken Token = default )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      var Results = new List<Scan_Result> ( );

      if ( Mode == Connection_Mode.Direct_Serial )
      {
        Raise_Error ( "Bus scanning is not available in Direct Serial mode." );
        return Results;
      }

      if ( !Is_Connected )
      {
        Raise_Error ( "Not connected." );
        return Results;
      }

      int Original_Address = GPIB_Address;

      try
      {
        Raw_Write ( "++auto 0" );
        Thread.Sleep ( 50 );

        for ( int Addr = 0; Addr <= 30; Addr++ )
        {
          Token.ThrowIfCancellationRequested ( );
          Progress?.Report ( $"Scanning GPIB address {Addr}..." );

          Raw_Write ( $"++addr {Addr}" );
          Thread.Sleep ( 40 );

          string Response = Try_Query ( "*IDN?" );
          if ( string.IsNullOrEmpty ( Response ) )
            Response = Try_Query ( "ID?" );

          if ( !string.IsNullOrEmpty ( Response ) )
          {
            Meter_Type? Detected = null;
            string Upper = Response.ToUpperInvariant ( );

            if ( Upper.Contains ( "3458" ) )
              Detected = Meter_Type.HP3458A;
            else if ( Upper.Contains ( "34401" ) )
              Detected = Meter_Type.HP34401A;
            else if ( Upper.Contains ( "33120" ) )
              Detected = Meter_Type.HP33120A;

            Results.Add ( new Scan_Result
            {
              Address = Addr,
              ID_String = Response,
              Detected_Type = Detected
            } );

            Progress?.Report ( $"  Found at {Addr}: {Response}" );
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
        Raw_Write ( $"++addr {Original_Address}" );
        Thread.Sleep ( 50 );
        Raw_Write ( $"++auto {( Auto_Read ? 1 : 0 )}" );
        Thread.Sleep ( 50 );
        GPIB_Address = Original_Address;
      }

      Progress?.Report ( $"Scan complete. Found {Results.Count} instrument(s)." );
      return Results;
    }

    public string Raw_Diagnostic ( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !Is_Connected )
        return "[Not connected]";

      var Result = new StringBuilder ( );
      Result.AppendLine ( $"  Mode -> {Mode}" );

      if ( Mode == Connection_Mode.Prologix_Ethernet )
      {
        Result.AppendLine ( $"  Host -> {Ethernet_Host}:{Ethernet_Port}" );
      }
      else if ( _Port != null )
      {
        Result.AppendLine ( $"  Port      -> {_Port.PortName}" );
        Result.AppendLine ( $"  Baud Rate -> {_Port.BaudRate}" );
        Result.AppendLine ( $"  CTS       -> {_Port.CtsHolding}" );
        Result.AppendLine ( $"  DSR       -> {_Port.DsrHolding}" );
        Result.AppendLine ( $"  CD        -> {_Port.CDHolding}" );
      }

      string [ ] Terminators = { "\r\n", "\n", "\r", "" };
      string [ ] Terminator_Names = { "CR+LF", "LF", "CR", "None" };

      for ( int I = 0; I < Terminators.Length; I++ )
      {
        try
        {
          if ( _Port != null )
          {
            _Port.DiscardInBuffer ( );
            _Port.DiscardOutBuffer ( );
          }
          Thread.Sleep ( 50 );

          byte [ ] Out_Bytes = Encoding.ASCII.GetBytes ( Command + Terminators [ I ] );

          if ( Mode == Connection_Mode.Prologix_Ethernet )
          {
            _Tcp_Stream?.Write ( Out_Bytes, 0, Out_Bytes.Length );
            _Tcp_Stream?.Flush ( );
          }
          else
          {
            _Port?.BaseStream.Write ( Out_Bytes, 0, Out_Bytes.Length );
            _Port?.BaseStream.Flush ( );
          }

          int Elapsed = 0;
          bool Has_Data = false;

          while ( Elapsed < 2000 )
          {
            Thread.Sleep ( 50 );
            Elapsed += 50;

            Has_Data = Mode == Connection_Mode.Prologix_Ethernet
                ? _Tcp_Stream?.DataAvailable == true
                : _Port?.BytesToRead > 0;

            if ( Has_Data )
              break;
          }

          if ( !Has_Data )
          {
            Result.AppendLine ( $"[{Terminator_Names [ I ]}] No response" );
            continue;
          }

          Thread.Sleep ( 200 );

          string Text = Mode == Connection_Mode.Prologix_Ethernet
              ? Read_Ethernet ( CancellationToken.None )
              : Read_Response_Serial ( );

          Result.AppendLine ( $"[{Terminator_Names [ I ]}] \"{Text.Replace ( "\r", "\\r" ).Replace ( "\n", "\\n" )}\"" );
          break;
        }
        catch ( Exception Ex )
        {
          Result.AppendLine ( $"[{Terminator_Names [ I ]}] Error: {Ex.Message}" );
        }
      }

      return Result.ToString ( ).TrimEnd ( );
    }

    private string Try_Query ( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      try
      {
        Raw_Write ( Command );
        Thread.Sleep ( 50 );
        Raw_Write ( "++read eoi" );
        return Mode == Connection_Mode.Prologix_Ethernet
            ? Read_Ethernet ( CancellationToken.None )
            : Read_Response_Serial ( );
      }
      catch ( TimeoutException ) { return ""; }
      catch { return ""; }
    }

    private void Raise_Error ( string Message )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Error_Occurred?.Invoke ( this, Message );
    }

    public void Dispose ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( !_Disposed )
      {
        Disconnect ( );
        _Disposed = true;
      }
      GC.SuppressFinalize ( this );
    }


    public string Verify_GPIB_Address ( int Address, bool Try_Legacy_ID = false )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( !Is_Connected )
        return "";
      if ( Mode == Connection_Mode.Direct_Serial )
      {
        string Serial_Response = Try_Legacy_ID
            ? Try_Query ( "ID?" )
            : Try_Query ( "*IDN?" );
        return Serial_Response;
      }
      int Original_Address = GPIB_Address;
      try
      {
        // Switch to address
        Raw_Write ( $"++addr {Address}" );
        GPIB_Address = Address;
        Thread.Sleep ( 50 );
        // Stop auto-read
        Raw_Write ( "++auto 0" );
        Thread.Sleep ( 50 );
        // For 3458A - stop trigger and flush buffer without clearing config
        if ( Try_Legacy_ID )
        {
          Raw_Write ( "TRIG HOLD" );
          Thread.Sleep ( 100 );
          // Flush any pending readings by reading until timeout
          try
          {
            Raw_Write ( "++read eoi" );
            Thread.Sleep ( 50 );
            if ( Mode == Connection_Mode.Prologix_Ethernet )
              Read_Ethernet ( CancellationToken.None );
            else
              Read_Response_Serial ( );
          }
          catch { }
        }
        string Response = Try_Legacy_ID
            ? Try_Query ( "ID?" )
            : Try_Query ( "*IDN?" );
        return Response;
      }
      catch ( Exception )
      {
        return "";
      }
      finally
      {
        if ( GPIB_Address != Original_Address )
        {
          Raw_Write ( $"++addr {Original_Address}" );
          Thread.Sleep ( 50 );
        }
        GPIB_Address = Original_Address;
      }
    }






    private static bool Is_Measurement ( string Response )
    {
      // Measurements are numeric — ID strings contain letters/commas
      return double.TryParse ( Response.Trim ( ),
          System.Globalization.NumberStyles.Float,
          System.Globalization.CultureInfo.InvariantCulture,
          out _ );
    }

  }
}
