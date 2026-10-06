using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Api.Auth;
using TitanFitness.Api.Common;
using TitanFitness.Application.Features.CheckIns.Commands.RecordCheckIn;
using TitanFitness.Application.Features.CheckIns.Contracts;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/check-ins")]
[Authorize(Policy = Policies.Staff)]
public sealed class CheckInsController(ISender sender) : ControllerBase
{
    /// <summary>New Check-in dialog → Save. 409 when the membership does not allow entry.</summary>
    [HttpPost]
    public async Task<IActionResult> Record(RecordCheckInRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RecordCheckInCommand(request.MemberId!.Value, request.BranchId!.Value,
            request.Date!.Value, request.Time!.Value, request.Notes), cancellationToken);

        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : this.ToProblem(result.Error);
    }
}
