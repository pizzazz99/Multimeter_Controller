using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Multimeter_Controller
{
  partial class Settings_Form
  {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose ( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose ( );
      }
      base.Dispose ( disposing );
    }

    private void InitializeComponent ( )
    {
      Main_Tab_Control = new TabControl ( );
      Prologix_Tab = new TabPage ( );
      Display_Tab = new TabPage ( );
      Polling_Tab = new TabPage ( );
      Files_Tab = new TabPage ( );
      Performance_Tab = new TabPage ( );
      UI_Tab = new TabPage ( );
      Zoom_Tab = new TabPage ( );
      HP3458_Tab = new TabPage ( );
      OK_Button = new Button ( );
      Cancel_Button = new Button ( );
      Apply_Button = new Button ( );
      Reset_Button = new Button ( );
     

      Main_Tab_Control.SuspendLayout ( );
      SuspendLayout ( );

      // 
      // Main_Tab_Control
      //
      Main_Tab_Control.Controls.Add ( HP3458_Tab );
      Main_Tab_Control.Controls.Add ( Prologix_Tab );
      Main_Tab_Control.Controls.Add ( Display_Tab );
      Main_Tab_Control.Controls.Add ( Polling_Tab );
      Main_Tab_Control.Controls.Add ( Files_Tab );
      Main_Tab_Control.Controls.Add ( Performance_Tab );
      Main_Tab_Control.Controls.Add ( UI_Tab );
      Main_Tab_Control.Controls.Add ( Zoom_Tab );
      Main_Tab_Control.Location = new Point ( 12, 12 );
      Main_Tab_Control.Name = "Main_Tab_Control";
      Main_Tab_Control.SelectedIndex = 0;
      Main_Tab_Control.Size = new Size ( 560, 480 );
      Main_Tab_Control.TabIndex = 0;


      // HP3458_Tab
      HP3458_Tab.BackColor = SystemColors.Control;
      HP3458_Tab.Location = new Point ( 4, 24 );
      HP3458_Tab.Name = "HP3458_Tab";
      HP3458_Tab.Size = new Size ( 552, 452 );
      HP3458_Tab.TabIndex = 6;
      HP3458_Tab.Text = "HP3458";  // ← this line is what shows as the tab title
                                                   // Prologix_Tab
      Prologix_Tab.BackColor = SystemColors.Control;
      Prologix_Tab.Location = new Point ( 4, 24 );
      Prologix_Tab.Name = "Prologix_Tab";
      Prologix_Tab.Size = new Size ( 552, 452 );
      Prologix_Tab.TabIndex = 7;
      Prologix_Tab.Text = "Prologix";
      // 
      // Display_Tab
      // 
      Display_Tab.BackColor = SystemColors.Control;
      Display_Tab.Location = new Point ( 4, 24 );
      Display_Tab.Name = "Display_Tab";
      Display_Tab.Padding = new Padding ( 3 );
      Display_Tab.Size = new Size ( 552, 452 );
      Display_Tab.TabIndex = 0;
      Display_Tab.Text = "Display";

      // 
      // Polling_Tab
      // 
      Polling_Tab.BackColor = SystemColors.Control;
      Polling_Tab.Location = new Point ( 4, 24 );
      Polling_Tab.Name = "Polling_Tab";
      Polling_Tab.Padding = new Padding ( 3 );
      Polling_Tab.Size = new Size ( 552, 452 );
      Polling_Tab.TabIndex = 1;
      Polling_Tab.Text = "Polling";

      // 
      // Files_Tab
      // 
      Files_Tab.BackColor = SystemColors.Control;
      Files_Tab.Location = new Point ( 4, 24 );
      Files_Tab.Name = "Files_Tab";
      Files_Tab.Size = new Size ( 552, 452 );
      Files_Tab.TabIndex = 2;
      Files_Tab.Text = "Files";

      // 
      // Performance_Tab
      // 
      Performance_Tab.BackColor = SystemColors.Control;
      Performance_Tab.Location = new Point ( 4, 24 );
      Performance_Tab.Name = "Performance_Tab";
      Performance_Tab.Size = new Size ( 552, 452 );
      Performance_Tab.TabIndex = 3;
      Performance_Tab.Text = "Performance";

      // 
      // UI_Tab
      // 
      UI_Tab.BackColor = SystemColors.Control;
      UI_Tab.Location = new Point ( 4, 24 );
      UI_Tab.Name = "UI_Tab";
      UI_Tab.Size = new Size ( 552, 452 );
      UI_Tab.TabIndex = 4;
      UI_Tab.Text = "UI/UX";

      // 
      // Zoom_Tab
      // 
      Zoom_Tab.BackColor = SystemColors.Control;
      Zoom_Tab.Location = new Point ( 4, 24 );
      Zoom_Tab.Name = "Zoom_Tab";
      Zoom_Tab.Size = new Size ( 552, 452 );
      Zoom_Tab.TabIndex = 5;
      Zoom_Tab.Text = "Zoom";

      // 
      // OK_Button
      // 
      OK_Button.Location = new Point ( 305, 505 );
      OK_Button.Name = "OK_Button";
      OK_Button.Size = new Size ( 75, 30 );
      OK_Button.TabIndex = 1;
      OK_Button.Text = "OK";
      OK_Button.UseVisualStyleBackColor = true;
      OK_Button.Click += OK_Button_Click;

      // 
      // Cancel_Button
      // 
      Cancel_Button.DialogResult = DialogResult.Cancel;
      Cancel_Button.Location = new Point ( 386, 505 );
      Cancel_Button.Name = "Cancel_Button";
      Cancel_Button.Size = new Size ( 75, 30 );
      Cancel_Button.TabIndex = 2;
      Cancel_Button.Text = "Cancel";
      Cancel_Button.UseVisualStyleBackColor = true;
      Cancel_Button.Click += Cancel_Button_Click;

      // 
      // Apply_Button
      // 
      Apply_Button.Location = new Point ( 467, 505 );
      Apply_Button.Name = "Apply_Button";
      Apply_Button.Size = new Size ( 75, 30 );
      Apply_Button.TabIndex = 3;
      Apply_Button.Text = "Apply";
      Apply_Button.UseVisualStyleBackColor = true;
      Apply_Button.Click += Apply_Button_Click;

      // 
      // Reset_Button
      // 
      Reset_Button.Location = new Point ( 12, 505 );
      Reset_Button.Name = "Reset_Button";
      Reset_Button.Size = new Size ( 100, 30 );
      Reset_Button.TabIndex = 4;
      Reset_Button.Text = "Reset Defaults";
      Reset_Button.UseVisualStyleBackColor = true;
      Reset_Button.Click += Reset_Button_Click;

      // 
      // Settings_Form
      // 
      AcceptButton = OK_Button;
      AutoScaleDimensions = new SizeF ( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = Cancel_Button;
      ClientSize = new Size ( 584, 547 );
      Controls.Add ( Reset_Button );
      Controls.Add ( Apply_Button );
      Controls.Add ( Cancel_Button );
      Controls.Add ( OK_Button );
      Controls.Add ( Main_Tab_Control );
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      MinimizeBox = false;
      Name = "Settings_Form";
      StartPosition = FormStartPosition.CenterParent;
      Text = "Multi-Poll Settings";
      Main_Tab_Control.ResumeLayout ( false );
      ResumeLayout ( false );
    }

    private TabControl Main_Tab_Control;
    private TabPage Display_Tab;
    private TabPage Polling_Tab;
    private TabPage Files_Tab;
    private TabPage Performance_Tab;
    private TabPage UI_Tab;
    private TabPage Zoom_Tab;
    private TabPage HP3458_Tab;
    private Button OK_Button;
    private Button Cancel_Button;
    private Button Apply_Button;
    private Button Reset_Button;
   
    private TabPage Prologix_Tab;
  }
}
