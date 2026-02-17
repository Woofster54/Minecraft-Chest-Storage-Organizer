using System;
using System.Collections.Generic;
using System.Text;
using fNbt;
using MinecraftStorage.Core.Models;

namespace MinecraftStorage.Core.Services
{
    public class NbtInventoryService
    {
        public List<InventoryItem> ExtractPlayerInventory(string playerFilePath)
        {

            var items = new List<InventoryItem>();

            var nbtFile = new NbtFile();
            nbtFile.LoadFromFile(playerFilePath);

            var root = nbtFile.RootTag;
            var inventoryTag = root["Inventory"] as NbtList;

            if (inventoryTag == null)
            {
                Console.WriteLine("Inventory tag not found.");
                return items;
            }

            foreach (NbtCompound item in inventoryTag)
            {
                if (!item.Contains("id") || !item.Contains("count"))
                    continue;

                var idTag = item["id"] as NbtString;
                var countTag = item["count"] as NbtInt;

                if (idTag == null || countTag == null)
                    continue;

                items.Add(new InventoryItem
                {
                    ItemName = idTag.Value,
                    Quantity = countTag.Value,
                    Source = "Player"
                });
            }

            return items;
        }

    }
}
