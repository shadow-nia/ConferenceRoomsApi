using ConferenceRooms.Api.Contracts;
using ConferenceRooms.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRooms.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Produces("application/json")]
public sealed class BookingsController(BookingManagementService bookingService) : ControllerBase
{
    /// <summary>Creates a booking, snapshots prices, and returns its total cost.</summary>
    [HttpPost]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookingResponse>> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await bookingService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = booking.Id }, booking);
    }

    /// <summary>Returns a booking with the price snapshot used at creation time.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await bookingService.GetAsync(id, cancellationToken));
}
