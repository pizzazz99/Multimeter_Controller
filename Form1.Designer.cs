
using System.Drawing;
using System.Windows.Forms;



namespace Multimeter_Controller
{
  partial class Form1
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
      Title_Label = new Label ( );
      Command_List_Label = new Label ( );
      Command_List = new ListBox ( );
      Detail_Text_Box = new TextBox ( );
      Open_Dictionary_Button = new Button ( );
      Connection_Mode_Label = new Label ( );
      Connection_Mode_Combo = new ComboBox ( );
      Connection_Group = new GroupBox ( );
      Find_Prologix_Button = new Button ( );
      Subnet_Label = new Label ( );
      Subnet_Textbox = new TextBox ( );
      IP_Address_Label = new Label ( );
      IP_Address_Textbox = new TextBox ( );
      Connected_Instrument_Textbox = new TextBox ( );
      label1 = new Label ( );
      Scan_Bus_Button = new Button ( );
      COM_Port_Label = new Label ( );
      COM_Port_Combo = new ComboBox ( );
      Refresh_Ports_Button = new Button ( );
      Baud_Rate_Label = new Label ( );
      Baud_Rate_Combo = new ComboBox ( );
      Data_Bits_Label = new Label ( );
      Data_Bits_Combo = new ComboBox ( );
      Parity_Label = new Label ( );
      Parity_Combo = new ComboBox ( );
      Stop_Bits_Label = new Label ( );
      Stop_Bits_Combo = new ComboBox ( );
      Flow_Control_Label = new Label ( );
      Flow_Control_Combo = new ComboBox ( );
      Prologix_Header_Label = new Label ( );
      Read_Timeout_Label = new Label ( );
      Read_Timeout_Combo_Box = new ComboBox ( );
      Defaults_Button = new Button ( );
      Connect_Button = new Button ( );
      Connection_Status_Label = new Label ( );
      GPIB_Address_Numeric = new NumericUpDown ( );
      Send_Command_Label = new Label ( );
      Send_Command_Text_Box = new TextBox ( );
      Execute_Button = new Button ( );
      Diag_Button = new Button ( );
      Response_Label = new Label ( );
      Response_Text_Box = new TextBox ( );
      Instruments_Group = new GroupBox ( );
      button1 = new Button ( );
      Meter_Roll_Label = new Label ( );
      Roll_Name_Textbox = new TextBox ( );
      Apply_NPLC_To_All_Button = new Button ( );
      NPLC_Combo_Box = new ComboBox ( );
      NPLC_Label = new Label ( );
      Instrument_Name_Label = new Label ( );
      Instrument_Name_Text = new TextBox ( );
      GPIB_Address_Label = new Label ( );
      Instrument_Type_Label = new Label ( );
      Instrument_Type_Combo = new ComboBox ( );
      Add_Instrument_Button = new Button ( );
      Remove_Instrument_Button = new Button ( );
      Saved_Instruments_Label = new Label ( );
      Instruments_List = new ListBox ( );
      Select_Instrument_Button = new Button ( );
      Multi_Poll_Button = new Button ( );
      Command_History_List_Box = new ListBox ( );
      History_Label = new Label ( );
      Meter_Info_Button = new Button ( );
      Settings_Button = new Button ( );
      Reset_Defaults_Button = new Button ( );
      Button_Show_Execution_Trace = new Button ( );
      Session_Settings_Button = new Button ( );
      Display_Recording_Button = new Button ( );
      Connection_Group.SuspendLayout ( );
      ( (System.ComponentModel.ISupportInitialize) GPIB_Address_Numeric ).BeginInit ( );
      Instruments_Group.SuspendLayout ( );
      SuspendLayout ( );
      // 
      // Title_Label
      // 
      Title_Label.AutoSize = true;
      Title_Label.Location = new Point ( 0, 0 );
      Title_Label.Name = "Title_Label";
      Title_Label.Size = new Size ( 0, 15 );
      Title_Label.TabIndex = 0;
      Title_Label.Visible = false;
      // 
      // Command_List_Label
      // 
      Command_List_Label.AutoSize = true;
      Command_List_Label.Location = new Point ( 12, 12 );
      Command_List_Label.Name = "Command_List_Label";
      Command_List_Label.Size = new Size ( 72, 15 );
      Command_List_Label.TabIndex = 1;
      Command_List_Label.Text = "Commands:";
      // 
      // Command_List
      // 
      Command_List.Anchor =    AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left ;
      Command_List.FormattingEnabled = true;
      Command_List.HorizontalScrollbar = true;
      Command_List.Location = new Point ( 12, 32 );
      Command_List.Name = "Command_List";
      Command_List.Size = new Size ( 184, 244 );
      Command_List.TabIndex = 2;
      Command_List.SelectedIndexChanged +=  Command_List_Selected_Index_Changed ;
      // 
      // Detail_Text_Box
      // 
      Detail_Text_Box.BackColor = SystemColors.Window;
      Detail_Text_Box.Font = new Font ( "Consolas", 9.5F );
      Detail_Text_Box.Location = new Point ( 202, 32 );
      Detail_Text_Box.Multiline = true;
      Detail_Text_Box.Name = "Detail_Text_Box";
      Detail_Text_Box.ReadOnly = true;
      Detail_Text_Box.ScrollBars = ScrollBars.Vertical;
      Detail_Text_Box.Size = new Size ( 622, 200 );
      Detail_Text_Box.TabIndex = 3;
      // 
      // Open_Dictionary_Button
      // 
      Open_Dictionary_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Open_Dictionary_Button.Location = new Point ( 65, 295 );
      Open_Dictionary_Button.Name = "Open_Dictionary_Button";
      Open_Dictionary_Button.Size = new Size ( 72, 35 );
      Open_Dictionary_Button.TabIndex = 11;
      Open_Dictionary_Button.Text = "Dictionary";
      Open_Dictionary_Button.UseVisualStyleBackColor = true;
      Open_Dictionary_Button.Click +=  Open_Dictionary_Button_Click ;
      // 
      // Connection_Mode_Label
      // 
      Connection_Mode_Label.AutoSize = true;
      Connection_Mode_Label.Location = new Point ( 12, 25 );
      Connection_Mode_Label.Name = "Connection_Mode_Label";
      Connection_Mode_Label.Size = new Size ( 72, 15 );
      Connection_Mode_Label.TabIndex = 23;
      Connection_Mode_Label.Text = "Connection:";
      // 
      // Connection_Mode_Combo
      // 
      Connection_Mode_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Connection_Mode_Combo.Location = new Point ( 112, 22 );
      Connection_Mode_Combo.Name = "Connection_Mode_Combo";
      Connection_Mode_Combo.Size = new Size ( 130, 23 );
      Connection_Mode_Combo.TabIndex = 24;
      Connection_Mode_Combo.SelectedIndexChanged +=  Connection_Mode_Combo_SelectedIndexChanged ;
      // 
      // Connection_Group
      // 
      Connection_Group.Anchor =    AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Right ;
      Connection_Group.Controls.Add ( Find_Prologix_Button );
      Connection_Group.Controls.Add ( Subnet_Label );
      Connection_Group.Controls.Add ( Subnet_Textbox );
      Connection_Group.Controls.Add ( IP_Address_Label );
      Connection_Group.Controls.Add ( IP_Address_Textbox );
      Connection_Group.Controls.Add ( Connected_Instrument_Textbox );
      Connection_Group.Controls.Add ( label1 );
      Connection_Group.Controls.Add ( Connection_Mode_Label );
      Connection_Group.Controls.Add ( Connection_Mode_Combo );
      Connection_Group.Controls.Add ( Scan_Bus_Button );
      Connection_Group.Controls.Add ( COM_Port_Label );
      Connection_Group.Controls.Add ( COM_Port_Combo );
      Connection_Group.Controls.Add ( Refresh_Ports_Button );
      Connection_Group.Controls.Add ( Baud_Rate_Label );
      Connection_Group.Controls.Add ( Baud_Rate_Combo );
      Connection_Group.Controls.Add ( Data_Bits_Label );
      Connection_Group.Controls.Add ( Data_Bits_Combo );
      Connection_Group.Controls.Add ( Parity_Label );
      Connection_Group.Controls.Add ( Parity_Combo );
      Connection_Group.Controls.Add ( Stop_Bits_Label );
      Connection_Group.Controls.Add ( Stop_Bits_Combo );
      Connection_Group.Controls.Add ( Flow_Control_Label );
      Connection_Group.Controls.Add ( Flow_Control_Combo );
      Connection_Group.Controls.Add ( Prologix_Header_Label );
      Connection_Group.Controls.Add ( Read_Timeout_Label );
      Connection_Group.Controls.Add ( Read_Timeout_Combo_Box );
      Connection_Group.Controls.Add ( Defaults_Button );
      Connection_Group.Controls.Add ( Connect_Button );
      Connection_Group.Controls.Add ( Connection_Status_Label );
      Connection_Group.Location = new Point ( 1080, 12 );
      Connection_Group.Name = "Connection_Group";
      Connection_Group.Size = new Size ( 248, 576 );
      Connection_Group.TabIndex = 15;
      Connection_Group.TabStop = false;
      Connection_Group.Text = "Connection Settings";
      // 
      // Find_Prologix_Button
      // 
      Find_Prologix_Button.Location = new Point ( 12, 410 );
      Find_Prologix_Button.Name = "Find_Prologix_Button";
      Find_Prologix_Button.Size = new Size ( 102, 25 );
      Find_Prologix_Button.TabIndex = 31;
      Find_Prologix_Button.Text = "Find Proligix IP";
      Find_Prologix_Button.UseVisualStyleBackColor = true;
      Find_Prologix_Button.Click +=  Find_Prologix_Button_Click ;
      // 
      // Subnet_Label
      // 
      Subnet_Label.AutoSize = true;
      Subnet_Label.Location = new Point ( 28, 285 );
      Subnet_Label.Name = "Subnet_Label";
      Subnet_Label.Size = new Size ( 44, 15 );
      Subnet_Label.TabIndex = 30;
      Subnet_Label.Text = "Subnet";
      // 
      // Subnet_Textbox
      // 
      Subnet_Textbox.Location = new Point ( 114, 282 );
      Subnet_Textbox.Name = "Subnet_Textbox";
      Subnet_Textbox.Size = new Size ( 100, 23 );
      Subnet_Textbox.TabIndex = 29;
      // 
      // IP_Address_Label
      // 
      IP_Address_Label.AutoSize = true;
      IP_Address_Label.Location = new Point ( 28, 256 );
      IP_Address_Label.Name = "IP_Address_Label";
      IP_Address_Label.Size = new Size ( 62, 15 );
      IP_Address_Label.TabIndex = 28;
      IP_Address_Label.Text = "IP Address";
      // 
      // IP_Address_Textbox
      // 
      IP_Address_Textbox.Location = new Point ( 114, 253 );
      IP_Address_Textbox.Name = "IP_Address_Textbox";
      IP_Address_Textbox.Size = new Size ( 100, 23 );
      IP_Address_Textbox.TabIndex = 27;
      // 
      // Connected_Instrument_Textbox
      // 
      Connected_Instrument_Textbox.Location = new Point ( 111, 322 );
      Connected_Instrument_Textbox.Name = "Connected_Instrument_Textbox";
      Connected_Instrument_Textbox.Size = new Size ( 129, 23 );
      Connected_Instrument_Textbox.TabIndex = 26;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point ( 10, 325 );
      label1.Name = "label1";
      label1.Size = new Size ( 84, 15 );
      label1.TabIndex = 25;
      label1.Text = "Connected To:";
      // 
      // Scan_Bus_Button
      // 
      Scan_Bus_Button.Enabled = false;
      Scan_Bus_Button.Location = new Point ( 13, 441 );
      Scan_Bus_Button.Name = "Scan_Bus_Button";
      Scan_Bus_Button.Size = new Size ( 89, 25 );
      Scan_Bus_Button.TabIndex = 9;
      Scan_Bus_Button.Text = "Scan Bus";
      Scan_Bus_Button.UseVisualStyleBackColor = true;
      Scan_Bus_Button.Click +=  Scan_Bus_Button_Click ;
      // 
      // COM_Port_Label
      // 
      COM_Port_Label.AutoSize = true;
      COM_Port_Label.Location = new Point ( 12, 50 );
      COM_Port_Label.Name = "COM_Port_Label";
      COM_Port_Label.Size = new Size ( 63, 15 );
      COM_Port_Label.TabIndex = 0;
      COM_Port_Label.Text = "COM Port:";
      // 
      // COM_Port_Combo
      // 
      COM_Port_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      COM_Port_Combo.Location = new Point ( 112, 46 );
      COM_Port_Combo.Name = "COM_Port_Combo";
      COM_Port_Combo.Size = new Size ( 95, 23 );
      COM_Port_Combo.TabIndex = 1;
      // 
      // Refresh_Ports_Button
      // 
      Refresh_Ports_Button.Location = new Point ( 212, 45 );
      Refresh_Ports_Button.Name = "Refresh_Ports_Button";
      Refresh_Ports_Button.Size = new Size ( 30, 25 );
      Refresh_Ports_Button.TabIndex = 2;
      Refresh_Ports_Button.Text = "↻";
      Refresh_Ports_Button.UseVisualStyleBackColor = true;
      Refresh_Ports_Button.Click +=  Refresh_Ports_Button_Click ;
      // 
      // Baud_Rate_Label
      // 
      Baud_Rate_Label.AutoSize = true;
      Baud_Rate_Label.Location = new Point ( 12, 77 );
      Baud_Rate_Label.Name = "Baud_Rate_Label";
      Baud_Rate_Label.Size = new Size ( 63, 15 );
      Baud_Rate_Label.TabIndex = 3;
      Baud_Rate_Label.Text = "Baud Rate:";
      // 
      // Baud_Rate_Combo
      // 
      Baud_Rate_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Baud_Rate_Combo.Location = new Point ( 112, 76 );
      Baud_Rate_Combo.Name = "Baud_Rate_Combo";
      Baud_Rate_Combo.Size = new Size ( 130, 23 );
      Baud_Rate_Combo.TabIndex = 4;
      // 
      // Data_Bits_Label
      // 
      Data_Bits_Label.AutoSize = true;
      Data_Bits_Label.Location = new Point ( 12, 108 );
      Data_Bits_Label.Name = "Data_Bits_Label";
      Data_Bits_Label.Size = new Size ( 56, 15 );
      Data_Bits_Label.TabIndex = 5;
      Data_Bits_Label.Text = "Data Bits:";
      // 
      // Data_Bits_Combo
      // 
      Data_Bits_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Data_Bits_Combo.Location = new Point ( 112, 105 );
      Data_Bits_Combo.Name = "Data_Bits_Combo";
      Data_Bits_Combo.Size = new Size ( 130, 23 );
      Data_Bits_Combo.TabIndex = 6;
      // 
      // Parity_Label
      // 
      Parity_Label.AutoSize = true;
      Parity_Label.Location = new Point ( 12, 138 );
      Parity_Label.Name = "Parity_Label";
      Parity_Label.Size = new Size ( 40, 15 );
      Parity_Label.TabIndex = 7;
      Parity_Label.Text = "Parity:";
      // 
      // Parity_Combo
      // 
      Parity_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Parity_Combo.Location = new Point ( 112, 135 );
      Parity_Combo.Name = "Parity_Combo";
      Parity_Combo.Size = new Size ( 130, 23 );
      Parity_Combo.TabIndex = 8;
      // 
      // Stop_Bits_Label
      // 
      Stop_Bits_Label.AutoSize = true;
      Stop_Bits_Label.Location = new Point ( 12, 168 );
      Stop_Bits_Label.Name = "Stop_Bits_Label";
      Stop_Bits_Label.Size = new Size ( 56, 15 );
      Stop_Bits_Label.TabIndex = 9;
      Stop_Bits_Label.Text = "Stop Bits:";
      // 
      // Stop_Bits_Combo
      // 
      Stop_Bits_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Stop_Bits_Combo.Location = new Point ( 112, 165 );
      Stop_Bits_Combo.Name = "Stop_Bits_Combo";
      Stop_Bits_Combo.Size = new Size ( 130, 23 );
      Stop_Bits_Combo.TabIndex = 10;
      // 
      // Flow_Control_Label
      // 
      Flow_Control_Label.AutoSize = true;
      Flow_Control_Label.Location = new Point ( 12, 198 );
      Flow_Control_Label.Name = "Flow_Control_Label";
      Flow_Control_Label.Size = new Size ( 78, 15 );
      Flow_Control_Label.TabIndex = 11;
      Flow_Control_Label.Text = "Flow Control:";
      // 
      // Flow_Control_Combo
      // 
      Flow_Control_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Flow_Control_Combo.Location = new Point ( 112, 195 );
      Flow_Control_Combo.Name = "Flow_Control_Combo";
      Flow_Control_Combo.Size = new Size ( 130, 23 );
      Flow_Control_Combo.TabIndex = 12;
      // 
      // Prologix_Header_Label
      // 
      Prologix_Header_Label.AutoSize = true;
      Prologix_Header_Label.Font = new Font ( "Segoe UI", 9F, FontStyle.Bold );
      Prologix_Header_Label.Location = new Point ( 12, 358 );
      Prologix_Header_Label.Name = "Prologix_Header_Label";
      Prologix_Header_Label.Size = new Size ( 102, 15 );
      Prologix_Header_Label.TabIndex = 15;
      Prologix_Header_Label.Text = "Prologix Settings";
      // 
      // Read_Timeout_Label
      // 
      Read_Timeout_Label.AutoSize = true;
      Read_Timeout_Label.Location = new Point ( 12, 227 );
      Read_Timeout_Label.Name = "Read_Timeout_Label";
      Read_Timeout_Label.Size = new Size ( 81, 15 );
      Read_Timeout_Label.TabIndex = 13;
      Read_Timeout_Label.Text = "Read Timeout";
      // 
      // Read_Timeout_Combo_Box
      // 
      Read_Timeout_Combo_Box.DropDownStyle = ComboBoxStyle.DropDownList;
      Read_Timeout_Combo_Box.Location = new Point ( 112, 224 );
      Read_Timeout_Combo_Box.Name = "Read_Timeout_Combo_Box";
      Read_Timeout_Combo_Box.Size = new Size ( 130, 23 );
      Read_Timeout_Combo_Box.TabIndex = 14;
      // 
      // Defaults_Button
      // 
      Defaults_Button.Location = new Point ( 12, 466 );
      Defaults_Button.Name = "Defaults_Button";
      Defaults_Button.Size = new Size ( 90, 30 );
      Defaults_Button.TabIndex = 20;
      Defaults_Button.Text = "Defaults";
      Defaults_Button.UseVisualStyleBackColor = true;
      // 
      // Connect_Button
      // 
      Connect_Button.Location = new Point ( 124, 466 );
      Connect_Button.Name = "Connect_Button";
      Connect_Button.Size = new Size ( 83, 30 );
      Connect_Button.TabIndex = 21;
      Connect_Button.Text = "Connect";
      Connect_Button.UseVisualStyleBackColor = true;
      Connect_Button.Click +=  Connect_Button_Click ;
      // 
      // Connection_Status_Label
      // 
      Connection_Status_Label.AutoSize = true;
      Connection_Status_Label.Font = new Font ( "Segoe UI", 9F, FontStyle.Bold );
      Connection_Status_Label.ForeColor = Color.Red;
      Connection_Status_Label.Location = new Point ( 124, 499 );
      Connection_Status_Label.Name = "Connection_Status_Label";
      Connection_Status_Label.Size = new Size ( 83, 15 );
      Connection_Status_Label.TabIndex = 22;
      Connection_Status_Label.Text = "Disconnected";
      // 
      // GPIB_Address_Numeric
      // 
      GPIB_Address_Numeric.Location = new Point ( 102, 102 );
      GPIB_Address_Numeric.Maximum = new decimal ( new int [ ] { 30, 0, 0, 0 } );
      GPIB_Address_Numeric.Name = "GPIB_Address_Numeric";
      GPIB_Address_Numeric.Size = new Size ( 60, 23 );
      GPIB_Address_Numeric.TabIndex = 3;
      GPIB_Address_Numeric.Value = new decimal ( new int [ ] { 22, 0, 0, 0 } );
      // 
      // Send_Command_Label
      // 
      Send_Command_Label.AutoSize = true;
      Send_Command_Label.Font = new Font ( "Segoe UI", 9F, FontStyle.Bold );
      Send_Command_Label.Location = new Point ( 204, 248 );
      Send_Command_Label.Name = "Send_Command_Label";
      Send_Command_Label.Size = new Size ( 97, 15 );
      Send_Command_Label.TabIndex = 4;
      Send_Command_Label.Text = "Send Command:";
      // 
      // Send_Command_Text_Box
      // 
      Send_Command_Text_Box.Font = new Font ( "Consolas", 10F );
      Send_Command_Text_Box.Location = new Point ( 307, 244 );
      Send_Command_Text_Box.Name = "Send_Command_Text_Box";
      Send_Command_Text_Box.Size = new Size ( 200, 23 );
      Send_Command_Text_Box.TabIndex = 5;
      // 
      // Execute_Button
      // 
      Execute_Button.Location = new Point ( 513, 243 );
      Execute_Button.Name = "Execute_Button";
      Execute_Button.Size = new Size ( 80, 26 );
      Execute_Button.TabIndex = 6;
      Execute_Button.Text = "Execute";
      Execute_Button.UseVisualStyleBackColor = true;
      Execute_Button.Click +=  Execute_Button_Click ;
      // 
      // Diag_Button
      // 
      Diag_Button.Location = new Point ( 689, 243 );
      Diag_Button.Name = "Diag_Button";
      Diag_Button.Size = new Size ( 120, 25 );
      Diag_Button.TabIndex = 8;
      Diag_Button.Text = "Raw Diagnostic";
      Diag_Button.UseVisualStyleBackColor = true;
      Diag_Button.Click +=  Diag_Button_Click ;
      // 
      // Response_Label
      // 
      Response_Label.AutoSize = true;
      Response_Label.Font = new Font ( "Segoe UI", 9F, FontStyle.Bold );
      Response_Label.Location = new Point ( 202, 344 );
      Response_Label.Name = "Response_Label";
      Response_Label.Size = new Size ( 63, 15 );
      Response_Label.TabIndex = 9;
      Response_Label.Text = "Response:";
      // 
      // Response_Text_Box
      // 
      Response_Text_Box.Anchor =    AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left ;
      Response_Text_Box.BackColor = SystemColors.Window;
      Response_Text_Box.Font = new Font ( "Consolas", 9.5F );
      Response_Text_Box.Location = new Point ( 202, 362 );
      Response_Text_Box.Multiline = true;
      Response_Text_Box.Name = "Response_Text_Box";
      Response_Text_Box.ReadOnly = true;
      Response_Text_Box.ScrollBars = ScrollBars.Vertical;
      Response_Text_Box.Size = new Size ( 622, 220 );
      Response_Text_Box.TabIndex = 10;
      // 
      // Instruments_Group
      // 
      Instruments_Group.Anchor =    AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Right ;
      Instruments_Group.Controls.Add ( button1 );
      Instruments_Group.Controls.Add ( Meter_Roll_Label );
      Instruments_Group.Controls.Add ( Roll_Name_Textbox );
      Instruments_Group.Controls.Add ( Apply_NPLC_To_All_Button );
      Instruments_Group.Controls.Add ( NPLC_Combo_Box );
      Instruments_Group.Controls.Add ( NPLC_Label );
      Instruments_Group.Controls.Add ( Instrument_Name_Label );
      Instruments_Group.Controls.Add ( Instrument_Name_Text );
      Instruments_Group.Controls.Add ( GPIB_Address_Label );
      Instruments_Group.Controls.Add ( GPIB_Address_Numeric );
      Instruments_Group.Controls.Add ( Instrument_Type_Label );
      Instruments_Group.Controls.Add ( Instrument_Type_Combo );
      Instruments_Group.Controls.Add ( Add_Instrument_Button );
      Instruments_Group.Controls.Add ( Remove_Instrument_Button );
      Instruments_Group.Controls.Add ( Saved_Instruments_Label );
      Instruments_Group.Controls.Add ( Instruments_List );
      Instruments_Group.Controls.Add ( Select_Instrument_Button );
      Instruments_Group.Controls.Add ( Multi_Poll_Button );
      Instruments_Group.Location = new Point ( 830, 12 );
      Instruments_Group.Name = "Instruments_Group";
      Instruments_Group.Size = new Size ( 240, 576 );
      Instruments_Group.TabIndex = 14;
      Instruments_Group.TabStop = false;
      Instruments_Group.Text = "GPIB Instruments";
      // 
      // button1
      // 
      button1.Location = new Point ( 126, 246 );
      button1.Name = "button1";
      button1.Size = new Size ( 87, 30 );
      button1.TabIndex = 57;
      button1.Text = "About NPLC";
      button1.UseVisualStyleBackColor = true;
      button1.Click +=  NPLC_Info_Button_Click ;
      // 
      // Meter_Roll_Label
      // 
      Meter_Roll_Label.AutoSize = true;
      Meter_Roll_Label.Location = new Point ( 15, 77 );
      Meter_Roll_Label.Name = "Meter_Roll_Label";
      Meter_Roll_Label.Size = new Size ( 61, 15 );
      Meter_Roll_Label.TabIndex = 17;
      Meter_Roll_Label.Text = "Meter Roll";
      // 
      // Roll_Name_Textbox
      // 
      Roll_Name_Textbox.Location = new Point ( 102, 74 );
      Roll_Name_Textbox.Name = "Roll_Name_Textbox";
      Roll_Name_Textbox.Size = new Size ( 129, 23 );
      Roll_Name_Textbox.TabIndex = 18;
      Roll_Name_Textbox.Leave +=  Roll_Name_Textbox_Leave ;
      // 
      // Apply_NPLC_To_All_Button
      // 
      Apply_NPLC_To_All_Button.Location = new Point ( 126, 207 );
      Apply_NPLC_To_All_Button.Name = "Apply_NPLC_To_All_Button";
      Apply_NPLC_To_All_Button.Size = new Size ( 90, 27 );
      Apply_NPLC_To_All_Button.TabIndex = 16;
      Apply_NPLC_To_All_Button.Text = "Apply to All";
      Apply_NPLC_To_All_Button.UseVisualStyleBackColor = true;
      Apply_NPLC_To_All_Button.Click +=  Apply_NPLC_To_All_Button_Click ;
      // 
      // NPLC_Combo_Box
      // 
      NPLC_Combo_Box.FormattingEnabled = true;
      NPLC_Combo_Box.Location = new Point ( 126, 177 );
      NPLC_Combo_Box.Name = "NPLC_Combo_Box";
      NPLC_Combo_Box.Size = new Size ( 90, 23 );
      NPLC_Combo_Box.TabIndex = 15;
      NPLC_Combo_Box.SelectedIndexChanged +=  NPLC_Combo_Box_SelectedIndexChanged ;
      // 
      // NPLC_Label
      // 
      NPLC_Label.AutoSize = true;
      NPLC_Label.Location = new Point ( 84, 180 );
      NPLC_Label.Name = "NPLC_Label";
      NPLC_Label.Size = new Size ( 40, 15 );
      NPLC_Label.TabIndex = 14;
      NPLC_Label.Text = "NPLC:";
      // 
      // Instrument_Name_Label
      // 
      Instrument_Name_Label.AutoSize = true;
      Instrument_Name_Label.Location = new Point ( 15, 19 );
      Instrument_Name_Label.Name = "Instrument_Name_Label";
      Instrument_Name_Label.Size = new Size ( 42, 15 );
      Instrument_Name_Label.TabIndex = 0;
      Instrument_Name_Label.Text = "Name:";
      // 
      // Instrument_Name_Text
      // 
      Instrument_Name_Text.Location = new Point ( 102, 16 );
      Instrument_Name_Text.Name = "Instrument_Name_Text";
      Instrument_Name_Text.Size = new Size ( 129, 23 );
      Instrument_Name_Text.TabIndex = 1;
      // 
      // GPIB_Address_Label
      // 
      GPIB_Address_Label.AutoSize = true;
      GPIB_Address_Label.Location = new Point ( 15, 105 );
      GPIB_Address_Label.Name = "GPIB_Address_Label";
      GPIB_Address_Label.Size = new Size ( 80, 15 );
      GPIB_Address_Label.TabIndex = 2;
      GPIB_Address_Label.Text = "GPIB Address:";
      // 
      // Instrument_Type_Label
      // 
      Instrument_Type_Label.AutoSize = true;
      Instrument_Type_Label.Location = new Point ( 15, 48 );
      Instrument_Type_Label.Name = "Instrument_Type_Label";
      Instrument_Type_Label.Size = new Size ( 35, 15 );
      Instrument_Type_Label.TabIndex = 4;
      Instrument_Type_Label.Text = "Type:";
      // 
      // Instrument_Type_Combo
      // 
      Instrument_Type_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Instrument_Type_Combo.Location = new Point ( 102, 45 );
      Instrument_Type_Combo.Name = "Instrument_Type_Combo";
      Instrument_Type_Combo.Size = new Size ( 129, 23 );
      Instrument_Type_Combo.TabIndex = 5;
      Instrument_Type_Combo.SelectedIndexChanged +=  Instrument_Type_Combo_SelectedIndexChanged ;
      // 
      // Add_Instrument_Button
      // 
      Add_Instrument_Button.Location = new Point ( 20, 132 );
      Add_Instrument_Button.Name = "Add_Instrument_Button";
      Add_Instrument_Button.Size = new Size ( 75, 27 );
      Add_Instrument_Button.TabIndex = 6;
      Add_Instrument_Button.Text = "Add";
      Add_Instrument_Button.UseVisualStyleBackColor = true;
      Add_Instrument_Button.Click +=  Add_Instrument_Button_Click ;
      // 
      // Remove_Instrument_Button
      // 
      Remove_Instrument_Button.Location = new Point ( 102, 132 );
      Remove_Instrument_Button.Name = "Remove_Instrument_Button";
      Remove_Instrument_Button.Size = new Size ( 83, 27 );
      Remove_Instrument_Button.TabIndex = 7;
      Remove_Instrument_Button.Text = "Remove";
      Remove_Instrument_Button.UseVisualStyleBackColor = true;
      Remove_Instrument_Button.Click +=  Remove_Instrument_Button_Click ;
      // 
      // Saved_Instruments_Label
      // 
      Saved_Instruments_Label.AutoSize = true;
      Saved_Instruments_Label.Font = new Font ( "Segoe UI", 9F, FontStyle.Bold );
      Saved_Instruments_Label.Location = new Point ( 15, 268 );
      Saved_Instruments_Label.Name = "Saved_Instruments_Label";
      Saved_Instruments_Label.Size = new Size ( 78, 15 );
      Saved_Instruments_Label.TabIndex = 8;
      Saved_Instruments_Label.Text = "Instruments:";
      // 
      // Instruments_List
      // 
      Instruments_List.Anchor =     AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Instruments_List.Location = new Point ( 15, 287 );
      Instruments_List.Name = "Instruments_List";
      Instruments_List.Size = new Size ( 210, 229 );
      Instruments_List.TabIndex = 10;
      Instruments_List.DoubleClick +=  Select_Instrument_Button_Click ;
      // 
      // Select_Instrument_Button
      // 
      Select_Instrument_Button.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Select_Instrument_Button.Location = new Point ( 39, 540 );
      Select_Instrument_Button.Name = "Select_Instrument_Button";
      Select_Instrument_Button.Size = new Size ( 165, 30 );
      Select_Instrument_Button.TabIndex = 11;
      Select_Instrument_Button.Text = "Switch to Selected";
      Select_Instrument_Button.UseVisualStyleBackColor = true;
      Select_Instrument_Button.Click +=  Select_Instrument_Button_Click ;
      // 
      // Multi_Poll_Button
      // 
      Multi_Poll_Button.Location = new Point ( 16, 214 );
      Multi_Poll_Button.Name = "Multi_Poll_Button";
      Multi_Poll_Button.Size = new Size ( 71, 45 );
      Multi_Poll_Button.TabIndex = 13;
      Multi_Poll_Button.Text = "Launch Poller";
      Multi_Poll_Button.UseVisualStyleBackColor = true;
      Multi_Poll_Button.Click +=  Multi_Poll_Button_Click ;
      // 
      // Command_History_List_Box
      // 
      Command_History_List_Box.FormattingEnabled = true;
      Command_History_List_Box.HorizontalScrollbar = true;
      Command_History_List_Box.Location = new Point ( 307, 288 );
      Command_History_List_Box.Name = "Command_History_List_Box";
      Command_History_List_Box.Size = new Size ( 286, 49 );
      Command_History_List_Box.TabIndex = 16;
      Command_History_List_Box.DoubleClick +=  Command_History_ListBox_DoubleClick ;
      // 
      // History_Label
      // 
      History_Label.AutoSize = true;
      History_Label.Font = new Font ( "Segoe UI", 9F, FontStyle.Bold );
      History_Label.Location = new Point ( 251, 288 );
      History_Label.Name = "History_Label";
      History_Label.Size = new Size ( 50, 15 );
      History_Label.TabIndex = 17;
      History_Label.Text = "History:";
      // 
      // Meter_Info_Button
      // 
      Meter_Info_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Meter_Info_Button.Location = new Point ( 25, 439 );
      Meter_Info_Button.Name = "Meter_Info_Button";
      Meter_Info_Button.Size = new Size ( 72, 56 );
      Meter_Info_Button.TabIndex = 18;
      Meter_Info_Button.Text = "Meter Info";
      Meter_Info_Button.UseVisualStyleBackColor = true;
      Meter_Info_Button.Click +=  Meter_Info_Button_Click ;
      // 
      // Settings_Button
      // 
      Settings_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Settings_Button.Location = new Point ( 25, 501 );
      Settings_Button.Name = "Settings_Button";
      Settings_Button.Size = new Size ( 72, 35 );
      Settings_Button.TabIndex = 19;
      Settings_Button.Text = "Settings";
      Settings_Button.UseVisualStyleBackColor = true;
      Settings_Button.Click +=  Settings_Button_Click ;
      // 
      // Reset_Defaults_Button
      // 
      Reset_Defaults_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Reset_Defaults_Button.Location = new Point ( 103, 501 );
      Reset_Defaults_Button.Name = "Reset_Defaults_Button";
      Reset_Defaults_Button.Size = new Size ( 72, 38 );
      Reset_Defaults_Button.TabIndex = 20;
      Reset_Defaults_Button.Text = "Reset Defaults";
      Reset_Defaults_Button.UseVisualStyleBackColor = true;
      // 
      // Button_Show_Execution_Trace
      // 
      Button_Show_Execution_Trace.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Button_Show_Execution_Trace.Location = new Point ( 103, 545 );
      Button_Show_Execution_Trace.Name = "Button_Show_Execution_Trace";
      Button_Show_Execution_Trace.Size = new Size ( 72, 38 );
      Button_Show_Execution_Trace.TabIndex = 21;
      Button_Show_Execution_Trace.Text = "Trace On";
      Button_Show_Execution_Trace.UseVisualStyleBackColor = true;
      Button_Show_Execution_Trace.Click +=  Button_Show_Execution_Trace_Click ;
      // 
      // Session_Settings_Button
      // 
      Session_Settings_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Session_Settings_Button.Location = new Point ( 25, 545 );
      Session_Settings_Button.Name = "Session_Settings_Button";
      Session_Settings_Button.Size = new Size ( 72, 38 );
      Session_Settings_Button.TabIndex = 22;
      Session_Settings_Button.Text = "Session Setings";
      Session_Settings_Button.UseVisualStyleBackColor = true;
      Session_Settings_Button.Click +=  Session_Settings_Button_Click ;
      // 
      // Display_Recording_Button
      // 
      Display_Recording_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Display_Recording_Button.Location = new Point ( 103, 439 );
      Display_Recording_Button.Name = "Display_Recording_Button";
      Display_Recording_Button.Size = new Size ( 72, 56 );
      Display_Recording_Button.TabIndex = 23;
      Display_Recording_Button.Text = "View Recorded Data";
      Display_Recording_Button.UseVisualStyleBackColor = true;
      Display_Recording_Button.Click +=  Display_Recording_Data_Button_Click ;
      // 
      // Form1
      // 
      AutoScaleDimensions = new SizeF ( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size ( 1340, 603 );
      Controls.Add ( Display_Recording_Button );
      Controls.Add ( Session_Settings_Button );
      Controls.Add ( Button_Show_Execution_Trace );
      Controls.Add ( Reset_Defaults_Button );
      Controls.Add ( Settings_Button );
      Controls.Add ( Meter_Info_Button );
      Controls.Add ( History_Label );
      Controls.Add ( Command_History_List_Box );
      Controls.Add ( Title_Label );
      Controls.Add ( Command_List_Label );
      Controls.Add ( Command_List );
      Controls.Add ( Detail_Text_Box );
      Controls.Add ( Send_Command_Label );
      Controls.Add ( Send_Command_Text_Box );
      Controls.Add ( Execute_Button );
      Controls.Add ( Diag_Button );
      Controls.Add ( Response_Label );
      Controls.Add ( Response_Text_Box );
      Controls.Add ( Open_Dictionary_Button );
      Controls.Add ( Instruments_Group );
      Controls.Add ( Connection_Group );
      Name = "Form1";
      StartPosition = FormStartPosition.CenterScreen;
      Text = "Multimeter Controller";
      Connection_Group.ResumeLayout ( false );
      Connection_Group.PerformLayout ( );
      ( (System.ComponentModel.ISupportInitialize) GPIB_Address_Numeric ).EndInit ( );
      Instruments_Group.ResumeLayout ( false );
      Instruments_Group.PerformLayout ( );
      ResumeLayout ( false );
      PerformLayout ( );
    }



