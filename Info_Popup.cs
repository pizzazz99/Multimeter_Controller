using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trace_Execution_Namespace;
using static Trace_Execution_Namespace.Trace_Execution;

namespace Multimeter_Controller
{
  public partial class Info_Popup : Form
  {
    private readonly Instrument_Comm _Comm;
    private readonly string [ ] _Commands;
    private readonly Meter_Type _Meter;

    public Info_Popup ( Instrument_Comm Comm, string [ ] Commands, Meter_Type Meter )
    {
      InitializeComponent ( );
      _Comm = Comm;
      _Commands = Commands;
      _Meter = Meter;

      this.Load += Info_Popup_Load;
    }




    private async void Info_Popup_Load ( object Sender, EventArgs E )
    {
      using var Block = Trace_Block.Start_If_Enabled ( );

      foreach ( string Command in _Commands )
      {
        try
        {
          Capture_Trace.Write ( "Querying Instrument with Command: " + Command );

          string Response = await Task.Run ( ( ) => _Comm.Query_Instrument ( Command ) );
          Results_Listbox.Items.Add ( $"{Command}  ->  {Response}" );
        }
        catch ( Exception Ex )
        {
          Results_Listbox.Items.Add ( $"{Command}  ->  ERROR: {Ex.Message}" );
        }
        Results_Listbox.TopIndex = Results_Listbox.Items.Count - 1;  // scroll to latest
      }
    }
  }
}
