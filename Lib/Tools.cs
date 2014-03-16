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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using U2Pa.Lib.IC;

namespace U2Pa.Lib
{
  /// <summary>
  /// A collection of various tools.
  /// </summary>
  public static class Tools
  {
    /// <summary>
    /// Read a binary file.
    /// </summary>
    /// <param name="fileName">The fully qualified filename.</param>
    /// <returns>The sequence of bytes representing the file.</returns>
    public static IEnumerable<byte> ReadBinaryFile(string fileName)
    {
      var buffer = new byte[1];
      using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
      {
        using (var br = new BinaryReader(fs))
        {
          while (0 != br.Read(buffer, 0, 1))
            yield return buffer[0];
        }
      }
    }

    /// <summary>
    /// Writes a binary file.
    /// </summary>
    /// <param name="fileName">The fully qualified filename.</param>
    /// <param name="data">The sequence of data to be written.</param>
    public static void WriteBinaryFile(string fileName, IEnumerable<byte> data)
    {
      using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
      {
        using (var bw = new BinaryWriter(fs))
        {
          bw.Write(data.ToArray());
          bw.Flush();
        }
      }
    }

    /// <summary>
    /// Analyzes the result of a ZIF-socket read.
    /// </summary>
    /// <param name="results">The array of results.</param>
    /// <param name="eprom">The eprom.</param>
    /// <param name="address">The address.</param>
    /// <param name="result">
    /// If <see cref="ReadSoundness.SeemsToBeAOkay"/> is returned, 
    /// this result can be trusted.
    /// </param>
    /// <returns>The result of the analyzis.</returns>
    public static ReadSoundness AnalyzeEpromReadSoundness(
      ZIFSocket[] results,
      Eprom eprom,
      int address,
      out ZIFSocket result)
    {
      result = null;
      var refLenght = results.Length;
      var revResults = results.Reverse().ToArray();

      var withCorrectAddresses = revResults.TakeWhile(x => eprom.GetAddress(x) == address).ToArray();
      if (withCorrectAddresses.Length == 0)
        return ReadSoundness.TryRewrite;
      if (withCorrectAddresses.Length != refLenght)
        return ReadSoundness.TryReread;

      var refData = eprom.GetData(revResults[0]);
      var withRefData = revResults.TakeWhile(x => eprom.GetData(x).SequenceEqual(refData)).ToArray();
      if (withRefData.Length != refLenght)
        return ReadSoundness.TryReread;

      result = revResults[0];
      return ReadSoundness.SeemsToBeAOkay;
    }

    /// <summary>
    /// Determines wether a byte can be patched.
    /// </summary>
    /// <param name="byteFromEprom">The byte read from the EPROM.</param>
    /// <param name="byteFromFile">The corresponding byte from the file.</param>
    /// <returns>The result.</returns>
    public static bool CanBePatched(byte byteFromEprom, byte byteFromFile)
    {
      // I know there's a smarter way to do this, but I'm a bit too tired atm.
      var eBits = new BitArray(new[] { byteFromEprom });
      var fBits = new BitArray(new[] { byteFromFile });

      var returnValue = true;
      for (var i = 0; i < 8; i++)
      {
        returnValue &= eBits[i] || (!eBits[i] && !fBits[i]);
      }
      return returnValue;
    }

    /// <summary>
    /// Pads a sting with spaces in both ends until <paramref name="maxLength"/> is reached.
    /// </summary>
    /// <param name="src">The string to be padded.</param>
    /// <param name="maxLength">The maximum length of the resulting string.</param>
    /// <returns>The padded string.</returns>
    /// <remarks>
    /// If <paramref name="maxLength"/> is larger than the length of <paramref name="src"/>,
    /// no padding is done <paramref name="src"/> is returned unchanged.
    /// </remarks>
    public static string Pad(this string src, int maxLength)
    {
      while(src.Length < maxLength)
      {
        src += " ";
        if (src.Length == maxLength)
          return src;
        src = " " + src;
      }
      return src;
    }

    /// <summary>
    /// Converts a <see cref="BitArray"/> to bytes.
    /// </summary>
    /// <param name="bits">The <see cref="BitArray"/> to be converted.</param>
    /// <returns>The resulting bytes.</returns>
    public static IEnumerable<byte> ToBytes(this BitArray bits, int biteSize = 8)
    {
      if (bits.Count % biteSize != 0)
        throw new U2PaException(String.Format("bits.Count % {0} != 0", biteSize));

      byte acc = 0x00;
      var mainIndex = 0;
      var accIndex = 0;
      while (true)
      {
        if (accIndex == biteSize)
        {
          yield return acc;
          acc = 0x00;
          accIndex = 0;
        }
        if (mainIndex == bits.Count)
          yield break;

        if (bits[mainIndex]) acc |= (byte)(0x01 << accIndex);
        mainIndex++;
        accIndex++;
      }
    }

    /// <summary>
    /// Parses an <see cref="XElement"/> into an array of <see cref="Pin"/>s.
    /// </summary>
    /// <param name="x">The <see cref="XElement"/> to be parsed.</param>
    /// <param name="name">The name of the subelement.</param>
    /// <returns>The parsed array of <see cref="Pin"/>s.</returns>
    internal static Pin[] ToPinArray(this XElement x, string name)
    {
      if (x.Element(name) == null) return new Pin[0];
      return x.Element(name).Value.Split(',').Where(y => !String.IsNullOrEmpty(y)).Select(Pin.Parse).ToArray();
    }

    internal static VectorValues ParseVectorValue(char letter)
    {
      switch (letter)
      {
        case '0':
         return VectorValues.Zero;
        case '1':
         return VectorValues.One;
        case 'V':
         return VectorValues.Vcc;
        case 'X':
         return VectorValues.DontCare;
        case 'G':
         return VectorValues.Gnd;
        case 'H':
         return VectorValues.High;
        case 'L':
         return VectorValues.Low;
        default:
         throw new U2PaException("Unknown VectorValue: '{0}'", letter);
      }
    }
  }
}
