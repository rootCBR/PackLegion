using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackLegion
{
    public class DatEntry
    {
        public bool added;
        public ulong hash;
        public string name;
        public byte[] content;
        public FatEntry fatEntry;

        public string GetName()
        {
            string resolvedName = Values.FileNameHash.ResolveHash(this.fatEntry.nameHash);

            if (string.IsNullOrEmpty(resolvedName))
            {
                return $"0x{this.fatEntry.nameHash:X16}";
            }

            return resolvedName;
        }
    }
}
