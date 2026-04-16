
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    GPU_Monitor_Form.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Floating always-visible tool window that displays live GPU telemetry
//   sourced from a GPU_Monitor background polling object.  The form owns no
//   polling logic of its own — it subscribes to GPU_Monitor.Data_Updated and
//   renders whatever GPU_Stats the monitor delivers every two seconds.
//
// WINDOW CHARACTERISTICS
//   Style     SizableToolWindow (thin border, no taskbar entry)
//   Size      340 × 420 px, minimum 300 × 380 px
//   Position  Bottom-right of the primary screen's working area (20 px inset)
//             on construction; user may drag it freely thereafter
//   TopMost   false — sits in normal Z-order, not forced above all windows
//   Colors    Dark charcoal background (24, 27, 31) / WhiteSmoke foreground;
//             hardcoded rather than theme-driven so the monitor window always
//             reads clearly regardless of the active application theme
//   Close     The system close button is suppressed via CreateParams
//             (CP_NOCLOSE_BUTTON = 0x200) so the user cannot accidentally
//             dismiss it — the owner form controls its lifetime
//
// LAYOUT  (built entirely in code by Build_UI())
//
//   Header row
//     _GPU_Name_Label   Adapter name from GPU_Stats.GPU_Name; "Detecting GPU…"
//                       until first data arrives; "No GPU detected" if
//                       Stats.Is_Available is false.  Bold, LimeGreen.
//     _Backend_Label    Render backend string ("Renderer: {Stats.Render_Backend}").
//                       Regular weight, gray.
//
//   Separator (thin 1 px Panel, color 60-60-70)
//
//   Core load section
//     _Core_Load_Label  "NN.N %" — color from Load_Color().
//     _Core_Bar         ProgressBar 310 × 8 px, Continuous style, 0–100.
//
//   Memory load section
//     _Memory_Load_Label  "NN.N %" — color from Load_Color().
//     _Memory_Bar         Same dimensions as core bar.
//
//   Separator
//
//   Stats grid (two-column: key label Col1=10, value label Col2=170)
//     _Core_Clock_Label    "{F0} MHz"   or "---"
//     _Memory_Clock_Label  "{F0} MHz"   or "---"
//     _Temp_Label          "{F0} °C"    or "---"; color: Red > 80°C,
//                          Orange > 65°C, LimeGreen otherwise
//     _Power_Label         "{F1} W"     or "---"
//     _Memory_Used_Label   "{F0} / {F0} MB" when both used and total are
//                          available, "{F0} MB" when only used is available,
//                          "---" otherwise
//     _Fan_Label           "{F0} RPM"   or "---"
//
//   Separator
//
//   Footer note  "Polling every 2s on background thread."
//                Italic, 7.5pt, gray.
//
// DATA FLOW
//   GPU_Monitor fires Data_Updated on its background thread.  On_Data_Updated()
//   receives the event, guards against a disposed form, then marshals to the
//   UI thread via BeginInvoke() (non-blocking) before calling Apply_Stats().
//   Apply_Stats() updates every label and both progress bars in a single pass.
//   If Stats.Is_Available is false only the name label is updated; all other
//   controls retain their last values.
//
// COLOR CODING
//   Load_Color( float Load ) → Color
//     > 85 %  Red      — near saturation
//     > 60 %  Orange   — moderately loaded
//     > 30 %  LimeGreen — normal activity
//     ≤ 30 %  Gray     — idle
//   Applied to _Core_Load_Label and _Memory_Load_Label on every update.
//
//   Temperature coloring (inline in Apply_Stats):
//     > 80 °C  Red
//     > 65 °C  Orange
//     ≤ 65 °C  LimeGreen
//
// UI HELPERS  (private, called only from Build_UI)
//   Make_Label( text, x, y, w, h, size, style, fg ) → Label
//     Creates a fixed-size Consolas label, adds it to Controls, and returns
//     the reference so the caller can store it in a field.  ForeColor defaults
//     to WhiteSmoke when fg is not supplied.
//
//   Add_Key_Label( text, x, y )
//     Creates a non-stored 155 × 18 Segoe UI label in LightSlateGray for the
//     left-column descriptors in the stats grid.
//
//   Add_Sep( y )
//     Adds a 310 × 1 Panel in color (60, 60, 70) as a horizontal rule.
//
// LIFETIME
//   The form is constructed by the owner form with a GPU_Monitor reference.
//   It subscribes to Data_Updated in the constructor and unsubscribes in
//   OnFormClosing() before calling base, ensuring no callbacks arrive after
//   disposal.  The close button is suppressed so the owner remains in control
//   of when the window is shown and hidden.
//
// NOTES
//   • All label fields are assigned in Build_UI() and never null after
//     construction; no null guards are needed in Apply_Stats().
//   • ProgressBar.Value is clamped with Math.Clamp before assignment to
//     prevent ArgumentException from out-of-range sensor spikes.
//   • The form does not apply the application Chart_Theme; its colors are
//     intentionally hardcoded so it remains legible regardless of which theme
//     the main window is using.
//   • The form is a partial class; the designer file contributes only the
//     InitializeComponent stub — all controls are created programmatically
//     in Build_UI().
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════

