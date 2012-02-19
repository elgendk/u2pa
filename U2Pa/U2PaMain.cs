using System;
using System.IO;
using System.Linq;

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

        var mb8516Data = Core.ReadMB8516().ToArray();
        var mb8516FileName = @"C:\Users\Elgen\Arcade\MoonCrestaBootleg\Test4A.bin";
        using (var fs = new FileStream(mb8516FileName, FileMode.Create, FileAccess.Write))
        {
          using (var bw = new BinaryWriter(fs))
          {
            bw.Write(mb8516Data);
            bw.Flush();
          }
        }
        Core.ShoutLine(4, "Read MB8516 data written to file {0}", mb8516FileName);
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