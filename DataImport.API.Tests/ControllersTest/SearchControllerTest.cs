namespace DataImport.API.Tests.Controllers;

public class SearchControllerTest
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly SearchController _sut;

    public SearchControllerTest() => _sut = new SearchController(_mediator);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSearchRecords_BlankName_ReturnsBadRequest(string? name)
    {
        var result = await _sut.GetSearchRecords(new SearchRequest(name!), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        await _mediator.DidNotReceive().Send(Arg.Any<GetFreeTextSearchQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSearchRecords_NullRequest_ReturnsBadRequest()
    {
        var result = await _sut.GetSearchRecords(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetSearchRecords_ValidName_SendsQueryAndReturnsOk()
    {
        var result = await _sut.GetSearchRecords(new SearchRequest("ivan"), CancellationToken.None);

        await _mediator.Received(1).Send(new GetFreeTextSearchQuery("ivan"), Arg.Any<CancellationToken>());
        Assert.IsType<OkObjectResult>(result);
    }
}