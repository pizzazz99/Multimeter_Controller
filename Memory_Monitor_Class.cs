using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
