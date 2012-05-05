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
using System.Linq;
using LibUsbDotNet;

namespace U2Pa.Lib
{
  public class Top2005Plus : TopDevice
  {

    public Top2005Plus(PublicAddress pa, UsbBulkDevice bulkDevice)
      : base(pa, bulkDevice)
    {
      Func<double, byte, Tuple<double, byte>> t = Tuple.Create<double,byte>;
      // In doubt about pins: 26, 28, 30...
      ValidVccPins = new List<int> {0, 8, 13, 16, 17, 24, 25, 26, 27, 28, 30, 32, 34, 36, 40};
      ValidVppPins = new List<int> {0, 1, 5, 7, 9, 10, 11, 12, 14, 15, 20, 26, 28, 29, 30, 31, 34, 35};
      ValidGndPins = new List<int> {0, 10, 14, 16, 20, 25, 25, 31};
      VppLevels = new List<Tuple<double, byte>> 
      {
        t(4.8, 0x00),
        t(6.9, 0x41), t(7.3, 0x46), t(7.5, 0x4B), t(8.8, 0x50), t(9.0, 0x5A), t(9.5, 0x5F), t(9.9, 0x64), t(10.4, 0x69),
        //t(4.8, 0x6E),
        t(12.0, 0x78), t(12.4, 0x7D), t(12.9, 0x82), t(13.4, 0x87), t(14.0, 0x8C), t(14.5, 0x91), t(15.0, 0x96), t(15.5, 0x9B), t(16.2, 0xA0),
        //t(4.8, 0xAA),
        t(20.9, 0xD2),
        //t(4.8, 0xD3), t(20.9, 0xFA), t(4.8, 0xFB)
      };
      VccLevels = new List<Tuple<double, byte>> 
      {
        //t(3.1, 0x00), t(4.9, 0x01),
        t(3.1, 0x19), t(3.6, 0x1F), t(4.3, 0x28), t(4.9, 0x2D)
      };

      UpLoadBitStreamTopWin6Style(Config.ICTestBinPath);
    }

    public override int ZIFType { get { return 40; } }

    public void UpLoadBitStreamTopWin6Style(string fileName)
    {
      // Prelude of black magic
      BulkDevice.SendPackage(5, new byte[] { 0x0A, 0x1B, 0x00 }, "???");
      BulkDevice.SendPackage(5, new byte[] { 0x0E, 0x21, 0x00, 0x00 }, "Start bitstream upload");
      BulkDevice.SendPackage(5, new byte[] { 0x07 }, "Some kind of finish-up/execute command?");
      BulkDevice.RecievePackage(5, "Some values that maby should be validated in some way");

      var fpgaProgram = new FPGAProgram(fileName);
      var bytesToSend = PackFPGABytes(fpgaProgram.Payload).SelectMany(x => x).ToArray();
      PA.ShoutLine(5, fpgaProgram.ToString());
      BulkDevice.SendPackage(5, bytesToSend, "Uploading file: {0}", fileName);

      // Postlude of black magic
      BulkDevice.SendPackage(5, new byte[] { 0x0E, 0x12, 0x00, 0x00 }, "Set Vpp boost off");
      BulkDevice.SendPackage(5, new byte[] { 0x1B }, "???");
      // Why 2 times???
      BulkDevice.SendPackage(5, new byte[] { 0x0E, 0x12, 0x00, 0x00 }, "Set Vpp boost off");
      BulkDevice.SendPackage(5, new byte[] { 0x1B }, "???");
      BulkDevice.SendPackage(5, new byte[] { 0x0E, 0x13, 0x32, 0x00 }, "Set Vcc = 5V");
      BulkDevice.SendPackage(5, new byte[] { 0x1B }, "???");
      BulkDevice.SendPackage(5, new byte[] { 0x0E, 0x15, 0x00, 0x00 }, "Clear all Vcc assignments");
      // Why no 0x1B here???
      BulkDevice.SendPackage(5, new byte[] { 0x0E, 0x17, 0x00, 0x00 }, "Clear all ??? assignments");
      // Why no 0x1B here???
      BulkDevice.SendPackage(5, new byte[] { 0x0A, 0x1D, 0x86 }, "???");
      BulkDevice.SendPackage(5, new byte[] { 0x0E, 0x16, 0x00, 0x00 }, "Clear all Gnd assignments");
      var clueless = new byte[]
                       {
                         0x3E, 0x00, 0x3E, 0x01, 0x3E, 0x02, 0x3E, 0x03, 0x3E, 0x04, 0x3E, 0x05, 0x3E, 0x06, 0x3E, 0x07,
                         0x3E, 0x08, 0x3E, 0x09, 0x3E, 0x0A, 0x3E, 0x0B, 0x3E, 0x0C, 0x3E, 0x0D, 0x3E, 0x0E, 0x3E, 0x0F,
                         0x3E, 0x10, 0x3E, 0x11, 0x3E, 0x12, 0x3E, 0x13, 0x3E, 0x14, 0x3E, 0x15, 0x3E, 0x16, 0x3E, 0x17,
                         0x07
                       };
      BulkDevice.SendPackage(5, clueless, "I´m clueless on this one atm");
      BulkDevice.RecievePackage(5, "Properly answer to clueless that maby should be validated in some way");
      // Again?
      BulkDevice.SendPackage(5, new byte[] { 0x0E, 0x12, 0x00, 0x00 }, "Set Vpp boost off");
      BulkDevice.SendPackage(5, new byte[] { 0x1B }, "???");
      // Again, again???
      BulkDevice.SendPackage(5, new byte[] { 0x0E, 0x12, 0x00, 0x00 }, "Set Vpp boost off");
    }
  }
}