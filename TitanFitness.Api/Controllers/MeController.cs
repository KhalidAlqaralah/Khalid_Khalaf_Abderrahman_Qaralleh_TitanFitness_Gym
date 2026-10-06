using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Api.Auth;
using TitanFitness.Api.Common;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.ClassSessions.Commands.BookSession;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Application.Features.ClassSessions.Queries.GetClassSchedule;
using TitanFitness.Application.Features.ClassSessions.Queries.GetClassSessionById;
using TitanFitness.Application.Features.SelfService.Queries.GetMyEligibility;

namespace TitanFitness.Api.Controllers;

/// <summary>Self-service for a signed-in member (Book Session — Member View). The member is always the token's member.</summary>
[ApiController]
[Route("api/me")]
[Authorize(Policy = Policies.Member)]
public sealed class MeController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    private Guid MemberId => currentUser.MemberId!.Value;

    [HttpGet("eligibility")]
    public async Task<IActionResult> GetEligibility([FromQuery] Guid? sessionId, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetMyEligibilityQuery(MemberId, sessionId), cancellationToken));

    /// <summary>Classes for the next week at every branch.</summary>
    [HttpGet("classes")]
    public Task<IReadOnlyList<ClassSessionResponse>> GetClasses([FromQuery] GetClassScheduleRequest request, CancellationToken cancellationToken) =>
        sender.Send(new GetClassScheduleQuery(request.BranchId, request.Date, request.Days, request.Search), cancellationToken);

    [HttpGet("classes/{id:guid}")]
    public async Task<IActionResult> GetClass(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetClassSessionByIdQuery(id), cancellationToken));

    [HttpPost("bookings")]
    public async Task<IActionResult> Book(BookMySessionRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new BookSessionCommand(request.SessionId!.Value, MemberId, request.Note), cancellationToken));
}
