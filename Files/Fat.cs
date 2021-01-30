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
        private const Endian endian = Endian.Little;
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
            input.Position = 0;

            var magic = input.ReadValueU32(endian);

            if (magic != _signature)
            {
                throw new FormatException("Bad magic");
            }

            var version = input.ReadValueS32(endian);

            if (version != 13)
            {
                throw new FormatException("Unsupported version");
            }

            var flags = input.ReadValueU32(endian);

            this.version = version;
            this.target = flags & 0xFF;
            this.platform = (flags >> 8) & 0xFF;
            this.unk70 = (byte)((flags >> 16) & 0xFF);
            this.unk5 = input.ReadValueU64(endian);
            this.unk6 = input.ReadValueU32(endian);

            uint entryCount = input.ReadValueU32(endian);

            FatEntry fatEntry = new FatEntry();

            for (uint i = 0; i < entryCount; i++)
            {
                FatEntry entry;

                fatEntry.Deserialize(input, endian, out entry);
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

            //Utility.Log.ToConsole($"Deserialized FAT with {entryCount} entries");
        }

        public void Serialize(Stream output)
        {
            output.Position = 0;

            var version = 13;

            //var unk1 = this.unk1;
            //var unk2 = this.unk2;
            //var unk3 = this.unk3;
            //var unk4 = this.unk4;
            var unk5 = this.unk5;
            var unk6 = this.unk6;

            output.WriteValueU32(_signature, endian);
            output.WriteValueS32(version, endian);

            uint flags = 0;
            flags |= (uint) ((byte) target & 0xFF) << 0;
            flags |= (uint) ((byte) platform & 0xFF) << 8;
            flags |= (uint) (unk70 & 0xFF) << 16;

            output.WriteValueU32(flags, endian);
            output.WriteValueU64(unk5, endian);
            output.WriteValueU32(unk6, endian);

            int entryCount = this.entries.Count;

            output.WriteValueS32(entryCount, endian);

            FatEntry fatEntry = new FatEntry();

            for (int i = 0; i < this.entries.Count; i++)
            {
                FatEntry entry = this.entries[i];
                fatEntry.Serialize(output, entry, endian);

                //Console.WriteLine($"Write DAT entry {i}: {entry.nameHash:X16}");
            }

            output.WriteValueU64(0, endian);

            //Utility.Log.ToConsole($"Serialized FAT with {entryCount} entries");
        }
    }
}
