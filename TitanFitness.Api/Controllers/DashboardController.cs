using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Api.Auth;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Application.Features.Dashboard.Contracts;
using TitanFitness.Application.Features.Dashboard.Queries.GetActiveMembers;
using TitanFitness.Application.Features.Dashboard.Queries.GetCheckInsToday;
using TitanFitness.Application.Features.Dashboard.Queries.GetUpcomingClasses;

namespace TitanFitness.Api.Controllers;

/// <summary>One focused endpoint per dashboard card, so each card loads and refreshes on its own.</summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Policies.Staff)]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet("check-ins-today")]
    public Task<CheckInsTodayResponse> GetCheckInsToday([FromQuery] DashboardRequest request, CancellationToken cancellationToken) =>
        sender.Send(new GetCheckInsTodayQuery(request.BranchId), cancellationToken);

    [HttpGet("active-members")]
    public Task<ActiveMembersResponse> GetActiveMembers([FromQuery] DashboardRequest request, CancellationToken cancellationToken) =>
        sender.Send(new GetActiveMembersQuery(request.BranchId), cancellationToken);

    [HttpGet("upcoming-classes")]
    public Task<IReadOnlyList<ClassSessionResponse>> GetUpcomingClasses([FromQuery] UpcomingClassesRequest request, CancellationToken cancellationToken) =>
        sender.Send(new GetUpcomingClassesQuery(request.BranchId, request.Take), cancellationToken);
}
