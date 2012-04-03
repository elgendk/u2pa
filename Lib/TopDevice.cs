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
using LibUsbDotNet;
using LibUsbDotNet.Main;
using U2Pa.Lib.IC;

namespace U2Pa.Lib
{
  public abstract class TopDevice : IDisposable
  {
    // TODO: Find out if these are the same for all Top Programmers.
    protected const int VendorId = 0x2471;
    protected const int ProductId = 0x0853;
    protected const int Configuration = 1;
    protected const byte Interface = 0;
    protected const ReadEndpointID ReadEndpointId = ReadEndpointID.Ep02;
    protected const WriteEndpointID WriteEndpointId = WriteEndpointID.Ep02;
    protected UsbBulkDevice BulkDevice;

    public abstract int ZIFType { get; }

    public PublicAddress PA { get; private set; }

    public List<int> ValidVccPins;
    public List<int> ValidVppPins;
    public List<int> ValidGndPins;

    protected TopDevice(PublicAddress pa, UsbBulkDevice bulkDevice)
    {
      BulkDevice = bulkDevice;
      PA = pa;
    }

    public static TopDevice Create(PublicAddress pa)
    {
      var bulkDevice = new UsbBulkDevice(pa, VendorId, ProductId, Configuration, Interface, ReadEndpointId, WriteEndpointId);

      var idString = ReadTopDeviceIdString(pa, bulkDevice);
      pa.ShoutLine(2, "Connected Top device is: {0}.", idString);

      if (idString.StartsWith("top2005+"))
        return new Top2005Plus(pa, bulkDevice);

      throw new U2PaException("Not supported Top Programmer {0}", idString);
    }

    public string ReadTopDeviceIdString()
    {
      return ReadTopDeviceIdString(PA, BulkDevice);
    }

    protected static string ReadTopDeviceIdString(PublicAddress pa, UsbBulkDevice bulkDevice)
    {
      bulkDevice.SendPackage(4, new byte[] { 0x0E, 0x11, 0x00, 0x00 }, "Write version string to buffer");
      bulkDevice.SendPackage(4, new byte[] { 0x07 }, "07 command");
      var readBuffer = bulkDevice.RecievePackage(4, "Reading buffer");

      // Can't we find a better stop condition than t != 0x20?
      return readBuffer.TakeWhile(t => t != 0x20).Aggregate("", (current, t) => current + (char)t);
    }

    protected static IEnumerable<byte[]> PackBytes(IEnumerable<byte> bytes)
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

    protected static int CheckBitStreamHeader(IList<byte> bytes)
    {
      return 0x47;
      // TODO: Make this alot smarter...
      //var startIndex = 0;
      //for (var i = 0; i < bytes.Count; i++)
      //{
      //  // Find position of 2nd 'e' in the stream...only works for ictest.bin
      //  if (bytes[i] == 0x65 && i > 19)
      //  {
      //    startIndex = i;
      //    break;
      //  }
      //}
      //if (startIndex == 0 || startIndex == bytes.Count)
      //  throw new U2PaException("Header check of bitstream failed!");
      //return startIndex + 1 + 4;
    }

