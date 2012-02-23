using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using U2Pa.Lib.Eproms;

namespace U2Pa.Lib
{
  public abstract class TopDevice : IDisposable
  {
    // TODO: Find out if these are the asme for all Top Programmers.
    protected const int VendorId = 0x2471;
    protected const int ProductId = 0x0853;
    protected UsbDevice UsbDevice { get; private set; }
    protected UsbEndpointReader UsbEndpointReader { get; private set; }
    protected UsbEndpointWriter UsbEndpointWriter { get; private set; }

    public PublicAddress PA { get; private set; }

    public List<byte> ValidVccPins;
    public List<byte> ValidVppPins;
    public List<byte> ValidGndPins;

    protected TopDevice(PublicAddress pa, UsbDevice usbDevice, UsbEndpointReader usbEndpointReader, UsbEndpointWriter usbEndpointWriter)
    {
      UsbDevice = usbDevice;
      UsbEndpointReader = usbEndpointReader;
      UsbEndpointWriter = usbEndpointWriter;
      PA = pa;
    }

    public static TopDevice Create(PublicAddress pa)
    {
      var usbDevice = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(VendorId, ProductId));

      // If the device is open and ready
      if (usbDevice == null)
        throw new U2PaException(
          "Top Universal Programmer with VendorId: 0x{0} and ProductId: 0x{1} not found.", 
          VendorId.ToString("X4"), 
          ProductId.ToString("X4"));

      pa.ShoutLine(4,
                   "Top Universal Programmer with VendorId: 0x{0} and ProductId: 0x{1} found.",
                   usbDevice.UsbRegistryInfo.Vid.ToString("X2"),
                   usbDevice.UsbRegistryInfo.Pid.ToString("X2"));

      var wholeUsbDevice = usbDevice as IUsbDevice;
      if (!ReferenceEquals(wholeUsbDevice, null))
      {
        // Select config #1
        wholeUsbDevice.SetConfiguration(1);
        byte temp;
        if (wholeUsbDevice.GetConfiguration(out temp))
          pa.ShoutLine(4, "Configuration with id: {0} selected.", temp.ToString("X2"));
        else
          throw new U2PaException("Failed to set configuration id: {0}", 1);

        // Claim interface #0.
        if (wholeUsbDevice.ClaimInterface(0))
          pa.ShoutLine(4, "Interface with id: {0} claimed.", 0);
        else
          throw new U2PaException("Failed to claim interface with id: {0}", 1);
      }

      // Open read endpoint $82 aka ReadEndPoint.Ep02.
      var usbEndpointReader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep02);
      if (usbEndpointReader == null)
        throw new U2PaException("Unable to open read endpoint ${0}", "82");
      pa.ShoutLine(4, "Reader endpoint ${0} opened.", usbEndpointReader.EndpointInfo.Descriptor.EndpointID.ToString("X2"));


      // Open write endpoint $02 aka WriteEndPoint.Ep02
      var usbEndpointWriter = usbDevice.OpenEndpointWriter(WriteEndpointID.Ep02);
      if (usbEndpointWriter == null)
        throw new U2PaException("Unable to open write endpoint ${0}", "02");
      pa.ShoutLine(4, "Writer endpoint ${0} opened.", usbEndpointWriter.EndpointInfo.Descriptor.EndpointID.ToString("X2"));

      var idString = ReadTopDeviceIdString(pa, usbEndpointReader, usbEndpointWriter);
      pa.ShoutLine(2, "Connected Top device is: {0}.", idString);

      if (idString.StartsWith("top2005+"))
        return new Top2005Plus(pa, usbDevice, usbEndpointReader, usbEndpointWriter);

