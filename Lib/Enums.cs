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

namespace U2Pa.Lib
{
  /// <summary>
  /// Indicates the presumed soundness of a read.
  /// </summary>
  public enum ReadSoundness
  {
    SeemsToBeAOkay,
    TryReread,
    TryRewrite
  }

  // Copied from the MaxLoader Manual:
  // =================================
  // The following are valid characters for test vectors:
  // 0 Apply input logic low (Vil) to an input pin
  // 1 Apply input logic high (Vih) to an input pin
  // C Clock an input pin (Vil, Vih, Vil)
  // F Float pin
  // N Power pin or untested output pin
  // V VCC pin
  // X Don't care: output values are not tested
  // G GND pin
  // K Clock an inverted input pin (Vih, Vil, Vih)
  // H Expected result on output pin is Vih
  // L Expected result on output pin is Vil
  // Z Test for high impedance

  // Start with:
  // ===========
  // 0 Apply input logic low (Vil) to an input pin
  // 1 Apply input logic high (Vih) to an input pin via pull-ups
  // V VCC pin
  // X Don't care: output values are not tested
  // G GND pin
  // H Expected result on output pin is Vih
  // L Expected result on output pin is Vil

  /// <summary>
  /// The values used in the vector test.
  /// </summary>
  public enum VectorValues
  {
    /// <summary>
    /// 0 Apply input logic low (Vil) to an input pin.
    /// </summary>
    Zero,

    /// <summary>
    /// 1 Apply input logic high (Vih) to an input pin.
    /// </summary>
    One,

    /// <summary>
    /// V VCC pin.
    /// </summary>
    Vcc,

    /// <summary>
    /// X Don't care: output values are not tested.
    /// </summary>
    DontCare,

    /// <summary>
    /// G GND pin.
    /// </summary>
    Gnd,

    /// <summary>
    /// H Expected result on output pin is Vih.
    /// </summary>
    High,

    /// <summary>
    /// L Expected result on output pin is Vil.
    /// </summary>
    Low
  }
}
