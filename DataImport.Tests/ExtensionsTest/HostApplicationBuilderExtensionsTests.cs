

namespace DataImport.Tests.ExtensionsTest;

public class HostApplicationBuilderExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddSanctionsDbContext_throws_when_connection_string_is_missing_or_blank(string? value)
    {
        // DisableDefaults keeps appsettings.json, user-secrets and env vars out of the test,
        // so a real connection string on your machine can't make the "missing" case pass or flake.
        var builder = new HostApplicationBuilder(
            new HostApplicationBuilderSettings { DisableDefaults = true });

        if (value is not null)
            builder.Configuration["ConnectionStrings:SanctionsDb"] = value;

        var ex = Assert.Throws<InvalidOperationException>(() => builder.AddSanctionsDbContext());

        Assert.Contains("SanctionsDb", ex.Message);
    }
}