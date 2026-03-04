using System;
using System.Drawing;
using System.Windows.Forms;

namespace Multimeter_Controller
{
  // ============================================================
  //  Theme_Editor_Dialog
  //  A WinForms dialog that lets the user:
  //    • Pick a built-in preset  (Dark / Light)
  //    • Customise every colour with a colour-picker button
  //    • See a live preview panel
  //    • Save or cancel
  //
  //  Usage (from your Theme_Button_Click or a menu item):
  //
  //    using var Dlg = new Theme_Editor_Dialog ( _Settings.Current_Theme );
  //    if ( Dlg.ShowDialog ( this ) == DialogResult.OK )
  //    {
  //      _Settings.Set_Theme ( Dlg.Result_Theme );
  //      _Settings.Save ( );
  //    }
  // ============================================================

  public class Theme_Editor_Dialog : Form
  {
    // ── public result ────────────────────────────────────────
    public Chart_Theme Result_Theme
    {
      get; private set;
    }

    // ── working copy that changes as the user edits ──────────
    private Chart_Theme _Working;

    // ── colour swatch buttons ────────────────────────────────
    private Button _Btn_Background;
    private Button _Btn_Foreground;
    private Button _Btn_Grid;
    private Button _Btn_Labels;
    private Button _Btn_Separator;
    private Button [ ] _Btn_Lines;          // 4 line-colour buttons

    // ── preview panel ────────────────────────────────────────
    private Panel _Preview_Panel;

    // ── name textbox ─────────────────────────────────────────
    private TextBox _Txt_Name;

    // ─────────────────────────────────────────────────────────
    public Theme_Editor_Dialog ( Chart_Theme Starting_Theme )
    {
      // Deep-copy so we don't mutate the live theme while editing
      _Working = Deep_Copy ( Starting_Theme );
      Result_Theme = _Working;

      Build_UI ( );
      Refresh_All_Swatches ( );
      Refresh_Preview ( );
    }

    // ── UI construction ──────────────────────────────────────
    private void Build_UI ( )
    {
      Text = "Theme Editor";
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      MinimizeBox = false;
      StartPosition = FormStartPosition.CenterParent;
      Size = new Size ( 520, 540 );
      BackColor = Color.FromArgb ( 40, 44, 52 );
      ForeColor = Color.White;

      int Left_Col = 20;
      int Right_Col = 200;
      int Row = 20;
      int Row_H = 36;

      // ── Name ────────────────────────────────────────────────
      Add_Label ( "Theme name:", Left_Col, Row );
      _Txt_Name = new TextBox
      {
        Text = _Working.Name,
        Location = new Point ( Right_Col, Row ),
        Width = 270,
        BackColor = Color.FromArgb ( 60, 65, 75 ),
        ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
      };
      Controls.Add ( _Txt_Name );
      Row += Row_H + 6;

      // ── Preset buttons ──────────────────────────────────────
      Add_Label ( "Preset:", Left_Col, Row + 4 );
      var Btn_Dark = Make_Action_Button ( "Dark", Right_Col, Row, 120 );
      Btn_Dark.Click += ( s, e ) => Apply_Preset ( Chart_Theme.Dark_Preset ( ) );
      var Btn_Light = Make_Action_Button ( "Light", Right_Col + 130, Row, 120 );
      Btn_Light.Click += ( s, e ) => Apply_Preset ( Chart_Theme.Light_Preset ( ) );
      Row += Row_H + 10;

      // ── Separator ───────────────────────────────────────────
      var Sep = new Label
      {
        Text = "",
        BorderStyle = BorderStyle.Fixed3D,
        Location = new Point ( Left_Col, Row ),
        Size = new Size ( 460, 2 ),
        AutoSize = false,
      };
      Controls.Add ( Sep );
      Row += 12;

      // ── Colour rows ─────────────────────────────────────────
      _Btn_Background = Add_Colour_Row ( "Background:", Left_Col, Right_Col, Row );
      Row += Row_H;
      _Btn_Foreground = Add_Colour_Row ( "Foreground:", Left_Col, Right_Col, Row );
      Row += Row_H;
      _Btn_Grid = Add_Colour_Row ( "Grid lines:", Left_Col, Right_Col, Row );
      Row += Row_H;
      _Btn_Labels = Add_Colour_Row ( "Labels:", Left_Col, Right_Col, Row );
      Row += Row_H;
      _Btn_Separator = Add_Colour_Row ( "Separators:", Left_Col, Right_Col, Row );
      Row += Row_H + 6;

      // ── Line colours ────────────────────────────────────────
      var Sep2 = new Label
      {
        Text = "",
        BorderStyle = BorderStyle.Fixed3D,
        Location = new Point ( Left_Col, Row ),
        Size = new Size ( 460, 2 ),
        AutoSize = false,
      };
      Controls.Add ( Sep2 );
      Row += 12;

      Add_Label ( "Line colours:", Left_Col, Row + 4 );
      _Btn_Lines = new Button [ 4 ];
      for ( int I = 0; I < 4; I++ )
      {
        int Idx = I;               // capture for lambda
        var Lbl = new Label
        {
          Text = $"Line {I + 1}",
          Location = new Point ( Left_Col, Row + 4 ),
          AutoSize = true,
          ForeColor = Color.White,
        };
        // We don't add the shared label here — use individual ones per line
        var Btn = new Button
        {
          Location = new Point ( Right_Col + I * 70, Row ),
          Size = new Size ( 58, 26 ),
          FlatStyle = FlatStyle.Flat,
          Text = $"L{I + 1}",
          ForeColor = Color.Black,
        };
        Btn.FlatAppearance.BorderColor = Color.Gray;
        Btn.Click += ( s, e ) => Pick_Colour_For_Line ( Idx );
        Controls.Add ( Btn );
        _Btn_Lines [ I ] = Btn;
      }
      Row += Row_H + 10;

      // ── Preview ─────────────────────────────────────────────
      Add_Label ( "Preview:", Left_Col, Row + 4 );
      _Preview_Panel = new Panel
      {
        Location = new Point ( Right_Col, Row ),
        Size = new Size ( 280, 70 ),
        BorderStyle = BorderStyle.FixedSingle,
      };
      _Preview_Panel.Paint += Preview_Panel_Paint;
      Controls.Add ( _Preview_Panel );
      Row += 80;

      // ── OK / Cancel ─────────────────────────────────────────
      var Btn_OK = Make_Action_Button ( "OK", Right_Col, Row, 120 );
      Btn_OK.Click += Btn_OK_Click;
      var Btn_Cancel = Make_Action_Button ( "Cancel", Right_Col + 130, Row, 120 );
      Btn_Cancel.Click += ( s, e ) => { DialogResult = DialogResult.Cancel; Close ( ); };

      AcceptButton = Btn_OK;
      CancelButton = Btn_Cancel;
    }

