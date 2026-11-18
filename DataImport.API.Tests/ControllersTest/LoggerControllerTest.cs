namespace DataImport.API.Tests.Controllers;

public class LoggerControllerTest
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly LoggerController _sut;

    public LoggerControllerTest() => _sut = new LoggerController(_mediator);

    [Fact]
    public async Task GetPaged_SendsQueryWithGivenArgs_ReturnsOk()
    {
        var result = await _sut.GetPaged(2, 50, false, CancellationToken.None);

        await _mediator.Received(1).Send(
            new GetQueriesPagedQuery(2, 50, false), Arg.Any<CancellationToken>());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetErrorCount_SendsQuery_ReturnsOk()
    {
        var result = await _sut.GetErrorCount(CancellationToken.None);

        await _mediator.Received(1).Send(new GetErrorCountQuery(), Arg.Any<CancellationToken>());
        Assert.IsType<OkObjectResult>(result);
    }
}
