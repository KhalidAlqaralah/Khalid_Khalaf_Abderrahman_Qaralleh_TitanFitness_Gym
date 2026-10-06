using MediatR;
using TitanFitness.Application.Features.ClassSessions.Contracts;

namespace TitanFitness.Application.Features.ClassSessions.Queries.GetClassSchedule;

public sealed record GetClassScheduleQuery(Guid? BranchId, DateOnly? Date, int Days, string? Search) : IRequest<IReadOnlyList<ClassSessionResponse>>;
