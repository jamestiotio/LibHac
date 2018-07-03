﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace hactoolnet
{
    internal static class CliParser
    {
        private static readonly CliOption[] CliOptions =
        {
            new CliOption("intype", 't', 1, (o, a) => o.InFileType = ParseFileType(a[0])),
            new CliOption("raw", 'r', 0, (o, a) => o.Raw = true),
            new CliOption("keyset", 'k', 1, (o, a) => o.Keyfile = a[0]),
            new CliOption("titlekeys", 1, (o, a) => o.TitleKeyFile = a[0]),
            new CliOption("section0", 1, (o, a) => o.SectionOut[0] = a[0]),
            new CliOption("section1", 1, (o, a) => o.SectionOut[1] = a[0]),
            new CliOption("section2", 1, (o, a) => o.SectionOut[2] = a[0]),
            new CliOption("section3", 1, (o, a) => o.SectionOut[3] = a[0]),
            new CliOption("section0dir", 1, (o, a) => o.SectionOutDir[0] = a[0]),
            new CliOption("section1dir", 1, (o, a) => o.SectionOutDir[1] = a[0]),
            new CliOption("section2dir", 1, (o, a) => o.SectionOutDir[2] = a[0]),
            new CliOption("section3dir", 1, (o, a) => o.SectionOutDir[3] = a[0]),
            new CliOption("exefs", 1, (o, a) => o.ExefsOut = a[0]),
            new CliOption("exefsdir", 1, (o, a) => o.ExefsOutDir = a[0]),
            new CliOption("romfs", 1, (o, a) => o.RomfsOut = a[0]),
            new CliOption("romfsdir", 1, (o, a) => o.RomfsOutDir = a[0]),
            new CliOption("outdir", 1, (o, a) => o.OutDir = a[0]),
            new CliOption("sdseed", 1, (o, a) => o.SdSeed = a[0]),
            new CliOption("sdpath", 1, (o, a) => o.SdPath = a[0]),
            new CliOption("listapps", 0, (o, a) => o.ListApps = true),
            new CliOption("listtitles", 0, (o, a) => o.ListTitles = true),
            new CliOption("listromfs", 0, (o, a) => o.ListRomFs = true),
            new CliOption("title", 1, (o, a) => o.TitleId = ParseTitleId(a[0])),
        };

        public static Options Parse(string[] args)
        {
            var options = new Options();
            var inputSpecified = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg;

                if (args[i].Length == 2 && (args[i][0] == '-' || args[i][0] == '/'))
                {
                    arg = args[i][1].ToString().ToLower();
                }
                else if (args[i].Length > 2 && args[i].Substring(0, 2) == "--")
                {
                    arg = args[i].Substring(2).ToLower();
                }
                else
                {
                    if (inputSpecified)
                    {
                        PrintWithUsage($"Unable to parse option {args[i]}");
                        return null;
                    }

                    options.InFile = args[i];
                    inputSpecified = true;
                    continue;
                }

                var option = CliOptions.FirstOrDefault(x => x.Long == arg || x.Short == arg);
                if (option == null)
                {
                    PrintWithUsage($"Unknown option {args[i]}");
                    return null;
                }

                if (i + option.ArgsNeeded >= args.Length)
                {
                    PrintWithUsage($"Need {option.ArgsNeeded} parameter{(option.ArgsNeeded == 1 ? "" : "s")} after {args[i]}");
                    return null;
                }

                var optionArgs = new string[option.ArgsNeeded];
                Array.Copy(args, i + 1, optionArgs, 0, option.ArgsNeeded);

                option.Assigner(options, optionArgs);
                i += option.ArgsNeeded;
            }

            if (!inputSpecified)
            {
                PrintWithUsage("Input file must be specified");
                return null;
            }

            return options;
        }

        private static FileType ParseFileType(string input)
        {
            if (!Enum.TryParse(input, true, out FileType type))
            {
                PrintWithUsage("Specified type is invalid.");
            }

            return type;
        }

        private static ulong ParseTitleId(string input)
        {
            if (input.Length != 16)
            {
                PrintWithUsage("Title ID must be 16 hex characters long");
            }

            if (!ulong.TryParse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id))
            {
                PrintWithUsage("Could not parse title ID");
            }

            return id;
        }

        private static void PrintWithUsage(string toPrint)
        {
            Console.WriteLine(toPrint);
            Console.WriteLine(GetUsage());
            // PrintUsage();
        }

        private static string GetUsage()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Usage: hactoolnet.exe [options...] <path>");
            sb.AppendLine("Options:");
            sb.AppendLine("  -r, --raw            Keep raw data, don\'t unpack.");
            sb.AppendLine("  -k, --keyset         Load keys from an external file.");
            sb.AppendLine("  -t, --intype=type    Specify input file type [nca, switchfs]");
            sb.AppendLine("  --titlekeys <file>   Load title keys from an external file.");
            sb.AppendLine("NCA options:");
            sb.AppendLine("  --section0 <file>    Specify Section 0 file path.");
            sb.AppendLine("  --section1 <file>    Specify Section 1 file path.");
            sb.AppendLine("  --section2 <file>    Specify Section 2 file path.");
            sb.AppendLine("  --section3 <file>    Specify Section 3 file path.");
            sb.AppendLine("  --section0dir <dir>  Specify Section 0 directory path.");
            sb.AppendLine("  --section1dir <dir>  Specify Section 1 directory path.");
            sb.AppendLine("  --section2dir <dir>  Specify Section 2 directory path.");
            sb.AppendLine("  --section3dir <dir>  Specify Section 3 directory path.");
            sb.AppendLine("  --listromfs          List files in RomFS.");
            sb.AppendLine("Switch FS options:");
            sb.AppendLine("  --sdseed <seed>      Set console unique seed for SD card NAX0 encryption.");
            sb.AppendLine("  --listapps           List application info.");
            sb.AppendLine("  --listtitles         List title info for all titles.");
            sb.AppendLine("  --title <title id>   Specify title ID to use.");
            sb.AppendLine("  --romfsdir <dir>     Specify RomFS directory path.");

            return sb.ToString();
        }

        private class CliOption
        {
            public CliOption(string longName, char shortName, int argsNeeded, Action<Options, string[]> assigner)
            {
                Long = longName;
                Short = shortName.ToString();
                ArgsNeeded = argsNeeded;
                Assigner = assigner;
            }
            public CliOption(string longName, int argsNeeded, Action<Options, string[]> assigner)
            {
                Long = longName;
                ArgsNeeded = argsNeeded;
                Assigner = assigner;
            }

            public string Long { get; }
            public string Short { get; }
            public int ArgsNeeded { get; }
            public Action<Options, string[]> Assigner { get; }
        }
    }
}