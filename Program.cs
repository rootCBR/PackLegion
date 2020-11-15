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
            // PackLegion.exe "%1" "outputDirectory\%~n1.fat"
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:\n\tPackLegion.exe [inputFolder] [outputFatPath]\n\tPackLegion.exe [inputFolder] [outputFatPath] [inputFatPath]");
                return;
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
            }
            else if (args.Length < 2)
            {
                return;
            }

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

                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($"Added replacement entry: {inputEntry.name}");
                            Console.ResetColor();
                        }
                    }

                    if (modified != true)
                    {
                        outputDatEntries.Add(existingEntry);

                        //Console.ForegroundColor = ConsoleColor.DarkGray;
                        //Console.WriteLine($"Added existing entry: {existingEntry.fatEntry.nameHash:X16}");
                        //Console.ResetColor();
                    }
                }

                foreach (DatEntry inputEntry in inputDatEntries)
                {
                    if (inputEntry.added != true)
                    {
                        outputDatEntries.Add(inputEntry);

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"Added new entry: {inputEntry.name}");
                        Console.ResetColor();
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
                        fatEntry.offset = (ulong) outputDat.Position;
                        fatEntry.uncompressedSize = (ulong) outputEntry.content.Length;
                        fatEntry.compressedSize = (ulong) outputEntry.content.Length;
                        fatEntry.compressionScheme = 0;

                        newEntry = true;
                    }
                    else
                    {
                        fatEntry = outputEntry.fatEntry;
                        fatEntry.offset = (ulong) outputDat.Position;
                    }

                    outputFatEntries.Add(fatEntry);

                    outputDat.Write(outputEntry.content, 0, outputEntry.content.Length);

                    if (newEntry == true)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        //Console.WriteLine($"Added entry: {outputEntry.name}");
                        Console.ResetColor();
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

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Done!");
                Console.ResetColor();
            }
        }
    }
}
