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
using System.Threading;
using U2Pa.Lib.IC;

namespace U2Pa.Lib
{
  /// <summary>
  /// An abstract representation of the connected Top Programmer. 
  /// </summary>
  public abstract class TopDevice : IDisposable
  {
    // TODO: Find out if these values are the same for all Top Programmers.
    protected const int VendorId = 0x2471;
    protected const int ProductId = 0x0853;
    protected const int Configuration = 1;
    protected const byte Interface = 0;
    protected const int ReadEndpointInt = 0x82;
    protected const int WriteEndpointInt = 0x02;
    
    public IUsbBulkDevice BulkDevice { get; private set; }

    /// <summary>
    /// The number of pins in the ZIF socket.
    /// </summary>
    public abstract int ZIFType { get; }

    /// <summary>
    /// The shouter instance.
    /// </summary>
    public IShouter Shouter { get; private set; }

    /// <summary>
    /// The list of pins in the ZIF socket that can be used as Vcc.
    /// </summary>
    public List<int> ValidVccPins;

    /// <summary>
    /// The list of pins in the ZIF socket that can be used as Vpp.
    /// </summary>
    public List<int> ValidVppPins;

    /// <summary>
    /// The list of pins in the ZIF socket that can be used as Gnd.
    /// </summary>
    public List<int> ValidGndPins;

    /// <summary>
    /// The valid VccLevels and their voltages.
    /// </summary>
    /// <remarks>
    /// Found by setting each value in the interval and measuring on the ZIF.
    /// </remarks>
    public List<Tuple<double, byte>> VccLevels;

    /// <summary>
    /// The valid VppLevels and their voltages.
    /// </summary>
    /// <remarks>
    /// Found by setting each value in the interval and measuring on the ZIF.
    /// </remarks>
    public List<Tuple<double, byte>> VppLevels;

    /// <summary>
    /// ctor.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="bulkDevice">The bulk device instance.</param>
    protected TopDevice(IShouter shouter, IUsbBulkDevice bulkDevice)
    {
      BulkDevice = bulkDevice;
      Shouter = shouter;
    }

    /// <summary>
    /// Opens a new instace of a bulk device using the constants specified for this Top Device.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <returns>The created instace of the bulk device.</returns>
    public static IUsbBulkDevice OpenBulkDevice(IShouter shouter)
    {
      return new UsbBulkDevice(shouter, VendorId, ProductId, Configuration, Interface, ReadEndpointInt, WriteEndpointInt);
    }

    /// <summary>
    /// Creates a new instance of the Top Device connected to the Top Programmer.
    /// <remarks>Atm. only works for model Top2005+.</remarks>
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <returns>The created instance of the Top Device.</returns>
    public static TopDevice Create(IShouter shouter)
    {
      var bulkDevice = OpenBulkDevice(shouter);

      var idString = ReadTopDeviceIdString(shouter, bulkDevice);
      shouter.ShoutLine(2, "Connected Top device is: {0}.", idString);

      if (idString.StartsWith("top2005+"))
        return new Top2005Plus(shouter, bulkDevice);

      throw new U2PaException("Not supported Top Programmer {0}", idString);
    }

    /// <summary>
    /// Reads the id string of the connected Top Programmer.
    /// </summary>
    /// <returns>The read id string.</returns>
    public string ReadTopDeviceIdString()
    {
      return ReadTopDeviceIdString(Shouter, BulkDevice);
    }

    /// <summary>
    /// Reads the id string of the connected Top Programmer.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <returns>The read id string.</returns>
    public static string ReadTopDeviceIdString(IShouter shouter)
    {
      using (var bulkDevice = OpenBulkDevice(shouter))
      {
        return ReadTopDeviceIdString(shouter, bulkDevice);
      }
    }

    /// <summary>
    /// Reads the id string of the connected Top Programmer.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="bulkDevice">The bulk device instance.</param>
    /// <returns>The read id string.</returns>
    public static string ReadTopDeviceIdString(IShouter shouter, IUsbBulkDevice bulkDevice)
    {
      bulkDevice.SendPackage(4, new byte[] { 0x0E, 0x11, 0x00, 0x00 }, "Write version string to buffer");
      bulkDevice.SendPackage(4, new byte[] { 0x07 }, "07 command");
      var readBuffer = bulkDevice.RecievePackage(4, "Reading buffer");

      return readBuffer.Take(16).Aggregate("", (current, t) => current + (char)t).TrimEnd();
    }

