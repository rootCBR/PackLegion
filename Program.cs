using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using Gibbed.IO;

namespace PackLegion
{
    class Program
    {
        static void Main(string[] args)
        {
            Utility.Log.ToConsole(Config.VersionString);

            // PackLegion.exe "%1" "outputDirectory\%~n1.fat"
            if (args.Length == 0)
            {
                Utility.Log.ToConsole(Config.UsageString);

                Environment.Exit(0);

                /*
                args = new string[]
                {
                    "D:\\Modding\\Disrupt\\Tools\\Repos\\_Test\\PackLegion\\bin\\Debug\\patch",
                    "D:\\Modding\\Disrupt\\Tools\\Repos\\_Test\\PackLegion\\bin\\Debug\\outputDirectory\\patch_new.fat",
                    "D:\\Modding\\Disrupt\\Tools\\Repos\\_Test\\PackLegion\\bin\\Debug\\outputDirectory\\patch.fat"
                };
                */

                /*
                args = new string[]
                {
                    "patch",
                    //"outputDirectory\\patch_new.fat",
                    "outputDirectory\\patch3.fat"
                };
                */

                /*
                args = new string[]
                {
                    "-c",
                    "patch",
                    "outputDirectory\\patch_new.fat",
                    "D:\\Games\\Ubisoft\\Watch Dogs Legion\\data_win64\\patch-o.fat",
                    "D:\\Games\\Ubisoft\\Watch Dogs Legion\\data_win64\\common-o.fat"
                };
                */

                /*
                args = new string[]
                {
                        "-c",
                        "-o",
                        "D:\\Modding\\Disrupt\\WDL\\_patch",
                        "patch.fat",
                };
                */
            }

            if (Config.ProcessArgs(args) > 0)
            {
                Work();
            }
            else
            {
                throw new Exception("Invalid number of arguments");
            }

            /*
            if (args[0] == "-c" || args[0] == "-combine")
            {
                if (args.Length < 4)
                {
                    throw new Exception("Invalid number of arguments");
                }

                CombineMode(args);
            }
            else
            {
                if (args.Length < 2)
                {
                    throw new Exception("Invalid number of arguments");
                }

                NormalMode(args);
            }
            */
        }

