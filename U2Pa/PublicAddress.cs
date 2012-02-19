using System;

namespace U2Pa
{
  public class PublicAddress
  {
    internal int VerbosityLevel { get; set; }

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
  }
}