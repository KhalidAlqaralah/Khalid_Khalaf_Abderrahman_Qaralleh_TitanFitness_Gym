using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Application.Branches.AddStudio;
using TitanFitness.Application.Branches.CreateBranch;
using TitanFitness.Application.Branches.ListBranches;
using TitanFitness.Application.Branches.UpdateBranch;
using TitanFitness.Application.Branches.UpdateStudio;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/branches")]
public sealed class BranchesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateBranchCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Created($"/api/branches/{id}", new { id });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await sender.Send(new ListBranchesQuery(), ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateBranchRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateBranchCommand(
            id, request.Name, request.Address, request.Opens, request.Closes), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/studios")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddStudio(Guid id, AddStudioRequest request, CancellationToken ct)
    {
        var studioId = await sender.Send(new AddStudioCommand(id, request.Name, request.Capacity), ct);
        return Created($"/api/branches/{id}/studios/{studioId}", new { id = studioId });
    }

    [HttpPut("{id:guid}/studios/{studioId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStudio(
        Guid id, Guid studioId, AddStudioRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateStudioCommand(id, studioId, request.Name, request.Capacity), ct);
        return NoContent();
    }
}

public sealed record UpdateBranchRequest(string Name, string? Address, TimeOnly Opens, TimeOnly Closes);

public sealed record AddStudioRequest(string Name, int Capacity);