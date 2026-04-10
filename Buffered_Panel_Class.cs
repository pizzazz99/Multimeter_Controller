using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{
  internal class Buffered_Panel : Panel
  {
    public Buffered_Panel ( )
    {
      DoubleBuffered = true;
      ResizeRedraw = true;
    }
  }
}