    /// <summary>
    /// Packs the provided data into packages of size 64;
    /// including a 4 byte header. These are used when programming the FPGA.
    /// </summary>
    /// <param name="bytes">The data to pack.</param>
    /// <returns>The packed packages ready for shipping.</returns>
    protected static IEnumerable<byte[]> PackFPGABytes(IEnumerable<byte> bytes)
    {
      var package = new List<byte> { 0x0e, 0x22, 0x00, 0x00 };
      foreach (var dataByte in bytes)
      {
        // Package full and ready to ship.
        if (package.Count == 64)
        {
          yield return package.ToArray();
          package = new List<byte> { 0x0e, 0x22, 0x00, 0x00 };
        }
        package.Add(dataByte);
      }
      // Last package not empty and not intirely filled.
      if (package.Count > 4)
      {
        while (package.Count < 64)
          package.Add(0x00);
        yield return package.ToArray();
      }
    }

    /// <summary>
    /// Reads an EPROM.
    /// </summary>
    /// <param name="eprom">The EPROM type.</param>
    /// <param name="progressBar">The progress bar.</param>
    /// <param name="bytes">The list that accumulates the read bytes.</param>
    /// <param name="fromAddress">Start reading this address.</param>
    /// <param name="totalNumberOfAdresses">The total number of adesses.</param>
    /// <returns>The next adress to be read.</returns>
    public int ReadEprom(
      Eprom eprom, 
      ProgressBar progressBar, 
      IList<byte> bytes, 
      int fromAddress, 
      int totalNumberOfAdresses)
    {
      const int rewriteCount = 5;
      const int rereadCount = 5;
      Shouter.ShoutLine(2, "Reading EPROM{0}", eprom.Type);
      SetVccLevel(eprom.VccLevel);
      var translator = new PinTranslator(eprom.DilType, ZIFType, 0, eprom.UpsideDown);
      ApplyVcc(translator.ToZIF, eprom.VccPins);
      ApplyGnd(translator.ToZIF, eprom.GndPins);
      PullUpsEnable(true);

      var zif = new ZIFSocket(40);
      var returnAddress = fromAddress;
      Shouter.ShoutLine(2, "Now reading bytes...");
      progressBar.Init();
      foreach (var address in Enumerable.Range(fromAddress, totalNumberOfAdresses - fromAddress))
      {
        zif.SetAll(true);
        zif.Disable(eprom.GndPins, translator.ToZIF);
        zif.Enable(eprom.Constants, translator.ToZIF);
        zif.Enable(eprom.ChipEnable, translator.ToZIF);
        zif.Enable(eprom.OutputEnable, translator.ToZIF);

        zif.SetEpromAddress(eprom, returnAddress = address);


        ZIFSocket resultZIF = null;
        var result = ReadSoundness.TryRewrite;

        for (var i = 0; i < rewriteCount; i++)
        {
          if (result == ReadSoundness.SeemsToBeAOkay)
            break;

          if (result == ReadSoundness.TryRewrite)
          {
            if (i > 0)
              progressBar.Shout("A: {0}; WS: {1}ms", address.ToString("X4"), 100*i);
            WriteZIF(zif, String.Format("A: {0}", address.ToString("X4")));
            if (i > 0)
              Thread.Sleep(100*i);
            result = ReadSoundness.TryReread;
          }

          for (var j = 0; j < rereadCount; j++)
          {
            if (result != ReadSoundness.TryReread)
              break;

            if (j > 0)
            {
              progressBar.Shout("A: {0}; WS: {1}ms; RS: {2}ms", address.ToString("X4"), 100*i, 100*(j*i));
              Thread.Sleep(100*(j*i));
            }
            var readZifs = ReadZIF(String.Format("for address {0}", address.ToString("X4")));
            result = Tools.AnalyzeEpromReadSoundness(readZifs, eprom, address, out resultZIF);
            if (j == rereadCount - 1)
              result = ReadSoundness.TryRewrite;
            if (result == ReadSoundness.SeemsToBeAOkay && j > 0)
              progressBar.Shout("Address: {0} read }};-P", address.ToString("X4"));
          }
        }

        if (result != ReadSoundness.SeemsToBeAOkay)
          return returnAddress;

        foreach(var b in resultZIF.GetEpromData(eprom))
          bytes.Add(b);

        progressBar.Progress();
      }
      return returnAddress + 1;
    }

