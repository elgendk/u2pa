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
using System.Diagnostics;
using System.Linq;
using U2Pa.Lib.IC;

namespace U2Pa.Lib
{
  public class Kernel
  {
    #region Prog
    /// <summary>
    /// Writes the id of the connected Top Programmer.
    /// </summary>
    /// <param name="pa">The public address instance.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int ProgId(PublicAddress pa)
    {
      var v = pa.VerbosityLevel;
      try
      {
        pa.VerbosityLevel = 0;
        pa.ShoutLine(0, "Connected Top Programmer has id: {0}", TopDevice.ReadTopDeviceIdString(pa));
        return 0;
      }
      finally
      {
        pa.VerbosityLevel = v;
      }
    }
    
    /// <summary>
    /// Calculates and writes some statistics about the connected Top Programmer
    /// and the UBS connection.
    /// </summary>
    /// <param name="pa">The public address instance.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int ProgStat(PublicAddress pa)
    {
      using (var td = TopDevice.Create(pa))
      {
        var writeZif = new ZIFSocket(40);
        writeZif.SetAll(false);
        td.WriteZIF(writeZif, "");
        td.WriteZIF(writeZif, "");
        Stopwatch sw = new Stopwatch();
        sw.Start();
        for(var i = 0; i < 1000; i++)
          td.WriteZIF(writeZif, "");
        sw.Stop();
        pa.ShoutLine(0, "Average WriteZif = {0}ms (1000 performed)", (double)sw.ElapsedMilliseconds / 1000);
        
        sw.Reset();
        
        sw.Start();
        for(var i = 0; i < 1000; i++)
          td.ReadZIF("");
        sw.Stop();
        pa.ShoutLine(0, "Average ReadZif  = {0}ms (1000 performed)", (double)sw.ElapsedMilliseconds / 1000);
      }
      return 0;
    }
    #endregion Prog

    #region Rom
    /// <summary>
    /// Displays the EPROM inserted into the Top-programmer.
    /// </summary>
    /// <param name="pa">The public address instance.</param>
    /// <param name="type"></param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int RomInfo(PublicAddress pa, string type)
    {
      var eprom = EpromXml.Specified[type];
      Console.WriteLine(eprom);
      return 0;
    }

    /// <summary>
    /// The main method for reading a rom that is defined in the Eproms.xml file.
    /// </summary>
    /// <param name="pa">The public address instance.</param>
    /// <param name="type">The type of the rom the be read.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
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

    /// <summary>
    /// Programs an EPROM.
    /// </summary>
    /// <param name="pa">The public address instance.</param>
    /// <param name="type">The type of the rom the be programmed.</param>
    /// <param name="fileData">The data to be written.</param>
    /// <param name="vppLevel">The Vpp-level; if not present, the one from the EPROM definition is used.
    /// <remarks>Not yet used!</remarks>
    /// </param>
    public static void RomWrite(PublicAddress pa, string type, IList<byte> fileData, params string[] vppLevel)
    {
      using (var topDevice = TopDevice.Create(pa))
      {
        var eprom = EpromXml.Specified[type];
        topDevice.WriteEpromClassic(eprom, fileData);
        //topDevice.WriteEpromFast(eprom, fileData);
      }
    }

    /// <summary>
    /// Verifies an EPROM against a list of bytes.
    /// </summary>
    /// <remarks>
    /// At present only works for 8-bit roms!
    /// </remarks>
    /// <param name="pa">The public address instance.</param>
    /// <param name="type">The type of the rom the be verified.</param>
    /// <param name="fileData">The list of data to verify against.</param>
    /// <returns>
    /// A list of tuples representing bytes that didn't verify.
    /// The tupeles has the form (address, actual_byte_on_EPROM, expected_byte).
    /// </returns>
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

    #region Dev
    public static int DevVppLevels(PublicAddress pa)
    {
      var onScreen = "n/f = increase Vpp by 1/10; p/b = decrease Vpp by 1/10; q = quit!";
      byte b = 0x00;
      var quit = false;
      while(!quit)
      {
        var tr = new PinTranslator(40, 40, 0, false);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(pa))
        {
          td.SetVppLevel(b);
          td.ApplyGnd(tr.ToZIF, new Pin { Number = 20 });
          td.ApplyVpp(tr.ToZIF, new Pin { Number = 1 });
          td.PullUpsEnable(true);
          td.WriteZIF(zif, "");
          Console.WriteLine(onScreen);
          switch (Console.ReadKey().Key)
          {
            case ConsoleKey.N:
              b++;
              break;
             
            case ConsoleKey.F:
              b += 10;
              break;

            case ConsoleKey.P:
              b--;
              break;

            case ConsoleKey.B:
              b -= 10;
              break;

            case ConsoleKey.Q:
              quit = true;
              break;

            default:
              Console.WriteLine(onScreen);
              break;
          }
        }
      }
      return 0;
    }

