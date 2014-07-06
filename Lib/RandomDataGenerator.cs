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
using System.Linq;
using System;

namespace U2Pa.Lib
{
  /// <summary>
  /// Generates random data in a consistent way such that
  /// consecutive calls to the same address returns same data.
  /// </summary>
  public class RandomDataGenerator : IDataGenerator
  {
    private BitArray[] data;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="dataLength">The width of the data bus.</param>
    /// <param name="totalNumberOfAdresses">Total number of addresses.</param>
    public RandomDataGenerator(int dataLength, int totalNumberOfAdresses)
    {
      Random rnd = new Random();
      data = Enumerable
        .Repeat(0, totalNumberOfAdresses)
        .Select(x => RandomBitArray(rnd, dataLength))
        .ToArray();      
    }

    private BitArray RandomBitArray(Random rnd, int dataLength)
    {
      var bitArray = new BitArray(dataLength);
      for (var i = 0; i < bitArray.Length; i++)
        bitArray[i] = rnd.Next() % 2 == 0;
      return bitArray;
    }

    /// <summary>
    /// Gets data for the specified address.
    /// </summary>
    /// <param name="address">Address.</param>
    /// <returns>Data.</returns>
    public BitArray GetData(int address)
    {
      return data[address];
    }
  }
}
