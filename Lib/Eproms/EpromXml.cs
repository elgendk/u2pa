using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Linq;

namespace U2Pa.Lib.Eproms
{
  public class EpromXml : Eprom
  {
    public static IDictionary<string, EpromXml> Specified { get; private set; } 
    static EpromXml()
    {
      Specified = XDocument.Load("Eproms\\Eproms.xml").Descendants("Eprom").ToDictionary(
        x => x.Attribute("type").Value,
        x => new EpromXml
               {
                 Type = x.Attribute("type").Value,
                 DilType = Int32.Parse(x.Attribute("dilType").Value),
                 Placement = Int32.Parse(x.Attribute("placement").Value),
                 VccLevel = Tools.ParseVccLevel(x.Attribute("Vcc").Value),
                 VppLevel = Tools.ParseVppLevel(x.Attribute("Vpp").Value),
                 AddressPins = x.Element("AddressPins").Value.Split(',').Select(Int32.Parse).ToArray(),
                 DataPins = x.Element("DataPins").Value.Split(',').Select(Int32.Parse).ToArray(),
                 EnablePins = x.Element("EnablePins").Value.Split(',').Select(Int32.Parse).ToArray(),
                 VccPins = x.Element("VccPins").Value.Split(',').Select(byte.Parse).ToArray(),
                 VppPins = x.Element("VppPins").Value.Split(',').Select(byte.Parse).ToArray(),
                 GndPins = x.Element("GndPins").Value.Split(',').Select(byte.Parse).ToArray(),
                 Notes = x.Element("Notes") != null ? x.Element("Notes").Value : null
               });
    }
  }
}