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

using NUnit.Framework;
using U2Pa.Lib;
using System.Linq;
using U2Pa.Lib.IC;
using System;

namespace U2Pa.Test
{
  [TestFixture]
  public class VectorTestTest
  {
    [Test]
    public void ParseTest()
    {
      var input = "01010LGL01010V";
      var result = input.AsVectorValues().ToArray();
      var expected = new[]
      {
        VectorValues.Zero,
        VectorValues.One,
        VectorValues.Zero,
        VectorValues.One,
        VectorValues.Zero,
        VectorValues.Low,
        VectorValues.Gnd,
        VectorValues.Low,
        VectorValues.Zero,
        VectorValues.One,
        VectorValues.Zero,
        VectorValues.One,
        VectorValues.Zero,
        VectorValues.Vcc,
      };
      Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Temp()
    {
      foreach (var vectorTest in VectorTestXml.Specified)
      {
        Console.WriteLine(vectorTest);
      }
    }
  }
}
