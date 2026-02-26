using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  public class Instrument
  {
    public string Name { get; set; } = "";
    public int Address
    {
      get; set;
    }
    public Meter_Type Type
    {
      get; set;
    }
    public bool Verified
    {
      get; set;
    }
    public bool Visible { get; set; } = true;

    public string Display => $"{Name}  (GPIB {Address}, {Type})";

    // For UI display
    public string DisplayString ( string Transport_Mode )
    {
      bool Is_GPIB = Transport_Mode == "Prologix_GPIB" || Transport_Mode == "Prologix_Ethernet";
      return Is_GPIB
          ? $"{Name}  (GPIB {Address}, {Type})"
          : $"{Name}  ({Type})";
    }


  }
}
