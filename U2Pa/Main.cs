using System.IO;
using System.Linq;

namespace U2Pa
{
  internal static class U2PaCmd
  {
    public static void Main(string[] args)
    {
      PublicAddress pa = new PublicAddress(4);
      try
      {
        pa.ShoutLine(1, "*************** U2Pa (C) Elgen 2012 ****************");
        pa.ShoutLine(1, "* Alternative software for the Top2005+ Programmer *");
        pa.ShoutLine(1, "****************************************************");
        pa.ShoutLine(1, "Verbosity level: {0}", pa.VerbosityLevel);

        using (TopDevice topDevice = TopDevice.Create(pa))
        {
          var eData = topDevice.ReadEprom("2716").ToArray();

          const string eFileName = @"C:\Users\Elgen\Arcade\MoonCrestaBootleg\Test4A.bin";
          using (var fs = new FileStream(eFileName, FileMode.Create, FileAccess.Write))
          {
            using (var bw = new BinaryWriter(fs))
            {
              bw.Write(eData);
              bw.Flush();
            }
          }
          pa.ShoutLine(4, "Read 272048 data written to file {0}", eFileName);
        }
      }
      catch (U2PaException e)
      {
        pa.ShoutLine(0, "Fatal error: {0}", e.Message);
        pa.ShoutLine(5, "Exception:\n{0}", e);
      }
    }
  }
}