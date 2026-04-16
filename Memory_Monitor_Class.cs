// ════════════════════════════════════════════════════════════════════════════════
// FILE:    Memory_Monitor.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Self-contained floating memory monitor that displays live process memory
//   statistics for the running application, refreshed every second via a
//   WinForms Timer.  The monitor owns its Form, RichTextBox, and Timer
//   internally and cleans them up when the window is closed.  The owner form
//   interacts only through Show(), Close(), and Is_Open.
//
// WINDOW CHARACTERISTICS
//   Style       Sizable, no control box (ControlBox = false — no title-bar
//               buttons; the window can only be closed programmatically via
//               Close() or by the OS)
//   Size        480 × 520 px, minimum 320 × 300 px
//   Position    CenterParent on first show
//   Content     Single RichTextBox docked Fill, black background, Consolas 9pt,
//               vertical scroll bar, read-only
//   Singleton   Show() brings the existing window to front if it is already
//               open rather than creating a second instance
//
// METRICS DISPLAYED  (refreshed every 1000 ms)
//
//   Working Set         Process.WorkingSet64 / 1 048 576 → MB
//                       Physical RAM currently committed to this process.
//                       Displayed large (14pt bold) as the primary indicator.
//
//   Peak Working Set    Process.PeakWorkingSet64 / 1 048 576 → MB
//                       Highest working set seen since process start.
//
//   Managed Heap (GC)   GC.GetTotalMemory(false) / 1 048 576 → MB
//                       Bytes allocated on the managed heap.  forceFullCollection
//                       is false so the call is non-blocking.
//
//   GC Collections      GC.CollectionCount(0/1/2) for Gen0, Gen1, Gen2.
//                       Cumulative counts since process start.
//
//   Trend               Derived from the last 5 history samples:
//                         Delta > 20 MB  "▲▲ Rising Fast"  Red
//                         Delta > 5 MB   "▲  Rising"       Goldenrod
//                         Delta < −5 MB  "▼  Falling"      LimeGreen
//                         otherwise      "─  Stable"       DodgerBlue
//                       Shows "...Sampling" in Gray until 5 samples exist.
//
//   Last 60s Spark Line Unicode block-element spark line (▁▂▃▄▅▆▇█) showing
//                       working-set trend over the rolling 60-second window.
//                       Each character maps one history sample to one of 8
//                       height levels via linear interpolation between the
//                       window's min and max values.  A flat window (all values
//                       equal) renders as a solid mid-height line of ▄.
//
// HISTORY BUFFER
//   _History    Queue<(DateTime Time, long MB)>, capped at Max_History = 60.
//               One entry is enqueued per Refresh() tick (every 1 s), giving
//               a 60-second rolling window.  The queue is cleared on form close
//               so a re-opened window starts fresh.
//
// REFRESH FLOW
//   1. Timer.Tick fires Refresh() on the UI thread (WinForms Timer).
//   2. Refresh() reads Process and GC metrics, enqueues the current MB value,
//      trims the history queue to Max_History, computes trend and spark line,
//      clears the RichTextBox, then re-renders the full display in one pass
//      using the Append() helper.
//   3. Refresh() is also called once immediately after Show() so the display
//      is populated before the first timer tick.
//   4. Refresh() guards against a null or disposed RichTextBox at entry so
//      late-firing ticks after form close are silently ignored.
//
// SPARK LINE ALGORITHM  (Build_Spark_Line)
//   Input   List<long> of MB values from the history queue.
//   Output  String of Unicode block characters, one per sample.
//   Steps:
//     1. Find Min and Max across all values.
//     2. If Range == 0, return a solid string of ▄ (index 3 of 8).
//     3. For each value V, compute index = (V − Min) / Range × 7, clamped
//        to [0, 7], and map to the corresponding block character.
//   Returns "..." if fewer than 2 values are present.
//
// LIFETIME / CLEANUP
//   Show( Form Owner )
//     Creates the Form, RichTextBox, and Timer.  Registers On_Form_Closed
//     on FormClosed.  Calls Refresh() immediately after Form.Show().
//
//   On_Form_Closed( sender, e )
//     Stops and disposes the Timer, nulls _Timer / _Form / _Rtb, and clears
//     _History.  Called on any close reason (user Alt+F4, owner calling
//     Close(), application exit).
//
//   Close()
//     Public method for the owner to programmatically close the window.
//     Safe to call when the window is not open (_Form?.Close() no-ops on null).
//
//   Is_Open → bool
//     Returns true only when _Form is non-null and not disposed.  Safe to
//     poll from the owner at any time.
//
// RENDERING HELPERS
//   Append( string Text, Font Fnt, Color Clr )
//     Sets SelectionFont and SelectionColor then calls AppendText() on the
//     RichTextBox.  All text is appended in a single left-to-right pass per
//     Refresh() cycle; no Selection manipulation of previously written text
//     is performed.  Guards against null/disposed RichTextBox.
//
// FONT USAGE
//   Section headers   Consolas 9pt Bold, Silver
//   Primary value     Consolas 14pt Bold, White  (working set)
//   Secondary values  Consolas 11pt Regular, LightGray
//   Trend value       Consolas 11pt Bold, trend-specific color
//   Spark line        Consolas 11pt Regular, DodgerBlue
//   Timestamp         Consolas 9pt Regular, DarkGray
//   Title             Consolas 11pt Bold, White
//
// NOTES
//   • All fields are nullable and null-guarded; the class is safe to
//     instantiate once and reuse across multiple Show/Close cycles.
//   • The Timer is a System.Windows.Forms.Timer (UI-thread affine) so no
//     cross-thread marshalling is needed in Refresh().
//   • WorkingSet64 reflects physical RAM pressure including shared pages;
//     GC.GetTotalMemory reports only managed allocations.  Both are useful
//     but measure different things — the display shows both without conflating
//     them.
//   • GC collection counts are cumulative since process start, not since the
//     monitor was opened.  They are most useful for spotting unexpected Gen2
//     collections during a polling session.
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════