      throw new U2PaException("Not supported Top Programmer {0}", idString);
    }

    public string ReadTopDeviceIdString()
    {
      return ReadTopDeviceIdString(PA, UsbEndpointReader, UsbEndpointWriter);
    }

    protected static string ReadTopDeviceIdString(PublicAddress pa, UsbEndpointReader usbEndpointReader, UsbEndpointWriter usbEndpointWriter)
    {
      int transferLength;
      var errorCode = usbEndpointWriter.Write(new byte[] { 0x0E, 0x11, 0x00, 0x00 }, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 4)
        pa.ShoutLine(4, "Send write-version-string-to-buffer command.");
      else
        throw new U2PaException("Failed to send write-version-string-to-buffer command. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);

      errorCode = usbEndpointWriter.Write(new byte[] { 0x07 }, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 1)
        pa.ShoutLine(4, "Send 07 command.");
      else
        throw new U2PaException("Failed to send 07 command. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);

      var readBuffer = new byte[64];
      errorCode = usbEndpointReader.Read(readBuffer, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == readBuffer.Length)
        pa.ShoutLine(4, "Buffer read.");
      else
        throw new U2PaException("Failed to read buffer. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);

      // Can we find a better stop condition than t != 0x20?
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

      // TODO: Make this alot smarter by using something like the below...
      //'skip e character
      //bitPtr = bitPtr + 1

      //'Get the length of bitstream
      //bitLen = 16777216 * fbuf(bitPtr) _
      //    + 65536 * fbuf(bitPtr + 1) _
      //    + 256 * fbuf(bitPtr + 2) _
      //    + fbuf(bitPtr + 3)

      //'skip pointer past length
      //bitPtr = bitPtr + 4

      //If fLen + 1 - bitPtr <> bitLen Then
      //    MsgBox ("Mismatch in bitstream len and file data remaining")
      //    Exit Sub
      //End If
    }

    internal void SendRawPackage(int verbosity, byte[] data, string description)
    {
      int transferLength;
      int timeOut = Math.Max(1000, data.Length / 10);
      var errorCode = UsbEndpointWriter.Write(data, timeOut, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == data.Length)
        PA.ShoutLine(verbosity, "Write operation success: {0}. Timeout {1}ms.", description, timeOut);
      else
        throw new U2PaException("Write operation failure. {0}. Transferlength: {1} ErrorCode: {2}", description, transferLength, errorCode);
    }

    protected byte[] RecieveRawPackage(int verbosity, string description)
    {
      var readBuffer = new byte[64];
      int transferLength;
      int timeOut = 1000;
      var errorCode = UsbEndpointReader.Read(readBuffer, timeOut, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == readBuffer.Length)
        PA.ShoutLine(verbosity, "Read operation  success: {0}. Timeout {1}ms.", description, timeOut);
      else
        throw new U2PaException("Read operation failure: {0}. Transferlength: {1} ErrorCode: {2}", description, transferLength, errorCode);
      return readBuffer;
    }

    public ErrorCode Write(byte[] package, int timeOut, out int transferLength)
    {
      return UsbEndpointWriter.Write(package, timeOut, out transferLength);
    }

    public ErrorCode Read(byte[] buffer, int timeOut, out int transferLength)
    {
      return UsbEndpointReader.Read(buffer, timeOut, out transferLength);
    }

    public int ReadEprom(Eprom eprom, PublicAddress.ProgressBar progressBar, IList<byte> bytes, int fromAddress)
    {
      const int rewriteCount = 5;
      const int rereadCount = 5;
      PA.ShoutLine(2, "Reading EPROM{0}.", eprom.Type);
      // Setting up chip...
      SetVccLevel(eprom.VccLevel);
      //SetVppLevel(eprom.VppLevel);
      var translator = new PinNumberTranslator(eprom.DilType, 0);
      ApplyVcc(translator, eprom.VccPins);
      ApplyGnd(translator, eprom.GndPins);

      var zif = new ZIFSocket(40);
      int totalNumberOfAdresses = 2.Pow(eprom.AddressPins.Length);
      int returnAddress = fromAddress;
      PA.ShoutLine(2, "Now reading bytes...");
      progressBar.Init();
      for (var address = fromAddress; address < totalNumberOfAdresses; address++)
      {
        returnAddress = address;
        zif.SetAll(true);
        zif.SetEpromAddress(eprom, address);

        // Set enable pins low
        foreach (var p in eprom.EnablePins)
          zif[translator.ToZIF(p)] = false;

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
            var readZifs = ReadZIF(String.Format("for address {0}", address.ToString("X4")), address);
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

    public virtual ZIFSocket[] ReadZIF(string packageName, int address)
    {
      var package = new List<byte>();
      for(var i = 0; i < 12; i++)
        for(byte b = 0x01; b <= 0x05; b++)
          package.Add(b);
      package.Add(0x07);

      // Write ZIF to Top Programmer buffer
      int transferLength;
      var errorCode = Write(package.ToArray(), 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == package.Count)
        PA.ShoutLine(5, "12 x ZIF written to buffer.");
      else
        throw new U2PaException("Failed to write 12 x ZIF to buffer. Transferlength: {0} ErrorCode: {1}",
                                transferLength, errorCode);

      // Read buffer
      var readBuffer = new byte[64];
      errorCode = Read(readBuffer, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == readBuffer.Length)
        PA.ShoutLine(5, "ZIF read {0}.", packageName);
      else
        throw new U2PaException("ZIF read failed {0}. Transferlength: {1} ErrorCode: {2}", packageName,
                                transferLength, errorCode);

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
      int transferLength;
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

      var errorCode = Write(package, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == package.Length)
        PA.ShoutLine(5, "{0} written to ZIF.", packageName);
      else
        throw new U2PaException("{0} write failed {0}. Transferlength: {1} ErrorCode: {2}",
                                packageName, transferLength, errorCode);
    }

    public virtual void SetVppLevel(VppLevel level)
    {
      int transferLength;
      var errorCode = UsbEndpointWriter.Write(new byte[] { 0x0e, 0x12, (byte)level, 0x00 }, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 4)
        PA.ShoutLine(4, "Vpp = {0}", level.ToString().Substring(4).Replace('_', '.'));
      else
        throw new U2PaException("Failed to set Vpp. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);
    }

    public virtual void SetVccLevel(VccLevel level)
    {
      int transferLength;
      var errorCode = UsbEndpointWriter.Write(new byte[] { 0x0e, 0x13, (byte)level, 0x00 }, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 4)
        PA.ShoutLine(4, "Vcc = {0}", level.ToString().Substring(4).Replace('_', '.'));
      else
        throw new U2PaException("Failed to set Vcc. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);
    }

    public virtual void ApplyVpp(PinNumberTranslator translator, params byte[] zipPins)
    {
      ApplyPropertyToPins(translator, "Vpp", 0x14, ValidVppPins, zipPins);
    }

    public virtual void ApplyVcc(PinNumberTranslator translator, params byte[] zifPins)
    {
      ApplyPropertyToPins(translator, "Vcc", 0x15, ValidVccPins, zifPins);
    }

    public virtual void ApplyGnd(PinNumberTranslator translator, params byte[] zifPins)
    {
      ApplyPropertyToPins(translator, "Gnd", 0x16, ValidGndPins, zifPins);
    }

    protected virtual void ApplyPropertyToPins(PinNumberTranslator translator, string name, byte propCode, ICollection<byte> validPins, params byte[] dilPins)
    {
      // Always start by clearing all pins
      int transferLength;
      var errorCode = UsbEndpointWriter.Write(new byte[] { 0x0e, propCode, 0x00, 0x00 }, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 4)
        PA.ShoutLine(4, "All {0} pins cleared", name);
      else
        throw new U2PaException("Failed to clear {0} pins. Transferlength: {1} ErrorCode: {2}", name, transferLength, errorCode);

      foreach (var dilPin in dilPins)
      {
        var zifPin = translator.ToZIF(dilPin);
        if (!validPins.Contains(zifPin))
          throw new U2PaException("Pin {0} is not a valid {1} pin.", zifPin, name);
        errorCode = UsbEndpointWriter.Write(new byte[] { 0x0e, propCode, zifPin, 0x00 }, 1000, out transferLength);
        if (errorCode == ErrorCode.None && transferLength == 4)
          PA.ShoutLine(4, "{0} applied to pin {1}", name, zifPin);
        else
          throw new U2PaException("Failed to apply {0} to pin {1}. Transferlength: {2} ErrorCode: {3}", name, zifPin, transferLength, errorCode);

      }
    }

    public void Dispose()
    {
      DisposeSpecific();

      // Remove all pin assignments
      ApplyVpp(null);
      ApplyVcc(null);
      ApplyGnd(null);
      if (UsbDevice == null) return;
      if (UsbDevice.IsOpen)
      {
        var wholeUsbDevice = UsbDevice as IUsbDevice;
        if (!ReferenceEquals(wholeUsbDevice, null))
        {
          // Release interface #0.
          wholeUsbDevice.ReleaseInterface(0);
          PA.ShoutLine(4, "Interface released.");
        }

        UsbDevice.Close();
        PA.ShoutLine(4, "Device closed.");
      }
      UsbDevice = null;

      // Free usb resources
      UsbDevice.Exit();
      PA.ShoutLine(4, "USB resources freed.");
    }

    protected virtual void DisposeSpecific()
    {}
  }
}