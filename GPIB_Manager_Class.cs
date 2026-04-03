
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    GPIB_Manager.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Thin orchestration layer that sits between the polling logic and the raw
//   Instrument_Comm transport.  Adds configurable retry-with-exponential-backoff
//   and automatic timeout save/restore around every GPIB read, so callers
//   do not need to handle these concerns themselves.
//
// CONSTRUCTOR
//   GPIB_Manager(Application_Settings, Instrument_Comm)
//     Stores references to the shared settings and the low-level comm object.
//     No hardware interaction occurs at construction time.
//
// KEY METHOD
//   double Read_Measurement(int address, string command = "READ?")
//     1. Saves the current Instrument_Comm.Read_Timeout_Ms.
//     2. Applies Default_GPIB_Timeout_Ms from settings for the duration.
//     3. Switches the adapter to the requested GPIB address.
//     4. Issues the command via Query_Instrument() and parses the response
//        as a double (InvariantCulture).
//     5. On any exception, waits 100 × 2^(attempt-1) ms (exponential backoff)
//        and retries up to Max_Retry_Attempts times.
//     6. Restores the original timeout in a finally block regardless of outcome.
//     7. Throws InvalidOperationException if all attempts fail, with the
//        retry count and the last exception message included in the message.
//
// RETRY BEHAVIOUR
//   Attempts  : 1 initial + Max_Retry_Attempts retries
//   Backoff   : 100 ms, 200 ms, 400 ms, 800 ms … (doubles each attempt)
//   Triggers  : any exception from Query_Instrument(), including empty response
//
// DEPENDENCIES
//   Application_Settings   — Default_GPIB_Timeout_Ms, Max_Retry_Attempts
//   Instrument_Comm        — Change_GPIB_Address(), Query_Instrument(),
//                            Read_Timeout_Ms
//
// NOTES
//   • _Current_Retry_Count is an instance field reset to 0 at the start of
//     every call; GPIB_Manager is not thread-safe and should not be shared
//     across concurrent polling threads.
//   • Thread.Sleep() is called on the calling thread — if Read_Measurement()
//     is invoked from the UI thread it will block. Call from a background
//     thread (e.g. the polling Task) only.
//   • The class currently has no logging or event surface; failed attempts
//     are silent until all retries are exhausted.
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════





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




