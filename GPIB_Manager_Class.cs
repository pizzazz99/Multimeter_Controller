using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  public class GPIB_Manager
  {
    private Application_Settings _Settings;
    private Instrument_Comm _Comm;
    private int _Current_Retry_Count = 0;

    public GPIB_Manager ( Application_Settings settings, Instrument_Comm comm )
    {
      _Settings = settings;
      _Comm = comm;
    }

    public double Read_Measurement ( int address, string command = "READ?" )
    {
      _Current_Retry_Count = 0;
      Exception Last_Exception = null;

      // Save original settings
      int Original_Timeout = _Comm.Read_Timeout_Ms;

      try
      {
        // Apply timeout from settings
        _Comm.Read_Timeout_Ms = _Settings.Default_GPIB_Timeout_Ms;

        while ( _Current_Retry_Count <= _Settings.Max_Retry_Attempts )
        {
          try
          {
            // Change address and query
            _Comm.Change_GPIB_Address ( address );
            string Response = _Comm.Query_Instrument ( command );

            if ( string.IsNullOrWhiteSpace ( Response ) )
            {
              throw new InvalidOperationException ( "Empty response" );
            }

            return double.Parse ( Response, CultureInfo.InvariantCulture );
          }
          catch ( Exception ex )
          {
            Last_Exception = ex;
            _Current_Retry_Count++;

            if ( _Current_Retry_Count <= _Settings.Max_Retry_Attempts )
            {
              // Wait before retry (exponential backoff)
              int Wait_Ms = 100 * (int) Math.Pow ( 2, _Current_Retry_Count - 1 );
              Thread.Sleep ( Wait_Ms );
            }
          }
        }

        // All retries failed
        string Error_Message = _Current_Retry_Count > 0
          ? $"Failed after {_Settings.Max_Retry_Attempts} retries: {Last_Exception?.Message}"
          : $"Error: {Last_Exception?.Message}";

        throw new InvalidOperationException ( Error_Message, Last_Exception );
      }
      finally
      {
        // Restore original timeout
        _Comm.Read_Timeout_Ms = Original_Timeout;
      }
    }
  }
}




