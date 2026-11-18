namespace DataImport.API.Tests.Controllers;

public class SanctionsControllerTest
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly SanctionsController _sut;

    public SanctionsControllerTest() => _sut = new SanctionsController(_mediator);

    // Unconfigured NSubstitute returns null for reference-type responses, which is the "not found" case.
    [Fact]
    public async Task GetById_NullResult_ReturnsNotFound()
    {
        var result = await _sut.GetById("123", CancellationToken.None);

        await _mediator.Received(1).Send(new GetSanctionByIdQuery("123"), Arg.Any<CancellationToken>());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetPaged_SendsQueryWithGivenArgs_ReturnsOk()
    {
        var result = await _sut.GetPaged(2, 50, "Individual", "smith", CancellationToken.None);

        await _mediator.Received(1).Send(
            new GetSanctionsPagedQuery(2, 50, "Individual", "smith"), Arg.Any<CancellationToken>());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDelistedPaged_SendsQueryWithGivenArgs_ReturnsOk()
    {
        var result = await _sut.GetDelistedPaged(3, 10, null, null, CancellationToken.None);

        await _mediator.Received(1).Send(
            new GetDelistedSanctionsPagedQuery(3, 10, null, null), Arg.Any<CancellationToken>());
        Assert.IsType<OkObjectResult>(result);
    }
}
