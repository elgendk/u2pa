//                             u2pa
//
//    A command line interface for Top Universal Programmers
//
//    Copyright (C) Elgen };-) aka Morten Overgaard 2012
//
//    This file is part of u2pa.
//
//    u2pa is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    u2pa is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with u2pa. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using U2Pa.Lib.IC;

namespace U2Pa.Lib
{
  public class Kernel
  {
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

    #region Rom
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pa"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static int RomInfo(PublicAddress pa, string type)
    {
      var eprom = EpromXml.Specified[type];
      Console.WriteLine(eprom);
      return 0;
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
      //if (type == "271024" || type == "272048")
      //{
      //  Console.WriteLine("Writing EPROMS of type {0} is not yet supported, sorry }};-(", type);
      //  return;
      //}
      using (var topDevice = TopDevice.Create(pa))
      {
        var eprom = EpromXml.Specified[type];
        topDevice.WriteEpromClassic(eprom, 10, fileData);
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
    #endregion Rom

    public static int Dev(PublicAddress pa)
    {
      var tr = new PinTranslator(40, 40, 0, false);
      var zif = new ZIFSocket(40);
      zif.SetAll(true);
      var v = 0;
      while (true)
      {
        using (var td = TopDevice.Create(pa))
        {
          td.SetVccLevel((VccLevel)v);
          td.SetVppLevel((VppLevel)v);
          td.ApplyGnd(tr.ToZIF, new Pin { Number = 20 });
          td.ApplyVcc(tr.ToZIF, new Pin { Number = 8 });
          td.ApplyVpp(tr.ToZIF, new Pin { Number = 1 });
          td.WriteZIF(zif, String.Format("v = {0}", v));
          Console.WriteLine("Vpp pin 1 at level {0}, Vcc pin 8 at level {0}, Gnd pin 20. Press Enter to advance, 'q' to quit.", v);
          var input = Console.ReadLine();
          if (input.Contains("q"))
            break;
        }
        v += 10;
        if (v >= 256) v = 0;
      }
      //Console.WriteLine("Testing {0} for Erasure }};-P", args[1]);
      //var fileData = Tools.ReadBinaryFile(args[1]).ToArray();
      
      //if(fileData.Any(b => b != 0xff))
      //  throw new U2PaException("No good }};-(");
      //else Console.WriteLine("File {0} filled with all nice little 0xFF's }};-P", args[1]);

      return 0;
    }

    public static int SRamTest(PublicAddress pa, string type)
    {
      var sram = SRamXml.Specified[type];
      var totalNumberOfAdresses = sram.AddressPins.Length == 0 ? 0 : 1 << sram.AddressPins.Length;
      List<Tuple<int, string, string>> firstPass, secondPass;
      using (var progressBar = pa.GetProgressBar(totalNumberOfAdresses * 4))
      {
        using (var topDevice = TopDevice.Create(pa))
        {
          pa.ShoutLine(1, "Testing SRAM{0}", type);
          progressBar.Init();
          firstPass = topDevice.SRamTest(pa, sram, progressBar, totalNumberOfAdresses, topDevice, false);
          secondPass = topDevice.SRamTest(pa, sram, progressBar, totalNumberOfAdresses, topDevice, true);
        }
      }
      var returnValue = 0;
      foreach (var tuple in firstPass)
      {
        pa.ShoutLine(1, "Bad cell found in first pass. Address: {0} Expected {1} Read {2}",
          tuple.Item1.ToString("X4"), tuple.Item3, tuple.Item2);
        returnValue = 1;
      }
      foreach (var tuple in secondPass)
      {
        pa.ShoutLine(1, "Bad cell found in second pass. Address: {0} Expected {1} Read {2}",
          tuple.Item1.ToString("X4"), tuple.Item3, tuple.Item2);
        returnValue = 1;
      }

      if(returnValue == 0)
        pa.ShoutLine(1, "This piece of SRAM is just a'okay }};-P");

      return returnValue;
    }
  }
}
