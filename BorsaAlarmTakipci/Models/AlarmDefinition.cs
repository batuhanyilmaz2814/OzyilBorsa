using SQLite;
using System;

namespace BorsaAlarmTakipci.Models
{
    public class AlarmDefinition
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string StockSymbol { get; set; }

        public double? UpperLimit { get; set; }

        public double? LowerLimit { get; set; }

        // Eksik özellikler eklendi
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
