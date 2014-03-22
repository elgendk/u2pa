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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using U2Pa.Lib;

namespace U2Pa.Cmd
{
  /// <summary>
  /// The main command interpreter class.
  /// </summary>
  internal static class U2PaCmd
  {
    private static IDictionary<string, string> helpTexts;
    private static IDictionary<string, string> HelpTexts
    {
      get
      { 
        if(helpTexts == null)
        {
          helpTexts = new Dictionary<string, string>();
          foreach (var fileName in Directory.GetFiles(Tools.GetSubDir("Help"), "*.txt"))
          {
            using(var file = File.OpenText(fileName))
            {
              while(!file.EndOfStream)
                if(file.ReadLine().StartsWith("~~~"))
                  break;
         
              helpTexts.Add(
                Path.GetFileNameWithoutExtension(fileName),
                file.ReadToEnd());
            }
          }
        }
        return helpTexts;
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
      var shouter = new Shouter(3);
      try
      {
        var cleanedArgs = new List<string>();
        var options = new Dictionary<string, string>();
        for (var i = 0; i < args.Length; i++)
        {
          if (args[i].StartsWith("-"))
          {
            if (i == args.Length - 1 || args[i + 1].StartsWith("-"))
              throw new U2PaException("Syntax error: option {0} provided without an argument.", args[i]);
            options.Add(args[i].TrimStart('-'), args[i + 1]);
            i++;
          }
          else
            cleanedArgs.Add(args[i]);
        }

        if (cleanedArgs.Count == 0 || cleanedArgs[0] == "help")
        {
          return Help(shouter, cleanedArgs);
        }

        if (options.ContainsKey("v"))
          shouter.VerbosityLevel = Int32.Parse(options["v"]);
        else if (options.ContainsKey("verbosity"))
          shouter.VerbosityLevel = Int32.Parse(options["verbosity"]);

        shouter.ShoutLine(2, "************* U2Pa (C) Elgen 2012 }};-P ***************");
        shouter.ShoutLine(2, "* Alternative software for Top Universal Programmers *");
        shouter.ShoutLine(2, "******************************************************");
        shouter.ShoutLine(2, "Verbosity level: {0}", shouter.VerbosityLevel);
        shouter.ShoutLine(2, "U2Pa initiated at {0}", timestamp);

        var returnCode = 0;
        switch (cleanedArgs[0])
        {
          case "rom":
            returnCode = Rom(shouter, cleanedArgs);
            break;

          case "sram":
            returnCode = SRam(shouter, cleanedArgs);
            break;

          case "prog":
            returnCode = Prog(shouter, cleanedArgs);
            break;

          case "bdump":
            returnCode = BDump(shouter, cleanedArgs);
            break;

          case "help":
            returnCode = Help(shouter, cleanedArgs);
            break;

          case "dev":
            returnCode = Dev(shouter, cleanedArgs);
            break;
        }

        shouter.ShoutLine(2, "Elapsed time: {0}", DateTime.Now - timestamp);

        return returnCode;
      }
      catch (Exception e)
      {
        shouter.ShoutLine(1, "Fatal error: {0}", e.Message);
        shouter.ShoutLine(5, "Exception:\n{0}", e);
        return 1;
      }
    }

    /// <summary>
    /// Entry point for the 'rom' catagory of commands.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    private static int Rom(IShouter shouter, IList<string> args)
    {
      if(args.Count == 1)
      {
        args.Insert(0, "help");
        Help(shouter, args);
        return 0;
      }

      switch (args[1])
      {
        case "all":
          var option = args.Count == 3 ? args[2] : null;
          return Kernel.RomAll(shouter, option);

        case "id":
          shouter.ShoutLine(1, "rom id not yet implemented!");
          return 1;

        case "info":
          return Kernel.RomInfo(shouter, args[2]);

        case "read":
          var data = Kernel.RomRead(shouter, args[2]);
          Tools.WriteBinaryFile(args[3], data);
          shouter.ShoutLine(2, "EPROM{0} data written to file {1}", args[2], args[3]);
          return 0;

        case "verify":
          var fileData = Tools.ReadBinaryFile(args[3]).ToArray();
          var didntVerify = Kernel.RomVerify(shouter, args[2], fileData).ToArray();
          if (didntVerify.Any())
          {
            foreach (var tuple in didntVerify)
            {
              shouter.ShoutLine(
                3,
                "Address {0} didn't verify. In file {1}, in EPROM {2}. Can{3} be patched",
                tuple.Item1.ToString("X4"),
                tuple.Item2.ToString("X2"),
                tuple.Item3.ToString("X2"),
                Tools.CanBePatched(tuple.Item2, tuple.Item3) ? "" : "'t");
            }
          }
          if (didntVerify.Length == 0)
            shouter.ShoutLine(2, "EPROM verifies nicely }};-)");
          return 0;

        case "write":
          fileData = Tools.ReadBinaryFile(args[3]).ToArray();
          Kernel.RomWrite(shouter, args[2], fileData);
          shouter.ShoutLine(2, "Filedata from {0} written to EPROM", args[2], args[3]);
          return 0;

        default:
          shouter.ShoutLine(1, "Unknown rom command {0}", args[1]);
          return 1;
      }
    }

    /// <summary>
    /// Entry point for the 'sram' catagory of commands.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="args">Command line argumsnts</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    private static int SRam(IShouter shouter, IList<string> args)
    {
      if (args.Count == 1)
      {
        args.Insert(0, "help");
        Help(shouter, args);
        return 0;
      }

      switch (args[1])
      {
        case "all":
          var option = args.Count == 3 ? args[2] : null;
          return Kernel.SRamAll(shouter, option);

        case "test":
          return Kernel.SRamTest(shouter, args[2]);

        case "info":
          return Kernel.SRamInfo(shouter, args[2]);

        default:
          shouter.ShoutLine(1, "Unknown sram command {0}", args[1]);
          return 1;
      }
    }

    /// <summary>
    /// Entry point for the 'prog' category of commands.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="args">Command line argumsnts</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    private static int Prog(IShouter shouter, IList<string> args)
    {
      if (args.Count == 1)
      {
        args.Insert(0, "help");
        Help(shouter, args);
        return 0;
      }
      
      switch (args[1])
      {
        case "id":
          Kernel.ProgId(shouter);
          return 0;
        
        case "stat":
          Kernel.ProgStat(shouter);
          return 0;

        default:
          shouter.ShoutLine(1, "Unknown prog command {0}", args[1]);
          return 1;
      }
    }

    /// <summary>
    /// Entry point for the 'bdump' catagory of commands.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="args">Command line argumsnts</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    private static int BDump(IShouter shouter, IList<string> args)
    {
      if (args.Count < 6)
      {
        args.Insert(0, "help");
        Help(shouter, args);
        return 0;
      }

      var numberOfOutputs = Int32.Parse(args[3]);
      var numberOfInputs = Int32.Parse(args[4]);

      switch (args[1])
      {
        case "process":
          Kernel.BDumpProcess(shouter, args[2], numberOfOutputs, numberOfInputs, args[5]);
          return 0;
        default:
          shouter.ShoutLine(1, "Unknown bdump command {0}", args[1]);
          return 1;
      }
    }

    /// <summary>
    /// Entry point for the 'help' catagory of commands.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    private static int Help(IShouter shouter, IList<string> args)
    {
      if (args.Count <= 1)
      {
        Console.Write(HelpTexts["main"]);
        return 0;
      }

      if (args.Count <= 2)
      {
        if (!HelpTexts.ContainsKey(args[1]))
          throw new U2PaException("No help entry for {0}", args[1]);

        Console.Write(HelpTexts[args[1]]);
        return 0;
      }

      var arg = args[1] + "_" + args[2];
      if (!HelpTexts.ContainsKey(arg))
        throw new U2PaException("No help entry for {0}", arg);
    
      Console.Write(HelpTexts[arg]);
      return 0;
    }

    /// <summary>
    /// Entry point for the 'dev' catagory of commands.
    /// </summary>
    /// <param name="shouter">The shouter instance.</param>
    /// <param name="args">Command line argumsnts</param>
    /// <returns>Exit code. 0 is fine; all other is bad.</returns>
    private static int Dev(IShouter shouter, IList<string> args)
    {
      shouter.VerbosityLevel = 5;
      return Kernel.Dev(shouter);
      //return Kernel.DevVppLevels(pa);
      //return Kernel.DevVccLevels(pa);
      //return Kernel.DevVppPins(pa);
      //return Kernel.DevVccPins(pa);
      //return Kernel.DevGndPins(pa);
    }
  }
}
