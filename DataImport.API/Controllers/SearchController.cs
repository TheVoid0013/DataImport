using DataImport.API.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DataImport.API.Controllers
{
    [ApiController]
    [Route("api/Search")]
    public class SearchController : ControllerBase
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