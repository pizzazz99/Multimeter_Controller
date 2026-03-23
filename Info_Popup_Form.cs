
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Info_Popup_Form.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Minimal read-only popup that displays a plain-text or pre-formatted string
//   in a scrollable RichTextBox.  Used as a lightweight in-app reference viewer
//   (e.g. meter specification sheets, command quick-reference, help text).
//
// CONSTRUCTOR
//   Info_Popup_Form(string Content)
//     Builds the entire UI inline — no designer file is used beyond the partial
//     class declaration.  The window is fixed at 700 × 600, centred on its
//     parent, and contains a single fill-docked RichTextBox (Consolas 9pt,
//     word-wrap off, both scrollbars) pre-loaded with Content.
//
// NOTES
//   • The form has no buttons or toolbar; close via the title-bar control box.
//   • Word-wrap is disabled so that columnar or tabulated reference text
//     renders correctly without line breaks being inserted.
//   • Content is assigned directly to RichTextBox.Text — RTF markup is not
//     interpreted; pass plain text only.
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════



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

