using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace U2Pa
{
  public static class Core
  {
    public static int VerbosityLevel = 5;
    private static UsbDevice U2PaDevice;
    private static UsbEndpointReader U2PaReader;
    private static UsbEndpointWriter U2PaWriter;
    private static List<byte> VccPins = new List<byte> { 8, 13, 17, 24, 25, 26, 27, 28, 30, 32, 34, 36, 40 };
    private static List<byte> VppPins = new List<byte> { 1, 5, 7, 9, 10, 11, 12, 14, 15, 20, 26, 28, 29, 30, 31, 34, 40 };
    private static List<byte> GndPins = new List<byte> { 10, 14, 16, 20, 25, 31 };
    private const int VendorId = 0x2471;
    private const int ProductId = 0x0853;
    private const string BitStreamFileName = @"C:\Top\Topwin6\Blib3\ictest.bit";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="verbosity"></param>
    /// <param name="message"></param>
    /// <param name="obj"></param>
    public static void ShoutLine(int verbosity, string message, params object[] obj)
    {
      if (verbosity <= VerbosityLevel)
        Console.WriteLine((VerbosityLevel == 5 ? "V" + verbosity + ": " : "") + message, obj);
    }

    /// <summary>
    /// 
    /// </summary>
    public static void Init()
    {
      U2PaDevice = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(VendorId, ProductId));

      // If the device is open and ready
      if (U2PaDevice == null)
        throw new U2PaException(
          "Top2005+ Programmer with VendorId: 0x{0} and ProductId: 0x{1} not found.",
          VendorId.ToString("X4"),
          ProductId.ToString("X4"));

      ShoutLine(4,
        "Top2005+ Programmer with VendorId: 0x{0} and ProductId: 0x{1} found.",
        U2PaDevice.UsbRegistryInfo.Vid.ToString("X2"),
        U2PaDevice.UsbRegistryInfo.Pid.ToString("X2"));

      var wholeUsbDevice = U2PaDevice as IUsbDevice;
      if (!ReferenceEquals(wholeUsbDevice, null))
      {
        // Select config #1
        wholeUsbDevice.SetConfiguration(1);
        byte temp;
        if (wholeUsbDevice.GetConfiguration(out temp))
          ShoutLine(4, "Configuration with id: {0} selected.", temp.ToString("X2"));
        else
          throw new U2PaException("Failed to set configuration id: {0}", 1);

        // Claim interface #0.
        if (wholeUsbDevice.ClaimInterface(0))
          ShoutLine(4, "Interface with id: {0} claimed.", 0);
        else
          throw new U2PaException("Failed to claim interface with id: {0}", 1);
      }

      // Open read endpoint $82 aka ReadEndPoint.Ep02.
      U2PaReader = U2PaDevice.OpenEndpointReader(ReadEndpointID.Ep02);
      if (U2PaReader == null)
        throw new U2PaException("Unable to open read endpoint ${0}", "82");
      ShoutLine(4, "Reader endpoint ${0} opened.", U2PaReader.EndpointInfo.Descriptor.EndpointID.ToString("X2"));


      // Open write endpoint $02 aka WriteEndPoint.Ep02
      U2PaWriter = U2PaDevice.OpenEndpointWriter(WriteEndpointID.Ep02);
      if (U2PaWriter == null)
        throw new U2PaException("Unable to open write endpoint ${0}", "02");
      ShoutLine(4, "Writer endpoint ${0} opened.", U2PaWriter.EndpointInfo.Descriptor.EndpointID.ToString("X2"));

      UploadBitStream(BitStreamFileName);
    }

    /// <summary>
    /// 
    /// </summary>
    public static void Close()
    {
      ApplyVpp();
      ApplyVcc();
      ApplyGnd();
      if (U2PaDevice == null) return;
      if (U2PaDevice.IsOpen)
      {
        var wholeUsbDevice = U2PaDevice as IUsbDevice;
        if (!ReferenceEquals(wholeUsbDevice, null))
        {
          // Release interface #0.
          wholeUsbDevice.ReleaseInterface(0);
          ShoutLine(4, "Interface released.");
        }

        U2PaDevice.Close();
        ShoutLine(4, "Device closed.");
      }
      U2PaDevice = null;

      // Free usb resources
      UsbDevice.Exit();
      ShoutLine(4, "USB resources freed.");
    }

    private static IEnumerable<byte> ReadBinaryFile(string fileName)
    {
      var buffer = new byte[1];
      var bytes = new List<byte>();
      // Open file and read it in
      using (var fs = new FileStream(BitStreamFileName, FileMode.Open, FileAccess.Read))
      {
        using (var br = new BinaryReader(fs))
        {
          while (0 != br.Read(buffer, 0, 1))
            bytes.Add(buffer[0]);
        }
      }
      var range = CheckBitStreamHeader(bytes);
      bytes.RemoveRange(0, range);
      return bytes;
    }

    private static int CheckBitStreamHeader(IList<byte> bytes)
    {
      var startIndex = 0;
      for (var i = 0; i < bytes.Count; i++)
      {
        // Find position of 2nd 'e' in the stream...only works for ictest.bin
        if (bytes[i] == 0x65 && i > 19)
        {
          startIndex = i;
          break;
        }
      }
      if (startIndex == 0 || startIndex == bytes.Count)
        throw new U2PaException("Header check of bitstream failed!");
      return startIndex + 1 + 4;

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

    public static void UploadBitStream(string fileName)
    {
      // Send "Start Bitstream upload" packet
      var startPackage = new byte[] { 0x0e, 0x21, 0x00, 0x00 };
      int transferLength;
      var errorCode = U2PaWriter.Write(startPackage, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == startPackage.Length)
        ShoutLine(4, "Bitstream upload successfully initiated.");
      else
        throw new U2PaException("Failed to initiate bitstream upload. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);

      // Bitstream upload loop
      var bytes = ReadBinaryFile(fileName);
      var succeededUploads = 0;
      foreach(var package in PackBytes(bytes))
      {
        errorCode = U2PaWriter.Write(package, 1000, out transferLength);
        if (errorCode == ErrorCode.None && transferLength == 64)
          ShoutLine(5, "{0} datapackages successfully uploaded.", ++succeededUploads);
        else
          throw new U2PaException("Datapackage #{0} failed to upload. Transferlength: {0} ErrorCode: {1}", succeededUploads + 1, transferLength, errorCode);
      }

      // Send "End Bitstream upload" packet
      var endPackage = new byte[] { 0x0e, 0x23, 0x00, 0x00 };
      errorCode = U2PaWriter.Write(startPackage, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == endPackage.Length)
        ShoutLine(4, "Bitstream upload successfully ended");
      else
        throw new U2PaException("Failed to end bitstream upload. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);

      // Get reply
      errorCode = U2PaWriter.Write(new byte[] { 0x07 }, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 1)
        ShoutLine(4, "Reply recieved.");
      else
        throw new U2PaException("Fail to get reply. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);
    
      // Get response - again, i dunno if it's necessary
      var responseBuffer = new byte[64];
      errorCode = U2PaReader.Read(responseBuffer, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == responseBuffer.Length)
        ShoutLine(4, "Response recieved.");
      else
        throw new U2PaException("Fail to get response. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);

      if (responseBuffer[0] !=  0x01)
        throw new U2PaException("Bitstream upload failed with TOP response: {0}", responseBuffer[0].ToString("X2"));
    }

    private static IEnumerable<byte[]> PackBytes(IEnumerable<byte> bytes)
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

    public static byte[] Read2716()
    {
      ShoutLine(4, "Reading 2716.");
      // Setting up chip...
      SetVccLevel(VccLevel.Vcc_5_0v);
      SetVppLevel(VppLevel.Vpp_Off);
      ApplyVcc(32);
      ApplyGnd(20);

      var addressPins = new[] {8, 7, 6, 5, 4, 3, 2, 1, 23, 22, 19};
      var dataPins = new[] {9, 10, 11, 13, 14, 15, 16, 17};
      var enablePins = new[] {18, 20};

      var t = new PinNumberTranslator(24, 0);

      var zif = new BitArray(41);
      for (int address = 0; address < 2.Pow(addressPins.Length); address++)
      {
        // Reset ZIF Vector to all 1's
        zif.SetAll(true);

        // Set adress pins
        var bitAddress = new BitArray(new[] {address});
        for (var i = 0; i < addressPins.Length; i++)
          zif[t.ToZIF(addressPins[i])] = bitAddress[i];

        // Set enable pins low
        foreach (var p in enablePins)
          zif[t.ToZIF(p)] = false;

        int transferLength;
        var package = zif.Spew2PBytes();
        //var errorCode = U2PaWriter.Write(new byte[]{0x10, 0x00, 0x11, 0x00, 0x12, 0x00, 0x13, 0x00, 0x14, 0x00, 0x0A, 0x15, 0xFF}, 1000, out transferLength);
        var errorCode = U2PaWriter.Write(package, 1000, out transferLength);
        if (errorCode == ErrorCode.None && transferLength == package.Length)
          ShoutLine(5, "Address {0} written to ZIF.", address.ToString("X4"));
        else
          throw new U2PaException("Failed to write address {0}. Transferlength: {1} ErrorCode: {2}",
                                   address.ToString("X4"), transferLength, errorCode);

        // Prepare ZIF for reading
        errorCode = U2PaWriter.Write(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x07 }, 1000, out transferLength);
        if (errorCode == ErrorCode.None && transferLength == 6)
          ShoutLine(5, "ZIF prepared for reading all pins.");
        else
          throw new U2PaException("Failed to prepare ZIF for reading. Transferlength: {0} ErrorCode: {1}",
                                   transferLength, errorCode);

        // Read ZIF
        var readBuffer = new byte[64];
        errorCode = U2PaReader.Read(readBuffer, 1000, out transferLength);
        if (errorCode == ErrorCode.None && transferLength == readBuffer.Length)
          ShoutLine(5, "ZIF read for address {0}.", address.ToString("X4"));
        else
          throw new U2PaException("Failed to read ZIF for address {0}. Transferlength: {1} ErrorCode: {2}", address, transferLength, errorCode);

        var readBytes = "";
        for (int i = 0; i < readBuffer.Length; i++)
          readBytes += readBuffer[i].ToString("X2") + " ";
        ShoutLine(5, readBytes);
      }
      return null;
    }

    public static void SetVppLevel(VppLevel level)
    {
      int transferLength;
      var errorCode = U2PaWriter.Write(new byte[] { 0x0e, 0x12, (byte)level, 0x00 }, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 4)
        ShoutLine(4, "Vpp = {0}", level.ToString().Substring(4).Replace('_', '.'));
      else
        throw new U2PaException("Failed to set Vpp. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);
    }

    public static void SetVccLevel(VccLevel level)
    {
      int transferLength;
      var errorCode = U2PaWriter.Write(new byte[] {0x0e, 0x13, (byte) level, 0x00}, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 4)
        ShoutLine(4, "Vcc = {0}", level.ToString().Substring(4).Replace('_', '.'));
      else
        throw new U2PaException("Failed to set Vcc. Transferlength: {0} ErrorCode: {1}", transferLength, errorCode);
    }

    public static void ApplyVpp(params byte[] zipPins)
    {
      ApplyPropertyToPins("Vpp", 0x14, VppPins, zipPins);
    }

    public static void ApplyVcc(params byte[] zifPins)
    {
      ApplyPropertyToPins("Vcc", 0x15, VccPins, zifPins);
    }

    public static void ApplyGnd(params byte[] zifPins)
    {
      ApplyPropertyToPins("Gnd", 0x16, GndPins, zifPins);
    }

    private static void ApplyPropertyToPins(string name, byte propCode, List<byte> validPins, params byte[] zifPins)
    {
      // Always start by clearing all Vcc pins
      int transferLength;
      var errorCode = U2PaWriter.Write(new byte[] { 0x0e, propCode, 0x00, 0x00 }, 1000, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == 4)
        ShoutLine(4, "All {0} pins cleared", name);
      else
        throw new U2PaException("Failed to clear {0} pins. Transferlength: {1} ErrorCode: {2}", name, transferLength, errorCode);
    
      foreach (var zifPin in zifPins)
      {
        if (!validPins.Contains(zifPin))
          throw new U2PaException("Pin {0} is not a valid {1} pin.", zifPin, name);
        errorCode = U2PaWriter.Write(new byte[] { 0x0e, propCode, zifPin, 0x00 }, 1000, out transferLength);
        if (errorCode == ErrorCode.None && transferLength == 4)
          ShoutLine(4, "{0} applied to pin {1}", name, zifPin);
        else
          throw new U2PaException("Failed to apply {0} to pin {1}. Transferlength: {2} ErrorCode: {3}", name, zifPin, transferLength, errorCode);
        
      }
    }


  }
}
