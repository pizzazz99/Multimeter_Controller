namespace Multimeter_Controller
{
  partial class Multi_Instrument_Poll_Form
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
      Delay_Label = new Label ( );
      Delay_Numeric = new NumericUpDown ( );
      Start_Stop_Button = new Button ( );
      Clear_Button = new Button ( );
      Record_Button = new Button ( );
      Load_Button = new Button ( );
      Chart_Panel = new Panel ( );
      Continuous_Check = new CheckBox ( );
      Measurement_Combo = new ComboBox ( );
      Measurement_Label = new Label ( );
      NPLC_Label = new Label ( );
      Cycles_Label = new Label ( );
      Cycles_Numeric = new NumericUpDown ( );
      Rolling_Check = new CheckBox ( );
      Max_Points_Numeric = new NumericUpDown ( );
      Legend_Toggle_Button = new Button ( );
      Cycle_Label = new Label ( );
      Cycle_Text_Box = new TextBox ( );
      View_Mode_Button = new Button ( );
      Normalize_Button = new Button ( );
      Progress_Text_Box = new TextBox ( );
      Zoom_Slider = new TrackBar ( );
      Pan_Scrollbar = new HScrollBar ( );
      Auto_Scroll_Panel = new Panel ( );
      Auto_Scroll_Check = new CheckBox ( );
      Reset_Errors_Button = new Button ( );
      Close_Button = new Button ( );
      NPLC_Textbox = new TextBox ( );
      Current_Values_Panel = new Buffered_Panel ( );
      Graph_Style_Label = new Label ( );
      Graph_Style_Combo = new ComboBox ( );
      NPLC_Delay_Textbox = new TextBox ( );
      label1 = new Label ( );
      Theme_Button = new Button ( );
      Poll_Speed_Button = new Button ( );
      ( (System.ComponentModel.ISupportInitialize) Delay_Numeric ).BeginInit ( );
      ( (System.ComponentModel.ISupportInitialize) Cycles_Numeric ).BeginInit ( );
      ( (System.ComponentModel.ISupportInitialize) Max_Points_Numeric ).BeginInit ( );
      ( (System.ComponentModel.ISupportInitialize) Zoom_Slider ).BeginInit ( );
      Auto_Scroll_Panel.SuspendLayout ( );
      SuspendLayout ( );
      // 
      // Title_Label
      // 
      Title_Label.AutoSize = true;
      Title_Label.Font = new Font ( "Segoe UI", 12F, FontStyle.Bold );
      Title_Label.Location = new Point ( 12, 9 );
      Title_Label.Name = "Title_Label";
      Title_Label.Size = new Size ( 190, 21 );
      Title_Label.TabIndex = 0;
      Title_Label.Text = "Multi-Instrument Poller";
      // 
      // Delay_Label
      // 
      Delay_Label.AutoSize = true;
      Delay_Label.Location = new Point ( 694, 258 );
      Delay_Label.Name = "Delay_Label";
      Delay_Label.Size = new Size ( 66, 15 );
      Delay_Label.TabIndex = 3;
      Delay_Label.Text = "Delay (ms):";
      // 
      // Delay_Numeric
      // 
      Delay_Numeric.Increment = new decimal ( new int [ ] { 50, 0, 0, 0 } );
      Delay_Numeric.Location = new Point ( 766, 255 );
      Delay_Numeric.Maximum = new decimal ( new int [ ] { 60000, 0, 0, 0 } );
      Delay_Numeric.Minimum = new decimal ( new int [ ] { 50, 0, 0, 0 } );
      Delay_Numeric.Name = "Delay_Numeric";
      Delay_Numeric.Size = new Size ( 80, 23 );
      Delay_Numeric.TabIndex = 4;
      Delay_Numeric.Value = new decimal ( new int [ ] { 500, 0, 0, 0 } );
      Delay_Numeric.ValueChanged +=  Delay_Numeric_ValueChanged ;
      // 
      // Start_Stop_Button
      // 
      Start_Stop_Button.Location = new Point ( 16, 251 );
      Start_Stop_Button.Name = "Start_Stop_Button";
      Start_Stop_Button.Size = new Size ( 85, 28 );
      Start_Stop_Button.TabIndex = 5;
      Start_Stop_Button.Text = "Start";
      Start_Stop_Button.UseVisualStyleBackColor = true;
      Start_Stop_Button.Click +=  Start_Stop_Button_Click ;
      // 
      // Clear_Button
      // 
      Clear_Button.Location = new Point ( 103, 251 );
      Clear_Button.Name = "Clear_Button";
      Clear_Button.Size = new Size ( 65, 28 );
      Clear_Button.TabIndex = 6;
      Clear_Button.Text = "Clear";
      Clear_Button.UseVisualStyleBackColor = true;
      Clear_Button.Click +=  Clear_Button_Click ;
      // 
      // Record_Button
      // 
      Record_Button.Location = new Point ( 170, 251 );
      Record_Button.Name = "Record_Button";
      Record_Button.Size = new Size ( 85, 28 );
      Record_Button.TabIndex = 7;
      Record_Button.Text = "Record";
      Record_Button.UseVisualStyleBackColor = true;
      Record_Button.Click +=  Record_Button_Click ;
      // 
      // Load_Button
      // 
      Load_Button.Location = new Point ( 256, 252 );
      Load_Button.Name = "Load_Button";
      Load_Button.Size = new Size ( 65, 28 );
      Load_Button.TabIndex = 8;
      Load_Button.Text = "Load";
      Load_Button.UseVisualStyleBackColor = true;
      Load_Button.Click +=  Load_Button_Click ;
      // 
      // Chart_Panel
      // 
      Chart_Panel.Anchor =     AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Chart_Panel.BackColor = Color.FromArgb (   24,   27,   31 );
      Chart_Panel.BorderStyle = BorderStyle.FixedSingle;
      Chart_Panel.Location = new Point ( 12, 287 );
      Chart_Panel.Name = "Chart_Panel";
      Chart_Panel.Size = new Size ( 919, 474 );
      Chart_Panel.TabIndex = 12;
      Chart_Panel.Paint +=  Chart_Panel_Paint ;
      Chart_Panel.MouseWheel +=  Chart_Panel_Mouse_Wheel ;
      Chart_Panel.Resize +=  Chart_Panel_Resize ;
      // 
      // Continuous_Check
      // 
      Continuous_Check.AutoSize = true;
      Continuous_Check.Checked = true;
      Continuous_Check.CheckState = CheckState.Checked;
      Continuous_Check.Location = new Point ( 13, 217 );
      Continuous_Check.Name = "Continuous_Check";
      Continuous_Check.Size = new Size ( 111, 19 );
      Continuous_Check.TabIndex = 16;
      Continuous_Check.Text = "Continuous Poll";
      Continuous_Check.UseVisualStyleBackColor = true;
      Continuous_Check.CheckedChanged +=  Continuous_Check_Changed ;
      // 
      // Measurement_Combo
      // 
      Measurement_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Measurement_Combo.FormattingEnabled = true;
      Measurement_Combo.Location = new Point ( 270, 155 );
      Measurement_Combo.Name = "Measurement_Combo";
      Measurement_Combo.Size = new Size ( 150, 23 );
      Measurement_Combo.TabIndex = 15;
      Measurement_Combo.SelectedIndexChanged +=  Measurement_Combo_Changed ;
      // 
      // Measurement_Label
      // 
      Measurement_Label.AutoSize = true;
      Measurement_Label.Location = new Point ( 180, 158 );
      Measurement_Label.Name = "Measurement_Label";
      Measurement_Label.Size = new Size ( 83, 15 );
      Measurement_Label.TabIndex = 14;
      Measurement_Label.Text = "Measurement:";
      // 
      // NPLC_Label
      // 
      NPLC_Label.AutoSize = true;
      NPLC_Label.Location = new Point ( 8, 155 );
      NPLC_Label.Name = "NPLC_Label";
      NPLC_Label.Size = new Size ( 40, 15 );
      NPLC_Label.TabIndex = 17;
      NPLC_Label.Text = "NPLC:";
      // 
      // Cycles_Label
      // 
      Cycles_Label.AutoSize = true;
      Cycles_Label.Enabled = false;
      Cycles_Label.Location = new Point ( 136, 219 );
      Cycles_Label.Name = "Cycles_Label";
      Cycles_Label.Size = new Size ( 44, 15 );
      Cycles_Label.TabIndex = 19;
      Cycles_Label.Text = "Cycles:";
      // 
      // Cycles_Numeric
      // 
      Cycles_Numeric.Enabled = false;
      Cycles_Numeric.Location = new Point ( 186, 216 );
      Cycles_Numeric.Maximum = new decimal ( new int [ ] { 100000, 0, 0, 0 } );
      Cycles_Numeric.Minimum = new decimal ( new int [ ] { 1, 0, 0, 0 } );
      Cycles_Numeric.Name = "Cycles_Numeric";
      Cycles_Numeric.Size = new Size ( 70, 23 );
      Cycles_Numeric.TabIndex = 20;
      Cycles_Numeric.Value = new decimal ( new int [ ] { 10, 0, 0, 0 } );
      // 
      // Rolling_Check
      // 
      Rolling_Check.AutoSize = true;
      Rolling_Check.Location = new Point ( 510, 246 );
      Rolling_Check.Name = "Rolling_Check";
      Rolling_Check.Size = new Size ( 79, 34 );
      Rolling_Check.TabIndex = 24;
      Rolling_Check.Text = "Show Last\r\n'N' Points";
      Rolling_Check.UseVisualStyleBackColor = true;
      Rolling_Check.CheckedChanged +=  Rolling_Check_CheckedChanged ;
      Rolling_Check.Click +=  Rolling_Check_CheckedChanged ;
      // 
      // Max_Points_Numeric
      // 
      Max_Points_Numeric.Enabled = false;
      Max_Points_Numeric.Increment = new decimal ( new int [ ] { 2, 0, 0, 0 } );
      Max_Points_Numeric.Location = new Point ( 597, 251 );
      Max_Points_Numeric.Maximum = new decimal ( new int [ ] { 100000, 0, 0, 0 } );
      Max_Points_Numeric.Minimum = new decimal ( new int [ ] { 5, 0, 0, 0 } );
      Max_Points_Numeric.Name = "Max_Points_Numeric";
      Max_Points_Numeric.Size = new Size ( 80, 23 );
      Max_Points_Numeric.TabIndex = 21;
      Max_Points_Numeric.Value = new decimal ( new int [ ] { 5, 0, 0, 0 } );
      Max_Points_Numeric.ValueChanged +=  Max_Points_Numeric_ValueChanged ;
      // 
      // Legend_Toggle_Button
      // 
      Legend_Toggle_Button.Location = new Point ( 858, 251 );
      Legend_Toggle_Button.Name = "Legend_Toggle_Button";
      Legend_Toggle_Button.Size = new Size ( 65, 28 );
      Legend_Toggle_Button.TabIndex = 26;
      Legend_Toggle_Button.Text = "Stats";
      Legend_Toggle_Button.UseVisualStyleBackColor = true;
      Legend_Toggle_Button.Click +=  Legend_Toggle_Button_Click ;
      // 
      // Cycle_Label
      // 
      Cycle_Label.AutoSize = true;
      Cycle_Label.Location = new Point ( 537, 189 );
      Cycle_Label.Name = "Cycle_Label";
      Cycle_Label.Size = new Size ( 44, 15 );
      Cycle_Label.TabIndex = 27;
      Cycle_Label.Text = "Cycles:";
      // 
      // Cycle_Text_Box
      // 
      Cycle_Text_Box.Location = new Point ( 623, 186 );
      Cycle_Text_Box.Name = "Cycle_Text_Box";
      Cycle_Text_Box.Size = new Size ( 300, 23 );
      Cycle_Text_Box.TabIndex = 28;
      // 
      // View_Mode_Button
      // 
      View_Mode_Button.Location = new Point ( 270, 215 );
      View_Mode_Button.Name = "View_Mode_Button";
      View_Mode_Button.Size = new Size ( 92, 23 );
      View_Mode_Button.TabIndex = 29;
      View_Mode_Button.Text = "Combined View";
      View_Mode_Button.UseVisualStyleBackColor = true;
      View_Mode_Button.Click +=  View_Mode_Button_Click ;
      // 
      // Normalize_Button
      // 
      Normalize_Button.Location = new Point ( 368, 215 );
      Normalize_Button.Name = "Normalize_Button";
      Normalize_Button.Size = new Size ( 75, 23 );
      Normalize_Button.TabIndex = 30;
      Normalize_Button.Text = "Normalize";
      Normalize_Button.UseVisualStyleBackColor = true;
      Normalize_Button.Click +=  Normalize_Button_Click ;
      // 
      // Progress_Text_Box
      // 
      Progress_Text_Box.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Progress_Text_Box.BackColor = SystemColors.Control;
      Progress_Text_Box.BorderStyle = BorderStyle.None;
      Progress_Text_Box.Location = new Point ( 13, 767 );
      Progress_Text_Box.Name = "Progress_Text_Box";
      Progress_Text_Box.ReadOnly = true;
      Progress_Text_Box.Size = new Size ( 915, 16 );
      Progress_Text_Box.TabIndex = 31;
      // 
      // Zoom_Slider
      // 
      Zoom_Slider.Location = new Point ( 12, 101 );
      Zoom_Slider.Maximum = 100;
      Zoom_Slider.Minimum = 1;
      Zoom_Slider.Name = "Zoom_Slider";
      Zoom_Slider.Size = new Size ( 150, 45 );
      Zoom_Slider.TabIndex = 32;
      Zoom_Slider.TickFrequency = 10;
      Zoom_Slider.Value = 50;
      Zoom_Slider.Scroll +=  Zoom_Slider_Scroll ;
      Zoom_Slider.ValueChanged +=  Zoom_Slider_ValueChanged ;
      // 
      // Pan_Scrollbar
      // 
      Pan_Scrollbar.Dock = DockStyle.Bottom;
      Pan_Scrollbar.Enabled = false;
      Pan_Scrollbar.Location = new Point ( 0, 813 );
      Pan_Scrollbar.Maximum = 109;
      Pan_Scrollbar.Name = "Pan_Scrollbar";
      Pan_Scrollbar.Size = new Size ( 943, 17 );
      Pan_Scrollbar.TabIndex = 33;
      Pan_Scrollbar.Scroll +=  Pan_Scrollbar_Scroll ;
      Pan_Scrollbar.ValueChanged +=  Pan_Scrollbar_ValueChanged ;
      // 
      // Auto_Scroll_Panel
      // 
      Auto_Scroll_Panel.Controls.Add ( Auto_Scroll_Check );
      Auto_Scroll_Panel.Dock = DockStyle.Bottom;
      Auto_Scroll_Panel.Location = new Point ( 0, 788 );
      Auto_Scroll_Panel.Name = "Auto_Scroll_Panel";
      Auto_Scroll_Panel.Size = new Size ( 943, 25 );
      Auto_Scroll_Panel.TabIndex = 34;
      // 
      // Auto_Scroll_Check
      // 
      Auto_Scroll_Check.AutoSize = true;
      Auto_Scroll_Check.Checked = true;
      Auto_Scroll_Check.CheckState = CheckState.Checked;
      Auto_Scroll_Check.Location = new Point ( 10, 3 );
      Auto_Scroll_Check.Name = "Auto_Scroll_Check";
      Auto_Scroll_Check.Size = new Size ( 130, 19 );
      Auto_Scroll_Check.TabIndex = 0;
      Auto_Scroll_Check.Text = "Auto-scroll to latest";
      Auto_Scroll_Check.UseVisualStyleBackColor = true;
      Auto_Scroll_Check.CheckedChanged +=  Auto_Scroll_Check_CheckedChanged ;
      // 
      // Reset_Errors_Button
      // 
      Reset_Errors_Button.Location = new Point ( 327, 252 );
      Reset_Errors_Button.Name = "Reset_Errors_Button";
      Reset_Errors_Button.Size = new Size ( 90, 29 );
      Reset_Errors_Button.TabIndex = 35;
      Reset_Errors_Button.Text = "Reset Errors";
      Reset_Errors_Button.UseVisualStyleBackColor = true;
      Reset_Errors_Button.Click +=  Reset_Errors_Button_Click ;
      // 
      // Close_Button
      // 
      Close_Button.Location = new Point ( 423, 251 );
      Close_Button.Name = "Close_Button";
      Close_Button.Size = new Size ( 75, 30 );
      Close_Button.TabIndex = 36;
      Close_Button.Text = "Close";
      Close_Button.UseVisualStyleBackColor = true;
      Close_Button.Click +=  Close_Button_Click ;
      // 
      // NPLC_Textbox
      // 
      NPLC_Textbox.Location = new Point ( 86, 152 );
      NPLC_Textbox.Name = "NPLC_Textbox";
      NPLC_Textbox.Size = new Size ( 70, 23 );
      NPLC_Textbox.TabIndex = 37;
      // 
      // Current_Values_Panel
      // 
      Current_Values_Panel.Anchor =   AnchorStyles.Top  |  AnchorStyles.Right ;
      Current_Values_Panel.BackColor = Color.FromArgb (   24,   27,   31 );
      Current_Values_Panel.BorderStyle = BorderStyle.FixedSingle;
      Current_Values_Panel.Location = new Point ( 623, 70 );
      Current_Values_Panel.Name = "Current_Values_Panel";
      Current_Values_Panel.Size = new Size ( 300, 100 );
      Current_Values_Panel.TabIndex = 38;
      // 
      // Graph_Style_Label
      // 
      Graph_Style_Label.AutoSize = true;
      Graph_Style_Label.Location = new Point ( 535, 214 );
      Graph_Style_Label.Name = "Graph_Style_Label";
      Graph_Style_Label.Size = new Size ( 42, 15 );
      Graph_Style_Label.TabIndex = 39;
      Graph_Style_Label.Text = "Graph:";
      // 
      // Graph_Style_Combo
      // 
      Graph_Style_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Graph_Style_Combo.Location = new Point ( 623, 215 );
      Graph_Style_Combo.Name = "Graph_Style_Combo";
      Graph_Style_Combo.Size = new Size ( 214, 23 );
      Graph_Style_Combo.TabIndex = 40;
      Graph_Style_Combo.SelectedIndexChanged +=  Graph_Style_Combo_SelectedIndexChanged ;
      // 
      // NPLC_Delay_Textbox
      // 
      NPLC_Delay_Textbox.Location = new Point ( 86, 185 );
      NPLC_Delay_Textbox.Name = "NPLC_Delay_Textbox";
      NPLC_Delay_Textbox.Size = new Size ( 70, 23 );
      NPLC_Delay_Textbox.TabIndex = 42;
      NPLC_Delay_Textbox.TextChanged +=  NPLC_Delay_Textbox_TextChanged ;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point ( 8, 188 );
      label1.Name = "label1";
      label1.Size = new Size ( 72, 15 );
      label1.TabIndex = 41;
      label1.Text = "NPLC Delay:";
      // 
      // Theme_Button
      // 
      Theme_Button.Location = new Point ( 181, 185 );
      Theme_Button.Name = "Theme_Button";
      Theme_Button.Size = new Size ( 75, 23 );
      Theme_Button.TabIndex = 43;
      Theme_Button.Text = "Theme";
      Theme_Button.UseVisualStyleBackColor = true;
      Theme_Button.Click +=  Theme_Button_Click ;
      // 
      // Poll_Speed_Button
      // 
      Poll_Speed_Button.Location = new Point ( 270, 186 );
      Poll_Speed_Button.Name = "Poll_Speed_Button";
      Poll_Speed_Button.Size = new Size ( 92, 23 );
      Poll_Speed_Button.TabIndex = 44;
      Poll_Speed_Button.Text = "Poll Speed";
      Poll_Speed_Button.UseVisualStyleBackColor = true;
      Poll_Speed_Button.Click +=  Poll_Speed_Button_Click ;
      // 
      // Multi_Instrument_Poll_Form
      // 
      AutoScaleDimensions = new SizeF ( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size ( 943, 830 );
      Controls.Add ( Poll_Speed_Button );
      Controls.Add ( Theme_Button );
      Controls.Add ( NPLC_Delay_Textbox );
      Controls.Add ( label1 );
      Controls.Add ( Graph_Style_Label );
      Controls.Add ( Graph_Style_Combo );
      Controls.Add ( NPLC_Textbox );
      Controls.Add ( Close_Button );
      Controls.Add ( Auto_Scroll_Panel );
      Controls.Add ( Pan_Scrollbar );
      Controls.Add ( Progress_Text_Box );
      Controls.Add ( Chart_Panel );
      Controls.Add ( Zoom_Slider );
      Controls.Add ( Reset_Errors_Button );
      Controls.Add ( Normalize_Button );
      Controls.Add ( View_Mode_Button );
      Controls.Add ( Cycle_Text_Box );
      Controls.Add ( Cycle_Label );
      Controls.Add ( Legend_Toggle_Button );
      Controls.Add ( Rolling_Check );
      Controls.Add ( Max_Points_Numeric );
      Controls.Add ( Title_Label );
      Controls.Add ( NPLC_Label );
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
      Controls.Add ( Current_Values_Panel );
      MinimumSize = new Size ( 600, 400 );
      Name = "Multi_Instrument_Poll_Form";
      StartPosition = FormStartPosition.CenterParent;
      Text = "Multi-Instrument Poll Form";
      ( (System.ComponentModel.ISupportInitialize) Delay_Numeric ).EndInit ( );
      ( (System.ComponentModel.ISupportInitialize) Cycles_Numeric ).EndInit ( );
      ( (System.ComponentModel.ISupportInitialize) Max_Points_Numeric ).EndInit ( );
      ( (System.ComponentModel.ISupportInitialize) Zoom_Slider ).EndInit ( );
      Auto_Scroll_Panel.ResumeLayout ( false );
      Auto_Scroll_Panel.PerformLayout ( );
      ResumeLayout ( false );
      PerformLayout ( );
    }






    #endregion

    private System.Windows.Forms.Label Title_Label;
    private System.Windows.Forms.Label NPLC_Label;
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
    private System.Windows.Forms.Panel Chart_Panel;
    private CheckBox Rolling_Check;
    private CheckBox Auto_Scroll_Check;
    private Panel Auto_Scroll_Panel;
    private NumericUpDown Max_Points_Numeric;
    private Button Legend_Toggle_Button;
    private Label Cycle_Label;
    private TextBox Cycle_Text_Box;
    private Button View_Mode_Button;
    private Button Normalize_Button;
    private TextBox Progress_Text_Box;
    private TrackBar Zoom_Slider;
    private Button Reset_Errors_Button;
    private HScrollBar Pan_Scrollbar;
    private Button Close_Button;
    private TextBox NPLC_Textbox;

    private Buffered_Panel Current_Values_Panel;
    private Label Graph_Style_Label;
    private ComboBox Graph_Style_Combo;
    private TextBox NPLC_Delay_Textbox;
    private Label label1;
    private Button Theme_Button;
    private Button Poll_Speed_Button;
  }
}
