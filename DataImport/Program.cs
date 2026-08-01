using DataImport.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = configuration.GetConnectionString("SanctionsDb");

var optionsBuilder = new DbContextOptionsBuilder<SanctionsDbContext>();
optionsBuilder.UseSqlServer(connectionString);

using var db = new SanctionsDbContext(optionsBuilder.Options);

var canConnect = await db.Database.CanConnectAsync();
Console.WriteLine(canConnect
    ? "Connected to database successfully."
    : "Could not connect to database — check your connection string.");

var existingCount = canConnect ? await db.SanctionDetails.CountAsync() : 0;
Console.WriteLine($"Current SanctionDetails row count: {existingCount}");