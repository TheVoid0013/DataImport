using DataImport.API.Controllers.BaseController;
using DataImport.Commands.Queries;
using DataImport.Data.Enums;
using DataImport.API.Filters;

namespace DataImport.API.Controllers;

[ValidateEnums]
[Route("api/v{version:apiVersion}/[controller]")]
public class CountryController : ApiControllerBasev1
{
    private readonly IMediator _mediator;

    public CountryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("get-count-by-country")]
    public async Task<IActionResult> GetCountryCount(
        [FromQuery] Country country,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCountryCountQuery(country), ct);
        return Ok(result);
    }

    [HttpGet("get-delisted-count-by-country")]
    public async Task<IActionResult> GetDelistedCountByCountry(
        [FromQuery] Country country,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCountryDelistedCountQuery(country), ct);
        return Ok(result);
    }

    [HttpGet("get-sanctions-by-country")]
    public async Task<IActionResult> GetCountrySanctionsPaged(
        [FromQuery] Country country,
        [FromQuery] int pageSize = 20,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var result = await _mediator.Send(
            new GetCountrySanctionsPagedQuery(country, pageSize, page), ct);
        return Ok(result);
    }

    [HttpGet("get-delisted-sanctions-by-country")]
    public async Task<IActionResult> GetDelistedCountrySanctionsPaged(
        [FromQuery] Country country,
        [FromQuery] int pageSize = 20,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var result = await _mediator.Send(
            new GetDelistedCountrySanctionsPagedQuery(country, pageSize, page), ct);

        return Ok(result);
    }
}