        public static void Work()
        {
            Config.Initialize(AppDomain.CurrentDomain.BaseDirectory);

            string inputFolderPath = Path.GetFullPath(Config.InputFolder);
            string outputFatPath = Path.GetFullPath(Config.OutputFat);
            string inputPatchFatPath = !string.IsNullOrEmpty(Config.InputFatOriginal) ? Path.GetFullPath(Config.InputFatOriginal) : string.Empty;
            string inputCommonFatPath = !string.IsNullOrEmpty(Config.InputFatCommon) ? Path.GetFullPath(Config.InputFatCommon) : string.Empty;

            //Utility.Log.ToConsole(string.Format("InputFolder: {0}\nOutputFat: {1}\nInputFatOriginal: {2}\nInputFatCommon: {3}", inputFolderPath, outputFatPath, inputPatchFatPath, inputCommonFatPath));

            string patchFatRead = outputFatPath;
            string commonFatRead = inputCommonFatPath;

            if (Config.Option_Original)
            {
                //Utility.Log.ToConsole("Option: Original");

                patchFatRead = inputPatchFatPath;

                if (string.IsNullOrEmpty(patchFatRead))
                {
                    throw new Exception("Original patch archive is not specified");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(patchFatRead))
                {
                    throw new Exception("Output patch archive is not specified");
                }
            }

            if (Config.Option_Combine)
            {
                //Utility.Log.ToConsole("Option: Combine");

                if (string.IsNullOrEmpty(commonFatRead))
                {
                    throw new FileNotFoundException("Common archive is not specified");
                }
            }

            Fat outputFat = GetFat(patchFatRead);
            Fat commonFat = null;

            // read patch dat
            string patchDatRead = Path.ChangeExtension(patchFatRead, "dat");
            List<DatEntry> patchDatEntries = GetDatEntries(outputFat, patchDatRead);

            string commonDatRead = null;
            List<DatEntry> commonDatEntries = null;

            if (Config.Option_Combine)
            {
                commonFat = GetFat(commonFatRead);

                // read common dat
                commonDatRead = Path.ChangeExtension(commonFatRead, "dat");
                commonDatEntries = GetDatEntries(commonFat, commonDatRead);
            }

            string[] inputFilePaths = Directory.GetFiles(inputFolderPath, "*.*", SearchOption.AllDirectories);

            List<DatEntry> inputDatEntries = new List<DatEntry>();

            foreach (string inputFilePath in inputFilePaths)
            {
                string archiveFilePath = inputFilePath.Replace(inputFolderPath + Path.DirectorySeparatorChar, "");
                ulong archiveFileHash = Values.Hashes.CRC64_WD2.Compute(archiveFilePath);

                if (archiveFilePath.StartsWith("0x"))
                {
                    archiveFileHash = ulong.Parse(archiveFilePath.Substring(2), NumberStyles.HexNumber);
                }

                //Console.WriteLine($"{archiveFilePath} -> {archiveFileHash:X16}");

                inputDatEntries.Add(new DatEntry()
                {
                    hash = archiveFileHash,
                    name = archiveFilePath,
                    content = File.ReadAllBytes(inputFilePath)
                });

                //Console.WriteLine($"Created input entry: {archiveFilePath}");
            }

            if (Config.Option_Combine)
            {
                // 1. gather fcb files to modify
                // 2. retrieve base fcb files from patch
                // 3. retrieve base fcb files from common

                foreach (DatEntry inputDatEntry in inputDatEntries)
                {
                    Stream inputDatStream = new MemoryStream(inputDatEntry.content);
                    Stream baseDatStreamDecompressed = new MemoryStream();

                    uint header = inputDatStream.ReadValueU32();
                    uint version = inputDatStream.ReadValueU16();

                    inputDatStream.Position = 0;

                    if (header == 0x4643626E && version == 3)
                    {
                        //Console.WriteLine($"FCB file: {inputDatEntry.name}");

                        DatEntry baseDatEntry = null;

                        ulong hash = inputDatEntry.hash;

                        foreach (DatEntry patchDatEntry in patchDatEntries)
                        {
                            if (patchDatEntry.fatEntry.nameHash == inputDatEntry.hash)
                            {
                                // found fcb base file in patch
                                baseDatEntry = patchDatEntry;
                            }
                        }

                        if (baseDatEntry == null)
                        {
                            foreach (DatEntry commonDatEntry in commonDatEntries)
                            {
                                //Console.WriteLine($"{commonDatEntry.fatEntry.nameHash} = {inputDatEntry.hash}");

                                if (commonDatEntry.fatEntry.nameHash == inputDatEntry.hash)
                                {
                                    // found fcb base file in common
                                    baseDatEntry = commonDatEntry;
                                }
                            }
                        }

                        if (baseDatEntry == null)
                        {
                            //Console.WriteLine("Could not find base file");
                        }
                        else
                        {
                            Stream baseDatStream = new MemoryStream(baseDatEntry.content);

                            int baseFileCompressionScheme = (int)baseDatEntry.fatEntry.compressionScheme;

                            if (baseFileCompressionScheme != 0)
                            {
                                //Console.WriteLine("Decompressing base file");

                                int baseFileSizeCompressed = (int)baseDatEntry.fatEntry.compressedSize;
                                int baseFileSizeUncompressed = (int)baseDatEntry.fatEntry.uncompressedSize;

                                if (baseFileCompressionScheme == 3)
                                {
                                    if (baseFileCompressionScheme == 3)
                                    {
                                        Compression.Schemes.LZ4LW.Decompress(
                                            baseDatStream,
                                            baseFileSizeCompressed,
                                            baseDatStreamDecompressed,
                                            baseFileSizeUncompressed);
                                    }
                                }
                                else
                                {
                                    //Console.WriteLine("Unsupported compression scheme");
                                }

                                if ((int)baseDatStreamDecompressed.Length == baseFileSizeUncompressed)
                                {
                                    //Console.WriteLine("Decompression successful");
                                }
                                else
                                {
                                    //Console.WriteLine("Decompression failed");
                                }
                            }
                            else
                            {
                                baseDatStreamDecompressed = baseDatStream;
                            }

                            //Utility.Log.ToConsole("Deserializing file: " + inputDatEntry.name);

                            Fcb inputFcbFile = new Fcb();
                            inputFcbFile.Deserialize(inputDatStream);

                            Fcb baseFcbFile = new Fcb();
                            baseFcbFile.Deserialize(baseDatStreamDecompressed);

                            if (inputFcbFile.root.Children.Count < baseFcbFile.root.Children.Count)
                            {
                                Stream newDatStream = new MemoryStream();
                                Fcb newFcbFile = baseFcbFile;
                                newFcbFile.Combine(inputFcbFile);
                                newFcbFile.Serialize(newDatStream);

                                inputDatEntry.content = ((MemoryStream)newDatStream).ToArray();
                            }

                            /*
                            string exportPath = Path.Combine(Path.GetFullPath("outputDirectory\\export"), inputDatEntry.name);

                            Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
                            var file = File.Create(exportPath);

                            //file.Write(inputDatEntry.content, 0, inputDatEntry.content.Length);
                            //file.Write(baseDatEntry.content, 0, baseDatEntry.content.Length);
                            //file.Write(((MemoryStream) baseDatStreamDecompressed).ToArray(), 0, (int) baseDatStreamDecompressed.Length);
                            */
                        }
                    }
                }
            }

            // write output dat+fat
            string outputDatWrite = Path.ChangeExtension(outputFatPath, "dat");
            using (var outputDat = File.Create(outputDatWrite))
            {
                List<DatEntry> replacementEntries = new List<DatEntry>();

                List<DatEntry> outputDatEntries = new List<DatEntry>();
                List<FatEntry> outputFatEntries = new List<FatEntry>();

                foreach (DatEntry existingEntry in patchDatEntries)
                {
                    bool modified = false;

                    foreach (DatEntry inputEntry in inputDatEntries)
                    {
                        if (inputEntry.hash == existingEntry.fatEntry.nameHash)
                        {
                            modified = true;

                            outputDatEntries.Add(inputEntry);
                            inputEntry.added = true;

                            Utility.Log.ToConsole($"Replaced entry: {inputEntry.name}");
                        }
                    }

                    if (modified != true)
                    {
                        outputDatEntries.Add(existingEntry);

                        //Utility.Log.ToConsole($"Added existing entry: {existingEntry.fatEntry.nameHash:X16}");
                    }
                }

                foreach (DatEntry inputEntry in inputDatEntries)
                {
                    if (inputEntry.added != true)
                    {
                        outputDatEntries.Add(inputEntry);

                        Utility.Log.ToConsole($"Added entry: {inputEntry.name}");
                    }
                }

                foreach (DatEntry outputEntry in outputDatEntries)
                {
                    FatEntry fatEntry;

                    if (outputEntry.name != null && outputEntry.fatEntry.nameHash == 0)
                    {
                        fatEntry = new FatEntry()
                        {
                            nameHash = Values.Hashes.CRC64_WD2.Compute(outputEntry.name),
                            offset = (ulong)outputDat.Position,
                            uncompressedSize = (ulong)outputEntry.content.Length,
                            compressedSize = (ulong)outputEntry.content.Length,
                            compressionScheme = 0
                        };
                    }
                    else
                    {
                        fatEntry = outputEntry.fatEntry;
                        fatEntry.offset = (ulong)outputDat.Position;
                    }

                    outputFatEntries.Add(fatEntry);

                    outputDat.Write(outputEntry.content, 0, outputEntry.content.Length);
                }

                //Console.WriteLine(outputFatEntries.ToString());
                //Console.WriteLine("Test");

                using (var fat = File.Create(outputFatPath))
                {
                    //Console.WriteLine("Output FAT: " + outputFatPath);

                    outputFat.entries = outputFatEntries;
                    outputFat.Serialize(fat);
                }

                Utility.Log.ToConsole("Done!");
            }

            // ...

            Fat GetFat(string fatFilePath)
            {
                Fat fat = new Fat();

                if (File.Exists(fatFilePath))
                {
                    using (var input = File.OpenRead(fatFilePath))
                    {
                        if (input.Length == 0)
                        {
                            throw new FormatException("empty file");
                        }

                        fat.Deserialize(input);

                        input.Close();
                    }
                }

                return fat;
            }

            List<DatEntry> GetDatEntries(Fat fat, string datFilePath)
            {
                List<DatEntry> datEntries = new List<DatEntry>();

                if (File.Exists(datFilePath))
                {
                    using (var input = File.OpenRead(datFilePath))
                    {
                        FatEntry[] entries = fat.entries.OrderBy(e => e.offset).ToArray();

                        for (int i = 0; i < entries.Length; i++)
                        {
                            FatEntry entry = entries[i];

                            input.Seek((long)entry.offset, SeekOrigin.Begin);

                            byte[] content = new byte[entry.compressedSize];
                            input.Read(content, 0, (int)entry.compressedSize);

                            datEntries.Add(new DatEntry()
                            {
                                content = content,
                                fatEntry = entry
                            });

                            if (entry.compressionScheme == 3)
                            {
                                // decompress
                            }
                        }

                        input.Close();
                    }
                }
                else
                {
                    throw new FileNotFoundException(datFilePath);
                }

                return datEntries.Count > 0 ? datEntries : null;
            }
        }

