
// ════════════════════════════════════════════════════════════════════════════════
// FILE:    App_Help.cs
// PROJECT: Multimeter_Controller
// ════════════════════════════════════════════════════════════════════════════════
//
// PURPOSE
//   Centralised help and about display for all forms in the application.
//   Each method loads a plain-text resource embedded in the assembly and
//   displays it in a modal Rich_Text_Popup dialog.  Adding help for a new
//   form requires only a new embedded .txt resource and a corresponding
//   one-method entry here.
//
// DESIGN
//   Both classes are static — nothing is ever instantiated.  All popup
//   instances are created inside using blocks so they are disposed
//   immediately after the user closes them; no popup state persists between
//   invocations.
//
// ── App_Help ──────────────────────────────────────────────────────────────────
//
//   Show_About( Form Owner )
//     Displays the application About box loaded from:
//       Multimeter_Controller.Main_Form_About.txt
//     Fixed size 520 × 400, not resizable.
//     Wired from every form's Help → About menu item.
//
//   Show_Launcher_Help( Form Owner )
//     Help content for the main launcher / instrument-list form, loaded from:
//       Multimeter_Controller.Main_Form_Help.txt
//     Size 560 × 700, resizable.
//
//   Show_Recording_Playback_Form_Help( Form Owner )
//     Help content for the recorded-data viewer (Recording_Playback_Form),
//     loaded from:
//       Multimeter_Controller.Recording_Playback_Form_Help.txt
//     Size 600 × 700, resizable.
//     Wired from helpToolStripMenuItem.Click in Recording_Playback_Form.
//
//   Show_Multi_Poll_Form_Help( Form Owner )
//     Help content for the multi-instrument live polling form, loaded from:
//       Multimeter_Controller.Multi_Poll_Form_Help.txt
//     Size 600 × 700, resizable.
//
//   Show_Settings_Form_Help( Form Owner )
//     Help content for the Settings dialog (Settings_Form), loaded from:
//       Multimeter_Controller.Settings_Form_Help.txt
//     Size 600 × 700, resizable.
//     Wired from the Help menu item added by Settings_Form.Initialize_Menu_Strip().
//
// ── Resource_Loader ───────────────────────────────────────────────────────────
//
//   Load( string Resource_Name ) → string
//     Retrieves an embedded resource by its fully-qualified manifest name
//     (e.g. "Multimeter_Controller.Main_Form_Help.txt") from the executing
//     assembly via GetManifestResourceStream().
//     Returns the full text content on success.
//     Returns "[Resource not found: {Resource_Name}]" if the stream is null,
//     which happens when:
//       • The resource name is misspelled or the wrong case.
//       • The .txt file's Build Action in the project is not set to
//         "Embedded Resource".
//       • The file was added to the wrong project.
//     The StreamReader uses the stream's default encoding (UTF-8 for text
//     files saved without a BOM, which is the standard for .txt resources).
//
// ── ADDING HELP FOR A NEW FORM ────────────────────────────────────────────────
//
//   1. Add a plain-text file to the project, e.g. My_Form_Help.txt.
//   2. In the file's Properties, set Build Action = Embedded Resource.
//   3. Note the manifest name: default namespace + "." + filename, e.g.
//      "Multimeter_Controller.My_Form_Help.txt".
//   4. Add a method here:
//
//        public static void Show_My_Form_Help( Form Owner )
//        {
//          using var Popup = new Rich_Text_Popup( "My Form - Help", 600, 700,
//                                                 Resizable: true );
//          Popup.Add_Raw_Text( Resource_Loader.Load(
//            "Multimeter_Controller.My_Form_Help.txt" ) );
//          Popup.Show_Popup( Owner );
//        }
//
//   5. Wire it from the form's help menu item:
//
//        Help_Item.Click += ( s, e ) => App_Help.Show_My_Form_Help( this );
//
// ── RESOURCE NAMING NOTES ─────────────────────────────────────────────────────
//   • The manifest resource name is case-sensitive on all platforms.
//   • Folder separators in the project become dots in the manifest name,
//     e.g. a file at Resources\My_Form_Help.txt becomes
//     "Multimeter_Controller.Resources.My_Form_Help.txt".
//   • All current help resources sit directly in the project root (no
//     subfolder), so the name is simply default_namespace + "." + filename.
//   • To enumerate all embedded resource names for debugging:
//       System.Reflection.Assembly.GetExecutingAssembly()
//             .GetManifestResourceNames()
//
// AUTHOR:  Mike Wheeler
// CREATED: 04/04/2026
// ════════════════════════════════════════════════════════════════════════════════

using Rich_Text_Popup_Namespace;

namespace Multimeter_Controller
{
  public static class App_Help
  {
    // -------------------------------------------------------------------------
    // Show_About  -  same across all forms
    // -------------------------------------------------------------------------
    public static void Show_About( Form Owner )
    {
      using ( var Popup = new Rich_Text_Popup( "About", 520, 400 ) )
      {
        string Content = Resource_Loader.Load( "Multimeter_Controller.Main_Form_About.txt" );
        Popup.Add_Raw_Text( Content );
        Popup.Show_Popup( Owner );
      }
    }

    // -------------------------------------------------------------------------
    // Show_Launcher_Help
    // -------------------------------------------------------------------------
    public static void Show_Launcher_Help( Form Owner )
    {
      using ( var Popup = new Rich_Text_Popup( "Launcher - Help", 560, 700, Resizable: true ) )
      {
        string Content = Resource_Loader.Load( "Multimeter_Controller.Main_Form_Help.txt" );
        Popup.Add_Raw_Text( Content );
        Popup.Show_Popup( Owner );
      }
    }

    public static void Show_Recording_Playback_Form_Help( Form Owner )
    {
      using ( var Popup = new Rich_Text_Popup( "Recorded Data Viewer - Help", 600, 700, Resizable: true ) )
      {
        string Content = Resource_Loader.Load( "Multimeter_Controller.Recording_Playback_Form_Help.txt" );
        Popup.Add_Raw_Text( Content );
        Popup.Show_Popup( Owner );
      }
    }

    public static void Show_Multi_Poll_Form_Help( Form Owner )
    {
      using ( var Popup = new Rich_Text_Popup( "Multi-Instrument Poller - Help", 600, 700, Resizable: true ) )
      {
        string Content = Resource_Loader.Load( "Multimeter_Controller.Multi_Poll_Form_Help.txt" );
        Popup.Add_Raw_Text( Content );
        Popup.Show_Popup( Owner );
      }
    }
    public static void Show_Settings_Form_Help( Form Owner )
    {
      using ( var Popup = new Rich_Text_Popup( "Settings - Help", 600, 700, Resizable: true ) )
      {
        string Content = Resource_Loader.Load( "Multimeter_Controller.Settings_Form_Help.txt" );
        Popup.Add_Raw_Text( Content );
        Popup.Show_Popup( Owner );
      }
    }
  }

  public static class Resource_Loader
  {
    public static string Load( string Resource_Name )
    {
      var       Assembly = System.Reflection.Assembly.GetExecutingAssembly();
      using var Stream   = Assembly.GetManifestResourceStream( Resource_Name );
      if ( Stream == null )
        return $"[Resource not found: {Resource_Name}]";
      using var Reader = new System.IO.StreamReader( Stream );
      return Reader.ReadToEnd();
    }
  }

}
