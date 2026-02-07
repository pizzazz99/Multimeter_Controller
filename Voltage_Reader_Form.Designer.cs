namespace Multimeter_Controller
{
  partial class Voltage_Reader_Form
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
      Function_Label = new Label ( );
      Function_Combo = new ComboBox ( );
      Readings_Label = new Label ( );
      Readings_Numeric = new NumericUpDown ( );
      Continuous_Check = new CheckBox ( );
      Delay_Label = new Label ( );
      Delay_Numeric = new NumericUpDown ( );
      Start_Stop_Button = new Button ( );
      Status_Label = new Label ( );
      Current_Value_Label = new Label ( );
      Progress_Label = new Label ( );
      Graph_Style_Label = new Label ( );
      Graph_Style_Combo = new ComboBox ( );
      Chart_Panel = new Panel ( );
      Clear_Button = new Button ( );
      Record_Button = new Button ( );
      Load_Button = new Button ( );
      Theme_Button = new Button ( );
      ( (System.ComponentModel.ISupportInitialize) Readings_Numeric ).BeginInit ( );
      ( (System.ComponentModel.ISupportInitialize) Delay_Numeric ).BeginInit ( );
      SuspendLayout ( );
      //
      // Title_Label
      //
      Title_Label.AutoSize = true;
      Title_Label.Font = new Font ( "Segoe UI", 12F, FontStyle.Bold );
      Title_Label.Location = new Point ( 12, 12 );
      Title_Label.Name = "Title_Label";
      Title_Label.Size = new Size ( 126, 21 );
      Title_Label.TabIndex = 0;
      Title_Label.Text = "Voltage Reader";
      //
      // Function_Label
      //
      Function_Label.AutoSize = true;
      Function_Label.Location = new Point ( 12, 50 );
      Function_Label.Name = "Function_Label";
      Function_Label.Size = new Size ( 57, 15 );
      Function_Label.TabIndex = 1;
      Function_Label.Text = "Function:";
      //
      // Function_Combo
      //
      Function_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Function_Combo.Location = new Point ( 90, 47 );
      Function_Combo.Name = "Function_Combo";
      Function_Combo.Size = new Size ( 140, 23 );
      Function_Combo.TabIndex = 2;
      //
      // Readings_Label
      //
      Readings_Label.AutoSize = true;
      Readings_Label.Location = new Point ( 12, 80 );
      Readings_Label.Name = "Readings_Label";
      Readings_Label.Size = new Size ( 119, 15 );
      Readings_Label.TabIndex = 3;
      Readings_Label.Text = "Number of Readings:";
      //
      // Readings_Numeric
      //
      Readings_Numeric.Location = new Point ( 160, 78 );
      Readings_Numeric.Maximum = new decimal ( new int [ ] { 1000, 0, 0, 0 } );
      Readings_Numeric.Minimum = new decimal ( new int [ ] { 1, 0, 0, 0 } );
      Readings_Numeric.Name = "Readings_Numeric";
      Readings_Numeric.Size = new Size ( 80, 23 );
      Readings_Numeric.TabIndex = 4;
      Readings_Numeric.Value = new decimal ( new int [ ] { 20, 0, 0, 0 } );
      //
      // Continuous_Check
      //
      Continuous_Check.AutoSize = true;
      Continuous_Check.Location = new Point ( 250, 80 );
      Continuous_Check.Name = "Continuous_Check";
      Continuous_Check.Size = new Size ( 88, 19 );
      Continuous_Check.TabIndex = 5;
      Continuous_Check.Text = "Continuous";
      Continuous_Check.CheckedChanged +=  Continuous_Check_Changed ;
      //
      // Delay_Label
      //
      Delay_Label.AutoSize = true;
      Delay_Label.Location = new Point ( 350, 80 );
      Delay_Label.Name = "Delay_Label";
      Delay_Label.Size = new Size ( 66, 15 );
      Delay_Label.TabIndex = 6;
      Delay_Label.Text = "Delay (ms):";
      //
      // Delay_Numeric
      //
      Delay_Numeric.Increment = new decimal ( new int [ ] { 50, 0, 0, 0 } );
      Delay_Numeric.Location = new Point ( 430, 78 );
      Delay_Numeric.Maximum = new decimal ( new int [ ] { 10000, 0, 0, 0 } );
      Delay_Numeric.Minimum = new decimal ( new int [ ] { 50, 0, 0, 0 } );
      Delay_Numeric.Name = "Delay_Numeric";
      Delay_Numeric.Size = new Size ( 80, 23 );
      Delay_Numeric.TabIndex = 7;
      Delay_Numeric.Value = new decimal ( new int [ ] { 500, 0, 0, 0 } );
      //
      // Start_Stop_Button
      //
      Start_Stop_Button.Location = new Point ( 12, 148 );
      Start_Stop_Button.Name = "Start_Stop_Button";
      Start_Stop_Button.Size = new Size ( 85, 28 );
      Start_Stop_Button.TabIndex = 8;
      Start_Stop_Button.Text = "Start";
      Start_Stop_Button.UseVisualStyleBackColor = true;
      Start_Stop_Button.Click +=  Start_Stop_Button_Click ;
      //
      // Status_Label
      //
      Status_Label.AutoSize = true;
      Status_Label.Font = new Font ( "Segoe UI", 9F, FontStyle.Bold );
      Status_Label.ForeColor = Color.Gray;
      Status_Label.Location = new Point ( 582, 155 );
      Status_Label.Name = "Status_Label";
      Status_Label.Size = new Size ( 28, 15 );
      Status_Label.TabIndex = 10;
      Status_Label.Text = "Idle";
      //
      // Current_Value_Label
      //
      Current_Value_Label.AutoSize = true;
      Current_Value_Label.Font = new Font ( "Consolas", 14F, FontStyle.Bold );
      Current_Value_Label.ForeColor = Color.DarkBlue;
      Current_Value_Label.Location = new Point ( 12, 112 );
      Current_Value_Label.Name = "Current_Value_Label";
      Current_Value_Label.Size = new Size ( 40, 22 );
      Current_Value_Label.TabIndex = 11;
      Current_Value_Label.Text = "---";
      //
      // Progress_Label
      //
      Progress_Label.AutoSize = true;
      Progress_Label.Location = new Point ( 300, 118 );
      Progress_Label.Name = "Progress_Label";
      Progress_Label.Size = new Size ( 0, 15 );
      Progress_Label.TabIndex = 12;
      //
      // Graph_Style_Label
      //
      Graph_Style_Label.AutoSize = true;
      Graph_Style_Label.Location = new Point ( 582, 83 );
      Graph_Style_Label.Name = "Graph_Style_Label";
      Graph_Style_Label.Size = new Size ( 42, 15 );
      Graph_Style_Label.TabIndex = 13;
      Graph_Style_Label.Text = "Graph:";
      //
      // Graph_Style_Combo
      //
      Graph_Style_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Graph_Style_Combo.Location = new Point ( 632, 80 );
      Graph_Style_Combo.Name = "Graph_Style_Combo";
      Graph_Style_Combo.Size = new Size ( 140, 23 );
      Graph_Style_Combo.TabIndex = 14;
      Graph_Style_Combo.SelectedIndexChanged +=  Graph_Style_Combo_Changed ;
      //
      // Chart_Panel
      //
      Chart_Panel.Anchor =     AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Chart_Panel.BackColor = Color.FromArgb (   24,   27,   31 );
      Chart_Panel.BorderStyle = BorderStyle.FixedSingle;
      Chart_Panel.Location = new Point ( 12, 182 );
      Chart_Panel.Name = "Chart_Panel";
      Chart_Panel.Size = new Size ( 760, 303 );
      Chart_Panel.TabIndex = 17;
      Chart_Panel.Paint +=  Chart_Panel_Paint ;
      Chart_Panel.Resize +=  Chart_Panel_Resize ;
      //
      // Clear_Button
      //
      Clear_Button.Location = new Point ( 103, 148 );
      Clear_Button.Name = "Clear_Button";
      Clear_Button.Size = new Size ( 65, 28 );
      Clear_Button.TabIndex = 9;
      Clear_Button.Text = "Clear";
      Clear_Button.UseVisualStyleBackColor = true;
      Clear_Button.Click +=  Clear_Button_Click ;
      //
      // Record_Button
      //
      Record_Button.Location = new Point ( 174, 148 );
      Record_Button.Name = "Record_Button";
      Record_Button.Size = new Size ( 85, 28 );
      Record_Button.TabIndex = 15;
      Record_Button.Text = "Record";
      Record_Button.UseVisualStyleBackColor = true;
      Record_Button.Click +=  Record_Button_Click ;
      //
      // Load_Button
      //
      Load_Button.Location = new Point ( 265, 148 );
      Load_Button.Name = "Load_Button";
      Load_Button.Size = new Size ( 65, 28 );
      Load_Button.TabIndex = 16;
      Load_Button.Text = "Load";
      Load_Button.UseVisualStyleBackColor = true;
      Load_Button.Click +=  Load_Button_Click ;
      //
      // Theme_Button
      //
      Theme_Button.Location = new Point ( 336, 148 );
      Theme_Button.Name = "Theme_Button";
      Theme_Button.Size = new Size ( 65, 28 );
      Theme_Button.TabIndex = 18;
      Theme_Button.Text = "Theme";
      Theme_Button.UseVisualStyleBackColor = true;
      Theme_Button.Click += Theme_Button_Click;
      //
      // Voltage_Reader_Form
      //
      AutoScaleDimensions = new SizeF ( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size ( 800, 500 );
      Controls.Add ( Title_Label );
      Controls.Add ( Function_Label );
      Controls.Add ( Function_Combo );
      Controls.Add ( Readings_Label );
      Controls.Add ( Readings_Numeric );
      Controls.Add ( Continuous_Check );
      Controls.Add ( Delay_Label );
      Controls.Add ( Delay_Numeric );
      Controls.Add ( Start_Stop_Button );
      Controls.Add ( Clear_Button );
      Controls.Add ( Status_Label );
      Controls.Add ( Current_Value_Label );
      Controls.Add ( Progress_Label );
      Controls.Add ( Graph_Style_Label );
      Controls.Add ( Graph_Style_Combo );
      Controls.Add ( Record_Button );
      Controls.Add ( Load_Button );
      Controls.Add ( Theme_Button );
      Controls.Add ( Chart_Panel );
      MinimumSize = new Size ( 600, 430 );
      Name = "Voltage_Reader_Form";
      StartPosition = FormStartPosition.CenterParent;
      Text = "Voltage Reader";
      ( (System.ComponentModel.ISupportInitialize) Readings_Numeric ).EndInit ( );
      ( (System.ComponentModel.ISupportInitialize) Delay_Numeric ).EndInit ( );
      ResumeLayout ( false );
      PerformLayout ( );
    }

    #endregion

    private System.Windows.Forms.Label Title_Label;
    private System.Windows.Forms.Label Function_Label;
    private System.Windows.Forms.ComboBox Function_Combo;
    private System.Windows.Forms.Label Readings_Label;
    private System.Windows.Forms.NumericUpDown Readings_Numeric;
    private System.Windows.Forms.Label Delay_Label;
    private System.Windows.Forms.NumericUpDown Delay_Numeric;
    private System.Windows.Forms.CheckBox Continuous_Check;
    private System.Windows.Forms.Button Start_Stop_Button;
    private System.Windows.Forms.Button Clear_Button;
    private System.Windows.Forms.Label Status_Label;
    private System.Windows.Forms.Label Current_Value_Label;
    private System.Windows.Forms.Label Progress_Label;
    private System.Windows.Forms.Label Graph_Style_Label;
    private System.Windows.Forms.ComboBox Graph_Style_Combo;
    private System.Windows.Forms.Panel Chart_Panel;
    private System.Windows.Forms.Button Record_Button;
    private System.Windows.Forms.Button Load_Button;
    private System.Windows.Forms.Button Theme_Button;
  }
}
