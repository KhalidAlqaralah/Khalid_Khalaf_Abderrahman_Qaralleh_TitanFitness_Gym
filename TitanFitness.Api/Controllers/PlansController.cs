using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Application.Plans.CreatePlan;
using TitanFitness.Application.Plans.ListPlans;
using TitanFitness.Application.Plans.UpdatePlan;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class PlansController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreatePlanCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Created($"/api/plans/{id}", new { id });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool publishedOnly = false, CancellationToken ct = default)
        => Ok(await sender.Send(new ListPlansQuery(publishedOnly), ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdatePlanRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdatePlanCommand(
            id, request.Name, request.Price, request.DurationInMonths,
            request.MaxFreezeDays, request.MaxFreezes, request.GuestPassQuota,
            request.AccessScope, request.IsPublished), ct);

        return NoContent();
    }
}

public sealed record UpdatePlanRequest(
    string Name,
    decimal Price,
    int DurationInMonths,
    int MaxFreezeDays,
    int MaxFreezes,
    int GuestPassQuota,
    AccessScope AccessScope,
    bool IsPublished);