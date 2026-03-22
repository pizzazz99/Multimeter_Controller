using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Multimeter_Controller
{
  public class Session_Settings_Form : Form
  {
    public Session_Settings_Form (
        Application_Settings Settings,
        List<Instrument> Instruments,
        Chart_Theme Theme )
    {
      Text = "Session Settings";
      Width = 660;
      Height = 700;
      MinimumSize = new Size ( 500, 400 );
      FormBorderStyle = FormBorderStyle.SizableToolWindow;
      StartPosition = FormStartPosition.CenterParent;
      Padding = new Padding ( 12, 8, 8, 8 );

      var RTB = new RichTextBox
      {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BackColor = Color.FromArgb ( 250, 250, 252 ),
        ForeColor = Color.Black,
        Font = new Font ( "Courier New", 9.5f ),
        BorderStyle = BorderStyle.None,
        ScrollBars = RichTextBoxScrollBars.Vertical,
        Padding = new Padding ( 16, 8, 8, 8 ),
      };

      var Close_Button = new Button
      {
        Text = "Close",
        Dock = DockStyle.Bottom,
        Height = 32,
      };

      Close_Button.Click += ( s, e ) => this.Close ( );

      Controls.Add ( Close_Button );
      Controls.Add ( RTB );

      Build ( RTB, Settings, Instruments, Theme );
      RTB.SelectionStart = 0;
    }


  

    private static string Color_Name ( Color C )
    {
      KnownColor [ ] Known_Colors = (KnownColor [ ]) Enum.GetValues ( typeof ( KnownColor ) );
      foreach ( KnownColor KC in Known_Colors )
      {
        Color Known = Color.FromKnownColor ( KC );
        if ( Known.IsSystemColor )
          continue;
        if ( Known.R == C.R && Known.G == C.G && Known.B == C.B )
          return KC.ToString ( );
      }
      return $"#{C.R:X2}{C.G:X2}{C.B:X2}";
    }

    private void Build (
        RichTextBox R,
        Application_Settings Settings,
        List<Instrument> Instruments,
        Chart_Theme Theme )
    {
      const string Indent = "    ";   // 4-space left margin on every line

      void Header ( string Text )
      {
        R.SelectionFont = new Font ( "Courier New", 10.5f, FontStyle.Bold );
        R.SelectionColor = Color.FromArgb ( 30, 80, 160 );
        R.AppendText ( Indent + Text + "\n" );
        R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Regular );
      }

      void Separator ( )
      {
        R.SelectionColor = Color.FromArgb ( 180, 180, 180 );
        R.AppendText ( Indent + new string ( '─', 54 ) + "\n" );
      }


      void Color_Row ( string Label, Color C, string Name = "" )
      {
        string Display = !string.IsNullOrEmpty ( Name ) ? Name
                       : C.IsNamedColor ? C.Name
                       : $"#{C.R:X2}{C.G:X2}{C.B:X2}";

        R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Regular );
        R.SelectionColor = Color.FromArgb ( 90, 90, 90 );
        R.AppendText ( $"{Indent}  {Label,-30}" );
        R.SelectionColor = C;              // ← colored swatch character
        R.AppendText ( "■  " );
        R.SelectionColor = Color.Black;    // ← name always in black
        R.AppendText ( Display + "\n" );
      }


      void Gap ( ) => R.AppendText ( "\n" );

      void Row ( string Label, string Value, Color? Value_Color = null )
      {
        R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Regular );
        R.SelectionColor = Color.FromArgb ( 90, 90, 90 );
        R.AppendText ( $"{Indent}  {Label,-30}" );
        R.SelectionColor = Value_Color ?? Color.FromArgb ( 20, 20, 20 );
        R.AppendText ( Value + "\n" );
      }

    

      // ── Instruments ───────────────────────────────────────────────
      Gap ( );
      Header ( "Instruments" );
      Separator ( );

      if ( Instruments.Count == 0 )
      {
        Row ( "Status", "No instruments configured" );
      }
      else
      {
        foreach ( var Inst in Instruments )
        {
          R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Bold );
          R.SelectionColor = Color.FromArgb ( 20, 100, 180 );
          R.AppendText ( $"{Indent}  ● {Inst.Name}\n" );
          R.SelectionFont = new Font ( "Courier New", 9.5f, FontStyle.Regular );
          R.SelectionColor = Color.Black;

          Row ( "    Type", Inst.Type.ToString ( ) );
          Row ( "    GPIB Address", $"{Inst.Address}" );
          Row ( "    Role", string.IsNullOrEmpty ( Inst.Meter_Roll ) ? "—" : Inst.Meter_Roll );
          Row ( "    NPLC", $"{Inst.NPLC}" );
          Row ( "    Display Digits", $"{Inst.Display_Digits}" );

          string Settle_Ms = ( (double) Inst.NPLC * ( 1000.0 / 60.0 ) * 2.0 ).ToString ( "F0" );
          Row ( "    Est. Settle Time", $"{Settle_Ms} ms" );
          Gap ( );
        }

        if ( Instruments.Count > 1 )
        {
          double Max_NPLC = (double) Instruments.Max ( i => i.NPLC );
          int Session_Settle = (int) ( Max_NPLC / 60.0 * 1000.0 * 2.0 );
          Row ( "Session Bottleneck NPLC", $"{Max_NPLC}  (worst-case instrument)" );
          Row ( "Session Settle Time", $"{Session_Settle} ms  (all instruments share this rate)" );
          Gap ( );
        }
      }

      // ── Polling ───────────────────────────────────────────────────
      Gap ( );
      Header ( "Polling" );
      Separator ( );
      Row ( "Poll Delay", $"{Settings.Default_Poll_Delay_Ms} ms" );
      Row ( "Continuous Mode", Settings.Default_Continuous_Poll ? "Yes" : "No" );
      Row ( "Default Measurement", Settings.Default_Measurement_Type );
      Row ( "Max Display Points", $"{Settings.Max_Display_Points:N0}" );
      Row ( "Stop At Max Points", Settings.Stop_Polling_At_Max_Display_Points ? "Yes" : "No" );
      Row ( "GPIB Timeout", $"{Settings.Default_GPIB_Timeout_Ms} ms" );
      Row ( "Prologix Read Timeout", $"{Settings.Prologix_Read_Tmo_Ms} ms" );
      Row ( "Max Retry Attempts", $"{Settings.Max_Retry_Attempts}" );
      Row ( "Instrument Settle", $"{Settings.Instrument_Settle_Ms} ms" );
      Gap ( );

      // ── Display ───────────────────────────────────────────────────
      Gap ( );
      Header ( "Display" );
      Separator ( );
      Row ( "Chart Refresh Rate", $"{Settings.Chart_Refresh_Rate_Ms} ms" );
      Row ( "Default View", Settings.Default_To_Combined_View ? "Combined" : "Split" );
      Row ( "Default Normalized", Settings.Default_To_Normalized_View ? "Yes" : "No" );
      Row ( "Show Legend On Startup", Settings.Show_Legend_On_Startup ? "Yes" : "No" );
      Row ( "Tooltips On Hover", Settings.Show_Tooltips_On_Hover ? "Yes" : "No" );
      Row ( "Tooltip Duration", $"{Settings.Tooltip_Display_Duration_Ms} ms" );
      Row ( "Tooltip Threshold", $"{Settings.Tooltip_Distance_Threshold} px" );
      Row ( "Default Zoom Level", $"{Settings.Default_Zoom_Level}" );
      Row ( "Throttle When Large", Settings.Throttle_When_Many_Points ? "Yes" : "No" );
      Row ( "Throttle Threshold", $"{Settings.Throttle_Point_Threshold:N0} points" );
      Gap ( );

      // ── Recording ─────────────────────────────────────────────────
      Gap ( );
      Header ( "Recording & Files" );
      Separator ( );
      Row ( "Save Folder", Settings.Default_Save_Folder );
      Row ( "Filename Pattern", Settings.Filename_Pattern );
      Row ( "Auto Save", Settings.Enable_Auto_Save ? $"Every {Settings.Auto_Save_Interval_Minutes} min" : "Disabled" );
      Row ( "Auto Save On Stop", Settings.Auto_Save_On_Stop ? "Yes" : "No" );
      Row ( "Prompt Before Clear", Settings.Prompt_Before_Clear ? "Yes" : "No" );
      Row ( "Export Format", Settings.Export_Format );
      Gap ( );

      // ── Memory ────────────────────────────────────────────────────
      Gap ( );
      Header ( "Memory" );
      Separator ( );
      Row ( "Max Points In Memory", $"{Settings.Max_Data_Points_In_Memory:N0}" );
      Row ( "Warning Threshold", $"{Settings.Warning_Threshold_Percent}%" );
      Row ( "Auto Trim Old Data", Settings.Auto_Trim_Old_Data ? $"Keep last {Settings.Keep_Last_N_Points:N0}" : "Disabled" );
      Gap ( );

      // ── Analysis ─────────────────────────────────────────────────

      Gap ( );
      Header ( "Analysis" );
      Separator ( );
      Row ( "Auto Analyze After Rec", Settings.Auto_Analyze_After_Recording ? "Yes" : "No" );
      Row ( "Show Mean", Settings.Analysis_Show_Mean ? "Yes" : "No" );
      Row ( "Show Std Dev", Settings.Analysis_Show_Std_Dev ? "Yes" : "No" );
      Row ( "Show Min/Max", Settings.Analysis_Show_Min_Max ? "Yes" : "No" );
      Row ( "Show RMS", Settings.Analysis_Show_RMS ? "Yes" : "No" );
      Row ( "Show Trend", Settings.Analysis_Show_Trend ? "Yes" : "No" );
      Row ( "Show Sample Rate", Settings.Analysis_Show_Sample_Rate ? "Yes" : "No" );
      Gap ( );

      // ── Connection ────────────────────────────────────────────────
      Gap ( );
      Header ( "Connection" );
      Separator ( );
      Row ( "Default GPIB Address", $"{Settings.Default_GPIB_Instrument_Address}" );
      Row ( "Prologix MAC", Settings.Prologic_MAC_Address );
      Row ( "Default IP", string.IsNullOrEmpty ( Settings.Default_IP_Address ) ? "Auto-detect" : Settings.Default_IP_Address );
      Row ( "Scan Timeout", $"{Settings.Prologic_Scan_Timeout_MS} ms" );
      Row ( "Send Reset On Connect", Settings.Send_Reset_On_Connect_3458 ? "Yes" : "No" );
      Row ( "Reset Settle Delay", $"{Settings.Reset_Settle_Delay_Ms} ms" );
      Row ( "Skew Warning Threshold", $"{Settings.Skew_Warning_Threshold_Seconds:F1} s" );
      Row ( "Stale Data Threshold", $"{Settings.Stale_Data_Threshold_Seconds:F1} s" );
      Gap ( );

      // ── Theme ─────────────────────────────────────────────────────
      Gap ( );
      Header ( "Theme" );
      Separator ( );
      Color_Row ( "Background", Theme.Background );
      Color_Row ( "Foreground", Theme.Foreground );
      Color_Row ( "Grid", Theme.Grid );
      Color_Row ( "Labels", Theme.Labels );

      for ( int I = 0; I < Theme.Line_Colors.Length; I++ )
        Color_Row ( $"Series {I + 1} Color", Theme.Line_Colors [ I ] );

      Gap ( );
    }
  }
}