        public static void NormalMode(string[] args)
        {
            string inputFolderPath = Path.GetFullPath(args[0]);
            string outputFatPath = Path.GetFullPath(args[1]);
            string inputFatPath = null;

            //Console.WriteLine(string.Format("inputFolder: {0}\noutputFatPath: {1}", inputFolderPath, outputFatPath));

            if (args.Length == 3)
            {
                inputFatPath = Path.GetFullPath(args[2]);

                if (!File.Exists(inputFatPath))
                {
                    throw new Exception("Invalid input FAT path");
                }
            }
            else if (args.Length == 2)
            {
                /*
                if (!File.Exists(outputFatPath))
                {
                    throw new Exception("Invalid output FAT path");
                }
                */
            }
            else
            {
                return;
            }

            //Values.FileNameHash.Initialize();

            Fat fat = new Fat();

            string inputFatRead = inputFatPath == null ? outputFatPath : inputFatPath;
            //Console.WriteLine("Input FAT: " + inputFatRead);

            // deserialize input fat
            if (File.Exists(inputFatRead))
            {
                using (var input = File.OpenRead(inputFatRead))
                {
                    if (input.Length == 0)
                    {
                        throw new FormatException("empty file");
                    }

                    fat.Deserialize(input);
                }
            }

            List<DatEntry> existingDatEntries = new List<DatEntry>();

            // read output dat
            string inputDatRead = Path.ChangeExtension(inputFatRead, "dat");
            if (File.Exists(inputDatRead))
            {
                using (var input = File.OpenRead(inputDatRead))
                {
                    FatEntry[] entries = fat.entries.OrderBy(e => e.offset).ToArray();

                    for (int i = 0; i < entries.Length; i++)
                    {
                        FatEntry entry = entries[i];

                        input.Seek((long)entry.offset, SeekOrigin.Begin);

                        byte[] content = new byte[entry.compressedSize];
                        input.Read(content, 0, (int)entry.compressedSize);

                        existingDatEntries.Add(new DatEntry()
                        {
                            content = content,
                            fatEntry = entry
                        });

                        bool export = false;

                        if (entry.compressionScheme == 0)
                        {
                            //export = true;
                        }

                        if (export == true)
                        {
                            var basePath = Path.Combine(Directory.GetParent(inputFolderPath).FullName, args[1] + "_output");

                            string entryName = entry.GetName();

                            var entryPath = Path.Combine(basePath, entryName);
                            var entryParent = Path.GetDirectoryName(entryPath);

                            Directory.CreateDirectory(entryParent);

                            using (var output = File.Create(entryPath))
                            {
                                output.Write(content, 0, content.Length);
                            }

                            Console.WriteLine(string.Format("{0:X16} @ {1}, {2} bytes ({3} compressed bytes, scheme {4})",
                                entryName,
                                entry.offset,
                                entry.uncompressedSize,
                                entry.compressedSize,
                                entry.compressionScheme));
                        }
                    }
                }
            }

            string[] inputFilePaths = Directory.GetFiles(inputFolderPath, "*.*", SearchOption.AllDirectories);

            List<DatEntry> inputDatEntries = new List<DatEntry>();

            foreach (string inputFilePath in inputFilePaths)
            {
                string archiveFilePath = inputFilePath.Replace(inputFolderPath + Path.DirectorySeparatorChar, "");
                ulong archiveFileHash = Values.Hashes.CRC64_WD2.Compute(archiveFilePath);

                if (archiveFilePath.StartsWith("0x"))
                {
                    archiveFileHash = ulong.Parse(archiveFilePath.Substring(2), NumberStyles.HexNumber);
                }

                //Console.WriteLine($"{archiveFilePath} -> {archiveFileHash:X16}");

                inputDatEntries.Add(new DatEntry()
                {
                    hash = archiveFileHash,
                    name = archiveFilePath,
                    content = File.ReadAllBytes(inputFilePath)
                });

                //Console.WriteLine($"Created input entry: {archiveFilePath}");
            }

            // write output dat+fat
            string outputDatWrite = Path.ChangeExtension(outputFatPath, "dat");
            using (var outputDat = File.Create(outputDatWrite))
            {
                List<DatEntry> replacementEntries = new List<DatEntry>();

                List<DatEntry> outputDatEntries = new List<DatEntry>();
                List<FatEntry> outputFatEntries = new List<FatEntry>();

                foreach (DatEntry existingEntry in existingDatEntries)
                {
                    bool modified = false;

                    foreach (DatEntry inputEntry in inputDatEntries)
                    {
                        if (inputEntry.hash == existingEntry.fatEntry.nameHash)
                        {
                            modified = true;

                            outputDatEntries.Add(inputEntry);
                            inputEntry.added = true;

                            Utility.Log.ToConsole($"Replaced entry: {inputEntry.name}");
                        }
                    }

                    if (modified != true)
                    {
                        outputDatEntries.Add(existingEntry);

                        //Utility.Log.ToConsole($"Added existing entry: {existingEntry.fatEntry.nameHash:X16}");
                    }
                }

                foreach (DatEntry inputEntry in inputDatEntries)
                {
                    if (inputEntry.added != true)
                    {
                        outputDatEntries.Add(inputEntry);

                        Utility.Log.ToConsole($"Added entry: {inputEntry.name}");
                    }
                }

                foreach (DatEntry outputEntry in outputDatEntries)
                {
                    FatEntry fatEntry;
                    bool newEntry = false;

                    if (outputEntry.name != null && outputEntry.fatEntry.nameHash == 0)
                    {
                        fatEntry = new FatEntry();

                        fatEntry.nameHash = Values.Hashes.CRC64_WD2.Compute(outputEntry.name);
                        fatEntry.offset = (ulong)outputDat.Position;
                        fatEntry.uncompressedSize = (ulong)outputEntry.content.Length;
                        fatEntry.compressedSize = (ulong)outputEntry.content.Length;
                        fatEntry.compressionScheme = 0;

                        newEntry = true;
                    }
                    else
                    {
                        fatEntry = outputEntry.fatEntry;
                        fatEntry.offset = (ulong)outputDat.Position;
                    }

                    outputFatEntries.Add(fatEntry);

                    outputDat.Write(outputEntry.content, 0, outputEntry.content.Length);

                    if (newEntry == true)
                    {
                        //Utility.Log.ToConsole($"Added entry: {outputEntry.name}");
                    }
                }

                //Console.WriteLine(outputFatEntries.ToString());
                //Console.WriteLine("Test");

                using (var outputFat = File.Create(outputFatPath))
                {
                    //Console.WriteLine("Output FAT: " + outputFatPath);

                    fat.entries = outputFatEntries;
                    fat.Serialize(outputFat);
                }

                Utility.Log.ToConsole("Done!");
            }
        }

