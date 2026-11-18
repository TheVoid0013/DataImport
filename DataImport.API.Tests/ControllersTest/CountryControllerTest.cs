namespace DataImport.API.Tests.ControllersTest;

public class CountryControllerTest
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CountryController _controller;
    private static readonly Country AnyCountry = Enum.GetValues<Country>().First();

    public CountryControllerTest()
    {
        _controller = new CountryController(_mediator);
    }

    [Fact]
    public async Task GetCountryCount_SendsQueryWithCountry_ReturnsOk()
    {
        var result = await _controller.GetCountryCount(AnyCountry, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        await _mediator.Received(1).Send(
            new GetCountryCountQuery(AnyCountry),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDelistedCountByCountry_SendsQueryWithCountry_ReturnsOk()
    {
        var result = await _controller.GetDelistedCountByCountry(AnyCountry, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        await _mediator.Received(1).Send(
            new GetCountryDelistedCountQuery(AnyCountry),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCountrySanctionsPaged_PassesValuesThrough()
    {
        var result = await _controller.GetCountrySanctionsPaged(
            AnyCountry, pageSize: 50, page: 3, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        await _mediator.Received(1).Send(
            new GetCountrySanctionsPagedQuery(AnyCountry, 50, 3),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(5000, 100)]
    public async Task GetCountrySanctionsPaged_ClampsPageSize(int input, int expected)
    {
        await _controller.GetCountrySanctionsPaged(
            AnyCountry, input, 1, CancellationToken.None);

        await _mediator.Received(1).Send(
            new GetCountrySanctionsPagedQuery(AnyCountry, expected, 1),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(7, 7)]
    public async Task GetCountrySanctionsPaged_ClampsPage(int input, int expected)
    {
        await _controller.GetCountrySanctionsPaged(
            AnyCountry, 20, input, CancellationToken.None);

        await _mediator.Received(1).Send(
            new GetCountrySanctionsPagedQuery(AnyCountry, 20, expected),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDelistedCountrySanctionsPaged_PassesValuesThrough()
    {
        var result = await _controller.GetDelistedCountrySanctionsPaged(
            AnyCountry, pageSize: 50, page: 3, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        await _mediator.Received(1).Send(
            new GetDelistedCountrySanctionsPagedQuery(AnyCountry, 50, 3),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(5000, 100)]
    public async Task GetDelistedCountrySanctionsPaged_ClampsPageSize(int input, int expected)
    {
        await _controller.GetDelistedCountrySanctionsPaged(
            AnyCountry, input, 1, CancellationToken.None);

        await _mediator.Received(1).Send(
            new GetDelistedCountrySanctionsPagedQuery(AnyCountry, expected, 1),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(7, 7)]
    public async Task GetDelistedCountrySanctionsPaged_ClampsPage(int input, int expected)
    {
        await _controller.GetDelistedCountrySanctionsPaged(
            AnyCountry, 20, input, CancellationToken.None);

        await _mediator.Received(1).Send(
            new GetDelistedCountrySanctionsPagedQuery(AnyCountry, 20, expected),
            Arg.Any<CancellationToken>());
    }
}