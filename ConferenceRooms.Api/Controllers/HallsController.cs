using ConferenceRooms.Api.Contracts;
using ConferenceRooms.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRooms.Api.Controllers;

[ApiController]
[Route("api/halls")]
[Produces("application/json")]
public sealed class HallsController(HallManagementService hallService) : ControllerBase
{
    /// <summary>Returns all active conference halls.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<HallResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<HallResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await hallService.GetAllAsync(cancellationToken));

    /// <summary>Returns a conference hall by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<HallResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HallResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await hallService.GetAsync(id, cancellationToken));

    /// <summary>Creates a conference hall and its available additional services.</summary>
    [HttpPost]
    [ProducesResponseType<HallResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HallResponse>> Create(
        [FromBody] CreateHallRequest request,
        CancellationToken cancellationToken)
    {
        var hall = await hallService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = hall.Id }, hall);
    }

    /// <summary>Partially updates a hall. When services are supplied, they replace the current list.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<HallResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<HallResponse>> Update(
        Guid id,
        [FromBody] UpdateHallRequest request,
        CancellationToken cancellationToken) =>
        Ok(await hallService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Soft-deletes a hall while keeping historical bookings and reports intact.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await hallService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Finds active halls with enough capacity and no overlapping booking.</summary>
    [HttpGet("available")]
    [ProducesResponseType<IReadOnlyCollection<HallResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IReadOnlyCollection<HallResponse>>> FindAvailable(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int capacity,
        CancellationToken cancellationToken) =>
        Ok(await hallService.FindAvailableAsync(from, to, capacity, cancellationToken));
}
