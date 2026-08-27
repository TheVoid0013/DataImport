using DataImport.API.Controllers.BaseController;
using DataImport.Commands.Queries;

namespace DataImport.API.Controllers
{

    [Route("api/v{version:apiVersion}/sanctions")]
    public class SanctionsController : ApiControllerBasev1
    {
        private readonly IMediator _mediator;

        public SanctionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetSanctionByIdQuery(id), ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sdnType = null,
            [FromQuery] string? lastNameContains = null,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetSanctionsPagedQuery(page, pageSize, sdnType, lastNameContains), ct);
            return Ok(result);
        }
    }
}