    public int ReadEprom(
      Eprom eprom, 
      PublicAddress.ProgressBar progressBar, 
      IList<byte> bytes, 
      int fromAddress, 
      int totalNumberOfAdresses,
      IList<int> failedAddresses = null)
    {
      const int rewriteCount = 5;
      const int rereadCount = 5;
      PA.ShoutLine(2, "Reading EPROM{0}", eprom.Type);
      // Setting up chip...
      SetVccLevel(eprom.VccLevel);
      var translator = new PinTranslator(eprom.DilType, ZIFType, 0, eprom.UpsideDown);
      ApplyVcc(translator.ToZIF, eprom.VccPins);
      ApplyGnd(translator.ToZIF, eprom.GndPins);

      var zif = new ZIFSocket(40);
      var returnAddress = fromAddress;
      PA.ShoutLine(2, "Now reading bytes...");
      progressBar.Init();
      foreach (var address in failedAddresses ?? Tools.Interval(fromAddress, totalNumberOfAdresses))
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
              progressBar.Shout("Address: {0} read }};-P", address);
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

    public void WriteEpromClassic(Eprom eprom, int pulse, IList<byte> bytes, IList<int> patch = null)
    {
      var totalNumberOfAdresses = eprom.AddressPins.Length == 0 ? 0 : 1 << eprom.AddressPins.Length;
      var stopWatch = new Stopwatch();
      var translator = new PinTranslator(eprom.DilType, ZIFType, 0, eprom.UpsideDown);

      var zif = new ZIFSocket(40);
      zif.SetAll(true);
      zif.Disable(eprom.Program, translator.ToZIF);

      WriteZIF(zif, "Apply 1 to all pins");
      SetVccLevel(eprom.VccLevel);
      SetVppLevel(eprom.VppLevel);
      ApplyGnd(translator.ToZIF, eprom.GndPins);
      ApplyVcc(translator.ToZIF, eprom.VccPins);
      ApplyVpp(translator.ToZIF, eprom.VppPins);

      using (var progress = PA.GetProgressBar(totalNumberOfAdresses))
      {
        progress.Init();
        foreach (var address in Tools.Interval(0, totalNumberOfAdresses))
        {
          if(patch != null)
          {
            if(!patch.Contains(address))
              continue;
            progress.Shout("Now patching address {0}", address);
          }

          // Pull up all pins
          zif.SetAll(true);

          // Set address and data
          zif.SetEpromAddress(eprom, address);
          var data = eprom.AddressPins.Length > 8
            ? new[] { bytes[2 * address], bytes[2 * address + 1] } 
            : new[] { bytes[address]}; 
          zif.SetEpromData(eprom, data);

          //  Set programming mode
          zif.Enable(eprom.Program, translator.ToZIF);

          zif.Disable(eprom.ChipEnable, translator.ToZIF);

          // Prepare ZIF without programming in order to let it stabilize
          // TODO: Do we really need to do this?
          WriteZIF(zif, "Write address & data to ZIF");

          zif.Enable(eprom.ChipEnable, translator.ToZIF);
          stopWatch.Reset();
          WriteZIF(zif, "Start pulse E");
          stopWatch.Start();

          // Set ChipEnable low again after at least <pulse> ms
          zif.Disable(eprom.ChipEnable, translator.ToZIF);
          while (stopWatch.ElapsedMilliseconds <= pulse)
          {
            /* Wait at least <pulse> ms */
          }
          WriteZIF(zif, "End pulse E");
          progress.Progress();
        }
      }
    }

    public List<Tuple<int, string, string>> SRamTest(PublicAddress pa, SRam sram, PublicAddress.ProgressBar progressBar, int totalNumberOfAdresses, TopDevice topDevice, bool firstBit)
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

    public virtual void SetVppLevel(VppLevel level)
    {
      BulkDevice.SendPackage(4, new byte[] { 0x0e, 0x12, (byte)level, 0x00 }, 
        "Vpp = {0}", level.ToString().Substring(4).Replace('_', '.'));
    }

    public virtual void SetVccLevel(VccLevel level)
    {
      BulkDevice.SendPackage(4, new byte[] { 0x0e, 0x13, (byte)level, 0x00 },
        "Vcc = {0}", level.ToString().Substring(4).Replace('_', '.'));
    }

    public virtual void ApplyVpp(Func<Pin, int> translate = null, params Pin[] dilPins)
    {
      ApplyPropertyToPins("Vpp", 0x14, ValidVppPins, translate, dilPins);
    }

    public virtual void ApplyVcc(Func<Pin, int> translate = null, params Pin[] dilPins)
    {
      ApplyPropertyToPins("Vcc", 0x15, ValidVccPins, translate, dilPins);
    }

    public virtual void ApplyGnd(Func<Pin, int> translate = null, params Pin[] dilPins)
    {
      ApplyPropertyToPins("Gnd", 0x16, ValidGndPins, translate, dilPins);
    }

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

    public void Dispose()
    {
      DisposeSpecific();

      // Remove all pin assignments
      ApplyVpp();
      ApplyVcc();
      ApplyGnd();
      if (BulkDevice == null) return;
      BulkDevice.Dispose();
    }

    protected virtual void DisposeSpecific()
    {}
  }
}