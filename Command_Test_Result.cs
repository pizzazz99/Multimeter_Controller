using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  public class Command_Test_Result
  {
    public string Command { get; set; } = "";
    public Test_Behavior Behavior
    {
      get; set;
    }
    public bool Passed
    {
      get; set;
    }
    public bool Skipped
    {
      get; set;
    }
    public string Response { get; set; } = "";
    public string Error_Message { get; set; } = "";
    public string Notes { get; set; } = "";

    public string Summary =>
      Skipped ? $"[SKIP]  {Command}  —  {Notes}"
      : Passed ? $"[PASS]  {Command}  —  {Response}"
               : $"[FAIL]  {Command}  —  {Error_Message}";

    public static Command_Test_Result Pass( Command_Entry Cmd, string Response ) =>
      new()
      {
        Command = Cmd.Command,
        Behavior = Cmd.Test_Behavior,
        Passed = true,
        Response = Response.Trim()
      };

    public static Command_Test_Result Fail( Command_Entry Cmd, string Reason ) =>
      new()
      {
        Command = Cmd.Command,
        Behavior = Cmd.Test_Behavior,
        Passed = false,
        Error_Message = Reason
      };

    public static Command_Test_Result Skip( Command_Entry Cmd, string Reason = "" ) =>
      new()
      {
        Command = Cmd.Command,
        Behavior = Cmd.Test_Behavior,
        Skipped = true,
        Notes = string.IsNullOrEmpty( Reason ) ? Cmd.Test_Behavior.ToString() : Reason
      };

    public void Flag_Instrument_Error( string Err_Response ) =>
      Error_Message += $"  [SYST:ERR? -> {Err_Response.Trim()}]";
  }
}
