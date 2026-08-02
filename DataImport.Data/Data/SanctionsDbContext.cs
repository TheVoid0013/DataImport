using DataImport.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataImport.Data.Data
{
    public class SanctionsDbContext : DbContext
    {
        public DbSet<SanctionDetail> SanctionDetails => Set<SanctionDetail>();

        public DbSet<DataImportLog> DataImportLogs => Set<DataImportLog>();


        public SanctionsDbContext(DbContextOptions<SanctionsDbContext> options) : base(options)
        {
        }


        /// <summary>
        /// Learning: Always define the table name, along side the indexes at the model itself.
        /// For more undersatnding, look at the SanctionDetail model class.
        /// The new models have also been defined with the table name and indexes.
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


            modelBuilder.Entity<DataImportLog>(entity =>
            {
                entity.Property(e => e.RanAtUtc)
                      .IsRequired();
            });

        }
     }
}
