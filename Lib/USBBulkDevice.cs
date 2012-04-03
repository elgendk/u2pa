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
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace U2Pa.Lib
{
  public class UsbBulkDevice : IDisposable
  {
    public int VendorId { get; private set; }
    public int ProductId { get; private set; }
    private byte Configuration;
    private int Interface;
    private ReadEndpointID ReadEndpointID;
    private WriteEndpointID WriteEndpointID;
    private UsbDevice UsbDevice { get; set; }
    private UsbEndpointReader UsbEndpointReader { get; set; }
    private UsbEndpointWriter UsbEndpointWriter { get; set; }
    private PublicAddress PA { get; set; }

    public UsbBulkDevice(PublicAddress pa, int vendorId, int productId, byte configuration, int nterface,
      ReadEndpointID readEndpointID, WriteEndpointID writeEndpointID)
    {
      PA = pa;
      VendorId = vendorId;
      ProductId = productId;
      Configuration = configuration;
      Interface = nterface;
      ReadEndpointID = readEndpointID;
      WriteEndpointID = writeEndpointID;
      Init();
    }

    private void Init()
    {
      UsbDevice = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(VendorId, ProductId));

      // If the device is open and ready
      if (UsbDevice == null)
        throw new U2PaException(
          "Top Universal Programmer with VendorId: 0x{0} and ProductId: 0x{1} not found.",
          VendorId.ToString("X4"),
          ProductId.ToString("X4"));

      PA.ShoutLine(4,
                   "Top Universal Programmer with VendorId: 0x{0} and ProductId: 0x{1} found.",
                   UsbDevice.UsbRegistryInfo.Vid.ToString("X2"),
                   UsbDevice.UsbRegistryInfo.Pid.ToString("X2"));

      var wholeUsbDevice = UsbDevice as IUsbDevice;
      if (!ReferenceEquals(wholeUsbDevice, null))
      {
        wholeUsbDevice.SetConfiguration(Configuration);
        byte temp;
        if (wholeUsbDevice.GetConfiguration(out temp))
          PA.ShoutLine(4, "Configuration with id: {0} selected.", temp.ToString("X2"));
        else
          throw new U2PaException("Failed to set configuration id: {0}", Configuration);

        if (wholeUsbDevice.ClaimInterface(Interface))
          PA.ShoutLine(4, "Interface with id: {0} claimed.", Interface);
        else
          throw new U2PaException("Failed to claim interface with id: {0}", Interface);
      }

      UsbEndpointReader = UsbDevice.OpenEndpointReader(ReadEndpointID);
      if (UsbEndpointReader == null)
        throw new U2PaException("Unable to open read endpoint ${0}", ReadEndpointID.ToString());
      PA.ShoutLine(4, "Reader endpoint ${0} opened.", UsbEndpointReader.EndpointInfo.Descriptor.EndpointID.ToString("X2"));

      UsbEndpointWriter = UsbDevice.OpenEndpointWriter(WriteEndpointID);
      if (UsbEndpointWriter == null)
        throw new U2PaException("Unable to open write endpoint ${0}", WriteEndpointID.ToString());
      PA.ShoutLine(4, "Writer endpoint ${0} opened.", UsbEndpointWriter.EndpointInfo.Descriptor.EndpointID.ToString("X2"));
    }

    public void SendPackage(int verbosity, byte[] data, string description, params object[] args)
    {
      description = String.Format(description, args);
      int transferLength;
      int timeOut = Math.Max(1000, data.Length / 10);
      var errorCode = Write(data, timeOut, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == data.Length)
        PA.ShoutLine(verbosity, "Write success: {0}. Timeout {1}ms.", description, timeOut);
      else
        throw new U2PaException("Write failure. {0}.\r\nTransferlength: {1} ErrorCode: {2}", description, transferLength, errorCode);
    }

    public byte[] RecievePackage(int verbosity, string description, params object[] args)
    {
      description = String.Format(description, args);
      var readBuffer = new byte[64];
      int transferLength;
      const int timeOut = 1000;
      var errorCode = Read(readBuffer, timeOut, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == readBuffer.Length)
        PA.ShoutLine(verbosity, "Read  success: {0}. Timeout {1}ms.", description, timeOut);
      else
        throw new U2PaException("Read failure: {0}.\r\nTransferlength: {1} ErrorCode: {2}", description, transferLength, errorCode);
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



    public void Dispose()
    {
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
  }
}