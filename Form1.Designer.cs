
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;



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
      Connection_Group = new GroupBox ( );
      Find_Prologic_Button = new Button ( );
      Connection_Mode_Label = new Label ( );
      Connection_Mode_Combo = new ComboBox ( );
      Selected_IP_Address_Label = new Label ( );
      IP_Address_Text = new TextBox ( );
      Button_Info = new Button ( );
      Connected_Instrument_Textbox = new TextBox ( );
      label1 = new Label ( );
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
      GPIB_Address_Label = new Label ( );
      GPIB_Address_Numeric = new NumericUpDown ( );
      Defaults_Button = new Button ( );
      Connect_Button = new Button ( );
      Connection_Status_Label = new Label ( );
      Instrument_Address_Numeric = new NumericUpDown ( );
      Send_Command_Label = new Label ( );
      Send_Command_Text_Box = new TextBox ( );
      Execute_Button = new Button ( );
      Diag_Button = new Button ( );
      Response_Label = new Label ( );
      Response_Text_Box = new TextBox ( );
      Instruments_Group = new GroupBox ( );
      Instrument_Name_Label = new Label ( );
      Instrument_Name_Text = new TextBox ( );
      Instrument_Address_Label = new Label ( );
      Instrument_Type_Label = new Label ( );
      Instrument_Type_Combo = new ComboBox ( );
      Add_Instrument_Button = new Button ( );
      Remove_Instrument_Button = new Button ( );
      Saved_Instruments_Label = new Label ( );
      Scan_Bus_Button = new Button ( );
      Instruments_List = new ListBox ( );
      Select_Instrument_Button = new Button ( );
      Voltage_Reader_Button = new Button ( );
      Multi_Poll_Button = new Button ( );
      Command_History_List_Box = new ListBox ( );
      History_Label = new Label ( );
      Button_Show_Execution_Trace = new Button ( );
      Settings_Button = new Button ( );
      NPLC_Label = new Label ( );
      NPLC_Combo = new ComboBox ( );
      Reset_Defaults_Button = new Button ( );
      label2 = new Label ( );
      Subnet_Textbox = new TextBox ( );
      Connection_Group.SuspendLayout ( );
      ( (System.ComponentModel.ISupportInitialize) GPIB_Address_Numeric ).BeginInit ( );
      ( (System.ComponentModel.ISupportInitialize) Instrument_Address_Numeric ).BeginInit ( );
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
      Command_List.Size = new Size ( 184, 334 );
      Command_List.TabIndex = 2;
      Command_List.SelectedIndexChanged +=  Command_List_Selected_Index_Changed ;
      // 
      // Detail_Text_Box
      // 
      Detail_Text_Box.BackColor = SystemColors.Window;
      Detail_Text_Box.Font = new System.Drawing.Font ( "Consolas", 9.5F );
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
      Open_Dictionary_Button.Location = new Point ( 12, 372 );
      Open_Dictionary_Button.Name = "Open_Dictionary_Button";
      Open_Dictionary_Button.Size = new Size ( 72, 35 );
      Open_Dictionary_Button.TabIndex = 11;
      Open_Dictionary_Button.Text = "Dictionary";
      Open_Dictionary_Button.UseVisualStyleBackColor = true;
      Open_Dictionary_Button.Click +=  Open_Dictionary_Button_Click ;
      // 
      // Connection_Group
      // 
      Connection_Group.Anchor =    AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Right ;
      Connection_Group.Controls.Add ( label2 );
      Connection_Group.Controls.Add ( Subnet_Textbox );
      Connection_Group.Controls.Add ( Find_Prologic_Button );
      Connection_Group.Controls.Add ( Connection_Mode_Label );
      Connection_Group.Controls.Add ( Connection_Mode_Combo );
      Connection_Group.Controls.Add ( Selected_IP_Address_Label );
      Connection_Group.Controls.Add ( IP_Address_Text );
      Connection_Group.Controls.Add ( Button_Info );
      Connection_Group.Controls.Add ( Connected_Instrument_Textbox );
      Connection_Group.Controls.Add ( label1 );
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
      Connection_Group.Controls.Add ( GPIB_Address_Label );
      Connection_Group.Controls.Add ( GPIB_Address_Numeric );
      Connection_Group.Controls.Add ( Defaults_Button );
      Connection_Group.Controls.Add ( Connect_Button );
      Connection_Group.Controls.Add ( Connection_Status_Label );
      Connection_Group.Location = new Point ( 1080, 12 );
      Connection_Group.Name = "Connection_Group";
      Connection_Group.Size = new Size ( 248, 503 );
      Connection_Group.TabIndex = 15;
      Connection_Group.TabStop = false;
      Connection_Group.Text = "Connection Settings";
      // 
      // Find_Prologic_Button
      // 
      Find_Prologic_Button.Location = new Point ( 9, 360 );
      Find_Prologic_Button.Name = "Find_Prologic_Button";
      Find_Prologic_Button.Size = new Size ( 110, 23 );
      Find_Prologic_Button.TabIndex = 32;
      Find_Prologic_Button.Text = "Find Prologic IP";
      Find_Prologic_Button.UseVisualStyleBackColor = true;
      Find_Prologic_Button.Click +=  Find_Prologix_Button_Click ;
      // 
      // Connection_Mode_Label
      // 
      Connection_Mode_Label.AutoSize = true;
      Connection_Mode_Label.Location = new Point ( 13, 29 );
      Connection_Mode_Label.Name = "Connection_Mode_Label";
      Connection_Mode_Label.Size = new Size ( 72, 15 );
      Connection_Mode_Label.TabIndex = 30;
      Connection_Mode_Label.Text = "Connection:";
      // 
      // Connection_Mode_Combo
      // 
      Connection_Mode_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Connection_Mode_Combo.Location = new Point ( 109, 26 );
      Connection_Mode_Combo.Name = "Connection_Mode_Combo";
      Connection_Mode_Combo.Size = new Size ( 130, 23 );
      Connection_Mode_Combo.TabIndex = 31;
      Connection_Mode_Combo.SelectedIndexChanged +=  Connection_Mode_Combo_SelectedIndexChanged ;
      // 
      // Selected_IP_Address_Label
      // 
      Selected_IP_Address_Label.AutoSize = true;
      Selected_IP_Address_Label.Location = new Point ( 35, 241 );
      Selected_IP_Address_Label.Name = "Selected_IP_Address_Label";
      Selected_IP_Address_Label.Size = new Size ( 65, 15 );
      Selected_IP_Address_Label.TabIndex = 29;
      Selected_IP_Address_Label.Text = "IP Address:";
      // 
      // IP_Address_Text
      // 
      IP_Address_Text.Location = new Point ( 110, 236 );
      IP_Address_Text.Name = "IP_Address_Text";
      IP_Address_Text.Size = new Size ( 129, 23 );
      IP_Address_Text.TabIndex = 28;
      IP_Address_Text.TextChanged +=  IP_Address_Text_TextChanged ;
      // 
      // Button_Info
      // 
      Button_Info.Location = new Point ( 13, 420 );
      Button_Info.Name = "Button_Info";
      Button_Info.Size = new Size ( 75, 23 );
      Button_Info.TabIndex = 27;
      Button_Info.Text = "Info";
      Button_Info.UseVisualStyleBackColor = true;
      Button_Info.Click +=  Button_Info_Click ;
      // 
      // Connected_Instrument_Textbox
      // 
      Connected_Instrument_Textbox.Location = new Point ( 110, 299 );
      Connected_Instrument_Textbox.Name = "Connected_Instrument_Textbox";
      Connected_Instrument_Textbox.Size = new Size ( 129, 23 );
      Connected_Instrument_Textbox.TabIndex = 26;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point ( 9, 302 );
      label1.Name = "label1";
      label1.Size = new Size ( 84, 15 );
      label1.TabIndex = 25;
      label1.Text = "Connected To:";
      // 
      // COM_Port_Label
      // 
      COM_Port_Label.AutoSize = true;
      COM_Port_Label.Location = new Point ( 22, 59 );
      COM_Port_Label.Name = "COM_Port_Label";
      COM_Port_Label.Size = new Size ( 63, 15 );
      COM_Port_Label.TabIndex = 0;
      COM_Port_Label.Text = "COM Port:";
      // 
      // COM_Port_Combo
      // 
      COM_Port_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      COM_Port_Combo.Location = new Point ( 110, 55 );
      COM_Port_Combo.Name = "COM_Port_Combo";
      COM_Port_Combo.Size = new Size ( 95, 23 );
      COM_Port_Combo.TabIndex = 1;
      // 
      // Refresh_Ports_Button
      // 
      Refresh_Ports_Button.Location = new Point ( 210, 54 );
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
      Baud_Rate_Label.Location = new Point ( 22, 85 );
      Baud_Rate_Label.Name = "Baud_Rate_Label";
      Baud_Rate_Label.Size = new Size ( 63, 15 );
      Baud_Rate_Label.TabIndex = 3;
      Baud_Rate_Label.Text = "Baud Rate:";
      // 
      // Baud_Rate_Combo
      // 
      Baud_Rate_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Baud_Rate_Combo.Location = new Point ( 110, 84 );
      Baud_Rate_Combo.Name = "Baud_Rate_Combo";
      Baud_Rate_Combo.Size = new Size ( 130, 23 );
      Baud_Rate_Combo.TabIndex = 4;
      // 
      // Data_Bits_Label
      // 
      Data_Bits_Label.AutoSize = true;
      Data_Bits_Label.Location = new Point ( 22, 116 );
      Data_Bits_Label.Name = "Data_Bits_Label";
      Data_Bits_Label.Size = new Size ( 56, 15 );
      Data_Bits_Label.TabIndex = 5;
      Data_Bits_Label.Text = "Data Bits:";
      // 
      // Data_Bits_Combo
      // 
      Data_Bits_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Data_Bits_Combo.Location = new Point ( 110, 113 );
      Data_Bits_Combo.Name = "Data_Bits_Combo";
      Data_Bits_Combo.Size = new Size ( 130, 23 );
      Data_Bits_Combo.TabIndex = 6;
      // 
      // Parity_Label
      // 
      Parity_Label.AutoSize = true;
      Parity_Label.Location = new Point ( 22, 146 );
      Parity_Label.Name = "Parity_Label";
      Parity_Label.Size = new Size ( 40, 15 );
      Parity_Label.TabIndex = 7;
      Parity_Label.Text = "Parity:";
      // 
      // Parity_Combo
      // 
      Parity_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Parity_Combo.Location = new Point ( 110, 143 );
      Parity_Combo.Name = "Parity_Combo";
      Parity_Combo.Size = new Size ( 130, 23 );
      Parity_Combo.TabIndex = 8;
      // 
      // Stop_Bits_Label
      // 
      Stop_Bits_Label.AutoSize = true;
      Stop_Bits_Label.Location = new Point ( 22, 176 );
      Stop_Bits_Label.Name = "Stop_Bits_Label";
      Stop_Bits_Label.Size = new Size ( 56, 15 );
      Stop_Bits_Label.TabIndex = 9;
      Stop_Bits_Label.Text = "Stop Bits:";
      // 
      // Stop_Bits_Combo
      // 
      Stop_Bits_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Stop_Bits_Combo.Location = new Point ( 110, 173 );
      Stop_Bits_Combo.Name = "Stop_Bits_Combo";
      Stop_Bits_Combo.Size = new Size ( 130, 23 );
      Stop_Bits_Combo.TabIndex = 10;
      // 
      // Flow_Control_Label
      // 
      Flow_Control_Label.AutoSize = true;
      Flow_Control_Label.Location = new Point ( 22, 206 );
      Flow_Control_Label.Name = "Flow_Control_Label";
      Flow_Control_Label.Size = new Size ( 78, 15 );
      Flow_Control_Label.TabIndex = 11;
      Flow_Control_Label.Text = "Flow Control:";
      // 
      // Flow_Control_Combo
      // 
      Flow_Control_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      Flow_Control_Combo.Location = new Point ( 110, 203 );
      Flow_Control_Combo.Name = "Flow_Control_Combo";
      Flow_Control_Combo.Size = new Size ( 130, 23 );
      Flow_Control_Combo.TabIndex = 12;
      // 
      // Prologix_Header_Label
      // 
      Prologix_Header_Label.AutoSize = true;
      Prologix_Header_Label.Font = new System.Drawing.Font ( "Segoe UI", 9F, FontStyle.Bold );
      Prologix_Header_Label.Location = new Point ( 9, 342 );
      Prologix_Header_Label.Name = "Prologix_Header_Label";
      Prologix_Header_Label.Size = new Size ( 102, 15 );
      Prologix_Header_Label.TabIndex = 15;
      Prologix_Header_Label.Text = "Prologix Settings";
      // 
      // GPIB_Address_Label
      // 
      GPIB_Address_Label.AutoSize = true;
      GPIB_Address_Label.Location = new Point ( 9, 394 );
      GPIB_Address_Label.Name = "GPIB_Address_Label";
      GPIB_Address_Label.Size = new Size ( 80, 15 );
      GPIB_Address_Label.TabIndex = 16;
      GPIB_Address_Label.Text = "GPIB Address:";
      // 
      // GPIB_Address_Numeric
      // 
      GPIB_Address_Numeric.Location = new Point ( 109, 391 );
      GPIB_Address_Numeric.Name = "GPIB_Address_Numeric";
      GPIB_Address_Numeric.Size = new Size ( 60, 23 );
      GPIB_Address_Numeric.TabIndex = 17;
      // 
      // Defaults_Button
      // 
      Defaults_Button.Location = new Point ( 10, 445 );
      Defaults_Button.Name = "Defaults_Button";
      Defaults_Button.Size = new Size ( 90, 30 );
      Defaults_Button.TabIndex = 20;
      Defaults_Button.Text = "Defaults";
      Defaults_Button.UseVisualStyleBackColor = true;
      Defaults_Button.Click +=  Defaults_Button_Click ;
      // 
      // Connect_Button
      // 
      Connect_Button.Location = new Point ( 122, 445 );
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
      Connection_Status_Label.Font = new System.Drawing.Font ( "Segoe UI", 9F, FontStyle.Bold );
      Connection_Status_Label.ForeColor = Color.Red;
      Connection_Status_Label.Location = new Point ( 122, 478 );
      Connection_Status_Label.Name = "Connection_Status_Label";
      Connection_Status_Label.Size = new Size ( 83, 15 );
      Connection_Status_Label.TabIndex = 22;
      Connection_Status_Label.Text = "Disconnected";
      // 
      // Instrument_Address_Numeric
      // 
      Instrument_Address_Numeric.Location = new Point ( 85, 77 );
      Instrument_Address_Numeric.Maximum = new decimal ( new int [ ] { 30, 0, 0, 0 } );
      Instrument_Address_Numeric.Name = "Instrument_Address_Numeric";
      Instrument_Address_Numeric.Size = new Size ( 60, 23 );
      Instrument_Address_Numeric.TabIndex = 3;
      Instrument_Address_Numeric.Value = new decimal ( new int [ ] { 22, 0, 0, 0 } );
      // 
      // Send_Command_Label
      // 
      Send_Command_Label.AutoSize = true;
      Send_Command_Label.Font = new System.Drawing.Font ( "Segoe UI", 9F, FontStyle.Bold );
      Send_Command_Label.Location = new Point ( 204, 248 );
      Send_Command_Label.Name = "Send_Command_Label";
      Send_Command_Label.Size = new Size ( 97, 15 );
      Send_Command_Label.TabIndex = 4;
      Send_Command_Label.Text = "Send Command:";
      // 
      // Send_Command_Text_Box
      // 
      Send_Command_Text_Box.Font = new System.Drawing.Font ( "Consolas", 10F );
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
      Response_Label.Font = new System.Drawing.Font ( "Segoe UI", 9F, FontStyle.Bold );
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
      Response_Text_Box.Font = new System.Drawing.Font ( "Consolas", 9.5F );
      Response_Text_Box.Location = new Point ( 202, 362 );
      Response_Text_Box.Multiline = true;
      Response_Text_Box.Name = "Response_Text_Box";
      Response_Text_Box.ReadOnly = true;
      Response_Text_Box.ScrollBars = ScrollBars.Vertical;
      Response_Text_Box.Size = new Size ( 622, 147 );
      Response_Text_Box.TabIndex = 10;
      // 
      // Instruments_Group
      // 
      Instruments_Group.Anchor =    AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Right ;
      Instruments_Group.Controls.Add ( Instrument_Name_Label );
      Instruments_Group.Controls.Add ( Instrument_Name_Text );
      Instruments_Group.Controls.Add ( Instrument_Address_Label );
      Instruments_Group.Controls.Add ( Instrument_Address_Numeric );
      Instruments_Group.Controls.Add ( Instrument_Type_Label );
      Instruments_Group.Controls.Add ( Instrument_Type_Combo );
      Instruments_Group.Controls.Add ( Add_Instrument_Button );
      Instruments_Group.Controls.Add ( Remove_Instrument_Button );
      Instruments_Group.Controls.Add ( Saved_Instruments_Label );
      Instruments_Group.Controls.Add ( Scan_Bus_Button );
      Instruments_Group.Controls.Add ( Instruments_List );
      Instruments_Group.Controls.Add ( Select_Instrument_Button );
      Instruments_Group.Location = new Point ( 830, 12 );
      Instruments_Group.Name = "Instruments_Group";
      Instruments_Group.Size = new Size ( 240, 503 );
      Instruments_Group.TabIndex = 14;
      Instruments_Group.TabStop = false;
      Instruments_Group.Text = "GPIB Instruments";
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
      Instrument_Name_Text.Location = new Point ( 85, 16 );
      Instrument_Name_Text.Name = "Instrument_Name_Text";
      Instrument_Name_Text.Size = new Size ( 146, 23 );
      Instrument_Name_Text.TabIndex = 1;
      // 
      // Instrument_Address_Label
      // 
      Instrument_Address_Label.AutoSize = true;
      Instrument_Address_Label.Location = new Point ( 15, 80 );
      Instrument_Address_Label.Name = "Instrument_Address_Label";
      Instrument_Address_Label.Size = new Size ( 52, 15 );
      Instrument_Address_Label.TabIndex = 2;
      Instrument_Address_Label.Text = "Address:";
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
      Instrument_Type_Combo.Location = new Point ( 85, 45 );
      Instrument_Type_Combo.Name = "Instrument_Type_Combo";
      Instrument_Type_Combo.Size = new Size ( 146, 23 );
      Instrument_Type_Combo.TabIndex = 5;
      Instrument_Type_Combo.SelectedIndexChanged +=  Instrument_Type_Combo_SelectedIndexChanged ;
      // 
      // Add_Instrument_Button
      // 
      Add_Instrument_Button.Location = new Point ( 60, 115 );
      Add_Instrument_Button.Name = "Add_Instrument_Button";
      Add_Instrument_Button.Size = new Size ( 75, 27 );
      Add_Instrument_Button.TabIndex = 6;
      Add_Instrument_Button.Text = "Add";
      Add_Instrument_Button.UseVisualStyleBackColor = true;
      Add_Instrument_Button.Click +=  Add_Instrument_Button_Click ;
      // 
      // Remove_Instrument_Button
      // 
      Remove_Instrument_Button.Location = new Point ( 142, 115 );
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
      Saved_Instruments_Label.Font = new System.Drawing.Font ( "Segoe UI", 9F, FontStyle.Bold );
      Saved_Instruments_Label.Location = new Point ( 39, 194 );
      Saved_Instruments_Label.Name = "Saved_Instruments_Label";
      Saved_Instruments_Label.Size = new Size ( 152, 15 );
      Saved_Instruments_Label.TabIndex = 8;
      Saved_Instruments_Label.Text = "Instruments for Multi-Poll";
      // 
      // Scan_Bus_Button
      // 
      Scan_Bus_Button.Enabled = false;
      Scan_Bus_Button.Location = new Point ( 142, 148 );
      Scan_Bus_Button.Name = "Scan_Bus_Button";
      Scan_Bus_Button.Size = new Size ( 80, 25 );
      Scan_Bus_Button.TabIndex = 9;
      Scan_Bus_Button.Text = "Scan Bus";
      Scan_Bus_Button.UseVisualStyleBackColor = true;
      Scan_Bus_Button.Click +=  Scan_Bus_Button_Click ;
      // 
      // Instruments_List
      // 
      Instruments_List.Anchor =     AnchorStyles.Top  |  AnchorStyles.Bottom   |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Instruments_List.Location = new Point ( 15, 212 );
      Instruments_List.Name = "Instruments_List";
      Instruments_List.Size = new Size ( 210, 244 );
      Instruments_List.TabIndex = 10;
      Instruments_List.DoubleClick +=  Select_Instrument_Button_Click ;
      // 
      // Select_Instrument_Button
      // 
      Select_Instrument_Button.Anchor =    AnchorStyles.Bottom  |  AnchorStyles.Left   |  AnchorStyles.Right ;
      Select_Instrument_Button.Location = new Point ( 39, 467 );
      Select_Instrument_Button.Name = "Select_Instrument_Button";
      Select_Instrument_Button.Size = new Size ( 165, 30 );
      Select_Instrument_Button.TabIndex = 11;
      Select_Instrument_Button.Text = "Switch to Selected";
      Select_Instrument_Button.UseVisualStyleBackColor = true;
      Select_Instrument_Button.Click +=  Select_Instrument_Button_Click ;
      // 
      // Voltage_Reader_Button
      // 
      Voltage_Reader_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Voltage_Reader_Button.Location = new Point ( 12, 413 );
      Voltage_Reader_Button.Name = "Voltage_Reader_Button";
      Voltage_Reader_Button.Size = new Size ( 72, 35 );
      Voltage_Reader_Button.TabIndex = 12;
      Voltage_Reader_Button.Text = "Single Poll";
      Voltage_Reader_Button.UseVisualStyleBackColor = true;
      Voltage_Reader_Button.Click +=  Single_Poll_Button_Click ;
      // 
      // Multi_Poll_Button
      // 
      Multi_Poll_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Multi_Poll_Button.Location = new Point ( 90, 413 );
      Multi_Poll_Button.Name = "Multi_Poll_Button";
      Multi_Poll_Button.Size = new Size ( 71, 35 );
      Multi_Poll_Button.TabIndex = 13;
      Multi_Poll_Button.Text = "Multi-Poll";
      Multi_Poll_Button.UseVisualStyleBackColor = true;
      Multi_Poll_Button.Click +=  Multi_Poll_Button_Click ;
      // 
      // Command_History_List_Box
      // 
      Command_History_List_Box.FormattingEnabled = true;
      Command_History_List_Box.HorizontalScrollbar = true;
      Command_History_List_Box.Location = new Point ( 307, 288 );
      Command_History_List_Box.Name = "Command_History_List_Box";
      Command_History_List_Box.Size = new Size ( 200, 49 );
      Command_History_List_Box.TabIndex = 16;
      Command_History_List_Box.DoubleClick +=  Command_History_ListBox_DoubleClick ;
      // 
      // History_Label
      // 
      History_Label.AutoSize = true;
      History_Label.Font = new System.Drawing.Font ( "Segoe UI", 9F, FontStyle.Bold );
      History_Label.Location = new Point ( 251, 288 );
      History_Label.Name = "History_Label";
      History_Label.Size = new Size ( 50, 15 );
      History_Label.TabIndex = 17;
      History_Label.Text = "History:";
      // 
      // Button_Show_Execution_Trace
      // 
      Button_Show_Execution_Trace.Location = new Point ( 603, 288 );
      Button_Show_Execution_Trace.Name = "Button_Show_Execution_Trace";
      Button_Show_Execution_Trace.Size = new Size ( 80, 26 );
      Button_Show_Execution_Trace.TabIndex = 19;
      Button_Show_Execution_Trace.Text = "Trace";
      Button_Show_Execution_Trace.UseVisualStyleBackColor = true;
      Button_Show_Execution_Trace.Click +=  Button_Show_Execution_Trace_Click ;
      // 
      // Settings_Button
      // 
      Settings_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Settings_Button.Location = new Point ( 13, 473 );
      Settings_Button.Name = "Settings_Button";
      Settings_Button.Size = new Size ( 71, 42 );
      Settings_Button.TabIndex = 20;
      Settings_Button.Text = "Settings";
      Settings_Button.UseVisualStyleBackColor = true;
      Settings_Button.Click +=  Settings_Button_Click ;
      // 
      // NPLC_Label
      // 
      NPLC_Label.AutoSize = true;
      NPLC_Label.Location = new Point ( 691, 319 );
      NPLC_Label.Name = "NPLC_Label";
      NPLC_Label.Size = new Size ( 40, 15 );
      NPLC_Label.TabIndex = 32;
      NPLC_Label.Text = "NPLC:";
      // 
      // NPLC_Combo
      // 
      NPLC_Combo.DropDownStyle = ComboBoxStyle.DropDownList;
      NPLC_Combo.FormattingEnabled = true;
      NPLC_Combo.Items.AddRange ( new object [ ] { "0.02", "0.2", "1", "10", "100" } );
      NPLC_Combo.Location = new Point ( 739, 316 );
      NPLC_Combo.Name = "NPLC_Combo";
      NPLC_Combo.Size = new Size ( 70, 23 );
      NPLC_Combo.TabIndex = 33;
      NPLC_Combo.SelectedIndexChanged +=  NPLC_Combo_SelectedIndexChanged ;
      // 
      // Reset_Defaults_Button
      // 
      Reset_Defaults_Button.Anchor =   AnchorStyles.Bottom  |  AnchorStyles.Left ;
      Reset_Defaults_Button.Location = new Point ( 90, 473 );
      Reset_Defaults_Button.Name = "Reset_Defaults_Button";
      Reset_Defaults_Button.Size = new Size ( 71, 42 );
      Reset_Defaults_Button.TabIndex = 34;
      Reset_Defaults_Button.Text = "Reset\r\nDefaults";
      Reset_Defaults_Button.UseVisualStyleBackColor = true;
      Reset_Defaults_Button.Click +=  Reset_Defaults_Button_Click ;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point ( 34, 275 );
      label2.Name = "label2";
      label2.Size = new Size ( 47, 15 );
      label2.TabIndex = 34;
      label2.Text = "Subnet:";
      // 
      // Subnet_Textbox
      // 
      Subnet_Textbox.Location = new Point ( 109, 270 );
      Subnet_Textbox.Name = "Subnet_Textbox";
      Subnet_Textbox.Size = new Size ( 129, 23 );
      Subnet_Textbox.TabIndex = 33;
      Subnet_Textbox.TextChanged +=  Subnet_Textbox_TextChanged;
      // 
      // Form1
      // 
      AutoScaleDimensions = new SizeF ( 7F, 15F );
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size ( 1340, 530 );
      Controls.Add ( Reset_Defaults_Button );
      Controls.Add ( NPLC_Label );
      Controls.Add ( NPLC_Combo );
      Controls.Add ( Settings_Button );
      Controls.Add ( Button_Show_Execution_Trace );
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
      Controls.Add ( Voltage_Reader_Button );
      Controls.Add ( Multi_Poll_Button );
      Controls.Add ( Instruments_Group );
      Controls.Add ( Connection_Group );
      Name = "Form1";
      StartPosition = FormStartPosition.CenterScreen;
      Text = "Multimeter Controller";
      Connection_Group.ResumeLayout ( false );
      Connection_Group.PerformLayout ( );
      ( (System.ComponentModel.ISupportInitialize) GPIB_Address_Numeric ).EndInit ( );
      ( (System.ComponentModel.ISupportInitialize) Instrument_Address_Numeric ).EndInit ( );
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
    private System.Windows.Forms.Label GPIB_Address_Label;
    private System.Windows.Forms.NumericUpDown GPIB_Address_Numeric;
    private System.Windows.Forms.Button Connect_Button;
    private System.Windows.Forms.Button Defaults_Button;
    private System.Windows.Forms.Label Connection_Status_Label;
    private System.Windows.Forms.Label Prologix_Header_Label;
    private System.Windows.Forms.Button Voltage_Reader_Button;
    private System.Windows.Forms.Button Multi_Poll_Button;
    private System.Windows.Forms.Button Diag_Button;

    // Instruments controls
    private System.Windows.Forms.GroupBox Instruments_Group;
    private System.Windows.Forms.Label Instrument_Name_Label;
    private System.Windows.Forms.TextBox Instrument_Name_Text;
    private System.Windows.Forms.Label Instrument_Address_Label;
    private System.Windows.Forms.NumericUpDown
      Instrument_Address_Numeric;
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
    private Button Button_Show_Execution_Trace;
    private Button Settings_Button;
    private Button Button_Info;
    private Label Selected_IP_Address_Label;
    private TextBox IP_Address_Text;
    private Label NPLC_Label;
    private ComboBox NPLC_Combo;
    private Label Connection_Mode_Label;
    private ComboBox Connection_Mode_Combo;
    private Button Find_Prologic_Button;
    private Button Reset_Defaults_Button;
    private Label label2;
    private TextBox Subnet_Textbox;
  }
}

