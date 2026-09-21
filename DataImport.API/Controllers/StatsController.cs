using DataImport.API.Controllers.BaseController;
using DataImport.Commands.Queries;
using DataImport.Presentation.GenericDTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DataImport.API.Controllers;

[Route("api/v{version:apiVersion}/Stats")]
public class StatsController : ApiControllerBasev1
{
    private readonly IMediator _mediator;

    public StatsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("top-countries")]
    [ProducesResponseType(typeof(TopCountriesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TopCountries(
        [FromQuery] int top = 6,
        CancellationToken ct = default)
    {
        var query = new GetTopCountriesQuery(Math.Clamp(top, 1, 20));
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("by-sdn-type")]
    [ProducesResponseType(typeof(List<SdnTypeCountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BySdnType(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSdnTypeCountsQuery(), ct);
        return Ok(result);
    }
}