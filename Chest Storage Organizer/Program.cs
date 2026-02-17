namespace Chest_Storage_Organizer
{
    using Chest_Storage_Organizer.Data;
    using Chest_Storage_Organizer.Models;
    using Chest_Storage_Organizer.Services;
    using fNbt;
  

    internal class Program
    {
        static void Main(string[] args)
        {
            string basePath = @"C:\Users\mrste\AppData\Roaming\.minecraft\saves\F it we ball\region\";
            var allChestRecords = new List<ChestItemRecord>();
            string[] regions =
            {
            "r.-1.-1.mca",
            "r.-1.0.mca"
        };

            int MinX = -316;
            int MaxX = -228;
            int MinY = 67;
            int MaxY = 133;
            int MinZ = -54;
            int MaxZ = 70;

            var regionReader = new RegionFileReader();

            foreach (var regionFile in regions)
            {
                Console.WriteLine($"Scanning {regionFile}");

                var chunks = regionReader.ReadAllChunks(basePath + regionFile);

                foreach (var chunk in chunks)
                {
                    var root = chunk.RootTag;
                    var blockEntities = root["block_entities"] as NbtList;

                    if (blockEntities == null)
                        continue;

                    foreach (NbtCompound entity in blockEntities)
                    {
                        var idTag = entity["id"] as NbtString;
                        if (idTag == null)
                            continue;

                        if (idTag.Value != "minecraft:chest")
                            continue;

                        int x = (entity["x"] as NbtInt)?.Value ?? 0;
                        int y = (entity["y"] as NbtInt)?.Value ?? 0;
                        int z = (entity["z"] as NbtInt)?.Value ?? 0;
                        if (x >= MinX && x <= MaxX &&
                            y >= MinY && y <= MaxY &&
                            z >= MinZ && z <= MaxZ)
                        {
                            Console.WriteLine($"Chest found at ({x}, {y}, {z})");

                            var itemsTag = entity["Items"] as NbtList;

                            if (itemsTag == null)
                            {
                                Console.WriteLine("  (Empty chest)");
                                continue;
                            }

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
                                allChestRecords.Add(new ChestItemRecord
                                {
                                    ChestX = x,
                                    ChestY = y,
                                    ChestZ = z,
                                    ItemName = kvp.Key,
                                    Quantity = kvp.Value
                                });
                            }

                            foreach (var kvp in itemTotals.OrderBy(k => k.Key))
                            {
                                Console.WriteLine($"  - {kvp.Key} x{kvp.Value}");
                            }
                        }
                    }
                }
            }
            var exporter = new ExcelExportService();

            string outputPath = @"C:\Users\mrste\Desktop\BaseInventory.xlsx";

            exporter.Export(outputPath, allChestRecords);

            Console.WriteLine("Excel export complete.");


        }
    }
}
