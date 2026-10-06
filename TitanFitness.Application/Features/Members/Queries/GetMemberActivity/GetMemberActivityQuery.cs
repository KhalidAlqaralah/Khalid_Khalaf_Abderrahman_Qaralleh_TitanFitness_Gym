using MediatR;
using TitanFitness.Application.Features.Members.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Members.Queries.GetMemberActivity;

public sealed record GetMemberActivityQuery(Guid MemberId, int Take) : IRequest<Result<IReadOnlyList<MemberActivityResponse>>>;
