using System.Collections;

namespace U2Pa
{
  public static class ExtensionMethods
  {
    public static byte[] Spew2PBytes(this BitArray bits)
    {
      return new byte[]
               {
                 0x10, bits.Spew2PByte(1, 8, 20),
                 0x11, bits.Spew2PByte(9, 16, 20),
                 0x12, bits.Spew2PByte(17, 25, 20),
                 0x13, bits.Spew2PByte(26, 33, 20),
                 0x14, bits.Spew2PByte(34, 40, 20),
                 0x0A, 0x15, 0xFF
               };
    }

    private static byte Spew2PByte(this BitArray bits, int from, int to, int skip)
    {
      byte acc = 0x00;
      var j = 0;
      for (var i = from; i <= to; i++)
      {
        if (i == skip) continue;
        if (bits[i]) acc |= (byte)(0x01 << j);
        j++;
      }
      return acc;
    }
  
    public static void Eat2PBytes(this BitArray bits, byte[] bytes)
    {}

    public static byte[] ToBytes(this BitArray bits)
    {
      return null;
    }

    public static int Pow(this int baseNumber, int exponent)
    {
      return exponent == 0 ? 1 : baseNumber * Pow(baseNumber, exponent - 1);
    }

    public static byte Pow(this byte baseNumber, int exponent)
    {
      return exponent == 0 ? (byte)1 : (byte)(baseNumber * Pow(baseNumber, exponent - 1));
    }
  }

}