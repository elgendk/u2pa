using System;
using U2Pa.Lib;

namespace U2Pa.Cmd
{
  internal static class U2PaCmd
  {
    #region Help Texts
    private const string mainHelp =
      @"USAGE: u2pa <category> <command> [arguments] [options...]

Categories are:
  help         displays detailed help for a category
  prog         commands related to the Top Programmer device itself
  ram          commands related to SRAM ICs
  rom          commands related to roms (that is ROM/PROM/EPROM/EEPROM...)

General options:
 -v   --verbosity   Verbosity; i must be in the range [0,..,5] default is 3; the higher i, the more crap on screen
";

    private const string romHelp =
      @"u2pa rom <command> [arguments] [options]

alias: [NONE] (yet? };-P)

all commands related to roms (that is ROM/PROM/EPROM/EEPROM...)

arguments:
  id          reads the id string of the EPROM and displays it on screen
  info        displays on screen an ASCII representation of the ERPOM inserted in to the Top
  read        reads the contents of an EPROM to a file 
  write       writes the contents of a file to an EPROM
";

    private const string progHelp = "";

    #endregion Help Texts

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
            returnCode = Rom(pa, args);
            break;

          case "ram":
            returnCode = Ram(pa, args);
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
        case "read":
          return Kernel.RomRead(pa, args[2], args[3]);
          break;

        case "write":
          return Kernel.RomWrite(pa, args[2], args[3]);
          break;

        case "info":
          return Kernel.RomInfo(pa, args[2]);
          break;

        case "id":
          pa.ShoutLine(1, "rom id not yet implemented!");
          return 1;
          break;

        default:
          pa.ShoutLine(1, "Unknown rom command {0}", args[1]);
          return 1;
      }
    }

    /// <summary>
    /// Entry point for the 'ram' catagory of commands.
    /// </summary>
    /// <param name="pa">Public addresser</param>
    /// <param name="args">Command line argumsnts</param>
    /// <returns></returns>
    private static int Ram(PublicAddress pa, string[] args)
    {
      pa.ShoutLine(1, "category ram not yet implemented!");
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
      if (args.Length <= 1)
      {
        Console.Write((string) progHelp);
        return 0;
      }

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
        Console.Write((string) mainHelp);
        return 0;
      }

      switch (args[1])
      {
        case "rom":
          Console.Write((string) romHelp);
          break;

        default:
          Console.WriteLine("No detailed help for category {0} found, sorry!", args[1]);
          break;
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
