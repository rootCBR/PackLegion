using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using PackLegion.App.XML;

namespace PackLegion
{
    static class Config
    {
        public static bool Option_Combine;
        public static bool Option_Original;
        public static string InputFolder;
        public static string InputFatOriginal;
        public static string InputFatCommon;
        public static string OutputFat;

        public static IEnumerable<ArgumentInfo> Arguments
        {
            get { return _args; }
        }

        private static IEnumerable<ArgumentInfo> _args;

        static readonly string[] _types = {"dev", "rel"};

        public static readonly int BuildType =
#if (!DEBUG)
            1;
#else
            0;
#endif

        /*
            v1.10 09.12.2020 20:27
        */
        public static readonly string BuildVersion = "1.10";

        public static string VersionString
        {
            get { return $"===== PackLegion v{BuildVersion.ToString()}-{_types[BuildType]} ====="; }
        }

        static readonly string[] _usage = {
            "Arguments: [options] <inputFolder> <outputFat> [o:inputFatPatch] [c:inputFatCommon]",
            "",
            "Options:",
            "  -o|original      Use the game's original patch archive as a base for the output archive",
            "                   instead of the output archive itself.",
            "  -c|combine       Automatically combine your modified library files (*.lib) with",
            "                   those that are already contained in the game files.",
            "                   Make sure your modified library files only contain the modified objects.",
            "",
            "Examples:",
            "  PackLegion.exe       \"patch\" \"patch.fat\"",
            "  PackLegion.exe -o    \"patch\" \"patch-n.fat\" \"patch-o.fat\"",
            "  PackLegion.exe -c    \"patch\" \"patch.fat\"                 \"common-o.fat\"",
            "  PackLegion.exe -o -c \"patch\" \"patch-n.fat\" \"patch-o.fat\" \"common-o.fat\"",
        };

        public static string UsageString
        {
            get { return string.Join("\r\n", _usage); }
        }

        public static bool HasArg(string name)
        {
            foreach (var arg in _args)
            {
                if (arg.HasName && (arg.Name == name))
                    return true;
            }

            return false;
        }

        public static string GetArg(string name)
        {
            foreach (var arg in _args)
            {
                if (arg.HasName && (arg.Name == name))
                    return arg.Value;
            }

            return null;
        }

        public static bool GetArg(string name, ref int value)
        {
            int result = 0;

            foreach (var arg in _args)
            {
                if (arg.HasName && (arg.Name == name))
                {
                    if (int.TryParse(arg.Value, out result))
                    {
                        value = result;
                        return true;
                    }
                }
            }

            return false;
        }

        public static int ProcessArgs(string[] args)
        {
            var optionCount = 0;
            var arguments = new List<ArgumentInfo>();

            for (int i = 0; i < args.Length; i++)
            {
                ArgumentInfo arg = new ArgumentInfo(args[i]);

                if (arg.HasName)
                {
                    if (arg.IsSwitch)
                    {
                        switch (arg.Name)
                        {
                            case "c":
                            case "combine":
                                Option_Combine = true;
                                optionCount++;
                                break;
                            case "o":
                            case "original":
                                Option_Original = true;
                                optionCount++;
                                break;
                        }
                    }
                }
                else
                {
                    if (arg.IsValue)
                    {
                        switch (i - optionCount)
                        {
                            // PackLegion.exe -c    "patch" "patch.fat"                    "common.fat"
                            // PackLegion.exe -o    "patch" "patch-n.fat" "orig\patch.fat"
                            // PackLegion.exe -o -c "patch" "patch-n.fat" "orig\patch.fat" "common.fat"
                            case 0:
                                InputFolder = arg.Value;
                                break;
                            case 1:
                                OutputFat = arg.Value;
                                break;
                            case 2:
                                if (Option_Original)
                                {
                                    InputFatOriginal = arg.Value;
                                }
                                else
                                {
                                    InputFatCommon = arg.Value;
                                }
                                break;
                            case 3:
                                InputFatCommon = arg.Value;
                                break;
                        }
                    }
                }

                arguments.Add(arg);
            }

            _args = arguments.AsEnumerable();

            return arguments.Count;
        }

        public static void Initialize(string dir)
        {
            var xmlFile = Path.Combine(dir, "config.xml");

            if (!File.Exists(xmlFile))
            {
                Utility.Log.ToConsole("Config file does not exist");

                return;
            }

            string xmlFileContent = File.ReadAllText(xmlFile);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlFileContent);

            XmlSerializer serializer = new XmlSerializer(typeof(XmlConfig.Config));

            using (XmlReader reader = new XmlNodeReader(doc))
            {
                XmlConfig.Config config = new XmlConfig.Config();

                config = (XmlConfig.Config) serializer.Deserialize(reader);

                string commonPath = config.OriginalCommonPath;
                string patchPath = config.OriginalPatchPath;

                if (!string.IsNullOrEmpty(commonPath) && string.IsNullOrEmpty(InputFatCommon))
                {
                    InputFatCommon = commonPath;
                }

                if (!string.IsNullOrEmpty(patchPath) && string.IsNullOrEmpty(InputFatOriginal))
                {
                    InputFatOriginal = patchPath;
                }
            }
        }
    }
}
