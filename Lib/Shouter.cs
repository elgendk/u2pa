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

namespace U2Pa.Lib
{
  /// <summary>
  /// A class that wraps output from the app.
  /// </summary>
  public class Shouter : IShouter
  {
    /// <summary>
    /// Verbosity.
    /// </summary>
    public int VerbosityLevel { get; set; }

    /// <summary>
    /// ctor.
    /// </summary>
    /// <param name="verbosityLevel">Verbosity.</param>
    public Shouter(int verbosityLevel)
    {
      VerbosityLevel = verbosityLevel;
    }

    /// <summary>
    /// Outputs a line.
    /// </summary>
    /// <param name="verbosity">Verbosity.</param>
    /// <param name="message">The message (may contain String.Format mark-ups).</param>
    /// <param name="obj">Parameters to the message.</param>
    public void ShoutLine(int verbosity, string message, params object[] obj)
    {
      if (verbosity <= VerbosityLevel)
        Console.WriteLine((VerbosityLevel == 5 ? "V" + verbosity + ": " : "") + message, obj);
    }
  }
}
