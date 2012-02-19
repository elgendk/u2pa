using System.Collections;
using System.Collections.Generic;

namespace U2Pa
{
  public static class ExtensionMethods
  {
    public static IEnumerable<byte> ToBytes(this BitArray bits)
    {
      if (bits.Count%8 != 0)
        throw new U2PaException("bits.Count % 8 != 0");

      byte acc = 0x00;
      var mainIndex = 0;
      var accIndex = 0;
      while (true)
      {
        if (accIndex == 8)
        {
          yield return acc;
          acc = 0x00;
          accIndex = 0;
        }
        if (mainIndex == bits.Count)
          yield break;

        if (bits[mainIndex]) acc |= (byte) (0x01 << accIndex);
        mainIndex++;
        accIndex++;
      }
    }

    public static int Pow(this int baseNumber, int exponent)
    {
      return exponent == 0 ? 1 : baseNumber * Pow(baseNumber, exponent - 1);
    }
  }

}