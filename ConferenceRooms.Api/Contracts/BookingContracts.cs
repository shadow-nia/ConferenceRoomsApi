using System.ComponentModel.DataAnnotations;

namespace ConferenceRooms.Api.Contracts;

public sealed record CreateBookingRequest(
    [Required] Guid HallId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    IReadOnlyCollection<Guid>? ServiceIds);

public sealed record BookingServiceResponse(Guid SourceServiceId, string Name, decimal Price);

public sealed record BookingResponse(
    Guid Id,
    Guid HallId,
    string HallName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    decimal BaseHourlyRate,
    decimal RoomAmount,
    decimal ServicesAmount,
    decimal TotalAmount,
    IReadOnlyCollection<BookingServiceResponse> Services,
    DateTimeOffset CreatedAt);
