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
using System.Collections.Generic;
using System.Linq;

namespace U2Pa.Lib
{
  /// <summary>
  /// Abstract representation of an FPGA-program.
  /// <remarks>
  /// The structure of the file is like this:
  /// (the lenghts are big endian)
  /// =======================================
  /// MagicHeader (13 bytes)
  /// FieldKey = 0x61; SourceFileName (1 byte)
  /// FieldLength (2 bytes)
  /// FieldPayload (FieldLength byte including a trailing 0x00)
  /// FieldKey = 0x62; FPGAType (1 byte)
  /// FieldLength (2 bytes)
  /// FieldPayload (FieldLength byte including a trailing 0x00)
  /// FieldKey = 0x63; CompileDate (1 byte)
  /// FieldLength (2 bytes)
  /// FieldPayload (FieldLength byte including a trailing 0x00)
  /// FieldKey = 0x64; CompileTime (1 byte)
  /// FieldLength (2 bytes)
  /// FieldPayload (FieldLength byte including a trailing 0x00)
  /// PayloadKey = 0x64; Program Payload (1 byte)
  /// PayloadLength (4 bytes)
  /// Payload (FieldLength byte including a trailing 0x00)
  /// =======================================
  /// </remarks>
  /// </summary>
  public class FPGAProgram
  {
    /// <summary>
    /// Magic Header; should always be the first thing in a *.bit-file.
    /// </summary>
    private static byte[] MagicHeader = new byte[] { 0x00, 0x09, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x00, 0x00, 0x01 };
    
    /// <summary>
    /// The name of the *.bit-file.
    /// </summary>
    public string FileName { get; private set; }

    /// <summary>
    /// The name of the source file.
    /// </summary>
    public string SourceFileName { get; private set; }

    /// <summary>
    /// The type of FPGA that program is compiled for.
    /// </summary>
    public string FPGAType { get; private set; }

    /// <summary>
    /// The compile date.
    /// </summary>
    public string CompileDate { get; private set; }

    /// <summary>
    /// The compile time.
    /// </summary>
    public string CompileTime { get; private set; }

    /// <summary>
    /// The actual program payload.
    /// </summary>
    public byte[] Payload { get; private set; }

    /// <summary>
    /// ctor.
    /// </summary>
    /// <param name="path"></param>
    public FPGAProgram(string path)
    {
      FileName = path;
      var data = Tools.ReadBinaryFile(path).ToList();
      
      var cursor = MagicHeader.Length;
      // Check that The Magic Header is correct
      if(!data.Take(cursor).SequenceEqual(MagicHeader))
        throw new U2PaException("Magic Header for the file {0} is corrupt", FileName);

      SourceFileName = ParseHeaderField(0x61, data, ref cursor);
      FPGAType = ParseHeaderField(0x62, data, ref cursor);
      CompileDate = ParseHeaderField(0x63, data, ref cursor);
      CompileTime = ParseHeaderField(0x64, data, ref cursor);
      Payload = ParsePayload(0x65, data, ref cursor);
    }

    /// <summary>
    /// Parse a single header field.
    /// </summary>
    /// <param name="key">The key of the field; possible values are { 0x61,..., 0x65 }.</param>
    /// <param name="data">The data read from the file.</param>
    /// <param name="cursor">The read cursor-index in the data array.</param>
    /// <returns>The field data as a string.</returns>
    private string ParseHeaderField(byte key, IList<byte> data, ref int cursor)
    {
      if(key != data[cursor])
        throw new U2PaException("Wrong key {0}; expected {1}", data[cursor], key);
      cursor++;

      var length = ToInt(2, data, ref cursor);
      var fieldData = data.Skip(cursor).Take(length).Aggregate("", (current, t) => current + (char)t).TrimEnd();
      cursor += length;
      return fieldData;
    }

    /// <summary>
    /// Parse the program payload.
    /// </summary>
    /// <param name="key">The key of the field (0x65).</param>
    /// <param name="data">The data read from the file.</param>
    /// <param name="cursor">The read cursor-index in the data array.</param>
    /// <returns>The payload as an array of bytes.</returns>
    private byte[] ParsePayload(byte key, IList<byte> data, ref int cursor)
    {
      if (key != data[cursor])
        throw new U2PaException("Wrong key {0}; expected {1}", data[cursor], key);
      cursor++;

      var length = ToInt(4, data, ref cursor);
      if(data.Count != cursor + length)
        throw new U2PaException("Wrong payload length {0}; expected {1}", data.Count - cursor, length);

      return data.Skip(cursor).Take(length).ToArray();
    }

    /// <summary>
    /// Converts 1 to 4 bytes (big endian) into an Int32.
    /// </summary>
    /// <param name="magnitude">How many bytes to read.</param>
    /// <param name="data">The data read from the file.</param>
    /// <param name="cursor">The read cursor-index in the data array.</param>
    /// <returns>The calculated Int32.</returns>
    private int ToInt(int magnitude, IList<byte> data, ref int cursor)
    {
      if (magnitude > 4 || magnitude < 1)
        throw new ArgumentException("Must be a natural number less than or equal to 4", "magnitude");

      return data.Skip((cursor += magnitude) - magnitude).Take(magnitude).Aggregate(0x00, (acc, b) => acc = (acc << 8) | b);
    }

    /// <summary>
    /// Prints basic info from the header.
    /// </summary>
    /// <returns>The string.</returns>
    public override string ToString()
    {
      return String.Format(@"
    FileName:       {0}
    SourceFileName: {1}
    FPGAType:       {2}
    CompileDate:    {3}
    CompileTime:    {4}
    Payload size:   {5} bytes",
                        FileName,
                        SourceFileName,
                        FPGAType,
                        CompileDate,
                        CompileTime,
                        Payload.Length);
    }
  }
}
