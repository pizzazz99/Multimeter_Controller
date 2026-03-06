using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Multimeter_Controller
{
  public partial class Info_Popup_Form : Form
  {
    public Info_Popup_Form ( string Content )
    {
      Text = "Meter Reference";
      Size = new Size ( 700, 600 );
      StartPosition = FormStartPosition.CenterParent;

      var Text_Box = new RichTextBox
      {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        Font = new Font ( "Consolas", 9f ),
        WordWrap = false,
        ScrollBars = RichTextBoxScrollBars.Both,
        Text = Content
      };
      Controls.Add ( Text_Box );
    }
  }
}

