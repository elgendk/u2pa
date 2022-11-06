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
  /// Progress bar.
  /// </summary>
  public class ProgressBar : IProgressBar
  {
    private IShouter shouter;
    private int size;
    private int stride;
    private int accCount;
    private int totalCount;
    private string currentBar = "";
    private int savedVerbosity;
    private int savedCursorTop;
    private int savedCursorLeft;
    private int verticalPlacement = 0;
    private bool initialized = false;

    /// <summary>
    /// ctor.
    /// </summary>
    /// <param name="shouter">Public shouter instance.</param>
    public ProgressBar(IShouter shouter)
    {
      this.shouter = shouter;
      savedVerbosity = shouter.VerbosityLevel;
    }

    /// <summary>
    /// Initiallizes the proressbar.
    /// <remarks>
    /// Can be called multiple times.
    /// </remarks>
    /// </summary>
    /// <param name="size">The total number of progresses before finished.</param>
    public void Init(int size)
    {
      this.size = size;
      stride = size / 64;
      if (initialized || savedVerbosity == 0)
        return;
      // Shut all others up };-)
      shouter.VerbosityLevel = -1;
      savedCursorTop = Console.CursorTop;
      savedCursorLeft = Console.CursorLeft;
      initialized = true;
      verticalPlacement = Math.Max(0, savedCursorTop - 35) + 2;
      SetCursor(verticalPlacement - 2);
      Console.WriteLine("".PadRight(75));
      Console.WriteLine("+ ------------- + ------------- + ------------- + ------------- +".PadRight(75));
      currentBar = "|               |               |               |               |".PadRight(75);
      Console.Write(String.Format(
        "\r{0}{1}{2}{3}{4}{5}{6}",
        currentBar,
        Environment.NewLine,
        "+ ------------- + ------------- + ------------- + ------------- +".PadRight(75),
        Environment.NewLine,
        "(messages will be displayed here)".PadRight(75),
        Environment.NewLine,
        "".PadRight(75)));
    }

    /// <summary>
    /// Disposes the progress bar.
    /// </summary>
    public void Dispose()
    {
      if (!initialized) return;
      SetCursor(verticalPlacement);
      Console.Write(
        "\r{0}{1}{2}",
        "|===============================================================|".PadRight(75),
        Environment.NewLine,
        Environment.NewLine);
      Console.CursorLeft = savedCursorLeft;
      Console.CursorTop = savedCursorTop;
      if (shouter.VerbosityLevel == -1)
        shouter.VerbosityLevel = savedVerbosity;
    }

    /// <summary>
    /// Progress oe step.
    /// </summary>
    public void Progress()
    {
      if (!initialized) return;

      if (savedVerbosity < 2) return;

      if (totalCount >= size || accCount < stride)
      {
        accCount++;
        totalCount++;
        return;
      }
      accCount = 0;
      var accBar = "|";
      for (var i = 1; i <= 64; i++)
      {
        if ((i + 1) * stride == totalCount && (i + 1) % 16 == 0)
        {
          accBar += ">)";
          i++;
          continue;
        }
        if (i * stride == totalCount)
        {
          accBar += ">";
          continue;
        }
        if ((i - 1) * stride < totalCount)
        {
          accBar += "=";
          continue;
        }
        accBar += i % 16 == 0 ? ("|") : " ";
      }
      currentBar = accBar.PadRight(75);
      SetCursor(verticalPlacement);
      Console.Write(String.Format("\r{0}" + Environment.NewLine, currentBar));
      accCount++;
      totalCount++;
    }

    /// <summary>
    /// Writes a message at the end of the progress bar.
    /// </summary>
    /// <param name="message">The message (may contain String.Format mark-ups).</param>
    /// <param name="obj">Parameters to the message.</param>
    public void Shout(string message, params object[] obj)
    {
      var m = String.Format(message, obj);
      SetCursor(verticalPlacement);
      Console.Write(String.Format(
        "\r{0}{1}{2}{3}{4}",
        currentBar,
        Environment.NewLine,
        "+ ------------- + ------------- + ------------- + ------------- +".PadRight(75),
        Environment.NewLine,
        m.PadRight(75)));
    }

    private void SetCursor(int verticalPlacement)
    {
      Console.SetCursorPosition(0, verticalPlacement);
    }
  }
}
