using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Api.Auth;
using TitanFitness.Api.Common;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Members.Commands.CreateMember;
using TitanFitness.Application.Features.Members.Commands.UpdateMember;
using TitanFitness.Application.Features.Members.Contracts;
using TitanFitness.Application.Features.Members.Queries.GetCurrentMembership;
using TitanFitness.Application.Features.Members.Queries.GetMemberActivity;
using TitanFitness.Application.Features.Members.Queries.GetMemberById;
using TitanFitness.Application.Features.Members.Queries.GetMembers;
using TitanFitness.Application.Features.Memberships.Contracts;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/members")]
[Authorize(Policy = Policies.Staff)]
public sealed class MembersController(ISender sender) : ControllerBase
{
    /// <summary>Member Directory: paged, searchable (name, number, branch, status), filterable by branch and status.</summary>
    [HttpGet]
    public Task<PagedResult<MemberListItemResponse>> GetMembers([FromQuery] GetMembersRequest request, CancellationToken cancellationToken) =>
        sender.Send(new GetMembersQuery(request.Page, request.PageSize, request.Search, request.SortBy, request.SortDirection,
            request.BranchId, request.Statuses ?? []), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetMemberByIdQuery(id), cancellationToken));

    /// <summary>The member's current membership with freeze and guest pass usage (404 when there is none).</summary>
    [HttpGet("{id:guid}/current-membership")]
    [ProducesResponseType<MembershipResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentMembership(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetCurrentMembershipQuery(id), cancellationToken));

    /// <summary>Recent Activity: check-ins and attended classes, newest first.</summary>
    [HttpGet("{id:guid}/activity")]
    public async Task<IActionResult> GetActivity(Guid id, [FromQuery] GetMemberActivityRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetMemberActivityQuery(id, request.Take), cancellationToken));

    /// <summary>Add Member dialog. Returns the generated TF-NNNN number.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateMemberRequest request, CancellationToken cancellationToken) =>
        this.Created(await sender.Send(new CreateMemberCommand(request.FullName, request.HomeBranchId!.Value,
            request.Email, request.Phone, request.Address), cancellationToken), nameof(GetById), created => new { id = created.Id });

    /// <summary>Edit Member dialog: name and home branch.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMemberRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new UpdateMemberCommand(id, request.FullName, request.HomeBranchId!.Value), cancellationToken));
}
