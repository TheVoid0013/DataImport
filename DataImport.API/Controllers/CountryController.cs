using DataImport.API.Controllers.BaseController;
using DataImport.Commands.Queries;
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
    
    [HttpPost]
    [Route("get-sanctions-by-country")]
    public async Task<IActionResult> GetCountrySanctionsPaged(
        [FromQuery] GetCountrySanctionsPagedQuery request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetCountrySanctionsPagedQuery(request.country)
            ,ct);
 
        if (result is null || result.TotalCount == 0)
            return NotFound();
 
        return Ok(result);
    }
    
}