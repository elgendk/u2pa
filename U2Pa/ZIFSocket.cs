using System.Collections;

namespace U2Pa
{
  public class ZIFSocket
  {
    private readonly BitArray pins;

    public ZIFSocket(int size)
    {
      pins = new BitArray(size + 1);
    }

    public ZIFSocket(int size, byte[] initial2Pbytes)
    : this(size)
    {
      Swallow2PBytes(initial2Pbytes);
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

    private void Swallow2PBytes(byte[] bytes)
    {
      Swallow2PByte(bytes[0], 1, 8, 20);
      Swallow2PByte(bytes[1], 9, 16, 20);
      Swallow2PByte(bytes[2], 17, 25, 20);
      Swallow2PByte(bytes[3], 26, 33, 20);
      Swallow2PByte(bytes[4], 34, 40, 20);
    }

    private void Swallow2PByte(byte b, int from, int to, int skip)
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

    public byte[] Spew2PBytes()
    {
      return new[]
               {
                 Spew2PByte(1, 8, 20),
                 Spew2PByte(9, 16, 20),
                 Spew2PByte(17, 25, 20),
                 Spew2PByte(26, 33, 20),
                 Spew2PByte(34, 40, 20)
               };
    }

    private byte Spew2PByte(int from, int to, int skip)
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