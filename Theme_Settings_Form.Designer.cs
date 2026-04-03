namespace Multimeter_Controller
{
  partial class Theme_Settings_Form
  {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose ( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose ();
      }
      base.Dispose ( disposing );
    }

    #region Windows Form Designer generated code

    private void InitializeComponent ( )
    {
      Title_Label = new Label ( );
      Bg_Label = new Label ( );
      Bg_Swatch = new Panel ( );
      Fg_Swatch = new Panel ( );
      Grid_Label = new Label ( );
      Grid_Swatch = new Panel ( );
      Labels_Label = new Label ( );
      Labels_Swatch = new Panel ( );
      Separator_Label = new Label ( );
      Separator_Swatch = new Panel ( );
      Line1_Label = new Label ( );
      Line1_Swatch = new Panel ( );
      Line2_Label = new Label ( );
      Line2_Swatch = new Panel ( );
      Line3_Label = new Label ( );
      Line3_Swatch = new Panel ( );
      Line4_Label = new Label ( );
      Line4_Swatch = new Panel ( );
      Dark_Preset_Button = new Button ( );
      Light_Preset_Button = new Button ( );
      OK_Button = new Button ( );
      Cancel_Close_Button = new Button ( );
      button1 = new Button ( );
      button2 = new Button ( );
      button3 = new Button ( );
      button4 = new Button ( );
      button5 = new Button ( );
      SuspendLayout ( );
      // 
      // Title_Label
      // 
      Title_Label.AutoSize = true;
      Title_Label.Font = new Font ( "Segoe UI", 11F, FontStyle.Bold );
      Title_Label.Location = new Point ( 14, 12 );
      Title_Label.Name = "Title_Label";
      Title_Label.Size = new Size ( 99, 20 );
      Title_Label.TabIndex = 0;
      Title_Label.Text = "Chart Theme";
      // 
      // Bg_Label
      // 
      Bg_Label.AutoSize = true;
      Bg_Label.Location = new Point ( 14, 50 );
      Bg_Label.Name = "Bg_Label";
      Bg_Label.Size = new Size ( 74, 15 );
      Bg_Label.TabIndex = 1;
      Bg_Label.Text = "Background:";
      // 
      // Bg_Swatch
      // 
      Bg_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Bg_Swatch.Cursor = Cursors.Hand;
      Bg_Swatch.Location = new Point ( 160, 46 );
      Bg_Swatch.Name = "Bg_Swatch";
      Bg_Swatch.Size = new Size ( 100, 24 );
      Bg_Swatch.TabIndex = 2;
      Bg_Swatch.Click +=  Bg_Swatch_Click ;
      // 
      // Fg_Swatch
      // 
      Fg_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Fg_Swatch.Cursor = Cursors.Hand;
      Fg_Swatch.Location = new Point ( 160, 46 );
      Fg_Swatch.Name = "Fg_Swatch";
      Fg_Swatch.Size = new Size ( 100, 24 );
      Fg_Swatch.TabIndex = 2;
      Fg_Swatch.Click += Fg_Swatch_Click;
      // 
      // Grid_Label
      // 
      Grid_Label.AutoSize = true;
      Grid_Label.Location = new Point ( 14, 82 );
      Grid_Label.Name = "Grid_Label";
      Grid_Label.Size = new Size ( 62, 15 );
      Grid_Label.TabIndex = 3;
      Grid_Label.Text = "Grid Lines:";
      // 
      // Grid_Swatch
      // 
      Grid_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Grid_Swatch.Cursor = Cursors.Hand;
      Grid_Swatch.Location = new Point ( 160, 78 );
      Grid_Swatch.Name = "Grid_Swatch";
      Grid_Swatch.Size = new Size ( 100, 24 );
      Grid_Swatch.TabIndex = 4;
      Grid_Swatch.Click +=  Grid_Swatch_Click ;
      // 
      // Labels_Label
      // 
      Labels_Label.AutoSize = true;
      Labels_Label.Location = new Point ( 14, 114 );
      Labels_Label.Name = "Labels_Label";
      Labels_Label.Size = new Size ( 67, 15 );
      Labels_Label.TabIndex = 5;
      Labels_Label.Text = "Axis Labels:";
      // 
      // Labels_Swatch
      // 
      Labels_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Labels_Swatch.Cursor = Cursors.Hand;
      Labels_Swatch.Location = new Point ( 160, 110 );
      Labels_Swatch.Name = "Labels_Swatch";
      Labels_Swatch.Size = new Size ( 100, 24 );
      Labels_Swatch.TabIndex = 6;
      Labels_Swatch.Click +=  Labels_Swatch_Click ;
      // 
      // Separator_Label
      // 
      Separator_Label.AutoSize = true;
      Separator_Label.Location = new Point ( 14, 146 );
      Separator_Label.Name = "Separator_Label";
      Separator_Label.Size = new Size ( 60, 15 );
      Separator_Label.TabIndex = 7;
      Separator_Label.Text = "Separator:";
      // 
      // Separator_Swatch
      // 
      Separator_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Separator_Swatch.Cursor = Cursors.Hand;
      Separator_Swatch.Location = new Point ( 160, 142 );
      Separator_Swatch.Name = "Separator_Swatch";
      Separator_Swatch.Size = new Size ( 100, 24 );
      Separator_Swatch.TabIndex = 8;
      Separator_Swatch.Click +=  Separator_Swatch_Click ;
      // 
      // Line1_Label
      // 
      Line1_Label.AutoSize = true;
      Line1_Label.Location = new Point ( 14, 190 );
      Line1_Label.Name = "Line1_Label";
      Line1_Label.Size = new Size ( 73, 15 );
      Line1_Label.TabIndex = 9;
      Line1_Label.Text = "Line Color 1:";
      // 
      // Line1_Swatch
      // 
      Line1_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Line1_Swatch.Cursor = Cursors.Hand;
      Line1_Swatch.Location = new Point ( 160, 186 );
      Line1_Swatch.Name = "Line1_Swatch";
      Line1_Swatch.Size = new Size ( 100, 24 );
      Line1_Swatch.TabIndex = 10;
      Line1_Swatch.Click +=  Line1_Swatch_Click ;
      // 
      // Line2_Label
      // 
      Line2_Label.AutoSize = true;
      Line2_Label.Location = new Point ( 14, 222 );
      Line2_Label.Name = "Line2_Label";
      Line2_Label.Size = new Size ( 73, 15 );
      Line2_Label.TabIndex = 11;
      Line2_Label.Text = "Line Color 2:";
      // 
      // Line2_Swatch
      // 
      Line2_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Line2_Swatch.Cursor = Cursors.Hand;
      Line2_Swatch.Location = new Point ( 160, 218 );
      Line2_Swatch.Name = "Line2_Swatch";
      Line2_Swatch.Size = new Size ( 100, 24 );
      Line2_Swatch.TabIndex = 12;
      Line2_Swatch.Click +=  Line2_Swatch_Click ;
      // 
      // Line3_Label
      // 
      Line3_Label.AutoSize = true;
      Line3_Label.Location = new Point ( 14, 254 );
      Line3_Label.Name = "Line3_Label";
      Line3_Label.Size = new Size ( 73, 15 );
      Line3_Label.TabIndex = 13;
      Line3_Label.Text = "Line Color 3:";
      // 
      // Line3_Swatch
      // 
      Line3_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Line3_Swatch.Cursor = Cursors.Hand;
      Line3_Swatch.Location = new Point ( 160, 250 );
      Line3_Swatch.Name = "Line3_Swatch";
      Line3_Swatch.Size = new Size ( 100, 24 );
      Line3_Swatch.TabIndex = 14;
      Line3_Swatch.Click +=  Line3_Swatch_Click ;
      // 
      // Line4_Label
      // 
      Line4_Label.AutoSize = true;
      Line4_Label.Location = new Point ( 14, 286 );
      Line4_Label.Name = "Line4_Label";
      Line4_Label.Size = new Size ( 73, 15 );
      Line4_Label.TabIndex = 15;
      Line4_Label.Text = "Line Color 4:";
      // 
      // Line4_Swatch
      // 
      Line4_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Line4_Swatch.Cursor = Cursors.Hand;
      Line4_Swatch.Location = new Point ( 160, 282 );
      Line4_Swatch.Name = "Line4_Swatch";
      Line4_Swatch.Size = new Size ( 100, 24 );
      Line4_Swatch.TabIndex = 16;
      Line4_Swatch.Click +=  Line4_Swatch_Click ;
      // 
      // Dark_Preset_Button
      // 
      Dark_Preset_Button.Location = new Point ( 14, 324 );
      Dark_Preset_Button.Name = "Dark_Preset_Button";
      Dark_Preset_Button.Size = new Size ( 100, 28 );
      Dark_Preset_Button.TabIndex = 17;
      Dark_Preset_Button.Text = "Dark Preset";
      Dark_Preset_Button.UseVisualStyleBackColor = true;
      Dark_Preset_Button.Click +=  Dark_Preset_Button_Click ;
      // 
      // Light_Preset_Button
      // 
      Light_Preset_Button.Location = new Point ( 120, 324 );
      Light_Preset_Button.Name = "Light_Preset_Button";
      Light_Preset_Button.Size = new Size ( 100, 28 );
      Light_Preset_Button.TabIndex = 18;
      Light_Preset_Button.Text = "Light Preset";
      Light_Preset_Button.UseVisualStyleBackColor = true;
      Light_Preset_Button.Click +=  Light_Preset_Button_Click ;
      // 
      // OK_Button
      // 
      OK_Button.Location = new Point ( 14, 473 );
      OK_Button.Name = "OK_Button";
      OK_Button.Size = new Size ( 80, 28 );
      OK_Button.TabIndex = 19;
      OK_Button.Text = "OK";
      OK_Button.UseVisualStyleBackColor = true;
      OK_Button.Click +=  OK_Button_Click ;
      // 
      // Cancel_Close_Button
      // 
      Cancel_Close_Button.Location = new Point ( 183, 473 );
      Cancel_Close_Button.Name = "Cancel_Close_Button";
      Cancel_Close_Button.Size = new Size ( 80, 28 );
      Cancel_Close_Button.TabIndex = 20;
      Cancel_Close_Button.Text = "Cancel";
      Cancel_Close_Button.UseVisualStyleBackColor = true;
      Cancel_Close_Button.Click +=  Cancel_Close_Button_Click ;
      // 
      // button1
      // 
      button1.Location = new Point ( 14, 358 );
      button1.Name = "button1";
      button1.Size = new Size ( 100, 28 );
      button1.TabIndex = 21;
      button1.Text = "Brown";
      button1.UseVisualStyleBackColor = true;
      button1.Click +=  Brown_Preset_Button_Click ;
      // 
      // button2
      // 
      button2.Location = new Point ( 120, 358 );
      button2.Name = "button2";
      button2.Size = new Size ( 100, 28 );
      button2.TabIndex = 22;
      button2.Text = "Gray";
      button2.UseVisualStyleBackColor = true;
      button2.Click +=  Grey_Preset_Button_Click ;
      // 
      // button3
      // 
      button3.Location = new Point ( 14, 392 );
      button3.Name = "button3";
      button3.Size = new Size ( 100, 28 );
      button3.TabIndex = 23;
      button3.Text = "Golden";
      button3.UseVisualStyleBackColor = true;
      button3.Click +=  Golden_Preset_Button_Click ;
      // 
      // button4
      // 
      button4.Location = new Point ( 120, 392 );
      button4.Name = "button4";
      button4.Size = new Size ( 100, 28 );
      button4.TabIndex = 24;
      button4.Text = "Light Yellow";
      button4.UseVisualStyleBackColor = true;
      button4.Click +=  Light_Yellow_Preset_Button_Click ;
      // 
      // button5
      // 
      button5.Location = new Point ( 14, 426 );
      button5.Name = "button5";
      button5.Size = new Size ( 100, 28 );
      button5.TabIndex = 25;
      button5.Text = "Light Blue";
      button5.UseVisualStyleBackColor = true;
      button5.Click +=  Light_Blue_Preset_Button_Click ;
      // 
      // Theme_Settings_Form
      // 
      AutoScaleDimensions = new SizeF ( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size ( 280, 513 );
      Controls.Add ( button5 );
      Controls.Add ( button3 );
      Controls.Add ( button4 );
      Controls.Add ( button1 );
      Controls.Add ( button2 );
      Controls.Add ( Title_Label );
      Controls.Add ( Bg_Label );
      Controls.Add ( Bg_Swatch );
      Controls.Add ( Fg_Swatch );
      Controls.Add ( Grid_Label );
      Controls.Add ( Grid_Swatch );
      Controls.Add ( Labels_Label );
      Controls.Add ( Labels_Swatch );
      Controls.Add ( Separator_Label );
      Controls.Add ( Separator_Swatch );
      Controls.Add ( Line1_Label );
      Controls.Add ( Line1_Swatch );
      Controls.Add ( Line2_Label );
      Controls.Add ( Line2_Swatch );
      Controls.Add ( Line3_Label );
      Controls.Add ( Line3_Swatch );
      Controls.Add ( Line4_Label );
      Controls.Add ( Line4_Swatch );
      Controls.Add ( Dark_Preset_Button );
      Controls.Add ( Light_Preset_Button );
      Controls.Add ( OK_Button );
      Controls.Add ( Cancel_Close_Button );
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      MinimizeBox = false;
      Name = "Theme_Settings_Form";
      StartPosition = FormStartPosition.CenterParent;
      Text = "Chart Theme Settings";
      ResumeLayout ( false );
      PerformLayout ( );
    }

    #endregion

    private System.Windows.Forms.Label Title_Label;
    private System.Windows.Forms.Label Bg_Label;
    private System.Windows.Forms.Panel Bg_Swatch;
    private System.Windows.Forms.Panel Fg_Swatch;
    private System.Windows.Forms.Label Grid_Label;
    private System.Windows.Forms.Panel Grid_Swatch;
    private System.Windows.Forms.Label Labels_Label;
    private System.Windows.Forms.Panel Labels_Swatch;
    private System.Windows.Forms.Label Separator_Label;
    private System.Windows.Forms.Panel Separator_Swatch;
    private System.Windows.Forms.Label Line1_Label;
    private System.Windows.Forms.Panel Line1_Swatch;
    private System.Windows.Forms.Label Line2_Label;
    private System.Windows.Forms.Panel Line2_Swatch;
    private System.Windows.Forms.Label Line3_Label;
    private System.Windows.Forms.Panel Line3_Swatch;
    private System.Windows.Forms.Label Line4_Label;
    private System.Windows.Forms.Panel Line4_Swatch;
    private System.Windows.Forms.Button Dark_Preset_Button;
    private System.Windows.Forms.Button Light_Preset_Button;
    private System.Windows.Forms.Button OK_Button;
    private System.Windows.Forms.Button Cancel_Close_Button;
    private Button button1;
    private Button button2;
    private Button button3;
    private Button button4;
    private Button button5;
  }
}
