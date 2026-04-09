// =============================================================================
// FILE:        Rich_Text_Popup.cs
// NAMESPACE:   Rich_Text_Popup_Namespace
// =============================================================================
//
// DESCRIPTION:
// ------------
// A self-contained, reusable rich-text popup window for Windows Forms
// applications. Encapsulates a Form, a padded RichTextBox, and an OK button
// into a single class that can be instantiated, populated with formatted
// content, and displayed with minimal boilerplate at the call site.
//
// Supports multiple text styles (title, heading, body, monospaced, warning,
// error), two-column label/value rows, coloured swatches, instrument headers,
// blank lines, and horizontal separators. All content methods return 'this'
// to enable fluent method chaining.
//
// -----------------------------------------------------------------------------
// CLASS:
// -----------------------------------------------------------------------------
//
//   Rich_Text_Popup
//   ---------------
//   A disposable popup form wrapping a RichTextBox with typed content methods.
//
//   Constructor:
//     Rich_Text_Popup ( string Title,
//                       int    Width     = 520,
//                       int    Height    = 560,
//                       bool   Resizable = false )
//
//       Title     - Text shown in the form title bar.
//       Width     - Initial form width  in pixels (default 520).
//       Height    - Initial form height in pixels (default 560).
//       Resizable - When true the border is Sizable and Maximise is enabled.
//                   Use for content-heavy popups the user may want to resize.
//
// -----------------------------------------------------------------------------
// CONTENT METHODS:  (all return 'this' for fluent chaining)
// -----------------------------------------------------------------------------
//
//   Add_Title   ( string Text )
//     Large bold blue line rendered in Segoe UI 13pt.
//     Use once at the top for the application or dialog name.
//
//   Add_Heading ( string Text )
//     Bold dark-grey Segoe UI line with an automatic blank line inserted above.
//     Use for section headers such as "FEATURES" or "USAGE".
//
//   Add_Heading_Mono ( string Text )
//     Bold blue Courier New 10.5pt header followed immediately by a full-width
//     grey horizontal separator line (U+2500 characters).
//     Use for monospaced settings/report sections such as "Instruments" or
//     "Polling" where column-aligned rows follow below.
//
//   Add_Body    ( string Text )
//     Standard Segoe UI 9pt body text in near-black.
//     The workhorse method for regular prose content.
//
//   Add_Body_Bold ( string Text )
//     Bold Segoe UI 9.5pt in near-black. Use for inline emphasis within body
//     content such as "Without a master instrument:".
//
//   Add_Body_Colored ( string Text, Color Color )
//     Segoe UI 9pt body text in a caller-specified colour. Use for contextual
//     highlights that do not fit the standard warning/error palette.
//
//   Add_Mono    ( string Text )
//     Consolas 9pt in dark green. Use for code snippets, file paths,
//     command-line examples, or fixed-width reference text.
//
//   Add_Raw_Text ( string Text,
//                  string Font_Name = "Courier New",
//                  float  Font_Size = 10F )
//     Appends a pre-formed multi-line string verbatim in the specified
//     monospaced font. Use for embedded resource dumps, file content, or
//     any text that is already fully formatted and needs no further styling.
//
//   Add_Row ( string  Label,
//             string  Value,
//             Color?  Value_Color = null,
//             int     Label_Width = 30 )
//     Appends a two-column Courier New label/value line. The label is padded
//     to Label_Width characters for alignment. Value renders in Value_Color
//     when supplied, otherwise near-black. Use for settings and report grids
//     where consistent column alignment is required.
//
//   Add_Color_Row ( string Label, Color C, string Name = "" )
//     Appends a label, a coloured square swatch (■ U+25A0) rendered in the
//     actual colour, and the colour name or hex value in black. When Name is
//     supplied it overrides the automatic name/hex resolution. Use for theme
//     and palette display sections.
//
//   Add_Instrument_Header ( string Name )
//     Appends a bold blue bullet (● U+25CF) line used as a sub-heading for
//     each instrument entry in an instrument list section.
//
//   Add_Warning ( string Text )
//     Segoe UI 9pt in orange RGB(180,60,0). Use for cautions or notices.
//
//   Add_Error   ( string Text )
//     Segoe UI 9pt in red RGB(180,0,0). Use for errors or critical conditions.
//
//   Add_Blank   ( )
//     Inserts one empty line. Call multiple times for larger gaps.
//
//   Add_Separator ( )
//     Inserts a full-width horizontal rule of Unicode box-drawing characters
//     (U+2500) in Consolas. Use to visually divide major sections.
//
// -----------------------------------------------------------------------------
// DISPLAY METHOD:
// -----------------------------------------------------------------------------
//
//   Show_Popup ( Form? Owner = null )
//     Scrolls the RichTextBox to the top and displays the form as a modal
//     dialog. Pass the calling form as Owner to centre the popup over it,
//     or pass null for screen-centred display.
//     Blocks until the user dismisses the popup.
//
// -----------------------------------------------------------------------------
// LAYOUT NOTES:
// -----------------------------------------------------------------------------
//
//   The RichTextBox is wrapped in a Panel with Padding = 14px on all sides.
//   This is necessary because RichTextBox silently ignores its own Padding
//   property — without the wrapper panel, text renders flush against the border.
//
//   The OK button is anchored to the bottom-right of a dedicated button panel
//   and repositions itself automatically on layout events, so it remains
//   correctly placed when Resizable = true and the user resizes the form.
//
//   The form AcceptButton is set to OK so pressing Enter dismisses the popup.
//
// -----------------------------------------------------------------------------
// FONTS AND COLOURS:
// -----------------------------------------------------------------------------
//
//   All fonts and colours are static readonly fields shared across all
//   instances. They must never be disposed by an instance — they live for
//   the lifetime of the application. Only the Form is disposed on Dispose().
//
//   Style            Font           Size    Weight    Colour
//   ---------------  -------------  ------  --------  ---------------------------
//   Title            Segoe UI       13pt    Bold      RGB(  0, 102, 204)  Blue
//   Heading          Segoe UI        9pt    Bold      RGB( 60,  60,  60)  Dark grey
//   Heading_Mono     Courier New    10.5pt  Bold      RGB( 30,  80, 160)  Dark blue
//   Body             Segoe UI        9pt    Regular   RGB( 40,  40,  40)  Near black
//   Body_Bold        Segoe UI        9.5pt  Bold      RGB( 40,  40,  40)  Near black
//   Mono             Consolas        9pt    Regular   RGB(  0,  80,   0)  Dark green
//   Raw_Text         Courier New    10pt    Regular   RGB( 40,  40,  40)  Near black
//   Row label        Courier New     9.5pt  Regular   RGB( 90,  90,  90)  Mid grey
//   Row value        Courier New     9.5pt  Regular   RGB( 20,  20,  20)  Near black
//   Warning          Segoe UI        9pt    Regular   RGB(180,  60,   0)  Orange
//   Error            Segoe UI        9pt    Regular   RGB(180,   0,   0)  Red
//   Separator        Consolas        9pt    Regular   RGB( 60,  60,  60)  Dark grey
//
// -----------------------------------------------------------------------------
// DISPOSAL:
// -----------------------------------------------------------------------------
//
//   Rich_Text_Popup implements IDisposable. Always wrap in a 'using' block or
//   call Dispose() explicitly to release the underlying Form resource.
//   Do NOT dispose the static font fields — they are shared across instances.
//
//   Recommended pattern:
//     using ( var Popup = new Rich_Text_Popup ( "Title" ) )
//     {
//       Popup.Add_Title ( "..." )
//            .Add_Body  ( "..." );
//       Popup.Show_Popup ( this );
//     }
//
// -----------------------------------------------------------------------------
// USAGE EXAMPLES:
// -----------------------------------------------------------------------------
//
//   1. SIMPLE ABOUT BOX:
//
//      using ( var Popup = new Rich_Text_Popup ( "About My App", 520, 480 ) )
//      {
//        Popup.Add_Title   ( "My Application" )
//             .Add_Body    ( "Version 2.0.0" )
//             .Add_Blank   ( )
//             .Add_Heading ( "FEATURES" )
//             .Add_Body    ( "  \u2022  Feature one" )
//             .Add_Body    ( "  \u2022  Feature two" )
//             .Add_Heading ( "NOTES" )
//             .Add_Warning ( "  Requires administrator privileges." )
//             .Add_Blank   ( )
//             .Add_Separator ( )
//             .Add_Body    ( "  Author:  Mike Williams" )
//             .Add_Body    ( "  License: MIT" );
//        Popup.Show_Popup ( this );
//      }
//
//   2. SETTINGS REPORT WITH ALIGNED ROWS:
//
//      using ( var Popup = new Rich_Text_Popup ( "Session Settings", 660, 700, Resizable: true ) )
//      {
//        Popup.Add_Heading_Mono ( "Polling" )
//             .Add_Row ( "Poll Delay",      $"{Settings.Poll_Delay_Ms} ms" )
//             .Add_Row ( "Continuous Mode", Settings.Continuous ? "Yes" : "No" )
//             .Add_Blank ( )
//             .Add_Heading_Mono ( "Theme" )
//             .Add_Color_Row ( "Background", Theme.Background )
//             .Add_Color_Row ( "Foreground", Theme.Foreground );
//        Popup.Show_Popup ( this );
//      }
//
//   3. EMBEDDED RESOURCE DUMP:
//
//      using ( var Popup = new Rich_Text_Popup ( "Meter Info", 600, 500, Resizable: true ) )
//      {
//        Popup.Add_Raw_Text ( content );
//        Popup.Show_Popup   ( this );
//      }
//
//   4. LAUNCHED FROM A BUTTON CLICK:
//
//      private void My_Button_Click ( object Sender, EventArgs E )
//      {
//        Show_My_Popup ( this, _Settings, _Instruments, _Theme );
//      }
//
//      public static void Show_My_Popup ( Form Owner, ... )
//      {
//        using ( var Popup = new Rich_Text_Popup ( "My Popup", 660, 700 ) )
//        {
//          Popup.Add_Heading_Mono ( "Section" )
//               .Add_Row ( "Label", "Value" );
//          Popup.Show_Popup ( Owner );
//        }
//      }
//
// =============================================================================
// AUTHOR:      Mike Wheeler
// VERSION:     1.1.0
// CREATED:     2026
// LICENSE:     Open Source
// =============================================================================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Trace_Execution_Namespace.Trace_Execution;