    // ── helper: labelled colour-picker row ───────────────────
    private Button Add_Colour_Row ( string Label_Text, int Lx, int Bx, int Y )
    {
      Add_Label ( Label_Text, Lx, Y + 4 );
      var Btn = new Button
      {
        Location = new Point ( Bx, Y ),
        Size = new Size ( 120, 26 ),
        FlatStyle = FlatStyle.Flat,
        Text = "",
      };
      Btn.FlatAppearance.BorderColor = Color.Gray;
      Controls.Add ( Btn );
      return Btn;
    }

    private Label Add_Label ( string Text, int X, int Y )
    {
      var Lbl = new Label
      {
        Text = Text,
        Location = new Point ( X, Y ),
        AutoSize = true,
        ForeColor = Color.White,
      };
      Controls.Add ( Lbl );
      return Lbl;
    }

    private Button Make_Action_Button ( string Text, int X, int Y, int Width )
    {
      var Btn = new Button
      {
        Text = Text,
        Location = new Point ( X, Y ),
        Size = new Size ( Width, 28 ),
        FlatStyle = FlatStyle.Flat,
        ForeColor = Color.White,
        BackColor = Color.FromArgb ( 60, 100, 160 ),
      };
      Btn.FlatAppearance.BorderColor = Color.CornflowerBlue;
      Controls.Add ( Btn );
      return Btn;
    }

    // ── swatch / preview refresh ─────────────────────────────
    private void Refresh_All_Swatches ( )
    {
      Set_Swatch ( _Btn_Background, _Working.Background );
      Set_Swatch ( _Btn_Foreground, _Working.Foreground );
      Set_Swatch ( _Btn_Grid, _Working.Grid );
      Set_Swatch ( _Btn_Labels, _Working.Labels );
      Set_Swatch ( _Btn_Separator, _Working.Separator );

      for ( int I = 0; I < 4 && I < _Working.Line_Colors.Length; I++ )
        Set_Swatch ( _Btn_Lines [ I ], _Working.Line_Colors [ I ] );
    }

    private static void Set_Swatch ( Button Btn, Color C )
    {
      Btn.BackColor = C;
      Btn.ForeColor = ( C.GetBrightness ( ) > 0.5f ) ? Color.Black : Color.White;
      Btn.Text = Get_Color_Name ( C );
    }

    private static string Get_Color_Name ( Color C )
    {
      // Check against all known named system colors
      foreach ( System.Reflection.PropertyInfo P in
        typeof ( Color ).GetProperties (
          System.Reflection.BindingFlags.Public |
          System.Reflection.BindingFlags.Static ) )
      {
        if ( P.PropertyType != typeof ( Color ) )
          continue;

        var Known = (Color) P.GetValue ( null )!;

        // Match on RGB (ignore alpha)
        if ( Known.R == C.R && Known.G == C.G && Known.B == C.B )
        {
          // Skip the auto-generated hex-style names (e.g. "ff1b2b3c")
          if ( Known.Name.Length == 8 &&
               Known.Name.All ( Ch => "0123456789abcdef".Contains ( Ch ) ) )
            break;

          return Known.Name;
        }
      }

      // Fall back to hex
      return $"#{C.R:X2}{C.G:X2}{C.B:X2}";
    }

