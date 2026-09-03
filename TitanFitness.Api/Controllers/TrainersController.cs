using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Application.Trainers.CreateTrainer;
using TitanFitness.Application.Trainers.ListTrainers;
using TitanFitness.Application.Trainers.UpdateTrainer;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/trainers")]
public sealed class TrainersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateTrainerCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Created($"/api/trainers/{id}", new { id });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = false, CancellationToken ct = default)
        => Ok(await sender.Send(new ListTrainersQuery(activeOnly), ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateTrainerRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateTrainerCommand(
            id, request.Name, request.Email, request.Phone, request.IsActive), ct);

        return NoContent();
    }
}

public sealed record UpdateTrainerRequest(string Name, string? Email, string? Phone, bool IsActive);