using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Members;

namespace TitanFitness.Application.Features.SelfService.Queries.FindMemberByNumber;

internal sealed class FindMemberByNumberQueryHandler(IReadRepository<Member> members) : IRequestHandler<FindMemberByNumberQuery, Guid?>
{
    public async Task<Guid?> Handle(FindMemberByNumberQuery query, CancellationToken cancellationToken)
    {
        var number = query.MembershipNumber.Trim().TrimStart('#').ToUpperInvariant();
        return await members.GetAll()
            .Where(m => m.Number.Value == number)
            .Select(m => (Guid?)m.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