namespace Rich_Text_Popup_Namespace
{
  // =============================================================================
  // Rich_Text_Popup.cs
  // =============================================================================
  // A self-contained popup window with a RichTextBox and an OK button.
  //
  // USAGE:
  //   using ( var Popup = new Rich_Text_Popup ( "My Title", 520, 560 ) )
  //   {
  //     Popup.Add_Title   ( "My Application" );
  //     Popup.Add_Heading ( "FEATURES" );
  //     Popup.Add_Body    ( "  \u2022  Some feature" );
  //     Popup.Add_Blank   ( );
  //     Popup.Add_Body    ( "  Author: Mike Williams" );
  //     Popup.Show_Popup  ( owner_form );
  //   }
  // =============================================================================




  public class Rich_Text_Popup : IDisposable
  {
    // -------------------------------------------------------------------------
    // Private fields
    // -------------------------------------------------------------------------
    private readonly Form _Form;
    private readonly RichTextBox _Rtb;
    private bool _Disposed = false;

    // ── Default fonts ─────────────────────────────────────────────────────────
    private static readonly Font _Font_Title = new Font( "Segoe UI", 13F, FontStyle.Bold );
    private static readonly Font _Font_Heading = new Font( "Segoe UI", 9F, FontStyle.Bold );
    private static readonly Font _Font_Body = new Font( "Segoe UI", 9F, FontStyle.Regular );
    private static readonly Font _Font_Mono = new Font( "Consolas", 9F, FontStyle.Regular );

