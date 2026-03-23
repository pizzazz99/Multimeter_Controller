

// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Theme_Settings_Form.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Modal dialog for interactively editing a Chart_Theme.  The form works on
//   a private working copy of the theme so the caller's theme is never mutated
//   unless the user clicks OK.  On OK the edited theme is exposed via the
//   Result property; on Cancel Result remains null and the caller's theme is
//   unchanged.
//
// CONSTRUCTOR
//   Theme_Settings_Form(Chart_Theme current)
//     Copies current into a new _Working instance via Chart_Theme.Copy_From(),
//     then calls Update_Swatches() to reflect the initial colors in the UI.
//     The designer file supplies all swatch Panel controls and preset/action
//     buttons; this file contains only logic.
//
// COLOR EDITING
//   Each color slot is represented by a colored Panel (swatch) in the designer.
//   Clicking any swatch calls Pick_Color() and immediately updates both
//   _Working and the swatch's BackColor.
//
//   Editable slots
//     Background   Bg_Swatch_Click
//     Foreground   Fg_Swatch_Click
//     Grid         Grid_Swatch_Click
//     Labels       Labels_Swatch_Click
//     Separator    Separator_Swatch_Click
//     Line 1–4     Line1_Swatch_Click … Line4_Swatch_Click
//
//   Pick_Color(current)
//     Opens a full ColorDialog (FullOpen = true, AnyColor = true).
//     Returns the chosen color on OK, or the original color on Cancel.
//
//   Update_Swatches()
//     Synchronises all nine swatch BackColor values from _Working.
//     Called after construction and after every preset button click.
//
// PRESET BUTTONS
//   Each preset button calls Chart_Theme.*_Preset() and copies the result
//   into _Working via Copy_From(), then calls Update_Swatches().
//   Available presets: Dark, Light, Light Blue, Brown, Grey, Golden,
//   Light Yellow.
//
// RESULT HANDLING
//   Result     [DesignerSerializationVisibility(Hidden)] public property.
//              Set to _Working on OK; remains null! on Cancel.
//              Callers should check DialogResult before reading Result.
//
//   OK_Button_Click       Assigns _Working to Result, sets DialogResult.OK,
//                         closes the form.
//   Cancel_Close_Button_Click  Sets DialogResult.Cancel, closes the form
//                         without modifying Result.
//
// NOTES
//   • _Working is always a separate instance from the theme passed in — the
//     caller's theme is never touched until it explicitly applies Result.
//   • The designer file (Theme_Settings_Form.Designer.cs) owns the layout of
//     swatch panels, preset buttons, OK, and Cancel controls.  This file
//     contains no control construction.
//
// AUTHOR:  [Your name]
// CREATED: [Date]
// ════════════════════════════════════════════════════════════════════════════════



using System.ComponentModel;

namespace Multimeter_Controller
{
  public partial class Theme_Settings_Form : Form
  {
    private Chart_Theme _Working;

    [DesignerSerializationVisibility (
      DesignerSerializationVisibility.Hidden )]
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
      Fg_Swatch.BackColor = _Working.Foreground;
      Grid_Swatch.BackColor = _Working.Grid;
      Labels_Swatch.BackColor = _Working.Labels;
      Separator_Swatch.BackColor = _Working.Separator;
      Line1_Swatch.BackColor = _Working.Line_Colors [ 0 ];
      Line2_Swatch.BackColor = _Working.Line_Colors [ 1 ];
      Line3_Swatch.BackColor = _Working.Line_Colors [ 2 ];
      Line4_Swatch.BackColor = _Working.Line_Colors [ 3 ];
    }

    private void Fg_Swatch_Click ( object? Sender, EventArgs E )
    {
      _Working.Foreground = Pick_Color ( _Working.Foreground );
      Fg_Swatch.BackColor = _Working.Foreground;
    }

    private void Light_Blue_Preset_Button_Click ( object? Sender, EventArgs E )
    {
      _Working.Copy_From ( Chart_Theme.Light_Blue_Preset ( ) );
      Update_Swatches ( );
    }

    private void Brown_Preset_Button_Click ( object? Sender, EventArgs E )
    {
      _Working.Copy_From ( Chart_Theme.Brown_Preset ( ) );
      Update_Swatches ( );
    }

    private void Grey_Preset_Button_Click ( object? Sender, EventArgs E )
    {
      _Working.Copy_From ( Chart_Theme.Grey_Preset ( ) );
      Update_Swatches ( );
    }

    private void Golden_Preset_Button_Click ( object? Sender, EventArgs E )
    {
      _Working.Copy_From ( Chart_Theme.Golden_Preset ( ) );
      Update_Swatches ( );
    }

    private void Light_Yellow_Preset_Button_Click ( object? Sender, EventArgs E )
    {
      _Working.Copy_From ( Chart_Theme.Light_Yellow_Preset ( ) );
      Update_Swatches ( );
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
