// =============================================================================
// FILE:        App_Help.cs
// DESCRIPTION: Centralized help and about popups for all forms.
// =============================================================================

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
      using (var Popup = new Rich_Text_Popup( "About", 520, 400 ))
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
      using (var Popup = new Rich_Text_Popup( "Launcher - Help", 560, 700, Resizable: true ))
      {
        string Content = Resource_Loader.Load( "Multimeter_Controller.Main_Form_Help.txt" );
        Popup.Add_Raw_Text( Content );
        Popup.Show_Popup( Owner );
      }
    }

 


    public static void Show_Recording_Playback_Form_Help( Form Owner )
    {
      using (var Popup = new Rich_Text_Popup( "Recorded Data Viewer - Help", 600, 700, Resizable: true ))
      {
        string Content = Resource_Loader.Load( "Multimeter_Controller.Recording_Playback_Form_Help.txt" );
        Popup.Add_Raw_Text( Content );
        Popup.Show_Popup( Owner );
      }
    }

    public static void Show_Multi_Poll_Form_Help( Form Owner )
    {
      using (var Popup = new Rich_Text_Popup( "Multi-Instrument Poller - Help", 600, 700, Resizable: true ))
      {
        string Content = Resource_Loader.Load( "Multimeter_Controller.Multi_Poll_Form_Help.txt" );
        Popup.Add_Raw_Text( Content );
        Popup.Show_Popup( Owner );
      }
    }
    public static void Show_Settings_Form_Help( Form Owner )
    {
      using (var Popup = new Rich_Text_Popup( "Settings - Help", 600, 700, Resizable: true ))
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
      var Assembly = System.Reflection.Assembly.GetExecutingAssembly();
      using var Stream = Assembly.GetManifestResourceStream( Resource_Name );
      if (Stream == null)
        return $"[Resource not found: {Resource_Name}]";
      using var Reader = new System.IO.StreamReader( Stream );
      return Reader.ReadToEnd();
    }
  }

}
