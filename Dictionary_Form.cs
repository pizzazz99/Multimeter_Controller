
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Dictionary_Form.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   A read-only reference dialog that displays the full GPIB command set for
//   a selected instrument (HP3458A, HP34401, or HP33120) in a searchable,
//   filterable DataGridView.  Intended as an in-app manual — operators can
//   look up command syntax, parameters, and examples without leaving the
//   application.
//
// CONSTRUCTOR
//   Dictionary_Form(Meter_Type)   Loads all Command_Entry records for the
//                                 given meter via Command_Dictionary_Class,
//                                 sets the window title, and populates the
//                                 category filter combo before binding the
//                                 grid.  Defaults to HP3458A if no meter
//                                 type is supplied.
//
// FILTERING
//   Two independent filters are AND-ed together on every change:
//     Category   ComboBox — "All Categories" or any Command_Category value.
//     Search     TextBox  — case-insensitive substring match across
//                           Command, Syntax, Description, and Parameters.
//   Apply_Filter() rebuilds _Filtered_Commands from _All_Commands and
//   updates Status_Label with the current match count.
//
// GRID COLUMNS  (fixed widths set in Bind_Grid)
//   Command      80 px    Short mnemonic (e.g. "NPLC")
//   Syntax      200 px    Full command syntax with parameter tokens
//   Description 280 px    Plain-English explanation
//   Category    100 px    Command_Category enum value
//   Parameters  250 px    Accepted values and ranges
//   Query Form   80 px    "NPLC?" equivalent, if supported
//   Default     120 px    Factory default value
//   Example     140 px    Representative usage string
//
// INTERACTIONS
//   Double-click on any row   Shows a MessageBox with all fields for that
//                             command formatted as a multi-line detail card.
//   Clear button              Resets both the search text and the category
//                             combo to their default states.
//
// DEPENDENCIES
//   Command_Dictionary_Class   Static provider of Command_Entry lists per meter.
//   Command_Entry              POCO with the eight fields bound to the grid.
//   Command_Category           Enum whose values populate the category filter.
//   Meter_Type                 Enum used to select the correct command set.
//
// NOTES
//   • _All_Commands is populated once in the constructor and never modified;
//     _Filtered_Commands is rebuilt from it on every filter change.
//   • Bind_Grid() sets DataSource to null before reassigning to force the
//     DataGridView to fully refresh its row collection.
//   • Column widths are only applied after the first non-empty bind because
//     Columns.Count is 0 until DataSource is set for the first time.
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════




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
