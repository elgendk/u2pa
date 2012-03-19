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

namespace U2Pa.Lib.IC
{
  public abstract class SRam
  {
    public string Type;
    public int DilType;
    public string Notes;
    public string Description;
    public int Placement;
    public bool UpsideDown;
    public int[] AddressPins;
    public int[] DataPins;
    public int[] ChipEnable;
    public int OutputEnable;
    public int WriteEnable;
    public VccLevel VccLevel;
    public int[] VccPins;
    public int[] GndPins;
  }
}