using MediatR;

namespace TitanFitness.Application.Features.SelfService.Queries.FindMemberByNumber;

/// <summary>Used at sign-in to link a member account to its member record.</summary>
public sealed record FindMemberByNumberQuery(string MembershipNumber) : IRequest<Guid?>;
