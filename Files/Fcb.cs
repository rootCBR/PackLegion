﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Gibbed.IO;

namespace PackLegion
{
    // Source: Gibbed.Disrupt
    public struct Fcb
    {
        public ushort version;
        public ushort flags;
        public FcbEntry root;

        public void Serialize(Stream output)
        {
            if (this.version != 3)
            {
                throw new FormatException("unsupported file version");
            }

            if (this.flags != 0)
            {
                //throw new FormatException("unsupported file flags");
            }

            var endian = Endian.Little;
            uint signature = 0x4643626E;

            using (var data = new MemoryStream())
            {
                uint totalObjectCount = 0, totalValueCount = 0;

                this.root.Serialize(data, ref totalObjectCount,
                                    ref totalValueCount,
                                    endian);
                data.Flush();
                data.Position = 0;

                // write magic
                output.WriteValueU32(signature, endian);

                // write version
                output.WriteValueU16(this.version, endian);

                // write everything else
                output.WriteValueU16(this.flags, endian);
                output.WriteValueU32(totalObjectCount, endian);
                output.WriteValueU32(totalValueCount, endian);
                output.WriteFromStream(data, data.Length);
            }
        }

        public void Deserialize(Stream input)
        {
            input.Position = 0;

            var endian = Endian.Little;
            uint signature = 0x4643626E;

            var magic = input.ReadValueU32(endian);

            if (magic != signature)
            {
                throw new FormatException("invalid header magic");
            }

            var version = input.ReadValueU16(endian);

            if (version != 3)
            {
                throw new FormatException("unsupported file version");
            }

            var flags = input.ReadValueU16(endian);

            if (flags != 0)
            {
                //throw new FormatException("unsupported file flags");
            }

            var totalObjectCount = input.ReadValueU32(endian);
            var totalValueCount = input.ReadValueU32(endian);

            var pointers = new List<FcbEntry>();

            this.version = version;
            this.flags = flags;
            this.root = FcbEntry.Deserialize(null, input, pointers, endian);
        }

        public void Combine(Fcb newFcb)
        {
            string BytesToString(byte[] bytes)
            {
                return BitConverter.ToString(bytes).Replace("-", "");
            }

            //Console.WriteLine("Combining FCBs");

            //Console.WriteLine("Old root: " + this.root.Children.Count);

            uint nameFieldKey = 0x389F6DA7;

            List<FcbEntry> entries = this.root.Children;
            List<FcbEntry> newEntries = newFcb.root.Children;

            for (int a = 0; a < entries.Count; a++)
            {
                FcbEntry existingEntry = entries[a];

                for (int b = 0; b < newEntries.Count; b++)
                {
                    FcbEntry newEntry = newEntries[b];

                    byte[] o_newEntryBytes = new byte[0];
                    byte[] o_existingEntryBytes = new byte[0];

                    if (newEntry.Fields.TryGetValue(nameFieldKey, out o_newEntryBytes) && existingEntry.Fields.TryGetValue(nameFieldKey, out o_existingEntryBytes))
                    {
                        if (BytesToString(o_newEntryBytes) == BytesToString(o_existingEntryBytes))
                        {
                            entries[a] = newEntry;
                            newEntries.RemoveAt(b);

                            //Utility.Log.ToConsole($"Replaced FCB entry: {BytesToString(existingEntry.Fields[nameFieldKey])}");
                        }
                    }
                }
            }

            foreach (FcbEntry newEntry in newEntries)
            {
                entries.Add(newEntry);

                //Console.WriteLine($"Added FCB entry: {BytesToString(newEntry.Fields[nameFieldKey])}");
            }

            this.root.Children = new List<FcbEntry>();

            foreach (FcbEntry entry in entries)
            {
                this.root.Children.Add(entry);
            }

            //Console.WriteLine("New root: " + this.root.Children.Count);
        }
    }
}