    /// <summary>
    /// Writes an EPROM using the Classic Algorithm
    /// </summary>
    /// <param name="eprom">The EPROM type.</param>
    /// <param name="bytes">The bytes to write.</param>
    /// <param name="patch">Not used yet!</param>
    public void WriteEpromClassic(Eprom eprom, IList<byte> bytes, IList<int> patch = null)
    {
      var totalNumberOfAdresses = eprom.AddressPins.Length == 0 ? 0 : 1 << eprom.AddressPins.Length;
      var translator = new PinTranslator(eprom.DilType, ZIFType, 0, eprom.UpsideDown);

      SetVccLevel(eprom.VccLevel);
      ApplyGnd(translator.ToZIF, eprom.GndPins);
      ApplyVcc(translator.ToZIF, eprom.VccPins);

      var initZif = new ZIFSocket(40);
      initZif.SetAll(true);
      initZif.Disable(eprom.GndPins, translator.ToZIF);
      initZif.Enable(eprom.Constants, translator.ToZIF);
      initZif.Disable(eprom.ChipEnable, translator.ToZIF);
      initZif.Disable(eprom.OutputEnable, translator.ToZIF);
      initZif.Disable(eprom.Program, translator.ToZIF);
      WriteZIF(initZif, "Apply 1 to all data and address pins");

      SetVppLevel(eprom.VppLevel);
      ApplyVpp(translator.ToZIF, eprom.VppPins);
      PullUpsEnable(true);

      using (var progress = new ProgressBar(Shouter, totalNumberOfAdresses))
      {
        progress.Init();
        foreach (var address in Enumerable.Range(0, totalNumberOfAdresses))
        {
          //if(patch != null)
          //{
          //  if(!patch.Contains(address))
          //    continue;
          //  progress.Shout("Now patching address {0}", address);
          //}

          var zif = new ZIFSocket(40);

          // Pull up all pins
          zif.SetAll(true);

          zif.Disable(eprom.GndPins, translator.ToZIF);
          zif.Enable(eprom.Constants, translator.ToZIF);
          zif.Disable(eprom.Program, translator.ToZIF);
          zif.Disable(eprom.ChipEnable, translator.ToZIF);
          zif.Disable(eprom.OutputEnable, translator.ToZIF);

          // Set address and data
          zif.SetEpromAddress(eprom, address);
          var data = eprom.DataPins.Length > 8
            ? new[] { bytes[2 * address], bytes[2 * address + 1] } 
            : new[] { bytes[address]}; 
          zif.SetEpromData(eprom, data);

          // Prepare ZIF without programming in order to let it stabilize
          // TODO: Do we really need to do this?
          WriteZIF(zif, "Write address & data to ZIF");
          
          // In order to let the boost converter for manual Vcc
          // spin up to full output voltage.
          if(address == 0 && eprom.InitialProgDelay != 0)
            BulkDevice.Delay(eprom.InitialProgDelay);

          // Enter programming mode
          zif.Enable(eprom.Program, translator.ToZIF);
          zif.Enable(eprom.ChipEnable, translator.ToZIF);
          WriteZIF(zif, "Start pulse E");

          // Exit programming mode after at least <pulse> ms
          zif.Disable(eprom.Program, translator.ToZIF);
          zif.Disable(eprom.ChipEnable, translator.ToZIF);
          BulkDevice.Delay(eprom.ProgPulse);
          WriteZIF(zif, "End pulse E");

          progress.Progress();
        }
      }
    }

