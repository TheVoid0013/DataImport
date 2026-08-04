using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DataImport.API.Controllers
{

    [ApiController]
    [Route("api/logger")]
    public class LoggerController : ControllerBase
    {
        private readonly IMediator mediator;
        public LoggerController(IMediator mediator) => this.mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool orderByDescending = true,
            CancellationToken ct = default)
        {
            var result = await mediator.Send(
                new Queries.GetQueriesPagedQuery(page, pageSize, orderByDescending), ct);
            return Ok(result);
        }


        [HttpGet]
        [Route("error-count")]
        public async Task<IActionResult> GetErrorCount(CancellationToken ct = default)
        {
            var result = await mediator.Send(new Queries.GetErrorCountQuery(), ct);
            return Ok(result);
        }
    }
}