    public static int DevVppPins(PublicAddress pa)
    {
      var onScreen = "n/f = increase pinnumber by 1/10; p/b = decrease pinnumber by 1/10; q = quit!";
      byte p = 20;
      var quit = false;
      while (!quit)
      {
        var tr = new PinTranslator(40, 40, 0, false);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(pa))
        {
          td.SetVppLevel(0x7D);
          td.ApplyGnd(tr.ToZIF, new Pin { Number = 10 });
          td.ApplyVpp(tr.ToZIF, new Pin { Number = p });
          td.PullUpsEnable(true);
          td.WriteZIF(zif, "");
          Console.WriteLine(onScreen);
          switch (Console.ReadKey().Key)
          {
            case ConsoleKey.N:
              p++;
              break;

            case ConsoleKey.F:
              p += 10;
              break;

            case ConsoleKey.P:
              p--;
              break;

            case ConsoleKey.B:
              p -= 10;
              break;

            case ConsoleKey.Q:
              quit = true;
              break;

            default:
              Console.WriteLine(onScreen);
              break;
          }
        }
      }
      return 0;
    }
 
    public static int DevVccPins(PublicAddress pa)
    {
      var onScreen = "n/f = increase pinnumber by 1/10; p/b = decrease pinnumber by 1/10; q = quit!";
      byte p = 20;
      var quit = false;
      while (!quit)
      {
        var tr = new PinTranslator(40, 40, 0, false);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(pa))
        {
          td.SetVccLevel(0x2D);
          td.ApplyGnd(tr.ToZIF, new Pin { Number = 10 });
          td.ApplyVcc(tr.ToZIF, new Pin { Number = p });
          td.PullUpsEnable(true);
          td.WriteZIF(zif, "");
          Console.WriteLine(onScreen);
          switch (Console.ReadKey().Key)
          {
            case ConsoleKey.N:
              p++;
              break;

            case ConsoleKey.F:
              p += 10;
              break;

            case ConsoleKey.P:
              p--;
              break;

            case ConsoleKey.B:
              p -= 10;
              break;

            case ConsoleKey.Q:
              quit = true;
              break;

            default:
              Console.WriteLine(onScreen);
              break;
          }
        }
      }
      return 0;
    }
    
    public static int DevVccLevels(PublicAddress pa)
    {
      var onScreen = "n/f = increase Vcc by 1/10; p/b = decrease Vcc by 1/10; q = quit!";
      byte b = 0x00;
      var quit = false;
      while (!quit)
      {
        var tr = new PinTranslator(40, 40, 0, false);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(pa))
        {
          td.SetVccLevel(b);
          td.ApplyGnd(tr.ToZIF, new Pin { Number = 20 });
          td.ApplyVcc(tr.ToZIF, new Pin { Number = 8 });
          td.PullUpsEnable(true);
          td.WriteZIF(zif, "");
          Console.WriteLine(onScreen);
          switch (Console.ReadKey().Key)
          {
            case ConsoleKey.N:
              b++;
              break;

            case ConsoleKey.F:
              b += 10;
              break;

            case ConsoleKey.P:
              b--;
              break;

            case ConsoleKey.B:
              b -= 10;
              break;

            case ConsoleKey.Q:
              quit = true;
              break;

            default:
              Console.WriteLine(onScreen);
              break;
          }
        }
      }
      return 0;
    }

    public static int DevGndPins(PublicAddress pa)
    {
      var onScreen = "n/f = increase pinnumber by 1/10; p/b = decrease pinnumber by 1/10; q = quit!";
      byte p = 0;
      var quit = false;
      while (!quit)
      {
        var tr = new PinTranslator(40, 40, 0, false);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(pa))
        {
          td.SetVccLevel(0x2D);
          td.ApplyGnd(tr.ToZIF, new Pin { Number = 40 });
          td.ApplyVcc(tr.ToZIF, new Pin { Number = 20 });
          td.PullUpsEnable(true);
          td.WriteZIF(zif, "");
          Console.WriteLine(onScreen);
          switch (Console.ReadKey().Key)
          {
            case ConsoleKey.N:
              p++;
              break;

            case ConsoleKey.F:
              p += 10;
              break;

            case ConsoleKey.P:
              p--;
              break;

            case ConsoleKey.B:
              p -= 10;
              break;

            case ConsoleKey.Q:
              quit = true;
              break;

            default:
              Console.WriteLine(onScreen);
              break;
          }
        }
      }
      return 0;
    }

    /// <summary>
    /// For dev.
    /// </summary>
    /// <param name="pa">The public address instance.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int Dev(PublicAddress pa)
    {
      //var fpgaFile = new FPGAProgram(@"C:\Top\Topwin6\Blib2\ictest.bit");
      //Console.WriteLine(fpgaFile);
      {
        var tr = new PinTranslator(40, 40, 0, false);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(pa))
        {
          td.SetVccLevel(5.0);
          td.SetVppLevel(12.5);
          td.ApplyGnd(tr.ToZIF, new Pin { Number = 20 });
          td.ApplyVcc(tr.ToZIF, new Pin { Number = 40 });
          td.ApplyVpp(tr.ToZIF, new Pin { Number = 1 });
          td.PullUpsEnable(true);
          td.WriteZIF(zif, "");
          Console.ReadLine();
        }
      }

      //Console.WriteLine("Testing {0} for Erasure }};-P", args[1]);
      //var fileData = Tools.ReadBinaryFile(args[1]).ToArray();

      //if(fileData.Any(b => b != 0xff))
      //  throw new U2PaException("No good }};-(");
      //else Console.WriteLine("File {0} filled with all nice little 0xFF's }};-P", args[1]);

      return 0;
    }
    #endregion Dev

    #region SRam
    /// <summary>
    /// A simple SRAM-test.
    /// </summary>
    /// <param name="pa">The public address instance.</param>
    /// <param name="type">The type of SRAM.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
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
          firstPass = topDevice.SRamTest(pa, sram, progressBar, totalNumberOfAdresses, false);
          secondPass = topDevice.SRamTest(pa, sram, progressBar, totalNumberOfAdresses, true);
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

    /// <summary>
    /// Displays the SRAM inserted into the Top-programmer.
    /// </summary>
    /// <param name="pa">The public address instance.</param>
    /// <param name="type">The type of SRAM.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int SRamInfo(PublicAddress pa, string type)
    {
      var sram = SRamXml.Specified[type];
      Console.WriteLine(sram);
      return 0;
    }
    #endregion SRam
  }
}