namespace Multimeter_Controller
{
  public partial class GPU_Monitor_Form : Form
  {
    private readonly GPU_Monitor _Monitor;

    // ── Labels ────────────────────────────────────────────────────────
    private Label _GPU_Name_Label;
    private Label _Backend_Label;
    private Label _Core_Load_Label;
    private Label _Memory_Load_Label;
    private Label _Core_Clock_Label;
    private Label _Memory_Clock_Label;
    private Label _Temp_Label;
    private Label _Power_Label;
    private Label _Memory_Used_Label;
    private Label _Fan_Label;

    // ── Load bars ─────────────────────────────────────────────────────
    private ProgressBar _Core_Bar;
    private ProgressBar _Memory_Bar;

    public GPU_Monitor_Form( GPU_Monitor Monitor )
    {
      _Monitor = Monitor;

      Text = "GPU Monitor";
      Size = new Size( 340, 420 );
      MinimumSize = new Size( 300, 380 );
      FormBorderStyle = FormBorderStyle.SizableToolWindow;
      StartPosition = FormStartPosition.Manual;
      TopMost = false;
      BackColor = Color.FromArgb( 24, 27, 31 );
      ForeColor = Color.WhiteSmoke;

      // Position in bottom-right of screen
      var Work_Area = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
      Location = new Point( Work_Area.Right - Width - 20,
                            Work_Area.Bottom - Height - 20 );

      Build_UI();

      _Monitor.Data_Updated += On_Data_Updated;
    }


    protected override CreateParams CreateParams
    {
      get
      {
        const int CP_NOCLOSE_BUTTON = 0x200;
        CreateParams Params = base.CreateParams;
        Params.ClassStyle |= CP_NOCLOSE_BUTTON;
        return Params;
      }
    }

    private void Build_UI()
    {
      int Y = 10;
      int LH = 24;
      int Col1 = 10;
      int Col2 = 170;

      // ── Header ────────────────────────────────────────────────────
      _GPU_Name_Label = Make_Label( "Detecting GPU...", Col1, Y,
                                    300, 20, 9f, FontStyle.Bold,
                                    Color.LimeGreen );
      Y += LH + 4;

      _Backend_Label = Make_Label( "", Col1, Y, 300, 18, 8f,
                                    FontStyle.Regular, Color.Gray );
      Y += LH + 8;

      // ── Separator ─────────────────────────────────────────────────
      Add_Sep( Y );
      Y += 12;

      // ── Core load ─────────────────────────────────────────────────
      Add_Key_Label( "GPU Core:", Col1, Y );
      _Core_Load_Label = Make_Label( "---", Col2, Y, 80, 18,
                                      8.5f, FontStyle.Bold,
                                      Color.LimeGreen );
      Y += LH;

      _Core_Bar = new ProgressBar
      {
        Location = new Point( Col1, Y ),
        Size = new Size( 310, 8 ),
        Minimum = 0,
        Maximum = 100,
        Style = ProgressBarStyle.Continuous,
      };
      Controls.Add( _Core_Bar );
      Y += 14;

      // ── Memory load ───────────────────────────────────────────────
      Add_Key_Label( "GPU Memory:", Col1, Y );
      _Memory_Load_Label = Make_Label( "---", Col2, Y, 80, 18,
                                        8.5f, FontStyle.Bold,
                                        Color.CornflowerBlue );
      Y += LH;

      _Memory_Bar = new ProgressBar
      {
        Location = new Point( Col1, Y ),
        Size = new Size( 310, 8 ),
        Minimum = 0,
        Maximum = 100,
        Style = ProgressBarStyle.Continuous,
      };
      Controls.Add( _Memory_Bar );
      Y += 18;

      Add_Sep( Y );
      Y += 12;

      // ── Stats grid ────────────────────────────────────────────────
      Add_Key_Label( "Core Clock:", Col1, Y );
      _Core_Clock_Label = Make_Label( "---", Col2, Y, 120, 18 );
      Y += LH;

      Add_Key_Label( "Mem Clock:", Col1, Y );
      _Memory_Clock_Label = Make_Label( "---", Col2, Y, 120, 18 );
      Y += LH;

      Add_Key_Label( "Temperature:", Col1, Y );
      _Temp_Label = Make_Label( "---", Col2, Y, 120, 18 );
      Y += LH;

      Add_Key_Label( "Power:", Col1, Y );
      _Power_Label = Make_Label( "---", Col2, Y, 120, 18 );
      Y += LH;

      Add_Key_Label( "VRAM Used:", Col1, Y );
      _Memory_Used_Label = Make_Label( "---", Col2, Y, 120, 18 );
      Y += LH;

      Add_Key_Label( "Fan Speed:", Col1, Y );
      _Fan_Label = Make_Label( "---", Col2, Y, 120, 18 );
      Y += LH + 8;

      Add_Sep( Y );
      Y += 12;

      // ── Note ──────────────────────────────────────────────────────
      Make_Label( "Polling every 2s on background thread.",
                  Col1, Y, 300, 18, 7.5f,
                  FontStyle.Italic, Color.Gray );
    }

