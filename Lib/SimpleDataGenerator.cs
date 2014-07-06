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

using System.Collections;

namespace U2Pa.Lib
{
  /// <summary>
  /// Generates random data in a consistent way such that
  /// consecutive calls to the same address returns same data.
  /// If first bit is 0, the data at address 0 will be ...1010,
  /// the next ...0101 and so on; if the first bit is 1, address 0
  /// will have ...0101 and address 1 ...1010.
  /// </summary>
  public class SimpleDataGenerator : IDataGenerator
  {
    private readonly BitArray evenAddresses;
    private readonly BitArray unEvenAddresses;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="dataLength">The width of the data bus.</param>
    /// <pparam name="firstBit">The first bit in the data at address 0.</pparam>
    public SimpleDataGenerator(int dataLength, bool firstBit)
    {
      evenAddresses = new BitArray(dataLength);
      unEvenAddresses = new BitArray(dataLength);
      var nextBit = firstBit;
      for (var i = 0; i < dataLength; i++)
      {
        evenAddresses[i] = nextBit;
        unEvenAddresses[i] = !nextBit;
        nextBit = !nextBit;
      }
    }

    /// <summary>
    /// Gets data for the specified address.
    /// </summary>
    /// <param name="address">Address.</param>
    /// <returns>Data.</returns>
    public BitArray GetData(int address)
    {
      return address % 2 == 0 ? evenAddresses : unEvenAddresses;
    }
  }
}
