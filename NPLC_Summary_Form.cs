using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimeter_Controller
{

  public class NPLC_Summary_Form : Form
  {
    public NPLC_Summary_Form ( List<Instrument_Series> Series )
    {
      Text = "NPLC Summary";
      AutoScroll = true;
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      MinimizeBox = false;
      StartPosition = FormStartPosition.CenterParent;
      Width = 320;

      var Panel = new FlowLayoutPanel
      {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        AutoScroll = true,
        WrapContents = false,
        Padding = new Padding ( 10 ),
      };


      foreach ( var S in Series )
      {
        var Group = new GroupBox
        {
          Text = $"{S.Name}  GPIB: {S.Address}",
          Width = 280,
          Margin = new Padding ( 0, 0, 0, 10 ),
        };

        // take account for the specific meter that has 10 digits
        string Digit_Text = S.Type == Meter_Type.HP3458 && S.Display_Digits == 10
    ? "10"
    : $"{S.Display_Digits}.5";


        int Y = 22;
        Add_Row ( Group, "Max NPLC:", $"{S.NPLC}", ref Y );
        Add_Row ( Group, "Digits:", Digit_Text, ref Y );
        Add_Row ( Group, "Integration:", $"{S.Integration_Ms:F0} ms", ref Y );
        Add_Row ( Group, "Settle Time:", $"{S.Settle_Ms:F0} ms", ref Y );
        Add_Row ( Group, "Rate:", $"~{S.Get_Readings_Per_Min ( Series.Count )}/min", ref Y );
        Add_Row ( Group, "Warning/Info:", S.NPLC_Warning_Text, ref Y, S.NPLC_Warning_Color );

        Group.Height = Y + 12;
        Panel.Controls.Add ( Group );
      }


      int Total_Height = Series.Count * 200 + 60;

      if ( Series.Count > 1 )
      {
        var Slowest = Series.OrderByDescending ( S => S.Settle_Ms ).First ( );
        var Fastest = Series.OrderBy ( S => S.Settle_Ms ).First ( );
        int Session_Rate = Slowest.Get_Readings_Per_Min ( Series.Count );
        bool All_Same = Series.All ( S => S.NPLC == Slowest.NPLC );

        var Footer = new GroupBox
        {
          Text = "Session Summary",
          Width = 280,
          Margin = new Padding ( 0, 0, 0, 10 ),
        };

        int Y = 22;
        Add_Row ( Footer, "Session Rate:", $"~{Session_Rate}/min", ref Y, Slowest.NPLC_Warning_Color );

        if ( !All_Same )
        {
          Add_Row ( Footer, "Slowest:", $"{Slowest.Name} (NPLC {Slowest.NPLC})", ref Y );
          Add_Row ( Footer, "Fastest:", $"{Fastest.Name} (NPLC {Fastest.NPLC})", ref Y );
          Add_Row ( Footer, "Note:", "Rate limited by slowest instrument", ref Y, Color.Gray );
        }

        Footer.Height = Y + 12;
        Total_Height += 200;
        Panel.Controls.Add ( Footer );
      }

      Height = Math.Min ( Total_Height, Screen.PrimaryScreen.WorkingArea.Height - 100 );
      Controls.Add ( Panel );

    }


    private void Add_Row ( GroupBox Parent, string Label, string Value,
                           ref int Y, Color? Value_Color = null )
    {
      Parent.Controls.Add ( new Label
      {
        Text = Label,
        Left = 10,
        Top = Y,
        Width = 100,
        AutoSize = false,
      } );

      Parent.Controls.Add ( new Label
      {
        Text = Value,
        Left = 115,
        Top = Y,
        Width = 155,
        AutoSize = false,
        ForeColor = Value_Color ?? SystemColors.ControlText,
      } );

      Y += 24;
    }
  }

  public class NPLC_Info_Form : Form
  {
    public NPLC_Info_Form ( )
    {
      Text = "NPLC Reference";
      Width = 670;
      Height = 500;
      MinimumSize = new Size ( 670, 500 );
      FormBorderStyle = FormBorderStyle.SizableToolWindow;
      StartPosition = FormStartPosition.CenterParent;

      var RTB = new RichTextBox
      {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BackColor = SystemColors.Control,
        ForeColor = Color.Black,
        Font = new Font ( "Courier New", 9.5f ),
        BorderStyle = BorderStyle.None,
        ScrollBars = RichTextBoxScrollBars.Vertical,
        Padding = new Padding ( 10 ),
      };

      Controls.Add ( RTB );
      Build ( RTB );
    }

    private void Build ( RichTextBox R )
    {
      R.SelectionIndent = 15;   // pixels from left edge

      void Header ( string Text )
      {
        R.SelectionFont = new Font ( "Courier New", 10.5f, FontStyle.Bold );
        R.SelectionColor = Color.Black;
        R.AppendText ( Text + "\n" );
        R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Regular );
        R.SelectionColor = Color.Black;
      }

      void Body ( string Text )
      {
        R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Regular );
        R.SelectionColor = Color.Black;
        R.AppendText ( Text + "\n" );
      }

      void Bullet ( string Label, string Text )
      {
        R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Bold );
        R.SelectionColor = Color.Black;
        R.AppendText ( $"  {Label}  " );
        R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Regular );
        R.SelectionColor = Color.Black;
        R.AppendText ( Text + "\n" );
      }

      void Table_Row ( string Col1, string Col2, string Col3, bool Is_Header = false )
      {
        FontStyle Style = Is_Header ? FontStyle.Bold : FontStyle.Regular;
        R.SelectionFont = new Font ( "Courier New", 9.5f, Style );
        R.SelectionColor = Color.Black;
        R.AppendText ( $"  {Col1,-8}  {Col2,-20}  {Col3}\n" );
      }

      void Gap ( ) => R.AppendText ( "\n" );

   
      Gap ( );
      Header ( "NPLC - Number of Power Line Cycles" );
      Gap ( );
      Body ( "NPLC is a dimensionless integer or fraction that specifies the" );
      Body ( "duration of a single measurement integration period, expressed" );
      Body ( "as a multiple of the period of the local AC mains frequency." );
      Body ( "It is the primary control parameter of an integrating analog-to-" );
      Body ( "digital converter in a precision digital multimeter, governing" );
      Body ( "the trade-off between measurement speed and noise rejection." );
      Gap ( );

      Gap ( );
      Header ( "Integration Window" );
      Gap ( );
      Body ( "At 60 Hz mains, one power line cycle is 16.667 ms." );
      Body ( "At 50 Hz mains, one power line cycle is 20.000 ms." );
      Body ( "The integration window in absolute time is therefore:" );
      Gap ( );
      Body ( "   T_integration = NPLC x (1 / f_line)" );
      Gap ( );

      Gap ( );
      Header ( "Why Power Line Cycles?" );
      Gap ( );
      Body ( "The choice of a power line cycle as the base unit is not" );
      Body ( "arbitrary. It is the fundamental period of the dominant" );
      Body ( "interference source in laboratory and industrial measurement" );
      Body ( "environments. By integrating over exactly one or more complete" );
      Body ( "cycles of that interference, the converter causes the sinusoidal" );
      Body ( "noise to sum to zero. This is formally called Normal Mode" );
      Body ( "Rejection. This rejection is theoretically perfect at integer" );
      Body ( "NPLC values and degrades at fractional values." );
      Gap ( );

      Gap ( );
      Header ( "Two Distinct Noise Rejection Mechanisms" );
      Gap ( );
      Body ( "Random noise does not cancel deterministically. It averages" );
      Body ( "down statistically at a rate proportional to the square root" );
      Body ( "of the number of cycles integrated:" );
      Gap ( );
      Body ( "   Doubling NPLC reduces random noise by sqrt(2)  = 1.41x" );
      Body ( "   1 PLC to 10 PLC reduces random noise by sqrt(10) = 3.16x" );
      Body ( "   1 PLC to 100 PLC reduces random noise by sqrt(100) = 10x" );
      Gap ( );
      Body ( "NPLC therefore simultaneously controls two distinct mechanisms:" );
      Gap ( );
      Body ( "   1. Deterministic cancellation of periodic line-frequency" );
      Body ( "      interference — theoretically perfect at integer values." );
      Gap ( );
      Body ( "   2. Statistical averaging of random noise — improves" );
      Body ( "      continuously with the square root of NPLC." );
      Gap ( );
      Gap ( );




      Body ( "NPLC controls how long the meter's A/D converter integrates the input" );
      Body ( "signal before producing a reading. The integration window is always a" );
      Body ( "precise multiple of the power line cycle — this is what makes noise" );
      Body ( "cancellation work." );
      Gap ( );
      Gap ( );
      Header ( "The Noise Problem It Solves" );
      Gap ( );
      Body ( "Mains power (50/60 Hz) radiates interference into measurement cables" );
      Body ( "and input circuitry. This noise is periodic — it repeats exactly once" );
      Body ( "per cycle. Integrating over a whole number of cycles causes the positive" );
      Body ( "and negative halves to cancel mathematically to zero. This is called" );
      Body ( "Normal Mode Rejection." );
      Gap ( );
      Gap ( );
      Header ( "Speed vs. Accuracy Trade-off" );
      Gap ( );
      Bullet ( "0.02 PLC", "333 µs window. Very fast, almost no rejection. ~4.5 digits." );
      Bullet ( "1    PLC", "One full cycle of line rejection. ~5.5-6.5 digits." );
      Bullet ( "10   PLC", "sqrt(10) noise reduction vs 1 PLC. Rated accuracy for most specs." );
      Bullet ( "100  PLC", "1.67s at 60 Hz. sqrt(100) = 10x noise reduction. Full accuracy." );
      Bullet ( "200  PLC", "3.33s (34420A only). Extra sqrt(2) matters for nanovolt signals." );
      Gap ( );

      Gap ( );
      Header ( "Integration Times at 60 Hz" );
      Gap ( );

      Table_Row ( "NPLC", "Calculation", "Integration Time", Is_Header: true );
      R.SelectionColor = Color.Black;
      R.AppendText ( "  " + new string ( '-', 48 ) + "\n" );
      Table_Row ( "0.02", "0.02  x 16.667 ms", "333 us" );
      Table_Row ( "0.2", "0.2   x 16.667 ms", "3.333 ms" );
      Table_Row ( "1", "1     x 16.667 ms", "16.667 ms" );
      Table_Row ( "2", "2     x 16.667 ms", "33.333 ms" );
      Table_Row ( "4", "4     x 16.667 ms", "66.667 ms" );
      Table_Row ( "8", "8     x 16.667 ms", "133.333 ms" );
      Table_Row ( "10", "10    x 16.667 ms", "166.667 ms" );
      Table_Row ( "16", "16    x 16.667 ms", "266.667 ms" );
      Table_Row ( "20", "20    x 16.667 ms", "333.333 ms" );
      Table_Row ( "32", "32    x 16.667 ms", "533.333 ms" );
      Table_Row ( "64", "64    x 16.667 ms", "1066.667 ms" );
      Table_Row ( "100", "100   x 16.667 ms", "1666.667 ms" );
      Table_Row ( "200", "200   x 16.667 ms", "3333.333 ms" );
      Gap ( );

      Gap ( );
      Header ( "Autozero Interaction" );
      Gap ( );
      Body ( "Autozero doubles effective measurement time — the meter takes two" );
      Body ( "integration passes per reading: one for the input, one with the" );
      Body ( "input shorted to measure offset. At 100 PLC with autozero on:" );
      Gap ( );
      R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Bold );
      R.SelectionColor = Color.Black;
      R.AppendText ( "  2 x 1667 ms = 3333 ms per reading at 60 Hz\n" );
      Gap ( );
      Body ( "Disabling autozero halves the time at the cost of drift susceptibility." );
      Gap ( );

      Gap ( );
      Header ( "Poll Interval = Integration + Overhead" );
      Gap ( );
      Bullet ( "Autozero:", "doubles integration time" );
      Bullet ( "Settling:", "amplifier settle after range change" );
      Bullet ( "A/D:", "rundown phase + output formatting" );
      Bullet ( "GPIB:", "transfer time across bus to controller" );
      Gap ( );

      R.SelectionStart = 0;
    }
  }
}
