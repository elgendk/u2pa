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

namespace U2Pa.Test
{
  [TestFixture]
  public class AdaptorTest
  {
    [Test]
    public void Adaptor27nXMbTest()
    {
      var adaptor = AdaptorXml.Specified["27nXMb"].Init(40, 40, 0, false);
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
  }
}