using DataImport.API.Controllers.BaseController;
using DataImport.Commands.Queries;
using DataImport.Data.Enums;

namespace DataImport.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class CountryController : ApiControllerBasev1
{
    private readonly IMediator _mediator;

    public CountryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("get-count-by-country")]
    public async Task<IActionResult> GetCountryCount(
        [FromQuery] Country country,
        CancellationToken ct)
    {
        if (!Enum.IsDefined(typeof(Country), country))
            return BadRequest("Valid country is required");

        var result = await _mediator.Send(new GetCountryCountQuery(country), ct);
        return Ok(result);
    }


    [HttpPost]
    [Route("get-delisted-count-by-country")]
    public async Task<IActionResult> GetDelistedCountByCountry(
        [FromQuery] Country country,
        CancellationToken ct
        )
    {
        if (!Enum.IsDefined(typeof(Country), country))
            return BadRequest("Valid country is required");

        var result= await _mediator.Send(new GetCountryDelistedCountQuery(country), ct);
        return Ok(result);
    }

    [HttpPost]
    [Route("get-sanctions-by-country")]
    public async Task<IActionResult> GetCountrySanctionsPaged(
        [FromQuery] Country country,
        [FromQuery] int pageSize = 20,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(Country), country))
            return BadRequest("Valid country is required");

        var result = await _mediator.Send(
            new GetCountrySanctionsPagedQuery(country, pageSize, page),
            ct);

        return Ok(result);
    }
}