    /// <summary>
    /// NOT WORKING YET!
    /// </summary>
    /// <param name="eprom"></param>
    /// <param name="bytes"></param>
    public void WriteEpromFast(Eprom eprom, IList<byte> bytes)
    {
      if(eprom.VppPins.Any(p => eprom.OutputEnable.Any(q => p.Number == q.Number)))
        throw new U2PaException("Fast Programming Algoritm can't by used for this EPROM (yet)");
      var totalNumberOfAdresses = eprom.AddressPins.Length == 0 ? 0 : 1 << eprom.AddressPins.Length;
      var translator = new PinTranslator(eprom.DilType, ZIFType, 0, eprom.UpsideDown);
   
      {
        var zif = new ZIFSocket(40);
        zif.SetAll(true);
        zif.Disable(eprom.Program, translator.ToZIF);
  
        WriteZIF(zif, "Apply 1 to all pins");
        SetVccLevel(eprom.VccLevel);
        SetVppLevel(eprom.VppLevel);
        ApplyGnd(translator.ToZIF, eprom.GndPins);
        ApplyVcc(translator.ToZIF, eprom.VccPins);
        ApplyVpp(translator.ToZIF, eprom.VppPins);
      }
      
      using (var progress = new ProgressBar(Shouter, totalNumberOfAdresses))
      {
        progress.Init();
        foreach (var address in Enumerable.Range(0, totalNumberOfAdresses))
        {
          var pulse = 1;
          while (pulse <= 32)
          {
            // Writing
            var writerZif = new ZIFSocket(40);
            writerZif.SetAll(true);
            writerZif.Disable(eprom.GndPins, translator.ToZIF);
            writerZif.Enable(eprom.Constants, translator.ToZIF);
            writerZif.Disable(eprom.ChipEnable, translator.ToZIF);
            writerZif.Disable(eprom.OutputEnable, translator.ToZIF);
            writerZif.Disable(eprom.Program, translator.ToZIF);

            writerZif.SetEpromAddress(eprom, address);
            
            var data = eprom.DataPins.Length > 8
              ? new[] { bytes[2 * address], bytes[2 * address + 1] }
              : new[] { bytes[address] };
            writerZif.SetEpromData(eprom, data);

            WriteZIF(writerZif, "Write address & d ata to ZIF");
            writerZif.Enable(eprom.ChipEnable, translator.ToZIF);
            writerZif.Enable(eprom.Program, translator.ToZIF);
            WriteZIF(writerZif, "Start pulse E");
            writerZif.Disable(eprom.ChipEnable, translator.ToZIF);
            writerZif.Disable(eprom.Program, translator.ToZIF);
            BulkDevice.Delay(pulse);
            WriteZIF(writerZif, "End pulse E");
            
            // Reading
            var addressZif = new ZIFSocket(40);
            addressZif.SetAll(true);
            addressZif.Disable(eprom.GndPins, translator.ToZIF);
            addressZif.Enable(eprom.Constants, translator.ToZIF);
            addressZif.Enable(eprom.ChipEnable, translator.ToZIF);
            addressZif.Enable(eprom.OutputEnable, translator.ToZIF);
            addressZif.Disable(eprom.Program, translator.ToZIF);
            addressZif.SetEpromAddress(eprom, address);

            WriteZIF(addressZif, "Write address");
            var dataZifs = ReadZIF("Read data");
            ZIFSocket resultZif;
            if (Tools.AnalyzeEpromReadSoundness(dataZifs, eprom, address, out resultZif) == ReadSoundness.SeemsToBeAOkay)
            {
              if (resultZif.GetEpromData(eprom).SequenceEqual(data))
              {
                // Data validates; now we overprogram
                writerZif.Enable(eprom.ChipEnable, translator.ToZIF);
                writerZif.Enable(eprom.Program, translator.ToZIF);
                WriteZIF(writerZif, "Overprogram");
                BulkDevice.Delay(3 * pulse);
                writerZif.Disable(eprom.ChipEnable, translator.ToZIF);
                writerZif.Disable(eprom.Program, translator.ToZIF);
                WriteZIF(writerZif, "End pulse E");
                break;
              }
              else
              {
                Console.WriteLine("Pulse: {0}", pulse);
                Console.WriteLine("Address: {0}", address.ToString("X4"));
                Console.WriteLine(writerZif.GetEpromData(eprom).First());
                Console.WriteLine(resultZif.GetEpromData(eprom).First());
              }
            }


            pulse *= 2;
          }
          if (pulse > 32)
          {
            progress.Shout("Address {0} doesn't validate! }};-(", address.ToString("X4"));
          }
          progress.Progress();
        }
      }
    }

