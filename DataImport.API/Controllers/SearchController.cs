using DataImport.API.Controllers.BaseController;
using DataImport.Commands.Queries;

namespace DataImport.API.Controllers
{

    [Route("api/v{version:apiVersion}/Search")]
    public class SearchController : ApiControllerBasev1
    {
        private readonly IMediator _mediator;
        public SearchController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost(Name = "Search-Records")]
        public async Task<IActionResult> GetSearchRecords(
            [FromBody] SearchRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
                return BadRequest("Search name is required.");
            var result = await _mediator.Send(
                new GetFreeTextSearchQuery(request.Name),
                ct);
            if (result is null || result.TotalCount == 0)
                return NotFound();
            return Ok(result);
        }
    }
}