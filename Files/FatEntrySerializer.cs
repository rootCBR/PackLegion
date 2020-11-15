using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Gibbed.IO;

namespace PackLegion
{
    internal class FatEntrySerializer
    {
        // [h] hash = 64 bits
        // [u] uncompressed size = 30 bits
        // [s] compression scheme = 2 bits
        // [o] offset = 34 bits
        // [c] compressed size = 30 bits

        public void Serialize(Stream output, FatEntry entry, Endian endian)
        {
            ulong hash = entry.nameHash;

            ulong compressedSizeAndOffset = 0;
            compressedSizeAndOffset |= (entry.compressedSize & 0x1FFFFFFFu) << 0;
            compressedSizeAndOffset |= entry.offset << 30;

            uint compressionMethodAndUncompressedSize = 0;
            compressionMethodAndUncompressedSize |= entry.compressionScheme & 3;
            compressionMethodAndUncompressedSize |= (uint) entry.uncompressedSize << 2;

            output.WriteValueU64(hash, endian);
            output.WriteValueU64(compressedSizeAndOffset, endian);
            output.WriteValueU32(compressionMethodAndUncompressedSize, endian);

            /*
            Console.WriteLine(string.Format("Serializing {0:X16}:", entry.nameHash));
            Console.WriteLine(string.Format("\tcompressedSize:\n\t\ta = {0}\n\t\tb = {1}\n\t\tc = {2}\n\t\td = {3}",
                Convert.ToString(entry.compressedSize),
                Convert.ToString((long)entry.compressedSize, toBase: 2),
                Convert.ToString((long)compressedSizeAndOffset, toBase: 2),
                compressedSizeAndOffset));
            Console.WriteLine(string.Format("\toffset:\n\t\ta = {0}\n\t\tb = {1}\n\t\tc = {2}\n\t\td = {3}",
                Convert.ToString(entry.offset),
                Convert.ToString((long)entry.offset, toBase: 2),
                Convert.ToString((long)compressedSizeAndOffset, toBase: 2),
                compressedSizeAndOffset));
            Console.WriteLine(string.Format("\tcompressionScheme:\n\t\ta = {0}\n\t\tb = {1}\n\t\tc = {2}\n\t\td = {3}",
                Convert.ToString(entry.compressionScheme),
                Convert.ToString(entry.compressionScheme, toBase: 2),
                Convert.ToString(compressionMethodAndUncompressedSize, toBase: 2),
                compressionMethodAndUncompressedSize));
            Console.WriteLine(string.Format("\tuncompressedSize:\n\t\ta = {0}\n\t\tb = {1}\n\t\tc = {2}\n\t\td = {3}",
                Convert.ToString(entry.uncompressedSize),
                Convert.ToString((long)entry.uncompressedSize, toBase: 2),
                Convert.ToString(compressionMethodAndUncompressedSize, toBase: 2),
                compressionMethodAndUncompressedSize));
            */
        }

        public void Deserialize(Stream input, Endian endian, out FatEntry entry)
        {
            var hash = input.ReadValueU64(endian);
            var compressedSizeAndOffset = input.ReadValueU64(endian);
            var compressionMethodAndUncompressedSize = input.ReadValueU32(endian);

            entry.nameHash = hash;
            entry.uncompressedSize = compressionMethodAndUncompressedSize >> 2;
            entry.compressionScheme = compressionMethodAndUncompressedSize & 3;
            entry.offset = compressedSizeAndOffset >> 30;
            entry.compressedSize = compressedSizeAndOffset & 0x3fffffff;

            /*
            Console.WriteLine(string.Format("Deserializing {0:X16}:", entry.nameHash));
            Console.WriteLine(string.Format("\tcompressedSize:\n\t\ta = {0}\n\t\tb = {1}\n\t\tc = {2}\n\t\td = {3}",
                compressedSizeAndOffset,
                Convert.ToString((long) compressedSizeAndOffset, toBase: 2),
                Convert.ToString((long) entry.compressedSize, toBase: 2),
                Convert.ToString(entry.compressedSize)));
            Console.WriteLine(string.Format("\toffset:\n\t\ta = {0}\n\t\tb = {1}\n\t\tc = {2}\n\t\td = {3}",
                compressedSizeAndOffset,
                Convert.ToString((long) compressedSizeAndOffset, toBase: 2),
                Convert.ToString((long) entry.offset, toBase: 2),
                Convert.ToString(entry.offset)));
            Console.WriteLine(string.Format("\tcompressionScheme:\n\t\ta = {0}\n\t\tb = {1}\n\t\tc = {2}\n\t\td = {3}",
                compressionMethodAndUncompressedSize,
                Convert.ToString(compressionMethodAndUncompressedSize, toBase: 2),
                Convert.ToString(entry.compressionScheme, toBase: 2),
                Convert.ToString(entry.compressionScheme)));
            Console.WriteLine(string.Format("\tuncompressedSize:\n\t\ta = {0}\n\t\tb = {1}\n\t\tc = {2}\n\t\td = {3}",
                compressionMethodAndUncompressedSize,
                Convert.ToString(compressionMethodAndUncompressedSize, toBase: 2),
                Convert.ToString((long) entry.uncompressedSize, toBase: 2),
                Convert.ToString(entry.uncompressedSize)));
            */
        }
    }
}
