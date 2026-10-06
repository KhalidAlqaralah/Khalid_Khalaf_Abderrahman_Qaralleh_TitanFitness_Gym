using MediatR;
using TitanFitness.Application.Features.SelfService.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.SelfService.Queries.GetMyEligibility;

/// <summary>Whether the member may book <paramref name="SessionId"/>; without a session, whether they hold an active membership today.</summary>
public sealed record GetMyEligibilityQuery(Guid MemberId, Guid? SessionId) : IRequest<Result<EligibilityResponse>>;
