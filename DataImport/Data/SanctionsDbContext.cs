using DataImport.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataImport.Data
{
    public class SanctionsDbContext : DbContext
    {
        public DbSet<SanctionDetail> SanctionDetails => Set<SanctionDetail>();


        public SanctionsDbContext(DbContextOptions<SanctionsDbContext> options) : base(options)
        {
        }


        /// <summary>
        /// Learning: Always define the table name, along side the indexes at the model itself.
        /// For more undersatnding, look at the SanctionDetail model class.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SanctionDetail>(entity =>
            {
                entity.Property(e => e.XmlRecord)
                      .HasColumnType("xml")
                      .IsRequired();

                entity.Property(e => e.ImportedAtUtc)
                      .IsRequired();
            });
        }
     }
}
