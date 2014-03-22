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
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using System.IO;
using System;

namespace U2Pa.Lib.IC
{
  public class AdaptorXml : Adaptor
  {
    private static IDictionary<string, AdaptorXml> specified;
    /// <summary>
    /// Dictionary of the <see cref="Adaptor"/>s specified in xml.
    /// <remarks>
    /// The keys are the 'Type' of the Adaptor.
    /// </remarks>
    /// </summary>
    public static IDictionary<string, AdaptorXml> Specified
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
      var xDocument = XDocument.Load(Path.Combine(Tools.GetSubDir("Xml"), "Adaptors.xml"));
      var xSchema = new XmlSchemaSet();
      xSchema.Add("", Path.Combine(Tools.GetSubDir("Xml"), "Adaptors.xsd"));
      xDocument.Validate(xSchema, null);
      specified = xDocument.Descendants("Adaptor").ToDictionary(x => x.Attribute("type").Value, Load);
    }

    /// <summary>
    /// Method for unit testing.
    /// </summary>
    internal static AdaptorXml ParseXmlDoc(string xmlString)
    {
      var xDocument = XDocument.Parse(xmlString);
      var xSchema = new XmlSchemaSet();
      xSchema.Add("", Path.Combine(Tools.GetSubDir("Xml"), "Adaptors.xsd"));
      xDocument.Validate(xSchema, null);
      return AdaptorXml.Load(xDocument.Descendants("Adaptor").Single());
    }

    internal static AdaptorXml Load(XElement x)
    {
      {
        var remaps = x.Elements("Remap")
          .Select(e => Tuple.Create(
            Int32.Parse(e.Attribute("hole").Value),
            Int32.Parse(e.Attribute("pin").Value)))
          .ToList();
        var fromHoleToPin = new Dictionary<int, int>();
        var fromPinToHole = new Dictionary<int, int>();
        foreach (var pair in remaps)
        {
          int alreadyInDic;
          if (fromHoleToPin.TryGetValue(pair.Item1, out alreadyInDic))
            throw new U2PaException("Both pin{0} and pin{1} are mapped to hole{2}", alreadyInDic, pair.Item2, pair.Item1);
          else
            fromHoleToPin.Add(pair.Item1, pair.Item2);

          if (fromPinToHole.TryGetValue(pair.Item2, out alreadyInDic))
          {
            if (pair.Item1 < alreadyInDic)
              fromPinToHole[pair.Item2] = pair.Item1;
          }
          else
            fromPinToHole.Add(pair.Item2, pair.Item1);
        }

        var holeType = Int32.Parse(x.Attribute("holes").Value);
        var pinType = Int32.Parse(x.Attribute("pins").Value);

        return new AdaptorXml
        {
          Type = x.Attribute("type").Value,
          HoleType = holeType,
          PinType = pinType,
          FromHoleToPin = fromHoleToPin,
          FromPinToHole = fromPinToHole
        };
      }
    }
  }
}
