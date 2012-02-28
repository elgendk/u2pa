using System;
using System.Collections.Generic;
using System.Globalization;

namespace U2Pa.Lib.Eproms
{
  public abstract class Eprom
  {
    public string Type;
    public int DilType;
    public string Notes;
    public int Placement;
    public int[] AddressPins;
    public int[] DataPins;
    public int ChipEnable;
    public int OutputEnable;
    public int Program;
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
      var t = new PinNumberTranslator(DilType, Placement);

      // First blank all fields.
      var zifPins = new string[41];
      for (var i = 0; i < zifPins.Length; i++)
        zifPins[i] = "";
      zifPins[t.ToZIF(ChipEnable)] = (ChipEnable < 0 ? "/" : "") + "CE";
      zifPins[t.ToZIF(OutputEnable)] = (OutputEnable < 0 ? "/" : "") + "OE";
      foreach (var t1 in VccPins)
        zifPins[t.ToZIF(t1)] = "Vcc";
      foreach (var t1 in GndPins)
        zifPins[t.ToZIF(t1)] = "Gnd";
      foreach (var t1 in VppPins)
      {
        var tmp = zifPins[t.ToZIF(t1)];
        zifPins[t.ToZIF(t1)] = tmp + "Vpp";
      }
      {
        var tmp = zifPins[t.ToZIF(Program)];
        zifPins[t.ToZIF(Program)] = tmp + (Program < 0 ? "/" : "") + "P";
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
      display += "  |   +------------------------------+    |\r\n";
      display += "  |   |  POWER    O      O    READY  |    |\r\n";
      display += "  |=  |   TOP               TOP2005+ |   =|\r\n";
      display += "  |   |      Universal Programmer    |    |\r\n";
      display += "  |   +------------------------------+    |\r\n";
      display += "  +---------------------------------------+\r\n";
      display += "  |          +-----------------+          |\r\n";
      for (var i = 0; i < 20; i++)
      {
        string middle;
        if (left[i].Item2 == 0) middle = " | | ";
        else if (left[i].Item1 == 21) middle = " +-----------+ ";
        else if (right[i].Item2 == 1) middle = " +-----O-----+ ";
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