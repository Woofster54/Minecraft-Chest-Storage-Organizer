using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using fNbt;

namespace Chest_Storage_Organizer.Services
{
    internal class RegionFileReader
    {
        public List<NbtFile> ReadAllChunks(string regionPath)
        {
            var chunks = new List<NbtFile>();

            using var fs = new FileStream(regionPath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            // First 4096 bytes = chunk locations
            byte[] locationTable = br.ReadBytes(4096);

            for (int i = 0; i < 1024; i++)
            {
                int offset = (locationTable[i * 4] << 16) |
                             (locationTable[i * 4 + 1] << 8) |
                             (locationTable[i * 4 + 2]);

                int sectorCount = locationTable[i * 4 + 3];

                if (offset == 0 || sectorCount == 0)
                    continue;

                long chunkPosition = offset * 4096L;
                fs.Seek(chunkPosition, SeekOrigin.Begin);

                byte[] lengthBytes = br.ReadBytes(4);
                if (lengthBytes.Length < 4)
                    continue;

                int length = (lengthBytes[0] << 24) |
                             (lengthBytes[1] << 16) |
                             (lengthBytes[2] << 8) |
                              lengthBytes[3];
                byte compressionType = br.ReadByte();

                byte[] compressedData = br.ReadBytes(length - 1);

                byte[] decompressedData;

                if (compressionType == 2) // Zlib
                {
                    using var ms = new MemoryStream(compressedData);
                    using var zlib = new ZLibStream(ms, CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    zlib.CopyTo(output);
                    decompressedData = output.ToArray();
                }
                else
                {
                    continue;
                }

                using var chunkStream = new MemoryStream(decompressedData);
                var nbt = new NbtFile();
                nbt.LoadFromStream(chunkStream, NbtCompression.None);

                chunks.Add(nbt);
            }

            return chunks;
        }
    }
}
