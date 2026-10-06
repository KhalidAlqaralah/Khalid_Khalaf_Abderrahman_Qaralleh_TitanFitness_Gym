using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Api.Auth;
using TitanFitness.Api.Common;
using TitanFitness.Application.Features.Branches.Commands.AddStudio;
using TitanFitness.Application.Features.Branches.Commands.CreateBranch;
using TitanFitness.Application.Features.Branches.Commands.UpdateBranch;
using TitanFitness.Application.Features.Branches.Commands.UpdateStudio;
using TitanFitness.Application.Features.Branches.Contracts;
using TitanFitness.Application.Features.Branches.Queries.GetBranches;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize(Policy = Policies.Staff)]
public sealed class BranchesController(ISender sender) : ControllerBase
{
    /// <summary>Every branch with its studios (header branch picker and every branch / room dropdown).</summary>
    [HttpGet]
    [Authorize]
    public Task<IReadOnlyList<BranchResponse>> GetBranches(CancellationToken cancellationToken) =>
        sender.Send(new GetBranchesQuery(), cancellationToken);

    [HttpPost]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Create(BranchRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBranchCommand(request.Name, request.Address, request.Opens, request.Closes), cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, new { id = result.Value }) : this.ToProblem(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Update(Guid id, BranchRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new UpdateBranchCommand(id, request.Name, request.Address, request.Opens, request.Closes), cancellationToken));

    [HttpPost("{id:guid}/studios")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> AddStudio(Guid id, StudioRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddStudioCommand(id, request.Name, request.Capacity), cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, new { id = result.Value }) : this.ToProblem(result.Error);
    }

    [HttpPut("{id:guid}/studios/{studioId:guid}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> UpdateStudio(Guid id, Guid studioId, StudioRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new UpdateStudioCommand(id, studioId, request.Name, request.Capacity), cancellationToken));
}
