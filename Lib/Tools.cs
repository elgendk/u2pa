using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using U2Pa.Lib.Eproms;

namespace U2Pa.Lib
{
  public static class Tools
  {
    internal static IEnumerable<byte> ReadBinaryFile(string fileName)
    {
      var buffer = new byte[1];
      var bytes = new List<byte>();
      // Open file and read it in
      using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
      {
        using (var br = new BinaryReader(fs))
        {
          while (0 != br.Read(buffer, 0, 1))
            bytes.Add(buffer[0]);
        }
      }
      return bytes;
    }

    public static ReadSoundness AnalyzeEpromReadSoundness(
      ZIFSocket[] results,
      Eprom eprom,
      int address,
      out ZIFSocket result)
    {
      result = null;
      var refLenght = results.Length;
      var revResults = results.Reverse().ToArray();

      var withCorrectAddresses = revResults.TakeWhile(x => x.GetEpromAddress(eprom) == address).ToArray();
      if (withCorrectAddresses.Length == 0)
        return ReadSoundness.TryRewrite;
      if (withCorrectAddresses.Length != refLenght)
        return ReadSoundness.TryReread;

      var refData = revResults[0].GetEpromData(eprom);
      var withRefData = revResults.TakeWhile(x => x.GetEpromData(eprom).SequenceEqual(refData)).ToArray();
      if (withRefData.Length != refLenght)
        return ReadSoundness.TryReread;

      result = revResults[0];
      return ReadSoundness.SeemsToBeAOkay;
    }

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


    public static IEnumerable<byte> ToBytes(this BitArray bits)
    {
      if (bits.Count % 8 != 0)
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

        if (bits[mainIndex]) acc |= (byte)(0x01 << accIndex);
        mainIndex++;
        accIndex++;
      }
    }

    internal static VccLevel ParseVccLevel(string stringValue)
    {
      stringValue = stringValue.Trim();
      if(String.IsNullOrEmpty(stringValue)) return VccLevel.Vcc_5_0v;
      if (stringValue == "2.5") return VccLevel.Vcc_2_5v;
      if (stringValue == "3.3") return VccLevel.Vcc_3_3v;
      if (stringValue == "5") return VccLevel.Vcc_5_0v;
      
      throw new U2PaException("Unknown Vcc: {0}", stringValue);
    }

    internal static VppLevel ParseVppLevel(string stringValue)
    {
      stringValue = stringValue.Trim();
      if (String.IsNullOrEmpty(stringValue)) return VppLevel.Vpp_Off;
      if (stringValue == "12.5") return VppLevel.Vpp_12_61v;
      if (stringValue == "21") return VppLevel.Vpp_21_11v;
      if (stringValue == "25") return VppLevel.Vpp_25_59v;

      throw new U2PaException("Unknown Vpp: {0}", stringValue);
    }
  }
}