    // ── Default colours ───────────────────────────────────────────────────────
    private static readonly Color _Color_Title = Color.FromArgb( 0, 102, 204 );
    private static readonly Color _Color_Heading = Color.FromArgb( 60, 60, 60 );
    private static readonly Color _Color_Body = Color.FromArgb( 40, 40, 40 );
    private static readonly Color _Color_Mono = Color.FromArgb( 0, 80, 0 );
    private static readonly Color _Color_Warning = Color.FromArgb( 180, 60, 0 );
    private static readonly Color _Color_Error = Color.FromArgb( 180, 0, 0 );

    public Form Form => _Form;


    // =========================================================================
    // Constructor
    // =========================================================================
    // Parameters:
    //   Title  - Text shown in the form's title bar.
    //   Width  - Initial form width  (default 520).
    //   Height - Initial form height (default 560).
    //   Resizable - When true the user can resize the popup (default false).
    // =========================================================================
    public Rich_Text_Popup( string Title, int Width = 520, int Height = 560, bool Resizable = false )
    {
      _Form = new Form
      {
        Text = Title,
        Size = new Size( Width, Height ),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = Resizable ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog,
        MaximizeBox = Resizable,
        MinimizeBox = false,
        ShowInTaskbar = false,
        BackColor = SystemColors.Window
      };

      // ── RichTextBox inside a padded panel ─────────────────────────────────
      _Rtb = new RichTextBox
      {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = SystemColors.Window,
        ScrollBars = RichTextBoxScrollBars.Vertical
      };

      var Content_Panel = new Panel
      {
        Dock = DockStyle.Fill,
        Padding = new Padding( 14 )
      };
      Content_Panel.Controls.Add( _Rtb );

      // ── OK button panel ───────────────────────────────────────────────────
      var Ok_Button = new Button
      {
        Text = "OK",
        DialogResult = DialogResult.OK,
        Size = new Size( 80, 28 ),
        Anchor = AnchorStyles.Top | AnchorStyles.Right
      };

      Ok_Button.Click += ( S, Args ) => _Form.Close();


      var Button_Panel = new Panel
      {
        Dock = DockStyle.Bottom,
        Height = 44,
        Padding = new Padding( 0, 6, 12, 6 )
      };

      Button_Panel.Controls.Add( Ok_Button );
      Button_Panel.Layout += ( S, Args ) =>
      {
        Ok_Button.Location = new Point(
            Button_Panel.ClientSize.Width - Ok_Button.Width - 12,
            Button_Panel.ClientSize.Height - Ok_Button.Height - 8 );
      };

      _Form.Controls.Add( Content_Panel );
      _Form.Controls.Add( Button_Panel );
      _Form.AcceptButton = Ok_Button;
    }

