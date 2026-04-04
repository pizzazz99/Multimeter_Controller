namespace Multimeter_Controller
{
  internal class Buffered_Panel : Panel
  {
    public Buffered_Panel()
    {
      DoubleBuffered = true;
      ResizeRedraw = true;
    }
  }
}
