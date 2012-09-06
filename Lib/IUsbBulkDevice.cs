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

namespace U2Pa.Lib
{
  /// <summary>
  /// Extends <see cref="IRawUsbBulkDevice"/> with delay of commands.
  /// </summary>
  public interface IUsbBulkDevice : IRawUsbBulkDevice
  {
    /// <summary>
    /// Used to delay before the next command is send.
    /// </summary>
    /// <param name="milliseconds">Delay in ms.</param>
    void Delay(int milliseconds);
  }

  /// <summary>
  /// A wrapper interface for low level commmunication with the Top Programmer.
  /// </summary>
  public interface IRawUsbBulkDevice : IDisposable
  {
    /// <summary>
    /// Sends a data package to the Top Programmer.
    /// </summary>
    /// <param name="verbosity">The verbosity to use when displaying messages.</param>
    /// <param name="data">The data to send.</param>
    /// <param name="description">The description to use when displaying messages.</param>
    /// <param name="args">Arguments for the description string.</param>
    void SendPackage(int verbosity, byte[] data, string description, params object[] args);

    /// <summary>
    /// Reads a data package from the Top Programmer.
    /// </summary>
    /// <param name="verbosity">The verbosity to use when displaying messages.</param>
    /// <param name="description">The description to use when displaying messages.</param>
    /// <param name="args">Arguments for the description string.</param>
    /// <returns>The read data.</returns>
    byte[] RecievePackage(int verbosity, string description, params object[] args);
  }
}
