using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Api.Auth;
using TitanFitness.Api.Common;
using TitanFitness.Application.Features.ClassSessions.Commands.BookSession;
using TitanFitness.Application.Features.ClassSessions.Commands.CancelBooking;
using TitanFitness.Application.Features.ClassSessions.Commands.CancelClass;
using TitanFitness.Application.Features.ClassSessions.Commands.MarkAttendance;
using TitanFitness.Application.Features.ClassSessions.Commands.ScheduleClass;
using TitanFitness.Application.Features.ClassSessions.Commands.UpdateClass;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Application.Features.ClassSessions.Queries.GetCapacityOverview;
using TitanFitness.Application.Features.ClassSessions.Queries.GetClassSchedule;
using TitanFitness.Application.Features.ClassSessions.Queries.GetClassSessionById;
using TitanFitness.Application.Features.ClassSessions.Queries.GetSessionBookings;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/class-sessions")]
[Authorize(Policy = Policies.Staff)]
public sealed class ClassSessionsController(ISender sender) : ControllerBase
{
    /// <summary>Class Schedule list (day or week, one branch or all, searchable by class, trainer or studio).</summary>
    [HttpGet]
    public Task<IReadOnlyList<ClassSessionResponse>> GetSchedule([FromQuery] GetClassScheduleRequest request, CancellationToken cancellationToken) =>
        sender.Send(new GetClassScheduleQuery(request.BranchId, request.Date, request.Days, request.Search), cancellationToken);

    /// <summary>Capacity Overview card for the same branch and dates as the list.</summary>
    [HttpGet("capacity-overview")]
    public Task<CapacityOverviewResponse> GetCapacityOverview([FromQuery] GetClassScheduleRequest request, CancellationToken cancellationToken) =>
        sender.Send(new GetCapacityOverviewQuery(request.BranchId, request.Date, request.Days), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetClassSessionByIdQuery(id), cancellationToken));

    [HttpGet("{id:guid}/bookings")]
    public async Task<IActionResult> GetBookings(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetSessionBookingsQuery(id), cancellationToken));

    /// <summary>Add New Class → Schedule Class.</summary>
    [HttpPost]
    public async Task<IActionResult> Schedule(ClassSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ScheduleClassCommand(request.ClassName, request.BranchId!.Value, request.TrainerId, request.StudioId,
            request.Date!.Value, request.StartTime!.Value, request.DurationInMinutes!.Value, request.CapacityLimit, request.Description), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
            : this.ToProblem(result.Error);
    }

    /// <summary>Edit Class → Save Changes.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ClassSessionRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new UpdateClassCommand(id, request.ClassName, request.BranchId!.Value, request.TrainerId, request.StudioId,
            request.Date!.Value, request.StartTime!.Value, request.DurationInMinutes!.Value, request.CapacityLimit, request.Description), cancellationToken));

    /// <summary>Cancel Class: cancels every booking on it.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new CancelClassCommand(id), cancellationToken));

    /// <summary>Book Session: confirmed while places remain, otherwise waitlisted.</summary>
    [HttpPost("{id:guid}/bookings")]
    public async Task<IActionResult> Book(Guid id, BookSessionRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new BookSessionCommand(id, request.MemberId!.Value, request.Note), cancellationToken));

    [HttpDelete("{id:guid}/bookings/{bookingId:guid}")]
    public async Task<IActionResult> CancelBooking(Guid id, Guid bookingId, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new CancelBookingCommand(id, bookingId), cancellationToken));

    [HttpPost("{id:guid}/bookings/{bookingId:guid}/attendance")]
    public async Task<IActionResult> MarkAttendance(Guid id, Guid bookingId, MarkAttendanceRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new MarkAttendanceCommand(id, bookingId, request.Attended), cancellationToken));
}
