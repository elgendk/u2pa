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
  public class PublicAddress
  {
    /// <summary>
    /// Verbosity.
    /// </summary>
    public int VerbosityLevel { get; set; }

    /// <summary>
    /// ctor.
    /// </summary>
    /// <param name="verbosityLevel">Verbosity.</param>
    public PublicAddress(int verbosityLevel)
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

    /// <summary>
    /// Creates a new instance of the <see cref="ProgressBar"/>.
    /// </summary>
    /// <param name="size">The total number of progresses before finished.</param>
    /// <returns>The created <see cref="ProgressBar"/>.</returns>
    public ProgressBar GetProgressBar(int size)
    {
      return new ProgressBar(this, size);
    }

    /// <summary>
    /// Progress bar.
    /// </summary>
    public class ProgressBar : IDisposable
    {
      private PublicAddress pa;
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
      /// <param name="pa">Public addresser.</param>
      /// <param name="size">The total number of progresses before finished.</param>
      public ProgressBar(PublicAddress pa, int size)
      {
        this.pa = pa;
        this.size = size;
        stride = size/64;
        savedVerbosity = pa.VerbosityLevel;
      }

      /// <summary>
      /// Initiallizes the proressbar.
      /// <remarks>
      /// Can be called multiple times.
      /// </remarks>
      /// </summary>
      public void Init()
      {
        if (initialized || savedVerbosity == 0)
          return;
        // Shut all others up };-)
        pa.VerbosityLevel = -1;
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
        if (pa.VerbosityLevel == -1)
          pa.VerbosityLevel = savedVerbosity;
      }

      /// <summary>
      /// Progress oe step.
      /// </summary>
      public void Progress()
      {
        if (!initialized) return;

        if (savedVerbosity < 2) return;

        if(totalCount >= size || accCount < stride)
        {
          accCount++;
          totalCount++;
          return;
        }
        accCount = 0;
        var accBar = "|";
        for(var i = 1; i <= 64; i++)
        {
          if ((i+1) * stride == totalCount && (i+1)%16 == 0)
          {
            accBar += ">)";
            i++;
            continue;
          }
          if(i * stride == totalCount)
          {
            accBar += ">";
            continue;
          }
          if ((i-1) * stride < totalCount)
          {
            accBar += "=";
            continue;
          }
          accBar += i%16 == 0 ? ("|") : " ";
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
      internal void Shout(string message, params object[] obj)
      {
        var m = String.Format(message, obj);
        SetCursor(verticalPlacement);
        Console.Write(String.Format(
          "\r{0}{1}{2}{3}{4}", 
          currentBar,
          Environment.NewLine,
          "+---------------------------------------------------------------+".PadRight(75),
          Environment.NewLine,
          m.PadRight(75)));
      }
      
      private void SetCursor(int verticalPlacement)
      {
        Console.SetCursorPosition(0, verticalPlacement);        
      }
    }
  }
}