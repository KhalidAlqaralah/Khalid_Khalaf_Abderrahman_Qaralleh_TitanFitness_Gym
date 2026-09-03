using MediatR;
using TitanFitness.Application.Common;

namespace TitanFitness.Application.Members.GetMemberProfile;

public sealed record GetMemberProfileQuery(Guid MemberId) : IRequest<MemberProfile>;

public sealed class GetMemberProfileQueryHandler(IReadQueries queries)
    : IRequestHandler<GetMemberProfileQuery, MemberProfile>
{
    public async Task<MemberProfile> Handle(GetMemberProfileQuery request, CancellationToken ct)
        => await queries.GetMemberProfileAsync(request.MemberId, DateOnly.FromDateTime(DateTime.Now), ct)
           ?? throw new NotFoundException("Member", request.MemberId);
}