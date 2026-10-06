using MediatR;
using TitanFitness.Application.Features.CheckIns.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.CheckIns.Commands.RecordCheckIn;

public sealed record RecordCheckInCommand(Guid MemberId, Guid BranchId, DateOnly Date, TimeOnly Time, string? Notes)
    : IRequest<Result<CheckInResponse>>;
