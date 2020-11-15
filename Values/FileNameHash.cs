using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PackLegion.Values
{
    using HashLookup = Dictionary<ulong, string>;

    public static class FileNameHash
    {
        public static readonly string lookupFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filelist.txt");

        private static HashLookup lookup = new HashLookup();

        public static void Initialize()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, lookupFile);

            if (!File.Exists(path))
            {
                return;
            }

            var lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var hash = Hashes.CRC64_WD2.Compute(line);

                //Console.WriteLine($"{hash:X16} = \"{line}\"");

                if (line.Length == 0)
                {
                    continue;
                }

                AddToLookup(line);
            }
        }

        public static void AddToLookup(ulong hash, string value)
        {
            if (!lookup.ContainsKey(hash))
            {
                lookup.Add(hash, value);
            }
        }

        public static void AddToLookup(string value)
        {
            var hash = Hashes.CRC64_WD2.Compute(value);
            AddToLookup(hash, value);
        }

        public static string ResolveHash(ulong hash)
        {
            if (lookup.ContainsKey(hash))
            {
                return lookup[hash];
            }

            return null;
        }
    }
}
