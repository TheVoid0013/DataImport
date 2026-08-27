
using DataImport.API.Controllers.BaseController;
using DataImport.Commands.Queries;
    
namespace DataImport.API.Controllers
{
    
    [Route("api/v{version:apiVersion}/logger")]
    public class LoggerController : ApiControllerBasev1
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
                new GetQueriesPagedQuery(page, pageSize, orderByDescending), ct);
            return Ok(result);
        }


        [HttpGet]
        [Route("error-count")]
        public async Task<IActionResult> GetErrorCount(CancellationToken ct = default)
        {
            var result = await mediator.Send(new GetErrorCountQuery(), ct);
            return Ok(result);
        }
    }
}
