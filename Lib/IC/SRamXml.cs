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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;

namespace U2Pa.Lib.IC
{
  /// <summary>
  /// Abstraction of an SRAM with definitions in xml.
  /// </summary>
  public class SRamXml : SRam
  {
    private static IDictionary<string, SRamXml> specified;
    /// <summary>
    /// Dictionary of the SRAMs specified in xml.
    /// <remarks>
    /// The keys are the 'Type' of the SRAM.
    /// </remarks>
    /// </summary>
    public static IDictionary<string, SRamXml> Specified
    {
      get
      {
        if (specified == null)
          Load();
        return specified;
      }
    }

    /// <summary>
    /// Loader.
    /// </summary>
    private static void Load()
    {
      var xDocument = XDocument.Load(Path.Combine("Xml", "SRams.xml"));
      var xSchema = new XmlSchemaSet();
      xSchema.Add("", Path.Combine("Xml", "SRams.xsd"));
      xDocument.Validate(xSchema, null);
      specified =
        xDocument.Descendants("SRam").ToDictionary(
        x => x.Attribute("type").Value,
        x => new SRamXml()
        {
          Type = x.Attribute("type").Value,
          DilType = Int32.Parse(x.Attribute("dilType").Value),
          Placement = Int32.Parse(x.Attribute("placement").Value),
          UpsideDown = x.Attribute("upsideDown") != null
            ? Boolean.Parse(x.Attribute("upsideDown").Value)
            : false,
          VccLevel = Double.Parse(x.Attribute("Vcc").Value, CultureInfo.InvariantCulture),
          AddressPins = x.ToPinArray("AddressPins"),
          DataPins = x.ToPinArray("DataPins"),
          ChipEnable = x.ToPinArray("ChipEnable"),
          OutputEnable = x.ToPinArray("OutputEnable"),
          WriteEnable = x.ToPinArray("WriteEnable"),
          Constants = x.ToPinArray("Constants"),
          VccPins = x.ToPinArray("VccPins"),
          GndPins = x.ToPinArray("GndPins"),
          Notes = x.Element("Notes") != null ? x.Element("Notes").Value : null,
          Description = x.Element("Description") != null ? x.Element("Description").Value : null
        });
    }
  }
}
