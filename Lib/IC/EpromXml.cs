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
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;

namespace U2Pa.Lib.IC
{
  public class EpromXml : Eprom
  {
    public static IDictionary<string, EpromXml> Specified { get; private set; }
    static EpromXml()
    {
      var xDocument = XDocument.Load("Xml\\Eproms.xml");
      var xSchema = new XmlSchemaSet();
      xSchema.Add("", "Xml\\ICs.xsd");
      xDocument.Validate(xSchema, null);
      Specified = 
        xDocument.Descendants("Eprom").ToDictionary(
        x => x.Attribute("type").Value,
        x => new EpromXml
               {
                 Type = x.Attribute("type").Value,
                 DilType = Int32.Parse(x.Attribute("dilType").Value),
                 Placement = Int32.Parse(x.Attribute("placement").Value),
                 UpsideDown = x.Attribute("upsideDown") != null 
                   ? Boolean.Parse(x.Attribute("upsideDown").Value) 
                   : false,
                 VccLevel = Tools.ParseVccLevel(x.Attribute("Vcc").Value),
                 VppLevel = Tools.ParseVppLevel(x.Attribute("Vpp").Value),
                 AddressPins = x.Element("AddressPins").Value.Split(',').Select(Int32.Parse).ToArray(),
                 DataPins = x.Element("DataPins").Value.Split(',').Select(Int32.Parse).ToArray(),
                 ChipEnable = x.Element("ChipEnable").Value.Split(',').Select(Int32.Parse).ToArray(),
                 OutputEnable = x.Element("OutputEnable").Value.Split(',').Select(Int32.Parse).ToArray(),
                 Program = x.Element("Program").Value.Split(',').Select(Int32.Parse).ToArray(),
                 VccPins = x.Element("VccPins").Value.Split(',').Select(Int32.Parse).ToArray(),
                 GndPins = x.Element("GndPins").Value.Split(',').Select(Int32.Parse).ToArray(),
                 VppPins = x.Element("VppPins").Value.Split(',').Select(Int32.Parse).ToArray(),
                 Notes = x.Element("Notes") != null ? x.Element("Notes").Value : null,
                 Description = x.Element("Description") != null ? x.Element("Description").Value : null
               });
    }
  }
}