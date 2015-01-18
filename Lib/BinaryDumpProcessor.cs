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
using System.Linq;
using System.Text;

namespace U2Pa.Lib
{
  /// <summary>
  /// A class for processing a binary dump i various ways.
  /// </summary>
  public class BinaryDumpProcessor
  {
    /// <summary>
    /// The file data.
    /// </summary>
    public byte[] Data { get; private set; }

    /// <summary>
    /// ctor.
    /// </summary>
    /// <param name="path"></param>
    public BinaryDumpProcessor(string path)
    {
      Data = Tools.ReadBinaryFile(path).ToArray();
    }

    /// <summary>
    /// Generates all the unreduced product terms in CUPL format.
    /// </summary>
    /// <param name="numberOfOutputs">Number of input pins.</param>
    /// <param name="numberOfInputs">Number of output pins.</param>
    /// <returns>The generated equations.</returns>
    public string GenerateCUPLEquations(int numberOfOutputs, int numberOfInputs)
    {
      var adresses = BuildAddressTable(numberOfInputs);
      var data = BuildDataTable();
      StringBuilder sb = new StringBuilder();
      for (var i = 0; i < numberOfOutputs; i++)
      {
        GenerateProductTerms(sb, numberOfInputs, i, adresses, data);
        sb.Append(Environment.NewLine);
      }
      return sb.ToString();
    }

    /// <summary>
    /// Generates the product terms for one output pin.
    /// </summary>
    /// <param name="sb">Accumulating <see cref="StringBuilder"/>.</param>
    /// <param name="numberOfInputs">Number of input pins.</param>
    /// <param name="outputNumber">The output pin to build product terms for.</param>
    /// <param name="addresses">The addresses.</param>
    /// <param name="data">The data.</param>
    /// <returns>The string containing the product terms.</returns>
    private string GenerateProductTerms(StringBuilder sb, int numberOfInputs, int outputNumber, BitArray[] addresses, BitArray[] data)
    {
      var prefix = String.Format(" D{0}       =", outputNumber);
      for (var j = 0; j < addresses.Length; j++)
      {
        if (!(data[j])[outputNumber])
          continue;

        sb.Append(Environment.NewLine);
        sb.Append(prefix);

        var productPrefix = "";
        for (var i = 0; i < numberOfInputs; i++)
        {
          sb.Append(productPrefix);
          productPrefix = " &";
          sb.AppendFormat(" {0}A{1}", (addresses[j])[i] ? " " : "!", i);
        }

        prefix = "          #";
      }

      sb.Append(";");
      return sb.ToString();
    }

    /// <summary>
    /// Generates a truth table in CUPL format.
    /// </summary>
    /// <param name="numberOfOutputs">Number of input pins.</param>
    /// <param name="numberOfInputs">Number of output pins.</param>
    /// <returns>The generated truth table.</returns>
    public string GenerateCUPLTruthTable(int numberOfOutputs, int numberOfInputs)
    {
      var sb = new StringBuilder();
      sb.AppendLine(String.Format("field address = [A{0}..0];", numberOfInputs - 1));
      sb.AppendLine(String.Format("field data = [D{0}..0];", numberOfOutputs - 1));
      sb.AppendLine();
      sb.AppendLine("table address => data {");
      for (var i = 0; i < Data.Length; i++)
      {
        sb.AppendLine(String.Format(
          " {0} => {1};",
          i.ToString("X").PadLeft(3, ' '),
          Data[i].ToString("X2")));
      }
      sb.AppendLine("}");

      return sb.ToString();
    }

    /// <summary>
    /// Debug method for generating a pretty print of the file data as a truthtable.
    /// </summary>
    /// <param name="numberOfOutputs">Number of input pins.</param>
    /// <param name="numberOfInputs">Number of output pins.</param>
    /// <returns>The generated truth table.</returns>
    public string GenerateHumanReadableTruthTable(int numberOfOutputs, int numberOfInputs)
    {
      var adresses = BuildAddressTable(numberOfInputs);
      var data = BuildDataTable();

      var sb = new StringBuilder();
      sb.AppendLine("| NNN | AAAAAAAAAAAAAAAA | DDDDDDDDDDDDDDDD | HH |");
      sb.AppendLine("|     | 0123456789ABCDEF | 0123456789ABCDEF |    |");
      sb.AppendLine("+-----+------------------+------------------+----+");
      for (var j = 0; j < Math.Pow(2, numberOfInputs); j++)
      {
        sb.Append("| ");
        sb.Append(j.ToString("X3").PadLeft(3, '0'));
        sb.Append(" | ");
        for (var i = 0; i < 16; i++)
        {
          if (i < numberOfInputs)
            sb.Append((adresses[j])[i] ? "1" : "0");
          else
            sb.Append(".");
        }
        sb.Append(" | ");
        for (var i = 0; i < 16; i++)
        {
          if (i < numberOfOutputs)
            sb.Append((data[j])[i] ? "1" : "0");
          else
            sb.Append(".");
        }
        sb.Append(" | ");
        sb.Append(Data[j].ToString("X2"));
        sb.Append(" |");
        sb.Append(Environment.NewLine);
      }
      sb.AppendLine("+-----+------------------+------------------+----+");

      return sb.ToString();
    }

    private BitArray[] BuildDataTable()
    {
      return Data.Select(i => new BitArray(new[] { i })).ToArray();
    }

    private BitArray[] BuildAddressTable(int numberOfInputs)
    {
      return Enumerable.Range(0, (Int32)Math.Pow(2,numberOfInputs)).Select(i => new BitArray(new[] { i })).ToArray();
    }
  }
}
