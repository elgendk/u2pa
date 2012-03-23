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
using System.Globalization;

namespace U2Pa.Lib.IC
{
  public abstract class Eprom
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
    public int[] OutputEnable;
    public int[] Program;
    public VccLevel VccLevel;
    public VppLevel VppLevel;
    public int[] VccPins;
    public int[] GndPins;
    public int[] VppPins;

    [Obsolete("We use xml-based definition on Eprom instead now.")]
    public static Eprom Create(string type)
    {
      throw new U2PaException("Unsupported EPROM: {0}", type);
    }

    public override string ToString()
    {
      // DIL pin, ZIF pin, description.
      var left = new List<Tuple<int, int>>();
      var right = new List<Tuple<int, int>>();
      var t = new PinNumberTranslator(DilType, 40, Placement, UpsideDown);

      // First blank all fields.
      var zifPins = new string[41];
      for (var i = 0; i < zifPins.Length; i++)
        zifPins[i] = "";
      foreach (var p in ChipEnable)
        zifPins[t.ToZIF(p)] = (p < 0 ? "/" : "") + "CE";
      foreach (var p in OutputEnable)
        zifPins[t.ToZIF(p)] = (p < 0 ? "/" : "") + "OE";
      foreach (var p in VccPins)
        zifPins[t.ToZIF(p)] = "Vcc";
      foreach (var p in GndPins)
        zifPins[t.ToZIF(p)] = "Gnd";
      foreach (var p in VppPins)
      {
        var tmp = zifPins[t.ToZIF(p)];
        zifPins[t.ToZIF(p)] = tmp + "Vpp";
      }
      foreach (var p in Program)
      {
        var tmp = zifPins[t.ToZIF(p)];
        zifPins[t.ToZIF(p)] = tmp + (p < 0 ? "/" : "") + "P";
      }
      for (var i = 0; i < AddressPins.Length; i++)
        zifPins[t.ToZIF(AddressPins[i])] = String.Format("A{0}", i);
      for (var i = 0; i < DataPins.Length; i++)
        zifPins[t.ToZIF(DataPins[i])] = String.Format("D{0}", i);

      // ASCII Graphics FTW };-P
      for (var i = 1; i <= 20; i++)
      {
        var leftZIF = 20 + i;
        var leftDIL = t.ToDIL(leftZIF);
        left.Add(Tuple.Create(leftZIF, leftDIL));

        var rightZIF = 21 - i;
        var rightDIL = t.ToDIL(rightZIF);
        right.Add(Tuple.Create(rightZIF, rightDIL));
      }
      string display = "\r\n";
      display += "  +---------------------------------------+\r\n";
      display += "  |   +-------------------------------+   |\r\n";
      display += "  |   |  POWER    O       O    READY  |   |\r\n";
      display += "  |=  |   TOP                TOP2005+ |  =|\r\n";
      display += "  |   |     Universal Programmer      |   |\r\n";
      display += "  |   +-------------------------------+   |\r\n";
      display += "  +---------------------------------------+\r\n";
      display += "  |          +-----------------+          |\r\n";
      for (var i = 0; i < 20; i++)
      {
        string middle;
        if (left[i].Item2 == 0) middle = " | | ";
        else if (left[i].Item1 == 21) middle = " +-----------+ ";
        else if (!UpsideDown && right[i].Item2 == 1) middle = " +-----O-----+ ";
        else if (right[i].Item2 == (DilType/4) + 1)
          middle = String.Format(" |{0}| ",
                                 Type.Pad(11));
        else middle = " |".PadRight(13) + "| ";

        display += String.Format("  |{0} {1} {2}{3}{4} {5} {6}|\r\n",
                                 left[i].Item1.ToString(CultureInfo.InvariantCulture).PadRight(2),
                                 zifPins[20 + (i + 1)].PadLeft(6),
                                 (left[i].Item2 == 0 ? "| -----" : left[i].Item2.ToString(CultureInfo.InvariantCulture).PadRight(2)),
                                 middle,
                                 (right[i].Item2 == 0 ? "----- |" : right[i].Item2.ToString(CultureInfo.InvariantCulture).PadLeft(2)),
                                 zifPins[21 - (i + 1)].PadRight(6),
                                 right[i].Item1.ToString(CultureInfo.InvariantCulture).PadLeft(2));
      }
      display += "  |          +----------------++          |\r\n";
      display += "  +---------------------------|-----------+\r\n";
      display += "       ZIF Socket Handle -->  |\r\n";
      display += "                              O\r\n";

      display += String.IsNullOrEmpty(Notes) ? "" : Notes;

      return display;
    }
  }
}
