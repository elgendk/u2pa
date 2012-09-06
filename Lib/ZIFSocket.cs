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
using U2Pa.Lib.IC;

namespace U2Pa.Lib
{
  /// <summary>
  /// A representation of the ZIF socket on the programmer.
  /// It can be populated with bits; one for every pin in the socket.
  /// It is 1-indexed, but also contains a 0-pin. This one can always
  /// be written to any value one should desire. It is uncertain what
  /// value you'll get if you try to read it.
  /// </summary>
  public class ZIFSocket
  {
    private readonly BitArray pins;
    public int Size { get; private set; }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="size">The number of pins.</param>
    /// <param name="initialTopbytes">Initial values of pins.</param>
    public ZIFSocket(int size, byte[] initialTopbytes)
    : this(size)
    {
      ImportTopBytes(initialTopbytes);
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="size">The number of pins.</param>
    public ZIFSocket(int size)
    {
      this.Size = size;
      pins = new BitArray(size + 1);
    }

    /// <summary>
    /// Indexer.
    /// </summary>
    /// <param name="i">Index.</param>
    /// <returns>The value of the bit at the specified index.</returns>
    public bool this[int i]
    {
      get { return pins[i]; }
      set { pins[i] = value; }
    }

    /// <summary>
    /// Sets all bits to the specified value.
    /// </summary>
    /// <param name="value">The value to set all bit to.</param>
    public void SetAll(bool value)
    {
      pins.SetAll(value);
    }

    /// <summary>
    /// Sets the pins in the provided dil mask with the provided data represented as an Int32.
    /// </summary>
    /// <param name="data">The data represented as an Int32.</param>
    /// <param name="dilMask">The dil mask to use.</param>
    /// <param name="translate">The (optional) pin translator to use.</param>
    public void SetPins(int data, Pin[] dilMask, Func<Pin, int> translate = null)
    {
      translate = translate ?? (x => x.Number);
      var bitData = new BitArray(new[] {data});
      SetPins(bitData, dilMask, translate);
    }

    /// <summary>
    /// Sets the pins in the provided dil mask with the provided data represented as a byte[].
    /// </summary>
    /// <param name="data">The data represented as a byte[].</param>
    /// <param name="dilMask">The dil mask to use.</param>
    /// <param name="translate">The (optional) pin translator to use.</param>
    public void SetPins(byte[] data, Pin[] dilMask, Func<Pin, int> translate = null)
    {
      translate = translate ?? (x => x.Number);
      var bitData = new BitArray(data);
      SetPins(bitData, dilMask, translate);
    }

    /// <summary>
    /// Sets the pins in the provided dil mask with the provided data represented as a ByteArray.
    /// </summary>
    /// <param name="bitData">The data represented as a ByteArray.</param>
    /// <param name="dilMask">The dil mask to use.</param>
    /// <param name="translate">The (optional) pin translator to use.</param>
    public void SetPins(BitArray bitData, Pin[] dilMask, Func<Pin, int> translate = null)
    {
      translate = translate ?? (x => x.Number);
      for (var i = 0; i < dilMask.Length; i++)
        pins[translate(dilMask[i])] = bitData[i];      
    } 

    /// <summary>
    /// Gets the values of the pins in the dil mask and returns it as an Int32.
    /// </summary>
    /// <param name="dilMask">The dil mask to read.</param>
    /// <param name="translate">The (optional) pin translator to use.</param>
    /// <returns>The read data as an Int32.</returns>
    public int GetDataAsInt(Pin[] dilMask, Func<Pin, int> translate = null)
    {
      translate = translate ?? (x => x.Number);
      var acc = 0;
      for (var i = 0; i < dilMask.Length; i++)
      {
        acc |= pins[translate(dilMask[i])] ? 1 << i : 0;
      }
      return acc;
    }

    /// <summary>
    /// Gets the data using the provided dil mask and returns it as an Int32.
    /// </summary>
    /// <param name="eprom">The dil mask to use.</param>
    /// <param name="translate">The (optional) pin translator to use.</param>
    /// <returns>The data as a sequence of bytes.</returns>
    public IEnumerable<byte> GetDataAsBytes(Pin[] dilMask, Func<Pin, int> translate = null)
    {
      translate = translate ?? (x => x.Number);
      var readByte = new BitArray(dilMask.Length);
      for (var i = 0; i < dilMask.Length; i++)
        readByte[i] = pins[translate(dilMask[i])];
      return readByte.ToBytes();
    }

    /// <summary>
    /// Enables all the pins in the provided array of pins.
    /// </summary>
    /// <param name="enablePins">The pins to enable.</param>
    /// <param name="translate">The (optional) pin translator to use.</param>
    public void Enable(Pin[] enablePins, Func<Pin, int> translate = null)
    {
      translate = translate ?? (x => x.Number);
      foreach(var p in enablePins)
        pins[translate(p)] = p.Enable;
    }

    /// <summary>
    /// Disables all the pins in the provided array of pins.
    /// </summary>
    /// <param name="disablePins">The pins to disable.</param>
    /// <param name="translate">The (optional) pin translator to use.</param>
    public void Disable(Pin[] disablePins, Func<Pin, int> translate = null)
    {
      translate = translate ?? (x => x.Number);
      foreach(var p in disablePins)
        pins[translate(p)] = p.Disable;
    }

    /// <summary>
    /// Imports the provided data into the ZIF socket.
    /// <remarks>This is hardcoded to 40 pin socket! Rewrite this!</remarks>
    /// </summary>
    /// <param name="bytes">The bytes to import.</param>
    private void ImportTopBytes(byte[] bytes)
    {
      ImportTopByte(bytes[0], 1, 8, 20);
      ImportTopByte(bytes[1], 9, 16, 20);
      ImportTopByte(bytes[2], 17, 25, 20);
      ImportTopByte(bytes[3], 26, 33, 20);
      ImportTopByte(bytes[4], 34, 40, 20);
    }

    /// <summary>
    /// Imports a single byte into the ZIF socket.
    /// </summary>
    /// <param name="data">The byte to import.</param>
    /// <param name="from">The pin to start at.</param>
    /// <param name="to">The pin to stop at.</param>
    /// <param name="skip">If greater than 0, skip this pin; otherwise ignorred.</param>
    private void ImportTopByte(byte data, int from, int to, int skip)
    {
      var skipOffset = 0;
      for(var i = from; i <= to; i++)
      {
        if (i == skip)
          skipOffset = 1;
        pins[i + skipOffset] = data%2 != 0;
        data = (byte)(data >> 1);
      }
    }

    /// <summary>
    /// Extracts all data from the ZIF socket.
    /// <remarks>This is hardcoded to 40 pin socket! Rewrite this!</remarks>
    /// </summary>
    /// <returns>The data as an array of bytes.</returns>
    public byte[] ToTopBytes()
    {
      return new[]
               {
                 ToTopByte(1, 8, 20),
                 ToTopByte(9, 16, 20),
                 ToTopByte(17, 25, 20),
                 ToTopByte(26, 33, 20),
                 ToTopByte(34, 40, 20)
               };
    }

    /// <summary>
    /// Extracts a single byte of data from the ZIF socket.
    /// </summary>
    /// <param name="from">The pin to start at.</param>
    /// <param name="to">The pin to stop at.</param>
    /// <param name="skip">If greater than 0, skip this pin; otherwise ignorred.</param>
    /// <returns>The extracted byte of data.</returns>
    private byte ToTopByte(int from, int to, int skip)
    {
      byte acc = 0x00;
      var j = 0;
      for (var i = from; i <= to; i++)
      {
        if (i == skip) continue;
        if (pins[i]) acc |= (byte)(0x01 << j);
        j++;
      }
      return acc;
    }

    /// <summary>
    /// A string representation of the data in the ZIF socket.
    /// <remarks>Intented for debugging.</remarks>
    /// </summary>
    /// <param name="translate">The (optional) pin translator to use.</param>
    /// <returns>The string representation.</returns>
    public string ToString(Func<Pin, int> translate = null)
    {
      translate = translate ?? (x => x.Number);
      string accZif = "";
      string accDil = "";
      string acc = "";
      for (var i = 1; i < pins.Length; i++)
      {
        var enereZif = i % 10;
        accZif += enereZif.ToString();

        acc += pins[i] ? "1" : "0";

        var dilPin = translate(new Pin { Number = i });
        var enereDil = dilPin % 10;
        accDil += dilPin == 0 ? "." : enereDil.ToString();
      }
      return
        "zif: " + accZif + Environment.NewLine +
        "bit: " + acc + Environment.NewLine +
        "dil: " + accDil;
    }
  }
}