    // Command list controls
    private System.Windows.Forms.Label Title_Label;
    private System.Windows.Forms.Label Command_List_Label;
    private System.Windows.Forms.ListBox Command_List;
    private System.Windows.Forms.TextBox Detail_Text_Box;
    private System.Windows.Forms.Button Open_Dictionary_Button;
    private System.Windows.Forms.Label Send_Command_Label;
    private System.Windows.Forms.TextBox Send_Command_Text_Box;
    private System.Windows.Forms.Button Execute_Button;
    private System.Windows.Forms.Label Response_Label;
    private System.Windows.Forms.TextBox Response_Text_Box;

    // Connection controls
    private System.Windows.Forms.Label Connection_Mode_Label;
    private System.Windows.Forms.ComboBox Connection_Mode_Combo;
    private System.Windows.Forms.GroupBox Connection_Group;
    private System.Windows.Forms.Label COM_Port_Label;
    private System.Windows.Forms.ComboBox COM_Port_Combo;
    private System.Windows.Forms.Button Refresh_Ports_Button;
    private System.Windows.Forms.Label Baud_Rate_Label;
    private System.Windows.Forms.ComboBox Baud_Rate_Combo;
    private System.Windows.Forms.Label Data_Bits_Label;
    private System.Windows.Forms.ComboBox Data_Bits_Combo;
    private System.Windows.Forms.Label Parity_Label;
    private System.Windows.Forms.ComboBox Parity_Combo;
    private System.Windows.Forms.Label Stop_Bits_Label;
    private System.Windows.Forms.ComboBox Stop_Bits_Combo;
    private System.Windows.Forms.Label Flow_Control_Label;
    private System.Windows.Forms.ComboBox Flow_Control_Combo;
    private System.Windows.Forms.Label Read_Timeout_Label;
    private System.Windows.Forms.ComboBox Read_Timeout_Combo_Box;
    private System.Windows.Forms.Button Connect_Button;
    private System.Windows.Forms.Button Defaults_Button;
    private System.Windows.Forms.Label Connection_Status_Label;
    private System.Windows.Forms.Label Prologix_Header_Label;
    private System.Windows.Forms.Button Multi_Poll_Button;
    private System.Windows.Forms.Button Diag_Button;

