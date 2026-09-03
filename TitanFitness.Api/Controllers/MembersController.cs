using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Application.Members.CreateMember;
using TitanFitness.Application.Members.GetMemberProfile;
using TitanFitness.Application.Members.SearchMembers;
using TitanFitness.Application.Members.UpdateMember;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/members")]
public sealed class MembersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateMemberCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Created($"/api/members/{id}", new { id });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await sender.Send(new SearchMembersQuery(search, branchId, page, pageSize), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetMemberProfileQuery(id), ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateMemberRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateMemberCommand(
            id, request.FullName, request.Email, request.Phone,
            request.Address, request.PhotoUrl, request.HomeBranchId), ct);

        return NoContent();
    }
}

public sealed record UpdateMemberRequest(
    string FullName,
    string? Email,
    string? Phone,
    string? Address,
    string? PhotoUrl,
    Guid HomeBranchId);