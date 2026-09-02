using ConferenceRooms.Api.Contracts;
using ConferenceRooms.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRooms.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public sealed class ReportsController(ReportService reportService) : ControllerBase
{
    /// <summary>Returns revenue by hall and the popularity of additional services for a period.</summary>
    [HttpGet("revenue")]
    [ProducesResponseType<RevenueReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RevenueReportResponse>> GetRevenue(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken cancellationToken) =>
        Ok(await reportService.GetRevenueAsync(from, to, cancellationToken));
}
