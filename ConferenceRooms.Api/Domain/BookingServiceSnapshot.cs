namespace ConferenceRooms.Api.Domain;

public sealed class BookingServiceSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid SourceServiceId { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
}