    /// <summary>
    /// A simple SRAM test.
    /// </summary>
    /// <param name="shouter">Public shouter instance.</param>
    /// <param name="sram">The SRAM type.</param>
    /// <param name="progressBar">The progress bar.</param>
    /// <param name="totalNumberOfAdresses">The total number of adresses.</param>
    /// <param name="firstBit">The value of the first bit to write to the SRAM.</param>
    /// <returns>Tuples of non-working addresses: (address, read_byte, expected_byte).</returns>
    public List<Tuple<int, string, string>> SRamTest(IShouter shouter, SRam sram, ProgressBar progressBar, int totalNumberOfAdresses, bool firstBit)
    {
      var tr = new PinTranslator(sram.DilType, 40, sram.Placement, sram.UpsideDown);
      SetVccLevel(sram.VccLevel);
      ApplyGnd(tr.ToZIF, sram.GndPins);
      ApplyVcc(tr.ToZIF, sram.VccPins);

      var badCells = new List<Tuple<int, string, string>>();
      var writerZif = new ZIFSocket(40);
      var startBit = firstBit;
      for (var address = 0; address < totalNumberOfAdresses; address++)
      {
        var bit = startBit;
        writerZif.SetAll(true);
        writerZif.Disable(sram.GndPins, tr.ToZIF);
        writerZif.Enable(sram.Constants, tr.ToZIF);
        writerZif.Enable(sram.ChipEnable, tr.ToZIF);
        writerZif.SetSRamAddress(sram, address);
        foreach (var i in sram.DataPins.Select(tr.ToZIF))
        {
          writerZif[i] = bit;
          bit = !bit;
        }
        startBit = !startBit;
        WriteZIF(writerZif, "");

        writerZif.Enable(sram.WriteEnable, tr.ToZIF);
        WriteZIF(writerZif, "");

        writerZif.Disable(sram.WriteEnable, tr.ToZIF);
        WriteZIF(writerZif, "");

        progressBar.Progress();
      }

      startBit = firstBit;
      for (var address = 0; address < totalNumberOfAdresses; address++)
      {
        var bit = startBit;
        writerZif.SetAll(true);
        writerZif.Disable(sram.GndPins, tr.ToZIF);
        writerZif.Enable(sram.Constants, tr.ToZIF);
        writerZif.Enable(sram.ChipEnable, tr.ToZIF);
        writerZif.Enable(sram.OutputEnable, tr.ToZIF);
        writerZif.Disable(sram.WriteEnable, tr.ToZIF);
        writerZif.SetSRamAddress(sram, address);
        WriteZIF(writerZif, "Writing SRam address.");

        var readerZif = ReadZIF("Reading SRam data.")[0];
        foreach (var i in sram.DataPins.Select(tr.ToZIF))
        {
          if (readerZif[i] != bit)
          {
            var expected = "";
            var read = "";
            bit = startBit;
            foreach (var j in sram.DataPins.Select(tr.ToZIF))
            {
              expected = expected + (bit ? "1" : "0");
              read = read + (readerZif[j] ? "1" : "0");
              bit = !bit;
            }
            badCells.Add(Tuple.Create(address, read, expected));
            progressBar.Shout("Bad cell at address {0}", address.ToString("X4"));
            break;
          }
          bit = !bit;
        }

        startBit = !startBit;
        progressBar.Progress();
      }
      return badCells;
    }