using System.Diagnostics;


namespace Multimeter_Controller
{
  public class Memory_Monitor
  {
    // ── Private fields ─────────────────────────────────────────────────
    private Form _Form = null;
    private RichTextBox _Rtb = null;
    private System.Windows.Forms.Timer _Timer = null;
    private readonly Queue<(DateTime Time, long MB)> _History = new Queue<(DateTime, long)>();

    private const int Max_History = 60;
    private const int Refresh_Interval_MS = 1000;
    public bool Is_Open => _Form != null && !_Form.IsDisposed;

    public void Close()
    {
      _Form?.Close();
    }


    // ── Public launch method ───────────────────────────────────────────
    public void Show(Form Owner)
    {
      if (_Form != null && !_Form.IsDisposed)
      {
        _Form.BringToFront();
        return;
      }

      _Form = new Form
      {
        Text = "Memory Monitor",
        Size = new Size(480, 520),
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.Sizable,
        MinimumSize = new Size(320, 300),
        Owner = Owner,
        ControlBox = false,
      };

      _Rtb = new RichTextBox
      {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BackColor = Color.Black,
        Font = new Font("Consolas", 9f),
        ScrollBars = RichTextBoxScrollBars.Vertical,
      };

      _Form.Controls.Add(_Rtb);

      _Timer = new System.Windows.Forms.Timer();
      _Timer.Interval = Refresh_Interval_MS;
      _Timer.Tick += (s, e) => Refresh();
      _Timer.Start();

      _Form.FormClosed += On_Form_Closed;

      _Form.Show(Owner);
      Refresh();   // populate immediately
    }

    // ── Cleanup ────────────────────────────────────────────────────────
    private void On_Form_Closed(object sender, FormClosedEventArgs e)
    {
      _Timer?.Stop();
      _Timer?.Dispose();
      _Timer = null;
      _Form = null;
      _Rtb = null;
      _History.Clear();
    }

