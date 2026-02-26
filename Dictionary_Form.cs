namespace Multimeter_Controller
{
  public partial class Dictionary_Form : Form
  {
    private readonly List<Command_Entry> _All_Commands;
    private List<Command_Entry> _Filtered_Commands;

    public Dictionary_Form (
      Meter_Type Meter = Meter_Type.HP3458 )
    {
      InitializeComponent ( );

      string Meter_Name = Meter switch
      {
        Meter_Type.HP34401 => "HP34401",
        Meter_Type.HP33120 => "HP33120",
        _ => "HP3458"
      };

      Text = $"{Meter_Name} - Command Dictionary";
      Title_Label.Text = $"{Meter_Name} Command Dictionary";

      _All_Commands = Command_Dictionary_Class.Get_All_Commands ( Meter );
      _Filtered_Commands = new List<Command_Entry> ( _All_Commands );
      Populate_Category_Filter ( );
      Bind_Grid ( );
    }

    private void Populate_Category_Filter ( )
    {
      Category_Filter_Combo.Items.Clear ( );
      Category_Filter_Combo.Items.Add ( "All Categories" );
      foreach ( Command_Category Category in Enum.GetValues (
        typeof ( Command_Category ) ) )
      {
        Category_Filter_Combo.Items.Add ( Category.ToString ( ) );
      }
      Category_Filter_Combo.SelectedIndex = 0;
    }

    private void Bind_Grid ( )
    {
      Command_Grid.DataSource = null;
      Command_Grid.DataSource = _Filtered_Commands;

      if ( Command_Grid.Columns.Count == 0 )
      {
        return;
      }

      Command_Grid.Columns [ "Command" ]!.Width = 80;
      Command_Grid.Columns [ "Syntax" ]!.Width = 200;
      Command_Grid.Columns [ "Description" ]!.Width = 280;
      Command_Grid.Columns [ "Category" ]!.Width = 100;
      Command_Grid.Columns [ "Parameters" ]!.Width = 250;
      Command_Grid.Columns [ "Query_Form" ]!.Width = 80;
      Command_Grid.Columns [ "Default_Value" ]!.Width = 120;
      Command_Grid.Columns [ "Example" ]!.Width = 140;

      Command_Grid.Columns [ "Query_Form" ]!.HeaderText = "Query Form";
      Command_Grid.Columns [ "Default_Value" ]!.HeaderText = "Default";
    }

    private void Apply_Filter ( )
    {
      string Search_Text =
        Search_Box.Text.Trim ( ).ToUpperInvariant ( );
      string Selected_Category =
        Category_Filter_Combo.SelectedItem?.ToString ( )
        ?? "All Categories";

      _Filtered_Commands = _All_Commands.Where ( Cmd =>
      {
        bool Category_Match =
          Selected_Category == "All Categories"
          || Cmd.Category.ToString ( ) == Selected_Category;

        if ( !Category_Match )
        {
          return false;
        }

        if ( string.IsNullOrEmpty ( Search_Text ) )
        {
          return true;
        }

        return Cmd.Command.ToUpperInvariant ( ).Contains ( Search_Text )
          || Cmd.Syntax.ToUpperInvariant ( ).Contains ( Search_Text )
          || Cmd.Description.ToUpperInvariant ( ).Contains (
            Search_Text )
          || Cmd.Parameters.ToUpperInvariant ( ).Contains (
            Search_Text );
      } ).ToList ( );

      Status_Label.Text =
        $"{_Filtered_Commands.Count} of {_All_Commands.Count} commands";
      Bind_Grid ( );
    }

    private void Search_Box_Text_Changed (
      object Sender, EventArgs E )
    {
      Apply_Filter ( );
    }

    private void Category_Filter_Combo_Selected_Index_Changed (
      object Sender, EventArgs E )
    {
      Apply_Filter ( );
    }

    private void Clear_Button_Click (
      object Sender, EventArgs E )
    {
      Search_Box.Text = "";
      Category_Filter_Combo.SelectedIndex = 0;
      Apply_Filter ( );
    }

    private void Command_Grid_Cell_Double_Click (
      object Sender, DataGridViewCellEventArgs E )
    {
      if ( E.RowIndex < 0
        || E.RowIndex >= _Filtered_Commands.Count )
      {
        return;
      }

      Command_Entry Selected_Command =
        _Filtered_Commands [ E.RowIndex ];
      string Detail_Text =
        $"Command: {Selected_Command.Command}\r\n" +
        $"Syntax: {Selected_Command.Syntax}\r\n" +
        $"Description: {Selected_Command.Description}\r\n" +
        $"Category: {Selected_Command.Category}\r\n" +
        $"Parameters: {Selected_Command.Parameters}\r\n" +
        $"Query Form: {Selected_Command.Query_Form}\r\n" +
        $"Default: {Selected_Command.Default_Value}\r\n" +
        $"Example: {Selected_Command.Example}";

      MessageBox.Show (
        Detail_Text,
        $"Command Details - {Selected_Command.Command}",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information );
    }
  }
}