    /// <summary>
    /// Reads the ZIF-socket (12 times).
    /// </summary>
    /// <param name="packageName">The message to shout.</param>
    /// <returns>The 12 <see cref="ZIFSocket"/>s.</returns>
    public virtual ZIFSocket[] ReadZIF(string packageName)
    {
      var package = new List<byte>();
      for(var i = 0; i < 12; i++)
        for(byte b = 0x01; b <= 0x05; b++)
          package.Add(b);
      package.Add(0x07);

      // Write ZIF to Top Programmer buffer
      BulkDevice.SendPackage(5, package.ToArray(), "Writing 12 x ZIF to buffer.");
 
      // Read buffer
      var readBuffer = BulkDevice.RecievePackage(5, "Reading ZIF {0}.", packageName);

      var zifs = new List<ZIFSocket>();
      for(var i = 0; i + 5 <= 60; i += 5)
      {
        var bytes = new List<byte>();
        for(var j = i; j < i + 5; j++)
          bytes.Add(readBuffer[j]);
        zifs.Add(new ZIFSocket(40, bytes.ToArray()));
      }
      return zifs.ToArray();
    }

    /// <summary>
    /// Writes a <see cref="ZIFSocket"/>.
    /// </summary>
    /// <param name="zif">The ZIF to be written.</param>
    /// <param name="packageName">The meassage to shout.</param>
    public virtual void WriteZIF(ZIFSocket zif, string packageName)
    {
      var rawBytes = zif.ToTopBytes();
      var package = new byte[]
                          {
                            0x10, rawBytes[0],
                            0x11, rawBytes[1],
                            0x12, rawBytes[2],
                            0x13, rawBytes[3],
                            0x14, rawBytes[4],
                            0x0A, 0x15, 0xFF
                          };

      BulkDevice.SendPackage(5, package, "{0} written to ZIF.", packageName);
    }

    /// <summary>
    /// Do we enable pullups on the ZIF-pins? 
    /// </summary>
    /// <param name="enable">True if enable.</param>
    public virtual void PullUpsEnable(bool enable)
    {
      BulkDevice.SendPackage(4, new byte[] { 0x0e, 0x28, (byte)(enable ? 0x01 : 0x00), 0x00 },
        "PullUps are {0}", enable ? "enabled" : "disabled");
    }

    /// <summary>
    /// Sets the Vpp level (byte version).
    /// </summary>
    /// <param name="level">The level represented as a raw byte.</param>
    public virtual void SetVppLevel(byte level)
    {
      BulkDevice.SendPackage(4, new byte[] { 0x0e, 0x12, level, 0x00 },
        "Vpp = {0}", level.ToString("X4"));
    }

    /// <summary>
    /// Sets the Vpp level (double version)
    /// </summary>
    /// <param name="rawLevel">The level represented as a voltage (double).</param>
    internal virtual void SetVppLevel(double rawLevel)
    {
      string stringLevel;
      var level = TranslateVppLevel(rawLevel, out stringLevel);
      BulkDevice.SendPackage(4, new byte[] { 0x0e, 0x12, level, 0x00 }, "Vpp = {0}", stringLevel);
    }

    /// <summary>
    /// Sets the Vcc level (byte version).
    /// </summary>
    /// <param name="level">The level represented as a raw byte.</param>
    internal virtual void SetVccLevel(byte level)
    {
      BulkDevice.SendPackage(4, new byte[] { 0x0e, 0x13, level, 0x00 },
        "Vcc = {0}", level.ToString("X4"));
    }

    /// <summary>
    /// Sets the Vcc level (double version)
    /// </summary>
    /// <param name="rawLevel">The level represented as a voltage (double).</param>
    public virtual void SetVccLevel(double rawLevel)
    {
      string stringLevel;
      var level = TranslateVccLevel(rawLevel, out stringLevel);
      BulkDevice.SendPackage(4, new byte[] { 0x0e, 0x13, level, 0x00 }, "Vcc = {0}", stringLevel);
    }

    /// <summary>
    /// Translates from a voltage (double) to the raw byte.
    /// </summary>
    /// <param name="rawLevel">The voltage.</param>
    /// <param name="stringLevel">Outputs the actual voltage as a string.</param>
    /// <returns>The raw byte.</returns>
    private byte TranslateVppLevel(double rawLevel, out string stringLevel)
    {
      return TranslateLevel(VppLevels, rawLevel, out stringLevel);
    }

