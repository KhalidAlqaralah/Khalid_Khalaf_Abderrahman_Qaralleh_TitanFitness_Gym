using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Api.Auth;
using TitanFitness.Api.Common;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Trainers.Commands.CreateTrainer;
using TitanFitness.Application.Features.Trainers.Commands.UpdateTrainer;
using TitanFitness.Application.Features.Trainers.Contracts;
using TitanFitness.Application.Features.Trainers.Queries.GetTrainerById;
using TitanFitness.Application.Features.Trainers.Queries.GetTrainerLookup;
using TitanFitness.Application.Features.Trainers.Queries.GetTrainers;
using TitanFitness.Application.Features.Trainers.Queries.GetTrainerSpecialties;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/trainers")]
[Authorize(Policy = Policies.Staff)]
public sealed class TrainersController(ISender sender) : ControllerBase
{
    /// <summary>Trainer Directory: paged, sortable, filterable by branch, specialty and status.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.Manager)]
    public Task<PagedResult<TrainerListItemResponse>> GetTrainers([FromQuery] GetTrainersRequest request, CancellationToken cancellationToken)
    {
        var statuses = request.Statuses ?? [];
        bool? active = statuses.Distinct().Count() == 1 ? statuses[0] == "Active" : null;

        return sender.Send(new GetTrainersQuery(request.Page, request.PageSize, request.Search, request.SortBy, request.SortDirection,
            request.BranchIds ?? [], request.Specialties ?? [], active), cancellationToken);
    }

    /// <summary>Distinct specialties for the Filter Trainers dialog.</summary>
    [HttpGet("specialties")]
    [Authorize(Policy = Policies.Manager)]
    public Task<IReadOnlyList<string>> GetSpecialties(CancellationToken cancellationToken) =>
        sender.Send(new GetTrainerSpecialtiesQuery(), cancellationToken);

    /// <summary>Instructor dropdown in the class dialogs: the only trainer endpoint front desk can use.</summary>
    [HttpGet("lookup")]
    public Task<IReadOnlyList<TrainerLookupResponse>> GetLookup([FromQuery] GetTrainerLookupRequest request, CancellationToken cancellationToken) =>
        sender.Send(new GetTrainerLookupQuery(request.BranchId, request.ActiveOnly), cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetTrainerByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Create(TrainerRequest request, CancellationToken cancellationToken) =>
        this.Created(await sender.Send(new CreateTrainerCommand(request.Name, request.Specialty, request.BranchId!.Value,
            request.Email, request.Phone, request.IsActive), cancellationToken), nameof(GetById), created => new { id = created.Id });

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Update(Guid id, TrainerRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new UpdateTrainerCommand(id, request.Name, request.Specialty, request.BranchId!.Value,
            request.Email, request.Phone, request.IsActive), cancellationToken));
}
