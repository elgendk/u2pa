using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using U2Pa.Eproms;

namespace U2Pa
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

    public List<byte> VccPins;
    public List<byte> VppPins;
    public List<byte> GndPins;

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
          "Top2005+ Programmer with VendorId: 0x{0} and ProductId: 0x{1} not found.", VendorId.ToString("X4"), ProductId.ToString("X4"));

      pa.ShoutLine(4,
                   "Top2005+ Programmer with VendorId: 0x{0} and ProductId: 0x{1} found.",
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
      pa.ShoutLine(1, "Connected Top device is: {0}.", idString);

      if (idString.StartsWith("top2005+"))
        return new Top2005Plus(pa, usbDevice, usbEndpointReader, usbEndpointWriter);

      throw new U2PaException("Not supported Top Programmer {0}", idString);
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

    protected void SendRawPackage(int verbosity, byte[] data, string description)
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
      var errorCode = UsbEndpointReader.Read(readBuffer, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == readBuffer.Length)
        PA.ShoutLine(verbosity, "Read operation success: {0}.", description);
      else
        throw new U2PaException("Read operation success: {0}. Transferlength: {1} ErrorCode: {2}", description, transferLength, errorCode);
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

    public IEnumerable<byte> ReadEprom(string type)
    {
      var eprom = Eprom.Create(type);
      PA.ShoutLine(4, "Reading EPROM{0}...", eprom.Type);
      // Setting up chip...
      SetVccLevel(eprom.VccLevel);
      SetVppLevel(eprom.VppLevel);
      ApplyVcc(eprom.VccPins);
      ApplyGnd(eprom.GndPins);

      var t = new PinNumberTranslator(eprom.DilType, 0);

      var zif = new ZIFSocket(40);
      for (int address = 0; address < 2.Pow(eprom.AddressPins.Length); address++)
      {
        // Reset ZIF Vector to all 1's
        zif.SetAll(true);

        // Set adress pins
        var bitAddress = new BitArray(new[] { address });
        for (var i = 0; i < eprom.AddressPins.Length; i++)
          zif[t.ToZIF(eprom.AddressPins[i])] = bitAddress[i];

        // Set enable pins low
        foreach (var p in eprom.EnablePins)
          zif[t.ToZIF(p)] = false;

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
          PA.ShoutLine(5, "Address {0} written to ZIF.", address.ToString("X4"));
        else
          throw new U2PaException("Failed to write address {0}. Transferlength: {1} ErrorCode: {2}",
                                   address.ToString("X4"), transferLength, errorCode);

        // Prepare ZIF for reading
        errorCode = Write(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x07 }, 1000, out transferLength);
        if (errorCode == ErrorCode.None && transferLength == 6)
          PA.ShoutLine(5, "ZIF prepared for reading all pins.");
        else
          throw new U2PaException("Failed to prepare ZIF for reading. Transferlength: {0} ErrorCode: {1}",
                                   transferLength, errorCode);

        // Read ZIF
        var readBuffer = new byte[64];
        errorCode = Read(readBuffer, 1000, out transferLength);
        if (errorCode == ErrorCode.None && transferLength == readBuffer.Length)
          PA.ShoutLine(5, "ZIF read for address {0}.", address.ToString("X4"));
        else
          throw new U2PaException("Failed to read ZIF for address {0}. Transferlength: {1} ErrorCode: {2}", address, transferLength, errorCode);

        var readBits = new ZIFSocket(40, readBuffer.Take(5).ToArray());
        var readByte = new BitArray(eprom.DataPins.Length);

        for (int i = 0; i < eprom.DataPins.Length; i++)
          readByte[i] = readBits[t.ToZIF(eprom.DataPins[i])];

        var bytes = readByte.ToBytes().ToArray();
        yield return bytes[0];
        if (eprom.DataPins.Length > 8)
          yield return bytes[1];
      }
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

    public virtual void ApplyVpp(params byte[] zipPins)
    {
      ApplyPropertyToPins("Vpp", 0x14, VppPins, zipPins);
    }

    public virtual void ApplyVcc(params byte[] zifPins)
    {
      ApplyPropertyToPins("Vcc", 0x15, VccPins, zifPins);
    }

    public virtual void ApplyGnd(params byte[] zifPins)
    {
      ApplyPropertyToPins("Gnd", 0x16, GndPins, zifPins);
    }

    protected virtual void ApplyPropertyToPins(string name, byte propCode, ICollection<byte> validPins, params byte[] zifPins)
    {
      // Always start by clearing all pins
      int transferLength;
      var errorCode = UsbEndpointWriter.Write(new byte[] { 0x0e, propCode, 0x00, 0x00 }, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 4)
        PA.ShoutLine(4, "All {0} pins cleared", name);
      else
        throw new U2PaException("Failed to clear {0} pins. Transferlength: {1} ErrorCode: {2}", name, transferLength, errorCode);

      foreach (var zifPin in zifPins)
      {
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
      ApplyVpp();
      ApplyVcc();
      ApplyGnd();
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