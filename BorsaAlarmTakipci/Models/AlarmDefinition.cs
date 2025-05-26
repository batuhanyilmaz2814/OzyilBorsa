using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace BorsaAlarmTakipci.Models
{
    [Table("AlarmDefinitions")] //SQLite'daki tablonun adı
    public class AlarmDefinition
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } // Alarm tanımının benzersiz kimliği

        [NotNull]

        public string StockSymbol { get; set; } // Hisse senedi sembolü

        public double? UpperLimit { get; set; } // Üst limit fiyatı

        public double? LowerLimit { get; set; } // Alt limit fiyatı

        public bool IsEnabled { get; set; } = true; // Alarmın etkin olup olmadığı

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Alarm tanımının oluşturulma tarihi


    }
}
