using MediatR;
using TitanFitness.Application.Features.Members.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Members.Queries.GetMemberById;

public sealed record GetMemberByIdQuery(Guid MemberId) : IRequest<Result<MemberResponse>>;
