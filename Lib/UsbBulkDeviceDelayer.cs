﻿//                             u2pa
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
using System.Diagnostics;

namespace U2Pa.Lib
{
  /// <summary>
  /// Extends an <see cref="IRawUsbBulkDevice"/> with delay of commands.
  /// </summary>
  public class UsbBulkDeviceDelayer : IUsbBulkDevice
  {
    private int currentDelay = 0;
    private Stopwatch stopWatch = new Stopwatch();
    private IRawUsbBulkDevice bulkDevice;

    public UsbBulkDeviceDelayer(IRawUsbBulkDevice bulkDevice)
    {
      this.bulkDevice = bulkDevice;
      this.stopWatch.Start();
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
      while (stopWatch.ElapsedMilliseconds < currentDelay)
      {
        // Wait };-P
      }
      currentDelay = 0;
      stopWatch.Restart();
    }

    public void SendPackage(int verbosity, byte[] data, string description, params object[] args)
    {
      DoWait();
      bulkDevice.SendPackage(verbosity, data, description, args);
    }

    public byte[] RecievePackage(int verbosity, string description, params object[] args)
    {
      DoWait();
      return bulkDevice.RecievePackage(verbosity, description, args);
    }

    public void Dispose()
    {
      DoWait();
      bulkDevice.Dispose();
    }
  }
}
