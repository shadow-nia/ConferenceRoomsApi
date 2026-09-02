using ConferenceRooms.Api.Contracts;
using ConferenceRooms.Api.Data;
using ConferenceRooms.Api.Domain;
using ConferenceRooms.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConferenceRooms.Api.Services;

public sealed class BookingManagementService(
    AppDbContext dbContext,
    RentalPriceCalculator priceCalculator)
{
    public async Task<BookingResponse> CreateAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        RentalPriceCalculator.ValidateInterval(request.StartAt, request.EndAt);
        if (request.HallId == Guid.Empty)
        {
            throw new BusinessValidationException("Hall ID is required.");
        }

        var hall = await dbContext.Halls
            .Include(x => x.Services)
            .SingleOrDefaultAsync(x => x.Id == request.HallId && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Hall was not found.");

        var serviceIds = request.ServiceIds?.ToArray() ?? [];
        if (serviceIds.Distinct().Count() != serviceIds.Length)
        {
            throw new BusinessValidationException("Service IDs must be unique.");
        }

        var selectedServices = hall.Services.Where(x => serviceIds.Contains(x.Id)).ToArray();
        if (selectedServices.Length != serviceIds.Length)
        {
            throw new BusinessValidationException("One or more selected services are unavailable for this hall.");
        }

        var startAtUtc = request.StartAt.ToUniversalTime();
        var endAtUtc = request.EndAt.ToUniversalTime();
        var isOccupied = await dbContext.Bookings.AnyAsync(
            x => x.HallId == request.HallId &&
                 x.Status == BookingStatus.Active &&
                 x.StartAt < endAtUtc &&
                 x.EndAt > startAtUtc,
            cancellationToken);

        if (isOccupied)
        {
            throw new ConflictException("The hall is already booked for this time interval.");
        }

        var quote = priceCalculator.Calculate(
            hall.BaseHourlyRate,
            request.StartAt,
            request.EndAt,
            selectedServices);

        var booking = new Booking
        {
            HallId = hall.Id,
            Hall = hall,
            StartAt = startAtUtc,
            EndAt = endAtUtc,
            BaseHourlyRateSnapshot = hall.BaseHourlyRate,
            RoomAmount = quote.RoomAmount,
            ServicesAmount = quote.ServicesAmount,
            TotalAmount = quote.TotalAmount,
            Services = selectedServices.Select(x => new BookingServiceSnapshot
            {
                SourceServiceId = x.Id,
                Name = x.Name,
                Price = x.Price
            }).ToList()
        };

        dbContext.Bookings.Add(booking);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
        {
            throw new ConflictException("The hall was booked by another client. Please choose another time.");
        }

        return Map(booking);
    }

    public async Task<BookingResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var booking = await dbContext.Bookings
            .AsNoTracking()
            .Include(x => x.Hall)
            .Include(x => x.Services)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Booking was not found.");

        return Map(booking);
    }

    private static BookingResponse Map(Booking booking) => new(
        booking.Id,
        booking.HallId,
        booking.Hall.Name,
        booking.StartAt,
        booking.EndAt,
        booking.Status.ToString(),
        booking.BaseHourlyRateSnapshot,
        booking.RoomAmount,
        booking.ServicesAmount,
        booking.TotalAmount,
        booking.Services
            .OrderBy(x => x.Name)
            .Select(x => new BookingServiceResponse(x.SourceServiceId, x.Name, x.Price))
            .ToArray(),
        booking.CreatedAt);
}