    // Instruments controls
    private System.Windows.Forms.GroupBox Instruments_Group;
    private System.Windows.Forms.Label Instrument_Name_Label;
    private System.Windows.Forms.TextBox Instrument_Name_Text;
    private System.Windows.Forms.Label GPIB_Address_Label;
    private System.Windows.Forms.NumericUpDown
      GPIB_Address_Numeric;
    private System.Windows.Forms.Label Instrument_Type_Label;
    private System.Windows.Forms.ComboBox Instrument_Type_Combo;
    private System.Windows.Forms.Button Add_Instrument_Button;
    private System.Windows.Forms.Button Remove_Instrument_Button;
    private System.Windows.Forms.Label Saved_Instruments_Label;
    private System.Windows.Forms.ListBox Instruments_List;
    private System.Windows.Forms.Button Select_Instrument_Button;
    private System.Windows.Forms.Button Scan_Bus_Button;
    private Label label1;
    private TextBox Connected_Instrument_Textbox;
    private ListBox Command_History_List_Box;
    private Label History_Label;
    private Button Find_Prologix_Button;
    private Label Subnet_Label;
    private TextBox Subnet_Textbox;
    private Label IP_Address_Label;
    private TextBox IP_Address_Textbox;
    private Label NPLC_Label;
    private Button Meter_Info_Button;
    private Button Settings_Button;
    private Button Reset_Defaults_Button;
    private Button Button_Show_Execution_Trace;
    private Button Session_Settings_Button;
    private ComboBox NPLC_Combo_Box;
    private Button Apply_NPLC_To_All_Button;
    private Label Session_Name_Label;
    private TextBox Session_Name_Textbox;
    private Label Meter_Roll_Label;
    private TextBox Roll_Name_Textbox;
    private Button Display_Recording_Button;
    private Button button1;
  }

}
