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
using U2Pa.Lib.IC;

namespace U2Pa.Lib
{
  /// <summary>
  /// A class for translating between pin numbers with respect
  /// <to either the ZIF-socket og the DIL-package
  /// </summary>
  public class PinTranslator
  {
    private readonly int dilIndex;
    private readonly int dilType;
    private readonly int zifType;
    private readonly int zifIndex;
    private readonly int placement;
    private readonly bool upsideDown;

    /// <summary>
    /// ctor.
    /// </summary>
    /// <param name="dilType">Number of pins on the DIL-package.</param>
    /// <param name="zifType">Number of pins in the ZIF-socket.</param>
    /// <param name="placement">Placement/offset of the DIL-package in the ZIF socket.</param>
    /// <param name="upsideDown">
    /// True if the DIL-package is placed upside-down
    /// instead of the drawing on Top-programmers frontcover.
    /// </param>
    public PinTranslator(int dilType, int zifType, int placement, bool upsideDown)
    {
      if (dilType % 2 != 0)
        throw new U2PaException("dilType must be even, but was {0}", dilType);
      this.dilType = dilType;
      dilIndex = dilType/2;
      if (zifType % 2 != 0)
        throw new U2PaException("zifType must be even, but was {0}", zifType);
      this.zifType = zifType;
      this.zifIndex = zifType/2;
      if (placement + dilIndex > zifIndex)
        throw new U2PaException("DIL{0} can't be placed at position {1}", dilType, placement);
      this.placement = placement;
      this.upsideDown = upsideDown;
    }

    private Pin Turn180DegIfNeeded(Pin dilPin)
    {
      if (!upsideDown) return dilPin;
      return new Pin 
      { 
        EnableLow = dilPin.EnableLow, 
        TrueZIF = dilPin.TrueZIF, 
        Number = Turn180DegIfNeeded(dilPin.Number)
      }; 
    }

    private int Turn180DegIfNeeded(int dilPinNumber)
    {
      if (!upsideDown) return dilPinNumber;
      return dilPinNumber > dilIndex ? dilPinNumber - dilIndex : dilPinNumber + dilIndex; 
    }

    /// <summary>
    /// Translates a ZIF-pinnumber to DIL-pinnumber.
    /// </summary>
    /// <param name="zifPinNumer">The ZIF-pinnumber.</param>
    /// <returns>The DIL-pinnumber.</returns>
    public int ToDIL(int zifPinNumer)
    {
      if (zifPinNumer == 0) return 0;
      int returnValue;
      if (zifPinNumer <= zifIndex)
      {
        returnValue = zifPinNumer - zifIndex + dilIndex + placement;
        return 
          returnValue > dilIndex || returnValue < 0 ? 0 : Turn180DegIfNeeded(returnValue);
      }
      
      returnValue = zifPinNumer - zifIndex + dilIndex - placement;
      return
        returnValue <= dilIndex || returnValue > dilType ? 0 : Turn180DegIfNeeded(returnValue);
    }

    /// <summary>
    /// Translates from DIL-pin to ZIF-pinnumber.
    /// </summary>
    /// <param name="dilPin">The DIL-pin.</param>
    /// <returns>The ZIF-pinumber.</returns>
    public int ToZIF(Pin dilPin)
    {
      if (dilPin.TrueZIF) return dilPin.Number;
      if (dilPin.Number == 0) return 0;
      dilPin = Turn180DegIfNeeded(dilPin);
      if (dilPin.Number <= dilIndex)
        return dilPin.Number + zifIndex - dilIndex - placement;
      return dilPin.Number + zifIndex - dilIndex + placement;
    }
  }
}