    private void Refresh_Preview ( )
    {
      _Preview_Panel.Invalidate ( );
    }

    // ── preview painting ─────────────────────────────────────
    private void Preview_Panel_Paint ( object? Sender, PaintEventArgs E )
    {
      var G = E.Graphics;
      var W = _Preview_Panel.Width;
      var H = _Preview_Panel.Height;

      // Background
      G.Clear ( _Working.Background );

      // Grid lines
      using var Grid_Pen = new Pen ( _Working.Grid, 1 );
      for ( int X = 0; X < W; X += 20 )
        G.DrawLine ( Grid_Pen, X, 0, X, H );
      for ( int Y = 0; Y < H; Y += 15 )
        G.DrawLine ( Grid_Pen, 0, Y, W, Y );

      // Separator
      using var Sep_Pen = new Pen ( _Working.Separator, 2 );
      G.DrawLine ( Sep_Pen, 0, H / 2, W, H / 2 );

      // Fake data lines
      var Random = new Random ( 42 );
      for ( int Li = 0; Li < Math.Min ( 4, _Working.Line_Colors.Length ); Li++ )
      {
        using var Line_Pen = new Pen ( _Working.Line_Colors [ Li ], 2 );
        int Prev_X = 0;
        int Prev_Y = H / 4 + Li * 8 + Random.Next ( -4, 4 );
        for ( int X = 10; X < W; X += 10 )
        {
          int Y = Math.Clamp ( Prev_Y + Random.Next ( -6, 7 ), 4, H - 4 );
          G.DrawLine ( Line_Pen, Prev_X, Prev_Y, X, Y );
          Prev_X = X;
          Prev_Y = Y;
        }
      }

      // Label sample text
      using var Font = new Font ( "Segoe UI", 7f );
      using var Brush = new SolidBrush ( _Working.Labels );
      G.DrawString ( "Preview", Font, Brush, 4, 4 );
    }

    // ── colour-picker wiring ─────────────────────────────────
    private void Pick_Colour ( Action<Color> Apply, Color Current )
    {
      using var Dlg = new ColorDialog
      {
        Color = Current,
        FullOpen = true,
        AllowFullOpen = true,
      };
      if ( Dlg.ShowDialog ( this ) == DialogResult.OK )
      {
        Apply ( Dlg.Color );
        Refresh_All_Swatches ( );
        Refresh_Preview ( );
      }
    }

    // Wire up each button in the constructor after Build_UI ──
    protected override void OnLoad ( EventArgs E )
    {
      base.OnLoad ( E );
      Wire_Colour_Buttons ( );
    }

    private void Wire_Colour_Buttons ( )
    {
      _Btn_Background.Click += ( s, e ) =>
        Pick_Colour ( C => _Working.Background = C, _Working.Background );

      _Btn_Foreground.Click += ( s, e ) =>
        Pick_Colour ( C => _Working.Foreground = C, _Working.Foreground );

      _Btn_Grid.Click += ( s, e ) =>
        Pick_Colour ( C => _Working.Grid = C, _Working.Grid );

      _Btn_Labels.Click += ( s, e ) =>
        Pick_Colour ( C => _Working.Labels = C, _Working.Labels );

      _Btn_Separator.Click += ( s, e ) =>
        Pick_Colour ( C => _Working.Separator = C, _Working.Separator );
    }

    private void Pick_Colour_For_Line ( int Index )
    {
      Pick_Colour (
        C =>
        {
          var New_Colors = (Color [ ]) _Working.Line_Colors.Clone ( );
          New_Colors [ Index ] = C;
          _Working.Line_Colors = New_Colors;
        },
        _Working.Line_Colors [ Index ] );
    }

    // ── preset ───────────────────────────────────────────────
    private void Apply_Preset ( Chart_Theme Preset )
    {
      _Working = Deep_Copy ( Preset );
      _Txt_Name.Text = _Working.Name;
      Refresh_All_Swatches ( );
      Refresh_Preview ( );
    }

    // ── OK ───────────────────────────────────────────────────
    private void Btn_OK_Click ( object? Sender, EventArgs E )
    {
      _Working.Name = _Txt_Name.Text.Trim ( );
      if ( string.IsNullOrEmpty ( _Working.Name ) )
        _Working.Name = "Custom";

      Result_Theme = _Working;
      DialogResult = DialogResult.OK;
      Close ( );
    }

    // ── deep copy helper ─────────────────────────────────────
    private static Chart_Theme Deep_Copy ( Chart_Theme Src )
    {
      return new Chart_Theme
      {
        Name = Src.Name,
        Background = Src.Background,
        Foreground = Src.Foreground,
        Grid = Src.Grid,
        Labels = Src.Labels,
        Separator = Src.Separator,
        Line_Colors = (Color [ ]) Src.Line_Colors.Clone ( ),
      };
    }
  }
}
