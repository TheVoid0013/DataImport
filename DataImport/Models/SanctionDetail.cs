using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DataImport.Models
{
    [Index(nameof(RecordUniqueId), IsUnique = true)]
    [Table("SanctionDetails")]
    public class SanctionDetail
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string RecordUniqueId { get; set; } = null!;

        [StringLength(25)]
        public string? Country { get; set; }

        public string? XmlRecord { get; set; }

        public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
