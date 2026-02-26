namespace Multimeter_Controller
{
  partial class Info_Popup
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;



    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent ( )
    {
      Results_Listbox = new ListBox ( );
      SuspendLayout ( );
      // 
      // Results_Listbox
      // 
      Results_Listbox.FormattingEnabled = true;
      Results_Listbox.Location = new Point ( 29, 24 );
      Results_Listbox.Name = "Results_Listbox";
      Results_Listbox.Size = new Size ( 316, 364 );
      Results_Listbox.TabIndex = 0;
      // 
      // Info_Popup
      // 
      ClientSize = new Size ( 375, 421 );
      Controls.Add ( Results_Listbox );
      Name = "Info_Popup";
      ResumeLayout ( false );


    }

    #endregion

    private ListBox Results_Listbox;
  }
}
