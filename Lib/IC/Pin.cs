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

namespace U2Pa.Lib.IC
{
  /// <summary>
  /// Abstraction of an IC-pin.
  /// </summary>
  public class Pin
  {
    /// <summary>
    /// True if the pin is enabled by a TTL-low signal.
    /// <remarks>
    /// In the xml-file, this is indicated by putting
    /// a '/'-sign in front of the pin number.
    /// </remarks>
    /// </summary>
    public bool EnableLow { get; internal set; }

    /// <summary>
    /// The number of the pin with respect to DIL-package.
    /// <remarks>
    /// If <see cref="TrueZIF"/> is true, the number is
    /// to be with respect to the ZIF-socket.
    /// </remarks>
    /// </summary>
    public int Number { get; internal set; }

    /// <summary>
    /// The boolean value that will enable this pin.
    /// </summary>
    public bool Enable { get{ return !EnableLow; } }

    /// <summary>
    /// The boolean value that will disable this pin.
    /// </summary>
    public bool Disable { get { return EnableLow; } }

    /// <summary>
    /// If true, <see cref="Number"/> to be with respect to the ZIF-socket.
    /// </summary>
    public bool TrueZIF { get; internal set; }

    /// <summary>
    /// Parses a pin-string from the xml-file.
    /// </summary>
    /// <param name="pinRep">The pin-string found in the xml-file.</param>
    /// <returns>The created instance of the <see cref="Pin"/>-class.</returns>
    public static Pin Parse(string pinRep)
    {
      pinRep = pinRep.Trim();
      var enableLow = pinRep.StartsWith("/");
      pinRep = pinRep.Replace("/", "");
      var trueZIF = pinRep.EndsWith("Z");
      pinRep = pinRep.Replace("Z", "");
      return new Pin { EnableLow = enableLow, TrueZIF = trueZIF, Number = Int32.Parse(pinRep) };
    }
  }
}