    private void On_Data_Updated( object? Sender, GPU_Stats Stats )
    {
      if (IsDisposed)
        return;

      // Marshal to UI thread — BeginInvoke so it never blocks
      // the background monitor thread
      BeginInvoke( () => Apply_Stats( Stats ) );
    }

    private void Apply_Stats( GPU_Stats Stats )
    {
      if (IsDisposed)
        return;

      if (!Stats.Is_Available)
      {
        _GPU_Name_Label.Text = "No GPU detected";
        return;
      }

      _GPU_Name_Label.Text = Stats.GPU_Name;
      _Backend_Label.Text = $"Renderer: {Stats.Render_Backend}";

      // ── Core load ─────────────────────────────────────────────────
      _Core_Load_Label.Text = $"{Stats.Core_Load:F1} %";
      _Core_Load_Label.ForeColor = Load_Color( Stats.Core_Load );
      _Core_Bar.Value = (int) Math.Clamp( Stats.Core_Load, 0, 100 );

      // ── Memory load ───────────────────────────────────────────────
      _Memory_Load_Label.Text = $"{Stats.Memory_Load:F1} %";
      _Memory_Load_Label.ForeColor = Load_Color( Stats.Memory_Load );
      _Memory_Bar.Value = (int) Math.Clamp( Stats.Memory_Load, 0, 100 );

      // ── Clocks ────────────────────────────────────────────────────
      _Core_Clock_Label.Text = Stats.Core_Clock > 0
                                 ? $"{Stats.Core_Clock:F0} MHz" : "---";
      _Memory_Clock_Label.Text = Stats.Memory_Clock > 0
                                 ? $"{Stats.Memory_Clock:F0} MHz" : "---";

      // ── Temperature ───────────────────────────────────────────────
      _Temp_Label.Text = Stats.Temperature > 0
                              ? $"{Stats.Temperature:F0} °C" : "---";
      _Temp_Label.ForeColor = Stats.Temperature > 80 ? Color.Red
                            : Stats.Temperature > 65 ? Color.Orange
                            : Color.LimeGreen;

      // ── Power ─────────────────────────────────────────────────────
      _Power_Label.Text = Stats.Power > 0
                          ? $"{Stats.Power:F1} W" : "---";

      // ── VRAM ──────────────────────────────────────────────────────
      _Memory_Used_Label.Text = Stats.Memory_Used > 0 && Stats.Memory_Total > 0
                                ? $"{Stats.Memory_Used:F0} / {Stats.Memory_Total:F0} MB"
                                : Stats.Memory_Used > 0
                                ? $"{Stats.Memory_Used:F0} MB"
                                : "---";

      // ── Fan ───────────────────────────────────────────────────────
      _Fan_Label.Text = Stats.Fan_Speed > 0
                        ? $"{Stats.Fan_Speed:F0} RPM" : "---";
    }

    // ── Color coding ──────────────────────────────────────────────────
    private static Color Load_Color( float Load ) =>
        Load > 85 ? Color.Red
      : Load > 60 ? Color.Orange
      : Load > 30 ? Color.LimeGreen
      : Color.Gray;

    // ── UI helpers ────────────────────────────────────────────────────
    private Label Make_Label( string Text, int X, int Y,
                               int W, int H,
                               float Size = 8.5f,
                               FontStyle Style = FontStyle.Regular,
                               Color? FG = null )
    {
      var L = new Label
      {
        Text = Text,
        Location = new Point( X, Y ),
        Size = new Size( W, H ),
        Font = new Font( "Consolas", Size, Style ),
        ForeColor = FG ?? Color.WhiteSmoke,
        AutoSize = false,
      };
      Controls.Add( L );
      return L;
    }

    private void Add_Key_Label( string Text, int X, int Y )
    {
      Controls.Add( new Label
      {
        Text = Text,
        Location = new Point( X, Y ),
        Size = new Size( 155, 18 ),
        Font = new Font( "Segoe UI", 8.5f ),
        ForeColor = Color.LightSlateGray,
        AutoSize = false,
      } );
    }

    private void Add_Sep( int Y )
    {
      Controls.Add( new Panel
      {
        Location = new Point( 10, Y ),
        Size = new Size( 310, 1 ),
        BackColor = Color.FromArgb( 60, 60, 70 ),
      } );
    }

    protected override void OnFormClosing( FormClosingEventArgs E )
    {
      _Monitor.Data_Updated -= On_Data_Updated;
   
      base.OnFormClosing( E );
    }
  }
}
