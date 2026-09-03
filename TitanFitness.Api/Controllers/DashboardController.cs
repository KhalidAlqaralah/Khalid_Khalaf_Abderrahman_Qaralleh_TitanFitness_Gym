using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Application.Dashboard.GetDashboard;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] Guid? branchId, CancellationToken ct = default)
        => Ok(await sender.Send(new GetDashboardQuery(branchId), ct));
}