using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  public static class Instrument_Command_Tester
  {
    public static List<Command_Test_Result> Test_All_Commands(
    IInstrument_Test_Profile Profile,
    Func<string, string> Query,
    Action<string> Send,
    Action<string>? Progress = null,
    CancellationToken Token = default )
    {
      var Results = new List<Command_Test_Result>();
      var Commands = Profile.Get_Commands();

      try
      {
        Send( Profile.Reset_Command );
      }
      catch { }
      try
      {
        Send( "*CLS" );
      }
      catch { }

      foreach (var Cmd in Commands)
      {
        if (Token.IsCancellationRequested)
          return Results;

        Command_Test_Result Result;

        try
        {
          Result = Cmd.Test_Behavior switch
          {
            Test_Behavior.Query_Safe => Run_Query( Cmd, Query ),
            Test_Behavior.Write_Then_Query => Run_Write_Then_Query( Cmd, Send, Query ),
            Test_Behavior.Requires_Sequence => Command_Test_Result.Skip( Cmd, "Requires sequence" ),
            Test_Behavior.Skip_Destructive => Command_Test_Result.Skip( Cmd, "Destructive — skipped" ),
            _ => Command_Test_Result.Skip( Cmd, "Unknown behavior" )
          };
        }
        catch (OperationCanceledException)
        {
          return Results;
        }
        catch (Exception Ex)
        {
          Result = Command_Test_Result.Fail( Cmd, $"Exception: {Ex.Message}" );
        }

        if (!Result.Skipped && Profile.Has_Error_Queue)
        {
          try
          {
            string Err = Query( Profile.Error_Query );
            if (!Err.TrimStart().StartsWith( "+0" ) && !Err.TrimStart().StartsWith( "0" ))
              Result.Flag_Instrument_Error( Err );
          }
          catch (OperationCanceledException) { return Results; }
          catch { }
        }

        Progress?.Invoke( Result.Summary );
        Results.Add( Result );
      }

      // ── Sequenced blocks ──────────────────────────────────────────────────
      if (Token.IsCancellationRequested)
        return Results;

      Progress?.Invoke( "" );
      Progress?.Invoke( "--- Sequenced Tests ---" );

      try
      {
        foreach (var R in Profile.Run_Sequences( Query, Send ))
        {
          if (Token.IsCancellationRequested)
            return Results;
          Progress?.Invoke( R.Summary );
          Results.Add( R );
        }
      }
      catch (OperationCanceledException)
      {
        return Results;
      }
      catch (Exception Ex)
      {
        Progress?.Invoke( $"[Sequence error: {Ex.Message}]" );
      }

      return Results;
    }


    private static Command_Test_Result Run_Query( Command_Entry Cmd,
                                                  Func<string, string> Query )
    {
      string Target = !string.IsNullOrWhiteSpace( Cmd.Query_Form ) ? Cmd.Query_Form : Cmd.Command;
      string Response = Query( Target );
      return string.IsNullOrWhiteSpace( Response )
        ? Command_Test_Result.Fail( Cmd, "Empty response" )
        : Command_Test_Result.Pass( Cmd, Response );
    }

    private static Command_Test_Result Run_Write_Then_Query( Command_Entry Cmd,
                                                             Action<string> Send,
                                                             Func<string, string> Query )
    {
      if (string.IsNullOrWhiteSpace( Cmd.Query_Form ))
        return Command_Test_Result.Skip( Cmd, "No query form to verify" );

      if (!string.IsNullOrWhiteSpace( Cmd.Example ))
        Send( Cmd.Example );

      string Response = Query( Cmd.Query_Form );
      return string.IsNullOrWhiteSpace( Response )
        ? Command_Test_Result.Fail( Cmd, "No response to query after write" )
        : Command_Test_Result.Pass( Cmd, Response );
    }
  }
}
