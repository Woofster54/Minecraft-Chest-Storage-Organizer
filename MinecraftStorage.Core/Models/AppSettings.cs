using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MinecraftStorage.Core.Models
{
    public class AppSettings
    {
        public string WorldName { get; set; } = "";
        public int MinX { get; set; }
        public int MaxX { get; set; }
        public int MinY { get; set; }
        public int MaxY { get; set; }
        public int MinZ { get; set; }
        public int MaxZ { get; set; }

        public string GoogleCredentialPath { get; set; } = "";
        public string SpreadsheetId { get; set; } = "";
        public string SheetName { get; set; } = "Sheet1";

        public List<string> PriorityItems { get; set; } = new();

    }
}
