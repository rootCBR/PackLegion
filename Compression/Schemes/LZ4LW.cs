using System;
using System.IO;
using Gibbed.IO;

namespace PackLegion.Compression.Schemes
{
    public static class LZ4LW
    {
        public static void Decompress(Stream input, int compressedSize, Stream output, int decompressedSize)
        {
            int safeDecodingDistance = ReadPackedS32(input, out var headerSize);

            byte[] buffer = new byte[decompressedSize];
            int startOffset = decompressedSize - compressedSize + headerSize;
            input.Read(buffer, startOffset, compressedSize - headerSize);

            DecompressInPlace(buffer, startOffset, decompressedSize - safeDecodingDistance);

            output.Write(buffer, 0, decompressedSize);
        }

        private static void DecompressInPlace(byte[] buffer, int inputStartPosition, int safeDecodingOffset)
        {
            int inputPosition = inputStartPosition;
            int outputPosition = 0;

            while (true)
            {
                if (outputPosition >= safeDecodingOffset && outputPosition >= inputPosition)
                {
                    break;
                }

                var token = buffer[inputPosition++];
                int literalLength = token >> 4;

                if (literalLength == 0x0F)
                {
                    byte tempToken;

                    do
                    {
                        tempToken = buffer[inputPosition++];

                        literalLength += tempToken;
                    } while (tempToken == 255);
                }

                if (literalLength > 0)
                {
                    Buffer.BlockCopy(buffer, inputPosition, buffer, outputPosition, literalLength);

                    inputPosition += literalLength;
                    outputPosition += literalLength;
                }

                var offset1 = buffer[inputPosition++];
                var offset2 = buffer[inputPosition++];
                var offset = offset1 | (offset2 << 8);

                if (offset >= 0xE000)
                {
                    int extra = buffer[inputPosition++];
                    offset += extra << 13;
                }

                var matchLength = token & 0xF;

                if (matchLength == 0x0F)
                {
                    byte tempToken;

                    do
                    {
                        tempToken = buffer[inputPosition++];

                        matchLength += tempToken;
                    } while (tempToken == 255);
                }

                matchLength += 4;

                for (int i = 0; i < matchLength; i++)
                {
                    buffer[outputPosition] = buffer[outputPosition - offset];
                    outputPosition++;
                }
            }
        }

        //Source: Gibbed.Disrupt
        private static int ReadPackedS32(Stream input, out int read)
        {
            read = 1;
            byte b = input.ReadValueU8();
            int value = b & 0x7F;
            int shift = 7;

            while ((b & 0x80) != 0)
            {
                if (shift > 21)
                {
                    throw new InvalidOperationException();
                }
                read++;
                b = input.ReadValueU8();
                value |= (b & 0x7F) << shift;
            }
            return value;
        }
    }
}
