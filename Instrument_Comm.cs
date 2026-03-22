using System.CodeDom;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
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
      Register_Exit_Hooks ( );
    }


    // =========================================================
    // APP-LEVEL EXIT HOOKS  (catch VS kill / window X / crash)
    // =========================================================
    private static Instrument_Comm? _Exit_Hook_Instance;
    private static bool _Exit_Hooks_Registered;

    private void Register_Exit_Hooks ( )
    {
      if ( _Exit_Hooks_Registered )
        return;
      _Exit_Hooks_Registered = true;
      _Exit_Hook_Instance = this;

      // Catches: VS "Stop Debugging", Environment.Exit(), end of Main
      AppDomain.CurrentDomain.ProcessExit += ( s, e ) =>
          _Exit_Hook_Instance?.Emergency_Shutdown ( );

      // Catches: unhandled exceptions that would kill the process
      AppDomain.CurrentDomain.UnhandledException += ( s, e ) =>
          _Exit_Hook_Instance?.Emergency_Shutdown ( );

      // Catches: WinForms message loop exceptions
      Application.ThreadException += ( s, e ) =>
          _Exit_Hook_Instance?.Emergency_Shutdown ( );
    }

    private bool _Emergency_Shutdown_Done;

    public void Emergency_Shutdown ( )
    {
      if ( _Emergency_Shutdown_Done )
        return;
      _Emergency_Shutdown_Done = true;

      // Can't use Trace_Block here — runtime may be tearing down
      try
      {
        Abort_Pending_Operations ( );
      }
      catch { }
      try
      {
        if ( _Port?.IsOpen == true )
        {
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
            _Port.Close ( );
          }
          catch { }
        }
        _Tcp_Stream?.Close ( );
        _Tcp_Client?.Close ( );
      }
      catch { }
      finally
      {
        try
        {
          Cleanup_Connections ( );
        }
        catch { }
      
      }
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
    public Meter_Type Connected_Meter { get; set; } = Meter_Type.HP34401;

    // =========================================================
    // CONNECTION PROPERTIES
    // =========================================================
    public Connection_Mode Mode { get; set; } = Connection_Mode.Prologix_GPIB;
    public string Port_Name { get; set; } = "";
    public int Baud_Rate { get; set; } = 9600;
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
    public int Read_Timeout_Ms { get; set; } = 3000;
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
    // VERIFIED ADDRESS CACHE
    // =========================================================
    private readonly HashSet<int> _Verified_Addresses = new ( );

    private readonly Dictionary<int, Scan_Result> _Verified_Cache = new ( );

    public bool Is_Address_Verified ( int address )
    {
      return _Verified_Addresses.Contains ( address );
    }

    public void Mark_Address_Verified ( int address )
    {
      _Verified_Addresses.Add ( address );
    }

    public void Clear_Verified_Cache ( ) =>
        _Verified_Addresses.Clear ( );





    // =========================================================
    // STATIC HELPERS
    // =========================================================
    public static string [ ] Get_Available_Ports ( ) => SerialPort.GetPortNames ( );
    public static int [ ] Get_Available_Baud_Rates ( ) =>
        new [ ] { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
    public static int [ ] Get_Available_Data_Bits ( ) => new [ ] { 7, 8 };


    public static int [ ] Get_Available_Read_Timeouts ( ) => new [ ] { 1000, 2000, 3000 };




    // =========================================================
    // CONNECT / DISCONNECT
    // =========================================================
    public void Connect ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( Is_Connected )
        Disconnect_Async ( );

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
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Port == null || !_Port.IsOpen )
        return;
      _Port.DiscardInBuffer ( );
    }

    public void Discard_Output_Buffer ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Port == null || !_Port.IsOpen )
        return;
      _Port.DiscardOutBuffer ( );
    }

    public void Discard_IO_Buffers ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Port == null || !_Port.IsOpen )
        return;

      Discard_Input_Buffer ( );
      Discard_Output_Buffer ( );
    }

    public void Flush_Buffers ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

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




    public async Task Disconnect_Async ( )
    {

      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( _Disposed )
        return;   // guard against double-call from exit hook

      await Task.Run ( ( ) =>
      {
        Abort_Pending_Operations ( );

        try
        {
          if ( _Port?.IsOpen == true )
          {
            try
            {
              Capture_Trace.Write ( "Discarding Input Buffer" );
              _Port.DiscardInBuffer ( );
            }
            catch { }
            try
            {
              Capture_Trace.Write ( "Discarding Output Buffer." );
              _Port.DiscardOutBuffer ( );
            }
            catch { }
            try
            {
              Capture_Trace.Write ( "Turning off DTR Enable." );
              _Port.DtrEnable = false;
            }
            catch { }
            try
            {
              Capture_Trace.Write ( "Turning off RTS Enable." );
              _Port.RtsEnable = false;
            }
            catch { }
            try
            {
              Capture_Trace.Write ( "Setting Port read/write to 1." );
              _Port.ReadTimeout = 1;
              _Port.WriteTimeout = 1;
            }
            catch { }

            Capture_Trace.Write ( "Closing Port." );
            _Port.Close ( );
          }
          try
          {
            Capture_Trace.Write ( "Closing TCP Stream." );
            _Tcp_Stream?.Close ( );
          }
          catch { }
          try
          {
            Capture_Trace.Write ( "Closing TCP Client." );
            _Tcp_Client?.Close ( );
          }
          catch { }
        }
        catch ( Exception ex ) { Raise_Error ( $"Disconnect warning: {ex.Message}" ); }
        finally
        {
          _Disposed = true;   // prevent Emergency_Shutdown from re-entering
          Cleanup_Connections ( );
          _Verified_Addresses.Clear ( );
          _Verified_Cache.Clear ( );
         
        }
      } );

      Connection_Changed?.Invoke ( this, false );  // back on UI thread
    }






    private void Cleanup_Connections ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      try
      {
        Capture_Trace.Write ( "Disposing Port." );
        _Port?.Dispose ( );
      }
      catch { }
      try
      {
        Capture_Trace.Write ( "Disposing TCP Stream." );
        _Tcp_Stream?.Dispose ( );
      }
      catch { }
      try
      {
        Capture_Trace.Write ( "Disposing TCP Client." );
        _Tcp_Client?.Dispose ( );
      }
      catch { }

      Capture_Trace.Write ( "Setting Port, TCP Stream, and TCP Client to null." );
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
      //  Raw_Write ( "++ifc" );
      //  Thread.Sleep ( 100 );
        Raw_Write ( "++savecfg 0" );
        Thread.Sleep ( 50 );
        _Is_First_Connect = false;

        Capture_Trace.Write ( "Prologix first-connect reset done" );
      }
    }

    public void Configure_Prologix_Transport_Only ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // Safe initial settings
      Raw_Write ( "++mode 1" ); // controller
      Thread.Sleep ( 50 );
      Raw_Write ( $"++auto 0" ); // don't auto read yet
      Thread.Sleep ( 50 );
      Raw_Write ( $"++read_tmo_ms {_Settings.Prologix_Read_Tmo_Ms}" );
      Thread.Sleep ( 50 );
      Raw_Write ( $"++eos {(int) EOS_Mode}" );
      Thread.Sleep ( 50 );

      Capture_Trace.Write ( "Prologix transport-only config sent" );

      // First connect reset if needed
      if ( _Is_First_Connect )
      {
        //  Raw_Write ( "++ifc" );
        //  Thread.Sleep ( 100 );
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

      try
      {
        if ( Mode == Connection_Mode.Prologix_Ethernet )
        {
          if ( _Tcp_Stream == null )
          {
            Capture_Trace.Write ( "Raw_Write - ERROR: Not connected (TCP stream is null)" );
            return;
          }
          _Tcp_Stream.Write ( Bytes, 0, Bytes.Length );
          _Tcp_Stream.Flush ( );
        }
        else
        {
          if ( _Port == null || !_Port.IsOpen )
          {
            Capture_Trace.Write ( "Raw_Write - ERROR: Not connected (port is null or closed)" );
            return;
          }
          _Port.Write ( Bytes, 0, Bytes.Length );
        }
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Raw_Write - EXCEPTION: {Ex.Message}" );
      }
    }


    // --- Inside your Comm class ---

    // Synchronous version
    public void Send_Instrument_Command ( int address, string command )
    {
      Raw_Write_Instrument ( command, address );
    }

    // Async wrapper
    public Task Send_Instrument_CommandAsync ( int address, string command )
    {
      return Task.Run ( ( ) => Send_Instrument_Command ( address, command ) );
    }

    // Raw write sync
    public void Raw_Write_Instrument ( string command, int address )
    {
      if ( Mode == Connection_Mode.Prologix_GPIB || Mode == Connection_Mode.Prologix_Ethernet )
      {
        Send_Prologix_Command ( $"++addr {address}" );
        Thread.Sleep ( 10 );
      }
      Raw_Write ( command );
    }

    // Raw write async wrapper
    public Task Raw_Write_InstrumentAsync ( string command, int address )
    {
      return Task.Run ( ( ) => Raw_Write_Instrument ( command, address ) );
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
      Capture_Trace.Write ( $"Prologix Command -> {Command}" );

      byte [ ] Bytes = Encoding.ASCII.GetBytes ( Command + "\r\n" );
      Write_Bytes ( Bytes );
    }

    public void Send_Instrument_Command ( string Command )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Raw_Write_Instrument ( Command.Trim ( ), Connected_Meter );
    }



    public Task Send_Prologix_CommandAsync ( string command )
    {
      // Run the synchronous Prologix command off the UI thread
      return Task.Run ( ( ) =>
      {
        using var Block = Trace_Block.Start_If_Enabled ( );
        Send_Prologix_Command ( command ); // existing sync method
      } );
    }





    private void Raw_Write_Instrument ( string Command, Meter_Type Meter )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      // 3458 wants LF only, others want CR+LF
      string Terminator = Meter == Meter_Type.HP3458 ? "\n" : "\r\n";

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
      Query_Instrument ( Command, Instrument_Settle_Ms, CancellationToken.None, Meter_Type.HP3458 );

    public string Query_Instrument ( string Command, Meter_Type Meter ) =>
        Query_Instrument ( Command, Instrument_Settle_Ms, CancellationToken.None, Meter );

    public string Query_Instrument ( string Command, CancellationToken Token ) =>
        Query_Instrument ( Command, Instrument_Settle_Ms, Token, Meter_Type.HP3458 );

    public string Query_Instrument ( string Command, CancellationToken Token, Meter_Type Meter ) =>
        Query_Instrument ( Command, Instrument_Settle_Ms, Token, Meter );

    public string Query_Instrument ( string Command, int Settle_Ms, CancellationToken Token, Meter_Type Meter )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      Capture_Trace.Write ( $"Query Command        : [{Command}]" );
      Capture_Trace.Write ( $"Instrument Settle Ms : [{Instrument_Settle_Ms}]" );
      Capture_Trace.Write ( $"Prologic Fetch MS    : [{Prologix_Fetch_Ms}]" );

      Send_Instrument_Command ( Command );
      Thread.Sleep ( Settle_Ms );
      Raw_Write_Prologix ( "++read eoi" );

      if ( Meter != Meter_Type.HP3456 )
        Thread.Sleep ( Prologix_Fetch_Ms );

      return Read_Instrument ( Token ) ?? "";
    }

    // =========================================================
    // READ
    // =========================================================
    public string? old_Read_Instrument ( CancellationToken Token = default )
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
      catch ( TimeoutException ) { throw; }
      catch ( InvalidOperationException ) { throw; }
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
      while ( true )
      {
        if ( Token.IsCancellationRequested )
        {
          Capture_Trace.Write ( $"Read cancelled after {Elapsed}ms" );
          return "";
        }
        if ( _Port == null || !_Port.IsOpen )
        {
          Capture_Trace.Write ( "Port closed while waiting for data" );
          return "";
        }
        if ( _Port.BytesToRead > 0 )
          break;
        Thread.Sleep ( 10 );
        Elapsed += 10;
        if ( Elapsed >= Read_Timeout_Ms )
        {
          Capture_Trace.Write ( $"Timeout waiting for response after {Elapsed}ms" );
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
            throw new TimeoutException ( "Timeout waiting for Ethernet response." );
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
        if ( _Port == null || !_Port.IsOpen )
        {
          break;  // port closed, return what we have
        }

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



    private Task Raw_WriteAsync ( string cmd ) =>
     Task.Run ( ( ) => Raw_Write ( cmd ) );

    private Task<string> Try_QueryAsync ( string cmd, CancellationToken token ) =>
        Task.Run ( ( ) => Try_Query ( cmd ), token );

    private Task<string> Try_Query_Short_Async ( string cmd, CancellationToken token, int Timeout_Ms = 3000 ) =>
    Task.Run ( ( ) => Try_Query_Short ( cmd, Timeout_Ms ), token );



    public async Task<List<Scan_Result>> Scan_GPIB_BusAsync (
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
      var Retry_Addresses = new List<int> ( );  // declared here so second pass can see it
      var Legacy_Addresses = new List<int> ( );  // showed readings -- need TRIG HOLD
      var All_Retry = new List<int> ( );  // all non-responding addresses -- get ID? in second pass
      var Needs_Drain = new HashSet<int> ( );  // were spewing readings -- also need TRIG HOLD
      try
      {
        await Raw_WriteAsync ( "++auto 0" );
        await Task.Delay ( 200, Token );

        for ( int Addr = 0; Addr <= 30; Addr++ )
        {
          Token.ThrowIfCancellationRequested ( );
          Capture_Trace.Write ( $"Scanning address {Addr}..." );
          Progress?.Report ( $"Scanning GPIB address {Addr}..." );
          await Raw_WriteAsync ( $"++addr {Addr}" );
          await Task.Delay ( 200, Token );
          await Task.Delay ( 50, Token );

          string Response = await Try_Query_Short_Async ( "*IDN?", Token, Timeout_Ms: 500 );

          if ( Response.Contains ( "Unrecognized command" ) || Response.Contains ( "Prologix" ) )
          {
            Retry_Addresses.Add ( Addr );
            continue;
          }

          bool Looks_Like_Reading = !string.IsNullOrEmpty ( Response ) && double.TryParse (
              Response.Split ( '\n' ) [ 0 ].Trim ( ),
              System.Globalization.NumberStyles.Float,
              System.Globalization.CultureInfo.InvariantCulture,
              out _ );

          if ( Looks_Like_Reading )
          {
            Raw_Write ( "TRIG HOLD" );
            await Task.Delay ( 200, Token );
            Drain_Buffer ( );
            All_Retry.Add ( Addr );
            Needs_Drain.Add ( Addr );
          }
          else if ( string.IsNullOrEmpty ( Response ) ||
                    Response.Contains ( "Unrecognized command" ) ||
                    Response.Contains ( "Prologix" ) )
          {
            All_Retry.Add ( Addr );
          }

          await Task.Yield ( );
        }
        Capture_Trace.Write ( $"First pass complete. Legacy addresses: {string.Join ( ", ", Legacy_Addresses )}" );
      }
      catch ( OperationCanceledException )
      {
        Capture_Trace.Write ( "Scan cancelled." );
        Progress?.Report ( "Scan cancelled." );
      }
      catch ( Exception ex )
      {
        Raise_Error ( $"Scan error: {ex.Message}" );
      }

      // Second pass -- always runs regardless of cancellation
      if ( All_Retry.Count > 0 )
      {
        Progress?.Report ( "Second pass -- checking for legacy instruments..." );
        Capture_Trace.Write ( "Second pass for legacy instruments..." );

        foreach ( int Addr in All_Retry )
        {
          if ( !Is_Connected )
          {
            Capture_Trace.Write ( "Connection lost during second pass -- stopping." );
            break;
          }
          if ( Addr == 0 )
            continue;
          if ( Results.Any ( r => r.Address == Addr ) )
            continue;

          Capture_Trace.Write ( $"Legacy scan at address {Addr}..." );
          Progress?.Report ( $"Legacy scan at address {Addr}..." );

          try
          {
            await Raw_WriteAsync ( $"++addr {Addr}" );
            await Task.Delay ( 300, CancellationToken.None );

            if ( Needs_Drain.Contains ( Addr ) )
            {
              Raw_Write ( "TRIG HOLD" );
              await Task.Delay ( 200, CancellationToken.None );
              Drain_Buffer ( );
              Flush_Device_Buffer ( );
              await Task.Delay ( 50, CancellationToken.None );
            }

            string Response = await Try_Query_Short_Async ( "ID?", CancellationToken.None );
            if ( string.IsNullOrEmpty ( Response ) )
            {
              Flush_Device_Buffer ( );
              await Task.Delay ( 100, CancellationToken.None );
              Response = await Try_Query_Short_Async ( "*IDN?", CancellationToken.None );
            }

            Capture_Trace.Write ( $"Legacy pass address {Addr} raw response: '{Response}'" );

            if ( !string.IsNullOrEmpty ( Response ) )
            {
              Meter_Type? Detected = null;
              string Upper = Response.ToUpperInvariant ( );
              if ( Upper.Contains ( "3458" ) )
                Detected = Meter_Type.HP3458;
              else if ( Upper.Contains ( "34401" ) )
                Detected = Meter_Type.HP34401;
              else if ( Upper.Contains ( "33120" ) )
                Detected = Meter_Type.HP33120;
              var Result = new Scan_Result
              {
                Address = Addr,
                ID_String = Response,
                Detected_Type = Detected
              };
              Results.Add ( Result );
              _Verified_Cache [ Addr ] = Result;
              Capture_Trace.Write ( $"  Found at {Addr}: {Response}" );
              Progress?.Report ( $"  Found legacy at {Addr}: {Response}" );
            }
          }
          catch ( Exception Ex )
          {
            Capture_Trace.Write ( $"Legacy scan error at address {Addr}: {Ex.Message}" );
          }
          await Task.Yield ( );
        }
      }
      // Cleanup
      try
      {
        await Raw_WriteAsync ( $"++addr {Original_Address}" );
        await Task.Delay ( 50, CancellationToken.None );
        await Raw_WriteAsync ( $"++auto {( Auto_Read ? 1 : 0 )}" );
        await Task.Delay ( 50, CancellationToken.None );
        GPIB_Address = Original_Address;
      }
      catch { }

      Capture_Trace.Write ( $"Scan complete. Found {Results.Count} instrument(s)." );
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

    // Short-timeout query used during address verification so a missing
    // instrument doesn't block for the full Read_Timeout_Ms (15 s).
    private string Try_Query_Short ( string Command, int Timeout_Ms = 3000 )
    {
      using var Cts = new CancellationTokenSource ( Timeout_Ms );
      try
      {
        Raw_Write ( Command );
        Thread.Sleep ( 50 );
        Raw_Write ( "++read eoi" );
        return Read_With_Timeout ( Timeout_Ms );
      }
      catch { return ""; }
    }




    // Drain any stale response from the bus with a short timeout so a
    // silent instrument doesn't add seconds to the verification time.
    private void Drain_Buffer ( int Timeout_Ms = 500, int Max_Iterations = 50 )
    {
      using var Cts = new CancellationTokenSource ( Timeout_Ms );
      try
      {
        for ( int i = 0; i < Max_Iterations; i++ )
        {
          using var Iter_Cts = new CancellationTokenSource ( Timeout_Ms );
          Raw_Write ( "++read eoi" );
          Thread.Sleep ( 50 );
          string Response = Mode == Connection_Mode.Prologix_Ethernet
              ? Read_Ethernet ( Iter_Cts.Token )
              : Read_Serial ( Iter_Cts.Token );
          // Empty response means buffer is clear
          if ( string.IsNullOrWhiteSpace ( Response ) )
            break;
        }
      }
      catch { }
    }

    private void Raise_Error ( string Message )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( $"Error: {Message}" );
      Error_Occurred?.Invoke ( this, Message );
    }

    public void Dispose ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );
      if ( !_Disposed )
      {
        Disconnect_Async ( );
        _Disposed = true;
      }
      GC.SuppressFinalize ( this );
    }


    public string old_Verify_GPIB_Address ( int Address, bool Try_Legacy_ID = false, bool Restore_Address = true )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !Is_Connected )
        return "";


      // Check cache first
      if ( _Verified_Cache.TryGetValue ( Address, out var Cached ) )
      {
        Capture_Trace.Write ( $"Cache hit for address {Address}: {Cached.ID_String}" );
        return Cached.ID_String;
      }


      if ( Mode == Connection_Mode.Direct_Serial )
      {
        return Try_Legacy_ID
            ? Try_Query_Short ( "ID?" )
            : Try_Query_Short ( "*IDN?" );
      }

      int Original_Address = GPIB_Address;

      try
      {
        // Switch to target address
        Raw_Write ( $"++addr {Address}" );
        GPIB_Address = Address;
        Thread.Sleep ( 200 );   // was 50 -- give bus time to settle

        // Stop auto-read
        Raw_Write ( "++auto 0" );
        Thread.Sleep ( 200 );   // was 50

        // Flush any stale data on THIS address before querying
        Flush_Device_Buffer ( );

        // For 3458 -- stop trigger and drain buffered readings
        if ( Try_Legacy_ID )
        {
          Raw_Write ( "TRIG HOLD" );
          Thread.Sleep ( 200 );
          Drain_Buffer ( );   // now loops until empty
          Flush_Device_Buffer ( );
        }

        string Response = Try_Legacy_ID
            ? Try_Query_Short ( "ID?" )
            : Try_Query_Short ( "*IDN?" );


        if ( !string.IsNullOrEmpty ( Response ) )
        {
          Meter_Type? Detected = null;
          string Upper = Response.ToUpperInvariant ( );
          if ( Upper.Contains ( "3458" ) )
            Detected = Meter_Type.HP3458;
          else if ( Upper.Contains ( "34401" ) )
            Detected = Meter_Type.HP34401;
          else if ( Upper.Contains ( "33120" ) )
            Detected = Meter_Type.HP33120;
          else if ( Upper.Contains ( "34420" ) )
            Detected = Meter_Type.HP34420;
          else if ( Upper.Contains ( "53132" ) )
            Detected = Meter_Type.HP53132;
          else
            Detected = Meter_Type.Generic_GPIB;

          _Verified_Cache [ Address ] = new Scan_Result
          {
            Address = Address,
            ID_String = Response,
            Detected_Type = Detected
          };
        }



        return Response;
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Verify failed at address {Address}: {Ex.Message}" );
        return "";
      }
      finally
      {
        if ( Restore_Address )
        {
          Raw_Write ( $"++addr {Original_Address}" );
          Thread.Sleep ( 100 );
          GPIB_Address = Original_Address;
        }
      }
    }





    public string Verify_GPIB_Address ( int Address, bool Try_Legacy_ID = false, bool Restore_Address = true )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      if ( !Is_Connected )
        return "";

      // Check cache first
      if ( _Verified_Cache.TryGetValue ( Address, out var Cached ) )
      {
        Capture_Trace.Write ( $"Cache hit for address {Address}: {Cached.ID_String}" );
        return Cached.ID_String;
      }

      if ( Mode == Connection_Mode.Direct_Serial )
        return Try_Query_Short ( "*IDN?" );

      int Original_Address = GPIB_Address;
      try
      {
        // ── Always initialize Prologix fully before any query ────────────
        Capture_Trace.Write ( $"Initializing Prologix for address {Address}" );
        Raw_Write ( "++mode 1" );
        Thread.Sleep ( 100 );
        Raw_Write ( "++auto 0" );
        Raw_Write ( "++eoi 1" );
        Raw_Write ( $"++addr {Address}" );
        GPIB_Address = Address;
        Thread.Sleep ( 200 );

        Flush_Device_Buffer ( );

        string Response = "";

        // ── Pass 1: SCPI *IDN? (modern instruments) ──────────────────────
        Capture_Trace.Write ( $"Pass 1: trying *IDN? at {Address}" );
        Raw_Write ( "++eos 2" );    // LF terminator for SCPI
        Thread.Sleep ( 50 );
        Response = Try_Query_Short ( "*IDN?" );

        // ── Pass 2: Legacy ID? (3458A, pre-SCPI HP instruments) ──────────
        if ( string.IsNullOrEmpty ( Response ) )
        {
          Capture_Trace.Write ( $"Pass 2: trying ID? at {Address}" );
          Flush_Device_Buffer ( );
          Raw_Write ( "++eos 2" );    // 3458A still uses LF
          Thread.Sleep ( 50 );

          // Stop any running measurement first
          Raw_Write ( "TRIG HOLD" );
          Thread.Sleep ( 200 );
          Drain_Buffer ( );

          Response = Try_Query_Short ( "ID?" );
        }

        // ── Pass 3: Numeric probe (3456A, truly pre-SCPI) ────────────────
        if ( string.IsNullOrEmpty ( Response ) )
        {
          Capture_Trace.Write ( $"Pass 3: trying numeric probe at {Address}" );
          Flush_Device_Buffer ( );
          Raw_Write ( "++eos 3" );    // 3456A uses CR only
          Thread.Sleep ( 50 );

          Raw_Write ( "W" );          // reset
          Thread.Sleep ( 200 );
          Raw_Write ( "F1R0S1Z1" );   // DCV, autorange, 1 PLC, autozero on
          Thread.Sleep ( 100 );
          Raw_Write ( "T3" );         // trigger
          Thread.Sleep ( 500 );

          Raw_Write ( "++read eoi" );
          string Numeric = Read_With_Timeout ( 3000 );

          if ( double.TryParse ( Numeric.Trim ( ),
                   System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out _ ) )
          {
            Capture_Trace.Write ( $"Pass 3 succeeded: numeric response {Numeric}" );
            Response = "HP3456A";
          }
          else
          {
            Capture_Trace.Write ( $"Pass 3: non-numeric response '{Numeric}'" );
          }
        }

        // ── Detect and cache ──────────────────────────────────────────────
        if ( !string.IsNullOrEmpty ( Response ) )
        {
          Meter_Type Detected;
          string Upper = Response.ToUpperInvariant ( );

          if ( Upper.Contains ( "34420" ) )
            Detected = Meter_Type.HP34420;  // must be before 34401
          else if ( Upper.Contains ( "34401" ) )
            Detected = Meter_Type.HP34401;
          else if ( Upper.Contains ( "53132" ) )
            Detected = Meter_Type.HP53132;
          else if ( Upper.Contains ( "33120" ) )
            Detected = Meter_Type.HP33120;
          else if ( Upper.Contains ( "3458" ) )
            Detected = Meter_Type.HP3458;   // must be before 3456
          else if ( Upper.Contains ( "3456" ) )
            Detected = Meter_Type.HP3456;
          else
            Detected = Meter_Type.Generic_GPIB;

          _Verified_Cache [ Address ] = new Scan_Result
          {
            Address = Address,
            ID_String = Response,
            Detected_Type = Detected
          };

          Capture_Trace.Write ( $"Detected {Detected} at address {Address}: {Response}" );
        }

        return Response;
      }
      catch ( Exception Ex )
      {
        Capture_Trace.Write ( $"Verify failed at address {Address}: {Ex.Message}" );
        return "";
      }
      finally
      {
        if ( Restore_Address )
        {
          Raw_Write ( $"++addr {Original_Address}" );
          Thread.Sleep ( 100 );
          GPIB_Address = Original_Address;
        }
      }
    }

    private string Read_With_Timeout ( int Timeout_Ms = 3000 )
    {
      using var Cts = new CancellationTokenSource ( Timeout_Ms );
      return Mode == Connection_Mode.Prologix_Ethernet
          ? Read_Ethernet ( Cts.Token )
          : Read_Serial ( Cts.Token );
    }


    // Drain any stale bytes sitting in the buffer for current address
    private void Flush_Device_Buffer ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      try
      {
        if ( Mode == Connection_Mode.Prologix_Ethernet )
        {
          byte [ ] Drain = new byte [ 1024 ];
          while ( _Tcp_Stream?.DataAvailable == true )
            _Tcp_Stream.Read ( Drain, 0, Drain.Length );
        }
        else
        {
          _Port?.DiscardInBuffer ( );
        }
      }
      catch { }
    }

    // =========================================================
    // ABORT PENDING OPERATIONS
    // =========================================================
    [System.Runtime.InteropServices.DllImport ( "kernel32.dll", SetLastError = true )]
    private static extern bool PurgeComm ( IntPtr hFile, uint dwFlags );

    private const uint PURGE_ALL = 0x000F; // TXABORT | RXABORT | TXCLEAR | RXCLEAR

    public void Abort_Pending_Operations ( )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      Capture_Trace.Write ( "Aborting pending operations..." );

      // --- Serial port ---
      if ( _Port?.IsOpen == true )
      {
        try
        {
          // Purge at the Windows driver level — deeper than DiscardInBuffer
          var Handle = ( _Port.BaseStream as FileStream )?.SafeFileHandle?.DangerousGetHandle ( );
          if ( Handle.HasValue && Handle.Value != IntPtr.Zero )
            PurgeComm ( Handle.Value, PURGE_ALL );
        }
        catch { }

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
      }

      // --- Ethernet ---
      if ( _Tcp_Stream?.CanRead == true )
      {
        try
        {
          byte [ ] Drain = new byte [ 4096 ];
          while ( _Tcp_Stream.DataAvailable )
            _Tcp_Stream.Read ( Drain, 0, Drain.Length );
        }
        catch { }
      }

      // Give any in-flight Thread.Sleep / poll loop time to observe the port state
      Thread.Sleep ( 150 );

      Capture_Trace.Write ( "Abort complete." );
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
