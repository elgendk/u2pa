namespace U2Pa
{
  internal static class U2PaMain
  {
    public static void Main(string[] args)
    {
      try
      {
        Core.ShoutLine(1, "*************** U2Pa (C) Elgen 2012 ****************");
        Core.ShoutLine(1, "* Alternative software for the Top2005+ Programmer *");
        Core.ShoutLine(1, "****************************************************");
        Core.ShoutLine(1, "Verbosity level: {0}", Core.VerbosityLevel);

        Core.Init();

        Core.Read2716();
      }
      catch (U2PaException e)
      {
        Core.ShoutLine(0, "Fatal error: {0}", e.Message);
        Core.ShoutLine(5, "Exception:\n{0}", e);
      }
      finally
      {
        Core.Close();
      }
    }
  }
}