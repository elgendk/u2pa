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
  public class ZIFSocket
  {
    private readonly BitArray pins;
    private readonly int size;

    public ZIFSocket(int size, byte[] initialTopbytes)
    : this(size)
    {
      SwallowTopBytes(initialTopbytes);
    }

    public ZIFSocket(int size)
    {
      this.size = size;
      pins = new BitArray(size + 1);
    }

    public bool this[int i]
    {
      get { return pins[i]; }
      set { pins[i] = value; }
    }

    public void SetAll(bool value)
    {
      pins.SetAll(value);
    }

    public void SetSRamAddress(SRam sram, int address)
    {
      SetPins(address, sram.AddressPins, new PinNumberTranslator(sram.DilType, size, sram.Placement, sram.UpsideDown).ToZIF);
    }

    public void SetSRamData(SRam sram, byte[] data)
    {
      SetPins(data, sram.DataPins, new PinNumberTranslator(sram.DilType, size, sram.Placement, sram.UpsideDown).ToZIF);
    }

    public void SetEpromAddress(Eprom eprom, int address)
    {
      SetPins(address, eprom.AddressPins, new PinNumberTranslator(eprom.DilType, size, eprom.Placement, eprom.UpsideDown).ToZIF);
    }

    public void SetEpromData(Eprom eprom, byte[] data)
    {
      SetPins(data, eprom.DataPins, new PinNumberTranslator(eprom.DilType, size, eprom.Placement, eprom.UpsideDown).ToZIF);
    }

    public void SetPins(int data, int[] dilMask, Func<int, int> translate = null)
    {
      translate = translate ?? (x => x);
      var bitData = new BitArray(new[] {data});
      SetPins(bitData, dilMask, translate);
    }

    public void SetPins(byte[] data, int[] dilMask, Func<int, int> translate = null)
    {
      translate = translate ?? (x => x);
      var bitData = new BitArray(data);
      SetPins(bitData, dilMask, translate);
    }

    public void SetPins(BitArray bitData, int[] dilMask, Func<int,int> translate = null)
    {
      translate = translate ?? (x => x);
      for (var i = 0; i < dilMask.Length; i++)
        pins[translate(dilMask[i])] = bitData[i];      
    } 

    public int GetDataAsInt(int[] dilMask, Func<int, int> translate = null)
    {
      translate = translate ?? (x => x);
      var acc = 0;
      for (var i = 0; i < dilMask.Length; i++)
      {
        acc |= pins[translate(dilMask[i])] ? 1 << i : 0;
      }
      return acc;
    }

    public int GetEpromAddress(Eprom eprom)
    {
      return GetDataAsInt(eprom.AddressPins, new PinNumberTranslator(eprom.DilType, size, eprom.Placement, eprom.UpsideDown).ToZIF);
    }

    public IEnumerable<byte> GetDataAsBytes(int[] dilMask, Func<int, int> translate = null)
    {
      translate = translate ?? (x => x);
      var readByte = new BitArray(dilMask.Length);
      for (var i = 0; i < dilMask.Length; i++)
        readByte[i] = pins[translate(dilMask[i])];
      return readByte.ToBytes();
    }

    public IEnumerable<byte> GetEpromData(Eprom eprom)
    {
      return GetDataAsBytes(eprom.DataPins, new PinNumberTranslator(eprom.DilType, size, eprom.Placement, eprom.UpsideDown).ToZIF);
    }

    public string ToString(Func<int, int> translate = null)
    {
      translate = translate ?? (x => x);
      string accZif = "";
      string accDil = "";
      string acc = "";
      for(var i = 1; i < pins.Length; i++)
      {
        var enereZif = i%10;
        accZif += enereZif.ToString();

        acc += pins[i] ? "1" : "0";
        
        var dilPin = translate(i);
        var enereDil = dilPin%10;
        accDil += dilPin == 0 ? "." : enereDil.ToString();
      }
      return 
        "zif: " + accZif + Environment.NewLine + 
        "bit: " + acc + Environment.NewLine + 
        "dil: " + accDil;
    }

    private void SwallowTopBytes(byte[] bytes)
    {
      SwallowTopByte(bytes[0], 1, 8, 20);
      SwallowTopByte(bytes[1], 9, 16, 20);
      SwallowTopByte(bytes[2], 17, 25, 20);
      SwallowTopByte(bytes[3], 26, 33, 20);
      SwallowTopByte(bytes[4], 34, 40, 20);
    }

    private void SwallowTopByte(byte b, int from, int to, int skip)
    {
      var skipOffset = 0;
      for(var i = from; i <= to; i++)
      {
        if (i == skip)
          skipOffset = 1;
        pins[i + skipOffset] = b%2 != 0;
        b = (byte)(b >> 1);
      }
    }

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
  }
}