using System.Collections.Generic;
using System.IO;

namespace U2Pa
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
  }
}
