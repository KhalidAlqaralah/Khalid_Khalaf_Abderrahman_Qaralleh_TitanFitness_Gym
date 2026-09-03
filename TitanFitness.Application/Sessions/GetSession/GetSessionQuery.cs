using MediatR;
using TitanFitness.Application.Common;

namespace TitanFitness.Application.Sessions.GetSession;

public sealed record GetSessionQuery(Guid SessionId) : IRequest<SessionDetail>;

public sealed class GetSessionQueryHandler(IReadQueries queries)
    : IRequestHandler<GetSessionQuery, SessionDetail>
{
    public async Task<SessionDetail> Handle(GetSessionQuery request, CancellationToken ct)
        => await queries.GetSessionAsync(request.SessionId, DateTime.Now, ct)
           ?? throw new NotFoundException("Session", request.SessionId);
}