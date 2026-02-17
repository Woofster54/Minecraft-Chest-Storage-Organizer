using System;
using System.Collections.Generic;
using System.Text;
using ClosedXML.Excel;
using Chest_Storage_Organizer.Models;

namespace Chest_Storage_Organizer.Services
{
    public class ExcelExportService
    {
        public void Export(string filePath, List<ChestItemRecord> records)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Base Inventory");

            int currentRow = 1;

            var grouped = records
                .GroupBy(r => new { r.ChestX, r.ChestY, r.ChestZ })
                .OrderBy(g => g.Key.ChestX)
                .ThenBy(g => g.Key.ChestY)
                .ThenBy(g => g.Key.ChestZ);

            int chestsPerRow = 4;
            int chestIndex = 0;

            foreach (var chest in grouped)
            {
                int rowBlockStart = (chestIndex / chestsPerRow) * 25 + 1;
                int columnBlockStart = (chestIndex % chestsPerRow) * 3 + 1;

                int chestRow = rowBlockStart;

                // Chest title
                worksheet.Cell(chestRow, columnBlockStart).Value =
                    $"Chest ({chest.Key.ChestX}, {chest.Key.ChestY}, {chest.Key.ChestZ})";

                var titleRange = worksheet.Range(chestRow, columnBlockStart, chestRow, columnBlockStart + 1);
                titleRange.Style.Fill.BackgroundColor = XLColor.Black;
                titleRange.Style.Font.FontColor = XLColor.White;
                titleRange.Style.Font.Bold = true;

                chestRow++;

                foreach (var item in chest.OrderBy(i => i.ItemName))
                {
                    worksheet.Cell(chestRow, columnBlockStart).Value = item.ItemName;
                    worksheet.Cell(chestRow, columnBlockStart + 1).Value = item.Quantity;
                    chestRow++;
                }

                chestIndex++;

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

    }
}
