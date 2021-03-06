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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Globalization;

namespace U2Pa.Lib.IC
{
  /// <summary>
  /// Abstraction of a vector test with definitions in xml.
  /// </summary>
  public class VectorTestXml : VectorTest
  {
    private static IDictionary<string, VectorTestXml> specified;
    /// <summary>
    /// Dictionary of the vector tests specified in xml.
    /// <remarks>
    /// The keys are the 'Type' of the vector test.
    /// </remarks>
    /// </summary>
    public static IDictionary<string, VectorTestXml> Specified
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
      var xDocument = XDocument.Load(Path.Combine(Tools.GetSubDir("Xml"), "VectorTests.xml"));
      var xSchema = new XmlSchemaSet();
      xSchema.Add("", Path.Combine(Tools.GetSubDir("Xml"), "VectorTests.xsd"));
      xDocument.Validate(xSchema, null);
      specified = xDocument.Descendants("VectorTest").ToDictionary(
        x => x.Attribute("type").Value,
        x => Load(x));
    }

    /// <summary>
    /// Method for unit testing.
    /// </summary>
    internal static VectorTestXml ParseXmlDoc(string xmlString, IDictionary<string, AdaptorXml> unitTestAdaptors)
    {
      var xDocument = XDocument.Parse(xmlString);
      var xSchema = new XmlSchemaSet();
      xSchema.Add("", Path.Combine(Tools.GetSubDir("Xml"), "VectorTests.xsd"));
      xDocument.Validate(xSchema, null);
      return VectorTestXml.Load(xDocument.Descendants("VectorTest").Single(), unitTestAdaptors);
    }

    private static VectorTestXml Load(XElement x, IDictionary<string, AdaptorXml> unitTestAdaptors = null)
    {
      return new VectorTestXml
      {
        Type = x.Attribute("type").Value,
        DilType = Int32.Parse(x.Attribute("dilType").Value),
        Placement = Int32.Parse(x.Attribute("placement").Value),
        VccLevel = Double.Parse(x.Attribute("Vcc").Value, CultureInfo.InvariantCulture),
        Vectors = ToStepArray(x),
        Notes = x.Element("Notes") != null ? x.Element("Notes").Value : null,
        Description = x.Element("Description") != null ? x.Element("Description").Value : null
      };
    }

    private static List<Vector> ToStepArray(XElement x)
    {
      List<Vector> returnValue = new List<Vector>();
      foreach(var stepString in x.Element("Steps").Value.Split(' ').Where(y => !String.IsNullOrEmpty(y.Trim())))
      {
        var step = new Vector();
        returnValue.Add(step);
        int pinNumber = 1;
        foreach(var vectorValue in stepString.Trim().AsVectorValues())
        {
          switch(vectorValue)
          {
            case VectorValues.DontCare:
              step.DontCares.Add(new Pin{ Number = pinNumber++ });
              break;

            case VectorValues.Gnd:
              step.GndPins.Add(new Pin{ Number = pinNumber++ });
              break;

            case VectorValues.Vcc:
              step.VccPins.Add(new Pin{ Number = pinNumber++ });
              break;

            case VectorValues.High:
            case VectorValues.Low:
              step.OutputPins.Add(new Pin{ Number = pinNumber++, EnableLow = vectorValue == VectorValues.Low });
              break;

            case VectorValues.One:
            case VectorValues.Zero:
              step.InputPins.Add(new Pin{ Number = pinNumber++, EnableLow = vectorValue == VectorValues.Zero });
              break;
          }
        }
      }
      return returnValue;
    }
  }
}
