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
using System.Linq;
using U2Pa.Lib;

namespace U2Pa.Test
{
  /// <summary>
  /// Test implementation.
  /// </summary>
  public class UsbBulkTestDevice : UsbBulkDeviceDelayer
  {
    public UsbBulkTestDevice() : base(new RawUsbBulkTestDevice())
    {
    }
  }

  public class RawUsbBulkTestDevice : IRawUsbBulkDevice
  {
    int numberOfCallsToSendPackage = 0;
    int numberOfCallsToRecievePackage = 0;
    byte[] idPackage = "top2005+DUMMY...".ToCharArray().Select(c => (byte)c).ToArray();
    public void SendPackage(int verbosity, byte[] data, string description, params object[] args)
    {
      numberOfCallsToSendPackage++;
    }

    public byte[] RecievePackage(int verbosity, string description, params object[] args)
    {
      return numberOfCallsToRecievePackage++ == 0 ? idPackage : new byte[0];
    }

    public void Dispose()
    { }
  }
}
