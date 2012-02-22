using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace U2Pa.Lib
{
  public class ZIFSocket
  {
    private readonly BitArray pins;

    public ZIFSocket(int size, byte[] initialTopbytes)
    : this(size)
    {
      SwallowTopBytes(initialTopbytes);
    }

    public ZIFSocket(int size)
    {
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

    public void SetEpromAddress(Eproms.Eprom eprom, int address)
    {
      var translator = new PinNumberTranslator(eprom.DilType, eprom.Placement);
      var bitAddress = new BitArray(new[] { address });
      for (var i = 0; i < eprom.AddressPins.Length; i++)
        pins[translator.ToZIF(eprom.AddressPins[i])] = bitAddress[i];
    }

    public int GetEpromAddress(Eproms.Eprom eprom)
    {
      var translator = new PinNumberTranslator(eprom.DilType, eprom.Placement);
      var acc = 0;
      for (var i = 0; i < eprom.AddressPins.Length; i++)
      {
        acc |= pins[translator.ToZIF(eprom.AddressPins[i])] ? 1 << i : 0;
      }
      return acc;
    }

    public IEnumerable<byte> GetEpromData(Eproms.Eprom eprom)
    {
      var translator = new PinNumberTranslator(eprom.DilType, eprom.Placement);
      var readByte = new BitArray(eprom.DataPins.Length);
      for (var i = 0; i < eprom.DataPins.Length; i++)
        readByte[i] = pins[translator.ToZIF(eprom.DataPins[i])];
      return readByte.ToBytes();
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