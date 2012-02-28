using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using U2Pa.Lib;

namespace U2Pa.Cmd
{
  internal static class U2PaCmd
  {
    private static IDictionary<string, string> helpTexts = new Dictionary<string, string>(); 
    static U2PaCmd()
    {
      foreach (var fileName in Directory.GetFiles("help", "*.txt"))
      {
        helpTexts.Add(
          Path.GetFileNameWithoutExtension(fileName), 
          File.ReadAllText(fileName));
      }
    }

    // Verbosity:
    // 0 totally silent
    // 1 only display fatal errors ie exceptions resulting in abort
    // 2 startup message and few messages
    // 3 ...
    // 4 ...
    // 5 all messages

    /// <summary>
    /// Main entry point for the whole application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    public static int Main(string[] args)
    {
      var timestamp = DateTime.Now;
      var pa = new PublicAddress(3);
      try
      {
        if (args.Length == 0 || args[0] == "help")
        {
          return Help(pa, args);
        }

        for (var i = 0; i < args.Length; i++)
        {
          if (args[i] != "--verbosity" && args[i] != "-v") continue;
          pa.VerbosityLevel = Int32.Parse(args[i + 1]);
          break;
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
            returnCode = Rom(pa, args);
            break;

          case "sram":
            returnCode = SRam(pa, args);
            break;

          case "prog":
            returnCode = Prog(pa, args);
            break;

          case "help":
            returnCode = Help(pa, args);
            break;

          case "dev":
            returnCode = Dev(pa, args);
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

    /// <summary>
    /// Entry point for the 'rom' catagory of commands.
    /// </summary>
    /// <param name="pa">Public addresser</param>
    /// <param name="args">Command line arguments</param>
    /// <returns></returns>
    private static int Rom(PublicAddress pa, string[] args)
    {
      switch (args[1])
      {
        case "id":
          pa.ShoutLine(1, "rom id not yet implemented!");
          return 1;

        case "info":
          return Kernel.RomInfo(pa, args[2]);

        case "read":
          var data = Kernel.RomRead(pa, args[2]);
          Tools.WriteBinaryFile(args[3], data);
          pa.ShoutLine(2, "EPROM{0} data written to file {1}", args[2], args[3]);
          return 0;
      
        case "write":
          var fileData = Tools.ReadBinaryFile(args[3]).ToArray();
          Kernel.RomWrite(pa, args[2], fileData);
          pa.ShoutLine(2, "Filedata from {0} written to EPROM", args[2], args[3]);
          return 0;

        case "verify":
          fileData = Tools.ReadBinaryFile(args[3]).ToArray();
          var didntVerify = Kernel.RomVerify(pa, args[2], fileData).ToArray();
          if(didntVerify.Any())
          {
            foreach (var tuple in didntVerify)
            {
              pa.ShoutLine(
                3,
                "Address {0} didn't verify. In file {1}, in EPROM {2}. Can{3} be patched",
                tuple.Item1.ToString("X4"),
                tuple.Item2.ToString("X2"),
                tuple.Item3.ToString("X2"),
                Tools.CanBePatched(tuple.Item2, tuple.Item3) ? "" : "'t");
            }
          }
          if(didntVerify.Length == 0)
            pa.ShoutLine(2, "EPROM verifies nicely }};-)");
          return 0;

        default:
          pa.ShoutLine(1, "Unknown rom command {0}", args[1]);
          return 1;
      }
    }


    /// <summary>
    /// Entry point for the 'sram' catagory of commands.
    /// </summary>
    /// <param name="pa">Public addresser</param>
    /// <param name="args">Command line argumsnts</param>
    /// <returns></returns>
    private static int SRam(PublicAddress pa, string[] args)
    {
      pa.ShoutLine(1, "category sram not yet implemented!");
      return 1;
    }

    /// <summary>
    /// Entry point for the 'prog' catagory of commands.
    /// </summary>
    /// <param name="pa">Public addresser</param>
    /// <param name="args">Command line argumsnts</param>
    /// <returns></returns>
    private static int Prog(PublicAddress pa, string[] args)
    {
      switch (args[1])
      {
        case "id":
          return Kernel.ProgId(pa);
      }
      pa.ShoutLine(1, "category prog not yet implemented!");
      return 1;
    }

    /// <summary>
    /// Entry point for the 'help' catagory of commands.
    /// </summary>
    /// <param name="pa">Public addresser</param>
    /// <param name="args">Command line argumsnts</param>
    /// <returns></returns>
    private static int Help(PublicAddress pa, string[] args)
    {
      if (args.Length <= 1)
      {
        Console.Write(helpTexts["main"]);
        return 0;
      }

      if(args.Length <= 2)
      {
        Console.Write(helpTexts[args[1]]);
        return 0;
      }

      if(args.Length <= 3)
      {
        Console.Write(helpTexts[args[1] + "_" + args[2]]);
        return 0;
      }
      return 0;
    }

    /// <summary>
    /// Entry point for the 'ram' catagory of commands.
    /// </summary>
    /// <param name="pa">Public addresser</param>
    /// <param name="args">Command line argumsnts</param>
    /// <returns></returns>
    private static int Dev(PublicAddress pa, string[] args)
    {
      return Kernel.Dev(pa, args);
    }
  }
}
