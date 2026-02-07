namespace Multimeter_Controller
{
  public partial class Theme_Settings_Form : Form
  {
    private Chart_Theme _Working;

    public Chart_Theme Result { get; private set; } = null!;

    public Theme_Settings_Form ( Chart_Theme Current )
    {
      InitializeComponent ( );

      _Working = new Chart_Theme ( );
      _Working.Copy_From ( Current );

      Update_Swatches ( );
    }

    private void Update_Swatches ( )
    {
      Bg_Swatch.BackColor = _Working.Background;
      Grid_Swatch.BackColor = _Working.Grid;
      Labels_Swatch.BackColor = _Working.Labels;
      Separator_Swatch.BackColor = _Working.Separator;
      Line1_Swatch.BackColor = _Working.Line_Colors [ 0 ];
      Line2_Swatch.BackColor = _Working.Line_Colors [ 1 ];
      Line3_Swatch.BackColor = _Working.Line_Colors [ 2 ];
      Line4_Swatch.BackColor = _Working.Line_Colors [ 3 ];
    }

    private Color Pick_Color ( Color Current )
    {
      using var Dlg = new ColorDialog ( );
      Dlg.Color = Current;
      Dlg.FullOpen = true;
      Dlg.AnyColor = true;

      if ( Dlg.ShowDialog ( this ) == DialogResult.OK )
      {
        return Dlg.Color;
      }

      return Current;
    }

    private void Bg_Swatch_Click (
      object? Sender, EventArgs E )
    {
      _Working.Background = Pick_Color (
        _Working.Background );
      Bg_Swatch.BackColor = _Working.Background;
    }

    private void Grid_Swatch_Click (
      object? Sender, EventArgs E )
    {
      _Working.Grid = Pick_Color ( _Working.Grid );
      Grid_Swatch.BackColor = _Working.Grid;
    }

    private void Labels_Swatch_Click (
      object? Sender, EventArgs E )
    {
      _Working.Labels = Pick_Color ( _Working.Labels );
      Labels_Swatch.BackColor = _Working.Labels;
    }

    private void Separator_Swatch_Click (
      object? Sender, EventArgs E )
    {
      _Working.Separator = Pick_Color (
        _Working.Separator );
      Separator_Swatch.BackColor = _Working.Separator;
    }

    private void Line1_Swatch_Click (
      object? Sender, EventArgs E )
    {
      _Working.Line_Colors [ 0 ] = Pick_Color (
        _Working.Line_Colors [ 0 ] );
      Line1_Swatch.BackColor =
        _Working.Line_Colors [ 0 ];
    }

    private void Line2_Swatch_Click (
      object? Sender, EventArgs E )
    {
      _Working.Line_Colors [ 1 ] = Pick_Color (
        _Working.Line_Colors [ 1 ] );
      Line2_Swatch.BackColor =
        _Working.Line_Colors [ 1 ];
    }

    private void Line3_Swatch_Click (
      object? Sender, EventArgs E )
    {
      _Working.Line_Colors [ 2 ] = Pick_Color (
        _Working.Line_Colors [ 2 ] );
      Line3_Swatch.BackColor =
        _Working.Line_Colors [ 2 ];
    }

    private void Line4_Swatch_Click (
      object? Sender, EventArgs E )
    {
      _Working.Line_Colors [ 3 ] = Pick_Color (
        _Working.Line_Colors [ 3 ] );
      Line4_Swatch.BackColor =
        _Working.Line_Colors [ 3 ];
    }

    private void Dark_Preset_Button_Click (
      object? Sender, EventArgs E )
    {
      _Working.Copy_From ( Chart_Theme.Dark_Preset ( ) );
      Update_Swatches ( );
    }

    private void Light_Preset_Button_Click (
      object? Sender, EventArgs E )
    {
      _Working.Copy_From ( Chart_Theme.Light_Preset ( ) );
      Update_Swatches ( );
    }

    private void OK_Button_Click (
      object? Sender, EventArgs E )
    {
      Result = _Working;
      DialogResult = DialogResult.OK;
      Close ( );
    }

    private void Cancel_Close_Button_Click (
      object? Sender, EventArgs E )
    {
      DialogResult = DialogResult.Cancel;
      Close ( );
    }
  }
}
