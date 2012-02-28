using System;

namespace U2Pa.Lib
{
  public class PublicAddress
  {
    public int VerbosityLevel { get; set; }

    public PublicAddress(int verbosityLevel)
    {
      VerbosityLevel = verbosityLevel;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="verbosity"></param>
    /// <param name="message"></param>
    /// <param name="obj"></param>
    public void ShoutLine(int verbosity, string message, params object[] obj)
    {
      if (verbosity <= VerbosityLevel)
        Console.WriteLine((VerbosityLevel == 5 ? "V" + verbosity + ": " : "") + message, obj);
    }

    public ProgressBar GetProgressBar(int size)
    {
      return new ProgressBar(this, size);
    }

    public class ProgressBar : IDisposable
    {
      private PublicAddress pa;
      private int size;
      private int stride;
      private int accCount;
      private int totalCount;
      private string currentBar = "";
      private int savedVerbosity;
      private bool initialized = false;

      public ProgressBar(PublicAddress pa, int size)
      {
        this.pa = pa;
        this.size = size;
        stride = size/64;
        savedVerbosity = pa.VerbosityLevel;
      }

      public void Init()
      {
        if (initialized)
          return;
        // Shut all others up };-)
        pa.VerbosityLevel = -1;
        initialized = true;
        Console.WriteLine();
        Console.WriteLine("0 _____________ 1 _____________ 2 _____________ 3 _____________ 4");
        currentBar = "|...............|...............|...............|...............|";
        Console.Write(String.Format("\r{0} {1}", currentBar, "(messages will be displayed here)").PadRight(120));
      }

      public void Dispose()
      {
        if (!initialized) return;
        Console.Write("\r{0}", "|===============================================================|");
        Console.WriteLine();
        Console.WriteLine();
        if (pa.VerbosityLevel == -1)
          pa.VerbosityLevel = savedVerbosity;
      }

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
          accBar += i%16 == 0 ? ("|") : ".";
        }
        currentBar = accBar;
        Console.Write(String.Format("\r{0}", currentBar).PadRight(120));
        accCount++;
        totalCount++;
      }

      internal void Shout(string message, params object[] obj)
      {
        var m = String.Format(message, obj);
        Console.Write(String.Format("\r{0} {1}", currentBar, m).PadRight(120));
      }
    }
  }
}