using ConferenceRooms.Api.Contracts;
using ConferenceRooms.Api.Data;
using ConferenceRooms.Api.Domain;
using ConferenceRooms.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRooms.Api.Services;

public sealed class ReportService(AppDbContext dbContext)
{
    public async Task<RevenueReportResponse> GetRevenueAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        if (from >= to)
        {
            throw new BusinessValidationException("Report start must be earlier than report end.");
        }

        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        var bookings = await dbContext.Bookings
            .AsNoTracking()
            .Where(x => x.Status == BookingStatus.Active && x.StartAt >= fromUtc && x.StartAt < toUtc)
            .Include(x => x.Hall)
            .Include(x => x.Services)
            .ToListAsync(cancellationToken);

        var byHall = bookings
            .GroupBy(x => new { x.HallId, x.Hall.Name })
            .Select(group => new HallRevenueResponse(
                group.Key.HallId,
                group.Key.Name,
                group.Count(),
                group.Sum(x => x.TotalAmount)))
            .OrderByDescending(x => x.Revenue)
            .ToArray();

        var popularServices = bookings
            .SelectMany(x => x.Services)
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PopularServiceResponse(
                group.First().Name,
                group.Count(),
                group.Sum(x => x.Price)))
            .OrderByDescending(x => x.TimesSelected)
            .ThenBy(x => x.Name)
            .ToArray();

        var roomRevenue = bookings.Sum(x => x.RoomAmount);
        var servicesRevenue = bookings.Sum(x => x.ServicesAmount);

        return new RevenueReportResponse(
            from,
            to,
            bookings.Count,
            roomRevenue,
            servicesRevenue,
            roomRevenue + servicesRevenue,
            byHall,
            popularServices);
    }
}
