using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Gibbed.IO;

namespace PackLegion
{
    /*
    struct FATHeader {
      uint32_t Magic;  //5TAF
      uint32_t Version;  //13
      uint32_t unk1;
      uint32_t unk2;
      uint32_t unk3;
      uint32_t unk4;
      uint32_t TotalFiles;
    };
    */

    public class Fat
    {
        private const int _signature = 0x46415435;

        public int version;

        private uint target;
        private uint platform;
        private byte unk70;

        public ulong unk5;
        public uint unk6;

        public List<FatEntry> entries;

        public Fat()
        {
            this.entries = new List<FatEntry>();
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);

            if (magic != _signature)
            {
                throw new FormatException("bad magic");
            }

            var version = input.ReadValueS32(Endian.Little);

            if (version != 13)
            {
                throw new FormatException("unsupported version");
            }

            var flags = input.ReadValueU32(Endian.Little);

            this.version = version;
            this.target = flags & 0xFF;
            this.platform = (flags >> 8) & 0xFF;
            this.unk70 = (byte)((flags >> 16) & 0xFF);
            this.unk5 = input.ReadValueU64(Endian.Little);
            this.unk6 = input.ReadValueU32(Endian.Little);

            uint entryCount = input.ReadValueU32(Endian.Little);

            FatEntrySerializer fatEntrySerializer = new FatEntrySerializer();

            for (uint i = 0; i < entryCount; i++)
            {
                FatEntry entry;

                fatEntrySerializer.Deserialize(input, Endian.Little, out entry);
                this.entries.Add(entry);

                //Console.WriteLine(entry.ToString());

                //Console.WriteLine($"Read DAT entry {i}: {entry.nameHash:X16}");
            }

            /*
            Console.WriteLine(string.Format("flags: {0}\nunk1: {1}\nunk2: {2}\nunk3: {3}\nunk4: {4}\nentryCount: {5}\nentries: {6}",
                flags,
                unk1,
                unk2,
                unk3,
                unk4,
                entryCount,
                entries.Count));
            */

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Deserialized FAT with {entryCount} entries");
            Console.ResetColor();
        }

        public void Serialize(Stream output)
        {
            var version = 13;

            //var unk1 = this.unk1;
            //var unk2 = this.unk2;
            //var unk3 = this.unk3;
            //var unk4 = this.unk4;
            var unk5 = this.unk5;
            var unk6 = this.unk6;

            var endian = Endian.Little;

            output.WriteValueU32(_signature, Endian.Little);
            output.WriteValueS32(version, Endian.Little);

            uint flags = 0;
            flags |= (uint) ((byte) target & 0xFF) << 0;
            flags |= (uint) ((byte) platform & 0xFF) << 8;
            flags |= (uint) (unk70 & 0xFF) << 16;
            output.WriteValueU32(flags, Endian.Little);

            output.WriteValueU64(unk5, Endian.Little);
            output.WriteValueU32(unk6, Endian.Little);

            FatEntrySerializer fatEntrySerializer = new FatEntrySerializer();

            var entrySerializer = fatEntrySerializer;

            int entryCount = this.entries.Count;

            output.WriteValueS32(entryCount, Endian.Little);

            for (int i = 0; i < this.entries.Count; i++)
            {
                FatEntry entry = this.entries[i];
                entrySerializer.Serialize(output, entry, endian);

                //Console.WriteLine($"Write DAT entry {i}: {entry.nameHash:X16}");
            }

            output.WriteValueU64(0, Endian.Little);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Serialized FAT with {entryCount} entries");
            Console.ResetColor();
        }
    }
}