    /// <summary>
    /// Translates from a voltage (double) to the raw byte.
    /// </summary>
    /// <param name="rawLevel">The voltage.</param>
    /// <param name="stringLevel">Outputs the actual voltage as a string.</param>
    /// <returns>The raw byte.</returns>
    private byte TranslateVccLevel(double rawLevel, out string stringLevel)
    {
      return TranslateLevel(VccLevels, rawLevel, out stringLevel);
    }

    /// <summary>
    /// Translates from a voltage (double) to the raw byte.
    /// </summary>
    /// <param name="levels">The levels to use when translating.</param>
    /// <param name="rawLevel">The voltage.</param>
    /// <param name="stringLevel">Outputs the actual voltage as a string.</param>
    /// <returns>The raw byte.</returns>
    private byte TranslateLevel(IList<Tuple<double, byte>> levels, double rawLevel, out string stringLevel)
    {
      var foundLevel = levels.OrderBy(x => x.Item1).TakeWhile(x => x.Item1 <= rawLevel).Last();
      stringLevel = foundLevel.Item1.ToString("F1");
      return foundLevel.Item2;
    }

    /// <summary>
    /// Clears all Vpp pins; then applies Vpp to the specified pins.
    /// </summary>
    /// <param name="translate">The translation function to use.</param>
    /// <param name="dilPins">The pins to set.</param>
    public virtual void ApplyVpp(Func<Pin, int> translate = null, params Pin[] dilPins)
    {
      ApplyPropertyToPins("Vpp", 0x14, ValidVppPins, translate, dilPins);
    }

    /// <summary>
    /// Clears all Vcc pins; then applies Vcc to the specified pins.
    /// </summary>
    /// <param name="translate">The translation function to use.</param>
    /// <param name="dilPins">The pins to set.</param>
    public virtual void ApplyVcc(Func<Pin, int> translate = null, params Pin[] dilPins)
    {
      ApplyPropertyToPins("Vcc", 0x15, ValidVccPins, translate, dilPins);
    }

    /// <summary>
    /// Clears all Gnd pins; then applies Gnd to the specified pins.
    /// </summary>
    /// <param name="translate">The translation function to use.</param>
    /// <param name="dilPins">The pins to set.</param>
    public virtual void ApplyGnd(Func<Pin, int> translate = null, params Pin[] dilPins)
    {
      ApplyPropertyToPins("Gnd", 0x16, ValidGndPins, translate, dilPins);
    }

    /// <summary>
    /// Use by the other Apply...-methods.
    /// </summary>
    protected virtual void ApplyPropertyToPins(string name, byte propCode, ICollection<int> validPins, Func<Pin, int> translate = null, params Pin[] dilPins)
    {
      translate = translate ?? (x => x.Number);
      // Always start by clearing all pins
      BulkDevice.SendPackage(4, new byte[] { 0x0e, propCode, 0x00, 0x00 }, "All {0} pins cleared", name);

      foreach (var zifPin in dilPins.Select(translate))
      {
        if (!validPins.Contains(zifPin))
          throw new U2PaException("Pin {0} is not a valid {1} pin.", zifPin, name);
        BulkDevice.SendPackage(4, new byte[] { 0x0e, propCode, (byte)zifPin, 0x00 }, "{0} applied to pin {1}", name, zifPin);
      }
    }

    /// <summary>
    /// Removes all pin assingments from the physical device and
    /// disposes all unmanaged resources.<s
    /// </summary>
    public void Dispose()
    {
      DisposeSpecific();

      SetVppLevel(0x00);
      SetVccLevel(0x00);

      ApplyVpp();
      ApplyVcc();
      ApplyGnd();

      PullUpsEnable(false);
      // Remove all pin assignments
       if (BulkDevice == null) return;
      BulkDevice.Dispose();
    }

    /// <summary>
    /// Disposes unmanaged resources specific to derived classes.
    /// <remarks>The first thing <seealso cref="Dispose"/>does, is call this.</remarks>
    /// </summary>
    protected virtual void DisposeSpecific()
    {}
  }
}
