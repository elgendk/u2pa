﻿//                             u2pa
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
  /// <summary>
  /// Abstraction of an EPROM.
  /// </summary>
  public abstract class Eprom
  {
    /// <summary>
    /// The 'name' of the EPROM i.e. '2764'. 
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
    /// The VppLevel.
    /// </summary>
    public double VppLevel;
    
    /// <summary>
    /// The prog pulse (in ms) to use in the classic write algoritm.
    /// </summary>
    public int ProgPulse;
    
    /// <summary>
    /// An optional delay allowing the external boost converter to spin up.
    /// </summary>
    public int InitialProgDelay;

    /// <summary>
    /// The ordered sequence of the address pins.
    /// </summary>
    public Pin[] AddressPins;

    /// <summary>
    /// The ordered sequence of the data pins.
    /// </summary>
    public Pin[] DataPins;

    /// <summary>
    /// The ChipEnable pin.
    /// </summary>
    public Pin[] ChipEnable;

    /// <summary>
    /// The OutputEnable pin.
    /// </summary>
    public Pin[] OutputEnable;

    /// <summary>
    /// The Program pin.
    /// </summary>
    public Pin[] Program;
    
    /// <summary>
    /// The sequence of pins that should remain at constant TTL-level.
    /// </summary>
    public Pin[] Constants;

    /// <summary>
    /// The pins that should be connected to Vcc.
    /// </summary>
    public Pin[] VccPins;

    /// <summary>
    /// The pins that should be connected to GND.
    /// </summary>
    public Pin[] GndPins;

    /// <summary>
    /// The pins the should be connected to Vpp. 
    /// </summary>
    public Pin[] VppPins;

    /// <summary>
    /// Adaptor; can be <c>null</c>.
    /// </summary>
    public Adaptor Adaptor;

    /// <summary>
    /// Placement/offset of the Adaptor in the ZIF socket.
    /// </summary>
    public int AdaptorPlacement;

    /// <summary>
    /// Gets the <see cref="IPinTranslator"/>.
    /// </summary>
    /// <param name="zifType">The zif type.</param>
    /// <returns></returns>
    public IPinTranslator GetPinTranslator(int zifType)
    {
      return Adaptor == null
        ? (IPinTranslator) (new PinTranslator(DilType, zifType, Placement))
        : (IPinTranslator) Adaptor.Init(zifType, AdaptorPlacement, DilType, Placement);
    }

    /// <summary>
    /// Extracts the address from the provided zif socket and returns it as an Int32.
    /// </summary>
    /// <param name="zif">The zif socket containing the address.</param>
    /// <returns>The address as an Int32.</returns>
    public int GetAddress(ZIFSocket zif)
    {
      return zif.GetDataAsInt(AddressPins, GetPinTranslator(zif.Size).ToZIF);
    }

    /// <summary>
    /// Gets the data from the provided zif socket and returns it as a sequence of bytes.
    /// </summary>
    /// <param name="zif">The zif socket containing the data.</param>
    /// <returns>The data as a sequence of bytes.</returns>
    public IEnumerable<byte> GetData(ZIFSocket zif)
    {
      return zif.GetDataAsBytes(DataPins, GetPinTranslator(zif.Size).ToZIF);
    }

    /// <summary>
    /// Sets the specified address on the specified zif socket.
    /// </summary>
    /// <param name="zif">The zif socket to write to.</param>
    /// <param name="address">The address.</param>
    public void SetAddress(ZIFSocket zif, int address)
    {
      zif.SetPins(address, AddressPins, GetPinTranslator(zif.Size).ToZIF);
    }

    /// <summary>
    /// Sets the specified data on the specified zif.
    /// </summary>
    /// <param name="zif">The zif socket to write to.</param>
    /// <param name="data">The data.</param>
    public void SetData(ZIFSocket zif, byte[] data)
    {
      zif.SetPins(data, DataPins, GetPinTranslator(zif.Size).ToZIF);
    }

    /// <summary>
    /// Displays the EPROM inserted correctly into the Top-programmer.
    /// </summary>
    /// <returns>The string representation of the EPROM.</returns>
    public override string ToString()
    {
      var adaptorInUse = Adaptor != null;
      // DIL pin, ZIF pin, description.
      var left = new List<Tuple<int, int>>();
      var right = new List<Tuple<int, int>>();
      var t = GetPinTranslator(40);

      // First blank all fields.
      var zifPins = new string[41];
      for (var i = 0; i < zifPins.Length; i++)
        zifPins[i] = "";
      foreach (var p in ChipEnable)
        zifPins[t.ToZIF(p)] = (p.EnableLow ? "/" : "") + "CE";
      foreach (var p in OutputEnable)
        zifPins[t.ToZIF(p)] = (p.EnableLow ? "/" : "") + "OE";
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
        zifPins[t.ToZIF(p)] = tmp + (p.EnableLow ? "/" : "") + "P";
      }
      for (var i = 0; i < AddressPins.Length; i++)
        zifPins[t.ToZIF(AddressPins[i])] = String.Format("A{0}", i);
      for (var i = 0; i < DataPins.Length; i++)
        zifPins[t.ToZIF(DataPins[i])] = String.Format("D{0}", i);
      foreach (var p in Constants)
      {
        zifPins[t.ToZIF(p)] = "C" + (p.EnableLow ? "0" : "1");
      }

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
        else if (!adaptorInUse && right[i].Item1 == 20 - Placement) middle = String.Format(" +-----{0}-----+ ", "-");
        else if (!adaptorInUse && right[i].Item1 == 20 - Placement - (DilType / 2) + 1) middle = String.Format(" +-----{0}-----+ ", "^");
        else if (!adaptorInUse && right[i].Item2 == (DilType / 4) + 1) middle = String.Format(" +{0}+ ", Type.Pad(11));
        else middle = " +".PadRight(13) + "+ ";

        display += String.Format("  |{0} {1} {2}{3}{4} {5} {6}|\r\n",
                                 left[i].Item1.ToString(CultureInfo.InvariantCulture).PadRight(2),
                                 zifPins[20 + (i + 1)].PadLeft(6),
                                 (left[i].Item2 == 0 ? "| =====" : left[i].Item2.ToString(CultureInfo.InvariantCulture).PadRight(2)),
                                 middle,
                                 (right[i].Item2 == 0 ? "===== |" : right[i].Item2.ToString(CultureInfo.InvariantCulture).PadLeft(2)),
                                 zifPins[21 - (i + 1)].PadRight(6),
                                 right[i].Item1.ToString(CultureInfo.InvariantCulture).PadLeft(2));
      }
      display += "  |          +----------------++          |\r\n";
      display += "  +---------------------------|-----------+\r\n";
      display += "       ZIF Socket Handle -->  |\r\n";
      display += "                              O\r\n";

      display += String.Format("\r\n      Eprom  : {0}", Type);
      display += adaptorInUse ? String.Format("\r\n      Adaptor: {0}\r\n", Adaptor.Type) : "";
      display += String.IsNullOrEmpty(Notes) ? "" : Notes;

      return display;
    }
  }
}
