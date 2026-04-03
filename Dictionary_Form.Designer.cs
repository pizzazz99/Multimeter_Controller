namespace Multimeter_Controller
{
  partial class Dictionary_Form
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

    private void InitializeComponent ()
    {
      components = new System.ComponentModel.Container ();

      Command_Grid = new System.Windows.Forms.DataGridView ();
      Search_Box = new System.Windows.Forms.TextBox ();
      Category_Filter_Combo = new System.Windows.Forms.ComboBox ();
      Clear_Button = new System.Windows.Forms.Button ();
      Status_Label = new System.Windows.Forms.Label ();
      Search_Label = new System.Windows.Forms.Label ();
      Category_Label = new System.Windows.Forms.Label ();
      Top_Panel = new System.Windows.Forms.Panel ();
      Title_Label = new System.Windows.Forms.Label ();

      ( (System.ComponentModel.ISupportInitialize)
        Command_Grid ).BeginInit ();
      Top_Panel.SuspendLayout ();
      SuspendLayout ();

      // Title_Label
      Title_Label.AutoSize = true;
      Title_Label.Font = new System.Drawing.Font (
        "Segoe UI", 14F, System.Drawing.FontStyle.Bold );
      Title_Label.Location = new System.Drawing.Point ( 12, 8 );
      Title_Label.Name = "Title_Label";
      Title_Label.Size = new System.Drawing.Size ( 450, 25 );
      Title_Label.Text = "";

      // Search_Label
      Search_Label.AutoSize = true;
      Search_Label.Location = new System.Drawing.Point ( 12, 48 );
      Search_Label.Name = "Search_Label";
      Search_Label.Text = "Search:";

      // Search_Box
      Search_Box.Location = new System.Drawing.Point ( 70, 45 );
      Search_Box.Name = "Search_Box";
      Search_Box.Size = new System.Drawing.Size ( 250, 23 );
      Search_Box.PlaceholderText = "Type to filter commands...";
      Search_Box.TextChanged +=
        new System.EventHandler ( Search_Box_Text_Changed );

      // Category_Label
      Category_Label.AutoSize = true;
      Category_Label.Location =
        new System.Drawing.Point ( 340, 48 );
      Category_Label.Name = "Category_Label";
      Category_Label.Text = "Category:";

      // Category_Filter_Combo
      Category_Filter_Combo.DropDownStyle =
        System.Windows.Forms.ComboBoxStyle.DropDownList;
      Category_Filter_Combo.Location =
        new System.Drawing.Point ( 410, 45 );
      Category_Filter_Combo.Name = "Category_Filter_Combo";
      Category_Filter_Combo.Size =
        new System.Drawing.Size ( 180, 23 );
      Category_Filter_Combo.SelectedIndexChanged +=
        new System.EventHandler (
          Category_Filter_Combo_Selected_Index_Changed );

      // Clear_Button
      Clear_Button.Location =
        new System.Drawing.Point ( 610, 44 );
      Clear_Button.Name = "Clear_Button";
      Clear_Button.Size = new System.Drawing.Size ( 75, 25 );
      Clear_Button.Text = "Clear";
      Clear_Button.UseVisualStyleBackColor = true;
      Clear_Button.Click +=
        new System.EventHandler ( Clear_Button_Click );

      // Status_Label
      Status_Label.AutoSize = true;
      Status_Label.Location =
        new System.Drawing.Point ( 700, 48 );
      Status_Label.Name = "Status_Label";
      Status_Label.Text = "";

      // Top_Panel
      Top_Panel.Controls.Add ( Title_Label );
      Top_Panel.Controls.Add ( Search_Label );
      Top_Panel.Controls.Add ( Search_Box );
      Top_Panel.Controls.Add ( Category_Label );
      Top_Panel.Controls.Add ( Category_Filter_Combo );
      Top_Panel.Controls.Add ( Clear_Button );
      Top_Panel.Controls.Add ( Status_Label );
      Top_Panel.Dock = System.Windows.Forms.DockStyle.Top;
      Top_Panel.Location = new System.Drawing.Point ( 0, 0 );
      Top_Panel.Name = "Top_Panel";
      Top_Panel.Size = new System.Drawing.Size ( 1200, 80 );

      // Command_Grid
      Command_Grid.AllowUserToAddRows = false;
      Command_Grid.AllowUserToDeleteRows = false;
      Command_Grid.AllowUserToOrderColumns = true;
      Command_Grid.AutoSizeRowsMode =
        System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
      Command_Grid.BackgroundColor =
        System.Drawing.SystemColors.Window;
      Command_Grid.ColumnHeadersHeightSizeMode =
        System.Windows.Forms
          .DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      Command_Grid.Dock = System.Windows.Forms.DockStyle.Fill;
      Command_Grid.Location =
        new System.Drawing.Point ( 0, 80 );
      Command_Grid.Name = "Command_Grid";
      Command_Grid.ReadOnly = true;
      Command_Grid.RowHeadersVisible = false;
      Command_Grid.SelectionMode =
        System.Windows.Forms.DataGridViewSelectionMode
          .FullRowSelect;
      Command_Grid.DefaultCellStyle.WrapMode =
        System.Windows.Forms.DataGridViewTriState.True;
      Command_Grid.CellDoubleClick +=
        new System.Windows.Forms.DataGridViewCellEventHandler (
          Command_Grid_Cell_Double_Click );

      // Dictionary_Form
      AutoScaleDimensions =
        new System.Drawing.SizeF ( 7F, 15F );
      AutoScaleMode =
        System.Windows.Forms.AutoScaleMode.Font;
      ClientSize = new System.Drawing.Size ( 1200, 700 );
      Controls.Add ( Command_Grid );
      Controls.Add ( Top_Panel );
      Name = "Dictionary_Form";
      Text = "";
      StartPosition =
        System.Windows.Forms.FormStartPosition.CenterParent;

      ( (System.ComponentModel.ISupportInitialize)
        Command_Grid ).EndInit ();
      Top_Panel.ResumeLayout ( false );
      Top_Panel.PerformLayout ();
      ResumeLayout ( false );
    }

    #endregion

    private System.Windows.Forms.DataGridView Command_Grid;
    private System.Windows.Forms.TextBox Search_Box;
    private System.Windows.Forms.ComboBox Category_Filter_Combo;
    private System.Windows.Forms.Button Clear_Button;
    private System.Windows.Forms.Label Status_Label;
    private System.Windows.Forms.Label Search_Label;
    private System.Windows.Forms.Label Category_Label;
    private System.Windows.Forms.Panel Top_Panel;
    private System.Windows.Forms.Label Title_Label;
  }
}
