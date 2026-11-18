namespace DataImport.API.Tests.Controllers;

public class StatsControllerTest
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly StatsController _sut;

    public StatsControllerTest() => _sut = new StatsController(_mediator);

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(6, 6)]
    [InlineData(20, 20)]
    [InlineData(21, 20)]
    [InlineData(999, 20)]
    public async Task TopCountries_ClampsTopTo1Through20(int top, int expected)
    {
        var result = await _sut.TopCountries(top, CancellationToken.None);

        await _mediator.Received(1).Send(new GetTopCountriesQuery(expected), Arg.Any<CancellationToken>());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task BySdnType_SendsQuery_ReturnsOk()
    {
        var result = await _sut.BySdnType(CancellationToken.None);

        await _mediator.Received(1).Send(new GetSdnTypeCountsQuery(), Arg.Any<CancellationToken>());
        Assert.IsType<OkObjectResult>(result);
    }
}