        public static void CombineMode(string[] args)
        {
            Fat GetFat(string fatFilePath)
            {
                Fat fat = new Fat();

                if (File.Exists(fatFilePath))
                {
                    using (var input = File.OpenRead(fatFilePath))
                    {
                        if (input.Length == 0)
                        {
                            throw new FormatException("empty file");
                        }

                        fat.Deserialize(input);

                        input.Close();
                    }
                }

                return fat;
            }

            List<DatEntry> GetDatEntries(Fat fat, string datFilePath)
            {
                List<DatEntry> datEntries = new List<DatEntry>();

                if (File.Exists(datFilePath))
                {
                    using (var input = File.OpenRead(datFilePath))
                    {
                        FatEntry[] entries = fat.entries.OrderBy(e => e.offset).ToArray();

                        for (int i = 0; i < entries.Length; i++)
                        {
                            FatEntry entry = entries[i];

                            input.Seek((long)entry.offset, SeekOrigin.Begin);

                            byte[] content = new byte[entry.compressedSize];
                            input.Read(content, 0, (int)entry.compressedSize);

                            datEntries.Add(new DatEntry()
                            {
                                content = content,
                                fatEntry = entry
                            });

                            if (entry.compressionScheme == 3)
                            {
                                // decompress
                            }
                        }

                        input.Close();
                    }
                }

                return datEntries.Count > 0 ? datEntries : null;
            }

            // PackLegion.exe -c "patch" "patch.fat" "common.fat"
            // PackLegion.exe -c "patch" "patch.fat" "patch-o.fat" "common.fat"

            // NEW
            // PackLegion.exe -c [inputFolder] [outputFat]

            string inputFolderPath = Path.GetFullPath(args[1]);
            string outputFatPath = Path.GetFullPath(args[2]);
            string inputPatchFatPath = null;
            string inputCommonFatPath = null;

            if (args.Length == 5)
            {
                inputPatchFatPath = Path.GetFullPath(args[3]);
                inputCommonFatPath = Path.GetFullPath(args[4]);

                if (!File.Exists(inputPatchFatPath))
                {
                    throw new Exception("Invalid patch FAT path");
                }
            }
            else if (args.Length == 4)
            {
                inputCommonFatPath = Path.GetFullPath(args[3]);
            }
            else
            {
                return;
            }

            if (!File.Exists(inputCommonFatPath))
            {
                throw new Exception("Invalid common FAT path");
            }

            string patchFatRead = inputPatchFatPath == null ? outputFatPath : inputPatchFatPath;
            string commonFatRead = inputCommonFatPath;

            //Console.WriteLine("Patch FAT: " + patchFatRead);
            //Console.WriteLine("Common FAT: " + commonFatRead);

            Fat outputFat = GetFat(patchFatRead);
            Fat commonFat = GetFat(commonFatRead);

            // read patch dat
            string patchDatRead = Path.ChangeExtension(patchFatRead, "dat");
            List<DatEntry> patchDatEntries = GetDatEntries(outputFat, patchDatRead);

            // read common dat
            string commonDatRead = Path.ChangeExtension(commonFatRead, "dat");
            List<DatEntry> commonDatEntries = GetDatEntries(commonFat, commonDatRead);

            string[] inputFilePaths = Directory.GetFiles(inputFolderPath, "*.*", SearchOption.AllDirectories);

            List<DatEntry> inputDatEntries = new List<DatEntry>();

            foreach (string inputFilePath in inputFilePaths)
            {
                string archiveFilePath = inputFilePath.Replace(inputFolderPath + Path.DirectorySeparatorChar, "");
                ulong archiveFileHash = Values.Hashes.CRC64_WD2.Compute(archiveFilePath);

                if (archiveFilePath.StartsWith("0x"))
                {
                    archiveFileHash = ulong.Parse(archiveFilePath.Substring(2), NumberStyles.HexNumber);
                }

                //Console.WriteLine($"{archiveFilePath} -> {archiveFileHash:X16}");

                inputDatEntries.Add(new DatEntry()
                {
                    hash = archiveFileHash,
                    name = archiveFilePath,
                    content = File.ReadAllBytes(inputFilePath)
                });

                //Console.WriteLine($"Created input entry: {archiveFilePath}");
            }

            // 1. gather fcb files to modify
            // 2. retrieve base fcb files from patch
            // 3. retrieve base fcb files from common

            foreach (DatEntry inputDatEntry in inputDatEntries)
            {
                Stream inputDatStream = new MemoryStream(inputDatEntry.content);
                Stream baseDatStreamDecompressed = new MemoryStream();

                uint header = inputDatStream.ReadValueU32();
                uint version = inputDatStream.ReadValueU16();

                inputDatStream.Position = 0;

                if (header == 0x4643626E && version == 3)
                {
                    //Console.WriteLine($"FCB file: {inputDatEntry.name}");

                    DatEntry baseDatEntry = null;

                    ulong hash = inputDatEntry.hash;

                    foreach (DatEntry patchDatEntry in patchDatEntries)
                    {
                        if (patchDatEntry.fatEntry.nameHash == inputDatEntry.hash)
                        {
                            // found fcb base file in patch
                            baseDatEntry = patchDatEntry;
                        }
                    }

                    if (baseDatEntry == null)
                    {
                        foreach (DatEntry commonDatEntry in commonDatEntries)
                        {
                            //Console.WriteLine($"{commonDatEntry.fatEntry.nameHash} = {inputDatEntry.hash}");

                            if (commonDatEntry.fatEntry.nameHash == inputDatEntry.hash)
                            {
                                // found fcb base file in common
                                baseDatEntry = commonDatEntry;
                            }
                        }
                    }

                    if (baseDatEntry == null)
                    {
                        //Console.WriteLine("Could not find base file");
                    }
                    else
                    {
                        Stream baseDatStream = new MemoryStream(baseDatEntry.content);

                        int baseFileCompressionScheme = (int)baseDatEntry.fatEntry.compressionScheme;

                        if (baseFileCompressionScheme != 0)
                        {
                            //Console.WriteLine("Decompressing base file");

                            int baseFileSizeCompressed = (int)baseDatEntry.fatEntry.compressedSize;
                            int baseFileSizeUncompressed = (int)baseDatEntry.fatEntry.uncompressedSize;

                            if (baseFileCompressionScheme == 3)
                            {
                                if (baseFileCompressionScheme == 3)
                                {
                                    Compression.Schemes.LZ4LW.Decompress(
                                        baseDatStream,
                                        baseFileSizeCompressed,
                                        baseDatStreamDecompressed,
                                        baseFileSizeUncompressed);
                                }
                            }
                            else
                            {
                                //Console.WriteLine("Unsupported compression scheme");
                            }

                            if ((int) baseDatStreamDecompressed.Length == baseFileSizeUncompressed)
                            {
                                //Console.WriteLine("Decompression successful");
                            }
                            else
                            {
                                //Console.WriteLine("Decompression failed");
                            }
                        }
                        else
                        {
                            baseDatStreamDecompressed = baseDatStream;
                        }

                        //Utility.Log.ToConsole("Deserializing file: " + inputDatEntry.name);

                        Fcb inputFcbFile = new Fcb();
                        inputFcbFile.Deserialize(inputDatStream);

                        Fcb baseFcbFile = new Fcb();
                        baseFcbFile.Deserialize(baseDatStreamDecompressed);

                        if (inputFcbFile.root.Children.Count < baseFcbFile.root.Children.Count)
                        {
                            Stream newDatStream = new MemoryStream();
                            Fcb newFcbFile = baseFcbFile;
                            newFcbFile.Combine(inputFcbFile);
                            newFcbFile.Serialize(newDatStream);

                            inputDatEntry.content = ((MemoryStream)newDatStream).ToArray();
                        }

                        /*
                        string exportPath = Path.Combine(Path.GetFullPath("outputDirectory\\export"), inputDatEntry.name);

                        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
                        var file = File.Create(exportPath);

                        //file.Write(inputDatEntry.content, 0, inputDatEntry.content.Length);
                        //file.Write(baseDatEntry.content, 0, baseDatEntry.content.Length);
                        //file.Write(((MemoryStream) baseDatStreamDecompressed).ToArray(), 0, (int) baseDatStreamDecompressed.Length);
                        */
                    }
                }
            }

            // write output dat+fat
            string outputDatWrite = Path.ChangeExtension(outputFatPath, "dat");
            using (var outputDat = File.Create(outputDatWrite))
            {
                List<DatEntry> replacementEntries = new List<DatEntry>();

                List<DatEntry> outputDatEntries = new List<DatEntry>();
                List<FatEntry> outputFatEntries = new List<FatEntry>();

                foreach (DatEntry existingEntry in patchDatEntries)
                {
                    bool modified = false;

                    foreach (DatEntry inputEntry in inputDatEntries)
                    {
                        if (inputEntry.hash == existingEntry.fatEntry.nameHash)
                        {
                            modified = true;

                            outputDatEntries.Add(inputEntry);
                            inputEntry.added = true;

                            Utility.Log.ToConsole($"Replaced entry: {inputEntry.name}");
                        }
                    }

                    if (modified != true)
                    {
                        outputDatEntries.Add(existingEntry);

                        //Utility.Log.ToConsole($"Added existing entry: {existingEntry.fatEntry.nameHash:X16}");
                    }
                }

                foreach (DatEntry inputEntry in inputDatEntries)
                {
                    if (inputEntry.added != true)
                    {
                        outputDatEntries.Add(inputEntry);

                        Utility.Log.ToConsole($"Added entry: {inputEntry.name}");
                    }
                }

                foreach (DatEntry outputEntry in outputDatEntries)
                {
                    FatEntry fatEntry;

                    if (outputEntry.name != null && outputEntry.fatEntry.nameHash == 0)
                    {
                        fatEntry = new FatEntry()
                        {
                            nameHash = Values.Hashes.CRC64_WD2.Compute(outputEntry.name),
                            offset = (ulong) outputDat.Position,
                            uncompressedSize = (ulong) outputEntry.content.Length,
                            compressedSize = (ulong) outputEntry.content.Length,
                            compressionScheme = 0
                        };
                    }
                    else
                    {
                        fatEntry = outputEntry.fatEntry;
                        fatEntry.offset = (ulong) outputDat.Position;
                    }

                    outputFatEntries.Add(fatEntry);

                    outputDat.Write(outputEntry.content, 0, outputEntry.content.Length);
                }

                //Console.WriteLine(outputFatEntries.ToString());
                //Console.WriteLine("Test");

                using (var fat = File.Create(outputFatPath))
                {
                    //Console.WriteLine("Output FAT: " + outputFatPath);

                    outputFat.entries = outputFatEntries;
                    outputFat.Serialize(fat);
                }

                Utility.Log.ToConsole("Done!");
            }
        }
    }
}
