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

namespace U2Pa.Lib.IC
{
  public abstract class Adaptor : IPinTranslator
  {
    /// <summary>
    /// The 'name' of the Adaptor. 
    /// </summary>
    public string Type;

    public int Placement { get; protected set; }
    public int PinType { get; protected set; }
    public int HoleType { get; protected set; }

    protected IDictionary<int, int> FromPinToHole { get; set; }
    protected IDictionary<int, int> FromHoleToPin { get; set; }
    protected IPinTranslator ICTranslator { get; set; }
    protected IPinTranslator AdaptorTranslator { get; set; }

    public Adaptor Init(int dilType, int zifType, int placement, bool upsideDown)
    {
      return Init(new PinTranslator(dilType, zifType, placement, upsideDown));
    }

    public Adaptor Init(IPinTranslator icTranslator)
    {
      ICTranslator = icTranslator;
      return this;
    }

    private static int Adapt(IDictionary<int, int> table, int pinToAdapt)
    {
      int adaptedPin;
      return table.TryGetValue(pinToAdapt, out adaptedPin)
        ? adaptedPin
        : pinToAdapt;
    }

    /// <summary>
    /// Translates from pin to hole.
    /// </summary>
    /// <param name="pinNumber">Pin number.</param>
    /// <returns>Hole number.</returns>
    public int PinToHole(int pinNumber)
    {
      return Adapt(FromPinToHole, pinNumber);
    }

    /// <summary>
    /// Translates from hole to pin.
    /// </summary>
    /// <param name="holeNumber">Hole number.</param>
    /// <returns>Pin number.</returns>
    public int HoleToPin(int holeNumber)
    {
      return Adapt(FromHoleToPin, holeNumber);
    }

    /// <summary>
    /// Translates a ZIF-pinnumber to DIL-pinnumber.
    /// </summary>
    /// <param name="zifPinNumer">The ZIF-pinnumber.</param>
    /// <returns>The DIL-pinnumber.</returns>
    public int ToDIL(int zifPinNumer)
    {
      return ICTranslator.ToDIL(PinToHole(AdaptorTranslator.ToDIL(zifPinNumer)));
    }

    /// <summary>
    /// Translates from DIL-pin to ZIF-pinnumber.
    /// </summary>
    /// <param name="dilPin">The DIL-pin.</param>
    /// <returns>The ZIF-pinumber.</returns>
    public int ToZIF(Pin dilPin)
    {
      var temp = new Pin
      {
        Number = HoleToPin(ICTranslator.ToZIF(dilPin)),
        EnableLow = dilPin.EnableLow,
        TrueZIF = false
      };
      return AdaptorTranslator.ToZIF(temp);
    }
  }
}
