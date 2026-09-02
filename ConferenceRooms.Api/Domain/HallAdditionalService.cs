namespace ConferenceRooms.Api.Domain;

public sealed class HallAdditionalService
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HallId { get; set; }
    public Hall Hall { get; set; } = null!;
    public required string Name { get; set; }
    public decimal Price { get; set; }
}
