//                        BipolarDump2PLD
//
//    A QAD commandline tool to make CUPL equations from a dump of a bipolar prom.
//
//    Copyright (C) Elgen };-) aka Morten Overgaard 2014
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
using System.IO;
using System.Linq;
using System.Text;

namespace U2Pa.BipolarDump2PLD
{
  internal static class BipolarDump2PLDMain
  {
    /// <summary>
    /// The main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <remarks>
    /// args[0] = number of outputs
    /// args[1] = number of inputs
    /// args[2] = options
    /// args[3] = file name
    /// </remarks>
    static void Main(string[] args)
    {
      if (args.Length < 4)
      {
        Console.WriteLine(
@"
Usage:
~> bd2pld.exe <number_of_outputs> <number_of_inputs> <option> <full_file_name>
Possible options are:
  ce = print CUPL equations
  cf = print CUPL truth table field data
  ht = print human readable truth table");

        return;
      }

      var numberOfOutputs = Int32.Parse(args[0]);
      var numberOfInputs = Int32.Parse(args[1]);
      var option = args[2];
      var fileData = ReadBinaryFile(args[3]).ToArray();

      //Building address table
      var adresses = Enumerable.Range(0, fileData.Length).Select(i => new BitArray(new[]{(byte)i})).ToArray();
      
      //Building data table
      var data = fileData.Select(i => new BitArray(new[] { i })).ToArray();

      Console.WriteLine();

      switch(option)
      {
        case "ce":
          PrintProductTerms(numberOfOutputs, numberOfInputs, adresses, data);
          break;
          
        case "cf":
          PrintTruthTableFieldData(numberOfOutputs, numberOfInputs, fileData, adresses.Length);
          break;

        case "ht":
          PrintHumanReadableTruthTable(numberOfOutputs, numberOfInputs, fileData, adresses, data);
          break;
      }

      Console.WriteLine();
    }

    /// <summary>
    /// Prints all the unreduced product terms in CUPL format.
    /// </summary>
    /// <param name="numberOfOutputs">Number of input pins.</param>
    /// <param name="numberOfInputs">Number of output pins.</param>
    /// <param name="adresses">The addresses.</param>
    /// <param name="data">The data.</param>
    static void PrintProductTerms(int numberOfOutputs, int numberOfInputs, BitArray[] adresses, BitArray[] data)
    {
      StringBuilder sb = new StringBuilder();
      for (var i = 0; i < numberOfOutputs; i++)
      {
        BuildProductTerms(sb, numberOfInputs, i, adresses, data);
        sb.Append(Environment.NewLine);
      }
      Console.Write(sb.ToString());
    }

    /// <summary>
    /// Builds the product terms for one output pin.
    /// </summary>
    /// <param name="sb">Accumulating <see cref="StringBuilder"/>.</param>
    /// <param name="numberOfInputs">Number of input pins.</param>
    /// <param name="outputNumber">The output pin to build product terms for.</param>
    /// <param name="addresses">The addresses.</param>
    /// <param name="data">The data.</param>
    /// <returns>The string containing the product terms.</returns>
    static string BuildProductTerms(StringBuilder sb, int numberOfInputs, int outputNumber, BitArray[] addresses, BitArray[] data)
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
    /// Prints CUPL truth table data.
    /// </summary>
    /// <param name="numberOfOutputs">Number of input pins.</param>
    /// <param name="numberOfInputs">Number of output pins.</param>
    /// <param name="numberOfAddresses">Number of adresses.</param>
    private static void PrintTruthTableFieldData(int numberOfOutputs, int numberOfInputs, byte[] fileData, int numberOfAddresses)
    {
      Console.WriteLine("field address = [A{0}..0];", numberOfInputs - 1);
      Console.WriteLine("field data = [D{0}..0];", numberOfOutputs - 1);
      Console.WriteLine();
      Console.WriteLine("table address => data {");
      for (var i = 0; i < numberOfAddresses; i++)
      {
        Console.WriteLine(
          " {0} => {1};", 
          i.ToString("X").PadLeft(3, ' '), 
          fileData[i].ToString("X2"));
      }
      Console.WriteLine("}");
    }

    /// <summary>
    /// Debug method for PrettyPriting the truthtable.
    /// </summary>
    static void PrintHumanReadableTruthTable(int numberOfOutputs, int numberOfInputs, byte[] fileData, BitArray[] adresses, BitArray[] data)
    {
      Console.WriteLine("| NNN | AAAAAAAA | DDDDDDDD | HH |");
      Console.WriteLine("|     | 01234567 | 01234567 |    |");
      Console.WriteLine("+-----+----------+----------+----+");
      for (var j = 0; j < adresses.Length; j++)
      {
        Console.Write("| ");
        Console.Write(j.ToString("X3").PadLeft(3, '0'));
        Console.Write(" | ");
        for (var i = 0; i < 8; i++)
        {
          if (i < numberOfInputs)
            Console.Write((adresses[j])[i] ? "1" : "0");
          else
            Console.Write(" ");
        }
        Console.Write(" | ");
        for (var i = 0; i < 8; i++)
        {
          if (i < numberOfOutputs)
            Console.Write((data[j])[i] ? "1" : "0");
          else
            Console.Write(" ");
        }
        Console.Write(" | ");
        Console.Write(fileData[j].ToString("X2"));
        Console.Write(" |");
        Console.Write(Environment.NewLine);
      }
    }

    /// <summary>
    /// Reads a binary file.
    /// </summary>
    /// <param name="fileName">Full filename.</param>
    /// <returns>The read bytes.</returns>
    /// <remarks>"Stolen" from the u2Pa-project.</remarks>
    static IEnumerable<byte> ReadBinaryFile(string fileName)
    {
      var buffer = new byte[1];
      using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
      {
        using (var br = new BinaryReader(fs))
        {
          while (0 != br.Read(buffer, 0, 1))
            yield return buffer[0];
        }
      }
    }
  }
}
