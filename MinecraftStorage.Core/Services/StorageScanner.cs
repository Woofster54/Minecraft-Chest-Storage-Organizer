using System;
using System.Collections.Generic;
using System.Text;
using MinecraftStorage.Core.Models;
using fNbt;

namespace MinecraftStorage.Core.Services
{
    public class StorageScanner
    {
        public List<ChestItemRecord> ScanBase(
            string regionFolderPath,
            int minX, int maxX,
            int minY, int maxY,
            int minZ, int maxZ, IProgress<int>? progress = null)
        {
            var allRecords = new List<ChestItemRecord>();

            var regionReader = new RegionFileReader();
            int minRegionX = (int)Math.Floor(minX / 512.0);
            int maxRegionX = (int)Math.Floor(maxX / 512.0);
            int minRegionZ = (int)Math.Floor(minZ / 512.0);
            int maxRegionZ = (int)Math.Floor(maxZ / 512.0);

            var regionFiles = new List<string>();

            for (int rx = minRegionX; rx <= maxRegionX; rx++)
            {
                for (int rz = minRegionZ; rz <= maxRegionZ; rz++)
                {
                    string regionFile = Path.Combine(regionFolderPath, $"r.{rx}.{rz}.mca");
                    if (File.Exists(regionFile))
                    {
                        regionFiles.Add(regionFile);
                    }
                }
            }
            int totalRegions = regionFiles.Count;
            int processedRegions = 0;

            foreach (var regionPath in regionFiles)
            {
                var chunks = regionReader.ReadAllChunks(regionPath);

                foreach (var chunk in chunks)
                {
                    var root = chunk.RootTag;
                    var blockEntities = root["block_entities"] as NbtList;

                    if (blockEntities == null)
                        continue;

                    foreach (NbtCompound entity in blockEntities)
                    {
                        var idTag = entity["id"] as NbtString;
                        if (idTag == null || idTag.Value != "minecraft:chest")
                            continue;

                        int x = (entity["x"] as NbtInt)?.Value ?? 0;
                        int y = (entity["y"] as NbtInt)?.Value ?? 0;
                        int z = (entity["z"] as NbtInt)?.Value ?? 0;

                        if (x < minX || x > maxX ||
                            y < minY || y > maxY ||
                            z < minZ || z > maxZ)
                            continue;

                        var itemsTag = entity["Items"] as NbtList;
                        if (itemsTag == null)
                            continue;

                        var itemTotals = new Dictionary<string, int>();

                        foreach (NbtCompound item in itemsTag)
                        {
                            var itemIdTag = item["id"] as NbtString;
                            var itemCountTag = item["count"] as NbtInt;

                            if (itemIdTag == null || itemCountTag == null)
                                continue;

                            string itemName = itemIdTag.Value.Replace("minecraft:", "");
                            int count = itemCountTag.Value;

                            if (itemTotals.ContainsKey(itemName))
                                itemTotals[itemName] += count;
                            else
                                itemTotals[itemName] = count;
                        }

                        foreach (var kvp in itemTotals)
                        {
                            allRecords.Add(new ChestItemRecord
                            {
                                ChestX = x,
                                ChestY = y,
                                ChestZ = z,
                                ItemName = kvp.Key,
                                Quantity = kvp.Value
                            });
                        }
                    }
                }
                processedRegions++;

                int percent = (int)((processedRegions / (double)totalRegions) * 100);
                progress?.Report(percent);
            }

            return allRecords;
        }
    }
}
