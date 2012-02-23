using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    public int[] EnablePins;
    public VccLevel VccLevel;
    public VppLevel VppLevel;
    public byte[] VccPins;
    public byte[] GndPins;
    public byte[] VppPins;
    public byte ProgramPin;

    public static Eprom Create(string type)
    {
      if (type == "2716")
        return new Eprom2716();
      if (type == "271024")
        return new Eprom271024();
      if (type == "272048")
        return new Eprom272048();

      throw new U2PaException("Unsupported EPROM: {0}", type);
    }

    public override string ToString()
    {
      // DIL pin, ZIF pin, description
      var left = new List<Tuple<int, int>>();
      var right = new List<Tuple<int, int>>();
      PinNumberTranslator t = new PinNumberTranslator(DilType, Placement);

      var zifPins = new string[41];
      for (var i = 0; i < zifPins.Length; i++)
        zifPins[i] = "";

      for (var i = 0; i < EnablePins.Length; i++)
        zifPins[t.ToZIF(EnablePins[i])] = String.Format("{0}", i == 0 ? "/CE" : "/OE");

      for (var i = 0; i < VccPins.Length; i++)
        zifPins[t.ToZIF(VccPins[i])] = "Vcc";

      for (var i = 0; i < GndPins.Length; i++)
        zifPins[t.ToZIF(GndPins[i])] = "Gnd";

      for (var i = 0; i < VppPins.Length; i++)
      {
        var tmp = zifPins[t.ToZIF(VppPins[i])];
        zifPins[t.ToZIF(VppPins[i])] = tmp + "Vpp";
      }

      if (ProgramPin != 0)
      {
        var tmp = zifPins[t.ToZIF(ProgramPin)];
        zifPins[t.ToZIF(ProgramPin)] = tmp + "Prg";
      }

      for (var i = 0; i < AddressPins.Length; i++)
        zifPins[t.ToZIF(AddressPins[i])] = String.Format("A{0}", i);

      for (var i = 0; i < DataPins.Length; i++)
        zifPins[t.ToZIF(DataPins[i])] = String.Format("D{0}", i);


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
      display += "+----------------------------------------+\r\n";
      display += "|    POWER   O              O   READY    |\r\n";
      display += "|    TOP                       TOPXXXX   |\r\n";
      display += "|           Universal Programmer         |\r\n";
      display += "+----------------------------------------+\r\n";
      for (var i = 0; i < 20; i++)
      {
        string middle;
        if (left[i].Item2 == 0) middle = "".PadLeft(11);
        else if (left[i].Item1 == 21) middle = " +-----------+ ";
        else if (right[i].Item2 == 1) middle = " +-----O-----+ ";
        else if (right[i].Item2 == (DilType/4) + 1)
          middle = String.Format(" +{0}+ ",
                                 Type.PadLeft(8).PadRight(11));
        else middle = " +".PadRight(13) + "+ ";

        display += String.Format("| {0} {1} {2}{3}{4} {5} {6}|\r\n",
                                 left[i].Item1.ToString().PadRight(2),
                                 zifPins[20 + (i + 1)].PadLeft(6),
                                 (left[i].Item2 == 0 ? "----" : left[i].Item2.ToString().PadRight(2)),
                                 middle,
                                 (right[i].Item2 == 0 ? "----" : right[i].Item2.ToString().PadLeft(2)),
                                 zifPins[21 - (i + 1)].PadRight(6),
                                 right[i].Item1.ToString().PadLeft(2));
      }
      display += "+-----------------------------+----------+\r\n";
      display += "                              |\r\n";
      display += "       ZIF Socket Handle -->  |\r\n";
      display += "                              O\r\n\r\n";

      return display;
    }
  }
}