using MediatR;
using TitanFitness.Application.Features.ClassSessions.Contracts;

namespace TitanFitness.Application.Features.Dashboard.Queries.GetUpcomingClasses;

public sealed record GetUpcomingClassesQuery(Guid? BranchId, int Take) : IRequest<IReadOnlyList<ClassSessionResponse>>;
