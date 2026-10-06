using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.ClassSessions.Commands.UpdateClass;

public sealed record UpdateClassCommand(
    Guid SessionId,
    string ClassName,
    Guid BranchId,
    Guid? TrainerId,
    Guid? StudioId,
    DateOnly Date,
    TimeOnly StartTime,
    int DurationInMinutes,
    int? CapacityLimit,
    string? Description) : IRequest<Result>;
