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
    /// <param name="shouter">The public address instance.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int ProgId(IShouter shouter)
    {
      var v = shouter.VerbosityLevel;
      try
      {
        shouter.VerbosityLevel = 0;
        shouter.ShoutLine(0, "Connected Top Programmer has id: {0}", TopDevice.ReadTopDeviceIdString(shouter));
        return 0;
      }
      finally
      {
        shouter.VerbosityLevel = v;
      }
    }
    
    /// <summary>
    /// Calculates and writes some statistics about the connected Top Programmer
    /// and the UBS connection.
    /// </summary>
    /// <param name="shouter">The public address instance.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int ProgStat(IShouter shouter)
    {
      using (var td = TopDevice.Create(shouter))
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
        shouter.ShoutLine(0, "Average WriteZif = {0}ms (1000 performed)", (double)sw.ElapsedMilliseconds / 1000);
        
        sw.Reset();
        
        sw.Start();
        for(var i = 0; i < 1000; i++)
          td.ReadZIF("");
        sw.Stop();
        shouter.ShoutLine(0, "Average ReadZif  = {0}ms (1000 performed)", (double)sw.ElapsedMilliseconds / 1000);
      }
      return 0;
    }
    #endregion Prog

    #region Rom
    /// <summary>
    /// Displays a list of all supported ROMs.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="option">If option "type" is passed, only the type is displayed.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int RomAll(IShouter shouter, string option)
    {
      var onlyDisplayTypeName = option == "type";
      shouter.ShoutLine(1, "Supported ROMs:");
      shouter.ShoutLine(1, "===============");
      foreach (var pair in EpromXml.Specified.OrderBy(pair => pair.Key))
      {
        if (onlyDisplayTypeName)
          shouter.ShoutLine(1, pair.Key);
        else
        {
          var rom = pair.Value;
          shouter.ShoutLine(1, "Type:");
          shouter.ShoutLine(1, "  {0}", rom.Type);
          if (!String.IsNullOrWhiteSpace(rom.Description))
          {
            shouter.ShoutLine(1, "Description:");
            shouter.ShoutLine(1, rom.Description);
          }
          if (!String.IsNullOrWhiteSpace(rom.Notes))
          {
            shouter.ShoutLine(1, "Notes:");
            shouter.ShoutLine(1, rom.Notes);
          }
          shouter.ShoutLine(1, "-----------");
        }
      }
      return 0;
    }

    /// <summary>
    /// Displays the EPROM inserted into the Top-programmer.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="type"></param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int RomInfo(IShouter shouter, string type)
    {
      var eprom = EpromXml.Specified[type];
      Console.WriteLine(eprom);
      return 0;
    }

    /// <summary>
    /// The main method for reading a rom that is defined in the Eproms.xml file.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="type">The type of the rom the be read.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static IList<byte> RomRead(IShouter shouter, string type)
    {
      IList<byte> bytes = new List<byte>();
      var eprom = EpromXml.Specified[type];
      var totalNumberOfAdresses = eprom.AddressPins.Length == 0 ? 0 : 1 << eprom.AddressPins.Length;
      var startAddress = 0;
      using (var progressBar = new ProgressBar(shouter))
      {
        while (startAddress < totalNumberOfAdresses)
        {
          using (var topDevice = TopDevice.Create(shouter))
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
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="type">The type of the rom the be programmed.</param>
    /// <param name="fileData">The data to be written.</param>
    /// <param name="vppLevel">The Vpp-level; if not present, the one from the EPROM definition is used.
    /// <remarks>Not yet used!</remarks>
    /// </param>
    public static void RomWrite(IShouter shouter, string type, IList<byte> fileData, params string[] vppLevel)
    {
      var eprom = EpromXml.Specified[type];
      var totalNumberOfAdresses = eprom.AddressPins.Length == 0 ? 0 : 1 << eprom.AddressPins.Length;
      using (var progressBar = new ProgressBar(shouter))
      {
        using (var topDevice = TopDevice.Create(shouter))
        {
          topDevice.WriteEpromClassic(eprom, progressBar, fileData);
        }
      }
    }

    /// <summary>
    /// Verifies an EPROM against a list of bytes.
    /// </summary>
    /// <remarks>
    /// At present only works for 8-bit roms!
    /// </remarks>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="type">The type of the rom the be verified.</param>
    /// <param name="fileData">The list of data to verify against.</param>
    /// <returns>
    /// A list of tuples representing bytes that didn't verify.
    /// The tupeles has the form (address, actual_byte_on_EPROM, expected_byte).
    /// </returns>
    public static List<Tuple<int, byte, byte>> RomVerify(IShouter shouter, string type, IList<byte> fileData)
    {
      var didntVerify = new List<Tuple<int, byte, byte>>();
      var epromData = RomRead(shouter, type);

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
    public static int DevVppLevels(Shouter shouter)
    {
      var onScreen = "n/f = increase Vpp by 1/10; p/b = decrease Vpp by 1/10; q = quit!";
      byte b = 0x00;
      var quit = false;
      while(!quit)
      {
        var tr = new PinTranslator(40, 40, 0);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(shouter))
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

    public static int DevVppPins(Shouter shouter)
    {
      var onScreen = "n/f = increase pinnumber by 1/10; p/b = decrease pinnumber by 1/10; q = quit!";
      byte p = 20;
      var quit = false;
      while (!quit)
      {
        var tr = new PinTranslator(40, 40, 0);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(shouter))
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
 
    public static int DevVccPins(Shouter shouter)
    {
      var onScreen = "n/f = increase pinnumber by 1/10; p/b = decrease pinnumber by 1/10; q = quit!";
      byte p = 20;
      var quit = false;
      while (!quit)
      {
        var tr = new PinTranslator(40, 40, 0);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(shouter))
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
    
    public static int DevVccLevels(Shouter shouter)
    {
      var onScreen = "n/f = increase Vcc by 1/10; p/b = decrease Vcc by 1/10; q = quit!";
      byte b = 0x00;
      var quit = false;
      while (!quit)
      {
        var tr = new PinTranslator(40, 40, 0);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(shouter))
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

    public static int DevGndPins(Shouter shouter)
    {
      var onScreen = "n/f = increase pinnumber by 1/10; p/b = decrease pinnumber by 1/10; q = quit!";
      byte p = 0;
      var quit = false;
      while (!quit)
      {
        var tr = new PinTranslator(40, 40, 0);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(shouter))
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
    /// <param name="shouter">The shouter instance.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int Dev(IShouter shouter)
    {
      //var fpgaFile = new FPGAProgram(@"C:\Top\Topwin6\Blib2\ictest.bit");
      //Console.WriteLine(fpgaFile);
      {
        var tr = new PinTranslator(40, 40, 0);
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        using (var td = TopDevice.Create(shouter))
        {
          td.SetVccLevel(5.0);
          td.SetVppLevel(13.0);
          td.ApplyGnd(tr.ToZIF, new Pin { Number = 10 });
          td.ApplyVcc(tr.ToZIF, new Pin { Number = 40 });
          td.PullUpsEnable(true);
          td.WriteZIF(zif, "");
          Console.ReadLine();
        }
      }

      {
        var tr = new PinTranslator(40, 40, 0);
        var zif = new ZIFSocket(40);
        zif.SetAll(false);
        using (var td = TopDevice.Create(shouter))
        {
          td.SetVccLevel(5.0);
          td.SetVppLevel(21.0);
          td.ApplyGnd(tr.ToZIF, new Pin { Number = 10 });
          td.ApplyVcc(tr.ToZIF, new Pin { Number = 40 });
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
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="type">The type of SRAM.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int SRamTest(IShouter shouter, string type)
    {
      var sram = SRamXml.Specified[type];
      var totalNumberOfAdresses = sram.AddressPins.Length == 0 ? 0 : 1 << sram.AddressPins.Length;
      List<Tuple<int, string, string>> firstRandomPass, secondRandomPass, firstSimplePass, secondSimplePass;
      using (var progressBar = new ProgressBar(shouter))
      {
        using (var topDevice = TopDevice.Create(shouter))
        {
          shouter.ShoutLine(1, "Testing SRAM{0}", type);
          progressBar.Init(totalNumberOfAdresses * 8);
          firstRandomPass = topDevice.SRamTestPass(shouter, sram, progressBar, "First random test", totalNumberOfAdresses, new RandomDataGenerator(sram.DataPins.Length, totalNumberOfAdresses));
          secondRandomPass = topDevice.SRamTestPass(shouter, sram, progressBar, "Second random test", totalNumberOfAdresses, new RandomDataGenerator(sram.DataPins.Length, totalNumberOfAdresses));
          firstSimplePass = topDevice.SRamTestPass(shouter, sram, progressBar, "First simple test (01..)", totalNumberOfAdresses, new SimpleDataGenerator(sram.DataPins.Length, false));
          secondSimplePass = topDevice.SRamTestPass(shouter, sram, progressBar, "Second simple test (10..)", totalNumberOfAdresses, new SimpleDataGenerator(sram.DataPins.Length, true));
        }
      }
      var returnValue = 0;
      var printingVerbosity = 1;
      returnValue = PrintTestPassResult(shouter, "first random", firstRandomPass, ref printingVerbosity);
      returnValue = PrintTestPassResult(shouter, "second random", secondRandomPass, ref printingVerbosity);
      returnValue = PrintTestPassResult(shouter, "first simple", firstSimplePass, ref printingVerbosity);
      returnValue = PrintTestPassResult(shouter, "second simple", secondSimplePass, ref printingVerbosity);
      if(returnValue == 0)
        shouter.ShoutLine(1, "This piece of SRAM is just a'okay }};-P");

      return returnValue;
    }

    private static int PrintTestPassResult(IShouter shouter, string text, List<Tuple<int, string, string>> pass, ref int printingVerbosity)
    {
      int returnValue = 0;
      var oldPrintingVerbosity = printingVerbosity;
      foreach (var tuple in pass)
      {
        shouter.ShoutLine(printingVerbosity, "Bad cell in {0} pass. Address: {1} Expected {2} Read {3}",
          text, tuple.Item1.ToString("X4"), tuple.Item3, tuple.Item2);
        printingVerbosity = 5;
        returnValue = 1;
        if (oldPrintingVerbosity <= 5)
          break;
      }
      return returnValue;
    }

    /// <summary>
    /// Displays the SRAM inserted into the Top-programmer.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="type">The type of SRAM.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int SRamInfo(IShouter shouter, string type)
    {
      var sram = SRamXml.Specified[type];
      Console.WriteLine(sram);
      return 0;
    }

    /// <summary>
    /// Displays a list of all supported SRAMs.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="option">If option "type" is passed, only the type is displayed.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int SRamAll(IShouter shouter, string option)
    {
      var onlyDisplayTypeName = option == "type";
      shouter.ShoutLine(1, "Supported SRAMs:");
      shouter.ShoutLine(1, "================");
      foreach (var pair in SRamXml.Specified.OrderBy(pair => pair.Key))
      {
        if (onlyDisplayTypeName)
          shouter.ShoutLine(1, pair.Key);
        else
        {
          var sram = pair.Value;
          shouter.ShoutLine(1, "Type:");
          shouter.ShoutLine(1, "  {0}", sram.Type);
          if (!String.IsNullOrWhiteSpace(sram.Description))
          {
            shouter.ShoutLine(1, "Description:");
            shouter.ShoutLine(1, sram.Description);
          }
          if (!String.IsNullOrWhiteSpace(sram.Notes))
          {
            shouter.ShoutLine(1, "Notes:");
            shouter.ShoutLine(1, sram.Notes);
          }
          shouter.ShoutLine(1, "-----------");
        }
      }
      return 0;
    }
    #endregion SRam

    #region BDump
    /// <summary>
    /// Processes a binary dump.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="format">Format.</param>
    /// <param name="numberOfOutputs">Number of input pins.</param>
    /// <param name="numberOfInputs">Number of output pins.</param>
    /// <param name="path">The file name.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int BDumpProcess(
      IShouter shouter, 
      string format, 
      int numberOfOutputs, 
      int numberOfInputs, 
      string path)
    {
      var processor = new BinaryDumpProcessor(path);

      string output;
      switch (format)
      {
        case "ceqn":
          output = processor.GenerateCUPLEquations(numberOfOutputs, numberOfInputs);
          break;

        case "ctt":
          output = processor.GenerateCUPLTruthTable(numberOfOutputs, numberOfInputs);
          break;

        case "tt":
          output = processor.GenerateHumanReadableTruthTable(numberOfOutputs, numberOfInputs);
          break;

        default:
          shouter.ShoutLine(1, "Unknown bdump format {0}", format);
          return 1;
      }
      Console.WriteLine();
      Console.WriteLine(output);
      return 0;
    }
    #endregion

    #region Vector
    /// <summary>
    /// A simple vector test.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="type">The type of test.</param>
    /// <returns>The results of the test.</returns>
    public static int VectorTest(IShouter shouter, string type)
    {
      IList<byte> bytes = new List<byte>();
      var vectorTest = VectorTestXml.Specified[type];
      List<VectorResult> results;
      using (var progressBar = new ProgressBar(shouter))
      {
        using (var topDevice = TopDevice.Create(shouter))
        {
          results = topDevice.VectorTest(vectorTest, progressBar);
        }
      }
      return 0;
    }
    #endregion Vector
  }
}
