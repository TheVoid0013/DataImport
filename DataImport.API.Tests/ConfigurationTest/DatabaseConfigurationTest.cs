namespace DataImport.API.Tests.ConfigurationTest;

public class DatabaseConfigurationTest
{
    private const string ConnectionString =
        "Server=localhost;Database=Sanctions;Trusted_Connection=True;TrustServerCertificate=True";

    private static IConfiguration BuildConfiguration(string? connectionString)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:SanctionsDb"] = connectionString
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static SanctionsDbContext ResolveContext(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddDatabaseConfiguration(configuration);

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<SanctionsDbContext>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddDatabaseConfiguration_MissingConnectionString_Throws(string? connectionString)
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString);

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddDatabaseConfiguration(configuration));

        Assert.Contains("ConnectionStrings:SanctionsDb", ex.Message);
    }

    [Fact]
    public void AddDatabaseConfiguration_ValidConnectionString_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddDatabaseConfiguration(BuildConfiguration(ConnectionString));

        Assert.Same(services, result);
    }

    [Fact]
    public void AddDatabaseConfiguration_ValidConnectionString_RegistersDbContext()
    {
        var services = new ServiceCollection();

        services.AddDatabaseConfiguration(BuildConfiguration(ConnectionString));

        Assert.Contains(services, d => d.ServiceType == typeof(SanctionsDbContext));
    }

    [Fact]
    public void AddDatabaseConfiguration_ValidConnectionString_UsesConfiguredConnectionString()
    {
        using var context = ResolveContext(BuildConfiguration(ConnectionString));

        var expected = new SqlConnectionStringBuilder(ConnectionString);
        var actual = new SqlConnectionStringBuilder(context.Database.GetConnectionString());

        Assert.Equal(expected.DataSource, actual.DataSource);
        Assert.Equal(expected.InitialCatalog, actual.InitialCatalog);
    }

    [Fact]
    public void AddDatabaseConfiguration_ValidConnectionString_SetsCommandTimeoutTo60Seconds()
    {
        using var context = ResolveContext(BuildConfiguration(ConnectionString));

        Assert.Equal(60, context.Database.GetCommandTimeout());
    }

    [Fact]
    public void AddDatabaseConfiguration_ValidConnectionString_EnablesRetryOnFailure()
    {
        using var context = ResolveContext(BuildConfiguration(ConnectionString));

        var strategy = context.Database.CreateExecutionStrategy();

        Assert.True(strategy.RetriesOnFailure);
    }
}