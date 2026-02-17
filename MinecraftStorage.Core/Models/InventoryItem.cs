using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftStorage.Core.Models
{
    public class InventoryItem
    {
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public string Source { get; set; }
    }
}
