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
  public abstract class VectorTest
  {
    /// <summary>
    /// The 'name' of the test i.e. 'TTL7432'. 
    /// </summary>
    public string Type;

    /// <summary>
    /// The number of pins.
    /// </summary>
    public int DilType;

    /// <summary>
    /// Important notes about the EPROM.
    /// </summary>
    public string Notes;

    /// <summary>
    /// Description.
    /// </summary>
    public string Description;

    /// <summary>
    /// Placement/offset of the EPROM in the ZIF socket.
    /// </summary>
    public int Placement;

    /// <summary>
    /// The VccLevel.
    /// </summary>
    public double VccLevel;

    /// <summary>
    /// Controls if pull-ups are to be enabled.
    /// </summary>
    public bool PullUpsEnabled;

    /// <summary>
    /// The ordered sequence of test steps.
    /// </summary>
    public List<Vector> Vectors;
  }

  public class Vector
  {
    /// <summary>
    /// The ordered sequence of the input pins.
    /// </summary>
    public List<Pin> InputPins = new List<Pin>();

    /// <summary>
    /// The ordered sequence of the output pins.
    /// </summary>
    public List<Pin> OutputPins = new List<Pin>();

    /// <summary>
    /// The ordered sequence of the output pins.
    /// </summary>
    public List<Pin> DontCares = new List<Pin>();

    /// <summary>
    /// The pins that should be connected to Vcc.
    /// </summary>
    public List<Pin> VccPins = new List<Pin>();

    /// <summary>
    /// The pins that should be connected to GND.
    /// </summary>
    public List<Pin> GndPins = new List<Pin>();
  }

  public class VectorResult
  { }
}
