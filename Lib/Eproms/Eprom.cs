using System;
using System.Collections.Generic;

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

    public string Display
    {
      get
      {
        // DIL pin, ZIF pin, description
        var left = new List<Tuple<int, int, string>>();
        var right = new List<Tuple<int, int, string>>();
        PinNumberTranslator t = new PinNumberTranslator(DilType, Placement);
        for (var i = 1; i <= 20; i++)
        {
          var leftZIF = 20 + i;
          var leftDIL = t.ToDIL(leftZIF);
          left.Add(Tuple.Create(leftZIF, leftDIL, ""));

          var rightZIF = 21 - i;
          var rightDIL = t.ToDIL(rightZIF);
          right.Add(Tuple.Create(rightZIF, rightDIL, ""));
        }
        string display = "";
        for (var i = 0; i < left.Count; i++)
          display += String.Format("{0} {1}   {2} {3}\r\n", left[i].Item1, left[i].Item2, right[i].Item2, right[i].Item1);

        return display;
      }
    }
  }
}