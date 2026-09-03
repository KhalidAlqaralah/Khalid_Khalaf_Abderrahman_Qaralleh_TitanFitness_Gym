using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Application.Memberships.CancelMembership;
using TitanFitness.Application.Memberships.ChangePlan;
using TitanFitness.Application.Memberships.FreezeMembership;
using TitanFitness.Application.Memberships.GetMembership;
using TitanFitness.Application.Memberships.IssueGuestPass;
using TitanFitness.Application.Memberships.PreviewFreeze;
using TitanFitness.Application.Memberships.PurchaseMembership;
using TitanFitness.Application.Memberships.RenewMembership;
using TitanFitness.Application.Memberships.UseGuestPass;
using TitanFitness.Application.Memberships.EndFreezeEarly;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/memberships")]
public sealed class MembershipsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Purchase(PurchaseMembershipCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Created($"/api/memberships/{id}", new { id });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetMembershipQuery(id), ct));

    [HttpGet("{id:guid}/freezes/preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewFreeze(
        Guid id,
        [FromQuery] DateOnly startDate,
        [FromQuery] int durationInMonths,
        CancellationToken ct)
        => Ok(await sender.Send(new PreviewFreezeQuery(id, startDate, durationInMonths), ct));

    [HttpPost("{id:guid}/freezes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Freeze(Guid id, FreezeMembershipRequest request, CancellationToken ct)
    {
        var freezeId = await sender.Send(
            new FreezeMembershipCommand(id, request.StartDate, request.DurationInMonths, request.Reason, request.Notes),
            ct);

        return Created($"/api/memberships/{id}/freezes/{freezeId}", new { id = freezeId });
    }

    [HttpPost("{id:guid}/freezes/{freezeId:guid}/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EndFreezeEarly(
        Guid id, Guid freezeId, EndFreezeEarlyRequest request, CancellationToken ct)
    {
        await sender.Send(new EndFreezeEarlyCommand(id, freezeId, request.EndedOn), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/guest-passes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IssueGuestPass(Guid id, CancellationToken ct)
    {
        var passId = await sender.Send(new IssueGuestPassCommand(id), ct);
        return Created($"/api/memberships/{id}/guest-passes/{passId}", new { id = passId });
    }

    [HttpPost("{id:guid}/guest-passes/{passId:guid}/use")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UseGuestPass(
        Guid id, Guid passId, UseGuestPassRequest request, CancellationToken ct)
    {
        await sender.Send(new UseGuestPassCommand(id, passId, request.GuestName), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/plan-changes/preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewPlanChange(
        Guid id,
        [FromQuery] Guid newPlanId,
        [FromQuery] PlanChangeTiming timing,
        CancellationToken ct)
        => Ok(await sender.Send(new PreviewPlanChangeQuery(id, newPlanId, timing), ct));

    [HttpPost("{id:guid}/plan-changes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangePlan(Guid id, ChangePlanRequest request, CancellationToken ct)
    {
        var newId = await sender.Send(
            new ChangeMembershipPlanCommand(id, request.NewPlanId, request.Timing), ct);

        return Created($"/api/memberships/{newId}", new { id = newId });
    }

    [HttpPost("{id:guid}/renewals")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Renew(Guid id, RenewRequest request, CancellationToken ct)
    {
        var newId = await sender.Send(new RenewMembershipCommand(id, request.PlanId), ct);
        return Created($"/api/memberships/{newId}", new { id = newId });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await sender.Send(new CancelMembershipCommand(id), ct);
        return NoContent();
    }
}

public sealed record UseGuestPassRequest(string? GuestName);

public sealed record ChangePlanRequest(Guid NewPlanId, PlanChangeTiming Timing);

public sealed record RenewRequest(Guid? PlanId);

public sealed record EndFreezeEarlyRequest(DateOnly EndedOn);