namespace Multimeter_Controller
{
   partial class Recording_Playback_Form
  {

 
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose ( bool disposing )
    {
      if ( disposing && ( components != null ) )
        components.Dispose ( );
      base.Dispose ( disposing );
    }



    #region Windows Form Designer generated code

    private void InitializeComponent ( )
    {
      Title_Label = new Label ( );
      Clear_Button = new Button ( );
      Load_Button = new Button ( );
      Chart_Panel = new Panel ( );
      Rolling_Check = new CheckBox ( );
      Max_Points_Numeric = new NumericUpDown ( );
      Legend_Toggle_Button = new Button ( );
      View_Mode_Button = new Button ( );
      Normalize_Button = new Button ( );
      Progress_Text_Box = new TextBox ( );
      Zoom_Slider = new TrackBar ( );
      Pan_Scrollbar = new HScrollBar ( );
      Auto_Scroll_Panel = new Panel ( );
      Auto_Scroll_Check = new CheckBox ( );
      Close_Button = new Button ( );
      Graph_Style_Label = new Label ( );
      Graph_Style_Combo = new ComboBox ( );
      Theme_Button = new Button ( );
      Poll_Speed_Button = new Button ( );
      label2 = new Label ( );
      Measurement_Label = new Label ( );
      Measurement_Combo = new ComboBox ( );
      label1 = new Label ( );
      label3 = new Label ( );
      label4 = new Label ( );
      Add_File_Checkbox = new CheckBox ( );
      Analyze_Instrument_Data_Button = new Button ( );
      Analyze_Poll_Timing_Button = new Button ( );
      ( (System.ComponentModel.ISupportInitialize) Max_Points_Numeric ).BeginInit ( );
      ( (System.ComponentModel.ISupportInitialize) Zoom_Slider ).BeginInit ( );
      Auto_Scroll_Panel.SuspendLayout ( );
      SuspendLayout ( );
      // 
      // Title_Label
      // 
      Title_Label.Location = new Point ( 13, 9 );
      Title_Label.Name = "Title_Label";
      Title_Label.Size = new Size ( 100, 23 );
      Title_Label.TabIndex = 46;
      // 
      // Clear_Button
      // 
      Clear_Button.Location = new Point ( 20, 154 );
      Clear_Button.Name = "Clear_Button";
      Clear_Button.Size = new Size ( 65, 28 );
      Clear_Button.TabIndex = 6;
      Clear_Button.Text = "Clear";
      Clear_Button.UseVisualStyleBackColor = true;
      Clear_Button.Click +=  Clear_Button_Click ;
      // 
      // Load_Button
      // 
      Load_Button.Location = new Point ( 13, 78 );
      Load_Button.Name = "Load_Button";
      Load_Button.Size = new Size ( 65, 28 );
      Load_Button.TabIndex = 8;
      Load_Button.Text = "Load File";
      Load_Button.UseVisualStyleBackColor = true;
      Load_Button.Click +=  Load_Button_Click ;
      // 
      // Chart_Panel
      // 
      Chart_Panel.Anchor =     AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Chart_Panel.BackColor = Color.FromArgb (   24,   27,   31 );
      Chart_Panel.BorderStyle = BorderStyle.FixedSingle;
      Chart_Panel.Location = new Point ( 12, 188 );
      Chart_Panel.Name = "Chart_Panel";
      Chart_Panel.Size = new Size ( 919, 482 );
      Chart_Panel.TabIndex = 12;
      Chart_Panel.Paint +=  Chart_Panel_Paint ;
      Chart_Panel.MouseWheel +=  Chart_Panel_Mouse_Wheel ;
      Chart_Panel.Resize +=  Chart_Panel_Resize ;
      // 
      // Rolling_Check
      // 
      Rolling_Check.AutoSize = true;
      Rolling_Check.Location = new Point ( 585, 143 );
      Rolling_Check.Name = "Rolling_Check";
      Rolling_Check.Size = new Size ( 79, 34 );
      Rolling_Check.TabIndex = 24;
      Rolling_Check.Text = "Show Last\r\n'N' Points";
      Rolling_Check.UseVisualStyleBackColor = true;
      Rolling_Check.CheckedChanged +=  Rolling_Check_CheckedChanged ;
      // 
      // Max_Points_Numeric
      // 
      Max_Points_Numeric.Enabled = false;
      Max_Points_Numeric.Increment = new decimal ( new int [ ] { 2, 0, 0, 0 } );
      Max_Points_Numeric.Location = new Point ( 672, 148 );
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
      Legend_Toggle_Button.Location = new Point ( 768, 138 );
      Legend_Toggle_Button.Name = "Legend_Toggle_Button";
      Legend_Toggle_Button.Size = new Size ( 65, 42 );
      Legend_Toggle_Button.TabIndex = 26;
      Legend_Toggle_Button.Text = "Show Stats";
      Legend_Toggle_Button.UseVisualStyleBackColor = true;
      Legend_Toggle_Button.Click +=  Legend_Toggle_Button_Click ;
      // 
      // View_Mode_Button
      // 
      View_Mode_Button.Location = new Point ( 166, 156 );
      View_Mode_Button.Name = "View_Mode_Button";
      View_Mode_Button.Size = new Size ( 92, 23 );
      View_Mode_Button.TabIndex = 29;
      View_Mode_Button.Text = "Combined View";
      View_Mode_Button.UseVisualStyleBackColor = true;
      View_Mode_Button.Click +=  View_Mode_Button_Click ;
      // 
      // Normalize_Button
      // 
      Normalize_Button.Location = new Point ( 264, 156 );
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
      Progress_Text_Box.Location = new Point ( 13, 668 );
      Progress_Text_Box.Name = "Progress_Text_Box";
      Progress_Text_Box.ReadOnly = true;
      Progress_Text_Box.Size = new Size ( 915, 16 );
      Progress_Text_Box.TabIndex = 31;
      // 
      // Zoom_Slider
      // 
      Zoom_Slider.Location = new Point ( 443, 98 );
      Zoom_Slider.Maximum = 100;
      Zoom_Slider.Minimum = 1;
      Zoom_Slider.Name = "Zoom_Slider";
      Zoom_Slider.Size = new Size ( 126, 45 );
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
      Pan_Scrollbar.Location = new Point ( 0, 714 );
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
      Auto_Scroll_Panel.Location = new Point ( 0, 689 );
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
      // Close_Button
      // 
      Close_Button.Location = new Point ( 857, 138 );
      Close_Button.Name = "Close_Button";
      Close_Button.Size = new Size ( 75, 41 );
      Close_Button.TabIndex = 36;
      Close_Button.Text = "Close";
      Close_Button.UseVisualStyleBackColor = true;
      Close_Button.Click +=  Close_Button_Click ;
      // 
      // Graph_Style_Label
      // 
      Graph_Style_Label.AutoSize = true;
      Graph_Style_Label.Location = new Point ( 642, 86 );
      Graph_Style_Label.Name = "Graph_Style_Label";
      Graph_Style_Label.Size = new Size ( 70, 15 );
      Graph_Style_Label.TabIndex = 39;
      Graph_Style_Label.Text = "Graph Type:";
      // 
      // Graph_Style_Combo
      // 
      Graph_Style_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Graph_Style_Combo.Location = new Point ( 718, 83 );
      Graph_Style_Combo.Name = "Graph_Style_Combo";
      Graph_Style_Combo.Size = new Size ( 214, 23 );
      Graph_Style_Combo.TabIndex = 40;
      Graph_Style_Combo.SelectedIndexChanged +=  Graph_Style_Combo_SelectedIndexChanged ;
      // 
      // Theme_Button
      // 
      Theme_Button.Location = new Point ( 718, 54 );
      Theme_Button.Name = "Theme_Button";
      Theme_Button.Size = new Size ( 75, 23 );
      Theme_Button.TabIndex = 43;
      Theme_Button.Text = "Theme";
      Theme_Button.UseVisualStyleBackColor = true;
      Theme_Button.Click +=  Theme_Button_Click ;
      // 
      // Poll_Speed_Button
      // 
      Poll_Speed_Button.Location = new Point ( 166, 127 );
      Poll_Speed_Button.Name = "Poll_Speed_Button";
      Poll_Speed_Button.Size = new Size ( 92, 23 );
      Poll_Speed_Button.TabIndex = 44;
      Poll_Speed_Button.Text = "Poll Speed";
      Poll_Speed_Button.UseVisualStyleBackColor = true;
      Poll_Speed_Button.Click +=  Poll_Speed_Button_Click ;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point ( 389, 108 );
      label2.Name = "label2";
      label2.Size = new Size ( 39, 15 );
      label2.TabIndex = 45;
      label2.Text = "Zoom";
      // 
      // Measurement_Label
      // 
      Measurement_Label.AutoSize = true;
      Measurement_Label.Location = new Point ( 359, 154 );
      Measurement_Label.Name = "Measurement_Label";
      Measurement_Label.Size = new Size ( 83, 15 );
      Measurement_Label.TabIndex = 14;
      Measurement_Label.Text = "Measurement:";
      // 
      // Measurement_Combo
      // 
      Measurement_Combo.FormattingEnabled = true;
      Measurement_Combo.Location = new Point ( 448, 149 );
      Measurement_Combo.Name = "Measurement_Combo";
      Measurement_Combo.Size = new Size ( 121, 23 );
      Measurement_Combo.TabIndex = 48;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point ( 125, 131 );
      label1.Name = "label1";
      label1.Size = new Size ( 35, 15 );
      label1.TabIndex = 49;
      label1.Text = "View:";
      // 
      // label3
      // 
      label3.AutoSize = true;
      label3.Location = new Point ( 106, 160 );
      label3.Name = "label3";
      label3.Size = new Size ( 54, 15 );
      label3.TabIndex = 50;
      label3.Text = "Instance:";
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Location = new Point ( 656, 58 );
      label4.Name = "label4";
      label4.Size = new Size ( 56, 15 );
      label4.TabIndex = 51;
      label4.Text = "Interface:";
      // 
      // Add_File_Checkbox
      // 
      Add_File_Checkbox.AutoSize = true;
      Add_File_Checkbox.Location = new Point ( 84, 84 );
      Add_File_Checkbox.Name = "Add_File_Checkbox";
      Add_File_Checkbox.Size = new Size ( 69, 19 );
      Add_File_Checkbox.TabIndex = 52;
      Add_File_Checkbox.Text = "Add File";
      Add_File_Checkbox.UseVisualStyleBackColor = true;
      // 
      // Analyze_Instrument_Data_Button
      // 
      Analyze_Instrument_Data_Button.Location = new Point ( 397, 31 );
      Analyze_Instrument_Data_Button.Name = "Analyze_Instrument_Data_Button";
      Analyze_Instrument_Data_Button.Size = new Size ( 80, 61 );
      Analyze_Instrument_Data_Button.TabIndex = 53;
      Analyze_Instrument_Data_Button.Text = "Analyze Instrument Recording";
      Analyze_Instrument_Data_Button.UseVisualStyleBackColor = true;
      Analyze_Instrument_Data_Button.Click +=  Analyze_Instrument_Data_Button_Click ;
      // 
      // Analyze_Poll_Timing_Button
      // 
      Analyze_Poll_Timing_Button.Location = new Point ( 320, 31 );
      Analyze_Poll_Timing_Button.Name = "Analyze_Poll_Timing_Button";
      Analyze_Poll_Timing_Button.Size = new Size ( 71, 61 );
      Analyze_Poll_Timing_Button.TabIndex = 54;
      Analyze_Poll_Timing_Button.Text = "Analyze Polling Recording";
      Analyze_Poll_Timing_Button.UseVisualStyleBackColor = true;
      Analyze_Poll_Timing_Button.Click +=  Analyze_Poll_Timing_Button_Click ;
      // 
      // Recording_Playback_Form
      // 
      AutoScaleDimensions = new SizeF ( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size ( 943, 731 );
      Controls.Add ( Analyze_Poll_Timing_Button );
      Controls.Add ( Analyze_Instrument_Data_Button );
      Controls.Add ( Add_File_Checkbox );
      Controls.Add ( label4 );
      Controls.Add ( label3 );
      Controls.Add ( label1 );
      Controls.Add ( Measurement_Combo );
      Controls.Add ( label2 );
      Controls.Add ( Poll_Speed_Button );
      Controls.Add ( Theme_Button );
      Controls.Add ( Graph_Style_Label );
      Controls.Add ( Graph_Style_Combo );
      Controls.Add ( Close_Button );
      Controls.Add ( Auto_Scroll_Panel );
      Controls.Add ( Pan_Scrollbar );
      Controls.Add ( Progress_Text_Box );
      Controls.Add ( Chart_Panel );
      Controls.Add ( Zoom_Slider );
      Controls.Add ( Normalize_Button );
      Controls.Add ( View_Mode_Button );
      Controls.Add ( Legend_Toggle_Button );
      Controls.Add ( Rolling_Check );
      Controls.Add ( Max_Points_Numeric );
      Controls.Add ( Title_Label );
      Controls.Add ( Measurement_Label );
      Controls.Add ( Clear_Button );
      Controls.Add ( Load_Button );
      MinimumSize = new Size ( 600, 400 );
      Name = "Recording_Playback_Form";
      StartPosition = FormStartPosition.CenterParent;
      Text = "Multi-Instrument Poll Form";
      ( (System.ComponentModel.ISupportInitialize) Max_Points_Numeric ).EndInit ( );
      ( (System.ComponentModel.ISupportInitialize) Zoom_Slider ).EndInit ( );
      Auto_Scroll_Panel.ResumeLayout ( false );
      Auto_Scroll_Panel.PerformLayout ( );
      ResumeLayout ( false );
      PerformLayout ( );
    }




    #endregion

    private System.Windows.Forms.Label Title_Label;
    private System.Windows.Forms.Button Clear_Button;
    private System.Windows.Forms.Button Load_Button;
    private System.Windows.Forms.Panel Chart_Panel;
    private CheckBox Rolling_Check;
    private CheckBox Auto_Scroll_Check;
    private Panel Auto_Scroll_Panel;
    private NumericUpDown Max_Points_Numeric;
    private Button Legend_Toggle_Button;
    private Button View_Mode_Button;
    private Button Normalize_Button;
    private TextBox Progress_Text_Box;
    private TrackBar Zoom_Slider;
    private HScrollBar Pan_Scrollbar;
    private Button Close_Button;
    private Label Graph_Style_Label;
    private ComboBox Graph_Style_Combo;
    private Button Theme_Button;
    private Button Poll_Speed_Button;
    private Label label2;
    private Label Measurement_Label;
    private ComboBox Measurement_Combo;
    private Label label1;
    private Label label3;
    private Label label4;
    private CheckBox Add_File_Checkbox;
    private Button Analyze_Instrument_Data_Button;
    private Button Analyze_Poll_Timing_Button;
  }
}
