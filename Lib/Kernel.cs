using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using U2Pa.Lib.Eproms;

namespace U2Pa.Lib
{
  public class Kernel
  {
    public static int RomInfo(PublicAddress pa, string type)
    {
      var eprom = EpromXml.Specified[type];
      Console.Write(eprom);
      
      if (!String.IsNullOrEmpty(eprom.Notes))
      {
        Console.Write(eprom.Notes);
        Console.WriteLine();
      }
      return 0;
    }

    public static int ProgId(PublicAddress pa)
    {
      var v = pa.VerbosityLevel;
      try
      {
        pa.VerbosityLevel = 0;
        using (var topDevice = TopDevice.Create(pa))
        {
          pa.ShoutLine(0, topDevice.ReadTopDeviceIdString());
        }
        return 0;
      }
      finally
      {
        pa.VerbosityLevel = v;
      }
    }

    /// <summary>
    /// The main method for reading a rom that is defined in the Eproms.xml file.
    /// </summary>
    /// <param name="pa">Public addresser.</param>
    /// <param name="type">The of the rom the be read.</param>
    /// <param name="fileName">Name of the file to save the read contents in.</param>
    /// <returns>Exit code.</returns>
    public static int RomRead(PublicAddress pa, string type, string fileName)
    {
      IList<byte> bytes = new List<byte>();
      var eprom = EpromXml.Specified[type];
      var totalNumberOfAdresses = eprom.AddressPins.Length == 0 ? 0 : 1 << eprom.AddressPins.Length;
      var startAddress = 0;
      using (var progressBar = pa.GetProgressBar(totalNumberOfAdresses))
      {
        while (startAddress < totalNumberOfAdresses)
        {
          using (var topDevice = TopDevice.Create(pa))
          {
            startAddress = topDevice.ReadEprom(eprom, progressBar, bytes, startAddress, totalNumberOfAdresses);
          }
          if(startAddress < totalNumberOfAdresses)
            progressBar.Shout("Disposing Top USB interface and inits a new");
        }
      }
      using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
      {
        using (var bw = new BinaryWriter(fs))
        {
          bw.Write(bytes.ToArray());
          bw.Flush();
        }
      }
      pa.ShoutLine(2, "EPROM{0} data written to file {1}", type, fileName);
      return 0;
    }

    public static int RomWrite(PublicAddress pa, string type, string fileName, params string[] vppLevel)
    {
      return 1;
    }

    public static int Dev(PublicAddress pa, string[] args)
    {
      using (var topDevice = TopDevice.Create(pa))
      {
        for (byte vcc = 0x00; vcc < 0x100; vcc++)
        {
          //topDevice.ApplyGnd(20);
          pa.ShoutLine(-1, "Vcc {0} centivolts", vcc);
          topDevice.SendRawPackage(-1, new byte[] {0x0e, 0x13, vcc, 0x00}, "dev");
          topDevice.SendRawPackage(-1, new byte[] {0x0e, 0x15, 0x08, 0x00}, "dev");
          pa.ShoutLine(-1, "Meassure from pin 20 (gnd) to pin 1");
          Console.ReadLine();
        }
      }
      return 0;
    }
  }
}
