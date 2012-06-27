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
using System.Diagnostics;

namespace U2Pa.Lib
{
  /// <summary>
  /// A wrapper class for the key methods in LibUsbDotNet we use.
  /// </summary>
  public class UsbBulkDevice : IUsbBulkDevice, IDisposable
  {
    private int VendorId;
    private int ProductId;
    private byte Configuration;
    private int Interface;
    private ReadEndpointID ReadEndpointID;
    private WriteEndpointID WriteEndpointID;
    private UsbDevice UsbDevice;
    private UsbEndpointReader UsbEndpointReader;
    private UsbEndpointWriter UsbEndpointWriter;
    private IShouter Shouter;
    private int currentDelay = 0;
    private Stopwatch stopWatch = new Stopwatch();

    /// <summary>
    /// ctor.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="vendorId">The vendor id.</param>
    /// <param name="productId">The product id.</param>
    /// <param name="configuration">The configuration id.</param>
    /// <param name="@interface">The interface id.</param>
    /// <param name="readEndpointInt">The read endpoint id.</param>
    /// <param name="writeEndpointInt">The write end point id.</param>
    public UsbBulkDevice(
      IShouter shouter, 
      int vendorId, 
      int productId, 
      byte configuration, 
      int @interface,
      int readEndpointInt, 
      int writeEndpointInt)
    {
      Shouter = shouter;
      VendorId = vendorId;
      ProductId = productId;
      Configuration = configuration;
      Interface = @interface;
      ReadEndpointID = (ReadEndpointID)readEndpointInt;
      WriteEndpointID = (WriteEndpointID)writeEndpointInt;
      Init();
    }

    /// <summary>
    /// Initiates the instance.
    /// <remarks>Greatly inspired by the examples in the LibUsbDotNet documentation.</remarks>
    /// </summary>
    private void Init()
    {
      UsbDevice = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(VendorId, ProductId));
	  
      if (UsbDevice == null)
        throw new U2PaException(
          "Top Universal Programmer with VendorId: 0x{0} and ProductId: 0x{1} not found.",
          VendorId.ToString("X4"),
          ProductId.ToString("X4"));

      Shouter.ShoutLine(4,
        "Top Universal Programmer with VendorId: 0x{0} and ProductId: 0x{1} found.",
        VendorId.ToString("X4"),
        ProductId.ToString("X4"));

      var wholeUsbDevice = UsbDevice as IUsbDevice;
      if (!ReferenceEquals(wholeUsbDevice, null))
      {
        wholeUsbDevice.SetConfiguration(Configuration);
        byte temp;
        if (wholeUsbDevice.GetConfiguration(out temp))
          Shouter.ShoutLine(4, "Configuration with id: {0} selected.", temp.ToString("X2"));
        else
          throw new U2PaException("Failed to set configuration id: {0}", Configuration);

        if (wholeUsbDevice.ClaimInterface(Interface))
          Shouter.ShoutLine(4, "Interface with id: {0} claimed.", Interface);
        else
          throw new U2PaException("Failed to claim interface with id: {0}", Interface);
      }
			
      UsbEndpointReader = UsbDevice.OpenEndpointReader(ReadEndpointID);
      if (UsbEndpointReader == null)
        throw new U2PaException("Unable to open read endpoint ${0}", ReadEndpointID.ToString());
      Shouter.ShoutLine(4, "Reader endpoint ${0} opened.", UsbEndpointReader.EndpointInfo.Descriptor.EndpointID.ToString("X2"));

      UsbEndpointWriter = UsbDevice.OpenEndpointWriter(WriteEndpointID);
      if (UsbEndpointWriter == null)
        throw new U2PaException("Unable to open write endpoint ${0}", WriteEndpointID.ToString());
      Shouter.ShoutLine(4, "Writer endpoint ${0} opened.", UsbEndpointWriter.EndpointInfo.Descriptor.EndpointID.ToString("X2"));
      stopWatch.Start();
    }

    /// <summary>
    /// Used to delay before the next command is send.
    /// </summary>
    /// <param name="milliseconds">Delay in ms.</param>
    public void Delay(int milliseconds)
    {
      currentDelay += milliseconds;
    }

    /// <summary>
    /// Waits until the delay runs out and resets it.
    /// </summary>
    private void DoWait()
    {
      while (stopWatch.ElapsedMilliseconds <= currentDelay)
      {
        // Wait };-P
      }
      currentDelay = 0;
      stopWatch.Restart();
    }

    /// <summary>
    /// Sends a data package to the Top Programmer.
    /// </summary>
    /// <param name="verbosity">The verbosity to use when displaying messages.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="description">The description to use when displaying messages.</param>
    /// <param name="args">Arguments for the description string.</param>
    public void SendPackage(int verbosity, byte[] data, string description, params object[] args)
    {
      description = String.Format(description, args);
      int transferLength;
      int timeOut = Math.Max(10000, data.Length / 10);
      DoWait();
      var errorCode = UsbEndpointWriter.Write(data, timeOut, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == data.Length)
        Shouter.ShoutLine(verbosity, "Write success: {0}. Timeout {1}ms.", description, timeOut);
      else
        throw new U2PaException("Write failure. {0}.\r\nTransferlength: {1} ErrorCode: {2}", description, transferLength, errorCode);
    }

    /// <summary>
    /// Reads a data package from the device.
    /// </summary>
    /// <param name="verbosity">The verbosity to use when displaying messages.</param>
    /// <param name="description">The description to use when displaying messages.</param>
    /// <param name="args">Arguments for the description string.</param>
    /// <returns>The read data.</returns>
    public byte[] RecievePackage(int verbosity, string description, params object[] args)
    {
      description = String.Format(description, args);
      var readBuffer = new byte[64];
      int transferLength;
      const int timeOut = 10000;
      DoWait();
      var errorCode = UsbEndpointReader.Read(readBuffer, timeOut, out transferLength);
      if (errorCode == ErrorCode.None && transferLength == readBuffer.Length)
        Shouter.ShoutLine(verbosity, "Read  success: {0}. Timeout {1}ms.", description, timeOut);
      else
        throw new U2PaException("Read failure: {0}.\r\nTransferlength: {1} ErrorCode: {2}", description, transferLength, errorCode);
      return readBuffer;
    }

    /// <summary>
    /// Disposes any unmanaged resources.
    /// <remarks>Greatly inspired by the examples in the LibUsbDotNet documentation.</remarks>
    /// </summary>
    public void Dispose()
    {
      DoWait();
      UsbEndpointReader.Flush();
      UsbEndpointReader.Reset();
      UsbEndpointReader.Dispose();
      UsbEndpointWriter.Flush();
      UsbEndpointWriter.Reset();
      UsbEndpointWriter.Dispose();
			
      if (UsbDevice == null) return;
      if (UsbDevice.IsOpen)
      {
        var wholeUsbDevice = UsbDevice as IUsbDevice;
        if (!ReferenceEquals(wholeUsbDevice, null))
        {
          // Release interface #0.
          wholeUsbDevice.ReleaseInterface(0);
          Shouter.ShoutLine(4, "Interface released.");
        }

        UsbDevice.Close();
        Shouter.ShoutLine(4, "Device closed.");
      }
      UsbDevice = null;

      // Free usb resources
      UsbDevice.Exit();
	  
      Shouter.ShoutLine(4, "USB resources freed.");
    }
  }
}
