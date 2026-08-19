using DataImport.API.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DataImport.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountryController : ControllerBase
{
    private readonly IMediator _mediator;

    public CountryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("get-count-by-country")]
    public async Task<IActionResult> GetCountryCount(
        [FromQuery] GetCountryCountQuery request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.country.ToString()))
        {
            return BadRequest("Country name is required");
        }

        var result = await _mediator.Send(
            new GetCountryCountQuery(request.country),
            ct);

        if (result is null || ((dynamic)result).Count == 0)
            return NotFound();

        return Ok(result);
    }
}