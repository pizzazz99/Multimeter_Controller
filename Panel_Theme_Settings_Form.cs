
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Panel_Theme_Settings_Form.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Modal dialog for editing the application panel theme (background and
//   foreground colors).  The user can either pick colors manually via the
//   system ColorDialog or apply one of the built-in presets.  Changes are
//   staged in a working copy of the theme and only committed when the user
//   clicks OK, so Cancel leaves the original theme untouched.
//
// USAGE
//   Instantiate with the current Chart_Theme, show as a dialog, then read
//   the Result property if DialogResult.OK is returned:
//
//     using var Dlg = new Panel_Theme_Settings_Form( _Settings.Panel_Theme );
//     if ( Dlg.ShowDialog( this ) == DialogResult.OK )
//       _Settings.Set_Theme( Dlg.Result, "Panel_Theme" );
//
// FIELDS
//   _Working          A working copy of the theme loaded from disk and
//                     initialised from the caller-supplied Current theme.
//                     All edits target this copy; the original is never
//                     modified until OK is clicked.
//
// PROPERTIES
//   Result            The committed Chart_Theme after OK.  Null until the
//                     user confirms.  The caller is responsible for saving
//                     and applying the returned theme.
//
// COLOR PICKERS
//   Fg_Panel_Swatch_Click   Opens ColorDialog for the foreground color,
//                           updates _Working.Foreground and refreshes the
//                           swatch Panel's BackColor immediately.
//   Bg_Panel_Swatch_Click   Same flow for _Working.Background.
//   Pick_Color()            Shared helper: opens a full ColorDialog pre-
//                           seeded with the supplied color and returns the
//                           chosen color, or the original if cancelled.
//   Update_Swatches()       Syncs both swatch Panel BackColors to the
//                           current _Working values.  Called after every
//                           color change (manual or preset).
//
// PRESETS  (each copies a static factory result into _Working, then refreshes)
//   Dark_Preset_Button_Click         Chart_Theme.Dark_Preset()
//   Light_Preset_Button_Click        Chart_Theme.Light_Preset()
//   Brown_Preset_Button_Click        Chart_Theme.Brown_Preset()
//   Grey_Preset_Button_Click         Chart_Theme.Grey_Preset()
//   Golden_Preset_Button_Click       Chart_Theme.Golden_Preset()
//   Light_Yellow_Preset_Button_Click Chart_Theme.Light_Yellow_Preset()
//   Light_Blue_Preset_Button_Click   Chart_Theme.Light_Blue_Preset()
//
// BUTTON HANDLERS
//   OK_Button_Click           Assigns _Working to Result, sets
//                             DialogResult.OK, and closes the form.
//   Cancel_Close_Button_Click Sets DialogResult.Cancel and closes without
//                             modifying Result.
//
// NOTES
//   • Only Background and Foreground are editable here.  Line colors,
//     grid, and label colors belong to the chart theme and are managed
//     separately in Settings_Form.
//   • The form uses a partial class; the designer file supplies the two
//     color-swatch Panels (Bg_Panel_Swatch, Fg_Panel_Swatch) and all
//     preset and OK/Cancel buttons.
//   • _Working is loaded from Chart_Theme.Panel_Theme_Path then
//     overwritten via Copy_From() so it carries the correct file path
//     for any subsequent Save() call the caller may perform.
//   • The Result property is decorated with
//     [DesignerSerializationVisibility(Hidden)] to prevent the WinForms
//     designer from attempting to serialize a Chart_Theme reference.
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════

using System.ComponentModel;

namespace Multimeter_Controller
{
  public partial class Panel_Theme_Settings_Form : Form
  {
    private Chart_Theme _Working;

    [DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
    public Chart_Theme Result { get; private set; } = null!;

    public Panel_Theme_Settings_Form( Chart_Theme Current )
    {
      InitializeComponent();

      _Working = Chart_Theme.Load( Chart_Theme.Panel_Theme_Path );
      _Working.Copy_From( Current );

      Update_Swatches();
    }

    private Color Pick_Color( Color Current )
    {
      using var Dlg = new ColorDialog();
      Dlg.Color = Current;
      Dlg.FullOpen = true;
      Dlg.AnyColor = true;

      if (Dlg.ShowDialog( this ) == DialogResult.OK)
      {
        return Dlg.Color;
      }

      return Current;
    }

    private void Fg_Panel_Swatch_Click( object? Sender, EventArgs E )
    {
      _Working.Foreground = Pick_Color( _Working.Foreground );
      Fg_Panel_Swatch.BackColor = _Working.Foreground;  // ✅ fixed
    }
    private void Bg_Panel_Swatch_Click( object? Sender, EventArgs E )
    {
      _Working.Background = Pick_Color( _Working.Background );
      Bg_Panel_Swatch.BackColor = _Working.Background;
    }

    private void Update_Swatches()
    {
      Bg_Panel_Swatch.BackColor = _Working.Background;
      Fg_Panel_Swatch.BackColor = _Working.Foreground;
    }


    private void Light_Blue_Preset_Button_Click( object? Sender, EventArgs E )
    {
      _Working.Copy_From( Chart_Theme.Light_Blue_Preset() );
      Update_Swatches();
    }


    private void Grey_Preset_Button_Click( object? Sender, EventArgs E )
    {
      _Working.Copy_From( Chart_Theme.Grey_Preset() );
      Update_Swatches();
    }

    private void Light_Yellow_Preset_Button_Click( object? Sender, EventArgs E )
    {
      _Working.Copy_From( Chart_Theme.Light_Yellow_Preset() );
      Update_Swatches();
    }

    private void Dark_Preset_Button_Click( object? Sender, EventArgs E )
    {
      _Working.Copy_From( Chart_Theme.Dark_Preset() );
      Update_Swatches();
    }

    private void Light_Preset_Button_Click( object? Sender, EventArgs E )
    {
      _Working.Copy_From( Chart_Theme.Light_Preset() );
      Update_Swatches();
    }


    private void Brown_Preset_Button_Click( object? Sender, EventArgs E )
    {
      _Working.Copy_From( Chart_Theme.Brown_Preset() );
      Update_Swatches();
    }

    private void Golden_Preset_Button_Click( object? Sender, EventArgs E )
    {
      _Working.Copy_From( Chart_Theme.Golden_Preset() );
      Update_Swatches();
    }

    private void OK_Button_Click( object? Sender, EventArgs E )
    {
      Result = _Working;
      DialogResult = DialogResult.OK;
      Close();
    }

    private void Cancel_Close_Button_Click( object? Sender, EventArgs E )
    {
      DialogResult = DialogResult.Cancel;
      Close();
    }

   
  }
}
