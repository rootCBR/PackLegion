using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Cryptography;
using Gibbed.IO;
using PackLegion.Files.XML;
using PackLegion.External;

namespace PackLegion
{
    class Program
    {
        static void Main(string[] args)
        {
            Utility.Log.ToConsole(Config.VersionString);

            if (args.Length == 0)
            {
                Utility.Log.ToConsole(Config.UsageString);

                Console.ReadKey(true);

                return;

                /*
                args = new string[]
                {
                        //"-o",
                        "-c",
                        "-v",
                        //"-n",
                        @"D:\Modding\Disrupt\WDL\_patch",
                        "patch.fat"
                };
                */
            }

            if (Config.ProcessArgs(args) > 0)
            {
                Config.Initialize(AppDomain.CurrentDomain.BaseDirectory);

                Stopwatch s = Stopwatch.StartNew();

                try
                {
                    WorkReallyNew();
                }
                catch (Exception e)
                {
                    s.Stop();

                    Console.WriteLine(e.ToString());

                    //Console.WriteLine("\nPress any key to close...");
                    Console.ReadKey(true);

                    return;
                }

                s.Stop();

                if (s.ElapsedMilliseconds < 1000)
                {
                    Utility.Log.ToConsole($"Operation finished in {s.ElapsedMilliseconds} ms.");
                }
                else
                {
                    Utility.Log.ToConsole($"Operation finished in {s.ElapsedMilliseconds / 1000} seconds.");
                }
            }
            else
            {
                throw new Exception("Invalid number of arguments");
            }

            //Console.ReadKey(true);
        }

