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
using NUnit.Framework;
using U2Pa.Lib;
using U2Pa.Lib.IC;

namespace U2Pa.Test
{
  [TestFixture]
  public class AdaptorTest
  {
    [Test]
    public void Adaptor27nXMbTest()
    {
      var adaptor = AdaptorXml.Specified["27nXMb"].Init(40, 0);
      for (var i = 1; i <= 40; i++)
      {
        var dilledPin = adaptor.ToDIL(i);
        if (i == 20)
        {
          Assert.That(dilledPin, Is.EqualTo(11));
          continue;
        }
        if (i == 30)
        {
          Assert.That(dilledPin, Is.EqualTo(20));
          continue;
        }
        Assert.That(dilledPin, Is.EqualTo(i));
      }
      for (var i = 1; i <= 40; i++)
      {
        var pin = new Pin { EnableLow = true, Number = i, TrueZIF = false };
        var ziffedPin = adaptor.ToZIF(pin);
        if (i == 11)
        {
          Assert.That(ziffedPin, Is.EqualTo(20));
          continue;
        }
        if (i == 20)
        {
          Assert.That(ziffedPin, Is.EqualTo(30));
          continue;
        }
        if (i == 30)
        {
          Assert.That(ziffedPin, Is.EqualTo(20));
          continue;
        } 
        Assert.That(ziffedPin, Is.EqualTo(i));
      }
    }

    /*
    [Test]
    public void AdaptorDummyTest()
    {
      var adaptor = AdaptorXml.Specified["Dummy"].Init(42, 0);
      for (var i = 0; i <= 43; i++)
      {
        var dilledPin = adaptor.ToDIL(i);
        Console.WriteLine(".toDil({0}) => {1}", i, dilledPin);
      }
      for (var i = 0; i <= 43; i++)
      {
        var pin = new Pin { EnableLow = true, Number = i, TrueZIF = false };
        var ziffedPin = adaptor.ToZIF(pin);
        Console.WriteLine(".toZIF({0}) => {1}", i, ziffedPin);
      }
    }
    */

//  1----------ZIF-----------40
//  2                        39
//  3                        38
//  4   1----Adaptor----32   37
//  5   2               31   36
//  6   3               30   35
//  7   4   1--IC--24   29   34
//  8   5   2      23   28   33
//  9   6   3      22   27   32
// 10   7<--4------21-->26   31
// 11   8   5      20   25   30
// 12   9   6      19   24   29
// 13  10   7      18   23   28
// 14  11   8      17   22   27
// 15  12   9      16   21   26
// 16  13  10      15   20   25
// 17  14  11      14   19   24
// 18  15  12--IC--13   18   23
// 19  16----Adaptor----17   22
// 20----------ZIF-----------21
//
// We remap 7 and 26 on the adaptor.
// All other we keep the same.
    class TestAdaptor : Adaptor
    {
      public TestAdaptor()
      {
        FromHoleToPin = new Dictionary<int, int> 
      {
        { 7, 26 }, { 26, 7 }
      };
        FromPinToHole = new Dictionary<int, int>
      {
        { 26, 7 }, { 7, 26 }
      };
        AdaptorTranslator = new PinTranslator(32, 40, 1);
        ICTranslator = new PinTranslator(24, 32, 1);
      }
    }

    [Test]
    public void TestPlacement()
    {
      var adaptor = new TestAdaptor();

      // .ToDil
      for (var i = 0; i <= 6; i++)
        Assert.That(adaptor.ToDIL(i), Is.EqualTo(0));
      for (var i = 7; i <= 9; i++)
        Assert.That(adaptor.ToDIL(i), Is.EqualTo(i - 6));
      Assert.That(adaptor.ToDIL(10), Is.EqualTo(21));
      for (var i = 11; i <= 18; i++)
        Assert.That(adaptor.ToDIL(i), Is.EqualTo(i - 6));
      for (var i = 19; i <= 22; i++)
        Assert.That(adaptor.ToDIL(i), Is.EqualTo(0));
      for (var i = 23; i <= 30; i++)
        Assert.That(adaptor.ToDIL(i), Is.EqualTo(i - 10));
      Assert.That(adaptor.ToDIL(31), Is.EqualTo(4));
      for (var i = 32; i <= 34; i++)
        Assert.That(adaptor.ToDIL(i), Is.EqualTo(i - 10));
      for (var i = 35; i <= 41; i++)
        Assert.That(adaptor.ToDIL(i), Is.EqualTo(0));

      // .ToZIF
      Assert.That(adaptor.ToZIF(new Pin { EnableLow = true, Number = 0, TrueZIF = false }), Is.EqualTo(0));
      for (var i = 1; i <= 3; i++)
        Assert.That(adaptor.ToZIF(new Pin { EnableLow = true, Number = i, TrueZIF = false }), Is.EqualTo(i + 6));
      Assert.That(adaptor.ToZIF(new Pin { EnableLow = true, Number = 4, TrueZIF = false }), Is.EqualTo(31));
      for (var i = 5; i <= 12; i++)
        Assert.That(adaptor.ToZIF(new Pin { EnableLow = true, Number = i, TrueZIF = false }), Is.EqualTo(i + 6));
      for (var i = 13; i <= 20; i++)
        Assert.That(adaptor.ToZIF(new Pin { EnableLow = true, Number = i, TrueZIF = false }), Is.EqualTo(i + 10));
      Assert.That(adaptor.ToZIF(new Pin { EnableLow = true, Number = 21, TrueZIF = false }), Is.EqualTo(10));
      for (var i = 22; i <= 24; i++)
        Assert.That(adaptor.ToZIF(new Pin { EnableLow = true, Number = i, TrueZIF = false }), Is.EqualTo(i + 10));
      for (var i = 25; i <= 41; i++)
        Assert.That(adaptor.ToZIF(new Pin { EnableLow = true, Number = i, TrueZIF = false }), Is.EqualTo(0));
    }
  }
}
