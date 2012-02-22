using System;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;

namespace U2Pa.Lib
{
  public class Top2005Plus : TopDevice
  {

    public Top2005Plus(PublicAddress pa, UsbDevice usbDevice, UsbEndpointReader usbEndpointReader, UsbEndpointWriter usbEndpointWriter)
      : base(pa, usbDevice, usbEndpointReader, usbEndpointWriter)
    {
      VccPins = new List<byte> {8, 13, 17, 24, 25, 26, 27, 28, 30, 32, 34, 36, 40};
      VppPins = new List<byte> {1, 5, 7, 9, 10, 11, 12, 14, 15, 20, 26, 28, 29, 30, 31, 34, 40};
      GndPins = new List<byte> {10, 14, 16, 20, 25, 31};

      UpLoadBitStreamTopWin6Style(@"C:\Top\Topwin6\Blib2\ictest.bit");
    }

    public void UpLoadBitStreamTopWin6Style(string fileName)
    {
      // Prelude of black magic
      SendRawPackage(5, new byte[] { 0x0A, 0x1B, 0x00 }, "???");
      SendRawPackage(5, new byte[] { 0x0E, 0x21, 0x00, 0x00 }, "Start bitstream upload");
      SendRawPackage(5, new byte[] { 0x07 }, "Some kind of finish up/execute command");
      RecieveRawPackage(5, "Some values that maby should be validated in some way");

      var bytes = Tools.ReadBinaryFile(fileName).ToList();
      var range = CheckBitStreamHeader(bytes);
      bytes.RemoveRange(0, range);

      var bytesToSend = PackBytes(bytes).SelectMany(x => x).ToArray();
      SendRawPackage(5, bytesToSend, String.Format("Uploading file: {0}", fileName));

      // Postlude of black magic
      SendRawPackage(5, new byte[] { 0x0E, 0x12, 0x00, 0x00 }, "Set Vpp boost off");
      SendRawPackage(5, new byte[] { 0x1B }, "???");
      // Why 2 times???
      SendRawPackage(5, new byte[] { 0x0E, 0x12, 0x00, 0x00 }, "Set Vpp boost off");
      SendRawPackage(5, new byte[] { 0x1B }, "???");
      SendRawPackage(5, new byte[] { 0x0E, 0x13, 0x32, 0x00 }, "Set Vcc = 5V");
      SendRawPackage(5, new byte[] { 0x1B }, "???");
      SendRawPackage(5, new byte[] { 0x0E, 0x15, 0x00, 0x00 }, "Clear all Vcc assignments");
      // Why no 0x1B here???
      SendRawPackage(5, new byte[] { 0x0E, 0x17, 0x00, 0x00 }, "Clear all ??? assignments");
      // Why no 0x1B here???
      SendRawPackage(5, new byte[] { 0x0A, 0x1D, 0x86 }, "???");
      SendRawPackage(5, new byte[] { 0x0E, 0x16, 0x00, 0x00 }, "Clear all Gnd assignments");
      var clueless = new byte[]
                       {
                         0x3E, 0x00, 0x3E, 0x01, 0x3E, 0x02, 0x3E, 0x03, 0x3E, 0x04, 0x3E, 0x05, 0x3E, 0x06, 0x3E, 0x07,
                         0x3E, 0x08, 0x3E, 0x09, 0x3E, 0x0A, 0x3E, 0x0B, 0x3E, 0x0C, 0x3E, 0x0D, 0x3E, 0x0E, 0x3E, 0x0F,
                         0x3E, 0x10, 0x3E, 0x11, 0x3E, 0x12, 0x3E, 0x13, 0x3E, 0x14, 0x3E, 0x15, 0x3E, 0x16, 0x3E, 0x17,
                         0x07
                       };
      SendRawPackage(5, clueless, "I´m clueless on this one atm");
      RecieveRawPackage(5, "Properly answer to clueless that maby should be validated in some way");
      // Again?
      SendRawPackage(5, new byte[] { 0x0E, 0x12, 0x00, 0x00 }, "Set Vpp boost off");
      SendRawPackage(5, new byte[] { 0x1B }, "???");
      // Again, again???
      SendRawPackage(5, new byte[] { 0x0E, 0x12, 0x00, 0x00 }, "Set Vpp boost off");
    }
  }
}