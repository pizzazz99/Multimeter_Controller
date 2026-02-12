namespace Multimeter_Controller
{
  partial class Multi_Poll_Form
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
      Query_Label = new Label ( );
      Query_Text = new TextBox ( );
      Delay_Label = new Label ( );
      Delay_Numeric = new NumericUpDown ( );
      Start_Stop_Button = new Button ( );
      Clear_Button = new Button ( );
      Record_Button = new Button ( );
      Load_Button = new Button ( );
      Theme_Button = new Button ( );
      Status_Label = new Label ( );
      Progress_Label = new Label ( );
      Cycle_Label = new Label ( );
      Chart_Panel = new Panel ( );
      Continuous_Check = new CheckBox ( );
      Measurement_Combo = new ComboBox ( );
      Measurement_Label = new Label ( );
      NPLC_Label = new Label ( );
      NPLC_Combo = new ComboBox ( );
      Cycles_Label = new Label ( );
      Cycles_Numeric = new NumericUpDown ( );
      ( (System.ComponentModel.ISupportInitialize) Delay_Numeric ).BeginInit ( );
      ( (System.ComponentModel.ISupportInitialize) Cycles_Numeric ).BeginInit ( );
      SuspendLayout ( );
      //
      // Title_Label
      //
      Title_Label.AutoSize = true;
      Title_Label.Font = new Font ( "Segoe UI", 12F, FontStyle.Bold );
      Title_Label.Location = new Point ( 12, 12 );
      Title_Label.Name = "Title_Label";
      Title_Label.Size = new Size ( 190, 21 );
      Title_Label.TabIndex = 0;
      Title_Label.Text = "Multi-Instrument Poller";
      //
      // Query_Label
      //
      Query_Label.AutoSize = true;
      Query_Label.Location = new Point ( 145, 48 );
      Query_Label.Name = "Query_Label";
      Query_Label.Size = new Size ( 42, 15 );
      Query_Label.TabIndex = 1;
      Query_Label.Text = "Query:";
      //
      // Query_Text
      //
      Query_Text.Font = new Font ( "Consolas", 10F );
      Query_Text.Location = new Point ( 190, 45 );
      Query_Text.Name = "Query_Text";
      Query_Text.Size = new Size ( 160, 23 );
      Query_Text.TabIndex = 2;
      Query_Text.Text = "TEMP?";
      //
      // Delay_Label
      //
      Delay_Label.AutoSize = true;
      Delay_Label.Location = new Point ( 370, 48 );
      Delay_Label.Name = "Delay_Label";
      Delay_Label.Size = new Size ( 66, 15 );
      Delay_Label.TabIndex = 3;
      Delay_Label.Text = "Delay (ms):";
      //
      // Delay_Numeric
      //
      Delay_Numeric.Increment = new decimal ( new int [ ] { 50, 0, 0, 0 } );
      Delay_Numeric.Location = new Point ( 445, 45 );
      Delay_Numeric.Maximum = new decimal ( new int [ ] { 10000, 0, 0, 0 } );
      Delay_Numeric.Minimum = new decimal ( new int [ ] { 50, 0, 0, 0 } );
      Delay_Numeric.Name = "Delay_Numeric";
      Delay_Numeric.Size = new Size ( 80, 23 );
      Delay_Numeric.TabIndex = 4;
      Delay_Numeric.Value = new decimal ( new int [ ] { 1000, 0, 0, 0 } );
      //
      // Measurement_Label
      //
      Measurement_Label.AutoSize = true;
      Measurement_Label.Location = new Point ( 545, 48 );
      Measurement_Label.Name = "Measurement_Label";
      Measurement_Label.Size = new Size ( 81, 15 );
      Measurement_Label.TabIndex = 14;
      Measurement_Label.Text = "Measurement:";
      //
      // Measurement_Combo
      //
      Measurement_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Measurement_Combo.FormattingEnabled = true;
      Measurement_Combo.Location = new Point ( 635, 45 );
      Measurement_Combo.Name = "Measurement_Combo";
      Measurement_Combo.Size = new Size ( 150, 23 );
      Measurement_Combo.TabIndex = 15;
      Measurement_Combo.SelectedIndexChanged +=  Measurement_Combo_Changed ;
      //
      // Continuous_Check
      //
      Continuous_Check.AutoSize = true;
      Continuous_Check.Checked = true;
      Continuous_Check.CheckState = CheckState.Checked;
      Continuous_Check.Location = new Point ( 12, 78 );
      Continuous_Check.Name = "Continuous_Check";
      Continuous_Check.Size = new Size ( 115, 19 );
      Continuous_Check.TabIndex = 16;
      Continuous_Check.Text = "Continuous Poll";
      Continuous_Check.UseVisualStyleBackColor = true;
      Continuous_Check.CheckedChanged += Continuous_Check_Changed;
      //
      // Cycles_Label
      //
      Cycles_Label.AutoSize = true;
      Cycles_Label.Location = new Point ( 135, 80 );
      Cycles_Label.Name = "Cycles_Label";
      Cycles_Label.Size = new Size ( 44, 15 );
      Cycles_Label.TabIndex = 19;
      Cycles_Label.Text = "Cycles:";
      Cycles_Label.Enabled = false;
      //
      // Cycles_Numeric
      //
      Cycles_Numeric.Location = new Point ( 185, 77 );
      Cycles_Numeric.Maximum = new decimal ( new int [ ] { 100000, 0, 0, 0 } );
      Cycles_Numeric.Minimum = new decimal ( new int [ ] { 1, 0, 0, 0 } );
      Cycles_Numeric.Name = "Cycles_Numeric";
      Cycles_Numeric.Size = new Size ( 70, 23 );
      Cycles_Numeric.TabIndex = 20;
      Cycles_Numeric.Value = new decimal ( new int [ ] { 10, 0, 0, 0 } );
      Cycles_Numeric.Enabled = false;
      //
      // NPLC_Label
      //
      NPLC_Label.AutoSize = true;
      NPLC_Label.Location = new Point ( 12, 48 );
      NPLC_Label.Name = "NPLC_Label";
      NPLC_Label.Size = new Size ( 38, 15 );
      NPLC_Label.TabIndex = 17;
      NPLC_Label.Text = "NPLC:";
      //
      // NPLC_Combo
      //
      NPLC_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      NPLC_Combo.FormattingEnabled = true;
      NPLC_Combo.Items.AddRange ( new object [ ]
        { "0.02", "0.2", "1", "10", "100" } );
      NPLC_Combo.Location = new Point ( 60, 45 );
      NPLC_Combo.Name = "NPLC_Combo";
      NPLC_Combo.Size = new Size ( 70, 23 );
      NPLC_Combo.TabIndex = 18;
      //
      // Start_Stop_Button
      //
      Start_Stop_Button.Location = new Point ( 265, 78 );
      Start_Stop_Button.Name = "Start_Stop_Button";
      Start_Stop_Button.Size = new Size ( 85, 28 );
      Start_Stop_Button.TabIndex = 5;
      Start_Stop_Button.Text = "Start";
      Start_Stop_Button.UseVisualStyleBackColor = true;
      Start_Stop_Button.Click +=  Start_Stop_Button_Click ;
      //
      // Clear_Button
      //
      Clear_Button.Location = new Point ( 356, 78 );
      Clear_Button.Name = "Clear_Button";
      Clear_Button.Size = new Size ( 65, 28 );
      Clear_Button.TabIndex = 6;
      Clear_Button.Text = "Clear";
      Clear_Button.UseVisualStyleBackColor = true;
      Clear_Button.Click +=  Clear_Button_Click ;
      //
      // Record_Button
      //
      Record_Button.Location = new Point ( 427, 78 );
      Record_Button.Name = "Record_Button";
      Record_Button.Size = new Size ( 85, 28 );
      Record_Button.TabIndex = 7;
      Record_Button.Text = "Record";
      Record_Button.UseVisualStyleBackColor = true;
      Record_Button.Click +=  Record_Button_Click ;
      //
      // Load_Button
      //
      Load_Button.Location = new Point ( 518, 78 );
      Load_Button.Name = "Load_Button";
      Load_Button.Size = new Size ( 65, 28 );
      Load_Button.TabIndex = 8;
      Load_Button.Text = "Load";
      Load_Button.UseVisualStyleBackColor = true;
      Load_Button.Click +=  Load_Button_Click ;
      //
      // Theme_Button
      //
      Theme_Button.Location = new Point ( 807, 78 );
      Theme_Button.Name = "Theme_Button";
      Theme_Button.Size = new Size ( 65, 28 );
      Theme_Button.TabIndex = 13;
      Theme_Button.Text = "Theme";
      Theme_Button.UseVisualStyleBackColor = true;
      Theme_Button.Click +=  Theme_Button_Click ;
      //
      // Status_Label
      //
      Status_Label.AutoSize = true;
      Status_Label.Font = new Font ( "Segoe UI", 9F, FontStyle.Bold );
      Status_Label.ForeColor = Color.Gray;
      Status_Label.Location = new Point ( 12, 15 );
      Status_Label.Name = "Status_Label";
      Status_Label.Size = new Size ( 28, 15 );
      Status_Label.TabIndex = 9;
      Status_Label.Text = "Idle";
      //
      // Progress_Label
      //
      Progress_Label.AutoSize = true;
      Progress_Label.Location = new Point ( 420, 85 );
      Progress_Label.Name = "Progress_Label";
      Progress_Label.Size = new Size ( 0, 15 );
      Progress_Label.TabIndex = 10;
      //
      // Cycle_Label
      //
      Cycle_Label.AutoSize = true;
      Cycle_Label.Font = new Font ( "Consolas", 10F, FontStyle.Bold );
      Cycle_Label.ForeColor = Color.DarkBlue;
      Cycle_Label.Location = new Point ( 500, 48 );
      Cycle_Label.Name = "Cycle_Label";
      Cycle_Label.Size = new Size ( 0, 17 );
      Cycle_Label.TabIndex = 11;
      //
      // Chart_Panel
      //
      Chart_Panel.Anchor =     AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Chart_Panel.BackColor = Color.FromArgb (   24,   27,   31 );
      Chart_Panel.BorderStyle = BorderStyle.FixedSingle;
      Chart_Panel.Location = new Point ( 12, 112 );
      Chart_Panel.Name = "Chart_Panel";
      Chart_Panel.Size = new Size ( 860, 440 );
      Chart_Panel.TabIndex = 12;
      Chart_Panel.Paint +=  Chart_Panel_Paint ;
      Chart_Panel.Resize +=  Chart_Panel_Resize ;
      //
      // Multi_Poll_Form
      //
      AutoScaleDimensions = new SizeF ( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size ( 884, 561 );
      Controls.Add ( Title_Label );
      Controls.Add ( NPLC_Label );
      Controls.Add ( NPLC_Combo );
      Controls.Add ( Query_Label );
      Controls.Add ( Query_Text );
      Controls.Add ( Delay_Label );
      Controls.Add ( Delay_Numeric );
      Controls.Add ( Measurement_Label );
      Controls.Add ( Measurement_Combo );
      Controls.Add ( Continuous_Check );
      Controls.Add ( Cycles_Label );
      Controls.Add ( Cycles_Numeric );
      Controls.Add ( Start_Stop_Button );
      Controls.Add ( Clear_Button );
      Controls.Add ( Record_Button );
      Controls.Add ( Load_Button );
      Controls.Add ( Theme_Button );
      Controls.Add ( Status_Label );
      Controls.Add ( Progress_Label );
      Controls.Add ( Cycle_Label );
      Controls.Add ( Chart_Panel );
      MinimumSize = new Size ( 600, 400 );
      Name = "Multi_Poll_Form";
      StartPosition = FormStartPosition.CenterParent;
      Text = "Multi-Instrument Poller";
      ( (System.ComponentModel.ISupportInitialize) Delay_Numeric ).EndInit ( );
      ( (System.ComponentModel.ISupportInitialize) Cycles_Numeric ).EndInit ( );
      ResumeLayout ( false );
      PerformLayout ( );
    }

    #endregion

    private System.Windows.Forms.Label Title_Label;
    private System.Windows.Forms.Label NPLC_Label;
    private System.Windows.Forms.ComboBox NPLC_Combo;
    private System.Windows.Forms.Label Query_Label;
    private System.Windows.Forms.TextBox Query_Text;
    private System.Windows.Forms.Label Delay_Label;
    private System.Windows.Forms.NumericUpDown Delay_Numeric;
    private System.Windows.Forms.Label Measurement_Label;
    private System.Windows.Forms.ComboBox Measurement_Combo;
    private System.Windows.Forms.CheckBox Continuous_Check;
    private System.Windows.Forms.Label Cycles_Label;
    private System.Windows.Forms.NumericUpDown Cycles_Numeric;
    private System.Windows.Forms.Button Start_Stop_Button;
    private System.Windows.Forms.Button Clear_Button;
    private System.Windows.Forms.Button Record_Button;
    private System.Windows.Forms.Button Load_Button;
    private System.Windows.Forms.Label Status_Label;
    private System.Windows.Forms.Label Progress_Label;
    private System.Windows.Forms.Label Cycle_Label;
    private System.Windows.Forms.Button Theme_Button;
    private System.Windows.Forms.Panel Chart_Panel;
  }
}
