using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Application.Sessions.BookSession;
using TitanFitness.Application.Sessions.CancelBooking;
using TitanFitness.Application.Sessions.CancelSession;
using TitanFitness.Application.Sessions.GetSchedule;
using TitanFitness.Application.Sessions.GetSession;
using TitanFitness.Application.Sessions.MarkAttendance;
using TitanFitness.Application.Sessions.ScheduleSession;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/class-sessions")]
public sealed class ClassSessionsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Schedule(ScheduleSessionCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Created($"/api/class-sessions/{id}", new { id });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedule(
        [FromQuery] Guid? branchId,
        [FromQuery] DateOnly? date,
        CancellationToken ct = default)
        => Ok(await sender.Send(
            new GetScheduleQuery(branchId, date ?? DateOnly.FromDateTime(DateTime.Now)), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetSessionQuery(id), ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await sender.Send(new CancelSessionCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/bookings")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(Guid id, BookSessionRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new BookSessionCommand(id, request.MemberId, request.Note), ct);
        return Created($"/api/class-sessions/{id}/bookings/{result.Id}", result);
    }

    [HttpDelete("{id:guid}/bookings/{bookingId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelBooking(Guid id, Guid bookingId, CancellationToken ct)
    {
        var promotedId = await sender.Send(new CancelBookingCommand(id, bookingId), ct);
        return Ok(new { promotedBookingId = promotedId });
    }

    [HttpPost("{id:guid}/bookings/{bookingId:guid}/attendance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkAttendance(
        Guid id, Guid bookingId, AttendanceRequest request, CancellationToken ct)
    {
        await sender.Send(new MarkAttendanceCommand(id, bookingId, request.Attended), ct);
        return NoContent();
    }
}

public sealed record BookSessionRequest(Guid MemberId, string? Note);

public sealed record AttendanceRequest(bool Attended);