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

using U2Pa.Lib.IC;

namespace U2Pa.Lib
{
  /// <summary>
  /// An interface for translating between pin numbers with respect
  /// to either the ZIF-socket og the DIL-package
  /// </summary>
  public interface IPinTranslator
  {
    /// <summary>
    /// Translates a ZIF-pinnumber to DIL-pinnumber.
    /// </summary>
    /// <param name="zifPinNumer">The ZIF-pinnumber.</param>
    /// <returns>The DIL-pinnumber.</returns>
    int ToDIL(int zifPinNumer);

    /// <summary>
    /// Translates from DIL-pin to ZIF-pinnumber.
    /// </summary>
    /// <param name="dilPin">The DIL-pin.</param>
    /// <returns>The ZIF-pinumber.</returns>
    int ToZIF(Pin dilPin);
  }
}