    // ── Core refresh ──────────────────────────────────────────────────
    private void Refresh()
    {
      if (_Rtb == null || _Rtb.IsDisposed)
        return;

      var Proc = Process.GetCurrentProcess();
      long MB = Proc.WorkingSet64 / 1_048_576;
      long Peak_MB = Proc.PeakWorkingSet64 / 1_048_576;
      long GC_MB = GC.GetTotalMemory(false) / 1_048_576;
      int Gen0 = GC.CollectionCount(0);
      int Gen1 = GC.CollectionCount(1);
      int Gen2 = GC.CollectionCount(2);

      // ── Track history ──────────────────────────────────────────────
      _History.Enqueue((DateTime.Now, MB));
      if (_History.Count > Max_History)
        _History.Dequeue();

      // ── Compute trend ──────────────────────────────────────────────
      string Trend_Symbol;
      Color Trend_Color;

      if (_History.Count >= 5)
      {
        var Recent = _History.TakeLast(5).ToList();
        long Delta = Recent.Last().MB - Recent.First().MB;

        if (Delta > 20)
        {
          Trend_Symbol = "▲▲ Rising Fast";
          Trend_Color = Color.Red;
        }
        else if (Delta > 5)
        {
          Trend_Symbol = "▲  Rising";
          Trend_Color = Color.Goldenrod;
        }
        else if (Delta < -5)
        {
          Trend_Symbol = "▼  Falling";
          Trend_Color = Color.LimeGreen;
        }
        else
        {
          Trend_Symbol = "─  Stable";
          Trend_Color = Color.DodgerBlue;
        }
      }
      else
      {
        Trend_Symbol = "...Sampling";
        Trend_Color = Color.Gray;
      }

      string Spark = Build_Spark_Line(_History.Select(h => h.MB).ToList());

      // ── Render ─────────────────────────────────────────────────────
      _Rtb.Clear();

      Append("  Memory Monitor\n",
              new Font("Consolas", 11f, FontStyle.Bold), Color.White);
      Append($"  {DateTime.Now:HH:mm:ss}\n\n",
              new Font("Consolas", 9f), Color.DarkGray);

      Append("  Working Set\n",
              new Font("Consolas", 9f, FontStyle.Bold), Color.Silver);
      Append($"  {MB} MB\n\n",
              new Font("Consolas", 14f, FontStyle.Bold), Color.White);

      Append("  Peak Working Set\n",
              new Font("Consolas", 9f, FontStyle.Bold), Color.Silver);
      Append($"  {Peak_MB} MB\n\n",
              new Font("Consolas", 11f), Color.LightGray);

      Append("  Managed Heap (GC)\n",
              new Font("Consolas", 9f, FontStyle.Bold), Color.Silver);
      Append($"  {GC_MB} MB\n\n",
              new Font("Consolas", 11f), Color.LightGray);

      Append("  GC Collections\n",
              new Font("Consolas", 9f, FontStyle.Bold), Color.Silver);
      Append($"  Gen0: {Gen0}   Gen1: {Gen1}   Gen2: {Gen2}\n\n",
              new Font("Consolas", 9f), Color.LightGray);

      Append("  Trend\n",
              new Font("Consolas", 9f, FontStyle.Bold), Color.Silver);
      Append($"  {Trend_Symbol}\n\n",
              new Font("Consolas", 11f, FontStyle.Bold), Trend_Color);

      Append("  Last 60s\n",
              new Font("Consolas", 9f, FontStyle.Bold), Color.Silver);
      Append($"  {Spark}\n",
              new Font("Consolas", 11f), Color.DodgerBlue);
    }

    // ── Spark line builder ─────────────────────────────────────────────
    private static string Build_Spark_Line(List<long> Values)
    {
      if (Values.Count < 2)
        return "...";

      char[] Sparks = { '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };
      long Min = Values.Min();
      long Max = Values.Max();
      long Range = Max - Min;

      if (Range == 0)
        return new string('▄', Values.Count);

      return new string(Values.Select(V =>
      {
        int Idx = (int)((V - Min) / (double)Range * (Sparks.Length - 1));
        return Sparks[Math.Clamp(Idx, 0, Sparks.Length - 1)];
      }).ToArray());
    }

    // ── Append helper ──────────────────────────────────────────────────
    private void Append(string Text, Font Fnt, Color Clr)
    {
      if (_Rtb == null || _Rtb.IsDisposed)
        return;

      _Rtb.SelectionFont = Fnt;
      _Rtb.SelectionColor = Clr;
      _Rtb.SelectionIndent = 0;
      _Rtb.SelectionRightIndent = 0;
      _Rtb.AppendText(Text);
    }
  }
}
