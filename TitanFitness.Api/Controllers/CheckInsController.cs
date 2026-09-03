using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Application.CheckIns.RecordCheckIn;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/check-ins")]
public sealed class CheckInsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Record(RecordCheckInCommand command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}