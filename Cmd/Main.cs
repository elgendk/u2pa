using System;
using U2Pa.Lib;

namespace U2Pa.Cmd
{
  internal static class U2PaCmd
  {
    // Verbosity:
    // 0 totally silent
    // 1 only display fatal errors ie exceptions resulting in abort
    // 2 startup message and few messages
    // 3 ...
    // 4 ...
    // 5 all messages

    public static int Main(string[] args)
    {
      var timestamp = DateTime.Now;
      var pa = new PublicAddress(3);
      try
      {
        if (args.Length == 0 || args[0] == "help")
        {
          return Kernel.Help(pa, args);
        }

        for (var i = 0; i < args.Length; i++)
        {
          if (args[i] == "--verbosity" || args[i] == "-v")
          {
            pa.VerbosityLevel = Int32.Parse(args[i + 1]);
            break;
          }
        }

        pa.ShoutLine(2, "************* U2Pa (C) Elgen 2012 }};-P ***************");
        pa.ShoutLine(2, "* Alternative software for Top Universal Programmers *");
        pa.ShoutLine(2, "******************************************************");
        pa.ShoutLine(2, "Verbosity level: {0}", pa.VerbosityLevel);
        pa.ShoutLine(2, "U2Pa initiated at {0}", timestamp);

        var returnCode = 0;
        switch (args[0])
        {
          case "rom":
            returnCode = Kernel.Rom(pa, args);
            break;

          case "ram":
            returnCode = Kernel.Ram(pa, args);
            break;

          case "prog":
            returnCode = Kernel.Prog(pa, args);
            break;

          case "help":
            returnCode = Kernel.Help(pa, args);
            break;

          case "dev":
            returnCode = Kernel.Dev(pa, args);
            break;
        }

        pa.ShoutLine(2, "Elapsed time: {0}", DateTime.Now - timestamp);

        return returnCode;
      }
      catch (Exception e)
      {
        pa.ShoutLine(1, "Fatal error: {0}", e.Message);
        pa.ShoutLine(5, "Exception:\n{0}", e);
        return 1;
      }
    }
  }
}