    // =========================================================================
    // Content Methods
    // =========================================================================


    public void Show_Non_Modal( Form Owner ) => _Form.Show( Owner );



    public Form Get_Form() => _Form;
    // -------------------------------------------------------------------------
    // Add_Row
    // -------------------------------------------------------------------------
    // Appends a two-column label/value line with optional value colour.
    // Label is padded to Label_Width characters for alignment.
    //
    // Parameters:
    //   Label       - Left column text.
    //   Value       - Right column text.
    //   Value_Color - Optional colour for the value (defaults to near-black).
    //   Label_Width - Column width for label padding (default 30).
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Row( string Label,
                                     string Value,
                                     Color? Value_Color = null,
                                     int Label_Width = 30 )
    {

   //   using var Block = Trace_Block.Start_If_Enabled();

      _Rtb.SelectionFont = _Font_Mono;
      _Rtb.SelectionColor = Color.FromArgb( 90, 90, 90 );
      _Rtb.AppendText( $"  {Label.PadRight( Label_Width )}" );
      _Rtb.SelectionFont = _Font_Mono;
      _Rtb.SelectionColor = Value_Color ?? Color.FromArgb( 20, 20, 20 );
      _Rtb.AppendText( Value + Environment.NewLine );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Color_Row
    // -------------------------------------------------------------------------
    // Appends a label, a coloured swatch character (■), and the colour name
    // or hex value. The swatch is rendered in the actual colour being described.
    //
    // Parameters:
    //   Label - Left column text.
    //   C     - The colour to display.
    //   Name  - Optional override for the colour name display string.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Color_Row( string Label, Color C, string Name = "" )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      string Display = !string.IsNullOrEmpty( Name ) ? Name
                     : C.IsNamedColor ? C.Name
                     : $"#{C.R:X2}{C.G:X2}{C.B:X2}";

      _Rtb.SelectionFont = _Font_Mono;
      _Rtb.SelectionColor = Color.FromArgb( 90, 90, 90 );
      _Rtb.AppendText( $"  {Label.PadRight( 30 )}" );
      _Rtb.SelectionColor = C;
      _Rtb.AppendText( "\u25A0  " );
      _Rtb.SelectionColor = Color.Black;
      _Rtb.AppendText( Display + Environment.NewLine );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Instrument_Header
    // -------------------------------------------------------------------------
    // Appends a bold blue bullet line used as a sub-heading for each instrument.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Instrument_Header( string Name )
    {

    //  using var Block = Trace_Block.Start_If_Enabled();

      _Rtb.SelectionFont = new Font( "Courier New", 9.5F, FontStyle.Bold );
      _Rtb.SelectionColor = Color.FromArgb( 20, 100, 180 );
      _Rtb.AppendText( $"  \u25CF {Name}" + Environment.NewLine );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Heading_Mono
    // -------------------------------------------------------------------------
    // Appends a bold blue monospaced section header followed by a separator.
    // Matches the Header() + Separator() pattern used in Session_Settings_Form.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Heading_Mono( string Text )
    {
     // using var Block = Trace_Block.Start_If_Enabled();

      _Rtb.SelectionFont = new Font( "Courier New", 10.5F, FontStyle.Bold );
      _Rtb.SelectionColor = Color.FromArgb( 30, 80, 160 );
      _Rtb.AppendText( "  " + Text + Environment.NewLine );

      _Rtb.SelectionFont = _Font_Mono;
      _Rtb.SelectionColor = Color.FromArgb( 180, 180, 180 );
      _Rtb.AppendText( "  " + new string( '\u2500', 54 ) + Environment.NewLine );
      return this;
    }



    // -------------------------------------------------------------------------
    // Add_Raw_Text
    // -------------------------------------------------------------------------
    // Appends a pre-formed multi-line string as monospaced body text.
    // Use for embedded resource content, file dumps, or any text that is
    // already fully formatted and should be rendered verbatim.
    //
    // Parameters:
    //   Text      - The raw string to append (may contain newlines).
    //   Font_Name - Monospaced font to use (default "Courier New").
    //   Font_Size - Point size (default 10F).
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Raw_Text( string Text,
                                          string Font_Name = "Courier New",
                                          float Font_Size = 10F )
    {
   //   using var Block = Trace_Block.Start_If_Enabled();

      _Rtb.SelectionFont = new Font( Font_Name, Font_Size, FontStyle.Regular );
      _Rtb.SelectionColor = _Color_Body;
      _Rtb.AppendText( Text );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Body_Bold
    // -------------------------------------------------------------------------
    // Appends a bold body-weight line in near-black. Use for inline emphasis
    // within body content such as "Without a master instrument:".
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Body_Bold( string Text )
    {
    //  using var Block = Trace_Block.Start_If_Enabled();

      Append( Text, new Font( "Segoe UI", 9.5F, FontStyle.Bold ), _Color_Body );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Body_Colored
    // -------------------------------------------------------------------------
    // Appends a body-weight line in a caller-specified colour. Use for
    // contextual highlights that do not fit the standard warning/error palette.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Body_Colored( string Text, Color Color )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      Append( Text, _Font_Body, Color );
      return this;
    }



    // -------------------------------------------------------------------------
    // Add_Title
    // -------------------------------------------------------------------------
    // Appends a large bold blue title line.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Title( string Text )
    {
     // using var Block = Trace_Block.Start_If_Enabled();

      Append( Text, _Font_Title, _Color_Title );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Heading
    // -------------------------------------------------------------------------
    // Appends a bold dark-grey section heading with a blank line above it.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Heading( string Text )
    {

  //    using var Block = Trace_Block.Start_If_Enabled();
      Append( string.Empty, _Font_Body, _Color_Body );   // blank line above
      Append( Text, _Font_Heading, _Color_Heading );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Body
    // -------------------------------------------------------------------------
    // Appends a standard body text line.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Body( string Text )
    {
  //    using var Block = Trace_Block.Start_If_Enabled();

      Append( Text, _Font_Body, _Color_Body );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Mono
    // -------------------------------------------------------------------------
    // Appends a monospaced (Consolas) line — useful for code, paths, values.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Mono( string Text )
    {

  //    using var Block = Trace_Block.Start_If_Enabled();

      Append( Text, _Font_Mono, _Color_Mono );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Warning
    // -------------------------------------------------------------------------
    // Appends a body line in orange — suitable for cautions or notices.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Warning( string Text )
    {
    //  using var Block = Trace_Block.Start_If_Enabled();

      Append( Text, _Font_Body, _Color_Warning );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Error
    // -------------------------------------------------------------------------
    // Appends a body line in red — suitable for errors or critical notices.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Error( string Text )
    {
  //    using var Block = Trace_Block.Start_If_Enabled();

      Append( Text, _Font_Body, _Color_Error );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Blank
    // -------------------------------------------------------------------------
    // Appends an empty line for spacing.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Blank()
    {
   //   using var Block = Trace_Block.Start_If_Enabled();

      Append( string.Empty, _Font_Body, _Color_Body );
      return this;
    }

    // -------------------------------------------------------------------------
    // Add_Separator
    // -------------------------------------------------------------------------
    // Appends a horizontal rule made of dashes.
    // -------------------------------------------------------------------------
    public Rich_Text_Popup Add_Separator()
    {
   //   using var Block = Trace_Block.Start_If_Enabled();

      Append( new string( '\u2500', 58 ), _Font_Mono, _Color_Heading );
      return this;
    }

    // =========================================================================
    // Display
    // =========================================================================

    // -------------------------------------------------------------------------
    // Show_Popup
    // -------------------------------------------------------------------------
    // Scrolls to the top and shows the form as a modal dialog.
    //
    // Parameters:
    //   Owner - The parent form (centres the popup over it). Pass null for
    //           screen-centred display.
    // -------------------------------------------------------------------------
    public void Show_Popup( Form? Owner = null )
    {
      using var Block = Trace_Block.Start_If_Enabled();

      _Rtb.SelectionStart = 0;
      _Rtb.ScrollToCaret();

      if (Owner != null)
        _Form.ShowDialog( Owner );
      else
        _Form.ShowDialog();
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    private void Append( string Text, Font Font, Color Color )
    {
     // using var Block = Trace_Block.Start_If_Enabled();

      _Rtb.SelectionFont = Font;
      _Rtb.SelectionColor = Color;
      _Rtb.AppendText( Text + Environment.NewLine );
    }

    // =========================================================================
    // IDisposable
    // =========================================================================

    public void Dispose()
    {
      if (!_Disposed)
      {
        _Form.Dispose();
        _Disposed = true;
      }
    }
  }
}

