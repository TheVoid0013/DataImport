
namespace DataImport.Tests.DiagnosticsTest;

public class MethodTimeLoggerTests
{
    // Any real method works; we just need a MethodBase with a name and declaring type.
    private static MethodBase SampleMethod() =>
        typeof(MethodTimeLoggerTests).GetMethod(nameof(SampleMethod),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public void Logs_method_name_elapsed_and_message_under_declaring_type_category()
    {
        var factory = new CapturingLoggerFactory();
        MethodTimeLogger.Configure(factory);

        // Whole milliseconds so the formatted text isn't culture-dependent.
        MethodTimeLogger.Log(SampleMethod(), TimeSpan.FromMilliseconds(250), "extra");

        var entry = Assert.Single(factory.Entries);
        Assert.Equal(typeof(MethodTimeLoggerTests).FullName, entry.Category);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("SampleMethod took 250ms extra", entry.Message);
    }

    [Fact]
    public void Does_not_throw_when_no_message_is_supplied()
    {
        var factory = new CapturingLoggerFactory();
        MethodTimeLogger.Configure(factory);

        var ex = Record.Exception(() =>
            MethodTimeLogger.Log(SampleMethod(), TimeSpan.FromMilliseconds(5), null));

        Assert.Null(ex);
        Assert.Single(factory.Entries);
    }

    [Fact]
    public void Swallows_ObjectDisposedException_when_factory_is_already_disposed()
    {
        var factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>())
            .Returns(_ => throw new ObjectDisposedException("host"));
        MethodTimeLogger.Configure(factory);

        var ex = Record.Exception(() =>
            MethodTimeLogger.Log(SampleMethod(), TimeSpan.FromMilliseconds(1), null));

        Assert.Null(ex);
    }

    [Fact]
    public void Does_not_swallow_other_exceptions()
    {
        var factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("unexpected"));
        MethodTimeLogger.Configure(factory);

        Assert.Throws<InvalidOperationException>(() =>
            MethodTimeLogger.Log(SampleMethod(), TimeSpan.FromMilliseconds(1), null));
    }

    [Fact]
    public void Is_a_no_op_when_not_configured()
    {
        MethodTimeLogger.Configure(null!); // simulates "never configured"

        var ex = Record.Exception(() =>
            MethodTimeLogger.Log(SampleMethod(), TimeSpan.FromMilliseconds(1), "x"));

        Assert.Null(ex);
    }

    // --- minimal test doubles ---

    private sealed record Entry(string Category, LogLevel Level, string Message);

    private sealed class CapturingLoggerFactory : ILoggerFactory
    {
        public List<Entry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }

    private sealed class CapturingLogger(string category, List<Entry> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => sink.Add(new Entry(category, logLevel, formatter(state, exception)));
    }
}