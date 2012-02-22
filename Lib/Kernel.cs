﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using U2Pa.Lib.Eproms;

namespace U2Pa.Lib
{
  public class Kernel
  {
    private const string mainHelp =
  @"USAGE: u2pa <category> <command> [arguments] [options...]

Categories are:
  rom          Commands related to roms (that is ROM/PROM/EPROM/EEPROM...);
               including reading and writing roms
  ram          Commands related to SRAM ICs
  prog         Commands related to the Top Programmer device;
               ie. reading the id string, uploading a bit stream ect
  help         Displays detailed help for a category

  dev          DON'T USE IT IF U HAVEN'T READ THE FRAKNING SRC, OR IT MIGHT TOAST YOUR IC AND/OR TOP!
               DON'T SAY I DIDN'T WARN U!!! };-P 

General options:
 -v   --verbosity   Verbosity; i must be in the range [0,..,5] default is 3; the higher i, the more crap on screen
";

    private const string romHelp =
      @"u2pa rom <command> [arguments] [options]

alias: [NONE] (yet? };-P)

all commands related to roms (that is ROM/PROM/EPROM/EEPROM...)

arguments:
  read
  write
  id

    Detailed description.

options:
";

    public static int Rom(PublicAddress pa, string[] args)
    {
      switch (args[1])
      {
        case "read":
          return RomRead(pa, args[2], args[3]);
          break;

        case "write":
          pa.ShoutLine(1, "rom write not yet implemented!");
          return 1;
          break;

        case "id":
          pa.ShoutLine(1, "rom id not yet implemented!");
          return 1;
          break;
          
        default:
          pa.ShoutLine(1, "Unknown rom command {0}", args[1]);
          return 1;
      }
    }

    public static int Ram(PublicAddress pa, string[] args)
    {
      pa.ShoutLine(1, "category ram not yet implemented!");
      return 1;
    }

    public static int Prog(PublicAddress pa, string[] args)
    {
      pa.ShoutLine(1, "category prog not yet implemented!");
      return 1;
    }

    public static int Help(PublicAddress pa, string[] args)
    {
      if(args.Length <= 1)
      {
        Console.Write(mainHelp);
        return 0;
      }

      switch (args[1])
      {
        case "rom":
          Console.Write(romHelp);
          break;

        default:
          Console.WriteLine("No detailed help for category {0} found, sorry!", args[1]);
          break;
      }

      return 0;
    }

    public static int Dev(PublicAddress pa, string[] args)
    {
      using (var topDevice = TopDevice.Create(pa))
      {
        for (byte vcc = 0x00; vcc < 0x100; vcc++)
        {
          topDevice.ApplyGnd(20);
          pa.ShoutLine(-1, "Vcc {0} centivolts", vcc);
          topDevice.SendRawPackage(-1, new byte[] { 0x0e, 0x13, vcc, 0x00 }, "dev");
          topDevice.SendRawPackage(-1, new byte[] { 0x0e, 0x15, 0x08, 0x00 }, "dev");
          pa.ShoutLine(-1, "Meassure from pin 20 (gnd) to pin 1");
          Console.ReadLine();
        }
      }
      return 0;
    }

    public static int RomRead(PublicAddress pa, string type, string fileName)
    {
      IList<byte> bytes = new List<byte>();
      var eprom = Eprom.Create(type);
      int totalNumberOfAdresses = 2.Pow(eprom.AddressPins.Length);
      int startAddress = 0;
      using (var progressBar = pa.GetProgressBar(totalNumberOfAdresses))
      {
        while (startAddress < totalNumberOfAdresses)
        {
          using (var topDevice = TopDevice.Create(pa))
          {
            startAddress = topDevice.ReadEprom(eprom, progressBar, bytes, startAddress);
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

    //public static void RomReadRecycle(PublicAddress pa, string type, string fileName)
    //{
    //  var eprom = Eprom.Create(type);
    //  var totalNumberOfAddresses = 2.Pow(eprom.AddressPins.Length);
    //  var addressStride = totalNumberOfAddresses/4;
    //  IList<byte[]> data = new List<byte[]>();
    //  using (var progressBar = pa.GetProgressBar(totalNumberOfAddresses))
    //  {
    //    var i = 0;
    //    do
    //    {
    //      using (var topDevice = TopDevice.Create(pa))
    //      {
    //        data.Add(topDevice.ReadEprom(eprom, i*addressStride, (i + 1)*addressStride, progressBar).ToArray());
    //        i++;
    //        progressBar.Shout(String.Format("Recycle({0}), Reconnect programmer!", i));
    //      }
    //      Console.ReadLine();
    //    } while (i*addressStride != totalNumberOfAddresses);
    //  }
    //  using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
    //  {
    //    using (var bw = new BinaryWriter(fs))
    //    {
    //      bw.Write(data.SelectMany(x => x).ToArray());
    //      bw.Flush();
    //    }
    //  }
    //  pa.ShoutLine(2, "EPROM{0} data written to file {1}", type, fileName);
    //}

    public static void RomWrite(PublicAddress pa, string type, string fileName, params string[] vppLevel)
    {
    }
  }
}
