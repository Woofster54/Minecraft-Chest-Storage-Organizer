using System;
using System.Collections.Generic;
using System.Text;

namespace Chest_Storage_Organizer.Models
{
    public class ChestItemRecord
    {
        public int ChestX { get; set; }
        public int ChestY { get; set; }
        public int ChestZ { get; set; }

        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }
}
