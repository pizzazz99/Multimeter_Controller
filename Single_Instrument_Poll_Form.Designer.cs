namespace Multimeter_Controller
{
  partial class Single_Instrument_Poll_Form
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
      NPLC_Label = new Label ( );
      NPLC_Combo = new ComboBox ( );
      Current_Value_Label = new Label ( );
      Graph_Style_Label = new Label ( );
      Graph_Style_Combo = new ComboBox ( );
      Chart_Panel = new Panel ( );
      Clear_Button = new Button ( );
      Record_Button = new Button ( );
      Load_Button = new Button ( );
      Theme_Button = new Button ( );
      Save_Chart_Button = new Button ( );
      Rolling_Check = new CheckBox ( );
      Max_Points_Numeric = new NumericUpDown ( );
      button1 = new Button ( );
      label1 = new Label ( );
      Cycle_Count_Text_Box = new TextBox ( );
      Close_Button = new Button ( );
      label2 = new Label ( );
      NPLC_Textbox = new TextBox ( );
      Auto_Scroll_Panel = new Panel ( );
      Auto_Scroll_Check = new CheckBox ( );
      Pan_Scrollbar = new HScrollBar ( );
      Progress_Text_Box = new TextBox ( );
      ( (System.ComponentModel.ISupportInitialize) Readings_Numeric ).BeginInit ( );
      ( (System.ComponentModel.ISupportInitialize) Delay_Numeric ).BeginInit ( );
      ( (System.ComponentModel.ISupportInitialize) Max_Points_Numeric ).BeginInit ( );
      Auto_Scroll_Panel.SuspendLayout ( );
      SuspendLayout ( );
      // 
      // Title_Label
      // 
      Title_Label.AutoSize = true;
      Title_Label.Font = new Font ( "Segoe UI", 12F, FontStyle.Bold );
      Title_Label.Location = new Point ( 12, 9 );
      Title_Label.Name = "Title_Label";
      Title_Label.Size = new Size ( 180, 21 );
      Title_Label.TabIndex = 0;
      Title_Label.Text = "Single Instrument Poll";
      // 
      // Function_Label
      // 
      Function_Label.AutoSize = true;
      Function_Label.Location = new Point ( 11, 39 );
      Function_Label.Name = "Function_Label";
      Function_Label.Size = new Size ( 57, 15 );
      Function_Label.TabIndex = 1;
      Function_Label.Text = "Function:";
      // 
      // Function_Combo
      // 
      Function_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Function_Combo.Location = new Point ( 89, 36 );
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
      Continuous_Check.Checked = true;
      Continuous_Check.CheckState = CheckState.Checked;
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
      Delay_Label.Location = new Point ( 400, 83 );
      Delay_Label.Name = "Delay_Label";
      Delay_Label.Size = new Size ( 66, 15 );
      Delay_Label.TabIndex = 6;
      Delay_Label.Text = "Delay (ms):";
      // 
      // Delay_Numeric
      // 
      Delay_Numeric.Increment = new decimal ( new int [ ] { 50, 0, 0, 0 } );
      Delay_Numeric.Location = new Point ( 480, 81 );
      Delay_Numeric.Maximum = new decimal ( new int [ ] { 10000, 0, 0, 0 } );
      Delay_Numeric.Minimum = new decimal ( new int [ ] { 50, 0, 0, 0 } );
      Delay_Numeric.Name = "Delay_Numeric";
      Delay_Numeric.Size = new Size ( 80, 23 );
      Delay_Numeric.TabIndex = 7;
      Delay_Numeric.Value = new decimal ( new int [ ] { 500, 0, 0, 0 } );
      // 
      // Start_Stop_Button
      // 
      Start_Stop_Button.Location = new Point ( 16, 164 );
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
      Status_Label.Location = new Point ( 624, 106 );
      Status_Label.Name = "Status_Label";
      Status_Label.Size = new Size ( 28, 15 );
      Status_Label.TabIndex = 10;
      Status_Label.Text = "Idle";
      // 
      // NPLC_Label
      // 
      NPLC_Label.AutoSize = true;
      NPLC_Label.Location = new Point ( 449, 170 );
      NPLC_Label.Name = "NPLC_Label";
      NPLC_Label.Size = new Size ( 40, 15 );
      NPLC_Label.TabIndex = 17;
      NPLC_Label.Text = "NPLC:";
      // 
      // NPLC_Combo
      // 
      NPLC_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      NPLC_Combo.FormattingEnabled = true;
      NPLC_Combo.Items.AddRange ( new object [ ] { "0.02", "0.2", "1", "10", "100" } );
      NPLC_Combo.Location = new Point ( 497, 167 );
      NPLC_Combo.Name = "NPLC_Combo";
      NPLC_Combo.Size = new Size ( 70, 23 );
      NPLC_Combo.TabIndex = 18;
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
      Graph_Style_Combo.Location = new Point ( 624, 80 );
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
      Chart_Panel.Location = new Point ( 12, 198 );
      Chart_Panel.Name = "Chart_Panel";
      Chart_Panel.Size = new Size ( 858, 381 );
      Chart_Panel.TabIndex = 17;
      Chart_Panel.Paint +=  Chart_Panel_Paint ;
      Chart_Panel.MouseLeave +=  Chart_Panel_MouseLeave ;
      Chart_Panel.MouseMove +=  Chart_Panel_MouseMove ;
      Chart_Panel.Resize +=  Chart_Panel_Resize ;
      // 
      // Clear_Button
      // 
      Clear_Button.Location = new Point ( 102, 164 );
      Clear_Button.Name = "Clear_Button";
      Clear_Button.Size = new Size ( 65, 28 );
      Clear_Button.TabIndex = 9;
      Clear_Button.Text = "Clear";
      Clear_Button.UseVisualStyleBackColor = true;
      Clear_Button.Click +=  Clear_Button_Click ;
      // 
      // Record_Button
      // 
      Record_Button.Location = new Point ( 172, 164 );
      Record_Button.Name = "Record_Button";
      Record_Button.Size = new Size ( 85, 28 );
      Record_Button.TabIndex = 15;
      Record_Button.Text = "Record";
      Record_Button.UseVisualStyleBackColor = true;
      Record_Button.Click +=  Record_Button_Click ;
      // 
      // Load_Button
      // 
      Load_Button.Location = new Point ( 263, 164 );
      Load_Button.Name = "Load_Button";
      Load_Button.Size = new Size ( 65, 28 );
      Load_Button.TabIndex = 16;
      Load_Button.Text = "Load";
      Load_Button.UseVisualStyleBackColor = true;
      Load_Button.Click +=  Load_Button_Click ;
      // 
      // Theme_Button
      // 
      Theme_Button.Location = new Point ( 754, 164 );
      Theme_Button.Name = "Theme_Button";
      Theme_Button.Size = new Size ( 65, 28 );
      Theme_Button.TabIndex = 18;
      Theme_Button.Text = "Theme";
      Theme_Button.UseVisualStyleBackColor = true;
      Theme_Button.Click +=  Theme_Button_Click ;
      // 
      // Save_Chart_Button
      // 
      Save_Chart_Button.Location = new Point ( 582, 164 );
      Save_Chart_Button.Name = "Save_Chart_Button";
      Save_Chart_Button.Size = new Size ( 85, 27 );
      Save_Chart_Button.TabIndex = 19;
      Save_Chart_Button.Text = "Save Chart";
      Save_Chart_Button.UseVisualStyleBackColor = true;
      Save_Chart_Button.Click +=  Save_Chart_Button_Click ;
      // 
      // Rolling_Check
      // 
      Rolling_Check.AutoSize = true;
      Rolling_Check.Location = new Point ( 387, 108 );
      Rolling_Check.Name = "Rolling_Check";
      Rolling_Check.Size = new Size ( 79, 34 );
      Rolling_Check.TabIndex = 20;
      Rolling_Check.Text = "Show Last\r\n'N' Points";
      Rolling_Check.UseVisualStyleBackColor = true;
      Rolling_Check.CheckedChanged +=  Rolling_Check_CheckedChanged ;
      // 
      // Max_Points_Numeric
      // 
      Max_Points_Numeric.Enabled = false;
      Max_Points_Numeric.Increment = new decimal ( new int [ ] { 2, 0, 0, 0 } );
      Max_Points_Numeric.Location = new Point ( 480, 112 );
      Max_Points_Numeric.Maximum = new decimal ( new int [ ] { 10000, 0, 0, 0 } );
      Max_Points_Numeric.Minimum = new decimal ( new int [ ] { 2, 0, 0, 0 } );
      Max_Points_Numeric.Name = "Max_Points_Numeric";
      Max_Points_Numeric.Size = new Size ( 80, 23 );
      Max_Points_Numeric.TabIndex = 21;
      Max_Points_Numeric.Value = new decimal ( new int [ ] { 100, 0, 0, 0 } );
      Max_Points_Numeric.ValueChanged +=  Max_Points_Numeric_ValueChanged ;
      // 
      // button1
      // 
      button1.Location = new Point ( 673, 164 );
      button1.Name = "button1";
      button1.Size = new Size ( 75, 27 );
      button1.TabIndex = 22;
      button1.Text = "Stats";
      button1.UseVisualStyleBackColor = true;
      button1.Click +=  Stats_Toggle_Button_Click ;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point ( 305, 44 );
      label1.Name = "label1";
      label1.Size = new Size ( 39, 15 );
      label1.TabIndex = 23;
      label1.Text = "Cycle:";
      // 
      // Cycle_Count_Text_Box
      // 
      Cycle_Count_Text_Box.Location = new Point ( 350, 39 );
      Cycle_Count_Text_Box.Name = "Cycle_Count_Text_Box";
      Cycle_Count_Text_Box.Size = new Size ( 420, 23 );
      Cycle_Count_Text_Box.TabIndex = 24;
      // 
      // Close_Button
      // 
      Close_Button.Location = new Point ( 334, 164 );
      Close_Button.Name = "Close_Button";
      Close_Button.Size = new Size ( 75, 27 );
      Close_Button.TabIndex = 25;
      Close_Button.Text = "Close";
      Close_Button.UseVisualStyleBackColor = true;
      Close_Button.Click +=  Close_Button_Click ;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point ( 698, 138 );
      label2.Name = "label2";
      label2.Size = new Size ( 40, 15 );
      label2.TabIndex = 34;
      label2.Text = "NPLC:";
      // 
      // NPLC_Textbox
      // 
      NPLC_Textbox.Location = new Point ( 740, 135 );
      NPLC_Textbox.Name = "NPLC_Textbox";
      NPLC_Textbox.Size = new Size ( 76, 23 );
      NPLC_Textbox.TabIndex = 35;
      // 
      // Auto_Scroll_Panel
      // 
      Auto_Scroll_Panel.Controls.Add ( Auto_Scroll_Check );
      Auto_Scroll_Panel.Dock = DockStyle.Bottom;
      Auto_Scroll_Panel.Location = new Point ( 0, 607 );
      Auto_Scroll_Panel.Name = "Auto_Scroll_Panel";
      Auto_Scroll_Panel.Size = new Size ( 898, 25 );
      Auto_Scroll_Panel.TabIndex = 38;
      // 
      // Auto_Scroll_Check
      // 
      // Auto_Scroll_Check
      Auto_Scroll_Check.AutoSize = true;
      Auto_Scroll_Check.Checked = true;
      Auto_Scroll_Check.CheckState = CheckState.Checked;
      Auto_Scroll_Check.Location = new Point ( 10, 3 );
      Auto_Scroll_Check.Name = "Auto_Scroll_Check";
      Auto_Scroll_Check.Size = new Size ( 130, 19 );
      Auto_Scroll_Check.TabIndex = 0;
      Auto_Scroll_Check.Text = "Auto-scroll to latest";
      Auto_Scroll_Check.UseVisualStyleBackColor = true;
      Auto_Scroll_Check.CheckedChanged += Auto_Scroll_Check_CheckedChanged;  // ← ADD
      // Pan_Scrollbar
      Pan_Scrollbar.Dock = DockStyle.Bottom;
      Pan_Scrollbar.Enabled = false;
      Pan_Scrollbar.Location = new Point ( 0, 632 );
      Pan_Scrollbar.Maximum = 109;
      Pan_Scrollbar.Name = "Pan_Scrollbar";
      Pan_Scrollbar.Size = new Size ( 898, 17 );
      Pan_Scrollbar.TabIndex = 37;
      Pan_Scrollbar.Scroll += Pan_Scrollbar_Scroll;        // ← ADD
      Pan_Scrollbar.ValueChanged += Pan_Scrollbar_ValueChanged;  // ← ADD
      // 
      // Progress_Text_Box
      // 
      Progress_Text_Box.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Progress_Text_Box.BackColor = SystemColors.Control;
      Progress_Text_Box.BorderStyle = BorderStyle.None;
      Progress_Text_Box.Location = new Point ( 10, 585 );
      Progress_Text_Box.Name = "Progress_Text_Box";
      Progress_Text_Box.ReadOnly = true;
      Progress_Text_Box.Size = new Size ( 857, 16 );
      Progress_Text_Box.TabIndex = 36;
      // Stats_Panel
      Stats_Panel = new Panel ( );
      Stats_Panel.Name = "Stats_Panel";
      Stats_Panel.Location = new Point ( 670, 198 );
      Stats_Panel.Size = new Size ( 200, 381 );
      Stats_Panel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
      Stats_Panel.BackColor = Color.FromArgb ( 24, 27, 31 );
      Stats_Panel.Visible = false;
      this.Controls.Add ( Stats_Panel );

      // 
      // Single_Instrument_Poll_Form
      // 
      AutoScaleDimensions = new SizeF ( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size ( 898, 649 );
      Controls.Add ( Pan_Scrollbar );       // docks bottom - sits at very bottom
      Controls.Add ( Auto_Scroll_Panel );   // docks bottom - sits just above scrollbar
      Controls.Add ( Progress_Text_Box );   // anchored bottom - sits above both
      Controls.Add ( NPLC_Textbox );
      Controls.Add ( label2 );
      Controls.Add ( Close_Button );
      Controls.Add ( Cycle_Count_Text_Box );
      Controls.Add ( label1 );
      Controls.Add ( button1 );
      Controls.Add ( Title_Label );
      Controls.Add ( NPLC_Label );
      Controls.Add ( NPLC_Combo );
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
      Controls.Add ( Graph_Style_Label );
      Controls.Add ( Graph_Style_Combo );
      Controls.Add ( Record_Button );
      Controls.Add ( Load_Button );
      Controls.Add ( Theme_Button );
      Controls.Add ( Save_Chart_Button );
      Controls.Add ( Chart_Panel );
      Controls.Add ( Rolling_Check );
      Controls.Add ( Max_Points_Numeric );
      Controls.Add ( Status_Panel );
      MinimumSize = new Size ( 600, 430 );
      Name = "Single_Instrument_Poll_Form";
      StartPosition = FormStartPosition.CenterParent;
      Text = "Single Instrument Poll Form";
      ( (System.ComponentModel.ISupportInitialize) Readings_Numeric ).EndInit ( );
      ( (System.ComponentModel.ISupportInitialize) Delay_Numeric ).EndInit ( );
      ( (System.ComponentModel.ISupportInitialize) Max_Points_Numeric ).EndInit ( );
      Auto_Scroll_Panel.ResumeLayout ( false );
      Auto_Scroll_Panel.PerformLayout ( );
      ResumeLayout ( false );
      PerformLayout ( );
    }

    #endregion
    private Panel Stats_Panel;
    private System.Windows.Forms.Label Title_Label;
    private System.Windows.Forms.Label Function_Label;
    private System.Windows.Forms.ComboBox Function_Combo;
    private System.Windows.Forms.Label Readings_Label;
    private System.Windows.Forms.NumericUpDown Readings_Numeric;
    private System.Windows.Forms.Label Delay_Label;
    private System.Windows.Forms.Label NPLC_Label;
    private System.Windows.Forms.ComboBox NPLC_Combo;
    private System.Windows.Forms.NumericUpDown Delay_Numeric;
    private System.Windows.Forms.CheckBox Continuous_Check;
    private System.Windows.Forms.Button Start_Stop_Button;
    private System.Windows.Forms.Button Clear_Button;
    private System.Windows.Forms.Label Status_Label;
    private System.Windows.Forms.Label Current_Value_Label;
    private System.Windows.Forms.Label Graph_Style_Label;
    private System.Windows.Forms.ComboBox Graph_Style_Combo;
    private System.Windows.Forms.Panel Chart_Panel;
    private System.Windows.Forms.Button Record_Button;
    private System.Windows.Forms.Button Load_Button;
    private System.Windows.Forms.Button Theme_Button;
    private System.Windows.Forms.Button Save_Chart_Button;
    private System.Windows.Forms.CheckBox Rolling_Check;
    private System.Windows.Forms.NumericUpDown Max_Points_Numeric;  // *** KEEP ONLY THIS ONE ***
    private Panel Status_Panel;
    private Button button1;
    private Label label1;
    private TextBox Cycle_Count_Text_Box;
    private Button Close_Button;
    private Label label2;
    private TextBox NPLC_Textbox;
    private Panel Auto_Scroll_Panel;
    private CheckBox Auto_Scroll_Check;
    private HScrollBar Pan_Scrollbar;
    private TextBox Progress_Text_Box;
  }
}
