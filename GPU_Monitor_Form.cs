
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    GPU_Monitor_Form.cs
// PROJECT: Multimeter_Controller
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
