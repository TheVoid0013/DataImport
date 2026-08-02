using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DataImport.Data.Models
{
    [Index(nameof(RecordUniqueId), IsUnique = true)]
    [Index(nameof(SdnType), nameof(LastName))]
    [Table("SanctionDetails")]
    public class SanctionDetail
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string RecordUniqueId { get; set; } = null!;

        [StringLength(100)]
        public string? Country { get; set; }

        public string? XmlRecord { get; set; }

        public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(350)]
        public string LastName { get; set; } = null!;

        [StringLength(350)]
        public string? FirstName { get; set; }

        [Required]
        [StringLength(20)]
        public string SdnType { get; set; } = null!;

    }
}
