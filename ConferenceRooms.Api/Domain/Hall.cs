namespace ConferenceRooms.Api.Domain;

public sealed class Hall
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public decimal BaseHourlyRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<HallAdditionalService> Services { get; set; } = [];
    public List<Booking> Bookings { get; set; } = [];
}
