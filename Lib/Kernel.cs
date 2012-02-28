using System;
using System.Collections.Generic;
using System.Linq;
using U2Pa.Lib.Eproms;

namespace U2Pa.Lib
{
  public class Kernel
  {
    public static int RomInfo(PublicAddress pa, string type)
    {
      var eprom = EpromXml.Specified[type];
      Console.WriteLine(eprom);
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
    /// <returns>Exit code.</returns>
    public static IList<byte> RomRead(PublicAddress pa, string type)
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
      return bytes;
    }

    public static void RomWrite(PublicAddress pa, string type, IList<byte> fileData, params string[] vppLevel)
    {
      if (type == "271024" || type == "272048")
      {
        Console.WriteLine("Writing EPROMS of type {0} is not yet supported, sorry }};-(", type);
        return;
      }
      using (var topDevice = TopDevice.Create(pa))
      {
        var eprom = EpromXml.Specified[type];
        topDevice.WriteEpromClassic(eprom, 25, fileData);
      }
    }

    public static List<Tuple<int, byte, byte>> RomVerify(PublicAddress pa, string type, IList<byte> fileData)
    {
      var didntVerify = new List<Tuple<int, byte, byte>>();
      var epromData = RomRead(pa, type);

      if(epromData.Count != fileData.Count)
        throw  new U2PaException("Filedata doesn't have the same length as EPROM data.");

      for(var address = 0; address < fileData.Count; address++)
      {
        if (epromData[address] != fileData[address])
          didntVerify.Add(Tuple.Create(address, epromData[address], fileData[address]));
      }
      return didntVerify;
    }

    public static int Dev(PublicAddress pa, string[] args)
    {
      Console.WriteLine("Testing {0} for Erasure }};-P", args[1]);
      var fileData = Tools.ReadBinaryFile(args[1]).ToArray();
      
      if(fileData.Any(b => b != 0xff))
        throw new U2PaException("No good }};-(");
      else Console.WriteLine("File {0} filled with all nice little 0xFF's }};-P", args[1]);

      return 0;
    }
  }
}
