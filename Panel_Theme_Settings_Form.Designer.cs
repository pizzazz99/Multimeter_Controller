namespace Multimeter_Controller
{
  partial class Panel_Theme_Settings_Form
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

    private void InitializeComponent()
    {
      Title_Label = new Label();
      Bg_Panel_Label = new Label();
      Bg_Panel_Swatch = new Panel();
      Fg_Swatch = new Panel();
      Dark_Preset_Button = new Button();
      Light_Preset_Button = new Button();
      OK_Button = new Button();
      Cancel_Close_Button = new Button();
      button1 = new Button();
      button2 = new Button();
      button3 = new Button();
      button4 = new Button();
      button5 = new Button();
      Fg_Panel_Label = new Label();
      Fg_Panel_Swatch = new Panel();
      panel2 = new Panel();
      SuspendLayout();
      // 
      // Title_Label
      // 
      Title_Label.AutoSize = true;
      Title_Label.Font = new Font( "Segoe UI", 11F, FontStyle.Bold );
      Title_Label.Location = new Point( 14, 12 );
      Title_Label.Name = "Title_Label";
      Title_Label.Size = new Size( 99, 20 );
      Title_Label.TabIndex = 0;
      Title_Label.Text = "Panel Theme";
      // 
      // Bg_Panel_Label
      // 
      Bg_Panel_Label.AutoSize = true;
      Bg_Panel_Label.Location = new Point( 14, 50 );
      Bg_Panel_Label.Name = "Bg_Panel_Label";
      Bg_Panel_Label.Size = new Size( 74, 15 );
      Bg_Panel_Label.TabIndex = 1;
      Bg_Panel_Label.Text = "Background:";
      // 
      // Bg_Panel_Swatch
      // 
      Bg_Panel_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Bg_Panel_Swatch.Cursor = Cursors.Hand;
      Bg_Panel_Swatch.Location = new Point( 160, 46 );
      Bg_Panel_Swatch.Name = "Bg_Panel_Swatch";
      Bg_Panel_Swatch.Size = new Size( 100, 24 );
      Bg_Panel_Swatch.TabIndex = 2;
      Bg_Panel_Swatch.Click += Bg_Panel_Swatch_Click;
      // 
      // Fg_Swatch
      // 
      Fg_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Fg_Swatch.Cursor = Cursors.Hand;
      Fg_Swatch.Location = new Point( 160, 46 );
      Fg_Swatch.Name = "Fg_Swatch";
      Fg_Swatch.Size = new Size( 100, 24 );
      Fg_Swatch.TabIndex = 2;
      Fg_Swatch.Click += Fg_Panel_Swatch_Click;
      // 
      // Dark_Preset_Button
      // 
      Dark_Preset_Button.Location = new Point( 14, 324 );
      Dark_Preset_Button.Name = "Dark_Preset_Button";
      Dark_Preset_Button.Size = new Size( 100, 28 );
      Dark_Preset_Button.TabIndex = 17;
      Dark_Preset_Button.Text = "Dark Preset";
      Dark_Preset_Button.UseVisualStyleBackColor = true;
      Dark_Preset_Button.Click += Dark_Preset_Button_Click;
      // 
      // Light_Preset_Button
      // 
      Light_Preset_Button.Location = new Point( 120, 324 );
      Light_Preset_Button.Name = "Light_Preset_Button";
      Light_Preset_Button.Size = new Size( 100, 28 );
      Light_Preset_Button.TabIndex = 18;
      Light_Preset_Button.Text = "Light Preset";
      Light_Preset_Button.UseVisualStyleBackColor = true;
      Light_Preset_Button.Click += Light_Preset_Button_Click;
      // 
      // OK_Button
      // 
      OK_Button.Location = new Point( 14, 473 );
      OK_Button.Name = "OK_Button";
      OK_Button.Size = new Size( 80, 28 );
      OK_Button.TabIndex = 19;
      OK_Button.Text = "OK";
      OK_Button.UseVisualStyleBackColor = true;
      OK_Button.Click += OK_Button_Click;
      // 
      // Cancel_Close_Button
      // 
      Cancel_Close_Button.Location = new Point( 183, 473 );
      Cancel_Close_Button.Name = "Cancel_Close_Button";
      Cancel_Close_Button.Size = new Size( 80, 28 );
      Cancel_Close_Button.TabIndex = 20;
      Cancel_Close_Button.Text = "Cancel";
      Cancel_Close_Button.UseVisualStyleBackColor = true;
      Cancel_Close_Button.Click += Cancel_Close_Button_Click;
      // 
      // button1
      // 
      button1.Location = new Point( 14, 358 );
      button1.Name = "button1";
      button1.Size = new Size( 100, 28 );
      button1.TabIndex = 21;
      button1.Text = "Brown";
      button1.UseVisualStyleBackColor = true;
      button1.Click += Brown_Preset_Button_Click;
      // 
      // button2
      // 
      button2.Location = new Point( 120, 358 );
      button2.Name = "button2";
      button2.Size = new Size( 100, 28 );
      button2.TabIndex = 22;
      button2.Text = "Gray";
      button2.UseVisualStyleBackColor = true;
      button2.Click += Grey_Preset_Button_Click;
      // 
      // button3
      // 
      button3.Location = new Point( 14, 392 );
      button3.Name = "button3";
      button3.Size = new Size( 100, 28 );
      button3.TabIndex = 23;
      button3.Text = "Golden";
      button3.UseVisualStyleBackColor = true;
      button3.Click += Golden_Preset_Button_Click;
      // 
      // button4
      // 
      button4.Location = new Point( 120, 392 );
      button4.Name = "button4";
      button4.Size = new Size( 100, 28 );
      button4.TabIndex = 24;
      button4.Text = "Light Yellow";
      button4.UseVisualStyleBackColor = true;
      button4.Click += Light_Yellow_Preset_Button_Click;
      // 
      // button5
      // 
      button5.Location = new Point( 14, 426 );
      button5.Name = "button5";
      button5.Size = new Size( 100, 28 );
      button5.TabIndex = 25;
      button5.Text = "Light Blue";
      button5.UseVisualStyleBackColor = true;
      button5.Click += Light_Blue_Preset_Button_Click;
      // 
      // Fg_Panel_Label
      // 
      Fg_Panel_Label.AutoSize = true;
      Fg_Panel_Label.Location = new Point( 12, 86 );
      Fg_Panel_Label.Name = "Fg_Panel_Label";
      Fg_Panel_Label.Size = new Size( 72, 15 );
      Fg_Panel_Label.TabIndex = 26;
      Fg_Panel_Label.Text = "Foreground:";
      // 
      // Fg_Panel_Swatch
      // 
      Fg_Panel_Swatch.BorderStyle = BorderStyle.FixedSingle;
      Fg_Panel_Swatch.Cursor = Cursors.Hand;
      Fg_Panel_Swatch.Location = new Point( 158, 82 );
      Fg_Panel_Swatch.Name = "Fg_Panel_Swatch";
      Fg_Panel_Swatch.Size = new Size( 100, 24 );
      Fg_Panel_Swatch.TabIndex = 27;
      Fg_Panel_Swatch.Click += Fg_Panel_Swatch_Click;
      // 
      // panel2
      // 
      panel2.BorderStyle = BorderStyle.FixedSingle;
      panel2.Cursor = Cursors.Hand;
      panel2.Location = new Point( 158, 82 );
      panel2.Name = "panel2";
      panel2.Size = new Size( 100, 24 );
      panel2.TabIndex = 28;
      // 
      // Panel_Theme_Settings_Form
      // 
      AutoScaleDimensions = new SizeF( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size( 280, 513 );
      Controls.Add( Fg_Panel_Label );
      Controls.Add( Fg_Panel_Swatch );
      Controls.Add( panel2 );
      Controls.Add( button5 );
      Controls.Add( button3 );
      Controls.Add( button4 );
      Controls.Add( button1 );
      Controls.Add( button2 );
      Controls.Add( Title_Label );
      Controls.Add( Bg_Panel_Label );
      Controls.Add( Bg_Panel_Swatch );
      Controls.Add( Fg_Swatch );
      Controls.Add( Dark_Preset_Button );
      Controls.Add( Light_Preset_Button );
      Controls.Add( OK_Button );
      Controls.Add( Cancel_Close_Button );
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      MinimizeBox = false;
      Name = "Panel_Theme_Settings_Form";
      StartPosition = FormStartPosition.CenterParent;
      Text = "Chart Theme Settings";
      ResumeLayout( false );
      PerformLayout();
    }

    #endregion





    private System.Windows.Forms.Label Title_Label;
    private System.Windows.Forms.Label Bg_Panel_Label;
    private System.Windows.Forms.Panel Bg_Panel_Swatch;
    private System.Windows.Forms.Panel Fg_Swatch;
    private System.Windows.Forms.Button Dark_Preset_Button;
    private System.Windows.Forms.Button Light_Preset_Button;
    private System.Windows.Forms.Button OK_Button;
    private System.Windows.Forms.Button Cancel_Close_Button;
    private Button button1;
    private Button button2;
    private Button button3;
    private Button button4;
    private Button button5;
    private Label Fg_Panel_Label;
    private Panel Fg_Panel_Swatch;
    private Panel panel2;
  }
}
