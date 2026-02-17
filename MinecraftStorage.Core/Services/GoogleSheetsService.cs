using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using MinecraftStorage.Core.Models;

namespace MinecraftStorage.Core.Services
{
    public class GoogleSheetsService
    {
        private readonly string _credentialPath;
        private readonly string _spreadsheetId;

        public GoogleSheetsService(string credentialPath, string spreadsheetId)
        {
            _credentialPath = credentialPath;
            _spreadsheetId = spreadsheetId;
        }

        public void UpdateSheet(List<ChestItemRecord> records)
        {
            var credential = GoogleCredential.FromFile(_credentialPath)
                .CreateScoped(SheetsService.Scope.Spreadsheets);

            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Minecraft Storage Sync"
            });
            var spreadsheet = service.Spreadsheets.Get(_spreadsheetId).Execute();

            int sheetId = spreadsheet.Sheets.First().Properties.SheetId.Value;


            var grouped = records
      .GroupBy(r => new { r.ChestX, r.ChestY, r.ChestZ })
      .OrderBy(g => g.Key.ChestX)
      .ThenBy(g => g.Key.ChestY)
      .ThenBy(g => g.Key.ChestZ)
      .ToList();

            var values = new List<IList<object>>();

            int chestsPerRow = 4;
            int rowBlockHeight = 25;

            int totalRowsNeeded = ((grouped.Count + chestsPerRow - 1) / chestsPerRow) * rowBlockHeight;

            for (int i = 0; i < totalRowsNeeded; i++)
            {
                values.Add(new List<object>());
            }

            for (int i = 0; i < grouped.Count; i++)
            {
                var chest = grouped[i];

                int rowBlockStart = (i / chestsPerRow) * rowBlockHeight;
                int columnBlockStart = (i % chestsPerRow) * 3;

                EnsureColumnWidth(values, rowBlockStart, columnBlockStart + 2);

                values[rowBlockStart][columnBlockStart] =
                    $"Chest ({chest.Key.ChestX}, {chest.Key.ChestY}, {chest.Key.ChestZ})";

                int currentRow = rowBlockStart + 1;

                foreach (var item in chest.OrderBy(i => i.ItemName))
                {
                    EnsureColumnWidth(values, currentRow, columnBlockStart + 1);

                    values[currentRow][columnBlockStart] = item.ItemName;
                    values[currentRow][columnBlockStart + 1] = item.Quantity;

                    currentRow++;
                }
            }
            // Clear entire sheet first
            var clearRequest = service.Spreadsheets.Values.Clear(
                new Google.Apis.Sheets.v4.Data.ClearValuesRequest(),
                _spreadsheetId,
                "A1:ZZ1000");   // Large enough range
            clearRequest.Execute();
            var valueRange = new ValueRange
            {
                Values = values
            };

            var updateRequest = service.Spreadsheets.Values.Update(
                valueRange,
                _spreadsheetId,
                "A1");

            updateRequest.ValueInputOption =
                SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            updateRequest.Execute();
            var requests = new List<Request>();

for (int i = 0; i < grouped.Count; i++)
{
    int rowBlockStart = (i / chestsPerRow) * rowBlockHeight;
    int columnBlockStart = (i % chestsPerRow) * 3;

    requests.Add(new Request
    {
        RepeatCell = new RepeatCellRequest
        {
            Range = new GridRange
            {
                SheetId = sheetId,
                StartRowIndex = rowBlockStart,
                EndRowIndex = rowBlockStart + 1,
                StartColumnIndex = columnBlockStart,
                EndColumnIndex = columnBlockStart + 2
            },
            Cell = new CellData
            {
                UserEnteredFormat = new CellFormat
                {
                    BackgroundColor = new Color { Red = 0, Green = 0, Blue = 0 },
                    TextFormat = new TextFormat
                    {
                        Bold = true,
                        ForegroundColor = new Color { Red = 1, Green = 1, Blue = 1 }
                    }
                }
            },
            Fields = "userEnteredFormat(backgroundColor,textFormat)"
        }
    });
}

var batchUpdate = new BatchUpdateSpreadsheetRequest
{
    Requests = requests
};

service.Spreadsheets.BatchUpdate(batchUpdate, _spreadsheetId).Execute();

        }
        private void EnsureColumnWidth(List<IList<object>> values, int row, int column)
        {
            while (values[row].Count <= column)
            {
                values[row].Add("");
            }
        }
    }
}