        public static void WorkReallyNew()
        {
            if (string.IsNullOrEmpty(Config.InputFolder))
            {
                throw new Exception("Invalid input folder");
            }

            string inputFolderPath = Path.GetFullPath(Config.InputFolder);
            string inputPatchFatPath = !string.IsNullOrEmpty(Config.InputFatOriginal) ? Path.GetFullPath(Config.InputFatOriginal) : string.Empty;
            string inputCommonFatPath = !string.IsNullOrEmpty(Config.InputFatCommon) ? Path.GetFullPath(Config.InputFatCommon) : string.Empty;
            string outputFatPath = !string.IsNullOrEmpty(Config.OutputFat) ? Path.GetFullPath(Config.OutputFat) : Path.GetFullPath(inputFolderPath.Split(Path.DirectorySeparatorChar).Last() + ".fat");

            if (!Directory.Exists(inputFolderPath))
            {
                throw new DirectoryNotFoundException(inputFolderPath);
            }

            bool modeOriginal = Config.Option_Original;
            bool modeCombine = Config.Option_Combine;
            bool modeInfo = Config.Option_Info;

            string readPatchFatPathOriginal = inputPatchFatPath;
            string readCommonFatPathCombine = inputCommonFatPath;

            string readInputFatPath = null;
            string readPatchFatPath = null;
            string readCommonFatPath = null;

            string writeOutputFatPath = outputFatPath;

            if (modeOriginal || modeCombine)
            {
                if (modeCombine)
                {
                    readInputFatPath = outputFatPath;
                    readPatchFatPath = readPatchFatPathOriginal;
                    readCommonFatPath = readCommonFatPathCombine;
                }

                if (modeOriginal)
                {
                    readPatchFatPath = readPatchFatPathOriginal;
                }
            }
            else
            {
                readInputFatPath = outputFatPath;
            }

            Utility.Log.ToConsoleVerbose(string.Format("Input archive to read from:\t\t{0}\nOriginal patch archive to read from:\t{1}\nOriginal common archive to read from:\t{2}\nArchive to write to:\t\t\t{3}", readInputFatPath, readPatchFatPath, readCommonFatPath, writeOutputFatPath));

            string[] inputFilePaths = Directory.GetFiles(inputFolderPath, "*.*", SearchOption.AllDirectories);

            using (var outputDatStream = File.Open(Path.ChangeExtension(writeOutputFatPath, "dat"), FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var outputFatStream = File.Open(writeOutputFatPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                FileStream patchDatStream = null;
                FileStream patchFatStream = null;

                FileStream commonDatStream = null;
                FileStream commonFatStream = null;

                if (writeOutputFatPath != readPatchFatPath && writeOutputFatPath != readCommonFatPath)
                {
                    if (modeOriginal || modeCombine)
                    {
                        string readPatchDatPath = Path.ChangeExtension(readPatchFatPath, "dat");

                        if (File.Exists(readPatchDatPath) && File.Exists(readPatchFatPath))
                        {
                            patchDatStream = File.OpenRead(readPatchDatPath);
                            patchFatStream = File.OpenRead(readPatchFatPath);
                        }
                        else
                        {
                            throw new FileNotFoundException("Original patch archive does not exist");
                        }

                        if (modeCombine)
                        {
                            string readCommonDatPath = Path.ChangeExtension(readCommonFatPath, "dat");

                            if (File.Exists(readCommonDatPath) && File.Exists(readCommonFatPath))
                            {
                                commonDatStream = File.OpenRead(readCommonDatPath);
                                commonFatStream = File.OpenRead(readCommonFatPath);
                            }
                            else
                            {
                                throw new FileNotFoundException("Original common archive does not exist");
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Attempting to write to original archive");
                }

                Fat outputFat = new Fat();
                Fat patchFat = null;
                Fat commonFat = null;

                if (modeCombine)
                {
                    Utility.Log.ToConsoleVerbose("Deserializing original patch archive...");
                    patchFat = new Fat();
                    patchFat.Deserialize(patchFatStream);

                    Utility.Log.ToConsoleVerbose("Deserializing original common archive...");
                    commonFat = new Fat();
                    commonFat.Deserialize(commonFatStream);
                }

                List<FatEntry> outputFatEntries = new List<FatEntry>();

                if (modeOriginal)
                {
                    Utility.Log.ToConsoleVerbose("Write mode: Create new archive with original files");

                    if (patchFat == null)
                    {
                        Utility.Log.ToConsoleVerbose("Not prepared yet, deserializing original patch archive...");
                        patchFat = new Fat();
                        patchFat.Deserialize(patchFatStream);
                    }

                    outputFat = patchFat;
                }
                else
                {
                    if (outputFatStream.Length != 0)
                    {
                        Utility.Log.ToConsoleVerbose("Write mode: Insert into existing archive");

                        Utility.Log.ToConsoleVerbose("Deserializing output archive...");
                        outputFat.Deserialize(outputFatStream);
                    }
                    else
                    {
                        Utility.Log.ToConsoleVerbose("Write mode: Create new archive");
                    }
                }

                if (outputFat.entries.Count > 0)
                {
                    //Utility.Log.ToConsole("Iterating original files...");

                    FatEntry[] entries = outputFat.entries.OrderBy(e => e.offset).ToArray();

                    FileStream inputDatStream = outputDatStream;

                    if (modeOriginal)
                    {
                        inputDatStream = patchDatStream;
                    }

                    ushort percentage = 0;

                    for (int a = 0; a < entries.Length; a++)
                    {
                        double x = entries[a].offset;
                        double y = inputDatStream.Length;
                        double t = x / y;
                        ushort currentPercent = (ushort)Math.Round(t * 100);

                        if (currentPercent > percentage || percentage == 0)
                        {
                            percentage = currentPercent;

                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write($"\rIterating original files... ({currentPercent}%)");
                            Console.ResetColor();

                            // how do you like me now?
                        }

                        FatEntry entry = entries[a];

                        if (GetReplacementEntry(entry.nameHash) != null)
                        {
                            continue;
                        }

                        byte[] content = new byte[entry.compressedSize];

                        inputDatStream.Seek((long)entry.offset, SeekOrigin.Begin);
                        inputDatStream.Read(content, 0, (int)entry.compressedSize);

                        //Console.WriteLine($"\nRead {entry.compressedSize} bytes from {entry.offset}");

                        entry.offset = (ulong)outputDatStream.Position;
                        outputFatEntries.Add(entry);

                        outputDatStream.Write(content, 0, content.Length);

                        //Console.WriteLine($"Write {entry.compressedSize} bytes from {(ulong) outputDatStream.Position - entry.compressedSize}");
                    }

                    Console.WriteLine("\r");

                    //inputDatStream.Close();

                    /*
                    Directory.CreateDirectory("temp");

                    using (var temp = new TempFileCollection("temp", true))
                    {
                        string tempFileName = temp.AddExtension("dat");

                        using (var createdStream = File.OpenWrite(tempFileName))
                        {
                            //Console.WriteLine(tempFileName);

                            FatEntry[] entries = outputFat.entries.OrderBy(e => e.offset).ToArray();

                            int lastMatch = 0;

                            for (int a = 0; a < entries.Length; a++)
                            {
                                FatEntry entry = entries[a];

                                bool hasReplacement = false;
                                uint matches = 0;

                                for (int b = 0; b < inputFilePaths.Length; b++)
                                {
                                    string path = inputFilePaths[b];

                                    if (entry.nameHash == GetFatEntryHash(path.Replace(inputFolderPath + Path.DirectorySeparatorChar, "")))
                                    {
                                        hasReplacement = true;
                                        matches++;

                                        break;
                                    }
                                }

                                if (hasReplacement)
                                {
                                    if (matches == inputFilePaths.Length - 1)
                                    {
                                        entries[a].offset = (ulong)createdStream.Position;

                                        byte[] block = new byte[outputDatStream.Length - outputDatStream.Position];
                                        outputDatStream.Read(block, 0, (int)(outputDatStream.Length - outputDatStream.Position));

                                        createdStream.WriteBytes(block);
                                    }
                                    else
                                    {
                                        for (int c = lastMatch; c < a; c++)
                                        {
                                            entries[c].offset = (ulong)createdStream.Position;

                                            FatEntry pastEntry = entries[c];

                                            byte[] block = new byte[pastEntry.compressedSize];
                                            outputDatStream.Read(block, 0, (int)pastEntry.compressedSize);

                                            createdStream.WriteBytes(block);
                                        }
                                    }

                                    lastMatch = a;
                                }
                            }

                            //createdStream.Position = 0;
                            //createdStream.CopyTo(outputDatStream);
                            createdStream.Close();

                            entries = null;
                        }

                        //temp.Delete();
                    }
                    */
                }

                Utility.Log.ToConsole("Adding input files...");

                foreach (string file in inputFilePaths)
                {
                    string path = file.Replace(inputFolderPath + Path.DirectorySeparatorChar, "");
                    ulong hash = GetFatEntryHash(path);

                    Stream stream = new MemoryStream(File.ReadAllBytes(file));

                    bool combined = false;

                    if (modeCombine)
                    {
                        Stream baseDatStreamDecompressed = new MemoryStream();

                        var endian = Endian.Little;

                        uint header = stream.ReadValueU32(endian);
                        uint version = stream.ReadValueU16(endian);

                        if (header == 0x4643626E && version == 3)
                        {
                            Utility.Log.ToConsoleVerbose($"FCB file: {path}");

                            Fcb fcb = new Fcb();
                            fcb.Deserialize(stream);

                            uint LibHash = 0xA90F3BCC;

                            if (fcb.root.NameHash == LibHash)
                            {
                                bool match = false;
                                FatEntry baseFatEntry = new FatEntry();

                                byte[] baseContent = null;

                                FatEntry[] patchEntries = patchFat.entries.OrderBy(e => e.offset).ToArray();

                                // search in patch
                                foreach (FatEntry entry in patchEntries)
                                {
                                    if (entry.nameHash == hash)
                                    {
                                        baseFatEntry = entry;

                                        baseContent = new byte[baseFatEntry.compressedSize];

                                        patchDatStream.Seek((long)baseFatEntry.offset, SeekOrigin.Begin);
                                        patchDatStream.Read(baseContent, 0, (int)baseFatEntry.compressedSize);

                                        match = true;
                                    }
                                }

                                // search in common
                                if (match != true)
                                {
                                    FatEntry[] commonEntries = commonFat.entries.OrderBy(e => e.offset).ToArray();

                                    foreach (FatEntry entry in commonEntries)
                                    {
                                        if (entry.nameHash == hash)
                                        {
                                            baseFatEntry = entry;

                                            baseContent = new byte[baseFatEntry.compressedSize];

                                            commonDatStream.Seek((long)baseFatEntry.offset, SeekOrigin.Begin);
                                            commonDatStream.Read(baseContent, 0, (int)baseFatEntry.compressedSize);

                                            match = true;
                                        }
                                    }
                                }

                                if (match != true)
                                {
                                    Utility.Log.ToConsole($"Could not combine entry: {path}");
                                    //throw new FileNotFoundException("Could not find base file");
                                }
                                else
                                {
                                    Stream baseDatStream = new MemoryStream(baseContent);

                                    int baseFileCompressionScheme = (int)baseFatEntry.compressionScheme;

                                    if (baseFileCompressionScheme != 0)
                                    {
                                        int baseFileSizeCompressed = (int)baseFatEntry.compressedSize;
                                        int baseFileSizeUncompressed = (int)baseFatEntry.uncompressedSize;

                                        if (baseFileCompressionScheme == 3)
                                        {
                                            Compression.Schemes.LZ4LW.Decompress(
                                                baseDatStream,
                                                baseFileSizeCompressed,
                                                baseDatStreamDecompressed,
                                                baseFileSizeUncompressed);
                                        }
                                        else
                                        {
                                            throw new InvalidDataException("Unsupported compression scheme");
                                        }

                                        if ((int)baseDatStreamDecompressed.Length != baseFileSizeUncompressed)
                                        {
                                            throw new InvalidDataException("Decompression failed");
                                        }
                                    }
                                    else
                                    {
                                        baseDatStreamDecompressed = baseDatStream;
                                    }

                                    Fcb inputFcbFile = new Fcb();
                                    inputFcbFile.Deserialize(stream);

                                    Fcb baseFcbFile = new Fcb();
                                    baseFcbFile.Deserialize(baseDatStreamDecompressed);

                                    //stream.Close(); // finally found you, sucker
                                    baseDatStreamDecompressed.Close();

                                    if (inputFcbFile.root.Children.Count < baseFcbFile.root.Children.Count)
                                    {
                                        Stream newDatStream = new MemoryStream();
                                        Fcb newFcbFile = baseFcbFile;
                                        newFcbFile.Combine(inputFcbFile);
                                        newFcbFile.Serialize(newDatStream);

                                        stream = newDatStream;

                                        combined = true;
                                    }
                                }
                            }

                        }
                    }

                    if (!outputDatStream.CanRead || !stream.CanRead)
                    {
                        Console.WriteLine("Test");
                    }

                    outputFatEntries.Add(new FatEntry()
                    {
                        nameHash = hash,
                        offset = (ulong)outputDatStream.Position,
                        uncompressedSize = (ulong)stream.Length,
                        compressedSize = (ulong)stream.Length,
                        compressionScheme = 0
                    });

                    outputDatStream.Write(((MemoryStream) stream).ToArray(), 0, (int) stream.Length);

                    if (combined)
                    {
                        Utility.Log.ToConsole($"Entry (combined): {path}");
                    }
                    else
                    {
                        Utility.Log.ToConsole($"Entry: {path}");
                    }
                }

                // ...

                outputFat.entries = outputFatEntries;
                outputFat.Serialize(outputFatStream);

                outputFatStream.Close();
                outputDatStream.Close();

                if (patchDatStream != null)
                {
                    patchDatStream.Close();
                    patchFatStream.Close();
                }

                if (commonDatStream != null)
                {
                    commonDatStream.Close();
                    commonFatStream.Close();
                }

                if (modeInfo)
                {
                    Utility.Log.ToConsole("Writing info file...");

                    Dictionary<uint, string> compressionSchemes = new Dictionary<uint, string>
                    {
                        {0, "none" },
                        {1, "oodle" },  // ?
                        {2, "lzma" },   // ?
                        {3, "lz4lw" },
                    };

                    string infoFilePath = Path.ChangeExtension(writeOutputFatPath, "nfo");

                    if (File.Exists(infoFilePath))
                    {
                        File.Delete(infoFilePath);
                    }

                    PackInfo info = new PackInfo();

                    foreach (FatEntry entry in outputFatEntries)
                    {
                        string path = GetFilePathFromHash(entry.nameHash);
                        ulong time = 0;
                        uint crc = 0;

                        if (path != null)
                        {
                            var crc32 = new Crc32();
                            var hash = string.Empty;

                            using (var fs = File.OpenRead(path))
                            {
                                foreach (byte b in crc32.ComputeHash(fs))
                                {
                                    hash += b.ToString("x2").ToLower();
                                }
                            }

                            path = path.Replace(inputFolderPath + Path.DirectorySeparatorChar, "").ToLower();
                            //time = (ulong) File.GetLastWriteTime(path).Ticks; //?
                            crc = uint.Parse(hash, NumberStyles.HexNumber);
                        }
                        else
                        {
                            path = $"_unknown\\0x{entry.nameHash:X16}";
                        }

                        info.common.Add(new RootFile()
                        {
                            Path = path,
                            Crc = entry.nameHash,
                            FilePosition = (uint)entry.offset,
                            FileSize = (uint)entry.compressedSize,
                            FileTime = time,
                            FileCRC = crc,
                            Compression = compressionSchemes[entry.compressionScheme],
                            FATsection = "compact",
                            OriginalFileSize = (uint)entry.uncompressedSize,
                            OriginalFileSizeSpecified = entry.compressionScheme != 0 ? true : false
                        });
                    }

                    using (var writer = new StreamWriter(infoFilePath))
                    {
                        var serializer = new XmlSerializer(typeof(PackInfo));

                        using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, NewLineOnAttributes = true }))
                        {
                            serializer.Serialize(xmlWriter, info);

                            xmlWriter.Close();
                        }
                    }
                }
            }

            string GetReplacementEntry(ulong hash)
            {
                foreach (string path in inputFilePaths)
                {
                    string file = path.Replace(inputFolderPath + Path.DirectorySeparatorChar, "");

                    if (GetFatEntryHash(file) == hash)
                    {
                        return file;
                    }
                }

                return null;
            }

            string GetFilePathFromHash(ulong hash)
            {
                for (int i = 0; i < inputFilePaths.Length; i++)
                {
                    string path = inputFilePaths[i].Replace(inputFolderPath + Path.DirectorySeparatorChar, "").Trim();
                    ulong fileHash = Values.Hashes.CRC64_WD2.Compute(path);

                    if (fileHash == hash)
                    {
                        return inputFilePaths[i];
                    }
                }

                return null; //Convert.ToString($"0x{hash:X16}");
            }

            /*
            uint FcbReadEntryCount(Stream input, Endian endian)
            {
                input.Seek(16, SeekOrigin.Begin);

                var value = input.ReadValueU8();

                if (value < 0xFE)
                {
                    return value;
                }

                return input.ReadValueU32(endian);
            }

            bool FcbIsSuitableForLibraryMultiExport(Fcb bof)
            {
                // Source: Gibbed.Disrupt
                uint LibHash = 0xA90F3BCC;
                uint LibItemHash = 0x72DE4948;
                uint TextHidNameHash = 0x9D8873F8;

                return bof.root.Fields.Count == 0 &&
                       bof.root.NameHash == LibHash &&
                       bof.root.Children.Any(c => c.NameHash != LibItemHash ||
                                                  c.Fields.ContainsKey(TextHidNameHash) == false) == false;
            }
            */
        }

        public static void WorkNew()
        {
            if (string.IsNullOrEmpty(Config.InputFolder))
            {
                throw new Exception("Invalid input folder");
            }

            string inputFolderPath = Path.GetFullPath(Config.InputFolder);
            string inputPatchFatPath = !string.IsNullOrEmpty(Config.InputFatOriginal) ? Path.GetFullPath(Config.InputFatOriginal) : string.Empty;
            string inputCommonFatPath = !string.IsNullOrEmpty(Config.InputFatCommon) ? Path.GetFullPath(Config.InputFatCommon) : string.Empty;
            string outputFatPath = !string.IsNullOrEmpty(Config.OutputFat) ? Path.GetFullPath(Config.OutputFat) : Path.GetFullPath(inputFolderPath.Split(Path.DirectorySeparatorChar).Last() + ".fat");

            //Utility.Log.ToConsole(string.Format("InputFolder: {0}\nOutputFat: {1}\nInputFatOriginal: {2}\nInputFatCommon: {3}", inputFolderPath, outputFatPath, inputPatchFatPath, inputCommonFatPath));

            if (!Directory.Exists(inputFolderPath))
            {
                throw new DirectoryNotFoundException(inputFolderPath);
            }

            string patchFatRead = outputFatPath;
            string commonFatRead = inputCommonFatPath;

            if (Config.Option_Original)
            {
                patchFatRead = inputPatchFatPath;

                if (string.IsNullOrEmpty(patchFatRead))
                {
                    throw new Exception("Original patch archive is not specified");
                }
            }

            if (Config.Option_Combine)
            {
                patchFatRead = inputPatchFatPath;

                if (string.IsNullOrEmpty(commonFatRead))
                {
                    throw new Exception("Common archive is not specified");
                }
            }

            FileStream outputFatStream = null;
            FileStream outputDatStream = null;
            Fat outputFat = new Fat();

            FileStream commonFatStream = null;
            FileStream commonDatStream = null;
            Fat commonFat = null;

            // read patch dat
            string patchDatRead = Path.ChangeExtension(patchFatRead, "dat");

            if (!string.IsNullOrEmpty(Config.OutputFat))
            {
                if (File.Exists(patchFatRead) && File.Exists(patchDatRead))
                {
                    Utility.Log.ToConsole($"Base archive: {patchFatRead}");

                    outputFatStream = File.Open(patchFatRead, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    outputDatStream = File.Open(patchDatRead, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                    if (outputFatStream.Length == 0)
                    {
                        throw new FormatException("FAT file is empty");
                    }

                    outputFat.Deserialize(outputFatStream);
                }
                else
                {
                    if (Config.Option_Combine)
                    {
                        if (!File.Exists(patchFatRead) || !File.Exists(patchDatRead))
                        {
                            throw new FileNotFoundException($"Patch archive does not exist ({patchFatRead})");
                        }
                    }

                    //throw new FileNotFoundException("Specified output patch archive does not exist");
                }
            }

            string commonDatRead = string.Empty;

            if (Config.Option_Combine)
            {
                commonDatRead = Path.ChangeExtension(commonFatRead, "dat");

                if (!File.Exists(commonFatRead) || !File.Exists(commonDatRead))
                {
                    throw new FileNotFoundException($"Common archive does not exist ({commonFatRead})");
                }

                commonFatStream = File.OpenRead(commonFatRead);
                commonDatStream = File.OpenRead(commonDatRead);

                if (commonFatStream.Length == 0)
                {
                    throw new FormatException("FAT file is empty");
                }

                commonFat = new Fat();
                commonFat.Deserialize(commonFatStream);
            }

            // files to be packed
            string[] inputFilePaths = Directory.GetFiles(inputFolderPath, "*.*", SearchOption.AllDirectories);

            // write output dat+fat
            string outputDatWrite = Path.ChangeExtension(outputFatPath, "dat");

            using (var outputDat = outputDatWrite == patchDatRead && outputDatStream != null ? outputDatStream : File.Create(outputDatWrite))
            {
                //FileStream inputDat = File.Exists(patchDatRead) ? (patchDatRead == outputDatWrite ? outputDat : File.OpenRead(patchDatRead)) : null;
                //FileStream commonDat = File.Exists(commonDatRead) && Config.Option_Combine ? File.OpenRead(commonDatRead) : null;

                List<ulong> replacedEntries = new List<ulong>();
                List<FatEntry> outputFatEntries = new List<FatEntry>();

                if (outputDatStream != null)
                {
                    if (Config.Option_Original && patchFatRead == inputPatchFatPath)
                    {
                        FatEntry[] entries = outputFat.entries.OrderBy(e => e.offset).ToArray();

                        foreach (FatEntry existingFatEntry in entries)
                        {
                            /*
                            outputDatStream.Seek((long)existingFatEntry.offset, SeekOrigin.Begin);

                            byte[] content = new byte[existingFatEntry.compressedSize];
                            outputDatStream.Read(content, 0, (int)existingFatEntry.compressedSize);
                            */

                            FatEntry fatEntry = new FatEntry();

                            string filePath = HasReplacementEntry(existingFatEntry.nameHash);

                            /*
                            if (filePath != null)
                            {
                                bool fcb = false;

                                if (content.Length >= 6)
                                {
                                    Stream datStream = new MemoryStream(content);
                                    Stream datStreamDecompressed = new MemoryStream();

                                    int compressionScheme = (int)existingFatEntry.compressionScheme;

                                    if (compressionScheme != 0)
                                    {
                                        //Console.WriteLine("Decompressing base file");

                                        int sizeCompressed = (int)existingFatEntry.compressedSize;
                                        int sizeUncompressed = (int)existingFatEntry.uncompressedSize;

                                        if (compressionScheme == 3)
                                        {
                                            Compression.Schemes.LZ4LW.Decompress(
                                                datStream,
                                                sizeCompressed,
                                                datStreamDecompressed,
                                                sizeUncompressed);
                                        }
                                        else
                                        {
                                            throw new InvalidDataException("Unsupported compression scheme");
                                        }

                                        if ((int)datStreamDecompressed.Length != sizeUncompressed)
                                        {
                                            throw new InvalidDataException("Decompression failed");
                                        }
                                    }

                                    datStreamDecompressed.Position = 0;

                                    uint header = datStreamDecompressed.ReadValueU32();
                                    uint version = datStreamDecompressed.ReadValueU16();

                                    datStreamDecompressed.Close();
                                    datStream.Close();

                                    if (header == 0x4643626E && version == 3)
                                    {
                                        fcb = true;
                                    }
                                }

                                if (fcb != true)
                                {
                                    fatEntry = new FatEntry()
                                    {
                                        nameHash = existingFatEntry.nameHash,
                                        offset = (ulong)outputDat.Position,
                                        uncompressedSize = (ulong)content.Length,
                                        compressedSize = (ulong)content.Length,
                                        compressionScheme = 0
                                    };

                                    replacedEntries.Add(fatEntry.nameHash);

                                    //Utility.Log.ToConsole($"Replaced entry: {filePath}");
                                    Utility.Log.ToConsole($"Entry: {filePath}");
                                }
                            }
                            else
                            {
                                fatEntry = existingFatEntry;
                                fatEntry.offset = (ulong)outputDat.Position;
                            }
                            */

                            if (filePath == null)
                            {
                                fatEntry.offset = (ulong)outputDat.Position;
                            }

                            outputDatStream.Seek((long)existingFatEntry.offset, SeekOrigin.Begin);

                            byte[] content = new byte[existingFatEntry.compressedSize];
                            outputDatStream.Read(content, 0, (int)existingFatEntry.compressedSize);

                            outputFatEntries.Add(fatEntry);

                            outputDat.Write(content, 0, content.Length);

                            content = null;
                        }
                    }
                }
                else
                {
                    //throw new Exception("Output DAT stream is null");
                }

                foreach (string file in inputFilePaths)
                {
                    string path = file.Replace(inputFolderPath + Path.DirectorySeparatorChar, "");
                    ulong hash = GetFatEntryHash(path);

                    if (replacedEntries.Contains(hash))
                    {
                        continue;
                    }

                    byte[] content = File.ReadAllBytes(file);

                    Stream stream = new MemoryStream(content);

                    bool combined = false;

                    if (Config.Option_Combine)
                    {
                        // 1. gather fcb files to modify
                        // 2. retrieve base fcb files from patch
                        // 3. retrieve base fcb files from common

                        Stream baseDatStreamDecompressed = new MemoryStream();

                        uint header = stream.ReadValueU32();
                        uint version = stream.ReadValueU16();

                        stream.Position = 0;

                        if (header == 0x4643626E && version == 3)
                        {
                            //Console.WriteLine($"FCB file: {path}");

                            bool match = false;
                            FatEntry baseFatEntry = new FatEntry();

                            byte[] baseContent = null;

                            foreach (FatEntry patchFatEntry in outputFat.entries)
                            {
                                if (patchFatEntry.nameHash == hash)
                                {
                                    // found fcb base file in patch
                                    baseFatEntry = patchFatEntry;

                                    baseContent = new byte[baseFatEntry.compressedSize];

                                    outputDatStream.Seek((long)baseFatEntry.offset, SeekOrigin.Begin);
                                    outputDatStream.Read(baseContent, 0, (int)baseFatEntry.compressedSize);

                                    match = true;
                                }
                            }

                            if (match != true)
                            {
                                foreach (FatEntry commonFatEntry in commonFat.entries)
                                {
                                    if (commonFatEntry.nameHash == hash)
                                    {
                                        // found fcb base file in common
                                        baseFatEntry = commonFatEntry;

                                        baseContent = new byte[baseFatEntry.compressedSize];

                                        commonDatStream.Seek((long)baseFatEntry.offset, SeekOrigin.Begin);
                                        commonDatStream.Read(baseContent, 0, (int)baseFatEntry.compressedSize);

                                        match = true;
                                    }
                                }
                            }

                            if (match != true)
                            {
                                Utility.Log.ToConsole($"Could not combine entry: {path}");
                                //throw new FileNotFoundException("Could not find base file");
                            }
                            else
                            {
                                Stream baseDatStream = new MemoryStream(baseContent);

                                int baseFileCompressionScheme = (int)baseFatEntry.compressionScheme;

                                if (baseFileCompressionScheme != 0)
                                {
                                    int baseFileSizeCompressed = (int)baseFatEntry.compressedSize;
                                    int baseFileSizeUncompressed = (int)baseFatEntry.uncompressedSize;

                                    if (baseFileCompressionScheme == 3)
                                    {
                                        Compression.Schemes.LZ4LW.Decompress(
                                            baseDatStream,
                                            baseFileSizeCompressed,
                                            baseDatStreamDecompressed,
                                            baseFileSizeUncompressed);
                                    }
                                    else
                                    {
                                        throw new InvalidDataException("Unsupported compression scheme");
                                    }

                                    if ((int)baseDatStreamDecompressed.Length != baseFileSizeUncompressed)
                                    {
                                        throw new InvalidDataException("Decompression failed");
                                    }
                                }
                                else
                                {
                                    baseDatStreamDecompressed = baseDatStream;
                                }

                                Fcb inputFcbFile = new Fcb();
                                inputFcbFile.Deserialize(stream);

                                Fcb baseFcbFile = new Fcb();
                                baseFcbFile.Deserialize(baseDatStreamDecompressed);

                                stream.Close();
                                baseDatStreamDecompressed.Close();

                                if (inputFcbFile.root.Children.Count < baseFcbFile.root.Children.Count)
                                {
                                    Stream newDatStream = new MemoryStream();
                                    Fcb newFcbFile = baseFcbFile;
                                    newFcbFile.Combine(inputFcbFile);
                                    newFcbFile.Serialize(newDatStream);

                                    content = ((MemoryStream)newDatStream).ToArray();

                                    combined = true;
                                }

                                if (combined != true)
                                {
                                    // ?
                                }
                            }
                        }
                    }

                    outputFatEntries.Add(new FatEntry()
                    {
                        nameHash = hash,
                        offset = (ulong)outputDat.Position,
                        uncompressedSize = (ulong)content.Length,
                        compressedSize = (ulong)content.Length,
                        compressionScheme = 0
                    });

                    if (combined == true)
                    {
                        Utility.Log.ToConsole($"Combined entry: {path}");
                    }
                    else
                    {
                        //Utility.Log.ToConsole($"Added entry: {path}");
                        Utility.Log.ToConsole($"Entry: {path}");
                    }

                    outputDat.Write(content, 0, content.Length);

                    content = null;
                }

                if (outputDatStream != null)
                {
                    outputDatStream.Close();
                }

                if (commonDatStream != null)
                {
                    commonDatStream.Close();
                }

                outputDat.Close();

                using (var fat = outputFatPath == patchFatRead && outputFatStream != null ? outputFatStream : File.Create(outputFatPath))
                {
                    outputFat.entries = outputFatEntries;
                    outputFat.Serialize(fat);

                    fat.Close();
                }
            }

            string HasReplacementEntry(ulong hash)
            {
                foreach (string path in inputFilePaths)
                {
                    string file = path.Replace(inputFolderPath + Path.DirectorySeparatorChar, "");

                    if (GetFatEntryHash(file) == hash)
                    {
                        return file;
                    }
                }

                return null;
            }
        }

        static ulong GetFatEntryHash(string path)
        {
            return path.StartsWith("0x") ? ulong.Parse(path.Substring(2), NumberStyles.HexNumber) : Values.Hashes.CRC64_WD2.Compute(path);
        }

        /*
        static Fat GetFat(string fatFilePath)
        {
            Fat fat = new Fat();

            if (File.Exists(fatFilePath))
            {
                using (var input = File.OpenRead(fatFilePath))
                {
                    if (input.Length == 0)
                    {
                        throw new FormatException($"FAT file is empty ({fatFilePath})");
                    }

                    fat.Deserialize(input);

                    input.Close();
                }
            }
            else
            {
                throw new FileNotFoundException(fatFilePath);
            }

            return fat;
        }

        public static void Work()
        {
            Config.Initialize(AppDomain.CurrentDomain.BaseDirectory);

            if (string.IsNullOrEmpty(Config.InputFolder))
            {
                throw new Exception("Invalid input folder");
            }

            string inputFolderPath = Path.GetFullPath(Config.InputFolder);
            string outputFatPath = !string.IsNullOrEmpty(Config.OutputFat) ? Path.GetFullPath(Config.OutputFat) : "patch.fat";
            string inputPatchFatPath = !string.IsNullOrEmpty(Config.InputFatOriginal) ? Path.GetFullPath(Config.InputFatOriginal) : string.Empty;
            string inputCommonFatPath = !string.IsNullOrEmpty(Config.InputFatCommon) ? Path.GetFullPath(Config.InputFatCommon) : string.Empty;

            //Utility.Log.ToConsole(string.Format("InputFolder: {0}\nOutputFat: {1}\nInputFatOriginal: {2}\nInputFatCommon: {3}", inputFolderPath, outputFatPath, inputPatchFatPath, inputCommonFatPath));

            if (!Directory.Exists(inputFolderPath))
            {
                throw new DirectoryNotFoundException(inputFolderPath);
            }

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

            if (Config.Option_Combine)
            {
                //Utility.Log.ToConsole("Option: Combine");

                if (string.IsNullOrEmpty(commonFatRead))
                {
                    throw new Exception("Common archive is not specified");
                }
            }

            Fat outputFat = new Fat();
            Fat commonFat = null;

            // read patch dat
            string patchDatRead = Path.ChangeExtension(patchFatRead, "dat");
            List<DatEntry> patchDatEntries = new List<DatEntry>();

            if (!string.IsNullOrEmpty(Config.OutputFat))
            {
                if (File.Exists(patchFatRead) && File.Exists(patchDatRead))
                {
                    outputFat = GetFat(patchFatRead);
                    patchDatEntries = GetDatEntries(outputFat, patchDatRead);
                }
                else
                {
                    throw new FileNotFoundException("Specified output patch archive does not exist");
                }
            }

            string commonDatRead = null;
            List<DatEntry> commonDatEntries = null;

            if (Config.Option_Combine)
            {
                commonFat = GetFat(commonFatRead);

                // read common dat
                commonDatRead = Path.ChangeExtension(commonFatRead, "dat");
                commonDatEntries = GetDatEntries(commonFat, commonDatRead);
            }

            // handle input files to be packed
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
        }
        */

    }
}
