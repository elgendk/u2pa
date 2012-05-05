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

using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;

namespace U2Pa.Lib
{
  /// <summary>
  /// Wraps the values defined in the file Config.xml.
  /// </summary>
  public static class Config
  {
    /// <summary>
    /// Static ctor.
    /// </summary>
    static Config()
    {
      var xDocument = XDocument.Load(@"Xml\Config.xml");
      var xSchema = new XmlSchemaSet();
      xSchema.Add("", @"Xml\Config.xsd");
      xDocument.Validate(xSchema, null);
      ICTestBinPath = xDocument
        .Descendants("Config")
        .Single()
        .Element("Files")
        .Element("ICTestBinPath")
        .Attribute("path")
        .Value;
    }

    /// <summary>
    /// Path to the "bit-bashing" FPGA-program ictest.bin
    /// that comes with the software TopWin6.
    /// </summary>
    public static string ICTestBinPath { get; private set; }
  }
}