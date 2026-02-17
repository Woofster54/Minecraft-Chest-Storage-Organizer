using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using fNbt;

namespace MinecraftStorage.Core.Services
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
                int index = i * 4;

                if (index + 3 >= locationTable.Length)
                    break;

                int offset = (locationTable[index] << 16) |
                             (locationTable[index + 1] << 8) |
                              locationTable[index + 2];

                int sectorCount = locationTable[index + 3];

                if (offset == 0 || sectorCount == 0)
                    continue;

                long chunkPosition = offset * 4096L;

                if (chunkPosition >= fs.Length)
                    continue;

                fs.Seek(chunkPosition, SeekOrigin.Begin);

                byte[] lengthBytes = br.ReadBytes(4);
                if (lengthBytes.Length < 4)
                    continue;

                int length = (lengthBytes[0] << 24) |
                             (lengthBytes[1] << 16) |
                             (lengthBytes[2] << 8) |
                              lengthBytes[3];

                if (length <= 0 || length > fs.Length)
                    continue;

                byte compressionType = br.ReadByte();

                byte[] compressedData = br.ReadBytes(length - 1);
                if (compressedData.Length < length - 1)
                    continue;

                if (compressionType != 2)
                    continue;

                using var ms = new MemoryStream(compressedData);
                using var zlib = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionMode.Decompress);
                using var output = new MemoryStream();
                zlib.CopyTo(output);

                using var chunkStream = new MemoryStream(output.ToArray());
                var nbt = new fNbt.NbtFile();
                nbt.LoadFromStream(chunkStream, fNbt.NbtCompression.None);

                chunks.Add(nbt);
            }

            return chunks;
        }
    }
}
