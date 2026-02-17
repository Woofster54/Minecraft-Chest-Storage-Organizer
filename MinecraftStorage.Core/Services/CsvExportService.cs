using System;
using System.Collections.Generic;
using System.Text;
using MinecraftStorage.Core.Models;
using System.Text;

namespace MinecraftStorage.Core.Services
{
    public class CsvExportService
    {
        public void Export(string filePath, List<ChestItemRecord> records)
        {
            var sb = new StringBuilder();

            var grouped = records
                .GroupBy(r => new { r.ChestX, r.ChestY, r.ChestZ })
                .OrderBy(g => g.Key.ChestX)
                .ThenBy(g => g.Key.ChestY)
                .ThenBy(g => g.Key.ChestZ);

            foreach (var chest in grouped)
            {
                sb.AppendLine($"Chest ({chest.Key.ChestX}, {chest.Key.ChestY}, {chest.Key.ChestZ})");
                sb.AppendLine("Item,Quantity");

                foreach (var item in chest.OrderBy(i => i.ItemName))
                {
                    sb.AppendLine($"{item.ItemName},{item.Quantity}");
                }

                sb.AppendLine(); // blank line between chests
            }

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
