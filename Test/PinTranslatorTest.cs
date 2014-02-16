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
using NUnit.Framework;
using U2Pa.Lib;
using U2Pa.Lib.IC;

namespace U2Pa.Test
{
  [TestFixture]
  public class PinTranslatorTest
  {
    [Test]
    public void Dil40ZIF40Placement0()
    {
      var translator = new PinTranslator(40, 40, 0);

      for (var i = 0; i <= 41; i++)
        Assert.That(translator.ToDIL(i), Is.EqualTo(i <= 40 ? i : 0));

      for (var i = 1; i <= 41; i++)
      {
        var pin = new Pin { EnableLow = true, Number = i, TrueZIF = false };
        Assert.That(translator.ToZIF(pin), Is.EqualTo(i <= 40 ? i : 0));
      }
    }

    [Test]
    public void Dil32ZIF40Placement1()
    {
      var translator = new PinTranslator(32, 40, 2);

      // .ToDil
      for (var i = 0; i <= 2; i++)
        Assert.That(translator.ToDIL(i), Is.EqualTo(0));
      for (var i = 3; i <= 18; i++)
        Assert.That(translator.ToDIL(i), Is.EqualTo(i - 2));
      for (var i = 19; i <= 21; i++)
        Assert.That(translator.ToDIL(i), Is.EqualTo(0));
      for (var i = 23; i <= 38; i++)
        Assert.That(translator.ToDIL(i), Is.EqualTo(i - 6));
      for (var i = 39; i <= 41; i++)
        Assert.That(translator.ToDIL(i), Is.EqualTo(0));

      // .ToZIF
      {
        var pin = new Pin { EnableLow = true, Number = 0, TrueZIF = false };
        Assert.That(translator.ToZIF(pin), Is.EqualTo(0));
      }
      for (var i = 1; i <= 16; i++)
      {
        var pin = new Pin { EnableLow = true, Number = i, TrueZIF = false };
        Assert.That(translator.ToZIF(pin), Is.EqualTo(i + 2));
      }
      for (var i = 17; i <= 32; i++)
      {
        var pin = new Pin { EnableLow = true, Number = i, TrueZIF = false };
        Assert.That(translator.ToZIF(pin), Is.EqualTo(i + 6));
      }
      for (var i = 33; i <= 41; i++)
      {
        var pin = new Pin { EnableLow = true, Number = i, TrueZIF = false };
        Assert.That(translator.ToZIF(pin), Is.EqualTo(0));
      }
    }
  }
}
