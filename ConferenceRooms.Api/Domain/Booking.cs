namespace ConferenceRooms.Api.Domain;

public sealed class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HallId { get; set; }
    public Hall Hall { get; set; } = null!;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Active;
    public decimal BaseHourlyRateSnapshot { get; set; }
    public decimal RoomAmount { get; set; }
    public decimal ServicesAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<BookingServiceSnapshot> Services { get; set; } = [];
}

public enum BookingStatus
{
    Active = 1,
    Cancelled = 2
}
