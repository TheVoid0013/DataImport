using DataImport.Hosting;


namespace DataImport.Tests.HostingTest;

public class ImportRunnerTests
{
    // Never use the real 15-minute default in tests.
    private static readonly TimeSpan NoDelay = TimeSpan.Zero;

    // ASSUMPTION: positional ctor (TotalParsed, Inserted, Updated, Unchanged).
    // Adjust if ImportOfacSdnDataResult is shaped differently.
    private static ImportOfacSdnDataResult MakeResult() => new(10, 4, 3, 3);

    [Fact]
    public async Task Succeeds_on_first_attempt_returns_result_and_sends_once()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = MakeResult();
        mediator.Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var actual = await ImportRunner.RunWithRetryAsync(mediator, 3, NoDelay);

        Assert.Same(expected, actual);
        await mediator.Received(1)
            .Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retries_after_failure_and_returns_result_when_a_later_attempt_succeeds()
    {
        var mediator = Substitute.For<IMediator>();
        var expected = MakeResult();
        mediator.Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<ImportOfacSdnDataResult>(new HttpRequestException("boom 1")),
                Task.FromException<ImportOfacSdnDataResult>(new HttpRequestException("boom 2")),
                Task.FromResult(expected));

        var actual = await ImportRunner.RunWithRetryAsync(mediator, 3, NoDelay);

        Assert.Same(expected, actual);
        await mediator.Received(3)
            .Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_last_exception_after_all_attempts_fail()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<ImportOfacSdnDataResult>(new InvalidOperationException("first")),
                Task.FromException<ImportOfacSdnDataResult>(new InvalidOperationException("second")),
                Task.FromException<ImportOfacSdnDataResult>(new InvalidOperationException("last")));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ImportRunner.RunWithRetryAsync(mediator, 3, NoDelay));

        Assert.Equal("last", ex.Message);
        await mediator.Received(3)
            .Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Single_attempt_failure_throws_immediately_without_retry()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ImportOfacSdnDataResult>(new TimeoutException("nope")));

        await Assert.ThrowsAsync<TimeoutException>(
            () => ImportRunner.RunWithRetryAsync(mediator, 1, NoDelay));

        await mediator.Received(1)
            .Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task Attempts_exactly_maxAttempts_times_before_giving_up(int maxAttempts)
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ImportOfacSdnDataResult>(new Exception("always fails")));

        await Assert.ThrowsAsync<Exception>(
            () => ImportRunner.RunWithRetryAsync(mediator, maxAttempts, NoDelay));

        await mediator.Received(maxAttempts)
            .Send(Arg.Any<ImportOfacSdnDataCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Zero_attempts_throws_InvalidOperationException_and_never_calls_mediator()
    {
        var mediator = Substitute.For<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ImportRunner.RunWithRetryAsync(mediator, 0, NoDelay));

        await mediator.DidNotReceiveWithAnyArgs()
            .Send(default(ImportOfacSdnDataCommand)!, default);
    }
}