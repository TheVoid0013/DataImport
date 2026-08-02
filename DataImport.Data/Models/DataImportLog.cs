using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DataImport.Data.Models
{
    [Table("DataImportLogs")]
    [Index(nameof(RanAtUtc))]
    [Index(nameof(Succeeded), nameof(RanAtUtc))]
    public class DataImportLog
    {
        public int Id { get; set; }
        public DateTime RanAtUtc { get; set; }
        public int Parsed { get; set; }
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Unchanged { get; set; }
        public bool WasDownloaded { get; set; } 
        public bool Succeeded { get; set; }
        public string? ErrorMessage { get; set; } 

    }
}
