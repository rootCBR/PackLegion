using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackLegion.Values.Hashes
{
    public static class CRC64_WD2
    {
        public static ulong Compute(string value)
        {
            string str = value.Replace("/", "\\").ToLower();

            ulong hash64 = 0xCBF29CE484222325;

            foreach (char t in str)
            {
                hash64 *= (ulong)0x100000001B3;
                hash64 ^= (ulong)t;
            }

            return hash64 & 0x1FFFFFFFFFFFFFFF | 0xA000000000000000;
        }
    }
}
