using MediatR;
using Microsoft.AspNetCore.Mvc;
using DataImport.API.Queries;

namespace DataImport.API.Controllers
{
    [ApiController]
    [Route("api/sanctions")]
    public class SanctionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SanctionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
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
