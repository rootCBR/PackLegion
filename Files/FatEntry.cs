using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PackLegion
{
    /*
    struct SFatFileEntry{
        // uint32_t    field_0;
        // uint32_t    field_4;
        // uint32_t    field_8;
        // uint32_t    field_c;
        // uint32_t    field_10;
        uint64_t Hash;
        uint64_t CompressedSize : 30;
        uint64_t Offset : 34;
        uint32_t CompressionMethod : 2;
        uint32_t UncompressedSize : 30;
    };
    */

    public struct FatEntry
    {
        public ulong nameHash;
        public ulong uncompressedSize;
        public ulong compressedSize;
        public ulong offset;
        public uint compressionScheme;

        public override string ToString()
        {
            string resolvedFileName = Values.FileNameHash.ResolveHash(this.nameHash);

            /*
            if (!string.IsNullOrEmpty(resolvedFileName))
            {
                return string.Format("{0:X16} ({1}) @ {2}, {3} bytes ({4} compressed bytes, scheme {5})",
                    this.nameHash,
                    resolvedFileName,
                    this.offset,
                    this.uncompressedSize,
                    this.compressedSize,
                    this.compressionScheme);
            }
            */

            return string.Format("{0:X16} @ {1}, {2} bytes ({3} compressed bytes, scheme {4})",
                this.nameHash,
                this.offset,
                this.uncompressedSize,
                this.compressedSize,
                this.compressionScheme);
        }

        public string GetName()
        {
            string resolvedName = Values.FileNameHash.ResolveHash(this.nameHash);

            if (string.IsNullOrEmpty(resolvedName))
            {
                return $"0x{this.nameHash:X16}";
            }

            return resolvedName;
        }
    }
}
