using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Api.Auth;
using TitanFitness.Api.Common;
using TitanFitness.Application.Features.Memberships.Commands.CancelMembership;
using TitanFitness.Application.Features.Memberships.Commands.EndFreezeEarly;
using TitanFitness.Application.Features.Memberships.Commands.FreezeMembership;
using TitanFitness.Application.Features.Memberships.Commands.IssueGuestPass;
using TitanFitness.Application.Features.Memberships.Commands.PurchaseMembership;
using TitanFitness.Application.Features.Memberships.Commands.RenewMembership;
using TitanFitness.Application.Features.Memberships.Commands.UseGuestPass;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Application.Features.Memberships.Queries.GetMembershipById;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/memberships")]
[Authorize(Policy = Policies.Staff)]
public sealed class MembershipsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetMembershipByIdQuery(id), cancellationToken));

    /// <summary>Sells a published plan to a member from a start date (today or later).</summary>
    [HttpPost]
    public async Task<IActionResult> Purchase(PurchaseMembershipRequest request, CancellationToken cancellationToken) =>
        this.Created(await sender.Send(new PurchaseMembershipCommand(request.MemberId!.Value, request.PlanId!.Value, request.StartDate!.Value),
            cancellationToken), nameof(GetById), created => new { id = created.Id });

    [HttpPost("{id:guid}/renewals")]
    public async Task<IActionResult> Renew(Guid id, RenewMembershipRequest request, CancellationToken cancellationToken) =>
        this.Created(await sender.Send(new RenewMembershipCommand(id, request.PlanId), cancellationToken),
            nameof(GetById), created => new { id = created.Id });

    /// <summary>Cancels the membership. Final: it cannot be resumed.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new CancelMembershipCommand(id), cancellationToken));

    /// <summary>Freeze Membership → Confirm Freeze.</summary>
    [HttpPost("{id:guid}/freezes")]
    public async Task<IActionResult> Freeze(Guid id, FreezeMembershipRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new FreezeMembershipCommand(id, request.StartDate!.Value, request.DurationInMonths!.Value,
            request.Reason!.Value, request.Notes), cancellationToken));

    [HttpPost("{id:guid}/freezes/{freezeId:guid}/end")]
    public async Task<IActionResult> EndFreeze(Guid id, Guid freezeId, EndFreezeRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new EndFreezeEarlyCommand(id, freezeId, request.EndedOn!.Value), cancellationToken));

    [HttpPost("{id:guid}/guest-passes")]
    public async Task<IActionResult> IssueGuestPass(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new IssueGuestPassCommand(id), cancellationToken));

    [HttpPost("{id:guid}/guest-passes/{passId:guid}/use")]
    public async Task<IActionResult> UseGuestPass(Guid id, Guid passId, UseGuestPassRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new UseGuestPassCommand(id, passId, request.GuestName), cancellationToken));
}
