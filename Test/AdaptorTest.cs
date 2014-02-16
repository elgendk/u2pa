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

using System.Collections.Generic;
using NUnit.Framework;
using U2Pa.Lib;
using U2Pa.Lib.IC;
using System;

namespace U2Pa.Test
{
  [TestFixture]
  public class AdaptorTest
  {
    [Test]
    public void Adaptor27nXMbTest()
    {
      var adaptor = AdaptorXml.Specified["27nXMb"].Init(40, 40, 0);
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

//  1----------ZIF-----------40
//  2   1----Adaptor----32   39
//  3   2   1--IC--24   31   38
//  4   3   2      23   30   37
//  5   4   3      22   29   36
//  6   5   4      21   28   35
//  7   6   5      20   27   34
//  8   7<--6------19-->26   33
//  9   8   7      18   25   32
// 10   9   8      17   24   31
// 11  10   9      16   23   30
// 12  11  10      15   22   29
// 13  12  11      14   21   28
// 14  13  12--IC--13   20   27
// 15  14               19   26
// 16  15               18   25
// 17  16----Adaptor----17   24
// 18                        23
// 19                        22
// 20----------ZIF-----------21
//
// We remap 7 and 19 on the adaptor.
// All other we keep the same.
    class TestAdaptor : Adaptor
    {
      public TestAdaptor()
      {
        FromHoleToPin = new Dictionary<int, int> 
      {
        { 7, 26 }
      };
        FromPinToHole = new Dictionary<int, int>
      {
        { 26, 7 }
      };
        AdaptorTranslator = new PinTranslator(32, 40, 1);
        ICTranslator = new PinTranslator(24, 32, 1);
      }
    }

    [Test]
    public void TestPlacement()
    {
      var adaptor = new TestAdaptor();
      for (var i = 1; i <= 40; i++)
      {
        var dilledPin = adaptor.ToDIL(i);
        Console.WriteLine("adaptor.ToDIL({0}) = {1}", i, dilledPin);
      }
      for (var i = 1; i <= 24; i++)
      {
        var pin = new Pin { EnableLow = true, Number = i, TrueZIF = false };
        var ziffedPin = adaptor.ToZIF(pin);
        Console.WriteLine("adaptor.ToZIF({0}) = {1}", i, ziffedPin);
      }
    }
  }
}